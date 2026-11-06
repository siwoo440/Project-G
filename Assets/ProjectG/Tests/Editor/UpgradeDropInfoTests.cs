using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 화면 기능
using ProjectG; // 강화 아이템 기능
public class UpgradeDropInfoTests // 전투 정보 화면 시험
{ // 시험 시작
    [Test] // 카드와 아이템 합산 표시 시험
    public void SummaryDistinguishesStoredItemsFromActualSharedTotals() // 합산 상한과 누적 구분
    { // 시험 시작
        GameObject playerObject = new GameObject("MixedInfoPlayer"); // 격리 플레이어
        GameObject cameraObject = new GameObject("MixedInfoCamera"); // 격리 카메라
        try // 시험 범위 준비
        { // 시험 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 초기화
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            UpgradeDropManager manager = cameraObject.AddComponent<UpgradeDropManager>(); // 드롭 관리자
            manager.Begin(player, camera, null); // 전투 상태 연결
            for (int index = 0; index < 3; index++) // 피해 아이템 세 번
            { // 반복 시작
                Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.V, player), Is.True); // 아이템 피해 획득
            } // 반복 종료
            Assert.That(player.ApplyCard(CardId.C01), Is.True); // 피해 카드로 공유 상한 도달
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, UpgradeItemGrade.V, player), Is.True); // 첫 연사 아이템
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.FireRate, UpgradeItemGrade.V, player), Is.True); // 둘째 연사 아이템
            Assert.That(player.ApplyCard(CardId.C02), Is.True); // 연사 카드로 공유 상한 도달
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Pierce, UpgradeItemGrade.IV, player), Is.True); // 관통 아이템 획득
            Assert.That(player.ApplyCard(CardId.C03), Is.True); // 관통 카드로 공유 상한 도달
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.UniqueCooldown, UpgradeItemGrade.V, player), Is.True); // 첫 고유기 아이템
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.UniqueCooldown, UpgradeItemGrade.V, player), Is.True); // 둘째 고유기 아이템
            Assert.That(player.ApplyCard(CardId.C05), Is.True); // 고유기 카드로 공유 상한 도달
            string summary = manager.InfoSummary(); // 실제 정보 표시 조회
            Assert.That(summary, Does.Contain("아이템 누적 수치")); // 아이템 원시 합계 구분
            Assert.That(summary, Does.Contain("화력 3회 +54%")); // 아이템 획득량 표시
            Assert.That(summary, Does.Contain("기본 피해 +60%")); // 카드 합산 실제 피해
            Assert.That(summary, Does.Contain("발사 간격 -40%")); // 카드 합산 실제 연사
            Assert.That(summary, Does.Contain("추가 관통 +3")); // 카드 합산 실제 관통
            Assert.That(summary, Does.Contain("J 대기 -40%")); // 카드 합산 실제 대기
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 획득 내역과 남은 보호막 시험
    public void SummaryReflectsActualItemBonusesAndRemainingShield() // 실제 누적 정보 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("InfoPlayer"); // 시험 플레이어 생성
        GameObject cameraObject = new GameObject("InfoCamera"); // 시험 카메라 생성
        try // 시험 범위 시작
        { // 시험 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            UpgradeDropManager manager = cameraObject.AddComponent<UpgradeDropManager>(); // 드롭 관리자 구성
            manager.Begin(player, camera, null); // 시험 전투 연결
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Damage, UpgradeItemGrade.V, player), Is.True); // 화력 획득
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Graze, UpgradeItemGrade.II, player), Is.True); // 그레이즈 획득
            Assert.That(player.Build.Upgrades.TryApply(UpgradeItemId.Shield, UpgradeItemGrade.III, player), Is.True); // 강화 보호막 획득
            Assert.That(player.Survival.TryHit(1), Is.False); // 보호막 한 번 소비
            string summary = manager.InfoSummary(); // 현재 전투 정보
            Assert.That(summary, Does.Contain("화력 1회 +18%")); // 실제 등급별 피해량
            Assert.That(summary, Does.Contain("근접 충전 1회 +15%")); // 실제 등급별 충전량
            Assert.That(summary, Does.Contain("강화 방어막 2회")); // 남은 차단 횟수
            Assert.That(summary, Does.Contain("회복 0회")); // 즉시 아이템 횟수
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 시험 카메라 제거
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(1920f, 1080f)] // 표준 화면 시험
    [TestCase(1280f, 720f)] // 축소 화면 시험
    public void InfoAndFeedbackAreasDoNotOverlap(float width, float height) // 획득 안내와 분리 확인
    { // 시험 시작
        Rect view = new Rect(0f, 0f, width, height); // 시험 전투 영역
        Rect info = UpgradeDropManager.InfoArea(view); // 전투 정보 위치
        Rect feedback = UpgradeDropManager.FeedbackArea(view); // 획득 안내 위치
        Assert.That(info.Overlaps(feedback), Is.False); // 두 표시 영역 분리
    } // 시험 종료
} // 시험 종료
