using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
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