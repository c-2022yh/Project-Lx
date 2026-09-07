using UnityEngine;

//상호작용 가능한 포탈
public class ScenePortal : MonoBehaviour, IInteractable
{
    private Player player;

    public string InteractionText => "이동";

    public void Interact(Player player)
    {
        Debug.Log("포탈 상호작용 성공");

        // 나중에 여기서 씬 이동 처리
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player foundPlayer = other.GetComponentInParent<Player>();

        if (foundPlayer == null) return;

        player = foundPlayer;
        player.SetInteractable(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player foundPlayer = other.GetComponentInParent<Player>();

        if (foundPlayer == null || foundPlayer != player) return;

        player.ClearInteractable(this);

        player = null;
    }
}