using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 보스방 연출. 보스방 안쪽에 트리거 콜라이더(Is Trigger)와 같이 붙인다.
///
/// 플레이어가 들어오면:
///   문이 위에서 쿵 내려와 잠김 → 카메라가 방 전체로 → 보스 이름 카드 → 보스 등장 → HP바
/// 보스를 잡으면:
///   히트스탑 + 슬로모션 + 큰 흔들림 → HP바 사라짐 → 카메라 복귀 → 문 열림 → 저장
/// 보스전 중에 죽으면:
///   부활한 뒤 방을 처음 상태로 되돌린다(문 열림, 보스 체력·위치 초기화). 다시 들어오면 재시작.
///
/// 잡은 보스방은 FieldObjectState로 기억해서 다시 와도 문이 열려 있고 보스가 없다.
/// 보스 오브젝트에 BossMarker를 붙이면 이름·칭호가 카드와 HP바에 나온다.
///
/// 돌문·안개(11.01): 문은 돌기둥 그림으로 바뀌고 떨어질 때 돌가루가 퍼진다. 바닥에 안개가 깔리고,
/// 보스 자리로 안개가 모였다가 보스가 나오는 순간 흩어진다. 잡으면 안개가 걷힌다. (BossFog)
/// HP바는 기본으로 끔 (showHealthBar).
///
/// 달빛 연출(moonPresentation, 11.01): 방이 어두워지고 돌가루·달빛 → 2페이즈 충격파·붉은 빛
/// → 격파 빛살 → 처치한 뒤에야 보주가 처음 드러나 플레이어에게 날아간다. (BossFx, BossScreenFx, BossOrbReveal)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossRoom : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private EnemyHealth boss;
    [Tooltip("켜면 연출이 끝날 때까지 보스를 꺼 둔다. 끄면 처음부터 보스가 서 있다.")]
    [SerializeField] private bool hideBossUntilIntro = true;

    [Header("Doors")]
    [Tooltip("닫혔을 때 위치에 놓아 둔다. 평소에는 꺼져 있다가 보스전 때 켜진다.")]
    [SerializeField] private Transform[] doors;
    [SerializeField, Min(0f)] private float doorDropHeight = 3f;
    [SerializeField, Min(0.05f)] private float doorMoveSeconds = 0.35f;

    [Header("Camera")]
    [SerializeField] private bool useRoomCamera = true;
    [Tooltip("비우면 이 트리거 영역의 가운데를 비춘다.")]
    [SerializeField] private Transform cameraCenter;
    [Tooltip("0이면 지금 카메라 크기 그대로.")]
    [SerializeField, Min(0f)] private float cameraSize = 0f;

    [Header("Timing (실제 시간, 초)")]
    [SerializeField, Min(0f)] private float introDelay = 0.6f;
    [SerializeField, Min(1.3f)] private float cardSeconds = 2.6f;
    [SerializeField, Min(0f)] private float clearHitStop = 0.22f;
    [SerializeField, Range(0.05f, 1f)] private float clearSlowScale = 0.3f;
    [SerializeField, Min(0f)] private float clearSlowSeconds = 1.1f;
    [SerializeField, Min(0f)] private float unlockDelay = 1.4f;

    [Header("Story")]
    [Tooltip("보스를 잡은 뒤 뜨는 대화. 비우면 Resources/StoryDialogues의 Boss Clear를 쓴다. 다 읽어야 문이 열린다.\n대사에 {보스}라고 쓰면 보스 이름이 된다.")]
    [SerializeField] private DialogueData clearDialogue;
    [Tooltip("대화 전에 \"격파\" 카드를 띄운다.")]
    [SerializeField] private bool showDefeatCard = true;
    [Tooltip("다 끝나면 엔딩 화면(플레이해 주셔서 감사합니다)을 띄우고 타이틀로 돌아간다.")]
    [SerializeField] private bool showEnding = true;

    [Header("Audio")]
    [SerializeField] private SoundCue introSound = new SoundCue();
    [SerializeField] private SoundCue doorCloseSound = new SoundCue();
    [SerializeField] private SoundCue doorOpenSound = new SoundCue();
    [SerializeField] private SoundCue clearSound = new SoundCue();
    [Header("Presentation (달빛)")]
    [Tooltip("등장 때 방이 어두워지고 돌가루·달빛, 2페이즈 충격파·붉은 빛, 격파 빛살·보주 등장을 켠다.")]
    [SerializeField] private bool moonPresentation = true;
    [Tooltip("격파 뒤 처음 모습을 드러내는 보주 그림. 비우면 코드로 그린 빛 구슬을 쓴다.")]
    [SerializeField] private Sprite revealOrbSprite;
    [Header("Look (돌문·안개)")]
    [Tooltip("문을 돌기둥 그림(Resources/Presentation/Boss/BossDoor_StonePillar)으로 바꾼다. 문 충돌 범위는 그대로.")]
    [SerializeField] private bool stonePillarDoors = true;
    [Tooltip("바닥 안개 + 보스 등장 때 안개가 모였다 흩어짐 + 잡으면 걷힘")]
    [SerializeField] private bool fog = true;
    [Header("Ambience (분위기·입장 예고)")]
    [Tooltip("하늘 위에서 내려오는 옅은 빛줄기 + 천천히 떠내려오는 먼지")]
    [SerializeField] private bool ambience = true;
    [Tooltip("보스방에 가까이 오면 땅이 울리고 문 자리에서 빛이 새어 나온다")]
    [SerializeField] private bool approachWarning = true;
    [Tooltip("보스방 가장자리에서 이 거리(칸)부터 예고가 시작된다")]
    [SerializeField, Min(1f)] private float warnDistance = 14f;
    [Tooltip("예고 구간에 들어가기 전에 천장에서 돌이 떨어진다 (맞으면 체력 감소)")]
    [SerializeField] private bool approachRocks = true;
    [SerializeField, Min(0)] private int approachRockCount = 3;
    [SerializeField, Min(0f)] private float approachRockDamage = 1f;
    [Tooltip("예고 구간 바깥 이 거리(칸) 안에 들어오면 돌이 떨어지기 시작")]
    [SerializeField, Min(1f)] private float approachRockRange = 8f;
    [Header("Health Bar")]
    [Tooltip("보스 HP바를 띄운다 (지금은 끔). 끄면 골렘 머리 위 작은 HP바도 같이 숨긴다.")]
    [SerializeField] private bool showHealthBar = false;
    [Tooltip("보스 머리 위에 한 줄짜리 체력바를 띄운다 (페이즈 표시 없음).")]
    [SerializeField] private bool overheadHealthBar = true;
    [Tooltip("보스를 캐릭터 뒤에 그린다 (원래는 Enemy 레이어라 캐릭터 앞에 그려짐)")]
    [SerializeField] private bool bossBehindPlayer = true;
    [Tooltip("HP바에서 2페이즈가 되는 자리(체력 비율). 골렘의 Phase Two Health와 맞춰 둔다.")]
    [SerializeField, Range(0f, 1f)] private float phaseMark = 0.5f;

    private const float DefeatCardSeconds = 1.8f;

    private enum State { Idle, Intro, Fighting, Cleared }

    private State state = State.Idle;
    private Vector3[] doorClosedPositions;
    private Vector3 bossStartPosition;
    private CinemachineCamera roomCamera;
    private PlayerHealth playerHealth;
    private Coroutine routine;
    private bool shownCardOnce;
    private string bossDisplayName;
    private BossMoonBeam beam;
    private GolemBossController golem;
    private Vector3 lastFragmentCenter;
    private BossFog fogLayer;
    private BossAmbience ambienceFx;
    private Transform playerT;
    private SpriteRenderer leakLeft, leakRight;
    private float nextRumble, leakStrength;
    private Material leakMat;
    private bool rocksDropped;
    private bool hasFragmentCenter;

    public bool IsCleared => state == State.Cleared;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        doorClosedPositions = new Vector3[doors != null ? doors.Length : 0];
        for (int i = 0; i < doorClosedPositions.Length; i++)
        {
            if (doors[i] == null) continue;
            doorClosedPositions[i] = doors[i].position;
            if (stonePillarDoors) DressDoor(doors[i]);
            doors[i].gameObject.SetActive(false);
        }

        if (boss != null)
        {
            bossStartPosition = boss.transform.position;
            GolemBossController controller = boss.GetComponent<GolemBossController>();
            if (controller != null) ConfigureCombatArena(controller);
        }

        if (FieldObjectState.IsDone(this))
        {
            state = State.Cleared;
            if (boss != null) boss.gameObject.SetActive(false);
            return;
        }

        if (hideBossUntilIntro && boss != null) boss.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (ambience) ambienceFx = BossAmbience.Create(RoomBounds());
    }

    // ───────────── 입장 예고 ─────────────

    private void Update()
    {
        if (!approachWarning) return;
        bool idle = state == State.Idle && boss != null;
        float near = 0f;   // 0 = 멀다, 1 = 문 바로 앞
        int side = 0;
        if (idle)
        {
            if (playerT == null)
            {
                Player p = FindAnyObjectByType<Player>();
                if (p != null) playerT = p.transform;
            }
            if (playerT != null)
            {
                Bounds room = RoomBounds();
                float px = playerT.position.x;
                float d = px < room.min.x ? room.min.x - px : px > room.max.x ? px - room.max.x : 0f;
                side = px < room.center.x ? -1 : 1;
                if (d > 0f && d < warnDistance && Mathf.Abs(playerT.position.y - room.center.y) < room.size.y + 4f)
                    near = 1f - d / warnDistance;
                // 예고 구간에 닿기 전: 낙석 (한 번만)
                if (approachRocks && !rocksDropped && approachRockCount > 0 && d > warnDistance && d < warnDistance + approachRockRange
                    && Mathf.Abs(playerT.position.y - room.center.y) < room.size.y + 4f)
                {
                    rocksDropped = true;
                    StartCoroutine(ApproachRocks(side));
                }
            }
        }

        // 문 자리에서 새어 나오는 빛
        leakStrength = Mathf.MoveTowards(leakStrength, near, Time.unscaledDeltaTime * 0.8f);
        UpdateLeak(ref leakLeft, -1, side == -1 ? leakStrength : Mathf.Min(leakStrength, 0f));
        UpdateLeak(ref leakRight, 1, side == 1 ? leakStrength : Mathf.Min(leakStrength, 0f));

        // 땅이 울림: 가까울수록 자주, 세게
        if (near > 0.05f && Time.unscaledTime >= nextRumble)
        {
            nextRumble = Time.unscaledTime + Mathf.Lerp(3.2f, 1.4f, near) + Random.Range(0f, 0.6f);
            CameraShake.Shake(Mathf.Lerp(0.06f, 0.16f, near));
            Bounds room = RoomBounds();
            float x = side < 0 ? room.min.x : room.max.x;
            Bounds fall = new Bounds(new Vector3(x - side * 3f, room.max.y, 0f), new Vector3(8f, room.size.y, 1f));
            BossStageFx.RockFall(fall, Mathf.RoundToInt(Mathf.Lerp(2f, 6f, near)), 0.6f);
            BossFog.LandingDust(new Vector3(x, room.min.y, 0f), 2.5f);
        }
    }

    // 플레이어 앞쪽(보스방 방향)에 하나씩 떨어진다. 멈춰 있으면 머리 위로도 떨어지니 피해야 한다.
    private IEnumerator ApproachRocks(int side)
    {
        float toRoom = -side;   // 플레이어가 왼쪽(side=-1)이면 오른쪽(+1)으로 가는 중
        float[] ahead = { 2.5f, 1.2f, 0f, 1.8f, 0.6f };
        for (int i = 0; i < approachRockCount; i++)
        {
            if (playerT == null || state != State.Idle) yield break;
            Vector3 at = new Vector3(playerT.position.x + toRoom * ahead[i % ahead.Length], playerT.position.y, 0f);
            FallingRockHazard.Drop(at, approachRockDamage, 10f);
            yield return new WaitForSeconds(0.9f);
        }
    }

    private void UpdateLeak(ref SpriteRenderer leak, int side, float strength)
    {
        if (strength <= 0.001f) { if (leak != null) leak.enabled = false; return; }
        Bounds room = RoomBounds();
        if (leak == null)
        {
            GameObject go = new GameObject(side < 0 ? "DoorLight_Left" : "DoorLight_Right");
            go.transform.SetParent(transform, true);
            leak = go.AddComponent<SpriteRenderer>();
            Shader wavy = Resources.Load<Shader>("Presentation/Boss/WavyLightColumn");
            if (wavy != null && wavy.isSupported)
            {
                leak.sprite = GolemBossVisual.Square;   // 흰 네모 + 일렁이는 빛기둥 셰이더
                leakMat ??= new Material(wavy) { hideFlags = HideFlags.DontSave };
                leak.sharedMaterial = leakMat;
            }
            else
            {
                leak.sprite = BossFxArt.UpFade;   // 셰이더가 없으면 단순한 빛
                leak.sharedMaterial = BossFxArt.Unlit;
            }
            leak.sortingLayerName = "Default";
            leak.sortingOrder = 46;
            Vector2 size = leak.sprite.bounds.size;
            float h = Mathf.Max(4f, room.size.y) + 2f;
            go.transform.localScale = new Vector3(3.6f / size.x, h / size.y, 1f);
            go.transform.position = new Vector3(side < 0 ? room.min.x - 1f : room.max.x + 1f, room.min.y + h * 0.5f, 0f);
        }
        leak.enabled = true;
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 2.2f);
        leak.color = new Color(0.55f, 0.78f, 1f, 0.75f * strength * pulse);
    }

    private void OnDisable()
    {
        UnwatchPlayer();
        UnhookBoss();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (state != State.Idle || boss == null) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null || health.IsDead) return;

        WatchPlayer(health);
        routine = StartCoroutine(FightRoutine());
    }

    // ───────────── 흐름 ─────────────

    private IEnumerator FightRoutine()
    {
        state = State.Intro;

        StartCoroutine(MoveDoors(true));
        leakStrength = 0f;
        if (leakLeft != null) leakLeft.enabled = false;
        if (leakRight != null) leakRight.enabled = false;
        if (useRoomCamera) SetRoomCamera(true);

        // 방이 어두워지고 천장에서 돌가루가 떨어진다
        if (moonPresentation)
        {
            BossScreenFx.Darken(0.55f, 0.5f);
            BossScreenFx.Letterbox(true, 0.6f);
            BossFx.CeilingDust(RoomBounds(), 2.2f);
            BossStageFx.RockFall(RoomBounds(), 14, 2f);   // 천장에서 작은 돌이 우수수
        }
        if (fog)
        {
            if (fogLayer == null) fogLayer = BossFog.Create(RoomBounds());
            fogLayer.SetTint(BossFog.FogColor);
            fogLayer.SetDensity(1f, 1.8f);
        }

        yield return new WaitForSecondsRealtime(introDelay);

        BossMarker marker = boss.GetComponent<BossMarker>();
        string bossName = marker != null ? marker.BossName : boss.name;
        bossDisplayName = bossName;
        string bossTitle = marker != null ? marker.BossTitle : "";

        // 두 번째 도전부터는 카드를 짧게
        float card = shownCardOnce ? Mathf.Min(cardSeconds, 1.4f) : cardSeconds;
        shownCardOnce = true;
        BossTitleCard.Show(bossTitle, bossName, card);
        GameAudio.Play(introSound, boss.transform.position);

        // 위에서 달빛이 내려와 보스 자리를 비춘다
        if (moonPresentation)
        {
            if (beam == null) beam = CreateBeam();
            if (beam != null) beam.SetMood(BossMoonBeam.Mood.Cool, 1.4f);
            BossScreenFx.Darken(0.2f, 1.2f);
        }

        // 보스 자리로 안개가 모인다
        Vector3 revealAt = bossStartPosition + Vector3.up * 1.5f;
        if (fog) BossFog.Gather(revealAt, 6f, card * 0.5f);

        yield return new WaitForSecondsRealtime(card * 0.5f);

        if (!boss.gameObject.activeSelf)
        {
            boss.transform.position = bossStartPosition;
            boss.gameObject.SetActive(true);
        }
        // 모인 안개가 흩어지며 보스가 드러난다
        if (fog)
        {
            BossFog.Burst(BossCenter(), 7f);
            CameraShake.Shake(CameraShake.Light);
        }

        golem = boss.GetComponent<GolemBossController>();
        if (golem != null)
        {
            ConfigureCombatArena(golem);
            golem.onPhaseTwo.RemoveListener(HandlePhaseTwo);
            golem.onPhaseTwo.AddListener(HandlePhaseTwo);
        }

        if (showHealthBar) BossHealthBarUI.Show(boss, bossName, golem != null ? phaseMark : -1f);
        if (overheadHealthBar) BossOverheadBar.Show(boss);
        TuneBossLook();
        state = State.Fighting;

        if (moonPresentation)
        {
            BossScreenFx.Darken(0f, 0.8f);
            BossScreenFx.Letterbox(false, 0.5f);
        }

        // 2페이즈에 골렘이 둘로 갈라지면 본체는 숨고 체력이 남은 채로 있다.
        // 조각이 모두 쓰러질 때(IsDefeated)까지 기다린다. (조각 HP바 두 줄은 BossHealthBarUI가 맡음)
        hasFragmentCenter = false;
        float nextTune = 0f;
        bool splitShown = false;
        while (boss != null && !boss.IsDead && (golem == null || !golem.IsDefeated))
        {
            if (Time.unscaledTime >= nextTune) { nextTune = Time.unscaledTime + 0.3f; TuneBossLook(); }   // 갈라진 조각도
            if (golem != null && golem.HasSplit && TryFragmentCenter(out Vector3 c))
            {
                if (!splitShown)
                {
                    splitShown = true;   // 둘로 갈라지는 순간
                    BossStageFx.SplitBurst(c, RoomBounds().min.y, 6f);
                    BossScreenFx.Flash(new Color(0.7f, 0.85f, 1f, 0.5f), 0.5f);
                    TuneBossLook();
                }
                lastFragmentCenter = c;
                hasFragmentCenter = true;
            }
            yield return null;
        }

        yield return ClearRoutine();
    }

    // 2페이즈: 큰 충격파 + 바닥 물결 + 방이 붉게 + HP바 주황
    private void HandlePhaseTwo()
    {
        if (!moonPresentation || state != State.Fighting || boss == null) return;

        BossFx.PhaseTwoBurst(BossCenter(), BossGroundY());
        BossStageFx.Crack(BossCenter(), BossGroundY(), 14f, 3.5f);   // 바닥이 갈라짐
        BossStageFx.Debris(new Vector3(BossCenter().x, BossGroundY() + 0.3f, 0f), 10, 6f, 8f);
        BossScreenFx.Flash(new Color(1f, 0.75f, 0.5f, 0.55f), 0.9f);
        BossScreenFx.Tint(true, 1.2f);
        if (beam != null) beam.SetMood(BossMoonBeam.Mood.Warm, 1f);
        if (showHealthBar) BossHealthBarUI.SetPhaseTwo(true);
        if (fogLayer != null)
        {
            fogLayer.SetTint(new Color(1f, 0.78f, 0.7f, 1f));   // 붉은 빛에 물든 안개
            fogLayer.SetDensity(1.3f, 0.8f);
        }
        StartCoroutine(PhaseLetterbox());
    }

    private IEnumerator PhaseLetterbox()
    {
        BossScreenFx.Letterbox(true, 0.3f);
        yield return new WaitForSecondsRealtime(1.3f);
        if (state == State.Fighting) BossScreenFx.Letterbox(false, 0.5f);
    }

    private IEnumerator ClearRoutine()
    {
        state = State.Cleared;
        Transform player = playerHealth != null ? playerHealth.transform : null;
        UnwatchPlayer();
        UnhookBoss();
        FieldObjectState.MarkDone(this);
        GameAudio.Play(clearSound, transform.position);

        // 보스가 사라지기 전에 자리를 기억해 둔다
        // 갈라졌으면 마지막 조각이 쓰러진 자리에서 격파 연출을 한다
        Vector3 center = hasFragmentCenter ? lastFragmentCenter : BossCenter();
        float groundY = hasFragmentCenter ? RoomBounds().min.y : BossGroundY();

        // 마지막 일격: 멈춤 → 슬로모션
        HitStop.Stop(clearHitStop);
        CameraShake.Shake(CameraShake.Heavy);
        if (moonPresentation)
        {
            BossScreenFx.Flash(new Color(1f, 1f, 1f, 0.9f), 0.35f);
            BossScreenFx.Letterbox(true, 0.4f);
        }
        yield return new WaitForSecondsRealtime(clearHitStop);
        HitStop.Cancel();   // 멈춤이 남아 있으면 슬로모션이 멈춤에 묻히므로 먼저 끝낸다
        HitStop.Slow(clearSlowScale, clearSlowSeconds);

        if (showHealthBar) BossHealthBarUI.Hide();
        BossOverheadBar.Hide();

        // 갈라진 틈에서 빛이 터지고 빛 조각이 흩어진다. 방은 다시 차가운 달빛으로.
        if (moonPresentation)
        {
            BossFx.DefeatBurst(center);
            BossStageFx.Crack(center, groundY, 18f, 4f, true);
            BossStageFx.Debris(new Vector3(center.x, groundY + 0.3f, 0f), 14, 7f, 9f, true);
            if (fogLayer != null) { fogLayer.SetTint(BossFog.FogColor); fogLayer.Clear(3f); fogLayer = null; }   // 안개가 걷힌다
            BossScreenFx.Tint(false, 1.4f);
            if (beam != null) beam.SetMood(BossMoonBeam.Mood.Bright, 1.6f);
        }

        yield return new WaitForSecondsRealtime(unlockDelay);

        // 보주는 처치한 뒤에야 처음 보인다
        BossOrbReveal orb = moonPresentation ? BossOrbReveal.Play(center, player, revealOrbSprite) : null;

        string shownName = string.IsNullOrEmpty(bossDisplayName) ? "보스" : bossDisplayName;
        if (showDefeatCard)
        {
            BossTitleCard.ShowDefeat(shownName, DefeatCardSeconds);
            yield return new WaitForSecondsRealtime(DefeatCardSeconds + 0.3f);
        }

        // 보주가 플레이어 곁으로 날아가 스며든다
        if (orb != null)
        {
            orb.FlyToPlayer();
            while (orb != null && !orb.IsDone) yield return null;
        }

        if (moonPresentation) BossScreenFx.Letterbox(false, 0.5f);

        // 쓰러진 보스의 마지막 말
        DialogueData dialogue = clearDialogue != null ? clearDialogue : StoryDialogues.BossClear;
        if (dialogue != null)
        {
            DialogueBox.Play(dialogue.With("{보스}", shownName));
            while (DialogueBox.IsBusy) yield return null;
        }

        if (useRoomCamera) SetRoomCamera(false);
        yield return MoveDoors(false);

        // 보스 처치는 바로 저장한다(제단 위치는 그대로).
        GameSession.SaveProgressNow();

        // 11.01: 엔딩 (감사 문구 → 타이틀)
        if (showEnding)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            BossEnding.Play();
        }
        routine = null;
    }

    // ───────────── 보스 모습 ─────────────

    private readonly System.Collections.Generic.HashSet<SpriteRenderer> tunedRenderers = new System.Collections.Generic.HashSet<SpriteRenderer>();
    private static readonly System.Reflection.FieldInfo GolemRockField =
        typeof(GolemBossController).GetField("rockSprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    private static readonly System.Reflection.FieldInfo GolemBarField =
        typeof(GolemBossVisual).GetField("showHealthBar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    // 보스(와 갈라진 조각)를 캐릭터 뒤로 보내고, 머리 위 HP바를 숨긴다. 골렘 코드·프리팹은 그대로 (실행 중에만).
    private void TuneBossLook()
    {
        if (boss == null) return;
        System.Collections.Generic.List<GameObject> targets = new System.Collections.Generic.List<GameObject> { boss.gameObject };
        foreach (GolemBossController g in FindObjectsByType<GolemBossController>())
            if (g != null && !targets.Contains(g.gameObject)) targets.Add(g.gameObject);

        int ground = SortingLayer.NameToID("Ground");
        foreach (GameObject t in targets)
        {
            if (t.GetComponent<BossHitFx>() == null && t.GetComponent<EnemyHealth>() != null) t.AddComponent<BossHitFx>();   // 피격 번쩍임·돌가루
            GolemBossController gc = t.GetComponent<GolemBossController>();
            if (gc != null && GolemRockField != null && GolemRockField.GetValue(gc) == null)
                GolemRockField.SetValue(gc, BossStageFx.RandomFallRock());   // 낙석 공격: 회색 네모 대신 이끼 낀 돌
            if (!showHealthBar && GolemBarField != null)
                foreach (GolemBossVisual v in t.GetComponentsInChildren<GolemBossVisual>(true)) GolemBarField.SetValue(v, false);
            if (!bossBehindPlayer) continue;
            foreach (SpriteRenderer sr in t.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || tunedRenderers.Contains(sr)) continue;
                string layer = SortingLayer.IDToName(sr.sortingLayerID);
                if (layer != "Enemy" && layer != "Effects") continue;
                // 땅 타일(0)보다 위, 캐릭터(Player 레이어)보다 뒤. 원래 앞뒤 순서는 유지.
                sr.sortingLayerID = ground;
                sr.sortingOrder = 100 + sr.sortingOrder + (layer == "Effects" ? 20 : 0);
                tunedRenderers.Add(sr);
            }
        }
    }

    // ───────────── 연출 도우미 ─────────────

    private Bounds RoomBounds() => GetComponent<Collider2D>().bounds;

    private void ConfigureCombatArena(GolemBossController controller)
    {
        Bounds room = RoomBounds();
        Transform leftDoor = null, rightDoor = null;
        if (doors != null)
            foreach (Transform door in doors)
            {
                if (door == null) continue;
                if (leftDoor == null || door.position.x < leftDoor.position.x) leftDoor = door;
                if (rightDoor == null || door.position.x > rightDoor.position.x) rightDoor = door;
            }
        float left = room.min.x, right = room.max.x;
        if (leftDoor != null && rightDoor != leftDoor)
        {
            left = DoorInnerEdge(leftDoor, true);
            right = DoorInnerEdge(rightDoor, false);
        }
        controller.ConfigureArena(left, right, room.min.y, room.max.y);
    }

    private static float DoorInnerEdge(Transform door, bool isLeft)
    {
        BoxCollider2D box = door.GetComponent<BoxCollider2D>();
        if (box == null) return door.position.x;
        Vector2 edge = box.offset + Vector2.right * box.size.x * (isLeft ? 0.5f : -0.5f);
        return box.transform.TransformPoint(edge).x;
    }

    // 달빛 기둥 시작점을 화면 위로 더 올린다 (11.01: 빛이 낮게 시작해 보이던 것)
    private const float BeamRaise = 4f;

    private BossMoonBeam CreateBeam()
    {
        Bounds room = RoomBounds();
        float x = boss != null ? bossStartPosition.x : room.center.x;
        // 화면 맨 위 바깥에서 시작하게 (방 카메라 = 방 가운데, 화면 절반 높이 = 카메라 크기)
        Camera main = Camera.main;
        float half = cameraSize > 0f ? cameraSize : main != null && main.orthographic ? main.orthographicSize : 7f;
        Vector3 c = cameraCenter != null ? cameraCenter.position : room.center;
        float top = Mathf.Max(room.max.y + 1f, c.y + half + 2f) + BeamRaise;
        float height = Mathf.Max(4f, top - room.min.y);
        return BossMoonBeam.Create(new Vector3(x, top, 0f), height, 5f);
    }

    /// <summary>보스 그림의 가운데. 그림이 없으면 위치에서 조금 위.</summary>
    private Vector3 BossCenter()
    {
        if (boss == null) return transform.position;
        return RendererCenter(boss.gameObject);
    }

    private static Vector3 RendererCenter(GameObject go)
    {
        bool found = false;
        Bounds b = default;
        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!sr.enabled || sr.sprite == null) continue;
            if (!found) { b = sr.bounds; found = true; }
            else b.Encapsulate(sr.bounds);
        }
        if (found) return new Vector3(b.center.x, b.center.y, 0f);
        return go.transform.position + Vector3.up * 1.5f;
    }

    private bool TryFragmentCenter(out Vector3 center)
    {
        center = default;
        if (golem == null) return false;
        foreach (GolemBossController f in golem.Fragments)
        {
            if (f == null || !f.gameObject.activeInHierarchy) continue;
            center = RendererCenter(f.gameObject);
            return true;
        }
        return false;
    }

    private float BossGroundY()
    {
        if (boss == null) return transform.position.y;
        Collider2D col = boss.GetComponent<Collider2D>();
        return col != null ? col.bounds.min.y : boss.transform.position.y;
    }

    private void UnhookBoss()
    {
        if (golem != null) golem.onPhaseTwo.RemoveListener(HandlePhaseTwo);
        golem = null;
    }

    private void ClearPresentation()
    {
        BossScreenFx.Clear();
        BossHealthBarUI.SetPhaseTwo(false);
        if (fogLayer != null) { fogLayer.Clear(0.6f); fogLayer = null; }
        if (beam != null) beam.FadeAndDestroy(0.4f);
        beam = null;
    }

    // 보스전 중 플레이어 사망 → 부활을 기다렸다가 방을 처음 상태로
    private void HandlePlayerDied()
    {
        if (state != State.Intro && state != State.Fighting) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ResetAfterRespawn());
    }

    private IEnumerator ResetAfterRespawn()
    {
        BossHealthBarUI.Hide();
        BossOverheadBar.Hide();
        BossTitleCard.Hide();
        UnhookBoss();
        ClearPresentation();

        while (playerHealth != null && playerHealth.IsDead) yield return null;

        ResetRoom();
        routine = null;
    }

    private void ResetRoom()
    {
        UnwatchPlayer();
        StopAllCoroutines();

        for (int i = 0; i < doorClosedPositions.Length; i++)
        {
            if (doors[i] == null) continue;
            doors[i].position = doorClosedPositions[i];
            doors[i].gameObject.SetActive(false);
        }

        if (useRoomCamera) SetRoomCamera(false);
        rocksDropped = false;   // 다시 다가오면 또 떨어짐

        if (boss != null)
        {
            // 껐다 켜면 EnemyHealth/EnemyAI가 체력·상태를 처음으로 돌린다.
            boss.gameObject.SetActive(false);
            boss.transform.position = bossStartPosition;
            if (!hideBossUntilIntro) boss.gameObject.SetActive(true);
        }

        state = State.Idle;
    }

    // ───────────── 문 ─────────────

    private IEnumerator MoveDoors(bool close)
    {
        if (doorClosedPositions.Length == 0) yield break;
        bool hasDoor = false;
        foreach (Transform door in doors) if (door != null) { hasDoor = true; break; }
        if (!hasDoor) yield break;
        if (!close) GameAudio.Play(doorOpenSound, transform.position);

        Vector3 up = Vector3.up * doorDropHeight;
        for (int i = 0; i < doorClosedPositions.Length; i++)
        {
            if (doors[i] == null) continue;
            if (close)
            {
                doors[i].position = doorClosedPositions[i] + up;
                doors[i].gameObject.SetActive(true);
            }
        }

        float t = 0f;
        while (t < doorMoveSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / doorMoveSeconds);
            // 닫힐 때는 가속하며 떨어지고, 열릴 때는 부드럽게 올라간다.
            float drop = close ? 1f - k * k : k * k * (3f - 2f * k);
            for (int i = 0; i < doorClosedPositions.Length; i++)
            {
                if (doors[i] == null) continue;
                doors[i].position = doorClosedPositions[i] + up * drop;
            }
            yield return null;
        }

        for (int i = 0; i < doorClosedPositions.Length; i++)
        {
            if (doors[i] == null) continue;
            doors[i].position = doorClosedPositions[i] + (close ? Vector3.zero : up);
            if (!close) doors[i].gameObject.SetActive(false);
        }

        if (close)
        {
            CameraShake.Shake(CameraShake.Medium);
            GameAudio.Play(doorCloseSound, transform.position);
            if (fog || stonePillarDoors)
                foreach (Transform door in doors)
                    if (door != null)
                    {
                        BossFog.DoorDust(DoorBottom(door), 3f);   // 돌가루
                        BossStageFx.Debris(DoorBottom(door) + Vector3.up * 0.3f, 5, 3f, 5f);   // 돌 파편
                    }
        }
    }

    // 문을 돌기둥 그림으로: 원래 그림은 끄고, 같은 높이의 돌기둥을 자식으로 붙인다
    private static void DressDoor(Transform door)
    {
        Sprite pillar = Resources.Load<Sprite>("Presentation/Boss/BossDoor_StonePillar");
        if (pillar == null || door.Find("StonePillar") != null) return;

        SpriteRenderer old = door.GetComponent<SpriteRenderer>();
        BoxCollider2D col = door.GetComponent<BoxCollider2D>();
        float h = col != null ? col.size.y * Mathf.Abs(door.lossyScale.y) : old != null ? old.bounds.size.y : 5f;
        float w = col != null ? col.size.x * Mathf.Abs(door.lossyScale.x) : 1f;
        Vector3 center = col != null ? (Vector3)(Vector2)col.bounds.center : door.position;

        GameObject go = new GameObject("StonePillar");
        go.transform.SetParent(door, true);
        go.transform.position = new Vector3(center.x, center.y, door.position.z);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pillar;
        sr.sortingLayerName = old != null ? SortingLayer.IDToName(old.sortingLayerID) : "Ground";
        sr.sortingOrder = old != null ? old.sortingOrder : 5;
        if (old != null) { sr.sharedMaterial = old.sharedMaterial; old.enabled = false; }

        // 높이는 문에 맞추고, 폭은 너무 넓어지지 않게 (충돌 폭의 1.8배까지)
        float k = h / pillar.bounds.size.y;
        float kx = Mathf.Min(k, w * 1.8f / pillar.bounds.size.x);
        Vector3 ls = door.lossyScale;
        go.transform.localScale = new Vector3(kx / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), k / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);
    }

    private static Vector3 DoorBottom(Transform door)
    {
        BoxCollider2D col = door.GetComponent<BoxCollider2D>();
        if (col != null) return new Vector3(col.bounds.center.x, col.bounds.min.y, 0f);
        return door.position;
    }

    // ───────────── 카메라 ─────────────

    private void SetRoomCamera(bool on)
    {
        if (on && roomCamera == null) roomCamera = CreateRoomCamera();
        if (roomCamera != null) roomCamera.gameObject.SetActive(on);
    }

    private CinemachineCamera CreateRoomCamera()
    {
        Vector3 center = cameraCenter != null ? cameraCenter.position : GetComponent<Collider2D>().bounds.center;
        Camera main = Camera.main;
        float z = main != null ? main.transform.position.z : -10f;

        GameObject go = new GameObject("BossRoomCamera");
        go.transform.position = new Vector3(center.x, center.y, z);

        CinemachineCamera cam = go.AddComponent<CinemachineCamera>();

        CinemachineBrain brain = main != null ? main.GetComponent<CinemachineBrain>() : null;
        if (brain == null) brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain != null && brain.ActiveVirtualCamera is CinemachineCamera live) cam.Lens = live.Lens;
        if (cameraSize > 0f) cam.Lens.OrthographicSize = cameraSize;

        cam.Priority = 100;
        CameraShake.Attach(cam);
        go.SetActive(false);
        return cam;
    }

    // ───────────── 플레이어 ─────────────

    private void WatchPlayer(PlayerHealth health)
    {
        UnwatchPlayer();
        playerHealth = health;
        playerHealth.OnDied += HandlePlayerDied;
    }

    private void UnwatchPlayer()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.9f, 0.25f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
