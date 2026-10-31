using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 동굴 밝기 변화. 카메라가 오른쪽(동굴 출구)으로 갈수록 전체 조명(Global Light 2D)이 밝아진다.
/// 튜토리얼 맵 빌더가 Tutorial_Light 오브젝트에 붙인다. 값은 Play 중 인스펙터에서 바꿔 볼 수 있다.
/// </summary>
public class CaveLightRamp : MonoBehaviour
{
    [Header("어디서부터 어디까지 (월드 x)")]
    [SerializeField] private float darkX = 10f;
    [SerializeField] private float brightX = 130f;

    [Header("밝기")]
    [SerializeField, Range(0f, 2f)] private float darkIntensity = 0.25f;
    [SerializeField, Range(0f, 2f)] private float brightIntensity = 0.9f;

    [Header("색")]
    [SerializeField] private Color darkColor = new Color(0.62f, 0.72f, 1f);     // 동굴 안: 푸르스름
    [SerializeField] private Color brightColor = new Color(1f, 0.97f, 0.9f);    // 밖: 따뜻한 햇빛

    private Light2D global;
    private Transform cam;

    private void Start()
    {
        foreach (Light2D l in FindObjectsByType<Light2D>())
        {
            if (l.lightType == Light2D.LightType.Global) { global = l; break; }
        }
    }

    private void LateUpdate()
    {
        if (global == null) return;
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(darkX, brightX, cam.position.x));
        global.intensity = Mathf.Lerp(darkIntensity, brightIntensity, k);
        global.color = Color.Lerp(darkColor, brightColor, k);
    }
}
