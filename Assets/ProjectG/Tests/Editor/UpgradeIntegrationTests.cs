using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 전투 강화 기능
public class UpgradeIntegrationTests // 강화 아이템 통합 시험
{ // 시험 시작
    [Test] // 카드와 아이템 공통 상한 시험
    public void CardsAndItemsShareAllFourStatCaps() // 합산 상한 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            for (int index = 0; index < 3; index++) // 세 단계 반복
            { // 반복 시작
                Assert.That(player.ApplyCard(CardId.C01), Is.True); // 피해 카드 선택
                Assert.That(player.ApplyCard(CardId.C02), Is.True); // 발사 간격 카드 선택
                Assert.That(player.ApplyCard(CardId.C03), Is.True); // 관통 카드 선택
                Assert.That(player.ApplyCard(CardId.C05), Is.True); // 고유기 카드 선택
            } // 반복 종료
            for (int index = 0; index < 3; index++) // 피해 아이템 세 단계
            { // 반복 시작
                Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, player), Is.True); // 피해 아이템 선택
            } // 반복 종료
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, player), Is.True); // 발사 간격 첫 아이템
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, player), Is.True); // 발사 간격 둘째 아이템
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, player), Is.False); // 발사 간격 상한 제외
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Pierce, player), Is.False); // 관통 상한 제외
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.UniqueCooldown, player), Is.True); // 고유기 아이템 선택
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.UniqueCooldown, player), Is.False); // 고유기 상한 제외
            Assert.That(player.Build.Damage(10f, false), Is.EqualTo(16f).Within(0.001f)); // 피해 60% 상한
            Assert.That(player.Build.IntervalScale, Is.EqualTo(0.6f).Within(0.001f)); // 발사 간격 40% 상한
            Assert.That(player.Build.ExtraPierce, Is.EqualTo(3)); // 추가 관통 상한
            Assert.That(player.Skills.UniqueCooldownReduction, Is.EqualTo(0.4f).Within(0.001f)); // 고유기 쿨타임 상한
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, player), Is.False); // 아이템 중첩 상한
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 소수 그레이즈 충전 시험
    public void GrazeItemAccumulatesFractionalEnergy() // 소수 충전 누적 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Graze, player), Is.True); // 그레이즈 아이템 적용
            for (int index = 0; index < 5; index++) // 다섯 번 스침
            { // 반복 시작
                player.RegisterGraze(); // 그레이즈 충전
            } // 반복 종료
            Assert.That(player.Skills.Energy, Is.EqualTo(6)); // 소수 누적 결과
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 강화 선택 안전 시간 시험
    public void UpgradeSafeStatePreventsDamageAndEndsCleanly() // 선택 중 무적 확인
    { // 시험 시작
        PlayerSurvival survival = new PlayerSurvival(100); // 생존 상태 생성
        survival.SetUpgradeSafe(true); // 선택 안전 시작
        Assert.That(survival.TryHit(10), Is.False); // 안전 중 피해 차단
        Assert.That(survival.Health, Is.EqualTo(100)); // 체력 유지 확인
        survival.SetUpgradeSafe(false); // 선택 안전 종료
        Assert.That(survival.TryHit(10), Is.True); // 안전 종료 후 피해
        Assert.That(survival.Health, Is.EqualTo(90)); // 체력 감소 확인
    } // 시험 종료
    [Test] // 선택 직후 안전 유예 시험
    public void TransitionSafetyExpiresWithoutChangingHitInvulnerability() // 안전 시간 독립 확인
    { // 시험 시작
        PlayerSurvival survival = new PlayerSurvival(100); // 생존 상태 생성
        survival.GrantTransitionSafety(0.5f); // 선택 후 안전 유예
        Assert.That(survival.TryHit(10), Is.False); // 유예 중 피해 방지
        survival.Advance(0.5f); // 유예 시간 경과
        Assert.That(survival.TryHit(10), Is.True); // 유예 종료 후 피해
        Assert.That(survival.IsInvulnerable, Is.True); // 피격 후 무적 유지
        survival.Advance(1f); // 피격 무적 시간 경과
        Assert.That(survival.IsInvulnerable, Is.False); // 피격 무적 정상 종료
    } // 시험 종료
} // 시험 종료
