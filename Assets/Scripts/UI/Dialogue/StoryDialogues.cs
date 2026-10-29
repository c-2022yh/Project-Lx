using System;
using UnityEngine;

/// <summary>
/// 스토리 대화를 어디서 띄울지 모아 둔 목록. Resources/StoryDialogues 에셋 하나만 쓴다.
///   - 게임 시작: 새 게임을 시작하고 첫 맵이 뜨면
///   - 유물: 그 유물을 처음 얻으면 (획득 알림창을 닫은 다음)
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

    private const float GameStartDelay = 0.8f;   // 화면이 밝아진 뒤에 뜨게

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
