using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "RFX_Dawn", menuName = "Relics/Effects/Dawn")]

//새벽 유물 효과
public class DawnRelicEffect : RelicEffect
{
    [Header("Extra Attack Timing")]
    [SerializeField]
    private float secondAfterimageDelay = 0.1f;

    [Header("Extra Attack Position")]
    [SerializeField]
    private Vector2 secondOffset = new Vector2(0.3f, 0f);

    [Header("Extra Attack Color")]
    [SerializeField]
    private Color secondColor =
        new Color(0.6f, 0.85f, 1f, 0.7f);

    [Header("Lifetime")]
    [SerializeField]
    private float destroyAfter = 0.2f;

    [Header("Extra Attack Damage")]
    [SerializeField]
    private AttackDamageSpec extraAttackDamageSpec;

    //유물 장착 시 런타임 생성
    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new DawnRelicRuntime(
            p,
            extraAttackDamageSpec,
            secondAfterimageDelay,
            secondOffset,
            secondColor,
            destroyAfter);

    }

    //유물 런타임 클래스
    private sealed class DawnRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly AttackDamageSpec extraAttackDamageSpec;

        private readonly float extraAttackDelay;
        private readonly Vector2 extraAttackOffset;
        private readonly Color extraAttackColor;
        private readonly float destroyAfter;

        private PlayerAttack playerAttack;
        private PlayerStats playerStats;

        private bool isEquipped;

        //생성자
        public DawnRelicRuntime(
        Player player,
        AttackDamageSpec extraAttackDamageSpec,
        float extraAttackDelay,
        Vector2 extraAttackOffset,
        Color extraAttackColor,
        float destroyAfter)
        {
            this.player = player;
            this.extraAttackDamageSpec = extraAttackDamageSpec;
            this.extraAttackDelay = extraAttackDelay;
            this.extraAttackOffset = extraAttackOffset;
            this.extraAttackColor = extraAttackColor;
            this.destroyAfter = destroyAfter;
        }

        //유물 장착
        public void Equip()
        {
            if (player == null) return;

            playerAttack = player.GetComponent<PlayerAttack>();
            playerStats = player.GetComponent<PlayerStats>();

            if (playerAttack == null || playerStats == null) return;

            isEquipped = true;
            playerAttack.OnGroundAttackEffectCreated += HandleGroundAttackCreated;

            Debug.Log("[Dawn] 새벽 유물 장착 완료");
        }

        //유물 해제
        public void Unequip()
        {
            isEquipped = false;

            if (playerAttack != null)
            {
                playerAttack.OnGroundAttackEffectCreated -= HandleGroundAttackCreated;
            }

            playerAttack = null;
            playerStats = null;
        }

        //유물 장착 시 지상 공격 이펙트 생성 시 추가 공격 생성
        private void HandleGroundAttackCreated(
            AttackPattern pattern,
            Vector3 originalSpawnPosition,
            Quaternion originalRotation,
            float direction)
        {
            if (!isEquipped) return;

            if (pattern == null) return;

            if (pattern.attackEffectPrefab == null) return;

            player.StartCoroutine(
                SpawnExtraAttack(
                    pattern,
                    originalSpawnPosition,
                    originalRotation,
                    direction
                )
            );
        }

        private IEnumerator SpawnExtraAttack(
            AttackPattern pattern,
            Vector3 originalSpawnPosition,
            Quaternion originalRotation,
            float direction)
        {
            //추가 공격의 시간차
            if (extraAttackDelay > 0f)
            {
                yield return new WaitForSeconds(
                    extraAttackDelay
                );
            }

            if (!isEquipped) yield break;
            if (playerStats == null) yield break;

            if (pattern == null || pattern.attackEffectPrefab == null)
            {
                yield break;
            }

            //왼쪽 오른쪽 구분
            float dir = direction >= 0f ? 1f : -1f;
            Vector3 directionalOffset = new Vector3(
                extraAttackOffset.x * dir,
                extraAttackOffset.y,
                0f
            );

            Vector3 spawnPosition = originalSpawnPosition + directionalOffset;

            // 원래 평타와 같은 이펙트를 한 번 더 생성
            GameObject extraAttack =
                Object.Instantiate(
                    pattern.attackEffectPrefab,
                    spawnPosition,
                    originalRotation
                );

            extraAttack.transform.localScale =
                pattern.effectScale;

            ApplyVisual(
                extraAttack,
                extraAttackColor,
                dir
            );

            AttackEffectHitbox hitbox =
                extraAttack.GetComponentInChildren<
                    AttackEffectHitbox>(true);

            if (hitbox == null)
            {
                Debug.LogWarning(
                    "[Dawn] 추가 공격 이펙트에서 " +
                    "AttackEffectHitbox를 찾지 못했습니다.",
                    extraAttack
                );

                Object.Destroy(extraAttack);
                yield break;
            }

            //추가타의 DamageInfo를 생성
            DamageInfo damageInfo = DamageInfo.Create(
                 playerStats.Offense,
                 extraAttackDamageSpec,
                 player.gameObject);

            //추가 공격에 피해 정보 전달
            hitbox.SetAttackInfo(damageInfo, dir);

            //activeTime이 지나면 공격 판정 종료
            player.StartCoroutine(
                DisableCollidersAfter(
                    extraAttack,
                    pattern.activeTime
                )
            );

            float lifeTime = destroyAfter;

            if (lifeTime <= 0f)
                lifeTime = pattern.effectDuration;

            // 공격 판정보다 먼저 삭제되지 않게 보호
            lifeTime = Mathf.Max(
                lifeTime,
                pattern.activeTime
            );

            Object.Destroy(
                extraAttack,
                lifeTime
            );
        }

        private static void ApplyVisual(
            GameObject extraAttack,
            Color color,
            float direction)
        {
            SpriteRenderer[] renderers =
                extraAttack.GetComponentsInChildren<
                    SpriteRenderer>(true);

            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.flipX = direction < 0f;
                renderer.color = color;
            }
        }

        private static IEnumerator DisableCollidersAfter(
            GameObject extraAttack,
            float activeTime)
        {
            if (activeTime > 0f)
            {
                yield return new WaitForSeconds(
                    activeTime
                );
            }

            if (extraAttack == null)
                yield break;

            Collider2D[] colliders =
                extraAttack.GetComponentsInChildren<
                    Collider2D>(true);

            foreach (Collider2D collider in colliders)
            {
                collider.enabled = false;
            }
        }
    }
}