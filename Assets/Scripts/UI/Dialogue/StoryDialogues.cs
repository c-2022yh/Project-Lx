using System;
using UnityEngine;

/// <summary>
/// 스토리 대화를 어디서 띄울지 모아 둔 목록. Resources/StoryDialogues 에셋 하나만 쓴다.
///   - 게임 시작: 새 게임을 시작하고 첫 맵이 뜨면
///   - 유물: 그 유물을 처음 얻으면 (획득 알림창을 닫은 다음)
///   - 보주 첫 장착: 보주를 처음으로 장착하면 (유물창을 닫은 다음). 한 번 보면 저장돼서 다시 안 뜬다.
/// 대사와 이름에 {유물}이라고 쓰면 장착한 보주 이름으로 바뀐다.
/// 보스를 잡았을 때 대사는 보스방(BossRoom)의 Clear Dialogue 칸에 넣는다. 비우면 여기 Boss Clear를 쓴다.
/// 보스 대사에 {보스}라고 쓰면 그 보스 이름으로 바뀐다.
/// 칸이 비어 있으면 아무것도 안 뜬다.
/// </summary>
[CreateAssetMenu(menuName = "여울잔/스토리 대화 목록", fileName = "StoryDialogues")]
public class StoryDialogues : ScriptableObject
{
    [Serializable]
    public class RelicEntry
    {
        public RelicData relic;
        public DialogueData dialogue;
    }

    [Tooltip("새 게임을 시작하고 첫 맵이 뜨면")]
    [SerializeField] private DialogueData gameStart;

    [Tooltip("이 유물을 처음 얻으면 (획득 알림창 다음). 보주를 얻었을 때 대사도 여기 넣는다.")]
    [SerializeField] private RelicEntry[] relics = new RelicEntry[0];

    [Tooltip("보주를 처음 장착하면 (유물창을 닫은 다음). 대사에 {유물}을 쓰면 장착한 보주 이름이 된다.")]
    [SerializeField] private DialogueData firstOrbEquip;

    [Tooltip("보스를 잡으면 (보스방에 따로 넣은 대사가 없을 때). {보스}는 보스 이름이 된다.")]
    [SerializeField] private DialogueData bossClear;

    // ── 시작 연출 (IntroCutscene, Tutorial 씬 처음 한 번) ──
    [Header("시작 연출 (튜토리얼 처음)")]
    [Tooltip("까만 화면에 한 줄씩 떠오르는 글. 줄(Line)마다 하나씩. 이름·초상화는 안 쓴다.")]
    [SerializeField] private DialogueData introNarration;

    [Tooltip("동굴에서 눈뜬 캐릭터의 첫마디. 대화창 모양이면 대화창으로, 내레이션 모양이면 화면 가운데 글(첫 줄만)로 나온다.")]
    [SerializeField] private DialogueData introWake;

    [Tooltip("보주가 처음 말을 거는 대사 (대화창)")]
    [SerializeField] private DialogueData introOrb;

    [Tooltip("보주를 얻을 때 획득 알림에 나오는 이름")]
    [SerializeField] private string introOrbName = "보주";

    [Tooltip("보주를 얻을 때 획득 알림 아래 설명")]
    [SerializeField] private string introOrbDescription = "곁을 따라다니며 함께 싸운다. 기력이 차면 각성할 수 있다.";

    [Tooltip("연출이 끝나고 뜨는 지역 이름 (비우면 안 뜬다)")]
    [SerializeField] private string introAreaName = "여울 숲";

    [Tooltip("지역 이름 아래 작은 글 (비워도 된다)")]
    [SerializeField] private string introAreaSubtitle = "";

    public static DialogueData IntroNarration => Instance != null ? Instance.introNarration : null;
    public static DialogueData IntroWake => Instance != null ? Instance.introWake : null;
    public static DialogueData IntroOrb => Instance != null ? Instance.introOrb : null;
    public static string IntroOrbName => Instance != null ? Instance.introOrbName : "보주";
    public static string IntroOrbDescription => Instance != null ? Instance.introOrbDescription : "";
    public static string IntroAreaName => Instance != null ? Instance.introAreaName : "";
    public static string IntroAreaSubtitle => Instance != null ? Instance.introAreaSubtitle : "";

    /// <summary>보스방에 따로 넣은 대사가 없을 때 쓰는 기본 보스 처치 대사. 없으면 null.</summary>
    public static DialogueData BossClear => Instance != null ? Instance.bossClear : null;

    private const float GameStartDelay = 0.8f;   // 화면이 밝아진 뒤에 뜨게
    private const string FirstOrbFlag = "story.firstOrbEquip";
    private const string RelicToken = "{유물}";

    private static StoryDialogues cached;
    private static bool looked;

    private static StoryDialogues Instance
    {
        get
        {
            if (!looked)
            {
                looked = true;
                cached = Resources.Load<StoryDialogues>("StoryDialogues");
            }
            return cached;
        }
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cached = null;
        looked = false;
    }
#endif

    public static void PlayGameStart()
    {
        StoryDialogues s = Instance;
        if (s != null && s.gameStart != null) DialogueBox.Play(s.gameStart, null, GameStartDelay);
    }

    /// <summary>보주를 장착했을 때 부른다. 처음 한 번만 뜬다.</summary>
    public static void PlayOrbEquip(RelicData orb)
    {
        StoryDialogues s = Instance;
        if (s == null || s.firstOrbEquip == null || orb == null) return;
        if (RunState.Current.HasFlag(FirstOrbFlag)) return;

        RunState.Current.SetFlag(FirstOrbFlag);
        DialogueBox.Play(WithRelic(s.firstOrbEquip, orb));
    }

    /// <summary>{유물}을 유물 이름으로 바꾸고, 빈 초상화에 보주 얼굴(없으면 유물 아이콘)을 넣은 복사본.</summary>
    private static DialogueData WithRelic(DialogueData source, RelicData relic)
    {
        return source.With(RelicToken, relic.RelicName, relic.OrbSprite != null ? relic.OrbSprite : relic.Icon);
    }

    public static void PlayForRelic(RelicData relic)
    {
        StoryDialogues s = Instance;
        if (s == null || relic == null || s.relics == null) return;

        foreach (RelicEntry e in s.relics)
        {
            if (e == null || e.relic != relic || e.dialogue == null) continue;
            DialogueBox.Play(e.dialogue);
            return;
        }
    }
}
