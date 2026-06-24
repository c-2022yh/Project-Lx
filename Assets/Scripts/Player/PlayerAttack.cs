using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[System.Serializable]
public class AttackPattern //공격 패턴
{
    public string attackName;

    [Header("Timing")]
    public float startupTime = 0.05f;   //선딜
    public float activeTime = 0.1f;     //공격판정 시간
    public float recoveryTime = 0.1f;   //후딜

    [Header("Combat")]
    public float damageMultiplier = 1f; //데미지 보정값

    [Header("Effect")] //공격 이펙트 프리펩 설정
    public GameObject attackEffectPrefab;
    public Vector2 effectOffset = new Vector2(0.8f, 0f);
    public Vector3 effectScale = Vector3.one;
    public float effectRotationZ = 0f;
    public float effectDuration = 0.12f;

    [Header("Effect Follow")] //이펙트가 플레이어를 얼마나 따라올 지 (공중공격 한정)
    public bool followPlayer = false;
    public float followDuration = 0.1f;

}


//플레이어 기본 공격을 다루는 스크립트
public class PlayerAttack : MonoBehaviour
{
    //공격 패턴(2단계)
    [Header("Ground Combo")]
    [SerializeField] private AttackPattern[] groundPatterns;
    [SerializeField] private float comboResetTime = 0.6f;

    //공중 공격
    [Header("Air Attack")]
    [SerializeField] private AttackPattern airAttack;

    private Player player;

    private int comboIndex = 0;
    private float lastAttackEndTime;
    private Coroutine attackCoroutine;

    private Vector3 originLocalPos;

    void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {

    }

    public void ExecuteAttack(Player p)
    {
        if (p == null) return;
        
        //이미 공격 중?
        if (attackCoroutine != null) return;

        //공중일 때
        if (!p.isGrounded)
        {
            if (airAttack == null) return;

            attackCoroutine = StartCoroutine(AttackRoutine(p, airAttack, true));
            return;
        }

        if (groundPatterns == null || groundPatterns.Length == 0) return;

        //공격 후 일정 시간이 지나면 콤보 초기화
        if (Time.time > lastAttackEndTime + comboResetTime) comboIndex = 0;

        AttackPattern pattern = groundPatterns[comboIndex];
        comboIndex++;
        //콤보인덱스가 0-1에만 돌도록
        if (comboIndex >= groundPatterns.Length) comboIndex = 0;

        attackCoroutine = StartCoroutine(AttackRoutine(p, pattern, false));
    }


    private IEnumerator AttackRoutine(Player p, AttackPattern pattern, bool isAirAttack)
    {
        //상태 진입
        p.playerActionState.EnterAttack();

        //플레이어 바라보는 방향 설정
        float dir = p.isFacingRight ? 1f : -1f;

        if (!isAirAttack)
        {
            //지상공격은 시작 시 x축 이동 제거
            p.rb.linearVelocity = new Vector2(0f, p.rb.linearVelocity.y);
        }


        //공격 프레임
        ShowAttackEffect(p, pattern, dir); //공격 이펙트 생성
        yield return ActiveAttackPhase(p, pattern, dir);

        lastAttackEndTime = Time.time;

        //상태 돌아오기
        if (p.playerActionState.isAttacking)
            p.playerActionState.EnterNormal();

        attackCoroutine = null;
    }

    //실제 공격 중 실행할 코루틴
    private IEnumerator ActiveAttackPhase(Player p, AttackPattern pattern, float dir)
    {
        float timer = 0f;
        float moved = 0f;

        while (timer < pattern.activeTime)
        {
            timer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(timer / pattern.activeTime);

            yield return new WaitForFixedUpdate();
        }
    }

    //이펙트 보였다 사라지게끔 함수
    private void ShowAttackEffect(Player p, AttackPattern pattern, float dir)
    {
        if (pattern.attackEffectPrefab == null) return;

        //생성위치: 플레이어가 바라보는 정면 앞
        Vector3 spawnPos = p.transform.position + new Vector3(pattern.effectOffset.x * dir, pattern.effectOffset.y, 0f);
        
        //회전각 적용
        Quaternion rotation = Quaternion.Euler(0f, 0f, pattern.effectRotationZ * dir);
        
        //시작
        GameObject effectObj = Instantiate(pattern.attackEffectPrefab, spawnPos, rotation);

        //크기 및 방향 설정
        effectObj.transform.localScale = pattern.effectScale;
        SpriteRenderer sr = effectObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = dir < 0f;

        //공격 히트박스 찾기
        AttackEffectHitbox hitbox = effectObj.GetComponentInChildren<AttackEffectHitbox>();
        if (hitbox != null)
        {
            hitbox.SetAttackInfo(pattern.damageMultiplier, dir);
        }

        //공중 공격은 이펙트가 플레이어를 따라오게
        if (pattern.followPlayer)
        {
            StartCoroutine(FollowEffect(effectObj.transform, p.transform, 
                pattern.effectOffset, dir, pattern.followDuration));
        }

        //삭제
        Destroy(effectObj, pattern.effectDuration);



    }

    //따라오는 이펙트 코루틴
    private IEnumerator FollowEffect(Transform effectTransform,Transform playerTransform, 
        Vector2 offset, float dir, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (effectTransform == null || playerTransform == null)
                yield break;

            effectTransform.position = playerTransform.position + new Vector3(
                offset.x * dir,
                offset.y,
                0f
            );

            timer += Time.deltaTime;
            yield return null;
        }
    }



}