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

public struct NoiseFlow : IComponentData {
    public float2 Origin;
    public float2 Phase;
    public float2 Amplitude;
    public float2 Frequency;
}
public class NoiseFlowSystem : JobComponentSystem {
    
    [ComputeJobOptimization]
    struct NoiseFlowJob : IJobProcessComponentData<Translate2D, NoiseFlow> {
        public int Length;

        public float Time;
        
        public void Execute(ref Translate2D t, [ReadOnly]ref NoiseFlow noiseflow) {
            var noise = Mathf.PerlinNoise(noiseflow.Phase.x + Time * noiseflow.Frequency.x, noiseflow.Phase.y + Time * noiseflow.Frequency.y);
            var offset = new float2(math.cos(noise * Mathf.PI * 2), math.sin(noise * Mathf.PI * 2)) * noiseflow.Amplitude.x;
            t.Value = new float2(
                noiseflow.Origin.x + offset.x * noiseflow.Amplitude.x,
                noiseflow.Origin.y + offset.x * noiseflow.Amplitude.y
            );
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var noiseflow = new NoiseFlowJob { Time = Time.timeSinceLevelLoad}.Schedule(this, 64, inputDeps);
        return noiseflow;
    }
}