using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 객체
using ProjectG; // 전투와 영구 성장
public class SettlementFlowTests // 정산 화면 통합 시험
{ // 시험 시작
    private sealed class MemoryStore : IProgressStorage // 격리된 저장소
    { // 저장소 시작
        public string Data; // 저장된 내용
        public bool FailReads; // 읽기 실패 여부
        public bool FailWrites; // 쓰기 실패 여부
        public bool LoseAcknowledgement; // 저장 응답 누락 여부
        public ProgressReadStatus Read(out string data, out string error) // 저장 내용 읽기
        { // 읽기 시작
            data = Data; // 보관 내용 반환
            error = FailReads ? "읽기 실패" : null; // 읽기 오류 전달
            return FailReads ? ProgressReadStatus.Error : Data == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 읽기 상태 반환
        } // 읽기 종료
        public bool Write(string expectedData, string data, out string error) // 저장 내용 비교 기록
        { // 쓰기 시작
            error = FailWrites ? "쓰기 실패" : Data != expectedData ? "쓰기 충돌" : null; // 쓰기 오류 전달
            if (error != null) // 오류 상태 확인
            { // 거부 시작
                return false; // 기존 내용 유지
            } // 거부 종료
            Data = data; // 내용 기록
            if (LoseAcknowledgement) // 저장 후 응답 오류 확인
            { // 응답 누락 시작
                LoseAcknowledgement = false; // 다음 저장 응답 복원
                error = "저장 응답 누락"; // 저장 결과 미확인 안내
                return false; // 저장 후 오류 전달
            } // 응답 누락 종료
            return true; // 저장 완료
        } // 쓰기 종료
    } // 저장소 종료
    private static GameSessionHost CreateHost(GameObject holder, MemoryStore store) // 격리된 메뉴 생성
    { // 생성 시작
        Camera camera = holder.AddComponent<Camera>(); // 메뉴 카메라 생성
        GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 출격 관리자 생성
        host.Begin(camera, new PermanentProgressService(store)); // 메모리 저장소 연결
        return host; // 관리자 반환
    } // 생성 종료
    private static void DestroyHost(GameObject holder) // 주입 서비스와 시험 객체 정리
    { // 정리 시작
        GameSessionHost host = holder.GetComponent<GameSessionHost>(); // 시험 관리자 확인
        if (host != null && host.Progress != null) // 주입된 서비스 확인
        { // 해제 시작
            host.Progress.Dispose(); // 시험 저장소 해제
        } // 해제 종료
        Object.DestroyImmediate(holder); // 시험 객체 제거
    } // 정리 종료
    private static void Prepare(GameSessionHost host) // 출격 화면 준비
    { // 준비 시작
        Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 화면 열기
        Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 열기
    } // 준비 종료
    [Test] // 저장 실패와 재시도 시험
    public void FailedSettlementKeepsBattleUntilRetryPersistsReward() // 저장 성공 전 결과 차단
    { // 시험 시작
        GameObject holder = new GameObject("SettlementHost"); // 시험 객체 생성
        float previousScale = Time.timeScale; // 시간 배율 보관
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시간 배율 초기화
            MemoryStore store = new MemoryStore(); // 메모리 저장소 생성
            GameSessionHost host = CreateHost(holder, store); // 화면 관리자 생성
            Prepare(host); // 출격 준비
            Assert.That(host.Flow.StartRun(), Is.True); // 출격 기록 저장과 시작
            BattlePlayer player = host.CurrentPlayer; // 현재 전투 보관
            store.FailWrites = true; // 정산 저장 실패 유도
            host.CurrentStage.State.Fail(); // 전투 실패 처리
            Assert.That(host.Flow.SettlementPending, Is.True); // 저장 재시도 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle)); // 결과 화면 전환 차단
            Assert.That(host.CurrentPlayer, Is.SameAs(player)); // 출격 데이터 유지
            Assert.That(Time.timeScale, Is.Zero); // 정산 대기 중 전투 정지
            Assert.That(host.CurrentReceipt, Is.Null); // 미지급 영수증 차단
            store.FailWrites = false; // 저장 오류 해제
            Assert.That(host.Flow.RetrySettlement(), Is.True); // 정산 재시도
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 저장 후 결과 화면
            Assert.That(host.CurrentReceipt.MasteryGained, Is.EqualTo(10)); // 실패 참여 숙련도
            Assert.That(host.CurrentReceipt.CommonMaterialsGained, Is.EqualTo(3)); // 실패 참여 재료
            Assert.That(host.Progress.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 저장된 숙련도
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            DestroyHost(holder); // 주입 서비스와 시험 객체 제거
            Time.timeScale = previousScale; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 저장 완료 후 응답 누락 화면 시험
    public void SavedSettlementWithLostAcknowledgementShowsResultOnce() // 저장 확인 후 결과 표시
    { // 시험 시작
        GameObject holder = new GameObject("LostSettlementHost"); // 격리된 메뉴 객체
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위 준비
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 시간 초기화
            MemoryStore store = new MemoryStore(); // 격리된 메모리 저장소
            GameSessionHost host = CreateHost(holder, store); // 출격 화면 연결
            Prepare(host); // 출격 준비 화면
            Assert.That(host.Flow.StartRun(), Is.True); // 출격 시작 저장
            store.LoseAcknowledgement = true; // 정산 저장 후 응답 누락
            host.CurrentStage.State.Fail(); // 실제 전투 실패 처리
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 원문 확인 후 결과 표시
            Assert.That(host.Flow.SettlementPending, Is.False); // 불필요한 재시도 대기 방지
            Assert.That(host.CurrentReceipt, Is.Not.Null); // 확인된 영수증 표시
            Assert.That(host.CurrentReceipt.AlreadySettled, Is.False); // 최초 지급 결과 확인
            Assert.That(host.Progress.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 숙련도 한 번 지급
            Assert.That(host.Progress.CommonMaterials, Is.EqualTo(3)); // 공통 재료 한 번 지급
            string persisted = store.Data; // 실제 저장 원문 보관
            Assert.That(host.Progress.TrySettle(host.CurrentReceipt.RunId, CharacterType.Rapid, false, out SettlementReceipt repeated, out string error), Is.True, error); // 같은 정산 재조회
            Assert.That(repeated.AlreadySettled, Is.True); // 기존 영수증 재사용
            Assert.That(store.Data, Is.EqualTo(persisted)); // 재조회 중 저장 변경 없음
            Assert.That(host.Progress.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 중복 숙련도 방지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            DestroyHost(holder); // 시험 객체와 저장소 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 포기 정산 실패 시험
    public void PauseRetryCannotDiscardBattleWhenSettlementFails() // 저장 실패 시 재출격 차단
    { // 시험 시작
        GameObject holder = new GameObject("RetryHost"); // 시험 객체 생성
        float previousScale = Time.timeScale; // 시간 배율 보관
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시간 배율 초기화
            MemoryStore store = new MemoryStore(); // 메모리 저장소 생성
            GameSessionHost host = CreateHost(holder, store); // 화면 관리자 생성
            Prepare(host); // 출격 준비
            Assert.That(host.Flow.StartRun(), Is.True); // 첫 출격
            BattlePlayer first = host.CurrentPlayer; // 첫 출격 객체
            store.FailWrites = true; // 저장 실패 유도
            Assert.That(host.Flow.RetryBattle(), Is.False); // 재출격 거부
            Assert.That(host.Flow.SettlementPending, Is.True); // 정산 재시도 대기
            Assert.That(host.CurrentPlayer, Is.SameAs(first)); // 이전 전투 유지
            store.FailWrites = false; // 저장 정상화
            Assert.That(host.Flow.RetrySettlement(), Is.True); // 정산과 재출격 진행
            Assert.That(host.CurrentPlayer, Is.Not.SameAs(first)); // 새 전투 생성
            Assert.That(host.Progress.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 첫 출격 참여 보상
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            DestroyHost(holder); // 주입 서비스와 시험 객체 제거
            Time.timeScale = previousScale; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 출격 시작 저장 실패 시험
    public void FailedStartWriteAllowsSameRequestToRetry() // 시작 기록 실패와 재시도
    { // 시험 시작
        GameObject holder = new GameObject("StartFailureHost"); // 시험 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            MemoryStore store = new MemoryStore(); // 메모리 저장소 생성
            GameSessionHost host = CreateHost(holder, store); // 화면 관리자 생성
            Prepare(host); // 출격 준비
            store.FailWrites = true; // 시작 기록 실패 유도
            Assert.That(host.Flow.StartRun(), Is.False); // 출격 차단
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 준비 화면 유지
            Assert.That(host.CurrentPlayer, Is.Null); // 전투 객체 생성 차단
            Assert.That(host.Flow.ErrorText, Is.Not.Empty); // 오류 안내 표시
            Assert.That(store.Data, Is.Null); // 기존 기록 덮어쓰기 차단
            store.FailWrites = false; // 저장 오류 해제
            Assert.That(host.Flow.StartRun(), Is.True); // 같은 시작 요청 재시도
            Assert.That(host.CurrentPlayer, Is.Not.Null); // 저장 성공 후 전투 생성
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            DestroyHost(holder); // 주입 서비스와 시험 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 중단 출격 복구 중복 방지 시험
    public void InterruptedRunIsRecoveredOnceAndAnnounced() // 미정산 복구와 재로딩 확인
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리된 저장소 생성
        PermanentProgressService prior = new PermanentProgressService(store); // 이전 세션 구성
        Assert.That(prior.Load(out string error), Is.True, error); // 기존 저장 로드
        Assert.That(prior.TryBegin("interrupted-run", CharacterType.Sniper, out error), Is.True, error); // 중단된 출격 기록
        GameObject first = new GameObject("RecoveryHost"); // 첫 복구 메뉴
        GameObject second = new GameObject("ReloadHost"); // 재시작 메뉴
        try // 시험 시작
        { // 시험 범위 시작
            GameSessionHost restored = CreateHost(first, store); // 중단 출격 자동 복구
            Assert.That(restored.Progress.Mastery(CharacterType.Sniper), Is.EqualTo(10)); // 실패 참여 숙련도
            Assert.That(restored.Progress.CommonMaterials, Is.EqualTo(3)); // 실패 참여 공통 재료
            Assert.That(restored.Flow.RecoveryNotice, Does.Contain("숙련도 +10")); // 실제 복구 내역 안내
            GameSessionHost reloaded = CreateHost(second, store); // 저장 결과 재로딩
            Assert.That(reloaded.Progress.Mastery(CharacterType.Sniper), Is.EqualTo(10)); // 중복 숙련도 방지
            Assert.That(reloaded.Progress.CommonMaterials, Is.EqualTo(3)); // 중복 재료 방지
            Assert.That(reloaded.Flow.RecoveryNotice, Is.Empty); // 재복구 안내 없음
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            DestroyHost(first); // 첫 메뉴와 서비스 제거
            DestroyHost(second); // 둘째 메뉴와 서비스 제거
            prior.Dispose(); // 이전 시험 서비스 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 중단 출격 복구 실패 시험
    public void RecoveryWriteFailureBlocksStartUntilStorageRecovers() // 복구 실패 시 원본 보존
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리된 저장소 생성
        PermanentProgressService prior = new PermanentProgressService(store); // 이전 세션 구성
        Assert.That(prior.Load(out string error), Is.True, error); // 기존 저장 로드
        Assert.That(prior.TryBegin("pending-run", CharacterType.Sniper, out error), Is.True, error); // 미정산 출격 기록
        string original = store.Data; // 이전 저장 내용 보관
        store.FailWrites = true; // 복구 저장 실패 유도
        GameObject holder = new GameObject("RecoveryFailureHost"); // 시험 메뉴 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameSessionHost host = CreateHost(holder, store); // 실패하는 복구 시도
            Prepare(host); // 출격 준비
            Assert.That(host.Flow.StartRun(), Is.False); // 새 출격 차단
            Assert.That(host.CurrentPlayer, Is.Null); // 전투 객체 없음
            Assert.That(store.Data, Is.EqualTo(original)); // 미정산 원본 보존
            Assert.That(host.Flow.ErrorText, Is.Not.Empty); // 복구 오류 안내
            store.FailWrites = false; // 저장 오류 해제
            Assert.That(host.Flow.StartRun(), Is.True); // 복구 후 새 출격 시작
            Assert.That(host.Progress.Mastery(CharacterType.Sniper), Is.EqualTo(10)); // 중단 출격 참여 보상
            Assert.That(host.Flow.RecoveryNotice, Does.Contain("숙련도 +10")); // 복구 내용 안내
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            DestroyHost(holder); // 주입 서비스와 시험 객체 제거
            prior.Dispose(); // 이전 시험 서비스 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 읽기 실패 차단 시험
    public void UnreadableProgressPreventsBattleStart() // 기존 저장 오류 보호
    { // 시험 시작
        GameObject holder = new GameObject("ReadFailureHost"); // 시험 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            MemoryStore store = new MemoryStore(); // 메모리 저장소 생성
            store.FailReads = true; // 저장 읽기 실패 유도
            GameSessionHost host = CreateHost(holder, store); // 화면 관리자 생성
            Prepare(host); // 출격 준비
            Assert.That(host.Flow.StartRun(), Is.False); // 출격 시작 거부
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 준비 화면 유지
            Assert.That(host.CurrentPlayer, Is.Null); // 전투 생성 차단
            Assert.That(host.Flow.ErrorText, Is.Not.Empty); // 오류 사유 표시
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            DestroyHost(holder); // 주입 서비스와 시험 객체 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
