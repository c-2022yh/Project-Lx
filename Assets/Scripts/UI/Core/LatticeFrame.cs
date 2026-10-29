using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 창살 무늬 창 테두리 (10.19 디자이너 프레임).
/// 그림 한 장을 잘라 둔 것이라 어떤 크기의 창에도 붙일 수 있다.
///   - 네 모서리 장식은 크기 고정, 직선만 창 크기에 맞춰 늘어난다 (Frame_Lattice, 9-slice).
///   - 위·아래·양옆 가운데 장식은 따로 떼어 항상 가운데에 붙인다. 그래서 늘어나지 않는다.
/// 그림은 Resources/UIFrame에 있어서 게임 중에 코드로 만드는 창(획득창 등)에서도 쓸 수 있다.
/// </summary>
public static class LatticeFrame
{
    public const string ObjectName = "LatticeFrame";
    private const string Folder = "UIFrame/";

    /// <summary>창 바깥으로 얼마나 내밀지. 장식이 창 가장자리에 걸치게 한다.</summary>
    public const float DefaultOutset = 12f;

    /// <summary>이보다 낮은 창은 양옆 가운데 장식이 모서리와 붙어 보여서 뺀다.</summary>
    public const float MinHeightForSideOrnaments = 340f;

    public static readonly Color DefaultColor = new Color(0.86f, 0.89f, 0.95f, 0.9f);

    /// <summary>
    /// target 창에 테두리를 붙인다. 이미 붙어 있으면 지우고 새로 붙인다.
    /// 창 안의 다른 내용보다 뒤(첫 번째 자식)에 둔다.
    /// outset이 음수면 창 안쪽으로 들어온다. scale은 모서리·가운데 장식 크기 배율.
    /// </summary>
    public static GameObject Attach(RectTransform target, float height, Color? color = null,
        float outset = DefaultOutset, float scale = 1f)
    {
        if (target == null) return null;

        Transform old = target.Find(ObjectName);
        if (old != null)
        {
            if (Application.isPlaying) Object.Destroy(old.gameObject);
            else Object.DestroyImmediate(old.gameObject);
        }

        Color c = color ?? DefaultColor;

        GameObject root = new GameObject(ObjectName, typeof(RectTransform));
        root.transform.SetParent(target, false);
        root.transform.SetSiblingIndex(0);
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(-outset, -outset);
        rt.offsetMax = new Vector2(outset, outset);

        // 직선 + 모서리
        Image body = NewImage("Body", root.transform, Load("Frame_Lattice"), c);
        body.type = Image.Type.Sliced;
        body.fillCenter = false;
        body.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, scale);
        Stretch(body.rectTransform);

        // 가운데 장식
        AddOrnament(root.transform, "Frame_Lattice_Top", new Vector2(0.5f, 1f), c, scale);
        AddOrnament(root.transform, "Frame_Lattice_Bottom", new Vector2(0.5f, 0f), c, scale);

        if (height + outset * 2f >= MinHeightForSideOrnaments)
        {
            AddOrnament(root.transform, "Frame_Lattice_Left", new Vector2(0f, 0.5f), c, scale);
            AddOrnament(root.transform, "Frame_Lattice_Right", new Vector2(1f, 0.5f), c, scale);
        }

        return root;
    }

    /// <summary>붙여 둔 테두리 색만 바꾼다 (획득창이 유물 계열 색으로 바꿀 때).</summary>
    public static void SetColor(GameObject frame, Color color)
    {
        if (frame == null) return;
        foreach (Image image in frame.GetComponentsInChildren<Image>(true))
            image.color = color;
    }

    private static void AddOrnament(Transform parent, string sprite, Vector2 anchor, Color color, float scale)
    {
        Sprite s = Load(sprite);
        Image image = NewImage(sprite.Replace("Frame_Lattice_", "Ornament_"), parent, s, color);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = Vector2.zero;
        if (s != null) rt.sizeDelta = s.rect.size * scale;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite Load(string name)
    {
        Sprite s = Resources.Load<Sprite>(Folder + name);
        if (s == null) Debug.LogWarning("[LatticeFrame] 프레임 그림이 없습니다: Resources/" + Folder + name);
        return s;
    }
}
