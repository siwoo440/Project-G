using System.Collections; // 코루틴 시험 기능
using System.Collections.Generic; // 시험 화면과 저장소 목록
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 가상 키보드 기능
using UnityEngine.InputSystem.LowLevel; // 키보드 입력 상태
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 화면 설정 기능
public class DisplaySettingsMenuPlayTests // 화면 설정 UI 시험
{ // 시험 시작
    private sealed class FakeDisplayBackend : IDisplayBackend // 모의 표시 장치
    { // 장치 시작
        private readonly List<DisplaySize> sizes = new List<DisplaySize>() // 시험 해상도 목록
        { // 목록 시작
            new DisplaySize(1920, 1080), // 첫 해상도
            new DisplaySize(1280, 720) // 둘째 해상도
        }; // 목록 종료
        public bool Allow; // 적용 성공 여부
        public int Calls; // 장치 요청 횟수
        public IReadOnlyList<DisplaySize> Resolutions => sizes; // 지원 해상도 조회
        public DisplaySize CurrentResolution => sizes[0]; // 초기 해상도 조회
        public bool Apply(DisplaySize resolution, DisplayWindowMode mode, int fps, bool vsync) // 모의 장치 적용
        { // 적용 시작
            Calls++; // 요청 횟수 증가
            return Allow; // 지정한 성공 여부 반환
        } // 적용 종료
    } // 장치 종료
    private sealed class MemoryDisplayStore : IDisplaySettingsStore // 메모리 설정 저장소
    { // 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 시험 설정 목록
        public int Flushes; // 저장 횟수
        public int Get(string key, int fallback) // 설정 조회
        { // 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값
        } // 조회 종료
        public void Set(string key, int value) // 설정 기록
        { // 기록 시작
            values[key] = value; // 시험 설정 보관
        } // 기록 종료
        public void Flush() // 시험 저장 완료
        { // 저장 시작
            Flushes++; // 저장 횟수 증가
        } // 저장 종료
    } // 저장소 종료
    private static IEnumerator Press(Keyboard keyboard, Key key) // 키 입력과 해제
    { // 입력 시작
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 키 누르기
        yield return null; // 입력 처리 대기
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
        yield return null; // 해제 처리 대기
    } // 입력 종료
    [UnityTest] // 모의 화면 설정 시험
    public IEnumerator DisplayChangesAreSavedAndFailuresRestorePreviousValues() // 화면 설정 성공과 실패 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 비대화형 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 포커스 없는 시험 입력
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        GameObject holder = new GameObject("Pause"); // 설정 화면 객체
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 기존 시간 설정
            FakeDisplayBackend backend = new FakeDisplayBackend(); // 모의 표시 장치
            MemoryDisplayStore store = new MemoryDisplayStore(); // 메모리 설정 저장소
            DisplaySettings settings = new DisplaySettings(backend, store); // 시험 화면 설정
            settings.Load(); // 기본 화면 설정 읽기
            PauseSettingsOverlay overlay = holder.AddComponent<PauseSettingsOverlay>(); // 설정 화면 구성
            overlay.SetDisplaySettings(settings); // 모의 표시 장치 주입
            overlay.Begin(null, null, null, null); // 메뉴 설정 연결
            Assert.That(overlay.OpenSettingsFromMenu(), Is.True); // 메뉴 설정 열기
            yield return null; // 화면 열기 프레임 대기
            yield return Press(keyboard, Key.DownArrow); // 일반 표시 항목
            yield return Press(keyboard, Key.DownArrow); // 집중 표시 항목
            yield return Press(keyboard, Key.DownArrow); // 키 설정 항목
            yield return Press(keyboard, Key.DownArrow); // 화면 설정 항목
            yield return Press(keyboard, Key.Enter); // 화면 설정 진입
            yield return Press(keyboard, Key.Enter); // 모의 장치 적용 실패
            Assert.That(settings.Mode, Is.EqualTo(DisplayWindowMode.Borderless)); // 기존 모드 복원
            Assert.That(store.Flushes, Is.Zero); // 실패한 값 저장 방지
            backend.Allow = true; // 모의 장치 성공 설정
            yield return Press(keyboard, Key.Enter); // 화면 모드 변경
            Assert.That(settings.Mode, Is.EqualTo(DisplayWindowMode.Windowed)); // 새 모드 반영
            yield return Press(keyboard, Key.DownArrow); // 해상도 항목
            yield return Press(keyboard, Key.Enter); // 다음 해상도 선택
            Assert.That(settings.Resolution, Is.EqualTo(new DisplaySize(1280, 720))); // 해상도 변경
            yield return Press(keyboard, Key.DownArrow); // 프레임 제한 항목
            yield return Press(keyboard, Key.Enter); // 다음 프레임 제한
            Assert.That(settings.FpsLimit, Is.EqualTo(120)); // 프레임 변경
            yield return Press(keyboard, Key.DownArrow); // 수직 동기화 항목
            yield return Press(keyboard, Key.Enter); // 동기화 전환
            Assert.That(settings.VSync, Is.False); // 동기화 변경
            Assert.That(store.Flushes, Is.EqualTo(4)); // 성공한 설정만 저장
            overlay.Resume(); // 설정 화면 닫기
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(holder); // 설정 화면 제거
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 백그라운드 입력 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 복원
            Time.timeScale = previousScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
