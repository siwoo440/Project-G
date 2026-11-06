using System; // 예외와 문자열 비교
using NUnit.Framework; // 시험 도구
using ProjectG; // 영구 성장 기능
public sealed class ProgressAchievementGrantTests // 격리된 과제 보상 시험
{ // 시험 시작
    public enum WriteMode // 저장소 응답 종류
    { // 종류 시작
        Normal, // 정상 저장
        FailBeforeWrite, // 쓰기 전 실패
        FailAfterWrite, // 쓰기 후 실패 응답
        ThrowAfterWrite, // 쓰기 후 예외 응답
        FailAfterWriteUnreadable // 쓰기 후 읽기 오류
    } // 종류 종료
    private sealed class MemoryStorage : IProgressStorage // 격리된 원문 저장소
    { // 저장소 시작
        public string Data; // 현재 저장 원문
        public int Writes; // 실제 저장 횟수
        public WriteMode Mode; // 다음 저장 응답
        private bool failReadOnce; // 일회성 재조회 실패
        public ProgressReadStatus Read(out string data, out string error) // 격리 원문 조회
        { // 조회 시작
            data = Data; // 실제 보관 원문
            error = null; // 초기 오류 제거
            if (failReadOnce) // 재조회 오류 확인
            { // 오류 시작
                failReadOnce = false; // 일회성 오류 소모
                error = "읽기 실패"; // 재조회 오류 안내
                return ProgressReadStatus.Error; // 불확실한 저장 상태
            } // 오류 종료
            return Data == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 저장 유무 구분
        } // 조회 종료
        public bool Write(string expectedData, string data, out string error) // 비교 후 원문 저장
        { // 기록 시작
            error = null; // 초기 오류 제거
            if (!string.Equals(Data, expectedData, StringComparison.Ordinal)) // 기존 원문 대조
            { // 충돌 시작
                error = "원문 충돌"; // 충돌 안내
                return false; // 다른 기록 보존
            } // 충돌 종료
            if (Mode == WriteMode.FailBeforeWrite) // 저장 전 실패 확인
            { // 실패 시작
                error = "쓰기 실패"; // 실패 안내
                return false; // 원문 유지
            } // 실패 종료
            Data = data; // 실제 원문 교체
            Writes++; // 실제 저장 횟수 증가
            if (Mode == WriteMode.ThrowAfterWrite) // 저장 후 예외 확인
            { // 예외 시작
                throw new InvalidOperationException("응답 예외"); // 저장 완료 후 응답 예외
            } // 예외 종료
            if (Mode == WriteMode.FailAfterWrite || Mode == WriteMode.FailAfterWriteUnreadable) // 저장 후 실패 응답 확인
            { // 오류 시작
                failReadOnce = Mode == WriteMode.FailAfterWriteUnreadable; // 재조회 실패 설정
                error = "응답 실패"; // 불확실한 저장 안내
                return false; // 실패 응답 반환
            } // 오류 종료
            return true; // 정상 저장 완료
        } // 기록 종료
    } // 저장소 종료
    private sealed class Verification : IAchievementVerifier // 격리 시험의 증거 검증기
    { // 검증기 시작
        public bool Verified; // 검증 결과
        public bool Throw; // 예외 시험 설정
        public bool IsVerified(CharacterAchievement achievement, string runId) // 외부 판정 결과 확인
        { // 확인 시작
            if (Throw) // 검증 예외 시험
            { // 예외 시작
                throw new InvalidOperationException("검증 실패"); // 검증기 오류 전달
            } // 예외 종료
            return Verified; // 주입된 시험 증거
        } // 확인 종료
    } // 검증기 종료
    private static PermanentProgressService Create(MemoryStorage store, Verification verifier) // 격리 검증기 주입
    { // 생성 시작
        return new PermanentProgressService(store, new EmptyProgressUnlockCatalog(), verifier); // 시험 전용 내부 생성자
    } // 생성 종료
    [Test] // 기본 경로 지급 차단 시험
    public void DefaultServiceCannotPayWithoutVerifier() // 미정 판정의 재료 지급 방지
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소
        PermanentProgressService service = new PermanentProgressService(store); // 실제 기본 생성 경로
        Assert.That(service.Load(out string error), Is.True, error); // 빈 저장 로드
        Assert.That(service.TryBegin("default-run", CharacterType.Rapid, out error), Is.True, error); // 격리 출격 시작
        string before = store.Data; // 기존 저장 원문
        Assert.That(service.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "default-run", out error), Is.False); // 증거 없는 지급 차단
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 전용 재료 보존
        Assert.That(store.Data, Is.EqualTo(before)); // 저장 원문 보존
        Assert.That(store.Data, Does.Contain("\"schemaVersion\":1")); // 기존 버전 유지
    } // 시험 종료
    [Test] // 과제와 숙련도 경계 시험
    public void VerifiedAwardMigratesOnceAndCombinesWithMasteryReward() // 같은 출격의 독립 보상 합산
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소
        Verification verifier = new Verification(); // 시험 검증기
        verifier.Verified = true; // 증거 확인 허용
        PermanentProgressService service = Create(store, verifier); // 격리 시험 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 빈 저장 로드
        for (int index = 0; index < 5; index++) // 숙련도 50 준비
        { // 출격 반복 시작
            string id = "before-" + index; // 독립 출격 식별
            Assert.That(service.TryBegin(id, CharacterType.Rapid, out error), Is.True, error); // 시험 출격 시작
            Assert.That(service.TrySettle(id, CharacterType.Rapid, false, out _, out error), Is.True, error); // 시험 실패 정산
        } // 출격 반복 종료
        Assert.That(service.TryBegin("award-run", CharacterType.Rapid, out error), Is.True, error); // 여섯 번째 출격
        Assert.That(store.Data, Does.Contain("\"schemaVersion\":1")); // 과제 기록 전 버전 유지
        Assert.That(service.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "award-run", out error), Is.True, error); // 검증된 과제 기록
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 첫 과제 전용 재료
        Assert.That(store.Data, Does.Contain("\"schemaVersion\":2")); // 원자적 신규 버전 이행
        Assert.That(store.Data, Does.Contain("\"achievementReceipts\"")); // 별도 과제 영수증
        int writes = store.Writes; // 직전 실제 저장 횟수
        Assert.That(service.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "award-run", out error), Is.True, error); // 같은 과제 재시도
        Assert.That(store.Writes, Is.EqualTo(writes)); // 재시도 저장 생략
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 중복 보상 방지
        Assert.That(service.TrySettle("award-run", CharacterType.Rapid, false, out _, out error), Is.True, error); // 숙련도 60 정산
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(2)); // 과제와 숙련도 재료 합산
        PermanentProgressService restored = Create(store, verifier); // 다음 실행 서비스
        Assert.That(restored.Load(out error), Is.True, error); // 신규 버전 로드
        Assert.That(restored.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.True); // 과제 완료 복원
        Assert.That(restored.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(2)); // 두 재료 보존
        writes = store.Writes; // 재실행 후 저장 횟수
        Assert.That(restored.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "award-run", out error), Is.True, error); // 저장된 과제 재시도
        Assert.That(store.Writes, Is.EqualTo(writes)); // 재시작 중복 쓰기 차단
        Assert.That(restored.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(2)); // 재시작 중복 지급 방지
    } // 시험 종료
    [Test] // 과제 검증과 출격 소속 시험
    public void UnverifiedInvalidAndWrongCharacterAwardsAreBlocked() // 확인되지 않은 과제 차단
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소
        Verification verifier = new Verification(); // 미확인 시험 검증기
        PermanentProgressService service = Create(store, verifier); // 검증기 주입 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 초기 저장 로드
        Assert.That(service.TryBegin("rapid-run", CharacterType.Rapid, out error), Is.True, error); // 속사형 출격 시작
        string before = store.Data; // 원문 보관
        Assert.That(service.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "rapid-run", out error), Is.False); // 미확인 과제 차단
        verifier.Throw = true; // 검증기 예외 설정
        Assert.That(service.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "rapid-run", out error), Is.False); // 검증 예외 차단
        verifier.Throw = false; // 검증기 예외 해제
        verifier.Verified = true; // 시험 증거 확인
        Assert.That(service.TryRecordAchievement(CharacterAchievement.SniperWeakHits10, "rapid-run", out error), Is.False); // 다른 캐릭터 출격 차단
        Assert.That(service.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "missing-run", out error), Is.False); // 존재하지 않는 출격 차단
        Assert.That(service.TryRecordAchievement((CharacterAchievement)99, "rapid-run", out error), Is.False); // 알 수 없는 과제 차단
        Assert.That(store.Data, Is.EqualTo(before)); // 모든 실패의 원문 보존
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 실패 시 재료 미지급
    } // 시험 종료
    [TestCase(WriteMode.FailAfterWrite)] // 저장 후 실패 응답
    [TestCase(WriteMode.ThrowAfterWrite)] // 저장 후 예외 응답
    [TestCase(WriteMode.FailAfterWriteUnreadable)] // 저장 후 재조회 실패
    public void AwardReconcilesUncertainStorageWithoutDoublePay(WriteMode mode) // 불확실한 저장의 안전한 재시도
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소
        Verification verifier = new Verification(); // 시험 검증기
        verifier.Verified = true; // 증거 확인 허용
        PermanentProgressService service = Create(store, verifier); // 검증기 주입 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 초기 저장 로드
        Assert.That(service.TryBegin("sniper-run", CharacterType.Sniper, out error), Is.True, error); // 저격형 출격 시작
        store.Mode = mode; // 과제 기록의 응답 방식
        bool completed = service.TryRecordAchievement(CharacterAchievement.SniperWeakHits10, "sniper-run", out error); // 검증된 과제 기록
        Assert.That(completed, Is.EqualTo(mode != WriteMode.FailAfterWriteUnreadable)); // 원문 재확인 가능 여부
        if (!completed) // 재조회 실패 확인
        { // 재시도 시작
            Assert.That(service.CharacterMaterials(CharacterType.Sniper), Is.Zero); // 불확실한 메모리 상태 유지
            store.Mode = WriteMode.Normal; // 저장소 응답 복원
            Assert.That(service.TryRecordAchievement(CharacterAchievement.SniperWeakHits10, "sniper-run", out error), Is.True, error); // 같은 원문 재시도
        } // 재시도 종료
        Assert.That(service.CharacterMaterials(CharacterType.Sniper), Is.EqualTo(1)); // 실제 보상 한 번 반영
        Assert.That(service.HasAchievement(CharacterAchievement.SniperWeakHits10), Is.True); // 완료 상태 보관
        int writes = store.Writes; // 실제 저장 횟수
        store.Mode = WriteMode.Normal; // 정상 응답 전환
        Assert.That(service.TryRecordAchievement(CharacterAchievement.SniperWeakHits10, "sniper-run", out error), Is.True, error); // 동일 과제 재시도
        Assert.That(store.Writes, Is.EqualTo(writes)); // 중복 저장 차단
        Assert.That(service.CharacterMaterials(CharacterType.Sniper), Is.EqualTo(1)); // 중복 보상 차단
    } // 시험 종료
    [Test] // 미기록 실패와 결과 후 검증 시험
    public void FailedWriteKeepsV1AndVerifiedCompletedRunCanRetry() // 실패 보존과 출격 결과 뒤 기록
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소
        Verification verifier = new Verification(); // 시험 검증기
        verifier.Verified = true; // 증거 확인 허용
        PermanentProgressService service = Create(store, verifier); // 검증기 주입 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 초기 저장 로드
        Assert.That(service.TryBegin("settled-run", CharacterType.Sniper, out error), Is.True, error); // 저격형 출격 시작
        Assert.That(service.TrySettle("settled-run", CharacterType.Sniper, false, out _, out error), Is.True, error); // 실패 출격 정산
        string before = store.Data; // 과제 전 v1 원문
        store.Mode = WriteMode.FailBeforeWrite; // 저장 전 오류 설정
        Assert.That(service.TryRecordAchievement(CharacterAchievement.SniperWeakHits10, "settled-run", out error), Is.False); // 저장 실패 확인
        Assert.That(store.Data, Is.EqualTo(before)); // 이전 v1 파일 보존
        Assert.That(service.CharacterMaterials(CharacterType.Sniper), Is.Zero); // 실패 후 미지급
        store.Mode = WriteMode.Normal; // 저장 오류 해제
        Assert.That(service.TryRecordAchievement(CharacterAchievement.SniperWeakHits10, "settled-run", out error), Is.True, error); // 검증된 과제 재시도
        Assert.That(service.CharacterMaterials(CharacterType.Sniper), Is.EqualTo(1)); // 한 번만 지급
    } // 시험 종료
    [Test] // 오래된 저장소 상태와 충돌 시험
    public void StaleServiceCannotEraseAwardAndCanReload() // 동시 세션 기록 보존
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 공유 격리 저장소
        Verification verifier = new Verification(); // 시험 검증기
        verifier.Verified = true; // 증거 확인 허용
        PermanentProgressService first = Create(store, verifier); // 첫 서비스
        Assert.That(first.Load(out string error), Is.True, error); // 첫 저장 로드
        Assert.That(first.TryBegin("shared-run", CharacterType.Rapid, out error), Is.True, error); // 동일 출격 시작
        PermanentProgressService stale = Create(store, verifier); // 오래된 상태 서비스
        Assert.That(stale.Load(out error), Is.True, error); // 출격 기록 로드
        Assert.That(first.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "shared-run", out error), Is.True, error); // 첫 서비스 과제 저장
        string awarded = store.Data; // 신규 과제 원문
        Assert.That(stale.TrySettle("shared-run", CharacterType.Rapid, false, out _, out error), Is.False); // 오래된 원문 덮어쓰기 차단
        Assert.That(store.Data, Is.EqualTo(awarded)); // 과제 영수증 보존
        Assert.That(stale.Load(out error), Is.True, error); // 최신 원문 재로드
        Assert.That(stale.TrySettle("shared-run", CharacterType.Rapid, false, out _, out error), Is.True, error); // 재로드 후 정상 정산
        Assert.That(stale.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 과제 보상 보존
        Assert.That(stale.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.True); // 별도 과제 기록 보존
    } // 시험 종료
    [TestCase(0, 0, "rapidMaterials")] // 속사형 재료 상한
    [TestCase(1, 1, "sniperMaterials")] // 저격형 재료 상한
    public void FullMaterialRangeBlocksAwardWithoutMigration(int achievementValue, int characterValue, string field) // 보상 숫자 초과 차단
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소
        Verification verifier = new Verification(); // 시험 검증기
        verifier.Verified = true; // 증거 확인 허용
        PermanentProgressService initial = Create(store, verifier); // 초기 시험 서비스
        Assert.That(initial.Load(out string error), Is.True, error); // 빈 저장 로드
        CharacterType character = (CharacterType)characterValue; // 시험 캐릭터
        Assert.That(initial.TryBegin("limit-run", character, out error), Is.True, error); // 출격 기록 생성
        string before = store.Data; // 이전 출격 원문
        store.Data = store.Data.Replace("\"" + field + "\":0", "\"" + field + "\":2147483647"); // 전용 재료 최대값
        Assert.That(store.Data, Is.Not.EqualTo(before)); // 경계 값 실제 반영
        string original = store.Data; // 경계 원문 보관
        PermanentProgressService service = Create(store, verifier); // 경계 상태 서비스
        Assert.That(service.Load(out error), Is.True, error); // 유효한 경계 원문 로드
        int writes = store.Writes; // 기존 저장 횟수
        Assert.That(service.TryRecordAchievement((CharacterAchievement)achievementValue, "limit-run", out error), Is.False); // 초과 보상 차단
        Assert.That(error, Is.Not.Null.And.Not.Empty); // 오류 사유 안내
        Assert.That(store.Data, Is.EqualTo(original)); // 이전 버전 원문 보존
        Assert.That(store.Writes, Is.EqualTo(writes)); // 잘못된 이행 차단
        Assert.That(service.CharacterMaterials(character), Is.EqualTo(int.MaxValue)); // 기존 재료 보존
    } // 시험 종료
    [Test] // 두 캐릭터의 독립 기록 시험
    public void BothCharacterAwardsSurviveSeparateRunsAndReload() // 캐릭터별 과제 독립성
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소
        Verification verifier = new Verification(); // 시험 검증기
        verifier.Verified = true; // 증거 확인 허용
        PermanentProgressService service = Create(store, verifier); // 시험 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 빈 저장 로드
        Assert.That(service.TryBegin("rapid-award", CharacterType.Rapid, out error), Is.True, error); // 속사형 출격
        Assert.That(service.TryRecordAchievement(CharacterAchievement.RapidBasicHits50, "rapid-award", out error), Is.True, error); // 속사형 과제 기록
        Assert.That(service.TrySettle("rapid-award", CharacterType.Rapid, false, out _, out error), Is.True, error); // 속사형 정산
        Assert.That(service.TryBegin("sniper-award", CharacterType.Sniper, out error), Is.True, error); // 저격형 출격
        Assert.That(service.TryRecordAchievement(CharacterAchievement.SniperWeakHits10, "sniper-award", out error), Is.True, error); // 저격형 과제 기록
        Assert.That(service.TrySettle("sniper-award", CharacterType.Sniper, false, out _, out error), Is.True, error); // 저격형 정산
        PermanentProgressService restored = Create(store, verifier); // 다음 실행 서비스
        Assert.That(restored.Load(out error), Is.True, error); // 저장 원문 재로드
        Assert.That(restored.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.True); // 속사형 기록 복원
        Assert.That(restored.HasAchievement(CharacterAchievement.SniperWeakHits10), Is.True); // 저격형 기록 복원
        Assert.That(restored.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 속사형 전용 보상
        Assert.That(restored.CharacterMaterials(CharacterType.Sniper), Is.EqualTo(1)); // 저격형 전용 보상
    } // 시험 종료
} // 시험 클래스 종료
