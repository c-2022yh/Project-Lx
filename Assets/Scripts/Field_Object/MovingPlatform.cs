using System.Collections.Generic;
using UnityEngine;

//위아래로 왕복하는 발판
//Rigidbody2D(Kinematic)로 움직이고, 위에 올라탄 오브젝트가 내려갈 때 떨어지지 않도록 같이 옮긴다.
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Move")]
    [Tooltip("시작 위치에서 위로 움직이는 거리 (음수면 아래로)")]
    [SerializeField] private float distance = 4f;

    [Tooltip("한쪽 끝에서 다른 쪽 끝까지 가는 데 걸리는 시간 (초)")]
    [Min(0.1f)]
    [SerializeField] private float travelTime = 2f;

    [Tooltip("양쪽 끝에서 멈추는 시간 (초)")]
    [Min(0f)]
    [SerializeField] private float waitTime = 0.5f;

    [Tooltip("여러 발판의 움직임을 엇갈리게 할 때 사용 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float startOffset = 0f;

    [Header("Rider")]
    [Tooltip("발판 위에 올라탄 것으로 볼 접촉면 기울기 (값이 클수록 평평한 면만 인정)")]
    [Range(0f, 1f)]
    [SerializeField] private float minRiderNormalY = 0.7f;

    [Header("Audio")]
    [SerializeField] private SoundCue movementLoop = new SoundCue();
    [SerializeField] private SoundCue turnSound = new SoundCue();

    private SoundHandle movementVoice;
    private int lastAudioDirection;

    private Rigidbody2D rb;
    private Vector2 startPosition;

    private readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>();
    private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        startPosition = rb.position;
    }

    private void FixedUpdate()
    {
        Vector2 current = rb.position;
        Vector2 target = startPosition + Vector2.up * (distance * GetProgress(Time.fixedTime));
        Vector2 delta = target - current;
        int direction = Mathf.Abs(delta.y) > 0.0001f ? (delta.y > 0f ? 1 : -1) : 0;
        if (direction == 0) movementVoice.Stop();
        else
        {
            if (lastAudioDirection != 0 && lastAudioDirection != direction)
                GameAudio.Play(turnSound, transform.position, transform);
            lastAudioDirection = direction;
            if (!movementVoice.IsPlaying) movementVoice = GameAudio.PlayLoop(movementLoop, transform);
        }

        //내려갈 때만 탑승자를 같이 옮김 (올라갈 때는 물리 충돌이 밀어 올림)
        if (delta.y < 0f)
        {
            CollectRiders();

            foreach (Rigidbody2D rider in riders)
            {
                //점프 중이면 옮기지 않음
                if (rider.linearVelocity.y > 0.01f) continue;

                rider.position += new Vector2(0f, delta.y);
            }
        }

        rb.MovePosition(target);
    }

    //0 = 시작 위치, 1 = 끝 위치. 양 끝에서 waitTime만큼 멈추고 부드럽게 출발/정지
    private void Update()
    {
        if (Time.timeScale <= 0f) movementVoice.Stop();
    }

    private void OnDisable()
    {
        movementVoice.Stop();
        lastAudioDirection = 0;
    }

    private float GetProgress(float time)
    {
        float cycle = (travelTime + waitTime) * 2f;
        float t = Mathf.Repeat(time + startOffset * cycle, cycle);

        if (t < waitTime) return 0f;
        t -= waitTime;

        if (t < travelTime) return Mathf.SmoothStep(0f, 1f, t / travelTime);
        t -= travelTime;

        if (t < waitTime) return 1f;
        t -= waitTime;

        return Mathf.SmoothStep(1f, 0f, t / travelTime);
    }

    //발판 윗면을 밟고 있는 Dynamic Rigidbody2D 찾기
    private void CollectRiders()
    {
        riders.Clear();
        rb.GetContacts(contacts);

        foreach (ContactPoint2D contact in contacts)
        {
            //발판에서 조회하면 rigidbody가 상대, normal은 상대 → 발판 방향
            Rigidbody2D other = contact.rigidbody;
            if (other == null || other == rb || other.bodyType != RigidbodyType2D.Dynamic) continue;

            //발판 → 상대 방향이 위쪽이면 윗면에 올라탄 것
            if (-contact.normal.y < minRiderNormalY) continue;

            riders.Add(other);
        }
    }

#if UNITY_EDITOR
    //이동 범위를 씬 뷰에 표시
    private void OnDrawGizmosSelected()
    {
        Vector3 from = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Vector3 to = from + Vector3.up * distance;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(from, to);

        Collider2D col = GetComponent<Collider2D>();
        Vector3 size = col != null ? col.bounds.size : Vector3.one;

        Gizmos.DrawWireCube(to, size);
    }
#endif
}
