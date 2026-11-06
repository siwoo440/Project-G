using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 화면 기능
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 입력 상태 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 게임 화면 기능
public class GrowthScreenPlayTests // 영구 성장 화면 실제 입력 시험
{ // 시험 시작
    [UnityTest] // 키보드 진입과 복귀 시험
    public IEnumerator KeyboardOpensGrowthAndReturnsWithoutStartingRun() // 키보드 성장 화면 이동
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 생성
        GameObject holder = new GameObject("GrowthMenu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 메뉴 화면 생성
            flow.Begin(holder.AddComponent<Camera>()); // 타이틀 초기화
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(flow.OpenPreparation(), Is.True); // 출격 준비 진입
            yield return null; // 화면 전환 프레임 경과
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow)); // 뒤로 버튼 선택
            yield return null; // 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow)); // 성장 버튼 선택
            yield return null; // 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 성장 화면 열기
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Growth)); // 성장 화면 진입
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 확인 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 성장 캐릭터 전환
            yield return null; // 입력 처리 대기
            Assert.That(flow.SelectedCharacter, Is.EqualTo(CharacterType.Sniper)); // 저격형 선택 확인
            Assert.That(flow.GrowthSummary(), Does.Contain("약점 사냥 1")); // 저격형 후보 즉시 표시
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 준비 화면 복귀
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 전투 시작 없는 복귀
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 취소 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 복원된 성장 버튼 선택
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Growth)); // 성장 버튼 포커스 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(holder); // 메뉴 객체 제거
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 포커스 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 마우스 진입과 복귀 시험
    public IEnumerator MouseOpensGrowthAndReturnsToPreparation() // 마우스 성장 화면 이동
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Mouse mouse = InputSystem.AddDevice<Mouse>(); // 시험 마우스 생성
        GameObject holder = new GameObject("GrowthMenu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 메뉴 화면 생성
            flow.Begin(holder.AddComponent<Camera>()); // 타이틀 초기화
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(flow.OpenPreparation(), Is.True); // 출격 준비 진입
            yield return null; // 화면 전환 프레임 경과
            Rect normalized = BattleSetup.CalculateViewport(Screen.width, Screen.height); // 메뉴 비율 계산
            Rect view = new Rect(normalized.x * Screen.width, (1f - normalized.yMax) * Screen.height, normalized.width * Screen.width, normalized.height * Screen.height); // 화면 좌표 변환
            var areaMethod = typeof(GameFlowController).GetMethod("PreparationActionRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); // 실제 표시 영역 조회
            Assert.That(areaMethod, Is.Not.Null); // 화면 배치 계산 확인
            object[] areaArguments = new object[] // 성장 버튼 영역 인수
            { // 인수 시작
                view, // 실제 전투 비율 화면
                2 // 영구 성장 버튼 순서
            }; // 인수 종료
            Rect growthArea = (Rect)areaMethod.Invoke(null, areaArguments); // 화면에 표시되는 버튼 영역
            Vector2 growth = growthArea.center; // 실제 성장 버튼 중심
            MouseState down = new MouseState(); // 마우스 클릭 상태
            down.position = new Vector2(growth.x, Screen.height - growth.y); // 입력 좌표 변환
            InputSystem.QueueStateEvent(mouse, down.WithButton(MouseButton.Left)); // 성장 버튼 클릭
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Growth)); // 성장 화면 진입
            MouseState released = new MouseState(); // 마우스 해제 상태
            released.position = down.position; // 이전 좌표 유지
            InputSystem.QueueStateEvent(mouse, released); // 클릭 해제
            yield return null; // 해제 처리 대기
            Vector2 sniper = new Vector2(view.x + view.width * 0.645f, view.y + view.height * 0.20f); // 저격형 버튼 중심
            MouseState select = new MouseState(); // 캐릭터 클릭 상태
            select.position = new Vector2(sniper.x, Screen.height - sniper.y); // 캐릭터 좌표 변환
            InputSystem.QueueStateEvent(mouse, select.WithButton(MouseButton.Left)); // 저격형 버튼 클릭
            yield return null; // 입력 처리 대기
            Assert.That(flow.SelectedCharacter, Is.EqualTo(CharacterType.Sniper)); // 저격형 선택 확인
            Assert.That(flow.GrowthSummary(), Does.Contain("약점 사냥 1")); // 저격형 후보 즉시 표시
            MouseState selectRelease = new MouseState(); // 캐릭터 클릭 해제
            selectRelease.position = select.position; // 캐릭터 좌표 유지
            InputSystem.QueueStateEvent(mouse, selectRelease); // 마우스 클릭 해제
            yield return null; // 해제 처리 대기
            Vector2 back = new Vector2(view.x + view.width * 0.50f, view.y + view.height * 0.865f); // 준비 복귀 버튼 중심
            MouseState backDown = new MouseState(); // 복귀 클릭 상태
            backDown.position = new Vector2(back.x, Screen.height - back.y); // 복귀 좌표 변환
            InputSystem.QueueStateEvent(mouse, backDown.WithButton(MouseButton.Left)); // 복귀 버튼 클릭
            yield return null; // 입력 처리 대기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 출격 준비 복귀
            Assert.That(flow.SelectedCharacter, Is.EqualTo(CharacterType.Sniper)); // 변경한 캐릭터 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(holder); // 메뉴 객체 제거
            InputSystem.RemoveDevice(mouse); // 시험 마우스 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 포커스 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
