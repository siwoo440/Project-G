using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 화면 기능
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 입력 상태 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 게임 화면 기능
public class GameFlowPlayTests // 실제 메뉴 입력 시험
{ // 시험 시작
    [UnityTest] // 키보드 화면 이동 시험
    public IEnumerator KeyboardSelectsCharacterSkillAndStartsRun() // 키보드 출격 흐름 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 생성
        GameObject holder = new GameObject("Menu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 메뉴 흐름 구성
            flow.Begin(holder.AddComponent<Camera>()); // 타이틀 시작
            yield return null; // 화면 전환 프레임 경과
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 시작 키 입력
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.CharacterSelect)); // 캐릭터 선택 진입
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 다음 캐릭터 입력
            yield return null; // 입력 처리 대기
            Assert.That(flow.SelectedCharacter, Is.EqualTo(CharacterType.Sniper)); // 저격형 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 준비 화면 입력
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 준비 화면 진입
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 공용 스킬 변경
            yield return null; // 입력 처리 대기
            Assert.That(flow.SelectedCommonSkill, Is.EqualTo(CommonSkill.Scatter)); // 산탄 스킬 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 출격 입력
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Battle)); // 전투 화면 진입
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(holder); // 메뉴 객체 제거
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 포커스 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 마우스 시작 버튼 시험
    public IEnumerator MouseStartsCharacterSelection() // 실제 클릭 선택 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Mouse mouse = InputSystem.AddDevice<Mouse>(); // 시험 마우스 생성
        GameObject holder = new GameObject("Menu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 메뉴 흐름 구성
            flow.Begin(holder.AddComponent<Camera>()); // 타이틀 시작
            yield return null; // 화면 전환 프레임 경과
            Rect normalized = BattleSetup.CalculateViewport(Screen.width, Screen.height); // 메뉴 비율 계산
            Rect view = new Rect(normalized.x * Screen.width, (1f - normalized.yMax) * Screen.height, normalized.width * Screen.width, normalized.height * Screen.height); // 화면 좌표 변환
            Vector2 button = new Vector2(view.x + view.width * 0.385f, view.y + view.height * 0.845f); // 시작 버튼 중심
            MouseState state = new MouseState(); // 마우스 상태 생성
            state.position = new Vector2(button.x, Screen.height - button.y); // 입력 좌표 변환
            InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left)); // 시작 버튼 클릭
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.CharacterSelect)); // 캐릭터 화면 진입
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(holder); // 메뉴 객체 제거
            InputSystem.RemoveDevice(mouse); // 시험 마우스 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 포커스 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 준비 화면 키보드 접근 시험
    public IEnumerator KeyboardCanChangeSkillScrollSummaryAndOpenGrowth() // 스킬과 하단 버튼 접근 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 생성
        GameObject holder = new GameObject("PreparationKeyboard"); // 메뉴 시험 객체
        try // 시험 범위 준비
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 생성
            flow.Begin(holder.AddComponent<Camera>()); // 메뉴 초기화
            flow.ProgressDescription = character => new string('가', 1000); // 긴 성장 안내 준비
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(flow.OpenPreparation(), Is.True); // 출격 준비 진입
            yield return null; // 화면 전환 프레임 경과
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.PageDown)); // 긴 정보 아래 이동
            yield return null; // 스크롤 입력 처리
            var scroll = typeof(GameFlowController).GetField("preparationSummaryScroll", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); // 실제 스크롤 상태 조회
            Assert.That(((Vector2)scroll.GetValue(flow)).y, Is.GreaterThan(0f)); // 키보드 스크롤 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 공용 스킬 변경
            yield return null; // 스킬 입력 처리
            Assert.That(flow.SelectedCommonSkill, Is.EqualTo(CommonSkill.Scatter)); // 산탄 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow)); // 두 번째 버튼 이동
            yield return null; // 버튼 입력 처리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow)); // 세 번째 버튼 이동
            yield return null; // 버튼 입력 처리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 영구 성장 확인
            yield return null; // 확인 입력 처리
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Growth)); // 키보드 성장 화면 진입
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(holder); // 메뉴 객체 제거
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 기존 포커스 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 기존 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
