using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Score : Manager<Score> {

    public int DesiredScore;
    public int RealScore;
    [Range(0, 1)]
    public float ScoreSpeed = 0.9f;

    public float ScaleMultiplier;
    float scale;
    public float MinScalePop;
    public float MaxScalePop;
    [Range(0, 1)]
    public float ScaleDecay = 0.5f;

    TextMesh text;

    void Awake() {
        text = GetComponent<TextMesh>();
        scale = transform.localScale.x;
    }

    public void AddScore(int score) {
        var popAmount = (float) score / DesiredScore;
        DesiredScore += score;
        ScaleMultiplier = Mathf.Lerp(MinScalePop, MaxScalePop, popAmount);
    }

    void FixedUpdate() {
        // round up score
        RealScore = Mathf.RoundToInt(Mathf.Lerp(RealScore, DesiredScore, ScoreSpeed));
        // scale
        ScaleMultiplier *= ScaleDecay;
    }

    void Update() {
        text.text = RealScore.ToString("n0");
        var finalScale = scale * (ScaleMultiplier + 1);
        transform.localScale = new Vector3(finalScale, finalScale, 1);
    }
}