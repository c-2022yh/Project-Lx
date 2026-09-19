using System;
using System.Collections.Generic;

/// <summary>
/// 한 판(run) 동안의 진행도. 씬을 넘어가도 유지된다.
///
/// 지금은 메모리에만 있어서 게임을 끄면 사라진다.
/// 세이브 시스템이 생기면 이 인터페이스를 구현한 파일 기반 클래스를 만들어
/// RunState.Current에 끼워넣으면 된다. UI와 게임 로직은 손댈 필요 없다.
///
/// 유물과 방은 전부 문자열 id로 저장한다 (RelicData.RelicId / 씬 이름).
/// 그래야 나중에 그대로 직렬화된다.
/// </summary>
public interface IRunState
{
    // ── 유물 ───────────────────────────────
    IReadOnlyCollection<string> OwnedRelicIds { get; }

    /// <summary>장착 순서가 곧 표시 순서다.</summary>
    IReadOnlyList<string> EquippedRelicIds { get; }

    bool IsOwned(string relicId);
    bool IsEquipped(string relicId);

    void AddOwnedRelic(string relicId);
    void SetEquipped(IEnumerable<string> relicIds);

    // ── 맵 ────────────────────────────────
    IReadOnlyCollection<string> VisitedRoomIds { get; }

    bool HasVisited(string roomId);
    void MarkVisited(string roomId);

    // ── 공통 ───────────────────────────────
    /// <summary>무엇이든 바뀌면 발행. UI가 여기 구독해서 갱신하면 된다.</summary>
    event Action OnChanged;

    void Clear();
}
