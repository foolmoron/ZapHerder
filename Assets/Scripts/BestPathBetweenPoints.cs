using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BestPathBetweenPointsWorker {

    public const int MAX_ENTITIES_PER_BUCKET = 5120;

    struct Bucket {
        public NativeArray<int> Entities;
        public int EntityCount;
    }
    
    float2 MinPosition;
    float2 BucketSize;
    int2 BucketCounts;

    NativeArray<float2> _Points;

    NativeArray<int> _PointsXBuckets;
    NativeArray<int> _PointsYBuckets;

    NativeArray<int> _BucketEntityCounts;
    NativeArray<int> _BucketEntities;

    public BestPathBetweenPointsWorker Init(int numPoints, float2 minPosition, float2 maxPosition, float2 bucketSize) {
        MinPosition = minPosition;
        BucketSize = bucketSize;

        _Points = new NativeArray<float2>(numPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _PointsXBuckets = new NativeArray<int>(numPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _PointsYBuckets = new NativeArray<int>(numPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        var bucketCounts = math.ceil((maxPosition - minPosition) / bucketSize);
        BucketCounts = new int2(Mathf.RoundToInt(bucketCounts.x), Mathf.RoundToInt(bucketCounts.y));
        var bucketCount = BucketCounts.x * BucketCounts.y;
        _BucketEntityCounts = new NativeArray<int>(bucketCount, Allocator.Persistent);
        _BucketEntities = new NativeArray<int>(bucketCount * MAX_ENTITIES_PER_BUCKET, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        return this;
    }
    
    [ComputeJobOptimization]
    public struct PointsXBucketsJob : IJobParallelFor {
        [ReadOnly] public float MinX;
        [ReadOnly] public float BucketsSizeX;
        [ReadOnly] public NativeArray<float2> Points;
        public NativeArray<int> PointsXBuckets;

        public void Execute(int index) {
            PointsXBuckets[index] = Mathf.FloorToInt((Points[index].x - MinX) / BucketsSizeX);
        }
    }
    
    [ComputeJobOptimization]
    public struct PointsYBucketsJob : IJobParallelFor {
        [ReadOnly] public float MinY;
        [ReadOnly] public float BucketsSizeY;
        [ReadOnly] public NativeArray<float2> Points;
        public NativeArray<int> PointsYBuckets;

        public void Execute(int index) {
            PointsYBuckets[index] = Mathf.FloorToInt((Points[index].y - MinY) / BucketsSizeY);
        }
    }

    [ComputeJobOptimization]
    public struct PointsToBucketsJob : IJob {
        [ReadOnly] public int BucketCountX;
        [ReadOnly] public NativeArray<int> PointsXBuckets;
        [ReadOnly] public NativeArray<int> PointsYBuckets;
        public NativeArray<int> BucketEntityCounts;
        public NativeArray<int> BucketEntities;
        
        public void Execute() {
            for (int index = 0; index < PointsXBuckets.Length; index++) {
                var bucketIndex = PointsXBuckets[index] + PointsYBuckets[index] * BucketCountX;
                var bucketEntitySlotIndex = bucketIndex * MAX_ENTITIES_PER_BUCKET + BucketEntityCounts[bucketIndex]; // get slot index before incrementing count

                BucketEntityCounts[bucketIndex] = math.min(MAX_ENTITIES_PER_BUCKET - 1, BucketEntityCounts[bucketIndex] + 1);
                BucketEntities[bucketEntitySlotIndex] = index;
            }
        }
    }


    public JobHandle DoJob(float2 start, float2 end, Entity[] entities) {
        var em = World.Active.GetExistingManager<EntityManager>();
        
        // setup arrays
        for (int i = 0; i < entities.Length; i++) {
            _Points[i] = em.GetComponentData<Translate2D>(entities[i]).Value;
        }

        for (int i = 0; i < _BucketEntityCounts.Length; i++) {
            _BucketEntityCounts[i] = 0;
        }

        // jobs
        var pointsXBucketsJob = new PointsXBucketsJob {
            MinX = MinPosition.x,
            BucketsSizeX = BucketSize.x,
            Points = _Points,
            PointsXBuckets = _PointsXBuckets,
        }.Schedule(_PointsXBuckets.Length, 128);

        var pointsYBucketsJob = new PointsYBucketsJob {
            MinY = MinPosition.y,
            BucketsSizeY = BucketSize.y,
            Points = _Points,
            PointsYBuckets = _PointsYBuckets,
        }.Schedule(_PointsYBuckets.Length, 128);
        
        var pointsToBucketsJob = new PointsToBucketsJob {
            BucketCountX = BucketCounts.x,
            PointsXBuckets = _PointsXBuckets,
            PointsYBuckets = _PointsYBuckets,
            BucketEntityCounts = _BucketEntityCounts,
            BucketEntities = _BucketEntities,
        }.Schedule(JobHandle.CombineDependencies(pointsXBucketsJob, pointsYBucketsJob));

        return pointsToBucketsJob;
    }

    StringBuilder s = new StringBuilder();
    public void D(int z) {
        var em = World.Active.GetExistingManager<EntityManager>();
        //s.Clear();
        //for (int i = 0; i < BucketCounts.x * BucketCounts.y; i++) {
        //    s.Append(i.ToString()).Append(": ").Append(_BucketEntityCounts[i]).AppendLine();
        //}
        //Debug.Log(s.ToString());
        for (int c = 0; c < _BucketEntityCounts.Length; c++) {
            var count = _BucketEntityCounts[c];
            var lit = (z % _BucketEntityCounts.Length) == c;
            for (int i = 0; i < count; i++) {
                var e = Main.Dots[_BucketEntities[c * MAX_ENTITIES_PER_BUCKET + i]];
                em.SetSharedComponentData(e, lit ? Main.DotAlwaysLitRenderer : Main.DotDepthLitRenderer);
            }
        }
    }

    void OnDestroy() {
        _Points.Dispose();
        _PointsXBuckets.Dispose();
        _PointsYBuckets.Dispose();
        _BucketEntityCounts.Dispose();
        _BucketEntities.Dispose();
    }
}