using System.Collections; // 코루틴 시험 기능
using System.Reflection; // 씬 속성 시험 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 게임 화면 기능
using UnityEngine; // 유니티 화면 기능
using UnityEngine.InputSystem; // 가상 키보드 기능
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 씬 불러오기 기능
using UnityEngine.TestTools; // 플레이 시험 도구
public class TitleCanvasRegressionPlayTests // 타이틀 참조와 자르기 회귀 시험
{ // 시험 시작
    [UnityTest] // 캔버스와 카메라 오류 시험
    public IEnumerator DisabledCanvasOrCameraCannotReceiveTitleInput() // 보이지 않는 화면 입력 차단
    { // 시험 시작
        var oldInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력
        var oldBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 비대화형 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 포커스 없는 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드
        try // 시험 준비
        { // 준비 시작
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 타이틀 씬
            yield return null; // 화면 연결 대기
            GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 메뉴 세션
            Canvas canvas = GameObject.Find("TitleCanvas").GetComponent<Canvas>(); // 실제 타이틀 캔버스
            Camera camera = canvas.worldCamera; // 실제 메뉴 카메라
            var hasView = typeof(GameFlowController).GetProperty("HasTitleView", BindingFlags.NonPublic | BindingFlags.Instance); // 표시 가능 상태 조회
            canvas.enabled = false; // 사용자의 캔버스 비활성화
            Assert.That((bool)hasView.GetValue(host.Flow), Is.False); // 비활성 캔버스 오류 표시 대상
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 숨겨진 버튼 키 입력
            yield return null; // 입력 처리 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 숨겨진 버튼 실행 차단
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리
            canvas.enabled = true; // 캔버스 복구
            camera.enabled = false; // 카메라 비활성화
            Assert.That((bool)hasView.GetValue(host.Flow), Is.False); // 비활성 카메라 오류 표시 대상
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 보이지 않는 버튼 키 입력
            yield return null; // 입력 처리 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 보이지 않는 버튼 실행 차단
            camera.enabled = true; // 카메라 복구
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            InputSystem.settings.editorInputBehaviorInPlayMode = oldInput; // 편집기 입력 복원
            InputSystem.settings.backgroundBehavior = oldBackground; // 백그라운드 입력 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 배경 자동 자르기 재활성 시험
    public IEnumerator ReenablingAutomaticCropRecalculatesUnchangedTexture() // 사용자 UV 이후 자동 비율 복구
    { // 시험 시작
        Texture2D texture = new Texture2D(2, 2); // 정사각 시험 이미지
        try // 시험 준비
        { // 준비 시작
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 타이틀 씬
            yield return null; // 화면 연결 대기
            GameObject canvas = GameObject.Find("TitleCanvas"); // 실제 타이틀 화면
            Component presenter = canvas.GetComponent("TitleScreenPresenter"); // 배경 표시 연결
            Component image = canvas.transform.Find("Viewport/Background").GetComponent("RawImage"); // 배경 이미지
            var crop = presenter.GetType().GetField("cropBackground", BindingFlags.NonPublic | BindingFlags.Instance); // 자동 자르기 설정
            var imageTexture = image.GetType().GetProperty("texture"); // 실제 이미지 속성
            var uv = image.GetType().GetProperty("uvRect"); // 실제 이미지 좌표
            imageTexture.SetValue(image, texture); // 정사각 이미지 적용
            yield return null; // 최초 자동 자르기
            crop.SetValue(presenter, false); // 수동 UV 사용
            yield return null; // 수동 상태 갱신
            uv.SetValue(image, new Rect(0f, 0f, 1f, 1f)); // 사용자 UV 변경
            crop.SetValue(presenter, true); // 자동 자르기 재활성
            yield return null; // 자동 상태 갱신
            Assert.That(((Rect)uv.GetValue(image)).height, Is.LessThan(0.8f)); // 동일 이미지의 비율 재계산
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(texture); // 시험 이미지 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
