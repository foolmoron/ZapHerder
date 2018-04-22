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

    Camera camera;
    BestPathBetweenPointsWorker pathWorker;
    List<float2> latestPath = new List<float2>();
    float pathDistance;
    float pathSpeed;

    JobHandle? pathJob;

    void Awake() {
        camera = FindObjectOfType<Camera>();
        line = Instantiate(LinePrefab).GetComponent<LineRenderer>();
        lineWidth = line.GetComponent<LineWidthShaker>();
    }

    void Start() {
        pathWorker = new BestPathBetweenPointsWorker().Init(Main.Dots.Length, new float2(-19.20f / 2 - 1, -10.80f / 2 - 1), new float2(19.20f / 2 + 1, 10.80f / 2 + 1), new float2(PathBucketSize, PathBucketSize));
    }

    void Update() {
        var em = World.Active.GetExistingManager<EntityManager>();
        // move on click
        if (Input.GetMouseButtonDown(0)) {
            Moving = true;
        }
        // do move
        if (Moving) {
            pathJob = null;
            pathDistance += pathSpeed * Time.deltaTime;
            var pathIndex = Mathf.Min(latestPath.Count, Mathf.FloorToInt(pathDistance));
            if (pathIndex >= latestPath.Count) {
                Moving = false;
            } else {
                transform.position = latestPath[pathIndex].to3();
            }
        }
        // do pathing
        else {
            pathJob = pathWorker.DoJob(transform.position.to2(), camera.ScreenToWorldPoint(Input.mousePosition).to2(), Main.Dots);
        }
        // line width
        lineWidth.BaseWidth = Moving ? LineMovingWidth : LineAimingWidth;
    }
    
    float x;
    int z = 500;
    void LateUpdate() {
        if (pathJob != null) {
            pathJob.Value.Complete();

            latestPath = pathWorker.GetFinishedPath(transform.position.to2(), camera.ScreenToWorldPoint(Input.mousePosition).to2());
            pathDistance = 0;
            var pathLengthModifier = Mathf.Pow((float)MedianPath / latestPath.Count, 0.5f);
            pathSpeed = latestPath.Count * pathLengthModifier / BaseMoveDuration;
        }

        var pathIndex = Mathf.Min(latestPath.Count, Mathf.FloorToInt(pathDistance));
        line.positionCount = latestPath.Count - pathIndex;
        var points = new Vector3[line.positionCount];
        for (int p = 0; p < points.Length; p++) {
            points[p] = latestPath[p + pathIndex].to3();
        }
        line.SetPositions(points);

        x -= Time.deltaTime;
        if (x <= 0) {
            x = 0.01f; 
            z++;
        }
        //pathWorker.D(z);
    }

    void OnDestroy() {
        pathWorker.Dispose();
    }
}