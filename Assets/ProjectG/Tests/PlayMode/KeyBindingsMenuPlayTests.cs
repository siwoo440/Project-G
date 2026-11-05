using System.Collections; // 코루틴 시험 기능
using System.Collections.Generic; // 시험 키 저장소
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 가상 키보드 기능
using UnityEngine.InputSystem.LowLevel; // 키보드 입력 상태
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 조작 설정 기능
public class KeyBindingsMenuPlayTests // 설정 화면 키 변경 시험
{ // 시험 시작
    private sealed class MemoryKeyStore : IKeyBindingStore // 메모리 키 저장소
    { // 저장소 시작
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(); // 시험 키 목록
        public string Get(string key, string fallback) // 저장 키 조회
        { // 조회 시작
            return values.TryGetValue(key, out string value) ? value : fallback; // 저장 키 또는 기본값
        } // 조회 종료
        public void Set(string key, string value) // 시험 키 기록
        { // 기록 시작
            values[key] = value; // 키 값 보관
        } // 기록 종료
        public void Flush() // 시험 저장 완료
        { // 저장 시작
        } // 저장 종료
    } // 저장소 종료
    private static IEnumerator Press(Keyboard keyboard, Key key) // 키 입력과 해제
    { // 입력 시작
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 키 누르기
        yield return null; // 입력 처리 대기
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
        yield return null; // 해제 처리 대기
    } // 입력 종료
    [UnityTest] // 키 변경 화면 시험
    public IEnumerator RebindingRejectsDuplicatesAndReservedKeys() // 충돌 방지와 재입력 확인
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
            PauseSettingsOverlay overlay = holder.AddComponent<PauseSettingsOverlay>(); // 설정 화면 구성
            overlay.Begin(null, null, null, null); // 메뉴 설정 연결
            KeyBindingSettings bindings = new KeyBindingSettings(new MemoryKeyStore(), "test."); // 시험 키 설정
            overlay.SetBindings(bindings); // 시험 저장소 연결
            Assert.That(overlay.OpenSettingsFromMenu(), Is.True); // 메뉴 설정 열기
            yield return null; // 화면 열기 프레임 대기
            yield return Press(keyboard, Key.DownArrow); // 일반 표시 항목
            yield return Press(keyboard, Key.DownArrow); // 집중 표시 항목
            yield return Press(keyboard, Key.DownArrow); // 키 설정 항목
            yield return Press(keyboard, Key.Enter); // 키 설정 화면 진입
            yield return Press(keyboard, Key.Enter); // 위 이동 키 변경 시작
            yield return Press(keyboard, Key.I); // 새 키 지정
            Assert.That(bindings.GetControl(PlayerAction.MoveUp), Is.EqualTo("i")); // 새 키 적용 확인
            yield return Press(keyboard, Key.Enter); // 같은 동작 재설정 시작
            yield return Press(keyboard, Key.A); // 다른 동작과 중복 입력
            Assert.That(bindings.GetControl(PlayerAction.MoveUp), Is.EqualTo("i")); // 중복 키 거부
            yield return Press(keyboard, Key.F1); // 예약된 시험 선택 키
            Assert.That(bindings.GetControl(PlayerAction.MoveUp), Is.EqualTo("i")); // 예약 키 거부
            yield return Press(keyboard, Key.O); // 충돌 없는 키 재입력
            Assert.That(bindings.GetControl(PlayerAction.MoveUp), Is.EqualTo("o")); // 재입력 성공
            overlay.Resume(); // 설정 화면 닫기
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 시간 배율 복원
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
