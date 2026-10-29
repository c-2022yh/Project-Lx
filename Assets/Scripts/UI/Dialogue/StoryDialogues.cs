using System;
using UnityEngine;

/// <summary>
/// 스토리 대화를 어디서 띄울지 모아 둔 목록. Resources/StoryDialogues 에셋 하나만 쓴다.
///   - 게임 시작: 새 게임을 시작하고 첫 맵이 뜨면
///   - 유물: 그 유물을 처음 얻으면 (획득 알림창을 닫은 다음)
///   - 보주 장착: 보주마다 그 보주를 처음 장착하면 (유물창을 닫은 다음). 한 번 보면 저장돼서 다시 안 뜬다.
/// 대사와 이름에 {유물}이라고 쓰면 그 유물 이름으로 바뀐다.
/// 보주 대화의 초상화를 비우면 그 보주 얼굴(RelicData의 Orb Sprite)이 들어간다.
/// 보스를 잡았을 때 대사는 보스방(BossRoom)의 Clear Dialogue 칸에 넣는다.
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

    [Tooltip("보주를 처음 장착하면 (보주마다 한 번, 유물창을 닫은 다음). 아래 목록에 따로 없는 보주는 이걸 쓴다. 대사에 {유물}을 쓰면 장착한 보주 이름이 된다.")]
    [SerializeField] private DialogueData firstOrbEquip;

    [Tooltip("보주마다 따로 대사를 주고 싶으면 여기 넣는다.")]
    [SerializeField] private RelicEntry[] orbEquips = new RelicEntry[0];

    private const float GameStartDelay = 0.8f;   // 화면이 밝아진 뒤에 뜨게
    private const string OrbFlagPrefix = "story.orbEquip.";
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

    /// <summary>보주를 장착했을 때 부른다. 보주마다 처음 한 번만 뜬다.</summary>
    public static void PlayOrbEquip(RelicData orb)
    {
        StoryDialogues s = Instance;
        if (s == null || orb == null) return;

        DialogueData dialogue = s.firstOrbEquip;
        if (s.orbEquips != null)
        {
            foreach (RelicEntry e in s.orbEquips)
                if (e != null && e.relic == orb && e.dialogue != null) dialogue = e.dialogue;
        }
        if (dialogue == null) return;

        string flag = OrbFlagPrefix + orb.name;
        if (RunState.Current.HasFlag(flag)) return;

        RunState.Current.SetFlag(flag);
        DialogueBox.Play(WithRelic(dialogue, orb));
    }

    /// <summary>{유물}을 유물 이름으로 바꾸고, 빈 초상화에 보주 얼굴(없으면 유물 아이콘)을 넣은 복사본.</summary>
    private static DialogueData WithRelic(DialogueData source, RelicData relic)
    {
        DialogueData copy = Instantiate(source);
        copy.lines = new DialogueData.Line[source.lines.Length];

        for (int i = 0; i < source.lines.Length; i++)
        {
            DialogueData.Line from = source.lines[i] ?? new DialogueData.Line();
            copy.lines[i] = new DialogueData.Line
            {
                speaker = (from.speaker ?? "").Replace(RelicToken, relic.RelicName),
                text = (from.text ?? "").Replace(RelicToken, relic.RelicName),
                portrait = from.portrait != null ? from.portrait
                         : relic.OrbSprite != null ? relic.OrbSprite : relic.Icon
            };
        }

        return copy;
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
