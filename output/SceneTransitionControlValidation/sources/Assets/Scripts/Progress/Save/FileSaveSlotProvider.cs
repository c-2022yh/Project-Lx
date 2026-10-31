using System;
using System.Globalization;
using UnityEngine;

/// <summary>저장 슬롯 화면에 실제 세이브 파일 내용을 보여준다.</summary>
public class FileSaveSlotProvider : ISaveSlotProvider
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SaveSlotService.Provider = new FileSaveSlotProvider();
    }

    public int SlotCount => SaveSystem.SlotCount;

    public SaveSlotInfo GetSlot(int slotIndex)
    {
        SaveData data = SaveSystem.Load(slotIndex);
        if (data == null) return new SaveSlotInfo { slotIndex = slotIndex, isEmpty = true };

        DateTime.TryParse(data.savedAt, null, DateTimeStyles.RoundtripKind, out DateTime savedAt);
        return new SaveSlotInfo
        {
            slotIndex = slotIndex,
            isEmpty = false,
            playTimeSeconds = data.playTimeSeconds,
            locationName = data.locationName,
            savedAt = savedAt,
        };
    }

    public void StartNewGame(int slotIndex) => GameSession.StartNew(slotIndex);

    public void LoadSlot(int slotIndex) => GameSession.Continue(slotIndex);

    public void DeleteSlot(int slotIndex) => SaveSystem.Delete(slotIndex);
}
