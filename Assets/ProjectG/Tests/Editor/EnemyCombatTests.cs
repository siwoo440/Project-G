using NUnit.Framework; // 테스트 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체 기능
public class EnemyCombatTests // 적과 보스 시험
{ // 시험 시작
    [TestCase(EnemyKind.Aimed, 50f, 6, EnemyRank.Normal)] // 조준형 적
    [TestCase(EnemyKind.Spread, 60f, 8, EnemyRank.Normal)] // 확산형 적
    [TestCase(EnemyKind.Dash, 80f, 10, EnemyRank.Normal)] // 돌진형 적
    [TestCase(EnemyKind.MidBoss, 600f, 30, EnemyRank.MidBoss)] // 중간 보스
    [TestCase(EnemyKind.FinalBoss, 1500f, 50, EnemyRank.FinalBoss)] // 최종 보스
    public void EnemyHasSpecifiedTestStats(EnemyKind kind, float hp, int experience, EnemyRank rank) // 초기 시험값 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성 요소
            enemy.Begin(kind, null, null, null); // 적 상태 초기화
            Assert.That(enemy.Health, Is.EqualTo(hp)); // 초기 체력 확인
            Assert.That(enemy.ExperienceReward, Is.EqualTo(experience)); // 경험치 시험값 확인
            Assert.That(enemy.Rank, Is.EqualTo(rank)); // 적 종류 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(EnemyKind.Aimed, 2, 6)] // 일반 적 보상
    [TestCase(EnemyKind.MidBoss, 10, 30)] // 중간 보스 보상
    [TestCase(EnemyKind.FinalBoss, 20, 50)] // 최종 보스 보상
    public void DefeatAwardsOneRankRewardOnly(EnemyKind kind, int energy, int experience) // 처치 보상 중복 방지 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체 생성
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
            player.Begin(Vector2.zero); // 플레이어 초기화
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성 요소
            enemy.Begin(kind, player, null, null); // 적 상태 초기화
            int xp = 0; // 경험치 보상 기록
            player.ExperienceRewarded += value => xp += value; // 보상 이벤트 연결
            enemy.ApplyDamage(5000f, player); // 적 처치
            enemy.ApplyDamage(5000f, player); // 보스 둘째 단계 진행
            enemy.ApplyDamage(5000f, player); // 보스 마지막 단계 처치
            enemy.ApplyDamage(5000f, player); // 중복 피해 시도
            Assert.That(enemy.IsAlive, Is.False); // 처치 상태 확인
            Assert.That(player.Skills.Energy, Is.EqualTo(energy)); // 적 등급별 에너지 확인
            Assert.That(xp, Is.EqualTo(experience)); // 경험치 중복 지급 방지
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 돌진 사전 예고 시험
    public void DashWaitsForWarningBeforeMoving() // 돌진 예고 시간 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체 생성
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
            player.Begin(new Vector2(0f, -3f)); // 플레이어 위치
            holder.transform.position = new Vector2(0f, 3f); // 적 시작 위치
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성 요소
            enemy.Begin(EnemyKind.Dash, player, null, null); // 돌진형 초기화
            enemy.Advance(0.79f); // 예고 중 시간 진행
            Assert.That(enemy.IsTelegraphing, Is.True); // 예고 상태 확인
            Assert.That(holder.transform.position.y, Is.EqualTo(3f).Within(0.001f)); // 예고 중 위치 유지
            enemy.Advance(0.11f); // 예고 종료와 돌진
            Assert.That(holder.transform.position.y, Is.LessThan(3f)); // 돌진 이동 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 최종 보스 단계 시험
    public void FinalBossChangesPatternByHealthAndExposesWeaknessAfterAttack() // 보스 단계 확인
    { // 시험 시작
        GameObject holder = new GameObject("Boss"); // 보스 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            EnemyActor boss = holder.AddComponent<EnemyActor>(); // 보스 구성 요소
            boss.Begin(EnemyKind.FinalBoss, null, null, null); // 보스 상태 초기화
            Assert.That(boss.PatternIndex, Is.Zero); // 첫 단계 확인
            Assert.That(boss.WeakExposed, Is.False); // 예고 중 약점 비노출
            boss.Advance(1.3f); // 직선 연사 패턴 종료
            Assert.That(boss.WeakExposed, Is.True); // 패턴 종료 후 약점 노출
            boss.ApplyDamage(501f, null); // 체력 단계 감소
            Assert.That(boss.PatternIndex, Is.EqualTo(1)); // 두 번째 단계 확인
            boss.ApplyDamage(500f, null); // 체력 단계 감소
            Assert.That(boss.PatternIndex, Is.EqualTo(2)); // 세 번째 단계 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 보스 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 실제 적 탄환 적중 시험
    public void ProjectileDamagesEnemyAndDefeatRewardsPlayer() // 탄환과 보상 연결 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject enemyObject = new GameObject("Enemy"); // 적 생성
        GameObject shotObject = new GameObject("Shot"); // 탄환 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 속사형 시작
            EnemyActor enemy = enemyObject.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Aimed, player, null, null); // 일반 적 시작
            Projectile shot = shotObject.AddComponent<Projectile>(); // 탄환 구성
            shot.Begin(Vector2.up, 5f, 50, true, player); // 기본 탄환 시작
            Assert.That(shot.TryHit(enemy, false), Is.True); // 적 적중 확인
            Assert.That(enemy.IsAlive, Is.False); // 적 처치 확인
            Assert.That(player.Skills.Energy, Is.EqualTo(2)); // 처치 에너지 확인
            Assert.That(player.Combo.Bonus, Is.EqualTo(0.01f).Within(0.0001f)); // 속사 누적 확인
            Assert.That(shot.TryHit(enemy, false), Is.False); // 중복 처치 차단
        } // 시험 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(shotObject); // 탄환 제거
            Object.DestroyImmediate(enemyObject); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 적탄 화면 이탈 회수 시험
    public void EnemyBulletPoolReclaimsBulletsOutsideAllScreenEdges() // 원형 탄막 회수 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            camera.orthographic = true; // 직교 화면
            camera.orthographicSize = 5f; // 화면 높이
            EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 적탄 풀 생성
            EnemyBullet upward = pool.Spawn(Vector2.zero, Vector2.up * 10f, 0.06f, 10, -5f, null); // 위쪽 이동 적탄
            upward.Simulate(1f); // 화면 밖 이동
            Assert.That(upward.InUse, Is.False); // 위쪽 이탈 회수 확인
            EnemyBullet sideways = pool.Spawn(Vector2.zero, Vector2.right * 20f, 0.06f, 10, -5f, null); // 옆쪽 이동 적탄
            sideways.Simulate(1f); // 화면 밖 이동
            Assert.That(sideways.InUse, Is.False); // 옆쪽 이탈 회수 확인
        } // 시험 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 카메라와 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 약점 노출 판정 시험
    public void BossWeakPointAppliesOnlyDuringRecovery() // 보스 약점 노출 확인
    { // 시험 시작
        GameObject holder = new GameObject("Boss"); // 보스 생성
        try // 시험 시작
        { // 시험 범위 시작
            EnemyActor boss = holder.AddComponent<EnemyActor>(); // 보스 구성
            boss.Begin(EnemyKind.MidBoss, null, null, null); // 보스 시작
            Assert.That(boss.Intersects(new Vector2(0f, -1f), new Vector2(0f, 1f), out bool hidden), Is.True); // 몸체 접촉
            Assert.That(hidden, Is.False); // 예고 중 약점 비노출
            boss.Advance(1.3f); // 연사 종료
            Assert.That(boss.Intersects(new Vector2(0f, -1f), new Vector2(0f, 1f), out bool exposed), Is.True); // 몸체 재접촉
            Assert.That(exposed, Is.True); // 회복 중 약점 노출
            boss.Advance(1f); // 약점 시간 종료
            boss.Intersects(new Vector2(0f, -1f), new Vector2(0f, 1f), out bool hiddenAgain); // 다음 패턴 판정
            Assert.That(hiddenAgain, Is.False); // 약점 재차단
        } // 시험 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 보스 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(EnemyKind.Aimed, 0f, 1.5f, 1)] // 조준형 직선탄
    [TestCase(EnemyKind.Spread, 0f, 2f, 3)] // 확산형 부채탄
    [TestCase(EnemyKind.MidBoss, 0f, 1.3f, 3)] // 중간 보스 직선 연사
    [TestCase(EnemyKind.FinalBoss, 500f, 0.8f, 12)] // 최종 보스 원형 확산
    [TestCase(EnemyKind.FinalBoss, 1000f, 0.8f, 6)] // 최종 보스 교차탄
    public void PatternsSpawnExpectedBullets(EnemyKind kind, float startingDamage, float elapsed, int count) // 적탄 패턴 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject enemyObject = new GameObject("Enemy"); // 적 생성
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            camera.orthographic = true; // 직교 화면
            camera.orthographicSize = 5f; // 세로 반경
            EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 적탄 풀
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(0f, -3f)); // 플레이어 시작
            enemyObject.transform.position = new Vector2(0f, 3f); // 적 위치
            EnemyActor enemy = enemyObject.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(kind, player, pool, camera); // 적 초기화
            if (kind == EnemyKind.FinalBoss && startingDamage >= 1000f) // 세 번째 단계 확인
            { // 단계 처리 시작
                enemy.ApplyDamage(500f, player); // 첫 체력 경계 진입
                enemy.ApplyDamage(startingDamage - 500f, player); // 둘째 체력 경계 진입
            } // 단계 처리 종료
            else // 다른 적과 단계
            { // 일반 처리 시작
                enemy.ApplyDamage(startingDamage, player); // 시험 단계 진입
            } // 일반 처리 종료
            enemy.Advance(elapsed); // 패턴 시간 진행
            Assert.That(pool.GetComponentsInChildren<EnemyBullet>().Length, Is.EqualTo(count)); // 적탄 수 확인
        } // 시험 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(enemyObject); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라와 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 돌진 연속 충돌 시험
    public void DashDealsContactDamageOnceAcrossFastMovement() // 돌진 접촉 피해 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject enemyObject = new GameObject("Enemy"); // 적 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 플레이어 시작
            enemyObject.transform.position = new Vector2(0f, 2f); // 적 위치
            EnemyActor enemy = enemyObject.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Dash, player, null, null); // 돌진형 시작
            enemy.Advance(1.3f); // 예고와 빠른 관통 이동
            Assert.That(player.Survival.Health, Is.EqualTo(85)); // 접촉 피해 확인
            player.Survival.Advance(2f); // 무적 시간 경과
            enemy.Advance(0.1f); // 돌진 지속
            Assert.That(player.Survival.Health, Is.EqualTo(85)); // 같은 돌진 중복 피해 방지
        } // 시험 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(enemyObject); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 필살기 적 처치 시험
    public void RapidUltimateDamagesEnemyAndAwardsEnergyOnce() // 필살기 피해와 보상 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject enemyObject = new GameObject("Enemy"); // 적 생성
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            camera.orthographic = true; // 직교 화면
            camera.orthographicSize = 5f; // 세로 반경
            EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 적탄 풀
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.AssignCamera(camera); // 카메라 연결
            player.AssignBulletPool(pool); // 적탄 풀 연결
            player.Begin(new Vector2(0f, -3f)); // 플레이어 시작
            enemyObject.transform.position = new Vector2(0f, 3f); // 적 위치
            EnemyActor enemy = enemyObject.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Aimed, player, pool, camera); // 일반 적 시작
            for (int index = 0; index < 100; index++) // 필살기 자원 충전
            { // 반복 시작
                player.RegisterGraze(); // 에너지 충전
            } // 반복 종료
            Assert.That(player.TryUseUltimate(), Is.True); // 필살기 발동
            Assert.That(enemy.IsAlive, Is.False); // 실제 적 처치
            Assert.That(player.Skills.Energy, Is.EqualTo(2)); // 소모 후 처치 보상
        } // 시험 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(enemyObject); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라와 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 돌진 화면 이탈 시험
    public void DashThatLeavesSideOfScreenDespawnsWithoutReward() // 옆 방향 이탈 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject enemyObject = new GameObject("Enemy"); // 적 생성
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            camera.orthographic = true; // 직교 화면
            camera.orthographicSize = 5f; // 화면 크기
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(8f, 3f)); // 오른쪽 플레이어 위치
            enemyObject.transform.position = new Vector2(0f, 3f); // 적 시작 위치
            EnemyActor enemy = enemyObject.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Dash, player, null, camera); // 돌진형 시작
            enemy.Advance(3.8f); // 예고 후 옆 방향 이탈
            Assert.That(enemy.gameObject.activeSelf, Is.False); // 화면 밖 퇴장 확인
            Assert.That(player.Skills.Energy, Is.Zero); // 이탈 시 처치 보상 없음
        } // 시험 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(enemyObject); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 저격형 보스 약점 피해 시험
    public void SniperShotGetsWeakPointBonusOnlyAfterBossAttack() // 보스 약점 피해 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject enemyObject = new GameObject("Boss"); // 보스 생성
        GameObject firstObject = new GameObject("FirstShot"); // 첫 탄환 생성
        GameObject secondObject = new GameObject("SecondShot"); // 둘째 탄환 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 시작
            EnemyActor boss = enemyObject.AddComponent<EnemyActor>(); // 보스 구성
            boss.Begin(EnemyKind.MidBoss, player, null, null); // 보스 시작
            Projectile first = firstObject.AddComponent<Projectile>(); // 첫 탄환 구성
            first.Begin(Vector2.up, 5f, 100, false, player, 3); // 고유 탄환 시험
            boss.Intersects(new Vector2(0f, -1f), new Vector2(0f, 1f), out bool hidden); // 비노출 판정
            first.TryHit(boss, hidden); // 일반 피해 적용
            Assert.That(boss.Health, Is.EqualTo(500f)); // 기본 피해 확인
            boss.Advance(1.3f); // 연사 종료
            Projectile second = secondObject.AddComponent<Projectile>(); // 둘째 탄환 구성
            second.Begin(Vector2.up, 5f, 100, false, player, 3); // 고유 탄환 시험
            boss.Intersects(new Vector2(0f, -1f), new Vector2(0f, 1f), out bool exposed); // 약점 노출 판정
            second.TryHit(boss, exposed); // 약점 피해 적용
            Assert.That(boss.Health, Is.EqualTo(350f)); // 추가 피해 확인
        } // 시험 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(firstObject); // 첫 탄환 제거
            Object.DestroyImmediate(secondObject); // 둘째 탄환 제거
            Object.DestroyImmediate(enemyObject); // 보스 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
