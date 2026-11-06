using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 전투 화면 배치
using UnityEditor; // 실제 아이콘 자산 조회
using UnityEditor.SceneManagement; // 실제 씬 열기
using UnityEngine; // 화면 좌표와 객체
using UnityEngine.UI; // 실제 씬 UI 요소
public class SceneBattleHudSceneTests // 직렬화된 오른쪽 패널 시험
{ // 시험 시작
    [TestCase("rapid")] // 아리아 원본 아이콘
    [TestCase("sniper")] // 리안 원본 아이콘
    [TestCase("shield")] // 보호막 원본 아이콘
    [TestCase("scatter")] // 산탄 원본 아이콘
    [TestCase("ultimate")] // 필살기 원본 아이콘
    public void PanelIconKeepsEntireOriginalTexture(string name) // 원본 전체 아이콘 참조
    { // 시험 시작
        string prefix = "Assets/ProjectG/Resources/SkillIcons/" + name; // 실제 원본 위치
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + ".png"); // 기존 PNG 이미지
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(prefix + "_panel.asset"); // 실제 패널 Sprite
        Assert.That(texture, Is.Not.Null); // 기존 아이콘 확인
        Assert.That(sprite, Is.Not.Null); // 직렬화 Sprite 확인
        Assert.That(sprite.texture, Is.SameAs(texture)); // 기존 PNG 원본 유지
        Assert.That(sprite.rect, Is.EqualTo(new Rect(0f, 0f, texture.width, texture.height))); // 원본 전체 표시
    } // 시험 종료
    private static Rect ScreenRect(RectTransform item, Rect view, float scale) // 부모 패널 기준 실제 배치
    { // 계산 시작
        Vector2 size = item.sizeDelta * scale; // 편집한 크기 반영
        Vector2 anchor = item.anchorMin; // 기준 앵커
        float x = view.xMin + anchor.x * view.width + item.anchoredPosition.x * scale - item.pivot.x * size.x; // 왼쪽 화면 좌표
        float y = view.yMin + (1f - anchor.y) * view.height - item.anchoredPosition.y * scale - (1f - item.pivot.y) * size.y; // 위쪽 화면 좌표
        return new Rect(x, y, size.x, size.y); // 실제 편집값 기반 영역
    } // 계산 종료
    [TestCase(1920f, 1080f)] // 전체 HD 화면
    [TestCase(960f, 540f)] // 작은 화면
    [TestCase(1024f, 768f)] // 4대3 창 화면
    public void SerializedPanelStaysInsideRightQuarter(float width, float height) // 실제 씬 연결과 배치 확인
    { // 시험 시작
        var previous = EditorSceneManager.GetSceneManagerSetup(); // 기존 편집 장면 보관
        try // 시험 범위
        { // 범위 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity", OpenSceneMode.Single); // 실제 전투 씬 열기
            SceneBattleHudPresenter hud = null; // 실제 HUD 구성
            foreach (GameObject root in scene.GetRootGameObjects()) // 실제 씬 루트 순회
            { // 확인 시작
                SceneBattleHudPresenter found = root.GetComponentInChildren<SceneBattleHudPresenter>(true); // 비활성 루트의 실제 HUD
                if (found != null) // 실제 HUD 확인
                { // 보관 시작
                    Assert.That(hud, Is.Null); // 중복 HUD 차단
                    hud = found; // 실제 HUD 보관
                } // 보관 종료
            } // 확인 종료
            Assert.That(hud, Is.Not.Null); // 실제 HUD 존재 확인
            Assert.That(hud.HasSceneReferences && hud.HasPanelReferences, Is.True); // 실제 정보와 스킬 연결 확인
            RectTransform viewport = hud.transform.Find("BattleViewport") as RectTransform; // 전체 16대9 기준 영역
            Assert.That(viewport, Is.Not.Null); // 실제 편집 영역 확인
            Assert.That(viewport.GetComponent<AspectRatioFitter>().aspectRatio, Is.EqualTo(16f / 9f).Within(0.001f)); // 전체 화면 비율
            RectTransform panel = viewport.Find("PlayerInfoPanel") as RectTransform; // 실제 오른쪽 정보 패널
            Assert.That(panel, Is.Not.Null); // 편집 가능한 패널 확인
            Assert.That(panel.anchorMin, Is.EqualTo(new Vector2(0.75f, 0f))); // 오른쪽 25% 시작
            Assert.That(panel.anchorMax, Is.EqualTo(Vector2.one)); // 전체 화면 오른쪽 끝
            Rect normalized = BattleSetup.CalculateViewport(width, height); // 전체 16대9 화면 영역
            Rect view = new Rect(normalized.x * width, (1f - normalized.yMax) * height, normalized.width * width, normalized.height * height); // 실제 화면 좌표
            Rect panelView = new Rect(view.xMin + view.width * 0.75f, view.yMin, view.width * 0.25f, view.height); // 실제 오른쪽 패널 좌표
            float scale = view.height / 1080f; // 1080p 기준 크기 배율
            RectTransform hp = panel.Find("HealthGroup") as RectTransform; // 실제 체력 영역
            RectTransform energy = panel.Find("EnergyGroup") as RectTransform; // 실제 에너지 영역
            RectTransform xp = panel.Find("ExperienceGroup") as RectTransform; // 실제 레벨과 경험치 영역
            Assert.That(hp, Is.Not.Null); // 실제 체력 객체
            Assert.That(energy, Is.Not.Null); // 실제 에너지 객체
            Assert.That(xp, Is.Not.Null); // 실제 경험치 객체
            Rect hpRect = ScreenRect(hp, panelView, scale); // 실제 체력 위치
            Rect energyRect = ScreenRect(energy, panelView, scale); // 실제 에너지 위치
            Rect xpRect = ScreenRect(xp, panelView, scale); // 실제 경험치 위치
            Assert.That(hpRect.xMin, Is.GreaterThanOrEqualTo(panelView.xMin)); // 체력 왼쪽 패널 경계
            Assert.That(hpRect.xMax, Is.LessThanOrEqualTo(panelView.xMax)); // 체력 오른쪽 패널 경계
            Assert.That(energyRect.xMin, Is.GreaterThanOrEqualTo(panelView.xMin)); // 에너지 왼쪽 패널 경계
            Assert.That(xpRect.xMax, Is.LessThanOrEqualTo(panelView.xMax)); // 경험치 오른쪽 패널 경계
            Assert.That(hpRect.Overlaps(energyRect), Is.False); // 체력과 에너지 분리
            Assert.That(energyRect.Overlaps(xpRect), Is.False); // 에너지와 경험치 분리
            RectTransform area = panel.Find("SkillArea") as RectTransform; // 실제 세 스킬 영역
            Assert.That(area, Is.Not.Null); // 편집 가능한 스킬 영역 확인
            Assert.That(area.anchorMin, Is.EqualTo(new Vector2(0.03f, 0.08f))); // 실제 스킬 시작 위치
            Assert.That(area.anchorMax, Is.EqualTo(new Vector2(0.97f, 0.23f))); // 실제 스킬 종료 위치
            string[] slots = new string[] // 세 스킬 슬롯
            { // 목록 시작
                "UniqueSlot", "CommonSlot", "UltimateSlot" // 실제 스킬 이름
            }; // 목록 종료
            foreach (string name in slots) // 실제 슬롯 순회
            { // 확인 시작
                Transform slot = area.Find(name); // 실제 직렬화 슬롯
                Assert.That(slot, Is.Not.Null, name); // 슬롯 존재 확인
                Assert.That(slot.GetComponent<Button>(), Is.Not.Null, name); // 편집 가능한 실제 버튼
                Assert.That(slot.Find("Frame").GetComponent<Image>(), Is.Not.Null, name); // 편집 가능한 사각형
                Assert.That(slot.Find("Unavailable").GetComponent<Image>(), Is.Not.Null, name); // 실제 사용 불가 음영
                Assert.That(slot.Find("Key").GetComponent<Text>(), Is.Not.Null, name); // 재지정 키 글자
            } // 확인 종료
            Assert.That(area.Find("UniqueSlot/Cooldown").GetComponent<Text>().color, Is.EqualTo(Color.white)); // 고유기 흰 쿨타임
            Assert.That(area.Find("CommonSlot/Cooldown").GetComponent<Text>().color, Is.EqualTo(Color.white)); // 공용기 흰 쿨타임
            Assert.That(xp.Find("LevelText").GetComponent<Text>().font, Is.Not.Null); // 사용자 글꼴 보존
            Assert.That(xp.Find("ExperienceFill").GetComponent<Image>().type, Is.EqualTo(Image.Type.Filled)); // 실제 경험치 게이지
        } // 범위 종료
        finally // 기존 씬 복원
        { // 복원 시작
            if (previous.Length > 0) // 복원할 씬 확인
            { // 복원 시작
                EditorSceneManager.RestoreSceneManagerSetup(previous); // 기존 씬 복원
            } // 복원 종료
        } // 복원 종료
    } // 시험 종료
} // 시험 종료
