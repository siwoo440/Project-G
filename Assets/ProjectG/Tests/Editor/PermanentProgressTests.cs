using System; // 고유 식별자 기능
using System.Collections.Generic; // 해금 후보 목록
using System.IO; // 격리된 파일 시험
using System.Text; // 큰 저장 경계 원문 생성
using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 영구 성장 기능
public class PermanentProgressTests // 영구 성장 시험
{ // 시험 시작
    private sealed class MemoryStore : IProgressStorage // 격리된 메모리 저장소
    { // 저장소 시작
        public string Data; // 저장된 원문
        public bool FailRead; // 읽기 실패 지정
        public bool FailWrite; // 쓰기 실패 지정
        public int Writes; // 성공한 쓰기 횟수
        public ProgressReadStatus Read(out string data, out string error) // 저장 데이터 조회
        { // 읽기 시작
            data = Data; // 이전 원문 반환
            error = FailRead ? "읽기 실패" : null; // 실패 설명
            return FailRead ? ProgressReadStatus.Error : Data == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 조회 상태
        } // 읽기 종료
        public bool Write(string expectedData, string data, out string error) // 이전 원문 비교 후 저장
        { // 쓰기 시작
            error = FailWrite ? "쓰기 실패" : null; // 실패 설명
            if (!string.Equals(Data, expectedData, StringComparison.Ordinal)) // 기존 원문 일치 확인
            { // 저장 충돌 시작
                error = "원문 충돌"; // 충돌 오류 설명
                return false; // 다른 기록 보존
            } // 저장 충돌 종료
            if (FailWrite) // 실패 조건 확인
            { // 실패 시작
                return false; // 원문 유지
            } // 실패 종료
            Data = data; // 전체 스냅샷 보관
            Writes++; // 성공 횟수 증가
            return true; // 저장 완료
        } // 쓰기 종료
    } // 저장소 종료
    private sealed class TestCatalog : IProgressUnlockCatalog // 격리된 시험용 해금 목록
    { // 시험 목록 시작
        public bool TryGet(CharacterType character, string optionId, out ProgressUnlockDefinition definition) // 확정된 시험 항목 조회
        { // 조회 시작
            definition = null; // 결과 초기화
            if (character == CharacterType.Rapid && optionId == "sample") // 첫 시험 항목 확인
            { // 첫 항목 시작
                definition = new ProgressUnlockDefinition(character, optionId, null, 18, 1); // 첫 해금 시험 비용
                return true; // 시험 항목 확인
            } // 첫 항목 종료
            if (character == CharacterType.Rapid && optionId == "later") // 후속 시험 항목 확인
            { // 후속 항목 시작
                definition = new ProgressUnlockDefinition(character, optionId, "sample", 2, 0); // 선행과 별도 시험 비용
                return true; // 시험 항목 확인
            } // 후속 항목 종료
            return false; // 미정 항목 차단
        } // 조회 종료
    } // 시험 목록 종료
    [Test] // 정산과 재실행 시험
    public void SettlementPersistsAndRepeatedRunIdCannotPayTwice() // 출격별 중복 방지 확인
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 성장 서비스 생성
        Assert.That(service.Load(out string error), Is.True, error); // 빈 저장소 로드
        Assert.That(service.TryBegin("run-a", CharacterType.Rapid, out error), Is.True, error); // 속사형 출격 시작
        Assert.That(service.TrySettle("run-a", CharacterType.Rapid, false, out SettlementReceipt first, out error), Is.True, error); // 실패 보상 지급
        Assert.That(first.MasteryGained, Is.EqualTo(10)); // 실패 숙련도
        Assert.That(first.CommonMaterialsGained, Is.EqualTo(3)); // 실패 공통 재료
        Assert.That(first.CharacterMaterialsGained, Is.Zero); // 전용 재료 경계 전
        Assert.That(first.AlreadySettled, Is.False); // 최초 지급 상태
        Assert.That(service.TryBegin("run-b", CharacterType.Sniper, out error), Is.True, error); // 저격형 출격 시작
        Assert.That(service.TrySettle("run-b", CharacterType.Sniper, true, out SettlementReceipt second, out error), Is.True, error); // 클리어 보상 지급
        Assert.That(second.MasteryGained, Is.EqualTo(20)); // 클리어 숙련도
        Assert.That(second.CommonMaterialsGained, Is.EqualTo(6)); // 클리어 공통 재료
        PermanentProgressService restored = new PermanentProgressService(store); // 재실행 서비스
        Assert.That(restored.Load(out error), Is.True, error); // 저장 복원
        Assert.That(restored.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 속사형 독립 숙련도
        Assert.That(restored.Mastery(CharacterType.Sniper), Is.EqualTo(20)); // 저격형 독립 숙련도
        Assert.That(restored.CommonMaterials, Is.EqualTo(9)); // 공통 재료 누적
        int writes = store.Writes; // 직전 저장 횟수
        Assert.That(restored.TrySettle("run-a", CharacterType.Rapid, false, out SettlementReceipt duplicate, out error), Is.True, error); // 같은 출격 재확인
        Assert.That(duplicate.AlreadySettled, Is.True); // 중복 지급 구분
        Assert.That(duplicate.MasteryGained, Is.EqualTo(10)); // 원래 보상 내역
        Assert.That(store.Writes, Is.EqualTo(writes)); // 중복 쓰기 방지
        Assert.That(restored.CommonMaterials, Is.EqualTo(9)); // 공통 재료 중복 차단
        Assert.That(restored.TrySettle("run-a", CharacterType.Sniper, false, out _, out error), Is.False); // 캐릭터 불일치 차단
        Assert.That(restored.TrySettle("run-a", CharacterType.Rapid, true, out _, out error), Is.False); // 결과 불일치 차단
        Assert.That(store.Writes, Is.EqualTo(writes)); // 불일치 데이터 보존
    } // 시험 종료
    [Test] // 숙련도 경계 시험
    public void MaterialThresholdUsesEachCharactersOwnMastery() // 캐릭터별 경계 계산 확인
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 성장 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 초기 상태 로드
        for (int index = 0; index < 5; index++) // 속사형 실패 다섯 회
        { // 지급 반복 시작
            Assert.That(service.TryBegin("rapid-" + index, CharacterType.Rapid, out error), Is.True, error); // 해당 출격 시작
            Assert.That(service.TrySettle("rapid-" + index, CharacterType.Rapid, false, out _, out error), Is.True, error); // 회차 정산
        } // 지급 반복 종료
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 60 미만 보상 없음
        Assert.That(service.TryBegin("sniper", CharacterType.Sniper, out error), Is.True, error); // 저격형 출격 시작
        Assert.That(service.TrySettle("sniper", CharacterType.Sniper, true, out _, out error), Is.True, error); // 다른 캐릭터 정산
        Assert.That(service.CharacterMaterials(CharacterType.Sniper), Is.Zero); // 캐릭터 간 경계 분리
        Assert.That(service.TryBegin("rapid-final", CharacterType.Rapid, out error), Is.True, error); // 속사형 출격 시작
        Assert.That(service.TrySettle("rapid-final", CharacterType.Rapid, true, out SettlementReceipt receipt, out error), Is.True, error); // 50에서 70으로 증가
        Assert.That(receipt.CharacterMaterialsGained, Is.EqualTo(1)); // 경계 통과 보상
        Assert.That(service.Mastery(CharacterType.Rapid), Is.EqualTo(70)); // 누적 숙련도
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 전용 재료 누적
    } // 시험 종료
    [Test] // 저장 실패와 재시도 시험
    public void FailedWriteDoesNotCommitMemoryOrOverwriteStoredSnapshot() // 실패 시 이전 상태 보존
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 성장 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 초기 로드
        Assert.That(service.TryBegin("first", CharacterType.Rapid, out error), Is.True, error); // 첫 출격 시작
        Assert.That(service.TrySettle("first", CharacterType.Rapid, false, out _, out error), Is.True, error); // 기준 스냅샷 생성
        Assert.That(service.TryBegin("second", CharacterType.Rapid, out error), Is.True, error); // 다음 출격 시작
        string previous = store.Data; // 이전 원문 보관
        store.FailWrite = true; // 저장 장애 설정
        Assert.That(service.TrySettle("second", CharacterType.Rapid, true, out _, out error), Is.False); // 새 정산 실패
        Assert.That(error, Is.Not.Null.And.Not.Empty); // 실패 원인 표시
        Assert.That(service.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 메모리 상태 보존
        Assert.That(service.CommonMaterials, Is.EqualTo(3)); // 공통 재료 보존
        Assert.That(store.Data, Is.EqualTo(previous)); // 이전 저장 데이터 보존
        store.FailWrite = false; // 저장 장애 해제
        Assert.That(service.TrySettle("second", CharacterType.Rapid, true, out SettlementReceipt retry, out error), Is.True, error); // 동일 출격 재시도
        Assert.That(retry.AlreadySettled, Is.False); // 실패한 정산 미반영
        Assert.That(service.Mastery(CharacterType.Rapid), Is.EqualTo(30)); // 정확한 재시도 결과
    } // 시험 종료
    [Test] // 손상과 읽기 오류 시험
    public void InvalidOrUnreadableSaveBlocksWritesWithoutResettingOriginal() // 손상 데이터 보존 확인
    { // 시험 시작
        foreach (string corrupt in new string[] // 잘못된 저장 목록
        { // 목록 시작
            "not-json", // 잘못된 원문
            "{\"schemaVersion\":999}", // 지원하지 않는 형식
            "{\"schemaVersion\":1,\"commonMaterials\":-3}" // 잘못된 재료 수
        }) // 목록 종료
        { // 손상 시험 시작
            MemoryStore store = new MemoryStore(); // 격리 저장소
            store.Data = corrupt; // 원본 데이터 주입
            PermanentProgressService service = new PermanentProgressService(store); // 성장 서비스
            Assert.That(service.Load(out string error), Is.False); // 손상 로드 차단
            Assert.That(error, Is.Not.Null.And.Not.Empty); // 오류 설명 제공
            Assert.That(service.TrySettle("new", CharacterType.Rapid, false, out _, out error), Is.False); // 손상 상태 쓰기 차단
            Assert.That(store.Data, Is.EqualTo(corrupt)); // 원본 덮어쓰기 차단
            Assert.That(store.Writes, Is.Zero); // 저장 시도 없음
        } // 손상 시험 종료
        MemoryStore unreadable = new MemoryStore(); // 읽기 오류 저장소
        unreadable.Data = "원본"; // 원본 데이터 보관
        unreadable.FailRead = true; // 읽기 장애 설정
        PermanentProgressService blocked = new PermanentProgressService(unreadable); // 성장 서비스
        Assert.That(blocked.Load(out string readError), Is.False); // 읽기 실패 차단
        Assert.That(blocked.TrySettle("new", CharacterType.Rapid, false, out _, out readError), Is.False); // 읽기 실패 후 쓰기 차단
        Assert.That(unreadable.Data, Is.EqualTo("원본")); // 원본 유지
    } // 시험 종료
    [Test] // 해금 비용과 권한 시험
    public void UnlockRequiresPermittedOptionAndPersistsOneTimeCost() // 허용 후보와 재료 차감 확인
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService service = new PermanentProgressService(store, new TestCatalog()); // 시험용 확정 목록 연결
        Assert.That(service.Load(out string error), Is.True, error); // 초기 로드
        for (int index = 0; index < 6; index++) // 첫 해금 재료 축적
        { // 지급 반복 시작
            Assert.That(service.TryBegin("earn-" + index, CharacterType.Rapid, out error), Is.True, error); // 해당 출격 시작
            Assert.That(service.TrySettle("earn-" + index, CharacterType.Rapid, false, out _, out error), Is.True, error); // 실패 참여 보상
        } // 지급 반복 종료
        PermanentProgressService production = new PermanentProgressService(store); // 빈 실서비스 목록
        Assert.That(production.Load(out error), Is.True, error); // 재료 상태 복원
        Assert.That(production.TryUnlock(CharacterType.Rapid, "sample", out error), Is.False); // 임의 후보 차단
        Assert.That(service.TryUnlock(CharacterType.Rapid, "later", out error), Is.False); // 선행 조건 미충족 차단
        Assert.That(service.CommonMaterials, Is.EqualTo(18)); // 실패 시 비용 보존
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 전용 재료 보존
        Assert.That(service.TryUnlock(CharacterType.Sniper, "sample", out error), Is.False); // 다른 캐릭터 후보 차단
        store.FailWrite = true; // 저장 장애 설정
        Assert.That(service.TryUnlock(CharacterType.Rapid, "sample", out error), Is.False); // 저장 실패 해금 차단
        Assert.That(service.CommonMaterials, Is.EqualTo(18)); // 비용 복원 확인
        Assert.That(service.IsUnlocked(CharacterType.Rapid, "sample"), Is.False); // 해금 미적용
        store.FailWrite = false; // 저장 장애 해제
        Assert.That(service.TryUnlock(CharacterType.Rapid, "sample", out error), Is.True, error); // 승인 후보 직접 해금
        Assert.That(service.CommonMaterials, Is.Zero); // 공통 재료 차감
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 전용 재료 차감
        Assert.That(service.TryUnlock(CharacterType.Rapid, "sample", out error), Is.False); // 중복 해금 차단
        PermanentProgressService restored = new PermanentProgressService(store); // 재실행 서비스
        Assert.That(restored.Load(out error), Is.True, error); // 저장 복원
        Assert.That(restored.IsUnlocked(CharacterType.Rapid, "sample"), Is.True); // 해금 상태 유지
        Assert.That(restored.IsUnlocked(CharacterType.Sniper, "sample"), Is.False); // 캐릭터 간 해금 분리
    } // 시험 종료
    [Test] // 시작 기록과 저장 실패 시험
    public void PendingRunBlocksNewRunsAndCannotSettleWithoutMatchingStart() // 출격 식별자 검증
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 성장 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 초기 로드
        Assert.That(service.TrySettle("missing", CharacterType.Rapid, false, out _, out error), Is.False); // 시작 없는 정산 차단
        store.FailWrite = true; // 시작 저장 실패 지정
        Assert.That(service.TryBegin("one", CharacterType.Rapid, out error), Is.False); // 저장 실패 시 출격 차단
        Assert.That(store.Data, Is.Null); // 실패한 시작 미저장
        store.FailWrite = false; // 저장 오류 해제
        Assert.That(service.TryBegin("one", CharacterType.Rapid, out error), Is.True, error); // 시작 기록 저장
        Assert.That(service.TryBegin("one", CharacterType.Rapid, out error), Is.True, error); // 같은 출격 시작 멱등 확인
        string pending = store.Data; // 진행 중 출격 기록
        Assert.That(service.TryBegin("two", CharacterType.Sniper, out error), Is.False); // 미정산 중 새 출격 차단
        Assert.That(service.TrySettle("one", CharacterType.Sniper, false, out _, out error), Is.False); // 다른 캐릭터 정산 차단
        Assert.That(service.TrySettle("two", CharacterType.Rapid, false, out _, out error), Is.False); // 다른 식별자 정산 차단
        Assert.That(store.Data, Is.EqualTo(pending)); // 진행 기록 보존
        PermanentProgressService restored = new PermanentProgressService(store); // 재실행 서비스
        Assert.That(restored.Load(out error), Is.True, error); // 진행 기록 복원
        Assert.That(restored.TryBegin("two", CharacterType.Sniper, out error), Is.False); // 미정산 중 재시작 차단
        Assert.That(restored.TrySettle("one", CharacterType.Rapid, false, out _, out error), Is.True, error); // 원래 출격 정산
        Assert.That(restored.TryBegin("one", CharacterType.Rapid, out error), Is.False); // 정산된 출격 재시작 차단
        Assert.That(restored.TryBegin("two", CharacterType.Sniper, out error), Is.True, error); // 정산 후 새 출격 허용
    } // 시험 종료
    [Test] // 중단 출격 복구 시험
    public void RecoveryPaysOneFailureAndPreservesPendingOnWriteFailure() // 중단 출격의 안전한 실패 정산
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService original = new PermanentProgressService(store); // 첫 실행 서비스
        Assert.That(original.Load(out string error), Is.True, error); // 초기 로드
        Assert.That(original.TryBegin("interrupted", CharacterType.Sniper, out error), Is.True, error); // 출격 시작 저장
        string pending = store.Data; // 미정산 출격 원문
        PermanentProgressService restored = new PermanentProgressService(store); // 다음 실행 서비스
        Assert.That(restored.Load(out error), Is.True, error); // 진행 중 출격 복원
        store.FailWrite = true; // 정산 저장 실패
        Assert.That(restored.TryRecoverPending(out SettlementReceipt failed, out error), Is.False); // 복구 실패 확인
        Assert.That(failed, Is.Null); // 지급 완료 미표시
        Assert.That(store.Data, Is.EqualTo(pending)); // 미정산 원문 보존
        store.FailWrite = false; // 저장 장애 해제
        Assert.That(restored.Load(out error), Is.True, error); // 미정산 기록 다시 복원
        Assert.That(restored.TryRecoverPending(out SettlementReceipt receipt, out error), Is.True, error); // 실패 참여 보상 복구
        Assert.That(receipt.Character, Is.EqualTo(CharacterType.Sniper)); // 원래 캐릭터 보상
        Assert.That(receipt.Cleared, Is.False); // 중단 출격 실패 처리
        Assert.That(restored.Mastery(CharacterType.Sniper), Is.EqualTo(10)); // 참여 숙련도 한 번 지급
        Assert.That(restored.CommonMaterials, Is.EqualTo(3)); // 참여 재료 한 번 지급
        Assert.That(restored.TryRecoverPending(out SettlementReceipt none, out error), Is.True, error); // 복구 후 재호출
        Assert.That(none, Is.Null); // 중복 영수증 없음
        Assert.That(restored.CommonMaterials, Is.EqualTo(3)); // 중복 지급 없음
    } // 시험 종료
    [Test] // 영수증 필드와 타입 손상 시험
    public void CorruptReceiptMissingOrWrongFieldsCannotOverwriteOriginal() // 불완전한 정산 기록 차단
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 성장 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 초기 로드
        Assert.That(service.TryBegin("receipt", CharacterType.Rapid, out error), Is.True, error); // 시험 출격 시작
        Assert.That(service.TrySettle("receipt", CharacterType.Rapid, false, out _, out error), Is.True, error); // 시험 정산 저장
        string valid = store.Data; // 정상 저장 원문
        string[] corruptions = new string[] // 손상 원문 목록
        { // 목록 시작
            valid.Replace("\"cleared\":false,", ""), // 클리어 필드 누락
            valid.Replace("\"characterMaterialsGained\":0", "\"characterMaterialsGained\":\"0\""), // 재료 수치 문자열 변조
            valid.Replace("\"character\":0,", ""), // 캐릭터 필드 누락
            valid.Replace("\"runId\":\"receipt\",", "\"runId\":\"receipt\",\"runId\":\"receipt\","), // 영수증 키 중복
            valid.Replace("\"rapidMastery\":10", "\"rapidMastery\":\"10\"") // 최상위 숫자 타입 변조
        }; // 손상 목록 종료
        foreach (string corrupt in corruptions) // 손상별 검사
        { // 검사 시작
            Assert.That(corrupt, Is.Not.EqualTo(valid)); // 실제 변조 여부 확인
            store.Data = corrupt; // 손상 원문 주입
            int writes = store.Writes; // 기존 쓰기 횟수
            PermanentProgressService blocked = new PermanentProgressService(store); // 재실행 서비스
            Assert.That(blocked.Load(out error), Is.False); // 손상 저장 차단
            Assert.That(blocked.TryBegin("new", CharacterType.Rapid, out error), Is.False); // 잘못된 상태 출격 차단
            Assert.That(store.Data, Is.EqualTo(corrupt)); // 손상 원본 보존
            Assert.That(store.Writes, Is.EqualTo(writes)); // 쓰기 미발생
        } // 검사 종료
    } // 시험 종료
    [Test] // 오래된 서비스의 동시 저장 충돌 시험
    public void SeparateServicesCannotOverwriteNewerSettlement() // 이전 상태 기반 덮어쓰기 차단
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 공동 저장소
        PermanentProgressService first = new PermanentProgressService(store); // 첫 서비스
        PermanentProgressService stale = new PermanentProgressService(store); // 이전 상태 서비스
        Assert.That(first.Load(out string error), Is.True, error); // 첫 초기 로드
        Assert.That(stale.Load(out error), Is.True, error); // 같은 상태 로드
        Assert.That(first.TryBegin("one", CharacterType.Rapid, out error), Is.True, error); // 첫 출격 저장
        string latest = store.Data; // 최신 원문 기록
        Assert.That(stale.TryBegin("two", CharacterType.Sniper, out error), Is.False); // 오래된 시작 저장 차단
        Assert.That(store.Data, Is.EqualTo(latest)); // 최신 출격 기록 유지
        Assert.That(first.TrySettle("one", CharacterType.Rapid, true, out _, out error), Is.True, error); // 첫 출격 정산
        latest = store.Data; // 최신 정산 원문
        Assert.That(stale.TryBegin("three", CharacterType.Rapid, out error), Is.False); // 이전 상태의 정산 덮기 차단
        Assert.That(store.Data, Is.EqualTo(latest)); // 완료 영수증 보존
        Assert.That(stale.Load(out error), Is.True, error); // 최신 상태 다시 로드
        Assert.That(stale.Mastery(CharacterType.Rapid), Is.EqualTo(20)); // 정산된 숙련도 유지
        Assert.That(stale.TryBegin("two", CharacterType.Sniper, out error), Is.True, error); // 최신 상태에서 새 출격
    } // 시험 종료
    [Test] // 보상 숫자 범위 시험
    public void OverflowingRewardValuesAreRejectedWithoutWriting() // 범위 초과 원본 보존
    { // 시험 시작
        foreach (string field in new string[] // 범위 시험 필드
        { // 목록 시작
            "rapidMastery", "commonMaterials", "rapidMaterials" // 숙련도와 재료
        }) // 목록 종료
        { // 필드 시험 시작
            MemoryStore store = new MemoryStore(); // 격리 저장소
            PermanentProgressService service = new PermanentProgressService(store); // 성장 서비스
            Assert.That(service.Load(out string error), Is.True, error); // 초기 로드
            Assert.That(service.TryBegin("limit", CharacterType.Rapid, out error), Is.True, error); // 출격 시작
            string original = store.Data; // 원래 저장 원문
            string modified = original.Replace("\"" + field + "\":0", "\"" + field + "\":2147483647"); // 선택 필드 최대값 설정
            if (field == "rapidMaterials") // 전용 재료 경계 확인
            { // 경계 준비 시작
                modified = modified.Replace("\"rapidMastery\":0", "\"rapidMastery\":50"); // 다음 출격에서 재료 지급
            } // 경계 준비 종료
            Assert.That(modified, Is.Not.EqualTo(original)); // 원문 변경 확인
            store.Data = modified; // 유효한 범위 경계 주입
            PermanentProgressService loaded = new PermanentProgressService(store); // 경계 상태 서비스
            Assert.That(loaded.Load(out error), Is.True, error); // 최대값 복원
            Assert.That(loaded.TrySettle("limit", CharacterType.Rapid, false, out _, out error), Is.False); // 범위 초과 정산 차단
            Assert.That(store.Data, Is.EqualTo(modified)); // 원본 데이터 보존
        } // 필드 시험 종료
    } // 시험 종료
    private static string BoundaryReceipt(int index, int idLength) // 고유한 경계 시험 영수증 생성
    { // 생성 시작
        string id = index.ToString("D6") + new string('x', idLength - 6); // 길이를 조절한 고유 식별자
        return ",{\"runId\":\"" + id + "\",\"character\":0,\"cleared\":false,\"masteryGained\":10,\"commonMaterialsGained\":3,\"characterMaterialsGained\":0}"; // 정수와 불리언 영수증
    } // 생성 종료
    [Test] // 저장 크기 경계 시험
    public void SnapshotCannotGrowBeyondTheRestorableLimit() // 복원 불가 크기의 저장 차단
    { // 시험 시작
        const int limit = 4194304; // 복원 가능한 최대 문자 수
        string prefix = "{\"schemaVersion\":1,\"commonMaterials\":0,\"rapidMastery\":0,\"sniperMastery\":0,\"rapidMaterials\":0,\"sniperMaterials\":0,\"activeRunId\":\"pending\",\"activeCharacter\":0,\"rapidUnlocks\":[],\"sniperUnlocks\":[],\"receipts\":["; // 유효한 스냅샷 시작
        string suffix = "]}"; // 유효한 스냅샷 끝
        StringBuilder data = new StringBuilder(prefix); // 큰 스냅샷 생성기
        data.Append(BoundaryReceipt(0, 128).Substring(1)); // 첫 고유 영수증
        int index = 1; // 다음 영수증 번호
        int maximum = BoundaryReceipt(1, 128).Length; // 가장 긴 영수증 길이
        int minimum = BoundaryReceipt(1, 6).Length; // 가장 짧은 영수증 길이
        int remaining = limit - 30 - data.Length - suffix.Length; // 목표까지 남은 공간
        while (remaining > maximum * 2) // 경계 근처까지 반복
        { // 영수증 추가 시작
            data.Append(BoundaryReceipt(index++, 128)); // 가장 긴 고유 영수증
            remaining = limit - 30 - data.Length - suffix.Length; // 남은 공간 갱신
        } // 영수증 추가 종료
        if (remaining >= minimum * 2) // 두 영수증으로 채울 공간 확인
        { // 길이 조정 시작
            int firstLength = Math.Min(maximum, remaining - minimum); // 첫 영수증 실제 길이
            data.Append(BoundaryReceipt(index++, 6 + firstLength - minimum)); // 길이를 맞춘 첫 영수증
            remaining = limit - 30 - data.Length - suffix.Length; // 남은 공간 갱신
            data.Append(BoundaryReceipt(index++, 6 + remaining - minimum)); // 남은 공간을 채운 영수증
        } // 길이 조정 종료
        else if (remaining > maximum) // 한 영수증을 채울 공간 확인
        { // 긴 영수증 추가 시작
            data.Append(BoundaryReceipt(index++, 128)); // 경계에 가까운 영수증
        } // 긴 영수증 추가 종료
        data.Append(suffix); // 전체 스냅샷 완성
        Assert.That(data.Length, Is.LessThanOrEqualTo(limit)); // 기존 원문의 복원 크기
        Assert.That(data.Length, Is.GreaterThan(limit - 130)); // 새 영수증이 초과할 경계
        MemoryStore store = new MemoryStore(); // 격리 저장소
        store.Data = data.ToString(); // 유효한 경계 원문 주입
        PermanentProgressService service = new PermanentProgressService(store); // 성장 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 경계 원문 복원
        int writes = store.Writes; // 이전 쓰기 횟수
        Assert.That(service.TrySettle("pending", CharacterType.Rapid, false, out _, out error), Is.False); // 초과하는 정산 저장 차단
        Assert.That(error, Is.Not.Null.And.Not.Empty); // 저장 불가 안내
        Assert.That(store.Data, Is.EqualTo(data.ToString())); // 기존 원문 보존
        Assert.That(store.Writes, Is.EqualTo(writes)); // 잘못된 저장 미발생
        Assert.That(service.Mastery(CharacterType.Rapid), Is.Zero); // 메모리 보상 미반영
    } // 시험 종료
    [Test] // 실제 파일 세션 소유 시험
    public void ActiveSessionCannotBeRecoveredByAnotherInstance() // 살아 있는 출격 복구 차단
    { // 시험 시작
        string path = Path.Combine(Path.GetTempPath(), "projectg-session-test-" + Guid.NewGuid().ToString("N") + ".json"); // 고유 시험 파일
        PermanentProgressService owner = new PermanentProgressService(new FileProgressStorage(path)); // 첫 게임 서비스
        PermanentProgressService other = new PermanentProgressService(new FileProgressStorage(path)); // 다른 게임 서비스
        try // 시험 범위 시작
        { // 잠금 시험 시작
            Assert.That(owner.Load(out string error), Is.True, error); // 첫 게임의 세션 소유
            Assert.That(owner.TryBegin("live", CharacterType.Rapid, out error), Is.True, error); // 첫 게임 출격 시작
            string original = File.ReadAllText(path); // 진행 중 출격 원문
            Assert.That(other.Load(out error), Is.False); // 살아 있는 게임의 세션 잠금
            Assert.That(other.TryRecoverPending(out _, out error), Is.False); // 다른 게임의 잘못된 복구 차단
            Assert.That(File.ReadAllText(path), Is.EqualTo(original)); // 살아 있는 출격 기록 보존
            owner.Dispose(); // 첫 게임 종료와 잠금 해제
            Assert.That(other.Load(out error), Is.True, error); // 다음 게임 세션 소유
            Assert.That(other.TryRecoverPending(out SettlementReceipt receipt, out error), Is.True, error); // 중단 출격 실패 복구
            Assert.That(receipt.MasteryGained, Is.EqualTo(10)); // 참여 숙련도 지급
            Assert.That(other.TryRecoverPending(out receipt, out error), Is.True, error); // 중복 복구 확인
            Assert.That(receipt, Is.Null); // 두 번째 지급 없음
        } // 잠금 시험 종료
        finally // 시험 정리 시작
        { // 정리 범위 시작
            owner.Dispose(); // 첫 세션 안전 해제
            other.Dispose(); // 다음 세션 안전 해제
            if (File.Exists(path)) // 시험 데이터 파일 확인
            { // 삭제 시작
                File.Delete(path); // 해당 시험 데이터만 삭제
            } // 삭제 종료
            if (File.Exists(path + ".lock")) // 시험 교체 잠금 확인
            { // 삭제 시작
                File.Delete(path + ".lock"); // 해당 시험 잠금만 삭제
            } // 삭제 종료
            if (File.Exists(path + ".session.lock")) // 시험 세션 잠금 확인
            { // 삭제 시작
                File.Delete(path + ".session.lock"); // 해당 시험 세션만 삭제
            } // 삭제 종료
        } // 정리 종료
    } // 시험 종료
    [Test] // 잘못된 UTF-8 보존 시험
    public void InvalidUtf8IsRejectedAndOriginalBytesRemain() // 손상 파일 원본 보존
    { // 시험 시작
        string path = Path.Combine(Path.GetTempPath(), "projectg-invalid-test-" + Guid.NewGuid().ToString("N") + ".json"); // 고유 시험 파일
        byte[] corrupted = new byte[] // 잘못된 UTF-8 바이트
        { // 바이트 목록 시작
            0x7b, 0x22, 0xff, 0x22, 0x3a, 0x30, 0x7d // 손상된 객체 원문
        }; // 바이트 목록 종료
        PermanentProgressService service = new PermanentProgressService(new FileProgressStorage(path)); // 격리 파일 서비스
        try // 손상 시험 시작
        { // 시험 범위 시작
            File.WriteAllBytes(path, corrupted); // 시험 파일에 손상 바이트 기록
            Assert.That(service.Load(out string error), Is.False); // 잘못된 UTF-8 거부
            Assert.That(service.TryBegin("new", CharacterType.Rapid, out error), Is.False); // 손상 상태 쓰기 차단
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(corrupted)); // 원본 바이트 보존
        } // 시험 범위 종료
        finally // 시험 정리 시작
        { // 정리 범위 시작
            service.Dispose(); // 시험 세션 잠금 해제
            if (File.Exists(path)) // 시험 파일 확인
            { // 삭제 시작
                File.Delete(path); // 해당 시험 파일만 삭제
            } // 삭제 종료
            if (File.Exists(path + ".session.lock")) // 시험 세션 잠금 확인
            { // 삭제 시작
                File.Delete(path + ".session.lock"); // 해당 시험 잠금만 삭제
            } // 삭제 종료
        } // 정리 종료
    } // 시험 종료
    [Test] // 실제 파일 직렬화 시험
    public void FileStorageWritesAndRestoresOnlyItsIsolatedFile() // 파일 저장과 복원 확인
    { // 시험 시작
        string path = Path.Combine(Path.GetTempPath(), "projectg-progress-test-" + Guid.NewGuid().ToString("N") + ".json"); // 고유 임시 파일
        PermanentProgressService service = null; // 첫 시험 서비스
        PermanentProgressService restored = null; // 복원 시험 서비스
        try // 파일 시험 시작
        { // 시험 범위 시작
            service = new PermanentProgressService(new FileProgressStorage(path)); // 임시 경로 저장소
            Assert.That(service.Load(out string error), Is.True, error); // 없는 파일 초기화
            Assert.That(service.TryBegin("file-run", CharacterType.Sniper, out error), Is.True, error); // 해당 출격 시작
            Assert.That(service.TrySettle("file-run", CharacterType.Sniper, true, out _, out error), Is.True, error); // 실제 파일 저장
            service.Dispose(); // 첫 게임 세션 소유 해제
            restored = new PermanentProgressService(new FileProgressStorage(path)); // 다른 서비스 생성
            Assert.That(restored.Load(out error), Is.True, error); // 파일 역직렬화
            Assert.That(restored.Mastery(CharacterType.Sniper), Is.EqualTo(20)); // 저장 숙련도 복원
            Assert.That(restored.CommonMaterials, Is.EqualTo(6)); // 저장 재료 복원
            Assert.That(restored.TryBegin("second-file-run", CharacterType.Sniper, out error), Is.True, error); // 다음 출격 시작
            Assert.That(restored.TrySettle("second-file-run", CharacterType.Sniper, false, out _, out error), Is.True, error); // 기존 파일 원자적 교체
            Assert.That(File.Exists(path), Is.True); // 저장 파일 유지
            restored.Dispose(); // 다음 게임 세션 소유 해제
        } // 시험 범위 종료
        finally // 시험 정리 시작
        { // 정리 범위 시작
            service?.Dispose(); // 첫 세션 안전 해제
            restored?.Dispose(); // 복원 세션 안전 해제
            if (File.Exists(path)) // 생성 파일 존재 확인
            { // 삭제 시작
                File.Delete(path); // 해당 시험 파일만 삭제
            } // 삭제 종료
            if (File.Exists(path + ".lock")) // 해당 시험 잠금 파일 확인
            { // 삭제 시작
                File.Delete(path + ".lock"); // 해당 시험 잠금 파일만 삭제
            } // 삭제 종료
            if (File.Exists(path + ".session.lock")) // 해당 시험 세션 잠금 파일 확인
            { // 삭제 시작
                File.Delete(path + ".session.lock"); // 해당 시험 세션 잠금 파일만 삭제
            } // 삭제 종료
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
