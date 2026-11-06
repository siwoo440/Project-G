using System.Reflection; // 실제 안내 영역 조회
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 전투 화면
using UnityEngine; // 화면 영역 기능
public class TutorialLayoutTests // 첫 출격 안내 화면 배치 시험
{ // 시험 시작
    private static Rect Area(System.Type type, string methodName, Rect view) // 실제 영역 계산 조회
    { // 조회 시작
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // 실제 배치 기능 검색
        Assert.That(method, Is.Not.Null, methodName + " 배치 계산이 필요합니다."); // 실제 계산 존재 확인
        object[] arguments = new object[] // 화면 영역 인수
        { // 인수 시작
            view // 전체 전투 표시 영역
        }; // 인수 종료
        return (Rect)method.Invoke(null, arguments); // 계산된 실제 표시 영역
    } // 조회 종료
    [TestCase(1920f, 1080f)] // 전체 HD 전투 화면
    [TestCase(960f, 540f)] // 작은 전투 화면
    public void TutorialPanelDoesNotCoverStageTextOrSkillSlots(float width, float height) // 실제 안내 영역 분리
    { // 시험 시작
        Rect view = new Rect(37f, 23f, width, height); // 여백이 있는 전투 화면
        Rect panel = Area(typeof(TutorialGuide), "PanelRect", view); // 실제 튜토리얼 영역
        Rect header = Area(typeof(StageDirector), "StageHeaderRect", view); // 실제 스테이지 진행 정보
        Rect phase = Area(typeof(StageDirector), "PhaseNoticeRect", view); // 실제 보스 단계 안내
        Rect context = Area(typeof(StageDirector), "ContextNoticeRect", view); // 실제 사건 안내
        Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(view.xMin)); // 왼쪽 화면 경계
        Assert.That(panel.xMax, Is.LessThanOrEqualTo(view.xMax)); // 오른쪽 화면 경계
        Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.yMin)); // 상단 화면 경계
        Assert.That(panel.yMax, Is.LessThanOrEqualTo(view.yMax)); // 하단 화면 경계
        Assert.That(panel.Overlaps(header), Is.False); // 스테이지 정보 분리
        Assert.That(panel.Overlaps(phase), Is.False); // 보스 단계 안내 분리
        Assert.That(panel.Overlaps(context), Is.False); // 사건 문구 분리
        for (int index = 0; index < 3; index++) // 세 스킬 칸 순회
        { // 확인 시작
            Assert.That(panel.Overlaps(SkillHudLayout.Slot(view, index)), Is.False); // 스킬 칸 가림 방지
        } // 확인 종료
    } // 시험 종료
} // 시험 종료
