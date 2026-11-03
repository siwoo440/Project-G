using NUnit.Framework; // 테스트 도구
using ProjectG; // 전투 계산 기능
public class CombatRulesTests // 전투 규칙 시험
{ // 시험 시작
    [Test] // 집중 홀드 시험
    public void HoldFocusTracksKeyState() // 홀드 입력 확인
    { // 시험 시작
        Assert.That(CombatRules.Focus(false, false, true, false), Is.True); // 키를 누른 집중
        Assert.That(CombatRules.Focus(true, false, false, false), Is.False); // 키를 놓은 해제
    } // 시험 종료
    [Test] // 집중 토글 시험
    public void ToggleChangesOnlyOnPress() // 토글 입력 확인
    { // 시험 시작
        Assert.That(CombatRules.Focus(false, true, true, true), Is.True); // 첫 입력 집중
        Assert.That(CombatRules.Focus(true, false, true, true), Is.True); // 키 유지 중 유지
        Assert.That(CombatRules.Focus(true, true, true, true), Is.False); // 재입력 해제
    } // 시험 종료
    [Test] // 자동 사격 시험
    public void ShotTimerFiresImmediatelyAndRespectsInterval() // 발사 간격 확인
    { // 시험 시작
        float timer = 0f; // 초기 발사 대기
        Assert.That(CombatRules.ShouldFire(ref timer, 0f, 0.25f), Is.True); // 첫 즉시 발사
        Assert.That(CombatRules.ShouldFire(ref timer, 0.1f, 0.25f), Is.False); // 간격 전 대기
        Assert.That(CombatRules.ShouldFire(ref timer, 0.15f, 0.25f), Is.True); // 간격 후 발사
    } // 시험 종료
} // 시험 종료
