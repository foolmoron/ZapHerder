using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BestPathBetweenPointsWorker : IDisposable {

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
    NativeArray<int2> _BucketPath;
    NativeArray<int2> _DirsToCheck;

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
        _BucketPath = new NativeArray<int2>(bucketCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _DirsToCheck = new NativeArray<int2>(3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

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
    
    [ComputeJobOptimization]
    public struct FindBestPathJob : IJob {
        [ReadOnly] public float2 Start;
        [ReadOnly] public float2 End;
        [ReadOnly] public float2 Min;
        [ReadOnly] public float2 BucketSize;
        [ReadOnly] public int2 BucketCounts;
        [ReadOnly] public NativeArray<int> BucketEntityCounts;
        public NativeArray<int2> BucketPath;
        public NativeArray<int2> DirsToCheck;
        int pathIndex;

        float pathHeuristic(int bucketIndex, int2 bucket, int2 endBucket) {
            return BucketEntityCounts[bucketIndex] - math.distance(bucket, endBucket);
        }

        public void Execute() {
            var startBucket = new int2(Mathf.FloorToInt((Start.x - Min.x) / BucketSize.x), Mathf.FloorToInt((Start.y - Min.y) / BucketSize.y));
            startBucket = math.min(startBucket, BucketCounts - new int2(1, 1));
            var endBucket = new int2(Mathf.FloorToInt((End.x - Min.x) / BucketSize.x), Mathf.FloorToInt((End.y - Min.y) / BucketSize.y));
            endBucket = math.min(endBucket, BucketCounts - new int2(1, 1));
            
            var currentBucket = startBucket;
            while (math.any(currentBucket != endBucket)) {
                BucketPath[pathIndex] = currentBucket;
                pathIndex++;

                if (pathIndex > 3000) {
                }

                var vectorToEnd = math.normalize(endBucket - currentBucket);
                var signsToEnd = (int2) math.sign(vectorToEnd);
                if (math.abs(vectorToEnd.y) > math.abs(vectorToEnd.x)) {
                    DirsToCheck[0] = new int2(0, signsToEnd.y);
                    DirsToCheck[1] = new int2(-1, signsToEnd.y);
                    DirsToCheck[2] = new int2(1, signsToEnd.y);
                } else {
                    DirsToCheck[0] = new int2(signsToEnd.y, 0);
                    DirsToCheck[1] = new int2(signsToEnd.y, -1);
                    DirsToCheck[2] = new int2(signsToEnd.y, 1);
                }

                var bestHeuristic = 0f;
                var bestBucket = currentBucket + signsToEnd;
                if (pathIndex % 4 == 0) {
                    bestBucket.x = currentBucket.x;
                }
                if (pathIndex % 4 == 1) {
                    bestBucket.y = currentBucket.y;
                }
                for (int i = 0; i < DirsToCheck.Length; i++) {
                    var dir = DirsToCheck[i];
                    var newBucket = currentBucket + dir;
                    var newBucketIndex = newBucket.x + newBucket.y * BucketCounts.x;
                    var valid = newBucketIndex >= 0 && newBucketIndex < BucketEntityCounts.Length;
                    if (pathIndex > 1) {
                        var prevDir = BucketPath[pathIndex - 1] - BucketPath[pathIndex - 2];
                        if (math.all(dir == prevDir)) {
                            valid = false;
                        }
                    }
                    for (int j = pathIndex - 1; j >= 0 && valid; j--) {
                        if (math.all(BucketPath[j] == newBucket)) {
                            valid = false;
                            break;
                        }
                    }
                    if (!valid) {
                        continue;
                    }
                    var h = pathHeuristic(newBucketIndex, newBucket, endBucket);
                    if (h > bestHeuristic) {
                        bestHeuristic = h;
                        bestBucket = newBucket;
                    }
                }
                currentBucket = bestBucket;
            }
            BucketPath[pathIndex] = endBucket;
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
        for (int i = 0; i < _BucketPath.Length; i++) {
            _BucketPath[i] = -1;
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

        var findBestPathJob = new FindBestPathJob {
            Start = start,
            End = end,
            Min = MinPosition,
            BucketSize = BucketSize,
            BucketCounts = BucketCounts,
            BucketEntityCounts = _BucketEntityCounts,
            BucketPath = _BucketPath,
            DirsToCheck = _DirsToCheck,
        }.Schedule(pointsToBucketsJob);

        return findBestPathJob;
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
            var lit = false;
            foreach (var path in _BucketPath) {
                if (math.any(path < 0)) break;
                if ((path.x + path.y * BucketCounts.x) == c) {
                    lit = true;
                    break;
                }
            }
            for (int i = 0; i < count; i++) {
                var e = Main.Dots[_BucketEntities[c * MAX_ENTITIES_PER_BUCKET + i]];
                em.SetSharedComponentData(e, lit ? Main.DotAlwaysLitRenderer : Main.DotDepthLitRenderer);
            }
        }
    }

    public void Dispose() {
        _Points.Dispose();
        _PointsXBuckets.Dispose();
        _PointsYBuckets.Dispose();
        _BucketEntityCounts.Dispose();
        _BucketEntities.Dispose();
        _BucketPath.Dispose();
        _DirsToCheck.Dispose();
    }
}