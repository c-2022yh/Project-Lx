using UnityEngine;

public class ScreenFXController : MonoBehaviour
{
    [SerializeField] private GameObject awakeningEdgeFX;

    private PlayerAwakening playerAwakening;

    private void Start()
    {
        if (awakeningEdgeFX != null)
            awakeningEdgeFX.SetActive(false);

        playerAwakening = FindAnyObjectByType<PlayerAwakening>();

        if (playerAwakening == null)
        {
            Debug.LogWarning("ScreenFXController: PlayerAwakening을 찾을 수 없습니다.", this);
            return;
        }

        playerAwakening.OnAwakeningStarted += ShowAwakening;
        playerAwakening.OnAwakeningEnded += HideAwakening;

        // 혹시 시작 시점부터 이미 각성 상태라면 동기화
        if (awakeningEdgeFX != null)
            awakeningEdgeFX.SetActive(playerAwakening.IsAwakened);
    }

    private void OnDestroy()
    {
        if (playerAwakening == null) return;

        playerAwakening.OnAwakeningStarted -= ShowAwakening;
        playerAwakening.OnAwakeningEnded -= HideAwakening;
    }

    private void ShowAwakening()
    {
        if (awakeningEdgeFX != null)
            awakeningEdgeFX.SetActive(true);
    }

    private void HideAwakening()
    {
        if (awakeningEdgeFX != null)
            awakeningEdgeFX.SetActive(false);
    }
}