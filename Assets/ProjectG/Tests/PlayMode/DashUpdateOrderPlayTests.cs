using System.Collections; // 코루틴 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 플레이 시험
public sealed class DashUpdateOrderPlayTests // 돌진 접촉 순서 시험
{ // 시험 시작
    [UnityTest] // 플레이 시험 등록
    public IEnumerator DashDoesNotReusePreviousPlayerMovementBeforeCurrentStep() // 이전 이동 궤적 재사용 방지
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("DashOrderCamera"); // 시험 카메라
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
        GameObject playerObject = new GameObject("DashOrderPlayer"); // 시험 플레이어
        BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
        GameObject enemyObject = null; // 적 객체 참조
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            yield return null; // 프레임 시간 확보
            player.Begin(new Vector2(-1f, 0f)); // 이전 프레임 시작 위치
            player.Step(Vector2.right, false, false, false, 1f, -4f, 4f, -4f, 4f); // 이전 프레임 화면 이동
            Assert.That(player.PreviousPosition.x, Is.EqualTo(-1f).Within(0.001f)); // 이전 프레임 시작 확인
            Assert.That(player.transform.position.x, Is.EqualTo(1f).Within(0.001f)); // 현재 플레이어 위치 확인
            enemyObject = new GameObject("DashOrderEnemy"); // 현재 프레임 새 적
            enemyObject.transform.position = Vector3.zero; // 이동 경로 중간 위치
            EnemyActor enemy = EnemyActorTestFactory.AttachActor(enemyObject, EnemyKind.Dash); // 돌진 적 구성
            enemy.Begin(EnemyKind.Dash, player, null, camera); // 돌진 적 초기화
            enemy.Advance(0.8f); // 돌진 예고 종료
            enemy.SendMessage("Update"); // 플레이어 갱신 전 적 갱신
            Assert.That(player.Survival.Health, Is.EqualTo(100)); // 과거 경로의 오피격 방지
            player.Step(Vector2.zero, false, false, false, 0.01f, -4f, 4f, -4f, 4f); // 현재 프레임 이동 갱신
            enemy.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); // 현재 이동 후 접촉 처리
            Assert.That(player.Survival.Health, Is.EqualTo(100)); // 현재 위치의 비접촉 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            if (enemyObject != null) // 적 존재 확인
            { // 적 제거 시작
                Object.Destroy(enemyObject); // 시험 적 제거
            } // 적 제거 종료
            Object.Destroy(playerObject); // 시험 플레이어 제거
            Object.Destroy(cameraObject); // 시험 카메라 제거
            Time.timeScale = previousScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 플레이 시험 등록
    public IEnumerator DashDamagePrecedesBulletOnSameFrame() // 같은 프레임 돌진 우선 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("ContactPriorityCamera"); // 시험 카메라
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
        EnemyBulletPool pool = EnemyBulletPlayTestFactory.Create(cameraObject); // 적탄 풀 구성
        GameObject playerObject = new GameObject("ContactPriorityPlayer"); // 시험 플레이어
        BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
        GameObject enemyObject = null; // 적 객체 참조
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            yield return null; // 기존 프레임 종료
            player.AssignCamera(camera); // 시험 카메라 연결
            player.Begin(Vector2.zero); // 플레이어 위치 초기화
            EnemyBullet bullet = pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, player); // 적보다 먼저 적탄 생성
            Assert.That(bullet, Is.Not.Null); // 시험 적탄 생성 확인
            enemyObject = new GameObject("ContactPriorityDash"); // 시험 돌진 적 생성
            enemyObject.transform.position = Vector3.zero; // 플레이어 접촉 위치
            EnemyActor enemy = EnemyActorTestFactory.AttachActor(enemyObject, EnemyKind.Dash); // 적 구성
            enemy.Begin(EnemyKind.Dash, player, pool, camera); // 돌진 적 초기화
            enemy.Advance(0.8f); // 돌진 예고 종료
            yield return null; // 첫 접촉 프레임 대기
            yield return null; // 접촉 처리 완료 대기
            Assert.That(player.Survival.Health, Is.EqualTo(85)); // 돌진 우선 피해 확인
            Assert.That(bullet.InUse, Is.False); // 무적 중 적탄 회수 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            if (enemyObject != null) // 적 존재 확인
            { // 적 제거 시작
                Object.Destroy(enemyObject); // 시험 적 제거
            } // 적 제거 종료
            Object.Destroy(playerObject); // 시험 플레이어 제거
            Object.Destroy(cameraObject); // 시험 카메라 제거
            Time.timeScale = previousScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 플레이 시험 등록
    public IEnumerator DashChecksTheCurrentPlayerMovementAfterUpdate() // 현재 프레임 이동 접촉 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("DashContactCamera"); // 시험 카메라
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
        GameObject playerObject = new GameObject("DashContactPlayer"); // 시험 플레이어
        BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
        GameObject enemyObject = null; // 적 객체 참조
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            yield return null; // 프레임 시간 확보
            player.Begin(new Vector2(-1f, 0f)); // 현재 이동 시작 위치
            enemyObject = new GameObject("DashContactEnemy"); // 시험 돌진 적
            enemyObject.transform.position = Vector3.zero; // 현재 경로 중간 위치
            EnemyActor enemy = EnemyActorTestFactory.AttachActor(enemyObject, EnemyKind.Dash); // 적 구성
            enemy.Begin(EnemyKind.Dash, player, null, camera); // 돌진 적 초기화
            enemy.Advance(0.8f); // 돌진 예고 종료
            enemy.SendMessage("Update"); // 플레이어 이동 전 적 갱신
            player.Step(Vector2.right, false, false, false, 1f, -4f, 4f, -4f, 4f); // 현재 프레임 화면 이동
            enemy.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); // 현재 이동 후 접촉 처리
            Assert.That(player.Survival.Health, Is.EqualTo(85)); // 현재 궤적의 충돌 피해 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            if (enemyObject != null) // 적 존재 확인
            { // 적 제거 시작
                Object.Destroy(enemyObject); // 시험 적 제거
            } // 적 제거 종료
            Object.Destroy(playerObject); // 시험 플레이어 제거
            Object.Destroy(cameraObject); // 시험 카메라 제거
            Time.timeScale = previousScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 클래스 종료
