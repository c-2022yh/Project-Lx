#pragma warning disable CS0108 // Concrete API intentionally hides the shared compatibility view.
using UnityEngine;

namespace ProjectLx.Legacy
{

//플레이어 생성, 유지 스크립트
public partial class PlayerPersistence : global::ProjectLx.PlayerRuntime.PlayerPersistenceComponent
{
    private static PlayerPersistence instance;

    private void Awake()
    {
        // 이미 유지 중인 플레이어가 있다면
        // 새로 생긴 플레이어는 제거
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }
}
}

#pragma warning restore CS0108
