using System.Collections.Generic;
using UnityEngine;

public enum RelicCategory
{
    Sword,  // 검 유물
    Orb,    // 보주 유물
    Body    // 신체 유물
}

[CreateAssetMenu(fileName = "RLC_NewRelic", menuName = "Relics/Relic Data")]

//유물 데이터 관리 SO
public class RelicData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] public string relicId;
    [SerializeField] public string relicName;
    [SerializeField] public Sprite icon;

    [TextArea(3, 6)]
    [SerializeField] public string description;

    [Header("Cost")]
    [SerializeField] public int cost = 1;

    [Header("Info")]
    [SerializeField] public RelicCategory category;

    [Header("Effects")]
    [SerializeField] private List<RelicEffect> effects = new();

    public string RelicId => relicId;
    public string RelicName => relicName;
    public Sprite Icon => icon;
    public string Description => description;
    public int Cost => cost;
    public RelicCategory Category => category;
    public IReadOnlyList<RelicEffect> Effects => effects;
}