using NUnit.Framework; // 테스트 도구
using UnityEditor; // 자원 가져오기 검사
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 전투 표시 기능
public class BattleVisualTests // 전투 표시 시험
{ // 시험 시작
    [Test] // 경험치 표시 시험
    public void ExperienceShardUsesTransparentShapedSprite() // 경험치 결정 모양 확인
    { // 시험 시작
        GameObject holder = new GameObject("Shard"); // 조각 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            ExperienceShard shard = holder.AddComponent<ExperienceShard>(); // 조각 구성 요소
            shard.Begin(null, Vector2.zero, 1); // 조각 표시 생성
            Sprite sprite = holder.GetComponent<SpriteRenderer>().sprite; // 표시 이미지 조회
            Assert.That(sprite.texture.width, Is.GreaterThan(2)); // 전용 이미지 크기
            TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite.texture)) as TextureImporter; // 이미지 가져오기 설정
            Assert.That(importer, Is.Not.Null); // 이미지 파일 연결
            Assert.That(importer.DoesSourceTextureHaveAlpha(), Is.True); // 투명 채널 존재
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 조각 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 보스 약점 표시 시험
    public void BossWeakPointRemainsVisibleWithoutProjectileWarning() // 탄막 예고 제거와 약점 유지
    { // 시험 시작
        GameObject holder = new GameObject("Boss"); // 보스 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            EnemyActor boss = holder.AddComponent<EnemyActor>(); // 보스 구성 요소
            boss.Begin(EnemyKind.FinalBoss, null, null, null); // 보스 표시 생성
            Assert.That(holder.transform.Find("EnemyAttackWarning"), Is.Null); // 탄막 경고 표시 제거
            Transform weakObject = holder.transform.Find("EnemyWeakPoint"); // 약점 표시 객체
            Assert.That(weakObject.gameObject.activeSelf, Is.False); // 초기 약점 비노출
            Sprite weak = weakObject.GetComponent<SpriteRenderer>().sprite; // 약점 표시 조회
            Assert.That(weak.texture.width, Is.GreaterThan(2)); // 약점 전용 이미지
            TextureImporter weakImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(weak.texture)) as TextureImporter; // 약점 이미지 설정
            Assert.That(weakImporter, Is.Not.Null); // 약점 이미지 파일 연결
            Assert.That(weakImporter.DoesSourceTextureHaveAlpha(), Is.True); // 약점 투명 채널
            boss.Advance(1.3f); // 첫 패턴 종료까지 진행
            Assert.That(weakObject.gameObject.activeSelf, Is.True); // 회복 중 약점 표시 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 보스 객체 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(EnemyKind.Aimed)] // 조준형 시험
    [TestCase(EnemyKind.Spread)] // 확산형 시험
    [TestCase(EnemyKind.MidBoss)] // 중간 보스 시험
    [TestCase(EnemyKind.FinalBoss)] // 최종 보스 시험
    public void ProjectileEnemiesHaveNoWarningMarker(EnemyKind kind) // 탄막 경고 표시 제거 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 시험 적 객체
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(kind, null, null, null); // 적 초기화
            enemy.Advance(kind == EnemyKind.Aimed ? 1.2f : kind == EnemyKind.Spread ? 1.7f : 0.6f); // 기존 발사 준비 시점
            Assert.That(holder.transform.Find("EnemyAttackWarning"), Is.Null); // 사격 전 경고 객체 없음
        } // 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 돌진 공격 경고 보존 시험
    public void DashWarningRemainsDuringPreparation() // 돌진 공격 경고 유지
    { // 시험 시작
        GameObject holder = new GameObject("Dash"); // 시험 돌진 적
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Dash, null, null, null); // 돌진 적 초기화
            Transform warning = holder.transform.Find("EnemyAttackWarning"); // 돌진 경고 객체
            Assert.That(warning, Is.Not.Null); // 돌진 경고 보존
            Assert.That(warning.gameObject.activeSelf, Is.True); // 준비 중 돌진 경고 표시
            enemy.Advance(0.81f); // 돌진 시작 시간 경과
            Assert.That(warning.gameObject.activeSelf, Is.False); // 돌진 시작 후 경고 숨김
        } // 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 객체 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
