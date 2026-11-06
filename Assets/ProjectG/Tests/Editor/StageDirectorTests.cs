using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 스테이지 전투 기능
public class StageDirectorTests // 스테이지 통합 시험
{ // 시험 시작
    [Test] // 시험 웨이브와 정리 시험
    public void SpawnsAfterTwoSecondsAndDestroysInactiveEnemies() // 적 등장과 회수 확인
    { // 시험 시작
        GameObject scene = new GameObject("StageScene"); // 스테이지 객체
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = scene.AddComponent<Camera>(); // 카메라 구성
            camera.orthographic = true; // 직교 전투 화면
            camera.orthographicSize = 5f; // 전투 화면 크기
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(scene); // 적탄 풀
            BattleProgress progress = holder.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 상태 연결
            UpgradeItemSelection selection = scene.AddComponent<UpgradeItemSelection>(); // 강화 선택 구성
            StageDirector stage = EnemyActorTestFactory.AttachStage(scene); // 스테이지 구성
            stage.Begin(player, camera, pool, progress, selection); // 스테이지 시작
            stage.Advance(1.9f, 1.9f); // 첫 적 등장 전 진행
            Assert.That(stage.ActiveEnemyCount, Is.Zero); // 등장 전 비어 있음
            stage.Advance(0.1f, 0.1f); // 첫 등장 시간
            Assert.That(stage.ActiveEnemyCount, Is.EqualTo(1)); // 일반 적 한 명
            EnemyActor enemy = scene.GetComponentInChildren<EnemyActor>(); // 생성 적 조회
            enemy.ApplyDamage(100f, null); // 일반 적 처치
            stage.Advance(0.1f, 0.1f); // 비활성 적 정리
            Assert.That(enemy == null, Is.True); // 처치된 적 객체 제거
            Assert.That(stage.ActiveEnemyCount, Is.Zero); // 활성 적 목록 정리
            Assert.That(stage.HasSceneTemplates, Is.True); // 비활성 템플릿 보존
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(scene); // 스테이지 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 보스 전환 안전 시험
    public void BossStartsWithoutChoiceAfterSafeTransition() // 선택 없는 보스 등장 확인
    { // 시험 시작
        GameObject scene = new GameObject("StageScene"); // 스테이지 객체
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = scene.AddComponent<Camera>(); // 카메라 구성
            camera.orthographic = true; // 직교 전투 화면
            camera.orthographicSize = 5f; // 전투 화면 크기
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(scene); // 적탄 풀
            BattleProgress progress = holder.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 상태 연결
            UpgradeItemSelection selection = scene.AddComponent<UpgradeItemSelection>(); // 강화 선택 구성
            StageDirector stage = EnemyActorTestFactory.AttachStage(scene); // 스테이지 구성
            stage.Begin(player, camera, pool, progress, selection); // 스테이지 시작
            EnemyBullet visible = pool.Spawn(Vector2.zero, Vector2.zero, 0.1f, 1, -10f, player); // 화면 안 적탄 생성
            stage.Advance(65f, 65f); // 첫 전투 경계까지 진행
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.MidBoss)); // 선택 없는 중간 보스 진입
            Assert.That(selection.IsOpen, Is.False); // 고정 강화 선택 미표시
            Assert.That(visible.InUse, Is.False); // 보스 전환 적탄 정리
            Assert.That(player.Survival.IsInvulnerable, Is.True); // 전환 중 무적
            Assert.That(progress.Blocked, Is.True); // 카드 선택 지연
            progress.CollectExperience(10); // 전환 중 레벨업 경험치 획득
            Assert.That(progress.Level.PendingChoices, Is.EqualTo(1)); // 카드 선택 예약
            Assert.That(progress.Offer, Is.Null); // 전환 중 카드 표시 지연
            Assert.That(player.Survival.TryHit(10), Is.False); // 전환 피해 차단
            stage.Advance(1f, 1f); // 안전 구간 진행
            Assert.That(stage.Boss, Is.Null); // 보스 등장 전 대기
            stage.Advance(1f, 1f); // 안전 구간 종료
            Assert.That(stage.Boss, Is.Not.Null); // 중간 보스 생성
            Assert.That(player.Survival.IsInvulnerable, Is.False); // 전환 무적 종료
            Assert.That(progress.Blocked, Is.False); // 카드 선택 지연 해제
            progress.CollectExperience(0); // 대기 카드 선택 갱신
            Assert.That(progress.Offer, Is.Not.Null); // 보스 등장 후 카드 표시
            progress.Shutdown(); // 카드 선택 정리와 시간 복원
            stage.Boss.ApplyDamage(100000f, player); // 보스 처치 시도
            stage.Boss.ApplyDamage(100000f, player); // 보스 단계 경계 진행
            stage.Boss.ApplyDamage(100000f, player); // 보스 최종 처치
            stage.Advance(0.1f, 0.1f); // 처치 결과 처리
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.CombatTwo)); // 중간 보스 후 일반 전투
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(scene); // 스테이지 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 보스 전환 중 재출격 시험
    public void RestartReleasesTransitionSafety() // 재출격 안전 상태 복원
    { // 시험 시작
        GameObject scene = new GameObject("StageScene"); // 스테이지 객체
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = scene.AddComponent<Camera>(); // 카메라 구성
            camera.orthographicSize = 5f; // 전투 화면 크기
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(scene); // 적탄 풀
            BattleProgress progress = holder.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 상태 연결
            UpgradeItemSelection selection = scene.AddComponent<UpgradeItemSelection>(); // 강화 선택 구성
            StageDirector stage = EnemyActorTestFactory.AttachStage(scene); // 스테이지 구성
            stage.Begin(player, camera, pool, progress, selection); // 스테이지 시작
            stage.Advance(65f, 65f); // 중간 보스 전환 시작
            Assert.That(selection.IsOpen, Is.False); // 고정 선택 미표시
            Assert.That(player.Survival.IsInvulnerable, Is.True); // 전환 무적 확인
            player.Begin(Vector2.zero); // 새 출격 시작
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.Preparation)); // 준비 구간 초기화
            Assert.That(player.Survival.IsInvulnerable, Is.False); // 안전 상태 해제
            Assert.That(progress.Blocked, Is.False); // 카드 표시 복원
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(scene); // 스테이지 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(true)] // 실패 처리 경우
    [TestCase(false)] // 스테이지 제거 경우
    public void FailureOrDestroyReleasesTransitionSafety(bool fail) // 종료 시 안전 상태 복원
    { // 시험 시작
        GameObject scene = new GameObject("StageScene"); // 스테이지 객체
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = scene.AddComponent<Camera>(); // 카메라 구성
            camera.orthographicSize = 5f; // 전투 화면 크기
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(scene); // 적탄 풀
            BattleProgress progress = holder.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 상태 연결
            UpgradeItemSelection selection = scene.AddComponent<UpgradeItemSelection>(); // 강화 선택 구성
            StageDirector stage = EnemyActorTestFactory.AttachStage(scene); // 스테이지 구성
            stage.Begin(player, camera, pool, progress, selection); // 스테이지 시작
            stage.Advance(65f, 65f); // 중간 보스 전환 시작
            Assert.That(selection.IsOpen, Is.False); // 고정 선택 미표시
            Assert.That(player.Survival.IsInvulnerable, Is.True); // 전환 무적 확인
            if (fail) // 실패 방식 확인
            { // 실패 처리 시작
                stage.State.Fail(); // 출격 실패 처리
            } // 실패 처리 종료
            else // 구성 요소 제거 방식
            { // 제거 시작
                stage.Shutdown(); // 편집 모드 종료 처리
                Object.DestroyImmediate(stage); // 스테이지 제어 제거
            } // 제거 종료
            Assert.That(player.Survival.IsInvulnerable, Is.False); // 안전 상태 해제
            Assert.That(progress.Blocked, Is.False); // 카드 표시 복원
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(scene); // 스테이지 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
