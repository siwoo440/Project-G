using System.Collections.Generic; // 시험 난수 대기열
using NUnit.Framework; // 자동 시험 기능
using UnityEngine; // 유니티 전투 객체
using ProjectG; // 전투 기록 기능
public class RunMeasurementTests // 실제 전투 지표 시험
{ // 시험 시작
    private sealed class SequenceRandom : System.Random // 예측 가능한 드롭 난수
    { // 난수 시작
        private readonly Queue<int> values; // 난수 순서
        public SequenceRandom(params int[] sequence) // 시험 난수 생성
        { // 생성 시작
            values = new Queue<int>(sequence); // 난수 목록 보관
        } // 생성 종료
        public override int Next(int maxValue) // 시험 값 선택
        { // 선택 시작
            int next = values.Dequeue(); // 다음 시험 값
            Assert.That(next, Is.GreaterThanOrEqualTo(0).And.LessThan(maxValue)); // 난수 범위 확인
            return next; // 지정된 난수 반환
        } // 선택 종료
    } // 난수 종료
    [Test] // 실제 필살기 성공 횟수
    public void UltimateCountsOnlySuccessfulActivation() // 에너지 부족과 실제 발동 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("MeasurementCamera"); // 카메라 객체 생성
        GameObject playerObject = new GameObject("MeasurementPlayer"); // 플레이어 객체 생성
        try // 시험 범위 시작
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(cameraObject); // 적탄 풀 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.AssignCamera(camera); // 카메라 연결
            player.AssignBulletPool(pool); // 적탄 풀 연결
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.TryUseUltimate(), Is.False); // 에너지 부족 발동 차단
            Assert.That(player.Statistics.Ultimates, Is.Zero); // 실패한 입력 미집계
            player.Skills.GainEnergy(100); // 필살기 충전
            Assert.That(player.TryUseUltimate(), Is.True); // 실제 필살기 사용
            Assert.That(player.Statistics.Ultimates, Is.EqualTo(1)); // 실제 발동 한 번
            Assert.That(player.TryUseUltimate(), Is.False); // 중복 발동 차단
            Assert.That(player.Statistics.Ultimates, Is.EqualTo(1)); // 실패 입력 미집계
            player.Begin(Vector2.zero); // 새 출격 시작
            Assert.That(player.Statistics.Ultimates, Is.Zero); // 새 출격 기록 초기화
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 드롭 시도와 상태 기록
    public void NormalEnemyDropsTrackAcquisitionExpiryAndDiscardSeparately() // 실제 드롭 단계 구분
    { // 시험 시작
        GameObject cameraObject = new GameObject("MeasurementCamera"); // 전투 카메라 객체
        GameObject playerObject = new GameObject("MeasurementPlayer"); // 전투 플레이어 객체
        float previousTime = Time.timeScale; // 시간 배율 보관
        try // 시험 범위 시작
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 복원
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            UpgradeDropManager manager = cameraObject.AddComponent<UpgradeDropManager>(); // 드롭 관리자 구성
            manager.Begin(player, camera, new UpgradeDropEffects(player, null), new SequenceRandom(99, 0, 0, 0, 0, 0, 0, 0, 0, 0)); // 실패와 세 드롭 순서
            player.RegisterEnemyDefeat(EnemyRank.FinalBoss, 0, Vector2.zero); // 최종 보스 처치 알림
            Assert.That(player.Statistics.DropAttempts, Is.Zero); // 최종 보스 시도 제외
            player.RegisterEnemyDefeat(EnemyRank.Normal, 0, Vector2.zero); // 일반 적 미드롭 처치
            Assert.That(player.Statistics.DropAttempts, Is.EqualTo(1)); // 일반 적 시도 기록
            player.RegisterEnemyDefeat(EnemyRank.Normal, 0, Vector2.zero); // 플레이어 위치 드롭
            manager.Advance(0.1f); // 드롭 획득
            player.RegisterEnemyDefeat(EnemyRank.Normal, 0, Vector2.right * 3f); // 먼 위치 드롭
            manager.Advance(UpgradeDropManager.TrialLifetime + 1f); // 미획득 드롭 만료
            player.RegisterEnemyDefeat(EnemyRank.Normal, 0, Vector2.right * 3f); // 종료 때 남을 드롭
            RunStatistics stats = player.Statistics; // 이전 출격 기록 보관
            Assert.That(stats.DropAttempts, Is.EqualTo(4)); // 일반 적 네 회 추첨
            Assert.That(stats.DropsSpawned, Is.EqualTo(3)); // 실제 드롭 세 개
            Assert.That(stats.DropsAcquired, Is.EqualTo(1)); // 한 개 획득
            Assert.That(stats.DropsExpired, Is.EqualTo(1)); // 한 개 자연 만료
            Assert.That(stats.DropsDiscarded, Is.Zero); // 종료 전 미획득 유지
            Assert.That(stats.Drops[0].Id, Is.EqualTo(UpgradeItemId.Damage)); // 첫 획득 종류
            Assert.That(stats.Drops[0].Grade, Is.EqualTo(UpgradeItemGrade.I)); // 첫 획득 등급
            Assert.That(stats.Drops[0].Outcome, Is.EqualTo(DropOutcome.Acquired)); // 획득 상태
            Assert.That(stats.Drops[1].Outcome, Is.EqualTo(DropOutcome.Expired)); // 만료 상태
            Assert.That(stats.Complete(false, 12f, 14f), Is.True); // 실패 출격 확정
            Assert.That(stats.DropsDiscarded, Is.EqualTo(1)); // 남은 드롭은 종료 폐기
            Assert.That(stats.Drops[2].Outcome, Is.EqualTo(DropOutcome.Discarded)); // 종료와 만료 구분
            manager.Clear(); // 출격 객체 정리
            Assert.That(stats.DropsDiscarded, Is.EqualTo(1)); // 정리 시 중복 폐기 차단
            player.Begin(Vector2.zero); // 새 출격 시작
            Assert.That(player.Statistics.DropAttempts, Is.Zero); // 새 출격 시도 초기화
            Assert.That(player.Statistics.DropsSpawned, Is.Zero); // 새 출격 드롭 초기화
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 드롭 및 카메라 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Time.timeScale = previousTime; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 출격 중 직접 초기화 시험
    public void RestartDiscardsOldDropsWithoutContaminatingNewRun() // 출격별 드롭 소속 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("RestartMeasurementCamera"); // 카메라 객체 생성
        GameObject playerObject = new GameObject("RestartMeasurementPlayer"); // 플레이어 객체 생성
        try // 시험 범위 시작
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 첫 출격 시작
            UpgradeDropManager manager = cameraObject.AddComponent<UpgradeDropManager>(); // 드롭 관리자 구성
            manager.Begin(player, camera, new UpgradeDropEffects(player, null), new SequenceRandom(0, 0, 0)); // 확정 시험 드롭 연결
            Assert.That(manager.TryDrop(EnemyRank.Normal, Vector2.right * 3f), Is.True); // 첫 출격 드롭 생성
            RunStatistics previous = player.Statistics; // 첫 출격 기록 보관
            player.Begin(Vector2.zero); // 종료 전 새 출격 시작
            Assert.That(previous.DropsDiscarded, Is.EqualTo(1)); // 이전 드롭 폐기 기록
            Assert.That(previous.Drops[0].Outcome, Is.EqualTo(DropOutcome.Discarded)); // 이전 기록의 결과 확인
            Assert.That(player.Statistics.DropsSpawned, Is.Zero); // 새 출격에 이전 생성 기록 없음
            Assert.That(player.Statistics.DropsDiscarded, Is.Zero); // 새 출격에 이전 폐기 기록 없음
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 드롭과 카메라 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 보스 단계 성공 기록
    public void BossClearsAreRecordedBeforeResultIsFinished() // 실제 보스 처치와 결과 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("MeasurementCamera"); // 카메라 객체 생성
        GameObject playerObject = null; // 플레이어 객체 보관
        float previousTime = Time.timeScale; // 시간 배율 보관
        try // 시험 범위 시작
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 복원
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라 구성
            playerObject = BattleSetup.Create(camera); // 실제 전투 구성
            BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 조회
            StageDirector stage = cameraObject.GetComponent<StageDirector>(); // 스테이지 조회
            Assert.That(player.Statistics.MidBossEncountered, Is.False); // 중간 보스 등장 전
            Assert.That(player.Statistics.FinalBossEncountered, Is.False); // 최종 보스 등장 전
            stage.Advance(65f, 65f); // 중간 보스 진입
            stage.Advance(2f, 2f); // 보스 등장 안전 종료
            Assert.That(stage.Boss, Is.Not.Null); // 중간 보스 확인
            Assert.That(player.Statistics.MidBossEncountered, Is.True); // 중간 보스 실제 등장 기록
            Assert.That(player.Statistics.MidBossCleared, Is.False); // 처치 전 돌파 미기록
            stage.Boss.ApplyDamage(100000f, player); // 중간 보스 처치
            stage.Advance(0.1f, 0.1f); // 다음 전투 전환
            Assert.That(player.Statistics.MidBossCleared, Is.True); // 중간 보스 돌파
            Assert.That(player.Statistics.FinalBossCleared, Is.False); // 최종 보스 미돌파
            stage.Advance(50f, 50f); // 최종 보스 진입
            stage.Advance(2f, 2f); // 최종 보스 등장
            Assert.That(stage.Boss, Is.Not.Null); // 최종 보스 확인
            Assert.That(player.Statistics.FinalBossEncountered, Is.True); // 최종 보스 실제 등장 기록
            Assert.That(player.Statistics.FinalBossCleared, Is.False); // 처치 전 최종 돌파 미기록
            stage.Boss.ApplyDamage(100000f, player); // 첫 체력 경계
            stage.Boss.ApplyDamage(100000f, player); // 둘째 체력 경계
            stage.Boss.ApplyDamage(100000f, player); // 최종 보스 처치
            stage.Advance(0.1f, 0.1f); // 결과 전환
            Assert.That(player.Statistics.FinalBossCleared, Is.True); // 결과 확정 전 최종 돌파 기록
            Assert.That(player.Statistics.Finished, Is.True); // 출격 결과 확정
            Assert.That(player.Statistics.Cleared, Is.True); // 클리어 상태
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 존재 확인
            { // 제거 시작
                Object.DestroyImmediate(playerObject); // 플레이어 제거
            } // 제거 종료
            Object.DestroyImmediate(cameraObject); // 전투 객체 제거
            Time.timeScale = previousTime; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
