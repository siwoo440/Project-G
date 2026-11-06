using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 단위 시험 기능
using UnityEngine; // 유니티 객체 기능
using UnityEngine.Rendering; // 그래픽 장치 구분
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 전투 측정 기능
public class BattleMeasurementRecorderPlayTests // 실제 프레임 측정 시험
{ // 시험 시작
    [UnityTest] // 정지와 배치 표본 제외 시험
    public IEnumerator PausedOrHeadlessFramesAreNeverReportedAsRenderedCombat() // 표본 수집 조건 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("FrameMeasureCamera"); // 카메라 객체 생성
        GameObject playerObject = new GameObject("FrameMeasurePlayer"); // 플레이어 객체 생성
        float previous = Time.timeScale; // 기존 시간 배율 보관
        try // 시험 범위 시작
        { // 준비 시작
            Time.timeScale = 0f; // 일시정지 상태 설정
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            camera.orthographicSize = 5f; // 시험 화면 크기
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 상태 생성
            EnemyBulletPool pool = EnemyBulletPlayTestFactory.Create(cameraObject); // 적탄 풀 생성
            StageDirector stage = EnemyActorTestFactory.AttachStage(cameraObject); // 스테이지 생성
            stage.Begin(player, camera, pool, null, null); // 스테이지 연결
            BattleMeasurementRecorder recorder = cameraObject.AddComponent<BattleMeasurementRecorder>(); // 측정 수집기
            recorder.Begin(player, stage, pool, 42); // 측정 연결
            yield return null; // 첫 정지 프레임
            yield return null; // 다음 정지 프레임
            Assert.That(recorder.Measurement.FrameCount, Is.Zero); // 일시정지 표본 제외
            Time.timeScale = 1f; // 전투 재개
            yield return null; // 실행 프레임 진행
            yield return null; // 다음 실행 프레임
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) // 비대화 또는 그래픽 없음 확인
            { // 미측정 확인 시작
                Assert.That(recorder.Measurement.FrameCount, Is.Zero); // 실제 렌더링 표본 아님
                Assert.That(recorder.BuildReport(), Does.Contain("미측정")); // 미측정 상태 표시
            } // 미측정 확인 종료
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 시험 플레이어 제거
            Object.Destroy(cameraObject); // 시험 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 일시정지 중 설정 변경 감지 시험
    public IEnumerator ChangedSettingsWhilePausedMarkOnlyPriorSamplesAndResetOnRestart() // 조건 변경 보고와 새 출격 초기화
    { // 시험 시작
        GameObject cameraObject = new GameObject("ChangedMeasureCamera"); // 카메라 객체 생성
        GameObject playerObject = new GameObject("ChangedMeasurePlayer"); // 플레이어 객체 생성
        float previous = Time.timeScale; // 기존 시간 배율 보관
        try // 시험 범위 시작
        { // 준비 시작
            Time.timeScale = 0f; // 일시정지 상태 설정
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            camera.orthographicSize = 5f; // 시험 화면 크기
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 상태 생성
            EnemyBulletPool pool = EnemyBulletPlayTestFactory.Create(cameraObject); // 적탄 풀 생성
            StageDirector stage = EnemyActorTestFactory.AttachStage(cameraObject); // 스테이지 생성
            stage.Begin(player, camera, pool, null, null); // 스테이지 연결
            MeasurementContext environment = new MeasurementContext(); // 격리된 표시 환경
            environment.Width = 1920; // 초기 가로 해상도
            environment.Height = 1080; // 초기 세로 해상도
            environment.VerticalSync = true; // 초기 동기화 설정
            environment.FrameLimit = 60; // 초기 FPS 제한
            environment.RenderDeviceAvailable = true; // 시험 장치 사용
            environment.Gpu = "test gpu"; // 시험 장치 이름
            environment.GraphicsApi = "test api"; // 시험 그래픽 API
            BattleMeasurementRecorder recorder = cameraObject.AddComponent<BattleMeasurementRecorder>(); // 측정 수집기
            recorder.Begin(player, stage, pool, 42, () => environment); // 가상 환경 주입
            Assert.That(recorder.Measurement.RecordFrame(0.02d, true), Is.True); // 변경 전 시험 표본
            environment.Width = 1280; // 가상 화면 설정 변경
            yield return null; // 일시정지 중 감지 대기
            Assert.That(recorder.Measurement.EnvironmentChanged, Is.True); // 정지 상태에서도 조건 변경 감지
            Assert.That(recorder.Measurement.FrameCount, Is.EqualTo(1)); // 변경 전 표본만 보존
            Assert.That(recorder.BuildReport(), Does.Contain("변경 전")); // 보고서의 구간 한계 명시
            Assert.That(recorder.BuildReport(), Does.Contain("전체 출격 성능 미측정")); // 전체 성능 미측정 표시
            player.Begin(Vector2.zero); // 새 출격 초기화
            Assert.That(recorder.Measurement.EnvironmentChanged, Is.False); // 새 출격 조건 다시 기록
            Assert.That(recorder.Measurement.FrameCount, Is.Zero); // 새 출격 표본 초기화
            Assert.That(recorder.Measurement.Context.Width, Is.EqualTo(1280)); // 새 출격 환경 반영
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 시험 플레이어 제거
            Object.Destroy(cameraObject); // 시험 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
