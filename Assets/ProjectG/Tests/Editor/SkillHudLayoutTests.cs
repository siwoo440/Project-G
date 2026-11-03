using NUnit.Framework; // 테스트 도구
using UnityEngine; // 화면 좌표 기능
using ProjectG; // 스킬 화면 기능
public class SkillHudLayoutTests // 스킬 화면 배치 시험
{ // 시험 시작
    [Test] // 오른쪽 아래 배치 시험
    public void ThreeSquareSlotsStayInsideBottomRight() // 세 칸 배치 확인
    { // 시험 시작
        Rect view = new Rect(0f, 0f, 1920f, 1080f); // 시험 화면 영역
        Rect first = SkillHudLayout.Slot(view, 0); // J 칸 위치
        Rect second = SkillHudLayout.Slot(view, 1); // K 칸 위치
        Rect third = SkillHudLayout.Slot(view, 2); // L 칸 위치
        Assert.That(first.width, Is.EqualTo(first.height).Within(0.001f)); // 정사각형 확인
        Assert.That(second.width, Is.EqualTo(first.width).Within(0.001f)); // 동일 크기 확인
        Assert.That(third.width, Is.EqualTo(first.width).Within(0.001f)); // 동일 크기 확인
        Assert.That(first.xMax, Is.LessThan(second.xMin)); // 첫 칸 간격 확인
        Assert.That(second.xMax, Is.LessThan(third.xMin)); // 둘째 칸 간격 확인
        Assert.That(third.xMax, Is.LessThanOrEqualTo(view.xMax)); // 오른쪽 경계 확인
        Assert.That(third.yMax, Is.LessThanOrEqualTo(view.yMax)); // 아래 경계 확인
        Assert.That(first.xMin, Is.GreaterThanOrEqualTo(view.xMin)); // 왼쪽 경계 확인
        Assert.That(first.yMin, Is.GreaterThan(view.center.y)); // 아래쪽 영역 확인
    } // 시험 종료
    [TestCase(8f, "8")] // 정수 쿨타임
    [TestCase(7.01f, "8")] // 올림 표시
    [TestCase(0.01f, "1")] // 마지막 1초 표시
    [TestCase(0f, "")] // 준비 상태 표시
    public void CooldownUsesCeilingSeconds(float remaining, string expected) // 남은 시간 숫자 확인
    { // 시험 시작
        Assert.That(SkillHudLayout.Countdown(remaining), Is.EqualTo(expected)); // 카운트다운 확인
    } // 시험 종료
    [TestCase(1920f, 1080f, 180f)] // 고화질 화면
    [TestCase(1280f, 720f, 162f)] // 일반 화면
    public void SlotsRestorePreviousSize(float width, float height, float expected) // 직전 크기 복원 확인
    { // 시험 시작
        Rect view = new Rect(0f, 0f, width, height); // 시험 화면 영역
        Assert.That(SkillHudLayout.Slot(view, 0).width, Is.EqualTo(expected).Within(0.001f)); // 복원 크기 확인
    } // 시험 종료
    [Test] // 좁은 화면 크기 시험
    public void SmallViewportKeepsAllSlotsInsideScreen() // 좁은 화면 경계 확인
    { // 시험 시작
        Rect view = new Rect(0f, 0f, 24f, 100f); // 좁은 시험 화면
        Rect first = SkillHudLayout.Slot(view, 0); // 첫 칸 위치
        Rect second = SkillHudLayout.Slot(view, 1); // 둘째 칸 위치
        Rect third = SkillHudLayout.Slot(view, 2); // 셋째 칸 위치
        Assert.That(first.xMin, Is.GreaterThanOrEqualTo(view.xMin)); // 왼쪽 경계 확인
        Assert.That(first.xMax, Is.LessThan(second.xMin)); // 첫 칸 간격 확인
        Assert.That(second.xMax, Is.LessThan(third.xMin)); // 둘째 칸 간격 확인
        Assert.That(third.xMax, Is.LessThanOrEqualTo(view.xMax)); // 오른쪽 경계 확인
    } // 시험 종료
    [Test] // 스킬 이미지 자원 시험
    public void GeneratedSkillImagesAreAvailable() // 이미지 자원 확인
    { // 시험 시작
        Assert.That(Resources.Load<Texture2D>("SkillIcons/rapid"), Is.Not.Null); // 고유 스킬 이미지
        Assert.That(Resources.Load<Texture2D>("SkillIcons/shield"), Is.Not.Null); // 보호막 이미지
        Assert.That(Resources.Load<Texture2D>("SkillIcons/scatter"), Is.Not.Null); // 산탄 이미지
        Assert.That(Resources.Load<Texture2D>("SkillIcons/ultimate"), Is.Not.Null); // 필살기 이미지
    } // 시험 종료
} // 시험 종료
