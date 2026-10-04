using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 이름 -> 그 씬의 맵 밑그림과 좌표 기준.
///
/// 맵 창(MapPanel)은 Popup_Canvas 프리팹 안에 하나뿐인데 씬은 여러 개다.
/// 그래서 이미지를 프리팹에 직접 박아두면 어느 씬에 들어가도 같은 지형이 나온다.
/// 열린 씬 이름으로 여기서 찾아 끼우면 씬마다 맞는 지도가 뜬다.
///
/// 항목은 Tools/UI > 맵 밑그림 뽑기 가 알아서 채운다. 손으로 쓸 일은 없다.
/// Resources 안에 둬야 빌드에 따라오고, 참조한 밑그림 이미지도 같이 끌려 들어간다.
/// </summary>
public class MapSketchLibrary : ScriptableObject
{
    /// <summary>Resources.Load에 넘기는 이름. 에셋 파일 이름과 반드시 같아야 한다.</summary>
    public const string ResourceName = "MapSketchLibrary";

    [System.Serializable]
    public class Entry
    {
        [Tooltip("씬 파일 이름. 확장자(.unity)는 빼고 적는다.")]
        public string sceneName;

        [Tooltip("그 씬의 지형을 뽑아 만든 밑그림. 디자인 작업물로 갈아끼워도 된다.")]
        public Sprite sketch;

        [Tooltip("밑그림 한가운데가 월드의 어느 지점인지.")]
        public Vector2 worldCenter;

        [Tooltip("원본 이미지에서 월드 1유닛이 몇 픽셀인지.")]
        public float unitsToPixels = 8f;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public static MapSketchLibrary Load()
    {
        return Resources.Load<MapSketchLibrary>(ResourceName);
    }

    /// <summary>등록된 씬이 아니면 null. 부른 쪽이 없는 경우를 감당해야 한다.</summary>
    public Entry Find(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;

        foreach (Entry entry in entries)
        {
            if (entry == null) continue;
            if (entry.sceneName == sceneName) return entry;
        }

        return null;
    }

#if UNITY_EDITOR
    /// <summary>뽑기 도구가 쓰는 등록 창구. 같은 씬을 다시 뽑으면 그 항목을 덮어쓴다.</summary>
    public Entry Upsert(string sceneName)
    {
        Entry found = Find(sceneName);
        if (found != null) return found;

        found = new Entry { sceneName = sceneName };
        entries.Add(found);
        return found;
    }
#endif
}
