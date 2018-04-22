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

    Renderer renderer;

    void Awake() {
        renderer = GetComponent<Renderer>();
    }

    void Start() {
        if (PointsInCircleWorker == null) {
            PointsInCircleWorker = new PointsInCircle();
        }
    }

    void Update() {
        time += Time.deltaTime;
        Time.timeScale = TimescaleCurve.Evaluate(time);
        renderer.enabled = time >= FlashMaxTime || Mathf.FloorToInt(time / FlashInterval) % 2 == 0;
    }

    void OnApplicationQuit() {
        PointsInCircleWorker.Dispose();
    }
}