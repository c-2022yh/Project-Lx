using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 포탈(ScenePortal / AutoScenePortal)이 어느 씬으로 가는지 읽어온다.
///
/// 두 포탈 모두 targetSceneName을 private 필드로만 들고 있고 꺼내는 프로퍼티가 없다.
/// 남의 파일을 고치지 않으려고 리플렉션으로 같은 이름의 필드를 읽는다.
/// 포탈 쪽에 public string TargetSceneName 같은 프로퍼티가 생기면 그걸 쓰도록 바꾸면 된다.
///
/// 필드 이름이 바뀌면 빈 문자열이 나오고, 맵에서는 "?" 없이 그냥 출구로 찍힌다.
/// </summary>
public static class PortalTarget
{
    private const string FieldName = "targetSceneName";

    private static readonly Dictionary<Type, FieldInfo> fields = new Dictionary<Type, FieldInfo>();

    public static string SceneOf(Component portal)
    {
        if (portal == null) return "";

        Type type = portal.GetType();

        if (!fields.TryGetValue(type, out FieldInfo field))
        {
            field = type.GetField(FieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            fields[type] = field;

            if (field == null)
            {
                Debug.LogWarning($"[PortalTarget] {type.Name}에서 {FieldName} 필드를 찾지 못했습니다. " +
                                 "맵에서 안 가본 곳 표시(?)가 빠집니다.");
            }
        }

        return field?.GetValue(portal) as string ?? "";
    }
}
