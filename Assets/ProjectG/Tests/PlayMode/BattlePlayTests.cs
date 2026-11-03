using System.Collections; // 코루틴 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 입력 시스템
using UnityEngine.InputSystem.LowLevel; // 가상 키보드 상태
using UnityEngine.TestTools; // 플레이 테스트 도구
using UnityEngine.SceneManagement; // 장면 전환 기능
using ProjectG; // 전투 화면 기능
public class BattlePlayTests // 실제 전투 흐름 시험
{ // 시험 시작
    [UnityTest] // 플레이 모드 시험
    public IEnumerator KeyboardMovesPlayerFocusesAndFires() // 키 입력과 자동 사격 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 비대화형 게임 입력 허용
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 설정
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 시험 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 추가
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 참조
        player.AssignCamera(camera); // 시험 카메라 연결
        try // 시험 시작
        { // 시험 범위 시작
            yield return null; // 시작 프레임 대기
            float start = playerObject.transform.position.x; // 시작 위치 기록
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.LeftShift)); // 오른쪽과 집중 입력
            InputSystem.Update(); // 가상 입력 처리
            Assert.That(keyboard.dKey.isPressed, Is.True); // 입력 장치 상태 확인
            yield return null; // 입력 처리 대기
            yield return new WaitForSeconds(0.1f); // 이동과 사격 대기
            Assert.That(playerObject.transform.position.x, Is.GreaterThan(start)); // 실제 이동 확인
            Assert.That(player.IsFocused, Is.True); // 실제 집중 확인
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.GreaterThan(0)); // 실제 탄환 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 모든 키 해제
            yield return null; // 해제 처리 대기
            Assert.That(player.IsFocused, Is.False); // 홀드 해제 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 생성 탄환 순회
            { // 탄환 정리 시작
                Object.Destroy(shot.gameObject); // 시험 탄환 제거
            } // 탄환 정리 종료
            Object.Destroy(playerObject); // 시험 플레이어 제거
            Object.Destroy(cameraObject); // 시험 카메라 제거
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 장면 자동 구성 시험
    public IEnumerator SampleSceneCreatesPlayerOnLoad() // 기본 장면 연결 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        yield return null; // 장면 초기화 대기
        Assert.That(Object.FindFirstObjectByType<BattlePlayer>(), Is.Not.Null); // 플레이어 자동 생성 확인
    } // 시험 종료
} // 시험 종료
