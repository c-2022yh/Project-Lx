# 보주 HUD 효과 설정

유물 SO 하나의 `Orb HUD Style`에서 관리합니다. HUD 컴포넌트, 공통 배경,
프리팹, 기존 SO의 연결값은 코드 변경 과정에서 수정하지 않았습니다.
공통 `OrbBackDrop`은 고정 이미지 + 유물별 `Backdrop Color`로 유지합니다.

## 공통 규칙

- `Show Eye Effect`와 `Eye Effect Condition`은 눈의 지속 표시만 제어합니다.
- `Play Full Charge Burst`는 실제 기력이 `Full Charge Eye Energy`(기본 100)에
  도달한 순간의 눈 확산 효과입니다. 기본 눈 표시와 독립적입니다.
- `Crescent` 모드에서는 30/30이 되어도 공통 완충 순간 효과를 재생하지 않습니다.
- 지속 추가 효과는 `Persistent Effect Condition`으로 제어합니다.
  `Persistent Effect Prefab`이 있으면 프리팹이 우선합니다. 비어 있으면
  PNG/프레임 배열을 코드가 재생합니다. PNG 기반 효과에는 Animator가 필요 없습니다.
- `Full Charge Burst Prefab`이 비어 있으면 기존 눈 이미지 확산 연출을 사용합니다.
- `Awakening End Effect Prefab`이 비어 있으면 `Awakening End Sprite`를 사용합니다.
  둘 다 없으면 각성 종료 효과를 재생하지 않습니다.
- 추가 효과의 `Layer`는 Front/Back 중 선택합니다. 루트는 실행 중 자동 생성됩니다.
- 이미지 연출은 보주 배경의 RectTransform을 기준으로 정렬하고 부모 HUD Scale을 상속합니다.
  같은 캔버스/여백으로 제작된 PNG를 자르지 않고 사용하세요.

## 그믐

- `Gauge Mode = Crescent`.
- `Gauge Material = Assets/Material/OrbCrescentFillMaterial.mat`.
  에디터에서는 셰이더 자동 탐색도 지원하지만 빌드 포함을 위해 머티리얼 연결을 권장합니다.
- `Crescent Radius = 0.38`, `Crescent Width = 0.3`에서 시작해 실제 아트에 맞춥니다.
  Width는 최대 충전 시 왼쪽 그믐달의 두께이고, 실제 기력 상한과 별개입니다.
- 기존 `Fill Sprite`에 밝은 보주 이미지를 연결합니다. 셰이더가 두 원의 차집합으로
  왼쪽 곡선 영역만 드러냅니다. 0/30은 없음, 15/30은 중간, 30/30은 완성된 그믐달입니다.
- `Show Eye Effect = On`, `Eye Effect Condition = Always`.
  눈이 정렬된 채 반짝이게 하려면 `Pulse Scale = 0`, `Pulse Alpha Amount = 0.25` 등을 사용합니다.
- `Play Full Charge Burst = Off`, `Persistent Effect Condition = None`, 각성 종료 효과 Off.
- 게임 효과 SO `RFX_DarkMoon`의 새 `Max Energy Cap`은 기본 30입니다.
  이전의 고정 차감값은 사용하지 않으며, 이 상한은 다른 최대 기력 보너스와 별도로 적용됩니다.

## 만월

- `Gauge Mode = Wave`.
- `Show Eye Effect = On`, `Eye Effect Condition = FullEnergy`.
  `Effect Color`의 Alpha 1, `Pulse Alpha Amount` 약 0.15로 밝게 유지할 수 있습니다.
- `Play Full Charge Burst = On`, `Full Charge Eye Energy = 100`.
- `Persistent Effect Condition = FullEnergy`, `Persistent Effect Prefab = None`.
- `Persistent Animation = Pulse`, `Persistent Effect Sprite = FullMoon_Halo`.
- `Persistent Effect Layer = Back`, `Persistent Effect Color = White`.
- `Persistent Pulse Period = 2.4`, `Persistent Alpha Range = (0.38, 0.8)`.
- `Persistent Fade Speed = 5`에서 시작합니다. 기력을 쓰면 후광은 서서히 사라집니다.
- 각성 종료 효과 Off.

## 환류

- 공통 완충 눈 확산은 On. 평상시 눈을 유지하지 않으려면 `Show Eye Effect = Off`.
- `Persistent Effect Condition = None`.
- `Play Awakening End Effect = On`, `Awakening End Effect Prefab = None`.
- `Awakening End Sprite = Reflux_RecoveryRing`, Color White, Alpha 약 0.8.
- ZIP 권장: Duration 0.36, From Scale 1.16, Scale 1.0, Fade In Fraction 0.2.
  확대하고 싶다면 From 1.0 / Scale 1.5처럼 바꿀 수 있습니다.
- ZIP은 Front를 권장합니다. 보주 뒤의 후광으로 쓰려면 Layer를 Back으로 선택합니다.
- 각성 종료 순간 한 번만 재생하며 남은/회복된 기력 비율과 무관하게 끝까지 표시합니다.

## 연소

- 공통 완충 눈 확산은 On. 평상시 눈 지속 표시는 필요 없으면 Off.
- `Persistent Effect Condition = Awakened`, `Persistent Effect Prefab = None`.
- `Persistent Animation = SpriteSequence`, `Persistent Effect Layer = Back`.
- `Persistent Effect Frames`에 `Burning_Flame_01, 02, 03, 04`를 순서대로 넣습니다.
- `Persistent Frame Rate = 6~8`, `Persistent Frame Cross Fade = 1`로 시작합니다.
  Cross Fade 0은 즉시 프레임 교체, 1은 프레임 사이를 부드럽게 섞습니다.
- Color White. 각성 종료/유물 해제 시 즉시 정리됩니다.

## 폭주

- 공통 완충 눈 확산은 On. 평상시 눈 지속 표시는 필요 없으면 Off.
- `Persistent Effect Condition = AwakeningSkillReady`, `Persistent Effect Prefab = None`.
- `Persistent Animation = Intermittent`.
- `Persistent Effect Frames`에 번개 PNG를 원하는 순서로 넣습니다.
- 예: Frame Rate 12, Flash Duration 0.25, Flash Interval (0.6, 1.8).
- Layer Front 또는 Back, Color White. 밝기/크기는 아트를 보며 조정합니다.
- Q의 1회 사용권이 남아 있는 각성 동안만 재생됩니다. 일시적인 행동 잠금은 사용권을
  없애지 않습니다. 실제 Q 사용 성공 또는 각성 종료 시 번개가 즉시 사라집니다.

## 확인 순서

1. 그믐: 실제 기력 0/15/30, 초과 획득 제한, 해제 후 기본 최대 기력 복원.
2. 그믐: 상시 눈 반짝임, 30 도달 시 공통 눈 확산 없음, 각성 차단 유지.
3. 만월: 100 도달 순간 눈 확산, 완충 유지 후광/눈빛, 소비 시 후광 페이드.
4. 환류: 각성 종료 후 회복 링 1회, 기력이 0이거나 회복 중이어도 재생 시간 유지.
5. 연소: 각성 중 프레임 순환, 각성 종료 즉시 정지.
6. 폭주: Q 사용권이 남을 때 간헐적 번개, Q 성공 시 정지, 실패한 Q 시도에는 유지.
7. 공통: 보주 교체/해제, HUD 비활성화/재활성화 시 중복 효과와 이벤트 구독이 남지 않음.

코드형 이미지 효과와 타이머는 기존과 같이 unscaled time을 사용합니다.
플레이 모드에서 원형 마스크, 앞뒤 배치, PNG 여백과 크기는 별도로 확인해야 합니다.
