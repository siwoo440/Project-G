---
# 3D 20일차 빗자루 조립 UI와 저장

---
## 조립 화면

하늘섬 외곽 항로의 `Esc → 빗자루 조립`에서 현재 조종사의 여덟 부위를 변경할 수 있다. 이 진입점은 출격 준비 화면을 연결하기 전의 시험 진입점이다. 조립 UI는 `CombatHUD/PauseMenu/BroomWorkshop`에 배치했다. 후보를 누르면 회전하는 3D 빗자루의 재질·크기와 현재 장착 대비 능력치 차이가 바뀐다. 해제를 미리 보고 확정하거나 뒤로 가서 취소할 수 있다.

같은 종류의 보유 부품은 개별 번호로 표시한다. 다른 캐릭터가 장착 중인 부품은 이름을 보여 주고 확정을 막는다. 그래픽은 조립 패널용으로 생성한 이미지와 기존 메뉴 버튼 이미지를 사용한다. [조립 패널 이미지](../../Assets/ProjectG/Brooms/WorkshopFrame.png)

내장 이미지 생성 도구 입력

> Create one game UI raster panel frame for Project G, an original magical broom flight action game. Wide landscape 16:9 rectangular framing, front-on flat vector-quality crisp artwork. Deep opaque ink navy interior solid near #101d2d with a very subtle large-scale texture, slim warm antique brass edge, tiny teal enamel corner accents. Use restrained elegant aviation workshop and arcane engineering feel through precise metal lines and almost flush brackets. Leave the center entirely empty and quiet with NO text, runes, headings, icons, buttons, divisions, image subjects, or objects. Outer perimeter closely matches the image bounds without large padding. Real transparency ONLY outside the tiny outer rounded corners, interior completely opaque, no shadow outside. Dark enough for white Korean text overlays. Production-friendly readable at 1024×576, single frame image.

---
## 장착 저장

장착을 확정하면 7명의 슬롯별 개별 ID를 한 항목으로 저장한다. 재실행 시 저장한 슬롯 개수·부위 일치·보유 여부·중복 여부를 확인한 뒤 전체 조합을 적용한다. 유효하지 않은 저장은 시작 장비를 유지한다. 이름을 붙여 저장하는 별도 프리셋, 제작 마법과 함께 저장하는 규칙, 마을 공방 연결은 남아 있다.

---
## 검증

Unity 6000.3.21f1 Play Mode 47개 통과. 실제 조립 버튼 진입, 교체 미리보기와 능력치 차이, 취소, 장착 확정, 씬을 다시 열었을 때 복원, 중복 ID가 있는 저장의 적용 차단을 확인했다.

Windows 1920×1080에서 질풍의 몸체를 저장한 후 프로그램을 종료했다. 별도 실행 프로세스의 1024×576 화면에서 같은 개별 부품이 복원됐다. 두 시험은 사용자 설정과 분리한 키를 사용했다. [부품 미리보기](../screenshots/3d-day-20-workshop.png) · [재실행 후 복원](../screenshots/3d-day-20-reload-1024.png)

21일차에는 제작 마법의 고정 비용, 구성 ID, 호환성과 탄환·분열 상한을 개발한다.
