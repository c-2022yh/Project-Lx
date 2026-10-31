# Project-Lx 효과음 전체 점검

이 문서는 현재 디스크에 저장된 C# 소스, 프리팹, 씬, 스킬·유물 에셋을 읽어 분석한 결과다. 게임 코드나 프리팹을 수정하지 않았다. Unity의 미저장 Inspector 변경, 실제 씬의 입력·재생·청음 결과는 포함하지 않는다. BGM과 UI는 요약만 하고 게임 플레이 효과음에 집중한다.

## 1. 현재 상태

- Project-Lx에서 메타 GUID를 확인한 음원은 157개다.
- `.prefab`, `.unity`, `.asset`, `.anim`의 `fileID: 8300000` AudioClip 참조는 모두 `Assets/Resources/Audio/GameAudioCatalog.asset`에만 있었다. 저장된 플레이어·적·보스 프리팹과 스킬 SO에 직접 지정된 전투 AudioClip 참조는 확인되지 않았다.
- 이 검색은 파일 참조 검사다. 플레이 중 동적 할당이나 저장되지 않은 Inspector 상태를 증명하지 않는다.
- 기존 씬·프리팹에 별도 AudioSource 또는 GameAudioEmitter가 부착된 사례도 확인되지 않았다. GameAudio의 재생용 AudioSource는 런타임에 자동 생성된다.
- 기본 공격·피격·사망·스킬·일부 적/보스 공격은 재생 코드가 있다. 이 부분은 먼저 음원을 지정하면 된다.
- 이동·각성·필드·환경·유물 고유 효과는 전용 사운드 호출이 없어서 별도 연결이 필요하다.
- 9개 스킬 SO 중 audio 설정이 파일에 명시된 것은 ScorchedEarth와 VitalConversion이다. 다른 7개도 코드에 공통 Audio 설정이 있으나 저장 파일에는 해당 블록이 없다. Inspector에서 확인 후 지정·저장해야 하며, 블록 부재를 기능 부재와 혼동하지 않는다.

### 상태와 우선순위

- **지정 가능**: 전용 재생 호출·설정이 있다. 현재 저장된 에셋에서는 전투 음원이 비어 있다.
- **추가 연결**: 동작은 있지만 그 시점의 사운드 호출·전용 설정이 없다.
- **공유/분리 필요**: 기존 칸으로 소리를 낼 수 있지만 서로 다른 시점을 구분할 수 없다.
- **선택**: 사운드 디자인 제안이다. 모든 동작에 별도 파일을 만들 필요는 없다.
- **P0**: 핵심 조작·전투 피드백. **P1**: 패턴 인지·진행·능력의 개성. **P2**: 환경·장식·선택적 정보.

## 2. 플레이어 기본 조작

근거: `Assets/Scripts/Player/PlayerMove.cs`, `Player.cs`, `PlayerHealth.cs`, `PlayerRespawn.cs`, `PlayerHitReaction.cs`.

| 동작 | 필요한 효과음 | 상태 | 우선순위·재생 기준 |
|---|---|---|---|
| 지상 이동 | 발걸음 | 추가 연결 | P0. 접지·실제 이동 중만 재생. 속도나 애니메이션 발 디딤과 맞춘다 |
| 지상 점프 | 도약 | 추가 연결 | P0. 입력 순간이 아니라 점프가 실제 성립했을 때 |
| 공중 추가 점프 | 짧은 공중 도약/바람 | 추가 연결 | P1. 코드에 공중 점프가 있으므로 지상 점프와 구분 가능 |
| 착지 | 가벼운 착지, 필요하면 강한 착지 | 추가 연결 | P0. 공중→접지 전환. 생성·리스폰 직후의 초기 접지는 제외 |
| 일반 대시 | 짧은 돌진·바람 | 추가 연결 | P0. 쿨타임·잠금 검사 통과 후 실제 대시 시작에 재생 |
| 일방통행 플랫폼 내려가기 | 필요하면 짧은 바람 | 추가 연결·선택 | P2. 착지음으로 충분하면 생략 |
| 방향 전환·일반 낙하·대시 종료 | 독립 소리는 대체로 불필요 | 선택 | 이동 중 상시 반복음을 늘리지 않는다 |
| 피격 | 캐릭터 반응/충격 | 지정 가능 | P0. PlayerHealth > Hurt Sound. 무적 중 거절된 피해에는 재생하지 않음 |
| 사망 | 사망 반응·소멸 | 지정 가능 | P0. PlayerHealth > Death Sound. 치명타격에서 사망음 사용 |
| 리스폰 | 재등장 | 추가 연결 | P1. 실제 위치 복구·조작 복귀와 맞춘다 |
| 일반 회복 | 회복 반짝임 | 추가 연결·선택 | P1. 생명전환·제단 등 회복 원인의 소리와 중복되지 않게 한다 |
| 체력 부족·스킬 사용 불가 | 경고·거절 | 추가 연결·선택 | P2. 현재 PlayerSkill은 쿨타임·상태 등 실패 시 조용히 반환. 연타 제한 필수 |

현재 발소리용 바닥 재질 분류는 확인되지 않았다. 처음에는 공통 발걸음 세트로 시작하고 실제 지형별 분류를 추가할 때 흙·돌·풀을 나눈다. 달리기/벽타기/패링처럼 현재 확인되지 않은 동작을 필수 목록에 넣지 않는다.

## 3. 기본 공격과 타격

근거: `Assets/Prefabs/Player/Player.prefab`, `Assets/Scripts/Player/PlayerAttack.cs`, `Assets/Scripts/Audio/AttackAudioContext.cs`.

| 동작 | 지정 위치/추가 지점 | 상태 | 우선순위 |
|---|---|---|---|
| 지상 1타 ‘짧게 베기’ | PlayerAttack > Ground Patterns[0] > Attack Sound | 지정 가능 | P0 |
| 지상 2타 ‘내려 치기’ | Ground Patterns[1] > Attack Sound | 지정 가능 | P0 |
| 공중 베기 | Air Attack > Attack Sound | 지정 가능 | P0 |
| 위 3종의 실제 명중 | 각 패턴 > Impact Sound | 지정 가능 | P0 |
| 황혼 유물의 추가 콤보 | TwilightRelicEffect의 thirdAttackPattern | 지정 가능 | P1. 기본 AttackPattern 경로로 들어가므로 그 패턴의 Audio 사용 |
| 치명타 | 공통 짧은 강조음 | 추가 연결·선택 | P1. 현재 별도 치명타 SoundCue 없음 |
| 살·식물·돌·껍질 등 대상 재질별 명중 | 공격음과 별도의 대상 재질 선택 | 추가 연결 | P1. 현재는 공격 패턴의 Impact Sound가 기준이며 대상 재질 자동 선택은 아님 |
| 벽·파괴 오브젝트 명중 | 해당 오브젝트의 적중/파괴 지점 | 추가 연결 | P1. 적 피해용 명중음과 별개 |

공격 휘두름음은 헛쳐도 나고, 명중음은 실제 피해가 승인됐을 때 난다. EnemyHealth의 Hurt Sound는 대상 자체의 반응이다. ‘검 충돌 + 적 반응’은 역할을 구분해서 조합하고, 두 칸에 같은 완성 타격음을 넣어 과하게 겹치지 않는다.

Impact Mode는 EveryHit / OncePerAction / OncePerTarget을 지원한다. 다단히트·다수 적을 때릴 때의 밀도는 이 설정과 Minimum Interval로 조절한다.

## 4. 스킬 9개

지정 위치: `Assets/ScriptableObjects/Skills/*.asset`의 Audio. 공통으로 Cast Sound / Release Sound / Impact Sound / Active Loop / Complete Sound / Cancel Sound가 있다. 6개를 모두 채울 필요는 없다.

| 스킬 | 최소 권장 소리 | 기존 설정으로 가능한 부분 | 추가·주의 사항 |
|---|---|---|---|
| Thrust | 찌르기 발동 + 명중 | Release / Impact | P0. 짧은 기술은 Cast를 비워 중복 방지 |
| TripleSlash | 각 베기 + 명중 | 베기마다 Release, 실제 피해 Impact | P0. 3타를 별도 음색으로 나누는 설정은 현재 없음. 마지막 타 강조를 원하면 분리 필요 |
| PiercingDash | 관통 돌진 + 관통 명중 | Release / Impact / 필요시 Active Loop | P0. PlayerMove의 일반 대시와 서로 다른 기술 |
| FireBall | 불 생성/발사 + 화염 명중 | Cast / Release / Impact / Projectile Audio > Flight Loop | P0. 벽 충돌은 비주얼만 호출하는 경로가 있어 별도 충돌음 연결 필요 |
| ProjectileShot | 발사 + 명중 | FireBall과 같은 ProjectileShotSkillData 구조, 별도 SO 설정 가능 | P1. 두 SO가 같은 음원을 공유해도 됨 |
| ShadowSwap | 그림자 설치 + 순간이동 | 두 단계 모두 현재 같은 Release Sound 호출 | P1. 설치·텔레포트 개별 음원을 원하면 단계별 칸·호출 분리 필요 |
| VitalConversion | 변환 진행 + 실제 회복 성립 | Cast / Active Loop / 회복 시 Release | P0~P1. 회복은 플레이어 애니메이션과 분리된 효과 지점에 맞추기 |
| Guard | 자세 진입 + 필요시 유지 | Release / Active Loop | P2. 현재 ProcessSkill은 자세 유지·물리 고정이며 별도 막기/패링 성공 판정은 확인되지 않음. 막기 성공음을 필수로 가정하지 않음 |
| ScorchedEarth | 준비/배열 + 연속 발사 + 연속 명중 | Cast / Active Loop / 각 탄 Release / Impact / Barrage Flight Loop | P1. 현재 코드는 모든 탄 해결 후 각성 종료. 별도 마지막 대형탄 전용 설정은 현재 구현에 없음 |

Cast는 실제 효과 전 준비음이며 Active 진입 시 정리된다. Active Loop는 액션 수명에 연결된다. 투사체 Flight Loop는 투사체 수명에 연결되어, 이미 발사한 탄이 살아 있는 동안 독립적으로 이어질 수 있다.

Complete는 벽 충돌음·폭발음이 아니다. 액션의 정상 종료음이다. 빠른 베기까지 Cast·Release·Complete를 모두 채우면 조작 한 번에 소리가 너무 많아질 수 있다. Cancel Sound는 중요한 마법이 끊긴 경우 등에 선택적으로 사용한다.

## 5. 각성·보주·기력

근거: `PlayerAwakening.cs`, `PlayerEnergy.cs`, `PlayerAwakeningVisual`, `PlayerAwakeningOrb`, `Relic_Orb` 런타임.

| 동작 | 필요한 소리 | 상태 | 우선순위 |
|---|---|---|---|
| 각성 가능 수치 도달 | 짧은 준비 완료 | 추가 연결 | P1. 임계치 미만→이상 전환에 1회. 게이지 갱신마다 재생하지 않음 |
| 각성 도입/발동 | 힘 응축→개방 | 추가 연결 | P0~P1. 도입 연출과 OnAwakeningStarted의 실제 시점을 나눠 확인 |
| 각성 유지 | 낮은 에너지 루프 | 추가 연결·선택 | P2. BGM·전투음을 가리면 생략 |
| 각성 종료 | 힘 해제·소진 | 추가 연결 | P1. 종료 이벤트 사용 가능 |
| 보주 손 이동·해방 연출 | 이동/개방 강조 | 추가 연결·선택 | P2. 각성 발동음으로 충분하면 생략 |
| 기력 소량 증가 | 작은 흡수음 | 추가 연결·선택 | P2. 다수 처치·연속 획득 시 묶기 필요 |
| Reflux의 각성 종료 후 기력 회복 | 짧은 역류/복원 | 추가 연결·선택 | P1. 각성 종료음과 중복 주의 |
| FullMoon 보너스 활성·DarkMoon 제한 | 별도 소리 생략 가능 | 선택 | 스탯·HUD 변화를 매번 소리로 표현할 필요 없음 |

EnergyOrb/FollowingOrb는 보주 표현용 컴포넌트다. 이름만 보고 필드에서 빨려 들어오는 처치 보상 오브젝트라고 단정하지 않는다. 실제 기력 증가는 PlayerEnergy.GainEnergy 및 적 보상 처리를 기준으로 연결한다.

## 6. 일반 적

현재 독립 프리팹 12종을 확인했다. 모두 EnemyHealth의 Hurt Sound / Death Sound를 사용할 수 있다. EnemyContactAttack의 Impact Sound는 플레이어에게 실제 피해를 준 충돌음이다. 모든 적에게 별도의 공격 시전음이 필요한 구조는 아니다.

| 프리팹/분류 | 필요한 소리 | 기존 지원 | 추가 연결 |
|---|---|---|---|
| Enemy_Leaf01/02/03 | 식물 피격·사망, 몸 접촉 타격 | Health + Contact | 이동 잎 스침은 선택. 3종 공통 세트 가능 |
| Enemy_Mushroom_Purple/Teal | 버섯 피격·파열, 접촉 타격 | Health + Contact | 2종 공통 세트 가능 |
| Enemy_Slime / Enemy_SapSlime | 점액 피격·파열, 접촉 타격 | Health + Contact | 이동 통통음은 실제 이동 방식 확인 후 선택 |
| Enemy_Shell | 껍질 피격·깨짐, 접촉 타격 | Health + Contact | 바닥 이동음 선택 |
| Enemy_Flying_Chase | 비행체 피격·소멸, 접촉 타격 | Health + Contact | 비행 루프는 소수·근거리만 선택적으로 |
| Enemy_Wisp | 피격·소멸, 돌진 접촉 | Health + Contact | EnemyFlyingChargeAI의 감지/준비/돌진 시작/회복에 전용 호출 없음. 준비·돌진음 P1 |
| Enemy_Flower | 피격·사망, 발사, 탄 명중 | Health + Contact + EnemyRangedAttack의 Attack/Impact/Flight Loop | 탄의 벽 충돌·소멸음은 별도 검토 |
| Enemy_Wolf | 피격·사망, 물기·돌진 | Health + Contact + EnemyWolfAttack의 Bite/Charge Attack/Impact/Active Loop | Prepare 전용 예고음은 현재 별도 칸 없음. 낮은 으르렁음은 P1 |

적의 Hurt Sound는 재질·몸의 반응 위주로 고르고, 모든 적에게 인간형 비명을 넣지 않는다. 풀링된 적은 Awake/Start 기준이 아니라 실제 공격·피격·사망 시점을 사용한다. OnEnable에 공격/등장음을 무조건 걸면 풀 재사용·보스 소환에서 의도치 않게 재생될 수 있다.

EnemyEliteAttack에는 근접 Attack/Impact와 Charge Sound/Loop 지원이 있다. 다만 위 독립 기본 적 프리팹 목록에 이 컴포넌트가 부착된 사례는 없었다. 실제 엘리트 씬 인스턴스/스폰 설정에 맞춰 별도로 적용 대상을 확인한다.

새 Pinecone 스프라이트 폴더는 있으나 이 점검 시점의 독립 적 프리팹 목록에는 Pinecone이 없었다. 아트만으로 원거리 공격 패턴이나 사운드 이벤트를 확정하지 않는다.

## 7. 골렘 보스와 보스방

근거: `Assets/Prefabs/Bosses/Boss_Golem.prefab`, `GolemBossController.cs`, `Assets/Scripts/Presentation/Boss/BossRoom.cs`.

| 동작 | 설정/소리 | 상태 | 우선순위 |
|---|---|---|---|
| Slam | Warning Sound + Impact Sound | 지정 가능 | P0~P1. 공격 준비·실제 범위 충격 |
| Crush | Warning Sound + Impact Sound | 지정 가능 | P0~P1. Slam과 구분되는 강도/톤 |
| Rockfall | Warning Sound + 각 범위 Impact Sound | 지정 가능 | P1. 낙석 여러 개의 충격 밀도 조절 |
| 골렘 피격·사망 | EnemyHealth Hurt/Death | 지정 가능 | P0~P1. 돌 균열·붕괴 |
| 두 독립 골렘으로 분열 | 균열→분열 | 추가 연결 | P1. 부모에서 한 번. 파편 생성·OnEnable에서 중복 재생하지 않기 |
| 부하 소환 | 징조/소환 | 추가 연결 | P1. 소환 한 웨이브 기준 권장 |
| 보스방 문 닫힘·열림 | 돌문·봉인 | 추가 연결 | P1. MoveDoors의 실제 이동과 동기화 |
| 보스 등장/이름 카드 | 짧은 등장 강조 | 추가 연결 | P1. UI 창열기 소리와 구분 |
| 보스 처치 확정 | 승리/해방 강조 | 추가 연결 | P1. 분열체 하나가 죽었을 때가 아니라 전체 IsDefeated 기준 |
| 보스 이동 | 무거운 발 디딤 | 추가 연결·선택 | P2. 소형 분열체와 크기·음량 차이 검토 |

보스의 Impact Sound는 적을 맞혔을 때만 나는 피해음과 다르다. 범위 공격 충격이 발생할 때 재생되어, 플레이어가 피했어도 들릴 수 있다. 두 소리의 역할을 유지한다.

## 8. 유물 고유 전투 효과

장착 UI 소리와 실제 능력 발동 소리는 별개다. 아래는 모두 전용 음원/발동 호출이 없거나 세부 분리가 필요하다.

| 효과 | 필요한 소리 | 우선순위·기준 |
|---|---|---|
| Thunder 연쇄 번개 | 최초 방전 + 필요시 작은 연쇄 타격 | P1. 적 수만큼 동일 큰 번개음을 겹치지 않기 |
| DarkShadow 처형 | 처형 확정 강조 | P1. 일반 명중·적 사망과 짧게 조합 |
| Dawn 추가타 | 지연 추가 베기 | P1. 추가 타격이 실제 생성되는 지점. 현재 기본 패턴의 Audio가 자동 재생된다고 가정하지 않음 |
| Twilight 추가 콤보 | 강한 추가 베기 | P1. 추가 AttackPattern에 기존 Attack/Impact 설정 사용 가능 |
| Devour 타격 회복 | 흡수 | P2. 매 명중마다 큰 회복음 금지 |
| ResidualMoon 처치 후 대시 쿨타임 초기화 | 짧은 리셋 알림 | P2. 필요하면 전용 정보음 |
| Regeneration 지속 회복 | 약한 회복 | P2. 주기마다 울리면 피로감이 커 생략 가능 |
| FeatherCloud 추가 공중 점프 | 공중 도약 변주 | P1. PlayerMove의 실제 공중 점프와 묶기 |
| WindShoes/StatRelic 등 상시 수치 보너스 | 별도 발동음 불필요 | 장착음·움직임 표현으로 충분 |
| Berserk의 ScorchedEarth | 스킬 항목 참조 | 같은 발동을 유물과 스킬 양쪽에서 중복 재생하지 않기 |

Thunder·Dawn 등 직접 DamageInfo를 만드는 유물 효과는 기본 공격의 Impact Audio Context를 자동 상속하지 않는다. 기존 효과의 실제 생성·피해 경로를 기준으로 연결해야 한다.

## 9. 필드·환경·위험물

| 동작 | 필요한 소리 | 상태·우선순위 |
|---|---|---|
| 제단 최초 활성화 | 활성화/봉인 해제 | 추가 연결, P1 |
| 이미 활성화된 제단에서 회복 | 짧은 회복 | 추가 연결, P1. UI 창 열기와 역할 구분 |
| 포탈/자동 포탈 진입 | 이동/전이 | 추가 연결, P1. 씬 로드로 로컬 AudioSource가 즉시 사라지지 않도록 기존 지속 GameAudio 활용 |
| 고정 보상 상자 열기 | 실제 상자 개방 | 추가 연결 검토, P1. UIChestOpen은 현재 유물 선택창이 열린 경우에 연결되어 고정 보상 경로와 다름 |
| 유물 선택 상자 | 선택창 등장음 | 기존 UIChestOpen, 지정됨. 상자 물리음까지 추가할 경우 중복 여부 결정 |
| 보주 직접 획득 OrbPickup | 획득/힘 개방 | 전용 필드음은 추가 연결, P1. 획득 알림 UIAcquire와 중복 구분 |
| BreakableBrick | 손상·파괴 | 추가 연결, P1 |
| FireBreakableWall | 화염 반응·붕괴 | 추가 연결, P1. 화염구 벽 충돌음과 같은 사건 중복 주의 |
| SecretBreakableWall | 깨짐·비밀 발견 | 추가 연결, P1 |
| HiddenAreaCover 진입 | 비밀 발견 강조 | 추가 연결·선택, P2. 단순 가림막 페이드와 진짜 최초 발견을 구분 |
| 가시 SpikeDamageZone | 필요시 물리 충격 | 추가 연결·선택, P1. PlayerHealth 피격음은 기존 경로 사용 가능 |
| PoisonGas_Zone 표현 | 가스 루프 | 추가 연결·선택, P2. 프리팹 이름만으로 독 상태 시스템이 구현됐다고 가정하지 않음 |
| 움직이는 플랫폼 | 구동·정지 | 추가 연결·선택, P2 |
| 풀/잎 접촉 | 잎 스침 | FoliageRustle/GrassStepLeaves는 현재 시각효과 중심. 추가 연결·선택, P2 |
| 숲·동굴 공간 | 바람·새·곤충·낙수 등 배경 환경음 | 추가 연결, P2. BGM과 별도 역할 |

## 10. 구매한 사운드 라이브러리의 활용 범위

프로젝트: `C:/Users/Ayumi/Desktop/Argo/SoundAssetTest_`

음원 루트: `Assets/Book of the Dead - URP/New Content/Audio/Battle Sound Library`

전체 프로젝트 WAV는 3,800개다. 2,925개가 Battle 조합본, 218개가 Magic 조합본이며 원본 레이어·모노/스테레오 버전도 있어, 서로 완전히 다른 3,800종의 상황음으로 해석하면 안 된다. 아래 후보는 폴더 의미에 근거하며 청음 평가를 하지 않았다.

| 프로젝트 용도 | 라이브러리 후보 폴더 |
|---|---|
| 검 명중 | Battle/Sword, _Combined Clips (Battle)/Sword/OnFlesh·OnMetal·OnWood·Impacts |
| 휘두름·대시 | Magic (Mono)/Generic/Whoosh. 무기 원본 레이어는 직접 듣고 단독 휘두름으로 쓸 수 있는지 확인 |
| 화염구 | Magic (Mono 또는 Stereo)/Fire, _Combined Clips (Magic)/Fire/Hits·Explosions |
| 그림자·어둠 | Magic/EvilDark 계열 |
| 회복·각성·버프 | Magic/HolyLight, Generic/Buff·Humming & Pulsing, _Combined Clips (Magic)/Buffs |
| 골렘 공격·돌 충격 | Magic/Earth 계열, 필요시 Battle/Breaks 조합 |
| 연쇄 번개 | Magic/Electric 계열 |
| 스킬 실패·소멸 | Generic/Spell Fail·Spell Fizzle |
| 투사체·관통 | Generic/Lance, Bow & Arrow 계열 |

발걸음·점프·착지, 캐릭터/적 음성, 숲·동굴 환경음 전용 세트는 이번 목록에서 확인되지 않았다. Generic Whoosh 등을 변형해서 일부 대응할 수 있지만, 적합성을 청음 전 확정하지 않는다. 필요한 WAV와 메타/임포트 설정만 선별해 가져오면 전용 에디터·데모·전투 조합기를 런타임 필수 의존성으로 도입할 필요는 없다.

## 11. 소리를 붙일 때 같이 확인할 사항

1. **핵심은 새 시스템보다 전투 에셋 지정부터다.** PlayerAttack·PlayerHealth·Skill SO·EnemyHealth·공격 패턴의 기존 칸을 우선 채운다.
2. **다단히트 밀도:** ScorchedEarth는 여러 탄 발사와 비행 루프를 지원한다. 현재 SFX Voice는 16개, UI Voice는 4개다. 탄마다 긴 루프를 붙이면 명중/피격음이 보이스 부족으로 생략될 수 있다. 긴 루프보다 짧은 발사/명중을 우선하고 제한을 조절한다.
3. **2D 거리감:** SoundCue의 Spatial Blend 기본값은 0이다. 먼 적 소리도 동일 음량일 수 있다. 거리 감쇠를 켤 때는 AudioListener의 카메라 Z 거리도 함께 검토한다.
4. **겹침:** 공격 Impact / EnemyHealth Hurt / Death / 치명타 강조를 모두 최대 음량으로 채우지 않는다. 어떤 층이 주된 타격감인지 정한다.
5. **승인 시점:** 실패한 점프/대시·스킬 입력, 무적 피격, 0 피해에는 성공/명중음을 붙이지 않는다.
6. **종료 수명:** 액션 취소는 준비·액션 루프를 정리하고, 이미 발사한 투사체 소리는 그 투사체 수명에 맞춘다.
7. **재질 분기:** 현재 기본 Impact는 공격자가 고른다. 적 재질별 사운드는 추가 구현 사항이다.
8. **카탈로그 중복 발견:** 현재 Sounds에 UIReject가 두 개다. GameAudioCatalog.Find는 첫 일치 항목을 사용하므로 뒤쪽 UIReject를 수정해도 실제 재생이 바뀌지 않을 수 있다. 이번 분석에서는 수정하지 않았다.
9. **공통 구형 슬롯:** GameSound.PlayerAttack/PlayerHurt/EnemyHurt 같은 enum 항목은 남아 있지만 현재 전투는 로컬 SoundCue 위주다. 카탈로그만 채운다고 모든 전투 효과음이 연결되는 것은 아니다. Use Fallback을 명시적으로 켠 경우만 대체한다.

## 12. 권장 진행 순서

### 1차: 플레이어 조작·전투

지상 1·2타/공중 휘두름, 공통 검 명중, 플레이어 피격·사망, 기본 대시, 점프·착지. 약 9~12개 용도의 중심 소리를 먼저 고르고, 발걸음·명중에는 반복감 방지용 변주를 추가한다. 이 수는 제작 제안이며 현재 자산 수가 아니다.

### 2차: 주력 스킬

Thrust·TripleSlash·PiercingDash·FireBall부터 발동/명중을 지정한다. ShadowSwap은 설치/전이를 나눌지 결정하고, VitalConversion은 회복 성립음, ScorchedEarth는 연속음 밀도를 조정한다.

### 3차: 적과 골렘

각 적의 피격·사망을 재질/계열별 공용 세트로 채운다. Flower 발사, Wolf 물기·돌진, Wisp 돌진 예고를 채우고 골렘 3패턴 예고/충격·분열·처치·보스방 봉인을 추가한다.

### 4차: 필드·능력·환경

제단·포탈·상자·파괴물, 번개·처형·추가타, 각성 도입/종료, 숲·동굴 환경음을 순서대로 채운다. 단순 스탯 변경·잔잔한 지속 회복·모든 게이지 갱신은 필요성이 확인될 때만 추가한다.

### 실제 검증

Player 이동→헛공격→단일 적→다수 적→치명타→사망, 스킬 취소와 이미 발사한 탄,
제단·포탈·상자, 골렘 분열체 두 개와 부하 동시 전투, 일시정지·SFX 음소거·씬 전환을 Play Mode에서 확인한다.
이번 작업은 분석이므로 새 코드 컴파일/Play Mode 검증을 수행하지 않았다.

원본 참조 인벤토리: `audio-references.json`.
