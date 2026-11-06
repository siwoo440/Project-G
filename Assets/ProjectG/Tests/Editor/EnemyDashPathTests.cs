using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체
public class EnemyDashPathTests // 돌진 경로 시험
{ // 시험 시작
    [Test] // 경로와 실제 방향 일치 시험
    public void DashFollowsVisiblePathWhenPlayerMovesDuringWarning() // 예고 후 이동 방향 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        try // 시험 범위
        { // 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(0f, -3f)); // 최초 대상 위치
            holder.transform.position = new Vector2(0f, 3f); // 적 시작 위치
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Dash, player, null, null); // 돌진 경고 시작
            Transform path = holder.transform.Find("EnemyDashPath"); // 화면상 경로
            Assert.That(path, Is.Not.Null); // 경로 표시 존재
            Assert.That(path.gameObject.activeSelf, Is.True); // 예고 중 경로 표시
            Assert.That(path.GetComponent<SpriteRenderer>().enabled, Is.True); // 경로 렌더러 활성
            Vector2 indicated = path.up; // 표시된 이동 방향
            Assert.That(indicated.x, Is.EqualTo(0f).Within(0.001f)); // 최초 대상 가로 방향
            Assert.That(indicated.y, Is.LessThan(-0.99f)); // 최초 대상 세로 방향
            playerObject.transform.position = new Vector2(3f, -3f); // 예고 중 회피 이동
            enemy.Advance(0.79f); // 남은 예고 진행
            Assert.That(path.gameObject.activeSelf, Is.True); // 돌진 전 경로 유지
            enemy.Advance(0.11f); // 돌진 이동 시작
            Vector2 traveled = (Vector2)holder.transform.position - new Vector2(0f, 3f); // 실제 이동 방향
            Assert.That(Vector2.Dot(traveled.normalized, indicated), Is.GreaterThan(0.999f)); // 표시와 이동 일치
            Assert.That(path.gameObject.activeSelf, Is.False); // 돌진 중 경로 숨김
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 재사용 시 돌진 경로 시험
    public void ReusedEnemyHidesAndRestoresSingleDashPath() // 종류 전환 경로 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Dash, null, null, null); // 첫 돌진 시작
            Transform path = holder.transform.Find("EnemyDashPath"); // 첫 경로 객체
            Assert.That(path, Is.Not.Null); // 경로 생성 확인
            enemy.Begin(EnemyKind.FinalBoss, null, null, null); // 보스로 전환
            Assert.That(path.gameObject.activeSelf, Is.False); // 보스 경로 숨김
            enemy.Begin(EnemyKind.Dash, null, null, null); // 재사용 돌진 시작
            Assert.That(holder.transform.Find("EnemyDashPath"), Is.SameAs(path)); // 경로 객체 재사용
            Assert.That(path.gameObject.activeSelf, Is.True); // 돌진 경로 재표시
            Assert.That(path.up.y, Is.LessThan(-0.99f)); // 대상 부재 시 아래 방향
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
