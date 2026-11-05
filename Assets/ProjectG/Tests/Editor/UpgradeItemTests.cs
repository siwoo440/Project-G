using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 전투 기능
using ProjectG; // 강화 아이템 기능
public class UpgradeItemTests // 강화 아이템 규칙 시험
{ // 시험 시작
    private GameObject holder; // 시험 플레이어 객체
    private BattlePlayer player; // 시험 플레이어
    [SetUp] // 시험 준비
    public void SetUp() // 플레이어 생성
    { // 준비 시작
        holder = new GameObject("Player"); // 시험 플레이어 객체
        player = holder.AddComponent<BattlePlayer>(); // 전투 구성 요소
        player.Begin(Vector2.zero); // 전투 상태 초기화
    } // 준비 종료
    [TearDown] // 시험 정리
    public void TearDown() // 플레이어 제거
    { // 정리 시작
        Object.DestroyImmediate(holder); // 시험 객체 제거
    } // 정리 종료
    [Test] // 피해와 연사 공유 상한 시험
    public void AttackAndFireRateShareCardCaps() // 카드 합산 확인
    { // 시험 시작
        for (int index = 0; index < 3; index++) // 카드와 아이템 반복
        { // 반복 시작
            Assert.That(player.ApplyCard(CardId.C01), Is.True); // 화력 카드 선택
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, player), Is.True); // 화력 아이템 선택
            Assert.That(player.ApplyCard(CardId.C02), Is.True); // 연사 카드 선택
            if (index < 2) // 유효한 연사 단계 확인
            { // 연사 선택 시작
                Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, player), Is.True); // 연사 아이템 선택
            } // 연사 선택 종료
        } // 반복 종료
        Assert.That(player.Build.Damage(100f, false), Is.EqualTo(160f).Within(0.001f)); // 피해 상한 확인
        Assert.That(player.Build.IntervalScale, Is.EqualTo(0.6f).Within(0.001f)); // 연사 상한 확인
        Assert.That(player.Build.Upgrades.CanApply(UpgradeItemId.Damage, player), Is.False); // 최대 화력 후보 제외
        Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, player), Is.False); // 최대 연사 중복 차단
    } // 시험 종료
    [Test] // 관통과 고유기 상한 시험
    public void PierceAndCooldownExcludeIneffectiveRewards() // 무효 강화 제외 확인
    { // 시험 시작
        Assert.That(player.ApplyCard(CardId.C03), Is.True); // 관통 카드 첫 단계
        Assert.That(player.ApplyCard(CardId.C03), Is.True); // 관통 카드 둘째 단계
        Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Pierce, player), Is.True); // 관통 아이템 적용
        Assert.That(player.Build.ExtraPierce, Is.EqualTo(3)); // 관통 합산 상한
        Assert.That(player.Build.Upgrades.CanApply(UpgradeItemId.Pierce, player), Is.False); // 무효 관통 제외
        for (int index = 0; index < 3; index++) // 고유기 카드 순회
        { // 반복 시작
            Assert.That(player.ApplyCard(CardId.C05), Is.True); // 고유기 강화
        } // 반복 종료
        Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.UniqueCooldown, player), Is.True); // 고유기 아이템 적용
        Assert.That(player.Skills.UniqueCooldownReduction, Is.EqualTo(0.4f).Within(0.001f)); // 실사용 쿨타임 동기화
        Assert.That(player.Build.Upgrades.CanApply(UpgradeItemId.UniqueCooldown, player), Is.False); // 상한 후보 제외
    } // 시험 종료
    [Test] // 방어막과 그레이즈 시험
    public void ShieldRefreshesAndGrazeAccumulatesFractions() // 방어와 소수 충전 확인
    { // 시험 시작
        Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Shield, player), Is.True); // 방어막 획득
        Assert.That(player.Survival.TryHit(10), Is.False); // 피해 1회 차단
        Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Shield, player), Is.True); // 방어막 재획득
        Assert.That(player.Survival.TryHit(10), Is.False); // 재획득 방어 확인
        Assert.That(player.Build.Upgrades.Count(UpgradeItemId.Shield), Is.EqualTo(1)); // 방어막 중복 방지
        Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Graze, player), Is.True); // 그레이즈 강화 선택
        for (int index = 0; index < 5; index++) // 다섯 번 그레이즈
        { // 반복 시작
            player.RegisterGraze(); // 근접 충전 누적
        } // 반복 종료
        Assert.That(player.Skills.Energy, Is.EqualTo(6)); // 소수 충전 합산 확인
    } // 시험 종료
    [Test] // 출격 초기화 시험
    public void RestartClearsTemporaryUpgradeItems() // 임시 강화 초기화
    { // 시험 시작
        Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, player), Is.True); // 강화 아이템 선택
        player.Begin(Vector2.zero); // 새 출격 시작
        Assert.That(player.Build.Upgrades.Count(UpgradeItemId.Damage), Is.Zero); // 강화 초기화 확인
    } // 시험 종료
} // 시험 종료
