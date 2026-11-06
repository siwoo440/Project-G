using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 전투 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 게임 전투 기능
public class UpgradeDropCombatPlayTests // 실제 처치와 획득 통합 시험
{ // 시험 시작
    private sealed class ZeroRandom : System.Random // 정해진 드롭 난수
    { // 난수 시작
        public override int Next(int maxValue) // 드롭과 등급 선택
        { // 선택 시작
            return 0; // 첫 확률과 첫 유효 후보
        } // 선택 종료
    } // 난수 종료
    [UnityTest] // 실제 전투 드롭 시험
    public IEnumerator EnemyDefeatCreatesPickupThatAppliesDamageAndResets() // 처치부터 재출격까지 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("RealDropCamera"); // 격리 전투 카메라
        GameObject playerObject = null; // 전투 플레이어 참조
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위 시작
        { // 시험 시작
            Time.timeScale = 1f; // 전투 시간 초기화
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라 구성
            camera.transform.position = new Vector3(0f, 0f, -10f); // 전투 화면 위치
            playerObject = BattleSetup.Create(camera); // 실제 전투 구성 생성
            BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 실제 전투 플레이어
            BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 실제 경험치 상태
            EnemyBulletPool pool = cameraObject.GetComponent<EnemyBulletPool>(); // 실제 적탄 풀
            UpgradeDropManager manager = cameraObject.GetComponent<UpgradeDropManager>(); // 실제 드롭 관리자
            Assert.That(manager, Is.Not.Null); // 전투 구성의 드롭 관리자 확인
            manager.Begin(player, camera, new UpgradeDropEffects(player, progress), new ZeroRandom()); // 실제 효과와 고정 난수 연결
            yield return null; // 실제 전투 프레임 시작
            Vector3 spawn = player.transform.position + Vector3.right * 2f; // 플레이어 밖 드롭 위치
            GameObject enemyObject = new GameObject("FirstRealEnemy"); // 실제 일반 적 객체
            enemyObject.transform.SetParent(cameraObject.transform, true); // 전투 장면 소속
            enemyObject.transform.position = spawn; // 실제 처치 위치
            EnemyActor enemy = EnemyActorTestFactory.AttachActor(enemyObject, EnemyKind.Aimed); // 실제 일반 적 구성
            enemy.Begin(EnemyKind.Aimed, player, pool, camera); // 일반 적 초기화
            enemy.ApplyDamage(1000f, player); // 일반 적 실제 처치
            Assert.That(enemy.IsAlive, Is.False); // 적 사망 확인
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 실제 처치에서 드롭 생성
            Assert.That(player.Build.Upgrades.Count(UpgradeItemId.Damage), Is.Zero); // 접촉 전 미획득
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 드롭 중 전투 비정지
            player.Step(Vector2.right, false, false, false, 0.5f, -8f, 8f, -5f, 5f); // 필드 아이템으로 이동
            manager.Advance(0.01f); // 이동 접촉 검사
            yield return null; // 실제 획득 프레임 경과
            Assert.That(manager.ActiveCount, Is.Zero); // 아이템 획득 후 제거
            Assert.That(player.Build.Upgrades.Count(UpgradeItemId.Damage), Is.EqualTo(1)); // 실제 강화 획득
            Assert.That(player.Build.Damage(1f, false), Is.EqualTo(1.1f).Within(0.0001f)); // 실제 탄환 피해 증가
            Assert.That(manager.LastFeedback, Does.Contain("화력 I")); // 획득 종류와 등급 표시
            Assert.That(manager.LastFeedback, Does.Contain("탄환 피해 +10%")); // 실제 변화량 표시
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 획득 후 전투 비정지
            GameObject nextEnemyObject = new GameObject("SecondRealEnemy"); // 다음 일반 적 객체
            nextEnemyObject.transform.SetParent(cameraObject.transform, true); // 전투 장면 소속
            nextEnemyObject.transform.position = player.transform.position + Vector3.right * 2f; // 다음 처치 위치
            EnemyActor nextEnemy = EnemyActorTestFactory.AttachActor(nextEnemyObject, EnemyKind.Aimed); // 다음 일반 적 구성
            nextEnemy.Begin(EnemyKind.Aimed, player, pool, camera); // 다음 적 초기화
            nextEnemy.ApplyDamage(1000f, player); // 다음 적 실제 처치
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 재출격 전 필드 드롭
            player.Begin(player.transform.position); // 같은 전투의 새 출격 초기화
            Assert.That(manager.ActiveCount, Is.Zero); // 이전 출격 드롭 제거
            Assert.That(manager.LastFeedback, Is.Null.Or.Empty); // 이전 획득 안내 제거
            Assert.That(player.Build.Damage(1f, false), Is.EqualTo(1f)); // 임시 피해 강화 초기화
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 존재 확인
            { // 제거 시작
                Object.Destroy(playerObject); // 전투 플레이어 제거
            } // 제거 종료
            Object.Destroy(cameraObject); // 전투 카메라와 적 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료