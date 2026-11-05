using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 가상 키보드 기능
using UnityEngine.InputSystem.LowLevel; // 키보드 입력 상태
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 전투 화면 기능
public class PauseSettingsPlayTests // 일시정지 실제 입력 시험
{ // 시험 시작
    [UnityTest] // 정지와 재개 키 시험
    public IEnumerator EscapePausesResumesAndLeavesCardSelectionAlone() // 실제 키 입력과 선택 충돌 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 비대화형 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 포커스 없는 시험 입력
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        GameObject cameraObject = new GameObject("PauseCamera"); // 카메라 객체
        GameObject playerObject = new GameObject("PausePlayer"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 시간 설정
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 연결
            PauseSettingsOverlay overlay = cameraObject.AddComponent<PauseSettingsOverlay>(); // 일시정지 구성
            overlay.Begin(player, camera, progress, null); // 전투 연결
            yield return null; // 입력 초기화 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 일시정지 입력
            yield return null; // 입력 처리 대기
            Assert.That(overlay.IsPaused, Is.True); // 정지 화면 표시
            Assert.That(Time.timeScale, Is.Zero); // 전투 시간 정지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 입력 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 재개 입력
            yield return null; // 입력 처리 대기
            Assert.That(overlay.IsPaused, Is.False); // 정지 화면 닫기
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 시간 복원
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 입력 해제
            yield return null; // 해제 처리 대기
            progress.CollectExperience(10); // 카드 화면 열기
            Assert.That(progress.Offer, Is.Not.Null); // 카드 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 카드 화면 뒤로 입력
            yield return null; // 입력 처리 대기
            Assert.That(overlay.IsPaused, Is.False); // 정지 화면 중복 방지
            Assert.That(progress.Offer, Is.Not.Null); // 카드 선택 유지
            progress.Shutdown(); // 카드 상태 정리
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 기존 백그라운드 입력 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 기존 편집기 입력 복원
            Time.timeScale = previousScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
