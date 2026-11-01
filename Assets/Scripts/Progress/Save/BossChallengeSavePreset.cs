using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A repeatable, non-persistent boss challenge in the final save slot.</summary>
[CreateAssetMenu(fileName = "BossChallengeSavePreset", menuName = "Save/Boss Challenge Preset")]
public sealed class BossChallengeSavePreset : ScriptableObject
{
    public const string ResourcePath = "BossChallengeSavePreset";

    [SerializeField] private bool enabledInBuild = true;
    [SerializeField] private string sceneName = "Boss";
    [SerializeField] private string locationName = "보스 도전 · 모든 유물 보유";
    [Tooltip("Place this at the shrine before the boss encounter trigger.")]
    [SerializeField] private Vector2 shrinePosition = new Vector2(-31f, 2.12f);
    [SerializeField] private bool includeAllRelics = true;
    [SerializeField] private List<string> completedStoryFlags = new List<string>
    {
        "story.intro", "story.introArrived", "story.firstOrbEquip"
    };

    public string SceneName => sceneName;
    public Vector2 ShrinePosition => shrinePosition;

    public static BossChallengeSavePreset ForSlot(int slot)
    {
#if UNITY_EDITOR
        return null;
#else
        if (slot != SaveSystem.SlotCount - 1) return null;
        BossChallengeSavePreset preset = Resources.Load<BossChallengeSavePreset>(ResourcePath);
        return preset != null && preset.enabledInBuild ? preset : null;
#endif
    }

    public SaveData CreateSaveData()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            throw new InvalidOperationException("Boss challenge scene is not configured.");

        var data = new SaveData
        {
            sceneName = sceneName,
            locationName = locationName,
            hasShrine = true,
            shrineX = shrinePosition.x,
            shrineY = shrinePosition.y,
            hasOrb = true,
            savedAt = ""
        };
        // Inventory only: loadout limits and the normal shrine equipment flow still apply.
        if (includeAllRelics)
        {
            RelicDatabase database = RelicDatabase.Instance;
            if (database == null) throw new InvalidOperationException("RelicDatabase is missing.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RelicData relic in database.All)
            {
                if (relic == null || string.IsNullOrWhiteSpace(relic.RelicId))
                    throw new InvalidOperationException("RelicDatabase contains an invalid relic.");
                if (!ids.Add(relic.RelicId))
                    throw new InvalidOperationException("Duplicate relic ID: " + relic.RelicId);
                data.ownedRelics.Add(relic.RelicId);
            }
            if (data.ownedRelics.Count == 0)
                throw new InvalidOperationException("RelicDatabase is empty.");
        }
        data.visitedRooms.Add(sceneName);
        foreach (string flag in completedStoryFlags)
            if (!string.IsNullOrWhiteSpace(flag) && !data.flags.Contains(flag)) data.flags.Add(flag);
        return data;
    }

}
