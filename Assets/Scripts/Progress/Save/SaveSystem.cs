using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 슬롯 파일 읽기/쓰기만 맡는다. 무엇을 저장할지는 GameSession이 정한다.
/// 위치: Application.persistentDataPath/saves/slot_N.json
/// </summary>
public static class SaveSystem
{
    public const int SlotCount = 3;

    private static string Folder => Path.Combine(Application.persistentDataPath, "saves");

    public static string PathOf(int slot) => Path.Combine(Folder, $"slot_{slot}.json");

    public static bool Exists(int slot) => IsValid(slot) && File.Exists(PathOf(slot));

    public static void Save(int slot, SaveData data)
    {
        if (!IsValid(slot) || data == null) return;
        try
        {
            Directory.CreateDirectory(Folder);
            string path = PathOf(slot);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            // 쓰다가 꺼져도 기존 파일이 깨지지 않게 임시 파일에 쓰고 바꿔 끼운다.
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] 슬롯 {slot + 1} 저장 실패: {e.Message}");
        }
    }

    public static SaveData Load(int slot)
    {
        if (!Exists(slot)) return null;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOf(slot)));
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] 슬롯 {slot + 1} 불러오기 실패: {e.Message}");
            return null;
        }
    }

    public static void Delete(int slot)
    {
        if (!Exists(slot)) return;
        File.Delete(PathOf(slot));
    }

    private static bool IsValid(int slot) => slot >= 0 && slot < SlotCount;
}
