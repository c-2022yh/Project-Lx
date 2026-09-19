using UnityEngine;

/// <summary>
/// 진행도에 접근하는 단일 창구. 어디서든 RunState.Current로 읽고 쓴다.
///
/// 세이브 시스템이 생기면 게임 시작 시 한 줄만 바꾸면 된다:
///     RunState.Current = new SaveFileRunState(슬롯번호);
/// </summary>
public static class RunState
{
    private static IRunState current;

    public static IRunState Current
    {
        get => current ??= new InMemoryRunState();
        set => current = value;
    }

    /// <summary>새 게임을 시작할 때. 진행도를 비운다.</summary>
    public static void StartNewRun()
    {
        Current.Clear();
        Debug.Log("[RunState] 새 진행도 시작");
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터는 Play를 멈춰도 static이 살아남아 진행도가 다음 Play로 새어나간다.
    /// 그걸 막는다. 빌드에는 포함되지 않는다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        current = null;
    }
#endif
}
