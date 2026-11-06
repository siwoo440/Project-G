using System; // 공통 자료형 기능
using System.Collections.Generic; // 목록과 중복 검사
using System.IO; // 영구 파일 입출력
using System.Text; // 파일 문자 인코딩
using UnityEngine; // 유니티 저장 경로와 직렬화
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum ProgressReadStatus // 저장 원문 조회 결과
    { // 결과 목록 시작
        Missing, // 저장 파일 없음
        Found, // 저장 원문 확인
        Error // 저장 원문 읽기 실패
    } // 결과 목록 종료
    public interface IProgressStorage // 교체 가능한 영구 저장소
    { // 저장소 계약 시작
        ProgressReadStatus Read(out string data, out string error); // 원문과 오류 조회
        bool Write(string expectedData, string data, out string error); // 기존 원문 비교 후 원자 저장
    } // 저장소 계약 종료
    public interface IProgressSessionStorage // 다른 게임 실행과 세션 배타 계약
    { // 세션 계약 시작
        IDisposable AcquireSession(out string error); // 살아 있는 게임의 배타 소유 요청
    } // 세션 계약 종료
    public sealed class FileProgressStorage : IProgressStorage, IProgressSessionStorage // 파일 기반 영구 저장소
    { // 저장소 시작
        private readonly string path; // 별도 성장 파일 경로
        public FileProgressStorage() : this(DefaultPath()) // 사용자 성장 파일 사용
        { // 생성 시작
        } // 생성 종료
        public FileProgressStorage(string filePath) // 명시된 파일 경로 사용
        { // 생성 시작
            path = Path.GetFullPath(filePath); // 파일별 절대 경로 보관
        } // 생성 종료
        private static string DefaultPath() // 사용자 또는 시험 저장 경로
        { // 경로 조회 시작
            string directory = Application.persistentDataPath; // 사용자 영구 저장 경로
#if UNITY_EDITOR // 에디터 전용 분리
            string testDirectory = Environment.GetEnvironmentVariable("PROJECTG_TEST_SAVE_DIR"); // 시험 전용 폴더 확인
            if (!string.IsNullOrWhiteSpace(testDirectory)) // 명시적 시험 경로 확인
            { // 경로 교체 시작
                directory = testDirectory; // 격리된 시험 폴더 사용
            } // 경로 교체 종료
#endif // 에디터 분기 종료
            return Path.Combine(directory, "projectg-progress-v1.json"); // 설정과 분리된 파일
        } // 경로 조회 종료
        public IDisposable AcquireSession(out string error) // 게임 세션 동안 파일 배타 소유
        { // 소유 시작
            error = null; // 오류 초기화
            try // 잠금 파일 접근
            { // 접근 범위 시작
                string directory = Path.GetDirectoryName(path); // 영구 파일의 폴더
                Directory.CreateDirectory(directory); // 저장 폴더 준비
                return new FileStream(path + ".session.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); // 종료 시 운영체제가 해제하는 잠금
            } // 접근 범위 종료
            catch (Exception exception) // 다른 실행 또는 접근 오류
            { // 오류 시작
                error = "영구 성장 파일을 사용할 수 없습니다: " + exception.Message; // 배타 소유 실패 안내
                return null; // 세션 시작 차단
            } // 오류 종료
        } // 소유 종료
        public ProgressReadStatus Read(out string data, out string error) // 성장 파일 읽기
        { // 읽기 시작
            data = null; // 초기 원문 비움
            error = null; // 초기 오류 비움
            try // 파일 접근 시작
            { // 접근 범위 시작
                data = File.ReadAllText(path, new UTF8Encoding(false, true)); // 잘못된 바이트를 거부하는 원문 읽기
                return ProgressReadStatus.Found; // 원문 확인
            } // 접근 범위 종료
            catch (FileNotFoundException) // 파일 없음 확인
            { // 신규 상태 시작
                return ProgressReadStatus.Missing; // 신규 데이터 허용
            } // 신규 상태 종료
            catch (DirectoryNotFoundException) // 저장 폴더 없음 확인
            { // 신규 폴더 시작
                return ProgressReadStatus.Missing; // 신규 데이터 허용
            } // 신규 폴더 종료
            catch (Exception exception) // 접근 실패 확인
            { // 오류 처리 시작
                error = "영구 성장 파일 읽기 실패: " + exception.Message; // 사용자 오류 안내
                return ProgressReadStatus.Error; // 원본 보존과 쓰기 차단
            } // 오류 처리 종료
        } // 읽기 종료
        public bool Write(string expectedData, string data, out string error) // 이전 원문 비교 후 교체
        { // 저장 시작
            error = null; // 초기 오류 비움
            string temporary = null; // 임시 파일 경로
            try // 안전한 저장 시작
            { // 저장 범위 시작
                string directory = Path.GetDirectoryName(Path.GetFullPath(path)); // 대상 폴더 확인
                Directory.CreateDirectory(directory); // 필요한 저장 폴더 준비
                using (FileStream lockFile = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 다른 저장자의 동시 교체 차단
                { // 파일 잠금 범위 시작
                    ProgressReadStatus currentStatus = Read(out string currentData, out string readError); // 잠금 중 현재 원문 확인
                    if (currentStatus == ProgressReadStatus.Error) // 현재 원문 읽기 실패
                    { // 읽기 오류 시작
                        error = readError; // 원본 보존 오류 안내
                        return false; // 읽지 못한 파일 교체 차단
                    } // 읽기 오류 종료
                    string current = currentStatus == ProgressReadStatus.Missing ? null : currentData; // 없는 파일과 원문 구분
                    if (!string.Equals(current, expectedData, StringComparison.Ordinal)) // 다른 저장자의 변경 확인
                    { // 충돌 처리 시작
                        error = "다른 실행에서 성장 데이터가 변경되어 저장을 중단했습니다"; // 충돌 오류 안내
                        return false; // 이전 상태 기반 덮어쓰기 차단
                    } // 충돌 처리 종료
                    temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp"; // 충돌 없는 같은 폴더 임시 파일
                    using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // 새 임시 파일 생성
                    { // 파일 쓰기 시작
                        using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false))) // 원문 인코딩 설정
                        { // 문자열 쓰기 시작
                            writer.Write(data); // 전체 스냅샷 쓰기
                            writer.Flush(); // 문자 버퍼 비우기
                            stream.Flush(true); // 파일 버퍼 디스크 반영
                        } // 문자열 쓰기 종료
                    } // 파일 쓰기 종료
                    if (File.Exists(path)) // 기존 저장 확인
                    { // 기존 파일 교체 시작
                        File.Replace(temporary, path, null); // 원본 파일 원자 교체
                    } // 기존 파일 교체 종료
                    else // 첫 파일 생성
                    { // 신규 파일 시작
                        File.Move(temporary, path); // 같은 폴더 원자 이동
                    } // 신규 파일 종료
                    temporary = null; // 임시 파일 이동 완료
                    return true; // 안전 저장 완료
                } // 파일 잠금 범위 종료
            } // 저장 범위 종료
            catch (Exception exception) // 접근 또는 교체 실패
            { // 실패 처리 시작
                error = "영구 성장 파일 저장 실패: " + exception.Message; // 오류 표시 문구
                return false; // 기존 원본과 메모리 유지
            } // 실패 처리 종료
            finally // 임시 파일 정리
            { // 정리 시작
                if (temporary != null) // 잔여 임시 파일 확인
                { // 정리 시도 시작
                    try // 임시 파일 접근
                    { // 접근 시작
                        if (File.Exists(temporary)) // 생성된 임시 파일 확인
                        { // 삭제 시작
                            File.Delete(temporary); // 현재 작업의 임시 파일만 삭제
                        } // 삭제 종료
                    } // 접근 종료
                    catch (Exception) // 임시 파일 정리 실패
                    { // 무시 범위 시작
                    } // 원래 오류 유지
                } // 정리 시도 종료
            } // 정리 종료
        } // 저장 종료
    } // 저장소 종료
    public sealed class SettlementReceipt // 외부에서 읽는 실제 정산 결과
    { // 영수증 시작
        public string RunId // 출격 식별자
        { // 속성 시작
            get; // 값 조회
        } // 출격 식별자
        public CharacterType Character // 출격 캐릭터
        { // 속성 시작
            get; // 값 조회
        } // 출격 캐릭터
        public bool Cleared // 클리어 여부
        { // 속성 시작
            get; // 값 조회
        } // 클리어 여부
        public int MasteryGained // 지급 숙련도
        { // 속성 시작
            get; // 값 조회
        } // 지급 숙련도
        public int CommonMaterialsGained // 지급 공통 재료
        { // 속성 시작
            get; // 값 조회
        } // 지급 공통 재료
        public int CharacterMaterialsGained // 지급 전용 재료
        { // 속성 시작
            get; // 값 조회
        } // 지급 전용 재료
        public int AchievementMaterialsGained // 별도 과제 전용 재료
        { // 속성 시작
            get; // 값 조회
        } // 속성 종료
        public bool AlreadySettled // 기존 정산 조회 여부
        { // 속성 시작
            get; // 값 조회
        } // 기존 정산 조회 여부
        internal SettlementReceipt(ProgressReceipt data, bool alreadySettled, int achievementMaterials = 0) // 보관 기록에서 읽기 전용 결과 생성
        { // 생성 시작
            RunId = data.runId; // 출격 식별자 복사
            Character = data.character; // 캐릭터 복사
            Cleared = data.cleared; // 결과 복사
            MasteryGained = data.masteryGained; // 숙련도 복사
            CommonMaterialsGained = data.commonMaterialsGained; // 공통 재료 복사
            CharacterMaterialsGained = data.characterMaterialsGained; // 전용 재료 복사
            AchievementMaterialsGained = achievementMaterials; // 별도 과제 보상 복사
            AlreadySettled = alreadySettled; // 재조회 여부 표시
        } // 생성 종료
    } // 영수증 종료
    [Serializable] // 유니티 직렬화 표시
    internal sealed class ProgressReceipt // 저장되는 정산 영수증
    { // 영수증 데이터 시작
        public string runId; // 출격 식별자
        public CharacterType character; // 출격 캐릭터
        public bool cleared; // 클리어 여부
        public int masteryGained; // 지급 숙련도
        public int commonMaterialsGained; // 지급 공통 재료
        public int characterMaterialsGained; // 지급 전용 재료
    } // 영수증 데이터 종료
    [Serializable] // 유니티 직렬화 표시
    internal class ProgressSnapshot // 하나로 저장하는 성장 스냅샷
    { // 스냅샷 시작
        public int schemaVersion = 1; // 저장 형식 버전
        public int commonMaterials; // 공통 재료
        public int rapidMastery; // 속사형 숙련도
        public int sniperMastery; // 저격형 숙련도
        public int rapidMaterials; // 속사형 전용 재료
        public int sniperMaterials; // 저격형 전용 재료
        public string activeRunId = ""; // 진행 중 출격 식별자
        public CharacterType activeCharacter; // 진행 중 출격 캐릭터
        public List<string> rapidUnlocks = new List<string>(); // 속사형 해금 목록
        public List<string> sniperUnlocks = new List<string>(); // 저격형 해금 목록
        public List<ProgressReceipt> receipts = new List<ProgressReceipt>(); // 정산 완료 출격 목록
    } // 스냅샷 종료
    internal enum CharacterAchievement // 결정된 캐릭터별 첫 과제
    { // 과제 시작
        RapidBasicHits50, // 속사형의 기본·집중 적중 과제
        SniperWeakHits10 // 저격형의 약점 적중 과제
    } // 과제 종료
    internal interface IAchievementVerifier // 향후 주입할 과제 증거 검증기
    { // 검증 계약 시작
        bool IsVerified(CharacterAchievement achievement, string runId); // 확인된 출격 증거 검사
    } // 검증 계약 종료
    [Serializable] // 유니티 직렬화 표시
    internal sealed class ProgressAchievementReceipt // 출격 정산과 분리된 과제 영수증
    { // 영수증 시작
        public CharacterAchievement achievement; // 과제 식별자
        public string runId; // 검증된 출격 식별자
        public CharacterType character; // 과제 대상 캐릭터
        public int characterMaterialsGained; // 한 번 지급한 전용 재료
    } // 영수증 종료
    [Serializable] // 유니티 직렬화 표시
    internal sealed class ProgressSnapshotV2 : ProgressSnapshot // 과제 기록이 있는 저장 형식
    { // 저장 형식 시작
        public List<ProgressAchievementReceipt> achievementReceipts = new List<ProgressAchievementReceipt>(); // 별도 과제 영수증
    } // 저장 형식 종료
    public sealed class ProgressUnlockDefinition // 확정된 해금 항목 정의
    { // 정의 시작
        public readonly CharacterType Character; // 해금 캐릭터
        public readonly string Id; // 해금 항목 식별자
        public readonly string PrerequisiteId; // 선행 해금 식별자
        public readonly int CommonCost; // 항목별 공통 재료 비용
        public readonly int CharacterCost; // 항목별 전용 재료 비용
        public ProgressUnlockDefinition(CharacterType character, string id, string prerequisiteId, int commonCost, int characterCost) // 확정된 항목 구성
        { // 구성 시작
            Character = character; // 해금 캐릭터 보관
            Id = id; // 항목 식별자 보관
            PrerequisiteId = prerequisiteId; // 선행 항목 보관
            CommonCost = commonCost; // 공통 비용 보관
            CharacterCost = characterCost; // 전용 비용 보관
        } // 구성 종료
    } // 정의 종료
    public interface IProgressUnlockCatalog // 게임에서 확정한 해금 목록
    { // 목록 계약 시작
        bool TryGet(CharacterType character, string optionId, out ProgressUnlockDefinition definition); // 확정 항목 조회
    } // 목록 계약 종료
    public sealed class EmptyProgressUnlockCatalog : IProgressUnlockCatalog // 아직 확정된 항목이 없는 목록
    { // 빈 목록 시작
        public bool TryGet(CharacterType character, string optionId, out ProgressUnlockDefinition definition) // 실제 해금 항목 조회
        { // 조회 시작
            definition = null; // 미정 항목 반환
            return false; // 임의의 해금 차단
        } // 조회 종료
    } // 빈 목록 종료
    public sealed class PermanentProgressService : IDisposable // 영구 성장과 단일 정산 서비스
    { // 서비스 시작
        private readonly IProgressStorage storage; // 주입된 저장소
        private readonly IProgressUnlockCatalog unlockCatalog; // 확정된 해금 항목 목록
        private readonly IAchievementVerifier achievementVerifier; // 명시적으로 주입한 과제 증거 검증기
        private ProgressSnapshot snapshot; // 마지막 저장 완료 상태
        private string expectedRaw; // 마지막으로 확인한 저장 원문
        private IDisposable sessionHandle; // 게임 실행 동안 유지하는 저장소 소유
        private bool disposed; // 서비스 해제 상태
        public bool IsLoaded // 저장 데이터 사용 가능 여부
        { // 속성 시작
            get; // 상태 조회
            private set; // 상태 변경
        } // 속성 종료
        public int CommonMaterials => snapshot != null && IsLoaded ? snapshot.commonMaterials : 0; // 현재 공통 재료
        public PermanentProgressService(IProgressStorage store) : this(store, new EmptyProgressUnlockCatalog()) // 기본 빈 해금 목록
        { // 생성 시작
        } // 생성 종료
        public PermanentProgressService(IProgressStorage store, IProgressUnlockCatalog catalog) : this(store, catalog, null) // 검증기 없는 기본 경로
        { // 생성 시작
        } // 생성 종료
        internal PermanentProgressService(IProgressStorage store, IProgressUnlockCatalog catalog, IAchievementVerifier verifier) // 격리된 검증기 주입
        { // 생성 시작
            storage = store; // 저장소 보관
            unlockCatalog = catalog; // 확인된 해금 목록 보관
            achievementVerifier = verifier; // 명시된 증거 검증기 보관
        } // 생성 종료
        public bool Load(out string error) // 저장된 전체 스냅샷 복원
        { // 복원 시작
            IsLoaded = false; // 유효성 확인 전 사용 차단
            error = null; // 오류 초기화
            if (storage == null || disposed) // 저장소와 서비스 상태 확인
            { // 오류 시작
                error = "영구 저장소가 없거나 이미 종료되었습니다"; // 저장소 오류 안내
                return false; // 저장 차단
            } // 오류 종료
            if (sessionHandle == null && storage is IProgressSessionStorage sessionStorage) // 파일 저장소의 세션 잠금 확인
            { // 잠금 확보 시작
                sessionHandle = sessionStorage.AcquireSession(out error); // 실행 종료까지 배타 소유
                if (sessionHandle == null) // 다른 실행 소유 확인
                { // 잠금 오류 시작
                    error = string.IsNullOrWhiteSpace(error) ? "다른 실행에서 성장 데이터를 사용 중입니다" : error; // 기본 오류 안내
                    return false; // 복구와 출격 차단
                } // 잠금 오류 종료
            } // 잠금 확보 종료
            try // 외부 저장소 접근
            { // 접근 시작
                ProgressReadStatus status = storage.Read(out string raw, out error); // 원문과 상태 조회
                if (status == ProgressReadStatus.Error) // 읽기 오류 확인
                { // 오류 시작
                    error = string.IsNullOrWhiteSpace(error) ? "영구 성장 파일 읽기 실패" : error; // 기본 오류 문구
                    return false; // 원본 보존
                } // 오류 종료
                if (status == ProgressReadStatus.Missing) // 최초 실행 확인
                { // 신규 상태 시작
                    snapshot = new ProgressSnapshot(); // 빈 성장 상태 생성
                    expectedRaw = null; // 기존 파일 없음 기록
                    IsLoaded = true; // 성장 서비스 사용 가능
                    return true; // 최초 상태 준비
                } // 신규 상태 종료
                if (status != ProgressReadStatus.Found || string.IsNullOrWhiteSpace(raw)) // 원문 상태 확인
                { // 오류 시작
                    error = "영구 성장 데이터가 비어 있습니다"; // 손상 오류 안내
                    return false; // 빈 원문 덮어쓰기 차단
                } // 오류 종료
                if (!ProgressJsonValidation.IsValid(raw, out int schemaVersion)) // 버전별 필수 필드와 타입 확인
                { // 형식 오류 시작
                    error = "영구 성장 데이터가 손상되었거나 지원되지 않습니다"; // 손상 오류 안내
                    return false; // 원본 보존
                } // 형식 오류 종료
                ProgressSnapshot parsed = schemaVersion == 2 ? (ProgressSnapshot)JsonUtility.FromJson<ProgressSnapshotV2>(raw) : JsonUtility.FromJson<ProgressSnapshot>(raw); // 확인된 버전으로 역직렬화
                if (!Validate(parsed)) // 저장 값과 범위 확인
                { // 오류 시작
                    error = "영구 성장 데이터가 손상되었거나 지원되지 않습니다"; // 손상 오류 안내
                    return false; // 원본 보존
                } // 오류 종료
                snapshot = parsed; // 확인된 저장 상태 채택
                expectedRaw = raw; // 확인된 원문 충돌 비교
                IsLoaded = true; // 성장 서비스 사용 가능
                return true; // 복원 완료
            } // 접근 종료
            catch (Exception exception) // 읽기와 역직렬화 예외
            { // 오류 시작
                error = "영구 성장 데이터 불러오기 실패: " + exception.Message; // 오류 안내
                return false; // 저장 차단
            } // 오류 종료
        } // 복원 종료
        public int Mastery(CharacterType character) // 캐릭터별 숙련도 조회
        { // 조회 시작
            return !IsLoaded || !ValidCharacter(character) ? 0 : character == CharacterType.Rapid ? snapshot.rapidMastery : snapshot.sniperMastery; // 캐릭터별 값
        } // 조회 종료
        public int CharacterMaterials(CharacterType character) // 캐릭터별 재료 조회
        { // 조회 시작
            return !IsLoaded || !ValidCharacter(character) ? 0 : character == CharacterType.Rapid ? snapshot.rapidMaterials : snapshot.sniperMaterials; // 캐릭터별 값
        } // 조회 종료
        public bool IsUnlocked(CharacterType character, string optionId) // 해금 여부 조회
        { // 조회 시작
            return IsLoaded && ValidCharacter(character) && ValidId(optionId) && Unlocks(snapshot, character).Contains(optionId); // 캐릭터별 해금 확인
        } // 조회 종료
        internal bool HasAchievement(CharacterAchievement achievement) // 내부 과제 기록 조회
        { // 조회 시작
            if (!IsLoaded || !(snapshot is ProgressSnapshotV2 upgraded) || upgraded.achievementReceipts == null) // 유효한 과제 저장 확인
            { // 미기록 시작
                return false; // 이전 버전 또는 미로드 상태
            } // 미기록 종료
            foreach (ProgressAchievementReceipt receipt in upgraded.achievementReceipts) // 별도 과제 기록 순회
            { // 조회 반복 시작
                if (receipt.achievement == achievement) // 과제 일치 확인
                { // 기록 확인 시작
                    return true; // 기존 과제 기록 확인
                } // 기록 확인 종료
            } // 조회 반복 종료
            return false; // 기록되지 않은 과제
        } // 조회 종료
        internal bool TryRecordAchievement(CharacterAchievement achievement, string runId, out string error) // 검증된 내부 과제 지급 요청
        { // 요청 시작
            error = null; // 이전 오류 제거
            if (!IsLoaded || !ValidId(runId) || !AchievementCharacter(achievement, out CharacterType character) || achievementVerifier == null) // 서비스와 검증기 확인
            { // 차단 시작
                error = "확인된 과제 증거를 사용할 수 없습니다"; // 미활성 판정 안내
                return false; // 무증거 지급 차단
            } // 차단 종료
            bool active = snapshot.activeRunId == runId && snapshot.activeCharacter == character; // 진행 중인 해당 캐릭터 출격
            ProgressReceipt run = FindReceipt(snapshot, runId); // 이미 정산된 출격 조회
            if (!active && (run == null || run.character != character)) // 출격과 캐릭터 소속 확인
            { // 차단 시작
                error = "과제와 출격 정보가 일치하지 않습니다"; // 소속 오류 안내
                return false; // 확인되지 않은 출격 차단
            } // 차단 종료
            try // 주입된 증거 검증
            { // 검증 범위 시작
                if (!achievementVerifier.IsVerified(achievement, runId)) // 과제 충족 증거 확인
                { // 차단 시작
                    error = "과제 달성 근거를 확인하지 못했습니다"; // 미확인 증거 안내
                    return false; // 조건 임의 확정 차단
                } // 차단 종료
            } // 검증 범위 종료
            catch (Exception exception) // 검증기 오류 확인
            { // 오류 시작
                error = "과제 증거 검증 실패: " + exception.Message; // 검증 실패 안내
                return false; // 불확실한 증거 차단
            } // 오류 종료
            if (HasAchievement(achievement)) // 최초 지급 여부 확인
            { // 재조회 시작
                return true; // 기존 지급 유지와 중복 저장 생략
            } // 재조회 종료
            try // 과제 보상과 이행 준비
            { // 준비 범위 시작
                ProgressSnapshotV2 next = snapshot.schemaVersion == 2 ? (ProgressSnapshotV2)Clone(snapshot) : JsonUtility.FromJson<ProgressSnapshotV2>(JsonUtility.ToJson(snapshot)); // 이전 상태의 독립 신규 형식
                if (snapshot.schemaVersion == 1) // 최초 이행 확인
                { // 이행 시작
                    next.schemaVersion = 2; // 과제 기록 버전 설정
                    next.achievementReceipts = new List<ProgressAchievementReceipt>(); // 이전 형식의 빈 과제 목록
                } // 이행 종료
                if (character == CharacterType.Rapid) // 속사형 과제 확인
                { // 속사 보상 시작
                    next.rapidMaterials = checked(next.rapidMaterials + 1); // 전용 재료 한 개 추가
                } // 속사 보상 종료
                else // 저격형 과제
                { // 저격 보상 시작
                    next.sniperMaterials = checked(next.sniperMaterials + 1); // 전용 재료 한 개 추가
                } // 저격 보상 종료
                ProgressAchievementReceipt receipt = new ProgressAchievementReceipt(); // 독립 과제 영수증 생성
                receipt.achievement = achievement; // 과제 식별자 기록
                receipt.runId = runId; // 검증한 출격 기록
                receipt.character = character; // 보상 캐릭터 기록
                receipt.characterMaterialsGained = 1; // 최초 전용 재료 기록
                next.achievementReceipts.Add(receipt); // 별도 과제 영수증 추가
                return Commit(next, out error); // 보상과 기록의 원자 저장
            } // 준비 범위 종료
            catch (OverflowException) // 보상 숫자 초과 확인
            { // 범위 오류 시작
                error = "과제 보상이 저장 가능한 범위를 초과했습니다"; // 범위 초과 안내
                return false; // 이전 상태 보존
            } // 범위 오류 종료
        } // 요청 종료
        public bool TryBegin(string runId, CharacterType character, out string error) // 실제 출격 시작 저장
        { // 시작 처리
            error = null; // 오류 초기화
            if (!IsLoaded || !ValidId(runId) || !ValidCharacter(character)) // 서비스와 출격 식별 확인
            { // 입력 오류 시작
                error = "출격 정보 또는 저장 상태가 올바르지 않습니다"; // 오류 안내
                return false; // 출격 차단
            } // 입력 오류 종료
            if (!string.IsNullOrEmpty(snapshot.activeRunId)) // 미정산 출격 확인
            { // 기존 출격 확인 시작
                if (snapshot.activeRunId == runId && snapshot.activeCharacter == character) // 같은 시작 기록 확인
                { // 중복 시작 처리
                    return true; // 같은 출격의 멱등 재확인
                } // 중복 시작 처리 종료
                error = "이전 출격이 아직 정산되지 않았습니다"; // 복구 필요 안내
                return false; // 다른 출격 차단
            } // 기존 출격 확인 종료
            if (FindReceipt(snapshot, runId) != null) // 완료한 출격 확인
            { // 중복 식별자 시작
                error = "이미 정산된 출격 식별자입니다"; // 중복 안내
                return false; // 이전 출격 재사용 차단
            } // 중복 식별자 종료
            ProgressSnapshot next = Clone(snapshot); // 기존 상태 복사
            next.activeRunId = runId; // 출격 식별자 기록
            next.activeCharacter = character; // 출격 캐릭터 기록
            return Commit(next, out error); // 저장 성공 시에만 시작 확정
        } // 시작 처리 종료
        public bool TryRecoverPending(out SettlementReceipt receipt, out string error) // 이전 미정산 출격 실패 복구
        { // 복구 시작
            receipt = null; // 지급 영수증 초기화
            error = null; // 오류 초기화
            if (!IsLoaded) // 저장 데이터 로드 확인
            { // 로드 오류 시작
                error = "영구 성장 데이터를 먼저 불러와야 합니다"; // 오류 안내
                return false; // 미확인 데이터 수정 차단
            } // 로드 오류 종료
            if (string.IsNullOrEmpty(snapshot.activeRunId)) // 미정산 출격 확인
            { // 복구 생략 시작
                return true; // 추가 지급 없이 정상 처리
            } // 복구 생략 종료
            return TrySettle(snapshot.activeRunId, snapshot.activeCharacter, false, out receipt, out error); // 저장된 출격의 실패 정산
        } // 복구 종료
        public bool TrySettle(string runId, CharacterType character, bool cleared, out SettlementReceipt receipt, out string error) // 기존 정산 경로
        { // 정산 시작
            return TrySettle(runId, character, cleared, null, out receipt, out error); // 과제 증거 없는 기존 정산
        } // 정산 종료
        internal bool TrySettle(string runId, CharacterType character, bool cleared, RapidAchievementRun evidence, out SettlementReceipt receipt, out string error) // 증거 포함 단일 정산
        { // 정산 시작
            receipt = null; // 결과 초기화
            error = null; // 오류 초기화
            if (!IsLoaded || !ValidId(runId) || !ValidCharacter(character)) // 저장과 출격 정보 확인
            { // 입력 오류 시작
                error = "출격 정보 또는 저장 상태가 올바르지 않습니다"; // 오류 안내
                return false; // 정산 차단
            } // 입력 오류 종료
            ProgressReceipt existing = FindReceipt(snapshot, runId); // 기존 정산 조회
            if (existing != null) // 기존 출격 확인
            { // 중복 처리 시작
                if (existing.character != character || existing.cleared != cleared) // 저장된 정산 내용 비교
                { // 불일치 시작
                    error = "기존 출격 정산 내용이 일치하지 않습니다"; // 오류 안내
                    return false; // 기존 보상 보존
                } // 불일치 종료
                receipt = new SettlementReceipt(existing, true, AchievementGain(snapshot, runId)); // 기존 출격의 별도 과제 포함
                return true; // 중복 지급 없이 조회
            } // 중복 처리 종료
            if (snapshot.activeRunId != runId || snapshot.activeCharacter != character) // 시작 기록 대조
            { // 불일치 시작
                error = "시작 기록과 출격 결과가 일치하지 않습니다"; // 오류 안내
                return false; // 다른 출격 정산 차단
            } // 불일치 종료
            try // 안전한 정산 계산
            { // 계산 시작
                int gainedMastery = cleared ? 20 : 10; // 참여와 클리어 숙련도 시험값
                int gainedCommon = cleared ? 6 : 3; // 참여와 클리어 공통 재료 시험값
                int oldMastery = Mastery(character); // 이전 캐릭터 숙련도
                int nextMastery = checked(oldMastery + gainedMastery); // 범위 검사한 새 숙련도
                int gainedCharacter = nextMastery / 60 - oldMastery / 60; // 캐릭터별 60 경계 보상
                int nextCommon = checked(snapshot.commonMaterials + gainedCommon); // 범위 검사한 공통 재료
                bool awardRapid = character == CharacterType.Rapid && evidence != null && evidence.Qualifies(runId) && !HasAchievement(CharacterAchievement.RapidBasicHits50); // 실제 출격의 최초 과제 확인
                int nextCharacter = checked(CharacterMaterials(character) + gainedCharacter + (awardRapid ? 1 : 0)); // 숙련과 과제 재료 합산
                ProgressSnapshot next = awardRapid && snapshot.schemaVersion == 1 ? JsonUtility.FromJson<ProgressSnapshotV2>(JsonUtility.ToJson(snapshot)) : Clone(snapshot); // 과제 시 독립 신규 형식 이행
                if (awardRapid && snapshot.schemaVersion == 1) // 최초 과제 형식 확인
                { // 이행 시작
                    next.schemaVersion = 2; // 과제 형식 버전 설정
                    ((ProgressSnapshotV2)next).achievementReceipts = new List<ProgressAchievementReceipt>(); // 별도 과제 목록 준비
                } // 이행 종료
                next.commonMaterials = nextCommon; // 공통 재료 누적
                if (character == CharacterType.Rapid) // 속사형 정산 확인
                { // 속사형 반영 시작
                    next.rapidMastery = nextMastery; // 속사형 숙련도 저장
                    next.rapidMaterials = nextCharacter; // 속사형 전용 재료 저장
                } // 속사형 반영 종료
                else // 저격형 정산
                { // 저격형 반영 시작
                    next.sniperMastery = nextMastery; // 저격형 숙련도 저장
                    next.sniperMaterials = nextCharacter; // 저격형 전용 재료 저장
                } // 저격형 반영 종료
                ProgressReceipt result = new ProgressReceipt(); // 새 정산 내역 생성
                result.runId = runId; // 출격 식별자 기록
                result.character = character; // 출격 캐릭터 기록
                result.cleared = cleared; // 클리어 여부 기록
                result.masteryGained = gainedMastery; // 숙련도 보상 기록
                result.commonMaterialsGained = gainedCommon; // 공통 재료 보상 기록
                result.characterMaterialsGained = gainedCharacter; // 전용 재료 보상 기록
                next.receipts.Add(result); // 정산 완료 이력 추가
                if (awardRapid) // 최초 과제 지급 확인
                { // 과제 영수증 시작
                    ProgressAchievementReceipt achievement = new ProgressAchievementReceipt(); // 별도 과제 영수증
                    achievement.achievement = CharacterAchievement.RapidBasicHits50; // 아리아 첫 과제
                    achievement.runId = runId; // 과제를 수행한 출격
                    achievement.character = CharacterType.Rapid; // 과제 대상 캐릭터
                    achievement.characterMaterialsGained = 1; // 최초 전용 증표
                    ((ProgressSnapshotV2)next).achievementReceipts.Add(achievement); // 정산과 같은 저장에 기록
                } // 과제 영수증 종료
                next.activeRunId = ""; // 진행 중 출격 해제
                if (!Commit(next, out error)) // 원자 저장 확인
                { // 실패 시작
                    return false; // 지급 완료 표시 차단
                } // 실패 종료
                receipt = new SettlementReceipt(result, false, awardRapid ? 1 : 0); // 저장된 실제 보상 반환
                return true; // 최초 정산 완료
            } // 계산 종료
            catch (OverflowException) // 보상 숫자 범위 초과
            { // 범위 오류 시작
                error = "보상 수치가 저장 가능한 범위를 초과했습니다"; // 범위 오류 안내
                return false; // 기존 상태 유지
            } // 범위 오류 종료
        } // 정산 종료
        public bool TryUnlock(CharacterType character, string optionId, out string error) // 확정된 후보 직접 해금
        { // 해금 시작
            error = null; // 오류 초기화
            if (!IsLoaded || !ValidCharacter(character) || !ValidId(optionId) || unlockCatalog == null) // 저장과 선택 정보 확인
            { // 입력 오류 시작
                error = "해금 정보 또는 저장 상태가 올바르지 않습니다"; // 오류 안내
                return false; // 해금 차단
            } // 입력 오류 종료
            ProgressUnlockDefinition definition; // 확정된 항목 정의
            try // 신뢰된 목록 조회
            { // 조회 시작
                if (!unlockCatalog.TryGet(character, optionId, out definition) || definition == null) // 확정 항목 확인
                { // 목록 오류 시작
                    error = "해금할 수 없는 선택지입니다"; // 미정 항목 안내
                    return false; // 임의 식별자 차단
                } // 목록 오류 종료
            } // 조회 종료
            catch (Exception exception) // 목록 조회 오류
            { // 조회 오류 시작
                error = "해금 목록 조회 실패: " + exception.Message; // 오류 안내
                return false; // 불확실한 해금 차단
            } // 조회 오류 종료
            if (definition.Character != character || !string.Equals(definition.Id, optionId, StringComparison.Ordinal) || definition.CommonCost < 0 || definition.CharacterCost < 0 || (definition.CommonCost == 0 && definition.CharacterCost == 0) || (!string.IsNullOrEmpty(definition.PrerequisiteId) && !ValidId(definition.PrerequisiteId))) // 항목별 비용과 선행 조건 검사
            { // 정의 오류 시작
                error = "해금 항목 정의가 올바르지 않습니다"; // 항목 오류 안내
                return false; // 잘못된 정의 차단
            } // 정의 오류 종료
            if (IsUnlocked(character, optionId)) // 중복 해금 확인
            { // 중복 시작
                error = "이미 해금한 선택지입니다"; // 중복 해금 안내
                return false; // 중복 비용 차감 차단
            } // 중복 종료
            if (!string.IsNullOrEmpty(definition.PrerequisiteId) && !IsUnlocked(character, definition.PrerequisiteId)) // 선행 해금 확인
            { // 선행 조건 시작
                error = "선행 해금 조건을 만족하지 않았습니다"; // 선행 조건 안내
                return false; // 순서 외 해금 차단
            } // 선행 조건 종료
            if (snapshot.commonMaterials < definition.CommonCost || CharacterMaterials(character) < definition.CharacterCost) // 해당 항목 비용 확인
            { // 재료 부족 시작
                error = "해금 재료가 부족합니다"; // 비용 오류 안내
                return false; // 부족한 해금 차단
            } // 재료 부족 종료
            ProgressSnapshot next = Clone(snapshot); // 기존 상태 복사
            next.commonMaterials -= definition.CommonCost; // 정의된 공통 비용 차감
            if (character == CharacterType.Rapid) // 속사형 해금 확인
            { // 속사형 반영 시작
                next.rapidMaterials -= definition.CharacterCost; // 정의된 전용 비용 차감
            } // 속사형 반영 종료
            else // 저격형 해금
            { // 저격형 반영 시작
                next.sniperMaterials -= definition.CharacterCost; // 정의된 전용 비용 차감
            } // 저격형 반영 종료
            Unlocks(next, character).Add(optionId); // 선택한 확정 후보 기록
            return Commit(next, out error); // 비용과 해금 동시 저장
        } // 해금 종료
        public void Dispose() // 저장소 세션 종료
        { // 종료 시작
            if (disposed) // 중복 종료 확인
            { // 중복 시작
                return; // 기존 종료 상태 유지
            } // 중복 종료
            disposed = true; // 서비스 종료 기록
            IsLoaded = false; // 추가 쓰기 차단
            if (sessionHandle != null) // 파일 잠금 존재 확인
            { // 잠금 해제 시작
                sessionHandle.Dispose(); // 운영체제 파일 핸들 반환
                sessionHandle = null; // 이전 잠금 참조 제거
            } // 잠금 해제 종료
        } // 종료 완료
        private bool Commit(ProgressSnapshot next, out string error) // 전체 스냅샷 저장
        { // 저장 시작
            error = null; // 오류 초기화
            try // 직렬화와 쓰기 시도
            { // 저장 범위 시작
                if (!Validate(next)) // 저장 후보 유효성 확인
                { // 오류 시작
                    error = "저장할 영구 성장 값이 올바르지 않습니다"; // 오류 안내
                    return false; // 잘못된 상태 보존 차단
                } // 오류 종료
                string serialized = JsonUtility.ToJson(next); // 전체 상태 직렬화
                if (!ProgressJsonValidation.IsValid(serialized)) // 복원 가능한 크기와 구조 확인
                { // 저장 크기 오류 시작
                    error = "영구 성장 데이터가 저장 가능한 크기나 형식을 벗어났습니다"; // 복원 불가 오류 안내
                    return false; // 기존 저장과 메모리 보존
                } // 저장 크기 오류 종료
                bool written; // 저장 응답 결과
                string writeError; // 저장 오류 또는 예외 안내
                try // 저장소 기록 요청
                { // 저장 요청 시작
                    written = storage.Write(expectedRaw, serialized, out writeError); // 확인한 원문과 비교 후 원자 저장
                } // 저장 요청 종료
                catch (Exception exception) // 저장 응답 예외
                { // 예외 처리 시작
                    written = false; // 저장 상태 재확인 필요
                    writeError = "영구 성장 데이터 저장 실패: " + exception.Message; // 원래 예외 안내 보관
                } // 예외 처리 종료
                if (!written) // 저장 실패 또는 응답 예외 확인
                { // 저장 실패 시작
                    writeError = string.IsNullOrWhiteSpace(writeError) ? "영구 성장 데이터 저장 실패" : writeError; // 원래 저장 오류 보관
                    bool confirmed = false; // 목표 원문 확인 여부
                    try // 저장 결과 재조회
                    { // 재조회 시작
                        ProgressReadStatus status = storage.Read(out string current, out string readError); // 실제 저장 원문 확인
                        confirmed = status == ProgressReadStatus.Found && string.IsNullOrEmpty(readError) && string.Equals(current, serialized, StringComparison.Ordinal); // 목표 원문과 정확히 일치하는 경우만 확인
                    } // 재조회 종료
                    catch (Exception) // 재조회 예외
                    { // 재조회 실패 시작
                        confirmed = false; // 불확실한 저장 상태 유지
                    } // 재조회 실패 종료
                    if (!confirmed) // 저장 결과 미확인
                    { // 확인 실패 시작
                        error = writeError; // 원래 저장 오류 유지
                        return false; // 이전 메모리 상태 보존
                    } // 확인 실패 종료
                    error = null; // 실제 원문 확인 후 오류 해제
                } // 저장 실패 종료
                snapshot = next; // 저장이 끝난 상태만 반영
                expectedRaw = serialized; // 저장된 원문 보관
                return true; // 저장 완료
            } // 저장 범위 종료
            catch (Exception exception) // 직렬화 또는 저장 오류
            { // 예외 처리 시작
                error = "영구 성장 데이터 저장 실패: " + exception.Message; // 오류 안내
                return false; // 이전 상태 유지
            } // 예외 처리 종료
        } // 저장 종료
        private static ProgressSnapshot Clone(ProgressSnapshot value) // 변경 전 깊은 복사
        { // 복사 시작
            string raw = JsonUtility.ToJson(value); // 실제 버전의 직렬화 원문
            return value.schemaVersion == 2 ? (ProgressSnapshot)JsonUtility.FromJson<ProgressSnapshotV2>(raw) : JsonUtility.FromJson<ProgressSnapshot>(raw); // 버전별 독립 복사
        } // 복사 종료
        private static bool ValidCharacter(CharacterType character) // 허용 캐릭터 확인
        { // 검사 시작
            return character == CharacterType.Rapid || character == CharacterType.Sniper; // 초기 캐릭터만 허용
        } // 검사 종료
        private static bool ValidId(string value) // 식별자 안전 검사
        { // 검사 시작
            return !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value == value.Trim(); // 공백과 길이 제한
        } // 검사 종료
        private static List<string> Unlocks(ProgressSnapshot value, CharacterType character) // 캐릭터별 해금 목록
        { // 조회 시작
            return character == CharacterType.Rapid ? value.rapidUnlocks : value.sniperUnlocks; // 해당 캐릭터 목록
        } // 조회 종료
        private static int AchievementGain(ProgressSnapshot value, string runId) // 해당 출격의 별도 과제 보상
        { // 조회 시작
            if (value is ProgressSnapshotV2 upgraded) // 과제 형식 확인
            { // 과제 조회 시작
                foreach (ProgressAchievementReceipt achievement in upgraded.achievementReceipts) // 과제 영수증 순회
                { // 순회 시작
                    if (achievement.runId == runId) // 같은 출격 확인
                    { // 보상 확인 시작
                        return achievement.characterMaterialsGained; // 저장된 과제 증표 반환
                    } // 보상 확인 종료
                } // 순회 종료
            } // 과제 조회 종료
            return 0; // 해당 출격의 과제 보상 없음
        } // 조회 종료
        private static ProgressReceipt FindReceipt(ProgressSnapshot value, string runId) // 출격 정산 내역 검색
        { // 검색 시작
            foreach (ProgressReceipt receipt in value.receipts) // 보관 영수증 순회
            { // 비교 시작
                if (string.Equals(receipt.runId, runId, StringComparison.Ordinal)) // 식별자 일치 확인
                { // 발견 시작
                    return receipt; // 기존 영수증 반환
                } // 발견 종료
            } // 비교 종료
            return null; // 미정산 출격
        } // 검색 종료
        private static bool Validate(ProgressSnapshot value) // 저장 상태의 범위와 무결성 확인
        { // 검사 시작
            if (value == null || (value.schemaVersion != 1 && value.schemaVersion != 2) || value.commonMaterials < 0 || value.rapidMastery < 0 || value.sniperMastery < 0 || value.rapidMaterials < 0 || value.sniperMaterials < 0 || value.rapidUnlocks == null || value.sniperUnlocks == null || value.receipts == null) // 형식과 음수 검사
            { // 오류 시작
                return false; // 잘못된 저장 차단
            } // 오류 종료
            if (!string.IsNullOrEmpty(value.activeRunId) && (!ValidId(value.activeRunId) || !ValidCharacter(value.activeCharacter))) // 진행 중 출격 검사
            { // 오류 시작
                return false; // 잘못된 출격 차단
            } // 오류 종료
            HashSet<string> runIds = new HashSet<string>(StringComparer.Ordinal); // 중복 출격 검색 집합
            foreach (ProgressReceipt receipt in value.receipts) // 정산 이력 검사
            { // 영수증 검사 시작
                if (receipt == null || !ValidId(receipt.runId) || !ValidCharacter(receipt.character) || !runIds.Add(receipt.runId) || receipt.masteryGained != (receipt.cleared ? 20 : 10) || receipt.commonMaterialsGained != (receipt.cleared ? 6 : 3) || receipt.characterMaterialsGained < 0 || receipt.characterMaterialsGained > 1) // 영수증 내용 검사
                { // 오류 시작
                    return false; // 손상된 정산 차단
                } // 오류 종료
            } // 영수증 검사 종료
            if (!string.IsNullOrEmpty(value.activeRunId) && runIds.Contains(value.activeRunId)) // 진행과 완료 중복 검사
            { // 오류 시작
                return false; // 모순된 출격 차단
            } // 오류 종료
            return ValidUnlocks(value.rapidUnlocks) && ValidUnlocks(value.sniperUnlocks) && ValidAchievements(value); // 해금과 과제 기록 검사
        } // 검사 종료
        private static bool ValidAchievements(ProgressSnapshot value) // 버전별 과제 영수증 검사
        { // 검사 시작
            if (value.schemaVersion == 1) // 기존 형식 확인
            { // 이전 버전 시작
                return !(value is ProgressSnapshotV2); // 과제 필드가 없는 기존 형식
            } // 이전 버전 종료
            if (!(value is ProgressSnapshotV2 upgraded) || upgraded.achievementReceipts == null) // 신규 형식과 목록 확인
            { // 오류 시작
                return false; // 과제 목록 누락 차단
            } // 오류 종료
            HashSet<CharacterAchievement> seen = new HashSet<CharacterAchievement>(); // 과제별 중복 검색
            foreach (ProgressAchievementReceipt receipt in upgraded.achievementReceipts) // 별도 과제 영수증 순회
            { // 영수증 검사 시작
                if (receipt == null || !ValidId(receipt.runId) || receipt.characterMaterialsGained != 1 || !AchievementCharacter(receipt.achievement, out CharacterType character) || receipt.character != character || !seen.Add(receipt.achievement)) // 과제와 캐릭터 보상 확인
                { // 오류 시작
                    return false; // 잘못된 과제 또는 중복 차단
                } // 오류 종료
                bool active = receipt.runId == value.activeRunId && receipt.character == value.activeCharacter; // 진행 중 출격 확인
                ProgressReceipt run = FindReceipt(value, receipt.runId); // 종료된 출격 확인
                if (!active && (run == null || run.character != receipt.character)) // 출격 소속 확인
                { // 오류 시작
                    return false; // 확인되지 않은 출격 차단
                } // 오류 종료
            } // 영수증 검사 종료
            return true; // 모든 과제 기록 유효
        } // 검사 종료
        private static bool AchievementCharacter(CharacterAchievement achievement, out CharacterType character) // 과제의 캐릭터 확인
        { // 확인 시작
            character = achievement == CharacterAchievement.SniperWeakHits10 ? CharacterType.Sniper : CharacterType.Rapid; // 결정된 캐릭터 연결
            return achievement == CharacterAchievement.RapidBasicHits50 || achievement == CharacterAchievement.SniperWeakHits10; // 허용 과제 확인
        } // 확인 종료
        private static bool ValidUnlocks(List<string> values) // 해금 식별자 중복 검사
        { // 검사 시작
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal); // 중복 검색 집합
            foreach (string value in values) // 해금 목록 순회
            { // 식별자 검사 시작
                if (!ValidId(value) || !ids.Add(value)) // 유효성과 중복 확인
                { // 오류 시작
                    return false; // 잘못된 목록 차단
                } // 오류 종료
            } // 식별자 검사 종료
            return true; // 목록 검증 완료
        } // 검사 종료
    } // 서비스 종료
} // 영역 종료
