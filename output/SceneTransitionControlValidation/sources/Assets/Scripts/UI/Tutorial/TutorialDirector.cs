using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 튜토리얼 안내를 하나씩 순서대로 띄우고, 플레이어가 그 행동을 하면 닫는다.
///
/// - 한 번 본 안내는 RunState 플래그로 남겨 다시 띄우지 않는다. 새 게임이면 다시 뜬다.
/// - 창이 떠 있거나 지도를 보는 동안은 숨겼다가, 닫히면 다시 띄운다.
/// - 씬에도 프리팹에도 붙이지 않는다. 스스로 생겨 DontDestroyOnLoad로 산다.
///   RelicRuntimeBridge와 같은 방식이고, 남의 프리팹을 건드리지 않기 위해서다.
///
/// 안내가 뜨는 경우:
///   1) 플레이어가 있는 첫 씬: 이동 → 점프 → 지도 → (장착한 스킬이 있으면) 스킬
///   2) 게임 알림: 제단 근처, 유물 상자 근처, 새 유물 획득, 기력 가득
///   3) TutorialTrigger 영역에 들어갔을 때 (맵에 놓는 건 맵 담당)
///
/// 플레이어 쪽 코드는 고치지 않았다. 공개 이벤트와 PlayerInput 액션만 읽는다.
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    private const string FlagPrefix = "tutorial.";
    private const float StartDelay = 1f;        // 씬이 뜨자마자 덮지 않게
    private const float MinShowTime = 0.5f;     // 들고 있던 키 때문에 바로 닫히지 않게
    private const float GapBetweenSteps = 0.6f;
    private const float NearbyPollInterval = 0.25f;

    private static TutorialDirector instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject go = new GameObject("[TutorialDirector]");
        instance = go.AddComponent<TutorialDirector>();
        DontDestroyOnLoad(go);
    }

    // ── 밖에서 부르는 곳 ─────────────────────

    public static void Request(TutorialPreset preset) => Request(TutorialSteps.Create(preset));

    public static void Request(TutorialStep step)
    {
        if (instance == null || step == null) return;

        instance.Enqueue(step);
    }

    public static bool HasSeen(string id) => RunState.Current.HasFlag(FlagPrefix + id);

    /// <summary>안내가 뜬 뒤로 각성을 했는지. 각성 안내가 닫히는 조건이다.</summary>
    public static bool HasAwakened => instance != null && instance.awakened;

    /// <summary>이번 판에 새로 얻은 유물 수 (시작 유물은 빼고).</summary>
    public static int RelicsAcquired => instance != null ? instance.relicsAcquired : 0;

    /// <summary>플레이어가 제단 범위 안에 있는지. 0.25초마다 갱신한다.</summary>
    public static bool IsNearShrine => instance != null && instance.nearShrine;

    /// <summary>지금 ↑로 상자를 열 수 있는지: 상자 범위 안이고 선택창이 떠 있지 않다.</summary>
    public static bool CanOpenChest => instance != null && instance.nearChest;

    // ── 상태 ────────────────────────────────

    private readonly List<TutorialStep> queue = new List<TutorialStep>();
    private TutorialStep current;
    private float shownAt;
    private float nextAllowedAt;
    private float nextNearbyPollAt;
    private bool awakened;
    private int playerScenesLoaded;
    private int knownRelicCount = -1;
    private int relicsAcquired;
    private bool nearShrine;
    private bool nearChest;

    private TutorialTooltip tooltip;
    private PlayerInput playerInput;
    private Collider2D[] playerColliders;
    private PlayerRelicManager relicManager;
    private PlayerEnergy energy;
    private PlayerAwakening awakening;
    private PlayerSkill playerSkill;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Detach();
    }

    private void Start()
    {
        StartCoroutine(AttachAfterFrame());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀌면 떠 있던 툴팁 오브젝트가 사라질 수 있다. 다시 찾는다.
        tooltip = null;
        StartCoroutine(AttachAfterFrame());
    }

    /// <summary>씬의 Start()들이 끝난 뒤에 붙는다. 시작 유물이 들어오는 걸 새 유물로 착각하지 않게.</summary>
    private IEnumerator AttachAfterFrame()
    {
        yield return null;

        Detach();

        playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput == null) yield break;   // 타이틀처럼 플레이어가 없는 씬

        Player player = FindAnyObjectByType<Player>();
        playerColliders = player != null ? player.GetComponentsInChildren<Collider2D>() : null;

        relicManager = FindAnyObjectByType<PlayerRelicManager>();
        energy = FindAnyObjectByType<PlayerEnergy>();
        awakening = FindAnyObjectByType<PlayerAwakening>();
        playerSkill = FindAnyObjectByType<PlayerSkill>();

        if (relicManager != null)
        {
            relicManager.OnRelicsChanged += OnRelicsChanged;

            // Player는 씬을 넘어 살아남으므로 처음 한 번만 기준을 잡는다.
            if (knownRelicCount < 0) knownRelicCount = relicManager.OwnedRelics.Count;
        }

        if (energy != null) energy.OnEnergyChanged += OnEnergyChanged;
        if (awakening != null) awakening.OnAwakeningStarted += OnAwakeningStarted;
        if (playerSkill != null) playerSkill.OnSkillsChanged += OnSkillsChanged;

        playerScenesLoaded++;

        if (playerScenesLoaded == 1)
        {
            nextAllowedAt = Time.time + StartDelay;
            Request(TutorialPreset.Move);
            Request(TutorialPreset.Jump);
            Request(TutorialPreset.Map);
            OnSkillsChanged();
        }
    }

    private void Detach()
    {
        if (relicManager != null) relicManager.OnRelicsChanged -= OnRelicsChanged;
        if (energy != null) energy.OnEnergyChanged -= OnEnergyChanged;
        if (awakening != null) awakening.OnAwakeningStarted -= OnAwakeningStarted;
        if (playerSkill != null) playerSkill.OnSkillsChanged -= OnSkillsChanged;

        relicManager = null;
        energy = null;
        awakening = null;
        playerSkill = null;
    }

    // ── 게임 알림 ───────────────────────────

    private void OnRelicsChanged()
    {
        if (relicManager == null) return;

        int count = relicManager.OwnedRelics.Count;

        if (count > knownRelicCount)
        {
            // 유물창 안내는 획득 알림창(AcquirePopup)의 [유물창 보기]가 대신한다.
            relicsAcquired += count - knownRelicCount;
        }

        knownRelicCount = count;
    }

    private void OnEnergyChanged()
    {
        if (energy != null && energy.IsFull) Request(TutorialPreset.Awaken);
    }

    private void OnAwakeningStarted()
    {
        awakened = true;
    }

    private void OnSkillsChanged()
    {
        if (playerSkill == null) return;

        for (int i = PlayerSkill.FirstNormalSlot; i < PlayerSkill.SlotCount; i++)
        {
            if (playerSkill.GetEquippedSkill(i) != null)
            {
                Request(TutorialPreset.Skill);
                return;
            }
        }
    }

    // ── 순서대로 보여주기 ────────────────────

    private void Enqueue(TutorialStep step)
    {
        if (IsDisabledInEditor) return;
        if (string.IsNullOrEmpty(step.Id) || HasSeen(step.Id)) return;
        if (current != null && current.Id == step.Id) return;

        foreach (TutorialStep queued in queue)
        {
            if (queued.Id == step.Id) return;
        }

        queue.Add(step);
    }

    private void Update()
    {
        PollNearby();

        if (tooltip == null) tooltip = FindTooltip();
        if (tooltip == null) return;

        bool blocked = IsBlocked();

        if (current == null)
        {
            if (blocked || Time.time < nextAllowedAt) return;

            current = TakeNextReady();
            if (current == null) return;

            if (current.Id == "awaken") awakened = false;

            shownAt = Time.time;
            ShowCurrent();
            return;
        }

        float elapsed = Time.time - shownAt;

        if (elapsed >= MinShowTime && IsDone(current))
        {
            Finish();
            return;
        }

        if (current.Timeout > 0f && elapsed >= current.Timeout)
        {
            Finish();
            return;
        }

        // 제단·상자에서 벗어나면 내리고 순서를 넘긴다. 돌아오면 다시 뜬다.
        if (!IsVisible(current))
        {
            tooltip.Hide();
            queue.Insert(0, current);
            current = null;
            return;
        }

        if (blocked && tooltip.IsShowing) tooltip.Hide();
        else if (!blocked && !tooltip.IsShowing) ShowCurrent();
    }

    /// <summary>
    /// 줄에서 지금 띄울 수 있는 첫 안내를 꺼낸다.
    /// 지금 자리에서 의미 없는 안내(제단·상자 밖)는 줄에 남겨두고 다음 것을 본다.
    /// </summary>
    private TutorialStep TakeNextReady()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            TutorialStep step = queue[i];

            // 줄 서 있는 사이에 이미 봤거나, 이미 해냈으면 띄우지 않는다.
            if (HasSeen(step.Id))
            {
                queue.RemoveAt(i--);
                continue;
            }

            if (step.Done != null && step.Done())
            {
                queue.RemoveAt(i--);
                RunState.Current.SetFlag(FlagPrefix + step.Id);
                continue;
            }

            if (!IsVisible(step)) continue;

            queue.RemoveAt(i);
            return step;
        }

        return null;
    }

    private static bool IsVisible(TutorialStep step) => step.Visible == null || step.Visible();

    private void ShowCurrent()
    {
        string key = string.IsNullOrEmpty(current.Key)
            ? KeyLabel(current.KeyActions ?? current.CompleteActions)
            : current.Key;

        tooltip.Show(key, current.Message, 0f);
    }

    private void Finish()
    {
        RunState.Current.SetFlag(FlagPrefix + current.Id);
        current = null;
        nextAllowedAt = Time.time + GapBetweenSteps;

        if (tooltip != null) tooltip.Hide();
    }

    private bool IsDone(TutorialStep step)
    {
        if (step.Done != null && step.Done()) return true;
        if (step.CompleteActions == null || playerInput == null || playerInput.actions == null) return false;

        foreach (string name in step.CompleteActions)
        {
            InputAction action = playerInput.actions.FindAction(name, false);
            if (action != null && action.WasPerformedThisFrame()) return true;
        }

        return false;
    }

    /// <summary>창·지도가 떠 있거나 게임이 멈춰 있으면 안내를 숨긴다.</summary>
    private static bool IsBlocked()
    {
        if (Time.timeScale <= 0f) return true;

        UIManager ui = UIManager.Instance;
        return ui != null && (ui.IsBlockingWindowOpen || ui.IsMapOpen);
    }

    /// <summary>제단·유물 상자 근처에 처음 갔을 때. 둘 다 ↑로 쓰는 물건이다.</summary>
    private void PollNearby()
    {
        if (playerInput == null || Time.time < nextNearbyPollAt) return;

        nextNearbyPollAt = Time.time + NearbyPollInterval;

        bool needShrine = !HasSeen("shrine");
        bool needChest = !HasSeen("chest");

        nearShrine = needShrine && ShrineAccess.NearbyShrine != null;

        // 선택창이 떠 있는 동안은 카드를 가리지 않게 상자 안내를 내린다.
        nearChest = needChest && IsTouchingChest() && FindAnyObjectByType<RelicSelectionPanelUI>() == null;

        if (nearShrine) Request(TutorialPreset.Shrine);
        if (nearChest) Request(TutorialPreset.Chest);
    }

    /// <summary>
    /// 플레이어가 유물 상자의 상호작용 영역에 닿아 있는지.
    /// RelicChest(관용님 파일)에는 범위 안인지 알려주는 값이 없어서,
    /// 상자가 SetInteractable을 부르는 것과 같은 기준(트리거 접촉)으로 직접 본다.
    /// </summary>
    private bool IsTouchingChest()
    {
        if (playerColliders == null || playerColliders.Length == 0) return false;

        foreach (RelicChest chest in FindObjectsByType<RelicChest>())
        {
            foreach (Collider2D area in chest.GetComponents<Collider2D>())
            {
                if (!area.enabled) continue;

                foreach (Collider2D body in playerColliders)
                {
                    if (body != null && body.enabled && area.IsTouching(body)) return true;
                }
            }
        }

        return false;
    }

    private static TutorialTooltip FindTooltip()
    {
        if (UIManager.Instance != null && UIManager.Instance.Tooltip != null) return UIManager.Instance.Tooltip;

        // 꺼둔 오브젝트라 inactive까지 찾아야 한다.
        return FindAnyObjectByType<TutorialTooltip>(FindObjectsInactive.Include);
    }

    // ── 키 이름 ─────────────────────────────

    /// <summary>액션들의 키보드 바인딩을 "← →", "A S D F"처럼 짧게 만든다.</summary>
    private string KeyLabel(string[] actionNames)
    {
        if (actionNames == null || playerInput == null || playerInput.actions == null) return "";

        List<string> keys = new List<string>();

        foreach (string name in actionNames)
        {
            InputAction action = playerInput.actions.FindAction(name, false);
            if (action == null) continue;

            foreach (InputBinding binding in action.bindings)
            {
                if (binding.isComposite) continue;

                string path = binding.effectivePath;
                if (string.IsNullOrEmpty(path) || !path.StartsWith("<Keyboard>/")) continue;

                string label = ShortKeyName(path.Substring("<Keyboard>/".Length));
                if (!keys.Contains(label)) keys.Add(label);
            }
        }

        return string.Join(" ", keys);
    }

    private static string ShortKeyName(string control)
    {
        switch (control)
        {
            case "leftArrow": return "←";
            case "rightArrow": return "→";
            case "upArrow": return "↑";
            case "downArrow": return "↓";
            case "leftShift":
            case "rightShift": return "Shift";
            case "leftCtrl":
            case "rightCtrl": return "Ctrl";
            case "space": return "Space";
            case "tab": return "Tab";
            case "escape": return "Esc";
            case "enter": return "Enter";
        }

        if (control.Length == 1) return control.ToUpperInvariant();

        return char.ToUpperInvariant(control[0]) + control.Substring(1);
    }

    // ── 에디터 테스트용 끄기 ─────────────────

#if UNITY_EDITOR
    private const string DisableKey = "Ludens.UI.DisableTutorial";
    private const string DisableMenu = "Tools/UI/튜토리얼 안내 끄기 (테스트용)";

    private static bool IsDisabledInEditor => EditorPrefs.GetBool(DisableKey, false);

    [MenuItem(DisableMenu)]
    private static void ToggleDisabled()
    {
        bool next = !IsDisabledInEditor;
        EditorPrefs.SetBool(DisableKey, next);
        Debug.Log(next ? "[TutorialDirector] 튜토리얼 안내: 끔" : "[TutorialDirector] 튜토리얼 안내: 켬");
    }

    [MenuItem(DisableMenu, true)]
    private static bool ToggleDisabledValidate()
    {
        Menu.SetChecked(DisableMenu, IsDisabledInEditor);
        return true;
    }
#else
    private static bool IsDisabledInEditor => false;
#endif
}
