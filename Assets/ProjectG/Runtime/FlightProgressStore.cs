using System; // 저장 수명과 출격 고유 ID
using System.IO; // 파일 교체와 배타 저장
using System.Text; // JSON 저장 문자 인코딩
using UnityEngine; // JSON 변환과 독립 시험 분리

namespace ProjectG // 프로젝트 영구 성장 영역
{
    [Serializable] // 저장 파일의 최근 출격 영수증
    public sealed class FlightProgressReceipt // 결과 화면과 중단 복구의 확정 보상
    {
        public string RunId; // 한 번만 정산하는 출격 ID
        public FlightBroomPilot Pilot; // 실제 첫 전투에 참가한 조종사
        public bool Cleared; // 실패와 정상 최종 격파 구분
        public bool Recovered; // 강제 종료 뒤 실패로 복원한 출격
        public int Mastery; // 이번 출격의 숙련도 증가
        public int Crystals; // 이번 출격의 공통 천공 결정 증가
        public int Gold; // 이번 출격의 골드 지급량
        public int Marks; // 이번 출격의 조종사 조율의 증표 증가
        public bool FirstTask; // 이번 정상 결과에서 최초 과제 지급
    }

    public sealed class FlightProgressStore : IDisposable // 출격당 원자 정산과 재실행 복구
    {
        [Serializable] // 캐릭터별 숙련도와 전용 재료
        private sealed class PilotProgress // 전투 임시 성장과 독립된 누적값
        {
            public int mastery; // 경계 보상 산정에 쓰는 누적 숙련도
            public int marks; // 보유한 해당 캐릭터 전용 재료
            public bool commonFan; // 마을에서 장착한 산탄 공용 스킬
            public bool completedFirstTask; // 해당 조종사의 과제 보상 지급 이력
        }

        [Serializable] // 상점에서 구매하거나 공방에서 강화한 개별 부품
        private sealed class BroomProgress // 기존 보유 ID와 결합한 영구 데이터
        {
            public string id; // 같은 종류에서도 고정된 개별 ID
            public bool purchased; // 상점에서 구입한 보유 권한
            public int upgrade; // 실제 공방에서 강화한 단계
        }

        [Serializable] // 한 번에 교체하는 단일 저장 스냅샷
        private sealed class Save // 진행 중인 출격과 보유 재화
        {
            public int version = 1; // 첫 3D 영구 성장 저장 형식
            public int crystals; // 모든 조종사의 공용 천공 결정
            public int gold; // 모든 조종사의 공용 골드
            public PilotProgress[] pilots = new PilotProgress[Enum.GetValues(typeof(FlightBroomPilot)).Length]; // 열거형 순서의 캐릭터별 성장
            public string pendingId; // 실제 참여 후 정산 전인 출격 ID
            public FlightBroomPilot pendingPilot; // 중단 출격을 복구할 캐릭터
            public FlightProgressReceipt recent; // 마지막 지급의 중복 요청 확인
            public string[] unlocks = { "pilot.aria", "pilot.rian" }; // 기존 출격 선택에서 제공하는 기본 해금
            public BroomProgress[] brooms = Array.Empty<BroomProgress>(); // 시작 부품 이후 추가한 개별 보유와 강화 내역
        }

        private const int MarkInterval = 60; // 전용 재료의 시험 숙련도 간격
        private static readonly int[] ShopPrices = { 900, 1800, 3000 }; // 일반·고급·희귀 부품의 기획 상점가
        private static readonly (int Crystals, int Gold)[] ForgePrices = { (3, 300), (4, 450), (5, 600), (7, 800), (9, 1100) }; // +1부터 +5까지 목표 단계 비용
        private readonly string path; // 단일 영구 저장 원본
        private FileStream ownership; // 같은 프로필에서 다른 실행의 쓰기 차단
        private bool primaryUnreadable; // 복구용 사본을 그대로 보호할 상태
        private Save state = Empty(); // 저장된 확정 상태와 메모리 시험 기본값
        public string Error { get; private set; } // 결과와 타이틀에 표시할 저장 오류
        public int SharedCrystals => state.crystals; // 프로필의 공통 재료
        public int Gold => state.gold; // 공방과 장비 상점에 쓰는 실제 골드
        public string PendingId => state.pendingId; // 아직 보상 지급이 없는 출격

        public FlightProgressStore(string filePath) // 보존할 파일 또는 격리 메모리 모드
        {
            path = filePath; // 실제 계정 진행 저장 위치
            if (path == null) // 사용자 저장을 사용하지 않는 Unity 배치 시험
            {
                return; // 초기 자료를 메모리에서만 검증
            }

            try // 다른 실행의 프로필 접근 확인
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)); // 저장 폴더 확인
                ownership = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); // 적용 중 다른 실행의 복구 및 지급 방지
                if (File.Exists(path) || File.Exists(path + ".bak")) // 기존 저장 또는 복구 사본
                {
                    Save original = Read(path); // 마지막으로 확정된 정산
                    Save loaded = original ?? Read(path + ".bak"); // 기존 원본이 손상된 경우 이전 확정본
                    if (loaded == null) // 두 파일에서 유효 기록을 읽을 수 없음
                    {
                        Error = "영구 저장 파일을 읽을 수 없습니다."; // 손상된 원본 보존 안내
                        Dispose(); // 사용자 파일 덮어쓰기 차단
                        return; // 임의 신규 프로필 생성 방지
                    }
                    state = loaded; // 정상 확인한 누적 기록
                    primaryUnreadable = original == null; // 다음 저장에서 좋은 백업 보존
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) // 배타 잠금 또는 파일 접근 실패
            {
                Error = "영구 저장에 접근할 수 없습니다."; // 실제 저장 실패 표시
                Dispose(); // 일부만 획득한 접근 해제
            }
        }

        public int GetMastery(FlightBroomPilot pilot) => state.pilots[(int)pilot].mastery; // 현재 캐릭터 누적 숙련도
        public int GetMarks(FlightBroomPilot pilot) => state.pilots[(int)pilot].marks; // 현재 캐릭터 전용 재료
        public bool HasUnlock(string id) => Array.IndexOf(state.unlocks, id) >= 0; // 실제로 저장된 영구 선택지
        internal bool GetCommonFan(FlightBroomPilot pilot) => state.pilots[(int)pilot].commonFan; // 캐릭터별 현재 공용 스킬
        internal bool OwnsBroomPart(string id) => FindBroom(state, id)?.purchased == true; // 상점에서 산 실제 개별 보유
        internal int BroomUpgrade(string id, int initial) => FindBroom(state, id)?.upgrade ?? initial; // 시작 부품과 강화한 부품의 실제 단계
        internal bool HasCompletedFirstTask(FlightBroomPilot pilot) => state.pilots[(int)pilot].completedFirstTask; // 각 조종사 최초 과제 정산

        internal bool BuyGrowthNode(string id, FlightBroomPilot pilot) // 공통 결정과 해당 조종사의 증표로 후속 카드 자격 구매
        {
            if (!FlightGrowthNodes.BelongsTo(id, pilot) || HasUnlock(id) || state.crystals < FlightGrowthNodes.CrystalCost || GetMarks(pilot) < FlightGrowthNodes.MarkCost) // 실제 기획과 보유 재료 검증
            {
                return false; // 같은 노드 재구매·다른 캐릭터·부족 재화 제외
            }
            Save next = Copy(); // 비용과 해금을 함께 기록할 새 상태
            next.crystals -= FlightGrowthNodes.CrystalCost; // 공통 결정 18
            next.pilots[(int)pilot].marks -= FlightGrowthNodes.MarkCost; // 해당 조종사 증표 1
            Array.Resize(ref next.unlocks, next.unlocks.Length + 1); // 기존 해금 목록에 공간 추가
            next.unlocks[next.unlocks.Length - 1] = id; // 캐릭터가 앞으로 얻을 실제 선택 자격
            return Write(next); // 두 비용과 권한을 동시에 저장
        }

        internal static int ShopPrice(int grade) => grade >= 1 && grade <= ShopPrices.Length ? ShopPrices[grade - 1] : 0; // 세 판매 등급의 가격

        internal static (int Crystals, int Gold) ForgePrice(int currentLevel) // 다음 목표 단계의 실제 비용
        {
            return currentLevel >= 0 && currentLevel < ForgePrices.Length ? ForgePrices[currentLevel] : default; // +5 이후의 요청 금지
        }

        internal bool SetCommonFan(FlightBroomPilot pilot, bool fan) // 보호막과 산탄의 영구 교체
        {
            if (GetCommonFan(pilot) == fan) // 이미 현재 선택을 장착함
            {
                return true; // 같은 조작의 중복 저장 제외
            }
            Save next = Copy(); // 기존 프로필과 재화 상태 보존
            next.pilots[(int)pilot].commonFan = fan; // 선택한 조종사에만 반영
            return Write(next); // 저장 성공과 실제 적용을 함께 전달
        }

        internal bool BuyBroomPart(string id, int grade) // 마을 상점의 개인 보유 및 골드 확정
        {
            int price = ShopPrice(grade); // 현재 상점의 기획 가격
            if (price <= 0 || state.gold < price || string.IsNullOrEmpty(id) || OwnsBroomPart(id)) // 미지원 부품·재화 부족·중복 구매 금지
            {
                return false; // 재화 또는 기존 부품 상태 유지
            }
            Save next = Copy(); // 구매 전의 기존 자산 보호
            BroomProgress item = EnsureBroom(next, id); // 같은 종류에서도 실제 개별 보유 식별
            item.purchased = true; // 공용 인벤토리에서 조회할 소유권
            next.gold -= price; // 골드와 보유 권한을 한 파일로 저장
            return Write(next); // 실제 기록을 마친 지급
        }

        internal bool UpgradeBroomPart(string id, int currentLevel) // 개별 장착 부품 강화 비용과 단계 확정
        {
            var cost = ForgePrice(currentLevel); // 상한까지의 결정과 골드
            if (string.IsNullOrEmpty(id) || cost.Gold <= 0 || BroomUpgrade(id, currentLevel) != currentLevel || state.crystals < cost.Crystals || state.gold < cost.Gold) // 최신 부품의 부족 재화와 오래된 입력 차단
            {
                return false; // 기존 부품과 재화 보존
            }
            Save next = Copy(); // 두 종류 비용과 능력치가 같은 파일에 반영
            EnsureBroom(next, id).upgrade = currentLevel + 1; // 유효한 현재 부품의 다음 단계
            next.crystals -= cost.Crystals; // 결정 소모
            next.gold -= cost.Gold; // 골드 소모
            return Write(next); // 비용과 강화 단계의 실제 저장
        }

        private static BroomProgress FindBroom(Save profile, string id) => Array.Find(profile.brooms, value => value.id == id); // 저장된 동일 개별 ID

        private static BroomProgress EnsureBroom(Save profile, string id) // 처음 구매하거나 강화하는 개별 부품
        {
            BroomProgress part = FindBroom(profile, id); // 기존 부품의 현재 업그레이드 확인
            if (part == null) // 이전 저장에 변경 내역이 없었던 부품
            {
                part = new BroomProgress { id = id }; // 개별 부품의 영구 저장 항목
                Array.Resize(ref profile.brooms, profile.brooms.Length + 1); // 기존 내역이 보존되는 배열 확장
                profile.brooms[profile.brooms.Length - 1] = part; // 새로운 변경 내역 등록
            }
            return part; // 구입 또는 강화의 공용 값
        }

        public string Begin(FlightBroomPilot pilot) // 첫 전투 진입을 파일에 먼저 확정
        {
            if (!string.IsNullOrEmpty(state.pendingId) || !ValidPilot(pilot)) // 다른 출격의 참여와 데이터 범위 보호
            {
                return null; // 첫 출격 정산 전에 기존 ID 보존
            }
            Save next = Copy(); // 기존 영구 재화의 임시 복사본
            next.pendingId = Guid.NewGuid().ToString("N"); // 같은 캐릭터도 출격마다 별도 ID
            next.pendingPilot = pilot; // 시작 후 바뀌지 않는 보상 대상
            return Write(next) ? next.pendingId : null; // 디스크 저장 성공 후 참여로 인정
        }

        public FlightProgressReceipt Complete(string id, bool cleared) // 정상 클리어·실패·포기·다시하기 정산
        {
            return CompleteWithTask(id, cleared, false); // 일반 참여만 확정
        }

        internal FlightProgressReceipt CompleteWithTask(string id, bool cleared, bool taskEligible) // 실제 전투 결과까지 인정한 첫 과제
        {
            return Settle(id, cleared, false, taskEligible); // 보상·과제와 출격을 원자적으로 기록
        }

        public FlightProgressReceipt Recover() // 다음 실행에서만 중단 출격의 참여 보상 지급
        {
            return Settle(state.pendingId, false, true, false); // 중단 출격은 기본 참여분만 지급
        }

        private FlightProgressReceipt Settle(string id, bool cleared, bool recovered, bool taskEligible) // 보상과 기록을 같은 저장에 추가
        {
            if (string.IsNullOrEmpty(id)) // 아직 전투 참여 전 또는 이미 복구됨
            {
                return null; // 정산할 출격 없음
            }
            if (state.recent?.RunId == id) // 같은 ID의 반복 진입
            {
                return state.recent; // 이전의 확정 보상만 반환
            }
            if (state.pendingId != id) // 오래되었거나 다른 ID
            {
                return null; // 알려지지 않은 참여를 재정산하지 않음
            }
            Save next = Copy(); // 저장 실패에도 이전 자료 보존
            PilotProgress progress = next.pilots[(int)next.pendingPilot]; // 참여한 캐릭터의 실제 누적값
            int oldSteps = progress.mastery / MarkInterval; // 직전 지급까지의 숙련도 경계
            var receipt = new FlightProgressReceipt // 실제 출격당 보상
            {
                RunId = id, // 이번에 정산할 유일한 ID
                Pilot = next.pendingPilot, // 실제 전투를 시작한 조종사
                Cleared = cleared, // 최종 보스 격파 구분
                Recovered = recovered, // 재실행 후 실패 정산 표시
                Mastery = cleared ? 20 : 10, // 참여 10과 성공 시 추가 10
                Crystals = cleared ? 6 : 3, // 성공 6 또는 기본 참여 3
                Gold = cleared ? 600 : 150, // 기획서의 기본 난이도 클리어·참여 골드
                FirstTask = taskEligible && !progress.completedFirstTask // 같은 조종사의 첫 정상 종료에서 한 번
            };
            progress.mastery += receipt.Mastery; // 이번 출격의 캐릭터별 참여 숙련도
            receipt.Marks = progress.mastery / MarkInterval - oldSteps + (receipt.FirstTask ? 1 : 0); // 숙련 경계와 최초 과제 보상 별도 합산
            progress.completedFirstTask |= receipt.FirstTask; // 이후 출격에서도 같은 과제 중복 차단
            progress.marks += receipt.Marks; // 캐릭터 전용 증표 누적
            next.crystals += receipt.Crystals; // 모든 캐릭터에서 공용 재료 증가
            next.gold += receipt.Gold; // 출격 ID당 한 번만 골드 지급
            next.recent = receipt; // 중복 정산 확인용 최근 영수증
            next.pendingId = null; // 기록과 보상을 한 번에 확정
            return Write(next) ? receipt : null; // 성공한 파일 교체에만 영수증 제공
        }

        private static Save Empty() // 유효한 첫 실행 형식
        {
            var fresh = new Save(); // 초기 프로필의 기본 상태
            for (int i = 0; i < fresh.pilots.Length; i++) // 모든 등록 캐릭터 준비
            {
                fresh.pilots[i] = new PilotProgress(); // 독립 숙련도와 재료
            }
            return fresh; // 열 수 있는 초기 기록
        }

        private Save Copy() => JsonUtility.FromJson<Save>(JsonUtility.ToJson(state)); // 기존 보유값을 보존하는 쓰기용 복사

        private static bool ValidPilot(FlightBroomPilot pilot) => Enum.IsDefined(typeof(FlightBroomPilot), pilot); // 저장된 열거형의 유효 범위

        private static Save Read(string file) // 기존 저장을 검증해 실제 사용 가능한지 확인
        {
            if (!File.Exists(file)) // 첫 실행 또는 복구 사본 미생성
            {
                return null; // 사용할 기존 파일 없음
            }
            try // 깨진 JSON과 부분 파일 검증
            {
                Save saved = JsonUtility.FromJson<Save>(File.ReadAllText(file, Encoding.UTF8)); // UTF-8 정산 내용 읽기
                if (saved == null || saved.version != 1 || saved.crystals < 0 || saved.gold < 0 || saved.unlocks == null // 알려진 형식과 재화 범위
                    || saved.pilots?.Length != Enum.GetValues(typeof(FlightBroomPilot)).Length // 캐릭터 배열의 예상 범위
                    || (!string.IsNullOrEmpty(saved.pendingId) && (!Guid.TryParseExact(saved.pendingId, "N", out _) || !ValidPilot(saved.pendingPilot)))) // 복구할 ID·조종사 유효성
                {
                    return null; // 내용이 손상된 저장 보호
                }
                foreach (PilotProgress profile in saved.pilots) // 실제 캐릭터별 누적값 범위
                {
                    if (profile == null || profile.mastery < 0 || profile.marks < 0) // 저장 가능 값만 허용
                    {
                        return null; // 복구 사본 확인을 요청
                    }
                }
                saved.brooms ??= Array.Empty<BroomProgress>(); // 26~29일차의 골드 없는 기존 프로필을 보존
                if (Array.Exists(saved.brooms, part => part == null || string.IsNullOrEmpty(part.id) || part.upgrade < 0 || part.upgrade > 5)) // 개별 저장의 허용 강화 범위
                {
                    return null; // 잘못된 부품 정보를 덮어쓰지 않고 이전 저장 검색
                }
                return saved; // 읽을 수 있는 기존 확정본
            }
            catch (Exception error) when (error is ArgumentException || error is IOException || error is UnauthorizedAccessException) // 읽거나 해석하지 못한 원본
            {
                return null; // 이전 정상 사본 순서로 다시 확인
            }
        }

        private bool Write(Save next) // 임시 파일을 완전히 닫은 뒤 저장 교체
        {
            if (path != null && ownership == null) // 다른 실행이 독점 중이거나 저장이 손상됨
            {
                return false; // 기존 사용자의 파일 그대로 보존
            }
            try // 한 출격의 보상과 ID를 함께 저장하는 구간
            {
                if (path != null) // 실제 사용자 또는 Windows 격리 시험 파일
                {
                    string temporary = path + ".tmp"; // 같은 볼륨의 원자적 교체 위치
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(next, true)); // 원본 변경 전 완전한 새 내용
                    using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None)) // 기존 임시 기록 재사용
                    {
                        output.Write(bytes, 0, bytes.Length); // 완전한 보상과 영수증
                        output.Flush(true); // 파일 교체 전 운영체제 버퍼 기록
                    }
                    if (File.Exists(path)) // 이미 정산 또는 참여 기록이 존재
                    {
                        File.Replace(temporary, path, primaryUnreadable ? null : path + ".bak"); // 원본을 교체하고 유효 사본 보호
                    }
                    else // 첫 출격의 최초 단일 저장
                    {
                        File.Move(temporary, path); // 같은 볼륨의 첫 확정 파일
                    }
                    primaryUnreadable = false; // 현재 원본 변경 완료
                }
                state = next; // 실제 확정 후에만 화면과 보상 갱신
                Error = null; // 성공한 새 기록
                return true; // 한 번 정산한 값 제공
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) // 디스크 변경 실패
            {
                Error = "영구 저장을 완료하지 못했습니다."; // 결과에서 아직 지급되지 않은 상태 표시
                return false; // 기존 출격과 보상 상태 유지
            }
        }

        public void Dispose() // 시험 종료 및 게임 도메인의 소유권 반환
        {
            ownership?.Dispose(); // 운영체제의 배타 파일 핸들 해제
            ownership = null; // 기존 인스턴스의 쓰기 방지
        }
    }

    internal static class FlightProgress // 타이틀과 전투가 공유하는 영구 정산
    {
        private static FlightProgressStore current; // 한 실행에서의 실제 파일 소유자
        internal static FlightProgressStore Current => current ??= new FlightProgressStore(SavePath()); // 씬과 무관한 단일 사용자 프로필

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 유지 중에도 새 플레이 상태 초기화
        private static void Reset() // 이전 플레이의 열린 저장 핸들 정리
        {
            current?.Dispose(); // 자동 시험이나 에디터의 이전 파일 닫기
            current = null; // 새 실행에서 실제 파일 재확인
        }

        private static string SavePath() // 일반 실행과 자동 검증을 분리한 위치
        {
            string test = Environment.GetEnvironmentVariable("PROJECTG_FLIGHT_PROGRESS_TEST"); // Windows 전용 재실행 검증 파일
            if (!string.IsNullOrEmpty(test)) // 명시적으로 격리 경로를 지정한 빌드
            {
                return test; // 이전 프로세스의 같은 격리 파일 읽기
            }
            return FlightBindings.UsesPreferences ? Path.Combine(Application.persistentDataPath, "Flight", "progress-v1.json") : null; // 실제 사용자 파일 또는 메모리 시험
        }
    }
}
