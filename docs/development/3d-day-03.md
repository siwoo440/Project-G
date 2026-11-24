---
# 3D 3일차 수동 조준과 아군 사격

---
## 구현

화면 중심의 실제 광선이 닿는 곳을 빗자루 총구에서 조준하도록 보정했다. 좌클릭은 기본탄, Shift와 좌클릭은 집중탄을 발사한다. 우클릭 둘러보기와 복귀 중에는 조준과 사격을 잠시 중지한다. 탄환과 두 표적은 프리팹·씬에서 편집할 수 있다.

---
## 검증

- Unity Play Mode 7개 통과. 가까운 표적에 일반탄 8, 높고 먼 표적에 집중탄 20의 시험 피해를 각각 확인했다.
- Windows 1920×1080 시험 빌드에서 실제 좌클릭 발사체 생성과 제작 HUD 화면을 확인했다.
- 직접 조작감과 다른 해상도의 조준점 가독성은 검증 전이다. 4일차에는 적탄 피격·그레이즈와 무적·실패 상태를 추가한다.

---
## HUD 원본

내장 이미지 생성으로 제작한 두 투명 PNG 원본을 적용했다. Unity 씬에서 위치와 크기를 수정할 수 있다.

- `Assets/ProjectG/FlightPrototype/FlightUI/FlightReticle.png`
- `Assets/ProjectG/FlightPrototype/FlightUI/FlightControlBackplate.png`

---
### 이미지 생성 프롬프트

- Create a production-ready transparent PNG overlay for Project G, an original fantasy aerial shooter. One very compact optical reticle in the exact mathematical center of a square transparent canvas. It should occupy about 65% of the canvas width and height. Four equally spaced tiny pointed glyphs at north/east/south/west, with a delicate thin broken circular outline in cool ivory and muted glacial cyan. Perfectly symmetrical, flat orthographic digital game UI, precisely aligned and clean, readable when scaled to 40 screen pixels. Keep the inner 45% completely transparent: the player targets an enemy in this central empty area. All exterior space fully transparent. Virtually no glow and no background, text, frame outside, logo, numbers, mockup, scenery or other graphics. A single UI sprite centered on the canvas with true alpha.
- Create one production-ready transparent PNG UI backplate sprite for Project G, an original fantasy aerial shooter about floating islands. One single wide horizontal slim rectangular dark navy glass control legend bar, target 4:1 aspect of the actual bar, centered in a wide landscape transparent canvas. Let the object use 90% of the canvas width; all space outside its silhouette must be fully transparent. Near-flat original art direction with slender warm brass keyline along its outer edge, small subtle cyan corners, a calm nearly opaque dark slate navy center absolutely clear for later live white game text, restrained lineweight and small decorative corner cuts. Orthographic flat 2D asset, crisp at small scale. No text, letters, symbols, icons, grid, screenshot, render mockup or separate glyphs. Clean empty long horizontal plaque. No drop shadow spilling far outside.
