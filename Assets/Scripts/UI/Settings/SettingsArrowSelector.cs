using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시안의  &lt;  ON  &gt;  같은 좌우 화살표 선택 위젯.
/// ON/OFF, 프레임 제한처럼 선택지가 적을 때 쓴다.
/// </summary>
public class SettingsArrowSelector : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private UnityEngine.UI.Button leftButton;
    [SerializeField] private UnityEngine.UI.Button rightButton;

    [Header("동작")]
    [Tooltip("켜면 끝에서 반대쪽 끝으로 넘어간다. ON/OFF처럼 선택지가 2개일 때 편하다.")]
    [SerializeField] private bool wrapAround = true;

    private string[] options = Array.Empty<string>();

    public int Index { get; private set; }

    /// <summary>사용자가 값을 바꿨을 때만 발행. SetIndex(notify:false)로는 발행되지 않는다.</summary>
    public event Action<int> OnIndexChanged;

    private void Awake()
    {
        if (leftButton != null) leftButton.onClick.AddListener(() => Step(-1));
        if (rightButton != null) rightButton.onClick.AddListener(() => Step(1));
    }

    public void Setup(string[] newOptions, int index)
    {
        options = newOptions ?? Array.Empty<string>();
        SetIndex(index, false);
    }

    /// <summary>ON/OFF 전용 편의 함수.</summary>
    public void SetupToggle(bool value)
    {
        Setup(new[] { "OFF", "ON" }, value ? 1 : 0);
    }

    public bool AsBool => Index == 1;

    public void SetIndex(int index, bool notify = true)
    {
        if (options.Length == 0)
        {
            Index = 0;
            if (valueText != null) valueText.text = "-";
            return;
        }

        Index = Mathf.Clamp(index, 0, options.Length - 1);

        if (valueText != null) valueText.text = options[Index];

        if (notify) OnIndexChanged?.Invoke(Index);
    }

    private void Step(int delta)
    {
        if (options.Length == 0) return;

        int next = Index + delta;

        if (wrapAround)
        {
            next = (next % options.Length + options.Length) % options.Length;
        }
        else
        {
            next = Mathf.Clamp(next, 0, options.Length - 1);
            if (next == Index) return;
        }

        SetIndex(next);
    }
}
