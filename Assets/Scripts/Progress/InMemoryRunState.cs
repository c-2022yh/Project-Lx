using System;
using System.Collections.Generic;

/// <summary>
/// 메모리에만 들고 있는 진행도. 세이브 시스템이 생기기 전까지의 기본 구현.
/// 씬 전환에는 견디고, 게임을 끄면 사라진다.
/// </summary>
public class InMemoryRunState : IRunState
{
    private readonly HashSet<string> owned = new();
    private readonly List<string> equipped = new();
    private readonly HashSet<string> visited = new();

    public IReadOnlyCollection<string> OwnedRelicIds => owned;
    public IReadOnlyList<string> EquippedRelicIds => equipped;
    public IReadOnlyCollection<string> VisitedRoomIds => visited;

    public event Action OnChanged;

    public bool IsOwned(string relicId) => !string.IsNullOrEmpty(relicId) && owned.Contains(relicId);

    public bool IsEquipped(string relicId) => !string.IsNullOrEmpty(relicId) && equipped.Contains(relicId);

    public void AddOwnedRelic(string relicId)
    {
        if (string.IsNullOrEmpty(relicId)) return;
        if (!owned.Add(relicId)) return;

        OnChanged?.Invoke();
    }

    public void SetEquipped(IEnumerable<string> relicIds)
    {
        equipped.Clear();

        if (relicIds != null)
        {
            foreach (string id in relicIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (equipped.Contains(id)) continue;

                equipped.Add(id);

                // 장착했다는 건 가지고 있다는 뜻이다.
                owned.Add(id);
            }
        }

        OnChanged?.Invoke();
    }

    public bool HasVisited(string roomId) => !string.IsNullOrEmpty(roomId) && visited.Contains(roomId);

    public void MarkVisited(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return;
        if (!visited.Add(roomId)) return;

        OnChanged?.Invoke();
    }

    public void Clear()
    {
        owned.Clear();
        equipped.Clear();
        visited.Clear();

        OnChanged?.Invoke();
    }
}
