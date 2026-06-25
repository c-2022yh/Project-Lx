using UnityEngine;
[System.Flags]
public enum SkillSlotType
{
    None = 0,
    SlotX = 1 << 0,
    SlotA = 1 << 1,
    SlotS = 1 << 2,
    SlotD = 1 << 3,
    SlotF = 1 << 4,
}