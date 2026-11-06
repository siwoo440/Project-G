using NUnit.Framework; // 테스트 도구
using System.Reflection; // 비공개 화면 배치 확인
using UnityEngine; // 유니티 장면 기능
using ProjectG; // 영구 성장 기능
public class GrowthScreenTests // 영구 성장 화면 시험
{ // 시험 시작
    private sealed class MemoryStore : IProgressStorage // 격리된 성장 저장소
    { // 저장소 시작
        private string contents; // 저장된 내용
        public ProgressReadStatus Read(out string data, out string error) // 성장 기록 읽기
        { // 읽기 시작
            data = contents; // 원문 전달
            error = null; // 오류 없음
            return contents == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 저장 존재 확인
        } // 읽기 종료
        public bool Write(string expectedData, string data, out string error) // 기록 비교와 저장
        { // 저장 시작
            error = contents == expectedData ? null : "저장 충돌"; // 충돌 안내
            if (error != null) // 저장 충돌 확인
            { // 차단 시작
                return false; // 기존 기록 보존
            } // 차단 종료
            contents = data; // 새 기록 보관
            return true; // 저장 성공
        } // 저장 종료
    } // 저장소 종료
    [TestCase(1920f, 1080f)] // 전체 HD 화면
    [TestCase(1280f, 720f)] // HD 화면
    [TestCase(960f, 540f)] // 작은 화면
    public void PreparationSummaryStaysInsideViewAndAboveButtons(float width, float height) // 성장 요약과 버튼 영역 분리
    { // 시험 시작
        Rect view = new Rect(37f, 23f, width, height); // 화면 여백이 있는 시험 영역
        MethodInfo summaryMethod = typeof(GameFlowController).GetMethod("PreparationSummaryRect", BindingFlags.Static | BindingFlags.NonPublic); // 요약 영역 계산 조회
        Assert.That(summaryMethod, Is.Not.Null); // 실제 배치 계산 연결
        object[] summaryArguments = new object[] // 요약 계산 인수
        { // 인수 시작
            view // 화면 영역
        }; // 인수 종료
        Rect summary = (Rect)summaryMethod.Invoke(null, summaryArguments); // 성장 요약 영역 계산
        MethodInfo buttonMethod = typeof(GameFlowController).GetMethod("ButtonRect", BindingFlags.Static | BindingFlags.NonPublic); // 실제 버튼 위치 조회
        object[] buttonArguments = new object[] // 버튼 계산 인수
        { // 인수 시작
            view, // 화면 영역
            1 // 버튼 번호
        }; // 인수 종료
        Rect button = (Rect)buttonMethod.Invoke(null, buttonArguments); // 인접한 버튼 영역 계산
        Assert.That(summary.xMin, Is.GreaterThanOrEqualTo(view.xMin)); // 화면 왼쪽 이탈 방지
        Assert.That(summary.xMax, Is.LessThanOrEqualTo(view.xMax)); // 화면 오른쪽 이탈 방지
        Assert.That(summary.yMin, Is.GreaterThanOrEqualTo(view.yMin)); // 화면 위쪽 이탈 방지
        Assert.That(summary.yMax, Is.LessThan(button.yMin)); // 버튼 위 여백 보존
    } // 시험 종료
    [Test] // 요약 글자 크기 조정 시험
    public void PreparationSummaryUsesLargestFittingFontAndPreservesMinimum() // 글자 크기와 긴 문구 확인
    { // 시험 시작
        MethodInfo method = typeof(GameFlowController).GetMethod("PreparationSummaryFontSize", BindingFlags.Static | BindingFlags.NonPublic); // 글자 크기 계산 조회
        Assert.That(method, Is.Not.Null); // 실제 크기 계산 연결
        System.Func<int, float> normal = size => size * 4f; // 세 줄과 줄 간격 시험 높이
        object[] normalArguments = new object[] // 일반 문구 인수
        { // 인수 시작
            96f, // 표시 영역 높이
            27, // 원하는 글자 크기
            normal // 일반 문구 높이 계산
        }; // 인수 종료
        int fitting = (int)method.Invoke(null, normalArguments); // 맞는 최대 글자 크기
        Assert.That(fitting, Is.EqualTo(24)); // 불필요한 축소 방지
        System.Func<int, float> longText = size => size * 20f; // 영역을 초과하는 긴 문구
        object[] longArguments = new object[] // 긴 문구 인수
        { // 인수 시작
            96f, // 표시 영역 높이
            27, // 원하는 글자 크기
            longText // 긴 문구 높이 계산
        }; // 인수 종료
        int minimum = (int)method.Invoke(null, longArguments); // 긴 문구의 최소 글자 크기
        Assert.That(minimum, Is.EqualTo(23)); // 기본 크기의 85% 하한 유지
    } // 시험 종료
    [TestCase(1080f, 23)] // 전체 HD의 최소 글자 크기
    [TestCase(720f, 16)] // HD의 최소 글자 크기
    [TestCase(540f, 14)] // 작은 화면의 절대 최소 크기
    public void PreparationSummaryMinimumRemainsReadable(float height, int expected) // 해상도별 글자 하한
    { // 시험 시작
        MethodInfo method = typeof(GameFlowController).GetMethod("PreparationSummaryFontSize", BindingFlags.Static | BindingFlags.NonPublic); // 실제 크기 계산 조회
        Assert.That(method, Is.Not.Null); // 크기 계산 기능 확인
        int preferred = Mathf.Max(14, Mathf.RoundToInt(height * 0.025f)); // 화면 비례 기본 본문 크기
        System.Func<int, float> overflowing = size => height * 2f; // 어떤 크기에서도 넘치는 문구
        object[] arguments = new object[] // 긴 문구 계산 인수
        { // 인수 시작
            height * 0.17f, // 성장 요약 영역 높이
            preferred, // 해상도별 기본 글자 크기
            overflowing // 긴 문구 높이 계산
        }; // 인수 종료
        int actual = (int)method.Invoke(null, arguments); // 실제 최소 글자 크기
        Assert.That(actual, Is.EqualTo(expected)); // 화면별 하한 확인
        Assert.That(actual, Is.LessThanOrEqualTo(preferred)); // 기본 크기 초과 방지
    } // 시험 종료
    [Test] // 화면 진입과 차단 시험
    public void GrowthScreenReturnsToPreparationWithoutStartingBattle() // 출격 준비 복귀 확인
    { // 시험 시작
        GameObject holder = new GameObject("GrowthMenu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 생성
            flow.Begin(holder.AddComponent<Camera>()); // 메뉴 초기화
            Assert.That(flow.OpenGrowth(), Is.False); // 타이틀에서 진입 차단
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(flow.OpenPreparation(), Is.True); // 출격 준비 진입
            flow.InputBlocked = true; // 외부 모달 입력 보유
            Assert.That(flow.OpenGrowth(), Is.False); // 모달 중 진입 차단
            flow.InputBlocked = false; // 외부 모달 종료
            Assert.That(flow.OpenGrowth(), Is.True); // 영구 성장 화면 진입
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Growth)); // 성장 화면 표시
            Assert.That(flow.StartRun(), Is.False); // 성장 화면에서 출격 차단
            flow.InputBlocked = true; // 다른 화면 입력 보유
            Assert.That(flow.Back(), Is.False); // 모달 중 복귀 차단
            flow.InputBlocked = false; // 입력 제한 해제
            Assert.That(flow.Back(), Is.True); // 성장 화면 닫기
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 준비 화면 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 메뉴 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 성장 화면 캐릭터 전환 시험
    public void GrowthCharacterSelectionUpdatesPreparationAndHonorsInputBlock() // 성장과 출격 선택 동기화
    { // 시험 시작
        GameObject holder = new GameObject("GrowthSelection"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 생성
            flow.Begin(holder.AddComponent<Camera>()); // 메뉴 초기화
            Assert.That(flow.SelectGrowthCharacter(CharacterType.Sniper), Is.False); // 다른 화면 선택 차단
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(flow.OpenGrowth(), Is.True); // 성장 화면 진입
            flow.InputBlocked = true; // 모달 입력 보유
            Assert.That(flow.SelectGrowthCharacter(CharacterType.Sniper), Is.False); // 모달 중 선택 차단
            flow.InputBlocked = false; // 모달 입력 해제
            Assert.That(flow.SelectGrowthCharacter((CharacterType)99), Is.False); // 잘못된 캐릭터 차단
            Assert.That(flow.SelectGrowthCharacter(CharacterType.Sniper), Is.True); // 저격형 선택
            Assert.That(flow.GrowthSummary(), Does.Contain("약점 사냥 1").And.Contain("관통 궤도 1")); // 저격형 기본 후보 표시
            Assert.That(flow.Back(), Is.True); // 출격 준비 복귀
            Assert.That(flow.SelectedCharacter, Is.EqualTo(CharacterType.Sniper)); // 선택 캐릭터 유지
            CharacterType started = CharacterType.Rapid; // 출격 캐릭터 기본값
            flow.RunRequested += (character, skill) => started = character; // 출격 캐릭터 기록
            Assert.That(flow.StartRun(), Is.True); // 선택 캐릭터 출격
            Assert.That(started, Is.EqualTo(CharacterType.Sniper)); // 성장 선택과 출격 일치
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 메뉴 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 캐릭터별 실제 성장과 후보 시험
    public void GrowthSummaryShowsStoredResourcesAndOnlySelectedCharacterNodes() // 실제 보유량과 기본 후보 확인
    { // 시험 시작
        GameObject holder = new GameObject("GrowthHost"); // 시험 장면 생성
        PermanentProgressService service = new PermanentProgressService(new MemoryStore()); // 격리 성장 서비스
        try // 시험 시작
        { // 시험 범위 시작
            Assert.That(service.Load(out string error), Is.True, error); // 저장 초기화
            Assert.That(service.TryBegin("rapid-test", CharacterType.Rapid, out error), Is.True, error); // 속사형 출격 기록
            Assert.That(service.TrySettle("rapid-test", CharacterType.Rapid, true, out _, out error), Is.True, error); // 속사형 클리어 기록
            Assert.That(service.TryBegin("sniper-test", CharacterType.Sniper, out error), Is.True, error); // 저격형 출격 기록
            Assert.That(service.TrySettle("sniper-test", CharacterType.Sniper, false, out _, out error), Is.True, error); // 저격형 실패 기록
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 출격 관리자 생성
            host.Begin(holder.AddComponent<Camera>(), service); // 격리 성장 연결
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(host.Flow.OpenGrowth(), Is.True); // 속사형 성장 화면
            string rapid = host.Flow.GrowthSummary(); // 표시될 속사형 성장 정보
            Assert.That(rapid, Does.Contain("숙련도 20")); // 실제 속사 숙련도
            Assert.That(rapid, Does.Contain("공통 재료 9")); // 실제 공통 재료
            Assert.That(rapid, Does.Contain("전용 재료 0")); // 실제 전용 재료
            Assert.That(rapid, Does.Contain("추격 사격 1").And.Contain("확산 포화 1")); // 속사형 기본 후보
            Assert.That(rapid, Does.Not.Contain("약점 사냥 1")); // 다른 캐릭터 후보 제외
            Assert.That(rapid, Does.Contain("기본 후보").And.Contain("미정")); // 무료 후보와 미정 안내
            Assert.That(rapid, Does.Contain("기본·집중탄 50회").And.Contain("아리아 첫 과제: 미완료")); // 구현된 과제 조건과 상태 안내
            Assert.That(rapid, Does.Not.Contain("조건·보상 미정")); // 결정된 과제의 잘못된 미정 표시 방지
            Assert.That(host.Flow.Back(), Is.True); // 출격 준비 복귀
            Assert.That(host.Flow.Back(), Is.True); // 캐릭터 선택 복귀
            Assert.That(host.Flow.SelectCharacter(CharacterType.Sniper), Is.True); // 저격형 선택
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 저격형 준비
            Assert.That(host.Flow.OpenGrowth(), Is.True); // 저격형 성장 화면
            string sniper = host.Flow.GrowthSummary(); // 표시될 저격형 성장 정보
            Assert.That(sniper, Does.Contain("숙련도 10")); // 실제 저격 숙련도
            Assert.That(sniper, Does.Contain("공통 재료 9")); // 공통 재료 유지
            Assert.That(sniper, Does.Contain("약점 사냥 1").And.Contain("관통 궤도 1")); // 저격형 기본 후보
            Assert.That(sniper, Does.Not.Contain("추격 사격 1")); // 다른 캐릭터 후보 제외
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 장면 제거
            service.Dispose(); // 시험 저장소 해제
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
