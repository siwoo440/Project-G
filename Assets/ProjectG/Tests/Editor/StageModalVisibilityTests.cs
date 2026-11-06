using System.Reflection; // 실제 표시 계약 조회
using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 실제 전투 상태
using UnityEngine; // 유니티 객체 기능
public class StageModalVisibilityTests // 실제 모달과 스테이지 문구 시험
{ // 시험 시작
    [Test] // 일시정지와 카드 선택 표시 시험
    public void StageTextHidesOnlyForActualModalsAndReturnsAfterClose() // 실제 모달 상태 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("StageModalCamera"); // 격리 카메라 객체
        GameObject playerObject = new GameObject("StageModalPlayer"); // 격리 플레이어
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 정상 전투 시간
            Camera camera = holder.AddComponent<Camera>(); // 실제 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.Begin(Vector2.zero); // 전투 상태 시작
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 실제 성장 상태
            progress.Begin(player, camera); // 실제 카드 상태 연결
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(holder); // 실제 적탄 풀
            UpgradeItemSelection items = holder.AddComponent<UpgradeItemSelection>(); // 실제 강화 선택
            StageDirector stage = EnemyActorTestFactory.AttachStage(holder); // 실제 적 템플릿 스테이지
            stage.Begin(player, camera, pool, progress, items); // 실제 전투 연결
            PauseSettingsOverlay overlay = holder.AddComponent<PauseSettingsOverlay>(); // 실제 일시정지 모달
            overlay.Begin(player, camera, progress, items); // 실제 모달 연결
            MethodInfo bind = typeof(StageDirector).GetMethod("SetModalOverlay"); // 모달 연결 계약
            PropertyInfo visible = typeof(StageDirector).GetProperty("IsHudVisible"); // 실제 표시 조건 계약
            Assert.That(bind, Is.Not.Null); // 실제 모달 연결 필요
            Assert.That(visible, Is.Not.Null); // 실제 표시 조건 필요
            bind.Invoke(stage, new object[] // 실제 모달 주입
            { // 입력 시작
                overlay // 실제 일시정지 상태
            }); // 입력 종료
            Assert.That((bool)visible.GetValue(stage), Is.True); // 일반 전투 문구 표시
            Time.timeScale = 0f; // 모달 없는 시간 정지
            Assert.That((bool)visible.GetValue(stage), Is.True); // 단순 시간 정지만으로 숨기지 않음
            Time.timeScale = 1f; // 실제 전투 재개
            Assert.That(overlay.TryPause(), Is.True); // 실제 일시정지 열기
            Assert.That((bool)visible.GetValue(stage), Is.False); // 일시정지와 사운드 모달 위 문구 차단
            overlay.Resume(); // 실제 모달 닫기
            Assert.That((bool)visible.GetValue(stage), Is.True); // 기존 전투 문구 복구
            progress.CollectExperience(10); // 실제 카드 선택 열기
            Assert.That(progress.Offer, Is.Not.Null); // 실제 카드 모달 확인
            Assert.That((bool)visible.GetValue(stage), Is.False); // 카드 모달 위 문구 차단
            int choice = System.Array.FindIndex(progress.Offer, card => card.Id != CardId.C06); // 교체 없는 실제 선택지
            Assert.That(progress.Select(choice), Is.True); // 실제 카드 선택
            Assert.That((bool)visible.GetValue(stage), Is.True); // 카드 종료 후 문구 복구
            Assert.That(items.Begin(player, camera, progress, pool), Is.True); // 실제 강화 선택 열기
            Assert.That((bool)visible.GetValue(stage), Is.False); // 강화 선택 위 문구 차단
            items.Cancel(); // 실제 강화 선택 닫기
            Assert.That((bool)visible.GetValue(stage), Is.True); // 강화 선택 종료 후 복구
            stage.State.Fail(); // 실제 결과 단계 전환
            Time.timeScale = 0f; // 결과 화면 시간 정지
            Assert.That((bool)visible.GetValue(stage), Is.True); // 결과 상태의 기존 문구 보존
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 실제 스테이지 제거
            Object.DestroyImmediate(playerObject); // 실제 플레이어 제거
            Time.timeScale = oldScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
