using System.Collections; // 코루틴 시험 기능
using System.Collections.Generic; // 저장 순서 목록
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투와 측정 기능
using UnityEngine; // 유니티 객체
using UnityEngine.InputSystem; // 가상 키보드 기능
using UnityEngine.InputSystem.LowLevel; // 가상 키 상태 기능
using UnityEngine.TestTools; // 플레이 모드 시험
public class MeasurementRetryPlayTests // 측정 보고서 재시도 시험
{ // 시험 시작
    private sealed class MemoryStore : IProgressStorage // 영구 성장 격리 저장소
    { // 저장소 시작
        private string contents; // 격리 저장 내용
        public ProgressReadStatus Read(out string data, out string error) // 저장 내용 조회
        { // 조회 시작
            data = contents; // 저장 원문 반환
            error = null; // 읽기 오류 없음
            return contents == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 원문 상태
        } // 조회 종료
        public bool Write(string expectedData, string data, out string error) // 원문 비교 저장
        { // 저장 시작
            if (contents != expectedData) // 이전 원문 확인
            { // 충돌 시작
                error = "시험 저장 충돌"; // 충돌 오류
                return false; // 기존 원문 보존
            } // 충돌 종료
            contents = data; // 새 원문 보관
            error = null; // 저장 오류 없음
            return true; // 저장 완료
        } // 저장 종료
    } // 저장소 종료
    [UnityTest] // 실제 결과와 미저장 보고 시험
    public IEnumerator FailedReportsStayOrderedUntilSuccessfulRetry() // 두 출격 보고 복구 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 이전 시간 배율
        GameObject holder = new GameObject("MeasurementRetryHost"); // 격리 화면 객체
        PermanentProgressService progress = new PermanentProgressService(new MemoryStore()); // 격리 영구 성장 서비스
        try // 시험 범위 시작
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 복원
            Camera camera = holder.AddComponent<Camera>(); // 메뉴 카메라 구성
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 출격 관리자 구성
            host.Begin(camera, progress); // 격리 저장소 연결
            host.MeasurementWriterOverride = text => false; // 측정 저장 오류 유도
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(host.Flow.StartRun(), Is.True); // 첫 출격 시작
            yield return BattleSceneTestWait.UntilReady(host); // 실제 전투 씬 완료
            host.CurrentPlayer.Skills.GainEnergy(100); // 첫 출격 필살기 충전
            Assert.That(host.CurrentPlayer.TryUseUltimate(), Is.True); // 첫 출격 필살기 발동
            host.CurrentStage.State.Fail(); // 첫 출격 실패 정산
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 정산 후 결과 화면
            Assert.That(host.CurrentReceipt, Is.Not.Null); // 영구 보상 정산 유지
            Assert.That(host.PendingMeasurementCount, Is.EqualTo(1)); // 첫 보고서 대기
            Assert.That(host.Flow.MeasurementNotice, Does.Contain("저장 실패")); // 저장 오류 안내
            Assert.That(host.Flow.MeasurementNotice, Does.Contain("Tab")); // 재시도 키 안내
            Assert.That(host.Flow.RequestRetry(), Is.True); // 재출격 확인
            Assert.That(host.Flow.ConfirmResultExit(true), Is.True); // 같은 조건 재출격
            yield return BattleSceneTestWait.UntilReady(host); // 실제 재출격 완료
            host.CurrentStage.State.Fail(); // 둘째 출격 실패 정산
            Assert.That(host.PendingMeasurementCount, Is.EqualTo(2)); // 두 보고서 모두 보존
            Assert.That(progress.Mastery(CharacterType.Rapid), Is.EqualTo(20)); // 두 출격의 보상 정상 지급
            List<string> saved = new List<string>(); // 저장 성공 순서
            int writeCalls = 0; // 시험 저장 호출 수
            host.MeasurementWriterOverride = text => // 부분 실패 저장소
            { // 시험 저장 시작
                writeCalls++; // 저장 시도 기록
                if (writeCalls == 2) // 둘째 보고서의 첫 시도
                { // 저장 실패 시작
                    return false; // 둘째 보고서 보존
                } // 저장 실패 종료
                saved.Add(text); // 성공한 보고서 보관
                return true; // 해당 보고서 저장 성공
            }; // 시험 저장 종료
            Assert.That(host.RetryMeasurementReports(), Is.False); // 부분 실패 알림
            Assert.That(saved.Count, Is.EqualTo(1)); // 성공한 첫 보고서만 제거
            Assert.That(saved[0], Does.Contain("필살기 1")); // 첫 출격 보고 순서
            Assert.That(host.PendingMeasurementCount, Is.EqualTo(1)); // 둘째 보고서 대기 유지
            Assert.That(host.RetryMeasurementReports(), Is.True); // 남은 보고서 저장 재시도
            Assert.That(saved.Count, Is.EqualTo(2)); // 두 보고서 저장 완료
            Assert.That(saved[1], Does.Contain("필살기 0")); // 둘째 출격 보고 순서
            Assert.That(host.PendingMeasurementCount, Is.Zero); // 저장 완료 보고 제거
            Assert.That(host.Flow.MeasurementNotice, Is.Null.Or.Empty); // 실패 안내 해제
            yield return null; // 실제 프레임 정리
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            progress.Dispose(); // 격리 서비스 해제
            Object.Destroy(holder); // 시험 객체 제거
            Time.timeScale = previousScale; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 실제 Tab 키 재시도 시험
    public IEnumerator TabRetriesAQueuedReportOnTheResultScreen() // 가상 키보드 재시도 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 이전 시간 배율
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 이전 에디터 입력 설정
        var previousBackground = InputSystem.settings.backgroundBehavior; // 이전 포커스 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 격리된 시험 키보드
        GameObject holder = new GameObject("MeasurementTabHost"); // 격리 화면 객체
        PermanentProgressService progress = new PermanentProgressService(new MemoryStore()); // 격리 저장 서비스
        try // 시험 범위 시작
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 복원
            Camera camera = holder.AddComponent<Camera>(); // 메뉴 카메라 구성
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 출격 관리자 구성
            host.Begin(camera, progress); // 격리 저장소 연결
            host.MeasurementWriterOverride = text => false; // 첫 보고 저장 실패 유도
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(host.Flow.StartRun(), Is.True); // 시험 출격 시작
            yield return BattleSceneTestWait.UntilReady(host); // 실제 전투 씬 완료
            host.CurrentStage.State.Fail(); // 출격 종료와 보고 실패
            Assert.That(host.PendingMeasurementCount, Is.EqualTo(1)); // 미저장 보고 확인
            int saved = 0; // 저장 성공 횟수
            host.MeasurementWriterOverride = text => // 격리 저장 성공 처리
            { // 저장 시작
                saved++; // 실제 저장 횟수 증가
                return true; // 저장 성공 반환
            }; // 저장 종료
            yield return null; // 화면 전환 프레임 경과
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); // 재시도 키 입력
            yield return null; // 실제 입력 프레임 처리
            Assert.That(saved, Is.EqualTo(1)); // 키 입력으로 한 번 저장
            Assert.That(host.PendingMeasurementCount, Is.Zero); // 저장 완료 보고 제거
            Assert.That(host.Flow.MeasurementNotice, Is.Null.Or.Empty); // 오류 안내 해제
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            progress.Dispose(); // 격리 서비스 해제
            Object.Destroy(holder); // 시험 객체 제거
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 이전 포커스 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 이전 입력 설정 복원
            Time.timeScale = previousScale; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
