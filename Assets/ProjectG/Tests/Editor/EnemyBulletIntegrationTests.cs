using NUnit.Framework; // 테스트 도구
using UnityEngine; // 게임 객체 기능
using ProjectG; // 전투 기능
public class EnemyBulletIntegrationTests // 적탄 연결 시험
{ // 시험 시작
    [Test] // 피격과 무적 연결 시험
    public void EnemyContactDamagesOnceAndRecyclesBullets() // 피격 연결 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("TestPlayer"); // 시험 플레이어 생성
        GameObject poolObject = new GameObject("TestPool"); // 시험 풀 생성
        BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(poolObject); // 풀 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 체력 초기화
            EnemyBullet first = pool.Spawn(Vector2.zero, Vector2.zero, 0f, 10, -5f, player); // 첫 적탄 생성
            first.Simulate(0.01f); // 첫 적탄 판정
            Assert.That(player.Survival, Is.Not.Null); // 생존 상태 확인
            Assert.That(player.Survival.Health, Is.EqualTo(90)); // 첫 피해 확인
            Assert.That(first.gameObject.activeSelf, Is.False); // 접촉 탄환 회수
            EnemyBullet second = pool.Spawn(Vector2.zero, Vector2.zero, 0f, 10, -5f, player); // 두 번째 적탄 생성
            second.Simulate(0.01f); // 무적 중 판정
            Assert.That(player.Survival.Health, Is.EqualTo(90)); // 중복 피해 차단
            Assert.That(second.gameObject.activeSelf, Is.False); // 무적 접촉 탄환 회수
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(poolObject); // 시험 풀 제거
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 그레이즈와 재사용 연결 시험
    public void ReusedBulletCanGrazeAgainOnlyOnce() // 재사용 그레이즈 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("TestPlayer"); // 시험 플레이어 생성
        GameObject poolObject = new GameObject("TestPool"); // 시험 풀 생성
        BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(poolObject); // 풀 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 체력 초기화
            EnemyBullet bullet = pool.Spawn(new Vector2(0.3f, 0f), Vector2.zero, 0f, 10, -5f, player); // 첫 근접 탄환
            bullet.Simulate(0.01f); // 첫 근접 판정
            bullet.Simulate(0.01f); // 반복 근접 판정
            Assert.That(player.GrazeCount, Is.EqualTo(1)); // 탄환당 한 번 확인
            pool.Recycle(bullet); // 탄환 회수
            EnemyBullet reused = pool.Spawn(new Vector2(0.3f, 0f), Vector2.zero, 0f, 10, -5f, player); // 같은 탄환 재사용
            reused.Simulate(0.01f); // 재사용 근접 판정
            Assert.That(player.GrazeCount, Is.EqualTo(2)); // 재사용 상태 초기화 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(poolObject); // 시험 풀 제거
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 사망 후 제어 차단 시험
    public void DefeatedPlayerCannotMoveOrShoot() // 출격 실패 처리 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("TestPlayer"); // 시험 플레이어 생성
        BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 체력 초기화
            Assert.That(player.Survival, Is.Not.Null); // 생존 상태 확인
            player.Survival.TryHit(100); // 치명 피해 적용
            player.Step(Vector2.right, false, false, false, 1f, -8f, 8f, -5f, 5f); // 실패 후 이동 시도
            Assert.That(playerObject.transform.position.x, Is.Zero); // 이동 차단 확인
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.Zero); // 발사 차단 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 생성 탄환 순회
            { // 탄환 정리 시작
                Object.DestroyImmediate(shot.gameObject); // 시험 탄환 제거
            } // 탄환 정리 종료
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 일시 정지 판정 시험
    public void PausedBulletDoesNotDamagePlayer() // 정지 중 피해 방지
    { // 시험 시작
        GameObject playerObject = new GameObject("TestPlayer"); // 시험 플레이어 생성
        GameObject poolObject = new GameObject("TestPool"); // 시험 풀 생성
        BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(poolObject); // 풀 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 체력 초기화
            EnemyBullet bullet = pool.Spawn(Vector2.zero, Vector2.zero, 0f, 10, -5f, player); // 겹친 적탄 생성
            bullet.Simulate(0f); // 정지 시간 판정 시도
            Assert.That(player.Survival.Health, Is.EqualTo(100)); // 정지 중 피해 차단
            Assert.That(bullet.gameObject.activeSelf, Is.True); // 정지 중 탄환 유지
            bullet.Simulate(0.01f); // 전투 재개
            Assert.That(player.Survival.Health, Is.EqualTo(90)); // 재개 후 접촉 처리
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(poolObject); // 시험 풀 제거
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
