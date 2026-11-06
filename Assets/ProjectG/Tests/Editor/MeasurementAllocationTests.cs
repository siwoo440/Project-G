using NUnit.Framework; // 자동 시험 도구
using ProjectG; // 전투 측정 기능
using UnityEngine; // 유니티 객체 기능
public sealed class MeasurementAllocationTests // 전투 측정 독립성과 할당 시험
{ // 시험 시작
    [Test] // 반복 스냅샷 독립성 시험
    public void UpdatesPreservePreviousCopiesAndRejectInvalidInput() // 이전 사본과 유효 상태 보존
    { // 시험 시작
        PlayMeasurement measurement = new PlayMeasurement(new MeasurementContext()); // 격리 측정 상태
        CombatMeasurementSnapshot input = new CombatMeasurementSnapshot(); // 수정 가능한 입력
        input.CombatSeconds = 1d; // 첫 전투 시간
        input.TotalSeconds = 2d; // 첫 전체 시간
        input.CardsSelected = 1; // 첫 카드 수
        input.UltimateActivations = 2; // 첫 필살기 수
        input.ItemDrops = 3; // 첫 드롭 수
        input.ItemPickups = 4; // 첫 획득 수
        input.EnemyDefeats = 5; // 첫 처치 수
        input.Hits = 6; // 첫 피격 수
        input.Grazes = 7; // 첫 그레이즈 수
        input.MidBossDefeats = 1; // 첫 중간 보스 기록
        input.FinalBossDefeats = 2; // 첫 최종 보스 기록
        input.BossPhase = 3; // 첫 보스 단계
        input.CurrentStagePhase = "FinalBoss"; // 첫 스테이지 구간
        input.ActiveEnemyProjectiles = 8; // 첫 활성 적탄 수
        input.PeakEnemyProjectiles = 9; // 첫 최대 적탄 수
        Assert.That(measurement.UpdateCombat(input), Is.True); // 첫 기록 수용
        CombatMeasurementSnapshot earlier = measurement.Combat; // 갱신 전 반환 사본
        input.CombatSeconds = 11d; // 다음 전투 시간
        input.TotalSeconds = 12d; // 다음 전체 시간
        input.CardsSelected = 11; // 다음 카드 수
        input.UltimateActivations = 12; // 다음 필살기 수
        input.ItemDrops = 13; // 다음 드롭 수
        input.ItemPickups = 14; // 다음 획득 수
        input.EnemyDefeats = 15; // 다음 처치 수
        input.Hits = 16; // 다음 피격 수
        input.Grazes = 17; // 다음 그레이즈 수
        input.MidBossDefeats = 0; // 다음 중간 보스 기록
        input.FinalBossDefeats = 0; // 다음 최종 보스 기록
        input.BossPhase = null; // 다음 보스 단계 미확인
        input.CurrentStagePhase = null; // 다음 구간 미확인
        input.ActiveEnemyProjectiles = 18; // 다음 활성 적탄 수
        input.PeakEnemyProjectiles = 19; // 다음 최대 적탄 수
        Assert.That(measurement.UpdateCombat(input), Is.True); // 다음 기록 수용
        CombatMeasurementSnapshot current = measurement.Combat; // 갱신 후 독립 사본
        Assert.That(current.CombatSeconds, Is.EqualTo(11d)); // 전투 시간 갱신
        Assert.That(current.TotalSeconds, Is.EqualTo(12d)); // 전체 시간 갱신
        Assert.That(current.CardsSelected, Is.EqualTo(11)); // 카드 수 갱신
        Assert.That(current.UltimateActivations, Is.EqualTo(12)); // 필살기 수 갱신
        Assert.That(current.ItemDrops, Is.EqualTo(13)); // 드롭 수 갱신
        Assert.That(current.ItemPickups, Is.EqualTo(14)); // 획득 수 갱신
        Assert.That(current.EnemyDefeats, Is.EqualTo(15)); // 처치 수 갱신
        Assert.That(current.Hits, Is.EqualTo(16)); // 피격 수 갱신
        Assert.That(current.Grazes, Is.EqualTo(17)); // 그레이즈 수 갱신
        Assert.That(current.MidBossDefeats, Is.Zero); // 중간 보스 초기화
        Assert.That(current.FinalBossDefeats, Is.Zero); // 최종 보스 초기화
        Assert.That(current.BossPhase, Is.Null); // 미확인 단계 갱신
        Assert.That(current.CurrentStagePhase, Is.Null); // 미확인 구간 갱신
        Assert.That(current.ActiveEnemyProjectiles, Is.EqualTo(18)); // 활성 적탄 갱신
        Assert.That(current.PeakEnemyProjectiles, Is.EqualTo(19)); // 최대 적탄 갱신
        Assert.That(earlier.CombatSeconds, Is.EqualTo(1d)); // 이전 시간 사본 보존
        Assert.That(earlier.BossPhase, Is.EqualTo(3)); // 이전 단계 사본 보존
        Assert.That(earlier.CurrentStagePhase, Is.EqualTo("FinalBoss")); // 이전 구간 사본 보존
        Assert.That(earlier.ActiveEnemyProjectiles, Is.EqualTo(8)); // 이전 적탄 사본 보존
        current.Hits = 99; // 반환 사본 수정
        input.Hits = -1; // 유효하지 않은 입력 수정
        Assert.That(measurement.UpdateCombat(input), Is.False); // 무효 입력 거부
        Assert.That(measurement.Combat.Hits, Is.EqualTo(16)); // 기존 유효 기록 보존
    } // 시험 종료
    [Test] // 수집기 갱신과 재출격 독립 시험
    public void RecorderRefreshPreservesPreviousRun() // 수집기 갱신과 재출격 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("AllocationCamera"); // 격리 카메라
        GameObject playerObject = new GameObject("AllocationPlayer"); // 격리 플레이어
        try // 시험 범위
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 첫 출격 시작
            EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 적탄 풀 구성
            StageDirector stage = cameraObject.AddComponent<StageDirector>(); // 스테이지 구성
            stage.Begin(player, camera, pool, null, null); // 스테이지 연결
            BattleMeasurementRecorder recorder = cameraObject.AddComponent<BattleMeasurementRecorder>(); // 측정 수집기
            recorder.Begin(player, stage, pool, 16); // 측정 시작
            player.Statistics.RecordHit(); // 첫 출격 피격 기록
            recorder.RefreshSnapshot(); // 첫 기록 반영
            PlayMeasurement previous = recorder.Measurement; // 첫 출격 기록 보관
            recorder.RefreshSnapshot(); // 반복 수집 갱신
            recorder.RefreshSnapshot(); // 연속 수집 갱신
            player.Begin(Vector2.zero); // 같은 객체에서 새 출격
            Assert.That(recorder.Measurement.Combat.Hits, Is.Zero); // 새 출격 피격 초기화
            Assert.That(previous.Combat.Hits, Is.EqualTo(1)); // 이전 출격 기록 독립성
            Assert.That(recorder.BuildReport(), Does.Contain("피격 0")); // 새 출격 보고서 반영
        } // 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(cameraObject); // 격리 카메라 제거
            UnityEngine.Object.DestroyImmediate(playerObject); // 격리 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 클래스 종료
