using System.Reflection; // 직렬화된 필드 시험 연결
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 전투 화면
using UnityEngine; // 화면 객체와 좌표
using UnityEngine.UI; // 씬 UI 구성
using UnityEngine.TestTools; // 오류 로그 검증
public class SceneBattleHudPresenterTests // 직렬화 HUD 동작 시험
{ // 시험 시작
    private static void Assign(SceneBattleHudPresenter view, string field, object value) // 인스펙터 필드 연결
    { // 연결 시작
        FieldInfo info = typeof(SceneBattleHudPresenter).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance); // 실제 직렬화 필드 조회
        Assert.That(info, Is.Not.Null, field); // 실제 필드 확인
        info.SetValue(view, value); // 시험 참조 연결
    } // 연결 종료
    private static Image Fill(Transform parent, Sprite sprite) // 편집 가능한 가로 게이지
    { // 생성 시작
        GameObject holder = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 실제 UI 객체
        holder.transform.SetParent(parent, false); // 화면 계층 연결
        Image image = holder.GetComponent<Image>(); // 편집 가능한 이미지
        image.sprite = sprite; // 실제 채움 스프라이트
        image.type = Image.Type.Filled; // 가로 채움 방식
        image.fillMethod = Image.FillMethod.Horizontal; // 수평 채움
        image.fillOrigin = 0; // 왼쪽 기준
        return image; // 구성한 채움 반환
    } // 생성 종료
    private static Text Label(Transform parent) // 편집 가능한 실제 숫자
    { // 생성 시작
        GameObject holder = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 실제 UI 객체
        holder.transform.SetParent(parent, false); // 화면 계층 연결
        return holder.GetComponent<Text>(); // 편집 가능한 글자
    } // 생성 종료
    [TestCase(-10f, 100f, 0f)] // 음수 하한
    [TestCase(50f, 100f, 0.5f)] // 중간 비율
    [TestCase(200f, 100f, 1f)] // 초과 상한
    [TestCase(1f, 0f, 0f)] // 최대값 0 방어
    public void FractionLimitsOnlyVisualFill(float current, float maximum, float expected) // 표시 비율 경계
    { // 시험 시작
        Assert.That(SceneBattleHudPresenter.Fraction(current, maximum), Is.EqualTo(expected).Within(0.0001f)); // 실제 계산 결과
        Assert.That(SceneBattleHudPresenter.Fraction(float.NaN, maximum), Is.Zero); // 잘못된 숫자 방어
    } // 시험 종료
    [Test] // 필수 참조 오류 시험
    public void MissingSceneReferencesReportAnErrorAndDisablePresenter() // 누락 시 명확한 오류
    { // 시험 시작
        GameObject root = new GameObject("IncompleteHud"); // 참조 없는 씬 객체
        try // 시험 범위
        { // 범위 시작
            root.SetActive(false); // 연결 전 자동 실행 방지
            SceneBattleHudPresenter view = root.AddComponent<SceneBattleHudPresenter>(); // 미연결 프레젠터
            Assert.That(view.IsReady, Is.False); // 연결 누락 확인
            LogAssert.Expect(LogType.Error, "전투 HUD의 카메라, 게이지 이미지, 숫자 또는 가로 채움 설정이 누락되었습니다."); // 예상 오류 등록
            typeof(SceneBattleHudPresenter).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null); // 실제 오류 처리 호출
            Assert.That(view.enabled, Is.False); // 불완전한 표시 차단
        } // 범위 종료
        finally // 객체 정리
        { // 정리 시작
            Object.DestroyImmediate(root); // 시험 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 실제 전투 상태와 편집 보존 시험
    public void CurrentValuesAndLevelUpUpdateWithoutChangingEditedAppearance() // 숫자와 크기 및 색상 확인
    { // 시험 시작
        GameObject root = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup)); // 격리 캔버스
        GameObject playerObject = new GameObject("HudPlayer"); // 격리 플레이어
        GameObject cameraObject = new GameObject("HudCamera"); // 격리 카메라
        Texture2D texture = new Texture2D(2, 2); // 격리 이미지 원본
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f)); // 실제 스프라이트
        try // 시험 범위
        { // 범위 시작
            root.SetActive(false); // 직렬화 참조 연결 전 비활성화
            Canvas canvas = root.GetComponent<Canvas>(); // 실제 캔버스
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 기준 UI
            CanvasGroup group = root.GetComponent<CanvasGroup>(); // 체력 투명도 그룹
            group.alpha = 0.8f; // 사용자가 정한 기본 투명도
            GameObject energyObject = new GameObject("EnergyGroup", typeof(RectTransform), typeof(CanvasGroup)); // 에너지 독립 영역
            energyObject.transform.SetParent(root.transform, false); // 화면 계층 연결
            CanvasGroup energyGroup = energyObject.GetComponent<CanvasGroup>(); // 에너지 투명도 그룹
            GameObject experienceObject = new GameObject("ExperienceGroup", typeof(RectTransform), typeof(CanvasGroup)); // 경험치 독립 영역
            experienceObject.transform.SetParent(root.transform, false); // 화면 계층 연결
            CanvasGroup experienceGroup = experienceObject.GetComponent<CanvasGroup>(); // 경험치 투명도 그룹
            Image hp = Fill(root.transform, sprite); // 체력 이미지
            Image energy = Fill(root.transform, sprite); // 에너지 이미지
            Image xp = Fill(root.transform, sprite); // 경험치 이미지
            Text hpText = Label(root.transform); // 체력 숫자
            Text energyText = Label(root.transform); // 에너지 숫자
            Text levelText = Label(root.transform); // 레벨 숫자
            Text xpText = Label(root.transform); // 경험치 숫자
            hp.color = new Color(0.2f, 0.4f, 0.7f, 0.9f); // 사용자가 정한 게이지 색상
            RectTransform hpRect = hp.rectTransform; // 사용자가 조정하는 위치
            hpRect.anchoredPosition = new Vector2(19f, -27f); // 사용자가 정한 위치
            hpRect.sizeDelta = new Vector2(155f, 23f); // 사용자가 정한 크기
            Camera camera = cameraObject.AddComponent<Camera>(); // 격리 카메라
            camera.orthographic = true; // 실제 전투와 같은 직교 투영
            camera.transform.position = new Vector3(0f, 0f, -10f); // 표시 대상 앞 카메라
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.Begin(Vector2.zero); // 실제 전투 상태 초기화
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 실제 전투 성장
            progress.Begin(player, camera); // 경험치 상태 초기화
            SceneBattleHudPresenter view = root.AddComponent<SceneBattleHudPresenter>(); // 실제 씬 프레젠터
            Assign(view, "menuCamera", camera); // 메뉴 카메라 참조
            Assign(view, "healthGroup", group); // 체력 투명도 그룹 참조
            Assign(view, "energyGroup", energyGroup); // 에너지 투명도 그룹 참조
            Assign(view, "experienceGroup", experienceGroup); // 경험치 투명도 그룹 참조
            Assign(view, "healthFill", hp); // 체력 채움 참조
            Assign(view, "energyFill", energy); // 에너지 채움 참조
            Assign(view, "experienceFill", xp); // 경험치 채움 참조
            Assign(view, "healthText", hpText); // 체력 숫자 참조
            Assign(view, "energyText", energyText); // 에너지 숫자 참조
            Assign(view, "levelText", levelText); // 레벨 숫자 참조
            Assign(view, "experienceText", xpText); // 경험치 숫자 참조
            Assert.That(view.IsReady, Is.True); // 완전한 씬 연결 확인
            typeof(SceneBattleHudPresenter).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null); // 씬 초기화 호출
            player.Survival.TryHit(25); // 실제 체력 감소
            player.Skills.GainEnergy(37); // 실제 에너지 증가
            progress.Level.Gain(15); // 실제 레벨업과 남은 경험치
            view.Present(player, camera, progress); // 현재 실제 값 표시
            Assert.That(hp.fillAmount, Is.EqualTo(0.75f).Within(0.001f)); // 체력 비율
            Assert.That(energy.fillAmount, Is.EqualTo(0.37f).Within(0.001f)); // 에너지 비율
            Assert.That(xp.fillAmount, Is.EqualTo(5f / 16f).Within(0.001f)); // 레벨업 후 경험치 비율
            Assert.That(hpText.text, Is.EqualTo("HP 75 / 100")); // 실제 체력 숫자
            Assert.That(energyText.text, Is.EqualTo("ENERGY 37 / 100")); // 실제 에너지 숫자
            Assert.That(levelText.text, Is.EqualTo("LV. 2")); // 변경된 레벨
            Assert.That(xpText.text, Is.EqualTo("XP 5 / 16")); // 경험치 이름과 남은 숫자
            Assert.That(hp.color, Is.EqualTo(new Color(0.2f, 0.4f, 0.7f, 0.9f))); // 편집 색상 보존
            Assert.That(hpRect.anchoredPosition, Is.EqualTo(new Vector2(19f, -27f))); // 편집 위치 보존
            Assert.That(hpRect.sizeDelta, Is.EqualTo(new Vector2(155f, 23f))); // 편집 크기 보존
            Assert.That(group.alpha, Is.EqualTo(0.8f).Within(0.001f)); // 편집 기본 투명도 보존
            root.SetActive(true); // 실제 캔버스 배치 활성화
            Canvas.ForceUpdateCanvases(); // 화면 영역 확정
            SpriteRenderer playerImage = playerObject.AddComponent<SpriteRenderer>(); // 겹침 대상 이미지
            playerImage.sprite = sprite; // 실제 플레이어 표시 영역
            view.Present(player, camera, progress); // 플레이어 겹침 반영
            Assert.That(group.alpha, Is.EqualTo(0.28f).Within(0.001f)); // 편집 기본값과 겹침 배율 합산
            Assert.That(hp.color, Is.EqualTo(new Color(0.2f, 0.4f, 0.7f, 0.9f))); // 겹침 시 원본 색상 보존
        } // 범위 종료
        finally // 객체 정리
        { // 정리 시작
            Object.DestroyImmediate(root); // UI 제거
            Object.DestroyImmediate(playerObject); // 전투 상태 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Object.DestroyImmediate(sprite); // 시험 스프라이트 제거
            Object.DestroyImmediate(texture); // 시험 이미지 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
