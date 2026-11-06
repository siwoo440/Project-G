using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 카드와 아이템 기능
public class CardSharedPreviewTests // 카드 공통 상한 미리보기 시험
{ // 시험 시작
    [Test] // 피해와 집중 피해의 공유 상한 시험
    public void DamageAndFocusPreviewsIncludeItemsAndRemainingCap() // 피해 변화량 확인
    { // 시험 시작
        GameObject holder = new GameObject("PreviewPlayer"); // 시험 플레이어 객체
        try // 시험 범위 시작
        { // 시험 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 초기화
            for (int index = 0; index < 3; index++) // 피해 아이템 세 번
            { // 획득 반복 시작
                Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.V, player), Is.True); // 피해 18퍼센트 획득
            } // 획득 반복 종료
            string damage = CardCatalog.DescribeChange(CardCatalog.Find(CardId.C01), player); // 공용 피해 미리보기
            Assert.That(damage, Does.Contain("기본 피해 +54% → +60%")); // 기본 피해 상한 반영
            Assert.That(damage, Does.Contain("집중 피해 +54% → +60%")); // 집중 피해 상한 반영
            Assert.That(CardCatalog.DescribeChange(CardCatalog.Find(CardId.C04), player), Does.Contain("집중 피해 +54% → +60%")); // 집중 카드의 잔여 한도
            string lens = CardCatalog.DescribeChange(CardCatalog.Find(CardId.C11), player); // 집속 렌즈 미리보기
            Assert.That(lens, Does.Contain("집중 폭 100% → 90%")); // 실제 집중 폭 변화
            Assert.That(lens, Does.Contain("집중 피해 +54% → +60%")); // 집속 렌즈의 실제 피해 변화
            Assert.That(player.Build.Upgrades.DamageBonus, Is.EqualTo(0.54f).Within(0.0001f)); // 미리보기의 상태 변경 방지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 발사 간격과 고유기 대기 공유 상한 시험
    public void CooldownAndFirePreviewsShowActualBoundedChanges() // 발사와 고유기 변화량 확인
    { // 시험 시작
        GameObject holder = new GameObject("PreviewPlayer"); // 시험 플레이어 객체
        try // 시험 범위 시작
        { // 시험 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, UpgradeItemGrade.V, player), Is.True); // 연사 16퍼센트 획득
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, UpgradeItemGrade.V, player), Is.True); // 연사 누적 32퍼센트
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.UniqueCooldown, UpgradeItemGrade.V, player), Is.True); // 대기 16퍼센트 획득
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.UniqueCooldown, UpgradeItemGrade.V, player), Is.True); // 대기 누적 32퍼센트
            Assert.That(CardCatalog.DescribeChange(CardCatalog.Find(CardId.C02), player), Does.Contain("발사 간격 -32% → -40%")); // 발사 감소 상한
            Assert.That(CardCatalog.DescribeChange(CardCatalog.Find(CardId.C05), player), Does.Contain("J 대기 감소 32% → 40%")); // 고유기 감소 상한
            Assert.That(player.Build.Upgrades.FireRateBonus, Is.EqualTo(0.32f).Within(0.0001f)); // 연사 상태 유지
            Assert.That(player.Build.Upgrades.UniqueCooldownBonus, Is.EqualTo(0.32f).Within(0.0001f)); // 대기 상태 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 추가 관통 공유 상한 시험
    public void PiercePreviewsIncludeExistingItemPierce() // 추가 관통 변화량 확인
    { // 시험 시작
        GameObject holder = new GameObject("PreviewPlayer"); // 시험 플레이어 객체
        try // 시험 범위 시작
        { // 시험 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 전투 초기화
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Pierce, UpgradeItemGrade.IV, player), Is.True); // 관통 두 단계 획득
            Assert.That(CardCatalog.DescribeChange(CardCatalog.Find(CardId.C03), player), Does.Contain("추가 관통 2 → 3")); // 공용 관통 변화
            Assert.That(CardCatalog.DescribeChange(CardCatalog.Find(CardId.S02), player), Does.Contain("추가 관통 2 → 3")); // 전용 관통 변화
            Assert.That(player.Build.ExtraPierce, Is.EqualTo(2)); // 미리보기 중 상태 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 기존 카드와 아이템의 합산 시험
    public void PreviewMatchesRealApplicationWithFocusedCards() // 실제 선택 후 값 일치 확인
    { // 시험 시작
        GameObject holder = new GameObject("MixedPreviewPlayer"); // 시험 플레이어 객체
        try // 시험 범위 시작
        { // 시험 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.ApplyCard(CardId.C01), Is.True); // 기존 기본 피해 카드
            Assert.That(player.ApplyCard(CardId.C04), Is.True); // 기존 집중 피해 카드
            Assert.That(player.ApplyCard(CardId.C11), Is.True); // 기존 집속 렌즈
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.IV, player), Is.True); // 기존 아이템 피해
            string damage = CardCatalog.DescribeChange(CardCatalog.Find(CardId.C01), player); // 공용 피해 예측
            Assert.That(damage, Does.Contain("기본 피해 +26% → +36%")); // 실제 기본 피해 예측
            Assert.That(damage, Does.Contain("집중 피해 +44% → +54%")); // 실제 집중 피해 예측
            string lens = CardCatalog.DescribeChange(CardCatalog.Find(CardId.C11), player); // 다음 집속 렌즈 예측
            Assert.That(lens, Does.Contain("집중 폭 90% → 80%")); // 누적된 집중 폭 예측
            Assert.That(lens, Does.Contain("집중 피해 +44% → +54%")); // 누적된 집중 피해 예측
            Assert.That(player.ApplyCard(CardId.C01), Is.True); // 공용 피해 카드 실제 선택
            Assert.That(player.Build.Damage(1f, false), Is.EqualTo(1.36f).Within(0.0001f)); // 예측된 기본 피해 실측
            Assert.That(player.Build.Damage(1f, true), Is.EqualTo(1.54f).Within(0.0001f)); // 예측된 집중 피해 실측
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료