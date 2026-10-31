using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유물 → 장착칸(동그란 칸)용 아이콘.
///
/// 보관함 아이콘은 네모 프레임을 달고 있어서 동그란 장착칸에 넣으면 프레임이 잘려 보였다.
/// 그래서 원 안에 그림만 있는 "장착용 아이콘"(Sprites/Relic_Equip_Icons, 이름 끝 _Equip)을 따로 쓴다.
///
/// 유물 데이터(RelicData)는 건드리지 않고, 이 표가 유물 ID와 그림을 짝지어 둔다.
/// 표는 Resources/RelicEquipIcons에 있고, 그림이 추가되면
/// Tools/UI/유물 장착 아이콘 다시 연결 을 누르면 이름으로 알아서 다시 채운다.
///   예) Relic_Body_Clover.png ↔ Relic_Body_Clover_Equip.png
/// </summary>
public class RelicEquipIcons : ScriptableObject
{
    public const string ResourceName = "RelicEquipIcons";

    [System.Serializable]
    public class Entry
    {
        public string relicId;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private static RelicEquipIcons loaded;
    private static bool tried;
    private Dictionary<string, Sprite> lookup;

    /// <summary>장착용 아이콘. 없으면 null (그러면 기존 아이콘을 원으로 잘라 쓴다).</summary>
    public static Sprite Get(RelicData relic)
    {
        if (relic == null || string.IsNullOrEmpty(relic.RelicId)) return null;

        if (!tried)
        {
            tried = true;
            loaded = Resources.Load<RelicEquipIcons>(ResourceName);
        }

        if (loaded == null) return null;

        if (loaded.lookup == null)
        {
            loaded.lookup = new Dictionary<string, Sprite>();
            foreach (Entry e in loaded.entries)
            {
                if (e != null && !string.IsNullOrEmpty(e.relicId) && e.sprite != null)
                    loaded.lookup[e.relicId] = e.sprite;
            }
        }

        return loaded.lookup.TryGetValue(relic.RelicId, out Sprite sprite) ? sprite : null;
    }

#if UNITY_EDITOR
    public void SetEntries(List<Entry> list)
    {
        entries = list;
        lookup = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = null;
        tried = false;
    }
#endif
}
