using System.Collections.Generic;
using UnityEngine;

//유물 선택창 전체 관리
public class RelicSelectionPanelUI : MonoBehaviour
{
    private const int ChoiceCount = 3;

    [Header("Relic Pools")]
    [SerializeField] private RelicData[] swordRelics;
    [SerializeField] private RelicData[] orbRelics;
    [SerializeField] private RelicData[] bodyRelics;


    [Header("Cards")]
    [SerializeField] private RelicCardUI[] relicCards;

    [Header("References")]
    [SerializeField] private RelicManager relicManager;


    private void Awake()
    {
        //프리팹에서 씬의 RelicManager를 연결하지 못한 경우 자동 탐색
        if (relicManager == null)
        {
            relicManager = FindFirstObjectByType<RelicManager>();
        }
    }


    //지정된 카테고리의 유물 선택창 열기
    public void OpenSelection(RelicCategory category)
    {
        gameObject.SetActive(true);

        List<RelicData> selectedPool;

        switch (category)
        {
            case RelicCategory.Sword:
                selectedPool = CreateValidPool(swordRelics);
                break;

            case RelicCategory.Orb:
                selectedPool = CreateValidPool(orbRelics);
                break;

            case RelicCategory.Body:
                selectedPool = CreateValidPool(bodyRelics);
                break;

            default:
                Debug.LogWarning(
                    $"[RelicSelectionPanelUI] 지원하지 않는 카테고리: {category}",
                    this
                );

                CloseSelection();
                return;
        }

        if (selectedPool.Count < ChoiceCount)
        {
            Debug.LogWarning(
                $"[RelicSelectionPanelUI] {category} 유물이 " +
                $"{ChoiceCount}개보다 적습니다.",
                this
            );

            CloseSelection();
            return;
        }

        ShowRandomRelics(selectedPool);
    }


    //배열에서 null과 중복을 제거한 후보 목록 생성
    private List<RelicData> CreateValidPool(
        RelicData[] relicArray
    )
    {
        List<RelicData> pool = new();

        if (relicArray == null)
        {
            return pool;
        }

        foreach (RelicData relic in relicArray)
        {
            if (relic == null)
            {
                continue;
            }

            if (!pool.Contains(relic))
            {
                pool.Add(relic);
            }
        }

        return pool;
    }


    //선택된 카테고리에서 중복 없이 3개 뽑기
    private void ShowRandomRelics(List<RelicData> sourcePool)
    {
        if (relicCards == null || relicCards.Length < ChoiceCount)
        {

            CloseSelection();
            return;
        }

        //원본 목록을 건드리지 않도록 복사
        List<RelicData> remainingRelics = new(sourcePool);

        for (int i = 0; i < ChoiceCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, remainingRelics.Count);

            RelicData selectedRelic = remainingRelics[randomIndex];

            remainingRelics.RemoveAt(randomIndex);

            relicCards[i].Setup(selectedRelic, HandleRelicSelected);
            
        }
    }


    //카드 선택 처리
    private void HandleRelicSelected(RelicData selectedRelic)
    {
        if (selectedRelic == null)
        {
            return;
        }

        if (relicManager == null)
        {
            return;
        }

        relicManager.EquipRelic(selectedRelic);

        CloseSelection();
    }


    //유물 선택창 닫기
    public void CloseSelection()
    {
        gameObject.SetActive(false);
    }
}