using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 적 전투 기능
using UnityEngine; // 화면 객체 기능
public class EnemySceneTemplateTests // 씬 적 템플릿 시험
{ // 시험 시작
    private static int CountDirect(Transform parent, string childName) // 직계 자식 개수 확인
    { // 확인 시작
        int count = 0; // 일치 개수
        foreach (Transform child in parent) // 자식 순회
        { // 순회 시작
            if (child.name == childName) // 이름 일치 확인
            { // 일치 시작
                count++; // 개수 증가
            } // 일치 종료
        } // 순회 종료
        return count; // 자식 개수 반환
    } // 확인 종료
    private static void SetField(object target, string name, object value) // 시험용 직렬화 참조 연결
    { // 연결 시작
        System.Reflection.FieldInfo field = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 내부 참조 조회
        Assert.That(field, Is.Not.Null); // 필드 존재 확인
        field.SetValue(target, value); // 시험 템플릿 참조 연결
    } // 연결 종료
    private static GameObject MakeTemplate() // 시험용 편집 템플릿 생성
    { // 템플릿 시작
        GameObject root = new GameObject("SceneTemplate"); // 적 템플릿 루트
        EnemyActor actor = root.AddComponent<EnemyActor>(); // 적 구성 요소
        GameObject body = new GameObject("EnemyBody"); // 편집 가능한 몸체
        body.transform.SetParent(root.transform, false); // 몸체 연결
        SpriteRenderer bodyImage = body.AddComponent<SpriteRenderer>(); // 몸체 이미지
        bodyImage.sprite = BattleSprites.Load("enemy_aimed"); // 기본 시험 이미지
        GameObject bar = new GameObject("EnemyHealthBar"); // 편집 가능한 체력바
        bar.transform.SetParent(root.transform, false); // 체력바 연결
        EnemyHealthBar healthBar = bar.AddComponent<EnemyHealthBar>(); // 체력바 기능
        GameObject frame = new GameObject("Frame"); // 편집 가능한 외곽
        frame.transform.SetParent(bar.transform, false); // 외곽 연결
        frame.AddComponent<SpriteRenderer>().sprite = BattleSprites.Load("ui_card_frame"); // 외곽 이미지
        GameObject fill = new GameObject("Fill"); // 편집 가능한 체력 이미지
        fill.transform.SetParent(bar.transform, false); // 체력 이미지 연결
        SpriteRenderer fillImage = fill.AddComponent<SpriteRenderer>(); // 체력 표시 요소
        fillImage.sprite = BattleSprites.Load("ui_card_frame"); // 시험용 체력 이미지
        SetField(healthBar, "frame", frame.GetComponent<SpriteRenderer>()); // 체력 외곽 참조 연결
        SetField(healthBar, "fill", fillImage); // 체력 이미지 참조 연결
        GameObject weak = new GameObject("EnemyWeakPoint"); // 편집 가능한 약점
        weak.transform.SetParent(root.transform, false); // 약점 연결
        weak.AddComponent<SpriteRenderer>().sprite = BattleSprites.Load("enemy_aimed"); // 시험 약점 이미지
        GameObject warning = new GameObject("EnemyAttackWarning"); // 편집 가능한 돌진 경고
        warning.transform.SetParent(root.transform, false); // 돌진 경고 연결
        warning.AddComponent<SpriteRenderer>().sprite = BattleSprites.Load("enemy_aimed"); // 시험 경고 이미지
        GameObject path = new GameObject("EnemyDashPath"); // 편집 가능한 돌진 경로
        path.transform.SetParent(root.transform, false); // 돌진 경로 연결
        path.AddComponent<SpriteRenderer>().sprite = BattleSprites.Load("enemy_aimed"); // 시험 경로 이미지
        SetField(actor, "bodyImage", bodyImage); // 몸체 참조 연결
        SetField(actor, "healthBar", healthBar); // 체력바 참조 연결
        SetField(actor, "weakMark", weak); // 약점 참조 연결
        SetField(actor, "warningMark", warning); // 돌진 경고 참조 연결
        SetField(actor, "dashPath", path); // 돌진 경로 참조 연결
        return root; // 시험 템플릿 반환
    } // 템플릿 종료
    [TestCase(EnemyKind.Aimed)] // 조준형 템플릿
    [TestCase(EnemyKind.Spread)] // 확산형 템플릿
    [TestCase(EnemyKind.Dash)] // 돌진형 템플릿
    [TestCase(EnemyKind.MidBoss)] // 중간 보스 템플릿
    [TestCase(EnemyKind.FinalBoss)] // 최종 보스 템플릿
    public void SpawnedTemplatePreservesEditedVisualWithoutDuplicateChildren(EnemyKind kind) // 종류별 편집값 보존
    { // 시험 시작
        GameObject template = MakeTemplate(); // 편집 가능한 시험 템플릿
        GameObject instance = null; // 복제 객체
        float oldScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 진행
            Transform body = template.transform.Find("EnemyBody"); // 편집할 몸체
            SpriteRenderer image = body.GetComponent<SpriteRenderer>(); // 편집할 이미지
            Sprite editedSprite = BattleSprites.Load("player_sniper"); // 임의 편집 이미지
            Color editedColor = new Color(0.25f, 0.5f, 0.75f, 0.8f); // 임의 편집 색상
            Vector3 editedPosition = new Vector3(0.3f, -0.4f, 0f); // 임의 편집 위치
            Quaternion editedRotation = Quaternion.Euler(0f, 0f, 17f); // 임의 편집 각도
            Vector3 editedScale = new Vector3(1.4f, 1.7f, 1f); // 임의 편집 크기
            image.sprite = editedSprite; // 사용자 이미지 편집
            image.color = editedColor; // 사용자 색상 편집
            body.localPosition = editedPosition; // 사용자 위치 편집
            body.localRotation = editedRotation; // 사용자 각도 편집
            body.localScale = editedScale; // 사용자 크기 편집
            template.SetActive(false); // 원본 템플릿 비활성화
            instance = Object.Instantiate(template); // 템플릿 복제
            instance.SetActive(true); // 실제 적 활성화
            EnemyActor enemy = instance.GetComponent<EnemyActor>(); // 복제 적 참조
            enemy.Begin(kind, null, null, null); // 적 전투 초기화
            Assert.That(CountDirect(instance.transform, "EnemyBody"), Is.EqualTo(1)); // 몸체 중복 생성 방지
            Assert.That(CountDirect(instance.transform, "EnemyHealthBar"), Is.EqualTo(1)); // 체력바 중복 생성 방지
            Assert.That(CountDirect(instance.transform, "EnemyWeakPoint"), Is.EqualTo(1)); // 약점 중복 생성 방지
            Transform actual = instance.transform.Find("EnemyBody"); // 실제 표시 몸체
            Assert.That(actual.GetComponent<SpriteRenderer>().sprite, Is.SameAs(editedSprite)); // 편집 이미지 유지
            Assert.That(actual.GetComponent<SpriteRenderer>().color, Is.EqualTo(editedColor)); // 편집 색상 유지
            Assert.That(actual.localPosition, Is.EqualTo(editedPosition)); // 편집 위치 유지
            Assert.That(Quaternion.Angle(actual.localRotation, editedRotation), Is.LessThan(0.001f)); // 편집 각도 유지
            Assert.That(actual.localScale, Is.EqualTo(editedScale)); // 편집 크기 유지
            bool before = enemy.Intersects(new Vector2(0f, -2f), new Vector2(0f, 2f), out bool weakBefore); // 초기 몸체 판정
            enemy.Advance(0.37f); // 표시 애니메이션 진행
            bool after = enemy.Intersects(new Vector2(0f, -2f), new Vector2(0f, 2f), out bool weakAfter); // 진행 후 몸체 판정
            Assert.That(after, Is.EqualTo(before)); // 표시와 피격 판정 분리
            Assert.That(weakAfter, Is.EqualTo(weakBefore)); // 표시와 약점 판정 분리
            enemy.Begin(kind, null, null, null); // 동일 종류 재초기화
            Assert.That(actual.localPosition, Is.EqualTo(editedPosition)); // 원래 편집 위치 복원
            Assert.That(Quaternion.Angle(actual.localRotation, editedRotation), Is.LessThan(0.001f)); // 원래 편집 각도 복원
            Assert.That(actual.localScale, Is.EqualTo(editedScale)); // 원래 편집 크기 복원
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = oldScale; // 시간 배율 복원
            if (instance != null) // 복제 존재 확인
            { // 복제 정리 시작
                Object.DestroyImmediate(instance); // 복제 제거
            } // 복제 정리 종료
            Object.DestroyImmediate(template); // 템플릿 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 템플릿 체력바 시험
    public void SpawnedTemplatePreservesEditedHealthBarAndDisplaysDamage() // 편집된 체력바 유지
    { // 시험 시작
        GameObject template = MakeTemplate(); // 편집 가능한 시험 템플릿
        GameObject instance = null; // 복제 객체
        try // 시험 범위
        { // 범위 시작
            Transform bar = template.transform.Find("EnemyHealthBar"); // 체력바 객체
            Transform frame = bar.Find("Frame"); // 체력바 외곽
            Transform fill = bar.Find("Fill"); // 체력바 내부
            Color frameColor = new Color(0.6f, 0.3f, 0.2f, 0.9f); // 편집 외곽 색상
            Color fillColor = new Color(0.9f, 0.5f, 0.2f, 0.8f); // 편집 체력 색상
            Vector3 barPosition = new Vector3(0.25f, -2f, 0f); // 편집 체력바 위치
            Vector3 frameScale = new Vector3(2.4f, 0.7f, 1f); // 편집 외곽 크기
            frame.GetComponent<SpriteRenderer>().color = frameColor; // 외곽 색상 편집
            fill.GetComponent<SpriteRenderer>().color = fillColor; // 체력 색상 편집
            bar.localPosition = barPosition; // 체력바 위치 편집
            frame.localScale = frameScale; // 외곽 크기 편집
            template.SetActive(false); // 원본 템플릿 비활성화
            instance = Object.Instantiate(template); // 템플릿 복제
            instance.SetActive(true); // 실제 적 활성화
            EnemyActor enemy = instance.GetComponent<EnemyActor>(); // 복제 적 참조
            enemy.Begin(EnemyKind.Aimed, null, null, null); // 적 전투 초기화
            Assert.That(CountDirect(instance.transform, "EnemyHealthBar"), Is.EqualTo(1)); // 체력바 중복 생성 방지
            Transform actualBar = instance.transform.Find("EnemyHealthBar"); // 실제 체력바
            Assert.That(actualBar.localPosition, Is.EqualTo(barPosition)); // 편집 위치 유지
            Assert.That(actualBar.Find("Frame").localScale, Is.EqualTo(frameScale)); // 편집 외곽 크기 유지
            Assert.That(actualBar.Find("Frame").GetComponent<SpriteRenderer>().color, Is.EqualTo(frameColor)); // 편집 외곽 색상 유지
            Assert.That(actualBar.Find("Fill").GetComponent<SpriteRenderer>().color, Is.EqualTo(fillColor)); // 편집 체력 색상 유지
            Assert.That(actualBar.gameObject.activeSelf, Is.False); // 초기 체력바 숨김
            enemy.ApplyDamage(1f, null); // 실제 피해 적용
            Assert.That(actualBar.gameObject.activeSelf, Is.True); // 피해 후 체력바 표시
            enemy.Advance(2.1f); // 체력바 표시 시간 경과
            Assert.That(actualBar.gameObject.activeSelf, Is.False); // 시간 경과 후 숨김
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (instance != null) // 복제 존재 확인
            { // 복제 정리 시작
                Object.DestroyImmediate(instance); // 복제 제거
            } // 복제 정리 종료
            Object.DestroyImmediate(template); // 원본 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
