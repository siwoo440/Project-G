using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 게임 화면 기능
using UnityEngine; // 유니티 객체 기능
using UnityEngine.Events; // 버튼 이벤트 기능
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 실제 씬 연결
using UnityEngine.TestTools; // 플레이 시험 기능
public class TitleSceneReviewPlayTests // 타이틀 입력과 편집 회귀 시험
{ // 시험 시작
    private static IEnumerator Press(Keyboard keyboard, Key key) // 키 입력과 해제
    { // 입력 시작
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 키 누르기
        yield return null; // 입력 프레임
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
        yield return null; // 해제 프레임
    } // 입력 종료
    private static IEnumerator Click(Mouse mouse, Vector2 point) // 포인터 클릭
    { // 클릭 시작
        MouseState state = new MouseState(); // 입력 상태
        state.position = point; // 클릭 좌표
        InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left)); // 왼쪽 버튼 입력
        yield return null; // 입력 프레임
        InputSystem.QueueStateEvent(mouse, new MouseState()); // 버튼 해제
        yield return null; // 해제 프레임
    } // 클릭 종료
    [UnityTest] // 비활성 버튼과 공통 이벤트 시험
    public IEnumerator DisabledButtonsBlockBothInputsAndKeyboardInvokesButtonEvent() // 입력 방식별 버튼 일치
    { // 시험 시작
        var oldInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력
        var oldBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력
        float oldScale = Time.timeScale; // 기존 시간 배율
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 비대화형 입력
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 포커스 없는 입력
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드
        Mouse mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스
        try // 시험 준비
        { // 준비 시작
            Time.timeScale = 1f; // 정상 시간
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 타이틀 씬
            yield return null; // 화면 연결 대기
            GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 화면 흐름
            GameObject canvas = GameObject.Find("TitleCanvas"); // 실제 타이틀 캔버스
            GameObject buttonObject = canvas.transform.Find("Viewport/Start").gameObject; // 시작 버튼 객체
            Component button = buttonObject.GetComponent("Button"); // 실제 버튼 구성
            Vector2 point = RectTransformUtility.WorldToScreenPoint(canvas.GetComponent<Canvas>().worldCamera, buttonObject.transform.position); // 표시 버튼 화면 위치
            buttonObject.SetActive(false); // 사용자의 버튼 숨김
            yield return Click(mouse, point); // 숨겨진 버튼 클릭
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 보이지 않는 버튼 차단
            buttonObject.SetActive(true); // 버튼 표시 복구
            button.GetType().GetProperty("interactable").SetValue(button, false); // 상호작용 비활성화
            yield return Press(keyboard, Key.Enter); // 선택 버튼 확인
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 비활성 버튼 키 입력 차단
            button.GetType().GetProperty("interactable").SetValue(button, true); // 버튼 입력 복구
            int invoked = 0; // 사용자 버튼 이벤트 횟수
            UnityEvent click = (UnityEvent)button.GetType().GetProperty("onClick").GetValue(button); // 직렬화 가능한 버튼 이벤트
            click.AddListener(() => invoked++); // 사용자 이벤트 연결
            yield return Press(keyboard, Key.Enter); // 키보드 버튼 실행
            Assert.That(invoked, Is.EqualTo(1)); // 키보드의 동일 이벤트 경로
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.CharacterSelect)); // 기존 화면 전환
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
            InputSystem.settings.editorInputBehaviorInPlayMode = oldInput; // 편집기 입력 복원
            InputSystem.settings.backgroundBehavior = oldBackground; // 백그라운드 입력 복원
            Time.timeScale = oldScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 모달 배경 보존 시험
    public IEnumerator SettingsKeepsEditedTitleBackgroundVisibleWhileBlockingInput() // 사용자 배경과 입력 차단
    { // 시험 시작
        var oldInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력
        var oldBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력
        float oldScale = Time.timeScale; // 기존 시간 배율
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 비대화형 입력
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 포커스 없는 입력
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드
        Mouse mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스
        Texture2D replacement = new Texture2D(2, 2); // 사용자가 교체한 이미지
        try // 시험 준비
        { // 준비 시작
            Time.timeScale = 1f; // 정상 시간
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 타이틀 씬
            yield return null; // 화면 연결 대기
            GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 화면 흐름
            GameObject canvas = GameObject.Find("TitleCanvas"); // 실제 타이틀 캔버스
            Transform viewport = canvas.transform.Find("Viewport"); // 사용자의 타이틀 화면
            Component image = viewport.Find("Background").GetComponent("RawImage"); // 배경 이미지 구성
            image.GetType().GetProperty("texture").SetValue(image, replacement); // 사용자 배경 변경
            Vector2 startPoint = RectTransformUtility.WorldToScreenPoint(canvas.GetComponent<Canvas>().worldCamera, viewport.Find("Start").position); // 시작 버튼 화면 위치
            yield return Press(keyboard, Key.DownArrow); // 설정 선택
            yield return Press(keyboard, Key.Enter); // 설정 열기
            Assert.That(host.Flow.InputBlocked, Is.True); // 모달 입력 차단
            Assert.That(viewport.gameObject.activeInHierarchy, Is.True); // 편집한 배경 유지
            Assert.That(image.GetType().GetProperty("texture").GetValue(image), Is.SameAs(replacement)); // 교체 이미지 유지
            yield return Click(mouse, startPoint); // 모달 뒤 버튼 클릭
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 모달 뒤 입력 차단
            yield return Press(keyboard, Key.Escape); // 설정 닫기
            yield return null; // 모달 동기화
            Assert.That(host.Flow.InputBlocked, Is.False); // 메뉴 입력 복구
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(replacement); // 시험 이미지 제거
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
            InputSystem.settings.editorInputBehaviorInPlayMode = oldInput; // 편집기 입력 복원
            InputSystem.settings.backgroundBehavior = oldBackground; // 백그라운드 입력 복원
            Time.timeScale = oldScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 실행 중 사용자 색상 변경 시험
    public IEnumerator InspectorColorChangesPersistDuringPlay() // 실행 중 편집한 색상 보존
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 타이틀 씬
        yield return null; // 화면 연결 대기
        Transform settings = GameObject.Find("TitleCanvas").transform.Find("Viewport/Settings/Text"); // 선택하지 않은 설정 글자
        Component text = settings.GetComponent("Text"); // 편집 가능한 글자 구성
        Color chosen = new Color(0.37f, 0.61f, 0.83f, 1f); // 사용자 지정 색상
        text.GetType().GetProperty("color").SetValue(text, chosen); // 실행 중 색상 변경
        yield return null; // 화면 갱신 프레임
        Assert.That((Color)text.GetType().GetProperty("color").GetValue(text), Is.EqualTo(chosen)); // 변경 색상 유지
    } // 시험 종료
} // 시험 종료
