using UnityEngine;

//오브젝트를 위아래로 천천히 떠다니게 하는 연출용 스크립트 (충돌 영역이 없는 시각 오브젝트에 사용)
public class FloatingBob : MonoBehaviour
{
    [Header("Bob")]
    [Tooltip("위아래로 움직이는 거리 (월드 단위)")]
    [SerializeField] private float amplitude = 0.08f;

    [Tooltip("한 번 오르내리는 데 걸리는 시간 (초)")]
    [Min(0.1f)]
    [SerializeField] private float period = 3f;

    [Header("Breathing (Optional)")]
    [Tooltip("크기가 커졌다 작아지는 정도. 0이면 크기는 바뀌지 않습니다.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float scaleAmount = 0.02f;

    [Tooltip("여러 개를 놓았을 때 움직임이 겹치지 않도록 시작 위치를 무작위로 정합니다.")]
    [SerializeField] private bool randomPhase = true;

    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;
    private float phase;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalScale = transform.localScale;
        phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * Mathf.PI * 2f / period + phase);

        transform.localPosition = startLocalPosition + Vector3.up * (wave * amplitude);

        //위로 올라갈 때 살짝 커지고, 내려갈 때 살짝 작아짐
        float scale = 1f + wave * scaleAmount;
        transform.localScale = new Vector3(
            startLocalScale.x * scale,
            startLocalScale.y * scale,
            startLocalScale.z);
    }
}
