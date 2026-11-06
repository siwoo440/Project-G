using NUnit.Framework; // 자동 시험 기능
using UnityEngine; // 유니티 객체
using ProjectG; // 전투 강화 기능
public class UpgradeItemGradeTests // 강화 등급별 규칙 시험
{ // 시험 시작
    private GameObject holder; // 시험 객체
    private BattlePlayer player; // 시험 플레이어
    [SetUp] // 개별 시험 준비
    public void SetUp() // 새 전투 생성
    { // 준비 시작
        holder = new GameObject("GradePlayer"); // 시험 객체 생성
        player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
        player.Begin(Vector2.zero); // 전투 초기화
    } // 준비 종료
    [TearDown] // 개별 시험 정리
    public void TearDown() // 시험 객체 제거
    { // 정리 시작
        Object.DestroyImmediate(holder); // 임시 객체 제거
    } // 정리 종료
    [TestCase(UpgradeItemGrade.I, 0.10f, 0.08f, 1, 0.06f, 0.10f)] // 첫 등급 수치
    [TestCase(UpgradeItemGrade.II, 0.12f, 0.10f, 1, 0.10f, 0.15f)] // 두 번째 등급 수치
    [TestCase(UpgradeItemGrade.III, 0.14f, 0.12f, 2, 0.12f, 0.20f)] // 세 번째 등급 수치
    [TestCase(UpgradeItemGrade.IV, 0.16f, 0.14f, 2, 0.14f, 0.25f)] // 네 번째 등급 수치
    [TestCase(UpgradeItemGrade.V, 0.18f, 0.16f, 3, 0.16f, 0.30f)] // 다섯 번째 등급 수치
    public void EachGradeAppliesItsConfiguredStatValues(UpgradeItemGrade grade, float damage, float fire, int pierce, float cooldown, float graze) // 등급별 누적 효과 확인
    { // 시험 시작
        UpgradeItemState state = player.Build.Upgrades; // 현재 강화 상태
        Assert.That(state.TryApply(UpgradeItemId.Damage, grade, player), Is.True); // 피해 강화 획득
        Assert.That(state.TryApply(UpgradeItemId.FireRate, grade, player), Is.True); // 발사 강화 획득
        Assert.That(state.TryApply(UpgradeItemId.Pierce, grade, player), Is.True); // 관통 강화 획득
        Assert.That(state.TryApply(UpgradeItemId.UniqueCooldown, grade, player), Is.True); // 쿨타임 강화 획득
        Assert.That(state.TryApply(UpgradeItemId.Graze, grade, player), Is.True); // 근접 충전 강화 획득
        Assert.That(player.Build.Damage(100f, false), Is.EqualTo(100f * (1f + damage)).Within(0.001f)); // 피해량 확인
        Assert.That(player.Build.IntervalScale, Is.EqualTo(1f - fire).Within(0.001f)); // 발사 간격 확인
        Assert.That(player.Build.ExtraPierce, Is.EqualTo(pierce)); // 관통 횟수 확인
        Assert.That(player.Skills.UniqueCooldownReduction, Is.EqualTo(cooldown).Within(0.001f)); // 실제 쿨타임 확인
        Assert.That(state.GrazeBonus, Is.EqualTo(graze).Within(0.001f)); // 소수 충전 증가 확인
    } // 시험 종료
    [Test] // 혼합 등급과 카드 상한
    public void MixedGradesRespectCountsAndShareRemainingCardCaps() // 상한과 횟수 확인
    { // 시험 시작
        UpgradeItemState state = player.Build.Upgrades; // 현재 강화 상태
        Assert.That(player.ApplyCard(CardId.C01), Is.True); // 피해 카드 첫 획득
        Assert.That(player.ApplyCard(CardId.C01), Is.True); // 피해 카드 둘째 획득
        Assert.That(player.ApplyCard(CardId.C01), Is.True); // 피해 카드 셋째 획득
        Assert.That(state.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.V, player), Is.True); // 높은 등급 피해 획득
        Assert.That(state.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.V, player), Is.True); // 남은 한도까지 피해 획득
        Assert.That(player.Build.Damage(100f, false), Is.EqualTo(160f).Within(0.001f)); // 카드 포함 피해 상한
        Assert.That(state.DamageBonus, Is.EqualTo(0.30f).Within(0.001f)); // 초과분 제외된 아이템 합계
        Assert.That(state.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.I, player), Is.False); // 무효 피해 후보 차단
        Assert.That(state.TryApply(UpgradeItemId.Graze, UpgradeItemGrade.I, player), Is.True); // 첫 그레이즈 등급
        Assert.That(state.TryApply(UpgradeItemId.Graze, UpgradeItemGrade.III, player), Is.True); // 다른 그레이즈 등급
        Assert.That(state.TryApply(UpgradeItemId.Graze, UpgradeItemGrade.V, player), Is.True); // 마지막 그레이즈 등급
        Assert.That(state.TryApply(UpgradeItemId.Graze, UpgradeItemGrade.I, player), Is.False); // 네 번째 획득 차단
        Assert.That(state.GrazeBonus, Is.EqualTo(0.60f).Within(0.001f)); // 등급 효과 합산
    } // 시험 종료
    [Test] // 추가 카드 상한과 등급 효과
    public void HighGradesApplyOnlyTheRemainingSharedStatLimits() // 연사 관통 쿨타임 상한 확인
    { // 시험 시작
        UpgradeItemState state = player.Build.Upgrades; // 현재 강화 상태
        for (int index = 0; index < 3; index++) // 카드 세 단계
        { // 반복 시작
            Assert.That(player.ApplyCard(CardId.C02), Is.True); // 연사 카드 선택
            Assert.That(player.ApplyCard(CardId.C05), Is.True); // 고유기 카드 선택
        } // 반복 종료
        Assert.That(player.ApplyCard(CardId.C03), Is.True); // 관통 카드 첫 단계
        Assert.That(player.ApplyCard(CardId.C03), Is.True); // 관통 카드 두 번째 단계
        Assert.That(state.TryApply(UpgradeItemId.FireRate, UpgradeItemGrade.V, player), Is.True); // 남은 연사 한도 획득
        Assert.That(state.TryApply(UpgradeItemId.Pierce, UpgradeItemGrade.V, player), Is.True); // 남은 관통 한도 획득
        Assert.That(state.TryApply(UpgradeItemId.UniqueCooldown, UpgradeItemGrade.V, player), Is.True); // 남은 쿨타임 한도 획득
        Assert.That(player.Build.IntervalScale, Is.EqualTo(0.6f).Within(0.001f)); // 연사 상한 확인
        Assert.That(player.Build.ExtraPierce, Is.EqualTo(3)); // 관통 상한 확인
        Assert.That(player.Build.UniqueCooldownReduction, Is.EqualTo(0.4f).Within(0.001f)); // 고유기 상한 확인
        Assert.That(state.PierceBonus, Is.EqualTo(1)); // 초과분 제외 관통 합계
        Assert.That(state.UniqueCooldownBonus, Is.EqualTo(0.1f).Within(0.001f)); // 초과분 제외 쿨타임 합계
        Assert.That(state.CanApply(UpgradeItemId.FireRate, UpgradeItemGrade.I, player), Is.False); // 연사 후보 제외
        Assert.That(state.CanApply(UpgradeItemId.Pierce, UpgradeItemGrade.I, player), Is.False); // 관통 후보 제외
        Assert.That(state.CanApply(UpgradeItemId.UniqueCooldown, UpgradeItemGrade.I, player), Is.False); // 고유기 후보 제외
    } // 시험 종료
    [Test] // 등급 방어막과 기존 보호막
    public void GradeShieldRefreshesToMaximumAndKeepsTimedShieldSeparate() // 보호막 혼용 확인
    { // 시험 시작
        UpgradeItemState state = player.Build.Upgrades; // 현재 강화 상태
        player.Survival.ActivateShield(5f); // 기존 시간 제한 보호막
        Assert.That(state.TryApply(UpgradeItemId.Shield, UpgradeItemGrade.III, player), Is.True); // 세 번 방어 획득
        Assert.That(player.Survival.UpgradeShieldCharges, Is.EqualTo(3)); // 횟수 저장 확인
        Assert.That(state.CanApply(UpgradeItemId.Shield, UpgradeItemGrade.II, player), Is.False); // 낮은 갱신 무효 후보 제외
        Assert.That(state.TryApply(UpgradeItemId.Shield, UpgradeItemGrade.V, player), Is.True); // 높은 횟수 갱신
        Assert.That(player.Survival.UpgradeShieldCharges, Is.EqualTo(5)); // 최대값 갱신 확인
        Assert.That(player.Survival.TryHit(10), Is.False); // 기존 보호막 우선 차단
        Assert.That(player.Survival.UpgradeShieldCharges, Is.EqualTo(5)); // 아이템 보호 횟수 보존
        for (int index = 0; index < 5; index++) // 방어 횟수 순회
        { // 반복 시작
            Assert.That(player.Survival.TryHit(10), Is.False); // 아이템 보호 횟수 소모
        } // 반복 종료
        Assert.That(player.Survival.UpgradeShieldCharges, Is.Zero); // 모든 보호 횟수 소모
        Assert.That(player.Survival.TryHit(10), Is.True); // 소모 후 피해 적용
        Assert.That(player.Survival.Health, Is.EqualTo(90)); // 체력 피해 확인
    } // 시험 종료
    [TestCase(UpgradeItemGrade.I, 10, 8)] // 첫 등급 즉시 효과
    [TestCase(UpgradeItemGrade.II, 15, 12)] // 두 번째 등급 즉시 효과
    [TestCase(UpgradeItemGrade.III, 20, 16)] // 세 번째 등급 즉시 효과
    [TestCase(UpgradeItemGrade.IV, 25, 20)] // 네 번째 등급 즉시 효과
    [TestCase(UpgradeItemGrade.V, 30, 25)] // 다섯 번째 등급 즉시 효과
    public void InstantItemsApplyGradeAndExcludeFullResources(UpgradeItemGrade grade, int heal, int energy) // 회복과 충전 확인
    { // 시험 시작
        UpgradeItemState state = player.Build.Upgrades; // 현재 강화 상태
        Assert.That(state.CanApply(UpgradeItemId.Heal, grade, player), Is.False); // 체력 최대 후보 제외
        Assert.That(player.Survival.TryHit(50), Is.True); // 체력 감소
        Assert.That(state.TryApply(UpgradeItemId.Heal, grade, player), Is.True); // 회복 아이템 사용
        Assert.That(player.Survival.Health, Is.EqualTo(50 + heal)); // 등급별 회복 확인
        Assert.That(state.TryApply(UpgradeItemId.Energy, grade, player), Is.True); // 에너지 아이템 사용
        Assert.That(player.Skills.Energy, Is.EqualTo(energy)); // 등급별 충전 확인
        player.Skills.GainEnergy(100); // 에너지 최대 충전
        Assert.That(state.CanApply(UpgradeItemId.Energy, grade, player), Is.False); // 충전 최대 후보 제외
    } // 시험 종료
    [Test] // 회수 자석 후보와 즉시 실행
    public void MagnetRequiresAnAvailableShardAndOnlySucceedsWhenCollected() // 경험치 조각 회수 확인
    { // 시험 시작
        UpgradeItemState state = player.Build.Upgrades; // 현재 강화 상태
        Assert.That(state.CanApply(UpgradeItemId.Magnet, UpgradeItemGrade.I, player, false), Is.False); // 조각 없는 후보 제외
        Assert.That(state.CanApply(UpgradeItemId.Magnet, UpgradeItemGrade.IV, player, true), Is.True); // 유효 조각 후보 허용
        Assert.That(UpgradeItemState.MagnetRadius(UpgradeItemGrade.I), Is.EqualTo(2f)); // 첫 등급 반경
        Assert.That(UpgradeItemState.MagnetRadius(UpgradeItemGrade.IV), Is.EqualTo(8f)); // 네 번째 등급 반경
        Assert.That(float.IsPositiveInfinity(UpgradeItemState.MagnetRadius(UpgradeItemGrade.V)), Is.True); // 마지막 등급 전체 화면
        Assert.That(state.TryApply(UpgradeItemId.Magnet, UpgradeItemGrade.I, player, () => false), Is.False); // 빈 회수 결과 차단
        int collected = 0; // 시험 회수 횟수
        bool Collect() // 시험 조각 회수
        { // 회수 시작
            collected++; // 회수 횟수 증가
            return true; // 회수 완료
        } // 회수 종료
        Assert.That(state.TryApply(UpgradeItemId.Magnet, UpgradeItemGrade.V, player, Collect), Is.True); // 즉시 조각 회수
        Assert.That(collected, Is.EqualTo(1)); // 회수 콜백 횟수
    } // 시험 종료
    [Test] // 기존 보호 장치 회귀
    public void ReactiveShieldRetainsOneHitAfterRealDamage() // 보호 장치 동작 확인
    { // 시험 시작
        player.Survival.InstallReactiveShield(); // 보호 장치 장착
        Assert.That(player.Survival.TryHit(10), Is.True); // 실제 피해와 보호 장치 발동
        player.Survival.Advance(1f); // 피격 무적 해제
        Assert.That(player.Survival.TryHit(10), Is.False); // 보호 장치 한 번 방어
        Assert.That(player.Survival.TryHit(10), Is.True); // 방어 소모 후 체력 피해
    } // 시험 종료
} // 시험 종료