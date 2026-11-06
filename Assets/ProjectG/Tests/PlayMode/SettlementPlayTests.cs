using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 전투 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 출격과 영구 성장 기능
public class SettlementPlayTests // 실제 전투 정산 흐름 시험
{ // 시험 시작
    private sealed class MemoryStore : IProgressStorage // 사용자 파일과 격리된 저장소
    { // 저장소 시작
        private string contents; // 격리된 저장 내용
        public bool FailWrites; // 저장 실패 유도
        public ProgressReadStatus Read(out string data, out string error) // 저장된 내용 읽기
        { // 읽기 시작
            data = contents; // 현재 내용 반환
            error = null; // 읽기 오류 없음
            return contents == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 저장 상태 반환
        } // 읽기 종료
        public bool Write(string expectedData, string data, out string error) // 저장 내용 비교 기록
        { // 기록 시작
            if (FailWrites || contents != expectedData) // 저장 실패와 충돌 확인
            { // 실패 시작
                error = "시험 저장 실패"; // 시험 오류 안내
                return false; // 기존 내용 보존
            } // 실패 종료
            contents = data; // 메모리 내용 기록
            error = null; // 기록 오류 없음
            return true; // 저장 완료
        } // 기록 종료
    } // 저장소 종료
    private static GameSessionHost CreateHost(GameObject holder, MemoryStore store) // 격리된 메뉴 생성
    { // 생성 시작
        Camera camera = holder.AddComponent<Camera>(); // 메뉴 카메라 생성
        GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 출격 관리자 생성
        host.Begin(camera, new PermanentProgressService(store)); // 메모리 서비스 주입
        return host; // 출격 관리자 반환
    } // 생성 종료
    private static void StartBattle(GameSessionHost host) // 초기 메뉴에서 출격
    { // 출격 시작
        Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 화면
        Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 화면
        Assert.That(host.Flow.StartRun(), Is.True); // 시작 기록 저장과 출격
    } // 출격 종료
    [UnityTest] // 실제 프레임 실패와 재출격 시험
    public IEnumerator ActualFailureSettlesRewardAndRetriesWithFreshBattle() // 실패 정산과 재시도 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 이전 시간 배율 보관
        GameObject holder = new GameObject("SettlementPlayHost"); // 메뉴 객체 생성
        GameSessionHost host = null; // 시험 관리자 참조
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 정상 시간 배율
            MemoryStore store = new MemoryStore(); // 사용자 파일과 격리된 저장소
            host = CreateHost(holder, store); // 화면 관리자 생성
            StartBattle(host); // 실제 시험 출격
            yield return BattleSceneTestWait.UntilReady(host); // 실제 전투 씬 완료
            BattlePlayer first = host.CurrentPlayer; // 첫 출격 플레이어 보관
            Assert.That(first.Survival.TryHit(1000), Is.True); // 체력 소진으로 실패 유도
            yield return null; // 실제 실패 프레임 진행
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 저장 후 결과 화면
            Assert.That(host.CurrentReceipt, Is.Not.Null); // 저장된 영수증 존재
            Assert.That(host.CurrentReceipt.MasteryGained, Is.EqualTo(10)); // 실패 참여 숙련도
            Assert.That(host.CurrentReceipt.CommonMaterialsGained, Is.EqualTo(3)); // 실패 참여 공통 재료
            Assert.That(host.Progress.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 저장된 캐릭터 숙련도
            Assert.That(host.Progress.CommonMaterials, Is.EqualTo(3)); // 저장된 공통 재료
            Assert.That(host.Flow.RequestRetry(), Is.True); // 결과 재시도 확인
            Assert.That(host.Flow.ConfirmResultExit(true), Is.True); // 재출격 확정
            yield return BattleSceneTestWait.UntilReady(host); // 실제 재출격 완료
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle)); // 새 전투 화면
            Assert.That(host.CurrentPlayer, Is.Not.SameAs(first)); // 새 전투 객체 생성
            Assert.That(host.Progress.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 이전 보상 중복 방지
            Assert.That(host.Progress.CommonMaterials, Is.EqualTo(3)); // 이전 재료 중복 방지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (host != null && host.Progress != null) // 주입된 서비스 확인
            { // 해제 시작
                host.Progress.Dispose(); // 시험 저장소 해제
            } // 해제 종료
            Object.Destroy(holder); // 시험 메뉴와 전투 제거
            Time.timeScale = previousScale; // 이전 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 출격 결과 측정 보고 시험
    public IEnumerator SettlementExportsOnlyRecordedRunWithoutWritingUserFiles() // 격리 보고와 정산 연결 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 이전 시간 배율 보관
        GameObject holder = new GameObject("MeasurementPlayHost"); // 시험 메뉴 객체
        GameSessionHost host = null; // 시험 관리자 참조
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 정상 시간 배율
            host = CreateHost(holder, new MemoryStore()); // 격리 저장소 연결
            string report = null; // 저장 요청 보고 내용
            int writes = 0; // 저장 요청 횟수
            host.MeasurementWriterOverride = text => // 격리 보고서 수집
            { // 수집 시작
                report = text; // 실제 보고 보관
                writes++; // 저장 요청 증가
                return true; // 시험 저장 성공
            }; // 수집 종료
            StartBattle(host); // 실제 시험 출격
            yield return BattleSceneTestWait.UntilReady(host); // 실제 전투 씬 완료
            BattlePlayer player = host.CurrentPlayer; // 현재 출격 플레이어
            player.Skills.GainEnergy(100); // 필살기 에너지 준비
            Assert.That(player.TryUseUltimate(), Is.True); // 실제 필살기 발동
            yield return null; // 실제 프레임 처리
            host.CurrentStage.State.Fail(); // 실패 결과 확정
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 결과 화면 확인
            Assert.That(writes, Is.EqualTo(1)); // 출격당 보고서 한 번 요청
            Assert.That(report, Does.Contain("필살기 1")); // 실제 필살기 기록 확인
            Assert.That(report, Does.Contain("출격 시드:")); // 재현 시드 기록 확인
            Assert.That(report, Does.Contain("미측정")); // 배치 렌더 성능 미측정 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (host != null && host.Progress != null) // 주입 서비스 확인
            { // 해제 시작
                host.Progress.Dispose(); // 격리 저장소 해제
            } // 해제 종료
            Object.Destroy(holder); // 시험 전투 제거
            Time.timeScale = previousScale; // 이전 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 저장 실패와 프레임 정지 시험
    public IEnumerator FailedWritePausesBattleUntilSettlementRetrySucceeds() // 정산 오류 대기와 재시도 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 이전 시간 배율 보관
        GameObject holder = new GameObject("SettlementFailurePlayHost"); // 메뉴 객체 생성
        GameSessionHost host = null; // 시험 관리자 참조
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 정상 시간 배율
            MemoryStore store = new MemoryStore(); // 사용자 파일과 격리된 저장소
            host = CreateHost(holder, store); // 화면 관리자 생성
            StartBattle(host); // 실제 시험 출격
            yield return BattleSceneTestWait.UntilReady(host); // 실제 전투 씬 완료
            BattlePlayer player = host.CurrentPlayer; // 출격 플레이어 보관
            store.FailWrites = true; // 정산 저장 오류 유도
            Assert.That(player.Survival.TryHit(1000), Is.True); // 체력 소진으로 실패 유도
            yield return null; // 실제 실패 프레임 진행
            Assert.That(host.Flow.SettlementPending, Is.True); // 저장 재시도 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle)); // 저장 전 결과 화면 차단
            Assert.That(host.CurrentReceipt, Is.Null); // 미저장 영수증 차단
            Assert.That(host.CurrentPlayer, Is.SameAs(player)); // 현재 전투 보존
            Assert.That(Time.timeScale, Is.Zero); // 전투 시간 정지
            float combatSeconds = host.CurrentStage.State.CombatSeconds; // 정지 전 전투 시간 보관
            Vector3 position = player.transform.position; // 정지 전 플레이어 위치 보관
            yield return new WaitForSecondsRealtime(0.08f); // 실제 시간 동안 프레임 진행
            Assert.That(host.CurrentStage.State.CombatSeconds, Is.EqualTo(combatSeconds)); // 전투 시간 유지
            Assert.That(player.transform.position, Is.EqualTo(position)); // 플레이어 위치 유지
            Assert.That(host.Flow.RetrySettlement(), Is.False); // 오류 지속 중 재시도 거부
            Assert.That(host.CurrentPlayer, Is.SameAs(player)); // 재시도 후 전투 보존
            store.FailWrites = false; // 저장 오류 해제
            Assert.That(host.Flow.RetrySettlement(), Is.True); // 정산 저장 재시도
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 저장 성공 후 결과 화면
            Assert.That(host.CurrentReceipt.MasteryGained, Is.EqualTo(10)); // 실패 참여 숙련도
            Assert.That(host.Progress.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 숙련도 한 번 지급
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (host != null && host.Progress != null) // 주입된 서비스 확인
            { // 해제 시작
                host.Progress.Dispose(); // 시험 저장소 해제
            } // 해제 종료
            Object.Destroy(holder); // 시험 메뉴와 전투 제거
            Time.timeScale = previousScale; // 이전 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
