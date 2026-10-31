# 유물·화염구·그림자·적·필드 SFX 설정 및 리뷰

신규 음원 칸은 비워두었습니다. 모든 칸의 기본 Channel은 SFX입니다.
Clip 하나 또는 Clip을 비우고 Clips의 랜덤 변주 목록을 사용합니다.
Volume / Minimum Interval / Max Simultaneous로 소리 크기와 연속 재생 밀도를 조절합니다.

## 유물: 장착 UI와 실제 발동음의 구분

Relic_* 데이터가 아니라 연결된 **RFX_* 효과 SO**에서 지정합니다.
각 SO 설정은 플레이어별 Runtime으로 전달하며, 공유 SO에 플레이어의 재생 상태를 저장하지 않습니다.

| 효과 SO | Audio 항목 | 재생 조건 |
|---|---|---|
| RFX_Thunder | Proc Sound / Impact Sound | 치명타로 연쇄 번개가 발동할 때 1회 / 실제 피해 승인된 전이 대상별 명중 |
| RFX_DarkShadow | Execute Sound | 처형 조건 통과 후 실제 사망했을 때 |
| RFX_Dawn | Extra Attack Sound / Extra Impact Sound | 지연 추가타가 유효한 히트박스로 만들어질 때 / 추가타의 실제 명중 |
| RFX_Devour | Heal Sound | 기본 공격으로 실제 체력이 증가했을 때 |
| RFX_ResidualMoon | Cooldown Reset Sound | 처치 후 X 대시 스킬의 남은 쿨타임을 실제 초기화할 때. 이미 0이면 무음 |
| RFX_Reflux | Recovery Sound | 살아 있는 상태에서 각성 종료 후 실제 기력이 증가했을 때 |
| RFX_WhiteFlower | Heal Sound | 재생 효과의 주기적 회복으로 실제 체력이 증가했을 때 |

Twilight의 추가 콤보는 기존 Third Attack Pattern의 Attack Sound / Impact Sound를 사용합니다.
Berserk가 발동하는 ScorchedEarth도 기존 스킬 Audio를 사용하므로 같은 사건의 소리를 유물 쪽에서 추가로 겹치지 않았습니다.
FullMoon·DarkMoon·이동/능력치 보너스처럼 상시 수치만 바꾸는 효과에는 임의의 발동음 조건을 만들지 않았습니다.

## 그림자 교체

**ShadowSwap 스킬 SO > Audio**의 Place Sound와 Teleport Sound를 각각 지정합니다.

- Place Sound: 그림자가 성공적으로 설치될 때.
- Teleport Sound: 설치 위치로 실제 이동했을 때.
- 두 단계는 공통 Release Sound 대신 위의 전용 칸을 사용합니다. 공통 Cast / Active Loop / Complete / Cancel 구조는 유지합니다.
- 설치 비용, 재사용 대기, 무료 이동, 이동 후 본 쿨타임 규칙은 바꾸지 않았습니다.

## 화염구

**FireBall 스킬 SO > Projectile Audio**에서 Flight Loop / Terrain Impact Sound / Expire Sound를 지정합니다.
시전·발사는 공통 Cast / Release, 적 피해 명중은 공통 Impact Sound를 계속 사용합니다.

- Terrain Impact Sound: 화염 전용 벽 또는 비밀벽에 닿아 소멸할 때.
- Expire Sound: 최대 사거리에서 소멸할 때. 적·벽 명중이나 외부 비활성화를 사거리 소멸로 취급하지 않습니다.
- 실제 제거가 Unity 프레임 끝으로 지연되어도 동일 화염구가 여러 콜라이더·두 컴포넌트에서 중복 처리되지 않게 했습니다.
- 비행 루프는 적중/벽 충돌/사거리 소멸/비활성화 때 정리됩니다. 충돌 단발음은 투사체가 삭제되어도 이어집니다.
- **Terrain Layers는 기본 Nothing**입니다. 일반 지형에 대한 기존 관통 동작을 보존했습니다.
- 일반 Ground에도 화염구가 막혀야 한다면 Terrain Layers에 Ground를 넣습니다. 트리거 영역은 일반 지형 충돌로 취급하지 않습니다.
- 다른 투사체 SO에는 새 음원을 자동 배정하거나 Ground 충돌을 켜지 않았습니다. ProjectileShot 에셋도 삭제하지 않았습니다.

## 나방으로 교체할 비행 돌진 적

**EnemyFlyingChargeAI > Audio**에 Prepare Sound / Charge Sound / Charge Loop가 있습니다.
준비 시작, 실제 돌진 시작, 돌진 중 루프를 각각 담당합니다.
회복·넉백/행동 중단·사망·비활성화에서 소리를 정리하고, 일시정지 동안도 돌진 소리를 정리합니다.

현재 독립 나방 프리팹은 없고 Enemy_Wisp가 이 공용 코드를 사용하므로, 기존 프리팹에 빈 설정을 추가했습니다.
나방 아트 교체·Animator/콜라이더·스포너 연결·Wisp 프리팹 삭제는 이번에 하지 않았습니다.
외형을 교체해 이 AI를 유지하면 동일한 사운드 칸을 사용할 수 있습니다.

## 골렘과 보스방

| 위치 | 항목 | 재생 시점 |
|---|---|---|
| Boss_Golem > GolemBossController | Split Sound | 두 분열체 생성이 성공한 뒤 부모에서 1회 |
| 위와 같음 | Summon Prepare Sound | 유효한 소환 위치가 있어 웨이브 준비를 시작할 때 |
| 위와 같음 | Summon Sound | 실제 부하가 생성된 웨이브에 1회 |
| 씬의 BossRoom > Audio | Intro Sound | 보스 이름 카드 등장 |
| 위와 같음 | Door Close Sound | 실제 문 닫기 이동이 끝났을 때 |
| 위와 같음 | Door Open Sound | 실제 문 열기 시작 |
| 위와 같음 | Clear Sound | 기존 전체 보스 처치 판정으로 방 클리어가 확정될 때 |

기존 Slam/Crush/Rockfall 예고·충격음은 유지합니다. 문이 없으면 문 소리도 없습니다.
분열체 각각이 죽을 때 Clear Sound를 내지 않습니다. 저장된 보스방 클리어 상태 복원에도 소리가 없습니다.
BossRoom은 씬 컴포넌트에서 직접 음원을 지정합니다. 이번에 씬 파일을 일괄 수정하지 않았습니다.

## 나머지 필드

| 위치 | 항목 | 재생 조건 |
|---|---|---|
| OrbPickup > Audio | Acquire Sound | 최초 보주 지급이 실제 성공했을 때 |
| RelicChest > Audio | Fixed Open Sound | 고정 보상 상자에서 유물 지급 성공. 선택형 상자는 기존 UIChestOpen 사용 |
| MovingPlatform > Audio | Movement Loop | 실제 이동 중만 반복. 대기·일시정지·비활성화 시 정리 |
| 위와 같음 | Turn Sound | 첫 출발을 제외한 왕복 방향 전환 시작 |
| FoliageRustle > Audio | Rustle Sound | 플레이어 속도·기존 쿨타임을 통과해 풀/덤불을 스칠 때 |
| HiddenAreaCover > Audio | Reveal Sound | 가림막이 숨김→드러남으로 전환될 때 |
| SpikeDamageZone > Audio | Damage Sound | 실제 플레이어 체력이 감소한 경우만. 독가스 프리팹도 이 컴포넌트 사용 |

보주/상자/숨은 구역의 단발음은 원본 오브젝트 수명에 묶지 않습니다.
회복·스킬·UI 획득 알림과 별도 소리이므로 같은 완성 효과음을 여러 곳에 겹치지 않게 음량을 조절합니다.
움직이는 플랫폼의 이동음은 Loop용 파일을 넣습니다. 현재 이동 경로·속도·탑승자 이동 처리는 바꾸지 않았습니다.

### 숨은 구역 의미

Play Reveal Once Per Instance는 기본 켜짐입니다. 같은 인스턴스에서 반복 출입할 때 매번 발견음을 내지 않습니다.
이 옵션을 끄면 재진입마다 재생할 수 있습니다.
**세이브 전체에서 영구적인 ‘최초 발견’ 여부를 새로 저장하지는 않았습니다.** 씬을 다시 로드해 새 인스턴스가 생기면 다시 재생될 수 있습니다.

## 환경음과 이번에 제외한 부분

숲·동굴·가스의 지속 환경음은 기존 GameAudioEmitter로 설정할 수 있습니다.
Use Custom Sound / Custom Sound > Clip / Loop / Play On Enable을 지정하고, 공간용 음량·거리 감쇠를 맞춥니다.
지역 경계·어느 환경음 파일을 쓸지 확정되지 않아 씬에 환경음 오브젝트를 임의 배치하거나 지역 간 크로스페이드를 추가하지 않았습니다.

명확하지 않아 보존/제외한 사항:

1. 나방의 실제 프리팹·Animator·스포너 교체와 Wisp 삭제.
2. 화염구가 일반 Ground를 관통할지 막힐지: 기본 기존 관통 유지, Terrain Layers로 선택.
3. 상시 수치형 유물의 임의 발동음, HUD 장식 애니메이션의 반복음을 능력 발동음으로 취급하는 것.
4. 숨은 구역의 영구 최초 발견 세이브 규칙, 구역별 환경음 자동 배치·전환.

## 카탈로그 정리

UIReject 두 항목 중 실제 사용되던 첫 번째 항목의 Clip과 모든 설정을 그대로 보존했습니다.
뒤쪽 중복만 삭제했으며 나머지 Sounds 및 Scene Music 설정은 유지했습니다.

## 리뷰·검증

- 실제 Unity 컴파일 입력 목록과 Unity 참조를 사용한 별도 전체 C# 컴파일 통과.
- 실제 ProjectileHitbox/FireballProjectile 소스를 사용하는 격리 하네스 15개 검사 통과: 두 콜백 순서, 다중 콜라이더, 적/벽/사거리 구분, 루프 정리, 일반 지형 기본 관통/선택 충돌.
- 카탈로그 첫 UIReject·나머지 설정 보존 및 중복 제거 검사 통과.
- 19개 프리팹/SO에 빈 SFX 설정 28개 추가. 기존 AudioClip 참조 보존 검사 수행.
- 재질·외형·패턴·피해량·스킬 비용/쿨타임·소환 개수·플랫폼 경로는 이번 작업에서 변경하지 않았습니다.
- 위 검사는 Unity Play Mode 또는 청음 검증이 아닙니다.

Play Mode 우선 확인: 나방 교체 대상의 준비/돌진·넉백/사망/풀 재사용, 유물 발동 후 해제·다수 적,
화염구의 화염벽·비밀벽·사거리 소멸, 그림자 설치·무료 이동, 보주·고정 상자 중복 상호작용,
움직이는 발판 대기/왕복/일시정지, 풀 스침과 숨은 구역 재진입, 독가스 무적 중 피격,
골렘 분열·소환·두 분열체 최종 처치·보스방 재도전/저장 복원.
