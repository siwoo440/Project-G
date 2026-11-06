using System; // 유효 숫자 시험
using NUnit.Framework; // 시험 도구
using ProjectG; // 플레이 측정 기능
public class PlayMeasurementTests // 플레이 측정 모델 시험
{ // 시험 시작
    [Test] // 비어 있는 측정 시험
    public void EmptyMeasurementsHaveNoFpsOrFrameTime() // 미측정 상태 확인
    { // 시험 시작
        PlayMeasurement measurement = new PlayMeasurement(new MeasurementContext()); // 빈 측정 상태
        Assert.That(measurement.FrameCount, Is.Zero); // 유효 프레임 수
        Assert.That(measurement.AverageFps, Is.Null); // 평균 프레임 미측정
        Assert.That(measurement.MinimumFps, Is.Null); // 최저 프레임 미측정
        Assert.That(measurement.AverageFrameMilliseconds, Is.Null); // 평균 시간 미측정
        Assert.That(measurement.MaximumFrameMilliseconds, Is.Null); // 최장 시간 미측정
    } // 시험 종료
    [Test] // 프레임 비율 산술 시험
    public void FrameMetricsUseTotalTimeAndLongestFrame() // 실제 시간 기준 계산
    { // 시험 시작
        PlayMeasurement measurement = new PlayMeasurement(new MeasurementContext()); // 측정 상태 생성
        Assert.That(measurement.RecordFrame(0.01, true), Is.True); // 첫 프레임 10밀리초
        Assert.That(measurement.RecordFrame(0.02, true), Is.True); // 둘째 프레임 20밀리초
        Assert.That(measurement.RecordFrame(0.04, true), Is.True); // 셋째 프레임 40밀리초
        Assert.That(measurement.FrameCount, Is.EqualTo(3)); // 표본 수 확인
        Assert.That(measurement.FrameSeconds[0], Is.EqualTo(0.01d)); // 첫 초 단위 표본
        Assert.That(measurement.FrameSeconds[1], Is.EqualTo(0.02d)); // 둘째 초 단위 표본
        Assert.That(measurement.FrameSeconds[2], Is.EqualTo(0.04d)); // 셋째 초 단위 표본
        Assert.That(measurement.AverageFps.Value, Is.EqualTo(3d / 0.07d).Within(0.000001d)); // 전체 표본과 경과 시간 비율
        Assert.That(measurement.MinimumFps.Value, Is.EqualTo(25d).Within(0.000001d)); // 최장 프레임의 역수
        Assert.That(measurement.AverageFrameMilliseconds.Value, Is.EqualTo(70d / 3d).Within(0.000001d)); // 평균 밀리초
        Assert.That(measurement.MaximumFrameMilliseconds.Value, Is.EqualTo(40d).Within(0.000001d)); // 최장 밀리초
    } // 시험 종료
    [Test] // 정지와 잘못된 프레임 시험
    public void PausedAndInvalidFrameSamplesAreExcluded() // 유효 표본만 수집
    { // 시험 시작
        PlayMeasurement measurement = new PlayMeasurement(new MeasurementContext()); // 측정 상태 생성
        Assert.That(measurement.RecordFrame(0.5, false), Is.False); // 정지 화면 표본 제외
        Assert.That(measurement.RecordFrame(0d, true), Is.False); // 영 시간 제외
        Assert.That(measurement.RecordFrame(-0.01d, true), Is.False); // 음수 시간 제외
        Assert.That(measurement.RecordFrame(double.NaN, true), Is.False); // 숫자가 아닌 표본 제외
        Assert.That(measurement.RecordFrame(double.PositiveInfinity, true), Is.False); // 무한 시간 제외
        Assert.That(measurement.RecordFrame(0.02d, true), Is.True); // 유효 전투 표본 허용
        Assert.That(measurement.FrameCount, Is.EqualTo(1)); // 제외 이후 유효 표본 수
        Assert.That(measurement.AverageFps.Value, Is.EqualTo(50d).Within(0.000001d)); // 유효 표본만 계산
    } // 시험 종료
    [Test] // 전투 통계의 주입 및 독립 복사 시험
    public void CombatSnapshotsValidateInputAndPreserveStoredValues() // 통계 주입 상태 확인
    { // 시험 시작
        PlayMeasurement measurement = new PlayMeasurement(new MeasurementContext()); // 측정 상태 생성
        CombatMeasurementSnapshot snapshot = new CombatMeasurementSnapshot(); // 전투 스냅샷 생성
        snapshot.CombatSeconds = 30d; // 전투 진행 시간
        snapshot.TotalSeconds = 40d; // 전체 경과 시간
        snapshot.CardsSelected = 3; // 카드 선택 횟수
        snapshot.UltimateActivations = 1; // 필살기 횟수
        snapshot.ItemDrops = 2; // 드롭 횟수
        snapshot.ItemPickups = 1; // 획득 횟수
        snapshot.Hits = 2; // 피격 횟수
        snapshot.Grazes = 4; // 그레이즈 횟수
        snapshot.MidBossDefeats = 1; // 중간 보스 처치
        snapshot.FinalBossDefeats = 0; // 최종 보스 미처치
        snapshot.ActiveEnemyProjectiles = 9; // 활성 적탄 수
        snapshot.PeakEnemyProjectiles = 9; // 관측 최대 적탄 수
        Assert.That(measurement.UpdateCombat(snapshot), Is.True); // 유효 기록 수용
        snapshot.ItemDrops = 99; // 원본 수정 시도
        Assert.That(measurement.Combat.ItemDrops, Is.EqualTo(2)); // 저장된 값 독립성
        CombatMeasurementSnapshot copy = measurement.Combat; // 기록 사본 조회
        copy.Hits = 99; // 사본 수정 시도
        Assert.That(measurement.Combat.Hits, Is.EqualTo(2)); // 반환 사본 독립성
        snapshot.Hits = -1; // 잘못된 피격 횟수
        Assert.That(measurement.UpdateCombat(snapshot), Is.False); // 음수 기록 거부
        Assert.That(measurement.Combat.Hits, Is.EqualTo(2)); // 기존 유효 기록 유지
        snapshot.Hits = 0; // 피격 횟수 복원
        snapshot.PeakEnemyProjectiles = 8; // 현재보다 낮은 최대 수
        Assert.That(measurement.UpdateCombat(snapshot), Is.False); // 잘못된 최대 수 거부
        snapshot.PeakEnemyProjectiles = 9; // 유효 최대 수 복원
        snapshot.CombatSeconds = double.NaN; // 잘못된 시간
        Assert.That(measurement.UpdateCombat(snapshot), Is.False); // 숫자가 아닌 시간 거부
    } // 시험 종료
    [Test] // 시험 환경 주입 시험
    public void ContextSeparatesAutomationAndUnknownHardware() // 환경 근거 보존
    { // 시험 시작
        MeasurementContext context = new MeasurementContext(); // 시험 환경 생성
        context.Source = MeasurementSource.Automation; // 자동 시뮬레이션 표시
        context.RenderDeviceAvailable = false; // 렌더 장치 미사용 표시
        context.Width = 1920; // 시험 가로 해상도
        context.Height = 1080; // 시험 세로 해상도
        context.Cpu = "test cpu"; // 시험 CPU 값
        context.UnityVersion = "test editor"; // 시험 엔진 버전
        PlayMeasurement measurement = new PlayMeasurement(context); // 입력 환경으로 측정 생성
        context.Source = MeasurementSource.HumanPlay; // 원본 분류 수정
        Assert.That(measurement.Context.Source, Is.EqualTo(MeasurementSource.Automation)); // 자동 측정 구분 보존
        Assert.That(measurement.Context.RenderDeviceAvailable, Is.False); // 그래픽 장치 미사용 보존
        Assert.That(measurement.Context.Gpu, Is.Null); // 확인하지 않은 GPU 미정 표시
        Assert.That(measurement.Context.Width, Is.EqualTo(1920)); // 가로 해상도 보존
    } // 시험 종료
    [TestCase(0)] // 가로 해상도 변경
    [TestCase(1)] // 세로 해상도 변경
    [TestCase(2)] // 수직 동기화 변경
    [TestCase(3)] // FPS 제한 변경
    [TestCase(4)] // 그래픽 장치 유무 변경
    [TestCase(5)] // 그래픽 장치 이름 변경
    [TestCase(6)] // 그래픽 API 변경
    public void ChangedRenderConditionsStopAndDoNotResumeSampling(int setting) // 설정 변경 후 표본 차단
    { // 시험 시작
        MeasurementContext context = new MeasurementContext(); // 기준 환경 생성
        context.Width = 1920; // 기준 가로 해상도
        context.Height = 1080; // 기준 세로 해상도
        context.VerticalSync = true; // 기준 수직 동기화
        context.FrameLimit = 60; // 기준 FPS 제한
        context.RenderDeviceAvailable = true; // 기준 장치 사용
        context.Gpu = "test gpu"; // 기준 그래픽 장치
        context.GraphicsApi = "test api"; // 기준 그래픽 API
        PlayMeasurement measurement = new PlayMeasurement(context); // 기준 환경 측정
        Assert.That(measurement.ObserveEnvironment(context), Is.True); // 같은 환경 유지
        Assert.That(measurement.RecordFrame(0.02d, true), Is.True); // 변경 전 표본 기록
        MeasurementContext changed = context.Copy(); // 변경 환경 사본
        if (setting == 0) // 가로 변경 확인
        { // 변경 시작
            changed.Width = 1280; // 다른 가로 해상도
        } // 변경 종료
        else if (setting == 1) // 세로 변경 확인
        { // 변경 시작
            changed.Height = 720; // 다른 세로 해상도
        } // 변경 종료
        else if (setting == 2) // 수직 동기화 변경 확인
        { // 변경 시작
            changed.VerticalSync = false; // 동기화 해제
        } // 변경 종료
        else if (setting == 3) // FPS 제한 변경 확인
        { // 변경 시작
            changed.FrameLimit = 120; // 다른 FPS 제한
        } // 변경 종료
        else if (setting == 4) // 그래픽 장치 유무 변경 확인
        { // 변경 시작
            changed.RenderDeviceAvailable = false; // 그래픽 장치 없음
        } // 변경 종료
        else if (setting == 5) // 그래픽 장치 이름 변경 확인
        { // 변경 시작
            changed.Gpu = "other gpu"; // 다른 그래픽 장치
        } // 변경 종료
        else // 그래픽 API 변경 확인
        { // 변경 시작
            changed.GraphicsApi = "other api"; // 다른 그래픽 API
        } // 변경 종료
        Assert.That(measurement.ObserveEnvironment(changed), Is.False); // 조건 변경 감지
        Assert.That(measurement.EnvironmentChanged, Is.True); // 변경 이력 유지
        Assert.That(measurement.RecordFrame(0.01d, true), Is.False); // 변경 후 표본 제외
        Assert.That(measurement.ObserveEnvironment(context), Is.False); // 원래 설정 복귀에도 중단 유지
        Assert.That(measurement.RecordFrame(0.01d, true), Is.False); // 복귀 후 혼합 표본 방지
        Assert.That(measurement.FrameCount, Is.EqualTo(1)); // 변경 전 표본만 보존
    } // 시험 종료
} // 시험 종료
