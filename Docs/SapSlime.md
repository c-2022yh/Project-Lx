# 수액 슬라임

- 바로 배치: `Assets/Prefabs/Enemies/Enemy_SapSlime.prefab`을 씬에 드래그한다.
- PNG: `Assets/Sprites/Enemies/SapSlime/`. 640x640, Sprite Single, Center Pivot, PPU 240, 알파 투명, 압축 없음.
- 대기: `SapSlime_Idle_1, 2, 1, 3`, 각 0.4초. 기존 슬라임과 같은 Walk Sprites 슬롯을 사용한다.
- 사망: `SapSlime_Death_1, 2, 3`, 각 0.12초, 마지막 유지 0.08초.
- 기존 프리팹에 수동 적용할 때는 루트 SpriteRenderer에 Idle_1, Enemy Animation의 Walk/Death Sprites에 위 순서대로 넣고 Sprite Faces Right를 켠다.
- 신규 프리팹은 기존 Enemy_Slime을 복제하여 이미지 참조와 원본 방향만 교체했다. 체력, 피해, 보상, 몸통/접촉 판정, 넉백, 체력바, 물리 설정과 재생 시간은 기존 값이다.
- 기존 씬, 풀/스포너 목록, 기존 슬라임은 교체하지 않았다. 자동 스폰하려면 EnemyPooler의 해당 슬롯을 새 프리팹으로 교체한다.
- 이미지 크기/알파와 여섯 GUID 연결을 파일 기준으로 확인했다. Unity 에디터 임포트, 대기/사망 전환과 재활성화, 실제 지면 정렬/판정 범위는 Play Mode 확인이 필요하다. 낮아진 아트와 기존 높이의 콜라이더가 맞는지도 확인한다.

이미지 생성: 내장 image_gen. 원본 견본과 기존 슬라임의 눌림/팽창/터짐을 참조했다. 프롬프트 기록과 밝은 배경의 미리보기는 `output/imagegen/SapSlime-concepts/`에 있다.
