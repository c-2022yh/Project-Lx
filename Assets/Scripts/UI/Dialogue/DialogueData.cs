using System;
using UnityEngine;

/// <summary>
/// 대화 한 묶음. Project 창 → 우클릭 → Create → 여울잔 → 대화 로 만든다.
///   - Box: 화면 아래 대화창 (이름표 + 초상화 + 대사)
///   - Narration: 대화창 없이 화면 가운데에 글이 한 줄씩 떠오른다 (게임 시작 같은 이야기 부분)
/// 띄우기: DialogueBox.Play(대화)
/// </summary>
[CreateAssetMenu(menuName = "여울잔/대화", fileName = "Dialogue_")]
public class DialogueData : ScriptableObject
{
    public enum Style
    {
        [InspectorName("대화창")] Box,
        [InspectorName("내레이션 (가운데 글)")] Narration
    }

    public enum Tint
    {
        [InspectorName("기본 (흰 자개)")] Default,
        [InspectorName("검 (연주홍)")] Sword,
        [InspectorName("보주 (연보라)")] Orb,
        [InspectorName("몸 (청자)")] Body,
        [InspectorName("보스 (연주홍)")] Boss
    }

    [Serializable]
    public class Line
    {
        [Tooltip("이름표에 나오는 이름. 비우면 이름표를 숨긴다.")]
        public string speaker;
        [Tooltip("왼쪽 동그란 초상화. 비우면 초상화 없이 대사가 왼쪽으로 붙는다.")]
        public Sprite portrait;
        [TextArea(2, 5)]
        public string text;
    }

    public Style style = Style.Box;
    [Tooltip("대화창 테두리·이름표 색")]
    public Tint tint = Tint.Default;
    public Line[] lines = new Line[0];

    public bool IsEmpty => lines == null || lines.Length == 0;
}
