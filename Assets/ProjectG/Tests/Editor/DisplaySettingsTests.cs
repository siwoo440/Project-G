using System.Collections.Generic; // 가상 설정 저장소
using NUnit.Framework; // 테스트 도구
using ProjectG; // 화면 설정 기능
public class DisplaySettingsTests // 화면 설정 시험
{ // 시험 시작
    private sealed class MemoryStore : IDisplaySettingsStore // 격리된 시험 저장소
    { // 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 설정 값 목록
        public int Get(string key, int fallback) // 설정 값 조회
        { // 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값
        } // 조회 종료
        public void Set(string key, int value) // 설정 값 저장
        { // 저장 시작
            values[key] = value; // 시험 값 보관
        } // 저장 종료
        public void Flush() // 저장 완료 처리
        { // 처리 시작
        } // 처리 종료
    } // 저장소 종료
    private sealed class FakeDisplay : IDisplayBackend // 실제 모니터를 건드리지 않는 시험 장치
    { // 장치 시작
        public IReadOnlyList<DisplaySize> Resolutions // 지원 해상도 목록
        { // 속성 시작
            get; // 목록 조회
            set; // 시험 목록 설정
        } // 속성 종료
        public DisplaySize CurrentResolution // 현재 해상도
        { // 속성 시작
            get; // 해상도 조회
            set; // 시험 해상도 설정
        } // 속성 종료
        public int ApplyCount; // 실제 적용 호출 수
        public DisplaySize AppliedResolution; // 적용 요청 해상도
        public DisplayWindowMode AppliedMode; // 적용 요청 화면 모드
        public int AppliedFps; // 적용 요청 프레임 제한
        public bool AppliedVSync; // 적용 요청 수직 동기화
        public bool Apply(DisplaySize resolution, DisplayWindowMode mode, int fps, bool vsync) // 설정 적용 요청
        { // 적용 시작
            ApplyCount++; // 호출 횟수 증가
            AppliedResolution = resolution; // 해상도 보관
            AppliedMode = mode; // 화면 모드 보관
            AppliedFps = fps; // 프레임 제한 보관
            AppliedVSync = vsync; // 수직 동기화 보관
            return true; // 시험 적용 성공
        } // 적용 종료
    } // 장치 종료
    private static FakeDisplay MakeDisplay() // 표준 시험 모니터 생성
    { // 생성 시작
        FakeDisplay display = new FakeDisplay(); // 가상 모니터
        display.Resolutions = new DisplaySize[] // 시험 지원 해상도
        { // 목록 시작
            new DisplaySize(1280, 720), // 작은 해상도
            new DisplaySize(1920, 1080) // 현재 해상도
        }; // 목록 종료
        display.CurrentResolution = new DisplaySize(1920, 1080); // 현재 해상도 설정
        return display; // 가상 모니터 반환
    } // 생성 종료
    [Test] // 초안 기본값과 적용 분리 시험
    public void DefaultsMatchDraftAndDoNotChangeDisplay() // 기본값 확인
    { // 시험 시작
        FakeDisplay display = MakeDisplay(); // 가상 모니터
        DisplaySettings settings = new DisplaySettings(display, new MemoryStore()); // 화면 설정 생성
        Assert.That(settings.Mode, Is.EqualTo(DisplayWindowMode.Borderless)); // 전체 창 기본값
        Assert.That(settings.Resolution, Is.EqualTo(new DisplaySize(1920, 1080))); // 현재 해상도 기본값
        Assert.That(settings.FpsLimit, Is.EqualTo(60)); // 초안 60 프레임
        Assert.That(settings.VSync, Is.True); // 초안 수직 동기화
        Assert.That(display.ApplyCount, Is.Zero); // 생성 중 실제 적용 방지
    } // 시험 종료
    [Test] // 유효하지 않은 설정 거부 시험
    public void RejectsUnsupportedResolutionModeAndFrameLimit() // 설정 안전 검사
    { // 시험 시작
        FakeDisplay display = MakeDisplay(); // 가상 모니터
        DisplaySettings settings = new DisplaySettings(display, new MemoryStore()); // 화면 설정 생성
        Assert.That(settings.TrySetResolution(800, 600), Is.False); // 미지원 해상도 거부
        Assert.That(settings.TrySetResolution(0, 1080), Is.False); // 잘못된 해상도 거부
        Assert.That(settings.TrySetMode((DisplayWindowMode)99), Is.False); // 잘못된 모드 거부
        Assert.That(settings.TrySetFpsLimit(75), Is.False); // 미지원 프레임 제한 거부
        Assert.That(settings.Resolution, Is.EqualTo(new DisplaySize(1920, 1080))); // 기존 해상도 유지
        Assert.That(display.ApplyCount, Is.Zero); // 자동 적용 방지
    } // 시험 종료
    [Test] // 명시적 적용과 저장 시험
    public void AppliesAndRestoresSavedValuesThroughInjectedServices() // 설정 저장과 복원
    { // 시험 시작
        FakeDisplay display = MakeDisplay(); // 가상 모니터
        MemoryStore store = new MemoryStore(); // 격리된 저장소
        store.Set("other.project.value", 123); // 무관한 기존 값
        DisplaySettings settings = new DisplaySettings(display, store); // 화면 설정 생성
        Assert.That(settings.TrySetMode(DisplayWindowMode.Windowed), Is.True); // 창 모드 선택
        Assert.That(settings.TrySetResolution(1280, 720), Is.True); // 지원 해상도 선택
        Assert.That(settings.TrySetFpsLimit(0), Is.True); // 무제한 프레임 선택
        settings.SetVSync(false); // 수직 동기화 끄기
        Assert.That(display.ApplyCount, Is.Zero); // 명시적 적용 전 대기
        Assert.That(settings.Apply(), Is.True); // 시험 장치에만 적용
        Assert.That(display.AppliedMode, Is.EqualTo(DisplayWindowMode.Windowed)); // 창 모드 전달
        Assert.That(display.AppliedResolution, Is.EqualTo(new DisplaySize(1280, 720))); // 해상도 전달
        Assert.That(display.AppliedFps, Is.Zero); // 무제한 전달
        Assert.That(display.AppliedVSync, Is.False); // 수직 동기화 전달
        Assert.That(settings.Save(), Is.True); // 개별 항목 저장
        DisplaySettings loaded = new DisplaySettings(display, store); // 새 설정 생성
        loaded.Load(); // 저장 설정 복원
        Assert.That(loaded.Mode, Is.EqualTo(DisplayWindowMode.Windowed)); // 화면 모드 복원
        Assert.That(loaded.Resolution, Is.EqualTo(new DisplaySize(1280, 720))); // 해상도 복원
        Assert.That(loaded.FpsLimit, Is.Zero); // 프레임 제한 복원
        Assert.That(loaded.VSync, Is.False); // 수직 동기화 복원
        Assert.That(store.Get("other.project.value", 0), Is.EqualTo(123)); // 무관한 기존 값 유지
        Assert.That(display.ApplyCount, Is.EqualTo(1)); // 복원 중 실제 적용 방지
    } // 시험 종료
    [Test] // 손상된 설정과 디스플레이 부재 시험
    public void InvalidSavedValuesRestoreDefaultsAndNoMonitorCannotApply() // 안전 복구 확인
    { // 시험 시작
        FakeDisplay display = MakeDisplay(); // 가상 모니터
        MemoryStore store = new MemoryStore(); // 격리된 저장소
        store.Set("ProjectG.Display.Mode", 999); // 잘못된 화면 모드
        store.Set("ProjectG.Display.Width", 13); // 미지원 가로 해상도
        store.Set("ProjectG.Display.Height", 13); // 미지원 세로 해상도
        store.Set("ProjectG.Display.Fps", 500); // 잘못된 프레임 제한
        store.Set("ProjectG.Display.VSync", -1); // 잘못된 수직 동기화
        DisplaySettings settings = new DisplaySettings(display, store); // 화면 설정 생성
        settings.Load(); // 손상된 값 복원
        Assert.That(settings.Mode, Is.EqualTo(DisplayWindowMode.Borderless)); // 기본 화면 모드
        Assert.That(settings.Resolution, Is.EqualTo(new DisplaySize(1920, 1080))); // 현재 해상도 복원
        Assert.That(settings.FpsLimit, Is.EqualTo(60)); // 기본 프레임 제한
        Assert.That(settings.VSync, Is.True); // 기본 수직 동기화
        FakeDisplay absent = new FakeDisplay(); // 없는 모니터 시험
        absent.Resolutions = new DisplaySize[0]; // 빈 해상도 목록
        absent.CurrentResolution = new DisplaySize(0, 0); // 없는 현재 해상도
        MemoryStore existing = new MemoryStore(); // 기존 설정 저장소
        existing.Set("ProjectG.Display.Width", 1920); // 기존 가로 해상도
        DisplaySettings missing = new DisplaySettings(absent, existing); // 화면 없는 설정
        Assert.That(missing.Apply(), Is.False); // 잘못된 화면 적용 차단
        Assert.That(missing.Save(), Is.False); // 화면 부재 시 저장 차단
        Assert.That(existing.Get("ProjectG.Display.Width", 0), Is.EqualTo(1920)); // 기존 해상도 보존
        Assert.That(absent.ApplyCount, Is.Zero); // 장치 적용 미호출
    } // 시험 종료
} // 시험 종료
