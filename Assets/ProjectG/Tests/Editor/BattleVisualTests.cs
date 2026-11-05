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
    [Test] // 보스 경고와 약점 표시 시험
    public void BossMarkersUseTransparentShapedSprites() // 보스 안내 모양 확인
    { // 시험 시작
        GameObject holder = new GameObject("Boss"); // 보스 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            EnemyActor boss = holder.AddComponent<EnemyActor>(); // 보스 구성 요소
            boss.Begin(EnemyKind.FinalBoss, null, null, null); // 보스 표시 생성
            Sprite warning = holder.transform.Find("EnemyAttackWarning").GetComponent<SpriteRenderer>().sprite; // 공격 예고 조회
            Sprite weak = holder.transform.Find("EnemyWeakPoint").GetComponent<SpriteRenderer>().sprite; // 약점 표시 조회
            Assert.That(warning.texture.width, Is.GreaterThan(2)); // 경고 전용 이미지
            TextureImporter warningImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(warning.texture)) as TextureImporter; // 경고 이미지 설정
            Assert.That(warningImporter, Is.Not.Null); // 경고 이미지 파일 연결
            Assert.That(warningImporter.DoesSourceTextureHaveAlpha(), Is.True); // 경고 투명 채널
            Assert.That(weak.texture.width, Is.GreaterThan(2)); // 약점 전용 이미지
            TextureImporter weakImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(weak.texture)) as TextureImporter; // 약점 이미지 설정
            Assert.That(weakImporter, Is.Not.Null); // 약점 이미지 파일 연결
            Assert.That(weakImporter.DoesSourceTextureHaveAlpha(), Is.True); // 약점 투명 채널
            Assert.That(boss.IsTelegraphing, Is.True); // 공격 예고 상태 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 보스 객체 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
