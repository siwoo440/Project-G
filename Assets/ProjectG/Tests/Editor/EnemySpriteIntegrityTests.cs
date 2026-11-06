using System.Collections.Generic; // 원본 Sprite 목록
using System.IO; // 재시작 전 기록 조회
using NUnit.Framework; // 시험 기능
using ProjectG; // 적 프리팹 기능
using UnityEditor; // 에셋과 식별자 조회
using UnityEngine; // 이미지와 색상
public class EnemySpriteIntegrityTests // 흰색 Sprite와 기존 식별자 시험
{ // 시험 시작
    [System.Serializable] // 기록 역직렬화
    private sealed class SpriteIdentity // 원본 Sprite 식별자
    { // 기록 시작
        public string path; // 기존 이미지 경로
        public string name; // 기존 Sprite 이름
        public string guid; // 기존 GUID
        public long id; // 기존 로컬 식별자
        public Rect rect; // 기존 Sprite 영역
        public float pixelsPerUnit; // 기존 픽셀 비율
    } // 기록 종료
    [System.Serializable] // 기록 목록 역직렬화
    private sealed class SpriteSnapshot // 기존 Sprite 기록
    { // 기록 시작
        public List<SpriteIdentity> entries; // 전체 원본 식별자
    } // 기록 종료
    [Test] // 재시작 후 픽셀과 프리팹 참조 시험
    public void PersistentWhitePixelAndAllPrefabReferencesSurviveRestart() // 단색 픽셀과 연결 보존
    { // 시험 시작
        const string path = "Assets/ProjectG/Resources/EnemyTemplates/white_persistent.png"; // 신규 흰색 이미지
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); // 재시작 후 텍스처
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); // 재시작 후 Sprite
        Assert.That(texture, Is.Not.Null); // 텍스처 보존
        Assert.That(sprite, Is.Not.Null); // Sprite 보존
        Assert.That(sprite.texture, Is.SameAs(texture)); // 실제 텍스처 참조
        Assert.That(texture.width, Is.EqualTo(1)); // 일 픽셀 폭
        Assert.That(texture.height, Is.EqualTo(1)); // 일 픽셀 높이
        Color pixel = texture.GetPixel(0, 0); // 실제 저장 픽셀
        Assert.That(pixel.r, Is.EqualTo(1f).Within(0.001f)); // 빨간색 최대
        Assert.That(pixel.g, Is.EqualTo(1f).Within(0.001f)); // 초록색 최대
        Assert.That(pixel.b, Is.EqualTo(1f).Within(0.001f)); // 파란색 최대
        Assert.That(pixel.a, Is.EqualTo(1f).Within(0.001f)); // 불투명 알파
        string[] names = // 실제 프리팹 종류
        { // 배열 시작
            "Aimed", "Spread", "Dash", "MidBoss", "FinalBoss" // 배열 항목
        }; // 배열 종료
        foreach (string name in names) // 종류별 확인
        { // 확인 시작
            EnemyActor prefab = Resources.Load<EnemyActor>("EnemyTemplates/" + name); // 저장된 실제 프리팹
            Assert.That(prefab, Is.Not.Null); // 프리팹 보존
            SpriteRenderer fill = prefab.transform.Find("EnemyHealthBar/Fill").GetComponent<SpriteRenderer>(); // 체력바 채움
            Assert.That(fill.sprite, Is.SameAs(sprite)); // 체력바 Sprite 참조
            if (name == "Dash") // 돌진형 확인
            { // 경로 확인 시작
                SpriteRenderer pathImage = prefab.transform.Find("EnemyDashPath").GetComponent<SpriteRenderer>(); // 돌진 경로
                Assert.That(pathImage.sprite, Is.SameAs(sprite)); // 경로 Sprite 참조
            } // 경로 확인 종료
        } // 종류 확인 종료
    } // 시험 종료
    [Test] // 재시작 후 기존 식별자 시험
    public void ExistingSpriteIdentifiersAndGeometrySurviveRestart() // 원본 Sprite 식별자 보존
    { // 시험 시작
        TextAsset fixture = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ProjectG/Tests/Editor/EnemyOriginalSpriteIds.json"); // 저장된 원본 Sprite 기준
        Assert.That(fixture, Is.Not.Null); // 기존 식별자 기록 확인
        SpriteSnapshot before = JsonUtility.FromJson<SpriteSnapshot>(fixture.text); // 생성 전 식별자 목록
        Assert.That(before.entries.Count, Is.GreaterThan(0)); // 실제 원본 Sprite 존재
        Dictionary<string, SpriteIdentity> expected = new Dictionary<string, SpriteIdentity>(); // 식별자 비교표
        foreach (SpriteIdentity entry in before.entries) // 기존 기록 순회
        { // 기록 시작
            expected.Add(entry.guid + ":" + entry.id, entry); // GUID와 로컬 ID 조합
        } // 기록 종료
        string[] names = // 기존 이미지 목록
        { // 배열 시작
            "ui_card_frame", "weakpoint_reticle", "attack_warning", "enemy_aimed", "enemy_spread", "enemy_dash", "boss_mid", "boss_final" // 배열 항목
        }; // 배열 종료
        int count = 0; // 확인한 Sprite 수
        foreach (string name in names) // 기존 이미지 순회
        { // 이미지 순회 시작
            string path = "Assets/ProjectG/Resources/BattleArt/" + name + ".png"; // 기존 이미지 경로
            foreach (Object item in AssetDatabase.LoadAllAssetsAtPath(path)) // 원본 하위 에셋 순회
            { // 하위 순회 시작
                Sprite sprite = item as Sprite; // Sprite 유형 확인
                if (sprite == null) // 다른 유형 확인
                { // 생략 시작
                    continue; // Sprite 외 에셋 생략
                } // 생략 종료
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id), Is.True); // 실제 식별자 확인
                string key = guid + ":" + id; // 원본 식별자 조합
                Assert.That(expected.ContainsKey(key), Is.True, path + " " + sprite.name); // 이전 식별자 존재
                SpriteIdentity prior = expected[key]; // 생성 전 기록
                Assert.That(prior.path, Is.EqualTo(path)); // 기존 원본 경로
                Assert.That(prior.name, Is.EqualTo(sprite.name)); // 기존 Sprite 이름
                Assert.That(prior.rect, Is.EqualTo(sprite.rect)); // 기존 Sprite 영역
                Assert.That(prior.pixelsPerUnit, Is.EqualTo(sprite.pixelsPerUnit).Within(0.001f)); // 기존 픽셀 비율
                count++; // 실제 확인 수 증가
            } // 하위 순회 종료
        } // 이미지 순회 종료
        Assert.That(count, Is.EqualTo(before.entries.Count)); // 기존 Sprite 누락 방지
    } // 시험 종료
} // 시험 종료
