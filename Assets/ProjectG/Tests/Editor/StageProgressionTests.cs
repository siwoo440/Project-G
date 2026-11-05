using System.Collections.Generic; // 단계 기록 기능
using NUnit.Framework; // 테스트 도구
using ProjectG; // 스테이지 진행 기능
public class StageProgressionTests // 스테이지 순서 시험
{ // 시험 시작
    [Test] // 목표 구간과 보스 대기 시험
    public void ProgressesInPlannedOrderWithoutSkippingBosses() // 스테이지 순서 확인
    { // 시험 시작
        StageProgression stage = new StageProgression(); // 스테이지 상태 생성
        List<StagePhase> entered = new List<StagePhase>(); // 진입 단계 기록
        stage.PhaseChanged += phase => entered.Add(phase); // 단계 변경 수집
        stage.Advance(20f, 20f); // 준비 구간 종료
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.CombatOne)); // 첫 일반 전투
        stage.Advance(45f, 45f); // 첫 전투 종료
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.UpgradeOne)); // 첫 강화 선택
        stage.Advance(30f, 30f); // 선택 지연
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.UpgradeOne)); // 선택 전 대기
        Assert.That(stage.CompleteUpgrade(), Is.True); // 첫 강화 선택 완료
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.MidBoss)); // 중간 보스 진입
        stage.Advance(100f, 100f); // 보스 처치 지연
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.MidBoss)); // 보스 강제 종료 방지
        Assert.That(stage.DefeatBoss(), Is.True); // 중간 보스 처치
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.CombatTwo)); // 두 번째 일반 전투
        stage.Advance(50f, 50f); // 두 번째 전투 종료
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.UpgradeTwo)); // 두 번째 강화 선택
        Assert.That(stage.CompleteUpgrade(), Is.True); // 두 번째 강화 선택 완료
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.FinalBoss)); // 최종 보스 진입
        stage.Advance(100f, 100f); // 최종 보스 처치 지연
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.FinalBoss)); // 최종 보스 강제 종료 방지
        Assert.That(stage.DefeatBoss(), Is.True); // 최종 보스 처치
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.Result)); // 결과 상태
        Assert.That(entered, Is.EqualTo(new StagePhase[] // 진입 순서 확인
        { // 목록 시작
            StagePhase.CombatOne, StagePhase.UpgradeOne, StagePhase.MidBoss, StagePhase.CombatTwo, // 전반 진행 순서
            StagePhase.UpgradeTwo, StagePhase.FinalBoss, StagePhase.Result // 후반 진행 순서
        })); // 목록 종료
    } // 시험 종료
    [Test] // 정지 시간 분리 시험
    public void CountsRealTimeWhileCombatTimeIsPaused() // 일시정지 시간 구분
    { // 시험 시작
        StageProgression stage = new StageProgression(); // 스테이지 상태 생성
        stage.Advance(5f, 5f); // 일반 진행
        stage.Advance(0f, 3f); // 카드 화면 정지
        Assert.That(stage.CombatSeconds, Is.EqualTo(5f)); // 전투 시간 유지
        Assert.That(stage.TotalSeconds, Is.EqualTo(8f)); // 전체 시간 증가
        Assert.That(stage.PhaseSeconds, Is.EqualTo(5f)); // 구간 시간 유지
    } // 시험 종료
    [Test] // 큰 경과 시간과 잘못된 이벤트 시험
    public void CarriesTimeAcrossTimedPhasesButWaitsForChoice() // 시간 경계와 선택 대기
    { // 시험 시작
        StageProgression stage = new StageProgression(); // 스테이지 상태 생성
        Assert.That(stage.DefeatBoss(), Is.False); // 잘못된 보스 신호 제외
        Assert.That(stage.CompleteUpgrade(), Is.False); // 잘못된 선택 신호 제외
        stage.Advance(70f, 70f); // 준비와 첫 전투 경계 통과
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.UpgradeOne)); // 첫 선택에서 대기
        Assert.That(stage.PhaseSeconds, Is.EqualTo(5f)); // 남은 시간 선택 구간 반영
        Assert.That(stage.CompleteUpgrade(), Is.True); // 유효한 선택 완료
        Assert.That(stage.CompleteUpgrade(), Is.False); // 선택 중복 방지
    } // 시험 종료
    [Test] // 실패와 종료 시험
    public void FailureStopsProgressAndFurtherTime() // 실패 후 진행 중지
    { // 시험 시작
        StageProgression stage = new StageProgression(); // 스테이지 상태 생성
        stage.Advance(4f, 4f); // 시험 시간 진행
        stage.Fail(); // 출격 실패
        stage.Advance(50f, 50f); // 종료 후 시간 입력
        Assert.That(stage.Phase, Is.EqualTo(StagePhase.Failed)); // 실패 상태 유지
        Assert.That(stage.TotalSeconds, Is.EqualTo(4f)); // 종료 시간 고정
        Assert.That(stage.DefeatBoss(), Is.False); // 종료 후 보스 신호 차단
    } // 시험 종료
} // 시험 종료
