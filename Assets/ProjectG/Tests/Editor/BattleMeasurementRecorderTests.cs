using NUnit.Framework; // 단위 시험 기능
using UnityEngine; // 유니티 객체 기능
using UnityEngine.Rendering; // 그래픽 장치 종류
using ProjectG; // 전투 측정 기능
public class BattleMeasurementRecorderTests // 전투 측정 수집 시험
{ // 시험 시작
    [Test] // 전투 적탄 수 조회 시험
    public void PoolCountsOnlyActiveBulletsAndDoesNotDoubleRecycle() // 활성 적탄 수 확인
    { // 시험 시작
        GameObject holder = new GameObject("MeasuredPool"); // 풀 객체 생성
        try // 시험 범위 시작
        { // 준비 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(holder); // 적탄 풀 생성
            EnemyBullet first = pool.Spawn(Vector2.zero, Vector2.up, 0.1f, 1, -10f, null); // 첫 적탄 생성
            EnemyBullet second = pool.Spawn(Vector2.one, Vector2.up, 0.1f, 1, -10f, null); // 둘째 적탄 생성
            Assert.That(pool.ActiveCount, Is.EqualTo(2)); // 활성 탄환 두 발
            pool.Recycle(first); // 첫 적탄 회수
            pool.Recycle(first); // 중복 회수 시도
            Assert.That(pool.ActiveCount, Is.EqualTo(1)); // 중복 회수 무시
            second.gameObject.SetActive(false); // 남은 탄환 비활성화
            Assert.That(pool.ActiveCount, Is.Zero); // 비활성 탄환 제외
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 실제 환경과 전투 통계 시험
    public void CapturesObservedContextAndCombatStateBeforeResult() // 시드와 결과 직전 상태 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("MeasurementCamera"); // 카메라 객체 생성
        GameObject playerObject = new GameObject("MeasurementPlayer"); // 플레이어 객체 생성
        float previous = Time.timeScale; // 기존 시간 보관
        try // 시험 범위 시작
        { // 준비 시작
            Time.timeScale = 1f; // 전투 시간 설정
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 생성
            camera.orthographicSize = 5f; // 시험 화면 범위
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 생성
            player.Begin(Vector2.zero); // 출격 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(cameraObject); // 적탄 풀 생성
            StageDirector stage = EnemyActorTestFactory.AttachStage(cameraObject); // 스테이지 생성
            stage.Begin(player, camera, pool, null, null); // 실제 스테이지 시작
            BattleMeasurementRecorder recorder = cameraObject.AddComponent<BattleMeasurementRecorder>(); // 수집기 생성
            recorder.Begin(player, stage, pool, 721); // 측정 대상과 시드 연결
            Assert.That(recorder.Measurement.Context.Seed, Is.EqualTo(721)); // 출격 시드 기록
            Assert.That(recorder.Measurement.Context.Character, Is.EqualTo(CharacterType.Rapid)); // 캐릭터 기록
            Assert.That(recorder.Measurement.Context.Cpu, Is.EqualTo(SystemInfo.processorType)); // 실제 CPU 정보
            Assert.That(recorder.Measurement.Context.Gpu, Is.EqualTo(SystemInfo.graphicsDeviceName)); // 실제 GPU 정보
            Assert.That(recorder.Measurement.Context.RenderDeviceAvailable, Is.EqualTo(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)); // 실제 장치 구분
            Assert.That(recorder.Measurement.Context.Source, Is.EqualTo(Application.isBatchMode ? MeasurementSource.Automation : MeasurementSource.Unknown)); // 자동과 미확인 출처 구분
            player.Statistics.RecordGraze(); // 실제 그레이즈 기록
            player.Statistics.RecordHit(); // 실제 피격 기록
            player.Statistics.RecordUltimate(); // 필살기 발동 기록
            player.Statistics.RecordCard(CardId.C01); // 카드 선택 기록
            DropRecord drop = player.Statistics.RecordDropSpawned(new UpgradeDrop(UpgradeItemId.Damage, UpgradeItemGrade.I)); // 실제 드롭 기록
            player.Statistics.RecordDropOutcome(drop, DropOutcome.Acquired); // 실제 획득 기록
            pool.Spawn(Vector2.one, Vector2.up, 0.1f, 1, -10f, player); // 활성 적탄 생성
            stage.Advance(3f, 3f); // 전투 시간 진행
            recorder.RefreshSnapshot(); // 현재 기록 반영
            Assert.That(recorder.Measurement.Combat.CombatSeconds, Is.EqualTo(3d)); // 전투 시간 기록
            Assert.That(recorder.Measurement.Combat.Hits, Is.EqualTo(1)); // 피격 수 기록
            Assert.That(recorder.Measurement.Combat.Grazes, Is.EqualTo(1)); // 그레이즈 수 기록
            Assert.That(recorder.Measurement.Combat.CardsSelected, Is.EqualTo(1)); // 카드 선택 수 기록
            Assert.That(recorder.Measurement.Combat.UltimateActivations, Is.EqualTo(1)); // 필살기 수 기록
            Assert.That(recorder.Measurement.Combat.ItemDrops, Is.EqualTo(1)); // 드롭 수 기록
            Assert.That(recorder.Measurement.Combat.ItemPickups, Is.EqualTo(1)); // 획득 수 기록
            Assert.That(recorder.Measurement.Combat.ActiveEnemyProjectiles, Is.EqualTo(1)); // 활성 적탄 수 기록
            Assert.That(recorder.BuildReport(), Does.Contain("미측정")); // 표본 없는 성능 표시
            stage.State.Fail(); // 결과 화면 전 실패 확정
            Assert.That(recorder.Measurement.Combat.CurrentStagePhase, Is.EqualTo(StagePhase.Failed.ToString())); // 최종 이벤트 직후 상태 기록
            Assert.That(recorder.Measurement.Combat.Hits, Is.EqualTo(1)); // 최종 기록 보존
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 스테이지와 수집기 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 재출격 상태 교체 시험
    public void RestartCreatesNewMeasurementAndObservesTheNewStageState() // 새 출격 연결 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("RestartMeasureCamera"); // 카메라 객체
        GameObject playerObject = new GameObject("RestartMeasurePlayer"); // 플레이어 객체
        try // 시험 범위 시작
        { // 준비 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 생성
            camera.orthographicSize = 5f; // 시험 화면 크기
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 생성
            player.Begin(Vector2.zero); // 첫 출격 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(cameraObject); // 적탄 풀 생성
            StageDirector stage = EnemyActorTestFactory.AttachStage(cameraObject); // 스테이지 생성
            stage.Begin(player, camera, pool, null, null); // 단계 초기화
            BattleMeasurementRecorder recorder = cameraObject.AddComponent<BattleMeasurementRecorder>(); // 측정 수집기
            recorder.Begin(player, stage, pool, 33); // 고정 시드 연결
            PlayMeasurement first = recorder.Measurement; // 첫 출격 측정 보관
            StageProgression oldState = stage.State; // 이전 진행 상태 보관
            player.Begin(Vector2.zero, CharacterType.Sniper); // 같은 객체의 새 출격
            Assert.That(recorder.Measurement, Is.Not.SameAs(first)); // 새 측정 상태 생성
            Assert.That(recorder.Measurement.Context.Character, Is.EqualTo(CharacterType.Sniper)); // 새 캐릭터 반영
            oldState.Fail(); // 과거 진행 상태 변경
            Assert.That(recorder.Measurement.Combat.CurrentStagePhase, Is.EqualTo(StagePhase.Preparation.ToString())); // 이전 상태 알림 제외
            stage.State.Fail(); // 새 출격 실패
            Assert.That(recorder.Measurement.Combat.CurrentStagePhase, Is.EqualTo(StagePhase.Failed.ToString())); // 새 상태 알림 반영
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 카메라와 수집기 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 최대 활성 적탄 수 보존 시험
    public void PeakBulletCountSurvivesRecyclingAndResetsForNextRun() // 최대값과 새 출격 초기화 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("PeakMeasureCamera"); // 카메라 객체 생성
        GameObject playerObject = new GameObject("PeakMeasurePlayer"); // 플레이어 객체 생성
        try // 시험 범위 시작
        { // 준비 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 첫 출격 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(cameraObject); // 적탄 풀 생성
            StageDirector stage = EnemyActorTestFactory.AttachStage(cameraObject); // 스테이지 생성
            stage.Begin(player, camera, pool, null, null); // 스테이지 연결
            BattleMeasurementRecorder recorder = cameraObject.AddComponent<BattleMeasurementRecorder>(); // 측정 수집기 생성
            recorder.Begin(player, stage, pool, 72); // 첫 출격 측정 연결
            EnemyBullet first = pool.Spawn(Vector2.zero, Vector2.up, 0.1f, 1, -10f, player); // 첫 적탄 생성
            EnemyBullet second = pool.Spawn(Vector2.one, Vector2.up, 0.1f, 1, -10f, player); // 둘째 적탄 생성
            recorder.RefreshSnapshot(); // 두 발 활성 상태 수집
            Assert.That(recorder.Measurement.Combat.PeakEnemyProjectiles, Is.EqualTo(2)); // 최초 최대값 기록
            pool.Recycle(first); // 첫 탄환 회수
            pool.Recycle(second); // 둘째 탄환 회수
            recorder.RefreshSnapshot(); // 회수 후 상태 수집
            Assert.That(recorder.Measurement.Combat.ActiveEnemyProjectiles, Is.Zero); // 현재 활성 수 초기화
            Assert.That(recorder.Measurement.Combat.PeakEnemyProjectiles, Is.EqualTo(2)); // 이전 최대값 보존
            Assert.That(recorder.BuildReport(), Does.Contain("최대 활성 적탄: 2")); // 최대값 보고 표시
            player.Begin(Vector2.zero); // 같은 구성의 새 출격
            Assert.That(recorder.Measurement.Combat.PeakEnemyProjectiles, Is.Zero); // 새 출격 최대값 초기화
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 스테이지와 수집기 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
