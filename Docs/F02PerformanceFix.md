# F_02 Ground 성능 수정

## 적용 범위

`Assets/Scenes/InProgress/F_02.unity`의 Grid 인스턴스 아래 Tilemap_Ground에만 세 가지 Override를 추가했습니다.

| 컴포넌트 | 변경 |
|---|---|
| TilemapRenderer | Individual → Chunk |
| TilemapCollider2D | Composite Operation: None → Merge |
| CompositeCollider2D | Geometry Type: Outlines → Polygons |

타일별 렌더링을 청크로 묶고, 이웃한 지형 충돌체를 합성하도록 합니다.
Polygons를 사용해 내부가 채워진 고체 지형을 유지합니다. 외곽 선분만 있는 빈 지형으로 바꾸지 않습니다.
Ground의 기존 PlatformEffector2D는 비활성 상태이며 바꾸지 않았습니다.

공유 Grid 프리팹, 다른 씬, 통과형 플랫폼, 독가스, 적, 사운드 코드, Ground 셰이더·머티리얼은 이번 수정에서 변경하지 않았습니다.
파일 수정 직후 추가한 Override 세 항목만 제거하면 직전 저장 상태의 F_02 파일과 정확히 일치하는 것을 검사했습니다.
따라서 이미 저장되어 있던 사용자의 씬 편집은 보존했습니다.

## 검증

- Unity 6000.4.0f1의 별도 최소 프로젝트에서 네이티브 Physics2D 테스트를 실행했습니다.
- 11,183개의 연속된 Grid 충돌 타일로 만든 합성 전/후 지형을 비교했습니다.
- 합성 전 TilemapCollider 형상 11,183개, 합성 후 TilemapCollider 형상 0개 + CompositeCollider 형상 2개였습니다.
- 지형 내부 충돌과 캡슐 캐릭터 접지가 모두 유지됐습니다. 정지 높이 차이는 약 0.000015입니다.
- 1,500회 물리 시뮬레이션 합계는 해당 1회 측정에서 5.47ms / 4.52ms였습니다. 반복 통계가 아니며 F_02 성능이나 FPS를 나타내지 않습니다.
- 테스트 지형은 연속된 정사각형 셀입니다. F_02의 실제 타일별 복잡한 충돌 모양, 사면, 독가스, 적, 조명·렌더링을 재현한 것은 아닙니다.
- F_02의 실제 FPS 개선과 화면 정렬·다중 텍스처·깊이 표현은 아직 Play Mode에서 확인해야 합니다.

네이티브 실행 결과: `output/PerformanceAudit/ground-native-check/results.json`.
최종 수정 직전 저장 파일: `output/PerformanceAudit/F_02.latest-before-ground-optimization.unity`.
최초 작업 시작 상태도 `output/PerformanceAudit/F_02.before-ground-optimization.unity`에 보존했습니다.

## Unity에서 확인

작업 도중 Unity가 열려 있던 씬을 다시 저장해 외부 파일 수정이 빠지는 사례가 확인됐습니다.
기존 편집을 잃지 않고 현재 열린 씬에도 설정을 맞추도록 `Assets/Editor/F02GroundPerformanceSetup.cs`를 추가했습니다.
스크립트 로드 후 편집 모드에서 열려 있는 F_02에 한 번 적용하며, Play 중이면 편집 모드로 돌아온 뒤 적용합니다.
프리팹 원본·다른 씬은 수정하지 않고 Undo로 되돌릴 수 있습니다. 적용 시 씬은 수정 상태가 되며 자동 저장하지 않습니다.

**Play Mode 종료 → Project Lx > Performance > Apply F_02 Ground Optimization → 씬 저장 → Play** 순서로 확인합니다.
새 메뉴가 아직 없다면 Unity의 스크립트 임포트·컴파일이 끝난 뒤 확인합니다.
이 방식은 씬을 다시 열 필요가 없어 현재 편집 중인 다른 내용을 유지합니다.
이동·점프·경사면·벽·플랫폼 내려가기·화염구 충돌, Ground 그림과 깊이 음영, 카메라 이동 중 타일 표시를 확인합니다.

독가스의 파티클을 껐을 때도 심하게 느리다는 사용자 확인에 따라 바닥의 물리/렌더링 부담을 우선 줄였습니다.
이 변경 후에도 느리다면 Unity Profiler에서 같은 F_02 구간의 CPU/Physics2D/Rendering/GPU 시간을 측정해 다음 병목을 좁혀야 합니다.
