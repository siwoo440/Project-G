using System; // 설정 변경 알림과 예외 처리
using System.Collections.Generic; // 해상도 목록
using UnityEngine; // 유니티 화면과 설정 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum DisplayWindowMode // PC 화면 모드
    { // 목록 시작
        ExclusiveFullscreen, Borderless, Windowed // 전체 화면과 전체 창 및 창 모드
    } // 목록 종료
    public struct DisplaySize : IEquatable<DisplaySize> // 해상도 크기
    { // 구조체 시작
        public int Width // 가로 화소 수
        { // 속성 시작
            get; // 가로 크기 조회
        } // 속성 종료
        public int Height // 세로 화소 수
        { // 속성 시작
            get; // 세로 크기 조회
        } // 속성 종료
        public DisplaySize(int width, int height) // 해상도 생성
        { // 생성 시작
            Width = width; // 가로 크기 보관
            Height = height; // 세로 크기 보관
        } // 생성 종료
        public bool Equals(DisplaySize other) // 해상도 비교
        { // 비교 시작
            return Width == other.Width && Height == other.Height; // 가로세로 일치 확인
        } // 비교 종료
        public override bool Equals(object other) // 객체 해상도 비교
        { // 비교 시작
            return other is DisplaySize size && Equals(size); // 같은 크기 확인
        } // 비교 종료
        public override int GetHashCode() // 해상도 해시값
        { // 계산 시작
            return (Width * 397) ^ Height; // 가로세로 해시 조합
        } // 계산 종료
    } // 구조체 종료
    public interface IDisplayBackend // 실제 화면 장치 분리
    { // 인터페이스 시작
        IReadOnlyList<DisplaySize> Resolutions // 지원 해상도 목록
        { // 속성 시작
            get; // 목록 조회
        } // 속성 종료
        DisplaySize CurrentResolution // 현재 모니터 해상도
        { // 속성 시작
            get; // 해상도 조회
        } // 속성 종료
        bool Apply(DisplaySize resolution, DisplayWindowMode mode, int fps, bool vsync); // 표시 설정 적용 요청
    } // 인터페이스 종료
    public interface IDisplaySettingsStore // 화면 설정 저장소
    { // 인터페이스 시작
        int Get(string key, int fallback); // 저장 값 조회
        void Set(string key, int value); // 설정 값 저장
        void Flush(); // 저장 완료 처리
    } // 인터페이스 종료
    public sealed class PlayerPrefsDisplayStore : IDisplaySettingsStore // 유니티 화면 설정 저장소
    { // 저장소 시작
        public int Get(string key, int fallback) // 저장 값 조회
        { // 조회 시작
            return PlayerPrefs.GetInt(key, fallback); // 저장 값 또는 기본값
        } // 조회 종료
        public void Set(string key, int value) // 설정 값 저장
        { // 저장 시작
            PlayerPrefs.SetInt(key, value); // 지정 항목만 갱신
        } // 저장 종료
        public void Flush() // 저장 완료
        { // 저장 시작
            PlayerPrefs.Save(); // 변경 항목 반영
        } // 저장 종료
    } // 저장소 종료
    public sealed class UnityDisplayBackend : IDisplayBackend // 유니티 실제 화면 장치
    { // 장치 시작
        public DisplaySize CurrentResolution // 현재 모니터 해상도
        { // 속성 시작
            get // 화면 정보 조회
            { // 조회 시작
                Resolution current = Screen.currentResolution; // 현재 모니터 해상도
                return new DisplaySize(current.width, current.height); // 화면 크기 반환
            } // 조회 종료
        } // 속성 종료
        public IReadOnlyList<DisplaySize> Resolutions // 현재 지원 해상도
        { // 속성 시작
            get // 목록 조회
            { // 조회 시작
                List<DisplaySize> sizes = new List<DisplaySize>(); // 중복 없는 해상도 목록
                foreach (Resolution resolution in Screen.resolutions) // 시스템 해상도 순회
                { // 순회 시작
                    DisplaySize size = new DisplaySize(resolution.width, resolution.height); // 해상도 크기 변환
                    if (size.Width > 0 && size.Height > 0 && !sizes.Contains(size)) // 유효한 새 해상도 확인
                    { // 추가 시작
                        sizes.Add(size); // 지원 목록 추가
                    } // 추가 종료
                } // 순회 종료
                DisplaySize current = CurrentResolution; // 현재 모니터 크기
                if (current.Width > 0 && current.Height > 0 && !sizes.Contains(current)) // 현재 화면 누락 확인
                { // 추가 시작
                    sizes.Add(current); // 현재 화면도 선택 가능
                } // 추가 종료
                return sizes.AsReadOnly(); // 외부 수정 방지 목록
            } // 조회 종료
        } // 속성 종료
        public bool Apply(DisplaySize resolution, DisplayWindowMode mode, int fps, bool vsync) // 실제 표시 장치 적용 요청
        { // 요청 시작
            try // 화면 설정 시도
            { // 시도 시작
                FullScreenMode screenMode = mode == DisplayWindowMode.ExclusiveFullscreen ? FullScreenMode.ExclusiveFullScreen : mode == DisplayWindowMode.Borderless ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; // 유니티 화면 모드 변환
                Screen.SetResolution(resolution.Width, resolution.Height, screenMode); // 해상도와 화면 모드 요청
                QualitySettings.vSyncCount = vsync ? 1 : 0; // 수직 동기화 요청
                Application.targetFrameRate = fps == 0 ? -1 : fps; // 프레임 제한 요청
                return true; // 설정 요청 성공
            } // 시도 종료
            catch (Exception) // 시스템 요청 실패
            { // 실패 시작
                return false; // 실패 상태 반환
            } // 실패 종료
        } // 요청 종료
    } // 장치 종료
    public sealed class DisplaySettings // 설정 값과 명시적 적용
    { // 클래스 시작
        private const string Prefix = "ProjectG.Display."; // 프로젝트별 저장 접두사
        private readonly IDisplayBackend backend; // 주입된 화면 장치
        private readonly IDisplaySettingsStore store; // 주입된 설정 저장소
        private readonly List<DisplaySize> resolutions = new List<DisplaySize>(); // 유효 해상도 목록
        private readonly IReadOnlyList<DisplaySize> resolutionView; // 외부 수정 방지 목록
        private readonly DisplaySize defaultResolution; // 초안 기본 해상도
        public event Action Changed; // 실제 설정 변경 알림
        public DisplayWindowMode Mode // 선택한 화면 모드
        { // 속성 시작
            get; // 화면 모드 조회
            private set; // 화면 모드 보관
        } // 속성 종료
        public DisplaySize Resolution // 선택한 해상도
        { // 속성 시작
            get; // 해상도 조회
            private set; // 해상도 보관
        } // 속성 종료
        public int FpsLimit // 초당 프레임 제한
        { // 속성 시작
            get; // 프레임 제한 조회
            private set; // 프레임 제한 보관
        } // 속성 종료
        public bool VSync // 수직 동기화 여부
        { // 속성 시작
            get; // 수직 동기화 조회
            private set; // 수직 동기화 보관
        } // 속성 종료
        public IReadOnlyList<DisplaySize> Resolutions => resolutionView; // 읽기 전용 지원 해상도
        public DisplaySettings() : this(new UnityDisplayBackend(), new PlayerPrefsDisplayStore()) // 실제 장치와 저장소 연결
        { // 생성 시작
        } // 생성 종료
        public DisplaySettings(IDisplayBackend displayBackend, IDisplaySettingsStore settingsStore) // 시험 가능한 설정 생성
        { // 생성 시작
            backend = displayBackend ?? throw new ArgumentNullException(nameof(displayBackend)); // 화면 장치 확인
            store = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore)); // 저장소 확인
            IReadOnlyList<DisplaySize> supported = backend.Resolutions; // 지원 해상도 목록 조회
            if (supported != null) // 지원 해상도 목록 확인
            { // 목록 시작
                foreach (DisplaySize size in supported) // 해상도 순회
                { // 순회 시작
                    if (size.Width > 0 && size.Height > 0 && !resolutions.Contains(size)) // 유효한 새 크기 확인
                    { // 추가 시작
                        resolutions.Add(size); // 선택 목록 추가
                    } // 추가 종료
                } // 순회 종료
            } // 목록 종료
            resolutionView = resolutions.AsReadOnly(); // 외부 수정 방지 목록 준비
            DisplaySize current = backend.CurrentResolution; // 현재 모니터 해상도
            defaultResolution = resolutions.Contains(current) ? current : resolutions.Count > 0 ? resolutions[0] : new DisplaySize(0, 0); // 안전한 초안 기본값
            Mode = DisplayWindowMode.Borderless; // 전체 창 초안 기본값
            Resolution = defaultResolution; // 현재 모니터 기본값
            FpsLimit = 60; // 60 프레임 초안 기본값
            VSync = true; // 수직 동기화 초안 기본값
        } // 생성 종료
        public bool TrySetMode(DisplayWindowMode mode) // 화면 모드 선택
        { // 설정 시작
            if (!ValidMode(mode)) // 유효한 모드 확인
            { // 차단 시작
                return false; // 잘못된 모드 거부
            } // 차단 종료
            ApplyValues(mode, Resolution, FpsLimit, VSync); // 선택 값 보관
            return true; // 유효한 선택
        } // 설정 종료
        public bool TrySetResolution(int width, int height) // 지원 해상도 선택
        { // 설정 시작
            DisplaySize size = new DisplaySize(width, height); // 요청 크기 생성
            if (!resolutions.Contains(size)) // 지원 여부 확인
            { // 차단 시작
                return false; // 미지원 해상도 거부
            } // 차단 종료
            ApplyValues(Mode, size, FpsLimit, VSync); // 선택 값 보관
            return true; // 유효한 선택
        } // 설정 종료
        public bool TrySetFpsLimit(int fps) // 프레임 제한 선택
        { // 설정 시작
            if (!ValidFps(fps)) // 유효한 제한 확인
            { // 차단 시작
                return false; // 잘못된 제한 거부
            } // 차단 종료
            ApplyValues(Mode, Resolution, fps, VSync); // 선택 값 보관
            return true; // 유효한 선택
        } // 설정 종료
        public void SetVSync(bool enabled) // 수직 동기화 선택
        { // 설정 시작
            ApplyValues(Mode, Resolution, FpsLimit, enabled); // 선택 값 보관
        } // 설정 종료
        public bool Apply() // 실제 표시 장치 적용 요청
        { // 적용 시작
            if (!resolutions.Contains(Resolution) || !ValidMode(Mode) || !ValidFps(FpsLimit)) // 안전한 설정 확인
            { // 차단 시작
                return false; // 잘못된 요청 방지
            } // 차단 종료
            try // 화면 적용 요청
            { // 요청 시작
                return backend.Apply(Resolution, Mode, FpsLimit, VSync); // 주입된 장치에 명시적 요청
            } // 요청 종료
            catch (Exception) // 화면 장치 오류
            { // 실패 시작
                return false; // 실패 상태 반환
            } // 실패 종료
        } // 적용 종료
        public bool Save() // 각 화면 설정 저장
        { // 저장 시작
            if (!resolutions.Contains(Resolution)) // 저장할 화면 확인
            { // 차단 시작
                return false; // 잘못된 해상도로 기존 설정 덮어쓰기 방지
            } // 차단 종료
            store.Set(Prefix + "Mode", (int)Mode); // 화면 모드 저장
            store.Set(Prefix + "Width", Resolution.Width); // 가로 해상도 저장
            store.Set(Prefix + "Height", Resolution.Height); // 세로 해상도 저장
            store.Set(Prefix + "Fps", FpsLimit); // 프레임 제한 저장
            store.Set(Prefix + "VSync", VSync ? 1 : 0); // 수직 동기화 저장
            store.Flush(); // 설정 저장 완료
            return true; // 저장 성공
        } // 저장 종료
        public void Load() // 저장된 설정 복원
        { // 복원 시작
            DisplayWindowMode mode = (DisplayWindowMode)store.Get(Prefix + "Mode", (int)DisplayWindowMode.Borderless); // 저장된 화면 모드
            int width = store.Get(Prefix + "Width", defaultResolution.Width); // 저장된 가로 크기
            int height = store.Get(Prefix + "Height", defaultResolution.Height); // 저장된 세로 크기
            DisplaySize size = new DisplaySize(width, height); // 저장된 해상도
            int fps = store.Get(Prefix + "Fps", 60); // 저장된 프레임 제한
            int sync = store.Get(Prefix + "VSync", 1); // 저장된 동기화 값
            ApplyValues(ValidMode(mode) ? mode : DisplayWindowMode.Borderless, resolutions.Contains(size) ? size : defaultResolution, ValidFps(fps) ? fps : 60, sync == 0 ? false : true); // 유효하지 않은 값 복구
        } // 복원 종료
        public void ResetDefaults() // 초안 기본 설정 복원
        { // 복원 시작
            ApplyValues(DisplayWindowMode.Borderless, defaultResolution, 60, true); // 기본 설정 보관
        } // 복원 종료
        private void ApplyValues(DisplayWindowMode mode, DisplaySize size, int fps, bool vsync) // 설정 값만 갱신
        { // 갱신 시작
            bool changed = Mode != mode || !Resolution.Equals(size) || FpsLimit != fps || VSync != vsync; // 실제 값 변경 확인
            Mode = mode; // 화면 모드 보관
            Resolution = size; // 해상도 보관
            FpsLimit = fps; // 프레임 제한 보관
            VSync = vsync; // 수직 동기화 보관
            if (changed) // 값 변경 확인
            { // 알림 시작
                Changed?.Invoke(); // 화면 갱신 알림
            } // 알림 종료
        } // 갱신 종료
        private static bool ValidMode(DisplayWindowMode mode) // 화면 모드 검사
        { // 검사 시작
            return mode == DisplayWindowMode.ExclusiveFullscreen || mode == DisplayWindowMode.Borderless || mode == DisplayWindowMode.Windowed; // 지원 모드 확인
        } // 검사 종료
        private static bool ValidFps(int fps) // 프레임 제한 검사
        { // 검사 시작
            return fps == 0 || fps == 60 || fps == 120 || fps == 144; // 지원 제한 확인
        } // 검사 종료
    } // 클래스 종료
} // 영역 종료
