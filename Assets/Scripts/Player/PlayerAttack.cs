using System;
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
    public AttackDamageSpec damageSpec; //고유 데미지 값  

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
    private PlayerStats playerStats;

    private int comboIndex = 0;
    private float lastAttackEndTime;
    private Coroutine attackCoroutine;

    private Vector3 originLocalPos;

    //기본공격 이펙트가 실제 생성됐을 때 알림
    public event Action<AttackPattern, Vector3, Quaternion, float> AttackEffectCreated;
    
    //기본공격이 적에게 실제로 적중했을 때 알림
    public event Action OnAttackHit;

    //기본공격이 적에게 실제로 적중했을 때 알림
    public void NotifyAttackHit()
    {
        OnAttackHit?.Invoke();
    }



    /*
    AttackPattern = 지금 사용한 1타/2타 패턴
    Vector3       = 원래 이펙트 생성 위치
    Quaternion    = 원래 이펙트 회전값
    float         = 공격 방향
    */

    void Awake()
    {
        player = GetComponent<Player>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {

    }

    public void ExecuteAttack()
    {
        if (player == null) return;
        
        //이미 공격 중?
        if (attackCoroutine != null) return;

        //공중일 때
        if (!player.isGrounded)
        {
            if (airAttack == null) return;

            //공중공격은 애니메이션 처리용 어택인덱스가 -1
            attackCoroutine = StartCoroutine(AttackRoutine(airAttack, true, -1));
            return;
        }

        if (groundPatterns == null || groundPatterns.Length == 0) return;

        //공격 후 일정 시간이 지나면 콤보 초기화
        if (Time.time > lastAttackEndTime + comboResetTime) comboIndex = 0;

        int attackIndex = comboIndex;
        AttackPattern pattern = groundPatterns[comboIndex];
        comboIndex++;
        //콤보인덱스가 0-1에만 돌도록
        if (comboIndex >= groundPatterns.Length) comboIndex = 0;

        attackCoroutine = StartCoroutine(AttackRoutine(pattern, false, attackIndex));
    }


    private IEnumerator AttackRoutine(AttackPattern pattern, bool isAirAttack, int attackIndex)
    {
        //상태 진입
        player.ActionState.EnterAttack();

        //공격 애니메이션 재생
        player.Animation.PlayAttack(attackIndex);

        //플레이어 바라보는 방향 설정
        float dir = player.isFacingRight ? 1f : -1f;

        if (!isAirAttack)
        {
            //지상공격은 시작 시 x축 이동 제거
            player.rb.linearVelocity = new Vector2(0f, player.rb.linearVelocity.y);

        }

        //선딜
        yield return new WaitForSeconds(pattern.startupTime);

        //공격 데미지 정보 생성
        DamageInfo damageInfo = CreateDamageInfo(pattern);

        //공격 이펙트 생성 및 히트박스 활성화  
        SpawnAttackEffect(pattern, dir, damageInfo, isAirAttack);
        
        //공격 활성 시간
        yield return ActiveAttackPhase(pattern, dir);

        
        //후딜
        yield return new WaitForSeconds(pattern.recoveryTime);

        lastAttackEndTime = Time.time;

        //상태 돌아오기
        if (player.ActionState.isAttacking)
            player.ActionState.EnterNormal();

        attackCoroutine = null;
    }

    //데미지 정보 생성
    private DamageInfo CreateDamageInfo(AttackPattern pattern)
    {
        return DamageInfo.Create(playerStats.Offense, pattern.damageSpec, gameObject);
    }

    

    //이펙트 보였다 사라지게끔 함수
    private void SpawnAttackEffect(AttackPattern pattern, float dir, DamageInfo damageInfo, bool isAirAttack)
    {
        if (pattern.attackEffectPrefab == null) return;

        //생성위치: 플레이어가 바라보는 정면 앞
        Vector3 spawnPos = player.transform.position + 
            new Vector3(pattern.effectOffset.x * dir, pattern.effectOffset.y, 0f);
        
        //회전각 적용
        Quaternion rotation = Quaternion.Euler(0f, 0f, pattern.effectRotationZ * dir);
        
        //시작
        GameObject effectObj = Instantiate(pattern.attackEffectPrefab, spawnPos, rotation);

        //크기 및 방향 설정
        effectObj.transform.localScale = pattern.effectScale;
        SpriteRenderer sr = effectObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = dir < 0f;

        //기본공격 이펙트 생성을 알림
        AttackEffectCreated?.Invoke(pattern, spawnPos, rotation, dir);

        //공격 히트박스 찾기
        AttackEffectHitbox hitbox = effectObj.GetComponentInChildren<AttackEffectHitbox>();
        if (hitbox != null)
        {
            hitbox.SetAttackInfo(damageInfo, dir, NotifyAttackHit);

            //히트박스 활성화 후 일정 시간 뒤 비활성화
            StartCoroutine(DisableHitboxAfter(hitbox, pattern.activeTime));
        }

        
        //공중 공격은 이펙트가 플레이어를 따라오게
        if (pattern.followPlayer)
        {
            StartCoroutine(FollowEffect(effectObj.transform, player.transform, 
                pattern.effectOffset, dir, pattern.followDuration));
        }

        //삭제
        Destroy(effectObj, pattern.effectDuration);

    }

    //실제 공격 중 실행할 코루틴
    private IEnumerator ActiveAttackPhase(AttackPattern pattern, float dir)
    {
        float timer = 0f;
        
        while (timer < pattern.activeTime)
        {
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
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

    //공격 판정 히트박스 비활성화 코루틴
    private IEnumerator DisableHitboxAfter(AttackEffectHitbox hitbox,  float duration)
    {
        yield return new WaitForSeconds(duration);

        if (hitbox != null) hitbox.DisableHitbox();
    }

    //황혼 추가콤보 적용 전용 메소드
    //외부 효과가 지상 콤보 패턴을 추가할 때 사용
    public bool AddGroundAttackPattern(AttackPattern pattern)
    {
        if (pattern == null) return false;

        //배열이 없는 경우 새로 생성
        if (groundPatterns == null)
        {
            groundPatterns = new AttackPattern[] { pattern };
            comboIndex = 0;
            return true;
        }

        //같은 패턴이 이미 들어 있다면 중복 추가 방지
        foreach (AttackPattern existingPattern in groundPatterns)
        {
            if (ReferenceEquals(existingPattern, pattern)) return false;
        }

        AttackPattern[] newPatterns = new AttackPattern[groundPatterns.Length + 1];

        Array.Copy(groundPatterns, newPatterns, groundPatterns.Length);
        newPatterns[newPatterns.Length - 1] = pattern;
        groundPatterns = newPatterns;

        //장착 순간 콤보가 꼬이지 않도록 초기화
        comboIndex = 0;

        return true;
    }

    //외부 효과가 추가했던 지상 콤보 패턴 제거
    public bool RemoveGroundAttackPattern(AttackPattern pattern)
    {
        if (pattern == null) return false;

        if (groundPatterns == null || groundPatterns.Length == 0) return false;
        
        int removeIndex = -1;

        for (int i = 0; i < groundPatterns.Length; i++)
        {
            if (ReferenceEquals(groundPatterns[i], pattern))
            {
                removeIndex = i;
                break;
            }
        }

        //배열에서 찾지 못함
        if (removeIndex < 0) return false;

        AttackPattern[] newPatterns = new AttackPattern[groundPatterns.Length - 1];

        int newIndex = 0;

        for (int i = 0; i < groundPatterns.Length; i++)
        {
            if (i == removeIndex) continue;

            newPatterns[newIndex] = groundPatterns[i];
            newIndex++;
        }

        groundPatterns = newPatterns;

        //제거 후 인덱스가 범위를 벗어나지 않게 초기화
        comboIndex = 0;

        return true;
    }

}

/*
 * startupTime
    ↓
이펙트 생성 + 히트박스 ON
    ↓
activeTime 경과
    ↓
히트박스 OFF
    ↓
이펙트는 계속 보일 수 있음
    ↓
effectDuration 경과
    ↓
이펙트 삭제
*/