using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 적탄 풀 기능
using UnityEngine; // 유니티 객체 기능
using UnityEngine.TestTools; // 오류 로그 확인
public class EnemyBulletBoundaryTests // 적탄 풀 경계 시험
{ // 시험 시작
    [Test] // 비활성 부모 회수 시험
    public void ClearAllRecyclesProtectedAndOffscreenBulletsWhileParentInactive() // 비활성 풀 전체 회수
    { // 시험 시작
        GameObject holder = new GameObject("Pool"); // 풀 객체
        try // 시험 범위
        { // 범위 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(holder); // 비활성 템플릿 풀
            EnemyBullet first = pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, null, false); // 소거 불가 탄환
            EnemyBullet second = pool.Spawn(new Vector2(30f, 30f), Vector2.zero, 0.06f, 10, -5f, null); // 화면 밖 탄환
            holder.SetActive(false); // 부모 오브젝트 비활성화
            Assert.That(pool.ClearAll(), Is.EqualTo(2)); // 비활성 계층 전체 회수
            Assert.That(first.InUse, Is.False); // 첫 탄환 사용 종료
            Assert.That(second.InUse, Is.False); // 둘째 탄환 사용 종료
            Assert.That(first.gameObject.activeSelf, Is.False); // 첫 탄환 개별 비활성화
            Assert.That(second.gameObject.activeSelf, Is.False); // 둘째 탄환 개별 비활성화
            holder.SetActive(true); // 부모 재활성화
            Assert.That(pool.ActiveCount, Is.Zero); // 이전 탄환 재등장 방지
            EnemyBullet next = pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, null); // 다음 탄환 발사
            Assert.That(next == first || next == second, Is.True); // 회수된 탄환 재사용
            Assert.That(pool.ActiveCount, Is.EqualTo(1)); // 새 탄환만 활성화
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 풀과 탄환 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 템플릿 부모 배율 시험
    public void TemplateMustStayDirectlyUnderPoolToPreserveParentScale() // 템플릿 직접 자식 계약
    { // 시험 시작
        GameObject holder = new GameObject("Pool"); // 풀 객체
        try // 시험 범위
        { // 범위 시작
            holder.transform.localScale = new Vector3(2f, 3f, 1f); // 풀의 편집 배율
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(holder); // 직접 자식 템플릿 풀
            EnemyBullet template = holder.GetComponentInChildren<EnemyBullet>(true); // 비활성 템플릿 조회
            EnemyBullet shot = pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, null); // 편집 탄환 발사
            SpriteRenderer original = template.GetComponentInChildren<SpriteRenderer>(true); // 템플릿 이미지
            SpriteRenderer clone = shot.GetComponentInChildren<SpriteRenderer>(); // 복제 탄환 이미지
            Assert.That(clone.transform.lossyScale.x, Is.EqualTo(original.transform.lossyScale.x).Within(0.0001f)); // 부모 가로 배율 일치
            Assert.That(clone.transform.lossyScale.y, Is.EqualTo(original.transform.lossyScale.y).Within(0.0001f)); // 부모 세로 배율 일치
            pool.Recycle(shot); // 기존 탄환 회수
            GameObject intermediate = new GameObject("Intermediate"); // 별도 중간 부모
            intermediate.transform.SetParent(holder.transform, false); // 풀 아래 중간 부모 배치
            intermediate.transform.localScale = Vector3.one * 2f; // 중간 부모 배율 변경
            template.transform.SetParent(intermediate.transform, false); // 템플릿 위치 변경
            Assert.That(pool.IsConfigured, Is.False); // 잘못된 부모 연결 차단
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("적탄.*템플릿")); // 부모 변경 오류 확인
            Assert.That(pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, null), Is.Null); // 잘못된 배율 발사 차단
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 풀과 탄환 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
