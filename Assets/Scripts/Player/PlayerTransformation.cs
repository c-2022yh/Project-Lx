using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTransformation : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    //상태 정의 (일반, 변신1, 변신2)
    public enum PlayerState { Normal, Transform1, Transform2 }
    public PlayerState currentState = PlayerState.Normal;

    private Vector3 initialScale;

    [Header("변신 비주얼 설정")]
    public Color normalColor = Color.green;

    public Color colorT1 = Color.red;
    public Color colorT2 = Color.blue;
    public Vector3 scaleT2 = new Vector3(1.4f, 0.7f, 1f); // 변신2 크기 설정

    private bool isTransformed = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 시작 시 기본 색상 적용
        spriteRenderer.color = normalColor;
        initialScale = transform.localScale;
    }

    // 변신 1 키 (예: 1)와 연결될 함수
    public void OnTransform1(InputValue value)
    {
        if (value.isPressed)
        {
            // 이미 변신1이면 일반으로, 아니면 변신1로
            SetState(currentState == PlayerState.Transform1 ? PlayerState.Normal : PlayerState.Transform1);
        }
    }

    // 변신 2 키 (예: 2)와 연결될 함수
    public void OnTransform2(InputValue value)
    {
        if (value.isPressed)
        {
            // 이미 변신2면 일반으로, 아니면 변신2로
            SetState(currentState == PlayerState.Transform2 ? PlayerState.Normal : PlayerState.Transform2);
        }
    }
    // 상태를 변경하고 그에 따른 외형/능력을 적용하는 핵심 함수
    private void SetState(PlayerState newState)
    {
        currentState = newState;

        // 모든 상태를 기본으로 리셋 후 필요한 것만 변경 (초기화 로직)
        spriteRenderer.color = Color.green;
        transform.localScale = initialScale;

        switch (currentState)
        {
            case PlayerState.Normal:
                UnityEngine.Debug.Log("일반 모드 복귀");
                break;

            case PlayerState.Transform1:
                spriteRenderer.color = colorT1;
                UnityEngine.Debug.Log("변신 1 완료!");
                break;

            case PlayerState.Transform2:
                spriteRenderer.color = colorT2;
                transform.localScale = scaleT2;
                UnityEngine.Debug.Log("변신 2 완료!");
                break;
        }
    }

}