using NUnit.Framework; // 테스트 도구
using ProjectG; // 성장 상태 기능
public class LevelStateTests // 경험치와 레벨 시험
{ // 시험 시작
    [Test] // 경험치 곡선 시험
    public void ExperienceUsesDocumentedLevelThresholds() // 요구 경험치 확인
    { // 시험 시작
        LevelState level = new LevelState(); // 전투 레벨 생성
        Assert.That(level.Level, Is.EqualTo(1)); // 시작 레벨 확인
        Assert.That(level.Required, Is.EqualTo(10)); // 첫 요구 경험치 확인
        level.Gain(9); // 첫 레벨 미만 경험치
        Assert.That(level.Level, Is.EqualTo(1)); // 조기 레벨업 방지
        level.Gain(1); // 첫 요구량 충족
        Assert.That(level.Level, Is.EqualTo(2)); // 첫 레벨업 확인
        Assert.That(level.Experience, Is.Zero); // 이월 경험치 확인
        Assert.That(level.Required, Is.EqualTo(16)); // 다음 요구 경험치 확인
        Assert.That(level.PendingChoices, Is.EqualTo(1)); // 카드 선택 예약 확인
    } // 시험 종료
    [Test] // 연속 레벨업 시험
    public void MultipleLevelUpsQueueSeparateSelections() // 여러 보상 순서 확인
    { // 시험 시작
        LevelState level = new LevelState(); // 전투 레벨 생성
        level.Gain(30); // 두 단계 이상 경험치
        Assert.That(level.Level, Is.EqualTo(3)); // 두 번 레벨업 확인
        Assert.That(level.Experience, Is.EqualTo(4)); // 남은 경험치 확인
        Assert.That(level.Required, Is.EqualTo(22)); // 세 번째 요구량 확인
        Assert.That(level.PendingChoices, Is.EqualTo(2)); // 카드 두 번 예약
        Assert.That(level.ConsumeChoice(), Is.True); // 첫 보상 소비
        Assert.That(level.PendingChoices, Is.EqualTo(1)); // 다음 보상 유지
        Assert.That(level.ConsumeChoice(), Is.True); // 둘째 보상 소비
        Assert.That(level.ConsumeChoice(), Is.False); // 추가 보상 방지
    } // 시험 종료
    [Test] // 잘못된 경험치 시험
    public void NegativeExperienceNeverReducesProgress() // 음수 경험치 방지
    { // 시험 시작
        LevelState level = new LevelState(); // 전투 레벨 생성
        level.Gain(7); // 경험치 획득
        level.Gain(-100); // 음수 입력
        Assert.That(level.Experience, Is.EqualTo(7)); // 경험치 보존
        Assert.That(level.Level, Is.EqualTo(1)); // 레벨 보존
    } // 시험 종료
} // 시험 종료
