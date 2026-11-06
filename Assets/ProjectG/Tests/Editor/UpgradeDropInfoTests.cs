using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 화면 기능
using ProjectG; // 강화 아이템 기능
public class UpgradeDropInfoTests // 전투 정보 화면 시험
{ // 시험 시작
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