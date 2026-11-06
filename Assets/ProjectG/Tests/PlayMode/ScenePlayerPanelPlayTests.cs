using System.Collections; // 실제 프레임 시험
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 전투 기능
using UnityEngine; // 유니티 객체 기능
using UnityEngine.SceneManagement; // 실제 씬 조회
using UnityEngine.TestTools; // 플레이 시험 기능
using UnityEngine.UI; // 실제 씬 UI 기능
public class ScenePlayerPanelPlayTests // 실제 오른쪽 정보 패널 시험
{ // 시험 시작
    private GameSessionHost ownedHost; // 시험이 소유한 실제 메뉴
    [UnityTearDown] // 성공과 실패 후 실제 씬 정리
    public IEnumerator ReleaseScene() // 소유한 출격과 씬 정리
    { // 정리 시작
        if (ownedHost == null) // 실제 호스트 확인
        { // 생략 시작
            yield break; // 다른 씬 객체 보존
        } // 생략 종료
        Object.Destroy(ownedHost.gameObject); // 실제 메뉴와 전투 종료
        ownedHost = null; // 시험 소유 참조 해제
        yield return null; // 호스트 파괴 프레임 대기
        float deadline = Time.realtimeSinceStartup + 15f; // 실제 시간 제한
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 실제 전투 씬 조회
        while (scene.IsValid() && scene.isLoaded && Time.realtimeSinceStartup < deadline) // 씬 언로드 확인
        { // 대기 시작
            yield return null; // 시간 배율과 무관한 다음 프레임
            scene = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 실제 씬 상태 재조회
        } // 대기 종료
        Assert.That(!scene.IsValid() || !scene.isLoaded, Is.True); // 실제 출격 씬 정리 확인
    } // 정리 종료
    [UnityTest] // 실제 재지정 키와 쿨타임 시험
    public IEnumerator ScenePanelShowsRealSkillAndPreservesEditedAppearance() // 실제 전투 UI 상태 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 메뉴 씬 열기
        yield return null; // 실제 메뉴 초기화
        ownedHost = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 출격 관리자
        Assert.That(ownedHost, Is.Not.Null); // 실제 메뉴 확인
        Assert.That(ownedHost.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택
        Assert.That(ownedHost.Flow.OpenPreparation(), Is.True); // 출격 준비
        Assert.That(ownedHost.Flow.StartRun(), Is.True); // 실제 전투 요청
        yield return BattleSceneTestWait.UntilReady(ownedHost); // 실제 씬 완료 대기
        BattlePlayer player = ownedHost.CurrentPlayer; // 실제 씬 플레이어
        SceneBattleHudPresenter hud = Object.FindFirstObjectByType<SceneBattleHudPresenter>(); // 실제 씬 정보 패널
        Assert.That(hud, Is.Not.Null); // 실제 패널 확인
        Transform panel = hud.transform.Find("BattleViewport/PlayerInfoPanel"); // 실제 오른쪽 패널
        Assert.That(panel, Is.Not.Null); // 패널 씬 객체 확인
        Transform area = panel.Find("SkillArea"); // 실제 스킬 표시 영역
        Canvas.ForceUpdateCanvases(); // 실제 스킬 비율 계산 완료
        string[] slotNames = new string[] // 실제 스킬 슬롯 이름
        { // 목록 시작
            "UniqueSlot", "CommonSlot", "UltimateSlot" // 세 실제 스킬 슬롯
        }; // 목록 종료
        foreach (string name in slotNames) // 실제 슬롯 순회
        { // 비율 확인 시작
            Rect rect = ((RectTransform)area.Find(name)).rect; // 실제 배치된 슬롯 크기
            Assert.That(rect.width, Is.GreaterThan(0f), name); // 실제 표시 크기 확인
            Assert.That(rect.height, Is.EqualTo(rect.width).Within(0.1f), name); // 실제 1대1 표시 확인
        } // 비율 확인 종료
        Image icon = area.Find("UniqueSlot/RapidIcon").GetComponent<Image>(); // 편집 가능한 아리아 아이콘
        Color edited = new Color(0.2f, 0.4f, 0.8f, 0.9f); // 사용자 색상
        Vector2 position = new Vector2(9f, 13f); // 사용자 위치
        Vector2 size = new Vector2(-7f, -5f); // 사용자 크기
        Sprite sprite = icon.sprite; // 사용자 이미지
        icon.color = edited; // 실제 색상 편집
        icon.rectTransform.anchoredPosition = position; // 실제 위치 편집
        icon.rectTransform.sizeDelta = size; // 실제 크기 편집
        KeyBindingSettings keys = new KeyBindingSettings(); // 격리 재지정 키
        Assert.That(keys.TryBind(PlayerAction.Unique, "u"), Is.True); // 실제 고유기 키 변경
        player.SetBindings(keys); // 실제 전투 키 연결
        yield return null; // 실제 UI 갱신 프레임
        Assert.That(area.Find("UniqueSlot/Key").GetComponent<Text>().text, Is.EqualTo("U")); // 재지정 키 표시
        Assert.That(area.Find("UltimateSlot/Unavailable").gameObject.activeSelf, Is.True); // 에너지 부족 음영
        Assert.That(player.TryUseRapid(), Is.True); // 실제 고유기 성공
        Assert.That(player.TryUseCommon(), Is.True); // 실제 공용기 성공
        player.Skills.Advance(3f); // 활성 종료와 실제 쿨타임 시작
        yield return null; // 실제 쿨타임 표시 대기
        Assert.That(area.Find("UniqueSlot/Cooldown").GetComponent<Text>().text, Is.Not.Empty); // 흰 쿨타임 숫자 표시
        Assert.That(area.Find("UniqueSlot/Cooldown").GetComponent<Text>().color, Is.EqualTo(Color.white)); // 기본 흰 숫자 색상
        Assert.That(area.Find("UniqueSlot/Unavailable").gameObject.activeSelf, Is.True); // 실제 고유기 쿨타임 음영
        Assert.That(area.Find("CommonSlot/Unavailable").gameObject.activeSelf, Is.True); // 실제 공용기 쿨타임 음영
        player.Skills.GainEnergy(100); // 실제 필살기 에너지 충전
        yield return null; // 실제 충전 표시 대기
        Assert.That(area.Find("UltimateSlot/Unavailable").gameObject.activeSelf, Is.False); // 충전 완료 음영 해제
        Assert.That(icon.color, Is.EqualTo(edited)); // 사용자 색상 보존
        Assert.That(icon.sprite, Is.SameAs(sprite)); // 사용자 이미지 보존
        Assert.That(icon.rectTransform.anchoredPosition, Is.EqualTo(position)); // 사용자 위치 보존
        Assert.That(icon.rectTransform.sizeDelta, Is.EqualTo(size)); // 사용자 크기 보존
    } // 시험 종료
} // 시험 종료
