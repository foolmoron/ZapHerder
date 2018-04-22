using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Orb : MonoBehaviour {

    [Range(0.1f, 2f)]
    public float PathBucketSize = 0.3f;
    [Range(0, 5)]
    public int HitsToDie = 2;
    int hits;
    [Range(0, 2)]
    public float StartHitLeniency = 0.5f;

    [Range(0, 1f)]
    public float BaseMoveDuration = 0.5f;
    [Range(0, 100)]
    public int MedianPath = 30;
    public bool Moving;

    public GameObject LinePrefab;
    LineRenderer line;
    LineWidthShaker lineWidth;
    [Range(0, 0.5f)]
    public float LineAimingWidth;
    [Range(0, 0.5f)]
    public float LineMovingWidth;

    public GameObject DeathCirclePrefab;
    public float DeathCircleZ = 10;

    Camera camera;
    BestPathBetweenPointsWorker pathWorker;
    List<float2> latestPath = new List<float2>();
    int prevPathIndex;
    float pathDistance;
    float pathSpeed;

    JobHandle? pathJob;

    int totalMarked;

    public float TimeToClickFast = 0.7f;
    public float TimeToClickLudicrous = 0.3f;
    float lastTimeReadyToClick;

    public float LongDistanceLength = 40;
    public float SharpshooterLength = 70;

    float z;

    void Awake() {
        camera = FindObjectOfType<Camera>();
        line = Instantiate(LinePrefab).GetComponent<LineRenderer>();
        lineWidth = line.GetComponent<LineWidthShaker>();
        z = transform.position.z;
    }

    void Start() {
        pathWorker = new BestPathBetweenPointsWorker().Init(Main.Dots.Length, new float2(-19.20f / 2 - 1, -10.80f / 2 - 1), new float2(19.20f / 2 + 1, 10.80f / 2 + 1), new float2(PathBucketSize, PathBucketSize));
    }

    void Update() {
        var em = World.Active.GetExistingManager<EntityManager>();
        // move on click
        if (Input.GetMouseButtonDown(0)) {
            Moving = true;
            Bonus.Inst.Commit();
            var timeToClick = Time.realtimeSinceStartup - lastTimeReadyToClick;
            if (timeToClick < TimeToClickLudicrous) {
                Bonus.Inst.AddBonus(new BonusRecord { Name = "GOTTA GO FAST", Amount = 4 });
            } else if (timeToClick < TimeToClickFast) {
                Bonus.Inst.AddBonus(new BonusRecord { Name = "FAST CLICK", Amount = 2 });
            }
            Debug.Log("T " + timeToClick);
        }
        // do move
        if (Moving) {
            pathJob = null;
            pathDistance += pathSpeed * Time.deltaTime;
            var pathIndex = Mathf.Min(latestPath.Count, Mathf.FloorToInt(pathDistance));
            if (pathIndex >= latestPath.Count) {
                Moving = false;
                Bonus.Inst.CommitDelayed(0.7f);
                lastTimeReadyToClick = Time.realtimeSinceStartup;
                if (latestPath.Count >= LongDistanceLength) {
                    Bonus.Inst.AddBonus(new BonusRecord { Name = "LONG DISTANCE", Amount = 2 });
                } else if (latestPath.Count >= SharpshooterLength) {
                    Bonus.Inst.AddBonus(new BonusRecord { Name = "SHARPSHOOTER", Amount = 5 });
                }
                Debug.Log("L " + latestPath.Count);
            } else if (pathIndex != prevPathIndex) {
                for (int p = prevPathIndex + 1; p <= pathIndex; p++) {
                    // move
                    transform.position = latestPath[pathIndex].to3(z);
                    // death (immune for near points)
                    var distFromBeginning = math.length(latestPath[pathIndex] - latestPath[0]);
                    if (distFromBeginning > StartHitLeniency && pathWorker.DidTouchMarked(p)) {
                        hits++;
                        Bonus.Inst.AddBonus(new BonusRecord { Name = "RISKY", Amount = 3 });
                    }
                    if (hits >= HitsToDie) { 
                        var deathCircle = Instantiate(DeathCirclePrefab, transform.position.withZ(DeathCircleZ), Quaternion.identity);
                        // disable stuff
                        enabled = false;
                        foreach (var dot in Main.Dots) {
                            em.AddComponentData(dot, new RandomFlowReset { Decay = 0.35f, IntervalDecay = 0.9f, });
                        }
                        line.gameObject.SetActive(false);
                        Bonus.Inst.CommitDelayed(0.7f);
                        break;
                    } else {
                        // mark dots
                        var marked = pathWorker.DoMarking(p);
                        totalMarked += marked;
                        Bonus.Inst.Zaps = totalMarked;
                    }
                }
            }
            prevPathIndex = pathIndex;
        }
        // do pathing
        else {
            pathJob = pathWorker.DoPathing(transform.position.to2(), camera.ScreenToWorldPoint(Input.mousePosition).to2(), Main.Dots);
            prevPathIndex = 0;
            hits = 0;
            totalMarked = 0;
        }
        // line width
        lineWidth.BaseWidth = Moving ? LineMovingWidth : LineAimingWidth;
    }
    
    void LateUpdate() {
        // pathing
        if (pathJob != null) {
            pathJob.Value.Complete();

            latestPath = pathWorker.GetFinishedPath(transform.position.to2(), camera.ScreenToWorldPoint(Input.mousePosition).to2());
            pathDistance = 0;
            var pathLengthModifier = Mathf.Pow((float)MedianPath / latestPath.Count, 0.5f);
            pathSpeed = latestPath.Count * pathLengthModifier / BaseMoveDuration;
        }
        // line stuff
        var pathIndex = Mathf.Min(latestPath.Count, Mathf.FloorToInt(pathDistance));
        line.positionCount = latestPath.Count - pathIndex;
        var points = new Vector3[line.positionCount];
        for (int p = 0; p < points.Length; p++) {
            points[p] = latestPath[p + pathIndex].to3();
        }
        line.SetPositions(points);
    }

    void OnDestroy() {
        pathWorker.Dispose();
    }
}