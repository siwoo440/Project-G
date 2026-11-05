using System; // 난수 기능
using System.Collections.Generic; // 카드 목록 기능
using System.Linq; // 후보 필터 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum CardId // 카드 식별자
    { // 식별자 시작
        C01, C02, C03, C04, C05, C06, // 공용 공격과 스킬
        C07, C08, C09, C10, C11, C12, // 공용 보조와 아이템
        R01, R02, S01, S02, // 캐릭터 전용
        F01, F02, F03, F04, F05 // 후보 부족 대체
    } // 식별자 종료
    public enum CardTier // 카드 시험 등급
    { // 등급 시작
        Normal, Rare, Hero // 일반 희귀 영웅
    } // 등급 종료
    public enum CardCategory // 카드 종류
    { // 종류 시작
        Attack, Skill, Support, Item, Survival, Character // 카드 분류
    } // 종류 종료
    public sealed class CardData // 카드 시험 데이터
    { // 클래스 시작
        public CardId Id // 카드 식별자
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public string Name // 카드 이름
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public string Description // 카드 효과 설명
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public CardTier Tier // 카드 등급
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public CardCategory Category // 카드 분류
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public int Maximum // 최대 강화 단계
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public CharacterType? Owner // 전용 캐릭터
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public CardData(CardId id, string name, string description, CardTier tier, CardCategory category, int maximum, CharacterType? owner = null) // 카드 데이터 생성
        { // 생성 시작
            Id = id; // 식별자 보관
            Name = name; // 이름 보관
            Description = description; // 효과 설명 보관
            Tier = tier; // 등급 보관
            Category = category; // 분류 보관
            Maximum = maximum; // 최대 단계 보관
            Owner = owner; // 전용 캐릭터 보관
        } // 생성 종료
    } // 클래스 종료
    public sealed class CardInventory // 전투 중 카드 보유
    { // 클래스 시작
        private readonly Dictionary<CardId, int> levels = new Dictionary<CardId, int>(); // 카드별 강화 단계
        public int Count(CardId id) // 카드 단계 조회
        { // 조회 시작
            return levels.TryGetValue(id, out int count) ? count : 0; // 보유 단계 반환
        } // 조회 종료
        public void Add(CardId id) // 선택 카드 기록
        { // 기록 시작
            levels[id] = Count(id) + 1; // 단계 증가
        } // 기록 종료
        public int ItemSlots => (Count(CardId.C09) > 0 ? 1 : 0) + (Count(CardId.C10) > 0 ? 1 : 0) + (Count(CardId.C11) > 0 ? 1 : 0); // 아이템 슬롯 사용 수
        public int SupportSlots => (Count(CardId.C07) > 0 ? 1 : 0) + (Count(CardId.C08) > 0 ? 1 : 0); // 보조 공격 슬롯 사용 수
    } // 클래스 종료
    public static class CardCatalog // 카드 목록과 후보 생성
    { // 클래스 시작
        private static readonly CardData[] initial = new CardData[] // 초기 시험 카드 목록
        { // 목록 시작
            new CardData(CardId.C01, "화력 조정", "기본·집중 피해 +10%", CardTier.Normal, CardCategory.Attack, 3), // 공용 피해
            new CardData(CardId.C02, "발사 조정", "발사 간격 -8%", CardTier.Normal, CardCategory.Attack, 3), // 공용 속도
            new CardData(CardId.C03, "관통탄", "기본·집중 관통 +1", CardTier.Rare, CardCategory.Attack, 3), // 공용 관통
            new CardData(CardId.C04, "집중 조율", "집중 공격 피해 +8%", CardTier.Normal, CardCategory.Attack, 3), // 집중 피해
            new CardData(CardId.C05, "고유기 순환", "J 쿨타임 -10%", CardTier.Normal, CardCategory.Skill, 3), // 고유 스킬
            new CardData(CardId.C06, "산탄 스킬", "K 보호막을 산탄으로 교체", CardTier.Rare, CardCategory.Skill, 1), // 공용 스킬
            new CardData(CardId.C07, "공격 드론", "자동 사격 피해 6 / 9 / 12", CardTier.Rare, CardCategory.Support, 3), // 보조 드론
            new CardData(CardId.C08, "회전 칼날", "주변 피해 8 / 12 / 16", CardTier.Rare, CardCategory.Support, 3), // 보조 칼날
            new CardData(CardId.C09, "자석", "경험치 흡수 반경 +20%", CardTier.Normal, CardCategory.Item, 3), // 경험치 자석
            new CardData(CardId.C10, "보호 장치", "피격 후 방어막 · 재충전 30초", CardTier.Rare, CardCategory.Item, 1), // 보호 장치
            new CardData(CardId.C11, "집속 렌즈", "집중 폭 -10% · 피해 +10%", CardTier.Rare, CardCategory.Item, 3), // 집속 렌즈
            new CardData(CardId.C12, "응급 회복", "체력 20 회복", CardTier.Normal, CardCategory.Survival, int.MaxValue), // 즉시 회복
            new CardData(CardId.R01, "추격 사격 1", "누적 감소 대기 2초 → 3초", CardTier.Rare, CardCategory.Character, 1, CharacterType.Rapid), // 속사 유지
            new CardData(CardId.R02, "확산 포화 1", "기본 공격 좌우 사격 폭 +20%", CardTier.Hero, CardCategory.Character, 1, CharacterType.Rapid), // 속사 확산
            new CardData(CardId.S01, "약점 사냥 1", "약점 추가 피해 +15%p", CardTier.Rare, CardCategory.Character, 1, CharacterType.Sniper), // 저격 약점
            new CardData(CardId.S02, "관통 궤도 1", "기본·집중 탄환 관통 +1", CardTier.Hero, CardCategory.Character, 1, CharacterType.Sniper) // 저격 관통
        }; // 목록 종료
        private static readonly CardData[] fallback = new CardData[] // 후보 부족 대체 보상
        { // 목록 시작
            new CardData(CardId.F04, "응급 보급", "체력 10 회복 · 시험값", CardTier.Normal, CardCategory.Survival, int.MaxValue), // 체력 대체 보상
            new CardData(CardId.F05, "에너지 보급", "필살기 에너지 5 충전 · 시험값", CardTier.Normal, CardCategory.Survival, int.MaxValue), // 에너지 대체 보상
            new CardData(CardId.F01, "안전 보급", "최대 체력 +1 및 1 회복", CardTier.Normal, CardCategory.Survival, int.MaxValue), // 대체 보상 하나
            new CardData(CardId.F02, "긴급 보급", "최대 체력 +2 및 2 회복", CardTier.Normal, CardCategory.Survival, int.MaxValue), // 대체 보상 둘
            new CardData(CardId.F03, "추가 보급", "최대 체력 +3 및 3 회복", CardTier.Normal, CardCategory.Survival, int.MaxValue) // 대체 보상 셋
        }; // 목록 종료
        public static IReadOnlyList<CardData> Initial => Array.AsReadOnly(initial); // 초기 카드 조회
        public static CardData Find(CardId id) // 카드 데이터 조회
        { // 조회 시작
            return initial.Concat(fallback).First(card => card.Id == id); // 카드 반환
        } // 조회 종료
        public static bool IsEligible(CardId id, CardInventory inventory, CharacterType character, CommonSkill skill, bool damaged, bool energyMissing = true) // 후보 가능 여부
        { // 확인 시작
            CardData card = Find(id); // 카드 데이터 확인
            if (card.Owner.HasValue && card.Owner.Value != character) // 캐릭터 확인
            { // 제외 시작
                return false; // 다른 캐릭터 제외
            } // 제외 종료
            if (inventory.Count(id) >= card.Maximum) // 최대 단계 확인
            { // 제외 시작
                return false; // 최대 강화 제외
            } // 제외 종료
            if (id == CardId.C06 && skill == CommonSkill.Scatter) // 공용 스킬 중복
            { // 제외 시작
                return false; // 같은 장착 스킬 제외
            } // 제외 종료
            if ((id == CardId.C12 || id == CardId.F04) && !damaged) // 최대 체력 확인
            { // 제외 시작
                return false; // 불필요한 회복 제외
            } // 제외 종료
            if (id == CardId.F05 && !energyMissing) // 에너지 상한 확인
            { // 제외 시작
                return false; // 불필요한 충전 제외
            } // 제외 종료
            if ((id == CardId.C07 || id == CardId.C08) && inventory.Count(id) == 0 && inventory.SupportSlots >= 2) // 보조 공격 슬롯 확인
            { // 제외 시작
                return false; // 보조 슬롯 초과 제외
            } // 제외 종료
            if ((id == CardId.C09 || id == CardId.C10 || id == CardId.C11) && inventory.Count(id) == 0 && inventory.ItemSlots >= 3) // 아이템 슬롯 확인
            { // 제외 시작
                return false; // 아이템 슬롯 초과 제외
            } // 제외 종료
            return true; // 유효 후보
        } // 확인 종료
        public static CardData[] Offer(CardInventory inventory, CharacterType character, CommonSkill skill, bool damaged, Random random, bool energyMissing = true) // 세 장 후보 생성
        { // 생성 시작
            List<CardData> available = initial.Where(card => IsEligible(card.Id, inventory, character, skill, damaged)).ToList(); // 유효 카드 목록
            List<CardData> selected = new List<CardData>(); // 선택지 목록
            while (selected.Count < 3 && available.Count > 0) // 후보 선택 반복
            { // 선택 시작
                List<CardData> pool = available; // 기본 후보
                if (selected.Count == 1 && available.Any(card => card.Category != selected[0].Category)) // 분류 다양성 확인
                { // 분류 처리 시작
                    pool = available.Where(card => card.Category != selected[0].Category).ToList(); // 다른 분류 우선
                } // 분류 처리 종료
                CardData card = Draw(pool, random); // 등급 가중 추첨
                selected.Add(card); // 선택지 추가
                available.Remove(card); // 중복 제거
            } // 선택 종료
            foreach (CardData card in fallback) // 대체 보상 순회
            { // 대체 시작
                if (selected.Count >= 3) // 선택지 개수 확인
                { // 완료 시작
                    break; // 대체 종료
                } // 완료 종료
                if (IsEligible(card.Id, inventory, character, skill, damaged, energyMissing)) // 대체 효과 유효 확인
                { // 추가 시작
                    selected.Add(card); // 서로 다른 대체 보상
                } // 추가 종료
            } // 대체 종료
            return selected.ToArray(); // 세 장 반환
        } // 생성 종료
        public static string DescribeChange(CardData card, BattlePlayer player) // 현재와 변경 수치 표시
        { // 설명 시작
            int level = player.Build.Cards.Count(card.Id); // 현재 카드 단계
            int next = level + 1; // 선택 후 카드 단계
            switch (card.Id) // 카드 종류 구분
            { // 구분 시작
                case CardId.C01: return "공용 피해 +" + level * 10 + "% → +" + next * 10 + "%"; // 기본 피해 변경
                case CardId.C02: return "발사 간격 -" + level * 8 + "% → -" + next * 8 + "%"; // 발사 간격 변경
                case CardId.C03: return "추가 관통 " + level + " → " + next; // 관통 변경
                case CardId.C04: return "집중 피해 +" + level * 8 + "% → +" + next * 8 + "%"; // 집중 피해 변경
                case CardId.C05: return "J 대기 감소 " + level * 10 + "% → " + next * 10 + "%"; // 고유기 변경
                case CardId.C06: return "보호막 1회·12초 → 산탄 3발·10초"; // 공용 스킬 비교
                case CardId.C07: return "드론 피해 " + (level == 0 ? 0 : 3 + level * 3) + " → " + (3 + next * 3); // 드론 피해 변경
                case CardId.C08: return "칼날 피해 " + (level == 0 ? 0 : 4 + level * 4) + " → " + (4 + next * 4); // 칼날 피해 변경
                case CardId.C09: return "흡수 반경 +" + level * 20 + "% → +" + next * 20 + "%"; // 흡수 반경 변경
                case CardId.C10: return "미장착 → 피격 후 1회 방어"; // 보호 장치 변경
                case CardId.C11: return "집중 폭 -" + level * 10 + "% → -" + next * 10 + "%"; // 집중 폭 변경
                case CardId.C12: return "체력 " + player.Survival.Health + " → " + Math.Min(player.Survival.MaximumHealth, player.Survival.Health + 20); // 즉시 회복량
                case CardId.R01: return "유지 시간 2초 → 3초"; // 누적 유지 변경
                case CardId.R02: return "기본 공격 좌우 폭 100% → 120%"; // 사격 폭 변경
                case CardId.S01: return "약점 추가 +50% → +65%"; // 약점 피해 변경
                case CardId.S02: return "기본·집중 추가 관통 +1"; // 저격 관통 변경
                case CardId.F01: return "최대 체력 " + player.Survival.MaximumHealth + " → " + (player.Survival.MaximumHealth + 1); // 대체 보상 하나
                case CardId.F02: return "최대 체력 " + player.Survival.MaximumHealth + " → " + (player.Survival.MaximumHealth + 2); // 대체 보상 둘
                case CardId.F03: return "최대 체력 " + player.Survival.MaximumHealth + " → " + (player.Survival.MaximumHealth + 3); // 대체 보상 셋
                case CardId.F04: return "체력 " + player.Survival.Health + " → " + Math.Min(player.Survival.MaximumHealth, player.Survival.Health + 10); // 대체 회복량
                case CardId.F05: return "에너지 " + player.Skills.Energy + " → " + Math.Min(100, player.Skills.Energy + 5); // 대체 충전량
                default: return ""; // 알 수 없는 카드
            } // 구분 종료
        } // 설명 종료
        private static CardData Draw(List<CardData> pool, Random random) // 등급 가중 추첨
        { // 추첨 시작
            int normal = pool.Any(card => card.Tier == CardTier.Normal) ? 75 : 0; // 일반 등급 가중치
            int rare = pool.Any(card => card.Tier == CardTier.Rare) ? 20 : 0; // 희귀 등급 가중치
            int hero = pool.Any(card => card.Tier == CardTier.Hero) ? 5 : 0; // 영웅 등급 가중치
            int roll = random.Next(normal + rare + hero); // 유효 등급 추첨
            CardTier tier = roll < normal ? CardTier.Normal : roll < normal + rare ? CardTier.Rare : CardTier.Hero; // 선택 등급
            List<CardData> candidates = pool.Where(card => card.Tier == tier).ToList(); // 등급 내 후보
            return candidates[random.Next(candidates.Count)]; // 등급 내 무작위 선택
        } // 추첨 종료
    } // 클래스 종료
} // 영역 종료
