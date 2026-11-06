using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 전투 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 스테이지 기능
public class StagePlayModeTests // 실제 스테이지 종료 시험
{ // 시험 시작
    [UnityTest] // 스테이지 제거 시험
    public IEnumerator DestroyingStageReleasesTransitionSafety() // 런타임 안전 상태 정리 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 카메라 객체
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
        GameObject playerObject = BattleSetup.Create(camera); // 실제 스테이지 구성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 조회
        BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 조회
        StageDirector stage = cameraObject.GetComponent<StageDirector>(); // 스테이지 조회
        try // 시험 시작
        { // 시험 범위 시작
            stage.Advance(65f, 65f); // 중간 보스 전환 시작
            Assert.That(cameraObject.GetComponent<UpgradeItemSelection>(), Is.Null); // 고정 강화 선택 제거 확인
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.MidBoss)); // 중간 보스 전환 확인
            Assert.That(stage.Boss, Is.Null); // 안전 시간 중 보스 대기
            Assert.That(player.Survival.IsInvulnerable, Is.True); // 보스 전환 무적 확인
            Assert.That(progress.Blocked, Is.True); // 보스 전환 카드 선택 지연
            stage.Advance(1f, 1f); // 안전 시간 중간 진행
            Assert.That(stage.Boss, Is.Null); // 보스 등장 전 대기 유지
            Object.Destroy(stage); // 스테이지 제어 제거
            yield return null; // 제거 처리 대기
            Assert.That(player.Survival.IsInvulnerable, Is.False); // 안전 상태 해제
            Assert.That(progress.Blocked, Is.False); // 카드 선택 지연 해제
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
