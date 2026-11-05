using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 전투 기록 기능
public class RunStatisticsIntegrationTests // 전투 기록 통합 시험
{ // 시험 시작
    [Test] // 실제 전투 기록 시험
    public void RecordsOnlyAppliedDamageAndSelectedCards() // 실제 처치와 피격 기록 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            player.RegisterGraze(); // 그레이즈 기록
            player.RegisterEnemyDefeat(EnemyRank.Normal); // 적 처치 기록
            Assert.That(player.ApplyCard(CardId.C01), Is.True); // 카드 선택
            player.Survival.ActivateShield(3f); // 보호막 준비
            Assert.That(player.Survival.TryHit(10), Is.False); // 보호막 방어
            Assert.That(player.Survival.TryHit(10), Is.True); // 실제 체력 피해
            Assert.That(player.Survival.TryHit(10), Is.False); // 무적 중 피해 차단
            Assert.That(player.Statistics.Defeats, Is.EqualTo(1)); // 적 처치 횟수
            Assert.That(player.Statistics.Grazes, Is.EqualTo(1)); // 그레이즈 횟수
            Assert.That(player.Statistics.Hits, Is.EqualTo(1)); // 실제 피격 횟수
            Assert.That(player.Statistics.Cards, Does.Contain(CardId.C01)); // 선택 카드 기록
            player.Begin(Vector2.zero); // 새 출격 시작
            Assert.That(player.Statistics.Defeats, Is.Zero); // 출격 기록 초기화
            Assert.That(player.Statistics.Cards.Count, Is.Zero); // 선택 카드 초기화
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
