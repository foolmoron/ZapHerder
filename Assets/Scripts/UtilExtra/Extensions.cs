using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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

    public static float random(this float val1, float val2) {
        return math.frac(math.sin(val1 * 12.9898f + val2 * 78.233f) * 43758.5453123f);
    }
}
