using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEngine
{
    public sealed class SerializeField : Attribute { }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    public struct Vector2 { public float x, y; public static implicit operator Vector2(Vector3 v) => new Vector2 { x = v.x, y = v.y }; }
    public struct Vector3 { public float x, y, z; }
    public struct Color { public static Color gray => default; public static Color white => default; }
    public class Transform { public Vector3 position; }
    public class GameObject { public string name = "TestShrine"; }
    public class MonoBehaviour
    {
        public Transform transform = new Transform();
        public GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : class => null;
    }
    public class SpriteRenderer { public Color color; }
    public class Collider2D { public T GetComponentInParent<T>() where T : class => null; }
    public static class Debug { public static void Log(object text) { } }
}
public class SoundCue { }
public class Player : MonoBehaviour
{
    public void SetInteractable(IInteractable value) { }
    public void ClearInteractable(IInteractable value) { }
}
public class PlayerRespawn { public int updates; public void SetRespawnPoint(Vector2 p) { updates++; } }
public class PlayerHealth
{
    public float CurrentHealth = 3f;
    public void FullHeal() { if (CurrentHealth > 0f) CurrentHealth = 5f; }
}
public static class GameAudio
{
    public static readonly List<SoundCue> played = new List<SoundCue>();
    public static void Play(SoundCue cue, Vector3 position, Transform owner) { played.Add(cue); }
}
public static class GameSession
{
    public static int saves;
    public static void SaveAtShrine(Vector2 p) { saves++; }
}
internal static class Program
{
    private static int checks;
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(Shrine s, string name, object value) => typeof(Shrine).GetField(name, Fields).SetValue(s, value);
    private static object Get(Shrine s, string name) => typeof(Shrine).GetField(name, Fields).GetValue(s);
    private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    private static void Main()
    {
        typeof(Shrine).GetField("activeShrine", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        var shrine = new Shrine();
        var health = new PlayerHealth();
        var respawn = new PlayerRespawn();
        Set(shrine, "playerHealth", health);
        Set(shrine, "playerRespawn", respawn);
        shrine.Activate();
        Check(GameAudio.played.Count == 0 && GameSession.saves == 0, "Out of range is silent and does not save");
        Set(shrine, "<IsPlayerInRange>k__BackingField", true);
        Set(shrine, "playerRespawn", null);
        shrine.Activate();
        Check(GameAudio.played.Count == 0, "Missing respawn target is silent");
        Set(shrine, "playerRespawn", respawn);
        shrine.Activate();
        Check(shrine.IsActive && respawn.updates == 1, "Activation preserves checkpoint behavior");
        Check(health.CurrentHealth == 5f && GameSession.saves == 1, "Activation heals and saves");
        Check(GameAudio.played.Count == 1 && ReferenceEquals(GameAudio.played[0], Get(shrine, "activationSound")), "Activation plays only activation cue");
        shrine.Activate();
        Check(GameAudio.played.Count == 1, "Full health repeat is silent");
        Check(GameSession.saves == 2, "Full health repeat still saves");
        health.CurrentHealth = 2f;
        shrine.Activate();
        Check(GameAudio.played.Count == 2 && ReferenceEquals(GameAudio.played[1], Get(shrine, "healSound")), "Repeat healing uses healing cue only");
        Check(health.CurrentHealth == 5f && respawn.updates == 1, "Repeat healing does not replace respawn point");
        var restored = new Shrine();
        int sounds = GameAudio.played.Count, saves = GameSession.saves;
        restored.RestoreActive();
        Check(restored.IsActive && !shrine.IsActive, "Restore keeps activation visuals/state behavior");
        Check(GameAudio.played.Count == sounds && GameSession.saves == saves, "Save restoration is silent and does not resave");
        shrine.Activate();
        Check(GameAudio.played.Count == sounds + 1 && ReferenceEquals(GameAudio.played[sounds], Get(shrine, "activationSound")), "Switching checkpoint uses activation cue");
        health.CurrentHealth = 0f;
        shrine.Activate();
        Check(GameAudio.played.Count == sounds + 1, "Rejected healing of dead player has no healing cue");
        Console.WriteLine("SHRINE_AUDIO_VALIDATION checks=" + checks + " failures=0");
    }
}
