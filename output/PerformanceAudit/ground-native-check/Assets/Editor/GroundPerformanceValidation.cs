using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class GroundPerformanceValidation
{
    [Serializable] public class Measurement
    {
        public bool merged;
        public int tileCount, tileShapes, compositeShapes;
        public float restingY;
        public bool solidInterior, standingContact;
        public double simulationMilliseconds;
    }
    [Serializable] public class Result
    {
        public Measurement before, after;
        public bool passed;
        public string limitation = "Synthetic full-cell tile geometry. Not an F_02 frame-rate or rendering measurement.";
    }

    public static void Run()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var result = new Result { before = Measure(false), after = Measure(true) };
            result.passed = result.before.tileShapes > 10000
                && result.after.compositeShapes > 0
                && result.after.compositeShapes < result.before.tileShapes / 10
                && result.before.solidInterior && result.after.solidInterior
                && result.before.standingContact && result.after.standingContact
                && Mathf.Abs(result.before.restingY - result.after.restingY) < 0.03f;
            File.WriteAllText(Path.Combine(root, "results.json"), JsonUtility.ToJson(result, true));
            UnityEngine.Debug.Log("GROUND_NATIVE_VALIDATION passed=" + result.passed);
            EditorApplication.Exit(result.passed ? 0 : 1);
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(root, "failure.txt"), e.ToString());
            UnityEngine.Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static Measurement Measure(bool merge)
    {
        var grid = new GameObject("Ground validation", typeof(Grid));
        var go = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid.transform, false);
        var map = go.GetComponent<Tilemap>();
        go.GetComponent<TilemapRenderer>().mode = merge ? TilemapRenderer.Mode.Chunk : TilemapRenderer.Mode.Individual;
        var groundBody = go.AddComponent<Rigidbody2D>();
        groundBody.bodyType = RigidbodyType2D.Static;
        var composite = go.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.vertexDistance = 0.0005f;
        var tileCollider = go.AddComponent<TilemapCollider2D>();
        tileCollider.compositeOperation = merge ? Collider2D.CompositeOperation.Merge : Collider2D.CompositeOperation.None;
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.colliderType = Tile.ColliderType.Grid;
        var texture = new Texture2D(2, 2);
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 2f);
        tile.sprite = sprite;
        const int count = 11183, width = 180;
        var positions = new Vector3Int[count];
        var tiles = new TileBase[count];
        for (int i = 0; i < count; i++)
        {
            positions[i] = new Vector3Int(i % width, i / width, 0);
            tiles[i] = tile;
        }
        map.SetTiles(positions, tiles);
        tileCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        var player = new GameObject("Body", typeof(Rigidbody2D), typeof(CapsuleCollider2D));
        player.transform.position = new Vector3(10.5f, 75f, 0);
        var body = player.GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        var collider = player.GetComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.7f, 1f);
        Physics2D.SyncTransforms();
        for (int i = 0; i < 500; i++) Physics2D.Simulate(0.02f);
        var result = new Measurement
        {
            merged = merge, tileCount = count,
            tileShapes = tileCollider.shapeCount,
            compositeShapes = composite.shapeCount,
            restingY = body.position.y,
            solidInterior = (merge ? (Collider2D)composite : tileCollider).OverlapPoint(new Vector2(10.5f, 30.5f)),
            standingContact = collider.IsTouching(merge ? (Collider2D)composite : tileCollider)
        };
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 1500; i++)
        {
            body.linearVelocity = new Vector2((i / 200) % 2 == 0 ? 2f : -2f, body.linearVelocity.y);
            Physics2D.Simulate(0.02f);
        }
        watch.Stop();
        result.simulationMilliseconds = watch.Elapsed.TotalMilliseconds;
        UnityEngine.Object.DestroyImmediate(player);
        UnityEngine.Object.DestroyImmediate(grid);
        UnityEngine.Object.DestroyImmediate(tile);
        UnityEngine.Object.DestroyImmediate(sprite);
        UnityEngine.Object.DestroyImmediate(texture);
        Physics2D.SyncTransforms();
        return result;
    }
}
