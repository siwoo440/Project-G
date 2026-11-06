using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 객체
using ProjectG; // 전투 기능
public class BattleSeedIntegrationTests // 출격 난수 재현 시험
{ // 시험 시작
    [Test] // 통합 출격 난수 시험
    public void SameSeedReproducesCardSeedAndDropCount() // 동일 출격 시드 확인
    { // 시험 시작
        GameObject firstCamera = new GameObject("SeedCameraOne"); // 첫 카메라 생성
        GameObject secondCamera = new GameObject("SeedCameraTwo"); // 둘째 카메라 생성
        GameObject firstPlayer = null; // 첫 플레이어 참조
        GameObject secondPlayer = null; // 둘째 플레이어 참조
        try // 시험 시작
        { // 시험 범위 시작
            firstPlayer = BattleSetup.Create(firstCamera.AddComponent<Camera>(), 12345); // 첫 재현 출격
            secondPlayer = BattleSetup.Create(secondCamera.AddComponent<Camera>(), 12345); // 같은 시드 출격
            BattleProgress firstProgress = firstPlayer.GetComponent<BattleProgress>(); // 첫 카드 추첨 상태
            BattleProgress secondProgress = secondPlayer.GetComponent<BattleProgress>(); // 둘째 카드 추첨 상태
            Assert.That(firstProgress.CardSeed, Is.EqualTo(12345)); // 첫 출격 시드 기록
            Assert.That(secondProgress.CardSeed, Is.EqualTo(12345)); // 둘째 출격 시드 기록
            BattlePlayer first = firstPlayer.GetComponent<BattlePlayer>(); // 첫 플레이어
            BattlePlayer second = secondPlayer.GetComponent<BattlePlayer>(); // 둘째 플레이어
            for (int index = 0; index < 100; index++) // 동일 처치 입력 반복
            { // 반복 시작
                first.RegisterEnemyDefeat(EnemyRank.Normal, 0, Vector2.zero); // 첫 드롭 판정
                second.RegisterEnemyDefeat(EnemyRank.Normal, 0, Vector2.zero); // 둘째 드롭 판정
            } // 반복 종료
            UpgradeDropManager firstDrops = firstCamera.GetComponent<UpgradeDropManager>(); // 첫 드롭 관리자
            UpgradeDropManager secondDrops = secondCamera.GetComponent<UpgradeDropManager>(); // 둘째 드롭 관리자
            Assert.That(firstDrops.ActiveCount, Is.GreaterThan(0)); // 유효한 시험 드롭 존재
            Assert.That(secondDrops.ActiveCount, Is.EqualTo(firstDrops.ActiveCount)); // 같은 입력의 드롭 수 재현
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (firstPlayer != null) // 첫 플레이어 확인
            { // 제거 시작
                Object.DestroyImmediate(firstPlayer); // 첫 플레이어 제거
            } // 제거 종료
            if (secondPlayer != null) // 둘째 플레이어 확인
            { // 제거 시작
                Object.DestroyImmediate(secondPlayer); // 둘째 플레이어 제거
            } // 제거 종료
            Object.DestroyImmediate(firstCamera); // 첫 전투 제거
            Object.DestroyImmediate(secondCamera); // 둘째 전투 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료