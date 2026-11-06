using System; // 문자열 비교 기능
using NUnit.Framework; // 자동 시험 도구
using ProjectG; // 영구 성장 기능
public sealed class ProgressAchievementSchemaTests // 과제 저장 형식 회귀 시험
{ // 시험 시작
    private sealed class MemoryStorage : IProgressStorage // 격리된 저장소
    { // 저장소 시작
        public string Data; // 저장된 원문
        public int Writes; // 성공 기록 횟수
        public ProgressReadStatus Read(out string data, out string error) // 원문 조회
        { // 조회 시작
            data = Data; // 현재 원문 반환
            error = null; // 읽기 오류 없음
            return Data == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 저장 유무 구분
        } // 조회 종료
        public bool Write(string expectedData, string data, out string error) // 원문 비교 저장
        { // 기록 시작
            error = null; // 이전 오류 제거
            if (!string.Equals(Data, expectedData, StringComparison.Ordinal)) // 이전 원문 확인
            { // 충돌 시작
                error = "원문 충돌"; // 저장 충돌 안내
                return false; // 다른 기록 보존
            } // 충돌 종료
            Data = data; // 다음 원문 저장
            Writes++; // 저장 횟수 증가
            return true; // 기록 성공
        } // 기록 종료
    } // 저장소 종료
    private const string V1 = "{\"schemaVersion\":1,\"commonMaterials\":0,\"rapidMastery\":0,\"sniperMastery\":0,\"rapidMaterials\":0,\"sniperMaterials\":0,\"activeRunId\":\"\",\"activeCharacter\":0,\"rapidUnlocks\":[],\"sniperUnlocks\":[],\"receipts\":[]}"; // 기존 정상 형식
    private const string V2 = "{\"schemaVersion\":2,\"commonMaterials\":3,\"rapidMastery\":10,\"sniperMastery\":0,\"rapidMaterials\":1,\"sniperMaterials\":0,\"activeRunId\":\"\",\"activeCharacter\":0,\"rapidUnlocks\":[],\"sniperUnlocks\":[],\"receipts\":[{\"runId\":\"run-one\",\"character\":0,\"cleared\":false,\"masteryGained\":10,\"commonMaterialsGained\":3,\"characterMaterialsGained\":0}],\"achievementReceipts\":[{\"achievement\":0,\"runId\":\"run-one\",\"character\":0,\"characterMaterialsGained\":1}]}"; // 과제 영수증 포함 형식
    [Test] // 이전 저장 형식 보존 시험
    public void V1LoadAndNormalRunRemainInV1Format() // 미활성 과제의 무이행 확인
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소 생성
        store.Data = V1; // 기존 버전 원문 준비
        PermanentProgressService service = new PermanentProgressService(store); // 기본 게임 경로
        Assert.That(service.Load(out string error), Is.True, error); // 기존 저장 로드
        Assert.That(store.Data, Is.EqualTo(V1)); // 로드 중 파일 보존
        Assert.That(service.TryBegin("old-run", CharacterType.Rapid, out error), Is.True, error); // 일반 출격 기록
        Assert.That(service.TrySettle("old-run", CharacterType.Rapid, false, out _, out error), Is.True, error); // 일반 결과 정산
        Assert.That(store.Data, Does.Contain("\"schemaVersion\":1")); // 기존 버전 유지
        Assert.That(store.Data, Does.Not.Contain("achievementReceipts")); // 비활성 과제 필드 미생성
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 근거 없는 재료 미지급
    } // 시험 종료
    [Test] // 두 번째 버전의 실제 정산과 과제 분리 시험
    public void V2RestoresSeparateAchievementAndRunReceipts() // 과제 기록을 포함한 저장 복원
    { // 시험 시작
        MemoryStorage store = new MemoryStorage(); // 격리 저장소 생성
        store.Data = V2; // 유효한 신규 버전 원문
        PermanentProgressService service = new PermanentProgressService(store); // 기본 게임 경로
        Assert.That(service.Load(out string error), Is.True, error); // 별도 과제 영수증 복원
        Assert.That(service.Mastery(CharacterType.Rapid), Is.EqualTo(10)); // 기존 숙련도 보존
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 기존 과제 재료 보존
        Assert.That(service.CommonMaterials, Is.EqualTo(3)); // 기존 공통 재료 보존
        Assert.That(service.TrySettle("run-one", CharacterType.Rapid, false, out SettlementReceipt receipt, out error), Is.True, error); // 기존 정산 재조회
        Assert.That(receipt.AlreadySettled, Is.True); // 출격 영수증 중복 방지
        Assert.That(store.Data, Is.EqualTo(V2)); // 조회 중 원문 미수정
        Assert.That(service.TryBegin("run-two", CharacterType.Sniper, out error), Is.True, error); // 신규 버전의 재출격
        Assert.That(service.TrySettle("run-two", CharacterType.Sniper, true, out _, out error), Is.True, error); // 신규 버전의 일반 정산
        Assert.That(store.Data, Does.Contain("\"schemaVersion\":2")); // 과제 기록 버전 유지
        Assert.That(store.Data, Does.Contain("\"achievementReceipts\"")); // 별도 과제 영수증 유지
        PermanentProgressService restored = new PermanentProgressService(store); // 다음 실행 서비스
        Assert.That(restored.Load(out error), Is.True, error); // 저장된 신규 버전 재로드
        Assert.That(restored.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 기존 과제 재료 보존
        Assert.That(restored.Mastery(CharacterType.Sniper), Is.EqualTo(20)); // 새 출격 정산 보존
    } // 시험 종료
    [Test] // 형식 손상과 미래 버전 차단 시험
    public void InvalidVersionAndAchievementRecordsAreRejectedWithoutWriting() // 과제 원문의 안전한 거부
    { // 시험 시작
        string record = "{\"achievement\":0,\"runId\":\"run-one\",\"character\":0,\"characterMaterialsGained\":1}"; // 유효 과제 영수증
        string[] invalid = new string[] // 손상 원문 목록
        { // 목록 시작
            V2.Replace("\"schemaVersion\":2", "\"schemaVersion\":3"), // 미지원 미래 버전
            V2.Replace(",\"achievementReceipts\":[" + record + "]", ""), // 과제 목록 누락
            V2.Replace("\"achievement\":0", "\"achievement\":\"0\""), // 식별자 타입 오류
            V2.Replace("\"characterMaterialsGained\":1", "\"characterMaterialsGained\":2"), // 보상 수량 오류
            V2.Replace(record, record + "," + record), // 과제 중복 영수증
            V2.Replace("\"achievement\":0", "\"achievement\":1"), // 캐릭터와 과제 불일치
            V2.Replace("\"runId\":\"run-one\",\"character\":0,\"characterMaterialsGained\":1", "\"runId\":\"unknown\",\"character\":0,\"characterMaterialsGained\":1"), // 존재하지 않는 출격
            V1.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"achievementReceipts\":[]") // 이전 버전의 추가 필드
        }; // 목록 종료
        foreach (string original in invalid) // 손상 사례 순회
        { // 검사 시작
            Assert.That(original, Is.Not.EqualTo(V2)); // 실제 변조 확인
            MemoryStorage store = new MemoryStorage(); // 격리 저장소 생성
            store.Data = original; // 손상 원문 주입
            PermanentProgressService service = new PermanentProgressService(store); // 기본 게임 경로
            Assert.That(service.Load(out string error), Is.False); // 손상 또는 미래 버전 거부
            Assert.That(store.Data, Is.EqualTo(original)); // 원본 원문 보존
            Assert.That(store.Writes, Is.Zero); // 거부 중 쓰기 방지
        } // 검사 종료
    } // 시험 종료
} // 시험 클래스 종료
