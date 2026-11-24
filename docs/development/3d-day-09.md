---
# 3D 9일차 자동 발사와 전투 마법 카드

---
## 조작과 카드

기본탄은 자동으로 발사되며 Shift를 누르면 집중탄으로 바뀐다. 왼쪽과 오른쪽 마우스 버튼은 각각 한 덱의 현재 카드를 사용한다. 성공하면 그 덱의 다음 카드가 아래에서 올라온다. 자원이 부족하면 현재 카드와 순서가 유지된다. 휠 버튼을 누른 동안 시점만 돌아가고, Backspace는 시험 출격을 다시 시작한다.

| 덱 | 순환하는 카드와 시험 마나 비용 |
|---|---|
| 왼쪽 | ARC 불꽃 12 → LANCE 빛 18 |
| 오른쪽 | AEGIS 물 14 → NOVA 자연 20 → SHADE 어둠 16 |

카드 위에 정사각형 Q·E·R을 가로로 배치했다. Q는 다발 마법탄, E는 짧은 보호, R은 그레이즈로 모은 필살기 에너지를 사용하는 넓은 사격이다. 좌하단 시험 안내의 입력도 갱신했다.

---
## 제작 이미지와 선명도

사용자가 제공한 `MagicCardFrame.png`, `MagicCardManaGem.png`, `CardElement*.png` 원본을 Unity에서 별도 레이어로 겹친다. 왼쪽 위 보석은 실제 마나 비용을 흰색 숫자로 보여주고, 오른쪽 위는 각 속성 문장이다. `MagicCardElementsArt.png`, `MagicCardLightArt.png`, `MagicCardDarkArt.png`는 틀과 보석이 없는 내장 이미지 생성 일러스트다. 마법 카드와 스킬 배치는 Hierarchy에서 편집할 수 있다.

오버레이를 픽셀 기준으로 정렬하고 작은 창에서 수치와 입력 글자를 키웠다. PNG는 압축과 밉맵을 끄고 원래 비율로 가져온다. 프로젝트 C#과 플레이 모드 테스트의 중괄호만 있는 줄에는 주석을 제거했다.

---
## 확인한 시험

- Unity Play Mode 19개 통과. 클릭 없는 자동 명중, 좌우 독립 순환, 어둠 아이콘과 마나 16, 마나 부족 시 순서 보존, Q 발동과 정사각형 스킬 배치를 확인했다.
- Windows 시험 빌드 1100×620과 1920×1080에서 자동 명중 5회, LANCE·SHADE, 보석 16, 흰색 Q 카운트다운을 확인했다. 두 해상도의 실제 화면을 저장했다.
- 사용자의 Unity Game 뷰 확대 비율, 수동 체감과 다른 PC는 검증 전이다. 카드 비용과 전투 수치는 시험값이다.

---
## 카드 그림 생성 프롬프트

- Create one horizontal sprite atlas with exactly three evenly distributed tall rectangular portrait illustrations for Project G, an original aerial-fantasy magic card game. Each portrait takes up its own equal-width third, with clear straight transparent gutters and no overlap. Portraits only, no card frame, text, numbers, icons, logos, transparent borders inside a portrait, or UI. On the LEFT, three warm orange-red fire bolts race through a smoky dark sky, readable compact arrangement. In the CENTER, a circular shimmering cyan-water barrier with aquatic flowing rings in a dark navy void. On the RIGHT, an expanding luminous emerald-green nature shockwave with leaf-shaped light in a dark violet sky. Tall vertical interiors, a bold simple centered focal subject legible at 60x90 pixels, restrained background, three wholly distinct palettes. True alpha between portraits. The art will be placed inside an existing ornate gold card frame.
- Create a single tall 2:3 portrait illustration for the interior art of an original fantasy aerial magic card in Project G. This is ART ONLY, full bleed, no frame, no rounded border, no cost gemstone, no element badge, no symbols on the corners, no letters, no text, no numbers and no UI. Show a majestic focused lance of golden daylight emerging from a white-gold radial arcane lightburst, bright sharp central focal shape with several slender streaks, distant lavender storm clouds and deep indigo negative space near the edges. Original richly painted luminous high fantasy spell, precise bright center, restrained composition and legibility at 70 pixels wide, like one in-game magic effect illustration. Include no card components in the image whatsoever.
- Create only the full-bleed tall 2:3 portrait interior illustration for the SHADE spell in an original fantasy aerial magic card game. A narrow fan of five sharp luminous amethyst slivers peels out of a nearly black violet vortex suspended in a dark blue storm sky. Clear bold violet core and clean silhouette, painterly luminous dark fantasy, refined contrast to remain visible at 68 pixels, no border, no frame, no circular icon, no badge, no jewel, no mana gem, no typography, no number, no UI, no ornamental corner. This is just the spell art without card parts.
