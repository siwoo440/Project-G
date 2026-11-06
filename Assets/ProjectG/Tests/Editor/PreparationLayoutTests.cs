using System.Reflection; // 실제 화면 배치 조회
using NUnit.Framework; // 시험 도구
using ProjectG; // 출격 화면 기능
using UnityEngine; // 화면 영역 기능
public class PreparationLayoutTests // 출격 준비 화면 배치 시험
{ // 시험 시작
    private static Rect Area(string name, Rect view, int index = -1) // 실제 화면 영역 조회
    { // 조회 시작
        MethodInfo method = typeof(GameFlowController).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static); // 화면 영역 계산 조회
        Assert.That(method, Is.Not.Null, name); // 실제 배치 기능 확인
        object[] arguments; // 계산 인수 준비
        if (index < 0) // 단일 영역 확인
        { // 단일 영역 시작
            arguments = new object[] // 화면 인수
            { // 인수 시작
                view // 전투 화면 영역
            }; // 인수 종료
        } // 단일 영역 종료
        else // 순서가 있는 영역
        { // 순서 영역 시작
            arguments = new object[] // 화면과 순서 인수
            { // 인수 시작
                view, // 전투 화면 영역
                index // 요소 순서
            }; // 인수 종료
        } // 순서 영역 종료
        return (Rect)method.Invoke(null, arguments); // 실제 영역 반환
    } // 조회 종료
    [TestCase(1920f, 1080f)] // 전체 HD 화면
    [TestCase(1280f, 720f)] // HD 화면
    [TestCase(960f, 540f)] // 작은 화면
    public void PreparationElementsStayInsideAndDoNotOverlap(float width, float height) // 영역 분리 확인
    { // 시험 시작
        Rect view = new Rect(37f, 23f, width, height); // 화면 여백 포함 시험 영역
        Rect portrait = Area("PreparationPortraitRect", view); // 캐릭터 그림 영역
        Rect info = Area("PreparationInfoRect", view); // 정보 패널 영역
        Rect detail = Area("PreparationDetailRect", view); // 캐릭터 설명 영역
        Rect summary = Area("PreparationSummaryRect", view); // 성장 요약 영역
        Rect skill1 = Area("SkillRect", view, 0); // 첫 공용 스킬 영역
        Rect skill2 = Area("SkillRect", view, 1); // 둘째 공용 스킬 영역
        Rect action1 = Area("PreparationActionRect", view, 0); // 출격 버튼 영역
        Rect action2 = Area("PreparationActionRect", view, 1); // 뒤로 버튼 영역
        Rect action3 = Area("PreparationActionRect", view, 2); // 성장 버튼 영역
        Assert.That(portrait.xMin, Is.GreaterThanOrEqualTo(view.xMin)); // 그림 왼쪽 경계
        Assert.That(portrait.yMin, Is.GreaterThanOrEqualTo(view.yMin)); // 그림 위쪽 경계
        Assert.That(info.xMax, Is.LessThanOrEqualTo(view.xMax)); // 정보 오른쪽 경계
        Assert.That(info.yMax, Is.LessThanOrEqualTo(view.yMax)); // 정보 아래쪽 경계
        Assert.That(portrait.Overlaps(info), Is.False); // 그림과 정보 분리
        Assert.That(portrait.Overlaps(skill1), Is.False); // 그림과 첫 스킬 분리
        Assert.That(portrait.Overlaps(skill2), Is.False); // 그림과 둘째 스킬 분리
        Assert.That(skill1.Overlaps(skill2), Is.False); // 스킬 버튼 간격
        Assert.That(detail.Overlaps(summary), Is.False); // 설명과 성장 정보 분리
        Assert.That(info.Contains(detail.min) && info.Contains(detail.max), Is.True); // 설명의 패널 소속
        Assert.That(info.Contains(summary.min) && info.Contains(summary.max), Is.True); // 성장 정보의 패널 소속
        Assert.That(action1.Overlaps(action2) || action2.Overlaps(action3), Is.False); // 하단 버튼 간격
        Assert.That(action1.yMin, Is.GreaterThan(info.yMax)); // 정보와 버튼 세로 간격
        Assert.That(action3.xMax, Is.LessThanOrEqualTo(view.xMax)); // 마지막 버튼 경계
        Assert.That(action3.yMax, Is.LessThanOrEqualTo(view.yMax)); // 하단 버튼 경계
    } // 시험 종료
    [TestCase(1920f, 1080f)] // 전체 HD 화면
    [TestCase(1280f, 720f)] // HD 화면
    [TestCase(960f, 540f)] // 작은 화면
    public void ScrollHintStaysWithinPanelOrnamentAndBelowScrollableText(float width, float height) // 긴 정보 안내 여백
    { // 시험 시작
        Rect view = new Rect(37f, 23f, width, height); // 화면 여백 포함 시험 영역
        Rect panel = Area("PreparationInfoRect", view); // 실제 장식 패널
        Rect summary = Area("PreparationScrollableSummaryRect", view); // 스크롤 가능한 요약 영역
        Rect hint = Area("PreparationScrollHintRect", view); // 스크롤 안내 영역
        Assert.That(hint.xMin, Is.GreaterThanOrEqualTo(panel.xMin + panel.width * 0.08f)); // 왼쪽 장식 여백
        Assert.That(hint.xMax, Is.LessThanOrEqualTo(panel.xMax - panel.width * 0.08f)); // 오른쪽 장식 여백
        Assert.That(hint.yMax, Is.LessThanOrEqualTo(panel.yMax - panel.height * 0.10f)); // 하단 장식 여백
        Assert.That(summary.yMax, Is.LessThan(hint.yMin)); // 내용과 안내 사이 여백
        Assert.That(hint.height, Is.GreaterThanOrEqualTo(14f)); // 작은 화면 최소 글자 높이
    } // 시험 종료
    [TestCase(100f, 100f, false)] // 정확히 들어가는 내용
    [TestCase(100f, 99f, false)] // 여백이 있는 내용
    [TestCase(100f, 101f, true)] // 영역을 넘는 내용
    public void ScrollHintAppearsOnlyWhenContentOverflows(float height, float contentHeight, bool expected) // 안내 표시 조건
    { // 시험 시작
        MethodInfo method = typeof(GameFlowController).GetMethod("PreparationSummaryNeedsScroll", BindingFlags.NonPublic | BindingFlags.Static); // 실제 표시 조건 조회
        Assert.That(method, Is.Not.Null); // 스크롤 조건 연결 확인
        object[] arguments = new object[] // 표시 조건 인수
        { // 인수 시작
            height, // 표시 영역 높이
            contentHeight // 실제 문구 높이
        }; // 인수 종료
        Assert.That((bool)method.Invoke(null, arguments), Is.EqualTo(expected)); // 실제 넘침에서만 안내
    } // 시험 종료
    [Test] // 요약 스크롤 입력 시험
    public void PreparationScrollMovesWithoutGoingAboveStart() // 키보드 스크롤 한계
    { // 시험 시작
        GameObject holder = new GameObject("PreparationScroll"); // 화면 시험 객체
        try // 시험 범위 준비
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 생성
            MethodInfo scroll = typeof(GameFlowController).GetMethod("ScrollPreparationSummary", BindingFlags.NonPublic | BindingFlags.Instance); // 스크롤 입력 조회
            FieldInfo position = typeof(GameFlowController).GetField("preparationSummaryScroll", BindingFlags.NonPublic | BindingFlags.Instance); // 저장된 스크롤 조회
            Assert.That(scroll, Is.Not.Null); // 키보드 스크롤 기능 확인
            Assert.That(position, Is.Not.Null); // 스크롤 상태 확인
            object[] down = new object[] // 아래 이동 인수
            { // 인수 시작
                120f // 아래쪽 이동량
            }; // 인수 종료
            scroll.Invoke(flow, down); // 스크롤 아래 이동
            Assert.That(((Vector2)position.GetValue(flow)).y, Is.EqualTo(120f)); // 아래 이동 확인
            object[] up = new object[] // 위 이동 인수
            { // 인수 시작
                -300f // 위쪽 이동량
            }; // 인수 종료
            scroll.Invoke(flow, up); // 스크롤 위 이동
            Assert.That(((Vector2)position.GetValue(flow)).y, Is.Zero); // 시작 위 이동 차단
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 캐릭터 전신 이미지 선택 시험
    public void PreparationUsesMatchingStandingArtwork() // 기획서 원본 연결 확인
    { // 시험 시작
        GameObject holder = new GameObject("PreparationPortrait"); // 화면 시험 객체
        try // 시험 범위 준비
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 생성
            flow.Begin(holder.AddComponent<Camera>()); // 메뉴 초기화
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 화면
            Assert.That(flow.OpenPreparation(), Is.True); // 첫 캐릭터 준비 화면
            MethodInfo portrait = typeof(GameFlowController).GetMethod("PreparationPortrait", BindingFlags.NonPublic | BindingFlags.Instance); // 실제 그림 선택 조회
            Assert.That(portrait, Is.Not.Null); // 준비 화면 그림 선택 확인
            Texture2D aria = Resources.Load<Texture2D>("CharacterPortraits/aria_standing"); // 아리아 기획서 원본
            Texture2D rian = Resources.Load<Texture2D>("CharacterPortraits/rian_standing"); // 리안 기획서 원본
            Assert.That(aria, Is.Not.Null); // 아리아 원본 로드
            Assert.That(rian, Is.Not.Null); // 리안 원본 로드
            Assert.That(portrait.Invoke(flow, null), Is.SameAs(aria)); // 아리아 전신 표시
            Assert.That(flow.Back(), Is.True); // 캐릭터 선택 복귀
            Assert.That(flow.SelectCharacter(CharacterType.Sniper), Is.True); // 리안 선택
            Assert.That(flow.OpenPreparation(), Is.True); // 리안 준비 화면
            Assert.That(portrait.Invoke(flow, null), Is.SameAs(rian)); // 리안 전신 표시
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 객체 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(0, GameFlowScreen.Battle)] // 출격 버튼
    [TestCase(1, GameFlowScreen.CharacterSelect)] // 뒤로 버튼
    [TestCase(2, GameFlowScreen.Growth)] // 영구 성장 버튼
    public void DrawnActionPositionsTriggerTheirIntendedScreens(int index, GameFlowScreen expected) // 표시 위치의 클릭 연결
    { // 시험 시작
        GameObject holder = new GameObject("PreparationClick"); // 화면 시험 객체
        try // 시험 범위 준비
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 생성
            flow.Begin(holder.AddComponent<Camera>()); // 메뉴 초기화
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 화면
            Assert.That(flow.OpenPreparation(), Is.True); // 출격 준비 화면
            Rect view = new Rect(0f, 0f, 1280f, 720f); // 시험 화면 크기
            Rect skill = Area("SkillRect", view, 1); // 산탄 버튼 표시 위치
            MethodInfo click = typeof(GameFlowController).GetMethod("HandleClick", BindingFlags.NonPublic | BindingFlags.Instance); // 실제 클릭 처리 조회
            Assert.That(click, Is.Not.Null); // 클릭 처리 연결 확인
            object[] skillClick = new object[] // 스킬 클릭 인수
            { // 인수 시작
                view, // 시험 화면 영역
                skill.center // 산탄 버튼 중심
            }; // 인수 종료
            click.Invoke(flow, skillClick); // 산탄 버튼 선택
            Assert.That(flow.SelectedCommonSkill, Is.EqualTo(CommonSkill.Scatter)); // 공용 스킬 변경 확인
            Rect action = Area("PreparationActionRect", view, index); // 실제 버튼 표시 위치
            object[] actionClick = new object[] // 하단 버튼 클릭 인수
            { // 인수 시작
                view, // 시험 화면 영역
                action.center // 하단 버튼 중심
            }; // 인수 종료
            click.Invoke(flow, actionClick); // 실제 버튼 선택
            Assert.That(flow.Screen, Is.EqualTo(expected)); // 버튼별 화면 전환 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 객체 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
