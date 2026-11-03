using NUnit.Framework; // 테스트 도구
using UnityEngine; // 벡터 자료형
using ProjectG; // 적탄 판정 기능
public class BulletInteractionTests // 적탄 판정 시험
{ // 시험 시작
    [Test] // 그레이즈 중복 시험
    public void GrazeOccursOnceAndResetAllowsReuse() // 재사용 판정 초기화
    { // 시험 시작
        BulletInteraction bullet = new BulletInteraction(); // 적탄 판정 생성
        PlayerSurvival player = new PlayerSurvival(100); // 시험 체력 생성
        Vector2 near = new Vector2(0.3f, 0f); // 그레이즈 위치
        Assert.That(bullet.Resolve(Vector2.zero, Vector2.zero, near, near, 0f, 10, player), Is.EqualTo(BulletContact.Graze)); // 첫 그레이즈
        Assert.That(bullet.Resolve(Vector2.zero, Vector2.zero, near, near, 0f, 10, player), Is.EqualTo(BulletContact.None)); // 중복 그레이즈 차단
        bullet.Reset(); // 재사용 상태 초기화
        Assert.That(bullet.Resolve(Vector2.zero, Vector2.zero, near, near, 0f, 10, player), Is.EqualTo(BulletContact.Graze)); // 재사용 후 그레이즈
        Assert.That(player.Health, Is.EqualTo(100)); // 피해 없음 확인
    } // 시험 종료
    [Test] // 피격 우선 시험
    public void DirectHitDealsDamageWithoutGraze() // 피격 우선 확인
    { // 시험 시작
        BulletInteraction bullet = new BulletInteraction(); // 적탄 판정 생성
        PlayerSurvival player = new PlayerSurvival(100); // 시험 체력 생성
        BulletContact result = bullet.Resolve(Vector2.zero, Vector2.zero, new Vector2(-1f, 0f), new Vector2(1f, 0f), 0f, 10, player); // 빠른 관통 판정
        Assert.That(result, Is.EqualTo(BulletContact.Hit)); // 피격 우선 확인
        Assert.That(player.Health, Is.EqualTo(90)); // 피해 적용 확인
    } // 시험 종료
    [Test] // 무적 중 접촉 시험
    public void InvulnerableContactDoesNotDamageOrGraze() // 무적 접촉 확인
    { // 시험 시작
        PlayerSurvival player = new PlayerSurvival(100); // 시험 체력 생성
        player.TryHit(10); // 첫 피해와 무적 시작
        BulletInteraction bullet = new BulletInteraction(); // 두 번째 적탄
        Assert.That(bullet.Resolve(Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, 0f, 10, player), Is.EqualTo(BulletContact.BlockedHit)); // 무적 접촉 확인
        Assert.That(player.Health, Is.EqualTo(90)); // 중복 피해 없음
    } // 시험 종료
    [Test] // 상대 이동 판정 시험
    public void MovingPlayerCrossingBulletStillHits() // 플레이어 이동 충돌 확인
    { // 시험 시작
        BulletInteraction bullet = new BulletInteraction(); // 적탄 판정 생성
        PlayerSurvival player = new PlayerSurvival(100); // 시험 체력 생성
        BulletContact result = bullet.Resolve(new Vector2(-1f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero, 0f, 10, player); // 상대 이동 판정
        Assert.That(result, Is.EqualTo(BulletContact.Hit)); // 이동 중 충돌 확인
    } // 시험 종료
} // 시험 종료
