---
# 3D 16일차 하늘섬 외곽 스테이지

---
## 배치와 진행

`OuterIslandsStage.unity`를 Windows 첫 씬으로 등록했다. 기존 `FlightPrototype.unity`는 비행·탄막 연습용으로 등록했다. 외곽 항로는 기존 조작, HUD, 점수와 드롭, 적의 패턴·이동을 사용한다.

전투 시계 기준 준비 0:00~0:20, 첫 일반 전투 0:20~1:05, 드롭 회수 1:05~1:20으로 구성했다. 0:03에 조준형 한 마리가 출현하고, 0:20·0:32·0:44·0:55에 조준형과 확산형을 조합한 일반 웨이브가 시작된다. 이전 적이 남아 있을 때에는 다음 배치를 기다린다. 중간 보스 전 진입 지점에서 이번 시험 항로가 끝난다. 중간 보스와 이후 일반 전투는 17일차 범위다.

`FlightVolume`에서 비행 체적, `SpawnPoints`에서 각 적의 진입·공격·이탈 위치, `EnemyEncounter`에서 웨이브의 시각과 구성, `CloudLayers`에서 구름 배치와 이동을 편집한다. 기존 물리 지형과 거리별 원경 섬, 구름, 대기 안개로 항로의 깊이를 표현했다. 구름은 내장 이미지 생성 도구에서 제작했다. [투명 구름 에셋](../../Assets/ProjectG/OuterIslands/OuterCloudBank.png)

> A single horizontal cloud-bank sprite for an aerial fantasy 3D video game, isolated on a truly transparent alpha background. A wide long horizontal bank of natural high-altitude soft cumulus cloud, gentle layered wisps with optical density toward the middle and deeply feathered mist at edges. Pale blue-grey shaded body, muted warm ivory on upper rims, realistic atmosphere and soft painterly game textures, calm daylight. Aspect ratio around 3:1 cloud footprint inside a landscape transparent canvas, margin transparent on all edges. Only the one cloud bank, no terrain, no sky color, no horizon line, no items, no text, no border. Lower contrast, suitable for layering behind brightly colored flying projectiles. The output is a texture sprite with transparency, not a photo of a sky.

---
## 시계와 검증

상단의 큰 시계는 실제 전투 진행 시간이다. `전체`는 정지 및 레벨업 선택에 걸린 시간도 포함한다. 항로가 완료되거나 플레이어가 실패·포기하면 시간을 고정한다. 재출격하면 두 시계와 웨이브가 초기화된다.

Unity 6000.3.21f1 Play Mode 41개 통과. 자동 출현과 종료, 재출격, 일시정지와 레벨업 카드 선택의 시간 분리, 비행 체적 경계와 두 구름층의 이동을 검증했다.

Windows 1920×1080·1024×576에서 시작 씬과 자동 웨이브, 실제 적탄, 두 시간의 차이, 구름 이동을 확인했다. [1920×1080](../screenshots/3d-day-16-flight.png) · [1024×576](../screenshots/3d-day-16-hud-1024.png)
