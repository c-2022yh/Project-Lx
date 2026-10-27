using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Creates real Unity assets without editing scenes or existing prefab GUIDs.
public static class GameAudioSetup
{
    public const string CatalogPath = "Assets/Resources/Audio/GameAudioCatalog.asset";
    public const string MixerPath = "Assets/Audio/GameAudioMixer.mixer";
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    // Re-running setup preserves clips, volume tuning and existing asset identities.
    [MenuItem("Project Lx/Audio/Set Up Audio")]
    public static void SetUp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory("Assets/Resources/Audio");
        Directory.CreateDirectory("Assets/Audio/Clips/BGM");
        Directory.CreateDirectory("Assets/Audio/Clips/SFX");
        Directory.CreateDirectory("Assets/Audio/Clips/UI");
        AssetDatabase.Refresh();
        var catalog = AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<GameAudioCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        catalog.AddMissingSlots();
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null) mixer = CreateMixer();
        catalog.mixer = mixer;
        catalog.bgmGroup = EnsureGroup(mixer, "BGM");
        catalog.sfxGroup = EnsureGroup(mixer, "SFX");
        catalog.uiGroup = EnsureGroup(mixer, "UI");
        ExposeVolume(mixer, mixer.FindMatchingGroups("Master")[0], "Master");
        ExposeVolume(mixer, catalog.bgmGroup, "BGM");
        ExposeVolume(mixer, catalog.sfxGroup, "SFX");
        ExposeVolume(mixer, catalog.uiGroup, "UI");
        EditorUtility.SetDirty(catalog);
        EditorUtility.SetDirty(mixer);
        AssetDatabase.SaveAssets();
        Verify();
        Selection.activeObject = catalog;
        Debug.Log("[GameAudio] Setup complete. Select GameAudioCatalog and assign clips; empty slots remain silent.");
    }

    // Unity exposes mixer creation through editor internals; keep that dependency here only.
    private static AudioMixer CreateMixer()
    {
        Type type = FindEditorType("UnityEditor.Audio.AudioMixerController");
        return (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", Flags).Invoke(null, new object[] { MixerPath });
    }

    // Audio editor types may live in separate Unity editor modules.
    private static Type FindEditorType(string name)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Unity editor type is unavailable: " + name);
    }

    // Add a category only when the mixer does not already contain it.
    private static AudioMixerGroup EnsureGroup(AudioMixer mixer, string name)
    {
        foreach (AudioMixerGroup found in mixer.FindMatchingGroups(name))
            if (found.name == name) return found;
        Type type = mixer.GetType();
        object group = type.GetMethod("CreateNewGroup", Flags).Invoke(mixer, new object[] { name, false });
        object master = type.GetProperty("masterGroup", Flags).GetValue(mixer);
        type.GetMethod("AddChildToParent", Flags).Invoke(mixer, new[] { group, master });
        return (AudioMixerGroup)group;
    }

    // Expose each group's volume under the existing settings parameter names.
    private static void ExposeVolume(AudioMixer mixer, AudioMixerGroup group, string name)
    {
        Type type = mixer.GetType();
        object guid = group.GetType().GetMethod("GetGUIDForVolume", Flags).Invoke(group, null);
        bool contains = (bool)type.GetMethod("ContainsExposedParameter", Flags).Invoke(mixer, new[] { guid });
        if (!contains)
        {
            Type pathType = type.Assembly.GetType("UnityEditor.Audio.AudioGroupParameterPath", true);
            object path = Activator.CreateInstance(pathType, Flags, null, new[] { (object)group, guid }, null);
            type.GetMethod("AddExposedParameter", Flags).Invoke(mixer, new[] { path });
        }
        PropertyInfo property = type.GetProperty("exposedParameters", Flags);
        var parameters = (Array)property.GetValue(mixer);
        for (int i = 0; i < parameters.Length; i++)
        {
            object parameter = parameters.GetValue(i);
            Type parameterType = parameter.GetType();
            if (!parameterType.GetField("guid", Flags).GetValue(parameter).Equals(guid)) continue;
            parameterType.GetField("name", Flags).SetValue(parameter, name);
            parameters.SetValue(parameter, i);
        }
        property.SetValue(mixer, parameters);
    }

    // Validate imported script references and actual mixer routing after asset generation.
    [MenuItem("Project Lx/Audio/Verify Setup")]
    public static void Verify()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(CatalogPath);
        if (catalog == null || catalog.mixer == null || catalog.bgmGroup == null || catalog.sfxGroup == null || catalog.uiGroup == null)
            throw new InvalidOperationException("Audio catalog or mixer routing is incomplete.");
        foreach (GameSound sound in Enum.GetValues(typeof(GameSound)))
            if (catalog.Find(sound) == null) throw new InvalidOperationException("Missing sound slot: " + sound);
        foreach (string parameter in new[] { "Master", "BGM", "SFX", "UI" })
            if (!catalog.mixer.GetFloat(parameter, out _)) throw new InvalidOperationException("Missing exposed parameter: " + parameter);
        Debug.Log("[GameAudio] VERIFIED: catalog slots, 3 mixer groups and 4 volume parameters.");
    }

    // Select the single asset used by the automatic runtime service.
    [MenuItem("Project Lx/Audio/Select Audio Catalog")]
    public static void SelectCatalog()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(CatalogPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
