using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEditor; // 직렬화 설정
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 오류 로그 시험
public class ExperienceInvalidPrefabTests // 잘못된 경험치 프리팹 시험
{ // 시험 시작
    [TestCase(true)] // 불량 객체 정리 시험
    [TestCase(false)] // 불량 프리팹 교체 복구 시험
    public void InvalidPrefabDoesNotPoisonPoolAfterReplacement(bool verifyCleanup) // 잘못된 프리팹 교체 후 회복
    { // 시험 시작
        GameObject cameraObject = new GameObject("Camera"); // 전투 카메라 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        GameObject invalidObject = new GameObject("InvalidShard"); // 이미지 없는 템플릿
        GameObject validObject = new GameObject("ValidShard"); // 정상 템플릿
        bool previousIgnore = LogAssert.ignoreFailingMessages; // 기존 로그 검사 방식
        try // 시험 범위
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 플레이어 초기화
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            ExperienceShard invalid = invalidObject.AddComponent<ExperienceShard>(); // 잘못된 조각 구성
            SpriteRenderer validImage = validObject.AddComponent<SpriteRenderer>(); // 정상 이미지 구성
            validImage.sprite = BattleSprites.Load("player_rapid"); // 교체된 이미지
            ExperienceShard valid = validObject.AddComponent<ExperienceShard>(); // 정상 조각 구성
            SerializedObject settings = new SerializedObject(progress); // 직렬화 상태 조회
            SerializedProperty prefab = settings.FindProperty("experiencePrefab"); // 프리팹 참조 조회
            prefab.objectReferenceValue = invalid; // 불량 프리팹 지정
            settings.ApplyModifiedPropertiesWithoutUndo(); // 불량 참조 적용
            progress.Begin(player, camera); // 전투 연결
            LogAssert.Expect(LogType.Error, "경험치 조각 프리팹의 이미지가 없습니다"); // 첫 오류 확인
            player.RegisterEnemyDefeat(EnemyRank.Normal, 3, new Vector2(2f, 2f)); // 불량 드롭 요청
            if (verifyCleanup) // 불량 인스턴스 정리 확인
            { // 정리 확인 시작
                Assert.That(cameraObject.GetComponentsInChildren<ExperienceShard>(true).Length, Is.Zero); // 불량 인스턴스 정리
                return; // 정리 시험 종료
            } // 정리 확인 종료
            settings.Update(); // 변경된 상태 조회
            prefab.objectReferenceValue = valid; // 정상 프리팹으로 교체
            settings.ApplyModifiedPropertiesWithoutUndo(); // 정상 참조 적용
            LogAssert.ignoreFailingMessages = true; // 잘못된 재사용의 오류를 결과로 확인
            player.RegisterEnemyDefeat(EnemyRank.Normal, 4, new Vector2(2f, 2f)); // 정상 드롭 요청
            ExperienceShard[] shards = cameraObject.GetComponentsInChildren<ExperienceShard>(true); // 남은 조각 조회
            Assert.That(shards.Length, Is.EqualTo(1)); // 정상 조각 하나만 유지
            Assert.That(shards[0].gameObject.activeSelf, Is.True); // 정상 조각 활성화
            Assert.That(shards[0].GetComponent<SpriteRenderer>().sprite, Is.SameAs(validImage.sprite)); // 정상 프리팹 사용
            Assert.That(shards[0].AvailableValue, Is.EqualTo(4)); // 새 조각 경험치 설정
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            LogAssert.ignoreFailingMessages = previousIgnore; // 기존 로그 검사 복원
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Object.DestroyImmediate(invalidObject); // 불량 템플릿 제거
            Object.DestroyImmediate(validObject); // 정상 템플릿 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
