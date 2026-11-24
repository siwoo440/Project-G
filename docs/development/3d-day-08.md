---
# 3D 8일차 전투 게이지와 스킬 슬롯

---
## 구현

제작한 직사각형 프레임과 색상 이미지를 사용해 HP·마력·필살기 에너지·LV/XP를 전장 왼쪽 위에 얹었다. J·K·L은 같은 크기의 정사각형에 배치했다. 자원이 부족하거나 대기 중이면 아이콘을 어둡게 하고, 대기 시간은 흰색 정수로 표시한다. 자원·경험치·쿨타임은 8일차 표시 시험값을 받을 수 있고 실제 소모와 실행은 9일차부터 연결한다.

---
## 검증

- Unity Play Mode 17개 통과. 실제 피격 HP, 생성 이미지 연결, 자원 부족 실루엣, 흰색 정수 카운트다운과 종료를 확인했다.
- Windows 1920×1080 시험 화면에 실제 HP 80, 시험 XP 4와 쿨타임 4를 확인했다.
- 다른 해상도와 사람의 조작은 검증 전이다.

---
## 제작 이미지

내장 이미지 생성으로 `FlightUI/FlightResourcesAtlas.png`, `FlightUI/FlightSkillsAtlas.png`를 제작했다.

- Create one transparent PNG sprite atlas for Project G's fantasy aerial shooter HUD. Exactly three long thin completely straight rectangular unframed bars stacked in three equally spaced rows, with clear transparent gutters. Upper third contains only an icy cyan blue mana stripe; exact middle contains only a saturated violet purple ultimate energy stripe; lower third contains only an amber-gold yellow experience stripe. All bars identical dimensions, occupy 85% of horizontal canvas width, square corners, subtle glossy vertical gradient, strong color at small scale, flat 2D game UI. The palette and design should be refined, readable, restrained. No frames, letters, icons, numbers, borders outside each bar, other graphics, glow spill, gradients in the empty background. Genuinely transparent background.
- Generate one original transparent PNG horizontal sprite atlas for three square Project G fantasy aerial shooter ability icons. Exactly THREE equally-sized isolated square tiles, each centrally placed in one equal-width column, no overlapping or touching, transparent gutters. Each tile is intentionally square with restrained thin brass edge, dark navy background and crisp fantasy illustration. The left icon shows a converging volley of 3 bright icy cyan arcane arrows. The middle icon shows a luminous sky blue magical round shield with a central prism. The right icon shows a bright gold-violet nova with radial rays. Keep all essential details inside the frame, leave some negative space near bottom left for an in-game keybind. Crisp legibility at 78 pixels. No letters, labels, text, numbers, surrounding UI or additional tiles. True alpha outside the tile silhouettes.
