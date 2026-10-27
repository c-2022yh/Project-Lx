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
| Entry Peak / Settle | 1.8배 / 0.35초 | 각성 진입 완료 직후 섬광과 안정화 |
| Pulse Amount / Frequency | 0.1 / 0.8Hz | 유지 중 밝기 맥동 |
| Light Color / Intensity / Radius | 푸른색 / 0.8 / 2.5 | 주변에 닿는 조명 |
| Light Offset | (0, -0.3) | 현재 포즈의 렌더러 중심에 대한 월드 좌표 오프셋 |
| Light Sorting Layers | Ground | 빛을 받을 **Sorting Layer 이름** 목록 |
| Enable Bloom | 켜짐 | 각성 중 Bloom 사용 |
| Bloom Intensity / Threshold / Scatter | 0.6 / 1.1 / 0.65 | 빛 번짐 강도 / 밝기 임계값 / 퍼짐 |

씬의 Player 인스턴스에 동일 항목의 override가 있으면 해당 값이 우선된다. 조명 색상은 Glow Color와 별도로 조절한다.

## 오라 초안 조절

같은 컴포넌트의 **Flowing Aura**를 조절한다. 노이즈를 위로 흘리는 가산 셰이더이며 별도 이미지나 프레임은 사용하지 않는다. 캐릭터와 같은 Sorting Layer에서 Order를 하나 낮춰 몸 뒤에 표시한다. 가장자리는 투명하게 없애고 중심은 약하게 비워 두었다.

| 항목 | 기본값 | 용도 |
| --- | --- | --- |
| Enable Aura | 켜짐 | 오라만 켜고 끄며 윤곽 효과와 비교 |
| Aura Material | M_PlayerAwakeningAura | 프리팹에 연결됨. 평소 조절은 아래 컴포넌트 값을 사용한다. |
| Aura Color / Intensity | 푸른색 / 2.5 | 오라 색상 / 발광 밝기 |
| Aura Opacity | 0.4 | 전체 농도 |
| Aura Size | (3, 4.2) | 폭 / 높이. Player 루트의 로컬 단위이며 포즈에 따라 변하지 않는다. |
| Aura Offset | (-0.2, 0.15) | Player 루트 기준 중심 위치 |
| Aura Rise Speed | 0.65 | 위로 흐르는 속도. 0이면 흐름 정지 |
| Aura Noise Scale | 5 | 무늬의 촘촘함. 높이면 더 작은 기운이 생긴다. |
| Aura Turbulence | 0.65 | 흐름의 좌우 일렁임 |

**미리보기:** 에디터의 Play 모드에서 Player의 **Preview In Play Mode**를 켠다. 기력이나 능력치 변경 없이 발광·오라·작은 조명·Bloom만 표시한다. 끄면 실제 각성 상태를 따른다. 사망 중에는 미리보기도 숨는다. 이 옵션은 에디터 전용이고 빌드에는 포함되지 않는다.

먼저 Aura Size/Offset으로 몸에 맞추고, Opacity/Intensity, Rise Speed 순서로 조절하면 된다. Play 모드에서 바꾼 값은 종료하면 돌아가므로 필요한 값을 기록하거나 컴포넌트 값을 복사해 종료 후 원본 프리팹에 적용한다. 오라에는 원본의 피격 깜빡임과 투명도도 반영한다.

## 기존 시스템과의 연결

- `IsAwakening` 진입부터 빛이 차오르고, `IsAwakened` 전환에 짧은 밝기 피크를 준다. 정상 종료는 페이드아웃하며, 사망·컴포넌트 비활성화에서는 즉시 초기화한다.
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
