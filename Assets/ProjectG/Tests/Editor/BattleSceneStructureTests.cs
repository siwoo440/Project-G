using System; // 실제 형식 조회
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 객체 기능
using UnityEditor; // 빌드 설정 확인
using UnityEditor.SceneManagement; // 저장된 씬 검사
using UnityEngine; // 씬 객체 기능
public class BattleSceneStructureTests // 직렬화된 실제 전투 씬 시험
{ // 시험 시작
    [Test] // 기존 시험 구성의 실제 프리팹 연결
    public void LegacyTestSetupInjectsAllEnemyTemplates() // 시험 경로의 적 템플릿 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 격리 시험 카메라
        GameObject playerObject = null; // 시험 플레이어
        try // 시험 범위
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 시험 카메라 구성
            playerObject = BattleSetup.Create(camera); // 기존 단위 시험용 전투 구성
            StageDirector stage = cameraObject.GetComponent<StageDirector>(); // 실제 스테이지 참조
            Assert.That(stage, Is.Not.Null); // 스테이지 구성 확인
            Assert.That(stage.HasSceneTemplates, Is.True); // 다섯 실제 템플릿 연결 확인
            Assert.That(stage.GetComponentsInChildren<EnemyActor>(true).Length, Is.EqualTo(5)); // 중복 없는 다섯 원본 확인
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 존재 확인
            { // 플레이어 정리 시작
                UnityEngine.Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
            } // 플레이어 정리 종료
            UnityEngine.Object.DestroyImmediate(cameraObject); // 시험 카메라와 템플릿 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 실제 빌드 씬과 객체 시험
    public void MenuAndBattleScenesContainRealSerializedBattleObjects() // 시작 씬 보존과 전투 씬 확인
    { // 시험 시작
        EditorBuildSettingsScene[] build = EditorBuildSettings.scenes; // 실제 빌드 씬 목록
        Assert.That(build.Length, Is.GreaterThanOrEqualTo(2)); // 메뉴와 전투 씬 포함
        Assert.That(build[0].enabled && build[0].path == "Assets/Scenes/SampleScene.unity", Is.True); // 기존 메뉴 시작 씬 보존
        int found = 0; // 전투 씬 중복 확인
        foreach (EditorBuildSettingsScene entry in build) // 빌드 씬 순회
        { // 순회 시작
            if (entry.enabled && entry.path == "Assets/Scenes/BattleScene.unity") // 활성 전투 씬 확인
            { // 발견 시작
                found++; // 전투 씬 개수 기록
            } // 발견 종료
        } // 순회 종료
        Assert.That(found, Is.EqualTo(1)); // 전투 씬 단일 등록
        var previous = EditorSceneManager.GetSceneManagerSetup(); // 기존 씬 구성 보관
        try // 시험 범위
        { // 범위 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity", OpenSceneMode.Additive); // 저장된 전투 씬 열기
            Type bindingType = typeof(BattlePlayer).Assembly.GetType("ProjectG.BattleSceneBindings"); // 실제 전투 씬 연결 형식
            Assert.That(bindingType, Is.Not.Null); // 연결 구성 확인
            Component bindings = null; // 실제 씬 연결 참조
            foreach (GameObject root in scene.GetRootGameObjects()) // 전투 씬 루트 순회
            { // 순회 시작
                Component candidate = root.GetComponent(bindingType); // 직렬화된 연결 확인
                if (candidate != null) // 연결된 루트 확인
                { // 발견 시작
                    Assert.That(bindings, Is.Null); // 중복 루트 차단
                    bindings = candidate; // 실제 연결 보관
                } // 발견 종료
            } // 순회 종료
            Assert.That(bindings, Is.Not.Null); // 실제 전투 루트 존재
            SerializedObject serialized = new SerializedObject(bindings); // 씬에 저장된 연결
            string[] fields = new string[] // 필수 객체 참조
            { // 목록 시작
                "battleCamera", "player", "background", "stage", "bulletPool", "progress", "drops", "hud" // 실제 전투 객체
            }; // 목록 종료
            foreach (string field in fields) // 필수 참조 순회
            { // 순회 시작
                SerializedProperty property = serialized.FindProperty(field); // 실제 저장된 필드
                Assert.That(property, Is.Not.Null, field); // 필드 존재 확인
                Assert.That(property.objectReferenceValue, Is.Not.Null, field); // 실제 씬 객체 연결
                Component component = property.objectReferenceValue as Component; // 연결된 실제 구성 요소
                Assert.That(component, Is.Not.Null, field); // 컴포넌트 연결 확인
                Assert.That(component.gameObject.scene, Is.EqualTo(scene), field); // 전투 씬 소속 확인
            } // 순회 종료
            EnemyBulletPool pool = serialized.FindProperty("bulletPool").objectReferenceValue as EnemyBulletPool; // 씬의 실제 적탄 풀
            Assert.That(pool.IsConfigured, Is.True); // 비활성 적탄 템플릿 필수 연결
            SerializedObject poolValues = new SerializedObject(pool); // 실제 풀 직렬화 상태
            EnemyBullet template = poolValues.FindProperty("template").objectReferenceValue as EnemyBullet; // 실제 생성에 사용하는 원본
            Assert.That(template, Is.Not.Null); // 씬 원본 참조 확인
            Assert.That(template.gameObject.scene, Is.EqualTo(scene)); // 씬에 저장된 템플릿 확인
            Assert.That(template.gameObject.activeSelf, Is.False); // 원본의 전투 참여 차단
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(template.gameObject), Is.True); // 편집 가능한 프리팹 인스턴스
            StageDirector stage = serialized.FindProperty("stage").objectReferenceValue as StageDirector; // 실제 스테이지 구성
            Assert.That(stage.HasSceneTemplates, Is.True); // 다섯 적 템플릿 연결 확인
            SerializedObject stageValues = new SerializedObject(stage); // 스테이지 직렬화 상태
            string[] enemyFields = new string[] // 실제 적 연결 필드
            { // 목록 시작
                "aimedTemplate", "spreadTemplate", "dashTemplate", "midBossTemplate", "finalBossTemplate" // 다섯 종류 참조
            }; // 목록 종료
            foreach (string field in enemyFields) // 적 템플릿 순회
            { // 확인 시작
                EnemyActor enemy = stageValues.FindProperty(field).objectReferenceValue as EnemyActor; // 실제 적 원본
                Assert.That(enemy, Is.Not.Null, field); // 연결된 원본 확인
                Assert.That(enemy.gameObject.scene, Is.EqualTo(scene), field); // 실제 전투 씬 소속
                Assert.That(enemy.transform.parent, Is.SameAs(stage.transform), field); // 스테이지 직계 자식
                Assert.That(enemy.gameObject.activeSelf, Is.False, field); // 원본의 전투 참여 차단
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(enemy.gameObject), Is.True, field); // 편집 가능한 원본 인스턴스
            } // 확인 종료
        } // 범위 종료
        finally // 기존 씬 복원
        { // 복원 시작
            if (previous.Length > 0) // 복원 대상 확인
            { // 복원 처리 시작
                EditorSceneManager.RestoreSceneManagerSetup(previous); // 이전 씬 구성 복원
            } // 복원 처리 종료
        } // 복원 종료
    } // 시험 종료
} // 시험 종료
