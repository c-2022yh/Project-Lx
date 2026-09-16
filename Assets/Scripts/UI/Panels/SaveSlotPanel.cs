using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 저장 슬롯 선택 화면.
/// 슬롯 정보는 SaveSlotService.Provider에서 가져오므로,
/// 세이브 시스템이 붙어도 이 스크립트는 고칠 게 없다.
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotEntryPrefab;

    private readonly List<SaveSlotEntry> spawnedEntries = new();
    private TitleMenuPanel returnTo;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>타이틀에서 열 때 호출. 뒤로가기 목적지를 같이 받는다.</summary>
    public void Open(TitleMenuPanel from)
    {
        returnTo = from;
        SetVisible(true);
        Refresh();
    }

    /// <summary>슬롯 목록을 다시 그린다.</summary>
    public void Refresh()
    {
        if (slotContainer == null || slotEntryPrefab == null)
        {
            Debug.LogWarning("[SaveSlotPanel] slotContainer 또는 slotEntryPrefab이 연결되지 않았습니다.");
            return;
        }

        // 다시 열 때 슬롯이 중복 생성되지 않도록 먼저 비운다.
        foreach (SaveSlotEntry entry in spawnedEntries)
            if (entry != null) Destroy(entry.gameObject);

        spawnedEntries.Clear();

        ISaveSlotProvider provider = SaveSlotService.Provider;

        for (int i = 0; i < provider.SlotCount; i++)
        {
            GameObject go = Instantiate(slotEntryPrefab, slotContainer);
            go.SetActive(true);

            SaveSlotEntry entry = go.GetComponent<SaveSlotEntry>();
            if (entry == null)
            {
                Debug.LogWarning("[SaveSlotPanel] slotEntryPrefab에 SaveSlotEntry가 없습니다.");
                continue;
            }

            entry.Bind(provider.GetSlot(i), OnSlotClicked);
            spawnedEntries.Add(entry);
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        ISaveSlotProvider provider = SaveSlotService.Provider;
        SaveSlotInfo info = provider.GetSlot(slotIndex);

        if (info != null && info.isEmpty)
            provider.StartNewGame(slotIndex);
        else
            provider.LoadSlot(slotIndex);
    }

    /// <summary>"+ 새 게임 시작" — 비어 있는 첫 슬롯을 쓴다.</summary>
    public void OnNewGameButton()
    {
        ISaveSlotProvider provider = SaveSlotService.Provider;

        for (int i = 0; i < provider.SlotCount; i++)
        {
            SaveSlotInfo info = provider.GetSlot(i);
            if (info == null || !info.isEmpty) continue;

            provider.StartNewGame(i);
            return;
        }

        // [SAVE_HOOK] 슬롯이 다 찼을 때의 덮어쓰기 확인 팝업은 세이브 시스템과 같이 붙이면 된다.
        Debug.Log("[SaveSlotPanel] 빈 슬롯이 없습니다. 덮어쓸 슬롯을 직접 선택해주세요.");
    }

    /// <summary>"뒤로가기"</summary>
    public void OnBackButton()
    {
        SetVisible(false);
        returnTo?.SetVisible(true);
    }
}
