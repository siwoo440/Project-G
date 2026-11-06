using System; // 런타임 형식 조회
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 전투 형식
public class TutorialSequenceTests // 첫 출격 안내 순서 시험
{ // 시험 시작
    [Test] // 단계 순서 확인
    public void MovementMustPrecedeFocus() // 이동 전 집중 완료 차단
    { // 시험 범위 시작
        Type sequenceType = typeof(BattlePlayer).Assembly.GetType("ProjectG.TutorialSequence"); // 실제 단계 관리 형식 조회
        Assert.That(sequenceType, Is.Not.Null, "첫 출격 안내 순서 관리가 필요합니다."); // 안내 기능 존재 확인
        Type signalType = typeof(BattlePlayer).Assembly.GetType("ProjectG.TutorialSignal"); // 실제 행동 신호 형식 조회
        Assert.That(signalType, Is.Not.Null); // 행동 신호 확인
        object sequence = Activator.CreateInstance(sequenceType); // 실제 단계 관리 생성
        var observe = sequenceType.GetMethod("Observe"); // 행동 반영 기능 조회
        var current = sequenceType.GetProperty("Current"); // 현재 단계 조회
        Assert.That(observe, Is.Not.Null); // 행동 반영 기능 확인
        Assert.That(current, Is.Not.Null); // 현재 단계 확인
        object[] focusArguments = new object[] // 집중 사격 인수
        { // 인수 시작
            Enum.Parse(signalType, "FocusShot") // 집중 사격 신호
        }; // 인수 종료
        observe.Invoke(sequence, focusArguments); // 순서에 맞지 않는 집중 사격
        Assert.That(current.GetValue(sequence).ToString(), Is.EqualTo("Move")); // 이동 단계 유지
        object[] moveArguments = new object[] // 이동 인수
        { // 인수 시작
            Enum.Parse(signalType, "Move") // 실제 이동 신호
        }; // 인수 종료
        observe.Invoke(sequence, moveArguments); // 실제 이동 반영
        Assert.That(current.GetValue(sequence).ToString(), Is.EqualTo("Focus")); // 다음 집중 단계 진입
    } // 시험 범위 종료
} // 시험 종료
