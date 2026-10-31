# 제단 효과음

Play Mode를 종료하고 `Assets/Prefabs/Field_Object/Shrine.prefab`을 엽니다.
**Shrine > Audio**에서 다음 음원을 지정합니다.

| 항목 | 재생 시점 |
|---|---|
| Activation Sound | 상호작용으로 이 제단이 현재 활성 체크포인트가 되었을 때 |
| Heal Sound | 이미 활성화된 제단을 다시 이용하여 실제로 체력이 증가했을 때 |

각 Clip에 음원을 하나 넣거나, Clip을 비우고 Clips에 여러 변주를 넣습니다.
기본 Channel은 SFX이며, Volume·Pitch Range·Minimum Interval도 각 항목에서 조절합니다.
두 음원은 비워두었습니다. 별도의 AudioSource는 필요하지 않습니다.

- 활성화하면서 회복되는 경우에는 Activation Sound만 재생합니다.
- 이미 활성화된 제단에서 체력이 가득 찼다면 Heal Sound는 재생하지 않습니다. 기존 저장 동작은 유지합니다.
- 다른 제단에서 이 제단으로 활성 체크포인트를 옮기는 경우에도 Activation Sound를 사용합니다. 영구적인 최초 발견 여부를 새로 저장하는 기능은 아닙니다.
- 이어하기·리스폰에서 RestoreActive로 상태를 복구할 때는 두 소리 모두 재생하지 않습니다.
- 범위 밖이거나 리스폰 컴포넌트가 없어 활성화가 거절되면 소리도 재생하지 않습니다.
- 제단 이용과 동시에 열리는 유물창의 UI 소리는 별도입니다. 필요하면 각 Volume을 조절합니다.

Shrine 원본을 사용하는 씬 인스턴스는 프리팹 설정을 상속합니다. 해당 필드에 씬 Override가 있으면 그 값이 우선합니다.

검증: 실제 Shrine 소스를 사용한 격리 하네스 13개 검사, Unity 참조를 사용한 별도 전체 C# 컴파일, 프리팹 설정 검사를 수행했습니다.
실제 트리거·저장/복원·회복과 소리 크기는 Play Mode에서 확인해야 합니다.
