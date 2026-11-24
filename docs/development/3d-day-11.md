---
# 3D 11일차 리안 저격형과 3D 약점 판정

---
## 시험 조작

- F2로 아리아와 리안의 전투 설정을 전환한다. 같은 시험 비행 모델을 쓰며 현재 체력·마력과 슬롯의 쿨타임은 이어진다.
- 리안이 Shift로 집중 조준할 때 노출 약점을 가리키면 조준점이 금색으로 바뀐다. 일반 마물 프리팹의 `WeakPoint`를 Hierarchy에서 편집할 수 있다.
- 일반·집중 자동 사격 간격은 각각 0.30초·0.48초로 시험한다. 집중탄은 몸통 20, 약점 30 피해를 준다. 리안의 공격이 별도 약점 판정에 처음 닿으면 피해를 50% 늘린다.

| 입력 | 리안 시험 스킬 |
|---|---|
| Q | 관통 사격. 마력 25, 쿨타임 10초. 피해 100의 탄 한 발이 직선상의 각 적에게 한 번씩 피해를 주며 지형 앞에서 멈춘다. |
| E | 공용 보호막. 마력 30, 쿨타임 12초. 1초 안에 처음 받는 피해 한 번을 막는다. |
| R | 천공 관측선. 에너지 100, 쿨타임 12초. 지형 앞까지 각 적에게 피해 220을 주고 광선 내부 적탄을 제거한다. |

---
## 검증

- Unity 6000.3.21f1 Play Mode 25개 통과. 캐릭터 전환과 아이콘 복귀, 몸통·약점 피해, Q의 개별 관통 명중, E의 첫 피해 방어, R의 지형 차단과 적탄 소거를 확인했다.
- Windows 시험 빌드 두 해상도에서 F2, Q 피해 100, R 피해 220, 광선 밖 적탄 유지, 전용 아이콘을 확인했다.
- 리안의 별도 캐릭터 모델, 실제 수동 비행 체감, 보스와 성장 노드의 관통 추가 효과는 확인 전이다.

---
## 제작 이미지

내장 이미지 생성으로 만든 `Assets/ProjectG/FlightPrototype/FlightUI/RianSkillsAtlas.png`의 왼쪽과 오른쪽을 Q와 R에 사용했다. 프롬프트는 다음과 같다.

> Create a horizontal sprite atlas for Project G, containing only two approximately square side-by-side spell pictures, each entirely occupying exactly its own equal-width half. No padding, no borders, no type, no UI, no separators. LEFT: one striking icy teal sniper bolt passing through two thin shattered rings, sharp upward diagonal from lower left to top right, deep inky navy behind it. RIGHT: an intense white-gold thin celestial observation beam with a broken gold reticle, strong straight diagonal and warm bright sparks on deep purple background. These are distinct square in-game skill icon illustrations readable at 48 pixels. Dark fantasy painterly digital style, simple bold visual shapes, no character or firearms.
