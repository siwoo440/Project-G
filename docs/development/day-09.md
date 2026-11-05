---
# 9일차 : 시험 스테이지와 보스 단계 연결

---
## 완료한 작업

- 기본 전투 장면에 하늘섬 외곽 시험 배경과 스테이지 진행 기능을 연결했다. 배경은 초기 시안이며 아직 스크롤하지 않는다.
- 준비 20초, 일반 전투 1의 45초, 첫 강화 아이템, 중간 보스 처치 대기, 일반 전투 2의 50초, 둘째 강화 아이템과 최종 보스 처치 대기를 순서대로 연결했다. 시간은 시험값이며 보스 처치가 늦어져도 강제 종료하지 않는다.
- 적 웨이브와 8일차 강화 아이템 선택을 실제 장면에 연결하고, 선택과 보스 등장 전의 안전 구간에서 카드 선택을 지연한다.
- 전투 진행 시간과 일시정지 등을 포함한 전체 소요 시간을 구분해 표시하고, 클리어·실패 시험 상태와 재출격 초기화를 연결했다.
- 기획서에 확정된 균열 조사 중 마물 습격과 클리어 후 낯선 마법 흔적 발견을 각각 짧은 시험 안내 문구로 표시한다.
- 최종 보스에게 한 번에 큰 피해가 들어와도 다음 체력 경계까지만 적용하고 초과 피해를 버린다. 각 단계 전환을 시험 HUD에 표시한다.
- 배경은 내장 이미지 생성 도구로 생성한 `Assets/ProjectG/Resources/BattleArt/stage_sky_outskirts.png` 원본이다.

---
## 생성 이미지 요청

Use case: stylized-concept. Asset type: 16:9 full-screen scrolling-shooter background, no foreground objects or interface. Create a wide, peaceful high-altitude fantasy sky above clouds with distant floating island edges and a few subtle magical architectural silhouettes near the perimeter, top-down vertical shooter perspective. Keep the large central combat area dark, low contrast, and uncluttered so small bright bullets and characters remain clearly visible; subdued navy teal clouds with muted golden light at the edges. No creatures, no characters, no text, no symbols or established story details.

---
## 확인한 테스트

- 강화 아이템·보스·스테이지 단계, 보스 처치 대기, 전환 안전 상태 해제, 클리어와 재출격 초기화를 자동 시험했다.
- Unity 6000.3.21f1 배치 모드에서 EditMode 169건과 PlayMode 18건이 통과했다.

---
## 남은 작업

- 실제 플레이 화면에서 배경·탄막·HUD의 가독성, 조작감, 보스 패턴과 강화 아이템 동선을 검증하지 않았다.
- 3~5분 출격, 카드 선택 5~8회, 필살기 1~2회의 목표는 측정 전이다. 시험용 적 등장 간격과 안전 시간은 조정 대상이다.
- 첫 사건과 마법 흔적은 시험 안내 문구까지만 적용했다. 인물 대화·상호작용과 세부 연출은 아직 구현하지 않았다.
- 보스 단계 전환에서 연속 타격은 각 타격마다 다음 체력 경계까지만 적용하는 시험 처리다. 후속 연출과 공정성 검증이 필요하다.
- 정식 결과 화면은 10일차, 보상·영구 성장 및 저장은 11일차 범위다. 현재 결과는 시험용 상태 표시다.

---
## 다음 일차

- 10일차에는 타이틀, 캐릭터 선택, 출격 준비, 일시정지, 결과·재시도와 설정 화면을 연결하고 UI와 탄막의 겹침을 확인한다.
