using System.Collections; // 실제 프레임 시험
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 출격 흐름
using UnityEngine; // 유니티 객체
using UnityEngine.SceneManagement; // 실제 씬 확인
using UnityEngine.TestTools; // 플레이 모드 시험
public class BattleScenePlayTests // 메뉴와 전투 씬 통합 시험
{ // 시험 시작
    [UnityTest] // 실제 출격 씬 시험
    public IEnumerator ActualRunUsesTheSerializedBattleScene() // 실제 출격 연결 확인
    { // 시험 시작
        GameSessionHost host = null; // 실제 메뉴 관리자
        float previousScale = Time.timeScale; // 기존 전투 시간 배율
        try // 시험 범위
        { // 범위 시작
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기존 메뉴 씬 열기
            yield return null; // 실제 메뉴 초기화 대기
            host = Object.FindFirstObjectByType<GameSessionHost>(); // 메뉴의 실제 관리자
            Assert.That(host, Is.Not.Null); // 메뉴 관리자 확인
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비
            bool started = host.Flow.StartRun(); // 실제 출격 요청
            Assert.That(started, Is.True, host.Flow.ErrorText); // 실패 원인을 포함한 출격 확인
            yield return BattleSceneTestWait.UntilReady(host); // 실제 씬 완료와 실시간 제한 확인
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle), host.Flow.ErrorText); // 실제 전투 준비 완료
            Scene battle = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 실제 전투 씬 조회
            Assert.That(battle.IsValid() && battle.isLoaded, Is.True); // 전투 씬 실제 로드
            Assert.That(host.CurrentPlayer, Is.Not.Null); // 실제 플레이어 연결
            Assert.That(host.CurrentPlayer.gameObject.scene, Is.EqualTo(battle)); // 씬에 저장된 실제 플레이어 사용
            Assert.That(host.CurrentStage.gameObject.scene, Is.EqualTo(battle)); // 씬에 저장된 실제 스테이지 사용
            BattleSceneBindings bindings = null; // 씬에서 로드된 연결
            foreach (GameObject root in battle.GetRootGameObjects()) // 실제 전투 루트 순회
            { // 순회 시작
                BattleSceneBindings candidate = root.GetComponent<BattleSceneBindings>(); // 저장된 연결 확인
                if (candidate != null) // 실제 연결 확인
                { // 연결 시작
                    bindings = candidate; // 실제 루트 보관
                } // 연결 종료
            } // 순회 종료
            Assert.That(bindings, Is.Not.Null); // 실제 전투 루트 확인
            Assert.That(host.CurrentPlayer, Is.SameAs(bindings.Player)); // 모형이 아닌 씬 플레이어 사용
            Assert.That(host.CurrentStage, Is.SameAs(bindings.Stage)); // 모형이 아닌 씬 스테이지 사용
            Assert.That(bindings.Hud.gameObject.scene, Is.EqualTo(battle)); // 실제 HUD의 전투 씬 소속
            Assert.That(host.Flow.StartRun(), Is.False); // 전투 중 중복 출격 차단
            GameObject oldRoot = bindings.gameObject; // 이전 씬의 실제 루트
            BattlePlayer oldPlayer = host.CurrentPlayer; // 이전 출격 플레이어 참조
            Time.timeScale = 0f; // 정지 상태의 씬 전환 시험
            Assert.That(host.Flow.RetryFromPause(), Is.True); // 저장 후 실제 재출격
            Assert.That(oldRoot.activeSelf, Is.False); // 이전 루트 즉시 비활성화
            yield return BattleSceneTestWait.UntilReady(host); // 정지 중 새 씬 완료와 실시간 제한 확인
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle), host.Flow.ErrorText); // 정지 중 새 씬 준비 완료
            Assert.That(host.CurrentPlayer, Is.Not.SameAs(oldPlayer)); // 새 씬의 별도 플레이어
            Assert.That(host.CurrentPlayer.gameObject.scene.handle, Is.Not.EqualTo(battle.handle)); // 새 전투 씬 인스턴스
            Assert.That(battle.isLoaded, Is.False); // 이전 전투 씬 언로드
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = previousScale; // 기존 시간 배율 복원
            if (host != null) // 실제 관리자 확인
            { // 제거 시작
                Object.Destroy(host.gameObject); // 메뉴와 현재 전투 정리
            } // 제거 종료
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
