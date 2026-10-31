using System;
using System.Collections.Generic;

/// <summary>
/// 슬롯 하나에 저장되는 내용. JsonUtility로 그대로 직렬화한다.
/// JsonUtility가 Dictionary/HashSet을 못 쓰므로 전부 List로 둔다.
/// </summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public float playTimeSeconds;
    public string savedAt = "";          // DateTime.ToString("o")

    // 이어하기 위치. 마지막으로 쓴 제단, 없으면 새 게임 시작 씬.
    public string sceneName = "";
    public string locationName = "";
    public bool hasShrine;
    public float shrineX;
    public float shrineY;

    // 진행도 (RunState)
    public List<string> ownedRelics = new();
    public List<string> equippedRelics = new();
    public List<string> visitedRooms = new();
    public List<string> flags = new();
    public List<ExploredScene> explored = new();

    // 플레이어
    public bool hasOrb;
    public List<string> equippedSkills = new();   // ASDF 슬롯 순서, 빈 칸은 ""

    [Serializable]
    public class ExploredScene
    {
        public string scene;
        public List<long> cells = new();
    }
}
