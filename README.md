---
# 프로젝트 G

하늘섬 사이를 마법 빗자루로 비행하는 PC용 3D 공중 탄막 슈팅 프로젝트입니다.

---
## 메인 화면

![프로젝트 G 3D 메인 화면](docs/screenshots/3d-day-23-title.png)

Windows 빌드는 `Assets/Scenes/FlightMainMenu.unity`에서 시작합니다. 새 게임과 이어서하기는 하늘섬 마을 착륙장으로 이어집니다. 이어서하기는 마지막 조종사와 저장된 장비·마법을 불러옵니다. 설정에서 화면 모드·해상도·음량·마우스 감도를 적용할 수 있습니다. [설정 화면](docs/screenshots/3d-day-39-master-volume.png) · [1024×576 시작 화면](docs/screenshots/3d-day-23-title-1024.png) · [23일차 기록](docs/development/3d-day-23.md)

---
## 하늘섬 마을

![프로젝트 G 하늘섬 마을 중앙 광장](docs/screenshots/3d-day-29-plaza.png)

착륙장에서 이동 키로 광장을 지나 북쪽 다리를 건넙니다. 출격소에 접근하면 **E** 또는 출격 준비 버튼으로 캐릭터와 장비를 선택할 수 있습니다. 준비에서 뒤로가기를 누르면 출격소 앞으로 돌아갑니다. 마을에서 Esc는 메인 화면으로 돌아갑니다. [착륙장](docs/screenshots/3d-day-29-landing.png) · [장비 상점](docs/screenshots/3d-day-29-market.png) · [강화 공방](docs/screenshots/3d-day-29-workshop.png) · [1024×576 출격소](docs/screenshots/3d-day-29-sortie-1024.png) · [29일차 기록](docs/development/3d-day-29.md)

서쪽 장비 상점에서는 골드로 빗자루 부품을 사고 장착하거나 캐릭터별 E 스킬을 보호막·산탄으로 바꿀 수 있습니다. 동쪽 강화 공방에서는 장착 부품에 천공 결정과 골드를 사용해 최대 +5까지 강화합니다. 두 시설은 가까이에서 E로 열고 Esc로 닫습니다. [상점 UI](docs/screenshots/3d-day-30-market.png) · [공방 UI](docs/screenshots/3d-day-30-forge.png) · [1024×576 재실행 확인](docs/screenshots/3d-day-30-forge-1024.png) · [30일차 기록](docs/development/3d-day-30.md)

강화 공방의 **후속 성장**에서 조종사별 첫 과제와 카드 해금을 확인합니다. 최초 과제 정산으로 해당 조종사의 조율의 증표를 한 번 받고, 천공 결정 18개와 증표 1개로 노드를 해금합니다. 해금한 2단계 카드는 출격 중 같은 분기 1단계를 골라야 후보에 들어갑니다. [아리아 첫 과제](docs/screenshots/3d-day-31-aria-task.png) · [리안 첫 과제](docs/screenshots/3d-day-31-rian-task.png) · [1024×576 후속 성장 복원](docs/screenshots/3d-day-31-restore-growth.png) · [31일차 기록](docs/development/3d-day-31.md)

---
## 캐릭터 선택과 출격 준비

![프로젝트 G 3D 리안 출격 준비](docs/screenshots/3d-day-24-rian.png)

`Assets/Scenes/FlightSortieBriefing.unity`에서 아리아·리안·루미·세라·녹스·베라·이리스를 선택하고, 빗자루 조립과 마법 편성을 조정합니다. 오른쪽 아래 출발 버튼으로 선택한 조합이 실제 항로에 반영됩니다. [아리아 준비 화면](docs/screenshots/3d-day-24-aria.png) · [1024×576 준비 화면](docs/screenshots/3d-day-24-briefing-1024.png) · [24일차 기록](docs/development/3d-day-24.md)

루미는 같은 적을 추적할수록 유도탄 회전력이 높아집니다. Q는 별빛 추적탄, R은 서로 다른 고도의 적에게 분산되는 별무리 항로를 발사합니다. 천공 결정은 파란 결정 다발 아이콘, 골드는 금빛 동전 아이콘으로 상점·강화·출격과 정산의 수치에 표시합니다. [루미 준비 화면](docs/screenshots/3d-day-32-lumi-briefing.png) · [전투](docs/screenshots/3d-day-32-lumi-combat.png) · [상점](docs/screenshots/3d-day-32-market.png) · [결과](docs/screenshots/3d-day-32-result.png) · [32일차 기록](docs/development/3d-day-32.md)

출격 준비에서 **첫 비행 안내** 또는 F1을 누르면 이동·집중·자동 사격·그레이즈·스킬을 연습합니다. Enter로 출격 준비에 돌아갈 수 있고, Tab으로 마우스 포인터를 전환해 안내 버튼을 클릭할 수 있습니다. [1024×576 첫 비행 안내](docs/screenshots/3d-day-27-guide-1024.png) · [4:3 출격 준비](docs/screenshots/3d-day-27-briefing-4x3.png) · [27일차 기록](docs/development/3d-day-27.md)

---
## 출격 결과와 재출격

![프로젝트 G 3D 아리아 출격 완료 화면](docs/screenshots/3d-day-37-aria-result.png)

최종 보스를 격파하거나 체력을 모두 잃으면 결과 확인 버튼이 켜지고 결과에 맞는 음악이 한 번 재생됩니다. 결과 화면은 전투·전체 시간, 점수, 격파 수, 그레이즈, 도달 레벨을 표시합니다. 같은 캐릭터와 저장한 장비·마법으로 다시 출격하거나 메인 화면으로 돌아갈 수 있습니다. [1024×576 실패 결과](docs/screenshots/3d-day-25-failed-1024.png) · [25일차 기록](docs/development/3d-day-25.md)

결과 화면 왼쪽에는 출격한 조종사의 전신 일러스트, 오른쪽에는 전투 기록·보상·다시 출격 버튼을 표시합니다. 세라는 근거리 적에게 더 큰 피해를 주며, Q 돌파 검기와 R 천공 연참으로 전방을 공격합니다. [세라 결과](docs/screenshots/3d-day-33-sera-result.png) · [루미 결과 1024×768](docs/screenshots/3d-day-33-lumi-result-4x3.png) · [세라 전투](docs/screenshots/3d-day-33-sera-blade.png) · [33일차 기록](docs/development/3d-day-33.md)

녹스는 그레이즈로 반격 표식을 모읍니다. Q는 표식 하나당 반격탄 한 발을 발사하고, R은 반경 3m 안의 일반 적탄을 지웁니다. [표식 HUD](docs/screenshots/3d-day-34-nox-marks.png) · [경계 역전](docs/screenshots/3d-day-34-nox-reversal.png) · [34일차 기록](docs/development/3d-day-34.md)

베라는 Q로 상단 프로펠러가 달린 부유 포대 한 기를 배치하며, 포대에서 R 교차 광선을 발사합니다. 포대가 만료되면 R이 잠기고 에너지는 보존됩니다. [부유 포대](docs/screenshots/3d-day-35-vera-turret.png) · [교차 광선](docs/screenshots/3d-day-35-vera-beam.png) · [35일차 기록](docs/development/3d-day-35.md)

이리스는 서로 다른 적을 맞히면 파동 범위가 넓어집니다. Q 확산 파동은 전방의 적을 공격하고, R 창공의 공명은 직접 적중한 곳에서 다른 적에게 최대 두 번 이어집니다. [일곱 조종사 선택](docs/screenshots/3d-day-36-iris-briefing.png) · [이리스 전투](docs/screenshots/3d-day-36-iris-chain.png) · [36일차 기록](docs/development/3d-day-36.md)

캐릭터별 빗자루 부품의 피해 감소, 최대 마력, 초당 마력 회복이 전투에 반영됩니다. 리안의 약점 보너스는 강화 후 실제 피해에 적용되는 수치를 표시합니다. [최종 보스 3페이즈](docs/screenshots/3d-day-37-aria-final-boss.png) · [37일차 통합 검증 기록](docs/development/3d-day-37.md)

첫 웨이브가 시작된 출격은 영구 정산에 포함됩니다. 결과에서 숙련도와 재료 증가량을, 출격 준비에서 누적 숙련도와 보유량을 확인할 수 있습니다. 전투 도중 종료한 출격은 다음 실행의 메인 화면에서 참여 보상으로 한 번 정산됩니다. [영구 정산 화면](docs/screenshots/3d-day-26-settlement.png) · [재실행 복구 화면](docs/screenshots/3d-day-26-recovery-1024.png) · [26일차 기록](docs/development/3d-day-26.md)

---
## 3D 하늘섬 외곽 항로

Unity **6000.3.21f1**에서 `Assets/Scenes/OuterIslandsStage.unity`를 열면 항로에 직접 출격할 수 있습니다. `FlightVolume`, `SpawnPoints`, `EnemyEncounter`, `CloudLayers`에서 비행 체적·적 경로·출현 시각·구름 배치를 편집합니다. 개별 조작과 탄막은 `Assets/Scenes/FlightPrototype.unity`에서 시험합니다.

![루미 3D 비행 모형과 하늘섬 항로](docs/screenshots/3d-day-38-lumi-flight.png)

일곱 조종사의 저폴리곤 비행 모형은 `AriaPilot/PilotModels`에서 편집합니다. 비행 방향에 따라 몸과 옷자락이 움직이고, 조종사는 화면 중앙의 조준점 왼쪽 아래에 있습니다. 적탄은 주황 조준 구체, 금빛 부채꼴 사각탄, 분홍 고리 결정, 붉은 나선 결정으로 구분합니다. [네 탄막 비교](docs/screenshots/3d-day-38-bullet-shapes.png) · [1024×576 탄막](docs/screenshots/3d-day-38-bullet-shapes-1024.png) · [일반 마물 실루엣](docs/screenshots/3d-day-38-enemy-shapes.png) · [38일차 기록](docs/development/3d-day-38.md)

타이틀 설정은 16:9 창과 1280×800, 1024×768을 지원합니다. [소형 16:9 전투](docs/screenshots/3d-day-27-hud-1024.png) · [16:10 전투](docs/screenshots/3d-day-27-hud-16x10.png) · [4:3 전투](docs/screenshots/3d-day-27-hud-4x3.png)

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
| F2 | 훈련 씬 또는 첫 웨이브 시작 전 아리아와 리안 전환 |
| Q / E / R | 아리아 집중 포화 / 보호 / 유성 탄막 · 리안 관통 사격 / 1회 보호 / 천공 관측선 |
| T | 비행 시험 씬에서만 적탄 연습 켜기·끄기 |
| Y | 비행 시험 씬에서만 조준형·확산형·돌진형 수동 출현 |
| 1~4 | 비행 시험 씬에서만 조준·부채꼴·고리·나선 연습 탄막 선택 |
| Backspace | 시험 출격 재시작 |
| Esc | 일시정지. 계속하기에서 전투 복귀 |

타이틀·마을·외곽 항로에 각 배경곡이 있으며 중간·최종 보스 진입에는 보스곡으로 바뀝니다. Esc로 전투 음악과 효과음을 멈춰도 정지 메뉴 버튼음이 들립니다. 마스터 음량은 메인 설정과 전투 설정에서 조절할 수 있습니다. [전투 음량 설정](docs/screenshots/3d-day-39-paused-audio.png) · [39일차 기록](docs/development/3d-day-39.md) · [정식 음원 제작 브리프](https://docs.google.com/document/d/11SYgjADf-6TG3s7-TG01_Ue1nwgq3DP6-zUa63HLyig/edit?tab=t.p64gk563fhsh)

![프로젝트 G 3D 22일차 제작 마법과 3D 시험장](docs/screenshots/3d-day-22-editor.png)

하늘섬 외곽 항로에서 `Esc → 마법 제작`을 열면 마법 구성을 편집하고, 3D 시험장에서 피해와 연습 마력을 확인할 수 있습니다. 조합을 저장한 뒤 좌우 칸에 편성하면 실제 마우스 클릭으로 시전합니다. 첫 웨이브가 시작된 뒤 저장한 편성은 다음 출격에서 적용됩니다. [1024×576 제작 화면](docs/screenshots/3d-day-22-editor-1024.png) · [22일차 기록](docs/development/3d-day-22.md)

`Esc → 빗자루 조립`에서는 현재 캐릭터의 8개 부위를 확인합니다. 후보를 고르면 3D 외형과 현재 조합에서 바뀌는 능력치가 표시되고, 장착 확정으로 저장한 조합은 재실행 후에도 복원됩니다. [조립 화면](docs/screenshots/3d-day-20-workshop.png) · [1024×576 재실행 화면](docs/screenshots/3d-day-20-reload-1024.png) · [20일차 기록](docs/development/3d-day-20.md)

제작 마법의 구성 ID와 고정 비용, 호환성·탄수 제한, 직선·확산·유도 방향은 편집기와 전투에서 공유합니다. [21일차 제작 마법 규칙](docs/development/3d-day-21.md)

질풍의 몸체를 장착하면 아리아의 수평 최고 속도가 12.3 m/s에서 12.7 m/s로 바뀝니다. Unity의 `AriaPilot/TrainingBroom/PartMounts`에서 부품 위치를 편집합니다. [기본 몸체](docs/screenshots/3d-day-19-default.png) · [질풍의 몸체](docs/screenshots/3d-day-19-flight.png) · [19일차 기록](docs/development/3d-day-19.md)

최종 보스전에서는 타이머 아래에 체력과 남은 페이즈가 표시됩니다. 노란 점은 현재 페이즈를 포함하고, 체력 1,000과 500에서 공격이 바뀌며 이전 탄막이 회수됩니다. `전체` 시간에는 일시정지와 레벨업 선택이 포함됩니다. [최종 보스 2페이즈](docs/screenshots/3d-day-18-flight.png) · [3페이즈 교차 나선](docs/screenshots/3d-day-18-crossing.png) · [1024×576 보스 HUD](docs/screenshots/3d-day-18-hud-1024.png) · [중간 보스전](docs/screenshots/3d-day-17-flight.png) · [바다와 해안](docs/screenshots/3d-day-17-surface.png) · [정지와 조작 안내](docs/screenshots/3d-day-14-pause.png)

![아리아의 1080p 최종 보스 3페이즈와 HUD](docs/screenshots/3d-day-28-aria-boss.png)

아리아와 리안의 1080p 자동 플레이에서 두 캐릭터의 장비·마법 편성, 성장, 최종 보스, 영구 정산과 재출격을 확인했습니다. 프레임 시간의 측정 조건과 미검증 항목은 28일차 기록에 정리했습니다. [아리아 클리어](docs/screenshots/3d-day-28-aria-result.png) · [리안 보스전](docs/screenshots/3d-day-28-rian-boss.png) · [리안 실패](docs/screenshots/3d-day-28-rian-result.png) · [재실행 복구](docs/screenshots/3d-day-28-recovery.png) · [28일차 기록](docs/development/3d-day-28.md)

[1일차 기록](docs/development/3d-day-01.md) · [2일차 기록](docs/development/3d-day-02.md) · [3일차 기록](docs/development/3d-day-03.md) · [4일차 기록](docs/development/3d-day-04.md) · [5일차 기록](docs/development/3d-day-05.md) · [6일차 기록](docs/development/3d-day-06.md) · [7일차 기록](docs/development/3d-day-07.md) · [8일차 기록](docs/development/3d-day-08.md) · [9일차 기록](docs/development/3d-day-09.md) · [10일차 기록](docs/development/3d-day-10.md) · [11일차 기록](docs/development/3d-day-11.md) · [12일차 기록](docs/development/3d-day-12.md) · [13일차 기록](docs/development/3d-day-13.md) · [14일차 기록](docs/development/3d-day-14.md) · [15일차 기록](docs/development/3d-day-15.md) · [16일차 기록](docs/development/3d-day-16.md) · [17일차 기록](docs/development/3d-day-17.md) · [18일차 기록](docs/development/3d-day-18.md)
