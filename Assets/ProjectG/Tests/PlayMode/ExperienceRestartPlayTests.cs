using System.Collections; // 코루틴 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 플레이 시험 도구
public class ExperienceRestartPlayTests // 재출격 경험치 시험
{ // 시험 시작
    [UnityTest] // 같은 프레임 재출격과 회수 시험
    public IEnumerator RestartCannotCollectPreviousRunShardBeforeDeferredDestroy() // 제거 지연 중 이전 경험치 차단
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("Camera"); // 전투 카메라 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 적용
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 첫 출격 시작
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 경험치 연결
            player.RegisterEnemyDefeat(EnemyRank.Normal, 3, Vector2.zero); // 플레이어 위치에 조각 생성
            ExperienceShard previous = cameraObject.GetComponentInChildren<ExperienceShard>(); // 이전 출격 조각
            Assert.That(previous, Is.Not.Null); // 조각 생성 확인
            player.Begin(Vector2.zero); // 같은 프레임 새 출격
            if (previous != null && previous.gameObject.activeInHierarchy) // 아직 활성화된 제거 대상 확인
            { // 이전 조각 처리 시작
                previous.Advance(0.01f); // 제거 전 조각 회수 시도
            } // 이전 조각 처리 종료
            Assert.That(progress.Level.Experience, Is.Zero); // 새 출격의 이전 경험치 유입 방지
            yield return null; // 지연 제거 완료 대기
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = previousScale; // 기존 시간 복원
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
