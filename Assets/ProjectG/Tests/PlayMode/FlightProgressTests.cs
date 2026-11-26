using System; // 실제 정산 형식과 시험 경로
using System.IO; // 독립 영구 저장 파일 검증
using NUnit.Framework; // 시작·정산·복구 결과의 단언
using UnityEngine; // 격리 시험용 임시 경로

namespace ProjectG.Tests // 프로젝트 자동 검증
{
    public sealed class FlightProgressTests // 영구 보상과 출격 ID 검증
    {
        [Test] // 실제 파일과 오프라인 Steam 어댑터 경계
        public void PlatformAchievementsRetryWithoutPayingOrSendingToAnotherAccount() // 콜백 승인과 저장 재시도만 검사
        {
            Type progress = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp", true); // 실제 디스크 프로필
            Type syncType = Type.GetType("ProjectG.FlightAchievementSync, Assembly-CSharp", true); // 플랫폼 구현과 독립된 해금 규칙
            const System.Reflection.BindingFlags methods = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 테스트 어셈블리에서 같은 게임 구현
            string directory = Path.Combine(Application.temporaryCachePath, "Platform-" + Guid.NewGuid().ToString("N")); // 사용자 진행과 분리한 자동 시험
            string path = Path.Combine(directory, "progress.json"); // 두 실행이 공유할 실제 정산 파일
            try
            {
                Type pilotType = progress.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사의 영구 저장 타입
                object pilot = Enum.Parse(pilotType, "Aria"); // 조건을 먼저 만족한 로컬 프로필
                using (var disk = (IDisposable)Activator.CreateInstance(progress, path)) // 연결 없이 클리어와 과제
                {
                    string first = (string)Call(progress, disk, "Begin", pilot); // 본편 완료 전에 참여 확정
                    Assert.That(Call(progress, disk, "CompleteWithTask", first, true, true), Is.Not.Null); // 첫 클리어와 첫 과제를 같은 파일에 저장
                    string later = (string)Call(progress, disk, "Begin", pilot); // 후속 실패로 저장의 최근 결과 교체
                    Assert.That(Call(progress, disk, "Complete", later, false), Is.Not.Null); // 이미 달성한 조건의 영속 확인
                }

                using (var disk = (IDisposable)Activator.CreateInstance(progress, path)) // 새로운 게임 실행과 같은 재열기
                {
                    string active = null; // 접속하지 않은 실제 상태
                    bool available = false; // 서버 조회의 결과 수신 여부
                    var cache = new System.Collections.Generic.HashSet<string>(); // Steam 클라이언트 준비물의 모의 캐시
                    var requests = new System.Collections.Generic.List<string>(); // 누락과 중복을 볼 요청 순서
                    var callbacks = new System.Collections.Generic.Queue<Action<bool>>(); // 실패와 성공을 주는 모의 콜백
                    Func<string> account = () => active; // 같은 프로필 소유자 확인
                    Func<string, bool?> read = id => available ? cache.Contains(id) : null; // 원격 조회 전후 구분
                    Func<string, Action<bool>, bool> submit = (id, done) => // 비동기 플랫폼 승인 대기
                    {
                        requests.Add(id); // 실제 어댑터 호출 기록
                        cache.Add(id); // 콜백 성공 전에도 클라이언트에 표시 가능한 해금
                        callbacks.Enqueue(done); // 제어된 원격 승인
                        return true; // 제출을 받은 상태
                    };
                    object sync = Activator.CreateInstance(syncType, methods, null, new object[] { disk, "owner", "TEST_CLEAR", "TEST_TASK", account, read, submit }, null); // 후보 API 이름은 자동 시험에만 사용
                    void Pump() => syncType.GetMethod("Pump", methods).Invoke(sync, null); // 기존 출격 체크포인트로부터 호출
                    Pump(); // 연결 없음
                    active = "other"; // 이 프로필과 다른 계정
                    Pump(); // 잘못된 계정에는 보내지 않음
                    active = "owner"; // 같은 사용자 로그인
                    Pump(); // 아직 원격 상태 수신 전
                    Assert.That(requests, Is.Empty); // 오프라인·다른 계정·조회 실패 시 요청 없음
                    available = true; // 실제 준비가 끝남
                    Pump(); // 첫 도전 과제 제출
                    Pump(); // 같은 요청이 아직 진행 중
                    Assert.That(requests, Is.EqualTo(new[] { "TEST_CLEAR" })); // 승인 전 중복 방지
                    callbacks.Dequeue()(false); // 캐시는 해금 상태지만 원격 저장 실패
                    Pump(); // 같은 해금을 다시 전송
                    Assert.That(requests, Is.EqualTo(new[] { "TEST_CLEAR", "TEST_CLEAR" })); // 실서버 승인 전 캐시가 누락시키지 않음
                    Action<bool> stale = callbacks.Dequeue(); // 늦게 돌아올 수 있는 콜백
                    active = "other"; // 다른 계정으로 전환
                    Pump(); // 이전 응답 무효화
                    stale(true); // 이미 다른 계정을 본 뒤 이전 응답
                    active = "owner"; // 다시 명시한 원래 owner
                    Pump(); // 실제 서버를 받아 같은 조건을 다시 시도
                    callbacks.Dequeue()(true); // 이번 연결에서 받은 실제 승인
                    Pump(); // 두 번째 영구 과제를 제출
                    callbacks.Dequeue()(true); // 첫 과제의 원격 저장
                    Pump(); // 이미 승인받은 두 과제를 건너뜀
                    Assert.That(requests, Is.EqualTo(new[] { "TEST_CLEAR", "TEST_CLEAR", "TEST_CLEAR", "TEST_TASK" })); // 실패와 연결 해제 때만 재요청
                    Assert.That(progress.GetProperty("Gold").GetValue(disk), Is.EqualTo(750)); // 도전 동기화가 게임 재화에 더 지급하지 않음
                    Assert.That(Crystals(progress, disk), Is.EqualTo(9)); // 실제 두 출격의 재화 보존
                }
            }
            finally
            {
                if (Directory.Exists(directory)) // 현재 검증의 한정된 임시 폴더
                {
                    foreach (string file in Directory.GetFiles(directory)) // 자동 검사에서 만든 저장 파일
                    {
                        File.Delete(file); // 독립 시험 파일만 정리
                    }

                    Directory.Delete(directory); // 비운 폴더 제거
                }
            }
        }

        [Test] // 재실행과 같은 영수증 재요청
        public void SettlementPersistsExactlyOnceAcrossStoreInstances() // 성공·실패·복구와 조종사 분리
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp"); // 정식 저장 구현 확인
            Assert.That(type, Is.Not.Null, "출격 정산 저장소"); // 별도 시제품이 아닌 실제 저장소
            string directory = Path.Combine(Application.temporaryCachePath, "Progress-" + Guid.NewGuid().ToString("N")); // 사용자 데이터와 격리
            string path = Path.Combine(directory, "progress.json"); // 재실행이 읽을 동일한 디스크 파일
            try // 테스트 완료 후 임시 파일 정리
            {
                Type pilotType = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 열거형
                object aria = Enum.Parse(pilotType, "Aria"); // 첫 조종사의 누적 보상
                object rian = Enum.Parse(pilotType, "Rian"); // 다른 조종사의 개별 기록
                string interrupted; // 다음 실행에서 복구할 실제 ID
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 디스크에 보상을 지급하는 첫 실행
                {
                    string run = (string)Call(type, store, "Begin", aria); // 첫 웨이브 참여 기록
                    Assert.That(run, Is.Not.Empty); // 파일과 연결된 출격 ID
                    Assert.That(Call(type, store, "Begin", rian), Is.Null); // 두 출격의 참여 충돌 방지
                    Assert.That(Call(type, store, "Complete", run, true), Is.Not.Null); // 클리어 최초 지급 확인
                    Assert.That(Call(type, store, "Complete", run, false), Is.Not.Null); // 동일 ID에 기존 영수증 반환
                    Assert.That(Crystals(type, store), Is.EqualTo(6)); // 중복으로 공통 재료가 오르지 않음
                    Assert.That(type.GetProperty("Gold").GetValue(store), Is.EqualTo(600)); // 클리어와 같은 ID에 골드는 한 번만 지급
                    Assert.That(Call(type, store, "GetMastery", aria), Is.EqualTo(20)); // 참여 및 클리어 숙련도
                    Assert.That(Call(type, store, "GetMastery", rian), Is.Zero); // 다른 조종사의 숙련도 유지
                    interrupted = (string)Call(type, store, "Begin", rian); // 보스 격파 전 앱 종료 가정
                    Assert.That(File.ReadAllText(path), Does.Contain(interrupted)); // 재실행 전에 참여 자체가 영구 저장
                }
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 모의 재실행에서 기존 파일 불러오기
                {
                    Assert.That(Call(type, store, "Recover"), Is.Not.Null); // 중단된 리안 출격의 기본 보상
                    Assert.That(Call(type, store, "Recover"), Is.Null); // 같은 중단 출격의 중복 보상 방지
                    Assert.That(Call(type, store, "GetMastery", rian), Is.EqualTo(10)); // 리안의 기본 참여 숙련도
                    Assert.That(Crystals(type, store), Is.EqualTo(9)); // 실패 이후 공통 재료 합계
                    Assert.That(type.GetProperty("Gold").GetValue(store), Is.EqualTo(750)); // 복구 시 실패 기본분 150만 한 번 지급
                    for (int i = 0; i < 2; i++) // 누적 숙련도 60의 경계 통과
                    {
                        string run = (string)Call(type, store, "Begin", aria); // 아리아의 새 출격 ID
                        Assert.That(Call(type, store, "Complete", run, true), Is.Not.Null); // 두 번의 신규 클리어
                    }
                    Assert.That(Call(type, store, "GetMastery", aria), Is.EqualTo(60)); // 누적 숙련도 경계
                    Assert.That(Call(type, store, "GetMarks", aria), Is.EqualTo(1)); // 첫 조율의 증표
                    Assert.That(Call(type, store, "GetMarks", rian), Is.Zero); // 조율의 증표 캐릭터별 분리
                }
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 세 번째 실행에서 중복 및 누적 확인
                {
                    Assert.That(Call(type, store, "Recover"), Is.Null); // 완료한 영수증의 재처리 없음
                    Assert.That(Call(type, store, "Complete", interrupted, false), Is.Null); // 오래된 ID의 이중 지급 없음
                    Assert.That(Crystals(type, store), Is.EqualTo(21)); // 실제 저장한 세 차례 성공과 한 차례 실패
                    Assert.That(Call(type, store, "GetMarks", aria), Is.EqualTo(1)); // 증표의 영구 저장
                }
            }
            finally // 사용자 저장과 무관한 시험 폴더 정리
            {
                if (Directory.Exists(directory)) // 파일을 쓴 시험만 정리
                {
                    foreach (string file in Directory.GetFiles(directory)) // 생성한 전용 정산 파일
                    {
                        File.Delete(file); // 시험 데이터 삭제
                    }
                    Directory.Delete(directory); // 빈 시험 폴더 삭제
                }
            }
        }

        [Test] // 마을 재화와 장비 교체의 영구 진행
        public void VillagePurchaseAndUpgradePersistWithExactCosts() // 부족·중복·강화 수치와 공용 스킬
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp"); // 실제 파일 저장 형식
            var purchase = type.GetMethod("BuyBroomPart", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 게임 내부 구매 규칙
            Assert.That(purchase, Is.Not.Null); // 시험 UI의 숫자가 아닌 실제 구매 API
            string dir = Path.Combine(Application.temporaryCachePath, "Village-" + Guid.NewGuid().ToString("N")); // 사용자 파일과 격리
            string path = Path.Combine(dir, "progress.json"); // 양쪽 실행이 공유할 실제 프로필
            try // 새 저장과 두 번의 재열기
            {
                Type pilotType = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 게임 조종사 열거형
                object aria = Enum.Parse(pilotType, "Aria"); // 장착을 바꿀 조종사
                object rian = Enum.Parse(pilotType, "Rian"); // 독립적인 공용기 상태
                const string item = "shop.harbor-grip:1"; // 상점의 첫 일반 부품 ID
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 마을에서 처음 연 프로필
                {
                    Assert.That(Call(type, store, "BuyBroomPart", item, 1), Is.False); // 골드가 없으면 지출 및 구매 금지
                    for (int i = 0; i < 2; i++) // 기본 난이도 두 번의 클리어 정산
                    {
                        string id = (string)Call(type, store, "Begin", aria); // 실제 고유 출격으로 올린 재화
                        Call(type, store, "Complete", id, true); // 천공 결정 6·골드 600
                    }
                    Assert.That(type.GetProperty("Gold").GetValue(store), Is.EqualTo(1200)); // 클리어의 골드 규칙
                    Assert.That(Call(type, store, "BuyBroomPart", item, 1), Is.True); // 일반 등급을 900골드로 구매
                    Assert.That(Call(type, store, "BuyBroomPart", item, 1), Is.False); // 같은 보유 개체 중복 구매 차단
                    Assert.That(Call(type, store, "UpgradeBroomPart", item, 0), Is.True); // 목표 +1의 실제 강화
                    Assert.That(Call(type, store, "UpgradeBroomPart", item, 0), Is.False); // 오래된 단계 재전송 차단
                    Assert.That(Call(type, store, "UpgradeBroomPart", item, 1), Is.False); // +2에 필요한 골드가 부족함
                    Assert.That(Call(type, store, "SetCommonFan", aria, true), Is.True); // 보호막에서 산탄으로 영구 장착
                    Assert.That(type.GetProperty("Gold").GetValue(store), Is.Zero); // 두 동작의 정확한 비용 합
                    Assert.That(Crystals(type, store), Is.EqualTo(9)); // 강화에 실제 결정 3개만 소모
                }
                using (var restored = (IDisposable)Activator.CreateInstance(type, path)) // 파일로부터 영구 상태 복원
                {
                    Assert.That(Call(type, restored, "OwnsBroomPart", item), Is.True); // 구입 후 계속 소유
                    Assert.That(Call(type, restored, "BroomUpgrade", item, 0), Is.EqualTo(1)); // 강화 레벨 보존
                    Assert.That(Call(type, restored, "GetCommonFan", aria), Is.True); // 구매와 독립된 스킬 장착 저장
                    Assert.That(Call(type, restored, "GetCommonFan", rian), Is.False); // 다른 조종사의 보호막 유지
                    Assert.That(type.GetProperty("Gold").GetValue(restored), Is.Zero); // 이전 금액 보존
                }
            }
            finally // 현재 시험에서 만든 파일만 제거
            {
                if (Directory.Exists(dir)) // 이전 실제 저장 위치에는 영향 없음
                {
                    foreach (string file in Directory.GetFiles(dir)) // 정산과 파일 잠금의 시험 자료
                    {
                        File.Delete(file); // 임시 파일 삭제
                    }
                    Directory.Delete(dir); // 현재 시험 폴더 마무리
                }
            }
        }

        [Test] // 최초 과제와 전용 성장 해금의 원자 저장
        public void FirstTaskAndGrowthUnlockPayOnceAcrossReload() // 과제 중복과 조종사별 비용 격리
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp"); // 실제 정산 구현
            var settlement = type.GetMethod("CompleteWithTask", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 종료 시 과제 보상 경로
            Assert.That(settlement, Is.Not.Null); // 과제와 출격 보상의 원자적 합산
            string directory = Path.Combine(Application.temporaryCachePath, "Growth-" + Guid.NewGuid().ToString("N")); // 게임 파일과 분리된 시험 작성
            string path = Path.Combine(directory, "progress.json"); // 다시 연 동일 프로필
            const string node = "growth.aria.chase-2"; // 최초 두 번째 분기 연구
            try // 세 번의 클리어와 신규 구매
            {
                Type pilot = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 ID의 유효 범위
                object aria = Enum.Parse(pilot, "Aria"); // 첫 과제 완료 캐릭터
                object rian = Enum.Parse(pilot, "Rian"); // 다른 조종사의 진행 보존
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 실제 성장 저장 시작
                {
                    string first = (string)Call(type, store, "Begin", aria); // 아리아의 한 출격
                    Call(type, store, "CompleteWithTask", first, true, true); // 기본·집중 공격 과제 충족
                    Call(type, store, "CompleteWithTask", first, true, true); // 같은 출격 결과의 중복 호출
                    Assert.That(Call(type, store, "GetMarks", aria), Is.EqualTo(1)); // 첫 조율의 증표 한 개만 지급
                    string repeat = (string)Call(type, store, "Begin", aria); // 조건을 충족한 별도 재출격
                    Call(type, store, "CompleteWithTask", repeat, true, true); // 이미 완료한 최초 과제
                    Assert.That(Call(type, store, "GetMarks", aria), Is.EqualTo(1)); // 다음 출격에서도 두 번째 과제 보상 금지
                    Assert.That(Call(type, store, "BuyGrowthNode", node, aria), Is.False); // 결정 12에서는 잠금 유지
                    string other = (string)Call(type, store, "Begin", rian); // 리안으로 공통 결정 확보
                    Call(type, store, "Complete", other, true); // 리안 과제를 충족하지 않음
                    Assert.That(Call(type, store, "BuyGrowthNode", node, rian), Is.False); // 다른 조종사의 연구를 대신 구입 금지
                    Assert.That(Call(type, store, "BuyGrowthNode", node, aria), Is.True); // 해당 조종사가 실제 결정 18·증표 1로 해금
                    Assert.That(Call(type, store, "BuyGrowthNode", node, aria), Is.False); // 같은 연구의 중복 지출 차단
                    Assert.That(Crystals(type, store), Is.Zero); // 구매 비용의 결정 소모
                    Assert.That(Call(type, store, "GetMarks", aria), Is.Zero); // 아리아의 증표 하나만 소모
                }
                using (var reopened = (IDisposable)Activator.CreateInstance(type, path)) // 새 실행에서 저장 상태 확인
                {
                    Assert.That(Call(type, reopened, "HasUnlock", node), Is.True); // 앞으로의 실제 레벨업 선택 자격
                    Assert.That(Call(type, reopened, "HasCompletedFirstTask", aria), Is.True); // 증표를 써도 최초 과제 기록 보존
                    Assert.That(Call(type, reopened, "HasCompletedFirstTask", rian), Is.False); // 다른 캐릭터의 과제 유지
                }
            }
            finally // 시험 폴더만 정리
            {
                if (Directory.Exists(directory)) // 해당 검증이 생성한 경로
                {
                    foreach (string file in Directory.GetFiles(directory)) // 시험 저장·백업·잠금
                    {
                        File.Delete(file); // 실제 사용자 저장과 무관
                    }
                    Directory.Delete(directory); // 비워 둔 시험 디렉터리 제거
                }
            }
        }

        [Test] // 연구 과제와 숙련도의 조종사별 분리
        public void ResearchQualificationsRequireOnePilotAndSurviveReload() // 출격별 과제와 공용 자격 저장
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp", true); // 실제 영구 성장 저장소
            Type pilot = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 ID
            object aria = Enum.Parse(pilot, "Aria"); // 숙련도를 먼저 쌓을 조종사
            object rian = Enum.Parse(pilot, "Rian"); // 다른 조종사의 독립 연구 과제
            string directory = Path.Combine(Application.temporaryCachePath, "Research-" + Guid.NewGuid().ToString("N")); // 사용자 저장과 분리한 임시 폴더
            string path = Path.Combine(directory, "progress.json"); // 재실행에서 확인할 실제 파일
            try // 실제 정산과 다시 불러오기
            {
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 실제 파일 기반 영구 저장
                {
                    void Sortie(object actor, int enemies, bool middle, bool cleared) // 지정 조종사의 한 번의 정상 정산
                    {
                        string id = (string)Call(type, store, "Begin", actor); // 해당 조종사의 새 출격
                        Assert.That(Call(type, store, "CompleteWithResearch", id, cleared, false, enemies, middle), Is.Not.Null); // 보상과 연구 과제의 원자 정산
                    }
                    Sortie(aria, 5, false, false); // 아리아의 첫 출격 다섯 처치와 숙련도 십
                    Sortie(aria, 5, false, false); // 두 출격의 처치 수를 합산하지 않음
                    Sortie(rian, 10, false, false); // 리안의 일반 과제와 숙련도 십
                    Assert.That(Call(type, store, "ResearchTier"), Is.EqualTo(0)); // 서로 다른 조종사의 조건 합산 금지
                    Sortie(rian, 0, false, false); // 같은 리안의 숙련도 이십
                    Assert.That(Call(type, store, "ResearchTier"), Is.EqualTo(1)); // 어느 한 조종사가 충족하면 공용 일반 자격
                    Sortie(rian, 0, true, false); // 리안의 중간 보스 과제와 부족한 숙련도
                    for (int i = 0; i < 4; i++) // 아리아의 숙련도 육십 도달
                    {
                        Sortie(aria, 0, false, false); // 과제 없는 다른 조종사의 숙련도 증가
                    }
                    Assert.That(Call(type, store, "ResearchTier"), Is.EqualTo(1)); // 심화 조건도 캐릭터 간 합산 금지
                    Sortie(aria, 0, true, false); // 아리아가 중간 보스 과제를 충족
                    Assert.That(Call(type, store, "ResearchTier"), Is.EqualTo(2)); // 동일 조종사의 심화 공용 자격
                    for (int i = 0; i < 3; i++) // 아리아의 숙련도 백이십 도달
                    {
                        Sortie(aria, 0, false, true); // 정상 최종 클리어와 이십 숙련도
                    }
                    Assert.That(Call(type, store, "ResearchTier"), Is.EqualTo(3)); // 최종 클리어와 숙련도를 충족한 특수 자격
                    string pending = (string)Call(type, store, "Begin", rian); // 강제 종료를 가정한 출격
                    Assert.That(Call(type, store, "Recover"), Is.Not.Null); // 참여 보상만 복구
                    Assert.That(Call(type, store, "CompleteWithResearch", pending, true, false, 10, true), Is.Not.Null); // 같은 출격의 중복 결과 조회
                    Assert.That(Call(type, store, "HasResearchTask", rian, 3), Is.False); // 복구 뒤 위조한 정상 과제 반영 금지
                }
                using (var reopened = (IDisposable)Activator.CreateInstance(type, path)) // 새 실행의 실제 저장 복원
                {
                    Assert.That(Call(type, reopened, "ResearchTier"), Is.EqualTo(3)); // 공용 자격의 영구 보존
                    Assert.That(Call(type, reopened, "HasResearchTask", aria, 2), Is.True); // 조종사별 심화 과제 보존
                    Assert.That(Call(type, reopened, "HasResearchTask", rian, 3), Is.False); // 다른 조종사의 특수 과제 분리
                }
            }
            finally // 현재 시험에서 만든 파일만 제거
            {
                if (Directory.Exists(directory)) // 독립 시험 폴더 존재 여부
                {
                    foreach (string file in Directory.GetFiles(directory)) // 저장과 백업·잠금 파일
                    {
                        File.Delete(file); // 사용자 저장과 무관한 임시 파일 제거
                    }
                    Directory.Delete(directory); // 비운 시험 폴더 제거
                }
            }
        }

        [Test] // 구성 분류별 연구 해금과 단일 재화 정산
        public void ResearchTreeRequiresPrerequisitesAndPersistsSharedUnlocks() // 선행 트리와 무료 연동 해금
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp", true); // 실제 영구 성장 저장소
            Type catalog = Type.GetType("ProjectG.FlightCraftedResearchCatalog, Assembly-CSharp", true); // 기획서의 분류별 연구 표
            Type group = Type.GetType("ProjectG.CraftedSpellGroup, Assembly-CSharp", true); // 기존 제작 분류 ID
            Type pilot = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 ID
            object aria = Enum.Parse(pilot, "Aria"); // 동일 조종사의 실제 연구 자격
            object shape = Enum.Parse(group, "Shape"); // 탄환 형태 분류
            object formation = Enum.Parse(group, "Formation"); // 발사 배열 분류
            object trajectory = Enum.Parse(group, "Trajectory"); // 이동 궤적 분류
            object aim = Enum.Parse(group, "Aim"); // 조준 방식 분류
            string directory = Path.Combine(Application.temporaryCachePath, "ResearchTree-" + Guid.NewGuid().ToString("N")); // 사용자 저장과 분리한 폴더
            string path = Path.Combine(directory, "progress.json"); // 재실행에서 확인할 실제 파일
            try // 실제 정산·연구 구매·저장 재열기
            {
                var nodes = (System.Collections.IList)catalog.GetProperty("All", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null); // 아홉 분류의 전체 연구 항목
                Assert.That(nodes.Count, Is.EqualTo(101)); // 전체 선택지와 무료 연동 항목의 누락 방지
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 실제 프로필과 공용 재화
                {
                    void Sortie(int enemies, bool middle) // 같은 조종사의 정상 클리어
                    {
                        string id = (string)Call(type, store, "Begin", aria); // 새 실제 출격
                        Call(type, store, "CompleteWithResearch", id, true, false, enemies, middle); // 과제와 참여 보상 확정
                    }
                    Assert.That(Call(type, store, "IsResearchUnlocked", shape, "orb"), Is.True); // 초기 구체탄은 무료 보유
                    Assert.That(Call(type, store, "IsResearchUnlocked", shape, "small"), Is.False); // 일반 연구는 처음에 잠김
                    Type presets = Type.GetType("ProjectG.CraftedSpellPresets, Assembly-CSharp", true); // 실제 기획 마법 구성 목록
                    var recipes = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 편집과 전투의 같은 프리셋
                    foreach (int index in new[] { 0, 3, 7, 11 }) // 처음부터 지급하는 네 마법
                    {
                        object recipe = recipes[index].GetType().GetProperty("Recipe").GetValue(recipes[index]); // 해당 마법의 실제 조합
                        Assert.That(Call(type, store, "IsRecipeResearchUnlocked", recipe), Is.True); // 초기 구성만으로 사용 가능
                    }
                    object later = recipes[9].GetType().GetProperty("Recipe").GetValue(recipes[9]); // 연구 요소가 필요한 홀리 샤워
                    Assert.That(Call(type, store, "IsRecipeResearchUnlocked", later), Is.False); // 일부 구성만 가진 마법의 잠금
                    Assert.That(Call(type, store, "BuyResearch", shape, "small"), Is.False); // 일반 자격 없는 연구 차단
                    Sortie(10, false); // 동일 조종사의 일반 자격과 첫 재화
                    Assert.That(Call(type, store, "BuyResearch", formation, "converge"), Is.False); // 같은 단계의 병렬 선행이 없으면 차단
                    int crystals = Crystals(type, store); // 실패한 구매 전후 비교할 결정
                    int gold = (int)type.GetProperty("Gold").GetValue(store); // 실패한 구매 전후 비교할 골드
                    Assert.That(Call(type, store, "BuyResearch", shape, "small"), Is.True); // 구체탄 선행의 소형탄 연구
                    Assert.That(Crystals(type, store), Is.EqualTo(crystals - 3)); // 일반 연구 결정 세 개 소비
                    Assert.That(type.GetProperty("Gold").GetValue(store), Is.EqualTo(gold - 300)); // 일반 연구 골드 삼백 소비
                    Assert.That(Call(type, store, "BuyResearch", shape, "small"), Is.False); // 이미 해금한 연구 재구매 금지
                    Assert.That(Call(type, store, "BuyResearch", shape, "disc"), Is.False); // 심화 자격이 없는 연구 차단
                    Assert.That(Call(type, store, "BuyResearch", trajectory, "curve"), Is.True); // 유도형의 일반 선행 연구
                    Sortie(0, true); // 같은 조종사의 중간 보스 과제
                    Sortie(0, false); // 동일 조종사의 숙련도 육십
                    Assert.That(Call(type, store, "BuyResearch", trajectory, "homing"), Is.True); // 심화 자격과 곡선 선행 충족
                    Assert.That(Call(type, store, "IsResearchUnlocked", aim, "homing"), Is.True); // 유도 조준의 무료 자동 연동
                    Assert.That(Call(type, store, "BuyResearch", aim, "homing"), Is.False); // 무료 연동의 중복 연구 차단
                }
                using (var reopened = (IDisposable)Activator.CreateInstance(type, path)) // 재실행한 동일 프로필
                {
                    Assert.That(Call(type, reopened, "IsResearchUnlocked", shape, "small"), Is.True); // 구성 분류별 영구 해금 보존
                    Assert.That(Call(type, reopened, "IsResearchUnlocked", aim, "homing"), Is.True); // 원래 선행으로부터 무료 연동 복원
                    Assert.That(Call(type, reopened, "IsResearchUnlocked", formation, "converge"), Is.False); // 실패한 연구의 잠금 보존
                }
            }
            finally // 현재 시험에서 만든 파일만 제거
            {
                if (Directory.Exists(directory)) // 독립 시험 폴더 존재 여부
                {
                    foreach (string file in Directory.GetFiles(directory)) // 저장과 백업·잠금 파일
                    {
                        File.Delete(file); // 사용자 저장과 무관한 임시 파일 제거
                    }
                    Directory.Delete(directory); // 비운 시험 폴더 제거
                }
            }
        }

        [Test] // 연구 트리와 기존 제작 선택지의 전체 대조
        public void EveryCraftedChoiceHasAnAcyclicResearchNode() // 누락·중복·잘못된 선행과 순환 금지
        {
            Type catalog = Type.GetType("ProjectG.FlightCraftedResearchCatalog, Assembly-CSharp", true); // 실제 연구 노드 표
            Type choices = Type.GetType("ProjectG.CraftedSpellCatalog, Assembly-CSharp", true); // 기존 제작 선택지 표
            Type group = Type.GetType("ProjectG.CraftedSpellGroup, Assembly-CSharp", true); // 제작 분류 열거형
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic; // 실제 내부 표 조회
            var nodes = (System.Collections.IList)catalog.GetProperty("All", hidden).GetValue(null); // 등록된 전체 연구 노드
            var find = catalog.GetMethod("Find", hidden); // 분류와 요소의 연구 조회
            var findKey = catalog.GetMethod("FindKey", hidden); // 선행 ID의 실제 노드 조회
            var keys = new System.Collections.Generic.HashSet<string>(); // 중복을 허용하지 않는 전체 분류 ID
            int initial = 0; // 효과 없음을 포함한 초기 지급 선택지
            foreach (object node in nodes) // 등록된 모든 연구 항목
            {
                Type nodeType = node.GetType(); // 실제 연구 데이터 형식
                string key = (string)nodeType.GetProperty("Key").GetValue(node); // 분류를 포함한 안정 ID
                Assert.That(keys.Add(key), Is.True); // 같은 분류와 요소의 중복 금지
                if ((int)nodeType.GetField("Tier").GetValue(node) == 0) // 초기 지급 항목
                {
                    initial++; // 초기 요소와 효과 없음 개수
                }
                string[] parents = (string[])nodeType.GetField("Parents").GetValue(node); // 이 요소의 모든 선행 ID
                foreach (string parent in parents) // 실제 존재해야 하는 선행 항목
                {
                    Assert.That(findKey.Invoke(null, new object[] { parent }), Is.Not.Null, parent); // 오탈자와 없는 분류 차단
                }
            }
            Assert.That(initial, Is.EqualTo(29)); // 초기 실요소 이십팔 개와 효과 없음
            foreach (object category in Enum.GetValues(group)) // 아홉 제작 분류
            {
                var options = (System.Collections.IEnumerable)choices.GetMethod("Choices").Invoke(null, new[] { category }); // 분류별 기존 선택지
                foreach (object option in options) // 조합 가능한 각 구성 요소
                {
                    string id = (string)option.GetType().GetField("Id").GetValue(option); // 기존 저장에서 쓰는 요소 ID
                    Assert.That(find.Invoke(null, new[] { category, (object)id }), Is.Not.Null, category + "." + id); // 실제 제작 선택지와 연구 표 일치
                }
            }
            void Visit(object node, System.Collections.Generic.HashSet<string> path) // 한 경로의 선행 연구 순환 검사
            {
                Type nodeType = node.GetType(); // 현재 선행 노드 형식
                string key = (string)nodeType.GetProperty("Key").GetValue(node); // 실제 노드의 분류 ID
                Assert.That(path.Add(key), Is.True, key); // 현재 경로에 같은 노드가 다시 등장하지 않음
                foreach (string parent in (string[])nodeType.GetField("Parents").GetValue(node)) // 모든 선행 분기
                {
                    Visit(findKey.Invoke(null, new object[] { parent }), path); // 해당 분기의 선행을 끝까지 확인
                }
                path.Remove(key); // 다른 독립 분기에서 같은 선행 허용
            }
            foreach (object node in nodes) // 모든 연구에서 시작한 선행 경로
            {
                Visit(node, new System.Collections.Generic.HashSet<string>()); // 순환 없는 유효 연구 트리
            }
        }

        [Test] // 연구 기록이 없던 이전 영구 저장과의 호환
        public void OlderProgressWithoutResearchFieldsKeepsExistingRewards() // 기존 재화와 숙련도 보존
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp", true); // 실제 영구 성장 저장소
            Type pilot = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 ID
            object aria = Enum.Parse(pilot, "Aria"); // 이전 저장의 조종사
            string directory = Path.Combine(Application.temporaryCachePath, "ResearchLegacy-" + Guid.NewGuid().ToString("N")); // 사용자 저장과 분리한 임시 폴더
            string path = Path.Combine(directory, "progress.json"); // 이전 형식을 흉내 낼 격리 파일
            try // 기존 보상 생성과 이전 형식 복원
            {
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 실제 파일 기반 프로필
                {
                    string id = (string)Call(type, store, "Begin", aria); // 이전 형식의 정상 출격
                    Call(type, store, "Complete", id, true); // 기존 숙련도와 재화 확정
                }
                string saved = File.ReadAllText(path); // 실제 정산이 생성한 저장 원본
                saved = System.Text.RegularExpressions.Regex.Replace(saved, ",\\s*\"research(?:Normal|Advanced|Special)\"\\s*:\\s*(?:true|false)", ""); // 이전 버전에 없던 연구 필드만 제거
                File.WriteAllText(path, saved); // 사용자 파일과 무관한 이전 형식 시험 자료
                using (var reopened = (IDisposable)Activator.CreateInstance(type, path)) // 연구 필드가 없는 이전 저장 재열기
                {
                    Assert.That(type.GetProperty("Error").GetValue(reopened), Is.Null); // 이전 저장을 오류 없이 복원
                    Assert.That(Call(type, reopened, "GetMastery", aria), Is.EqualTo(20)); // 기존 캐릭터 숙련도 보존
                    Assert.That(Crystals(type, reopened), Is.EqualTo(6)); // 기존 공용 결정 보존
                    Assert.That(type.GetProperty("Gold").GetValue(reopened), Is.EqualTo(600)); // 기존 공용 골드 보존
                    Assert.That(Call(type, reopened, "ResearchTier"), Is.Zero); // 없는 과제는 임의로 완료하지 않음
                    string next = (string)Call(type, reopened, "Begin", aria); // 이전 저장으로 새 출격
                    Call(type, reopened, "CompleteWithResearch", next, false, false, 10, false); // 실제 새 과제를 정상 정산
                    Assert.That(Call(type, reopened, "ResearchTier"), Is.EqualTo(1)); // 기존 숙련도와 새 과제로 일반 자격
                }
            }
            finally // 현재 시험에서 만든 파일만 제거
            {
                if (Directory.Exists(directory)) // 독립 시험 폴더 존재 여부
                {
                    foreach (string file in Directory.GetFiles(directory)) // 저장과 백업·잠금 파일
                    {
                        File.Delete(file); // 사용자 저장과 무관한 임시 파일 제거
                    }
                    Directory.Delete(directory); // 비운 시험 폴더 제거
                }
            }
        }

        [Test] // 상점과 무료 보상이 공유하는 실제 해금
        public void ResearchRewardsWaitForEligibilityAndNeverDuplicate() // 최초 보스 보상과 일반 선택권의 영구 보류
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp", true); // 실제 영구 성장 저장소
            Type pilot = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 ID
            Type group = Type.GetType("ProjectG.CraftedSpellGroup, Assembly-CSharp", true); // 기존 제작 분류
            object aria = Enum.Parse(pilot, "Aria"); // 연구 과제를 진행할 조종사
            object shape = Enum.Parse(group, "Shape"); // 탄환 형태 분류
            object formation = Enum.Parse(group, "Formation"); // 발사 배열 분류
            string directory = Path.Combine(Application.temporaryCachePath, "ResearchReward-" + Guid.NewGuid().ToString("N")); // 사용자 저장과 분리한 폴더
            string path = Path.Combine(directory, "progress.json"); // 재실행에서 확인할 실제 파일
            try // 정상 실패·클리어와 선택권 저장
            {
                using (var store = (IDisposable)Activator.CreateInstance(type, path)) // 실제 프로필과 공용 재화
                {
                    string early = (string)Call(type, store, "Begin", aria); // 첫 중간 보스 격파 출격
                    Assert.That(Call(type, store, "CompleteWithResearchRewards", early, false, false, 0, 0, true, "outer"), Is.Not.Null); // 정상 실패에서도 중간 보스 기록
                    var advanced = (string[])Call(type, store, "PendingResearchRewards", 2); // 심화 단계의 미수령 보상
                    Assert.That(advanced.Length, Is.EqualTo(1)); // 자격이 없어도 첫 보상 보류
                    Assert.That(Call(type, store, "ClaimResearchReward", advanced[0], shape, "disc"), Is.False); // 심화 자격이 없으면 무료 수령 금지
                    string run = (string)Call(type, store, "Begin", aria); // 일반 적 처치와 정상 클리어
                    Assert.That(Call(type, store, "CompleteWithResearchRewards", run, true, false, 30, 1, true, "outer"), Is.Not.Null); // 일반·정예·중간·최종 결과의 원자 정산
                    Assert.That(Call(type, store, "CompleteWithResearchRewards", run, true, false, 30, 1, true, "outer"), Is.Not.Null); // 같은 출격 결과의 중복 요청
                    var general = (string[])Call(type, store, "PendingResearchRewards", 1); // 일반 보상의 미수령 목록
                    advanced = (string[])Call(type, store, "PendingResearchRewards", 2); // 보류된 심화 보상 목록
                    var special = (string[])Call(type, store, "PendingResearchRewards", 3); // 최초 최종 보스의 미수령 보상
                    Assert.That(general.Length, Is.EqualTo(1)); // 같은 출격의 일반 보상 한 번
                    Assert.That(advanced.Length, Is.EqualTo(2)); // 첫 중간 보스와 첫 정예 보상만 각 한 번
                    Assert.That(special.Length, Is.EqualTo(1)); // 스테이지 첫 클리어 보상 한 번
                    Assert.That(Call(type, store, "ClaimResearchReward", general[0], formation, "converge"), Is.False); // 선행 없는 무료 선택도 차단
                    int crystals = Crystals(type, store); // 무료 선택 직전 결정
                    int gold = (int)type.GetProperty("Gold").GetValue(store); // 무료 선택 직전 골드
                    Assert.That(Call(type, store, "ClaimResearchReward", general[0], shape, "small"), Is.True); // 적격 일반 요소 하나 무료 선택
                    Assert.That(Crystals(type, store), Is.EqualTo(crystals)); // 선택권은 결정을 소모하지 않음
                    Assert.That(type.GetProperty("Gold").GetValue(store), Is.EqualTo(gold)); // 선택권은 골드를 소모하지 않음
                    Assert.That(Call(type, store, "ClaimResearchReward", general[0], shape, "large"), Is.False); // 같은 선택권 재사용 금지
                    string more = (string)Call(type, store, "Begin", aria); // 고정 상점 골드를 모을 출격
                    Call(type, store, "CompleteWithResearchRewards", more, true, false, 0, 0, false, "outer"); // 같은 스테이지 재클리어
                    Assert.That(((string[])Call(type, store, "PendingResearchRewards", 3)).Length, Is.EqualTo(1)); // 같은 스테이지의 최종 보상 중복 금지
                    Assert.That(Call(type, store, "BuyResearchShop", shape, "large"), Is.True); // 일반 요소의 골드 구백 상점 구매
                    Assert.That(Call(type, store, "BuyResearchShop", shape, "large"), Is.False); // 연구·보상·상점의 동일 ID 중복 방지
                    Assert.That(Call(type, store, "BuyResearchShop", shape, "summon"), Is.False); // 특수 연구는 상점 판매 제외
                    string pending = (string)Call(type, store, "Begin", aria); // 중단 복구를 가정한 출격
                    Assert.That(Call(type, store, "Recover"), Is.Not.Null); // 참여 보상만 복구
                    Assert.That(Call(type, store, "CompleteWithResearchRewards", pending, true, false, 30, 1, true, "other"), Is.Not.Null); // 같은 출격의 중복 결과 조회
                    Assert.That(((string[])Call(type, store, "PendingResearchRewards", 1)).Length, Is.Zero); // 강제 종료의 일반 보상 발급 금지
                    Assert.That(((string[])Call(type, store, "PendingResearchRewards", 2)).Length, Is.EqualTo(2)); // 강제 종료의 정예·보스 보상 발급 금지
                }
                using (var reopened = (IDisposable)Activator.CreateInstance(type, path)) // 재실행한 동일 프로필
                {
                    Assert.That(Call(type, reopened, "IsResearchUnlocked", shape, "small"), Is.True); // 무료 보상으로 얻은 공용 해금 보존
                    Assert.That(Call(type, reopened, "IsResearchUnlocked", shape, "large"), Is.True); // 상점으로 얻은 같은 형식의 해금 보존
                    Assert.That(((string[])Call(type, reopened, "PendingResearchRewards", 2)).Length, Is.EqualTo(2)); // 조건 미충족 보상의 영구 보류
                    Assert.That(((string[])Call(type, reopened, "PendingResearchRewards", 3)).Length, Is.EqualTo(1)); // 최초 최종 보스 보류 보존
                }
            }
            finally // 현재 시험에서 만든 파일만 제거
            {
                if (Directory.Exists(directory)) // 독립 시험 폴더 존재 여부
                {
                    foreach (string file in Directory.GetFiles(directory)) // 저장과 백업·잠금 파일
                    {
                        File.Delete(file); // 사용자 저장과 무관한 임시 파일 제거
                    }
                    Directory.Delete(directory); // 비운 시험 폴더 제거
                }
            }
        }

        [Test] // 단계별 모든 구성 요소를 얻은 뒤 보류 종료
        public void ExhaustedResearchTierClosesOnlyItsOwnPendingRewards() // 남은 보상의 단계별 독립 처리
        {
            Type type = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp", true); // 실제 영구 성장 저장소
            Type pilot = type.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 ID
            Type group = Type.GetType("ProjectG.CraftedSpellGroup, Assembly-CSharp", true); // 기존 제작 분류
            Type catalog = Type.GetType("ProjectG.FlightCraftedResearchCatalog, Assembly-CSharp", true); // 확정된 전체 연구 표
            object aria = Enum.Parse(pilot, "Aria"); // 정상 정산할 조종사
            object shape = Enum.Parse(group, "Shape"); // 남은 소형탄의 제작 분류
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 격리 시험의 기존 해금 설정
            using (var store = (IDisposable)Activator.CreateInstance(type, new object[] { null })) // 사용자 파일과 분리한 메모리 프로필
            {
                object state = type.GetField("state", hidden).GetValue(store); // 같은 원자 저장의 현재 스냅샷
                var unlockField = state.GetType().GetField("unlocks"); // 계정 공용 해금 ID 목록
                var unlocks = new System.Collections.Generic.List<string>((string[])unlockField.GetValue(state)); // 기존 기본 해금 보존
                var nodes = (System.Collections.IList)catalog.GetProperty("All", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null); // 실제 연구 목록
                foreach (object node in nodes) // 한 요소만 남기고 일반 단계 해금을 준비
                {
                    Type nodeType = node.GetType(); // 실제 연구 노드 형식
                    string key = (string)nodeType.GetProperty("Key").GetValue(node); // 분류를 포함한 안정 ID
                    if ((int)nodeType.GetField("Tier").GetValue(node) == 1 && key != "Shape.small") // 선택할 소형탄 이외의 일반 요소
                    {
                        unlocks.Add("research." + key); // 이미 획득한 공용 해금 기록
                    }
                }
                unlockField.SetValue(state, unlocks.ToArray()); // 실제 저장의 선택지 소진 직전 상태
                string first = (string)Call(type, store, "Begin", aria); // 일반과 정예 보상 조건을 만족할 출격
                Call(type, store, "CompleteWithResearchRewards", first, true, false, 30, 1, false, "outer"); // 일반·심화 보상과 일반 자격
                string second = (string)Call(type, store, "Begin", aria); // 별도 일반 보상을 받을 출격
                Call(type, store, "CompleteWithResearchRewards", second, false, false, 30, 0, false, "outer"); // 두 번째 일반 보상
                var general = (string[])Call(type, store, "PendingResearchRewards", 1); // 아직 열려 있는 일반 보상
                Assert.That(general.Length, Is.EqualTo(2)); // 일반 요소가 남아 있는 동안 두 건 보류
                Assert.That(((string[])Call(type, store, "PendingResearchRewards", 2)).Length, Is.EqualTo(1)); // 별도 심화 보상 보류
                Assert.That(Call(type, store, "ClaimResearchReward", general[0], shape, "small"), Is.True); // 마지막 일반 요소 무료 선택
                Assert.That(((string[])Call(type, store, "PendingResearchRewards", 1)).Length, Is.Zero); // 소진된 일반 단계의 나머지 보상 종료
                Assert.That(((string[])Call(type, store, "PendingResearchRewards", 2)).Length, Is.EqualTo(1)); // 다른 심화 단계의 보상 유지
                Assert.That(Call(type, store, "ClaimResearchReward", general[1], shape, "small"), Is.False); // 종료한 보상 재사용 금지
            }
        }

        private static object Call(Type type, object store, string method, params object[] supplied) // 실제 게임 어셈블리의 정산 호출
        {
            return type.GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(store, supplied); // 참조 없는 테스트 조립과 연결
        }

        private static int Crystals(Type type, object store) // 정산 전후에 비교할 공통 재료
        {
            return (int)type.GetProperty("SharedCrystals").GetValue(store); // 현재 저장된 천공 결정
        }
    }
}
