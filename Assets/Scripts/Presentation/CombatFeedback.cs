using UnityEngine;

/// <summary>
/// 타격감 연출(히트스탑 + 카메라 흔들림)을 한 곳에서 정한다. 수치는 아래 상수만 고치면 된다.
///
/// - 플레이어 공격이 적에게 맞음: AttackEffectHitbox가 OnPlayerHit을 부른다.
/// - 플레이어가 맞음: PlayerHealth.OnDamaged를 스스로 구독한다.
/// - 보스(BossMarker)는 더 세게.
/// </summary>
public class CombatFeedback : MonoBehaviour
{
    // ── 적을 때릴 때 ─────────────────────────
    private const float HitStopNormal = 0.045f;
    private const float HitStopCritical = 0.07f;
    private const float HitStopKill = 0.09f;
    private const float ShakeNormal = 0.10f;
    private const float ShakeCritical = 0.2f;
    private const float ShakeKill = 0.28f;
    private const float KickNormal = 0.06f;

    // ── 보스를 때릴 때 (위 값 대신) ───────────
    private const float HitStopBoss = 0.07f;
    private const float HitStopBossCritical = 0.11f;
    private const float ShakeBoss = 0.2f;
    private const float ShakeBossCritical = 0.32f;
    private const float KickBoss = 0.1f;

    // ── 플레이어가 맞을 때 ───────────────────
    private const float HitStopHurt = 0.06f;
    private const float ShakeHurt = 0.35f;

    private static CombatFeedback instance;

    private PlayerHealth watchedHealth;
    private float nextSearchTime;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[CombatFeedback]");
        instance = go.AddComponent<CombatFeedback>();
        DontDestroyOnLoad(go);
    }

    /// <summary>플레이어 공격이 대상에게 들어간 직후 부른다.</summary>
    public static void OnPlayerHit(IDamageable target, float attackDirection, bool isCritical)
        => OnPlayerHitObject(target as Component, attackDirection, isCritical);

    /// <summary>IDamageable로 받지 않는 곳(스킬 등)에서 쓴다.</summary>
    public static void OnPlayerHitObject(Component c, float attackDirection, bool isCritical)
    {
        if (c == null) return;

        EnemyHealth enemy = c.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;   // 적이 아닌 대상(상자 등)은 연출 없음

        bool isBoss = c.GetComponentInParent<BossMarker>() != null;
        bool killed = enemy.IsDead;
        Vector2 dir = new Vector2(attackDirection >= 0f ? 1f : -1f, 0f);

        if (isBoss)
        {
            // 보스 처치 연출(슬로모션 등)은 BossRoom이 따로 한다.
            HitStop.Stop(isCritical || killed ? HitStopBossCritical : HitStopBoss);
            CameraShake.Shake(isCritical || killed ? ShakeBossCritical : ShakeBoss);
            CameraShake.Kick(dir, KickBoss);
            return;
        }

        if (killed)
        {
            HitStop.Stop(HitStopKill);
            CameraShake.Shake(ShakeKill);
        }
        else if (isCritical)
        {
            HitStop.Stop(HitStopCritical);
            CameraShake.Shake(ShakeCritical);
        }
        else
        {
            HitStop.Stop(HitStopNormal);
            CameraShake.Shake(ShakeNormal);
        }

        CameraShake.Kick(dir, KickNormal);
    }

    private void Update()
    {
        // 플레이어는 DontDestroyOnLoad라 한 번 찾으면 계속 쓴다. 없을 때만 가끔 다시 찾는다.
        if (watchedHealth != null || Time.unscaledTime < nextSearchTime) return;
        nextSearchTime = Time.unscaledTime + 0.5f;

        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health == null) return;

        watchedHealth = health;
        watchedHealth.OnDamaged += HandlePlayerDamaged;
    }

    private void OnDestroy()
    {
        if (watchedHealth != null) watchedHealth.OnDamaged -= HandlePlayerDamaged;
        if (instance == this) instance = null;
    }

    private void HandlePlayerDamaged(float amount)
    {
        if (amount <= 0f) return;
        HitStop.Stop(HitStopHurt);
        CameraShake.Shake(ShakeHurt);
    }
}
