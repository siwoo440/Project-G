---
# 3D 13일차 레벨업 카드와 상단 중앙 웨이브 HUD

---
## 전투 상단

화면 상단 중앙에 출격 경과 시간과 그 아래의 Wave 번호·원형 몬스터 배지·남은 적 수를 배치했다. 배경·아이콘·내부 간격과 글자를 첫 배치의 80%로 조정했다. 처음에 Wave 1에서 시작한다. Y로 다음 시험 웨이브를 시작하면 현재 번호가 오르고, 활성 적을 처치하거나 적이 이탈하면 남은 수가 줄어든다. Backspace로 재출격하면 시간과 첫 웨이브가 초기화된다. 정식 스테이지 웨이브는 16일차에 연결한다.

`CombatHUD/WaveReadout`에서 글자, 간격, 배지와 배경을 편집할 수 있다. 원형 몬스터 배지 `WaveMonsterBadge.png`는 내장 이미지 생성 도구에서 아래 프롬프트로 만들었다.

> Single isolated circular enemy-count HUD badge for Project G, an original aerial fantasy game. On perfectly transparent background, center the circular badge, with the disc taking approximately 90% of the square canvas. A fairly thin antique gold circular metal rim with a dark deep navy disc, an immediately recognizable stylized lavender-white flying bat-monster mask silhouette inside, two pointed wide wings and two eyes. Clear dramatic contrast, professional crisp game UI icon, readable at 22 pixels, strong simple silhouette and flat dark interior, very restrained rim highlights. Entire complete round outline in frame with no spikes outside the circle. No numbers, no words, no lettering, no additional objects, no drop shadow outside the circle.

---
## 레벨업 보상

경험치로 레벨이 오르면 전투를 멈추고 1·2·3 또는 카드 클릭으로 서로 다른 보상 세 장에서 하나를 고른다. 여러 레벨이 한 번에 오르면 각 선택을 차례로 표시한다. 선택 중에는 자동사격, 마법 카드, 비행, 웨이브 타이머와 적탄이 정지한다.

기본 후보 13종에는 공용 강화 9종과 아리아·리안의 첫 전용 노드 4종이 포함된다. 등급 비율은 75·20·5 시험값으로 적용한다. 최대 단계에 도달한 강화와 효과가 없는 회복은 제외하며, 후보 부족 시 서로 다른 즉시 보상으로 채운다. E 산탄을 고르면 공용 보호막과 아이콘을 교체하고 기존 E 보호 효과를 해제한다. 출격을 다시 시작하면 강화와 슬롯 상태가 초기화된다. 공격 드론·회전 칼날·보호 장치의 슬롯 연결과 강화 아이템 공유 상한은 14일차 범위다.

카드 레이아웃은 `LevelUpChoiceCard.prefab`과 씬의 `CombatHUD/LevelUpChoices`에서 편집할 수 있다.

---
## 검증

Unity 6000.3.21f1 Play Mode 32개 통과. 시간·웨이브·남은 적 수, 상단 80% 크기, R 슬롯과 다음 카드 경계, 연속 레벨업 정지·복귀, ESC 메뉴, 키 변경과 포기 후 재출격을 확인했다.
---
## 정지 화면과 키 변경

- ESC로 정지한다. 왼쪽의 계속하기·다시하기·설정·포기하기를 사용하며, 파란 안내선에 맞춰 왼쪽 열을 옮기고 오른쪽 조작·설정·포기 패널을 넓혔다.
- 키 변경에서 동작을 고르고 새 키를 누른다. 서로 겹치는 키는 적용 전에 알려 준다. 실제 조작과 키보드의 강조, 설명 옆 키캡, 전투 스킬 키가 같은 설정을 사용한다.
- 설정의 소리·감도 탭은 전체 음량, 음소거, 시점 감도를 제어한다. 개별 음악·효과음·음성 트랙은 현재 시험 씬에 없다.
- 제작한 평면 키보드 원본은 [SVG](../art/flight-pause-keyboard.svg)에서 편집할 수 있다. 버튼 아이콘 원본은 내장 이미지 생성 도구에서 제작했다. 한국어 설명에는 나눔고딕과 함께 배포되는 SIL OFL 1.1을 적용했다.
- Q·E·R 묶음을 오른쪽으로 옮겨 마지막 슬롯의 끝을 오른쪽 다음 카드와 맞췄다.

> A horizontal strip sprite atlas made of exactly four square equally sized cells in a single row for Project G, an original naval sky-fantasy PC game. Each cell has one gold brass line icon centered on pure solid very dark navy, consistent thick readable stroke, spare contrast for 24px: left PLAY triangle pointing right, second a single circular RESTART arrow, third a SETTINGS cog, fourth an EXIT door with outward pointing arrow. Very simple flat crisp UI, simple inked line symbols with subtle gold highlights. Equal four cells separated solely by dark space; no borders, no letters, no numbers, no panels, no labels. Exact 4x1 output composition, each icon comfortably inside its own quarter.

Windows 1100×620·1920×1080에서 상단 HUD 및 정지 메뉴를 캡처했다. 마우스로 설정을 열어 Q를 G로 변경하고, 키보드 강조·설명 키캡·스킬 표시와 G 스킬 발동을 확인했다. 소리·감도 탭의 음량 66%와 감도 130% 표기도 캡처했다.
