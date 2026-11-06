using UnityEngine; // 유니티 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class CardBuild // 전투 중 카드 효과
    { // 클래스 시작
        public CardInventory Cards // 선택한 카드 상태
        { // 속성 시작
            get; // 값 조회
        } = new CardInventory(); // 카드 보유 초기화
        public UpgradeItemState Upgrades // 강화 아이템 보유 상태
        { // 속성 시작
            get; // 값 조회
        } = new UpgradeItemState(); // 아이템 보유 초기화
        public float Damage(float baseDamage, bool focused) // 기본 공격 피해 계산
        { // 계산 시작
            float bonus = Cards.Count(CardId.C01) * 0.10f + Upgrades.DamageBonus; // 카드와 아이템 피해 합산
            if (focused) // 집중 공격 확인
            { // 집중 계산 시작
                bonus += Cards.Count(CardId.C04) * 0.08f + Cards.Count(CardId.C11) * 0.10f; // 집중 피해 합산
            } // 집중 계산 종료
            return baseDamage * (1f + Mathf.Min(0.6f, bonus)); // 공유 시험 상한 적용
        } // 계산 종료
        public float IntervalScale => 1f - Mathf.Min(0.4f, Cards.Count(CardId.C02) * 0.08f + Upgrades.FireRateBonus); // 발사 간격 감소 상한
        public int ExtraPierce => Mathf.Min(3, Cards.Count(CardId.C03) + Cards.Count(CardId.S02) + Upgrades.PierceBonus); // 추가 관통 상한
        public float UniqueCooldownReduction => Mathf.Min(0.4f, Cards.Count(CardId.C05) * 0.10f + Upgrades.UniqueCooldownBonus); // 고유 스킬 쿨타임 감소
        public float WeakMultiplier => 1.5f + Cards.Count(CardId.S01) * 0.15f; // 저격 약점 피해 배율
        public float FocusWidth => Mathf.Max(0.1f, 1f - Cards.Count(CardId.C11) * 0.10f); // 집중탄 폭 시험 비율
        public float PickupRadius => 1.5f * (1f + Cards.Count(CardId.C09) * 0.20f); // 경험치 회수 시험 반경
        public int DroneLevel => Cards.Count(CardId.C07); // 드론 강화 단계
        public int BladeLevel => Cards.Count(CardId.C08); // 칼날 강화 단계
    } // 클래스 종료
} // 영역 종료
