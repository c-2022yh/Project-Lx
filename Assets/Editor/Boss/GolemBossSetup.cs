using System.IO;
using UnityEditor;
using UnityEngine;

// Creates new assets only. Re-running never overwrites assigned art or tuning.
public static class GolemBossSetup
{
    public const string PrefabPath = "Assets/Prefabs/Bosses/Boss_Golem.prefab";
    public const string PlaceholderPath = "Assets/Prefabs/Bosses/GolemPlaceholder.asset";

    [MenuItem("Project Lx/Boss/Create Golem Prefab")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) { Selection.activeObject = existing; return; }
        Directory.CreateDirectory("Assets/Prefabs/Bosses");
        AssetDatabase.Refresh();
        Sprite placeholder = LoadPlaceholder();
        var root = new GameObject("Boss_Golem");
        root.layer = LayerMask.NameToLayer("Enemy");
        root.tag = "Enemy";
        try
        {
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = placeholder;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(2.6f, 3.6f);
            renderer.color = new Color(0.48f, 0.53f, 0.58f);
            renderer.sortingLayerName = "Enemy";
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 5f;
            body.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(2.6f, 3.6f);
            collider.excludeLayers = 1 << LayerMask.NameToLayer("Player");
            root.AddComponent<EnemyStats>();
            root.AddComponent<Enemy>();
            var health = root.AddComponent<EnemyHealth>();
            root.AddComponent<EnemyAnimation>();
            root.AddComponent<GolemBossVisual>();
            var boss = root.AddComponent<GolemBossController>();
            var hp = new SerializedObject(health);
            hp.FindProperty("maxHp").floatValue = 300f;
            hp.FindProperty("energyReward").floatValue = 100f;
            hp.FindProperty("damagePopupOffset").vector3Value = new Vector3(0f, 2.3f, 0f);
            hp.FindProperty("hitBlinkCount").intValue = 1;
            var slime = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Slime.prefab");
            if (slime != null)
            {
                var source = new SerializedObject(slime.GetComponent<EnemyHealth>());
                hp.FindProperty("damagePopupPrefab").objectReferenceValue = source.FindProperty("damagePopupPrefab").objectReferenceValue;
                hp.FindProperty("hitEffectPrefab").objectReferenceValue = source.FindProperty("hitEffectPrefab").objectReferenceValue;
                var setup = new SerializedObject(boss);
                var prefabs = setup.FindProperty("minionPrefabs");
                prefabs.arraySize = 1;
                prefabs.GetArrayElementAtIndex(0).objectReferenceValue = slime;
                setup.ApplyModifiedPropertiesWithoutUndo();
            }
            hp.ApplyModifiedPropertiesWithoutUndo();
            Selection.activeObject = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[GolemBoss] Prefab ready: " + PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static Sprite LoadPlaceholder()
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(PlaceholderPath))
            if (asset is Sprite sprite) return sprite;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.name = "Golem White Primitive";
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        AssetDatabase.CreateAsset(texture, PlaceholderPath);
        var result = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f, 0, SpriteMeshType.FullRect);
        result.name = "Golem Placeholder";
        AssetDatabase.AddObjectToAsset(result, texture);
        AssetDatabase.SaveAssets();
        return result;
    }
}
