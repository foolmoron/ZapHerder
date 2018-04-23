using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct BonusRecord {
    public string Name;
    public float Amount;
}
public class Bonus : Manager<Bonus> {

    public int Zaps;
    public List<BonusRecord> Bonuses = new List<BonusRecord>();

    public float ScaleMultiplier;
    float scale;
    public float MinScalePop;
    public float MaxScalePop;
    [Range(0, 1)]
    public float ScaleDecay = 0.5f;

    public GameObject BonusPrefab;
    public Vector2 PopupMin;
    public Vector2 PopupMax;
    public float RotationMax;

    float commitDelay;

    TextMesh text;

    void Awake() {
        text = GetComponent<TextMesh>();
        scale = transform.localScale.x;
    }

    public void Clear() {
        Zaps = 0;
        Bonuses.Clear();
    }

    public void Commit() {
        commitDelay = 0;
        Score.Inst.DesiredScore += GetTotal();
        Clear();
    }

    public void CommitDelayed(float time) {
        commitDelay = time;
    }

    public void AddBonus(BonusRecord bonus) {
        Bonuses.Add(bonus);
        var popAmount = bonus.Amount;
        ScaleMultiplier = Mathf.Lerp(MinScalePop, MaxScalePop, popAmount);

        var pos = new Vector3(Mathf.Lerp(PopupMin.x, PopupMax.x, Random.value), Mathf.Lerp(PopupMin.y, PopupMax.y, Random.value), -5f);
        var rot = Mathf.Lerp(-RotationMax, RotationMax, Random.value);
        var bonusText = Instantiate(BonusPrefab, pos, Quaternion.Euler(0, 0, rot)).GetComponentInChildren<TextMesh>();
        bonusText.text = bonus.Name + "\nx" + bonus.Amount.ToString("0.0");
    }

    public int GetTotal() {
        var total = (float)Zaps;
        foreach (var bonus in Bonuses) {
            total *= bonus.Amount;
        }
        return Mathf.FloorToInt(total);
    }

    void FixedUpdate() {
        // scale
        ScaleMultiplier *= ScaleDecay;
        // delay
        if (commitDelay > 0) {
            commitDelay -= Time.unscaledDeltaTime;
            if (commitDelay <= 0) {
                Commit();
            }
        }
    }

    void Update() {
        var str = "";
        if (Zaps > 0) {
            str = Zaps.ToString("n0") + " Zaps ";
            str += string.Join(" ", Bonuses.Map(b => "x " + b.Amount.ToString("0.0")));
            if (Bonuses.Count > 0) {
                str += " = " + GetTotal().ToString("n0");
            }
        }
        text.text = str;

        var finalScale = scale * (ScaleMultiplier + 1);
        transform.localScale = new Vector3(finalScale, finalScale, 1);
    }
}