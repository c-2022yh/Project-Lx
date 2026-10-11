using System;

/// <summary>미리 정해둔 안내. TutorialTrigger의 드롭다운에 그대로 나온다.</summary>
public enum TutorialPreset
{
    Move,
    Jump,
    Attack,
    Dash,
    Skill,
    Shrine,
    Chest,
    Awaken,
    RelicWindow,
    Map,
    Custom,
}

/// <summary>안내 하나. 무엇을 보여주고, 무엇을 하면 닫히는지.</summary>
public class TutorialStep
{
    /// <summary>한 번만 띄우기 위한 이름. RunState 플래그 "tutorial.{Id}"로 남는다.</summary>
    public string Id;

    /// <summary>화면에 보일 키. 비우면 KeyActions의 키보드 바인딩에서 만든다.</summary>
    public string Key;

    public string Message;

    /// <summary>키 이름을 만들 입력 액션. 비우면 CompleteActions를 쓴다.</summary>
    public string[] KeyActions;

    /// <summary>이 중 하나를 쓰면 닫힌다.</summary>
    public string[] CompleteActions;

    /// <summary>입력 말고 다른 완료 조건 (창이 열렸는지 등). 매 프레임 본다.</summary>
    public Func<bool> Done;

    /// <summary>0보다 크면 이 시간이 지나면 행동을 안 해도 닫는다.</summary>
    public float Timeout;

    /// <summary>
    /// 그 자리에서만 의미 있는 안내(제단·상자)용. false가 되면 내리고 다른 안내에게 순서를 넘긴다.
    /// 다시 true가 되면 이어서 띄운다. 비우면 항상 보인다.
    /// </summary>
    public Func<bool> Visible;
}

/// <summary>
/// 미리 정해둔 안내 목록. 문구와 닫히는 조건을 한곳에 모아둔다.
/// 키 이름은 여기 적지 않는다. InputSystem_Actions의 바인딩에서 읽어오므로
/// 키가 바뀌어도 안내가 같이 따라간다.
/// </summary>
public static class TutorialSteps
{
    public static TutorialStep Create(TutorialPreset preset)
    {
        switch (preset)
        {
            case TutorialPreset.Move:
                return Action("move", "좌우로 이동하세요", "Move");

            case TutorialPreset.Jump:
                return Action("jump", "점프하세요", "Jump");

            case TutorialPreset.Attack:
                return Action("attack", "공격하세요", "Attack");

            case TutorialPreset.Dash:
                return Action("dash", "대시로 회피하세요", "Dash");

            case TutorialPreset.Skill:
                return Action("skill", "스킬을 사용하세요", "SkillA", "SkillS", "SkillD", "SkillF");

            case TutorialPreset.Shrine:
            {
                TutorialStep step = Action("shrine", "제단을 활성화하고 유물을 바꾸세요", "Interact");
                step.Visible = () => TutorialDirector.IsNearShrine;
                return step;
            }

            case TutorialPreset.Chest:
            {
                // ↑만 눌러서는 닫지 않는다. 유물을 실제로 하나 얻어야 끝난다.
                // 기준은 안내를 요청한 순간의 획득 횟수다. 선택창이 떠 있는 동안
                // 안내가 잠깐 내려가 있어도 그 사이 얻은 것까지 센다.
                int acquiredBefore = TutorialDirector.RelicsAcquired;

                return new TutorialStep
                {
                    Id = "chest",
                    Message = "상자를 열어 유물을 고르세요",
                    KeyActions = new[] { "Interact" },
                    Done = () => TutorialDirector.RelicsAcquired > acquiredBefore,
                    Visible = () => TutorialDirector.CanOpenChest,
                };
            }

            case TutorialPreset.Awaken:
                // 키만 눌러서는 닫지 않는다. 보주가 없으면 눌러도 각성이 안 되기 때문이다.
                return new TutorialStep
                {
                    Id = "awaken",
                    Message = "기력이 가득 찼습니다. 각성하세요",
                    KeyActions = new[] { "Awaken" },
                    Done = () => TutorialDirector.HasAwakened,
                    Timeout = 10f,
                };

            case TutorialPreset.RelicWindow:
                // I 키는 UIManager가 직접 읽어서 액션 바인딩이 없다.
                return new TutorialStep
                {
                    Id = "relic_window",
                    Key = "I",
                    Message = "새 유물을 얻었습니다. 유물창을 열어보세요",
                    Done = () => UIManager.Instance != null && UIManager.Instance.IsRelicWindowOpen,
                    Timeout = 10f,
                };

            case TutorialPreset.Map:
                return new TutorialStep
                {
                    Id = "map",
                    Key = "Tab",
                    Message = "누르고 있으면 지도가 보입니다",
                    Done = () => UIManager.Instance != null && UIManager.Instance.IsMapOpen,
                    Timeout = 10f,
                };

            default:
                return null;
        }
    }

    private static TutorialStep Action(string id, string message, params string[] actions)
    {
        return new TutorialStep
        {
            Id = id,
            Message = message,
            CompleteActions = actions,
        };
    }
}
