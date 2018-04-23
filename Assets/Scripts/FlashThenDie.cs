using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FlashThenDie : MonoBehaviour {

    public float FlashInterval = 0.08f;
    public float FlashMaxTime = 0.35f;
    public float DeathTime = 0.7f;
    float time;
    
    Renderer renderer;

    void Awake() {
        renderer = GetComponent<Renderer>();
    }

    void Start() {
    }

    void Update() {
        time += Time.unscaledDeltaTime;
        renderer.enabled = time >= FlashMaxTime || Mathf.FloorToInt(time / FlashInterval) % 2 == 0;
        if (time >= DeathTime) {
            Destroy(gameObject);
        }
    }
}