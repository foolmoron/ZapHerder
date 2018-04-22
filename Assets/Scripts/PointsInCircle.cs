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

public class PointsInCircle : IDisposable {
    
    NativeArray<float2> _Points;
    NativeArray<float> _PointDistances;

    public PointsInCircle Init(int numPoints) {
        _Points = new NativeArray<float2>(numPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _PointDistances = new NativeArray<float>(numPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        return this;
    }

    [ComputeJobOptimization]
    public struct PointDistancesJob : IJobParallelFor {
        [ReadOnly] public float2 Center;
        [ReadOnly] public NativeArray<float2> Points;
        public NativeArray<float> _PointDistances;

        public void Execute(int index) {
            _PointDistances[index] = math.distance(Center, Points[index]);
        }
    }
    
    public JobHandle DoDistances(float2 center, Entity[] entities) {
        var em = World.Active.GetExistingManager<EntityManager>();

        // setup arrays
        for (int i = 0; i < entities.Length; i++) {
            _Points[i] = em.GetComponentData<Translate2D>(entities[i]).Value;
        }

        var pointDistancesJob = new PointDistancesJob {
            Center = center,
            Points =  _Points,
            _PointDistances = _PointDistances,
        }.Schedule(_PointDistances.Length, 64);

        return pointDistancesJob;
    }

    public int CountUnmarked(float radius) {
        var count = 0;
        for (int i = 0; i < _PointDistances.Length; i++) {
            if (_PointDistances[i] <= radius) {
                count++;
            }
        }
        return count;
    }

    public void Dispose() {
        _Points.Dispose();
        _PointDistances.Dispose();
    }
}