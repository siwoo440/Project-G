using System; // 저장소 팩토리 보관
using System.Collections; // 프레임 시험 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 출격 기능
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 플레이 모드 시험
public class TutorialHostPlayTests // 실제 출격 안내 시험
{ // 시험 시작
    private sealed class MemoryProgress : IProgressStorage // 격리된 성장 저장소
    { // 저장소 시작
        private string content; // 현재 저장 원문
        public ProgressReadStatus Read(out string data, out string error) // 저장 내용 조회
        { // 조회 시작
            data = content; // 격리 내용 반환
            error = null; // 오류 없음
            return content == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 저장 상태 확인
        } // 조회 종료
        public bool Write(string expected, string value, out string error) // 원문 비교 저장
        { // 저장 시작
            error = content == expected ? null : "저장 충돌"; // 충돌 확인
            if (error != null) // 충돌 확인
            { // 충돌 시작
                return false; // 기존 원문 유지
            } // 충돌 종료
            content = value; // 격리 원문 갱신
            return true; // 저장 성공
        } // 저장 종료
    } // 저장소 종료
    private sealed class BrokenTutorialStore : ITutorialStateStore // 저장소 장애 시험
    { // 저장소 시작
        public bool TryRead(out TutorialSaveState state) // 읽기 실패
        { // 조회 시작
            state = TutorialSaveState.Unseen; // 기본 출력
            return false; // 장애 반환
        } // 조회 종료
        public bool TryWrite(TutorialSaveState state) // 쓰기 실패
        { // 저장 시작
            return false; // 장애 반환
        } // 저장 종료
    } // 저장소 종료
    private static void StartRun(GameSessionHost host) // 실제 첫 출격
    { // 출격 시작
        Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 화면
        Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비
        Assert.That(host.Flow.StartRun(), Is.True); // 실제 출격 성공
    } // 출격 종료
    private static IEnumerator WaitForBattle(GameSessionHost host) // 실제 씬 로딩 완료 대기
    { // 대기 시작
        for (int frame = 0; frame < 300 && host.Flow.Screen == GameFlowScreen.Loading; frame++) // 비동기 씬 완료 확인
        { // 프레임 대기 시작
            yield return null; // 시간 배율과 무관한 다음 프레임
        } // 프레임 대기 종료
        Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle), host.Flow.ErrorText); // 실제 전투 시작 확인
    } // 대기 종료
    [UnityTest] // 실제 출격과 재출격 시험
    public IEnumerator RetryResumesUnfinishedGuideAndSkipPersists() // 첫 출격 중단과 건너뛰기
    { // 시험 시작
        Func<ITutorialStateStore> previous = GameSessionHost.TutorialStorageFactoryOverride; // 이전 시험 설정
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("TutorialHost"); // 격리 메뉴 객체
        PermanentProgressService service = new PermanentProgressService(new MemoryProgress()); // 격리 성장 서비스
        try // 시험 범위
        { // 시험 범위 시작
            Time.timeScale = 1f; // 정상 전투 시간
            MemoryTutorialStateStore guideStore = new MemoryTutorialStateStore(); // 격리 안내 상태
            GameSessionHost.TutorialStorageFactoryOverride = () => guideStore; // 메모리 안내 저장소 주입
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 실제 출격 관리자
            host.MeasurementWriterOverride = text => true; // 시험 기록 외부 저장 차단
            host.Begin(holder.AddComponent<Camera>(), service); // 격리 성장 연결
            StartRun(host); // 첫 실제 출격
            yield return WaitForBattle(host); // 실제 전투 씬 완료 대기
            TutorialGuide first = host.CurrentPlayer.transform.root.GetComponent<TutorialGuide>(); // 실제 전투 씬 안내 조회
            Assert.That(first, Is.Not.Null); // 안내 연결 확인
            Assert.That(first.IsShowing, Is.True); // 새 이용자 안내 표시
            Assert.That(host.Flow.RetryFromPause(), Is.True); // 정산 후 재출격
            Assert.That(first.IsShowing, Is.False); // 이전 출격 구독 해제
            yield return WaitForBattle(host); // 새 전투 씬 완료 대기
            TutorialGuide retry = host.CurrentPlayer.transform.root.GetComponent<TutorialGuide>(); // 새 출격 안내 조회
            Assert.That(retry, Is.Not.SameAs(first)); // 새 출격 안내 구성
            Assert.That(retry.IsShowing, Is.True); // 미완료 안내 재개
            retry.Skip(); // 사용자 건너뛰기
            Assert.That(host.Flow.RetryFromPause(), Is.True); // 다시 정산과 재출격
            yield return WaitForBattle(host); // 마지막 전투 씬 완료 대기
            TutorialGuide skipped = host.CurrentPlayer.transform.root.GetComponent<TutorialGuide>(); // 마지막 출격 안내 조회
            Assert.That(skipped.IsShowing, Is.False); // 건너뛰기 영속 확인
            yield return null; // 이전 전투 객체 실제 파괴
            Assert.That(host.CurrentPlayer, Is.Not.Null); // 현재 전투 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            GameSessionHost.TutorialStorageFactoryOverride = previous; // 원래 저장소 설정 복원
            UnityEngine.Object.Destroy(holder); // 시험 객체 파괴
            service.Dispose(); // 격리 성장 서비스 해제
            Time.timeScale = previousScale; // 이전 시간 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 이전 출격 기록과 저장 장애 시험
    public IEnumerator ExistingHistoryAndUnavailableGuideStoreNeverBlockBattle() // 실제 이전 출격과 장애 확인
    { // 시험 시작
        Func<ITutorialStateStore> previous = GameSessionHost.TutorialStorageFactoryOverride; // 이전 시험 설정
        GameObject firstHolder = new GameObject("ExistingTutorialHost"); // 기존 이용자 메뉴
        GameObject secondHolder = new GameObject("BrokenTutorialHost"); // 저장 장애 메뉴
        PermanentProgressService existing = new PermanentProgressService(new MemoryProgress()); // 기존 이용자 격리 저장
        PermanentProgressService fresh = new PermanentProgressService(new MemoryProgress()); // 새 이용자 격리 저장
        try // 시험 범위
        { // 시험 범위 시작
            Assert.That(existing.Load(out string error), Is.True, error); // 기존 저장 초기화
            Assert.That(existing.TryBegin("previous-run", CharacterType.Rapid, out error), Is.True, error); // 이전 출격 기록
            Assert.That(existing.TrySettle("previous-run", CharacterType.Rapid, false, out _, out error), Is.True, error); // 이전 출격 정산
            MemoryTutorialStateStore unseen = new MemoryTutorialStateStore(); // 새 안내 저장소
            GameSessionHost.TutorialStorageFactoryOverride = () => unseen; // 안내 저장소 주입
            GameSessionHost oldHost = firstHolder.AddComponent<GameSessionHost>(); // 기존 출격 관리자
            oldHost.Begin(firstHolder.AddComponent<Camera>(), existing); // 기존 저장 연결
            StartRun(oldHost); // 실제 출격
            yield return WaitForBattle(oldHost); // 기존 이용자 전투 씬 대기
            Assert.That(oldHost.CurrentPlayer.transform.root.GetComponent<TutorialGuide>().IsShowing, Is.False); // 기존 이용자 자동 안내 차단
            GameSessionHost.TutorialStorageFactoryOverride = () => new BrokenTutorialStore(); // 안내 저장 장애 주입
            GameSessionHost freshHost = secondHolder.AddComponent<GameSessionHost>(); // 새 출격 관리자
            freshHost.Begin(secondHolder.AddComponent<Camera>(), fresh); // 새 저장 연결
            StartRun(freshHost); // 저장 장애 중 실제 출격
            yield return WaitForBattle(freshHost); // 새 이용자 전투 씬 대기
            Assert.That(freshHost.CurrentPlayer.transform.root.GetComponent<TutorialGuide>().IsShowing, Is.False); // 안내 읽기 장애 생략
            Assert.That(freshHost.CurrentPlayer.Survival.IsAlive, Is.True); // 실제 전투 보존
            yield return null; // 실제 프레임 진행
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            GameSessionHost.TutorialStorageFactoryOverride = previous; // 이전 저장소 설정 복원
            UnityEngine.Object.Destroy(firstHolder); // 기존 이용자 시험 정리
            UnityEngine.Object.Destroy(secondHolder); // 저장 장애 시험 정리
            existing.Dispose(); // 기존 격리 저장 해제
            fresh.Dispose(); // 새 격리 저장 해제
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
