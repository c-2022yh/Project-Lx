using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 할로우나이트식 방 지도 (10.29 지도 시안). 씬 하나 = 방 하나.
///
/// 방마다 땅굴 모양 그림(바깥 테두리만, 발판 선 없음)과 "전체 지도에서 어디에 놓이는지"를 들고 있다.
/// 지도 창(MapPanel)은 이걸 읽어서 지금 방과 가 본 방만 이어 붙여 그린다. 안 가 본 방은 안 그린다.
///
/// 손으로 채우지 않는다. Unity 메뉴 Tools/UI > 지도 방 그림 뽑기 (빌드 씬 전체)를 누르면
/// 빌드 목록의 씬을 하나씩 열어 타일맵에서 방 모양을 뽑고, 포털 연결을 따라 방 위치를 정해 여기 적는다.
/// 맵이 바뀌면 메뉴를 다시 누르면 된다.
/// </summary>
public class MapRoomAtlas : ScriptableObject
{
    public const string ResourceName = "MapRoomAtlas";

    [Serializable]
    public class Room
    {
        public string sceneName;

        [Tooltip("방 모양 그림. 흰 테두리 + 회색 속 + 테두리 바깥 옅은 빛. 색은 지도 창이 입힌다.")]
        public Sprite image;

        [Tooltip("그림 왼쪽 아래가 그 씬 월드의 어디인지")]
        public Vector2 worldMin;

        [Tooltip("그림이 덮는 월드 크기")]
        public Vector2 worldSize;

        [Tooltip("전체 지도에서 그림 왼쪽 아래가 놓이는 곳 (월드 단위)")]
        public Vector2 atlasMin;
    }

    [SerializeField] private List<Room> rooms = new List<Room>();

    public IReadOnlyList<Room> Rooms => rooms;

    private static MapRoomAtlas cached;
    private static bool looked;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cached = null;
        looked = false;
    }
#endif

    /// <summary>Resources/MapRoomAtlas. 아직 안 뽑았으면 null.</summary>
    public static MapRoomAtlas Load()
    {
        if (!looked)
        {
            looked = true;
            cached = Resources.Load<MapRoomAtlas>(ResourceName);
        }
        return cached;
    }

    public Room Find(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        foreach (Room room in rooms)
            if (room != null && room.sceneName == sceneName && room.image != null) return room;
        return null;
    }

    /// <summary>그 씬의 월드 좌표 → 전체 지도 좌표.</summary>
    public static Vector2 ToAtlas(Room room, Vector2 world) => room.atlasMin + (world - room.worldMin);

#if UNITY_EDITOR
    public void SetRooms(List<Room> newRooms)
    {
        rooms = newRooms ?? new List<Room>();
        looked = false;
    }
#endif
}
