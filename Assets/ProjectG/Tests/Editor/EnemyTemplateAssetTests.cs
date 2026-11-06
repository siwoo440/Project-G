using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 적 전투 기능
using UnityEditor; // 직렬화 참조 기능
using UnityEngine; // 유니티 객체 기능
using UnityEngine.TestTools; // 누락 오류 확인
public class EnemyTemplateAssetTests // 적 템플릿 에셋 시험
{ // 시험 시작
    private static readonly string[] Names = // 프리팹 이름
    { // 배열 시작
        "Aimed", "Spread", "Dash", "MidBoss", "FinalBoss" // 배열 항목
    }; // 배열 종료
    private static readonly string[] Textures = // 기존 이미지 이름
    { // 배열 시작
        "enemy_aimed", "enemy_spread", "enemy_dash", "boss_mid", "boss_final" // 배열 항목
    }; // 배열 종료
    private static readonly string[] Fields = // 스테이지 연결 필드
    { // 배열 시작
        "aimedTemplate", "spreadTemplate", "dashTemplate", "midBossTemplate", "finalBossTemplate" // 배열 항목
    }; // 배열 종료
    [TestCase(0)] // 조준형 이미지
    [TestCase(1)] // 확산형 이미지
    [TestCase(2)] // 돌진형 이미지
    [TestCase(3)] // 중간 보스 이미지
    [TestCase(4)] // 최종 보스 이미지
    public void PrefabKeepsOriginalTextureBoundsAndSize(int index) // 기존 외형과 표시 참조
    { // 시험 시작
        EnemyKind kind = (EnemyKind)index; // 해당 종류
        EnemyActor asset = Resources.Load<EnemyActor>("EnemyTemplates/" + Names[index]); // 실제 프리팹 자산
        Assert.That(asset, Is.Not.Null); // 종류별 에셋 존재
        Assert.That(asset.gameObject.activeSelf, Is.False); // 템플릿 비활성 유지
        Assert.That(asset.HasSceneVisual(kind), Is.True); // 필수 표시 연결
        SpriteRenderer body = asset.transform.Find("EnemyBody").GetComponent<SpriteRenderer>(); // 몸체 이미지
        Texture2D original = Resources.Load<Texture2D>("BattleArt/" + Textures[index]); // 기존 원본 텍스처
        Assert.That(body.sprite.texture, Is.SameAs(original)); // 원본 텍스처 참조 보존
        Assert.That(body.sprite.rect, Is.EqualTo(new Rect(0f, 0f, original.width, original.height))); // 기존 전체 표시 영역
        Assert.That(body.sprite.pixelsPerUnit, Is.EqualTo(Mathf.Max(original.width, original.height)).Within(0.001f)); // 기존 표시 비율
        Assert.That(body.transform.localScale, Is.EqualTo(index >= 3 ? new Vector3(6.6f, 3.3f, 1f) : new Vector3(3f, 2.1f, 1f))); // 기존 몸체 확대
        SpriteRenderer frame = asset.transform.Find("EnemyHealthBar/Frame").GetComponent<SpriteRenderer>(); // 체력바 외곽
        SpriteRenderer fill = asset.transform.Find("EnemyHealthBar/Fill").GetComponent<SpriteRenderer>(); // 체력바 채움
        Assert.That(frame.sprite.texture.name, Is.EqualTo("ui_card_frame")); // 기존 외곽 텍스처
        Assert.That(fill.sprite.texture, Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ProjectG/Resources/EnemyTemplates/white_persistent.png"))); // 저장된 단색 텍스처
    } // 시험 종료
    [Test] // 씬 템플릿 실제 사용 시험
    public void StageSpawnsEditedSceneInstanceAndKeepsTemplateInactive() // 씬 편집값의 실제 스폰 반영
    { // 시험 시작
        GameObject holder = new GameObject("Stage"); // 스테이지 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        GameObject cameraObject = new GameObject("Camera"); // 카메라 객체
        try // 시험 범위
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라
            camera.orthographic = true; // 기존 직교 카메라
            camera.orthographicSize = 5f; // 기존 화면 높이
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 생성
            player.Begin(new Vector2(0f, -3f)); // 출격 상태 초기화
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(cameraObject); // 실제 적탄 풀
            StageDirector stage = holder.AddComponent<StageDirector>(); // 스테이지 구성
            SerializedObject serialized = new SerializedObject(stage); // 스테이지 필드 조회
            EnemyActor first = null; // 편집 대상 템플릿
            for (int index = 0; index < Names.Length; index++) // 종류별 반복
            { // 반복 시작
                EnemyActor prefab = Resources.Load<EnemyActor>("EnemyTemplates/" + Names[index]); // 종류별 실제 프리팹
                Assert.That(prefab, Is.Not.Null); // 프리팹 존재 확인
                EnemyActor template = Object.Instantiate(prefab, holder.transform, false); // 비활성 씬 인스턴스
                serialized.FindProperty(Fields[index]).objectReferenceValue = template; // 실제 씬 템플릿 참조
                if (index == 0) // 조준형 확인
                { // 편집 대상 시작
                    first = template; // 편집 대상 저장
                } // 편집 대상 종료
            } // 반복 종료
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 실제 참조 저장
            SpriteRenderer edited = first.transform.Find("EnemyBody").GetComponent<SpriteRenderer>(); // 편집 몸체
            edited.sprite = BattleSprites.Load("player_sniper"); // 씬 이미지 변경
            edited.color = new Color(0.3f, 0.6f, 0.9f, 1f); // 씬 색상 변경
            edited.transform.localScale = new Vector3(2f, 4f, 1f); // 씬 크기 변경
            Assert.That(stage.HasSceneTemplates, Is.True); // 다섯 템플릿 연결 확인
            stage.Begin(player, camera, pool, null, null); // 실제 스테이지 시작
            stage.Advance(2.1f, 2.1f); // 첫 적 등장 진행
            EnemyActor[] active = holder.GetComponentsInChildren<EnemyActor>(); // 실제 활성 적 조회
            Assert.That(active.Length, Is.EqualTo(1)); // 복제 적 한 개 생성
            SpriteRenderer spawned = active[0].transform.Find("EnemyBody").GetComponent<SpriteRenderer>(); // 실제 적 표시
            Assert.That(spawned.sprite, Is.SameAs(edited.sprite)); // 씬 편집 이미지 반영
            Assert.That(spawned.color, Is.EqualTo(edited.color)); // 씬 편집 색상 반영
            Assert.That(spawned.transform.localScale, Is.EqualTo(edited.transform.localScale)); // 씬 편집 크기 반영
            Assert.That(first.gameObject.activeSelf, Is.False); // 템플릿 비활성 유지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 스테이지와 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라와 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 템플릿 누락 시험
    public void MissingTemplateStopsStageInitializationWithError() // 필수 씬 참조 누락 차단
    { // 시험 시작
        GameObject holder = new GameObject("Stage"); // 빈 스테이지
        try // 시험 범위
        { // 범위 시작
            StageDirector stage = holder.AddComponent<StageDirector>(); // 템플릿 없는 스테이지
            Assert.That(stage.HasSceneTemplates, Is.False); // 연결 누락 확인
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("적.*보스.*템플릿")); // 명시적 오류 확인
            stage.Begin(null, null, null, null, null); // 잘못된 시작 시도
            Assert.That(stage.State, Is.Null); // 시작과 임의 생성 차단
            Assert.That(holder.GetComponentsInChildren<EnemyActor>(true).Length, Is.Zero); // 임의 적 생성 방지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 스테이지 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
