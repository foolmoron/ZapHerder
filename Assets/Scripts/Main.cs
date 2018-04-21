using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Transforms2D;
using UnityEngine;

public static class Main {

    public static EntityArchetype DotArchetype;
    public static MeshInstanceRenderer DotRenderer;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void BeforeSceneLoad() {
        var em = World.Active.GetOrCreateManager<EntityManager>();

        DotArchetype = em.CreateArchetype(typeof(Translate2D), typeof(Rotate2D), typeof(Scale2D), typeof(TransformMatrix));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AfterSceneLoad() {
        var em = World.Active.GetOrCreateManager<EntityManager>();

        DotRenderer = GameObject.Find("Prototypes/Dot").GetComponent<MeshInstanceRendererComponent>().Value;

        for (int x = 0; x < 100; x++) {
            for (int y = 0; y < 100; y++) {
                var dot = em.CreateEntity(DotArchetype);
                em.SetComponentData(dot, new Translate2D { Value = new float2(x * 0.1f + Mathf.Sin(y * 0.22f) * 0.1f - 5f, y * 0.1f + Mathf.Sin(x * 0.19f) * 0.1f - 5f) });
                em.SetComponentData(dot, new Scale2D { Value = new float2(0.1f, 0.1f) });
                em.AddSharedComponentData(dot, DotRenderer);
            }
        }
    }
}
