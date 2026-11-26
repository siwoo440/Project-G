using UnityEngine; // 마법 카드의 위치와 자원
using UnityEngine.InputSystem; // 좌우 마우스 클릭
using UnityEngine.UI; // 겹쳐서 구성한 실제 카드 이미지

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(140)] // 입력과 스킬 자원 적용 뒤 카드 사용
    public sealed class FlightMagicDeck : MonoBehaviour // 좌우 독립 전투 카드 회전
    {
        [System.Serializable] // 씬에서 완성한 카드 데이터
        private sealed class Card // 카드의 효과와 그림 조각
        {
            public string title; // 씬 편집용 카드 이름
            public FlightMagicEffect effect; // 사용할 실제 마법
            public int manaCost; // 보석에 표시할 실제 비용
            public Texture2D illustration; // 테두리 없는 마법 일러스트
            public Rect illustrationUv; // 원본의 일러스트 범위
            public Texture2D element; // 첨부한 카드 속성 아이콘
        }

        [System.Serializable] // 실제 씬의 카드 이미지 그룹
        private sealed class CardView // 틀·속성·보석을 위에 겹치는 표시
        {
            public RectTransform root; // 현재 카드의 배치 위치
            public RawImage art; // 뒤쪽의 원본 마법 그림
            public RawImage symbol; // 위쪽의 속성
            public Text cost; // 보석 안의 실제 마나 숫자
            public Text title; // 카드의 이름
            public CanvasGroup tint; // 현재 마력이 부족할 때 어두운 표시
            private Vector2 resting; // 씬에서 편집한 기본 위치
            private float arriveAt; // 다음 카드 등장 연출 종료
            private bool available; // 색 변경용 이전 자원 상태
            private bool initialized; // 표시가 초기화되었는지 여부

            public void Initialize() // 기존 카드 배치 기억
            {
                resting = root.anchoredPosition; // 카드 본래 위치
            }

            public void Set(Card card) // 현재 또는 다음 카드의 개별 레이어 교체
            {
                art.texture = card.illustration; // 테두리 없는 그림
                art.uvRect = card.illustrationUv; // 해당 원본 영역
                symbol.texture = card.element; // 독립된 속성 아이콘
                cost.text = card.manaCost.ToString(); // 비용 표시와 실제 소비의 공통 수치
                title.text = card.title; // 카드 이름 표시
            }

            public void Enter() // 다음 카드가 아래에서 올라오는 연출
            {
                arriveAt = Time.time + 0.18f; // 이동 종료 시점
            }

            public void SetCrafted(string name, int mana, Texture2D element, Card source) // 조립한 마법의 레이어
            {
                Set(source); // 같은 속성의 기존 일러스트와 카드 틀
                symbol.texture = element; // 실제 제작 마법의 속성
                title.text = name; // 제작한 마법 이름
                cost.text = mana.ToString(); // 실제 소비하는 마력
            }

            public void Render(bool usable, bool preview) // 레이어의 색과 카드 전환 위치
            {
                if (available != usable || !initialized) // 자원 색이 바뀌었는지 확인
                {
                    tint.alpha = preview ? (usable ? 0.79f : 0.42f) : (usable ? 1f : 0.52f); // 현재 카드와 다음 카드의 구분
                    available = usable; // 최신 사용 가능 상태
                    initialized = true; // 초기 밝기 반영 완료
                }
                float left = Mathf.Clamp01((arriveAt - Time.time) / 0.18f); // 등장 이동의 남은 비율
                root.anchoredPosition = resting + Vector2.down * ((root.rect.width * 17f / 92f) * left * left); // 부드럽게 원래 위치로 올라오는 현재 카드
            }
        }

        [System.Serializable] // 좌우마다 독립된 순서와 미리보기
        private sealed class Lane // 클릭 한 쪽의 카드 순환
        {
            public CardView active = new CardView(); // 현재 사용할 큰 카드
            public CardView next = new CardView(); // 바깥쪽에 있는 작은 다음 카드
            public Card[] cards; // 한쪽의 반복 카드 순서
            public int Index { get; private set; } // 현재 순환 위치
            private string craftedId; // 지금 그린 제작 마법 식별자

            public void Initialize() // 큰 카드와 미리보기 위치 기록
            {
                active.Initialize(); // 현재 카드 위치
                next.Initialize(); // 다음 카드 위치
                Reset(); // 첫 카드와 다음 카드 표시
            }

            public void Reset() // 새 출격의 원래 카드 순서
            {
                Index = 0; // 첫 번째 카드
                Refresh(); // 두 카드 레이어 적용
            }

            public void TryUse(FlightSkillController magic) // 성공한 카드만 다음 순서
            {
                Card card = cards[Index]; // 현재 표시와 같은 카드
                if (magic.TryUseCard(card.effect, card.manaCost)) // 실제 마나 소모 및 효과 성공
                {
                    Index = (Index + 1) % cards.Length; // 다음 카드가 현재 카드가 됨
                    Refresh(); // 새 현재·다음 카드 표시
                    active.Enter(); // 새 카드가 올라오는 이동
                }
            }

            public void Render(int mana, CraftedSpellRecipe recipe, FlightCraftedSpellBook book, int slot, Lane opposite) // 기존 순환 또는 직접 편성한 마법
            {
                if (book != null) // 실제 출격의 제작 마법 두 칸
                {
                    next.root.gameObject.SetActive(false); // 한 장 이하인 덱의 미리보기 숨김
                    active.root.gameObject.SetActive(recipe != null); // 빈 슬롯은 카드 자체를 숨김
                    if (recipe == null) // 편성하지 않은 실제 전투 칸
                    {
                        craftedId = null; // 다음에 장착할 마법의 표시 준비
                        return; // 시험 카드와 시전 가능 표시 없음
                    }
                    if (craftedId != recipe.Id) // 제작 마법을 새로 편성한 상태
                    {
                        craftedId = recipe.Id; // 변경 시 한 번만 이미지 적용
                        Texture2D symbol = book.Symbol(recipe); // 실제 저장한 속성 이미지
                        Card art = FindArt(symbol) ?? opposite.FindArt(symbol) ?? cards[Index]; // 같은 원소의 기존 그림 우선
                        active.SetCrafted(FlightCraftedSpellBook.NameOf(recipe), FlightCraftedSpellCaster.Stats(recipe).Mana, symbol, art); // 저장 조합의 그림·마력·이름
                        next.root.gameObject.SetActive(false); // 고정 장착 제작 마법은 순환 없이 유지
                    }
                    active.Render(book.CanCast(slot), false); // 마력과 재사용 시간의 어둡기
                    return; // 고정 장착과 이전 시험 덱을 같이 표시하지 않음
                }
                if (craftedId != null) // 제작 슬롯을 해제한 상태
                {
                    Refresh(); // 시험 카드와 미리보기 복원
                }
                active.Render(mana >= cards[Index].manaCost, false); // 현재 카드의 마력 상태
                next.Render(mana >= cards[(Index + 1) % cards.Length].manaCost, true); // 작은 미리보기의 마력 상태
            }

            private Card FindArt(Texture2D element) // 같은 원소의 이미 그린 카드 일러스트
            {
                foreach (Card entry in cards) // 이 줄의 기존 카드
                {
                    if (entry.element == element) // 속성 일치
                    {
                        return entry; // 원래 레이어를 그대로 재사용
                    }
                }
                return null; // 반대쪽에서 조회할 카드
            }

            private void Refresh() // 실제 순서에서 두 표시 갱신
            {
                craftedId = null; // 기본 카드의 표시 상태
                next.root.gameObject.SetActive(true); // 순환 후보를 다시 표시
                active.Set(cards[Index]); // 지금 사용할 카드
                next.Set(cards[(Index + 1) % cards.Length]); // 사용 후 올라올 카드
            }
        }

        [SerializeField] private FirstFlightController pilot; // 마우스 잠금과 독립 시점 상태
        [SerializeField] private FlightSkillController magic; // 공유 마력과 실제 효과
        [SerializeField] private Lane left = new Lane(); // 좌클릭 카드
        [SerializeField] private Lane right = new Lane(); // 우클릭 카드
        [SerializeField] private FlightCraftedSpellBook crafted; // 실전 항로의 선택형 제작 마법

        public int LeftIndex => left.Index; // 왼쪽 순환 검증 상태
        public int RightIndex => right.Index; // 오른쪽 순환 검증 상태

        private void Awake() // 씬에서 편집한 카드 초기화
        {
            left.Initialize(); // 왼쪽 현재 카드와 미리보기
            right.Initialize(); // 오른쪽 현재 카드와 미리보기
        }

        private void LateUpdate() // 실제 클릭과 카드 표시
        {
            if (Time.timeScale == 0f) // 강화 선택 클릭을 전투 카드가 소비하지 않음
            {
                return; // 모달이 열린 동안 마법 덱 유지
            }

            Mouse mouse = Mouse.current; // 마우스의 실제 상태
            if (pilot.HasPointerControl && !pilot.PointerCapturedThisFrame && mouse != null) // 재잠금 클릭이 아닌 실제 조작
            {
                if (mouse.leftButton.wasPressedThisFrame) // 왼쪽 현재 카드 선택
                {
                    UseLane(left, 0); // 왼쪽 카드만 사용
                }
                if (mouse.rightButton.wasPressedThisFrame) // 오른쪽 현재 카드 선택
                {
                    UseLane(right, 1); // 오른쪽 카드만 사용
                }
            }
            left.Render(magic.Mana, crafted?.ActiveSlot(0), crafted, 0, right); // 왼쪽 카드 실시간 표시
            right.Render(magic.Mana, crafted?.ActiveSlot(1), crafted, 1, left); // 오른쪽 카드 실시간 표시
        }

        private void UseLane(Lane lane, int index) // 편성 정보에 맞는 실제 클릭 처리
        {
            if (crafted != null) // 실제 출격에서 사용하는 제작 마법 도감
            {
                crafted.Use(index); // 빈 칸이면 아무 동작 없이 유지
            }
            else // 제작 마법 도감이 없는 독립 비행 시험 덱
            {
                lane.TryUse(magic); // 지금까지의 독립 카드 순환
            }
        }

        internal void ResetRun() // 새 출격의 카드 순서 복구
        {
            crafted?.ResetRun(); // 새 출격 이전 제작 마법 정리
            left.Reset(); // 왼쪽 덱 최초 그림
            right.Reset(); // 오른쪽 덱 최초 그림
        }
    }
}
