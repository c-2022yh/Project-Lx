using UnityEngine;

//일회성 이펙트 자동 삭제
public class EffectAutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.15f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}