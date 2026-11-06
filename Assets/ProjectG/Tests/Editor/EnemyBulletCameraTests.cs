using NUnit.Framework; // 시험 도구
using ProjectG; // 적탄과 풀 기능
using UnityEngine; // 유니티 객체
public class EnemyBulletCameraTests // 적탄 카메라 교체 시험
{ // 시험 시작
    [Test] // 동적 카메라 시험
    public void BulletUsesCurrentCameraAfterCameraCreationAndReplacement() // 카메라 추가와 교체 반영
    { // 시험 시작
        GameObject root = new GameObject("BulletCameraPool"); // 격리된 적탄 풀
        try // 시험 범위 준비
        { // 시험 범위 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(root); // 카메라 없는 풀 생성
            EnemyBullet bullet = pool.Spawn(new Vector2(100f, 0f), Vector2.zero, 0.1f, 1, -100f, null); // 좌우 화면 밖 적탄
            bullet.Simulate(0.1f); // 카메라 없는 이탈 검사
            Assert.That(bullet.InUse, Is.True); // 카메라 부재 시 좌우 회수 없음
            Camera first = root.AddComponent<Camera>(); // 첫 카메라 추가
            first.orthographic = true; // 직교 화면 사용
            first.orthographicSize = 5f; // 좁은 화면 높이
            bullet.Simulate(0.1f); // 새 화면 기준 이탈 검사
            Assert.That(bullet.InUse, Is.False); // 화면 밖 적탄 회수
            Object.DestroyImmediate(first); // 첫 카메라 제거
            bullet = pool.Spawn(new Vector2(100f, 0f), Vector2.zero, 0.1f, 1, -100f, null); // 기존 탄환 재사용
            bullet.Simulate(0.1f); // 카메라 제거 후 검사
            Assert.That(bullet.InUse, Is.True); // 삭제된 카메라 제외
            Camera second = root.AddComponent<Camera>(); // 다른 카메라 추가
            second.orthographic = true; // 직교 화면 사용
            second.orthographicSize = 100f; // 적탄을 포함하는 넓은 화면
            second.enabled = false; // 비활성 카메라 설정
            bullet.Simulate(0.1f); // 새로운 화면 기준 검사
            Assert.That(bullet.InUse, Is.True); // 비활성 카메라의 넓은 화면 유지
            second.orthographicSize = 5f; // 새 카메라 화면 축소
            bullet.Simulate(0.1f); // 바뀐 화면 크기 검사
            Assert.That(bullet.InUse, Is.False); // 비활성 카메라의 변경된 화면 밖 회수
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(root); // 풀과 적탄 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
