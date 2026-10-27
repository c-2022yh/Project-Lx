# 사운드 기본 세팅

## 음원 넣기

1. Unity Project 창에서 `Assets/Audio/Clips/BGM`, `SFX`, `UI` 중 알맞은 폴더에 음원을 넣습니다. 폴더 위치는 정리용이며 실제 연결은 다음 단계가 필요합니다.
2. `Project Lx > Audio > Select Audio Catalog` 메뉴 또는 `Assets/Resources/Audio/GameAudioCatalog.asset`을 선택합니다.
3. Inspector의 Sounds에서 원하는 이벤트를 펼친 뒤 Clips의 첫 번째 칸에 음원을 드래그합니다.
4. Play Mode에서 해당 동작을 실행합니다. 씬에 매니저를 추가할 필요는 없습니다.

빈 칸은 무음입니다. 여러 Clips를 넣으면 변형 중 하나를 선택합니다. 음원 연결은 Play Mode를 끈 상태에서 해야 저장됩니다.

| 슬롯 | 자동 재생 시점 |
|---|---|
| PlayerAttack | 기본 지상/공중 공격의 선딜 종료 후 공격이 발생할 때 |
| PlayerHurt | 무적 검사를 통과한 실제 피해, 생존한 경우 |
| PlayerDeath | 플레이어 체력이 0이 되는 피해 |
| EnemyHurt | 실제 피해 후 생존한 적 |
| EnemyDeath | 일반 처치 또는 Execute로 적이 사망할 때 |
| UIClick | Unity UI Button 클릭 또는 키보드/패드 Submit |
| WindowOpen / WindowClose | 인벤토리, 스킬, 맵, 일시정지 창의 열기/닫기 |
| TitleBgm | Title 씬 |
| GameplayBgm | Title 이외의 단일 씬 진입 |

기본 공격 소리는 모든 콤보가 공유합니다. 스킬, 점프, 대시, 발소리, 상자, 유물별 소리는 아직 자동 연결하지 않았습니다.

## 조절

- Volume: 해당 소리만의 음량. 환경설정의 전체/분류별 음량과 함께 적용됩니다.
- Minimum Interval: 같은 이벤트의 최소 재생 간격(초). 적 여러 마리의 동시 피격 소리가 과도하게 겹치는 것을 줄입니다. 0이면 제한하지 않습니다.
- Pitch Range: 둘 다 1이면 원음. 효과음에만 작은 변화를 줄 수 있습니다. 배경음은 항상 1입니다.
- SFX Voices / UI Voices: 동시에 재생할 수. 가득 차면 새로운 효과음을 건너뜁니다.
- Music Fade Seconds: 배경음 전환 시간. 같은 BGM은 씬 이동해도 유지합니다. 다음 BGM 슬롯이 비어 있으면 이전 곡을 페이드 아웃합니다.
- Auto Play Scene Music: 기본 씬별 BGM 재생 여부.
- Auto Bind Buttons: 씬의 Unity UI Button에 클릭 음원을 자동 연결합니다. 동적 버튼은 최대 0.25초 후 연결됩니다. 즉시 연결하려면 버튼 프리팹에 GameAudioButton을 추가합니다.

모든 음원은 우선 2D로 재생합니다. BGM/UI는 일시정지 중에도 재생합니다. 게임 일시정지는 기존 Time.timeScale 방식을 유지하므로 이미 재생 중인 효과음을 중단하지 않습니다. AudioListener는 기존 카메라의 것을 사용하며 새로 추가하지 않습니다.

## 구조와 확장

`GameAudio`가 실행 시작 시 자동 생성되고 씬 전환에도 유지됩니다. `GameAudioCatalog`는 Resources에서 읽으며, 씬/프리팹에 별도 매니저 참조를 지정하지 않아도 됩니다.

Mixer 경로는 `Assets/Audio/GameAudioMixer.mixer`이며 Master 아래 BGM/SFX/UI로 나뉩니다. 노출된 파라미터 Master/BGM/SFX/UI를 기존 환경설정에서 적용합니다. 설정창은 기존 적용/저장 방식을 유지합니다. 볼륨 0은 정확한 무음으로 처리합니다.

새 이벤트는 GameSound에 추가하고 `Project Lx > Audio > Set Up Audio`를 다시 실행한 뒤 필요한 시점에 `GameAudio.Play(GameSound.새이벤트)`를 호출합니다. 기존 슬롯/음원은 유지됩니다. 애니메이션 이벤트나 UnityEvent에서 호출하려면 GameAudioEmitter 컴포넌트의 Play 함수를 사용할 수 있습니다. 이미 자동 연결된 동작에 추가하면 중복 재생될 수 있으니 한쪽만 사용합니다.

설정 메뉴의 Mixer 생성 부분은 Unity Editor 내부 API를 사용합니다. Unity 버전을 올린 후 설정 도구가 실패하면 확인이 필요합니다. 생성된 Mixer와 게임 실행 코드는 내부 API를 사용하지 않습니다.

## 확인할 항목

`Project Lx > Audio > Verify Setup`은 카탈로그 슬롯, Mixer 연결, 볼륨 파라미터를 확인합니다.

실제 음원 연결 후에는 공격/피격/처치, 타이틀↔게임 이동, 네 가지 볼륨 및 0 음소거, 일시정지 중 UI, 연속 타격을 Play Mode에서 확인합니다. 정적 컴파일과 에셋 검증만으로 청감 및 게임 전체 흐름을 검증한 것은 아닙니다.
