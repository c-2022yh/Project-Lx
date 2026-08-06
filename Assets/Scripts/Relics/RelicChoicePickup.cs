
using UnityEngine;

//필드에서 획득하면 유물 선택창을 여는 오브젝트
public class RelicChoicePickup : MonoBehaviour
{
    //중복 획득 방지
    private bool isPicked;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPicked) return;

        if (!other.CompareTag("Player")) return;

        isPicked = true;

        Debug.Log("유물 선택 오브젝트 획득");

        //다음 단계에서 이곳에 유물 선택 UI 열기 추가
    }
}