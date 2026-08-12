using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//현재 장착한 유물 아이콘을 HUD에 표시
public class RelicHUDPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RelicManager relicManager;
    [SerializeField] private Transform iconContainer;

    [Header("Prefab")]
    [SerializeField] private Image relicIconPrefab;

    //이 스크립트가 직접 생성한 아이콘만 보관
    private readonly List<Image> spawnedIcons = new();

    //마지막으로 확인한 유물 개수
    private int lastRelicCount = -1;


    private void Awake()
    {
        if (iconContainer == null)
        {
            iconContainer = transform;
        }
    }


    private void Start()
    {
        //첫 프레임에 반드시 갱신
        lastRelicCount = -1;
    }


    private void Update()
    {
        if (relicManager == null)
            return;

        int currentRelicCount =
            relicManager.EquippedRelics.Count;

        //유물 개수가 변하지 않았으면 갱신하지 않음
        if (currentRelicCount == lastRelicCount)
            return;

        lastRelicCount = currentRelicCount;

        RefreshIcons();
    }


    private void RefreshIcons()
    {
        if (relicManager == null ||
            iconContainer == null ||
            relicIconPrefab == null)
        {
            return;
        }

        Debug.Log(
            $"[RelicHUD] 갱신 실행 / 현재 유물 수: " +
            $"{relicManager.EquippedRelics.Count}"
        );

        //이 스크립트가 생성했던 아이콘만 제거
        foreach (Image icon in spawnedIcons)
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
            }
        }

        spawnedIcons.Clear();

        //현재 장착된 유물 아이콘 생성
        foreach (RelicData relic in
                 relicManager.EquippedRelics)
        {
            if (relic == null || relic.Icon == null)
                continue;

            Image newIcon = Instantiate(
                relicIconPrefab,
                iconContainer
            );

            newIcon.name =
                $"RelicIcon_{relic.RelicName}";

            newIcon.sprite = relic.Icon;
            newIcon.preserveAspect = true;
            newIcon.raycastTarget = false;
            newIcon.gameObject.SetActive(true);

            spawnedIcons.Add(newIcon);
        }
    }


    private void OnDestroy()
    {
        Debug.LogWarning(
            "[RelicHUD] RelicHUDPanel 오브젝트가 파괴되었습니다.",
            this
        );
    }
}