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

    private void OnEnable()
    {
        health = GetComponent<PlayerHealth>();
        if (health != null) health.OnDied += ClearAnchors;
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

    private void ClearAnchors()
    {
        foreach (var anchor in anchors.Values)
            if (anchor.shadow != null) Destroy(anchor.shadow);
        anchors.Clear();
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= ClearAnchors;
        ClearAnchors();
    }
}
