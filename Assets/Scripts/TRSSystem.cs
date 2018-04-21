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

public struct Translate2D : IComponentData {
    public float2 Value;
}
public struct Rotate2D : IComponentData {
    public float2 Value;
}
public struct Scale2D : IComponentData {
    public float2 Value;
}
public class TRSSystem : JobComponentSystem {
    
    [ComputeJobOptimization]
    struct TransformsWithJob : IJobParallelFor {
        public int Length;
        [ReadOnly] public ComponentDataArray<Translate2D> Translate;
        [ReadOnly] public ComponentDataArray<Rotate2D> Rotate;
        [ReadOnly] public ComponentDataArray<Scale2D> Scale;
        public ComponentDataArray<TransformMatrix> Matrix;

        public void Execute(int index) {
            Matrix[index] = new TransformMatrix {
                Value =
                new float4x4(
                    Scale[index].Value.x, 0, 0, 0,
                    0, Scale[index].Value.y, 0, 0,
                    0, 0, 1, 0,
                    Translate[index].Value.x, Translate[index].Value.y, 0, 1
                )
            };
        }
    }
    [Inject] TransformsWithJob transformsWithJob;

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var trs = transformsWithJob.Schedule(transformsWithJob.Length, 1, inputDeps);
        return trs;
    }
}