# 씬 폴더 안내

씬의 용도에 따라 `Assets/Scenes`를 세 폴더로 나눕니다.

| 폴더 | 용도 | 포함된 씬 |
| --- | --- | --- |
| `Game` | 정식 플레이용 씬 | `Title`, `Intro`, `Tutorial`, `F_01`~`F_10`, `Boss` |
| `Test` | 기능 확인과 실험 | `Panyong_Combat_`, `Test_Alpha`, `Test_BossRoom` |
| `Legacy` | 이전 버전, 백업, 기본 틀 | `Area_01`, `F_02 backup`, `Forest01`, `Forest01_Entrance`, `Forest02`, `Forest020`, `GamePlay_Base`, `Hongmin`, `F_base` |

정식 게임의 시작 씬은 `Assets/Scenes/Game/Title.unity`입니다. 보스의 정식 맵 수정은 `Game/Boss.unity`, 테스트 맵 수정은 `Test/Test_BossRoom.unity`에서 합니다. 두 씬은 별도 파일이므로 한쪽의 씬 변경이 다른 쪽에 자동으로 적용되지는 않습니다.

Hongmin 씬에 딸린 `Hongmin/Global Volume Profile.asset`과 이전 `New SceneTemplate 1.scenetemplate`도 `Legacy`에 보관합니다. Unity가 제공하는 `Assets/Settings/Scenes/URP2DSceneTemplate.unity`는 이번 정리 대상에서 제외했습니다.

## 이동 규칙

- 정식 플레이에 사용할 새 씬은 `Game`, 기능 확인용 씬은 `Test`에 만듭니다.
- 이전 씬을 보관할 때는 관련 에셋과 함께 `Legacy`로 옮깁니다. 폴더 이름만으로 미사용 에셋이라고 판단해 삭제하지 않습니다.
- Unity Project 창에서 이동하는 것을 기본으로 합니다. Unity가 닫힌 상태에서 외부 도구로 옮길 때는 `.meta`를 함께 옮겨 GUID를 유지합니다.
- 씬 파일 이름은 저장 데이터와 포탈의 대상 이름으로 사용될 수 있으므로, 폴더 이동과 씬 이름 변경을 구분합니다.
- 경로 변경 시 빌드 목록과 경로를 사용하는 에디터 도구도 함께 수정합니다.

## 이번 정리의 범위

씬 내용, 씬 이름, 기존 에셋의 `.meta`는 그대로 유지했습니다. 빌드 목록의 순서와 활성화 상태도 유지했으므로 `Test`와 `Legacy`로 옮긴 씬 중 일부는 여전히 빌드에 포함됩니다. 폴더 구분만으로 빌드에서 제외되지는 않습니다.

빌드 목록의 `F_02` GUID는 이동 전부터 실제 씬 `.meta`와 달랐으므로 실제 GUID로 맞췄습니다.

지도 추출 메뉴는 `Tools/UI/지도 방 그림 뽑기 (Game 씬)`입니다. 튜토리얼 편집기, F_02 최적화 도구, 보스 테스트 씬 생성기, 타이틀 생성기와 글꼴 통일 도구도 새 경로를 사용합니다.
