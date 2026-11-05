using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 보스 전투 기능
public class BossPhaseBoundaryTests // 보스 체력 경계 시험
{ // 시험 시작
    [Test] // 큰 피해 단계 보호 시험
    public void LargeHitsStopAtEachFinalBossPhaseBoundary() // 단계별 피해 제한 확인
    { // 시험 시작
        GameObject holder = new GameObject("Boss"); // 보스 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            EnemyActor boss = holder.AddComponent<EnemyActor>(); // 보스 구성
            boss.Begin(EnemyKind.FinalBoss, null, null, null); // 최종 보스 시작
            int announced = 0; // 마지막 전환 알림
            boss.PhaseChanged += phase => announced = phase; // 전환 알림 연결
            boss.ApplyDamage(5000f, null); // 첫 대량 피해
            Assert.That(boss.Health, Is.EqualTo(1000f).Within(0.001f)); // 첫 경계 보호
            Assert.That(boss.PatternIndex, Is.EqualTo(1)); // 둘째 단계 전환
            Assert.That(announced, Is.EqualTo(1)); // 둘째 단계 알림
            boss.ApplyDamage(5000f, null); // 둘째 대량 피해
            Assert.That(boss.Health, Is.EqualTo(500f).Within(0.001f)); // 둘째 경계 보호
            Assert.That(boss.PatternIndex, Is.EqualTo(2)); // 셋째 단계 전환
            Assert.That(announced, Is.EqualTo(2)); // 셋째 단계 알림
            boss.ApplyDamage(5000f, null); // 마지막 대량 피해
            Assert.That(boss.IsAlive, Is.False); // 마지막 단계 처치
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 보스 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 경계 없는 보스 시험
    public void MiddleBossStillReceivesFullDamage() // 중간 보스 피해 유지
    { // 시험 시작
        GameObject holder = new GameObject("MidBoss"); // 중간 보스 객체
        try // 시험 시작
        { // 시험 범위 시작
            EnemyActor boss = holder.AddComponent<EnemyActor>(); // 보스 구성
            boss.Begin(EnemyKind.MidBoss, null, null, null); // 중간 보스 시작
            boss.ApplyDamage(1000f, null); // 대량 피해 적용
            Assert.That(boss.IsAlive, Is.False); // 체력 전체 소모 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 보스 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
