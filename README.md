---
# 프로젝트 G

하늘섬 사이를 마법 빗자루로 비행하는 PC용 3D 공중 탄막 슈팅 프로젝트입니다.

---
## 메인 화면

![프로젝트 G 3D 메인 화면](docs/screenshots/3d-day-23-title.png)

Windows 빌드는 `Assets/Scenes/FlightMainMenu.unity`에서 시작합니다. 새 게임과 이어서하기는 출격 준비로 이어집니다. 이어서하기는 마지막 조종사와 저장된 장비·마법을 불러옵니다. 설정에서 화면 모드·해상도·음량·마우스 감도를 적용할 수 있습니다. [설정 화면](docs/screenshots/3d-day-23-settings.png) · [1024×576 시작 화면](docs/screenshots/3d-day-23-title-1024.png) · [23일차 기록](docs/development/3d-day-23.md)

---
## 캐릭터 선택과 출격 준비

![프로젝트 G 3D 리안 출격 준비](docs/screenshots/3d-day-24-rian.png)

`Assets/Scenes/FlightSortieBriefing.unity`에서 아리아·리안을 선택하고, 빗자루 조립과 마법 편성을 조정합니다. 오른쪽 아래 출발 버튼으로 선택한 조합이 실제 항로에 반영됩니다. [아리아 준비 화면](docs/screenshots/3d-day-24-aria.png) · [1024×576 준비 화면](docs/screenshots/3d-day-24-briefing-1024.png) · [24일차 기록](docs/development/3d-day-24.md)

---
## 출격 결과와 재출격

![프로젝트 G 3D 출격 완료 화면](docs/screenshots/3d-day-25-clear.png)

최종 보스를 격파하거나 체력을 모두 잃으면 결과 확인 버튼이 켜집니다. 결과 화면은 전투·전체 시간, 점수, 격파 수, 그레이즈, 도달 레벨을 표시합니다. 같은 캐릭터와 저장한 장비·마법으로 다시 출격하거나 메인 화면으로 돌아갈 수 있습니다. [1024×576 실패 결과](docs/screenshots/3d-day-25-failed-1024.png) · [25일차 기록](docs/development/3d-day-25.md)

---
## 3D 하늘섬 외곽 항로

Unity **6000.3.21f1**에서 `Assets/Scenes/OuterIslandsStage.unity`를 열면 항로에 직접 출격할 수 있습니다. `FlightVolume`, `SpawnPoints`, `EnemyEncounter`, `CloudLayers`에서 비행 체적·적 경로·출현 시각·구름 배치를 편집합니다. 개별 조작과 탄막은 `Assets/Scenes/FlightPrototype.unity`에서 시험합니다.

| 입력 | 동작 |
|---|---|
| WASD | 전후·좌우 비행 |
| Space | 상승 |
| Ctrl | 하강 |
| Shift | 집중 비행 |
| 마우스 이동 | 비행 방향과 화면 중앙 조준 조작 |
| 자동 | 기본 마법탄 사격 |
| 좌클릭 / 우클릭 | 왼쪽 / 오른쪽 현재 마법 카드 |
| Shift 유지 | 집중 마법탄 자동 사격 |
| 휠 버튼 유지 | 비행 방향을 유지하며 둘러보기 |
| F2 | 아리아와 리안의 전투 설정·빗자루 조합 전환 |
| Q / E / R | 아리아 집중 포화 / 보호 / 유성 탄막 · 리안 관통 사격 / 1회 보호 / 천공 관측선 |
| T | 비행 시험 씬에서만 적탄 연습 켜기·끄기 |
| Y | 비행 시험 씬에서만 조준형·확산형·돌진형 수동 출현 |
| 1~4 | 비행 시험 씬에서만 조준·부채꼴·고리·나선 연습 탄막 선택 |
| Backspace | 시험 출격 재시작 |
| Esc | 일시정지. 계속하기에서 전투 복귀 |

![프로젝트 G 3D 22일차 제작 마법과 3D 시험장](docs/screenshots/3d-day-22-editor.png)

하늘섬 외곽 항로에서 `Esc → 마법 제작`을 열면 마법 구성을 편집하고, 3D 시험장에서 피해와 연습 마력을 확인할 수 있습니다. 조합을 저장한 뒤 좌우 칸에 편성하면 실제 마우스 클릭으로 시전합니다. 첫 웨이브가 시작된 뒤 저장한 편성은 다음 출격에서 적용됩니다. [1024×576 제작 화면](docs/screenshots/3d-day-22-editor-1024.png) · [22일차 기록](docs/development/3d-day-22.md)

`Esc → 빗자루 조립`에서는 현재 캐릭터의 8개 부위를 확인합니다. 후보를 고르면 3D 외형과 현재 조합에서 바뀌는 능력치가 표시되고, 장착 확정으로 저장한 조합은 재실행 후에도 복원됩니다. [조립 화면](docs/screenshots/3d-day-20-workshop.png) · [1024×576 재실행 화면](docs/screenshots/3d-day-20-reload-1024.png) · [20일차 기록](docs/development/3d-day-20.md)

제작 마법의 구성 ID와 고정 비용, 호환성·탄수 제한, 직선·확산·유도 방향은 편집기와 전투에서 공유합니다. [21일차 제작 마법 규칙](docs/development/3d-day-21.md)

질풍의 몸체를 장착하면 아리아의 수평 최고 속도가 12.3 m/s에서 12.7 m/s로 바뀝니다. Unity의 `AriaPilot/TrainingBroom/PartMounts`에서 부품 위치를 편집합니다. [기본 몸체](docs/screenshots/3d-day-19-default.png) · [질풍의 몸체](docs/screenshots/3d-day-19-flight.png) · [19일차 기록](docs/development/3d-day-19.md)

최종 보스전에서는 타이머 아래에 체력과 남은 페이즈가 표시됩니다. 노란 점은 현재 페이즈를 포함하고, 체력 1,000과 500에서 공격이 바뀌며 이전 탄막이 회수됩니다. `전체` 시간에는 일시정지와 레벨업 선택이 포함됩니다. [최종 보스 2페이즈](docs/screenshots/3d-day-18-flight.png) · [3페이즈 교차 나선](docs/screenshots/3d-day-18-crossing.png) · [1024×576 보스 HUD](docs/screenshots/3d-day-18-hud-1024.png) · [중간 보스전](docs/screenshots/3d-day-17-flight.png) · [바다와 해안](docs/screenshots/3d-day-17-surface.png) · [정지와 조작 안내](docs/screenshots/3d-day-14-pause.png)

[1일차 기록](docs/development/3d-day-01.md) · [2일차 기록](docs/development/3d-day-02.md) · [3일차 기록](docs/development/3d-day-03.md) · [4일차 기록](docs/development/3d-day-04.md) · [5일차 기록](docs/development/3d-day-05.md) · [6일차 기록](docs/development/3d-day-06.md) · [7일차 기록](docs/development/3d-day-07.md) · [8일차 기록](docs/development/3d-day-08.md) · [9일차 기록](docs/development/3d-day-09.md) · [10일차 기록](docs/development/3d-day-10.md) · [11일차 기록](docs/development/3d-day-11.md) · [12일차 기록](docs/development/3d-day-12.md) · [13일차 기록](docs/development/3d-day-13.md) · [14일차 기록](docs/development/3d-day-14.md) · [15일차 기록](docs/development/3d-day-15.md) · [16일차 기록](docs/development/3d-day-16.md) · [17일차 기록](docs/development/3d-day-17.md) · [18일차 기록](docs/development/3d-day-18.md)
