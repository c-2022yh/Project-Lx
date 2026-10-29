using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Skills/ShadowSwap", menuName = "Skills/ShadowSwap")]

//그림자 교체 스킬
public class ShadowSwapSkillData : UtilitySkillData
{
    [Header("Shadow Settings")]
    public GameObject shadowPrefab;

    [Min(0f), Tooltip("Minimum time between placing the shadow and teleporting. Full cooldown starts after teleport.")]
    public float recastDelay = 0.3f;

    private ShadowSwapRuntime GetRuntime(Player p) => p != null ? p.GetComponent<ShadowSwapRuntime>() : null;

    public override float GetEnergyCost(Player p)
    {
        var runtime = GetRuntime(p);
        return runtime != null && runtime.GetShadow(this) != null ? 0f : base.GetEnergyCost(p);
    }

    public override bool CanUse(Player p)
    {
        if (p == null || !base.CanUse(p)) return false;
        var runtime = GetRuntime(p);
        if (runtime != null && runtime.GetShadow(this) != null)
            return runtime.GetRecastRemaining(this) <= 0f;
        return shadowPrefab != null;
    }

    public override float GetCooldownAfterCast(Player p)
    {
        var runtime = GetRuntime(p);
        return runtime != null && runtime.GetShadow(this) != null
            ? runtime.GetRecastRemaining(this) : cooldownTime;
    }

    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;
        var runtime = GetRuntime(p);
        if (runtime == null) runtime = p.gameObject.AddComponent<ShadowSwapRuntime>();
        GameObject currentShadow = runtime.GetShadow(this);
        if (currentShadow == null && shadowPrefab == null) yield break;
        if (currentShadow != null && runtime.GetRecastRemaining(this) > 0f) yield break;
        PlayReleaseSound(p, p.transform.position);
        

        if (currentShadow == null)
        {
            currentShadow = Instantiate(shadowPrefab, p.transform.position, Quaternion.identity);
            currentShadow.GetComponent<ShadowAfterimageVisual>()?.Capture(p.sr);
            runtime.Install(this, currentShadow, recastDelay);
            Debug.Log("그림자 설치");
            yield break;
        }

        Vector3 shadowPos = currentShadow.transform.position;

        p.transform.position = shadowPos;

        var visual = currentShadow.GetComponent<ShadowAfterimageVisual>();
        if (visual != null) visual.Consume();
        else Destroy(currentShadow);
        runtime.TakeShadow(this);

        if (p.rb != null)
        {
            p.rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("그림자 순간이동 완료");

        yield break;
    }
}