using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// RelicManager(씬마다 새로 생기는 런타임 상태)와 RunState(씬을 넘어 유지되는 진행도)를 잇는다.
///
/// 이게 없으면 씬을 넘어갈 때마다 Player가 새로 생기면서 장착한 유물이 전부 풀린다.
///
/// 아무 프리팹에도 씬에도 붙이지 않는다. 게임이 시작되면 스스로 생겨나
/// DontDestroyOnLoad로 살아남고, 씬이 바뀔 때마다 그 씬의 RelicManager를 찾아 붙는다.
/// Player 프리팹을 포함해 남의 파일을 건드리지 않기 위해서다.
///
/// RelicManager 자체도 손대지 않았다. 공개 API(EquipRelic / UnequipRelic /
/// EquippedRelics / OnRelicsChanged)만으로 충분해서다.
/// </summary>
public class RelicRuntimeBridge : MonoBehaviour
{
    private static RelicRuntimeBridge instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject go = new GameObject("[RelicRuntimeBridge]");
        instance = go.AddComponent<RelicRuntimeBridge>();
        DontDestroyOnLoad(go);
    }

    private RelicManager manager;
    private bool isRestoring;
    private bool isQuitting;

    private void Awake()
    {
        // 혹시 누가 씬이나 프리팹에 직접 붙였더라도 하나만 남긴다.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        // 종료 중에도 같은 알림이 날아온다.
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        Detach();
    }

    private void Start()
    {
        StartCoroutine(AttachAfterFrame());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(AttachAfterFrame());
    }

    /// <summary>
    /// 씬의 다른 Start()들이 모두 끝난 뒤에 붙는다.
    /// RelicManager.Start()가 startingRelics를 끼우는 것과 순서가 엇갈리지 않게 하려는 것.
    /// </summary>
    private IEnumerator AttachAfterFrame()
    {
        yield return null;

        RelicManager found = FindAnyObjectByType<RelicManager>();

        if (found == null)
        {
            // 타이틀 씬처럼 플레이어가 없는 씬. 할 일 없음.
            Detach();
            yield break;
        }

        if (found != manager)
        {
            Detach();
            manager = found;
            manager.OnRelicsChanged += SyncToRunState;
        }

        Restore();
        SyncToRunState();
    }

    private void Detach()
    {
        if (manager == null) return;

        manager.OnRelicsChanged -= SyncToRunState;
        manager = null;
    }

    /// <summary>진행도에 기록된 장착 상태를 이 씬의 RelicManager에 다시 입힌다.</summary>
    private void Restore()
    {
        if (manager == null) return;

        IReadOnlyList<string> savedIds = RunState.Current.EquippedRelicIds;

        // 첫 씬이라 기록이 없으면, 지금 장착된 것(startingRelics 등)을 진행도의 시작점으로 삼는다.
        if (savedIds.Count == 0) return;

        isRestoring = true;

        List<RelicData> target = RelicDatabase.Resolve(savedIds);

        // 진행도에 없는 건 벗긴다 (startingRelics가 씬마다 다시 끼워지는 것 방지).
        List<RelicData> currentlyEquipped = new List<RelicData>(manager.EquippedRelics);

        foreach (RelicData equipped in currentlyEquipped)
        {
            if (equipped == null) continue;
            if (target.Contains(equipped)) continue;

            manager.UnequipRelic(equipped);
        }

        // 진행도에 있는데 안 끼워진 건 끼운다.
        foreach (RelicData relic in target)
        {
            if (relic == null) continue;
            if (ContainsRelic(manager.EquippedRelics, relic)) continue;

            manager.EquipRelic(relic);
        }

        isRestoring = false;
    }

    /// <summary>
    /// IReadOnlyList에는 Contains 인스턴스 메서드가 없고, LINQ를 끌어오면
    /// 매 호출마다 할당이 생긴다. 목록이 몇 개뿐이라 직접 훑는 게 낫다.
    /// </summary>
    private static bool ContainsRelic(IReadOnlyList<RelicData> list, RelicData relic)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == relic) return true;

        return false;
    }

    /// <summary>현재 장착 상태를 진행도에 기록한다.</summary>
    private void SyncToRunState()
    {
        if (isRestoring || manager == null) return;

        // 씬을 나갈 때 RelicManager.OnDestroy()가 UnequipAllRelics()를 부르면서
        // "장착 0개" 알림을 쏜다. 그걸 그대로 기록하면 진행도가 비워져,
        // 이 클래스가 막으려던 문제가 그대로 일어난다.
        if (isQuitting) return;
        if (!manager.gameObject.scene.isLoaded) return;

        List<string> ids = new List<string>();

        foreach (RelicData relic in manager.EquippedRelics)
        {
            if (relic == null) continue;

            if (string.IsNullOrEmpty(relic.RelicId))
            {
                Debug.LogWarning($"[RelicRuntimeBridge] relicId가 비어 있어 진행도에 기록할 수 없습니다: {relic.name}", relic);
                continue;
            }

            ids.Add(relic.RelicId);
        }

        // SetEquipped가 장착한 유물을 자동으로 '보유'에도 넣는다.
        // 이 게임은 유물을 먹으면 곧바로 장착되므로, 한 번이라도 장착했던 것이 곧 보유한 것이다.
        RunState.Current.SetEquipped(ids);
    }
}
