using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 들어가 본 씬을 RunState에 기록한다. 맵에서 안 가본 곳으로 가는 포탈에 "?"를 띄울 때 쓴다.
///
/// 지금은 메모리 기록이라 게임을 끄면 사라진다. 세이브가 생기면 RunState째로 저장된다.
/// 씬에도 프리팹에도 붙이지 않는다. 게임이 시작되면 스스로 sceneLoaded에 붙는다.
/// </summary>
public static class VisitedScenes
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 에디터에서 도메인 리로드를 꺼두면 static 구독이 남는다. 두 번 붙지 않게 먼저 뗀다.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 처음 열린 씬은 sceneLoaded가 이미 지나간 뒤라 직접 기록한다.
        RunState.Current.MarkVisited(SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RunState.Current.MarkVisited(scene.name);
    }

    public static bool HasVisited(string sceneName) => RunState.Current.HasVisited(sceneName);
}
