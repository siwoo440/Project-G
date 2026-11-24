---
# 3D 6일차 시험 마물과 피격 체력바

---
## 구현

유리 지느러미 시험 마물의 3D 머티리얼과 날개 메시를 만들고 씬에 프리팹 인스턴스를 배치했다. 날갯짓·부유·꼬리 흔들림과 핵의 피격광을 적용했다. 체력바는 첫 피격 때 마물 아래에 나타나며 마지막 피해 2초 후 다시 숨는다. 붉은 이미지는 남은 체력에 따라 왼쪽을 기준으로 줄어든다.

---
## 검증

- Unity Play Mode 14개 통과. 날개 움직임, 첫 피격 전 숨김, 기본탄 피해 8, 게이지 90%, 마지막 피격 2초 뒤 숨김을 확인했다.
- Windows 1920×1080 시험 빌드에서 실제 사격 후 마물 HP 72와 체력바 표시를 확인했다.
- 마물 모델·체력·표시 시간은 시험값이다. 사람이 조작했을 때의 가독성, 다른 화면 비율과 다수 마물 환경은 검증 전이다.

---
## 제작 이미지

내장 이미지 생성으로 `FlightUI/EnemyHealthFrame.png`와 `FlightUI/EnemyHealthFill.png`의 투명 PNG를 만들었다.

- Generate one original game UI asset for Project G, a fantasy aerial shooter: a slim, perfectly horizontal empty enemy HP track on a transparent background. Actual full track ratio 8:1, centered; dark opaque charcoal-blue inside, delicate and very clean warm dark-brass rim with tiny angular corner chamfers, low-profile ornament so it remains readable at 110 by 13 screen pixels. Large uninterrupted empty center, straight horizontal top and bottom. Flat orthographic 2D overlay only, crisp clear edges. No text, digits, icons, symbols, gradients outside the frame, no second item, no game scene. True alpha outside the track.
- Generate one original game UI asset for Project G, a fantasy aerial shooter: a long flat completely straight horizontal RECTANGULAR bright coral red enemy HP fill stripe, actual stripe 9:1 aspect ratio, precisely centered on genuinely transparent background and filling most of the canvas width. Solid readable crimson-coral red with a very subtle vertical gradient and tiny amber highlight line at the top. Exact square corners, constant height, no frame, no bevels, no text, no logo, no segments, no glow beyond the silhouette, no material outside the stripe. Orthographic flat UI texture that can be cropped horizontally to show HP.
