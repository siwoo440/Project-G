using System.Collections; // 코루틴 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 플레이 시험
public sealed class DemoSpawnerWarningPlayTests // 시험 발사기 경고 제거 시험
{ // 시험 시작
    [UnityTest] // 플레이 시험 등록
    public IEnumerator DemoSpawnerKeepsLaneAndTimingWithoutWarning() // 예고 없이 기존 발사 유지
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("DemoSpawnerCamera"); // 시험 카메라
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
        EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 적탄 풀
        GameObject playerObject = new GameObject("DemoSpawnerPlayer"); // 시험 플레이어
        BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
        GameObject spawnerObject = new GameObject("DemoSpawner"); // 시험 발사기
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            camera.orthographicSize = 5f; // 카메라 범위 설정
            player.Begin(new Vector2(0f, -4f)); // 안전한 시험 위치
            DemoBulletSpawner spawner = spawnerObject.AddComponent<DemoBulletSpawner>(); // 발사기 구성
            spawner.Begin(pool, player, camera); // 발사기 시작
            Assert.That(spawnerObject.transform.Find("EnemyShotWarning"), Is.Null); // 탄막 생성 경고 없음
            yield return new WaitForSeconds(0.8f); // 첫 발사 전 대기
            Assert.That(pool.ActiveCount, Is.Zero); // 첫 발사 이전 확인
            float deadline = Time.realtimeSinceStartup + 2f; // 첫 발사 대기 제한
            yield return new WaitUntil(() => pool.ActiveCount == 1 || Time.realtimeSinceStartup >= deadline); // 발사 또는 제한 시간 대기
            Assert.That(pool.ActiveCount, Is.EqualTo(1)); // 제한 시간 안의 첫 발사 확인
            EnemyBullet[] first = pool.GetComponentsInChildren<EnemyBullet>(); // 첫 활성 탄환 조회
            Assert.That(first[0].transform.position.x, Is.EqualTo(-5f * 16f / 9f * 0.6f).Within(0.001f)); // 첫 가로 경로 확인
            Assert.That(first[0].transform.position.y, Is.EqualTo(4.85f).Within(0.001f)); // 발사 순간 세로 위치 확인
            yield return new WaitForSeconds(0.9f); // 두 번째 발사 전 대기
            Assert.That(pool.ActiveCount, Is.EqualTo(1)); // 발사 간격 유지 확인
            yield return new WaitForSeconds(0.45f); // 두 번째 발사 시점 대기
            Assert.That(pool.ActiveCount, Is.EqualTo(2)); // 두 번째 발사 확인
            EnemyBullet[] second = pool.GetComponentsInChildren<EnemyBullet>(); // 활성 탄환 조회
            Assert.That(System.Array.Exists(second, bullet => Mathf.Abs(bullet.transform.position.x) < 0.001f), Is.True); // 두 번째 중앙 경로 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.Destroy(spawnerObject); // 발사기 제거
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라와 적탄 제거
            Time.timeScale = previousScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 클래스 종료
