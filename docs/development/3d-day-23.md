---
# 3D 23일차 메인 화면·설정·일시정지

---
## 시작 화면

Windows의 첫 씬을 `FlightMainMenu`로 바꿨다. 왼쪽에 비행 일러스트를 놓고, 오른쪽에 새 게임·이어서하기·설정·나가기 순서로 버튼을 배치했다. 저장된 출격 기록이 없는 첫 실행에서는 이어서하기가 비활성화된다. 출격 기록이나 이전 버전의 장비·마법 기록이 있으면 새 게임 전에 확인창을 표시한다. 취소하면 기존 기록을 유지한다.

현재 이어서하기는 보존한 장비와 마법을 불러와 하늘섬 외곽 항로의 새 출격을 시작한다. 전투 중단 시점의 스냅샷과 정산 복구는 후속 일차에서 연결한다. 일시정지에서 포기를 승인하면 기존 다시하기와 함께 메인 화면 복귀 버튼이 나타난다.

---
## 환경 설정

창 모드·전체 화면, 1920×1080부터 1024×576까지 네 가지 해상도, 전체 음량과 마우스 감도를 적용할 수 있다. 타이틀과 실전 일시정지가 마스터 음량과 마우스 감도의 저장 키를 공유한다. 마우스 클릭, W/S 또는 위·아래, Enter와 Esc를 지원한다. 설정 슬라이더는 왼쪽·오른쪽 키로 조정할 수 있다.

---
## 이미지

타이틀의 왼쪽 일러스트는 내장 이미지 생성 도구에서 제작했다. 화면의 제목과 네 버튼은 씬의 독립 UI 오브젝트다. [메인 화면 배경](../../Assets/ProjectG/MainMenu/FlightTitleArt.png)

> Create a polished 16:9 widescreen background illustration for the startup screen of Project G, an original PC 3D magical broom flight game. The LEFT 60% holds a single striking cel-shaded anime fantasy young adult aviator in dark navy flight coat, teal trim, copper scarf streaming in the wind, viewed from a three-quarter side/rear angle in midflight on an ornate brass-and-teal magical broom, with a large sky island city emerging from the layered clouds behind them. Dramatic warm dawn rim light below, grounded 3D volume and deep high-altitude parallax, painterly atmospheric ocean haze far below, architectural details on left. Keep the RIGHT 40% visually quiet and dark with deep ink blue clouds, no figure, landmarks, bright focal point, objects, or text there, because white title and four menu buttons will be composited there by Unity. Cinematic arcane aviation, well-defined silhouette, subdued teal amber, no text, no letters, no UI, no borders or logos. Original artwork, no existing game characters.

---
## 검증

Unity 6000.3.21f1 Play Mode 67개 통과. 배포 첫 씬, 저장이 없는 이어서하기 비활성, 키보드에서 설정으로 이동, 설정 저장과 재진입 시 음량 복원, 새 출격, 포기 후 타이틀 복귀, 기존 기록이 있는 새 게임 확인과 이어서하기를 검증했다.

Windows 1920×1080과 1024×576의 격리 빌드에서 가상 마우스로 설정 버튼을 클릭하고 Enter로 출격했다. 화면·음량 저장, 일시정지 후 타이틀 복귀, 새 게임 확인창을 검증했다. 실제 모니터에서 창 모드·전체 화면을 전환하는 수동 확인은 대기다. [메인 화면](../screenshots/3d-day-23-title.png) · [설정](../screenshots/3d-day-23-settings.png) · [1024×576](../screenshots/3d-day-23-title-1024.png)

24일차에는 기획서의 아리아·리안 스탠딩 일러스트를 사용하는 캐릭터 선택과 출격 준비 화면을 연결한다.
