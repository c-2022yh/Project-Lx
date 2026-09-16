using UnityEngine;

/// <summary>
/// 저장 슬롯 화면과 세이브 시스템 사이의 경계.
///
/// 지금은 세이브 시스템이 없으므로 EmptySaveSlotProvider가 기본값이고,
/// 슬롯 3칸이 전부 "비어 있음"으로 그려진다. UI 확인에는 충분하다.
///
/// 세이브 시스템이 생기면:
///   1. ISaveSlotProvider를 구현한 클래스를 하나 만들고
///   2. 게임 시작 시 SaveSlotService.Provider = new MySaveProvider();
/// 그러면 UI는 손대지 않아도 실제 데이터를 표시한다.
/// </summary>
public interface ISaveSlotProvider
{
    int SlotCount { get; }

    SaveSlotInfo GetSlot(int slotIndex);

    /// <summary>해당 슬롯으로 새 게임 시작.</summary>
    void StartNewGame(int slotIndex);

    /// <summary>해당 슬롯의 저장 데이터로 게임 이어하기.</summary>
    void LoadSlot(int slotIndex);
}

public static class SaveSlotService
{
    private static ISaveSlotProvider provider;

    public static ISaveSlotProvider Provider
    {
        get => provider ??= new EmptySaveSlotProvider();
        set => provider = value;
    }
}

/// <summary>세이브 시스템이 붙기 전까지 쓰는 빈 구현.</summary>
public class EmptySaveSlotProvider : ISaveSlotProvider
{
    public int SlotCount => 3;

    public SaveSlotInfo GetSlot(int slotIndex)
    {
        return new SaveSlotInfo { slotIndex = slotIndex, isEmpty = true };
    }

    public void StartNewGame(int slotIndex)
    {
        // [SAVE_HOOK] 세이브 시스템이 생기면 여기서 새 저장 데이터를 만든다.
        Debug.Log($"[SaveSlot] 슬롯 {slotIndex + 1}번으로 새 게임 시작 (세이브 시스템 연결 전)");
        GameFlow.StartGameplay();
    }

    public void LoadSlot(int slotIndex)
    {
        // [SAVE_HOOK] 세이브 시스템이 생기면 여기서 저장 데이터를 불러온다.
        Debug.Log($"[SaveSlot] 슬롯 {slotIndex + 1}번 불러오기 (세이브 시스템 연결 전)");
        GameFlow.StartGameplay();
    }
}
