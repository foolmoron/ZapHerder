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

        DotArchetype = em.CreateArchetype(typeof(TransformMatrix));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AfterSceneLoad() {
        var em = World.Active.GetOrCreateManager<EntityManager>();

        DotRenderer = GameObject.Find("Prototypes/Dot").GetComponent<MeshInstanceRendererComponent>().Value;

        for (int x = 0; x < 10; x++) {
            for (int y = 0; y < 10; y++) {
                var dot = em.CreateEntity(DotArchetype);
                em.SetComponentData(dot, new TransformMatrix { Value = new float2(x * 1.1f + Mathf.Sin(y * 0.8f) * 0.5f - 5f, y * 1.1f + Mathf.Sin(x * 0.8f) * 0.5f - 5f).toMatrix() });
                em.AddSharedComponentData(dot, DotRenderer);
            }
        }
    }
}
