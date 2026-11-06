using System.Collections; // 프레임 시험 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 안내와 전투
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 프레임 시험 도구
public class TutorialActionsPlayTests // 실제 행동 단계 시험
{ // 시험 시작
    [UnityTest] // 전체 실제 동작 시험
    public IEnumerator ActualActionsAdvanceInOrderWithoutSupplyingEnergy() // 순서와 실제 성공 확인
    { // 시험 시작
        GameObject holder = new GameObject("TutorialPlayer"); // 격리 플레이어
        GameObject cameraObject = new GameObject("TutorialCamera"); // 격리 전투 화면
        try // 시험 범위
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라
            EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 실제 적탄 풀
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.AssignCamera(camera); // 전투 카메라 연결
            player.Begin(Vector2.zero, CharacterType.Rapid); // 플레이어 상태 초기화
            player.AssignBulletPool(pool); // 실제 적탄 풀 연결
            TutorialGuide guide = cameraObject.AddComponent<TutorialGuide>(); // 실제 안내 구성
            MemoryTutorialStateStore store = new MemoryTutorialStateStore(); // 격리된 안내 저장
            Assert.That(guide.Begin(player, camera, null, null, null, store, false), Is.True); // 첫 출격 안내 시작
            player.Step(Vector2.right, false, false, false, 0.1f, -8f, 8f, -5f, 5f); // 실제 이동
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Focus)); // 집중 단계 진입
            yield return null; // 다음 안내 프레임 대기
            player.Step(Vector2.zero, true, true, false, 0.5f, -8f, 8f, -5f, 5f); // 실제 집중 사격
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Skill)); // 스킬 단계 진입
            yield return null; // 다음 안내 프레임 대기
            player.Skills.Blocked = true; // 발동 제한 설정
            Assert.That(player.TryUseRapid(), Is.False); // 스킬 발동 실패 확인
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Skill)); // 실패 입력 무시
            player.Skills.Blocked = false; // 발동 제한 해제
            Assert.That(player.TryUseRapid(), Is.True); // 실제 스킬 성공
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Graze)); // 그레이즈 단계 진입
            yield return null; // 다음 안내 프레임 대기
            EnemyBullet bullet = pool.Spawn((Vector2)holder.transform.position + Vector2.right * 0.3f, Vector2.zero, 0.01f, 1, -10f, player); // 실제 근접 탄환
            bullet.Simulate(0.01f); // 적탄 실제 접촉 판정
            Assert.That(player.GrazeCount, Is.EqualTo(1)); // 실제 그레이즈 기록
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Ultimate)); // 필살기 단계 진입
            yield return null; // 다음 안내 프레임 대기
            Assert.That(player.Skills.Energy, Is.EqualTo(1)); // 자동 보급 없는 실제 자원
            Assert.That(player.TryUseUltimate(), Is.False); // 자원 부족 발동 실패
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Ultimate)); // 실패 입력 무시
            player.Skills.GainEnergy(99); // 시험에서만 자원 충전
            Assert.That(player.TryUseUltimate(), Is.True); // 실제 필살기 성공
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Finished)); // 안내 완료
            Assert.That(store.TryRead(out TutorialSaveState state) && state == TutorialSaveState.Finished, Is.True); // 완료 영속 상태
        } // 시험 범위 종료
        finally // 객체 정리
        { // 정리 시작
            Object.Destroy(holder); // 플레이어와 탄환 정리
            Object.Destroy(cameraObject); // 전투 화면과 적탄 정리
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
