using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 전투 화면 기능
using UnityEditor.SceneManagement; // 실제 씬 검사 기능
using UnityEngine; // 화면 좌표 기능
using UnityEngine.UI; // 실제 UI 비율 기능
public class PanelVisualGeometryTests // 실제 패널의 가독성 배치 시험
{ // 시험 시작
    [TestCase(1080f)] // 전체 HD 높이
    [TestCase(540f)] // 작은 화면 높이
    public void SoundHeadingClearsFrameAndFirstRow(float height) // 음량 제목과 첫 행 여백
    { // 시험 시작
        var previous = EditorSceneManager.GetSceneManagerSetup(); // 기존 씬 구성 보관
        try // 시험 범위
        { // 범위 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single); // 실제 메뉴 씬 열기
            Transform root = null; // 실제 음량 패널 루트
            foreach (GameObject item in scene.GetRootGameObjects()) // 실제 루트 순회
            { // 확인 시작
                if (item.name == "SoundSettingsCanvas") // 실제 음량 화면 확인
                { // 보관 시작
                    root = item.transform; // 실제 화면 보관
                } // 보관 종료
            } // 확인 종료
            Assert.That(root, Is.Not.Null); // 실제 음량 화면 확인
            RectTransform panel = root.Find("Viewport/Panel") as RectTransform; // 실제 패널 배경
            RectTransform heading = panel.Find("Heading") as RectTransform; // 실제 제목 영역
            RectTransform first = panel.Find("Volume0") as RectTransform; // 실제 첫 음량 행
            Assert.That(heading.anchorMax.y, Is.LessThanOrEqualTo(0.90f)); // 상단 장식과 제목 분리
            float panelHeight = height * (panel.anchorMax.y - panel.anchorMin.y); // 실제 비율 기준 패널 높이
            float gap = (heading.anchorMin.y - first.anchorMax.y) * panelHeight; // 제목과 첫 행 여백
            Assert.That(gap, Is.GreaterThanOrEqualTo(height * 0.01f)); // 1080p와 540p의 실제 간격
        } // 범위 종료
        finally // 씬 복원
        { // 복원 시작
            if (previous.Length > 0) // 복원할 씬 확인
            { // 복원 시작
                EditorSceneManager.RestoreSceneManagerSetup(previous); // 기존 씬 복원
            } // 복원 종료
        } // 복원 종료
    } // 시험 종료
    [TestCase(1080f)] // 전체 HD 높이
    [TestCase(540f)] // 작은 화면 높이
    public void BattleHeadingAndSquareSlotsStayInsidePanel(float height) // 전투 제목과 정사각형 슬롯
    { // 시험 시작
        var previous = EditorSceneManager.GetSceneManagerSetup(); // 기존 씬 구성 보관
        try // 시험 범위
        { // 범위 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity", OpenSceneMode.Single); // 실제 전투 씬 열기
            Transform panel = null; // 실제 오른쪽 패널
            foreach (GameObject root in scene.GetRootGameObjects()) // 실제 씬 순회
            { // 확인 시작
                Transform found = root.transform.Find("BattleHudCanvas/BattleViewport/PlayerInfoPanel"); // 실제 직렬화된 패널
                if (found != null) // 패널 확인
                { // 보관 시작
                    panel = found; // 실제 패널 보관
                } // 보관 종료
            } // 확인 종료
            Assert.That(panel, Is.Not.Null); // 실제 패널 확인
            RectTransform heading = panel.Find("Heading") as RectTransform; // 실제 제목 영역
            RectTransform health = panel.Find("HealthGroup") as RectTransform; // 실제 체력 영역
            Assert.That(heading.anchorMax.y, Is.LessThanOrEqualTo(0.90f)); // 상단 장식과 제목 분리
            float headingBottom = (1f - heading.anchorMin.y) * height; // 화면 위쪽 기준 제목 아래
            float healthTop = -health.anchoredPosition.y * height / 1080f; // 실제 체력 영역 위쪽
            Assert.That(healthTop - headingBottom, Is.GreaterThanOrEqualTo(height * 0.01f)); // 제목과 체력 영역 간격
            RectTransform area = panel.Find("SkillArea") as RectTransform; // 실제 세 스킬 영역
            string[] names = new string[] // 실제 스킬 슬롯 이름
            { // 목록 시작
                "UniqueSlot", "CommonSlot", "UltimateSlot" // 세 스킬 표시
            }; // 목록 종료
            foreach (string name in names) // 실제 슬롯 순회
            { // 검사 시작
                RectTransform slot = area.Find(name) as RectTransform; // 직렬화된 실제 슬롯
                AspectRatioFitter ratio = slot.GetComponent<AspectRatioFitter>(); // 실제 비율 제한
                Assert.That(ratio, Is.Not.Null, name); // 정사각형 구성 확인
                Assert.That(ratio.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.WidthControlsHeight), name); // 가로에 맞춘 세로
                Assert.That(ratio.aspectRatio, Is.EqualTo(1f).Within(0.001f), name); // 실제 1대1 비율
                float panelWidth = height * 16f / 9f * 0.25f; // 16대9 내부 오른쪽 너비
                float areaWidth = panelWidth * (area.anchorMax.x - area.anchorMin.x); // 실제 스킬 영역 너비
                float slotWidth = areaWidth * (slot.anchorMax.x - slot.anchorMin.x); // 실제 슬롯 가로
                float areaHeight = height * (area.anchorMax.y - area.anchorMin.y); // 실제 스킬 영역 높이
                Assert.That(slotWidth, Is.LessThanOrEqualTo(areaHeight), name); // 정사각형이 부모 영역 내부에 위치
            } // 검사 종료
        } // 범위 종료
        finally // 씬 복원
        { // 복원 시작
            if (previous.Length > 0) // 복원할 씬 확인
            { // 복원 시작
                EditorSceneManager.RestoreSceneManagerSetup(previous); // 기존 씬 복원
            } // 복원 종료
        } // 복원 종료
    } // 시험 종료
} // 시험 종료
