using System; // 예외와 환경 변수
using System.IO; // 격리 파일 입출력
using System.Linq; // 저장 바이트 비교
using System.Text; // 표식 문자 인코딩
using System.Threading; // 종료 대기
using ProjectG; // 실제 영구 성장 기능
using UnityEngine; // 유니티 직렬화 기능
public static class ProjectGRecoveryProbe // 격리된 프로세스 복구 시험
{ // 시험 시작
    private const string FirstRun = "day17-crash-run"; // 강제 종료할 출격 식별자
    private const string NextRun = "day17-next-run"; // 복구 후 출격 식별자
    [Serializable] // 저장 필드 읽기 표시
    private sealed class ProbeSnapshot // 진행 파일의 확인 필드
    { // 자료 시작
        public string activeRunId; // 저장된 진행 출격 식별자
    } // 자료 종료
    private static string Root() // 명시된 격리 폴더 조회
    { // 조회 시작
        string value = Environment.GetEnvironmentVariable("PROJECTG_PROBE_DIR"); // 자식 프로세스 전용 경로
        if (string.IsNullOrWhiteSpace(value)) // 격리 경로 누락 확인
        { // 차단 시작
            throw new InvalidOperationException("격리 시험 경로가 없습니다"); // 기본 사용자 경로 접근 차단
        } // 차단 종료
        return Path.GetFullPath(value); // 정규화된 시험 경로
    } // 조회 종료
    private static string SavePath() // 실제 격리 성장 파일 경로
    { // 조회 시작
        return Path.Combine(Root(), "projectg-probe-progress.json"); // 지정 폴더의 전용 파일
    } // 조회 종료
    private static void Require(bool condition, string message) // 시험 조건 검증
    { // 검증 시작
        if (!condition) // 실패 조건 확인
        { // 실패 시작
            throw new InvalidOperationException(message); // 검증 실패와 프로세스 중단
        } // 실패 종료
    } // 검증 종료
    private static void WriteMarker(string name, string text) // 완료 표식의 디스크 기록
    { // 기록 시작
        string path = Path.Combine(Root(), name); // 지정 폴더의 표식 경로
        using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) // 새 표식 파일 생성
        { // 쓰기 시작
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true)) // UTF-8 문자 출력
            { // 문자 시작
                writer.Write(text); // 표식 내용 기록
                writer.Flush(); // 문자 버퍼 비우기
                stream.Flush(true); // 운영체제 파일 버퍼 기록
            } // 문자 종료
        } // 쓰기 종료
    } // 기록 종료
    public static void BeginAndWait() // 실제 출격 기록 후 강제 종료 대기
    { // 시험 시작
        using (PermanentProgressService service = new PermanentProgressService(new FileProgressStorage(SavePath()))) // 전용 파일 명시 주입
        { // 서비스 시작
            Require(service.Load(out string error), "첫 로드 실패: " + error); // 빈 전용 저장소 확인
            Require(service.TryBegin(FirstRun, CharacterType.Rapid, out error), "첫 출격 저장 실패: " + error); // 실제 출격 시작 저장
            string saved = File.ReadAllText(SavePath(), new UTF8Encoding(false, true)); // 저장 완료 원문 확인
            ProbeSnapshot state = JsonUtility.FromJson<ProbeSnapshot>(saved); // 실제 파일의 출격 식별 조회
            Require(state != null && state.activeRunId == FirstRun, "첫 출격 기록 불일치"); // 파일 내 진행 기록 확인
            string marker = System.Diagnostics.Process.GetCurrentProcess().Id + "\n" + FirstRun + "\n"; // 실제 Unity PID와 출격 식별자
            WriteMarker("ready.txt", marker); // 저장 종료 후 준비 표식 동기 기록
            Thread.Sleep(Timeout.Infinite); // 운영체제 강제 종료 대기
        } // 서비스 종료
    } // 시험 종료
    public static void RecoverAndSettle() // 새 프로세스 복구와 다음 출격
    { // 시험 시작
        using (PermanentProgressService service = new PermanentProgressService(new FileProgressStorage(SavePath()))) // 같은 전용 파일 주입
        { // 서비스 시작
            Require(service.Load(out string error), "복구 로드 실패: " + error); // 종료된 세션의 파일 로드
            Require(service.TryRecoverPending(out SettlementReceipt recovered, out error), "미정산 복구 실패: " + error); // 기존 출격 실패 정산
            Require(recovered != null && recovered.RunId == FirstRun && !recovered.Cleared && !recovered.AlreadySettled, "복구 영수증 불일치"); // 복구 지급 확인
            Require(service.Mastery(CharacterType.Rapid) == 10 && service.CommonMaterials == 3, "복구 보상 불일치"); // 참여 보상 시험값 확인
            Require(service.TryRecoverPending(out SettlementReceipt none, out error) && none == null, "중복 복구 차단 실패: " + error); // 추가 미정산 없음 확인
            Require(service.TrySettle(FirstRun, CharacterType.Rapid, false, out SettlementReceipt repeated, out error) && repeated.AlreadySettled, "같은 결과 조회 실패: " + error); // 기존 영수증 재조회
            Require(!service.TrySettle(FirstRun, CharacterType.Rapid, true, out _, out error), "다른 결과 중복 허용"); // 다른 결과 덮어쓰기 차단
            Require(service.Mastery(CharacterType.Rapid) == 10 && service.CommonMaterials == 3, "중복 복구 보상 증가"); // 보상 중복 방지 확인
            Require(service.TryBegin(NextRun, CharacterType.Sniper, out error), "다음 출격 시작 실패: " + error); // 다른 캐릭터 재출격 저장
            Require(service.TrySettle(NextRun, CharacterType.Sniper, true, out SettlementReceipt settled, out error), "다음 출격 정산 실패: " + error); // 실제 클리어 정산
            Require(settled != null && !settled.AlreadySettled && service.Mastery(CharacterType.Sniper) == 20 && service.CommonMaterials == 9, "다음 출격 보상 불일치"); // 누적 시험 보상 확인
        } // 서비스 종료
        WriteMarker("recovered.txt", "첫 출격 복구 및 중복 방지 완료\n다음 출격 저장과 정산 완료\n"); // 두 번째 프로세스 결과 기록
    } // 시험 종료
    public static void VerifyReload() // 세 번째 프로세스 영속성 확인
    { // 시험 시작
        using (PermanentProgressService service = new PermanentProgressService(new FileProgressStorage(SavePath()))) // 같은 전용 파일 주입
        { // 서비스 시작
            Require(service.Load(out string error), "최종 로드 실패: " + error); // 원자 교체 후 파일 재로드
            Require(service.Mastery(CharacterType.Rapid) == 10 && service.Mastery(CharacterType.Sniper) == 20 && service.CommonMaterials == 9, "재로드 성장 값 불일치"); // 실제 누적 값 확인
            byte[] before = File.ReadAllBytes(SavePath()); // 중복 조회 전 파일 원문
            Require(service.TryRecoverPending(out SettlementReceipt pending, out error) && pending == null, "미정산 잔류: " + error); // 진행 중 출격 없음
            Require(service.TrySettle(FirstRun, CharacterType.Rapid, false, out SettlementReceipt first, out error) && first.AlreadySettled, "첫 영수증 재조회 실패: " + error); // 첫 출격 중복 차단
            Require(service.TrySettle(NextRun, CharacterType.Sniper, true, out SettlementReceipt next, out error) && next.AlreadySettled, "다음 영수증 재조회 실패: " + error); // 다음 출격 중복 차단
            Require(before.SequenceEqual(File.ReadAllBytes(SavePath())), "중복 조회 중 저장 파일 변경"); // 재조회 시 파일 미변경
            Require(service.Mastery(CharacterType.Rapid) == 10 && service.Mastery(CharacterType.Sniper) == 20 && service.CommonMaterials == 9, "재조회 후 중복 보상"); // 잔액 유지 확인
        } // 서비스 종료
        WriteMarker("verified.txt", "별도 프로세스 재로드·이중 지급 차단 완료\n"); // 최종 검증 표식
    } // 시험 종료
} // 시험 클래스 종료
