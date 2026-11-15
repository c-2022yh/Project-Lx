using UnityEditor;
using UnityEngine;

/// <summary>Confirmation for editor commands that replace authored content.</summary>
internal static class ProjectLxEditorSafety
{
    internal static bool ConfirmOverwrite(string operation, string targets)
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning($"[Project Lx] {operation}: interactive Edit Mode confirmation is required.");
            return false;
        }

        return EditorUtility.DisplayDialog(
            "프로젝트 제작 도구 실행 확인",
            operation + "\n\n대상:\n" + targets +
            "\n\n기존 배치나 직접 조정한 내용이 교체될 수 있습니다. 실행할까요?",
            "실행", "취소");
    }
}
