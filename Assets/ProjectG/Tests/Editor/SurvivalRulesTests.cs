using NUnit.Framework; // 테스트 도구
using UnityEngine; // 벡터 자료형
using ProjectG; // 생존 규칙 기능
public class SurvivalRulesTests // 생존 규칙 시험
{ // 시험 시작
    [Test] // 원형 경계 시험
    public void HitAndGrazeUseInclusiveBoundaries() // 판정 경계 확인
    { // 시험 시작
        Assert.That(CollisionMath.Intersects(Vector2.zero, new Vector2(0.12f, 0f), new Vector2(0.12f, 0f), 0.12f), Is.True); // 피격 경계 포함
        Assert.That(CollisionMath.Intersects(Vector2.zero, new Vector2(0.1201f, 0f), new Vector2(0.1201f, 0f), 0.12f), Is.False); // 피격 바깥 제외
        Assert.That(CollisionMath.Intersects(Vector2.zero, new Vector2(0.40f, 0f), new Vector2(0.40f, 0f), 0.40f), Is.True); // 그레이즈 경계 포함
        Assert.That(CollisionMath.Intersects(Vector2.zero, new Vector2(0.4001f, 0f), new Vector2(0.4001f, 0f), 0.40f), Is.False); // 그레이즈 바깥 제외
    } // 시험 종료
    [Test] // 연속 판정 시험
    public void FastBulletCrossingPlayerStillHits() // 빠른 탄환 통과 확인
    { // 시험 시작
        Assert.That(CollisionMath.Intersects(Vector2.zero, new Vector2(-1f, 0f), new Vector2(1f, 0f), 0.12f), Is.True); // 프레임 사이 통과 확인
    } // 시험 종료
    [Test] // 무적 시간 시험
    public void DamageIsBlockedForOneSecond() // 중복 피해 방지
    { // 시험 시작
        PlayerSurvival health = new PlayerSurvival(100); // 시험 체력 생성
        Assert.That(health.TryHit(10), Is.True); // 첫 피해 적용
        Assert.That(health.Health, Is.EqualTo(90)); // 첫 피해 결과
        health.Advance(0.99f); // 무적 시간 진행
        Assert.That(health.TryHit(10), Is.False); // 무적 중 피해 차단
        health.Advance(0.01f); // 무적 시간 종료
        Assert.That(health.TryHit(10), Is.True); // 종료 후 피해 적용
        Assert.That(health.Health, Is.EqualTo(80)); // 누적 피해 결과
    } // 시험 종료
    [Test] // 실패 상태 시험
    public void HealthCannotGoBelowZeroAndDeathBlocksFurtherHits() // 체력 하한 확인
    { // 시험 시작
        PlayerSurvival health = new PlayerSurvival(10); // 시험 체력 생성
        Assert.That(health.TryHit(20), Is.True); // 치명 피해 적용
        Assert.That(health.Health, Is.Zero); // 체력 하한 확인
        Assert.That(health.IsAlive, Is.False); // 출격 실패 확인
        health.Advance(2f); // 무적 시간 경과
        Assert.That(health.TryHit(1), Is.False); // 실패 후 피해 차단
    } // 시험 종료
    [Test] // 보호막 1회 방어 시험
    public void ShieldBlocksOneHitWithoutStartingInvulnerability() // 보호막 단일 방어 확인
    { // 시험 시작
        PlayerSurvival health = new PlayerSurvival(100); // 시험 체력 생성
        health.ActivateShield(1f); // 보호막 활성화
        Assert.That(health.HasShield, Is.True); // 보호막 존재 확인
        Assert.That(health.TryHit(10), Is.False); // 첫 피해 방어
        Assert.That(health.Health, Is.EqualTo(100)); // 체력 보존 확인
        Assert.That(health.HasShield, Is.False); // 보호막 소모 확인
        Assert.That(health.TryHit(10), Is.True); // 다음 피해 적용
    } // 시험 종료
    [Test] // 보호막 종료 시험
    public void ShieldExpiresAfterOneSecond() // 보호막 지속 시간 확인
    { // 시험 시작
        PlayerSurvival health = new PlayerSurvival(100); // 시험 체력 생성
        health.ActivateShield(1f); // 보호막 활성화
        Assert.That(health.HasShield, Is.True); // 초기 보호막 확인
        health.Advance(1f); // 지속 시간 경과
        Assert.That(health.HasShield, Is.False); // 보호막 종료 확인
        Assert.That(health.TryHit(10), Is.True); // 종료 후 피해 확인
    } // 시험 종료
} // 시험 종료
