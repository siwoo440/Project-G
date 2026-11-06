using System.Collections.Generic; // 기존 Sprite 식별자 목록
using NUnit.Framework; // 시험 도구
using ProjectG; // 기존 전투 이미지
using UnityEditor; // 에셋 식별자 확인
using UnityEngine; // 유니티 이미지 기능
public class EnemyBulletSpriteAssetTests // 적탄 Sprite 직렬화 시험
{ // 시험 시작
    [Test] // 기존 Sprite 식별자 보존 시험
    public void ExistingSpriteIdentifiersRemainStable() // 원본 분할 이미지 보존 확인
    { // 시험 시작
        const string path = "Assets/ProjectG/Resources/BattleArt/bullet_enemy.png"; // 기존 이미지 경로
        const string expectedGuid = "be3ef1e08abd29b4ea5a6ff1f0305a7a"; // 기존 텍스처 식별자
        long[] expectedIds = new long[] // 원래 분할 Sprite 파일 식별자
        { // 식별자 목록 시작
            2813306821376561297L, 2630409133068385374L, 5559343878543700701L, -4497015415081142893L, // 첫 네 조각
            3408856098700834604L, -7644323076558514121L, 4977519639583158696L, -3095194787636300486L, // 둘째 네 조각
            4743967903891720779L, -255873477524776290L, 514211133777403932L, 4821411142640817190L, // 셋째 네 조각
            6671993862919199702L, 7604982927983319266L, -9041820113404567849L, -6591250184237625659L // 마지막 네 조각
        }; // 식별자 목록 종료
        Rect[] expectedRects = new Rect[] // 기존 분할 이미지 영역
        { // 영역 목록 시작
            new Rect(112f, 25f, 1057f, 1193f), new Rect(514f, 939f, 6f, 6f), new Rect(850f, 883f, 17f, 17f), new Rect(866f, 866f, 16f, 28f), // 첫 네 조각
            new Rect(1037f, 763f, 7f, 10f), new Rect(1061f, 657f, 15f, 24f), new Rect(979f, 624f, 17f, 14f), new Rect(265f, 594f, 9f, 9f), // 둘째 네 조각
            new Rect(338f, 398f, 15f, 14f), new Rect(940f, 393f, 6f, 6f), new Rect(432f, 353f, 9f, 11f), new Rect(866f, 335f, 27f, 20f), // 셋째 네 조각
            new Rect(742f, 306f, 9f, 16f), new Rect(860f, 291f, 11f, 13f), new Rect(508f, 187f, 8f, 7f), new Rect(1163f, 97f, 8f, 12f) // 마지막 네 조각
        }; // 영역 목록 종료
        Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(expectedGuid)); // 기존 텍스처 GUID 유지
        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(); // 가져온 분할 이미지 목록
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path)) // 모든 하위 에셋 순회
        { // 수집 시작
            if (asset is Sprite sprite) // Sprite 에셋 확인
            { // 보관 시작
                sprites[sprite.name] = sprite; // 이름별 Sprite 보관
            } // 보관 종료
        } // 수집 종료
        for (int index = 0; index < expectedIds.Length; index++) // 기존 분할 순회
        { // 확인 시작
            string name = "bullet_enemy_" + index; // 기존 분할 이름
            Assert.That(sprites.TryGetValue(name, out Sprite sprite), Is.True, name); // 기존 분할 존재
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long fileId), Is.True); // 실제 식별자 조회
            Assert.That(guid, Is.EqualTo(expectedGuid)); // 기존 텍스처 GUID 유지
            Assert.That(fileId, Is.EqualTo(expectedIds[index]), name); // 기존 분할 파일 ID 유지
            Assert.That(sprite.rect, Is.EqualTo(expectedRects[index]), name); // 기존 분할 영역 유지
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100f), name); // 기존 픽셀 단위 유지
        } // 확인 종료
    } // 시험 종료
    [Test] // 직렬화 Sprite와 기존 외형 비교 시험
    public void TemplateUsesExistingCropAndWorldDisplayWidth() // 원래 적탄 범위와 표시 폭 유지
    { // 시험 시작
        const string path = "Assets/ProjectG/Resources/Prefabs/EnemyBullet.prefab"; // 새 적탄 프리팹 경로
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); // 실제 프리팹 조회
        Assert.That(prefab, Is.Not.Null); // 프리팹 존재 확인
        Assert.That(prefab.activeSelf, Is.False); // 씬 템플릿 기본 비활성
        SpriteRenderer image = prefab.GetComponentInChildren<SpriteRenderer>(true); // 편집 가능한 탄환 이미지
        Assert.That(image, Is.Not.Null); // 이미지 구성 확인
        Assert.That(image.sprite, Is.Not.Null); // 직렬화 Sprite 확인
        Sprite previous = BattleSprites.Load("bullet_enemy"); // 기존 런타임 이미지
        Assert.That(image.sprite.texture, Is.SameAs(previous.texture)); // 기존 텍스처 사용
        Assert.That(image.sprite.rect.x, Is.EqualTo(previous.rect.x).Within(0.01f)); // 기존 가로 시작점
        Assert.That(image.sprite.rect.y, Is.EqualTo(previous.rect.y).Within(0.01f)); // 기존 세로 시작점
        Assert.That(image.sprite.rect.width, Is.EqualTo(previous.rect.width).Within(0.01f)); // 기존 중앙 가로 범위
        Assert.That(image.sprite.rect.height, Is.EqualTo(previous.rect.height).Within(0.01f)); // 기존 중앙 세로 범위
        float width = image.sprite.bounds.size.x * image.transform.localScale.x * image.transform.parent.localScale.x * prefab.transform.localScale.x; // 초기 표시 월드 폭
        Assert.That(width, Is.EqualTo(0.36f).Within(0.001f)); // 기존 3배 표시 크기
    } // 시험 종료
} // 시험 종료
