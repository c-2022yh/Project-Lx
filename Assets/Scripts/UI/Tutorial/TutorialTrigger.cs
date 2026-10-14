using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 플레이어가 이 영역에 들어오면 튜토리얼 안내를 띄운다.
///
/// 맵 담당이 씬에 놓고 크기만 맞추면 된다 (BoxCollider2D 크기 = 영역).
/// 어떤 안내를 띄울지는 Preset 드롭다운에서 고른다. 문구와 키는 자동으로 들어간다.
/// 목록에 없는 안내가 필요하면 Custom을 고르고 아래 칸을 채운다.
///
/// 같은 안내는 한 번만 뜬다. 같은 Preset 트리거를 여러 개 놓아도 처음 것만 뜬다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("띄울 안내. Custom이면 아래 칸을 직접 채운다.")]
    [SerializeField] private TutorialPreset preset = TutorialPreset.Jump;

    [Header("Custom일 때만")]
    [Tooltip("한 번만 띄우기 위한 이름. 비우면 씬 이름 + 오브젝트 이름을 쓴다.")]
    [SerializeField] private string customId;

    [Tooltip("화면에 보일 키. 비우면 아래 입력 액션의 키를 쓴다.")]
    [SerializeField] private string customKey;

    [SerializeField] private string customMessage;

    [Tooltip("이 입력을 쓰면 닫힌다 (예: Jump, Dash, Attack). 비우면 아래 시간 뒤에 닫힌다.")]
    [SerializeField] private string customAction;

    [SerializeField, Min(0.5f)] private float customDuration = 4f;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        // 누가 실수로 꺼도 벽이 되지 않게.
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null) return;

        TutorialDirector.Request(BuildStep());
    }

    private TutorialStep BuildStep()
    {
        if (preset != TutorialPreset.Custom) return TutorialSteps.Create(preset);

        if (string.IsNullOrEmpty(customMessage)) return null;

        bool hasAction = !string.IsNullOrEmpty(customAction);
        string id = string.IsNullOrEmpty(customId) ? $"{gameObject.scene.name}/{name}" : customId;

        return new TutorialStep
        {
            Id = "custom." + id,
            Key = customKey,
            Message = customMessage,
            CompleteActions = hasAction ? new[] { customAction } : null,
            Timeout = hasAction ? 0f : customDuration,
        };
    }

#if UNITY_EDITOR
    // 씬 뷰에서 영역과 무슨 안내인지 보이게.
    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.15f);
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(box.offset, box.size);
        Gizmos.matrix = Matrix4x4.identity;

        string label = preset == TutorialPreset.Custom ? "튜토리얼: " + customMessage : "튜토리얼: " + preset;
        Handles.Label(transform.TransformPoint(box.offset), label);
    }
#endif
}
