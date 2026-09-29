using System;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "RFX_DarkShadow", menuName = "Relics/Effects/Dark Shadow")]

//암영 유물 효과
public class DarkShadowRelicEffect : RelicEffect
{
    [Header("Execute")]
    [Tooltip("적 현재 체력이 해당 비율 이하일 때 처형")]
    [SerializeField]
    [Range(0.01f, 1f)]
    private float executeHealthRatio = 0.15f;

    [Header("Execute Effect")]
    [SerializeField]
    private GameObject executeEffectPrefab;

    [SerializeField]
    private Vector3 executeEffectOffset = new Vector3(0f, 0.3f, 0f);


    //유물 장착 시 런타임 생성
    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new DarkShadowRelicRuntime(
            p,
            executeHealthRatio,
            executeEffectPrefab,
            executeEffectOffset
        );
    }


    //암영 유물 런타임 클래스
    private sealed class DarkShadowRelicRuntime : IRelicRuntime
    {
        private readonly Player player;

        private readonly float executeHealthRatio;
        private readonly GameObject executeEffectPrefab;
        private readonly Vector3 executeEffectOffset;

        private PlayerAttack playerAttack;

        private bool isEquipped;


        public DarkShadowRelicRuntime(
            Player player,
            float executeHealthRatio,
            GameObject executeEffectPrefab,
            Vector3 executeEffectOffset)
        {
            this.player = player;

            this.executeHealthRatio = executeHealthRatio;
            this.executeEffectPrefab = executeEffectPrefab;
            this.executeEffectOffset = executeEffectOffset;
        }


        //유물 장착
        public void Equip()
        {
            if (player == null) return;

            playerAttack = player.GetComponent<PlayerAttack>();

            if (playerAttack == null)
            {
                Debug.LogWarning("[DarkShadow] PlayerAttack을 찾지 못했습니다.");
                return;
            }

            isEquipped = true;

            playerAttack.OnAttackHit += HandleAttackHit;

            Debug.Log("[DarkShadow] 암영 유물 장착 완료");
        }


        //유물 장착 해제
        public void Unequip()
        {
            isEquipped = false;

            if (playerAttack != null)
            {
                playerAttack.OnAttackHit -= HandleAttackHit;
            }

            playerAttack = null;

            Debug.Log("[DarkShadow] 암영 유물 장착 해제");
        }


        //기본공격 적중 시 처형 가능 여부 확인
        private void HandleAttackHit(IDamageable target, DamageInfo damageInfo)
        {
            if (!isEquipped) return;
            if (target == null) return;

            Component targetComponent = target as Component;

            if (targetComponent == null) return;

            EnemyHealth enemyHealth =
                targetComponent.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
            {
                Debug.Log("[DarkShadow] EnemyHealth를 찾지 못했습니다.");
                return;
            }

            float healthRatio =
                enemyHealth.CurrentHp / enemyHealth.MaxHp;

            Debug.Log(
                $"[DarkShadow] HP: {enemyHealth.CurrentHp} / {enemyHealth.MaxHp}" +
                $" | Ratio: {healthRatio:0.00}" +
                $" | Execute: {executeHealthRatio:0.00}"
            );

            if (!enemyHealth.CanExecute(executeHealthRatio)) return;

            SpawnExecuteEffect(enemyHealth.transform.position);

            enemyHealth.Execute(damageInfo);

            Debug.Log("[DarkShadow] 암영 처형 발동");
        }


        //처형 이펙트 생성
        private void SpawnExecuteEffect(Vector3 targetPosition)
        {
            if (executeEffectPrefab == null) return;

            Object.Instantiate(
                executeEffectPrefab,
                targetPosition + executeEffectOffset,
                Quaternion.identity
            );
        }
    }
}