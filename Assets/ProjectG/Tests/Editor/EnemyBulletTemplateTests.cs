using System.Reflection; // 연결 API 확인
using NUnit.Framework; // 시험 도구
using ProjectG; // 적탄과 전투 기능
using UnityEditor; // 템플릿 직렬화 설정
using UnityEngine; // 유니티 객체 기능
using UnityEngine.TestTools; // 오류 로그 확인
public class EnemyBulletTemplateTests // 씬에 배치된 적탄 템플릿 시험
{ // 시험 시작
    private static void Attach(EnemyBulletPool pool, EnemyBullet template) // 비활성 템플릿 연결
    { // 연결 시작
        MethodInfo method = typeof(EnemyBulletPool).GetMethod("SetTemplate"); // 명시적 연결 API
        Assert.That(method, Is.Not.Null); // 연결 API 존재 확인
        method.Invoke(pool, new object[] // 씬 템플릿 전달
        { // 배열 시작
            template // 배열 항목
        }); // 배열 종료
    } // 연결 종료
    private static EnemyBullet MakeTemplate(Transform owner, Sprite sprite) // 편집한 비활성 템플릿 생성
    { // 템플릿 시작
        GameObject root = new GameObject("EnemyBulletTemplate"); // 템플릿 루트
        root.SetActive(false); // Hierarchy 템플릿 비활성화
        root.transform.SetParent(owner, false); // 씬 풀에 배치
        root.transform.localScale = new Vector3(2f, 1.5f, 1f); // 편집한 루트 크기
        GameObject radiusScale = new GameObject("RadiusScale"); // 판정별 표시 배율 자식
        radiusScale.transform.SetParent(root.transform, false); // 템플릿에 연결
        GameObject visual = new GameObject("Artwork"); // 편집 가능한 표시 자식
        visual.transform.SetParent(radiusScale.transform, false); // 표시 배율 아래 연결
        visual.transform.localScale = new Vector3(0.7f, 0.8f, 1f); // 편집한 이미지 크기
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>(); // 이미지 구성
        renderer.sprite = sprite; // 편집한 이미지
        renderer.color = new Color(0.4f, 0.7f, 0.9f, 0.6f); // 편집한 색상과 투명도
        EnemyBullet template = root.AddComponent<EnemyBullet>(); // 적탄 템플릿 구성
        SerializedObject fields = new SerializedObject(template); // 템플릿 연결 조회
        fields.FindProperty("image").objectReferenceValue = renderer; // 편집 이미지 연결
        fields.FindProperty("radiusScale").objectReferenceValue = radiusScale.transform; // 표시 배율 연결
        fields.ApplyModifiedPropertiesWithoutUndo(); // 템플릿 연결 저장
        return template; // 적탄 템플릿 반환
    } // 템플릿 종료
    [Test] // 씬 편집과 재사용 시험
    public void SpawnUsesEditedTemplateAndReusesWithoutActivatingTemplate() // 편집값과 풀 재사용 확인
    { // 시험 시작
        GameObject holder = new GameObject("Pool"); // 풀 객체
        try // 시험 범위
        { // 범위 시작
            EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 실제 풀 구성
            Sprite sprite = BattleSprites.Load("player_rapid"); // 사용자가 변경한 이미지
            EnemyBullet template = MakeTemplate(holder.transform, sprite); // 비활성 편집 템플릿
            Attach(pool, template); // 템플릿 연결
            Assert.That(pool.ActiveCount, Is.Zero); // 원본 템플릿 집계 제외
            EnemyBullet first = pool.Spawn(Vector2.zero, Vector2.down, 0.06f, 10, -5f, null); // 첫 발사
            Assert.That(first, Is.Not.Null); // 실제 탄환 생성
            Assert.That(template.gameObject.activeSelf, Is.False); // 원본 템플릿 비활성 유지
            Assert.That(pool.ActiveCount, Is.EqualTo(1)); // 복제 탄환만 집계
            SpriteRenderer image = first.GetComponentInChildren<SpriteRenderer>(); // 실제 탄환 이미지
            Assert.That(image.sprite, Is.SameAs(sprite)); // 편집 이미지 유지
            Assert.That(image.color, Is.EqualTo(new Color(0.4f, 0.7f, 0.9f, 0.6f))); // 편집 색상 유지
            Assert.That(first.transform.localScale, Is.EqualTo(template.transform.localScale)); // 편집 루트 크기 유지
            Assert.That(image.transform.localScale, Is.EqualTo(template.transform.Find("RadiusScale/Artwork").localScale)); // 편집 이미지 크기 유지
            Assert.That(first.Radius, Is.EqualTo(0.06f).Within(0.0001f)); // 표시와 독립된 판정 반경
            pool.Recycle(first); // 기존 탄환 회수
            EnemyBullet reused = pool.Spawn(Vector2.right, Vector2.down, 0.06f, 10, -5f, null); // 같은 탄환 재사용
            Assert.That(reused, Is.SameAs(first)); // 풀 객체 재사용
            Assert.That(reused.GetComponentInChildren<SpriteRenderer>().sprite, Is.SameAs(sprite)); // 재사용 이미지 유지
            Assert.That(reused.GetComponentInChildren<SpriteRenderer>().color, Is.EqualTo(image.color)); // 재사용 색상 유지
            Assert.That(reused.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(1)); // 렌더러 중복 방지
            Assert.That(template.gameObject.activeSelf, Is.False); // 원본 템플릿 계속 비활성
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 풀과 탄환 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 편집된 이미지의 화면 경계 시험
    public void ClearVisibleUsesEditedDisplaySizeWithoutChangingHitRadius() // 실제 표시가 걸친 탄환 소거
    { // 시험 시작
        GameObject holder = new GameObject("Pool"); // 풀 객체
        try // 시험 범위
        { // 범위 시작
            EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 실제 풀 구성
            Attach(pool, MakeTemplate(holder.transform, BattleSprites.Load("bullet_enemy"))); // 크게 편집한 템플릿 연결
            EnemyBullet bullet = pool.Spawn(new Vector2(8.5f, 0f), Vector2.zero, 0.06f, 10, -5f, null); // 중심은 화면 밖에 위치
            SpriteRenderer image = bullet.GetComponentInChildren<SpriteRenderer>(); // 실제 표시 이미지
            Assert.That(image.bounds.min.x, Is.LessThan(8f)); // 편집 이미지가 화면에 걸침
            Assert.That(bullet.Radius, Is.EqualTo(0.06f).Within(0.0001f)); // 피격 판정 반경 유지
            Assert.That(pool.ClearVisible(new Rect(-8f, -5f, 16f, 10f)), Is.EqualTo(1)); // 보이는 탄환 소거
            Assert.That(bullet.gameObject.activeSelf, Is.False); // 화면의 탄환 회수
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 풀과 탄환 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 표시 크기와 판정 반경 독립 시험
    public void EditedVisualDoesNotChangeHitOrGrazeRadius() // 편집한 크기에서 충돌 유지
    { // 시험 시작
        GameObject holder = new GameObject("Pool"); // 풀 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        try // 시험 범위
        { // 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 생존 상태 초기화
            EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 실제 풀 구성
            Attach(pool, MakeTemplate(holder.transform, BattleSprites.Load("bullet_enemy"))); // 크게 편집한 템플릿 연결
            EnemyBullet bullet = pool.Spawn(new Vector2(0.3f, 0f), Vector2.zero, 0.06f, 10, -5f, player); // 판정은 근접 범위
            bullet.Simulate(0.01f); // 첫 그레이즈 검사
            bullet.Simulate(0.01f); // 중복 그레이즈 검사
            Assert.That(player.GrazeCount, Is.EqualTo(1)); // 탄환별 근접 충전 한 번
            Assert.That(player.Survival.Health, Is.EqualTo(100)); // 이미지 크기로 인한 피격 방지
            pool.Recycle(bullet); // 시험 탄환 회수
            EnemyBullet hit = pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, player); // 실제 피격 위치
            Assert.That(hit, Is.SameAs(bullet)); // 같은 탄환 재사용
            hit.Simulate(0.01f); // 피격 처리
            Assert.That(player.Survival.Health, Is.EqualTo(90)); // 기존 피해 유지
            Assert.That(hit.gameObject.activeSelf, Is.False); // 피격 탄환 회수
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 풀과 탄환 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 템플릿 누락 시험
    public void MissingTemplateReportsErrorWithoutGeneratingRuntimePlaceholder() // 암묵적 생성 차단
    { // 시험 시작
        GameObject holder = new GameObject("Pool"); // 풀 객체
        try // 시험 범위
        { // 범위 시작
            EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 템플릿 없는 풀
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("적탄.*템플릿")); // 명시적 누락 오류
            EnemyBullet bullet = pool.Spawn(Vector2.zero, Vector2.down, 0.06f, 10, -5f, null); // 잘못된 발사 요청
            Assert.That(bullet, Is.Null); // 대체 탄환 생성 차단
            Assert.That(holder.GetComponentsInChildren<EnemyBullet>(true).Length, Is.Zero); // 숨은 대체 객체 차단
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 새 출격 전체 회수 시험
    public void ClearAllRecyclesProtectedAndOffscreenShotsButNotTemplate() // 출격 경계의 모든 적탄 정리
    { // 시험 시작
        GameObject holder = new GameObject("Pool"); // 풀 객체
        try // 시험 범위
        { // 범위 시작
            EnemyBulletPool pool = holder.AddComponent<EnemyBulletPool>(); // 실제 풀 구성
            EnemyBullet template = MakeTemplate(holder.transform, BattleSprites.Load("bullet_enemy")); // 비활성 템플릿
            Attach(pool, template); // 템플릿 연결
            EnemyBullet protectedBullet = pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, null, false); // 소거 불가 탄환
            EnemyBullet offscreen = pool.Spawn(new Vector2(30f, 30f), Vector2.zero, 0.06f, 10, -5f, null); // 화면 밖 탄환
            MethodInfo clearAll = typeof(EnemyBulletPool).GetMethod("ClearAll"); // 출격 전체 회수 API
            Assert.That(clearAll, Is.Not.Null); // 전체 회수 제공 확인
            clearAll.Invoke(pool, null); // 출격 경계 정리
            Assert.That(pool.ActiveCount, Is.Zero); // 활성 탄환 전체 회수
            Assert.That(protectedBullet.gameObject.activeSelf, Is.False); // 소거 불가 탄환도 회수
            Assert.That(offscreen.gameObject.activeSelf, Is.False); // 화면 밖 탄환도 회수
            Assert.That(template.gameObject.activeSelf, Is.False); // 템플릿 비활성 유지
            EnemyBullet next = pool.Spawn(Vector2.zero, Vector2.down, 0.06f, 10, -5f, null); // 다음 출격 적탄
            Assert.That(next == protectedBullet || next == offscreen, Is.True); // 기존 탄환 재사용
            Assert.That(next, Is.Not.SameAs(template)); // 원본 템플릿 발사 금지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 풀과 탄환 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
