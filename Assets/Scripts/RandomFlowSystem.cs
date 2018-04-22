using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Transforms2D;
using UnityEngine;

public struct RandomFlow : IComponentData {
    public float Seed;
    public float2 Origin;
    public float Interval;
    public float MaxAmplitude;

    public float _Time;
    public int _IntervalCount;
    public float2 _CurrentOffset;
    public float2 _TargetOffset;
}
public struct RandomFlowReset : IComponentData {
    public float Decay;
    public float IntervalDecay;
}
public class RandomFlowSystem : JobComponentSystem {
    
    [ComputeJobOptimization]
    struct RandomFlowJob : IJobProcessComponentData<Translate2D, RandomFlow> {
        public int Length;

        public float dt;

        float rand(ref RandomFlow randomflow, float offset) => 
            randomflow.Seed.random(randomflow._IntervalCount * 5 + offset);
        
        public void Execute(ref Translate2D t, ref RandomFlow randomflow) {
            randomflow._Time -= dt;
            if (randomflow._Time <= 0) {
                randomflow._IntervalCount++;
                randomflow._Time = randomflow.Interval;
                randomflow._CurrentOffset = randomflow._TargetOffset;
                randomflow._TargetOffset = new float2(rand(ref randomflow, 1) - 0.5f, rand(ref randomflow, 2) - 0.5f) * 2f * randomflow.MaxAmplitude;
            }
            var target = randomflow._TargetOffset;
            var offset = math.lerp(randomflow._CurrentOffset, target, 1 - (randomflow._Time / randomflow.Interval));
            t.Value = randomflow.Origin + offset;
        }
    }
    
    [ComputeJobOptimization]
    struct RandomFlowResetJob : IJobProcessComponentData<RandomFlow, RandomFlowReset> {
        public int Length;
        
        public void Execute(ref RandomFlow randomflow, [ReadOnly] ref RandomFlowReset randomreset) {
            randomflow.MaxAmplitude = randomflow.MaxAmplitude * randomreset.Decay;
            randomflow.Interval = math.max(0.01f, randomflow.Interval * randomreset.IntervalDecay);
            randomflow._Time = randomflow._Time * randomreset.IntervalDecay;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var randomreset = new RandomFlowResetJob { }.Schedule(this, 64, inputDeps);
        var randomflow = new RandomFlowJob { dt = Time.deltaTime }.Schedule(this, 64, JobHandle.CombineDependencies(inputDeps, randomreset));
        return randomflow;
    }
}