using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 전투 기능
using ProjectG; // 스테이지 기능
public class StageFullFlowTests // 스테이지 전체 흐름 시험
{ // 시험 시작
    [Test] // 출격부터 완료까지 시험
    public void BothBossesLeadToClearWithoutFixedChoicesAndReset() // 보스 순서와 선택 없는 진행 확인
    { // 시험 시작
        GameObject scene = new GameObject("StageScene"); // 스테이지 객체
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = scene.AddComponent<Camera>(); // 카메라 구성
            camera.orthographicSize = 5f; // 시험 화면 크기
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 시작
            EnemyBulletPool pool = scene.AddComponent<EnemyBulletPool>(); // 적탄 풀 구성
            BattleProgress progress = holder.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 상태 연결
            UpgradeItemSelection selection = scene.AddComponent<UpgradeItemSelection>(); // 강화 선택 구성
            StageDirector stage = scene.AddComponent<StageDirector>(); // 스테이지 구성
            stage.Begin(player, camera, pool, progress, selection); // 스테이지 시작
            Assert.That(stage.ContextMessage, Does.Contain("균열 조사")); // 첫 사건 안내
            stage.Advance(65f, 65f); // 첫 전투 경계 진행
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.MidBoss)); // 중간 보스 단계
            Assert.That(selection.IsOpen, Is.False); // 첫 고정 선택 미표시
            Assert.That(stage.Boss, Is.Null); // 보스 안전 시간 보존
            stage.Advance(2f, 2f); // 중간 보스 안전 구간
            Assert.That(stage.Boss, Is.Not.Null); // 중간 보스 등장
            stage.Boss.ApplyDamage(10000f, player); // 중간 보스 처치
            stage.Advance(0.1f, 0.1f); // 처치 결과 진행
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.CombatTwo)); // 후속 일반 전투
            EnemyBullet visible = pool.Spawn(Vector2.zero, Vector2.zero, 0.1f, 1, -10f, player); // 최종 보스 전 적탄 생성
            stage.Advance(50f, 50f); // 둘째 전투 경계 진행
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.FinalBoss)); // 최종 보스 단계
            Assert.That(selection.IsOpen, Is.False); // 둘째 고정 선택 미표시
            Assert.That(visible.InUse, Is.False); // 최종 보스 전환 적탄 정리
            Assert.That(stage.Boss, Is.Null); // 최종 보스 안전 시간 보존
            Assert.That(player.Survival.IsInvulnerable, Is.True); // 최종 보스 전환 무적
            Assert.That(progress.Blocked, Is.True); // 최종 보스 전환 카드 지연
            stage.Advance(2f, 2f); // 최종 보스 안전 구간
            Assert.That(stage.Boss, Is.Not.Null); // 최종 보스 등장
            Assert.That(player.Survival.IsInvulnerable, Is.False); // 최종 보스 전환 무적 해제
            Assert.That(progress.Blocked, Is.False); // 최종 보스 전환 카드 허용
            stage.Boss.ApplyDamage(10000f, player); // 첫 체력 단계
            Assert.That(stage.Boss.PatternIndex, Is.EqualTo(1)); // 둘째 단계 확인
            stage.Boss.ApplyDamage(10000f, player); // 둘째 체력 단계
            Assert.That(stage.Boss.PatternIndex, Is.EqualTo(2)); // 셋째 단계 확인
            stage.Boss.ApplyDamage(10000f, player); // 최종 보스 처치
            stage.Advance(0.1f, 0.1f); // 처치 결과 진행
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.Result)); // 스테이지 완료
            Assert.That(stage.ContextMessage, Does.Contain("마법 흔적")); // 클리어 후 조사 안내
            Assert.That(player.Skills.Energy, Is.EqualTo(30)); // 보스 보상 중복 방지
            player.Begin(Vector2.zero); // 새 출격 시작
            Assert.That(stage.State.Phase, Is.EqualTo(StagePhase.Preparation)); // 스테이지 초기화
            Assert.That(player.Skills.Energy, Is.Zero); // 출격 자원 초기화
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(scene); // 스테이지 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
