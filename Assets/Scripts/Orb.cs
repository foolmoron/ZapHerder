using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Orb : MonoBehaviour {

    Camera camera;
    BestPathBetweenPointsWorker pathWorker;

    JobHandle pathJob;

    void Awake() {
        camera = FindObjectOfType<Camera>();
    }

    void Start() {
        pathWorker = new BestPathBetweenPointsWorker().Init(Main.Dots.Length, new float2(-19.20f / 2 - 1, -10.80f / 2 - 1), new float2(19.20f / 2 + 1, 10.80f / 2 + 1), new float2(1f, 1f));
    }

    void Update() {
        var em = World.Active.GetExistingManager<EntityManager>();
        pathJob = pathWorker.DoJob(transform.position.to2(), camera.ScreenToWorldPoint(Input.mousePosition).to2(), Main.Dots);
    }
    
    float x;
    public int z;
    void LateUpdate() {
        x -= Time.deltaTime;
        if (x <= 0) {
            x = 0.05f;
            z++;
        }
        pathJob.Complete();
        pathWorker.D(z);
    }
}