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
    public static MeshInstanceRenderer DotDepthLitRenderer;
    public static MeshInstanceRenderer DotAlwaysLitRenderer;

    public static Entity[] Dots { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void BeforeSceneLoad() {
        var em = World.Active.GetOrCreateManager<EntityManager>();

        DotArchetype = em.CreateArchetype(typeof(Translate2D), typeof(Rotate2D), typeof(Scale2D), typeof(TransformMatrix));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AfterSceneLoad() {
        var em = World.Active.GetOrCreateManager<EntityManager>();

        DotDepthLitRenderer = GameObject.Find("Prototypes/DotDepthLit").GetComponent<MeshInstanceRendererComponent>().Value;
        DotAlwaysLitRenderer = GameObject.Find("Prototypes/DotAlwaysLit").GetComponent<MeshInstanceRendererComponent>().Value;

        const int X = 192;
        const int Y = 108;
        Dots = new Entity[X * Y];
        for (int x = 0; x < 192; x++) {
            for (int y = 0; y < 108; y++) {
                var dot = em.CreateEntity(DotArchetype);

                em.SetComponentData(dot, new Translate2D { Value = new float2(-19.20f / 2 + x * 0.1f, -10.80f / 2 + y * 0.1f) });
                em.SetComponentData(dot, new Scale2D { Value = new float2(0.06f) });
                //em.AddComponentData(dot, new SinFlow {
                //    Origin = em.GetComponentData<Translate2D>(dot).Value,
                //    Phase = new float2(y * 0.23f, x * 0.23f),
                //    Amplitude = new float2(0.6f, 0.6f),
                //    Frequency = new float2(3.8f, 3.1f + y * 0.02f),
                //});
                //em.AddComponentData(dot, new NoiseFlow {
                //    Origin = em.GetComponentData<Translate2D>(dot).Value,
                //    Phase = new float2(y * 0.1f, x * 0.1f),
                //    Amplitude = new float2(0.9f, 0.9f),
                //    Frequency = new float2(2f, 2f),
                //});
                em.AddComponentData(dot, new RandomFlow {
                    Seed = Random.value,
                    Origin = em.GetComponentData<Translate2D>(dot).Value,
                    Interval = 0.5f,
                    MaxAmplitude = 0.35f,
                    _Time = 2 + Random.value,
                });
                em.AddSharedComponentData(dot, DotDepthLitRenderer);

                Dots[x + y * X] = dot;
            }
        }
    }
}
