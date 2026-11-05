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
    private static GameSessionHost StartSceneBattle() // 타이틀에서 시험 출격 시작
    { // 출격 시작
        GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 화면 연결 조회
        Assert.That(host, Is.Not.Null); // 타이틀 구성 확인
        Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
        Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
        Assert.That(host.Flow.StartRun(), Is.True); // 시험 출격 시작
        return host; // 화면 연결 반환
    } // 출격 종료
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
    public IEnumerator SampleSceneStartsAtTitleBeforeBattle() // 타이틀과 출격 연결 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        yield return null; // 장면 초기화 대기
        GameSessionHost host = Object.FindFirstObjectByType<GameSessionHost>(); // 타이틀 흐름 조회
        Assert.That(host, Is.Not.Null); // 타이틀 구성 확인
        Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 타이틀 첫 화면
        Assert.That(host.CurrentPlayer, Is.Null); // 출격 전 플레이어 없음
        StartSceneBattle(); // 시험 출격
        Assert.That(host.CurrentPlayer, Is.Not.Null); // 출격 시 플레이어 생성
    } // 시험 종료
    [UnityTest] // 시험 적탄 생성 시험
    public IEnumerator SampleSceneSpawnsEnemyBullet() // 적탄 자동 생성 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        StartSceneBattle(); // 타이틀에서 출격
        yield return new WaitForSeconds(3.8f); // 첫 적 등장과 사격 대기
        Assert.That(Object.FindFirstObjectByType<EnemyBulletPool>(), Is.Not.Null); // 적탄 풀 존재 확인
        Assert.That(Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Length, Is.GreaterThan(0)); // 활성 적탄 확인
    } // 시험 종료
    [UnityTest] // 실제 피격과 그레이즈 시험
    public IEnumerator EnemyBulletsDamageAndGrazeInPlayMode() // 플레이 모드 생존 흐름 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        StartSceneBattle(); // 타이틀에서 출격
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
        StartSceneBattle(); // 타이틀에서 출격
        yield return new WaitForSeconds(3.2f); // 첫 적 공격 예고 대기
        EnemyActor enemy = Object.FindFirstObjectByType<EnemyActor>(); // 현재 웨이브 적 조회
        Assert.That(enemy, Is.Not.Null); // 적 등장 확인
        Assert.That(enemy.IsTelegraphing, Is.True); // 공격 예고 상태 확인
        Assert.That(enemy.transform.Find("EnemyAttackWarning").gameObject.activeSelf, Is.True); // 경고 이미지 표시 확인
        Assert.That(Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Length, Is.Zero); // 조기 발사 방지
        yield return new WaitForSeconds(0.5f); // 발사 시점 대기
        Assert.That(Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None).Length, Is.GreaterThan(0)); // 예고 후 발사 확인
    } // 시험 종료
    [UnityTest] // 이동·집중·스킬 동시 입력 시험
    public IEnumerator MovementFocusAndThreeSkillsWorkTogether() // 동시 입력 연결 확인
    { // 시험 시작
        CombatTarget[] previousTargets = Object.FindObjectsByType<CombatTarget>(FindObjectsSortMode.None); // 다른 시험의 기존 표적
        foreach (CombatTarget target in previousTargets) // 기존 표적 순회
        { // 격리 시작
            target.gameObject.SetActive(false); // 필살기 시험에서 제외
        } // 격리 종료
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
            foreach (CombatTarget target in previousTargets) // 이전 표적 순회
            { // 복원 시작
                if (target != null) // 표적 존재 확인
                { // 복원 처리 시작
                    target.gameObject.SetActive(true); // 이전 표적 복원
                } // 복원 처리 종료
            } // 복원 종료
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 설정 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 시험 표적과 자동 사격 연결 시험
    public IEnumerator RapidShotsReachTrainingTargetAndBuildBonus() // 실제 표적 적중 확인
    { // 시험 시작
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 기본 장면 불러오기
        StartSceneBattle(); // 타이틀에서 출격
        yield return null; // 초기화 대기
        BattlePlayer player = Object.FindFirstObjectByType<BattlePlayer>(); // 플레이어 확인
        TrainingTarget target = TrainingTarget.Create(new Vector2(0f, 2.5f)); // 독립 시험 표적 생성
        target.transform.SetParent(player.transform.parent, true); // 현재 출격 객체에 표적 연결
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
        StartSceneBattle(); // 타이틀에서 출격
        yield return new WaitForSeconds(2.3f); // 시험 적 등장 대기
        StageDirector encounter = Object.FindFirstObjectByType<StageDirector>(); // 스테이지 등장기 확인
        BattlePlayer player = Object.FindFirstObjectByType<BattlePlayer>(); // 플레이어 확인
        Assert.That(encounter, Is.Not.Null); // 등장기 존재 확인
        EnemyActor enemy = encounter.GetComponentInChildren<EnemyActor>(); // 현재 적 조회
        Assert.That(enemy, Is.Not.Null); // 적 등장 확인
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
        StageDirector encounter = cameraObject.GetComponent<StageDirector>(); // 스테이지 등장기 참조
        UpgradeItemSelection selection = cameraObject.GetComponent<UpgradeItemSelection>(); // 강화 선택 참조
        try // 시험 시작
        { // 시험 범위 시작
            encounter.Advance(2.1f, 2.1f); // 첫 적 등장
            EnemyActor first = encounter.GetComponentInChildren<EnemyActor>(); // 일반 적 조회
            Assert.That(first, Is.Not.Null); // 일반 적 생성 확인
            Transform firstBody = first.transform.Find("EnemyBody"); // 일반 적 몸체 조회
            Assert.That(first.transform.position.y + firstBody.lossyScale.y * 0.5f, Is.LessThanOrEqualTo(camera.transform.position.y + camera.orthographicSize)); // 일반 적 화면 경계
            encounter.Advance(63f, 63f); // 첫 강화 구간 진입
            Assert.That(selection.TryChoose(0), Is.True); // 첫 강화 선택
            encounter.Advance(2.1f, 2.1f); // 중간 보스 등장
            for (int index = 0; index < 2; index++) // 두 보스 순회
            { // 순회 시작
                EnemyActor enemy = encounter.Boss; // 현재 보스 확인
                Assert.That(enemy, Is.Not.Null); // 적 생성 확인
                Transform body = enemy.transform.Find("EnemyBody"); // 적 몸체 조회
                float top = enemy.transform.position.y + body.lossyScale.y * 0.5f; // 표시 상단 위치
                Assert.That(top, Is.LessThanOrEqualTo(camera.transform.position.y + camera.orthographicSize)); // 화면 상단 이탈 방지
                enemy.ApplyDamage(10000f, player); // 다음 적 진행
                enemy.ApplyDamage(10000f, player); // 다음 체력 단계 진행
                enemy.ApplyDamage(10000f, player); // 마지막 체력 단계 진행
                encounter.Advance(0.1f, 0.1f); // 보스 처치 처리
                if (index == 0) // 중간 보스 종료 확인
                { // 다음 보스 시작
                    encounter.Advance(50f, 50f); // 두 번째 일반 전투 진행
                    Assert.That(selection.TryChoose(0), Is.True); // 둘째 강화 선택
                    encounter.Advance(2.1f, 2.1f); // 최종 보스 등장
                } // 다음 보스 종료
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
    [UnityTest] // 실제 키보드 레벨업 선택 시험
    public IEnumerator LevelUpPausesAndKeyboardSelectsCardsInOrder() // 연속 카드 선택 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 편집기 입력 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 게임 입력 허용
        var previousBackground = InputSystem.settings.backgroundBehavior; // 백그라운드 입력 설정
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 조회
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            yield return null; // 초기화 대기
            progress.CollectExperience(30); // 두 번 레벨업
            Assert.That(Time.timeScale, Is.Zero); // 전투 정지 확인
            for (int choice = 0; choice < 2; choice++) // 두 번 선택
            { // 선택 반복 시작
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1)); // 첫 카드 선택
                yield return null; // 입력 처리 대기
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
                yield return null; // 해제 처리 대기
                if (progress.ConfirmingSwap) // 스킬 교체 확인 여부
                { // 확인 시작
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 교체 확인 입력
                    yield return null; // 확인 처리 대기
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
                    yield return null; // 해제 처리 대기
                } // 확인 종료
                Assert.That(progress.Level.PendingChoices, Is.EqualTo(1 - choice)); // 보상 순서 확인
            } // 선택 반복 종료
            Assert.That(progress.Offer, Is.Null); // 선택 화면 종료
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 재개
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            InputSystem.RemoveDevice(keyboard); // 키보드 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 설정 복원
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 선택 화면 중 구성 요소 제거 시험
    public IEnumerator DestroyingProgressDuringChoiceRestoresTimeScale() // 실제 정리 시 시간 복원
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 조회
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 0.5f; // 기존 게임 속도
            yield return null; // 초기화 대기
            progress.CollectExperience(10); // 레벨업 선택
            Assert.That(Time.timeScale, Is.Zero); // 선택 중 전투 정지
            Object.Destroy(progress); // 성장 구성 제거
            yield return null; // 제거 처리 대기
            Assert.That(Time.timeScale, Is.EqualTo(0.5f)); // 이전 시간 배율 복원
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 적 경험치 조각 연결 시험
    public IEnumerator EnemyDefeatDropsExperienceBeforePickup() // 처치와 수집 분리 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        GameObject enemyObject = new GameObject("TestEnemy"); // 시험 적 생성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 참조
        BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 조회
        try // 시험 시작
        { // 시험 범위 시작
            enemyObject.transform.position = playerObject.transform.position + Vector3.up * 3f; // 회수 범위 밖 적 위치
            EnemyActor enemy = enemyObject.AddComponent<EnemyActor>(); // 적 구성 요소
            enemy.Begin(EnemyKind.Aimed, player, cameraObject.GetComponent<EnemyBulletPool>(), camera); // 일반 적 초기화
            enemy.ApplyDamage(100f, player); // 적 처치
            ExperienceShard shard = Object.FindFirstObjectByType<ExperienceShard>(); // 경험치 조각 조회
            Assert.That(shard, Is.Not.Null); // 경험치 조각 생성 확인
            Assert.That(progress.Level.Experience, Is.Zero); // 즉시 경험치 지급 방지
            shard.transform.position = playerObject.transform.position + Vector3.right * 0.1f; // 회수 범위로 이동
            yield return null; // 자동 수집 대기
            yield return null; // 수집 처리 대기
            Assert.That(progress.Level.Experience, Is.EqualTo(6)); // 조각 수집 보상 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.Destroy(enemyObject); // 시험 적 제거
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 실제 포인터 카드 선택 시험
    public IEnumerator LevelUpAcceptsMouseSelectionWhilePaused() // 포인터 선택 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 편집기 입력 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 게임 입력 허용
        var previousBackground = InputSystem.settings.backgroundBehavior; // 백그라운드 입력 설정
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Mouse mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 조회
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            yield return null; // 초기화 대기
            progress.CollectExperience(10); // 카드 선택 열기
            Assert.That(Time.timeScale, Is.Zero); // 전투 정지 확인
            int index = System.Array.FindIndex(progress.Offer, card => card.Id != CardId.C06); // 교체 없는 카드 선택
            Rect pixels = camera.pixelRect; // 카메라 화면 영역
            Rect view = new Rect(pixels.xMin, Screen.height - pixels.yMax, pixels.width, pixels.height); // 화면 좌표 변환
            Vector2 point = CardUiLayout.Card(view, index).center; // 선택 카드 중심
            MouseState state = new MouseState(); // 마우스 상태 생성
            state.position = new Vector2(point.x, Screen.height - point.y); // 화면 좌표 설정
            state = state.WithButton(MouseButton.Left); // 왼쪽 버튼 누르기
            InputSystem.QueueStateEvent(mouse, state); // 포인터 입력 전달
            yield return null; // 입력 처리 대기
            Assert.That(progress.Level.PendingChoices, Is.Zero); // 카드 선택 확인
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 재개 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            InputSystem.RemoveDevice(mouse); // 마우스 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 설정 복원
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 포인터 스킬 교체 확인 시험
    public IEnumerator MouseCanCancelAndConfirmSkillReplacement() // 포인터 교체 흐름 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 편집기 입력 설정
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 게임 입력 허용
        var previousBackground = InputSystem.settings.backgroundBehavior; // 백그라운드 입력 설정
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        Mouse mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 조회
        BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 조회
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            yield return null; // 초기화 대기
            progress.CollectExperience(10); // 카드 선택 열기
            progress.Offer[0] = CardCatalog.Find(CardId.C06); // 교체 카드 설정
            Rect view = CardUiLayout.View(camera, Screen.height); // 화면 좌표 변환
            Vector2 point = CardUiLayout.Card(view, 0).center; // 교체 카드 위치
            MouseState state = new MouseState(); // 마우스 상태
            state.position = new Vector2(point.x, Screen.height - point.y); // 카드 화면 위치
            InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left)); // 카드 클릭
            yield return null; // 입력 처리 대기
            Assert.That(progress.ConfirmingSwap, Is.True); // 교체 확인 표시
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 버튼 해제
            yield return null; // 해제 처리 대기
            point = CardUiLayout.Cancel(view).center; // 취소 버튼 위치
            state.position = new Vector2(point.x, Screen.height - point.y); // 취소 화면 위치
            InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left)); // 취소 클릭
            yield return null; // 취소 처리 대기
            Assert.That(progress.ConfirmingSwap, Is.False); // 확인 화면 닫힘
            Assert.That(player.Skills.Common, Is.EqualTo(CommonSkill.Shield)); // 기존 스킬 유지
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 버튼 해제
            yield return null; // 해제 처리 대기
            point = CardUiLayout.Card(view, 0).center; // 교체 카드 위치
            state.position = new Vector2(point.x, Screen.height - point.y); // 카드 화면 위치
            InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left)); // 카드 클릭
            yield return null; // 입력 처리 대기
            Assert.That(progress.ConfirmingSwap, Is.True); // 교체 재확인
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 버튼 해제
            yield return null; // 해제 처리 대기
            point = CardUiLayout.Confirm(view).center; // 확인 버튼 위치
            state.position = new Vector2(point.x, Screen.height - point.y); // 확인 화면 위치
            InputSystem.QueueStateEvent(mouse, state.WithButton(MouseButton.Left)); // 확인 클릭
            yield return null; // 확인 처리 대기
            Assert.That(player.Skills.Common, Is.EqualTo(CommonSkill.Scatter)); // 산탄 교체 확인
            Assert.That(progress.Level.PendingChoices, Is.Zero); // 카드 선택 완료
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 재개 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            InputSystem.RemoveDevice(mouse); // 마우스 제거
            InputSystem.settings.backgroundBehavior = previousBackground; // 입력 설정 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput; // 편집기 입력 설정 복원
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 레벨업 직후 탄막 정지 시험
    public IEnumerator LevelUpImmediatelyStopsEnemyBullets() // 같은 프레임 정지 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 생성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 참조
        BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 조회
        EnemyBulletPool pool = cameraObject.GetComponent<EnemyBulletPool>(); // 적탄 풀 조회
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            yield return null; // 초기 프레임 대기
            EnemyBullet bullet = pool.Spawn(new Vector2(2f, 2f), Vector2.down, 0.06f, 10, -5f, player); // 시험 적탄 생성
            GameObject shotObject = new GameObject("PauseShot"); // 시험 아군탄 생성
            shotObject.transform.SetParent(cameraObject.transform, false); // 시험 장면에 연결
            shotObject.transform.position = new Vector2(4f, 0f); // 아군탄 위치
            Projectile shot = shotObject.AddComponent<Projectile>(); // 아군탄 구성
            shot.Begin(Vector2.up, 5f); // 아군탄 초기화
            GameObject enemyObject = new GameObject("PauseEnemy"); // 시험 적 생성
            enemyObject.transform.SetParent(cameraObject.transform, false); // 시험 장면에 연결
            enemyObject.transform.position = new Vector2(-4f, 2f); // 적 위치
            EnemyActor enemy = enemyObject.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Aimed, player, pool, camera); // 적 초기화
            Vector2 before = bullet.transform.position; // 정지 전 적탄 위치
            Vector2 shotBefore = shot.transform.position; // 정지 전 아군탄 위치
            Vector2 enemyBefore = enemy.transform.position; // 정지 전 적 위치
            progress.CollectExperience(10); // 즉시 카드 선택 열기
            Assert.That(Time.timeScale, Is.Zero); // 전투 정지 확인
            bullet.SendMessage("LateUpdate"); // 같은 프레임 적탄 처리 시도
            shot.SendMessage("Update"); // 같은 프레임 아군탄 처리 시도
            enemy.SendMessage("Update"); // 같은 프레임 적 처리 시도
            Assert.That((Vector2)bullet.transform.position, Is.EqualTo(before)); // 적탄 즉시 정지
            Assert.That((Vector2)shot.transform.position, Is.EqualTo(shotBefore)); // 아군탄 즉시 정지
            Assert.That((Vector2)enemy.transform.position, Is.EqualTo(enemyBefore)); // 적 즉시 정지
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 처치부터 레벨업까지 통합 시험
    public IEnumerator DefeatsPickupAndCardSelectionResumeBattle() // 경험치부터 전투 복귀
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject playerObject = BattleSetup.Create(camera); // 시험 전투 구성
        BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 플레이어 참조
        BattleProgress progress = playerObject.GetComponent<BattleProgress>(); // 성장 상태 참조
        GameObject firstObject = new GameObject("EnemyOne"); // 첫 적 생성
        GameObject secondObject = new GameObject("EnemyTwo"); // 둘째 적 생성
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            firstObject.transform.position = playerObject.transform.position + Vector3.right * 0.1f; // 첫 위치
            secondObject.transform.position = playerObject.transform.position + Vector3.left * 0.1f; // 둘째 위치
            EnemyActor first = firstObject.AddComponent<EnemyActor>(); // 첫 적 구성
            EnemyActor second = secondObject.AddComponent<EnemyActor>(); // 둘째 적 구성
            first.Begin(EnemyKind.Aimed, player, cameraObject.GetComponent<EnemyBulletPool>(), camera); // 첫 적 시작
            second.Begin(EnemyKind.Aimed, player, cameraObject.GetComponent<EnemyBulletPool>(), camera); // 둘째 적 시작
            first.ApplyDamage(100f, player); // 첫 적 처치
            second.ApplyDamage(100f, player); // 둘째 적 처치
            yield return null; // 경험치 흡수 대기
            yield return null; // 카드 화면 대기
            Assert.That(progress.Level.Level, Is.EqualTo(2)); // 레벨 증가 확인
            Assert.That(progress.Level.Experience, Is.EqualTo(2)); // 이월 경험치 확인
            Assert.That(progress.Level.PendingChoices, Is.EqualTo(1)); // 선택 예약 확인
            Assert.That(progress.Offer, Is.Not.Null); // 카드 화면 확인
            Assert.That(Time.timeScale, Is.Zero); // 전투 정지 확인
            int index = System.Array.FindIndex(progress.Offer, card => card.Id != CardId.C06); // 교체 없는 선택지
            CardId selected = progress.Offer[index].Id; // 선택 카드 기록
            Assert.That(progress.Select(index), Is.True); // 카드 적용
            Assert.That(player.Build.Cards.Count(selected), Is.EqualTo(1)); // 효과 보관 확인
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 재개 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.Destroy(firstObject); // 첫 적 제거
            Object.Destroy(secondObject); // 둘째 적 제거
            Object.Destroy(playerObject); // 플레이어 제거
            Object.Destroy(cameraObject); // 카메라 제거
            Time.timeScale = previousScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
