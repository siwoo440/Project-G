using System; // 난수 기능
using System.Linq; // 카드 목록 검사
using NUnit.Framework; // 테스트 도구
using ProjectG; // 카드 기능
public class CardOfferTests // 카드 후보 시험
{ // 시험 시작
    [Test] // 카드 목록 시험
    public void InitialPoolHasTwelveCommonAndFourCharacterCards() // 초기 카드 수 확인
    { // 시험 시작
        Assert.That(CardCatalog.Initial.Count, Is.EqualTo(16)); // 초기 카드 수 확인
        Assert.That(CardCatalog.Initial.Count(card => card.Owner == null), Is.EqualTo(12)); // 공용 카드 수 확인
        Assert.That(CardCatalog.Initial.Count(card => card.Owner == CharacterType.Rapid), Is.EqualTo(2)); // 속사 카드 수 확인
        Assert.That(CardCatalog.Initial.Count(card => card.Owner == CharacterType.Sniper), Is.EqualTo(2)); // 저격 카드 수 확인
    } // 시험 종료
    [TestCase(CharacterType.Rapid)] // 속사형 선택
    [TestCase(CharacterType.Sniper)] // 저격형 선택
    public void OffersThreeDistinctCompatibleCards(CharacterType character) // 후보 중복과 캐릭터 제한 확인
    { // 시험 시작
        CardInventory inventory = new CardInventory(); // 빈 카드 보유 상태
        Random random = new Random(42); // 재현 가능한 난수
        for (int index = 0; index < 100; index++) // 여러 차례 후보 생성
        { // 반복 시작
            CardData[] cards = CardCatalog.Offer(inventory, character, CommonSkill.Shield, true, random); // 카드 후보 생성
            Assert.That(cards.Length, Is.EqualTo(3)); // 세 장 확인
            Assert.That(cards.Select(card => card.Id).Distinct().Count(), Is.EqualTo(3)); // 중복 제외
            Assert.That(cards.All(card => card.Owner == null || card.Owner == character), Is.True); // 캐릭터 제한
            Assert.That(cards.Select(card => card.Category).Distinct().Count(), Is.GreaterThanOrEqualTo(2)); // 후보 분류 혼합
        } // 반복 종료
    } // 시험 종료
    [Test] // 선행 조건과 최대 레벨 시험
    public void PrerequisitesAndMaximumLevelsFilterOffers() // 선행 조건과 상한 확인
    { // 시험 시작
        CardInventory inventory = new CardInventory(); // 빈 카드 보유 상태
        Assert.That(CardCatalog.IsEligible(CardId.R02, inventory, CharacterType.Rapid, CommonSkill.Shield, true), Is.True); // 첫 분기 기본 후보
        Assert.That(CardCatalog.IsEligible(CardId.S02, inventory, CharacterType.Sniper, CommonSkill.Shield, true), Is.True); // 저격 첫 분기 기본 후보
        inventory.Add(CardId.R02); // 전용 카드 획득
        Assert.That(CardCatalog.IsEligible(CardId.R02, inventory, CharacterType.Rapid, CommonSkill.Shield, true), Is.False); // 전용 카드 최대 단계
        inventory.Add(CardId.C01); // 첫 강화
        inventory.Add(CardId.C01); // 둘째 강화
        inventory.Add(CardId.C01); // 셋째 강화
        Assert.That(CardCatalog.IsEligible(CardId.C01, inventory, CharacterType.Rapid, CommonSkill.Shield, true), Is.False); // 최대 레벨 제외
        Assert.That(CardCatalog.IsEligible(CardId.S01, inventory, CharacterType.Rapid, CommonSkill.Shield, true), Is.False); // 다른 캐릭터 제외
    } // 시험 종료
    [Test] // 장비와 회복 조건 시험
    public void FullHealthAndEquippedSkillAreExcluded() // 비활성 선택지 제외
    { // 시험 시작
        CardInventory inventory = new CardInventory(); // 빈 카드 보유 상태
        Assert.That(CardCatalog.IsEligible(CardId.C12, inventory, CharacterType.Rapid, CommonSkill.Shield, false), Is.False); // 최대 체력 회복 제외
        Assert.That(CardCatalog.IsEligible(CardId.C06, inventory, CharacterType.Rapid, CommonSkill.Scatter, true), Is.False); // 같은 공용 스킬 제외
    } // 시험 종료
    [Test] // 후보 부족 대체 보상 시험
    public void ExhaustedPoolProvidesThreeDistinctFallbacks() // 대체 보상 확인
    { // 시험 시작
        CardInventory inventory = new CardInventory(); // 카드 보유 상태
        foreach (CardData card in CardCatalog.Initial) // 초기 카드 순회
        { // 순회 시작
            if (card.Owner == CharacterType.Sniper || card.Id == CardId.C12 || card.Id == CardId.C06) // 제외 대상 확인
            { // 제외 시작
                continue; // 다음 카드 확인
            } // 제외 종료
            for (int level = 0; level < card.Maximum; level++) // 최대 단계까지 반복
            { // 반복 시작
                inventory.Add(card.Id); // 카드 보유 단계 증가
            } // 반복 종료
        } // 순회 종료
        CardData[] cards = CardCatalog.Offer(inventory, CharacterType.Rapid, CommonSkill.Scatter, false, new Random(1), false); // 후보 생성
        Assert.That(cards.Length, Is.EqualTo(3)); // 세 장 유지
        Assert.That(cards.Select(card => card.Id).Distinct().Count(), Is.EqualTo(3)); // 대체 보상 중복 방지
        Assert.That(cards.All(card => card.Id == CardId.F01 || card.Id == CardId.F02 || card.Id == CardId.F03), Is.True); // 안전 대체 보상 확인
        CardData[] mixed = CardCatalog.Offer(inventory, CharacterType.Rapid, CommonSkill.Scatter, true, new Random(1), true); // 체력과 에너지 부족 후보
        Assert.That(mixed.Any(card => card.Id == CardId.F05), Is.True); // 에너지 보급 제시
        Assert.That(mixed.Any(card => card.Id == CardId.F04), Is.True); // 체력 보급 제시
    } // 시험 종료
    [Test] // 희귀도 가중치 시험
    public void FirstOfferUsesInitialTierWeightsWhenAllTiersExist() // 시험 등급 비율 확인
    { // 시험 시작
        CardInventory inventory = new CardInventory(); // 카드 보유 상태
        Random random = new Random(99); // 재현 가능한 난수
        int normal = 0; // 일반 카드 횟수
        int rare = 0; // 희귀 카드 횟수
        int hero = 0; // 영웅 카드 횟수
        for (int index = 0; index < 10000; index++) // 후보 생성 반복
        { // 반복 시작
            CardData[] cards = CardCatalog.Offer(inventory, CharacterType.Sniper, CommonSkill.Shield, true, random); // 후보 생성
            if (cards[0].Tier == CardTier.Normal) // 일반 카드 확인
            { // 집계 시작
                normal++; // 일반 카드 집계
            } // 집계 종료
            if (cards[0].Tier == CardTier.Rare) // 희귀 카드 확인
            { // 집계 시작
                rare++; // 희귀 카드 집계
            } // 집계 종료
            if (cards[0].Tier == CardTier.Hero) // 영웅 카드 확인
            { // 집계 시작
                hero++; // 영웅 카드 집계
            } // 집계 종료
        } // 반복 종료
        Assert.That(normal, Is.InRange(7200, 7800)); // 일반 75% 범위
        Assert.That(rare, Is.InRange(1700, 2300)); // 희귀 20% 범위
        Assert.That(hero, Is.InRange(350, 650)); // 영웅 5% 범위
    } // 시험 종료
} // 시험 종료
