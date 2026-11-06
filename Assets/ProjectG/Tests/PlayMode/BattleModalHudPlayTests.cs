using System.Collections; // 실제 프레임 시험
using System.Reflection; // 실제 표시 조건 조회
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 전투 구성
using UnityEngine; // 유니티 객체 기능
using UnityEngine.SceneManagement; // 실제 씬 전환
using UnityEngine.TestTools; // 플레이 시험 기능
using UnityEngine.UI; // 실제 게이지 글자
public class BattleModalHudPlayTests // 전투 모달과 정보 표시 시험
{ // 시험 시작
    private GameSessionHost host; // 시험 소유 메뉴 관리자
    private float oldScale; // 기존 시간 배율
    [UnityTest] // 일시정지와 카드 중 게이지 유지
    public IEnumerator PanelKeepsRealGaugesDuringModalAndHidesAfterPlayerRemoval() // 실제 정보 패널 수명
    { // 시험 시작
        oldScale = Time.timeScale; // 기존 시간 배율 보관
        Time.timeScale = 1f; // 정상 전투 시간
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 메뉴 씬 열기
        yield return null; // 실제 메뉴 초기화
        host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 메뉴 관리자
        Assert.That(host, Is.Not.Null); // 실제 관리자 확인
        Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택
        Assert.That(host.Flow.OpenPreparation(), Is.True); // 실제 출격 준비
        Assert.That(host.Flow.StartRun(), Is.True); // 실제 전투 요청
        yield return BattleSceneTestWait.UntilReady(host); // 실제 전투 씬 완료
        BattlePlayer player = host.CurrentPlayer; // 실제 출격 플레이어
        PauseSettingsOverlay overlay = host.GetComponent<PauseSettingsOverlay>(); // 실제 일시정지 모달
        SceneBattleHudPresenter hud = Object.FindFirstObjectByType<SceneBattleHudPresenter>(); // 실제 정보 패널
        Transform panel = hud.transform.Find("BattleViewport/PlayerInfoPanel"); // 실제 오른쪽 패널
        CanvasGroup health = panel.Find("HealthGroup").GetComponent<CanvasGroup>(); // 실제 체력 영역
        CanvasGroup energy = panel.Find("EnergyGroup").GetComponent<CanvasGroup>(); // 실제 에너지 영역
        CanvasGroup experience = panel.Find("ExperienceGroup").GetComponent<CanvasGroup>(); // 실제 경험치 영역
        BattleProgress progress = player.GetComponent<BattleProgress>(); // 실제 카드 성장 상태
        UpgradeDropManager drops = host.CurrentStage.GetComponent<UpgradeDropManager>(); // 실제 필드 아이템 관리자
        Assert.That(drops.TryDrop(EnemyRank.MidBoss, new Vector2(4f, 3f)), Is.True); // 실제 강화 아이템 생성
        PropertyInfo dropLabels = typeof(UpgradeDropManager).GetProperty("CanDisplayHud"); // 실제 아이템 문구 표시 조건
        Assert.That(dropLabels, Is.Not.Null); // 실제 모달 표시 조건 확인
        player.Survival.TryHit(7); // 실제 체력 변경
        player.Skills.GainEnergy(37); // 실제 에너지 변경
        yield return null; // 실제 값 표시 대기
        Assert.That(health.alpha, Is.GreaterThan(0f)); // 실제 체력 게이지 표시
        string healthText = panel.Find("HealthGroup/HealthText").GetComponent<Text>().text; // 실제 체력 문구
        Assert.That(overlay.TryPause(), Is.True); // 실제 일시정지 열기
        yield return null; // 정지 상태 UI 갱신
        PropertyInfo visible = typeof(StageDirector).GetProperty("IsHudVisible"); // 실제 문구 표시 조건
        Assert.That(visible, Is.Not.Null); // 모달 표시 조건 존재
        Assert.That((bool)visible.GetValue(host.CurrentStage), Is.False); // 일시정지 위 문구 차단
        Assert.That(health.alpha, Is.GreaterThan(0f)); // 일시정지 중 체력 유지
        Assert.That(energy.alpha, Is.GreaterThan(0f)); // 일시정지 중 에너지 유지
        Assert.That(experience.alpha, Is.GreaterThan(0f)); // 일시정지 중 경험치 유지
        Assert.That(panel.Find("HealthGroup/HealthText").GetComponent<Text>().text, Is.EqualTo(healthText)); // 기존 실제 체력 문구 유지
        overlay.Resume(); // 실제 전투 재개
        Assert.That((bool)visible.GetValue(host.CurrentStage), Is.True); // 일시정지 종료 후 문구 복구
        progress.CollectExperience(10); // 실제 카드 선택 열기
        Assert.That(progress.Offer, Is.Not.Null); // 실제 카드 모달 확인
        Assert.That((bool)dropLabels.GetValue(drops), Is.False); // 카드 모달 위 아이템 문구 차단
        yield return null; // 카드 선택 UI 갱신
        Assert.That(health.alpha, Is.GreaterThan(0f)); // 카드 선택 중 체력 유지
        Assert.That(energy.alpha, Is.GreaterThan(0f)); // 카드 선택 중 에너지 유지
        Assert.That(experience.alpha, Is.GreaterThan(0f)); // 카드 선택 중 경험치 유지
        Assert.That((bool)visible.GetValue(host.CurrentStage), Is.False); // 카드 모달 위 문구 차단
        int choice = System.Array.FindIndex(progress.Offer, card => card.Id != CardId.C06); // 교체 없는 실제 카드
        Assert.That(progress.Select(choice), Is.True); // 실제 카드 선택 완료
        yield return null; // 실제 모달 닫힘 반영
        Assert.That((bool)visible.GetValue(host.CurrentStage), Is.True); // 카드 종료 후 문구 복구
        Assert.That((bool)dropLabels.GetValue(drops), Is.True); // 카드 종료 후 아이템 문구 복원
        Assert.That(drops.ActiveCount, Is.EqualTo(1)); // 실제 아이템 보존
        Object.Destroy(player.gameObject); // 실제 플레이어 제거
        yield return null; // 객체 제거 프레임
        yield return null; // 실제 HUD 숨김 프레임
        Assert.That(health.alpha, Is.Zero); // 플레이어 소멸 시 체력 숨김
        Assert.That(energy.alpha, Is.Zero); // 플레이어 소멸 시 에너지 숨김
        Assert.That(experience.alpha, Is.Zero); // 플레이어 소멸 시 경험치 숨김
    } // 시험 종료
    [UnityTearDown] // 성공과 실패 후 씬 정리
    public IEnumerator Teardown() // 실제 출격 씬 정리
    { // 정리 시작
        if (host != null) // 실제 관리자 확인
        { // 제거 시작
            Object.Destroy(host.gameObject); // 실제 메뉴와 전투 종료
        } // 제거 종료
        yield return null; // 호스트 파괴 대기
        float deadline = Time.realtimeSinceStartup + 15f; // 실제 시간 제한
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 실제 전투 씬 조회
        while (scene.IsValid() && scene.isLoaded && Time.realtimeSinceStartup < deadline) // 실제 씬 언로드 확인
        { // 대기 시작
            yield return null; // 시간 배율과 무관한 다음 프레임
            scene = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 실제 씬 상태 조회
        } // 대기 종료
        Time.timeScale = oldScale; // 기존 시간 배율 복원
        Assert.That(!scene.IsValid() || !scene.isLoaded, Is.True); // 실제 전투 씬 정리 확인
    } // 정리 종료
} // 시험 종료
