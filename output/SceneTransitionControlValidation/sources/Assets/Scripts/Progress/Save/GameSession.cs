using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 지금 어느 슬롯으로 플레이 중인지, 플레이 시간, 저장/이어하기를 맡는다.
///
/// 저장 시점: 제단 활성화·휴식, 일시정지 → 나가기, 게임 종료.
/// 이어하면 마지막으로 쓴 제단에서 시작한다 (제단이 없으면 시작 씬 기본 위치).
///
/// 타이틀을 거치지 않고 게임 씬을 바로 재생하면 슬롯이 없으므로 저장하지 않는다.
/// </summary>
public class GameSession : MonoBehaviour
{
    private static GameSession instance;

    public static int CurrentSlot { get; private set; } = -1;
    public static bool HasSlot => CurrentSlot >= 0;

    // 죽으면 돌아갈 곳. 슬롯과 상관없이 마지막으로 쓴 제단(없으면 처음 들어온 씬의 시작 위치).
    private static string respawnScene;
    private static Vector2 respawnPos;

    public static bool HasRespawn => !string.IsNullOrEmpty(respawnScene);
    public static string RespawnScene => respawnScene;
    public static Vector2 RespawnPosition => respawnPos;

    private SaveData data;           // 현재 슬롯의 최신 저장 내용
    private bool pendingFirstSave;   // 새 게임: 첫 게임 씬이 뜨면 바로 저장
    private bool pendingRestore;     // 이어하기: 첫 게임 씬이 뜨면 위치/스킬 복구
    private bool pendingRespawn;     // 사망 리스폰: 제단 씬이 뜨면 제단 위치로

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        CurrentSlot = -1;
        respawnScene = null;
        respawnPos = Vector2.zero;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureInstance();

    private static GameSession EnsureInstance()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject("[GameSession]");
        instance = go.AddComponent<GameSession>();
        DontDestroyOnLoad(go);
        return instance;
    }

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
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // 일시정지(timeScale 0) 중에는 세지 않는다.
        if (data == null || IsTitle(SceneManager.GetActiveScene().name)) return;
        if (Time.timeScale <= 0f) return;
        data.playTimeSeconds += Time.unscaledDeltaTime;
    }

    private void OnApplicationQuit()
    {
        if (HasSlot && !IsTitle(SceneManager.GetActiveScene().name)) SaveProgress();
    }

    // ───────────── 외부에서 부르는 것들 ─────────────

    /// <summary>빈 슬롯으로 새 게임.</summary>
    public static void StartNew(int slot)
    {
        GameSession s = EnsureInstance();
        CurrentSlot = slot;
        RunState.StartNewRun();
        ClearRespawn();
        s.data = new SaveData { sceneName = GameFlow.GameplaySceneName };
        s.pendingFirstSave = true;
        s.pendingRestore = false;
        GameFlow.StartGameplay();
    }

    /// <summary>저장된 슬롯 이어하기.</summary>
    public static void Continue(int slot)
    {
        SaveData loaded = SaveSystem.Load(slot);
        if (loaded == null)
        {
            StartNew(slot);
            return;
        }

        GameSession s = EnsureInstance();
        CurrentSlot = slot;
        s.data = loaded;
        ApplyToRunState(loaded);
        s.pendingFirstSave = false;
        s.pendingRestore = true;

        string scene = string.IsNullOrEmpty(loaded.sceneName) ? GameFlow.GameplaySceneName : loaded.sceneName;
        if (loaded.hasShrine) SetRespawn(scene, new Vector2(loaded.shrineX, loaded.shrineY));
        else ClearRespawn();
        Time.timeScale = 1f;
        SceneFader.LoadScene(scene);
    }

    /// <summary>제단 활성화/휴식. 이어하기 위치를 이 제단으로 바꾸고 저장한다.</summary>
    public static void SaveAtShrine(Vector2 respawnPosition)
    {
        // 리스폰 위치는 슬롯이 없어도(에디터에서 바로 재생) 갱신한다.
        SetRespawn(SceneManager.GetActiveScene().name, respawnPosition);

        if (!HasSlot || instance == null || instance.data == null) return;
        SaveData d = instance.data;
        d.sceneName = SceneManager.GetActiveScene().name;
        d.hasShrine = true;
        d.shrineX = respawnPosition.x;
        d.shrineY = respawnPosition.y;
        instance.SaveProgress();
    }

    /// <summary>일시정지 → 나가기. 진행도를 저장하고 타이틀로.</summary>
    public static void SaveAndGoToTitle()
    {
        if (HasSlot && instance != null) instance.SaveProgress();
        GameFlow.GoToTitle();
    }

    /// <summary>보스 처치처럼 바로 남겨야 하는 일. 진행도만 저장하고 이어하기 위치(제단)는 그대로 둔다.</summary>
    public static void SaveProgressNow()
    {
        if (HasSlot && instance != null) instance.SaveProgress();
    }

    /// <summary>
    /// PlayerRespawn이 제단 씬을 불러오기 직전에 부른다.
    /// 씬이 뜨자마자(카메라가 잡기 전에) 플레이어를 제단 위치로 옮긴다.
    /// </summary>
    public static void BeginRespawnLoad()
    {
        EnsureInstance().pendingRespawn = true;
    }

    // ───────────── 내부 ─────────────

    private static void SetRespawn(string scene, Vector2 position)
    {
        respawnScene = scene;
        respawnPos = position;
    }

    private static void ClearRespawn()
    {
        respawnScene = null;
        respawnPos = Vector2.zero;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsTitle(scene.name))
        {
            // 플레이어는 DontDestroyOnLoad라 타이틀까지 따라온다. 다음 게임을 깨끗하게 시작하려고 지운다.
            Player leftover = FindFirstObjectByType<Player>();
            if (leftover != null) Destroy(leftover.gameObject);
            CurrentSlot = -1;
            data = null;
            pendingFirstSave = pendingRestore = pendingRespawn = false;
            ClearRespawn();
            return;
        }

        if (pendingRespawn)
        {
            pendingRespawn = false;
            if (scene.name == respawnScene) PlacePlayer(respawnPos);
        }

        if (pendingRestore)
        {
            pendingRestore = false;
            PlaceAtShrine();
            StartCoroutine(RestorePlayerAfterBridge(data));
        }
        else if (pendingFirstSave)
        {
            pendingFirstSave = false;
            StartCoroutine(FirstSaveAfterFrame());
            StoryDialogues.PlayGameStart();
        }

        // 아직 제단을 안 썼으면 처음 들어온 씬의 시작 위치로 돌아오게 한다.
        if (!HasRespawn)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null) SetRespawn(scene.name, player.transform.position);
        }

        RestoreShrineVisual(scene);
    }

    // 마지막으로 쓴 제단이 이 씬에 있으면 켜진 상태로 보이게 한다.
    private static void RestoreShrineVisual(Scene scene)
    {
        if (!HasRespawn || scene.name != respawnScene) return;
        foreach (Shrine shrine in FindObjectsByType<Shrine>(FindObjectsSortMode.None))
        {
            if (shrine.gameObject.scene != scene) continue;
            if (Vector2.Distance(shrine.RespawnPosition, respawnPos) > 0.5f) continue;
            shrine.RestoreActive();
            break;
        }
    }

    private IEnumerator FirstSaveAfterFrame()
    {
        // RelicRuntimeBridge가 시작 유물을 RunState에 적은 뒤에 저장한다.
        yield return null;
        yield return null;
        SaveProgress();
    }

    // SceneSpawnManager.Awake가 기본 위치에 놓은 뒤라 여기서 덮어쓰면 된다.
    private void PlaceAtShrine()
    {
        if (data == null || !data.hasShrine) return;
        PlacePlayer(new Vector2(data.shrineX, data.shrineY));
    }

    private static void PlacePlayer(Vector2 pos)
    {
        Player player = FindFirstObjectByType<Player>();
        if (player == null) return;

        player.transform.position = pos;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = pos;
            rb.linearVelocity = Vector2.zero;
        }
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.SetRespawnPoint(pos);
    }

    /// <summary>
    /// 스킬은 유물·보주가 지급하므로 따로 저장하지 않는다.
    /// RelicRuntimeBridge가 유물을 다시 끼운 뒤(한 프레임 뒤) 보주를 주고, 슬롯 배치만 맞춘다.
    /// </summary>
    private IEnumerator RestorePlayerAfterBridge(SaveData d)
    {
        yield return null;
        yield return null;

        Player player = FindFirstObjectByType<Player>();
        if (player == null || d == null) yield break;

        PlayerEnergy energy = player.GetComponent<PlayerEnergy>();
        if (d.hasOrb && energy != null && !energy.HasOrb) energy.AcquireOrb();

        yield return null;

        PlayerSkill skills = player.GetComponent<PlayerSkill>();
        if (skills != null && d.equippedSkills != null && d.equippedSkills.Count > 0)
            RestoreSkillSlots(skills, d.equippedSkills);
    }

    private static void RestoreSkillSlots(PlayerSkill skills, List<string> names)
    {
        for (int i = PlayerSkill.FirstNormalSlot; i < PlayerSkill.SlotCount; i++)
            skills.UnequipSkill(i);

        for (int i = 0; i < names.Count; i++)
        {
            int slot = PlayerSkill.FirstNormalSlot + i;
            if (slot >= PlayerSkill.SlotCount) break;
            if (string.IsNullOrEmpty(names[i])) continue;
            foreach (SkillData owned in skills.OwnedSkills)
            {
                if (owned == null || owned.name != names[i]) continue;
                skills.EquipSkill(slot, owned);
                break;
            }
        }

        // 저장 당시엔 없던 스킬(나중에 유물 효과가 바뀐 경우 등)은 빈 칸에 채운다.
        foreach (SkillData owned in skills.OwnedSkills)
        {
            if (owned == null || skills.IsSkillEquipped(owned)) continue;
            for (int slot = PlayerSkill.FirstNormalSlot; slot < PlayerSkill.SlotCount; slot++)
            {
                if (skills.GetEquippedSkill(slot) != null) continue;
                skills.EquipSkill(slot, owned);
                break;
            }
        }
    }

    /// <summary>지금 상태를 data에 담아 파일로 쓴다. 위치(제단)는 건드리지 않는다.</summary>
    private void SaveProgress()
    {
        if (!HasSlot || data == null) return;

        data.version = SaveData.CurrentVersion;
        data.savedAt = System.DateTime.Now.ToString("o");
        data.locationName = LocationName(data.sceneName);
        CaptureRunState(data);
        CapturePlayer(data);

        SaveSystem.Save(CurrentSlot, data);
        Debug.Log($"[GameSession] 슬롯 {CurrentSlot + 1} 저장 ({data.sceneName}, {data.playTimeSeconds:0}s)");
    }

    private static void CaptureRunState(SaveData d)
    {
        IRunState rs = RunState.Current;
        d.ownedRelics = new List<string>(rs.OwnedRelicIds);
        d.equippedRelics = new List<string>(rs.EquippedRelicIds);
        d.visitedRooms = new List<string>(rs.VisitedRoomIds);
        d.flags = new List<string>(rs.Flags);

        d.explored.Clear();
        if (rs is InMemoryRunState mem)
        {
            foreach (KeyValuePair<string, HashSet<long>> pair in mem.ExploredCells)
                d.explored.Add(new SaveData.ExploredScene { scene = pair.Key, cells = new List<long>(pair.Value) });
        }
    }

    private static void CapturePlayer(SaveData d)
    {
        Player player = FindFirstObjectByType<Player>();
        if (player == null) return;   // 플레이어가 없으면 이전 값 유지

        PlayerEnergy energy = player.GetComponent<PlayerEnergy>();
        if (energy != null) d.hasOrb = energy.HasOrb;

        PlayerSkill skills = player.GetComponent<PlayerSkill>();
        if (skills != null)
        {
            d.equippedSkills = new List<string>();
            for (int i = PlayerSkill.FirstNormalSlot; i < PlayerSkill.SlotCount; i++)
            {
                SkillData s = skills.GetEquippedSkill(i);
                d.equippedSkills.Add(s != null ? s.name : "");
            }
        }
    }

    private static void ApplyToRunState(SaveData d)
    {
        RunState.StartNewRun();
        IRunState rs = RunState.Current;
        foreach (string id in d.ownedRelics) rs.AddOwnedRelic(id);
        rs.SetEquipped(d.equippedRelics);
        foreach (string room in d.visitedRooms) rs.MarkVisited(room);
        foreach (string flag in d.flags) rs.SetFlag(flag);
        foreach (SaveData.ExploredScene e in d.explored)
        {
            if (e == null || e.cells == null) continue;
            foreach (long cell in e.cells) rs.MarkExplored(e.scene, cell);
        }
    }

    private static bool IsTitle(string sceneName) => sceneName == GameFlow.TitleSceneName;

    // 씬 이름 → 슬롯에 보일 지역 이름. 정해지면 여기에 추가.
    private static readonly Dictionary<string, string> LocationNames = new()
    {
    };

    private static string LocationName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return "";
        return LocationNames.TryGetValue(sceneName, out string name) ? name : sceneName;
    }
}
