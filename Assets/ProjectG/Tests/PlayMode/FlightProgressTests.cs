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
