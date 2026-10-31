using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 오브젝트에 붙여서 "○○을 획득했습니다" 알림창을 띄운다. 코드 없이 인스펙터만 채우면 된다.
///
/// 언제 띄울지(When)
///   닿으면        : 플레이어가 이 오브젝트(콜라이더)에 닿는 순간
///   사라지면      : 이 오브젝트가 꺼지거나 지워지는 순간 (주워서 없어지는 아이템).
///                   씬이 바뀌거나 게임을 끌 때, 플레이어가 멀리 있을 때는 띄우지 않는다.
///   직접 부를 때만 : 다른 컴포넌트의 이벤트(UnityEvent)에 Show()를 연결해서 쓴다.
///
/// 한 번 띄운 알림은 다시 뜨지 않는다(끄면 매번 뜬다). 기록은 RunState에 남아 세이브와 같이 간다.
/// 유물은 이걸 붙이지 않아도 자동으로 뜬다.
/// </summary>
[AddComponentMenu("UI/획득 알림 (AcquireTrigger)")]
public class AcquireTrigger : MonoBehaviour
{
    public enum When
    {
        [InspectorName("닿으면")] OnTouch,
        [InspectorName("사라지면 (주웠을 때)")] OnDisappear,
        [InspectorName("직접 부를 때만")] Manual
    }

    [Tooltip("알림창 왼쪽 아이콘. 비우면 아이콘 칸 없이 뜬다.")]
    [SerializeField] private Sprite icon;

    [Tooltip("\"○○을 획득했습니다\"의 ○○. 을/를은 알아서 붙는다.")]
    [SerializeField] private string itemName;

    [Tooltip("아래 작은 글씨. 비워도 된다.")]
    [SerializeField, TextArea(2, 4)] private string description;

    [Tooltip("언제 띄울지")]
    [SerializeField] private When when = When.OnTouch;

    [Tooltip("켜 두면 한 번 본 알림은 다시 안 뜬다.")]
    [SerializeField] private bool onlyOnce = true;

    [Tooltip("'사라지면'일 때: 플레이어가 이 거리(유닛) 안에 있을 때만 띄운다.\n" +
             "멀리서 다른 이유로 꺼진 건 주운 게 아니므로 무시한다.")]
    [SerializeField, Min(0.5f)] private float pickupRadius = 3f;

    private static bool quitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    private static void OnQuitting() => quitting = true;

    /// <summary>알림을 띄운다. 이벤트에 연결해서 써도 된다.</summary>
    public void Show()
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning($"[AcquireTrigger] {name}: 이름(Item Name)이 비어 있어 알림을 띄우지 않았어요.", this);
            return;
        }

        if (onlyOnce)
        {
            string flag = "acquire:" + Id;
            if (RunState.Current.HasFlag(flag)) return;
            RunState.Current.SetFlag(flag);
        }

        AcquirePopup.Show(icon, itemName, description);
    }

    /// <summary>한 번만 띄우기용 이름. 씬 이름 + 오브젝트 이름 + 아이템 이름.</summary>
    private string Id => $"{gameObject.scene.name}/{name}/{itemName}";

    private void OnTriggerEnter2D(Collider2D other) => TryTouch(other);

    private void OnCollisionEnter2D(Collision2D collision) => TryTouch(collision.collider);

    private void TryTouch(Collider2D other)
    {
        if (when != When.OnTouch) return;
        if (other == null || other.GetComponentInParent<Player>() == null) return;

        Show();
    }

    private void OnDisable()
    {
        if (when != When.OnDisappear) return;
        if (quitting || !Application.isPlaying) return;

        // 씬을 내리는 중이면 모든 오브젝트가 꺼진다. 주운 게 아니다.
        if (!gameObject.scene.isLoaded) return;

        Player player = FindAnyObjectByType<Player>();
        if (player == null) return;

        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance > pickupRadius) return;

        Show();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (when == When.OnTouch && GetComponent<Collider2D>() == null)
            Debug.LogWarning($"[AcquireTrigger] {name}: '닿으면'은 Collider2D가 있어야 해요.", this);
    }

    // 씬 뷰에서 무슨 알림인지 보이게.
    private void OnDrawGizmos()
    {
        Handles.color = new Color(0.56f, 0.82f, 0.90f, 1f);
        Handles.Label(transform.position + Vector3.up * 1.2f,
                      string.IsNullOrEmpty(itemName) ? "획득 알림 (이름 없음)" : "획득: " + itemName);

        if (when == When.OnDisappear)
        {
            Handles.color = new Color(0.56f, 0.82f, 0.90f, 0.35f);
            Handles.DrawWireDisc(transform.position, Vector3.forward, pickupRadius);
        }
    }
#endif
}
