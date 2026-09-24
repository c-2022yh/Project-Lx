using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RFX_Thunder", menuName = "Relics/Effects/Thunder")]

//뇌전 유물 효과
public class ThunderRelicEffect : RelicEffect
{
    [System.Serializable]
    private class ThunderDamageSpec
    {
        [Header("Damage Type")]
        public DamageType damageType = DamageType.Magical;

        [Header("Base Damage")]
        [Min(0f)]
        public float damageMultiplier = 0.8f;

        [Header("Bonus Penetration")]
        public float bonusPenetration = 0f;

        [Header("Bonus Damage Amplification")]
        public float bonusDamageAmplification = 0f;
    }

    [Header("Effect")]
    [SerializeField]
    private GameObject thunderHitEffectPrefab;

    [SerializeField]
    private ChainLightningEffect chainLightningPrefab;

    [Header("Lightning Damage")]
    [SerializeField]
    private ThunderDamageSpec damageSpec = new();

    [Header("Chain")]
    [SerializeField]
    [Min(1)]
    private int maxTargets = 4;

    [SerializeField]
    [Min(0f)]
    private float chainRadius = 5f;

    [SerializeField]
    [Range(0f, 1f)]
    private float damageRemainPerChain = 0.8f;

    [SerializeField]
    [Min(0f)]
    private float chainDelay = 0.05f;

    [Header("Target")]
    [SerializeField]
    private LayerMask targetLayer;

    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new ThunderRelicRuntime(
            p,
            damageSpec.damageType,
            damageSpec.damageMultiplier,
            damageSpec.bonusPenetration,
            damageSpec.bonusDamageAmplification,
            maxTargets,
            chainRadius,
            damageRemainPerChain,
            chainDelay,
            targetLayer,
            chainLightningPrefab,
            thunderHitEffectPrefab
        );
    }

    private sealed class ThunderRelicRuntime : IRelicRuntime
    {
        private readonly Player player;

        private readonly ChainLightningEffect chainLightningPrefab;
        private readonly GameObject thunderHitEffectPrefab;

        private readonly DamageType damageType;
        private readonly float damageMultiplier;
        private readonly float bonusPenetration;
        private readonly float bonusDamageAmplification;

        private readonly int maxTargets;
        private readonly float chainRadius;
        private readonly float damageRemainPerChain;
        private readonly float chainDelay;
        private readonly LayerMask targetLayer;

        private PlayerAttack playerAttack;
        private PlayerStats playerStats;

        private bool isEquipped;

        public ThunderRelicRuntime(
             Player player,
             DamageType damageType,
             float damageMultiplier,
             float bonusPenetration,
             float bonusDamageAmplification,
             int maxTargets,
             float chainRadius,
             float damageRemainPerChain,
             float chainDelay,
             LayerMask targetLayer,
             ChainLightningEffect chainLightningPrefab,
             GameObject thunderHitEffectPrefab)
        {
            this.player = player;

            this.damageType = damageType;
            this.damageMultiplier = damageMultiplier;
            this.bonusPenetration = bonusPenetration;
            this.bonusDamageAmplification = bonusDamageAmplification;

            this.maxTargets = maxTargets;
            this.chainRadius = chainRadius;
            this.damageRemainPerChain = damageRemainPerChain;
            this.chainDelay = chainDelay;
            this.targetLayer = targetLayer;

            this.chainLightningPrefab = chainLightningPrefab;
            this.thunderHitEffectPrefab = thunderHitEffectPrefab;
        }

        public void Equip()
        {
            if (player == null) return;

            playerAttack = player.GetComponent<PlayerAttack>();

            playerStats = player.GetComponent<PlayerStats>();

            if (playerAttack == null || playerStats == null)
            {
                Debug.LogWarning("[Thunder] 필요한 컴포넌트를 찾지 못했습니다.");
                return;
            }

            isEquipped = true;

            playerAttack.OnAttackHit += HandleAttackHit;

            Debug.Log("[Thunder] 뇌전 유물 장착 완료");
        }

        public void Unequip()
        {
            isEquipped = false;

            if (playerAttack != null)
            {
                playerAttack.OnAttackHit -= HandleAttackHit;
            }

            playerAttack = null;
            playerStats = null;
        }

        //원래 공격 적중
        private void HandleAttackHit(IDamageable target, DamageInfo originalDamageInfo)
        {
            if (!isEquipped) return;

            if (target == null) return;

            // 치명타가 아니면 뇌전 발동 X
            if (!originalDamageInfo.isCritical) return;
            player.StartCoroutine(ChainLightning(target));
        }

        //뇌전 연쇄 공격
        private IEnumerator ChainLightning(IDamageable firstTarget)
        {
            HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

            IDamageable currentTarget = firstTarget;

            //첫 대상은 100%
            float currentDamageMultiplier = 1f;

            int hitCount = 0;

            while (currentTarget != null && hitCount < maxTargets)
            {
                if (!isEquipped) yield break;

                //번개 피해
                DamageInfo lightningDamage = CreateLightningDamageInfo(currentDamageMultiplier);

                Vector2 hitDirection = GetHitDirection(currentTarget);

                currentTarget.TakeDamage(lightningDamage, hitDirection);

                // 적중 이펙트 생성
                UnityEngine.Component targetComponent = currentTarget as UnityEngine.Component;

                if (targetComponent != null && thunderHitEffectPrefab != null)
                {
                    UnityEngine.Object.Instantiate(
                        thunderHitEffectPrefab,
                        targetComponent.transform.position,
                        Quaternion.identity
                    );
                }

                hitTargets.Add(currentTarget);

                hitCount++;

                if (hitCount >= maxTargets) yield break;

                // 다음 전이 전에 피해 감소
                currentDamageMultiplier *= damageRemainPerChain;

                if (chainDelay > 0f)
                {
                    yield return new WaitForSeconds(chainDelay);
                }

                IDamageable nextTarget = FindNextTarget(currentTarget, hitTargets);

                if (nextTarget == null) yield break;

                SpawnChainEffect(currentTarget, nextTarget);

                currentTarget = nextTarget;
            }
        }

        //뇌전 전용 체인 이펙트 생성
        private void SpawnChainEffect(IDamageable from, IDamageable to)
        {
            if (chainLightningPrefab == null) return;

            UnityEngine.Component fromComponent = from as UnityEngine.Component;

            UnityEngine.Component toComponent = to as UnityEngine.Component;

            if (fromComponent == null || toComponent == null) return;

            ChainLightningEffect effect = UnityEngine.Object.Instantiate(chainLightningPrefab);

            effect.Init(fromComponent.transform, toComponent.transform);
        }

        //뇌전 전용 DamageInfo 생성
        private DamageInfo CreateLightningDamageInfo(float chainMultiplier)
        {
            OffensiveStats offense = playerStats.Offense;

            float damage = 0f;
            float penetration = 0f;

            switch (damageType)
            {
                case DamageType.Physical:

                    damage =
                        offense.physicalAttack *
                        damageMultiplier *
                        chainMultiplier;

                    penetration =
                        offense.physicalPenetration +
                        bonusPenetration;

                    break;


                case DamageType.Magical:

                    damage =
                        offense.magicalAttack *
                        damageMultiplier *
                        chainMultiplier;

                    penetration =
                        offense.magicalPenetration +
                        bonusPenetration;

                    break;
            }

            float amplification =
                offense.damageAmplification +
                bonusDamageAmplification;

            return new DamageInfo(
                damage,
                damageType,
                penetration,

                //뇌전은 절대 치명타 아님
                false,

                amplification, player.gameObject
            );
        }

        //적 찾기
        private IDamageable FindNextTarget(IDamageable currentTarget, HashSet<IDamageable> hitTargets)
        {
            Component currentComponent = currentTarget as Component;

            if (currentComponent == null) return null;

            Vector2 currentPosition = currentComponent.transform.position;

            Collider2D[] colliders =
                Physics2D.OverlapCircleAll(
                    currentPosition,
                    chainRadius,
                    targetLayer
                );

            IDamageable closestTarget = null;

            float closestDistance = float.MaxValue;

            foreach (Collider2D col in colliders)
            {
                IDamageable candidate = col.GetComponentInParent<IDamageable>();

                if (candidate == null) continue;

                if (hitTargets.Contains(candidate)) continue;

                Component candidateComponent = candidate as Component;

                if (candidateComponent == null) continue;

                //플레이어 자신 제외
                if (candidateComponent.transform.root == player.transform.root) continue;

                float distance = Vector2.Distance(currentPosition, candidateComponent.transform.position);

                if (distance >= closestDistance) continue;

                closestDistance = distance;
                closestTarget = candidate;
            }

            return closestTarget;
        }

        private Vector2 GetHitDirection(IDamageable target)
        {
            Component targetComponent = target as Component;

            if (targetComponent == null)
            {
                return player.isFacingRight ? Vector2.right : Vector2.left;
            }

            Vector2 direction = targetComponent.transform.position - player.transform.position;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return player.isFacingRight ? Vector2.right : Vector2.left;
            }

            return direction.normalized;
        }
    }
}