using UnityEngine;

public class ScreenFXController : MonoBehaviour
{
    [SerializeField] private GameObject awakeningEdgeFX;

    private PlayerAwakening playerAwakening;
    private PlayerAwakeningVisual awakeningVisual;
    private PlayerHealth health;

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

        awakeningVisual = playerAwakening.GetComponent<PlayerAwakeningVisual>();
        health = playerAwakening.GetComponent<PlayerHealth>();

        playerAwakening.OnAwakeningStarted += ShowAwakening;
        playerAwakening.OnAwakeningEnded += HideAwakening;

        // 혹시 시작 시점부터 이미 각성 상태라면 동기화
        if (awakeningEdgeFX != null)
            RefreshEdge();
    }

    private void LateUpdate() => RefreshEdge();

    private void RefreshEdge()
    {
        if (awakeningEdgeFX == null) return;
        bool show = playerAwakening != null && (health == null || !health.IsDead)
            && (awakeningVisual != null && awakeningVisual.UsesIntroSequence
                ? awakeningVisual.ShouldShowScreenEdge : playerAwakening.IsAwakened);
        if (awakeningEdgeFX.activeSelf != show) awakeningEdgeFX.SetActive(show);
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