using System.Collections.Generic;
using UnityEngine;

/// <summary>Installed anchors belong to a player, never to a shared skill asset.</summary>
[DisallowMultipleComponent]
public sealed class ShadowSwapRuntime : MonoBehaviour
{
    private sealed class Anchor
    {
        public GameObject shadow;
        public float readyAt;
    }

    private readonly Dictionary<ShadowSwapSkillData, Anchor> anchors = new();
    private PlayerHealth health;
    private PlayerSkill skills;
    private readonly List<ShadowSwapSkillData> removedSkills = new();

    private void OnEnable()
    {
        health = GetComponent<PlayerHealth>();
        if (health != null) health.OnDied += ClearAnchors;
        skills = GetComponent<PlayerSkill>();
        if (skills != null) skills.OnSkillsChanged += ClearUnequippedAnchors;
    }

    public GameObject GetShadow(ShadowSwapSkillData skill)
    {
        return anchors.TryGetValue(skill, out var anchor) ? anchor.shadow : null;
    }

    public float GetRecastRemaining(ShadowSwapSkillData skill)
    {
        return anchors.TryGetValue(skill, out var anchor) && anchor.shadow != null
            ? Mathf.Max(0f, anchor.readyAt - Time.time) : 0f;
    }

    public void Install(ShadowSwapSkillData skill, GameObject shadow, float delay)
    {
        anchors[skill] = new Anchor { shadow = shadow, readyAt = Time.time + Mathf.Max(0f, delay) };
    }

    public GameObject TakeShadow(ShadowSwapSkillData skill)
    {
        GameObject shadow = GetShadow(skill);
        anchors.Remove(skill);
        return shadow;
    }

    private void ClearUnequippedAnchors()
    {
        if (skills == null) return;

        removedSkills.Clear();
        foreach (var entry in anchors)
        {
            if (!skills.IsSkillEquipped(entry.Key)) removedSkills.Add(entry.Key);
        }

        foreach (var skill in removedSkills)
        {
            GameObject shadow = TakeShadow(skill);
            if (shadow != null) Destroy(shadow);
        }
        removedSkills.Clear();
    }

    private void ClearAnchors()
    {
        foreach (var anchor in anchors.Values)
            if (anchor.shadow != null) Destroy(anchor.shadow);
        anchors.Clear();
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= ClearAnchors;
        if (skills != null) skills.OnSkillsChanged -= ClearUnequippedAnchors;
        ClearAnchors();
    }
}
