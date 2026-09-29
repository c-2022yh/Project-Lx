#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerRelicManager))]
public class PlayerRelicManagerEditor : Editor
{
    private RelicData testRelic;
    private bool showOwnedRelics = true;
    private string resultMessage;
    private MessageType resultType = MessageType.Info;

    //시작 설정과 현재 보유·장착 상태, 테스트 조작 버튼을 표시
    public override void OnInspectorGUI()
    {
        PlayerRelicManager manager = target as PlayerRelicManager;
        if (manager == null) return;

        bool isRuntime = Application.IsPlaying(manager.gameObject);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            DrawDefaultInspector();
        }

        EditorGUILayout.Space(10f);

        if (!isRuntime)
        {
            EditorGUILayout.HelpBox(
                "Starting Owned Relics: 시작 시 인벤토리\n" +
                "Starting Relics: 시작 시 장착\n",
                MessageType.Info);
            return;
        }

        DrawEquippedRelics(manager);
        EditorGUILayout.Space(10f);
        DrawOwnedRelics(manager);
        EditorGUILayout.Space(10f);
        DrawTestControls(manager);

        if (!string.IsNullOrEmpty(resultMessage))
        {
            EditorGUILayout.HelpBox(resultMessage, resultType);
        }

        EditorGUILayout.HelpBox(
            "보유 목록 : 장착 중인 유물도 포함\n" +
            "테스트 버튼은 실제 획득·장착·해제 함수를 호출, 플레이 중 변경은 종료시 사라짐",
            MessageType.Info);
    }

    //장착한 검·보주와 신체 유물 목록, 신체 코스트와 개수 표시
    private void DrawEquippedRelics(PlayerRelicManager manager)
    {
        EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
        DrawEquippedSlot(manager, "Sword Relic", manager.EquippedSwordRelic);
        DrawEquippedSlot(manager, "Orb Relic", manager.EquippedOrbRelic);

        EditorGUILayout.LabelField(
            "Body Relic Cost",
            $"{manager.CurrentBodyCost} / {manager.MaxBodyCost}");
        EditorGUILayout.LabelField(
            "Body Relic Count",
            $"{manager.CurrentBodyCount} / {manager.MaxBodyCount}");

        List<RelicData> snapshot = new(manager.EquippedRelics);
        int bodyIndex = 0;

        foreach (RelicData relic in snapshot)
        {
            if (relic == null || relic.Category != RelicCategory.Body) continue;
            bodyIndex++;
            DrawEquippedSlot(manager, $"Body {bodyIndex} ({relic.Cost}Cost)", relic);
        }

        if (bodyIndex == 0) EditorGUILayout.LabelField("Body Relic", "None");
    }

    //장착 슬롯의 데이터와 실제 해제 함수를 호출하는 버튼 표시
    private void DrawEquippedSlot(PlayerRelicManager manager, string label, RelicData relic)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(label, relic, typeof(RelicData), false);
            }

            using (new EditorGUI.DisabledScope(relic == null))
            {
                if (GUILayout.Button("해제", GUILayout.Width(48f)))
                {
                    bool success = manager.UnequipRelic(relic);
                    SetResult(success, "유물 해제 완료, 보유 목록에는 남아있음",
                        "유물 해제불가,  현재 상태/콘솔 확인");
                }
            }
        }
    }

    //실제 보유 목록과 유물별 보유·장착 표시, 장착 조건에 따른 버튼 표시
    private void DrawOwnedRelics(PlayerRelicManager manager)
    {
        showOwnedRelics = EditorGUILayout.Foldout(
            showOwnedRelics, $"보유 유물 : ({manager.OwnedRelics.Count})", true);
        if (!showOwnedRelics) return;

        List<RelicData> snapshot = new(manager.OwnedRelics);
        if (snapshot.Count == 0)
        {
            EditorGUILayout.LabelField("획득 유물 없음.");
            return;
        }

        foreach (RelicData relic in snapshot)
        {
            if (relic == null) continue;
            bool equipped = manager.IsRelicEquipped(relic);
            bool canEquip = manager.CanEquipRelic(relic, out string reason);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string relicName = string.IsNullOrEmpty(relic.RelicName) ? relic.name : relic.RelicName;
                EditorGUILayout.LabelField(relicName, equipped ? "장착 중" : "보유 중");

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(relic, typeof(RelicData), false);
                    }

                    if (equipped)
                    {
                        if (GUILayout.Button("해제", GUILayout.Width(48f)))
                        {
                            bool success = manager.UnequipRelic(relic);
                            SetResult(success, "유물 해제 완료, 보유 목록에는 남아있음", 
                                "유물 해제불가,  현재 상태/콘솔 확인");
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(!canEquip))
                        {
                            if (GUILayout.Button(new GUIContent("장착", reason), GUILayout.Width(48f)))
                            {
                                EquipRelic(manager, relic);
                            }
                        }
                    }
                }

                if (!equipped && !canEquip && !string.IsNullOrEmpty(reason))
                {
                    EditorGUILayout.HelpBox(reason, MessageType.None);
                }
            }
        }
    }

    //유물 데이터를 선택해 획득하거나 보유 유물을 장착하는 테스트 버튼 표시
    private void DrawTestControls(PlayerRelicManager manager)
    {
        EditorGUILayout.LabelField("유물 테스트", EditorStyles.boldLabel);
        testRelic = (RelicData)EditorGUILayout.ObjectField(
            "테스트할 유물", testRelic, typeof(RelicData), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(testRelic == null || manager.HasRelic(testRelic)))
            {
                if (GUILayout.Button("획득 (보유만)"))
                {
                    bool success = manager.AcquireRelic(testRelic);
                    SetResult(success, "유물 획득 완료, 자동 장착X",
                        "유물 획득 실패, 보유 상태/콘솔 확인");
                }
            }

            bool canEquip = manager.CanEquipRelic(testRelic, out string reason);
            using (new EditorGUI.DisabledScope(!canEquip))
            {
                if (GUILayout.Button(new GUIContent("장착", reason))) EquipRelic(manager, testRelic);
            }
        }

        if (testRelic != null)
        {
            string state = manager.IsRelicEquipped(testRelic) ? "장착 중" :
                manager.HasRelic(testRelic) ? "보유 중" : "미보유";
            EditorGUILayout.LabelField("선택 유물 상태", state);
        }
    }

    //기존 장착 함수를 호출하고 교체·코스트 검사 결과를 표시
    private void EquipRelic(PlayerRelicManager manager, RelicData relic)
    {
        bool success = manager.EquipRelic(relic);
        SetResult(success, "유물 장착 완료",
            "유물 장착 실패, 스킬 설정/콘솔 확인");
    }

    //마지막 테스트 동작의 성공 또는 실패 메시지를 설정
    private void SetResult(bool success, string successMessage, string failureMessage)
    {
        resultMessage = success ? successMessage : failureMessage;
        resultType = success ? MessageType.Info : MessageType.Warning;
        Repaint();
    }

    //플레이 중 게임에서 획득하거나 장착한 변화도 인스펙터에 계속 반영
    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }
}
#endif
