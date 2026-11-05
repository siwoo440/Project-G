using NUnit.Framework; // 테스트 도구
using System.Collections.Generic; // 카드 목록 검사
using ProjectG; // 전투 기록 기능
public class RunStatisticsTests // 출격 기록 시험
{ // 시험 시작
    [Test] // 출격 기록과 순서 시험
    public void RecordsDefeatsGrazesHitsAndSelectedCardsInOrder() // 전투 지표 확인
    { // 시험 시작
        RunStatistics run = new RunStatistics(); // 새 출격 기록
        run.RecordDefeat(); // 첫 적 처치
        run.RecordDefeat(); // 둘째 적 처치
        run.RecordGraze(); // 첫 스침
        run.RecordHit(); // 실제 체력 피해
        run.RecordCard(CardId.C01); // 첫 카드 선택
        run.RecordCard(CardId.C01); // 같은 카드 강화
        run.RecordCard(CardId.R01); // 전용 카드 선택
        Assert.That(run.Defeats, Is.EqualTo(2)); // 처치 수 확인
        Assert.That(run.Grazes, Is.EqualTo(1)); // 스침 수 확인
        Assert.That(run.Hits, Is.EqualTo(1)); // 실제 피격 수 확인
        Assert.That(run.Cards, Is.EqualTo(new CardId[] // 카드 선택 순서
        { // 목록 시작
            CardId.C01, CardId.C01, CardId.R01 // 중복을 포함한 이력
        })); // 목록 확인 종료
        ICollection<CardId> cards = run.Cards as ICollection<CardId>; // 읽기 전용 카드 목록
        Assert.That(cards.IsReadOnly, Is.True); // 외부 수정 차단 상태
        Assert.Throws<System.NotSupportedException>(() => cards.Add(CardId.C02)); // 외부 직접 추가 차단
    } // 시험 종료
    [Test] // 출격 종료와 중복 방지 시험
    public void CompletionFreezesResultAndIgnoresLaterEvents() // 결과 고정 확인
    { // 시험 시작
        RunStatistics run = new RunStatistics(); // 새 출격 기록
        run.RecordDefeat(); // 적 처치 기록
        Assert.That(run.Complete(true, 180f, 200f), Is.True); // 클리어와 시간 기록
        Assert.That(run.Complete(false, 1f, 1f), Is.False); // 중복 종료 차단
        run.RecordDefeat(); // 종료 후 처치 시도
        run.RecordGraze(); // 종료 후 스침 시도
        run.RecordHit(); // 종료 후 피해 시도
        run.RecordCard(CardId.C02); // 종료 후 카드 시도
        Assert.That(run.Finished, Is.True); // 종료 상태 확인
        Assert.That(run.Cleared, Is.True); // 클리어 상태 유지
        Assert.That(run.CombatSeconds, Is.EqualTo(180f)); // 전투 시간 유지
        Assert.That(run.TotalSeconds, Is.EqualTo(200f)); // 전체 시간 유지
        Assert.That(run.Defeats, Is.EqualTo(1)); // 종료 전 처치 유지
        Assert.That(run.Grazes, Is.Zero); // 종료 후 스침 차단
        Assert.That(run.Hits, Is.Zero); // 종료 후 피해 차단
        Assert.That(run.Cards.Count, Is.Zero); // 종료 후 카드 차단
    } // 시험 종료
    [Test] // 실패와 재출격 시험
    public void ResetClearsFailedRunAndInvalidTimes() // 재출격 초기화 확인
    { // 시험 시작
        RunStatistics run = new RunStatistics(); // 새 출격 기록
        run.RecordHit(); // 실제 피격 기록
        run.RecordCard(CardId.S01); // 카드 기록
        Assert.That(run.Complete(false, float.NaN, -1f), Is.True); // 실패와 잘못된 시간
        Assert.That(run.Cleared, Is.False); // 실패 상태 확인
        Assert.That(run.CombatSeconds, Is.Zero); // 잘못된 전투 시간 방지
        Assert.That(run.TotalSeconds, Is.Zero); // 음수 전체 시간 방지
        run.Reset(); // 새 출격 초기화
        Assert.That(run.Finished, Is.False); // 출격 진행 상태
        Assert.That(run.Hits, Is.Zero); // 피격 이력 초기화
        Assert.That(run.Cards.Count, Is.Zero); // 카드 이력 초기화
        Assert.That(run.Complete(true, 5f, 8f), Is.True); // 새 출격 종료 가능
    } // 시험 종료
} // 시험 종료
