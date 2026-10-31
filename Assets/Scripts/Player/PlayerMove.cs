using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

//플레이어 움직임 (이동, 점프, 대쉬) 구현한 스크립트
public class PlayerMove : MonoBehaviour
{
    private Player player;
    private Rigidbody2D rb;

    //이동속도 세팅
    [Header("Movement Settings")]
    public float moveSpeed = 10f;

    [Header("Fall Settings")]
    [Tooltip("Maximum downward speed in world units per second. Does not limit upward or horizontal movement.")]
    [SerializeField, Min(0.1f)] private float maxFallSpeed = 25f;

    //점프 세팅
    [Header("Jump Settings")]
    public float jumpForce = 15f;
    public float jumpCooldown = 0.4f;
    private float lastJumpTime = -999f;
    [SerializeField] private int maxAirJumps = 1;
    private int remainingAirJumps;

    [Header("Jump Apex")]
    [Tooltip("Reduce gravity only while rising toward the apex, with upward speed at or below this threshold. Falling uses normal gravity. 0 disables the effect.")]
    [SerializeField, Min(0f)] private float apexSpeedThreshold = 2f;
    [Tooltip("Fraction of normal gravity while approaching the apex from below. 1 keeps normal gravity; lower values prolong the end of ascent.")]
    [SerializeField, Range(0.05f, 1f)] private float apexGravityMultiplier = 0.3f;

    [Header("Drop Through")]
    [SerializeField, Min(0.1f)] private float dropSpeed = 5f;
    [Tooltip("Minimum delay for overlap-based collision recovery. Passing fully below the departure surface restores collision immediately.")]
    [SerializeField, Min(0.02f)] private float dropMinimumDuration = 0.15f;
    [Tooltip("Clearance below the departure surface required before restoring one-way collision.")]
    [SerializeField, Min(0f)] private float dropClearance = 0.02f;
    private bool jumpRequestedDown;
    private float dropStartTime;
    private readonly List<Collider2D> dropPlatforms = new List<Collider2D>(4);
    private readonly List<IgnoredPlatformCollision> ignoredPlatformCollisions = new List<IgnoredPlatformCollision>(4);

    private struct IgnoredPlatformCollision
    {
        public Collider2D playerCollider;
        public Collider2D platform;
        public float surfaceOffsetY;
    }

    //코요태 타임 관련 세팅
    [Header("Jump Juice")]
    public float coyoteTime = 0.15f;
    public float bufferTime = 0.15f;
    private float lastGroundedTime = -999f;    //마지막으로 땅에 있었던 시간
    private float lastJumpRequestTime = -999f; //마지막으로 점프키를 누른 시간
    

    //대쉬관련 세팅
    [Header("Dash Settings")]
    public float dashSpeed = 100f;
    public float dashDuration = 0.02f;
    public float dashCooldown = 0.5f;
    public float lastDashTime; //대쉬 내부쿨 관련 변수

    //대쉬 잠금 여부 (유물 장착 시 해제)
    [SerializeField] private bool isDashUnlocked = false;

    public bool IsDashUnlocked => isDashUnlocked;

    //공중에서 마찰계수 정하기 ->스무딩 저항값
    [Header("Friction (Lerp)")]
    [Range(0, 1)] public float airControlMin = 0.8f;
    [Range(0f, 0.3f)] public float groundSmooth = 0.01f;
    [Range(0f, 0.5f)] public float airSmooth = 0.1f;

    [Header("Movement Audio")]
    [SerializeField] private SoundCue jumpSound = new SoundCue();
    [SerializeField] private SoundCue dashSound = new SoundCue();
    [Tooltip("Used only when no surface rule matches. Empty cues stay silent.")]
    [SerializeField] private SoundCue defaultFootstepSound = new SoundCue();
    [SerializeField] private SoundCue defaultLandingSound = new SoundCue();
    [Tooltip("Ordered surface rules. Grass overlays are checked before solid floor names.")]
    [SerializeField] private MovementSurfaceSounds[] movementSurfaces =
    {
        new MovementSurfaceSounds
        {
            surfaceName = "Grass",
            overlayTilemapNames = new[] { "Tilemap_Grass", "Grass" }
        },
        new MovementSurfaceSounds
        {
            surfaceName = "Stone",
            colliderObjectNames = new[] { "Tilemap_Ground", "Ground", "TIleMap_Platform", "MovingPlatform" }
        }
    };
    [Tooltip("Horizontal walking distance between steps, in world units.")]
    [SerializeField, Min(0.05f)] private float footstepDistance = 2f;
    [SerializeField, Min(0f)] private float minimumFootstepInterval = 0.12f;
    [SerializeField, Min(0f)] private float minimumLandingAirTime = 0.08f;
    [Tooltip("Probe visible Grass tiles just above the actual supporting contact.")]
    [SerializeField, Min(0f)] private float surfaceProbeHeight = 0.12f;
    [SerializeField, Min(0f)] private float surfaceProbeHalfWidth = 0.12f;
    [Tooltip("Ignore large position changes caused by teleports, not walking.")]
    [SerializeField, Min(0.1f)] private float audioTeleportDistance = 4f;

    private readonly PlayerMovementAudioState movementAudioState = new PlayerMovementAudioState();
    private Vector2 lastAudioPosition;
    private Collider2D lastAudioSupport;
    private Vector3 lastAudioSupportPosition;
    private bool hasAudioPosition;
    private Tilemap[] audioTilemaps;
    private int audioSceneHandle = int.MinValue;
    private float nextAudioTilemapScan;

    //잔상 생성기
    private GhostPooler ghostPooler;


    private void Awake()
    {
        //컴포넌트 연결
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
        ghostPooler = GetComponentInChildren<GhostPooler>();

    }

    //방향전환
    // Called immediately after Player's authoritative ground check, before jump input.
    public void TickMovementAudio()
    {
        if (player == null || rb == null || player.Health == null || player.Health.IsDead)
        {
            ResetMovementAudio();
            return;
        }

        int sceneHandle = SceneManager.GetActiveScene().handle;
        if (audioSceneHandle != sceneHandle)
        {
            audioSceneHandle = sceneHandle;
            audioTilemaps = null;
            ResetMovementAudio();
        }

        Vector2 position = rb.position;
        if (hasAudioPosition && Vector2.Distance(position, lastAudioPosition) > audioTeleportDistance)
            ResetMovementAudio();

        Collider2D support = null;
        Vector2 surfacePoint = position;
        bool grounded = player.isGrounded && player.TryGetGroundSupport(out support, out surfacePoint);
        Vector3 supportPosition = support != null ? support.transform.position : Vector3.zero;
        float distance = hasAudioPosition ? position.x - lastAudioPosition.x : 0f;
        if (support != null && support == lastAudioSupport)
            distance -= supportPosition.x - lastAudioSupportPosition.x;

        bool canStep = grounded && player.ActionState != null && player.ActionState.CanMove()
            && !player.ActionState.isDashing && Mathf.Abs(player.moveInput.x) > 0.1f
            && Mathf.Abs(distance) > 0.0001f;
        if (UIManager.Instance != null && UIManager.Instance.IsBlockingWindowOpen) canStep = false;

        movementAudioState.Tick(grounded, canStep, Mathf.Abs(distance), Time.time,
            footstepDistance, minimumFootstepInterval, minimumLandingAirTime,
            out bool step, out bool land);
        if (step || land)
        {
            MovementSurfaceSounds surface = ResolveMovementSurface(support, surfacePoint);
            SoundCue cue = land
                ? (surface != null ? surface.landingSound : defaultLandingSound)
                : (surface != null ? surface.footstepSound : defaultFootstepSound);
            PlayMovementSound(cue);
        }

        hasAudioPosition = true;
        lastAudioPosition = position;
        lastAudioSupport = support;
        lastAudioSupportPosition = supportPosition;
    }

    public void ResetMovementAudio()
    {
        movementAudioState.Reset();
        hasAudioPosition = false;
        lastAudioSupport = null;
    }

    private void PlayMovementSound(SoundCue cue)
    {
        GameAudio.Play(cue, transform.position, transform, true);
    }

    private MovementSurfaceSounds ResolveMovementSurface(Collider2D support, Vector2 point)
    {
        if (movementSurfaces == null || support == null) return null;
        if (audioTilemaps == null || Time.unscaledTime >= nextAudioTilemapScan)
        {
            audioTilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude);
            nextAudioTilemapScan = Time.unscaledTime + 1f;
        }

        // Grass is visual-only in Grid: a collider-only lookup would always choose Ground.
        foreach (MovementSurfaceSounds surface in movementSurfaces)
        {
            if (surface == null || surface.overlayTilemapNames == null || surface.overlayTilemapNames.Length == 0) continue;
            foreach (Tilemap tilemap in audioTilemaps)
            {
                if (tilemap == null || !tilemap.gameObject.activeInHierarchy
                    || tilemap.gameObject.scene != support.gameObject.scene
                    || !MatchesSurfaceName(tilemap.name, surface.overlayTilemapNames)) continue;
                for (int x = -1; x <= 1; x++)
                {
                    Vector3 sample = point + new Vector2(x * surfaceProbeHalfWidth, surfaceProbeHeight);
                    if (tilemap.HasTile(tilemap.WorldToCell(sample))) return surface;
                }
            }
        }

        foreach (MovementSurfaceSounds surface in movementSurfaces)
        {
            if (surface == null) continue;
            for (Transform floor = support.transform; floor != null; floor = floor.parent)
                if (MatchesSurfaceName(floor.name, surface.colliderObjectNames)) return surface;
        }
        return null;
    }

    private static bool MatchesSurfaceName(string objectName, string[] names)
    {
        if (names == null) return false;
        const string cloneSuffix = "(Clone)";
        if (objectName.EndsWith(cloneSuffix, System.StringComparison.Ordinal))
            objectName = objectName.Substring(0, objectName.Length - cloneSuffix.Length).TrimEnd();
        foreach (string candidate in names)
            if (!string.IsNullOrWhiteSpace(candidate)
                && string.Equals(objectName, candidate.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public void ApplyApexGravity()
    {
        if (player.isGrounded || !rb.simulated || rb.gravityScale <= 0f
            || Physics2D.gravity.y >= 0f || apexSpeedThreshold <= 0f
            || rb.linearVelocity.y <= 0f || rb.linearVelocity.y > apexSpeedThreshold) return;

        // Counter part of gravity for this physics step without changing gravityScale.
        // Dash/skill physics freezes therefore keep their original gravity restore value.
        float multiplier = Mathf.Clamp(apexGravityMultiplier, 0.05f, 1f);
        float compensation = -Physics2D.gravity.y * rb.gravityScale * (1f - multiplier);
        rb.AddForce(Vector2.up * (compensation * rb.mass), ForceMode2D.Force);
    }

    public void ApplyFallSpeedLimit()
    {
        Vector2 velocity = rb.linearVelocity;
        float speedLimit = Mathf.Max(0.1f, maxFallSpeed);
        if (velocity.y < -speedLimit)
            rb.linearVelocity = new Vector2(velocity.x, -speedLimit);
    }

    private void Flip()
    {
        if (player.ActionState.isAttacking ||
            player.ActionState.isDashing ||
            (player.ActionState.isSkillActive && !player.ActionState.CanMove())) return;

        player.isFacingRight = !player.isFacingRight;
        player.sr.flipX = !player.isFacingRight; ;

    }

    ///이동 함수
    public void ExecuteMove(float speedMultiplier = 1f, float accelMultiplier = 1f)
    {
        if (!player.ActionState.CanMove()) return;


        //움직이는 방향 바라보기
        if (player.moveInput.x > 0 && !player.isFacingRight) Flip();
        else if (player.moveInput.x < 0 && player.isFacingRight) Flip();


        //목표 속도 계산 (보정값 적용)
        float rawTargetSpeedX = player.moveInput.x * (moveSpeed * speedMultiplier);
        //공중 제어 보정
        if (!player.isGrounded) rawTargetSpeedX *= airControlMin;

        //가속/감속 비율 계산
        float decelVar = player.isGrounded ? groundSmooth : airSmooth;
        //가속도에도 보정값이 필요하다면 적용 (1f - decelVar가 클수록 빠릿하게 반응)
        float lerpFactor = (1f - decelVar) * accelMultiplier;
        //플레이어가 부드럽게 움직이도록 하는 과정
        float calculatedX = Mathf.Lerp(player.rb.linearVelocity.x, rawTargetSpeedX, lerpFactor);
        //일정량의 작은 미끄러짐은 0으로 보정
        if (player.moveInput.x == 0 && Mathf.Abs(calculatedX) < 0.1f) calculatedX = 0f;

        //움직임 실행
        player.rb.linearVelocity = new Vector2(calculatedX, player.rb.linearVelocity.y);
    }



    ///점프 함수
    public void ClearJumpRequest()
    {
        lastJumpRequestTime = float.NegativeInfinity;
        jumpRequestedDown = false;
    }

    public void RequestJump()
    {
        lastJumpRequestTime = Time.time;
        jumpRequestedDown = player.moveInput.y < -0.5f;
    }
    public void ExecuteJump()
    {
        // A pending collision restore must not discard a new jump on another floor.
        UpdateDropCollisions();

        //액션 불가능 상태
        if (!player.ActionState.CanJump()) return;

        //땅에 닿으면 초기화
        if (player.isGrounded)
        {
            lastGroundedTime = Time.time;
            remainingAirJumps = maxAirJumps;
        }

        //필터링: 선입력이 없거나 유효시간이 지나면 끝
        if (Time.time - lastJumpRequestTime > bufferTime) return;

        if (jumpRequestedDown && TryDropThrough()) return;
        
        //(지상:코요태 vs 공중)
        bool isCoyoteValid = (Time.time - lastGroundedTime <= coyoteTime);
        bool shouldJump = false;

        //지상 점프 or 코요태 타임 중
        if ((player.isGrounded || isCoyoteValid) && Time.time >= lastJumpTime + jumpCooldown)
        {
            lastGroundedTime = -999f; //코요태 타임 초기화
            shouldJump = true;
        }
        //공중점프
        else if (!player.isGrounded && remainingAirJumps > 0)
        {
            remainingAirJumps--;
            shouldJump = true;
        }


        //최종 점프 실행
        if (shouldJump)
        {
            lastJumpRequestTime = -999f; //선입력 버퍼 초기화
            lastJumpTime = Time.time;

            player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, jumpForce );
            player.NotifyJumpStarted();
            PlayMovementSound(jumpSound);
        }
    }

    private bool TryDropThrough()
    {
        if (!player.TryGetDropPlatforms(dropPlatforms)) return false;
        if (!player.TryGetGroundSupport(out _, out Vector2 surfacePoint)) return false;

        foreach (Collider2D playerCollider in GetComponentsInChildren<Collider2D>())
        {
            if (playerCollider.attachedRigidbody != rb || playerCollider.isTrigger ||
                !playerCollider.isActiveAndEnabled) continue;
            foreach (Collider2D platform in dropPlatforms)
            {
                //다른 시스템이 이미 무시한 충돌은 복구 대상으로 소유하지 않는다.
                if (Physics2D.GetIgnoreCollision(playerCollider, platform)) continue;
                Physics2D.IgnoreCollision(playerCollider, platform, true);
                ignoredPlatformCollisions.Add(new IgnoredPlatformCollision
                {
                    playerCollider = playerCollider,
                    platform = platform,
                    surfaceOffsetY = surfacePoint.y - platform.transform.position.y
                });
            }
        }

        if (ignoredPlatformCollisions.Count == 0) return false;
        dropStartTime = Time.time;
        lastJumpRequestTime = -999f;
        lastGroundedTime = -999f;
        lastJumpTime = Time.time;
        player.isGrounded = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -dropSpeed);
        return true;
    }

    public bool IsIgnoringPlatform(Collider2D platform)
    {
        foreach (IgnoredPlatformCollision collision in ignoredPlatformCollisions)
        {
            if (collision.platform == platform) return true;
        }
        return false;
    }

    public void UpdateDropCollisions()
    {
        for (int i = ignoredPlatformCollisions.Count - 1; i >= 0; i--)
        {
            IgnoredPlatformCollision collision = ignoredPlatformCollisions[i];
            if (collision.playerCollider != null && collision.platform != null &&
                collision.playerCollider.isActiveAndEnabled && collision.platform.isActiveAndEnabled)
            {
                // Use the departure surface, not overlap with the entire Tilemap collider.
                // Other platform tiles may still overlap the player after this floor is cleared.
                float surfaceY = collision.platform.transform.position.y + collision.surfaceOffsetY;
                bool clearedSurface = collision.playerCollider.bounds.max.y < surfaceY - Mathf.Max(0f, dropClearance);
                bool landedElsewhere = player.isGrounded;
                if (!clearedSurface && !landedElsewhere)
                {
                    if (Time.time - dropStartTime < dropMinimumDuration) continue;
                    if (collision.playerCollider.Distance(collision.platform).isOverlapped) continue;
                }
            }

            RestorePlatformCollision(collision);
            ignoredPlatformCollisions.RemoveAt(i);
        }
    }

    private void RestorePlatformCollision(IgnoredPlatformCollision collision)
    {
        if (collision.playerCollider != null && collision.platform != null)
            Physics2D.IgnoreCollision(collision.playerCollider, collision.platform, false);
    }

    private void OnDisable()
    {
        foreach (IgnoredPlatformCollision collision in ignoredPlatformCollisions)
            RestorePlatformCollision(collision);
        ignoredPlatformCollisions.Clear();
        ResetMovementAudio();
        lastJumpRequestTime = -999f;
        lastGroundedTime = -999f;
    }

    //현재 최대 공중 점프 횟수
    public int MaxAirJumps => maxAirJumps;

    //최대 공중 점프 횟수 변경
    public void ModifyMaxAirJumps(int amount)
    {
        maxAirJumps += amount;

        //공중 점프 횟수는 음수가 되지 않도록 제한
        maxAirJumps = Mathf.Max(0, maxAirJumps);

        //장착 중이거나 해제되었을 때
        //현재 남은 공중 점프 횟수도 같이 보정
        remainingAirJumps = Mathf.Clamp(remainingAirJumps + amount, 0, maxAirJumps);

        UnityEngine.Debug.Log(
            $"Max Air Jumps: {maxAirJumps}, " +
            $"Remaining Air Jumps: {remainingAirJumps}"
        );
    }


    /// 대쉬 함수
    public void ExecuteDash()
    {
        if (!isDashUnlocked) return;
        if (Time.time < lastDashTime + dashCooldown) return;

        lastDashTime = Time.time;

        // Cancel pending effects before the dash takes ownership of movement and gravity.
        player.Attack?.CancelCurrentAttack();
        player.Skill?.CancelCurrentSkill();

        float dir;

        if (Mathf.Abs(player.moveInput.x) > 0.01f)
            dir = Mathf.Sign(player.moveInput.x);
        else
            dir = player.isFacingRight ? 1f : -1f;

        //대쉬 중 중력 잠시 끄기
        StartCoroutine(DashRoutine(dir));

        //애니메이션 설정
        player.Animation.PlayDash(0.12f);
    }

    private IEnumerator DashRoutine(float dir)
    {
        //상태 업데이트
        player.ActionState.EnterDash();
        PlayMovementSound(dashSound);

        //중력 제거
        player.SetPhysicsFreeze(true);

        //대쉬 시작 지점에 잔상
        GameObject ghost = ghostPooler.GetGhost();
        if (ghost != null)
        {
            ghost.SetActive(true);
            ghost.GetComponent<GhostEffect>().Init(player.transform.position, player.sr.flipX);
        }

        //고정 이동 루프
        float timer = 0f;
        while (timer < dashDuration)
        {
            player.rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        player.rb.linearVelocity = new Vector2(0f, player.rb.linearVelocity.y);

        //상태 되돌리기 but,본인이 바꾼 상태일때만 노말로 교체->남이 바꾼 State 참견 금지
        if (player.ActionState.isDashing)
        {
            player.ActionState.EnterNormal();
        }


        //중력 되돌리기
        player.SetPhysicsFreeze(false);

    }

    //대쉬 해금 여부 설정
    public void SetDashUnlocked(bool unlocked)
    {
        isDashUnlocked = unlocked;
    }

}

[System.Serializable]
public sealed class MovementSurfaceSounds
{
    public string surfaceName;
    [Tooltip("Exact GameObject names on the supporting collider or its parents. Case insensitive.")]
    public string[] colliderObjectNames = new string[0];
    [Tooltip("Visual-only Tilemaps sampled at the player's supporting foot position.")]
    public string[] overlayTilemapNames = new string[0];
    public SoundCue footstepSound = new SoundCue();
    public SoundCue landingSound = new SoundCue();
}

// Pure timing state: no additional MonoBehaviour/script import is needed.
internal sealed class PlayerMovementAudioState
{
    private bool initialized, wasGrounded, hasGroundedBaseline;
    private float airborneSince, travel, lastStepTime;

    public void Reset()
    {
        initialized = wasGrounded = hasGroundedBaseline = false;
        travel = 0f;
        lastStepTime = float.NegativeInfinity;
    }

    public void Tick(bool grounded, bool canStep, float distance, float time,
        float stepDistance, float stepInterval, float landingAirTime, out bool step, out bool land)
    {
        step = land = false;
        if (!initialized)
        {
            initialized = true;
            wasGrounded = hasGroundedBaseline = grounded;
            airborneSince = time;
            travel = 0f;
            lastStepTime = float.NegativeInfinity;
            return;
        }
        if (!grounded)
        {
            if (wasGrounded) airborneSince = time;
            wasGrounded = false;
            travel = 0f;
            return;
        }
        if (!wasGrounded)
        {
            land = hasGroundedBaseline && time - airborneSince >= System.Math.Max(0f, landingAirTime);
            hasGroundedBaseline = wasGrounded = true;
            travel = 0f;
            return;
        }
        hasGroundedBaseline = true;
        if (!canStep) { travel = 0f; return; }
        travel += System.Math.Max(0f, distance);
        if (travel < System.Math.Max(0.05f, stepDistance) || time - lastStepTime < System.Math.Max(0f, stepInterval)) return;
        step = true;
        travel = 0f;
        lastStepTime = time;
    }
}
