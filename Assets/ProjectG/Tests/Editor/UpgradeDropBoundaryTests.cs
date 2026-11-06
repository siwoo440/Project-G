using NUnit.Framework; // 시험 도구
using ProjectG; // 강화 아이템 기능
using UnityEngine; // 유니티 객체
public class UpgradeDropBoundaryTests // 강화 아이템 획득 경계 시험
{ // 시험 시작
    private sealed class ZeroRandom : System.Random // 고정 드롭 난수
    { // 난수 시작
        public override int Next(int maxValue) // 난수 요청
        { // 반환 시작
            return 0; // 첫 후보 반환
        } // 반환 종료
    } // 난수 종료
    private sealed class Effects : IUpgradeDropEffects // 격리된 획득 효과
    { // 효과 시작
        public int Acquired; // 실제 획득 횟수
        public bool CanApply(UpgradeItemId id, UpgradeItemGrade grade) // 적용 가능 확인
        { // 확인 시작
            return true; // 시험 효과 허용
        } // 확인 종료
        public bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, out string feedback) // 시험 효과 적용
        { // 적용 시작
            Acquired++; // 획득 횟수 증가
            feedback = "획득"; // 획득 안내
            return true; // 획득 성공
        } // 적용 종료
    } // 효과 종료
    [TestCase(-0.8f, 1)] // 만료 전 이동 접촉
    [TestCase(-2f, 0)] // 만료 후 이동 접촉
    public void PickupUsesOnlyMovementBeforeExpiry(float start, int expected) // 남은 시간 안의 접촉 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("BoundaryCamera"); // 격리 카메라
        GameObject playerObject = new GameObject("BoundaryPlayer"); // 격리 플레이어
        try // 시험 범위 준비
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 시간 진행
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(start, 0f)); // 획득 범위 바깥 시작
            Effects effects = new Effects(); // 실제 획득 관찰
            UpgradeDropManager manager = cameraObject.AddComponent<UpgradeDropManager>(); // 드롭 관리자
            manager.Begin(player, camera, effects, new ZeroRandom()); // 고정 드롭 연결
            float dropHeight = UpgradeDropManager.TrialFallSpeed * (UpgradeDropManager.TrialLifetime - 0.1f); // 만료 직전에 중심에 닿는 시작 높이
            Assert.That(manager.TryDrop(EnemyRank.Normal, new Vector2(0f, dropHeight)), Is.True); // 낙하할 아이템 생성
            manager.Advance(UpgradeDropManager.TrialLifetime - 0.1f); // 만료 직전까지 대기
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 유효 아이템 확인
            player.Step(Vector2.right, false, false, false, 0.5f, -8f, 8f, -5f, 5f); // 반 초의 이동 구간
            manager.Advance(0.5f); // 이동 중 만료 경계 처리
            Assert.That(effects.Acquired, Is.EqualTo(expected)); // 유효 시간 내 접촉만 획득
            Assert.That(manager.ActiveCount, Is.Zero); // 획득 또는 만료 후 제거
            Assert.That(player.Statistics.DropsAcquired, Is.EqualTo(expected)); // 획득 통계 확인
            Assert.That(player.Statistics.DropsExpired, Is.EqualTo(1 - expected)); // 만료 통계 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 카메라와 아이템 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Time.timeScale = oldScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 정지 중 유지 시간 시험
    public void PauseDoesNotConsumeRemainingTimeOrCollectItem() // 정지 후 정상 획득
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("PauseDropCamera"); // 격리 카메라
        GameObject playerObject = new GameObject("PauseDropPlayer"); // 격리 플레이어
        try // 시험 범위 준비
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 시간 진행
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(-0.8f, 0f)); // 획득 범위 바깥 시작
            Effects effects = new Effects(); // 획득 효과 관찰
            UpgradeDropManager manager = cameraObject.AddComponent<UpgradeDropManager>(); // 드롭 관리자
            manager.Begin(player, camera, effects, new ZeroRandom()); // 고정 드롭 연결
            float dropHeight = UpgradeDropManager.TrialFallSpeed * (UpgradeDropManager.TrialLifetime - 0.1f); // 만료 직전에 중심에 닿는 시작 높이
            Assert.That(manager.TryDrop(EnemyRank.Normal, new Vector2(0f, dropHeight)), Is.True); // 낙하할 아이템 생성
            manager.Advance(UpgradeDropManager.TrialLifetime - 0.1f); // 만료 직전까지 대기
            Time.timeScale = 0f; // 카드 선택과 일시정지
            manager.Advance(1f); // 정지 중 진행 시도
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 정지 중 만료 방지
            Assert.That(effects.Acquired, Is.Zero); // 정지 중 획득 방지
            Time.timeScale = 1f; // 전투 시간 재개
            player.Step(Vector2.right, false, false, false, 0.5f, -8f, 8f, -5f, 5f); // 유효 시간 내 통과
            manager.Advance(0.5f); // 재개 후 획득 판정
            Assert.That(effects.Acquired, Is.EqualTo(1)); // 남은 시간 보존 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 카메라와 아이템 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Time.timeScale = oldScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
