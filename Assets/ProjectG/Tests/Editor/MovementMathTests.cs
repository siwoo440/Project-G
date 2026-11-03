using NUnit.Framework; // 테스트 도구
using UnityEngine; // 벡터 자료형
using ProjectG; // 이동 계산기
public class MovementMathTests // 이동 규칙 시험
{ // 시험 시작
    [Test] // 일반 이동 시험
    public void NormalMovementCrossesAvailableWidthInFourSeconds() // 일반 횡단 시간
    { // 시험 본문 시작
        Vector2 result = MovementMath.Move(new Vector2(-8f, 0f), Vector2.right, -8f, 8f, -5f, 5f, false, 4f); // 일반 이동 결과
        Assert.That(result.x, Is.EqualTo(8f).Within(0.0001f)); // 오른쪽 경계 확인
    } // 시험 본문 끝
    [Test] // 집중 이동 시험
    public void FocusMovementCrossesAvailableWidthInEightSeconds() // 집중 횡단 시간
    { // 시험 본문 시작
        Vector2 result = MovementMath.Move(new Vector2(-8f, 0f), Vector2.right, -8f, 8f, -5f, 5f, true, 8f); // 집중 이동 결과
        Assert.That(result.x, Is.EqualTo(8f).Within(0.0001f)); // 오른쪽 경계 확인
    } // 시험 본문 끝
    [Test] // 집중 비율 시험
    public void FocusMovementUsesHalfSpeed() // 집중 이동 비율
    { // 시험 본문 시작
        Vector2 result = MovementMath.Move(Vector2.zero, Vector2.right, -8f, 8f, -5f, 5f, true, 1f); // 집중 일초 이동
        Assert.That(result.x, Is.EqualTo(2f).Within(0.0001f)); // 절반 속도 확인
    } // 시험 본문 끝
    [Test] // 대각선 속도 시험
    public void DiagonalMovementHasSameSpeed() // 대각선 속도 확인
    { // 시험 본문 시작
        Vector2 result = MovementMath.Move(Vector2.zero, Vector2.one, -8f, 8f, -5f, 5f, false, 1f); // 대각선 일초 이동
        Assert.That(result.magnitude, Is.EqualTo(4f).Within(0.0001f)); // 일반 속도 확인
    } // 시험 본문 끝
    [Test] // 화면 경계 시험
    public void MovementStopsAtBounds() // 경계 이탈 방지
    { // 시험 본문 시작
        Vector2 result = MovementMath.Move(new Vector2(7f, 4f), Vector2.one, -8f, 8f, -5f, 5f, false, 10f); // 경계 초과 이동
        Assert.That(result.x, Is.EqualTo(8f).Within(0.0001f)); // 오른쪽 경계 확인
        Assert.That(result.y, Is.EqualTo(5f).Within(0.0001f)); // 위쪽 경계 확인
    } // 시험 본문 끝
} // 시험 끝
