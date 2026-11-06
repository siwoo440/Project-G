using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 실제 전투 연결
using UnityEditor; // 자산과 직렬화 기능
using UnityEditor.SceneManagement; // 씬 검사 기능
using UnityEngine; // 유니티 객체 기능
public class ScenePlayerVisualTests // 실제 씬 플레이어 이미지 시험
{ // 시험 시작
    [TestCase("player_rapid")] // 아리아 원본 이미지
    [TestCase("player_sniper")] // 리안 원본 이미지
    public void SceneSpriteUsesTheWholeOriginalTexture(string name) // 실제 원본 범위 확인
    { // 시험 시작
        string prefix = "Assets/ProjectG/Resources/BattleArt/" + name; // 기존 이미지 경로
        Texture2D original = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + ".png"); // 기존 PNG 텍스처
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(prefix + "_scene.asset"); // 직렬화된 전체 Sprite
        Assert.That(original, Is.Not.Null); // 기존 이미지 확인
        Assert.That(sprite, Is.Not.Null); // 독립 Sprite 확인
        Assert.That(sprite.texture, Is.SameAs(original)); // 동일한 원본 텍스처
        Assert.That(sprite.rect, Is.EqualTo(new Rect(0f, 0f, original.width, original.height))); // 원본 전체 표시
        Assert.That(sprite.pixelsPerUnit, Is.EqualTo(Mathf.Max(original.width, original.height)).Within(0.001f)); // 기존 표시 단위 유지
        Assert.That(sprite.bounds.size.x, Is.EqualTo(1f).Within(0.001f)); // 기존 원본의 가로 단위
        Assert.That(sprite.bounds.size.y, Is.EqualTo(1f).Within(0.001f)); // 기존 원본의 세로 단위
    } // 시험 종료
    [Test] // 실제 씬 직렬화 크기 시험
    public void SceneReferencesBothFullSpritesAndMaintainsHitboxDiameter() // 캐릭터와 피격 표시 크기
    { // 시험 시작
        var previous = EditorSceneManager.GetSceneManagerSetup(); // 기존 편집 씬 구성
        try // 시험 범위
        { // 범위 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity", OpenSceneMode.Additive); // 실제 전투 씬 열기
            BattleSceneBindings bindings = null; // 실제 씬 연결
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 순회
            { // 확인 시작
                BattleSceneBindings candidate = root.GetComponent<BattleSceneBindings>(); // 실제 연결 조회
                if (candidate != null) // 실제 연결 확인
                { // 보관 시작
                    Assert.That(bindings, Is.Null); // 중복 연결 방지
                    bindings = candidate; // 실제 연결 보관
                } // 보관 종료
            } // 확인 종료
            Assert.That(bindings, Is.Not.Null); // 씬 연결 확인
            SpriteRenderer body = bindings.Player.GetComponent<SpriteRenderer>(); // 실제 아리아 이미지
            Sprite rapid = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectG/Resources/BattleArt/player_rapid_scene.asset"); // 전체 아리아 원본
            Sprite sniper = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectG/Resources/BattleArt/player_sniper_scene.asset"); // 전체 리안 원본
            Assert.That(body.sprite, Is.SameAs(rapid)); // 실제 아리아 연결
            SerializedObject serialized = new SerializedObject(bindings); // 실제 씬 저장 참조
            Assert.That(serialized.FindProperty("sniperSprite").objectReferenceValue, Is.SameAs(sniper)); // 실제 리안 연결
            Assert.That(body.sprite.bounds.size.x * Mathf.Abs(body.transform.lossyScale.x), Is.EqualTo(1.125f).Within(0.001f)); // 기존 플레이어 월드 너비
            HitboxIndicator hitbox = bindings.Player.GetComponentInChildren<HitboxIndicator>(true); // 실제 피격 표시
            SpriteRenderer marker = hitbox.GetComponent<SpriteRenderer>(); // 표시 이미지
            Assert.That(marker.sprite.bounds.size.x * Mathf.Abs(marker.transform.lossyScale.x), Is.EqualTo(0.24f).Within(0.001f)); // 피격 반경 지름
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (previous.Length > 0) // 복원할 씬 확인
            { // 복원 시작
                EditorSceneManager.RestoreSceneManagerSetup(previous); // 기존 씬 구성 복원
            } // 복원 종료
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
