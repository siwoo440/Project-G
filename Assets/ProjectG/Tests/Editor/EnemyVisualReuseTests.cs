using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체
public class EnemyVisualReuseTests // 적 시각 요소 재사용 시험
{ // 시험 시작
    private static int CountChildren(Transform parent, string name) // 이름별 자식 수 확인
    { // 확인 시작
        int count = 0; // 일치 개수
        foreach (Transform child in parent) // 자식 순회
        { // 순회 시작
            if (child.name == name) // 이름 일치 확인
            { // 집계 시작
                count++; // 일치 개수 증가
            } // 집계 종료
        } // 순회 종료
        return count; // 집계 결과 반환
    } // 확인 종료
    [Test] // 적 종류 변경 시험
    public void ReusingEnemyPreservesEditedBodyAndUpdatesDashWarning() // 표시 보존과 중복 방지 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = EnemyActorTestFactory.AttachActor(holder, EnemyKind.Dash); // 적 구성
            enemy.Begin(EnemyKind.Dash, null, null, null); // 돌진 적 초기화
            Transform body = holder.transform.Find("EnemyBody"); // 최초 몸체
            Transform weak = holder.transform.Find("EnemyWeakPoint"); // 최초 약점
            Transform warning = holder.transform.Find("EnemyAttackWarning"); // 최초 돌진 경고
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 최초 체력바
            Assert.That(warning.gameObject.activeSelf, Is.True); // 돌진 경고 표시 확인
            enemy.ApplyDamage(1f, null); // 기존 피해 표시
            enemy.Begin(EnemyKind.FinalBoss, null, null, null); // 최종 보스로 재사용
            Assert.That(CountChildren(holder.transform, "EnemyBody"), Is.EqualTo(1)); // 몸체 중복 방지
            Assert.That(CountChildren(holder.transform, "EnemyWeakPoint"), Is.EqualTo(1)); // 약점 중복 방지
            Assert.That(CountChildren(holder.transform, "EnemyAttackWarning"), Is.EqualTo(1)); // 경고 중복 방지
            Assert.That(CountChildren(holder.transform, "EnemyHealthBar"), Is.EqualTo(1)); // 체력바 중복 방지
            Assert.That(holder.transform.Find("EnemyBody"), Is.SameAs(body)); // 몸체 재사용 확인
            Assert.That(holder.transform.Find("EnemyWeakPoint"), Is.SameAs(weak)); // 약점 재사용 확인
            Assert.That(holder.transform.Find("EnemyHealthBar"), Is.SameAs(bar)); // 체력바 재사용 확인
            Assert.That(body.GetComponent<SpriteRenderer>().sprite.texture.name, Is.EqualTo("enemy_dash")); // 편집 이미지 유지
            Assert.That(body.localScale.x, Is.EqualTo(3f).Within(0.001f)); // 편집 크기 유지
            Assert.That(warning.gameObject.activeSelf, Is.False); // 보스에서 돌진 경고 숨김
            Assert.That(bar.gameObject.activeSelf, Is.False); // 이전 피해 표시 초기화
            enemy.Begin(EnemyKind.Dash, null, null, null); // 다시 돌진 적으로 재사용
            Assert.That(CountChildren(holder.transform, "EnemyBody"), Is.EqualTo(1)); // 반복 재사용 몸체 확인
            Assert.That(CountChildren(holder.transform, "EnemyAttackWarning"), Is.EqualTo(1)); // 반복 재사용 경고 확인
            Assert.That(holder.transform.Find("EnemyAttackWarning"), Is.SameAs(warning)); // 돌진 경고 재사용
            Assert.That(body.GetComponent<SpriteRenderer>().sprite.texture.name, Is.EqualTo("enemy_dash")); // 돌진 이미지 갱신
            Assert.That(body.localScale.x, Is.EqualTo(3f).Within(0.001f)); // 일반 적 크기 복원
            Assert.That(warning.gameObject.activeSelf, Is.True); // 돌진 경고 복원
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
