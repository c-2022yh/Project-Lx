# 포탈·파괴물 효과음

Play Mode를 종료하고 원본 프리팹 또는 해당 씬 오브젝트에서 지정합니다.
기본 Channel은 SFX이며 음원은 비워두었습니다. Clip 하나 또는 Clip을 비우고 Clips의 랜덤 변주 목록을 사용합니다.

| 대상 | 지정 위치 | 음원 항목 |
|---|---|---|
| 상호작용 포탈 | ScenePortal 프리팹 > ScenePortal > Audio | Transition Sound |
| 자동 포탈 | AutoScenePortal 프리팹 > AutoScenePortal > Audio | Transition Sound |
| 일반 벽돌 | BreakableBrick 프리팹 > BreakableBrick > Audio | Hit Sound / Break Sound |
| 화염 파괴벽 | FireBreakableBrick 프리팹 > FireBreakableWall > Audio | Hit Sound / Break Sound |
| 비밀벽 | Grid 또는 Area_01 내부 Tilemap_SecretWall > SecretBreakableWall > Audio | Hit Sound / Break Sound |

## 포탈

- Transition Sound는 유효한 이동 요청으로 씬 전환을 시작할 때 한 번 재생합니다. 단순히 상호작용 포탈 근처에 접근하는 소리와는 다릅니다.
- 목적지 이름이 비어 있거나 이미 다른 전환이 진행 중이면 재생하지 않습니다.
- 자동 포탈은 플레이어만 처리하고 여러 플레이어 충돌체가 들어와도 한 번만 재생합니다.
- 기존 SceneFader의 씬 전환·페이드 시점은 유지합니다. 이동 중 새 요청이 기존 TargetSpawnId를 덮어쓰지 않도록 가드했습니다.
- GameAudio의 지속 오브젝트가 재생하며 출발 포탈의 수명에는 묶지 않았습니다. 씬 전환으로 포탈이 제거되어도 재생 음원을 중단하지 않습니다.
- 씬 로딩 자체가 메인 스레드를 점유하는 동안 지연이 전혀 없다는 뜻은 아닙니다. 실제 재생은 Play Mode에서 확인합니다.
- 기본 Spatial Blend 0을 유지하면 출발 위치와 도착 위치/카메라 거리 차이에 의한 감쇠가 없습니다.
- 씬이 빌드 목록에 없거나 로드에 실패하는 경우의 기존 SceneFader 오류 처리는 이번 변경 범위에 포함하지 않았습니다.

## 파괴물

- Hit Sound: 타격을 인정했지만 아직 파괴되지 않았을 때.
- Break Sound: 실제 파괴 시 한 번. 마지막 타격에는 Hit Sound를 겹치지 않습니다.
- Required Hits To Break가 1이면 바로 Break Sound만 나므로 Hit Sound를 지정해도 들리지 않는 것이 정상입니다.
- 일반 벽돌과 화염벽은 파괴 확정 이후 같은 프레임에 추가 호출이 들어와도 파괴음·상태 저장·파편을 반복하지 않습니다.
- 비밀벽은 Tilemap 전체가 아닌 해당 타일의 생존/파괴에 맞춰 소리를 내고, 타일 중심을 재생 위치로 사용합니다.
- 이미 파괴된 오브젝트나 타일을 저장 상태에서 복원할 때는 소리가 없습니다.
- 원본 오브젝트의 수명에 소리를 묶지 않아 Destroy 뒤에도 파괴음 잔향이 이어집니다.
- 비밀벽 여러 타일을 한 번에 부수는 경우 각 타격 요청이 들어올 수 있습니다. Minimum Interval / Max Simultaneous로 재생 밀도를 조절합니다.
- 기존 공격 명중·화염구 효과음과 별도입니다. 같은 완성 타격음을 여러 칸에 중복 지정하지 않도록 조절합니다.

## 검증 범위

실제 5개 소스를 사용하는 격리 하네스의 18개 검사, Unity 참조를 이용한 전체 C# 별도 컴파일,
6개 프리팹의 10개 SFX 설정 칸 검사를 통과했습니다.
실제 포탈 트리거·페이드·씬 전환 후 잔향, 화염구 반응, 비밀 타일 위치 및 다중 타일 공격은 Play Mode에서 확인해야 합니다.
