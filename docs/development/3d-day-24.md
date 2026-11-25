---
# 3D 24일차 캐릭터 선택과 출격 준비

---
## 출격 준비 씬

메인 메뉴와 실전 항로 사이에 `FlightSortieBriefing`을 추가했다. 왼쪽 위에 뒤로가기, 오른쪽 아래에 출발, 왼쪽에 스탠딩, 오른쪽에 캐릭터와 현재 조합을 배치했다. 아리아와 리안은 기획서 3.1·3.2에 들어 있는 투명 스탠딩 원본을 사용한다. 설명에는 현재 3D 빌드의 Q·E·R 스킬을 표시한다.

[A/D]와 왼쪽·오른쪽 키로 캐릭터를 바꾸고 Enter로 출발한다. 마우스로도 같은 버튼을 사용할 수 있다. 캐릭터 선택, 그림, 설명과 각 버튼은 `SortieBriefing/Ready`에서 편집한다.

---
## 실제 장착과 출격

조립 화면과 마법 편집기의 게임 오브젝트를 준비 씬에도 배치했다. 두 화면이 실전과 같은 부품과 저장 규칙을 사용한다. 준비 화면에서 저장한 부품과 마법은 오른쪽 설명에 바로 반영된다. 출발을 확정하면 실제 전투가 선택한 조종사의 자동탄·스킬·빗자루·마법을 초기화한다. 이어서하기는 마지막에 출발한 조종사를 먼저 표시한다.

공방 배경은 내장 이미지 생성 도구로 제작했다. [공방 배경](../../Assets/ProjectG/Briefing/BriefingHangar.png)

> A wide 16:9 original fantasy aerial workshop interior background, painted cinematic environment for Project G's Unity character selection screen. A moody lookout hangar above a blue ocean of clouds, low stone-and-brass floor, broad curved rear openings, a few small floating island hints and warm brass lamp accents at far edges only. Navy and muted teal dominant. Keep the left-central foreground an empty subdued low-detail platform with restrained warm rim light, where an existing transparent full-body character portrait will be layered. The right half should be deep, quiet ink blue with no bright center so Korean UI can be composed. Tiny atmospheric dust, subtle depth, refined original storybook 3D fantasy airfield. No people, no characters, no figures, no broom in the foreground, no letters or text, no emblems, no panels or UI.

---
## 검증

Unity 6000.3.21f1 Play Mode 68개 통과. 리안 스탠딩을 배치하고 조립·마법 화면에서 질풍의 몸체와 좌측 파이어 볼트를 확정한 뒤 실제 리안 전투에 반영되는지 확인했다.

Windows 1920×1080과 1024×576에서 타이틀 → 출격 준비 → 리안 선택 → 장비·마법 변경 → 전투를 검증했다. 실전 리안 기본 스킬 프로필, 질풍의 몸체 12.70 m/s와 좌측 파이어 볼트의 구성 비용 20을 기록했다. [아리아](../screenshots/3d-day-24-aria.png) · [리안](../screenshots/3d-day-24-rian.png) · [1024×576](../screenshots/3d-day-24-briefing-1024.png)

25일차는 전투 종료를 별도 결과 씬으로 연결하고, 같은 조합의 재출격과 타이틀 복귀를 검증한다.
