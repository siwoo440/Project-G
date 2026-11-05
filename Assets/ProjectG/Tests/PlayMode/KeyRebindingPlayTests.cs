using System.Collections; // 코루틴 시험 기능
using System.Collections.Generic; // 시험 저장소 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 화면 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.InputSystem.LowLevel; // 키보드 상태 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 전투 조작 기능
public class KeyRebindingPlayTests // 실제 전투 키 재지정 시험
{ // 시험 시작
    private sealed class MemoryStore : IKeyBindingStore // 격리된 시험 저장소
    { // 저장소 시작
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(); // 시험 설정 목록
        public string Get(string key, string fallback) // 저장 키 조회
        { // 조회 시작
            return values.TryGetValue(key, out string value) ? value : fallback; // 기록 값 또는 기본값
        } // 조회 종료
        public void Set(string key, string value) // 설정 기록
        { // 기록 시작
            values[key] = value; // 시험 값 보관
        } // 기록 종료
        public void Flush() // 저장 완료
        { // 저장 시작
        } // 저장 종료
    } // 저장소 종료
    [UnityTest] // 실행 중 이동 키 변경 시험
    public IEnumerator RebindingImmediatelyChangesPlayerMovementKey() // 새 키 실제 이동 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        GameObject cameraObject = new GameObject("Camera"); // 카메라 객체
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
        GameObject playerObject = BattleSetup.Create(camera); // 전투 플레이어 생성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 참조
        try // 시험 시작
        { // 시험 범위 시작
            KeyBindingSettings bindings = new KeyBindingSettings(new MemoryStore(), "Test.Keys."); // 격리된 키 설정
            player.SetBindings(bindings); // 플레이어 입력 연결
            Assert.That(bindings.TryBind(PlayerAction.MoveRight, "rightArrow"), Is.True); // 오른쪽 키 변경
            Assert.That(bindings.TryBind(PlayerAction.Unique, "i"), Is.True); // 고유 스킬 키 변경
            Assert.That(player.ControlLabel(PlayerAction.Unique), Is.EqualTo("I")); // 변경된 스킬 키 표시
            yield return null; // 입력 초기화 대기
            float start = playerObject.transform.position.x; // 초기 가로 위치
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); // 이전 오른쪽 키 입력
            yield return null; // 입력 처리 대기
            yield return new WaitForSeconds(0.05f); // 이동 처리 대기
            Assert.That(playerObject.transform.position.x, Is.EqualTo(start).Within(0.001f)); // 이전 키 이동 차단
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이전 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 새 오른쪽 키 입력
            yield return null; // 입력 처리 대기
            yield return new WaitForSeconds(0.05f); // 이동 처리 대기
            Assert.That(playerObject.transform.position.x, Is.GreaterThan(start)); // 새 키 이동 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 설정 복원
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
