using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Menu : Manager<Menu> {

    public bool Started;
    public bool GameOver;

    public GameObject TitleStuff;

    public Orb Orb;

    public Transform Scor;
    public Vector3 ScoreIn;
    public Vector3 ScoreMid;
    public Vector3 ScoreOut;

    public TextMesh BestScore;
    public Vector3 BestScoreIn;
    public Vector3 BestScoreOut;

    public Transform BestScore2;
    public Vector3 BestScore2In;
    public Vector3 BestScore2Out;

    void Awake() {
        Orb.gameObject.SetActive(false);
    }

    public void StartGame() {
        Started = true;
        GameOver = false;
        var em = World.Active.GetExistingManager<EntityManager>();
        foreach (var dot in Main.Dots) {
            if (em.HasComponent<RandomFlowReset>(dot))
                em.RemoveComponent<RandomFlowReset>(dot);
            em.SetSharedComponentData(dot, Main.DotDepthLitRenderer);
        }
        Main.SetupRandomDots();
        Orb.transform.position = Vector3.zero.withZ(-1);
        Orb.gameObject.SetActive(true);
        Orb.enabled = true;
        Orb.line.gameObject.SetActive(true);

        Score.Inst.DesiredScore = 0;
        Score.Inst.RealScore = 0;
        Bonus.Inst.Clear();
    }

    public void EndGame() {
        GameOver = true;
        Time.timeScale = 1;
        var best = Mathf.Max(Score.Inst.RealScore, PlayerPrefs.GetInt("best"));
        PlayerPrefs.SetInt("best", best);
        BestScore.text = best.ToString("n0");

        Orb.gameObject.SetActive(false);

        var deaths = FindObjectsOfType<DeathCircle>();
        foreach (var death in deaths) {
            Destroy(death.gameObject);
        }
    }

    void FixedUpdate() {
    }

    void Update() {
        if ((GameOver || !Started) && Input.GetMouseButtonUp(0)) {
            StartGame();
        }
        RandomFlowSystem.STOP = !Started;
        TitleStuff.SetActive(!Started);
        Scor.position = Vector3.Lerp(Scor.position, GameOver ? ScoreMid : Started ? ScoreIn : ScoreOut, 0.5f);
        BestScore.transform.position = Vector3.Lerp(BestScore.transform.position, GameOver ? BestScoreIn : BestScoreOut, 0.5f);
        BestScore2.position = Vector3.Lerp(BestScore2.position, GameOver ? BestScore2In : BestScore2Out, 0.5f);
    }
}