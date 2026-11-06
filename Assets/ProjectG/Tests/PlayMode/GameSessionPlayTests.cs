using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.InputSystem.LowLevel; // 키보드 상태 기능
using UnityEngine.SceneManagement; // 장면 전환 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 메뉴와 출격 기능
public class GameSessionPlayTests // 실제 출격 화면 흐름 시험
{ // 시험 시작
    [UnityTest] // 타이틀부터 재시도까지 시험
    public IEnumerator KeyboardStartsSelectedBattleAndRetriesAfterFailure() // 화면과 전투 통합 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정
        float previousScale = Time.timeScale; // 기존 시간 배율
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 키 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        GameSessionHost host = null; // 메뉴 연결 참조
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 시작 장면 불러오기
            yield return null; // 타이틀 초기화 대기
            host = Object.FindFirstObjectByType<GameSessionHost>(); // 화면 연결 조회
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 타이틀 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 게임 시작 입력
            yield return null; // 입력 처리 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.CharacterSelect)); // 캐릭터 선택 화면
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 시작 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 저격형 선택 입력
            yield return null; // 선택 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 출격 준비 입력
            yield return null; // 준비 처리 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 출격 준비 화면
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 준비 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 산탄 선택 입력
            yield return null; // 선택 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 출격 입력
            yield return null; // 출격 처리 대기
            yield return BattleSceneTestWait.UntilReady(host); // 실제 전투 씬 완료
            Assert.That(host.CurrentPlayer.Character, Is.EqualTo(CharacterType.Sniper)); // 저격형 출격
            Assert.That(host.CurrentPlayer.Skills.Common, Is.EqualTo(CommonSkill.Scatter)); // 산탄 장착
            host.CurrentPlayer.Survival.TryHit(1000); // 시험 실패 피해
            yield return null; // 결과 처리 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 실패 결과 화면
            Assert.That(host.CurrentPlayer.Statistics.Hits, Is.EqualTo(1)); // 실제 피격 기록
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 출격 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 재시도 요청
            yield return null; // 확인 화면 대기
            Assert.That(host.Flow.ConfirmingResultExit, Is.True); // 진행 손실 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 확인 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 확인 버튼 선택
            yield return null; // 선택 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 재시도 확정
            yield return null; // 새 출격 처리 대기
            yield return BattleSceneTestWait.UntilReady(host); // 실제 재출격 완료
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle)); // 전투 재시작
            Assert.That(host.CurrentPlayer.Character, Is.EqualTo(CharacterType.Sniper)); // 캐릭터 선택 유지
            Assert.That(host.CurrentPlayer.Statistics.Hits, Is.Zero); // 새 출격 기록 초기화
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (host != null) // 메뉴 객체 확인
            { // 제거 시작
                Object.Destroy(host.gameObject); // 화면과 전투 제거
            } // 제거 종료
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 입력 설정 복원
            InputSystem.settings.backgroundBehavior = previousBackground; // 포커스 설정 복원
            Time.timeScale = previousScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
