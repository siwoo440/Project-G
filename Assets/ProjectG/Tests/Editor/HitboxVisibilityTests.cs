using NUnit.Framework; // 테스트 도구
using ProjectG; // 피격 표시 기능
public class HitboxVisibilityTests // 피격 표시 시험
{ // 시험 시작
    [TestCase(false, false, false, false)] // 일반 표시 끄기
    [TestCase(false, true, false, true)] // 일반 표시 켜기
    [TestCase(false, false, true, false)] // 집중 설정과 일반 분리
    [TestCase(false, true, true, true)] // 두 표시 켠 일반
    [TestCase(true, false, false, false)] // 집중 표시 끄기
    [TestCase(true, false, true, true)] // 집중 표시 켜기
    [TestCase(true, true, false, false)] // 일반 설정과 집중 분리
    [TestCase(true, true, true, true)] // 두 표시 켠 집중
    public void VisibilityUsesOnlyCurrentMode(bool focused, bool normalVisible, bool focusVisible, bool expected) // 상태별 표시 확인
    { // 시험 시작
        Assert.That(HitboxVisibility.IsVisible(focused, normalVisible, focusVisible), Is.EqualTo(expected)); // 독립 설정 확인
    } // 시험 종료
} // 시험 종료
