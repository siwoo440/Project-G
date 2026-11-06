using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEditor; // 직렬화 설정 도구
using UnityEngine; // 유니티 객체
public class ExperiencePrefabTests // 경험치 프리팹 시험
{ // 시험 시작
    [Test] // 기본 프리팹 자원 확인
    public void DefaultExperiencePrefabHasEditableSpriteAndScale() // 기본 프리팹 편집 가능 확인
    { // 시험 시작
        ExperienceShard prefab = Resources.Load<ExperienceShard>("Prefabs/ExperienceShard"); // 실제 게임 자원 조회
        Assert.That(prefab, Is.Not.Null); // 기본 프리팹 존재
        SpriteRenderer image = prefab.GetComponentInChildren<SpriteRenderer>(); // 편집 가능한 이미지
        Assert.That(image, Is.Not.Null); // 이미지 구성 확인
        Assert.That(image.sprite, Is.Not.Null); // 기본 이미지 연결 확인
        Sprite sprite = image.sprite; // 프리팹 이미지
        Texture2D texture = Resources.Load<Texture2D>("BattleArt/experience_pickup"); // 기존 텍스처 경로
        Assert.That(sprite.texture, Is.SameAs(texture)); // 기존 텍스처 참조 유지
        Assert.That(sprite.rect.width, Is.EqualTo(texture.width)); // 원본 가로 전체 표시
        Assert.That(sprite.rect.height, Is.EqualTo(texture.height)); // 원본 세로 전체 표시
        Assert.That(sprite.pixelsPerUnit, Is.EqualTo(Mathf.Max(texture.width, texture.height))); // 기존 단위 기준 유지
        Assert.That(prefab.transform.localScale.x * sprite.bounds.size.x, Is.EqualTo(0.9f).Within(0.0001f)); // 기존 세 배 표시 폭 유지
    } // 시험 종료
    [Test] // 실제 기본 경로 시험
    public void DefaultPrefabIsUsedByActualDrop() // 기본 프리팹의 실제 생성 확인
    { // 시험 시작
        ExperienceShard asset = Resources.Load<ExperienceShard>("Prefabs/ExperienceShard"); // 게임의 기본 프리팹
        Assert.That(asset, Is.Not.Null); // 프리팹 존재 확인
        GameObject cameraObject = new GameObject("Camera"); // 전투 카메라 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        try // 시험 범위
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(0f, -3f)); // 플레이어 초기화
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 기본 설정으로 전투 연결
            player.RegisterEnemyDefeat(EnemyRank.Normal, 6, new Vector2(2f, 2f)); // 실제 적 처치 드롭
            ExperienceShard shard = cameraObject.GetComponentInChildren<ExperienceShard>(); // 생성된 조각
            Assert.That(shard, Is.Not.Null); // 실제 드롭 생성
            Assert.That(shard.GetComponentInChildren<SpriteRenderer>().sprite, Is.SameAs(asset.GetComponentInChildren<SpriteRenderer>().sprite)); // 프리팹 이미지 사용
            Assert.That(shard.transform.localScale, Is.EqualTo(asset.transform.localScale)); // 프리팹 크기 사용
            Assert.That(shard.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(1)); // 이미지 구성 중복 방지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 정리
            Object.DestroyImmediate(cameraObject); // 카메라와 조각 정리
        } // 정리 종료
    } // 시험 종료
    [Test] // 편집한 프리팹 생성과 풀 재사용 시험
    public void SpawnUsesEditedAppearanceAndKeepsItWhenReused() // 편집한 이미지와 크기 보존
    { // 시험 시작
        GameObject cameraObject = new GameObject("Camera"); // 전투 카메라 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        GameObject templateObject = new GameObject("ShardTemplate"); // 편집한 프리팹 대체 객체
        try // 시험 범위
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(0f, -3f)); // 플레이어 초기화
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            SpriteRenderer templateImage = templateObject.AddComponent<SpriteRenderer>(); // 편집한 표시 구성
            templateImage.sprite = BattleSprites.Load("player_rapid"); // 사용자가 변경한 이미지
            templateObject.transform.localScale = new Vector3(2.2f, 1.4f, 1f); // 사용자가 변경한 크기
            ExperienceShard template = templateObject.AddComponent<ExperienceShard>(); // 경험치 템플릿
            SerializedObject settings = new SerializedObject(progress); // 직렬화 상태 조회
            SerializedProperty prefab = settings.FindProperty("experiencePrefab"); // 편집 가능한 프리팹 참조
            Assert.That(prefab, Is.Not.Null); // 참조 설정 존재
            prefab.objectReferenceValue = template; // 변경한 템플릿 지정
            settings.ApplyModifiedPropertiesWithoutUndo(); // 참조 적용
            progress.Begin(player, camera); // 전투 성장 연결
            player.RegisterEnemyDefeat(EnemyRank.Normal, 6, new Vector2(2f, 2f)); // 실제 드롭 요청
            ExperienceShard shard = cameraObject.GetComponentInChildren<ExperienceShard>(); // 생성된 조각
            Assert.That(shard, Is.Not.Null); // 조각 생성 확인
            Assert.That(shard.GetComponents<SpriteRenderer>().Length, Is.EqualTo(1)); // 이미지 구성 중복 방지
            Assert.That(shard.GetComponent<SpriteRenderer>().sprite, Is.SameAs(templateImage.sprite)); // 변경 이미지 반영
            Assert.That(shard.transform.localScale, Is.EqualTo(templateObject.transform.localScale)); // 변경 크기 반영
            Assert.That(shard.Take(), Is.EqualTo(6)); // 첫 조각 회수
            player.RegisterEnemyDefeat(EnemyRank.Normal, 4, new Vector2(3f, 2f)); // 다음 드롭 요청
            ExperienceShard reused = cameraObject.GetComponentInChildren<ExperienceShard>(); // 다시 활성화된 조각
            Assert.That(reused, Is.SameAs(shard)); // 기존 조각 재사용
            Assert.That(reused.GetComponent<SpriteRenderer>().sprite, Is.SameAs(templateImage.sprite)); // 재사용 이미지 유지
            Assert.That(reused.transform.localScale, Is.EqualTo(templateObject.transform.localScale)); // 재사용 크기 유지
            Assert.That(reused.AvailableValue, Is.EqualTo(4)); // 재사용 경험치 초기화
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 정리
            Object.DestroyImmediate(cameraObject); // 카메라와 조각 정리
            Object.DestroyImmediate(templateObject); // 시험 템플릿 정리
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
