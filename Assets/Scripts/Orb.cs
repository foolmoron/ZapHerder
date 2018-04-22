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

    public GameObject LinePrefab;
    LineRenderer line;

    Camera camera;
    BestPathBetweenPointsWorker pathWorker;

    JobHandle pathJob;

    void Awake() {
        camera = FindObjectOfType<Camera>();

        line = Instantiate(LinePrefab).GetComponent<LineRenderer>();
    }

    void Start() {
        pathWorker = new BestPathBetweenPointsWorker().Init(Main.Dots.Length, new float2(-19.20f / 2 - 1, -10.80f / 2 - 1), new float2(19.20f / 2 + 1, 10.80f / 2 + 1), new float2(PathBucketSize, PathBucketSize));
    }

    void Update() {
        var em = World.Active.GetExistingManager<EntityManager>();
        pathJob = pathWorker.DoJob(transform.position.to2(), camera.ScreenToWorldPoint(Input.mousePosition).to2(), Main.Dots);
    }
    
    float x;
    int z = 500;
    void LateUpdate() {
        pathJob.Complete();

        var path = pathWorker.GetFinishedPath(transform.position.to2(), camera.ScreenToWorldPoint(Input.mousePosition).to2());
        line.positionCount = path.Count;
        line.SetPositions(path.Map(p => new Vector3(p.x, p.y)));

        x -= Time.deltaTime;
        if (x <= 0) {
            x = 0.01f;
            z++;
        }
        pathWorker.D(z);
    }

    void OnDestroy() {
        pathWorker.Dispose();
    }
}