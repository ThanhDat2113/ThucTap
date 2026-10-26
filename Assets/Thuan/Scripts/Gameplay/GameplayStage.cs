using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageFighter
{
    public string displayName;
    public GameObject prefab;
    [Tooltip("De trong = dung Animator Controller co san trong prefab")]
    public RuntimeAnimatorController controller;
    public Sprite avatar;
    public string idleState = "idle";
    public string danceState = "dance";
    public string missState = "miss";
    public bool flipX;
    [Tooltip("Nhan chieu cao muon hien thi")]
    public float heightScale = 1f;
}

// Dua nhan vat nguoi choi (Main1/Main2) va Boss (Graffiti King / DJ Null) len san khau,
// cho ho phan ung theo PERFECT / GOOD / MISS va gan avatar len thanh HP.
public class GameplayStage : MonoBehaviour
{
    [SerializeField] private GameplayHUD hud;

    [Header("Nguoi choi: chon theo CharacterData.gameplayModel (0 = nam, 1 = nu)")]
    [SerializeField] private StageFighter[] players;
    [SerializeField] private List<CharacterData> roster = new List<CharacterData>();

    [Header("Doi thu: Story Mode lay theo tuan, Free Play lay theo bai")]
    [SerializeField] private StageFighter[] opponents;

    [Header("Vi tri (toa do the gioi, la diem dat chan)")]
    [SerializeField] private Vector2 playerFeet = new Vector2(-5.3f, -3.55f);
    [SerializeField] private Vector2 opponentFeet = new Vector2(5.3f, -3.55f);
    [SerializeField] private float playerHeight = 5.4f;
    [SerializeField] private float opponentHeight = 6.2f;
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private int bossFlinchEvery = 10;

    public StageCharacter Player { get; private set; }
    public StageCharacter Opponent { get; private set; }

    private int streak;

    private void Start()
    {
        StageFighter p = PickPlayer();
        StageFighter o = PickOpponent();
        if (p != null) Player = Spawn(p, playerFeet, playerHeight, "Player");
        if (o != null) Opponent = Spawn(o, opponentFeet, opponentHeight, "Opponent");
        if (hud != null)
        {
            hud.Judged += OnJudged;
            StartCoroutine(ApplyAvatars(p, o));
        }
    }

    private void OnDestroy() { if (hud != null) hud.Judged -= OnJudged; }

    private IEnumerator ApplyAvatars(StageFighter p, StageFighter o)
    {
        yield return null; // sau GameplayHUD.Start
        hud.SetPortraits(p != null ? p.avatar : null, o != null ? o.avatar : null);
    }

    private StageFighter PickPlayer()
    {
        if (players == null || players.Length == 0) return null;
        CharacterData me = roster.Find(c => c != null && c.Id == CharacterSelection.SelectedId);
        int model = me != null ? me.gameplayModel : 0;
        return players[Mathf.Clamp(model, 0, players.Length - 1)];
    }

    private StageFighter PickOpponent()
    {
        if (opponents == null || opponents.Length == 0) return null;
        int index;
        if (StorySession.Active && StorySession.Week != null) index = StorySession.Week.opponent;
        else
        {
            SongData song = GameSession.SelectedSong;
            index = 0;
            if (song != null) foreach (char ch in song.name) index += ch; // on dinh tren moi may
        }
        return opponents[index % opponents.Length];
    }

    private StageCharacter Spawn(StageFighter f, Vector2 feet, float height, string objName)
    {
        GameObject go = Instantiate(f.prefab, transform);
        go.name = objName + "_" + f.displayName;
        var anim = go.GetComponentInChildren<Animator>();
        if (anim != null && f.controller != null) anim.runtimeAnimatorController = f.controller;
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = f.flipX;
            sr.sortingOrder = sortingOrder;
            if (sr.sprite != null)
            {
                Bounds b = sr.sprite.bounds;
                float scale = height * f.heightScale / Mathf.Max(0.01f, b.size.y);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                go.transform.localPosition = new Vector3(feet.x, feet.y - b.min.y * scale, 0f);
            }
        }
        var sc = go.AddComponent<StageCharacter>();
        sc.Init(anim, f.idleState, f.danceState, f.missState);
        return sc;
    }

    private void OnJudged(HudJudgement j, int lane)
    {
        if (j == HudJudgement.Miss)
        {
            streak = 0;
            if (Player != null) Player.Fail();
            if (Opponent != null) Opponent.Cheer();   // doi thu che gieu
            return;
        }
        streak++;
        if (Player != null) Player.Cheer();
        if (Opponent != null)
        {
            if (j == HudJudgement.Perfect) Opponent.Hurt();
            if (bossFlinchEvery > 0 && streak % bossFlinchEvery == 0) Opponent.Fail(); // bi ap dao
        }
    }
}
