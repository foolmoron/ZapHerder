using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class BestPathBetweenPointsWorker : IDisposable {

    public const int MAX_ENTITIES_PER_BUCKET = 32;

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

    List<float2> path;
    List<Entity> pathDots;

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

        _DirsToCheck = new NativeArray<int2>(new[] {
            new int2(1, 1),
            new int2(1, 0),
            new int2(1, -1),
            new int2(0, -1),
            new int2(-1, -1),
            new int2(-1, 0),
            new int2(-1, 1),
            new int2(0, 1),
        }, Allocator.Persistent);
        

        path = new List<float2>(bucketCount);
        pathDots = new List<Entity>(bucketCount);

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

        float pathHeuristic(int bucketIndex, int2 dir, int2 optimalDir) {
            var dirFactor = math.pow(math.max(0, math.csum(math.abs(optimalDir - dir)) - 1), 4) * 2;
            var entityFactor = BucketEntityCounts[bucketIndex];
            return entityFactor - dirFactor;
        }

        public void Execute() {
            var startBucket = new int2(Mathf.FloorToInt((Start.x - Min.x) / BucketSize.x), Mathf.FloorToInt((Start.y - Min.y) / BucketSize.y));
            startBucket = math.clamp(startBucket, new int2(0, 0), BucketCounts - new int2(1, 1));
            var endBucket = new int2(Mathf.FloorToInt((End.x - Min.x) / BucketSize.x), Mathf.FloorToInt((End.y - Min.y) / BucketSize.y));
            endBucket = math.clamp(endBucket, new int2(0, 0), BucketCounts - new int2(1, 1));
            
            var currentBucket = startBucket;
            while (math.any(currentBucket != endBucket)) { // go until we reach the end
                // or if the best path is out of bounds because the mouse is out of bounds
                if (math.any(currentBucket < 0) || math.any(currentBucket >= BucketCounts)) {
                    break;
                }

                BucketPath[pathIndex] = currentBucket;
                pathIndex++;

                if (pathIndex > 3000) {
                }

                var vectorToEnd = math.normalize(endBucket - currentBucket);
                var signsToEnd = (int2) math.sign(vectorToEnd);

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
                    var h = pathHeuristic(newBucketIndex, dir, signsToEnd);
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

    public JobHandle DoPathing(float2 start, float2 end, Entity[] entities) {
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

    public List<float2> GetFinishedPath(float2 start, float2 end) {
        var em = World.Active.GetExistingManager<EntityManager>();
        path.Clear();
        pathDots.Clear();
        path.Add(start);
        for (int i = 0; i < _BucketPath.Length; i++) {
            if (math.any(_BucketPath[i] < 0)) {
                break;
            }
            var bucketIndex = _BucketPath[i].x + _BucketPath[i].y * BucketCounts.x;
            var pointIndex = Random.Range(0, _BucketEntityCounts[bucketIndex]);
            var dotIndex = _BucketEntities[bucketIndex * MAX_ENTITIES_PER_BUCKET + pointIndex];
            if (dotIndex < 0 || dotIndex >= Main.Dots.Length) {
                break;
            }
            var e = Main.Dots[dotIndex];
            var newPath = em.GetComponentData<Translate2D>(e).Value;
            if (i > 1 && math.csum(math.abs(newPath - path[i])) > 3f) {
                break;
            }
            path.Add(newPath);
            pathDots.Add(e);
        }
        path.Add(end);
        return path;
    }
    
    public int DoMarking(int pathIndex) {
        pathIndex--; // due to start point
        if (pathIndex >= 0 && pathIndex < _BucketPath.Length && math.all(_BucketPath[pathIndex] != -1)) {
            var em = World.Active.GetExistingManager<EntityManager>();
            var bucketIndex = _BucketPath[pathIndex].x + _BucketPath[pathIndex].y * BucketCounts.x;
            var count = _BucketEntityCounts[bucketIndex];
            var marked = 0;
            for (int i = 0; i < count; i++) {
                var dot = Main.Dots[_BucketEntities[bucketIndex * MAX_ENTITIES_PER_BUCKET + i]];
                if (em.GetSharedComponentData<MeshInstanceRenderer>(dot).material != Main.DotAlwaysLitRenderer.material) {
                    em.SetSharedComponentData(dot, Main.DotAlwaysLitRenderer);
                    marked++;
                }
            }
            return marked;
        }
        return 0;
    }

    public bool DidTouchMarked(int pathIndex) {
        pathIndex--; // due to start point
        if (pathIndex >= 0 && pathIndex < pathDots.Count) {
            var em = World.Active.GetExistingManager<EntityManager>();
            var dot = pathDots[pathIndex];
            return em.GetSharedComponentData<MeshInstanceRenderer>(dot).material == Main.DotAlwaysLitRenderer.material;
        }
        return false;
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