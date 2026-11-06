using System; // 공통 화면 유형 조회
using System.Reflection; // 초기 화면 계산 API 확인
using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 전투 화면 기능
using UnityEngine; // 유니티 좌표 기능
using Object = UnityEngine.Object; // 유니티 객체 명확화
public class BattleLayoutBoundsTests // 전투 화면 분할과 경계 시험
{ // 시험 시작
    private static Rect Viewport(string methodName, float width, float height) // 공통 화면 영역 계산
    { // 계산 시작
        Type layout = typeof(BattleViewport).Assembly.GetType("ProjectG.BattleLayout"); // 실제 분할 계산 유형
        Assert.That(layout, Is.Not.Null); // 공통 계산기 존재 확인
        MethodInfo method = layout.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static); // 영역 계산 API 조회
        Assert.That(method, Is.Not.Null); // 계산 API 존재 확인
        return (Rect)method.Invoke(null, new object[] // 해당 화면의 정규화 영역
        { // 배열 시작
            width, height // 배열 항목
        }); // 배열 종료
    } // 계산 종료
    [TestCase(1920f, 1080f)] // 16대9 화면
    [TestCase(1280f, 800f)] // 16대10 화면
    [TestCase(960f, 720f)] // 4대3 화면
    [TestCase(960f, 540f)] // 작은 16대9 화면
    public void BattleAndPanelSplitInsideExistingLetterbox(float width, float height) // 콘텐츠 내부 분할과 카메라 비율
    { // 시험 시작
        Rect content = BattleSetup.CalculateViewport(width, height); // 기존 16대9 콘텐츠 영역
        Rect battle = Viewport("BattleViewport", width, height); // 왼쪽 전투 영역
        Rect panel = Viewport("PanelViewport", width, height); // 오른쪽 정보 영역
        Assert.That(battle.xMin, Is.EqualTo(content.xMin).Within(0.0001f)); // 콘텐츠 왼쪽 일치
        Assert.That(battle.yMin, Is.EqualTo(content.yMin).Within(0.0001f)); // 콘텐츠 아래쪽 일치
        Assert.That(battle.height, Is.EqualTo(content.height).Within(0.0001f)); // 콘텐츠 세로 유지
        Assert.That(battle.width, Is.EqualTo(content.width * 0.75f).Within(0.0001f)); // 전투 가로 75퍼센트
        Assert.That(panel.xMin, Is.EqualTo(battle.xMax).Within(0.0001f)); // 전투와 패널 경계 일치
        Assert.That(panel.xMax, Is.EqualTo(content.xMax).Within(0.0001f)); // 콘텐츠 오른쪽 일치
        Assert.That(panel.height, Is.EqualTo(content.height).Within(0.0001f)); // 패널 세로 유지
        Assert.That(battle.width * width / (battle.height * height), Is.EqualTo(4f / 3f).Within(0.0001f)); // 실제 전투 화면 비율
    } // 시험 종료
    [Test] // 작은 전투 화면 상단 문구 시험
    public void StageLabelsRemainInsideSmallBattleView() // 패널과 무관한 상단 문구
    { // 시험 시작
        Rect battle = new Rect(100f, 20f, 480f, 360f); // 좁은 전투 화면
        Rect[] labels = // 실제 상단 표시 영역
        { // 배열 시작
            StageDirector.StageHeaderRect(battle), StageDirector.PhaseNoticeRect(battle), StageDirector.ContextNoticeRect(battle) // 배열 항목
        }; // 배열 종료
        foreach (Rect label in labels) // 표시 영역 순회
        { // 검사 시작
            Assert.That(label.xMin, Is.GreaterThanOrEqualTo(battle.xMin)); // 전투 왼쪽 경계
            Assert.That(label.xMax, Is.LessThanOrEqualTo(battle.xMax)); // 패널 침범 방지
        } // 검사 종료
    } // 시험 종료
    [Test] // 오른쪽 가장자리 아이템 문구 시험
    public void DropLabelRemainsInsideBattleViewNearPanel() // 아이템 이름의 패널 침범 방지
    { // 시험 시작
        Rect battle = new Rect(100f, 20f, 480f, 360f); // 좁은 전투 화면
        MethodInfo method = typeof(UpgradeDropManager).GetMethod("DropLabelRect", BindingFlags.Public | BindingFlags.Static); // 실제 아이템 문구 계산
        Assert.That(method, Is.Not.Null); // 문구 경계 API 존재
        Rect label = (Rect)method.Invoke(null, new object[] // 오른쪽 아이템 문구
        { // 배열 시작
            battle, new Vector2(battle.xMax - 2f, battle.y + 100f) // 배열 항목
        }); // 배열 종료
        Assert.That(label.xMin, Is.GreaterThanOrEqualTo(battle.xMin)); // 전투 왼쪽 경계
        Assert.That(label.xMax, Is.LessThanOrEqualTo(battle.xMax)); // 패널 침범 방지
    } // 시험 종료
    [Test] // 화면 비율에 따른 적 등장 시험
    public void StageSpawnsInsideFourByThreeBattleBounds() // 실제 카메라 가로 경계 사용
    { // 시험 시작
        GameObject scene = new GameObject("Stage"); // 스테이지 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        try // 시험 범위
        { // 범위 시작
            Camera camera = scene.AddComponent<Camera>(); // 전투 카메라
            camera.orthographic = true; // 직교 투영
            camera.orthographicSize = 5f; // 기존 세로 반경
            camera.aspect = 4f / 3f; // 실제 전투 가로 비율
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.Begin(new Vector2(0f, -3f)); // 안전한 플레이어 위치
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(scene); // 실제 적탄 풀
            StageDirector stage = EnemyActorTestFactory.AttachStage(scene); // 다섯 적 원본 연결
            stage.Begin(player, camera, pool, null, null); // 스테이지 시작
            stage.Advance(2.01f, 2.01f); // 첫 적 등장 시간
            EnemyActor[] enemies = scene.GetComponentsInChildren<EnemyActor>(); // 실제 활성 적
            Assert.That(enemies.Length, Is.EqualTo(1)); // 실제 적 등장
            Assert.That(enemies[0].transform.position.x, Is.EqualTo(-5f * (4f / 3f) * 0.55f).Within(0.001f)); // 실제 전투 폭 기준 등장
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(scene); // 카메라와 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 화면 밖 적탄 회수 시험
    public void EnemyBulletRecyclesAfterLeavingFourByThreeBattleView() // 실제 전투 가로 경계 회수
    { // 시험 시작
        GameObject scene = new GameObject("BattleCamera"); // 전투 카메라 객체
        try // 시험 범위
        { // 범위 시작
            Camera camera = scene.AddComponent<Camera>(); // 실제 카메라
            camera.orthographicSize = 5f; // 기존 세로 반경
            camera.aspect = 4f / 3f; // 실제 전투 가로 비율
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(scene); // 실제 적탄 풀
            EnemyBullet bullet = pool.Spawn(new Vector2(8.5f, 0f), Vector2.right, 0.06f, 1, -5f, null); // 화면 밖 적탄
            bullet.Simulate(0.01f); // 이탈 판정 진행
            Assert.That(bullet.InUse, Is.False); // 화면 밖 적탄 회수
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(scene); // 카메라와 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 필살기 화면 범위 시험
    public void UltimateClearsVisibleButNotOffscreenBulletsInFourByThreeView() // 실제 전투 화면만 소거
    { // 시험 시작
        GameObject scene = new GameObject("BattleCamera"); // 카메라 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        try // 시험 범위
        { // 범위 시작
            Camera camera = scene.AddComponent<Camera>(); // 실제 카메라
            camera.orthographicSize = 5f; // 기존 세로 반경
            camera.aspect = 4f / 3f; // 실제 전투 가로 비율
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(scene); // 실제 적탄 풀
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.AssignCamera(camera); // 전투 카메라 연결
            player.AssignBulletPool(pool); // 적탄 풀 연결
            player.Begin(Vector2.zero); // 속사형 시작
            player.Skills.GainEnergy(100); // 필살기 에너지 준비
            EnemyBullet visible = pool.Spawn(new Vector2(6f, 0f), Vector2.zero, 0.06f, 1, -5f, player); // 전투 화면 안 적탄
            EnemyBullet outside = pool.Spawn(new Vector2(8f, 0f), Vector2.zero, 0.06f, 1, -5f, player); // 전투 화면 밖 적탄
            Assert.That(player.TryUseUltimate(), Is.True); // 실제 필살기 사용
            Assert.That(visible.InUse, Is.False); // 보이는 적탄 소거
            Assert.That(outside.InUse, Is.True); // 보이지 않는 적탄 유지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(scene); // 카메라와 적탄 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(1920f, 1080f)] // 전체 해상도
    [TestCase(1280f, 800f)] // 16대10 화면
    [TestCase(960f, 720f)] // 4대3 화면
    [TestCase(960f, 540f)] // 작은 화면
    public void CardsTutorialAndPauseStayInsideBattleArea(float width, float height) // 선택과 정지 화면 경계
    { // 시험 시작
        Rect normalized = BattleLayout.BattleViewport(width, height); // 정규화 전투 영역
        Rect battle = new Rect(normalized.x * width, (1f - normalized.yMax) * height, normalized.width * width, normalized.height * height); // 화면 위쪽 기준 전투 영역
        for (int index = 0; index < 3; index++) // 세 카드 순회
        { // 카드 검사 시작
            Rect card = CardUiLayout.Card(battle, index); // 실제 카드 영역
            Assert.That(card.xMin, Is.GreaterThanOrEqualTo(battle.xMin)); // 카드 왼쪽 경계
            Assert.That(card.xMax, Is.LessThanOrEqualTo(battle.xMax)); // 패널 침범 방지
        } // 카드 검사 종료
        Rect tutorial = TutorialGuide.PanelRect(battle); // 실제 첫 출격 안내
        Assert.That(tutorial.xMin, Is.GreaterThanOrEqualTo(battle.xMin)); // 안내 왼쪽 경계
        Assert.That(tutorial.xMax, Is.LessThanOrEqualTo(battle.xMax)); // 안내 패널 침범 방지
        MethodInfo method = typeof(PauseSettingsOverlay).GetMethod("PanelRect", BindingFlags.Static | BindingFlags.NonPublic); // 실제 일시정지 패널 계산
        Assert.That(method, Is.Not.Null); // 패널 계산 존재
        Rect pause = (Rect)method.Invoke(null, new object[] // 실제 일시정지 패널
        { // 배열 시작
            battle // 배열 항목
        }); // 배열 종료
        Assert.That(pause.xMin, Is.GreaterThanOrEqualTo(battle.xMin)); // 정지 화면 왼쪽 경계
        Assert.That(pause.xMax, Is.LessThanOrEqualTo(battle.xMax)); // 정지 화면의 정보 패널 침범 방지
    } // 시험 종료
    [Test] // 좁아진 전투 이동 시험
    public void MovementCrossesActualBattleWidthInFourSeconds() // 실제 폭 기준 이동 속도
    { // 시험 시작
        float halfWidth = 5f * 4f / 3f; // 전투 가로 반경
        Vector2 left = new Vector2(-halfWidth, 0f); // 전투 왼쪽 끝
        Vector2 normal = MovementMath.Move(left, Vector2.right, -halfWidth, halfWidth, -5f, 5f, false, 4f); // 일반 속도 네 초
        Vector2 focused = MovementMath.Move(left, Vector2.right, -halfWidth, halfWidth, -5f, 5f, true, 4f); // 집중 속도 네 초
        Assert.That(normal.x, Is.EqualTo(halfWidth).Within(0.001f)); // 네 초 횡단 유지
        Assert.That(focused.x, Is.EqualTo(0f).Within(0.001f)); // 집중 속도 절반
    } // 시험 종료
} // 시험 종료
