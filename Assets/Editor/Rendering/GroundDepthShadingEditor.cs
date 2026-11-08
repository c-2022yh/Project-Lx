using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GroundDepthShading)), CanEditMultipleObjects]
public sealed class GroundDepthShadingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("이 Tilemap의 빈 공간에서 멀어질수록 어두워집니다. 거리 단위는 타일 칸입니다.\nSurface Width: 밝게 유지할 폭 / Fade Distance: 어두워지는 폭\nMax Darkness: 최대 어둠 / Depth Tint: 내부 색 / Falloff Power: 변화 곡선", MessageType.Info);
        if (GUILayout.Button("깊이 마스크 다시 생성 (Rebuild Depth Mask)"))
            foreach (Object item in targets)
                ((GroundDepthShading)item).RebuildMask();

        var shading = (GroundDepthShading)target;
        var renderer = shading.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
        if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader.name != "ProjectLx/Ground Depth Lit")
            EditorGUILayout.HelpBox("Tilemap Renderer의 Material에 M_GroundDepthLit을 지정하세요.", MessageType.Warning);
    }
}
