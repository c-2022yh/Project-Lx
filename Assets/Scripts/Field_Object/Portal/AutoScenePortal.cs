using UnityEngine;

//플레이어가 닿으면 자동으로 발동하는 맵 이동 포탈
public class AutoScenePortal : MonoBehaviour
{
    private bool isTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        isTriggered = true;

        Debug.Log("자동 포탈 진입 성공");

        //나중에 여기서 씬 이동 처리
    }
}