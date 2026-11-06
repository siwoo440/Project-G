using System; // 고유 식별자와 파일 핸들
using System.IO; // 격리된 임시 파일
using NUnit.Framework; // 시험 도구
using ProjectG; // 출격 정산 기능
public class ProgressFailureRecoveryTests // 정산 장애와 재시작 시험
{ // 시험 시작
    private sealed class AmbiguousStore : IProgressStorage // 저장 성공 후 응답 오류 모의
    { // 저장소 시작
        public string Data; // 현재 저장 원문
        public bool LoseNextAcknowledgement; // 다음 저장 응답 누락 여부
        public bool FailRead; // 읽기 장애 여부
        public bool ThrowOnRead; // 읽기 예외 여부
        public bool ThrowAfterWrite; // 기록 후 예외 여부
        public bool ThrowBeforeWrite; // 기록 전 예외 여부
        public ProgressReadStatus Read(out string data, out string error) // 저장 원문 조회
        { // 조회 시작
            if (ThrowOnRead) // 읽기 예외 설정 확인
            { // 예외 발생 시작
                throw new IOException("읽기 예외"); // 읽기 예외 전달
            } // 예외 발생 종료
            data = FailRead ? null : Data; // 오류가 없을 때만 원문 반환
            error = FailRead ? "읽기 실패" : null; // 읽기 오류 안내
            if (FailRead) // 읽기 장애 확인
            { // 오류 시작
                return ProgressReadStatus.Error; // 확인할 수 없는 원문
            } // 오류 종료
            return Data == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 파일 상태 반환
        } // 조회 종료
        public bool Write(string expectedData, string data, out string error) // 기존 원문과 비교 저장
        { // 저장 시작
            if (!string.Equals(Data, expectedData, StringComparison.Ordinal)) // 원문 충돌 확인
            { // 충돌 시작
                error = "저장 충돌"; // 충돌 안내
                return false; // 기존 원문 보존
            } // 충돌 종료
            if (ThrowBeforeWrite) // 기록 전 예외 확인
            { // 예외 발생 시작
                ThrowBeforeWrite = false; // 다음 호출 복원
                throw new IOException("기록 전 예외"); // 저장 전 예외 전달
            } // 예외 발생 종료
            Data = data; // 실제 저장 완료
            if (ThrowAfterWrite) // 기록 후 예외 확인
            { // 예외 발생 시작
                ThrowAfterWrite = false; // 다음 호출 복원
                throw new IOException("저장 응답 예외"); // 저장 후 예외 전달
            } // 예외 발생 종료
            if (LoseNextAcknowledgement) // 저장 응답 누락 확인
            { // 응답 실패 시작
                LoseNextAcknowledgement = false; // 다음 호출 복원
                error = "저장 응답 누락"; // 결과 불확실 안내
                return false; // 호출자에게 실패 전달
            } // 응답 실패 종료
            error = null; // 저장 오류 없음
            return true; // 저장 결과 전달
        } // 저장 종료
    } // 저장소 종료
    [Test] // 실제 파일 잠금 실패와 재시도 시험
    public void FileLockFailurePreservesPendingAndRetrySurvivesRestart() // 정산 보존과 재시작 확인
    { // 시험 시작
        string directory = Path.Combine(Path.GetTempPath(), "projectg-17-test-" + Guid.NewGuid().ToString("N")); // 고유한 임시 폴더
        string path = Path.Combine(directory, "progress.json"); // 격리된 성장 파일
        PermanentProgressService first = null; // 첫 실행 서비스
        PermanentProgressService restored = null; // 재시작 서비스
        try // 시험 범위 준비
        { // 시험 범위 시작
            Directory.CreateDirectory(directory); // 고유 폴더 생성
            first = new PermanentProgressService(new FileProgressStorage(path)); // 명시된 임시 저장소 연결
            Assert.That(first.Load(out string error), Is.True, error); // 최초 저장 상태 확인
            Assert.That(first.TryBegin("file-retry", CharacterType.Sniper, out error), Is.True, error); // 진행 중 출격 기록
            string pending = File.ReadAllText(path); // 정산 전 원문 보관
            using (FileStream blocker = new FileStream(path + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None)) // 다른 저장자의 배타 잠금
            { // 저장 충돌 시작
                Assert.That(first.TrySettle("file-retry", CharacterType.Sniper, false, out SettlementReceipt failed, out error), Is.False); // 잠금 중 정산 실패
                Assert.That(failed, Is.Null); // 미확정 영수증 방지
                Assert.That(error, Is.Not.Empty); // 저장 오류 안내
                Assert.That(first.Mastery(CharacterType.Sniper), Is.Zero); // 실패 후 메모리 미지급
                Assert.That(File.ReadAllText(path), Is.EqualTo(pending)); // 진행 중 원문 보존
            } // 저장 충돌 종료
            Assert.That(first.TrySettle("file-retry", CharacterType.Sniper, false, out SettlementReceipt saved, out error), Is.True, error); // 잠금 해제 후 정산 재시도
            Assert.That(saved.AlreadySettled, Is.False); // 최초 지급 확인
            Assert.That(first.Mastery(CharacterType.Sniper), Is.EqualTo(10)); // 참여 숙련도 한 번 지급
            first.Dispose(); // 첫 실행의 세션 잠금 해제
            restored = new PermanentProgressService(new FileProgressStorage(path)); // 같은 격리 파일로 재시작
            Assert.That(restored.Load(out error), Is.True, error); // 저장된 원문 복원
            Assert.That(restored.TryRecoverPending(out SettlementReceipt recovery, out error), Is.True, error); // 추가 복구 대상 확인
            Assert.That(recovery, Is.Null); // 이미 정산된 출격 재복구 방지
            Assert.That(restored.TrySettle("file-retry", CharacterType.Sniper, false, out SettlementReceipt duplicate, out error), Is.True, error); // 같은 정산 재조회
            Assert.That(duplicate.AlreadySettled, Is.True); // 이전 영수증 재사용
            Assert.That(restored.Mastery(CharacterType.Sniper), Is.EqualTo(10)); // 재시작 후 중복 숙련도 방지
            Assert.That(restored.CommonMaterials, Is.EqualTo(3)); // 재시작 후 중복 재료 방지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            first?.Dispose(); // 첫 서비스 잠금 해제
            restored?.Dispose(); // 재시작 서비스 잠금 해제
            if (File.Exists(path)) // 격리 저장 파일 확인
            { // 저장 파일 삭제 시작
                File.Delete(path); // 이 시험의 저장 파일만 삭제
            } // 저장 파일 삭제 종료
            if (File.Exists(path + ".lock")) // 격리 쓰기 잠금 확인
            { // 잠금 파일 삭제 시작
                File.Delete(path + ".lock"); // 이 시험의 잠금 파일만 삭제
            } // 잠금 파일 삭제 종료
            if (File.Exists(path + ".session.lock")) // 격리 세션 잠금 확인
            { // 세션 파일 삭제 시작
                File.Delete(path + ".session.lock"); // 이 시험의 세션 파일만 삭제
            } // 세션 파일 삭제 종료
            if (Directory.Exists(directory)) // 격리 폴더 확인
            { // 폴더 정리 시작
                Directory.Delete(directory, false); // 빈 시험 폴더만 삭제
            } // 폴더 정리 종료
        } // 정리 종료
    } // 시험 종료
    [Test] // 저장 완료 후 응답 누락 시험
    public void LostWriteAcknowledgementConfirmsExactSavedReceiptWithoutDoubleReward() // 저장된 정산 즉시 확인
    { // 시험 시작
        AmbiguousStore store = new AmbiguousStore(); // 격리된 모의 저장소
        PermanentProgressService first = new PermanentProgressService(store); // 첫 실행 서비스
        PermanentProgressService restored = null; // 재시작 서비스
        try // 시험 범위 준비
        { // 시험 범위 시작
            Assert.That(first.Load(out string error), Is.True, error); // 최초 저장 상태 확인
            Assert.That(first.TryBegin("uncertain-result", CharacterType.Rapid, out error), Is.True, error); // 진행 중 출격 저장
            store.LoseNextAcknowledgement = true; // 저장 성공 후 응답 오류 유도
            Assert.That(first.TrySettle("uncertain-result", CharacterType.Rapid, true, out SettlementReceipt saved, out error), Is.True, error); // 실제 저장된 원문 재확인
            Assert.That(saved.AlreadySettled, Is.False); // 최초 정산 영수증 확인
            Assert.That(first.Mastery(CharacterType.Rapid), Is.EqualTo(20)); // 확인된 메모리 반영
            string persisted = store.Data; // 실제 저장된 정산 원문
            Assert.That(first.TrySettle("uncertain-result", CharacterType.Rapid, true, out SettlementReceipt repeated, out error), Is.True, error); // 같은 실행의 결과 재조회
            Assert.That(repeated.AlreadySettled, Is.True); // 기존 정산 영수증 재사용
            Assert.That(store.Data, Is.EqualTo(persisted)); // 저장된 정산 보존
            first.Dispose(); // 첫 실행 종료
            restored = new PermanentProgressService(store); // 같은 저장소 재시작
            Assert.That(restored.Load(out error), Is.True, error); // 실제 저장 원문 복원
            Assert.That(restored.TryRecoverPending(out SettlementReceipt recovery, out error), Is.True, error); // 중단 출격 확인
            Assert.That(recovery, Is.Null); // 이미 저장된 정산 재지급 방지
            Assert.That(restored.TrySettle("uncertain-result", CharacterType.Rapid, true, out SettlementReceipt receipt, out error), Is.True, error); // 실제 저장된 영수증 조회
            Assert.That(receipt.AlreadySettled, Is.True); // 이전 결과 재사용
            Assert.That(restored.Mastery(CharacterType.Rapid), Is.EqualTo(20)); // 클리어 숙련도 한 번 지급
            Assert.That(restored.CommonMaterials, Is.EqualTo(6)); // 클리어 공통 재료 한 번 지급
            Assert.That(restored.TrySettle("uncertain-result", CharacterType.Sniper, true, out _, out error), Is.False); // 다른 캐릭터 재정산 차단
            Assert.That(restored.TrySettle("uncertain-result", CharacterType.Rapid, false, out _, out error), Is.False); // 다른 결과 재정산 차단
            Assert.That(store.Data, Is.EqualTo(persisted)); // 확인 과정의 저장 내용 보존
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            first.Dispose(); // 첫 서비스 해제
            restored?.Dispose(); // 재시작 서비스 해제
        } // 정리 종료
    } // 시험 종료
    [Test] // 저장 후 확인 읽기 실패 시험
    public void LostAcknowledgementWithReadFailureKeepsErrorUntilExactReadCanSucceed() // 미확인 상태의 안전한 재시도
    { // 시험 시작
        AmbiguousStore store = new AmbiguousStore(); // 격리된 모의 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 시험 서비스
        try // 시험 범위 준비
        { // 시험 범위 시작
            Assert.That(service.Load(out string error), Is.True, error); // 최초 저장 상태 확인
            Assert.That(service.TryBegin("unreadable-result", CharacterType.Rapid, out error), Is.True, error); // 진행 중 출격 저장
            store.LoseNextAcknowledgement = true; // 저장 후 응답 오류 유도
            store.FailRead = true; // 저장 원문 재조회 실패 유도
            Assert.That(service.TrySettle("unreadable-result", CharacterType.Rapid, false, out SettlementReceipt failed, out error), Is.False); // 확인 불가 정산 차단
            Assert.That(failed, Is.Null); // 미확인 영수증 차단
            Assert.That(error, Is.EqualTo("저장 응답 누락")); // 원래 저장 오류 유지
            Assert.That(service.Mastery(CharacterType.Rapid), Is.Zero); // 확인 전 메모리 유지
            string persisted = store.Data; // 실제 저장 원문 보관
            store.FailRead = false; // 읽기 장애 해제
            Assert.That(service.TrySettle("unreadable-result", CharacterType.Rapid, false, out SettlementReceipt saved, out error), Is.True, error); // 같은 서비스에서 원문 일치 재확인
            Assert.That(saved.AlreadySettled, Is.False); // 확인된 최초 정산
            Assert.That(service.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 숙련도 한 번 반영
            Assert.That(service.CommonMaterials, Is.EqualTo(3)); // 공통 재료 한 번 반영
            Assert.That(store.Data, Is.EqualTo(persisted)); // 재시도 중 원문 미변경
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            service.Dispose(); // 시험 서비스 해제
        } // 정리 종료
    } // 시험 종료
    [Test] // 다른 실행의 정산 충돌 시험
    public void DifferentPersistedSnapshotRemainsConflictAndIsNotOverwritten() // 서로 다른 결과의 안전한 거절
    { // 시험 시작
        AmbiguousStore store = new AmbiguousStore(); // 격리된 모의 저장소
        PermanentProgressService stale = new PermanentProgressService(store); // 이전 상태 서비스
        PermanentProgressService other = new PermanentProgressService(store); // 다른 저장자 서비스
        try // 시험 범위 준비
        { // 시험 범위 시작
            Assert.That(stale.Load(out string error), Is.True, error); // 이전 상태 로드
            Assert.That(stale.TryBegin("changed-result", CharacterType.Rapid, out error), Is.True, error); // 진행 중 출격 저장
            Assert.That(other.Load(out error), Is.True, error); // 다른 저장자의 같은 출격 로드
            Assert.That(other.TrySettle("changed-result", CharacterType.Rapid, false, out _, out error), Is.True, error); // 다른 결과 먼저 저장
            string different = store.Data; // 다른 실행의 원문
            Assert.That(stale.TrySettle("changed-result", CharacterType.Rapid, true, out SettlementReceipt blocked, out error), Is.False); // 서로 다른 결과 거부
            Assert.That(blocked, Is.Null); // 충돌 영수증 없음
            Assert.That(error, Is.EqualTo("저장 충돌")); // 기존 저장 오류 유지
            Assert.That(stale.Mastery(CharacterType.Rapid), Is.Zero); // 이전 메모리 유지
            Assert.That(store.Data, Is.EqualTo(different)); // 다른 저장자의 원문 보존
            Assert.That(other.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 다른 정산 보상 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            stale.Dispose(); // 이전 서비스 해제
            other.Dispose(); // 다른 서비스 해제
        } // 정리 종료
    } // 시험 종료
    [Test] // 저장 후 예외 시험
    public void WriteExceptionAfterPersistConfirmsExactSnapshot() // 기록 후 예외의 안전한 복구
    { // 시험 시작
        AmbiguousStore store = new AmbiguousStore(); // 격리된 모의 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 시험 서비스
        try // 시험 범위 준비
        { // 시험 범위 시작
            Assert.That(service.Load(out string error), Is.True, error); // 최초 상태 확인
            Assert.That(service.TryBegin("write-exception", CharacterType.Rapid, out error), Is.True, error); // 출격 시작 저장
            store.ThrowAfterWrite = true; // 정산 저장 뒤 예외 설정
            Assert.That(service.TrySettle("write-exception", CharacterType.Rapid, false, out SettlementReceipt receipt, out error), Is.True, error); // 정확한 저장 원문 확인
            Assert.That(receipt.AlreadySettled, Is.False); // 최초 정산 결과 확인
            Assert.That(service.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 숙련도 한 번 반영
            Assert.That(service.CommonMaterials, Is.EqualTo(3)); // 공통 재료 한 번 반영
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            service.Dispose(); // 시험 서비스 해제
        } // 정리 종료
    } // 시험 종료
    [Test] // 저장 확인 중 읽기 예외 시험
    public void ReadExceptionAfterWriteFailureKeepsOriginalErrorAndMemory() // 확인 실패 원문과 오류 보존
    { // 시험 시작
        AmbiguousStore store = new AmbiguousStore(); // 격리된 모의 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 시험 서비스
        try // 시험 범위 준비
        { // 시험 범위 시작
            Assert.That(service.Load(out string error), Is.True, error); // 최초 상태 확인
            Assert.That(service.TryBegin("read-exception", CharacterType.Rapid, out error), Is.True, error); // 출격 시작 저장
            store.LoseNextAcknowledgement = true; // 저장 뒤 응답 오류
            store.ThrowOnRead = true; // 저장 결과 조회 예외
            Assert.That(service.TrySettle("read-exception", CharacterType.Rapid, false, out SettlementReceipt receipt, out error), Is.False); // 확인 불가 상태 유지
            Assert.That(receipt, Is.Null); // 미확인 영수증 제외
            Assert.That(error, Is.EqualTo("저장 응답 누락")); // 원래 저장 오류 유지
            Assert.That(service.Mastery(CharacterType.Rapid), Is.Zero); // 이전 메모리 유지
            string persisted = store.Data; // 저장된 원문 보관
            store.ThrowOnRead = false; // 읽기 오류 해제
            Assert.That(service.TrySettle("read-exception", CharacterType.Rapid, false, out _, out error), Is.True, error); // 같은 정산 재시도
            Assert.That(store.Data, Is.EqualTo(persisted)); // 이미 저장된 원문 유지
            Assert.That(service.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 단일 보상 반영
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            service.Dispose(); // 시험 서비스 해제
        } // 정리 종료
    } // 시험 종료
    [Test] // 저장 전 예외 시험
    public void WriteExceptionBeforePersistPreservesOriginalErrorAndSnapshot() // 기록 전 예외의 안전한 실패
    { // 시험 시작
        AmbiguousStore store = new AmbiguousStore(); // 격리된 모의 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 시험 서비스
        try // 시험 범위 준비
        { // 시험 범위 시작
            Assert.That(service.Load(out string error), Is.True, error); // 최초 상태 확인
            Assert.That(service.TryBegin("before-write", CharacterType.Rapid, out error), Is.True, error); // 출격 시작 저장
            string pending = store.Data; // 진행 중 원문 보관
            store.ThrowBeforeWrite = true; // 정산 기록 전 예외
            Assert.That(service.TrySettle("before-write", CharacterType.Rapid, false, out SettlementReceipt receipt, out error), Is.False); // 저장되지 않은 정산 차단
            Assert.That(receipt, Is.Null); // 미지급 영수증 제외
            Assert.That(error, Does.Contain("기록 전 예외")); // 원래 저장 예외 안내
            Assert.That(store.Data, Is.EqualTo(pending)); // 진행 중 원문 유지
            Assert.That(service.Mastery(CharacterType.Rapid), Is.Zero); // 이전 메모리 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            service.Dispose(); // 시험 서비스 해제
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
