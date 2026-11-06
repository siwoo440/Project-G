using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 가상 키보드 기능
using UnityEngine.InputSystem.LowLevel; // 입력 상태 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 전투 정보 기능
public class UpgradeDropInfoPlayTests // 전투 정보 실제 입력 시험
{ // 시험 시작
    [UnityTest] // 탭 표시와 전투 시간 시험
    public IEnumerator HoldingTabShowsInfoWithoutPausingAndHidesDuringPause() // 입력과 정지 상태 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정
        float previousScale = Time.timeScale; // 기존 전투 시간
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 가상 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        GameObject cameraObject = new GameObject("InfoCamera"); // 시험 카메라 생성
        GameObject playerObject = new GameObject("InfoPlayer"); // 시험 플레이어 생성
        try // 시험 범위 시작
        { // 시험 시작
            Time.timeScale = 1f; // 전투 시간 초기화
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            Camera camera = cameraObject.AddComponent<Camera>(); // 시험 카메라 구성
            UpgradeDropManager manager = cameraObject.AddComponent<UpgradeDropManager>(); // 드롭 관리자 구성
            manager.Begin(player, camera, null); // 전투 정보 연결
            yield return null; // 입력 장치 초기화
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); // 탭 누르기
            yield return null; // 입력 처리 대기
            Assert.That(manager.IsInfoVisible, Is.True); // 누르는 동안 정보 표시
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 시간 유지
            Time.timeScale = 0f; // 카드 선택 또는 일시정지
            yield return null; // 상태 반영 대기
            Assert.That(manager.IsInfoVisible, Is.False); // 다른 정지 화면과 충돌 방지
            Time.timeScale = 1f; // 전투 복귀
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 탭 해제
            yield return null; // 해제 처리 대기
            Assert.That(manager.IsInfoVisible, Is.False); // 해제 시 정보 숨김
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(cameraObject); // 시험 카메라 제거
            Object.Destroy(playerObject); // 시험 플레이어 제거
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            Time.timeScale = previousScale; // 기존 전투 시간 복원
            InputSystem.settings.backgroundBehavior = previousBackground; // 포커스 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료