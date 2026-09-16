using System;
using UnityEngine;

/// <summary>
/// 저장 슬롯 화면이 한 칸을 그리는 데 필요한 정보.
/// 세이브 시스템이 생기면 이 구조체를 채워서 넘겨주면 된다.
/// </summary>
[Serializable]
public class SaveSlotInfo
{
    public int slotIndex;

    /// <summary>비어 있으면 UI가 "새 게임 시작" 상태로 그린다.</summary>
    public bool isEmpty = true;

    public string slotName = "";

    /// <summary>초 단위 누적 플레이 시간.</summary>
    public float playTimeSeconds;

    public int level;

    /// <summary>"숲 입구" 같은 사람이 읽는 위치 이름.</summary>
    public string locationName = "";

    /// <summary>마지막 저장 시각. 비어 있으면 표시하지 않는다.</summary>
    public DateTime savedAt;

    /// <summary>슬롯 썸네일. null이면 회색 자리표시자가 그려진다.</summary>
    public Sprite thumbnail;

    public string PlayTimeText
    {
        get
        {
            TimeSpan t = TimeSpan.FromSeconds(playTimeSeconds);
            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }
    }

    public string SavedAtText => savedAt == default ? "" : savedAt.ToString("yyyy-MM-dd HH:mm");
}
