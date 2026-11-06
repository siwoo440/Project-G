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
        try // 시험 범위
        { // 범위 시작
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 저장된 씬 열기
            yield return null; // 실제 메뉴와 UI 초기화
            hud = GameObject.Find("BattleHudCanvas"); // 실제 HUD 객체
            title = GameObject.Find("TitleCanvas"); // 실제 제목 객체
            GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 메뉴 호스트
            Assert.That(hud, Is.Not.Null); // HUD 씬 객체 확인
            Assert.That(host, Is.Not.Null); // 메뉴 호스트 확인
            cameraObject = host.gameObject; // 씬의 메뉴 카메라
            Object.Destroy(cameraObject); // 실제 카메라와 호스트 파괴
            yield return null; // 객체 파괴 프레임
            yield return null; // 이후 HUD 갱신 프레임
            Assert.That(hud.GetComponent<SceneBattleHudPresenter>(), Is.Not.Null); // 남은 HUD 객체 확인
            Assert.That(hud.transform.Find("BattleViewport/HealthGroup").GetComponent<CanvasGroup>().alpha, Is.Zero); // 체력 게이지 숨김
            Assert.That(hud.transform.Find("BattleViewport/EnergyGroup").GetComponent<CanvasGroup>().alpha, Is.Zero); // 에너지 게이지 숨김
            Assert.That(hud.transform.Find("BattleViewport/ExperienceGroup").GetComponent<CanvasGroup>().alpha, Is.Zero); // 경험치 게이지 숨김
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
