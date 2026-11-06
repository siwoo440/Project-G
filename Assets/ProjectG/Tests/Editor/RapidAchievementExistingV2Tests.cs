using NUnit.Framework; // 시험 도구
using ProjectG; // 성장 저장 기능
using UnityEngine; // 유니티 직렬화 기능
public sealed class RapidAchievementExistingV2Tests // 기존 과제 보존 시험
{ // 시험 시작
    private sealed class MemoryStore : IProgressStorage // 격리된 메모리 저장소
    { // 저장소 시작
        public string Data; // 저장 원문
        public int Writes; // 실제 저장 횟수
        public ProgressReadStatus Read(out string data, out string error) // 원문 조회
        { // 조회 시작
            data = Data; // 저장 원문 전달
            error = null; // 읽기 오류 없음
            return Data == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 원문 존재 확인
        } // 조회 종료
        public bool Write(string expectedData, string data, out string error) // 원문 비교 후 저장
        { // 저장 시작
            error = null; // 저장 오류 초기화
            if (Data != expectedData) // 원문 충돌 확인
            { // 충돌 처리 시작
                error = "격리 원문 충돌"; // 충돌 원인 전달
                return false; // 기존 원문 보존
            } // 충돌 처리 종료
            Data = data; // 원문 전체 저장
            Writes++; // 저장 횟수 증가
            return true; // 저장 성공
        } // 저장 종료
    } // 저장소 종료
    private sealed class Verification : IAchievementVerifier // 기존 과제 기록용 시험 검증기
    { // 검증기 시작
        public bool IsVerified(CharacterAchievement achievement, string runId) // 시험 증거 확인
        { // 확인 시작
            return achievement == CharacterAchievement.SniperWeakHits10 && runId == "sniper-existing"; // 기존 리안 과제만 허용
        } // 확인 종료
    } // 검증기 종료
    [Test] // 서로 다른 과제의 영수증 보존 시험
    public void RapidSettlementKeepsExistingV2AchievementAndBothRewardsAfterReload() // 기존 리안 과제와 아리아 정산 확인
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        Verification verifier = new Verification(); // 리안 기존 기록용 검증기
        PermanentProgressService service = new PermanentProgressService(store, new EmptyProgressUnlockCatalog(), verifier); // 격리 성장 서비스
        Assert.That(service.Load(out string error), Is.True, error); // 초기 저장 로드
        Assert.That(service.TryBegin("sniper-existing", CharacterType.Sniper, out error), Is.True, error); // 기존 리안 출격 시작
        Assert.That(service.TryRecordAchievement(CharacterAchievement.SniperWeakHits10, "sniper-existing", out error), Is.True, error); // 기존 리안 과제 영수증 준비
        Assert.That(service.TrySettle("sniper-existing", CharacterType.Sniper, false, out _, out error), Is.True, error); // 기존 리안 출격 정산
        Assert.That(store.Data, Does.Contain("\"schemaVersion\":2")); // 기존 버전 이행 확인
        Assert.That(service.CharacterMaterials(CharacterType.Sniper), Is.EqualTo(1)); // 기존 리안 전용 증표
        Assert.That(service.TryBegin("rapid-new", CharacterType.Rapid, out error), Is.True, error); // 새 아리아 출격 시작
        RapidAchievementRun evidence = new RapidAchievementRun("rapid-new"); // 새 출격의 격리 과제 증거
        for (int index = 0; index < 50; index++) // 유효 적중 횟수 준비
        { // 반복 시작
            evidence.RecordHit(); // 격리 증거 한 건 누적
        } // 반복 종료
        int writes = store.Writes; // 정산 전 저장 횟수
        Assert.That(service.TrySettle("rapid-new", CharacterType.Rapid, false, evidence, out SettlementReceipt receipt, out error), Is.True, error); // 아리아 과제 포함 정산
        Assert.That(store.Writes, Is.EqualTo(writes + 1)); // 정산과 과제 단일 저장
        Assert.That(receipt.AchievementMaterialsGained, Is.EqualTo(1)); // 아리아 별도 과제 보상 표시
        Assert.That(service.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 아리아 전용 증표
        Assert.That(service.CharacterMaterials(CharacterType.Sniper), Is.EqualTo(1)); // 기존 리안 전용 증표 보존
        ProgressSnapshotV2 snapshot = JsonUtility.FromJson<ProgressSnapshotV2>(store.Data); // 실제 저장 스냅샷 복원
        Assert.That(snapshot.achievementReceipts.Count, Is.EqualTo(2)); // 서로 다른 과제 영수증 보존
        Assert.That(snapshot.achievementReceipts[0].achievement, Is.EqualTo(CharacterAchievement.SniperWeakHits10)); // 기존 리안 과제 식별자
        Assert.That(snapshot.achievementReceipts[0].runId, Is.EqualTo("sniper-existing")); // 기존 리안 출격 식별자
        Assert.That(snapshot.achievementReceipts[0].character, Is.EqualTo(CharacterType.Sniper)); // 기존 리안 캐릭터
        Assert.That(snapshot.achievementReceipts[0].characterMaterialsGained, Is.EqualTo(1)); // 기존 리안 과제 지급
        Assert.That(snapshot.achievementReceipts[1].achievement, Is.EqualTo(CharacterAchievement.RapidBasicHits50)); // 새 아리아 과제 식별자
        Assert.That(snapshot.achievementReceipts[1].runId, Is.EqualTo("rapid-new")); // 새 아리아 출격 식별자
        Assert.That(snapshot.achievementReceipts[1].character, Is.EqualTo(CharacterType.Rapid)); // 새 아리아 캐릭터
        Assert.That(snapshot.achievementReceipts[1].characterMaterialsGained, Is.EqualTo(1)); // 새 아리아 과제 지급
        PermanentProgressService restored = new PermanentProgressService(store); // 재실행 성장 서비스
        Assert.That(restored.Load(out error), Is.True, error); // 기존 영수증을 포함한 형식 복원
        Assert.That(restored.HasAchievement(CharacterAchievement.SniperWeakHits10), Is.True); // 기존 리안 과제 복원
        Assert.That(restored.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.True); // 새 아리아 과제 복원
        Assert.That(restored.CharacterMaterials(CharacterType.Sniper), Is.EqualTo(1)); // 리안 증표 재로드 보존
        Assert.That(restored.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 아리아 증표 재로드 보존
        writes = store.Writes; // 재정산 전 저장 횟수
        Assert.That(restored.TrySettle("rapid-new", CharacterType.Rapid, false, out SettlementReceipt repeated, out error), Is.True, error); // 저장된 아리아 정산 재조회
        Assert.That(repeated.AlreadySettled, Is.True); // 기존 정산 확인
        Assert.That(repeated.AchievementMaterialsGained, Is.EqualTo(1)); // 기존 과제 보상 재표시
        Assert.That(store.Writes, Is.EqualTo(writes)); // 재조회 시 중복 저장 방지
        Assert.That(restored.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 재조회 중복 지급 방지
    } // 시험 종료
} // 시험 종료
