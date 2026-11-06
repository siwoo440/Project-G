using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 전투 기능
using ProjectG; // 측정 보고서 기능
public class MeasurementReportContentsTests // 보고서의 스테이지 정보 시험
{ // 시험 시작
    [Test] // 실제 스테이지와 구간·보스 단계 시험
    public void ReportIncludesObservedStageAndBossProgress() // 확인된 측정 조건 표시
    { // 시험 시작
        GameObject cameraObject = new GameObject("ReportStageCamera"); // 시험 카메라 객체
        GameObject playerObject = new GameObject("ReportStagePlayer"); // 시험 플레이어 객체
        try // 시험 범위 시작
        { // 시험 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 적탄 풀 구성
            StageDirector stage = cameraObject.AddComponent<StageDirector>(); // 스테이지 구성
            stage.Begin(player, camera, pool, null, null); // 스테이지 연결
            MeasurementContext context = new MeasurementContext(); // 시험 환경 정보
            context.Stage = "하늘섬 외곽 시험 스테이지"; // 실제 스테이지 이름
            BattleMeasurementRecorder recorder = cameraObject.AddComponent<BattleMeasurementRecorder>(); // 측정 수집기
            recorder.Begin(player, stage, pool, 42, () => context); // 시험 환경과 시드 연결
            CombatMeasurementSnapshot combat = new CombatMeasurementSnapshot(); // 확인된 전투 상태
            combat.CurrentStagePhase = "FinalBoss"; // 종료 시 기록 구간
            combat.BossPhase = 2; // 확인된 보스 단계
            Assert.That(recorder.Measurement.UpdateCombat(combat), Is.True); // 실제 스냅샷 입력
            string report = recorder.BuildReport(); // 저장할 보고서 생성
            Assert.That(report, Does.Contain("스테이지: 하늘섬 외곽 시험 스테이지")); // 실제 스테이지 표시
            Assert.That(report, Does.Contain("종료 구간: FinalBoss")); // 측정된 구간 표시
            Assert.That(report, Does.Contain("보스 단계: 2")); // 측정된 보스 단계 표시
            combat.CurrentStagePhase = null; // 구간 미확인 상태
            combat.BossPhase = null; // 보스 단계 미확인 상태
            Assert.That(recorder.Measurement.UpdateCombat(combat), Is.True); // 미확인 스냅샷 입력
            report = recorder.BuildReport(); // 미확인 보고서 생성
            Assert.That(report, Does.Contain("종료 구간: 미확인")); // 없는 구간의 미확인 표시
            Assert.That(report, Does.Contain("보스 단계: 미확인")); // 없는 단계의 미확인 표시
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 시험 카메라 제거
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 종료와 재출격 시 보스 단계 보존 시험
    public void LastObservedBossPhaseSurvivesFailureAndResetsForNewRun() // 관측값 보존과 초기화 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("BossReportCamera"); // 시험 카메라 객체
        GameObject playerObject = new GameObject("BossReportPlayer"); // 시험 플레이어 객체
        try // 시험 범위 시작
        { // 시험 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라 구성
            camera.orthographicSize = 5f; // 시험 화면 높이
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 적탄 풀 구성
            StageDirector stage = cameraObject.AddComponent<StageDirector>(); // 스테이지 구성
            stage.Begin(player, camera, pool, null, null); // 스테이지 연결
            BattleMeasurementRecorder recorder = cameraObject.AddComponent<BattleMeasurementRecorder>(); // 측정 수집기
            recorder.Begin(player, stage, pool, 42); // 실제 전투 기록 연결
            Assert.That(recorder.Measurement.Combat.BossPhase, Is.Null); // 보스 관측 전 미확인
            stage.Advance(65f, 65f); // 중간 보스 전환
            stage.Advance(2f, 2f); // 중간 보스 등장
            Assert.That(stage.Boss, Is.Not.Null); // 중간 보스 확인
            stage.Boss.ApplyDamage(10000f, player); // 중간 보스 처치
            stage.Advance(0.1f, 0.1f); // 다음 전투 진행
            stage.Advance(50f, 50f); // 최종 보스 전환
            stage.Advance(2f, 2f); // 최종 보스 등장
            Assert.That(stage.Boss, Is.Not.Null); // 최종 보스 확인
            stage.Boss.ApplyDamage(10000f, player); // 최종 보스 둘째 단계 진입
            recorder.RefreshSnapshot(); // 실제 둘째 단계 관측
            Assert.That(recorder.Measurement.Combat.BossPhase, Is.EqualTo(2)); // 관측한 보스 단계
            stage.State.Fail(); // 출격 실패와 보스 객체 정리
            Assert.That(recorder.Measurement.Combat.BossPhase, Is.EqualTo(2)); // 종료 후 마지막 관측값 보존
            Assert.That(recorder.BuildReport(), Does.Contain("보스 단계: 2")); // 종료 보고서에 관측값 표시
            player.Begin(Vector2.zero); // 새 출격 초기화
            Assert.That(recorder.Measurement.Combat.BossPhase, Is.Null); // 이전 출격 관측값 제거
            Assert.That(recorder.BuildReport(), Does.Contain("보스 단계: 미확인")); // 새 출격 미확인 표시
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 시험 카메라 제거
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료