using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Transforms2D;
using UnityEngine;

public class TRSSystem : ComponentSystem {

    struct Transforms {
        public int Length;
        [ReadOnly] public ComponentDataArray<Translate2D> Translate;
        [ReadOnly] public ComponentDataArray<Rotate2D> Rotate;
        [ReadOnly] public ComponentDataArray<Scale2D> Scale;
        public ComponentDataArray<TransformMatrix> Matrix;
    }

    [Inject] Transforms transforms;

    protected override void OnUpdate() {
        for (int i = 0; i < transforms.Length; i++) {
            transforms.Matrix[i] = new TransformMatrix { Value = 
                new float4x4(
                    transforms.Scale[i].Value.x, 0, 0, 0,
                    0, transforms.Scale[i].Value.y, 0, 0,
                    0, 0, 1, 0,
                    transforms.Translate[i].Value.x, transforms.Translate[i].Value.y, 0, 1
                )
            };
        }
    }
}