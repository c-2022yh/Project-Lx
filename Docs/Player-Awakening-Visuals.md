# 플레이어 각성 발광

`Assets/Prefabs/Player/Player.prefab`의 `PlayerAwakeningVisual`에 Visual의 SpriteRenderer, `M_PlayerAwakeningGlow`, `M_PlayerAwakeningAura`가 연결되어 있다. 새 스프라이트를 지정할 필요는 없다. 실행 중 현재 애니메이션을 따라가는 가산 발광 렌더러, 몸 뒤의 노이즈 오라, Point Light 2D, 각성 전용 Bloom Volume을 생성한다.

## 조절 위치

Player 프리팹 루트의 **Player Awakening Visual**:

| 항목 | 기본값 | 용도 |
| --- | --- | --- |
| Glow Color / Intensity | 푸른색 / 4 | 윤곽의 색과 HDR 발광 밝기. 이전 6에서 낮춰 오라와 조합했다. |
| Outline Pixels | 2 | 텍스처 픽셀 기준 안쪽 윤곽 두께 |
| Body Glow | 0.025 | 몸 내부에 더하는 빛. 얼굴이 흐려지면 먼저 낮춘다. |
| Fade In / Out | 0.3초 / 0.3초 | 진입·종료의 부드러운 전환 |
| Entry Peak / Settle | 1.8배 / 0.35초 | 방출 순간의 밝기 배수 / 유지 밝기로 돌아오는 시간 |
| Pulse Amount / Frequency | 0.1 / 0.8Hz | 유지 중 밝기 맥동 |
| Light Color / Intensity / Radius | 푸른색 / 0.8 / 2.5 | 주변에 닿는 조명 |
| Light Offset | (0, -0.3) | 현재 포즈의 렌더러 중심에 대한 월드 좌표 오프셋 |
| Light Sorting Layers | Ground | 빛을 받을 **Sorting Layer 이름** 목록 |
| Enable Bloom | 켜짐 | 각성 중 Bloom 사용 |
| Bloom Intensity / Threshold / Scatter | 0.6 / 1.1 / 0.65 | 빛 번짐 강도 / 밝기 임계값 / 퍼짐 |

씬의 Player 인스턴스에 동일 항목의 override가 있으면 해당 값이 우선된다. 조명 색상은 Glow Color와 별도로 조절한다.

## 각성 진입: 집중 → 방출 → 안정

**Awakening Intro** 설정은 기존 각성 도입 동작에 연출을 얹는다. 능력치 적용·기력 소모·물리 정지·조작 복귀는 기존 PlayerAwakening이 소유한다. PlayerAwakening의 IntroElapsed/IntroDuration을 읽어 효과 시간을 맞추므로 각성 유지 상태에 들어가는 순간 섬광이 재발생하지 않는다.

| 기본 시점 | 연출 |
| --- | --- |
| 0~0.25초 | 발밑에 작은 빛이 모이고, 낮고 좁은 오라와 몸 윤곽이 차오른다. |
| 0.25~0.4초 | 오라가 빠르게 높아지고 양옆으로 벌어진다. 발광·오라·작은 조명이 순간적으로 밝아진다. 화면 가장자리 UI도 이 구간부터 켜진다. |
| 0.4~0.75초 | 밝기·높이·각도·속도가 기존 유지 설정값으로 돌아온다. |
| 0.75초 이후 | 유지 오라와 맥동. 기본 조작 복귀 시점은 기존대로 0.8초다. |

| 설정 | 기본값 | 역할 |
| --- | --- | --- |
| Enable Intro Sequence | 켜짐 | 단계 연출 사용. 끄면 이전 페이드·각성 완료 시 밝기 피크 방식 사용. |
| Charge Duration | 0.25초 | 발밑으로 힘을 모으는 시간 |
| Release Duration | 0.15초 | 모인 기운을 방출하는 시간 |
| Charge Brightness | 0.65배 | 집중 완료 시 윤곽·조명의 밝기 |
| Charge Height | 0.22배 | 집중 완료 시 오라 높이. 유지 높이에 대한 비율 |
| Charge Fan Angle | 20도 | 집중 중의 좁은 퍼짐 각도 |
| Charge Aura Boost | 2배 | 집중 중 발밑 오라를 더 잘 보이게 하는 밝기 보정 |
| Release Height | 1.15배 | 방출 순간의 높이. 유지 높이에 대한 비율 |
| Release Angle | 1.15배 | 방출 순간의 각도. 유지 각도에 대한 비율 |
| Release Speed | 3배 | 방출 순간의 흐름 속도 |
| Entry Peak | 1.8배 | 방출 순간의 밝기 |
| Entry Settle | 0.35초 | 방출 후 유지 상태로 안정되는 시간 |

세 단계의 요청 시간 합이 PlayerAwakening의 도입 시간보다 길면 모두 같은 비율로 압축한다. 도입 시간이 0이면 즉시 유지 외형을 사용한다. 도입 시간이 더 길면 안정화 후 유지 외형으로 남은 시간을 기다린다.

PlayerScreenEffects의 Entry Distortion 강도는 같은 방출 피크에 맞춰 적용되며 비네트·피격·저체력 경고는 기존 계산을 유지한다. 화면 왜곡 강도는 해당 컴포넌트에서 조절한다. 기존 별도 AwakeningEffect 참조나 효과음 클립을 자동으로 추가하지 않는다.

Preview In Play Mode도 켤 때마다 진입 단계를 재생한다. 다시 보려면 껐다 켠다. 시간은 게임 시간에 따라가므로 일시정지 중에는 진행하지 않는다. 중간 취소는 현재 크기로 페이드아웃하며, 사망·비활성화는 즉시 초기화한다. 색상은 장착 보주 SO를 그대로 따른다.

진입 연출 검증: 전체 C# 컴파일과 별도 Unity 렌더링을 확인했다. 실제 Visual·PlayerScreenEffects·ScreenFXController 소스로 단계 경계의 연속성, 짧은/0초 도입, 조작 복귀 시 재섬광 없음, 화면 왜곡·UI 켜짐 동기화, 발생점 고정, 취소 시 크기 유지, 미리보기 재생, 사망 정리 등 51개 확인 및 기존 색상 40개 확인을 통과했다. 검증의 체력·각성 상태는 최소 대역이며 실제 Animator 동작, 입력·조작 복귀, 게임 씬의 UI 표시와 일시정지는 Play Mode 확인이 필요하다.

## 보주 유물별 각성 색상

`Assets/ScriptableObjects/Relic/Relic_Orb/Relic_*.asset`의 **Orb Awakening Colors**에 색상을 저장한다. HUD와 인게임 보주 이미지의 색을 참고해 초기값을 지정했으며, 실행 중 이미지에서 자동 추출하지 않는다.

| SO 설정 | 역할 |
| --- | --- |
| Override Awakening Colors | 이 보주 유물의 각성 색상을 사용한다. 검·신체 유물에는 적용되지 않는다. |
| Awakening Glow Color | 캐릭터 윤곽 및 Body Glow의 색. HDR 색상 지원. |
| Awakening Aura Color | 발밑에서 퍼지는 부채꼴 오라의 색. HDR 색상 지원. |
| Awakening Light Color | 주변 지형을 비추는 작은 조명의 색. |

| 유물 | 초기 색상 방향 |
| --- | --- |
| 폭주 / Berserk | HUD의 푸른 테두리를 따른 윤곽 + 인게임 보주의 붉은 눈을 따른 오라 |
| 연소 / Burning | 인게임 보주의 눈 색을 따른 민트·청록색 |
| 그믐 / DarkMoon | HUD·인게임 보주의 보라색 |
| 만월 / FullMoon | HUD·인게임 보주의 금빛·따뜻한 노랑 |
| 환류 / Reflux | HUD·인게임 보주의 파랑·푸른빛 |

Player의 **Use Equipped Orb Colors**가 켜져 있고 장착한 보주 SO의 **Override Awakening Colors**가 켜져 있을 때 SO의 세 색이 우선한다. 획득만 한 유물은 적용하지 않는다. 유물 교체 및 SO 색상 편집은 매 프레임 반영된다. 밝기·농도·윤곽 두께·흐름 속도는 기존 Player 컴포넌트에서 조절한다.

보주 미장착, SO의 색상 설정 꺼짐, Player의 Use Equipped Orb Colors 꺼짐, 유물 관리자 없음 상태에서는 Player 컴포넌트의 Glow Color / Aura Color / Light Color를 사용한다. SO 색상을 읽을 때 이 기본값을 덮어쓰지 않으므로 유물을 해제하면 원래 색으로 돌아간다. 기본색을 직접 비교하려면 Use Equipped Orb Colors를 잠시 끈다.

이 설정은 캐릭터 윤곽·오라·주변 조명에 적용된다. HUD 자체의 색상은 기존 Orb HUD 설정을 사용하며, 화면 가장자리 UI 및 별도 일회성 AwakeningEffect의 색상은 이 세 항목에 연결되어 있지 않다. Bloom은 발광하는 색을 번지게 하므로 오라·윤곽 색 변경이 자연스럽게 반영된다.

검증: 전체 게임 C# 소스 컴파일 통과. 별도 Unity 프로젝트에서 실제 RelicData·OrbHUDStyle·PlayerAwakeningVisual 소스와 보주 SO 5개를 사용해 5종 렌더링, 각 렌더러/조명의 색상 전달, 해제·설정 해제·관리자 없음의 기본색 복귀, SO 편집 즉시 반영, 기본 머티리얼·색 보존, 사망 시 숨김 등 40개 확인을 통과했다. 이 검증의 장착 상태·체력·각성 상태는 최소 대역으로 제공했으므로 실제 게임의 인벤토리·각성·리스폰 연동은 Play Mode 확인이 필요하다.

## 오라 초안 조절

같은 컴포넌트의 **Flowing Aura**를 조절한다. 발밑 한 점에서 위쪽 부채꼴로 노이즈를 흘리는 가산 셰이더이며 별도 이미지나 프레임은 사용하지 않는다. 가운데 흐름은 위로, 양옆 흐름은 대각선으로 퍼진다. 캐릭터와 같은 Sorting Layer에서 Order를 하나 낮춰 몸 뒤에 표시한다. 방향마다 길이를 다르게 하고 끝을 흐리게 없애 단단한 부채꼴 테두리가 보이지 않게 했다.

| 항목 | 기본값 | 용도 |
| --- | --- | --- |
| Enable Aura | 켜짐 | 오라만 켜고 끄며 윤곽 효과와 비교 |
| Aura Material | M_PlayerAwakeningAura | 프리팹에 연결됨. 평소 조절은 아래 컴포넌트 값을 사용한다. |
| Aura Color / Intensity | 푸른색 / 2.5 | 오라 색상 / 발광 밝기 |
| Aura Opacity | 0.4 | 전체 농도 |
| Aura Size | (5.4, 4.2) | 표시 영역의 폭 / 최대 도달 거리. Player 루트의 로컬 단위. 퍼짐은 Fan Angle로 조절하며, 폭이 너무 작으면 양옆이 잘릴 수 있다. |
| Aura Offset | (-0.15, -1.65) | Player 루트 기준 발밑 발생점. 이전 중심 위치 방식에서 발생점 기준으로 변경했다. |
| Aura Fan Angle | 80도 | 부채꼴 전체 각도. 높이면 양옆으로 더 넓게 퍼진다. |
| Aura Stream Count | 7 | 기운 흐름의 촘촘함. 일부는 노이즈와 가장자리 페이드로 약하게 보인다. |
| Aura Rise Speed | 0.65 | 발생점에서 바깥으로 퍼지는 속도. 0이면 흐름 정지 |
| Aura Noise Scale | 5 | 무늬의 촘촘함. 높이면 더 작은 기운이 생긴다. |
| Aura Turbulence | 0.65 | 흐름의 좌우 일렁임 |

**미리보기:** 에디터의 Play 모드에서 Player의 **Preview In Play Mode**를 켠다. 기력이나 능력치 변경 없이 발광·오라·작은 조명·Bloom만 표시한다. 끄면 실제 각성 상태를 따른다. 사망 중에는 미리보기도 숨는다. 이 옵션은 에디터 전용이고 빌드에는 포함되지 않는다.

먼저 Aura Offset을 발밑에 맞추고, Fan Angle과 Aura Size의 Y로 퍼짐과 높이를 조절한 뒤 Opacity/Intensity, Rise Speed 순서로 맞춘다. 공중에서도 발생점은 플레이어를 따라간다. 지면을 탐색하거나 바닥에 고정하지 않는다. 좌우 반전과 포즈 변경에도 발생점은 안정적으로 유지된다. 씬 인스턴스에 이전 Aura Offset override가 남아 있으면 새 기준에 맞춰 변경하거나 해당 override를 되돌린다.

Play 모드에서 바꾼 값은 종료하면 돌아가므로 필요한 값을 기록하거나 컴포넌트 값을 복사해 종료 후 원본 프리팹에 적용한다. 오라에는 원본의 피격 깜빡임과 투명도도 반영한다.

## 기존 시스템과의 연결

- 각성 진입의 집중·방출·안정 단계를 거쳐 유지 외형으로 이어진다. 정상 종료는 페이드아웃하며, 사망·컴포넌트 비활성화에서는 즉시 초기화한다.
- 원본 SpriteRenderer의 스프라이트, 방향, 정렬, 투명도와 enabled를 따라간다. 원본 머티리얼·색·애니메이터는 발광 컴포넌트가 변경하지 않는다. 피격 깜빡임 중 발광도 숨는다.
- 윤곽은 투명도 경계의 **안쪽**에 생성한다. Tight sprite mesh를 확장하지 않고도 동작하며, 바깥쪽의 부드러운 번짐은 Bloom이 만든다.
- `PlayerAwakening.Apply Sprite Tint`는 기본적으로 꺼 둔다. 기존 Normal/Awakened Color 값은 보존했으며, 필요할 때만 레거시 전체 색상 변경을 다시 켤 수 있다.
- 기존 `ScreenEffectsCamera`가 게임 카메라의 후처리·HDR·Default Volume 레이어를 활성화한다. 기존 타이틀 제외 규칙을 유지한다. Bloom은 이 마커가 활성화된 Game 카메라에만 적용하며 Scene View에는 적용하지 않는다.
- 각성 Bloom은 별도 런타임 프로필을 사용하므로 기존 비네트·화면 가장자리 효과 프로필을 수정하지 않는다. Bloom 자체는 화면의 밝은 픽셀에 적용되므로 다른 HDR 효과도 각성 중 번질 수 있다.
- 조명은 현재 Renderer2D의 Multiply 블렌드 스타일(0)을 사용한다. 지형이 Sprite-Lit 등 2D 조명 대응 머티리얼을 사용해야 보인다. Unlit 지형을 자동으로 교체하지 않는다. 벽 너머 빛 차단에는 해당 지형의 Shadow Caster 2D 설정이 필요하다.

## Play Mode 확인

구현 시 전체 게임 C# 소스를 Unity 참조로 컴파일했고, 별도 Unity 6000.4/URP 17.4 프로젝트에서 발광 셰이더 컴파일과 기존 idle 스프라이트 한 포즈의 Bloom 렌더링을 확인했다. 오라 추가 후에도 전체 C# 컴파일, 오라 셰이더 컴파일, 서로 다른 두 시점의 단일 포즈 렌더링을 확인했다. 아래 실제 게임 씬의 동작 검증은 별도로 필요하다.

1. 실제 게임 카메라에서 각성 진입, 유지, 에너지 소진 종료를 확인한다. 일반 상태에서 추가 발광·조명·Bloom이 남지 않아야 한다.
2. 달리기·좌우 반전·점프·공격 때 윤곽이 현재 포즈를 따르는지, 몸 뒤 오라의 위치와 크기가 포즈 변경이나 반전으로 튀지 않는지 확인한다.
3. 피격 무적 깜빡임에 발광이 혼자 남지 않는지, 얼굴·무기와 원래 색이 읽히는지 확인한다.
4. 각성 도입부/유지 중 사망, 리스폰, 재각성, 씬 전환을 확인한다.
5. Ground의 조명 대응 머티리얼, 조명 범위, Shadow Caster, 다른 밝은 효과의 Bloom 번짐을 확인한다.

기본값은 초안이며 실제 게임 화면에서 시각 조정이 필요하다. 잔상과 별도 불꽃 이미지는 이번 범위에 포함하지 않는다.
