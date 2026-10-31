# 플레이어 이동 효과음

Play Mode를 종료하고 `Assets/Prefabs/Player/Player.prefab`을 엽니다.
**PlayerMove > Movement Audio**에서 음원을 지정합니다. 별도 AudioSource는 필요하지 않습니다.
모든 항목은 기본적으로 SFX이며, 비워두면 무음입니다.

| 위치 | 지정할 소리 |
|---|---|
| Jump Sound > Clip | 실제 점프 성공 시. 지상·추가 공중 점프가 공유 |
| Dash Sound > Clip | 일반 대시 시작 시. PiercingDash 스킬과는 별개 |
| Movement Surfaces > Grass > Footstep Sound | 풀 발걸음 |
| Movement Surfaces > Grass > Landing Sound | 풀 착지 |
| Movement Surfaces > Stone > Footstep Sound | 돌 발걸음 |
| Movement Surfaces > Stone > Landing Sound | 돌 착지 |
| Default Footstep Sound / Default Landing Sound | 어느 재질 규칙에도 맞지 않는 바닥. 필요할 때만 지정 |

Clip 하나 또는 Clip을 비우고 Clips에 여러 변주를 넣습니다. Volume과 Pitch Range도 각 소리에서 조절합니다.
지정된 재질의 음원이 비어 있을 때 Default로 자동 대체하지는 않습니다.

## 풀과 돌 구분

- 실제 접지 여부·지지 충돌체는 기존 Player.CheckGrounded 결과를 사용합니다. 접지·점프·물리 레이어 설정은 바꾸지 않았습니다.
- Grid의 `Tilemap_Grass`는 충돌체가 없는 장식 Tilemap입니다. 실제 접지점 바로 위에 해당 타일이 있는지 확인해 풀 소리를 선택합니다.
- 풀 타일이 없으면 지지 충돌체 또는 부모의 GameObject 이름을 검사합니다.
- 기본 Stone 이름 목록은 `Tilemap_Ground`, `Ground`, `TIleMap_Platform`, `MovingPlatform`입니다.
- 이름은 대소문자를 구분하지 않는 정확한 이름 비교이며, 런타임의 `(Clone)` 접미사는 무시합니다.
- 시각적 풀 Tilemap 규칙을 먼저 검사하고, 그 다음 충돌체 이름 규칙을 배열 순서대로 검사합니다.
- Tilemap의 한 셀에 재질이 혼재하거나 풀 그림이 실제 발 디딤 위치와 떨어져 있다면 실제 씬에서 검사 높이·폭을 맞춰야 합니다.

## 플랫폼과 새 재질

플랫폼은 통과 가능 여부가 아니라 표면 재질에 맞춰 분류하면 됩니다. 현재 플랫폼은 돌 소리를 공유합니다.
나무 플랫폼이 생기면 Movement Surfaces에 원소를 추가하고 다음처럼 설정합니다.

1. Surface Name: Wood.
2. Collider Object Names: 해당 플랫폼 충돌체 또는 부모 오브젝트의 정확한 이름.
3. Footstep Sound / Landing Sound에 나무 소리를 지정합니다.
4. 같은 이름이 Stone에도 있으면 Stone 목록에서 제거하거나 Wood 규칙을 Stone보다 앞에 배치합니다.

현재의 MovingPlatform을 나무 소리로 바꾸려면 `MovingPlatform` 이름을 Wood 규칙으로 옮기면 됩니다.
새 재질을 위해 스크립트나 GameObject Layer를 추가할 필요는 없습니다.

## 간격과 감지 조절

| 설정 | 의미 | 기본값 |
|---|---|---|
| Footstep Distance | 한 번의 발소리 사이에 실제로 걸어야 하는 수평 거리 | 2 |
| Minimum Footstep Interval | 빠르게 이동해도 발소리를 이보다 자주 재생하지 않음 | 0.12초 |
| Minimum Landing Air Time | 짧은 접지 흔들림을 착지로 취급하지 않는 최소 공중 시간 | 0.08초 |
| Surface Probe Height | 실제 지지 접점에서 위로 풀 타일을 검사하는 거리 | 0.12 |
| Surface Probe Half Width | 중앙과 좌우에서 풀 타일을 검사하는 반폭 | 0.12 |
| Audio Teleport Distance | 한 물리 프레임의 큰 위치 이동을 보행으로 세지 않는 기준 | 4 |

걷기음은 입력만 유지한다고 나지 않습니다. 실제 이동 거리·접지·이동 가능 상태를 확인합니다.
공중·일반 대시·사망·이동을 막는 UI 창에서는 발소리를 누적하지 않습니다. 발판의 평행이동은 같은 지지체 위에서 이동 거리에서 빼고, 입력이 없는 탑승은 발소리를 내지 않습니다.
생성·리스폰·씬 변경·ShadowSwap 순간이동은 오디오 이력을 초기화해서 잘못된 걸음·착지음을 방지합니다.

## 확인할 것

풀과 돌을 왕복하며 걷기, 풀 없는 Ground, 고정·움직이는 플랫폼, 벽에 대고 이동,
제자리·공중·일시정지, 지상/추가 공중 점프, 플랫폼 내려가기, 대시 잠금·쿨타임,
착지 직후 점프, 리스폰·그림자 교체를 Play Mode에서 확인합니다.

별도 C# 컴파일 및 순수 타이밍 검사는 실제 Tilemap/Collider 접촉과 청음을 대체하지 않습니다.
