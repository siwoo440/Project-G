using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 메뉴 상태 기능
using UnityEngine; // 유니티 화면 기능
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 실제 장면 연결
using UnityEngine.TestTools; // 플레이 시험 도구
public class TitleScenePlayTests // 직렬화된 타이틀 입력 시험
{ // 시험 시작
    [UnityTest] // 필수 참조 누락 시험
    public IEnumerator MissingSceneReferencesDisableIncompletePresenter() // 불완전한 타이틀 차단
    { // 시험 시작
        GameObject holder = new GameObject("IncompleteTitle"); // 참조 없는 시험 화면
        try // 시험 준비
        { // 준비 시작
            LogAssert.Expect(LogType.Error, "타이틀 화면의 필수 연결이 없습니다."); // 오류 안내 확인
            TitleScreenPresenter view = holder.AddComponent<TitleScreenPresenter>(); // 미연결 화면 추가
            yield return null; // 초기화 프레임
            Assert.That(view.enabled, Is.False); // 불완전한 화면 차단
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(holder); // 시험 화면 제거
        } // 정리 종료
    } // 시험 종료
    private static IEnumerator Press(Keyboard keyboard, Key key) // 키 입력과 해제
    { // 입력 시작
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 키 입력
        yield return null; // 입력 프레임
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
        yield return null; // 해제 프레임
    } // 입력 종료
    private static IEnumerator Click(Mouse mouse, Vector2 point) // 가상 마우스 클릭
    { // 클릭 시작
        MouseState state = new MouseState(); // 마우스 상태
        state.position = point; // 화면 좌표
        InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left)); // 버튼 누르기
        yield return null; // 클릭 프레임
        InputSystem.QueueStateEvent(mouse, new MouseState()); // 버튼 해제
        yield return null; // 해제 프레임
    } // 클릭 종료
    [UnityTest] // 버튼 이동과 실제 입력 시험
    public IEnumerator MovedStartButtonUsesItsActualScenePosition() // 편집한 위치의 버튼 동작
    { // 시험 시작
        var previousInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력
        float previousScale = Time.timeScale; // 기존 시간 배율
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Mouse mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스
        try // 시험 준비
        { // 준비 시작
            Time.timeScale = 1f; // 정상 시간 배율
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 저장된 씬 열기
            yield return null; // 화면 연결 대기
            GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 메뉴 흐름
            GameObject canvas = GameObject.Find("TitleCanvas"); // 직렬화된 타이틀
            Assert.That(canvas, Is.Not.Null); // 타이틀 존재 확인
            RectTransform button = canvas.transform.Find("Viewport/Start").GetComponent<RectTransform>(); // 편집 가능한 시작 버튼
            button.anchoredPosition += new Vector2(-200f, 200f); // 사용자가 바꾼 버튼 위치
            Canvas.ForceUpdateCanvases(); // 화면 배치 갱신
            yield return Click(mouse, RectTransformUtility.WorldToScreenPoint(canvas.GetComponent<Canvas>().worldCamera, button.position)); // 바뀐 위치 클릭
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.CharacterSelect)); // 실제 캐릭터 화면 전환
            Assert.That(canvas.transform.Find("Viewport").gameObject.activeSelf, Is.False); // 타이틀 숨김 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 백그라운드 입력 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousInput; // 편집기 입력 복원
            Time.timeScale = previousScale; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 키보드와 설정 모달 시험
    public IEnumerator KeyboardOpensAndClosesSettingsWithoutLeakingClicks() // 설정 화면과 입력 제한 확인
    { // 시험 시작
        var previousInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력
        float previousScale = Time.timeScale; // 기존 시간 배율
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드
        Mouse mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스
        try // 시험 준비
        { // 준비 시작
            Time.timeScale = 1f; // 정상 시간 배율
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 메뉴 씬 열기
            yield return null; // 화면 연결 대기
            GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 메뉴 흐름
            GameObject canvas = GameObject.Find("TitleCanvas"); // 타이틀 캔버스
            Transform viewport = canvas.transform.Find("Viewport"); // 실제 표시 영역
            Vector2 startPoint = RectTransformUtility.WorldToScreenPoint(canvas.GetComponent<Canvas>().worldCamera, viewport.Find("Start").position); // 시작 버튼 화면 위치
            yield return Press(keyboard, Key.DownArrow); // 설정 버튼 선택
            yield return Press(keyboard, Key.Enter); // 설정 화면 열기
            Assert.That(host.Flow.InputBlocked, Is.True); // 메뉴 입력 차단 확인
            Assert.That(viewport.gameObject.activeSelf, Is.True); // 사용자 배경 보존
            yield return Click(mouse, startPoint); // 숨겨진 시작 버튼 클릭
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 배경 버튼 차단
            yield return Press(keyboard, Key.Escape); // 설정 화면 닫기
            yield return null; // 메뉴 상태 동기화
            Assert.That(host.Flow.InputBlocked, Is.False); // 메뉴 입력 복구
            Assert.That(viewport.gameObject.activeSelf, Is.True); // 타이틀 표시 복구
            yield return Press(keyboard, Key.UpArrow); // 시작 버튼 선택
            yield return Press(keyboard, Key.Enter); // 시작 화면 진입
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.CharacterSelect)); // 키보드 화면 전환
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 백그라운드 입력 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousInput; // 편집기 입력 복원
            Time.timeScale = previousScale; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
