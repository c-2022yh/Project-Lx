using UnityEngine;

//화면 효과를 적용할 실제 게임 카메라를 구분하는 스크립트
//Cinemachine 가상 카메라와 UI 전용 카메라에는 추가하지 않음
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class ScreenEffectsCamera : MonoBehaviour { }
