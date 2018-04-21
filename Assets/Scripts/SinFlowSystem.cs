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

public struct SinFlow : IComponentData {
    public float2 Origin;
    public float2 Phase;
    public float2 Amplitude;
    public float2 Frequency;
}
public class SinFlowSystem : JobComponentSystem {
    
    [ComputeJobOptimization]
    struct SinFlowJob : IJobProcessComponentData<Translate2D, Scale2D, SinFlow> {
        public int Length;

        public float Time;
        
        public void Execute(ref Translate2D t, ref Scale2D s, [ReadOnly]ref SinFlow sinflow) {
            t.Value = new float2(
                sinflow.Origin.x + math.sin(sinflow.Phase.x + Time * sinflow.Frequency.x) * sinflow.Amplitude.x,
                sinflow.Origin.y + math.sin(sinflow.Phase.y + Time * sinflow.Frequency.y) * sinflow.Amplitude.y
            );
            //s.Value = new float2(
            //    0.1f + math.sin(sinflow.Phase.x + Time * 1.5f) * 0.1f,
            //    0.1f + math.sin(sinflow.Phase.y + Time * 2.7f) * 0.1f
            //);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var sinflow = new SinFlowJob {Time = Time.timeSinceLevelLoad}.Schedule(this, 64, inputDeps);
        return sinflow;
    }
}