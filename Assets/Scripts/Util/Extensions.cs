using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Extensions {

    public static float4x4 toMatrix(this float2 pos) {
        return new float4x4(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            pos.x, pos.y, 0, 1
        );
    }
}
