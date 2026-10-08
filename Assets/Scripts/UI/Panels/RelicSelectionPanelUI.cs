using System.Collections.Generic;
using UnityEngine;

public class RelicSelectionPanelUI : MonoBehaviour
{
    private const int ChoiceCount = 3;

    [Header("Cards")]
    [SerializeField] private RelicCardUI[] relicCards;

    [Header("References")]
    [SerializeField] private PlayerRelicManager relicManager;

    private readonly List<RelicData> displayedRelics = new();
    private bool isSelectionOpen;
    private bool isSelecting;

    //연결된 유물 관리자가 없으면 씬에서 찾아 연결
    private void Awake()
    {
        if (relicManager == null)
        {
            relicManager = FindFirstObjectByType<PlayerRelicManager>();
        }
    }

    //미보유 유물 중 최대 3개를 제시. 후보가 없거나 카드 연결이 부족하면 열지 않음
    public bool OpenSelection(RelicData[] relicPool)
    {
        if (isSelectionOpen || isSelecting)
        {
            Debug.LogWarning("[RelicSelectionPanelUI] 이미 선택창을 사용 중입니다.", this);
            return false;
        }

        if (relicManager == null)
        {
            relicManager = FindFirstObjectByType<PlayerRelicManager>();
        }

        if (relicManager == null)
        {
            Debug.LogWarning("[RelicSelectionPanelUI] 활성 플레이어의 PlayerRelicManager를 찾지 못했습니다. References를 연결해 주세요.", this);
            return false;
        }

        if (transform.parent != null && !transform.parent.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[RelicSelectionPanelUI] 상위 UI 오브젝트가 비활성화되어 있습니다. 부모 Canvas/Panel을 확인해 주세요.", this);
            return false;
        }

        List<RelicData> validPool = CreateValidPool(relicPool);
        int choiceCount = Mathf.Min(ChoiceCount, validPool.Count);

        if (choiceCount == 0)
        {
            Debug.LogWarning("[RelicSelectionPanelUI] 선택할 미보유 유물이 없습니다. 상자의 Relic Pool과 플레이어의 시작 보유/장착 목록을 확인해 주세요.", this);
            return false;
        }

        if (relicCards == null || relicCards.Length < choiceCount)
        {
            Debug.LogWarning($"[RelicSelectionPanelUI] Cards 배열에 카드 {choiceCount}개를 연결해야 합니다.", this);
            return false;
        }

        for (int i = 0; i < choiceCount; i++)
        {
            if (relicCards[i] == null)
            {
                Debug.LogWarning($"[RelicSelectionPanelUI] Cards 배열의 {i}번 카드가 비어 있습니다.", this);
                return false;
            }
        }

        gameObject.SetActive(true);

        if (!gameObject.activeInHierarchy || !enabled)
        {
            Debug.LogWarning("[RelicSelectionPanelUI] 선택창이 활성화되지 않았습니다. UI 활성화 제어와 컴포넌트를 확인해 주세요.", this);
            return false;
        }

        ShowRandomRelics(validPool);
        isSelectionOpen = true;
        return true;
    }

    //빈 데이터, 중복 데이터, 이미 보유한 유물을 선택 후보에서 제외
    private List<RelicData> CreateValidPool(RelicData[] relicArray)
    {
        List<RelicData> pool = new();
        if (relicArray == null) return pool;

        foreach (RelicData relic in relicArray)
        {
            if (relic == null) continue;
            if (relicManager.HasRelic(relic)) continue;
            if (!pool.Contains(relic)) pool.Add(relic);
        }

        return pool;
    }

    //중복 없이 최대 3개를 무작위 표시하고 남는 카드 숨김
    private void ShowRandomRelics(List<RelicData> sourcePool)
    {
        List<RelicData> remainingRelics = new(sourcePool);
        int choiceCount = Mathf.Min(ChoiceCount, remainingRelics.Count);
        displayedRelics.Clear();

        for (int i = 0; i < relicCards.Length; i++)
        {
            if (relicCards[i] == null) continue;
            relicCards[i].gameObject.SetActive(i < choiceCount);
            if (i >= choiceCount) continue;

            int randomIndex = UnityEngine.Random.Range(0, remainingRelics.Count);
            RelicData selectedRelic = remainingRelics[randomIndex];
            remainingRelics.RemoveAt(randomIndex);
            displayedRelics.Add(selectedRelic);

            relicCards[i].Setup(selectedRelic, HandleRelicSelected);
        }
    }

    //선택한 유물을 보유 목록에만 추가. 성공 시 창을 닫고 중복 클릭은 차단
    private void HandleRelicSelected(RelicData selectedRelic)
    {
        if (!isSelectionOpen || isSelecting) return;
        if (selectedRelic == null || relicManager == null) return;
        if (!displayedRelics.Contains(selectedRelic)) return;

        isSelecting = true;

        try
        {
            bool acquired = relicManager.AcquireRelic(selectedRelic);

            if (!acquired)
            {
                Debug.LogWarning("[RelicSelectionPanelUI] 유물을 획득하지 못했습니다.");
                return;
            }

            CloseSelection();
        }
        finally
        {
            isSelecting = false;
        }
    }

    //선택 상태를 종료하고 창 숨김
    public void CloseSelection()
    {
        isSelectionOpen = false;
        displayedRelics.Clear();
        gameObject.SetActive(false);
    }

    //외부에서 창이 비활성화된 경우에도 선택 상태 정리
    private void OnDisable()
    {
        isSelectionOpen = false;
        displayedRelics.Clear();
    }
}
