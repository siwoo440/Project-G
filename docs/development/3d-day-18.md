---
# 3D 18일차 최종 보스와 페이즈 HUD

---
## 최종 보스 전투

중간 보스 격파 후 일반 전투 50초와 회수 15초가 지나고 마지막 일반 적이 사라지면 최종 보스가 등장한다. 시험 최대 체력은 1,500이다. 체력을 1,000과 500으로 나누고, 큰 단일 피해는 다음 경계까지만 반영한다. 전환 0.9초 동안 추가 피해와 발사를 중단하고 이전 탄막을 공유 풀에 회수한다. 보스의 공격 종료 뒤에는 실제 피격 판정이 있는 약점을 1초간 열어 준다.

| 페이즈 | 체력 범위 | 시험 공격 |
|---|---|---|
| 1 | 1,500~1,001 | 중앙 직선 조준 연사, 피해 10 |
| 2 | 1,000~501 | 중앙의 입체 고리 확산, 피해 15 |
| 3 | 500~1 | 양쪽 날개 발사점에서 교차하는 입체 나선, 피해 20 |

격파하면 전투 시간·전체 소요 시간을 고정하고 항로 완료를 표시한다. 기존 처치 경로에서 경험치 50과 필살기 에너지 20을 처리한다. 최종 보스에게서 강화 아이템은 드롭하지 않는다. 결과·정산 화면은 후속 일차 범위다.

최종 보스 외형은 기존 마물의 날갯짓 구조에 청록색 비대칭 날개·긴 꼬리·머리 장식을 적용한 시험 모델이다. 이름과 최종 외형은 미정이다. `EnemyEncounter/FinalBoss`에서 보스 외형·약점·발사점을, `SpawnPoints/FinalBossRoute`에서 진입과 전투 위치를 편집한다.

---
## 보스 전용 HUD

`CombatHUD/BossReadout`의 붉은 체력바와 체력 숫자는 최종 보스의 실제 체력을 표시한다. 현재 페이즈를 포함해 남아 있는 점을 노랗게 켜고 끝난 점은 회색 윤곽으로 표시한다. 진입·전환·약점과 현재 패턴도 표시한다. 프레임은 내장 이미지 생성 도구에서 제작했다. [보스 체력바 프레임](../../Assets/ProjectG/OuterIslands/FinalBossHudFrame.png)

> A single extremely wide horizontal rectangular fantasy aerial boss health HUD frame for an original Unity PC action game, aspect ratio about 8:1 designed to fill a thin game overlay. Flat gold-brass and charcoal-navy frame with subtle antique metal corners, a quiet solid deep navy recessed rectangular inner plate, restrained small garnet accents at the far ends. Symmetrical left to right. Put design strictly around the edge, keep the entire center clear for a red dynamic health fill and two division markers to be added later. Strong readable outline at small size, 2D front-on orthographic UI sprite. Frame completely visible with tight perimeter margin. Only one isolated horizontal frame on fully transparent alpha around the exterior corners. No numbers, no lettering, no gauge fill, no phase dots, no characters, no background scenery.

---
## 검증

Unity 6000.3.21f1 Play Mode 44개 통과. 정상 항로 진입, 단일 피해 제한, 전환 중 같은 프레임과 후속 프레임 피해 차단, 이전 탄막 회수, 체력바 비율과 3→2→1 점등, 최종 격파와 보상 1회·강화 아이템 미지급을 확인했다.

Windows 1920×1080·1024×576 시험에서 첫 웨이브부터 자동 출현을 확인했다. 체력 경계를 검증하기 위해 보스에 시험 피해를 적용했고, 원형 확산과 교차 나선의 화면 및 최종 격파 후 시계 정지를 확인했다. 수동 난이도와 최적 회피 동선은 아직 검증하지 않았다. [2페이즈](../screenshots/3d-day-18-flight.png) · [3페이즈](../screenshots/3d-day-18-crossing.png)

19일차는 빗자루 8개 부품 슬롯, 개별 부품 데이터, 3D 부착점과 비행 수치 반영을 연결한다.
