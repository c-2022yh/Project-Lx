using UnityEngine;

//유물 효과가 런타임에서 어떻게 동작할지 정의하는 SO
public abstract class RelicEffect : ScriptableObject
{
    public abstract IRelicRuntime CreateRuntime(Player player);
}