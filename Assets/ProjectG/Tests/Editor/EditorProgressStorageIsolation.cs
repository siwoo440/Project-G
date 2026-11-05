using System; // 시험 저장소 팩토리
using NUnit.Framework; // 시험 초기화 도구
using ProjectG; // 영구 저장 인터페이스
[SetUpFixture] // 시험 어셈블리 공통 초기화
public sealed class EditorProgressStorageIsolation // 실제 저장소 격리
{ // 시험 설정 시작
    private Func<IProgressStorage> previous; // 기존 저장소 팩토리
    private sealed class MemoryStore : IProgressStorage // 시험 전용 메모리 저장소
    { // 저장소 시작
        private string contents; // 격리된 저장 내용
        public ProgressReadStatus Read(out string data, out string error) // 시험 저장 읽기
        { // 읽기 시작
            data = contents; // 저장 내용 전달
            error = null; // 읽기 오류 없음
            return contents == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 저장 상태 반환
        } // 읽기 종료
        public bool Write(string expectedData, string data, out string error) // 시험 저장 비교 기록
        { // 기록 시작
            if (contents != expectedData) // 기존 내용 변경 확인
            { // 충돌 시작
                error = "저장 내용이 변경되었습니다."; // 저장 충돌 안내
                return false; // 덮어쓰기 차단
            } // 충돌 종료
            contents = data; // 메모리 내용 저장
            error = null; // 저장 오류 없음
            return true; // 저장 완료
        } // 기록 종료
    } // 저장소 종료
    [OneTimeSetUp] // 전체 시험 시작 전
    public void InstallMemoryStorage() // 실제 저장소 차단
    { // 설정 시작
        previous = GameSessionHost.StorageFactoryOverride; // 기존 설정 보관
        GameSessionHost.StorageFactoryOverride = () => new MemoryStore(); // 새 출격마다 시험 저장소
    } // 설정 종료
    [OneTimeTearDown] // 전체 시험 종료 후
    public void RestoreStorageFactory() // 원래 저장소 복원
    { // 복원 시작
        GameSessionHost.StorageFactoryOverride = previous; // 기존 설정 복원
    } // 복원 종료
} // 시험 설정 종료
