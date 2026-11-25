---
# 3D 29일차 하늘섬 마을과 출격 동선

---
## 마을 씬

`FlightIslandVillage`에 착륙장과 분수 광장, 장비 상점·강화 공방·출격소가 있는 세 섬, 연결 다리 세 개를 배치했다. 하늘과 바다·해안 배경을 전투 항로와 공유한다. 현재 도보 캐릭터와 건물은 위치와 동선을 확인하는 로우폴리 시제품이다. 주민과 지명은 확정 전까지 일반 시설명으로 표시한다.

`VillageWorld/Islands`, `Bridges`, `InteractionPoints`에서 건물·지형·연결 다리·시설 입구를 편집한다. `VillageWalker`는 보행 충돌을, `Village Camera`는 편집된 각도와 추적 거리, `VillageHUD`는 구역과 상호작용을 담당한다.

---
## 시작과 출격

새 게임과 이어서하기는 모두 착륙장에서 시작한다. 기본 WASD 또는 설정한 이동 키로 지면과 다리 위를 걸으며, 지면이 끝나는 방향에서는 이동을 제한한다. 북쪽 출격소 앞에서 E 또는 버튼으로 출격 준비를 열고, 뒤로가기는 출격소 바로 앞으로 돌아온다.

---
## 검증

Unity 6000.3.21f1 Play Mode 72개로 기존 메뉴, 새 게임·이어서하기, 낭떠러지, 출격소 접근 제한, 다리 보행, 이전 시설 복귀를 확인했다. 독립 Windows 1920×1080과 1024×576에서 세 다리를 걸어 장비 상점·강화 공방·출격소에 도착하고, 추적 카메라, 출격소 복귀, 전투 진입을 확인했다. [착륙장](../screenshots/3d-day-29-landing.png) · [중앙 광장](../screenshots/3d-day-29-plaza.png) · [장비 상점](../screenshots/3d-day-29-market.png) · [강화 공방](../screenshots/3d-day-29-workshop.png) · [출격소](../screenshots/3d-day-29-sortie.png) · [1024×576 출격소](../screenshots/3d-day-29-sortie-1024.png)

장비 교체, 공방 강화, 시설 이용에 따른 재화 보존은 30일차에서 연결한다.
