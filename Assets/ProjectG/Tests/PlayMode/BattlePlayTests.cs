using System.Collections; // 코루틴 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 입력 시스템
using UnityEngine.InputSystem.LowLevel; // 가상 키보드 상태
using UnityEngine.TestTools; // 플레이 테스트 도구
using UnityEngine.SceneManagement; // 장면 전환 기능
using ProjectG; // 전투 화면 기능
public class BattlePlayTests // 실제 전투 흐름 시험
{ // 시험 시작
    [UnityTest] // 플레이 모드 시험
    public IEnumerator KeyboardMovesPlayerFocusesAndFires() // 키 입력과 자동 사격 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 비대화형 게임 입력 허용
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 설정
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 시험 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 추가
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 참조
        player.AssignCamera(camera); // 시험 카메라 연결
        try // 시험 시작
        { // 시험 범위 시작
            yield return null; // 시작 프레임 대기
            float start = playerObject.transform.position.x; // 시작 위치 기록
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.LeftShift)); // 오른쪽과 집중 입력
            InputSystem.Update(); // 가상 입력 처리
            Assert.That(keyboard.dKey.isPressed, Is.True); // 입력 장치 상태 확인
            yield return null; // 입력 처리 대기
            yield return new WaitForSeconds(0.1f); // 이동과 사격 대기
            Assert.That(playerObject.transform.position.x, Is.GreaterThan(start)); // 실제 이동 확인
            Assert.That(player.IsFocused, Is.True); // 실제 집중 확인
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.GreaterThan(0)); // 실제 탄환 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 모든 키 해제
            yield return null; // 해제 처리 대기
            Assert.That(player.IsFocused, Is.False); // 홀드 해제 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 생성 탄환 순회
            { // 탄환 정리 시작
                Object.Destroy(shot.gameObject); // 시험 탄환 제거
            } // 탄환 정리 종료
            Object.Destroy(playerObject); // 시험 플레이어 제거
            Object.Destroy(cameraObject); // 시험 카메라 제거
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 장면 자동 구성 시험
    public IEnumerator SampleSceneCreatesPlayerOnLoad() // 기본 장면 연결 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        yield return null; // 장면 초기화 대기
        Assert.That(Object.FindFirstObjectByType<BattlePlayer>(), Is.Not.Null); // 플레이어 자동 생성 확인
    } // 시험 종료
    [UnityTest] // 시험 적탄 생성 시험
    public IEnumerator SampleSceneSpawnsEnemyBullet() // 적탄 자동 생성 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        yield return new WaitForSeconds(1.5f); // 시험 적탄 생성 대기
        Assert.That(Object.FindFirstObjectByType<EnemyBulletPool>(), Is.Not.Null); // 적탄 풀 존재 확인
        Assert.That(Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Length, Is.GreaterThan(0)); // 활성 적탄 확인
    } // 시험 종료
    [UnityTest] // 실제 피격과 그레이즈 시험
    public IEnumerator EnemyBulletsDamageAndGrazeInPlayMode() // 플레이 모드 생존 흐름 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        yield return null; // 장면 초기화 대기
        BattlePlayer player = Object.FindFirstObjectByType<BattlePlayer>(); // 플레이어 참조
        EnemyBulletPool pool = Object.FindFirstObjectByType<EnemyBulletPool>(); // 적탄 풀 참조
        Assert.That(player, Is.Not.Null); // 플레이어 존재 확인
        Assert.That(pool, Is.Not.Null); // 적탄 풀 존재 확인
        Vector2 position = player.transform.position; // 플레이어 위치
        EnemyBullet near = pool.Spawn(position + new Vector2(0.3f, 0f), Vector2.zero, 0f, 10, -5f, player); // 근접 적탄 생성
        yield return null; // 그레이즈 판정 대기
        Assert.That(player.GrazeCount, Is.EqualTo(1)); // 그레이즈 확인
        pool.Recycle(near); // 근접 적탄 회수
        EnemyBullet hit = pool.Spawn(position, Vector2.zero, 0f, 10, -5f, player); // 피격 적탄 생성
        yield return null; // 피격 판정 대기
        Assert.That(player.Survival.Health, Is.EqualTo(90)); // 체력 감소 확인
        Assert.That(hit.gameObject.activeSelf, Is.False); // 피격 탄환 회수 확인
        EnemyBullet blocked = pool.Spawn(position, Vector2.zero, 0f, 10, -5f, player); // 무적 중 적탄 생성
        yield return null; // 무적 판정 대기
        Assert.That(player.Survival.Health, Is.EqualTo(90)); // 무적 중 중복 피해 차단
        Assert.That(blocked.gameObject.activeSelf, Is.False); // 무적 접촉 회수 확인
    } // 시험 종료
    [UnityTest] // 적탄 예고 시험
    public IEnumerator EnemyShotIsTelegraphedBeforeSpawning() // 적탄 사전 예고 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        yield return null; // 장면 초기화 대기
        Assert.That(GameObject.Find("EnemyShotWarning"), Is.Not.Null); // 예고 표시 확인
        yield return new WaitForSeconds(0.5f); // 사전 경고 대기
        Assert.That(Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Length, Is.Zero); // 조기 발사 방지
        yield return new WaitForSeconds(0.7f); // 발사 시점 대기
        Assert.That(Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Length, Is.GreaterThan(0)); // 예고 후 발사 확인
    } // 시험 종료
    [UnityTest] // 이동·집중·스킬 동시 입력 시험
    public IEnumerator MovementFocusAndThreeSkillsWorkTogether() // 동시 입력 연결 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 게임 입력 허용
        var previousBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 설정
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 시험 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 추가
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 참조
        EnemyBulletPool pool = cameraObject.GetComponent<EnemyBulletPool>(); // 적탄 풀 참조
        try // 시험 시작
        { // 시험 범위 시작
            yield return null; // 시작 프레임 대기
            for (int index = 0; index < 100; index++) // 필살기 자원 충전
            { // 반복 시작
                player.RegisterGraze(); // 그레이즈 충전
            } // 반복 종료
            Vector2 start = playerObject.transform.position; // 시작 위치 기록
            EnemyBullet bullet = pool.Spawn(start + Vector2.right, Vector2.zero, 0.06f, 10, -5f, player); // 화면 안 적탄 생성
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.LeftShift, Key.J, Key.K, Key.L)); // 이동·집중·스킬 동시 입력
            yield return null; // 입력 처리 대기
            yield return new WaitForSeconds(0.05f); // 스킬 처리 대기
            Assert.That(playerObject.transform.position.x, Is.GreaterThan(start.x)); // 이동 유지 확인
            Assert.That(player.IsFocused, Is.True); // 집중 유지 확인
            Assert.That(player.Skills.RapidActive, Is.GreaterThan(0f)); // J 활성 확인
            Assert.That(player.Survival.HasShield, Is.True); // K 보호막 확인
            Assert.That(player.Skills.Energy, Is.Zero); // L 자원 소모 확인
            Assert.That(bullet.gameObject.activeSelf, Is.False); // L 적탄 소거 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 생성 탄환 순회
            { // 정리 시작
                Object.Destroy(shot.gameObject); // 시험 탄환 제거
            } // 정리 종료
            Object.Destroy(playerObject); // 시험 플레이어 제거
            Object.Destroy(cameraObject); // 시험 카메라 제거
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 시험 표적과 자동 사격 연결 시험
    public IEnumerator RapidShotsReachTrainingTargetAndBuildBonus() // 실제 표적 적중 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        yield return null; // 초기화 대기
        BattlePlayer player = Object.FindFirstObjectByType<BattlePlayer>(); // 플레이어 확인
        TrainingTarget target = Object.FindFirstObjectByType<TrainingTarget>(); // 시험 표적 확인
        Assert.That(target, Is.Not.Null); // 표적 생성 확인
        yield return new WaitForSeconds(1.2f); // 실제 탄환 이동 대기
        Assert.That(target.TotalDamage, Is.GreaterThan(0f)); // 표적 피해 확인
        Assert.That(player.Combo.Bonus, Is.GreaterThan(0f)); // 적중 누적 확인
    } // 시험 종료
    [UnityTest] // 캐릭터 선택 키 시험
    public IEnumerator SniperCanBeSelectedWithKeyboard() // 실제 저격형 선택 확인
    { // 시험 시작
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 편집기 입력 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 게임 입력 허용
        var previousBackground = InputSystem.settings.backgroundBehavior; // 백그라운드 입력 설정
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 구성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 확인
        try // 시험 시작
        { // 시험 범위 시작
            yield return null; // 입력 초기화 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F2)); // 저격형 선택 입력
            yield return null; // 입력 처리 대기
            Assert.That(player.Character, Is.EqualTo(CharacterType.Sniper)); // 저격형 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 입력 해제
            yield return null; // 입력 해제 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F1)); // 속사형 선택 입력
            yield return null; // 입력 처리 대기
            Assert.That(player.Character, Is.EqualTo(CharacterType.Rapid)); // 속사형 선택 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 탄환 순회
            { // 순회 시작
                Object.Destroy(shot.gameObject); // 시험 탄환 제거
            } // 순회 종료
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            InputSystem.RemoveDevice(keyboard); // 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 실제 적 등장과 처치 시험
    public IEnumerator EnemyAppearsAndProjectileDefeatRewardsPlayer() // 적 전투 흐름 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        yield return new WaitForSeconds(2.3f); // 시험 적 등장 대기
        EnemyDemoEncounter encounter = Object.FindFirstObjectByType<EnemyDemoEncounter>(); // 적 등장기 확인
        BattlePlayer player = Object.FindFirstObjectByType<BattlePlayer>(); // 플레이어 확인
        Assert.That(encounter, Is.Not.Null); // 등장기 존재 확인
        Assert.That(encounter.Current, Is.Not.Null); // 적 등장 확인
        EnemyActor enemy = encounter.Current; // 현재 적 보관
        int experience = 0; // 보상 기록
        player.ExperienceRewarded += value => experience += value; // 경험치 보상 연결
        int before = player.Skills.Energy; // 기존 에너지 보관
        GameObject shot = new GameObject("TestShot"); // 시험 탄환 생성
        shot.transform.position = enemy.transform.position; // 적 위치에서 시험
        shot.AddComponent<Projectile>().Begin(Vector2.up, 5f, 50, true, player); // 일반 적 처치 탄환
        yield return null; // 탄환 접촉 대기
        yield return null; // 처치 처리 대기
        Assert.That(enemy.IsAlive, Is.False); // 실제 적 처치 확인
        Assert.That(player.Skills.Energy, Is.GreaterThanOrEqualTo(before + 2)); // 처치 에너지 확인
        Assert.That(experience, Is.EqualTo(6)); // 경험치 이벤트 확인
    } // 시험 종료
    [UnityTest] // 확대된 적과 보스 등장 시험
    public IEnumerator EnlargedEnemiesSpawnWithinVerticalScreenBounds() // 적 등장 화면 경계 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 참조
        EnemyDemoEncounter encounter = cameraObject.GetComponent<EnemyDemoEncounter>(); // 적 등장기 참조
        try // 시험 시작
        { // 시험 범위 시작
            encounter.Advance(2.1f); // 첫 적 등장
            for (int index = 0; index < 5; index++) // 적과 보스 순회
            { // 순회 시작
                EnemyActor enemy = encounter.Current; // 현재 적 확인
                Assert.That(enemy, Is.Not.Null); // 적 생성 확인
                Transform body = enemy.transform.Find("EnemyBody"); // 적 몸체 조회
                float top = enemy.transform.position.y + body.lossyScale.y * 0.5f; // 표시 상단 위치
                Assert.That(top, Is.LessThanOrEqualTo(camera.transform.position.y + camera.orthographicSize)); // 화면 상단 이탈 방지
                enemy.ApplyDamage(10000f, player); // 다음 적 진행
                encounter.Advance(1.6f); // 다음 적 등장
            } // 순회 종료
            yield return null; // 정리 프레임 대기
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 탄환 순회
            { // 순회 시작
                Object.Destroy(shot.gameObject); // 시험 탄환 제거
            } // 순회 종료
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
