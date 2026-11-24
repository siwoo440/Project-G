---
# 3D 17일차 중간 보스와 하늘섬 아래의 지표

---
## 중간 보스

첫 일반 전투와 드롭 회수 뒤 전투 시간 1:20에 등장한다. 기본 시험값은 체력 600과 탄막 피해 10이다. 보스는 조준 방향을 한 패턴 동안 유지하며 직선 4발과 부채탄 2회를 교대한다. 수평 조준과 제한된 탄막 고도로 상하 회피 공간을 둔다. 공격 종료 뒤 가슴의 약점과 충돌 판정을 1초 동안 열어 준다. 시험 약점은 피해를 50% 늘리며 캐릭터의 약점 보너스가 더 높으면 그 값을 사용한다.

격파하면 경험치 30과 필살기 에너지 10을 지급한다. 유효한 강화 후보가 있으면 아이템 하나를 떨어뜨린다. 보스가 살아 있는 동안 시간만으로 후속 전투가 시작되지 않는다. 실제 격파 시각을 기준으로 4·16·28·40초에 후속 일반 웨이브가 시작되며 돌진형이 합류한다. 최종 보스 진입 전까지 후속 전투 50초와 회수 15초를 사용한다.

보스 외형은 기존 조류형 구조에 자주색 날개와 금속 장식을 붙인 시험 모델이다. 이름과 정식 외형은 미정이다. `EnemyEncounter/MidBoss`에서 약점·발사점·시험 수치를, `SpawnPoints/MidBossRoute`에서 진입점과 공격 위치를 편집할 수 있다.

---
## 하늘과 먼 지표

하늘 아래 낮은 고도에 생성한 바다·해안 이미지를 투영했다. 수평선 쪽에서는 옅은 대기색과 섞이고 아래쪽 시점에서는 해안선과 먼 섬이 보인다. 구름과 부유섬은 별도 월드 깊이에서 움직이며 지표 투영은 배경으로 렌더링한다. `OuterSky.mat`의 색상·투영 높이·이미지 범위를 편집할 수 있다. 지표 이미지는 내장 이미지 생성 도구로 제작했다. [바다와 해안 이미지](../../Assets/ProjectG/OuterIslands/OuterSurfaceMap.png)

> Create ONE square 2048px-style aerial orthographic terrain texture for Project G, an original 3D fantasy flight game. The camera looks exactly straight down from extreme altitude, like a high-resolution satellite map. Broad deep cobalt blue ocean with large desaturated cool jade mainland entering the upper-right and across the right edge, long realistic curving coastline, several irregular offshore islands, small pale turquoise coastal shallows, faint drainage rivers reaching the ocean, subtle rugged dark green mountain ridges visible as soft relief. Blue atmospheric haze, restrained contrast and smoothed detail at very high altitude. Ocean should cover roughly 65 percent and land cover roughly 35 percent, with clearly readable shoreline shapes. Crisp painterly aerial photograph feel, no dark corners or hard vignettes, edge-to-edge texture. NO clouds, fog banks, horizon, perspective, sky, labels, map symbols, human structures or typography. Full opaque bitmap intended to be sampled as a skybox ground projection.

---
## 검증

Unity 6000.3.21f1 Play Mode 43개 통과. 자동 등장, 패턴 교대와 피해 10, 노출 약점과 피해 배율, 강화·경험치·필살기 에너지, 잔류 적탄 회수와 후속 웨이브를 검증했다. 실제 보스전의 난이도와 회피 동선은 후속 수동 플레이에서 확인한다.

Windows 1920×1080·1024×576에서 정상 시작 씬을 실행해 보스 등장과 두 패턴, 약점 노출 및 격파 뒤 전환을 확인했다. 머티리얼이 실제로 그려지는지 확인하기 위해 검증용으로 추적 카메라를 아래로 기울여 지표를 캡처했다. [중간 보스전](../screenshots/3d-day-17-flight.png) · [바다와 해안](../screenshots/3d-day-17-surface.png)

18일차는 최종 보스의 체력 단계별 공격, 전용 체력바와 페이즈 표시를 연결한다.
