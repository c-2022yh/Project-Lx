# 검은 지형 덮개 팔레트

128×128px / Pixels Per Unit 128 / Grid 1×1 / 표면 노출 24px 기준이다.
타일 자체의 크기나 Transform Scale을 줄이지 않는다. 투명 부분을 통해 원래 지형이 보인다.

## 사용하기

1. Unity가 파일을 가져온 뒤 `Window > 2D > Tile Palette`를 연다.
2. 팔레트 목록에서 `GroundBlack_Palette`를 선택한다. 목록에 바로 나타나지 않으면 Tile Palette 창을 다시 연다.
3. 실제 지형을 준비한 뒤 같은 Grid 아래 Rectangular Tilemap을 만들고 이름을 `Ground_Black`으로 지정한다.
4. Active Tilemap을 `Ground_Black`으로 지정하고 경계·모서리 타일과 내부 타일을 직접 배치한다.

- Grid는 Rectangle / Cell Size (1,1) / Cell Gap (0,0)이어야 한다.
- 덮개는 기존 지형과 같은 Sorting Layer에서 Order in Layer를 1 높이고 `Materials/GroundBlack_Unlit.mat`을 적용한다. 필요에 따라 프로젝트의 GroundCover Sorting Layer를 사용할 수도 있다.
- Collider2D나 Rigidbody2D는 덮개에 추가하지 않는다. 충돌은 원래 지형이 담당한다. 모든 제공 Tile의 Collider Type도 None이다.
- 스프라이트는 Point / Compression None / Mip Maps Off / Mesh Full Rect이다. 24px은 월드 기준 0.1875유닛이다.

## 타일 선택

팔레트는 왼쪽에서 오른쪽, 위에서 아래 순서다.

| 이름 | 투명 영역 |
| --- | --- |
| Interior | 없음. 전부 검정 |
| Edge_Top / Bottom / Left / Right | 이름 방향의 24px 띠 |
| Outer_TopLeft / TopRight / BottomLeft / BottomRight | 이름의 두 방향 띠. 지형의 볼록한 바깥 모서리 |
| Inner_TopLeft / TopRight / BottomLeft / BottomRight | 이름 방향의 24×24px 사각형만 투명. 대각선에 빈 공간이 있는 오목한 지형 모서리 |

Inner의 방향은 타일 안에서 투명한 사각형이 있는 위치를 뜻한다. 예: `Inner_TopLeft`는 위·왼쪽·현재 칸에는 지형이 있고 왼쪽 위 대각선 칸만 비었을 때 사용한다.

13종은 기본적인 두꺼운 지형용이다. 한 칸 폭 기둥·얇은 발판처럼 반대쪽 두 면이 동시에 노출되는 칸이나, 한 칸에 여러 오목한 모서리가 모이는 형태에는 추가 조합 타일이 필요하다. 그런 구간을 Interior로 덮어 표면을 가리지 않도록 주의한다.

## 작업 범위와 검증

팔레트, 덮개 PNG 13종, Tile 에셋 13종과 Unlit 머티리얼만 제공한다. 테스트 씬, 테스트 프리팹, 임시 지형, 에디터 도구는 포함하지 않는다. 원본 씬·Grid·지형·전역 조명·프로젝트 설정은 수정하지 않았다. Rule Tile 자동 배치, 요철, 흐림 경계도 추가하지 않았다.

별도 제작 검사에서 13종의 크기·검정 RGB·이진 알파·투명 영역 넓이와 합성 배치의 경계 두께·내부 연결을 확인했다. 실제 Unity 임포트 결과와 카메라 이동 시 이음새, 조명 반응은 사용 중인 씬에서 확인해야 한다.
