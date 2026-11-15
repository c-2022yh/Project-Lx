#pragma warning disable CS0108 // Concrete API intentionally hides the shared compatibility view.
using UnityEngine;

//발도 납도 관련 스크립트. 현재는 미구현
public partial class PlayerWeaponVisualState : global::ProjectLx.PlayerRuntime.PlayerWeaponVisualStateComponent
{
    [SerializeField] private Animator animator;
    [SerializeField] private float drawnKeepTime = 1.2f;

    private float lastCombatTime;
    private bool isDrawn;

    public void NotifyCombatAction()
    {
        lastCombatTime = Time.time;

        if (!isDrawn)
        {
            isDrawn = true;
            animator.SetBool("IsDrawn", true);
            animator.SetTrigger("Draw");
        }
    }

    private void Update()
    {
        if (!isDrawn) return;

        if (Time.time > lastCombatTime + drawnKeepTime)
        {
            isDrawn = false;
            animator.SetBool("IsDrawn", false);
            animator.SetTrigger("Sheath");
        }
    }
}
#pragma warning restore CS0108
