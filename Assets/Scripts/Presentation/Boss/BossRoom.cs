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

    [Header("Presentation (달빛)")]
    [Tooltip("등장 때 방이 어두워지고 돌가루·달빛, 2페이즈 충격파·붉은 빛, 격파 빛살·보주 등장을 켠다.")]
    [SerializeField] private bool moonPresentation = true;
    [Tooltip("격파 뒤 처음 모습을 드러내는 보주 그림. 비우면 코드로 그린 빛 구슬을 쓴다.")]
    [SerializeField] private Sprite revealOrbSprite;
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
            doors[i].gameObject.SetActive(false);
        }

        if (boss != null) bossStartPosition = boss.transform.position;

        if (FieldObjectState.IsDone(this))
        {
            state = State.Cleared;
            if (boss != null) boss.gameObject.SetActive(false);
            return;
        }

        if (hideBossUntilIntro && boss != null) boss.gameObject.SetActive(false);
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
        if (useRoomCamera) SetRoomCamera(true);

        // 방이 어두워지고 천장에서 돌가루가 떨어진다
        if (moonPresentation)
        {
            BossScreenFx.Darken(0.55f, 0.5f);
            BossScreenFx.Letterbox(true, 0.6f);
            BossFx.CeilingDust(RoomBounds(), 2.2f);
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

        // 위에서 달빛이 내려와 보스 자리를 비춘다
        if (moonPresentation)
        {
            if (beam == null) beam = CreateBeam();
            if (beam != null) beam.SetMood(BossMoonBeam.Mood.Cool, 1.4f);
            BossScreenFx.Darken(0.2f, 1.2f);
        }

        yield return new WaitForSecondsRealtime(card * 0.5f);

        if (!boss.gameObject.activeSelf)
        {
            boss.transform.position = bossStartPosition;
            boss.gameObject.SetActive(true);
        }

        golem = boss.GetComponent<GolemBossController>();
        if (golem != null)
        {
            golem.onPhaseTwo.RemoveListener(HandlePhaseTwo);
            golem.onPhaseTwo.AddListener(HandlePhaseTwo);
        }

        BossHealthBarUI.Show(boss, bossName, golem != null ? phaseMark : -1f);
        state = State.Fighting;

        if (moonPresentation)
        {
            BossScreenFx.Darken(0f, 0.8f);
            BossScreenFx.Letterbox(false, 0.5f);
        }

        // 2페이즈에 골렘이 둘로 갈라지면 본체는 숨고 체력이 남은 채로 있다.
        // 조각이 모두 쓰러질 때(IsDefeated)까지 기다린다. (조각 HP바 두 줄은 BossHealthBarUI가 맡음)
        hasFragmentCenter = false;
        while (boss != null && !boss.IsDead && (golem == null || !golem.IsDefeated))
        {
            if (golem != null && golem.HasSplit && TryFragmentCenter(out Vector3 c))
            {
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
        BossScreenFx.Flash(new Color(1f, 0.75f, 0.5f, 0.55f), 0.9f);
        BossScreenFx.Tint(true, 1.2f);
        if (beam != null) beam.SetMood(BossMoonBeam.Mood.Warm, 1f);
        BossHealthBarUI.SetPhaseTwo(true);
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

        // 보스가 사라지기 전에 자리를 기억해 둔다
        // 갈라졌으면 마지막 조각이 쓰러진 자리에서 격파 연출을 한다
        Vector3 center = hasFragmentCenter ? lastFragmentCenter : BossCenter();

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

        BossHealthBarUI.Hide();

        // 갈라진 틈에서 빛이 터지고 빛 조각이 흩어진다. 방은 다시 차가운 달빛으로.
        if (moonPresentation)
        {
            BossFx.DefeatBurst(center);
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
        routine = null;
    }

    // ───────────── 연출 도우미 ─────────────

    private Bounds RoomBounds() => GetComponent<Collider2D>().bounds;

    private BossMoonBeam CreateBeam()
    {
        Bounds room = RoomBounds();
        float x = boss != null ? bossStartPosition.x : room.center.x;
        float top = room.max.y + 1f;
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

        if (close) CameraShake.Shake(CameraShake.Medium);
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
