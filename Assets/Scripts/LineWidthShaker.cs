using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineWidthShaker : MonoBehaviour {

    public float BaseWidth;
    [Range(0, 1.5f)]
    public float ShakePerc = 0.5f;

    LineRenderer line;

    void Awake() {
        line = GetComponent<LineRenderer>();
        if (BaseWidth <= 0) {
            BaseWidth = line.widthMultiplier;
        }
    }

    void Update() {
        line.widthMultiplier = Mathf.Lerp(-1, 1, Random.value) * ShakePerc * BaseWidth + BaseWidth;
    }
}