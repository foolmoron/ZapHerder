using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class DeathCircle : MonoBehaviour {

    static PointsInCircle PointsInCircleWorker;

    public AnimationCurve TimescaleCurve;
    public float FlashInterval = 0.08f;
    public float FlashMaxTime = 0.35f;
    float time;

    public int WowDeath = 100;
    public int GloriousDeath = 300;

    Renderer renderer;
    float radius;

    JobHandle? pointsCircleJob;
    bool pointsCalculated;

    void Awake() {
        renderer = GetComponent<Renderer>();
        radius = GetComponent<SphereCollider>().radius * transform.localScale.x;
    }

    void Start() {
        if (PointsInCircleWorker == null) {
            PointsInCircleWorker = new PointsInCircle().Init(Main.Dots.Length);
        }
    }

    void Update() {
        time += Time.deltaTime;
        Time.timeScale = TimescaleCurve.Evaluate(time);
        renderer.enabled = time >= FlashMaxTime || Mathf.FloorToInt(time / FlashInterval) % 2 == 0;

        if (!pointsCalculated && pointsCircleJob == null) {
            pointsCircleJob = PointsInCircleWorker.DoDistances(transform.position.to2(), Main.Dots);
            pointsCalculated = true;
        }
    }

    void LateUpdate() {
        if (pointsCircleJob != null) {
            pointsCircleJob.Value.Complete();
            var points = PointsInCircleWorker.CountUnmarked(radius);
            Debug.Log(points);
            Bonus.Inst.Zaps += points;
            if (points > GloriousDeath) {
                Bonus.Inst.AddBonus(new BonusRecord { Name = "GLORIOUS DEATH", Amount = 6 });
            } else if (points > WowDeath) {
                Bonus.Inst.AddBonus(new BonusRecord { Name = "WOW DEATH", Amount = 2 });
            }
            Debug.Log("D " + points);
            Bonus.Inst.CommitDelayed(1.25f);
            pointsCircleJob = null;
        }
    }

    void OnApplicationQuit() {
        PointsInCircleWorker.Dispose();
    }
}