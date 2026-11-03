using NUnit.Framework; // 테스트 도구
using UnityEngine; // 게임 객체 기능
using ProjectG; // 적탄 풀 기능
public class EnemyBulletPoolTests // 적탄 풀 시험
{ // 시험 시작
    [Test] // 풀 재사용 시험
    public void RecycleReusesSameObjectAndResetsPosition() // 적탄 재사용 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPool"); // 시험 풀 생성
        EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 풀 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            EnemyBullet first = pool.Spawn(Vector2.zero, Vector2.down, 0.06f, 10, -5f, null); // 첫 적탄 생성
            Assert.That(first, Is.Not.Null); // 적탄 존재 확인
            first.Simulate(1f); // 적탄 이동
            Assert.That(first.transform.position.y, Is.EqualTo(-1f).Within(0.0001f)); // 이동 거리 확인
            pool.Recycle(first); // 적탄 회수
            pool.Recycle(first); // 중복 회수 시도
            EnemyBullet second = pool.Spawn(new Vector2(2f, 3f), Vector2.down, 0.06f, 10, -5f, null); // 적탄 재사용
            Assert.That(second, Is.SameAs(first)); // 동일 객체 확인
            Assert.That(second.transform.position.x, Is.EqualTo(2f).Within(0.0001f)); // 새 위치 확인
            EnemyBullet third = pool.Spawn(Vector2.zero, Vector2.down, 0.06f, 10, -5f, null); // 동시 적탄 생성
            Assert.That(third, Is.Not.SameAs(second)); // 중복 회수 방지
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 화면 이탈 회수 시험
    public void BulletIsRecycledAfterLeavingBottom() // 이탈 회수 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPool"); // 시험 풀 생성
        EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 풀 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            EnemyBullet bullet = pool.Spawn(new Vector2(0f, -4f), Vector2.down, 0.06f, 10, -5f, null); // 이탈할 탄환
            Assert.That(bullet, Is.Not.Null); // 탄환 생성 확인
            bullet.Simulate(2f); // 화면 아래 이동
            Assert.That(bullet.gameObject.activeSelf, Is.False); // 이탈 회수 확인
            Assert.That(pool.Spawn(Vector2.zero, Vector2.down, 0.06f, 10, -5f, null), Is.SameAs(bullet)); // 회수 객체 재사용
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 필살기 탄막 소거 시험
    public void UltimateClearsOnlyVisibleClearableBullets() // 소거 대상 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPool"); // 시험 풀 생성
        EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 풀 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            EnemyBullet clearable = pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, null, true); // 화면 안 소거 가능 탄환
            EnemyBullet protectedBullet = pool.Spawn(Vector2.right, Vector2.zero, 0.06f, 10, -5f, null, false); // 소거 불가 탄환
            EnemyBullet outside = pool.Spawn(new Vector2(12f, 0f), Vector2.zero, 0.06f, 10, -5f, null, true); // 화면 밖 탄환
            int removed = pool.ClearVisible(new Rect(-8f, -5f, 16f, 10f)); // 화면 내 소거
            Assert.That(removed, Is.EqualTo(1)); // 소거 수 확인
            Assert.That(clearable.gameObject.activeSelf, Is.False); // 소거 가능 탄환 회수
            Assert.That(protectedBullet.gameObject.activeSelf, Is.True); // 소거 불가 탄환 유지
            Assert.That(outside.gameObject.activeSelf, Is.True); // 화면 밖 탄환 유지
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 화면 끝 확대 탄환 시험
    public void EnlargedBulletRemainsAndCanBeClearedWhileVisible() // 보이는 탄환 처리 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPool"); // 시험 풀 생성
        EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 풀 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            EnemyBullet bullet = pool.Spawn(new Vector2(0f, -5.1f), Vector2.zero, 0.06f, 10, -5f, null); // 화면 끝 탄환 생성
            bullet.Simulate(0.01f); // 탄환 상태 진행
            Assert.That(bullet.InUse, Is.True); // 보이는 탄환 유지
            Assert.That(pool.ClearVisible(new Rect(-8f, -5f, 16f, 10f)), Is.EqualTo(1)); // 보이는 탄환 소거
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 풀 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
