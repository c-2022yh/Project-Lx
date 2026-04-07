using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTransformation : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;

    [Header("변신 색상 설정")]
    public Color normalColor = Color.white;
    public Color transformedColor = Color.green; // 원하는 색으로 인스펙터에서 수정 가능

    private bool _isTransformed = false;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // 시작 시 기본 색상 적용
        _spriteRenderer.color = normalColor;
    }

    // Input System에서 버튼 입력을 받았을 때 호출될 함수
    public void OnTransform(InputValue value)
    {
        if (value.isPressed)
        {
            ToggleTransformation();
        }
    }

    private void ToggleTransformation()
    {
        _isTransformed = !_isTransformed;

        // 삼항 연산자로 색상 변경
        _spriteRenderer.color = _isTransformed ? transformedColor : normalColor;

        Debug.Log(_isTransformed ? "변신 완료!" : "기본 상태 복귀");
    }
}