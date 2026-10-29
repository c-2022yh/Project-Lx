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
    [Tooltip("보스를 잡은 뒤 뜨는 대화. 비우면 안 뜬다. 다 읽어야 문이 열린다.")]
    [SerializeField] private DialogueData clearDialogue;

    private enum State { Idle, Intro, Fighting, Cleared }

    private State state = State.Idle;
    private Vector3[] doorClosedPositions;
    private Vector3 bossStartPosition;
    private CinemachineCamera roomCamera;
    private PlayerHealth playerHealth;
    private Coroutine routine;
    private bool shownCardOnce;

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

        yield return new WaitForSecondsRealtime(introDelay);

        BossMarker marker = boss.GetComponent<BossMarker>();
        string bossName = marker != null ? marker.BossName : boss.name;
        string bossTitle = marker != null ? marker.BossTitle : "";

        // 두 번째 도전부터는 카드를 짧게
        float card = shownCardOnce ? Mathf.Min(cardSeconds, 1.4f) : cardSeconds;
        shownCardOnce = true;
        BossTitleCard.Show(bossTitle, bossName, card);

        yield return new WaitForSecondsRealtime(card * 0.5f);

        if (!boss.gameObject.activeSelf)
        {
            boss.transform.position = bossStartPosition;
            boss.gameObject.SetActive(true);
        }

        BossHealthBarUI.Show(boss, bossName);
        state = State.Fighting;

        while (boss != null && !boss.IsDead) yield return null;

        yield return ClearRoutine();
    }

    private IEnumerator ClearRoutine()
    {
        state = State.Cleared;
        UnwatchPlayer();
        FieldObjectState.MarkDone(this);

        // 마지막 일격: 멈춤 → 슬로모션
        HitStop.Stop(clearHitStop);
        CameraShake.Shake(CameraShake.Heavy);
        yield return new WaitForSecondsRealtime(clearHitStop);
        HitStop.Cancel();   // 멈춤이 남아 있으면 슬로모션이 멈춤에 묻히므로 먼저 끝낸다
        HitStop.Slow(clearSlowScale, clearSlowSeconds);

        BossHealthBarUI.Hide();

        yield return new WaitForSecondsRealtime(unlockDelay);

        if (clearDialogue != null)
        {
            DialogueBox.Play(clearDialogue);
            while (DialogueBox.IsBusy) yield return null;
        }

        if (useRoomCamera) SetRoomCamera(false);
        yield return MoveDoors(false);

        // 보스 처치는 바로 저장한다(제단 위치는 그대로).
        GameSession.SaveProgressNow();
        routine = null;
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
