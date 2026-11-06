using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 전투 화면 배치
using UnityEditor; // 씬 참조 검사
using UnityEditor.SceneManagement; // 씬 자산 열기
using UnityEngine; // 화면 좌표와 객체
using UnityEngine.UI; // 씬 UI 구성 요소
public class SceneBattleHudSceneTests // 직렬화된 전투 HUD 시험
{ // 시험 시작
    private static Rect ScreenRect(RectTransform item, Rect view) // 실제 앵커의 시험 해상도 좌표
    { // 계산 시작
        float scale = view.height / 1080f; // 기준 화면 배율
        Vector2 size = item.sizeDelta * scale; // 편집한 크기 반영
        Vector2 anchor = item.anchorMin; // 모서리 기준 앵커
        float x = view.xMin + anchor.x * view.width + item.anchoredPosition.x * scale - item.pivot.x * size.x; // 왼쪽 화면 좌표
        float y = view.yMin + (1f - anchor.y) * view.height - item.anchoredPosition.y * scale - (1f - item.pivot.y) * size.y; // 위쪽 화면 좌표
        return new Rect(x, y, size.x, size.y); // 편집 값 기반 영역
    } // 계산 종료
    [TestCase(1920f, 1080f)] // 전체 HD 전투 화면
    [TestCase(960f, 540f)] // 작은 전투 화면
    public void SerializedHudFitsBattleAndAvoidsSkillSlots(float width, float height) // 실제 씬 연결과 위치 확인
    { // 시험 시작
        var previous = EditorSceneManager.GetSceneManagerSetup(); // 기존 편집 장면 보관
        try // 시험 범위
        { // 범위 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single); // 실제 저장된 씬 열기
            GameObject canvasObject = null; // 전투 캔버스 참조
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 순회
            { // 순회 시작
                if (root.name == "BattleHudCanvas") // 직렬화된 전투 UI 확인
                { // 발견 시작
                    canvasObject = root; // 실제 캔버스 보관
                } // 발견 종료
            } // 순회 종료
            Assert.That(canvasObject, Is.Not.Null); // 전투 캔버스 존재
            SceneBattleHudPresenter presenter = canvasObject.GetComponent<SceneBattleHudPresenter>(); // 실제 프레젠터
            Assert.That(presenter, Is.Not.Null); // 프레젠터 존재
            Assert.That(presenter.IsReady, Is.True); // 필수 이미지와 글자 참조 확인
            RectTransform viewport = canvasObject.transform.Find("BattleViewport") as RectTransform; // 편집 가능한 전투 영역
            Assert.That(viewport, Is.Not.Null); // 전투 영역 확인
            AspectRatioFitter fitter = viewport.GetComponent<AspectRatioFitter>(); // 씬에 저장된 비율 제한
            Assert.That(fitter, Is.Not.Null); // 비율 구성 확인
            Assert.That(fitter.aspectRatio, Is.EqualTo(16f / 9f).Within(0.001f)); // 확정 전투 비율
            RectTransform hp = viewport.Find("HealthGroup") as RectTransform; // 체력 씬 객체
            RectTransform energy = viewport.Find("EnergyGroup") as RectTransform; // 에너지 씬 객체
            RectTransform xp = viewport.Find("ExperienceGroup") as RectTransform; // 레벨과 경험치 씬 객체
            Assert.That(hp, Is.Not.Null); // 체력 편집 객체 확인
            Assert.That(energy, Is.Not.Null); // 에너지 편집 객체 확인
            Assert.That(xp, Is.Not.Null); // 경험치 편집 객체 확인
            Rect view = new Rect(0f, 0f, width, height); // 16대9 시험 화면
            Rect hpRect = ScreenRect(hp, view); // 실제 체력 배치
            Rect energyRect = ScreenRect(energy, view); // 실제 에너지 배치
            Rect xpRect = ScreenRect(xp, view); // 실제 경험치 배치
            Assert.That(hpRect.xMin, Is.GreaterThanOrEqualTo(view.xMin)); // 체력 왼쪽 경계
            Assert.That(hpRect.xMax, Is.LessThan(view.center.x)); // 체력 왼쪽 상단
            Assert.That(xpRect.xMin, Is.GreaterThan(view.center.x)); // 경험치 오른쪽 상단
            Assert.That(xpRect.xMax, Is.LessThanOrEqualTo(view.xMax)); // 경험치 오른쪽 경계
            Assert.That(energyRect.xMin, Is.GreaterThan(view.center.x)); // 에너지 오른쪽 하단
            Assert.That(energyRect.yMax, Is.LessThanOrEqualTo(view.yMax)); // 에너지 아래 경계
            Rect stage = new Rect(view.center.x - 350f, view.yMin + 10f, 700f, 32f); // 실제 스테이지 상단 표시
            Assert.That(hpRect.Overlaps(stage), Is.False); // 체력과 진행 정보 분리
            Assert.That(xpRect.Overlaps(stage), Is.False); // 경험치와 진행 정보 분리
            for (int index = 0; index < 3; index++) // 기존 스킬 칸 순회
            { // 순회 시작
                Assert.That(energyRect.Overlaps(SkillHudLayout.Slot(view, index)), Is.False); // 에너지와 스킬 분리
            } // 순회 종료
            Assert.That(xp.Find("LevelText").GetComponent<Text>().font, Is.Not.Null); // 씬에 저장된 글꼴 확인
            Assert.That(xp.Find("ExperienceFill").GetComponent<Image>().type, Is.EqualTo(Image.Type.Filled)); // 편집 가능한 경험치 채움
        } // 범위 종료
        finally // 기존 씬 복원
        { // 복원 시작
            if (previous.Length > 0) // 복원할 씬 확인
            { // 복원 처리 시작
                EditorSceneManager.RestoreSceneManagerSetup(previous); // 기존 편집 씬 복원
            } // 복원 처리 종료
        } // 복원 종료
    } // 시험 종료
} // 시험 종료
