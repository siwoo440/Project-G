using System.Collections; // 실제 프레임 시험
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 씬 HUD
using UnityEngine; // 유니티 객체 기능
using UnityEngine.SceneManagement; // 직렬화 씬 열기
using UnityEngine.TestTools; // 플레이 모드 시험
public class SceneBattleHudPlayTests // 씬 HUD 생명주기 시험
{ // 시험 시작
    [UnityTest] // 카메라 파괴 후 화면 시험
    public IEnumerator DestroyedMenuCameraDoesNotThrowAndHidesGauges() // 사라진 카메라 보호
    { // 시험 시작
        GameObject hud = null; // 실제 전투 UI 참조
        GameObject title = null; // 실제 제목 UI 참조
        GameObject cameraObject = null; // 실제 메뉴 카메라 참조
        Camera menuCamera = null; // 파괴할 카메라 구성
        try // 시험 범위
        { // 범위 시작
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 저장된 씬 열기
            yield return null; // 실제 메뉴와 UI 초기화
            title = GameObject.Find("TitleCanvas"); // 실제 제목 객체
            GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 메뉴 호스트
            Assert.That(host, Is.Not.Null); // 메뉴 호스트 확인
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(host.Flow.StartRun(), Is.True); // 실제 전투 씬 진입
            yield return BattleSceneTestWait.UntilReady(host); // 실제 완료와 실시간 제한 확인
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle), host.Flow.ErrorText); // 실제 전투 시작 확인
            hud = GameObject.Find("BattleHudCanvas"); // 전투 씬의 실제 HUD 객체
            Assert.That(hud, Is.Not.Null); // HUD 씬 객체 확인
            cameraObject = host.gameObject; // 씬의 메뉴 카메라
            menuCamera = cameraObject.GetComponent<Camera>(); // 실제 메뉴 카메라
            Component cameraData = cameraObject.GetComponent("UniversalAdditionalCameraData"); // 카메라에 의존하는 렌더 설정
            if (cameraData != null) // 렌더 설정 존재 확인
            { // 제거 시작
                Object.Destroy(cameraData); // 카메라 제거 전 의존 구성 정리
                yield return null; // 실제 구성 제거 대기
            } // 제거 종료
            Object.Destroy(menuCamera); // 카메라 구성만 파괴
            yield return null; // 객체 파괴 프레임
            yield return null; // 이후 HUD 갱신 프레임
            Assert.That(hud.GetComponent<SceneBattleHudPresenter>(), Is.Not.Null); // 남은 HUD 객체 확인
            Assert.That(hud.transform.Find("BattleViewport/PlayerInfoPanel/HealthGroup").GetComponent<CanvasGroup>().alpha, Is.Zero); // 체력 게이지 숨김
            Assert.That(hud.transform.Find("BattleViewport/PlayerInfoPanel/EnergyGroup").GetComponent<CanvasGroup>().alpha, Is.Zero); // 에너지 게이지 숨김
            Assert.That(hud.transform.Find("BattleViewport/PlayerInfoPanel/ExperienceGroup").GetComponent<CanvasGroup>().alpha, Is.Zero); // 경험치 게이지 숨김
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (hud != null) // HUD 객체 확인
            { // 제거 시작
                Object.Destroy(hud); // 씬 HUD 정리
            } // 제거 종료
            if (title != null) // 제목 객체 확인
            { // 제거 시작
                Object.Destroy(title); // 씬 제목 정리
            } // 제거 종료
            if (cameraObject != null) // 남은 카메라 확인
            { // 제거 시작
                Object.Destroy(cameraObject); // 카메라 정리
            } // 제거 종료
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
