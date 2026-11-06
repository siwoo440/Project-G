using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 화면 기능
using UnityEngine; // 카메라와 위치 기능
using UnityEngine.InputSystem; // 시험용 키보드
using UnityEngine.InputSystem.LowLevel; // 가상 키 입력
using UnityEngine.TestTools; // 실제 프레임 시험
public class BattleViewportSplitPlayTests // 실제 전투 화면 분할 시험
{ // 시험 시작
    [UnityTest] // 카메라 첫 프레임 시험
    public IEnumerator CameraUsesLeftThreeQuartersFromActivation() // 실제 카메라의 영역과 비율
    { // 시험 시작
        GameObject holder = new GameObject("BattleCamera"); // 전투 카메라 객체
        try // 시험 범위
        { // 범위 시작
            Camera camera = holder.AddComponent<Camera>(); // 실제 카메라
            camera.orthographic = true; // 전투 직교 카메라
            holder.AddComponent<BattleViewport>(); // 실제 화면 분할 연결
            Rect expected = BattleLayout.BattleViewport(Screen.width, Screen.height); // 현재 화면의 전투 영역
            Assert.That(camera.rect.x, Is.EqualTo(expected.x).Within(0.0001f)); // 활성 직후 왼쪽 위치
            Assert.That(camera.rect.width, Is.EqualTo(expected.width).Within(0.0001f)); // 활성 직후 전투 폭
            Assert.That(camera.aspect, Is.EqualTo(4f / 3f).Within(0.0001f)); // 활성 직후 전투 비율
            yield return null; // 실제 프레임 진행
            Assert.That(camera.rect.width, Is.EqualTo(expected.width).Within(0.0001f)); // 갱신 후 전투 폭
            Assert.That(camera.aspect, Is.EqualTo(4f / 3f).Within(0.0001f)); // 갱신 후 종횡비
            Rect content = BattleSetup.CalculateViewport(Screen.width, Screen.height); // 전체 메뉴 콘텐츠
            Assert.That(camera.rect.xMax, Is.EqualTo(content.x + content.width * 0.75f).Within(0.0001f)); // 실제 오른쪽 전투 경계
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(holder); // 시험 카메라 제거
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 실제 플레이어 입력 시험
    public IEnumerator PlayerMovesAtOneQuarterOfVisibleWidthPerSecond() // 실제 카메라 폭의 이동 속도
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력 설정
        float previousTimeScale = Time.timeScale; // 기존 시간 배율
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 비대화형 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 독립 시험 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드
        GameObject cameraObject = new GameObject("BattleCamera"); // 전투 카메라 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 실제 전투 시간
            Camera camera = cameraObject.AddComponent<Camera>(); // 실제 카메라
            camera.orthographic = true; // 전투 직교 카메라
            camera.orthographicSize = 5f; // 기존 세로 반경
            cameraObject.AddComponent<BattleViewport>(); // 실제 전투 분할
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.AssignCamera(camera); // 전투 카메라 연결
            float halfWidth = camera.orthographicSize * camera.aspect; // 실제 전투 가로 반경
            player.Begin(new Vector2(-halfWidth, 0f)); // 왼쪽 경계에서 시작
            yield return null; // 첫 입력 프레임 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); // 오른쪽 이동 입력
            InputSystem.Update(); // 가상 입력 처리
            yield return null; // 입력 반영 프레임
            float firstTime = Time.time; // 측정 시작 시간
            float firstX = player.transform.position.x; // 측정 시작 위치
            yield return new WaitForSeconds(0.8f); // 실제 전투 시간 진행
            float elapsed = Time.time - firstTime; // 실제 진행 시간
            float traveled = player.transform.position.x - firstX; // 실제 이동 거리
            Assert.That(traveled, Is.EqualTo(halfWidth * 0.5f * elapsed).Within(0.25f)); // 보이는 폭 기준 네 초 횡단
            Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(halfWidth)); // 오른쪽 전투 경계 유지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어와 생성물 제거
            Object.Destroy(cameraObject); // 카메라 제거
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 기존 입력 설정 복원
            InputSystem.settings.backgroundBehavior = previousBackground; // 기존 배경 설정 복원
            Time.timeScale = previousTimeScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
