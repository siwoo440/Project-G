using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 객체
using ProjectG; // 전투 기능
public class DropCombatIntegrationTests // 드롭 전투 연계 시험
{ // 시험 시작
    [Test] // 경험치 회수 시험
    public void MagnetCollectsOnlyActiveShardsInRangeOnce() // 범위와 중복 회수 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("MagnetCamera"); // 시험 카메라 생성
        GameObject playerObject = null; // 플레이어 객체 참조
        float previousScale = Time.timeScale; // 시간 배율 보관
        try // 시험 범위 시작
        { // 시험 시작
            Time.timeScale = 1f; // 전투 시간 복원
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            playerObject = BattleSetup.Create(camera); // 전투 구성 생성
            BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 조회
            BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 조회
            Vector2 origin = player.transform.position; // 플레이어 위치
            player.RegisterEnemyDefeat(EnemyRank.Normal, 9, origin + Vector2.right); // 가까운 조각 생성
            player.RegisterEnemyDefeat(EnemyRank.Normal, 3, origin + Vector2.left); // 두 번째 조각 생성
            player.RegisterEnemyDefeat(EnemyRank.Normal, 7, origin + Vector2.right * 5f); // 먼 조각 생성
            player.RegisterEnemyDefeat(EnemyRank.Normal, 7, origin + Vector2.right * 100f); // 화면 밖 조각 생성
            Assert.That(progress.HasExperienceInRange(2f), Is.True); // 가까운 조각 확인
            Assert.That(progress.CollectExperienceInRange(2f), Is.True); // 가까운 조각 즉시 회수
            Assert.That(progress.Level.Level, Is.EqualTo(2)); // 복수 조각의 레벨업
            Assert.That(progress.Level.Experience, Is.EqualTo(2)); // 회수 경험치 합계
            Assert.That(progress.HasExperienceInRange(2f), Is.False); // 범위 안 조각 소진
            Assert.That(progress.CollectExperienceInRange(2f), Is.False); // 중복 회수 차단
            Assert.That(progress.CollectExperienceInRange(float.PositiveInfinity), Is.True); // 전체 화면 조각 회수
            Assert.That(progress.Level.Experience, Is.EqualTo(9)); // 먼 조각 한 번 지급
            Assert.That(progress.CollectExperienceInRange(float.PositiveInfinity), Is.False); // 전체 중복 회수 차단
            Assert.That(progress.HasExperienceInRange(float.PositiveInfinity), Is.False); // 화면 밖 조각 제외
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 확인
            { // 제거 시작
                Object.DestroyImmediate(playerObject); // 플레이어 제거
            } // 제거 종료
            Object.DestroyImmediate(cameraObject); // 시험 카메라와 자식 제거
            Time.timeScale = previousScale; // 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 카드와 아이템 상한 시험
    public void CardAtSharedDamageCapIsNotEligibleOrApplied() // 아이템 선획득 상한 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("SharedCapPlayer"); // 시험 플레이어 생성
        try // 시험 범위 시작
        { // 시험 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.V, player), Is.True); // 피해 아이템 첫 획득
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.V, player), Is.True); // 피해 아이템 둘째 획득
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.V, player), Is.True); // 피해 아이템 셋째 획득
            Assert.That(player.ApplyCard(CardId.C01), Is.True); // 잔여 상한까지 카드 적용
            Assert.That(CardCatalog.IsEligible(CardId.C01, player.Build.Cards, player.Character, player.Skills.Common, false, true, player.Build), Is.False); // 공유 상한 후보 제외
            Assert.That(player.ApplyCard(CardId.C01), Is.False); // 효과 없는 카드 적용 차단
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 효과 안내 시험
    public void PickupFeedbackExplainsEffectAndActualAmount() // 실제 효과와 남은 양 표시 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("FeedbackPlayer"); // 시험 플레이어 생성
        try // 시험 범위 시작
        { // 시험 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 초기화
            UpgradeDropEffects effects = new UpgradeDropEffects(player, null); // 전투 효과 연결
            Assert.That(effects.TryApply(UpgradeItemId.FireRate, UpgradeItemGrade.V, out string rate), Is.True); // 최고 연사 등급 적용
            Assert.That(rate, Does.Contain("발사 간격 -16%")); // 효과와 감소 방향 확인
            Assert.That(player.Survival.TryHit(5), Is.True); // 체력 다섯 손실
            Assert.That(effects.TryApply(UpgradeItemId.Heal, UpgradeItemGrade.V, out string healing), Is.True); // 최고 회복 등급 적용
            Assert.That(healing, Does.Contain("체력 +5")); // 최대 체력에 맞춘 회복량 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
