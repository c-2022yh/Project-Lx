using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 화면 사이 이동을 한 곳에 모아둔다.
/// 씬 이름이 바뀌면 여기만 고치면 된다.
/// </summary>
public static class GameFlow
{
    /// <summary>타이틀 씬 이름. TitleUIBuilder가 이 이름으로 씬을 만든다.</summary>
    public const string TitleSceneName = "Title";

    /// <summary>"게임하기"로 진입할 씬. 실제 시작 씬이 정해지면 여기를 바꾼다.</summary>
    public static string GameplaySceneName = "Test_Alpha";

    public static void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(TitleSceneName);
    }

    public static void StartGameplay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameplaySceneName);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
