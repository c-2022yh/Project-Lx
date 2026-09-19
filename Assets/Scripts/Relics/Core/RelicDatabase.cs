using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// relicId -> RelicData 조회표.
/// 진행도는 유물을 문자열 id로만 들고 있으므로(나중에 파일로 저장하기 위해)
/// 화면에 그릴 때 실제 데이터로 되돌릴 통로가 필요하다.
///
/// 에셋은 Assets/Resources/RelicDatabase.asset 에 두고 런타임에 자동으로 읽는다.
/// 목록은 Tools/Relics > Rebuild Relic Database 로 자동 채운다.
/// </summary>
[CreateAssetMenu(fileName = "RelicDatabase", menuName = "Relics/Relic Database")]
public class RelicDatabase : ScriptableObject
{
    public const string ResourcesPath = "RelicDatabase";

    [Tooltip("Tools/Relics > Rebuild Relic Database 로 자동으로 채워진다.")]
    [SerializeField] private List<RelicData> relics = new();

    private Dictionary<string, RelicData> byId;

    public IReadOnlyList<RelicData> All => relics;

    // ── 인스턴스 접근 ───────────────────────
    private static RelicDatabase instance;

    public static RelicDatabase Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = Resources.Load<RelicDatabase>(ResourcesPath);

            if (instance == null)
            {
                Debug.LogError(
                    "[RelicDatabase] Assets/Resources/RelicDatabase.asset 을 찾지 못했습니다.\n" +
                    "Unity 메뉴에서 Tools/Relics > Rebuild Relic Database 를 실행해주세요.");
            }

            return instance;
        }
    }

    // ── 조회 ───────────────────────────────
    public RelicData Find(string relicId)
    {
        if (string.IsNullOrEmpty(relicId)) return null;

        BuildIndexIfNeeded();

        return byId.TryGetValue(relicId, out RelicData data) ? data : null;
    }

    public static RelicData Get(string relicId)
    {
        RelicDatabase db = Instance;
        return db != null ? db.Find(relicId) : null;
    }

    /// <summary>id 목록을 데이터 목록으로. 찾지 못한 id는 건너뛴다.</summary>
    public static List<RelicData> Resolve(IEnumerable<string> relicIds)
    {
        List<RelicData> result = new List<RelicData>();
        if (relicIds == null) return result;

        foreach (string id in relicIds)
        {
            RelicData data = Get(id);

            if (data == null)
            {
                Debug.LogWarning($"[RelicDatabase] 알 수 없는 유물 id: {id}");
                continue;
            }

            result.Add(data);
        }

        return result;
    }

    private void BuildIndexIfNeeded()
    {
        if (byId != null) return;

        byId = new Dictionary<string, RelicData>();

        foreach (RelicData relic in relics)
        {
            if (relic == null) continue;

            if (string.IsNullOrEmpty(relic.RelicId))
            {
                Debug.LogWarning($"[RelicDatabase] relicId가 비어 있습니다: {relic.name}", relic);
                continue;
            }

            if (byId.ContainsKey(relic.RelicId))
            {
                Debug.LogWarning($"[RelicDatabase] relicId가 중복됩니다: {relic.RelicId}", relic);
                continue;
            }

            byId[relic.RelicId] = relic;
        }
    }

#if UNITY_EDITOR
    /// <summary>에디터 도구가 목록을 채울 때 쓴다.</summary>
    public void EditorSetRelics(List<RelicData> newRelics)
    {
        relics = newRelics;
        byId = null;
    }
#endif
}
