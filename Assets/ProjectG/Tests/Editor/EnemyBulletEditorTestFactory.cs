using NUnit.Framework; // 시험 단언 기능
using ProjectG; // 적탄 풀 기능
using UnityEngine; // 유니티 객체 기능
internal static class EnemyBulletEditorTestFactory // 편집 시험용 적탄 템플릿
{ // 도우미 시작
    public static EnemyBulletPool Create(GameObject owner) // 격리된 풀과 템플릿 생성
    { // 생성 시작
        EnemyBullet asset = Resources.Load<EnemyBullet>("Prefabs/EnemyBullet"); // 실제 적탄 프리팹
        Assert.That(asset, Is.Not.Null); // 시험 자원 누락 확인
        EnemyBullet template = Object.Instantiate(asset, owner.transform, false); // 비활성 템플릿 인스턴스
        EnemyBulletPool pool = owner.AddComponent<EnemyBulletPool>(); // 시험 풀 구성
        Assert.That(pool.SetTemplate(template), Is.True); // 명시적 템플릿 연결
        return pool; // 연결된 시험 풀 반환
    } // 생성 종료
} // 도우미 종료
