using System.Collections;
using UnityEngine;

public class PlayerAwakening : MonoBehaviour
{
    public Rigidbody2D rb;

    [Header("Awakening Settings")]
    public float awakeningDuration = 20f;
    public float awakeningFreezeTime = 0.5f;
    
    //각성 컬러
    public Color normalColor = Color.green;
    public Color awakenedColor = Color.red;

    //속도 보정 값
    [Header("Stat Multiplier")]
    public float awakenedMoveMultiplier = 1.3f;
    public float awakenedJumpMultiplier = 1.2f;

    [Header("Effect")]
    public GameObject awakeningEffectPrefab;
 
    private bool isAwakened = false;    //각성모드 들어가고 있는 상태 ->변신
    private bool isAwakening = false;   //지금 각성 중?

    private Coroutine awakeningCoroutine;

    public bool IsAwakened => isAwakened;

    //각성 상태에 진입하는 함수
    public void TryAwaken(Player p)
    {
        if (p == null) return;
        if (p.playerEnergy == null) return;
        if (isAwakened || isAwakening) return;
        
        //기력 다 모아야 각성가능
        if (p.playerEnergy.currentEnergy < p.playerEnergy.maxEnergy) return;
        
        awakeningCoroutine = StartCoroutine(AwakeningRoutine(p));
    }

    //변신 중 코루틴
    private IEnumerator AwakeningRoutine(Player p)
    {
        isAwakening = true;

        p.playerActionState.EnterAwakening();

        //중력 제거
        p.SetPhysicsFreeze(true);

        if (awakeningEffectPrefab != null)
        {
            Instantiate(awakeningEffectPrefab, p.transform.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(awakeningFreezeTime);

        //중력 되돌리기
        p.SetPhysicsFreeze(false);

        //실제 각성 상태에 들어감
        EnterAwakened(p);

        isAwakening = false;

        //State 되돌리기
        if (p.playerActionState.isAwakening)
        {
            p.playerActionState.EnterNormal();
        }

        yield return new WaitForSeconds(awakeningDuration);

        ExitAwakened(p);

    }

    private void EnterAwakened(Player p)
    {
        isAwakened = true;

        //각성 상태에서 컬러, 이동속도, 점프력 증가
        p.spriteRenderer.color = awakenedColor;
        p.playerMove.moveSpeed *= awakenedMoveMultiplier;
        p.playerMove.jumpForce *= awakenedJumpMultiplier;
    }

    private void ExitAwakened(Player p)
    {
        isAwakened = false;

        //컬러, 이동속도, 점프력 다시 복귀
        p.spriteRenderer.color = normalColor;
        p.playerMove.moveSpeed /= awakenedMoveMultiplier;
        p.playerMove.jumpForce /= awakenedJumpMultiplier;

        //기력 게이지 초기화
        p.playerEnergy.ResetEnergy();
        
    }

}