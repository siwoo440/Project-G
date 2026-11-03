using NUnit.Framework; // 테스트 도구
using ProjectG; // 스킬 상태 기능
public class SkillStateTests // 스킬 규칙 시험
{ // 시험 시작
    [Test] // 에너지 충전 시험
    public void EnergyStartsAtZeroAndCapsAtOneHundred() // 에너지 범위 확인
    { // 시험 시작
        SkillState skills = new SkillState(); // 초기 스킬 상태
        Assert.That(skills.Energy, Is.Zero); // 시작 에너지 확인
        skills.GainGraze(); // 그레이즈 충전
        skills.GainDefeat(EnemyRank.Normal); // 일반 적 처치 충전
        skills.GainDefeat(EnemyRank.MidBoss); // 중간 보스 충전
        skills.GainDefeat(EnemyRank.FinalBoss); // 최종 보스 충전
        Assert.That(skills.Energy, Is.EqualTo(33)); // 충전량 확인
        for (int index = 0; index < 100; index++) // 추가 충전 반복
        { // 반복 시작
            skills.GainGraze(); // 그레이즈 충전
        } // 반복 종료
        Assert.That(skills.Energy, Is.EqualTo(100)); // 최대 에너지 확인
    } // 시험 종료
    [Test] // 필살기 조건 시험
    public void UltimateRequiresAliveAndFullEnergy() // 필살기 자원 확인
    { // 시험 시작
        SkillState skills = new SkillState(); // 초기 스킬 상태
        Assert.That(skills.TryUltimate(true), Is.False); // 자원 부족 차단
        for (int index = 0; index < 100; index++) // 에너지 충전 반복
        { // 반복 시작
            skills.GainGraze(); // 그레이즈 충전
        } // 반복 종료
        Assert.That(skills.TryUltimate(false), Is.False); // 실패 상태 발동 차단
        Assert.That(skills.Energy, Is.EqualTo(100)); // 실패 시 자원 보존
        Assert.That(skills.TryUltimate(true), Is.True); // 충족 시 발동
        Assert.That(skills.Energy, Is.Zero); // 필살기 비용 차감
        Assert.That(skills.TryUltimate(true), Is.False); // 연속 발동 차단
    } // 시험 종료
    [Test] // 고유 스킬 시험
    public void RapidSkillRunsThreeSecondsThenEightSecondCooldown() // 고유 스킬 시간 확인
    { // 시험 시작
        SkillState skills = new SkillState(); // 초기 스킬 상태
        Assert.That(skills.TryRapid(true), Is.True); // 고유 스킬 발동
        Assert.That(skills.RapidActive, Is.EqualTo(3f).Within(0.0001f)); // 활성 시간 확인
        Assert.That(skills.AttackIntervalScale, Is.EqualTo(0.7f).Within(0.0001f)); // 발사 간격 감소 확인
        Assert.That(skills.TryRapid(true), Is.False); // 활성 중 중복 차단
        skills.Advance(3f); // 활성 시간 경과
        Assert.That(skills.RapidCooldown, Is.EqualTo(8f).Within(0.0001f)); // 종료 후 쿨타임 확인
        Assert.That(skills.AttackIntervalScale, Is.EqualTo(1f).Within(0.0001f)); // 기본 간격 복원
        skills.Advance(7.99f); // 쿨타임 종료 직전
        Assert.That(skills.TryRapid(true), Is.False); // 재사용 차단
        skills.Advance(0.01f); // 쿨타임 종료
        Assert.That(skills.TryRapid(true), Is.True); // 재사용 허용
    } // 시험 종료
    [Test] // 공용 스킬 교체 시험
    public void CommonSlotKeepsCooldownWhenSkillIsReplaced() // 교체 시 쿨타임 유지
    { // 시험 시작
        SkillState skills = new SkillState(); // 초기 스킬 상태
        Assert.That(skills.Common, Is.EqualTo(CommonSkill.Shield)); // 기본 보호막 확인
        Assert.That(skills.TryCommon(true), Is.True); // 보호막 발동
        Assert.That(skills.CommonCooldown, Is.EqualTo(12f).Within(0.0001f)); // 보호막 쿨타임 확인
        skills.Equip(CommonSkill.Scatter); // 공용 산탄 교체
        Assert.That(skills.TryCommon(true), Is.False); // 교체로 쿨타임 초기화 방지
        skills.Advance(12f); // 쿨타임 경과
        Assert.That(skills.TryCommon(true), Is.True); // 산탄 발동
        Assert.That(skills.CommonCooldown, Is.EqualTo(10f).Within(0.0001f)); // 산탄 쿨타임 확인
    } // 시험 종료
    [Test] // 사용 제한 시험
    public void BlockedStatePreventsEverySkillWithoutCost() // 사용 제한 상태 확인
    { // 시험 시작
        SkillState skills = new SkillState(); // 초기 스킬 상태
        for (int index = 0; index < 100; index++) // 에너지 충전 반복
        { // 반복 시작
            skills.GainGraze(); // 그레이즈 충전
        } // 반복 종료
        skills.Blocked = true; // 사용 제한 적용
        Assert.That(skills.TryRapid(true), Is.False); // 고유 스킬 차단
        Assert.That(skills.TryCommon(true), Is.False); // 공용 스킬 차단
        Assert.That(skills.TryUltimate(true), Is.False); // 필살기 차단
        Assert.That(skills.Energy, Is.EqualTo(100)); // 자원 보존 확인
    } // 시험 종료
    [TestCase(EnemyRank.Normal, 2)] // 일반 적 보상
    [TestCase(EnemyRank.MidBoss, 10)] // 중간 보스 보상
    [TestCase(EnemyRank.FinalBoss, 20)] // 최종 보스 보상
    public void EachEnemyRankAwardsOnlyItsOwnReward(EnemyRank rank, int expected) // 처치 보상 중복 방지
    { // 시험 시작
        SkillState skills = new SkillState(); // 초기 스킬 상태
        skills.GainDefeat(rank); // 해당 적 한 번 처치
        Assert.That(skills.Energy, Is.EqualTo(expected)); // 대상별 보상만 확인
    } // 시험 종료
    [Test] // 독립 쿨타임 시험
    public void RapidAndCommonCooldownsAdvanceIndependently() // 독립 쿨타임 확인
    { // 시험 시작
        SkillState skills = new SkillState(); // 초기 스킬 상태
        Assert.That(skills.TryRapid(true), Is.True); // 고유 스킬 발동
        Assert.That(skills.TryCommon(true), Is.True); // 공용 스킬 발동
        skills.Advance(4f); // 활성 종료 후 1초 경과
        Assert.That(skills.RapidCooldown, Is.EqualTo(7f).Within(0.0001f)); // 고유 쿨타임 확인
        Assert.That(skills.CommonCooldown, Is.EqualTo(8f).Within(0.0001f)); // 공용 쿨타임 확인
    } // 시험 종료
} // 시험 종료
