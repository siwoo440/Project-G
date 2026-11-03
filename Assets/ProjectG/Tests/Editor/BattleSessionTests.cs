using NUnit.Framework; // 테스트 도구
using UnityEngine; // 벡터 자료형
using ProjectG; // 전투 상태 기능
public class BattleSessionTests // 전투 흐름 시험
{ // 시험 시작
    [Test] // 이동 및 자동 공격 시험
    public void MovesAndFiresWithoutAttackInput() // 자동 공격 확인
    { // 시험 시작
        BattleSession battle = new BattleSession(new Vector2(-8f, 0f)); // 전투 상태 생성
        int shots = battle.Advance(Vector2.right, false, false, false, 1f, -8f, 8f, -5f, 5f); // 오른쪽 이동
        Assert.That(battle.Position.x, Is.EqualTo(-4f).Within(0.0001f)); // 이동 거리 확인
        Assert.That(shots, Is.EqualTo(2)); // 일반 탄환 두 발 확인
    } // 시험 종료
    [Test] // 집중 공격 시험
    public void FocusMovesAtHalfSpeedAndChangesShot() // 집중 상태 확인
    { // 시험 시작
        BattleSession battle = new BattleSession(Vector2.zero); // 전투 상태 생성
        int shots = battle.Advance(Vector2.right, true, true, false, 1f, -8f, 8f, -5f, 5f); // 집중 이동
        Assert.That(battle.Position.x, Is.EqualTo(2f).Within(0.0001f)); // 집중 거리 확인
        Assert.That(battle.Focused, Is.True); // 집중 상태 확인
        Assert.That(shots, Is.EqualTo(1)); // 집중 탄환 한 발 확인
    } // 시험 종료
} // 시험 종료
