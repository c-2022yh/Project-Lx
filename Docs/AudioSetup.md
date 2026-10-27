# 개별 사운드 설정

## 가장 먼저 할 일

음원 파일을 Project 창으로 가져온 뒤, 아래 위치의 **SoundCue > Clip** 칸에 드래그합니다. 음원 폴더에 복사하는 것만으로 연결되지는 않습니다. Play Mode를 끈 상태에서 지정해야 저장됩니다.

| 원하는 소리 | Inspector에서 지정할 위치 |
|---|---|
| 기본 지상 공격 1타/2타 등 | Player 프리팹 > PlayerAttack > Ground Patterns > 각 패턴 > Audio |
| 기본 공중 공격 | Player 프리팹 > PlayerAttack > Air Attack > Audio |
| 스킬별 소리 | 해당 스킬 SO > Audio |
| 플레이어 피격/사망 | Player 프리팹 > PlayerHealth > Audio > Hurt Sound / Death Sound |
| 적 피격/사망 | 각 적 프리팹 > EnemyHealth > Audio > Hurt Sound / Death Sound |
| 적 원거리 공격/명중/비행음 | 각 적 프리팹 > EnemyRangedAttack > Audio |
| 적 엘리트 근접 공격/명중 | EnemyEliteAttack > Melee Pattern > Audio |
| 적 엘리트 돌진/지속음 | EnemyEliteAttack > Charge Audio |
| 늑대 물기/돌진별 소리 | EnemyWolfAttack > Bite / Ground Charge > 각 SoundCue |
| 적 접촉 피해의 충돌음 | EnemyContactAttack > Audio > Impact Sound |
| 공통 버튼 클릭, 창 열기/닫기 | Project Lx > Audio > Select Audio Catalog > Sounds > UIClick / WindowOpen / WindowClose |
| 특정 버튼만 다른 소리 | 해당 버튼에 GameAudioButton 추가 > Use Custom Sound 체크 > Custom Sound > Clip |
| 기본 타이틀/게임 BGM | 공통 카탈로그 > Sounds > TitleBgm / GameplayBgm |
| 특정 씬 BGM | 공통 카탈로그 > Scene Music에 항목 추가 > 정확한 Scene Name과 Music > Clip 지정 |

실제 프리팹/씬이 가진 패턴이 어느 것인지 확인하고 해당 인스턴스 또는 원본 프리팹에 지정합니다. 프리팹 Variant와 씬 인스턴스의 기존 Override가 있으면 해당 값이 우선합니다. 씬이나 프리팹을 다시 생성하는 별도 UI 빌더를 실행하면 수동으로 붙인 버튼 설정은 재확인이 필요합니다.

## SoundCue 사용법

SoundCue는 SO 에셋을 따로 만들어 연결하는 방식이 아니라, 각 공격/스킬/캐릭터 안에 펼쳐지는 공통 직렬화 설정 형식입니다. 모든 재생은 GameAudio가 담당합니다.

- **Clip**: 주 음원 하나. 여기에 넣는 것만으로 기본 재생이 가능합니다.
- **Clips**: 랜덤 변형 목록. Clip을 비워 두었을 때만 사용합니다. 빈 항목은 제외하고 선택합니다.
- **Channel**: SFX / UI / BGM 출력 분류. 공격·스킬·피격은 SFX, UI는 UI를 사용합니다. 씬별 Music은 BGM으로 재생합니다.
- **Volume**: 해당 소리의 기본 음량. 환경설정의 전체/분류별 음량과 함께 적용됩니다.
- **Pitch Range**: 둘 다 1이면 원음. 역순으로 입력해도 작은 값~큰 값 범위를 사용합니다. BGM은 항상 1입니다.
- **Minimum Interval**: 같은 cue가 연속 재생될 최소 간격(초). 0이면 간격 제한이 없습니다.
- **Max Simultaneous**: 같은 cue를 동시에 재생할 수. 전체 SFX/UI 풀에도 각각 상한이 있고, 가득 차면 새 요청을 건너뜁니다.
- **Limit Scope**: 기본 Per Owner는 캐릭터별 제한입니다. 같은 SO를 쓰는 다른 적/플레이어가 서로의 소리를 막지 않습니다. Global은 모든 사용자가 제한을 공유합니다.
- **Spatial Blend**: 기본 0은 2D. SFX에서 1로 설정하면 소리가 발생한 월드 위치에서 거리 감쇠와 방향감을 적용합니다. Min/Max Distance 사이를 선형 감쇠합니다. 실제 AudioListener와의 3D 거리이므로 2D 카메라의 Z 간격도 범위 설정에 고려합니다. UI/BGM은 2D입니다.
- **Use Fallback**: 기본 꺼짐. 켰을 때만, 로컬 Clip/Clips가 모두 비어 있으면 Fallback Sound의 공통 카탈로그 설정 전체를 사용합니다. 연결된 대체 슬롯도 비어 있으면 무음이며 연쇄 fallback은 하지 않습니다.

기존 카탈로그의 PlayerAttack/PlayerHurt/PlayerDeath/EnemyHurt/EnemyDeath 항목은 이전 데이터와 명시적인 fallback용으로 보존했습니다. **현재 전투 동작은 이 슬롯을 자동으로 사용하지 않습니다.** 각 패턴/프리팹에 음원을 넣거나, 해당 cue에서 Use Fallback을 켜야 합니다.

## 공격음과 피격음의 구분

- Attack Sound / Release Sound: 휘두르기, 발사처럼 공격 자체가 발생하는 소리.
- Impact Sound: 실제로 피해를 준 충돌·폭발 소리.
- Hurt Sound: 피해를 받은 캐릭터의 신음이나 반응 소리.
- Death Sound: 사망한 캐릭터의 소리. 치명타격에서는 Hurt 대신 Death를 재생합니다.

Impact와 Hurt/Death는 함께 재생될 수 있습니다. 같은 충돌 음원을 양쪽에 넣으면 겹치므로 용도를 구분합니다. 공격이 빗나가면 Impact는 재생되지 않고, 무적이나 피해 0으로 실제 피해가 없으면 Impact/Hurt도 재생되지 않습니다. 파괴 가능한 벽/벽돌은 별도 판정이므로 이 캐릭터 명중음 경로의 대상이 아닙니다.

## 스킬 Audio의 재생 시점

| 슬롯 | 시점/수명 |
|---|---|
| Cast Sound | 사용 조건과 비용 지불을 통과하고 시전을 시작할 때. 선딜 종료 또는 취소 때 정지 |
| Release Sound | 실제 효과 발생 시점. 아래 스킬별 표 참고 |
| Impact Sound | 실제 피해 적용 시점. 치명타격도 포함 |
| Active Loop | 선딜 종료 후 효과 실행 구간 동안 반복. 효과 구간 종료·취소·사망·오브젝트 비활성화 때 정지 |
| Complete Sound | 효과와 후딜이 끝나 정상 종료할 때 |
| Cancel Sound | 시전이 취소될 때. 오브젝트가 이미 비활성화됐으면 재생하지 않음 |

| 스킬 | Release Sound 시점 |
|---|---|
| Thrust | 찌르기 효과 프리팹 생성 |
| TripleSlash | 각 베기 효과 생성. 기본 최소 간격 0.05초이며 필요에 따라 조절 |
| ProjectileShot | 유효한 투사체를 초기화하고 발사 |
| PiercingDash | 돌진 효과 실행 시작 |
| Guard | 지상 조건을 통과하고 효과 시작 |
| ShadowSwap | 유효한 그림자 프리팹을 사용해 배치/교체 효과 시작 |
| VitalConversion | 생존 확인 후 실제 회복 적용 |
| ScorchedEarth | 준비가 끝나 각 투사체를 발사할 때. 동시 발사는 cue 제한에 따라 합쳐질 수 있음 |

ProjectileShot의 Projectile Audio > Flight Loop, ScorchedEarth의 Barrage Configuration > Projectile Audio > Flight Loop는 **투사체 자체가 소유하는 지속음**입니다. 투사체가 사라지거나 비활성화되면 멈춥니다. 플레이어의 Active Loop는 시전 동작이 끝나면 멈추므로 비행 소리는 Flight Loop에 넣습니다.

이미 발사된 투사체는 기존 게임 동작대로 시전 취소 이후에도 독립적으로 동작할 수 있습니다. 그 투사체의 비행음/명중음도 실제 투사체 수명과 명중에 따릅니다. 취소된 시전의 대기 중 발사 요청과 시전자 지속음은 멈춥니다.

스킬 SO에는 설정만 보관하며, 현재 소리 핸들/반복음/명중 제한 상태는 시전자와 실행 객체가 보관합니다.

## 범위 공격과 다단히트

기본 공격 패턴의 Impact Mode와 스킬 Audio > Impact Mode에서 선택합니다.

- Every Hit: 피해가 적용될 때마다 요청. Minimum Interval과 Max Simultaneous 제한도 함께 적용됩니다.
- Once Per Action: 기본 공격 한 번 또는 스킬 시전 전체에서 명중음 한 번.
- Once Per Target: 그 공격/시전에서 피해 대상마다 한 번. 같은 적을 여러 번 때려도 추가 명중음은 요청하지 않습니다.

TripleSlash 여러 타격과 ScorchedEarth 여러 투사체는 시전 하나의 명중 정책을 공유합니다. 명중음이 재생 제한에 막힌 경우에는 '이미 재생함'으로 처리하지 않아 다음 유효 명중에서 다시 시도할 수 있습니다.

## BGM / UI / 환경 소리

씬별 BGM은 카탈로그의 Scene Music에 정확한 씬 이름을 넣습니다. 해당 항목의 Music이 비어 있으면 이전 곡을 페이드 아웃하고 무음으로 전환합니다. 등록되지 않은 씬은 Title이면 TitleBgm, 그 외는 GameplayBgm을 사용합니다. 같은 cue는 씬 이동 시 유지합니다. 자동 전환은 Single 씬 로드에서 적용하며, Additive 로드에서는 현재 음악을 유지합니다.

공통 버튼음은 자동 연결됩니다. 동적으로 생성한 버튼은 최대 0.25초 뒤 연결되며, 즉시 연결하려면 프리팹에 GameAudioButton을 붙입니다. Use Custom Sound를 켜고 빈 cue를 두면 해당 버튼만 무음입니다. 오디오 컴포넌트를 비활성화하면 클릭음이 나오지 않습니다.

임의의 효과/환경 오브젝트에는 GameAudioEmitter를 붙여 Custom Sound에 음원을 지정합니다. Play On Enable과 Loop를 켜면 활성화 동안 지속음을 재생하고 비활성화 때 정지합니다. 애니메이션 이벤트나 UnityEvent에서 Play/Stop도 호출할 수 있습니다. 자동 연결된 동작에 별도 재생 이벤트까지 붙이면 중복될 수 있습니다.

## 볼륨과 검증

기존 전체/BGM/SFX/UI 설정과 적용·저장 방식을 유지합니다. 게임 일시정지는 기존 Time.timeScale 방식을 유지하므로 이미 시작한 SFX를 자동 정지시키지는 않습니다. UI/BGM과 BGM 페이드는 일시정지 중에도 작동합니다.

원본 씬·기존 프리팹·스킬 에셋의 음원은 비워 둔 상태이며, 기존 메타 GUID와 C# 인코딩을 유지했습니다. `Project Lx > Audio > Verify Setup`으로 공통 카탈로그와 Mixer 연결을 확인할 수 있습니다.

실제 음원을 넣은 뒤 기본 공격 1/2타, 각 스킬의 시전·발사·명중·취소, 지속형 효과 종료, 적 사망, UI, 씬 이동을 Play Mode에서 확인합니다. 별도 검증 프로젝트의 테스트는 실제 게임 씬의 타이밍과 청감 검증을 대신하지 않습니다.
