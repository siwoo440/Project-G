using System; // 격리 난수 기능
using System.Reflection; // 실제 모달 연결 조회
using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 실제 전투 상태
using UnityEngine; // 유니티 객체 기능
public class UpgradeDropModalVisibilityTests // 강화 아이템 문구와 실제 모달 시험
{ // 시험 시작
    private sealed class ZeroRandom : System.Random // 확정 시험 드롭 난수
    { // 난수 시작
        public override int Next(int maximum) // 실제 추첨 요청
        { // 반환 시작
            return 0; // 첫 유효 후보 선택
        } // 반환 종료
    } // 난수 종료
    private sealed class Effects : IUpgradeDropEffects // 격리된 강화 효과
    { // 효과 시작
        public bool CanApply(UpgradeItemId id, UpgradeItemGrade grade) // 실제 후보 유효성
        { // 확인 시작
            return true; // 격리 후보 허용
        } // 확인 종료
        public bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, out string feedback) // 격리 획득 처리
        { // 처리 시작
            feedback = "획득"; // 실제 획득 안내
            return true; // 격리 효과 적용
        } // 처리 종료
    } // 효과 종료
    [Test] // 실제 모달과 유지 상태 시험
    public void LabelsHideOnlyForRealModalsAndReturnWithoutLosingDrops() // 모달별 라벨 표시 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("DropModalCamera"); // 격리 카메라 객체
        GameObject playerObject = new GameObject("DropModalPlayer"); // 격리 플레이어
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 정상 전투 시간
            Camera camera = holder.AddComponent<Camera>(); // 실제 카메라
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.Begin(new Vector2(0f, -3f)); // 실제 전투 시작 위치
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 실제 성장 상태
            progress.Begin(player, camera); // 실제 카드 선택 연결
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(holder); // 실제 적탄 풀
            UpgradeItemSelection items = holder.AddComponent<UpgradeItemSelection>(); // 실제 강화 선택
            StageDirector stage = EnemyActorTestFactory.AttachStage(holder); // 실제 스테이지 구성
            stage.Begin(player, camera, pool, progress, items); // 실제 전투와 선택 연결
            PauseSettingsOverlay overlay = holder.AddComponent<PauseSettingsOverlay>(); // 실제 일시정지 모달
            overlay.Begin(player, camera, progress, items); // 실제 전투 모달 연결
            stage.SetModalOverlay(overlay); // 실제 스테이지 모달 연결
            UpgradeDropManager manager = holder.AddComponent<UpgradeDropManager>(); // 실제 드롭 관리자
            manager.Begin(player, camera, new Effects(), new ZeroRandom()); // 실제 필드 드롭 연결
            MethodInfo bind = typeof(UpgradeDropManager).GetMethod("SetStage"); // 실제 스테이지 연결 계약
            PropertyInfo visible = typeof(UpgradeDropManager).GetProperty("CanDisplayHud"); // 실제 표시 조건
            Assert.That(bind, Is.Not.Null); // 실제 연결 필요
            Assert.That(visible, Is.Not.Null); // 실제 모달 표시 조건 필요
            bind.Invoke(manager, new object[] // 실제 스테이지 연결
            { // 입력 시작
                stage // 실제 모달 상태를 가진 스테이지
            }); // 입력 종료
            Assert.That(manager.TryDrop(EnemyRank.MidBoss, new Vector2(2f, 2f)), Is.True); // 실제 필드 아이템 생성
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 실제 아이템 보존
            Assert.That((bool)visible.GetValue(manager), Is.True); // 일반 전투 라벨 표시
            Time.timeScale = 0f; // 모달 없는 시간 정지
            Assert.That((bool)visible.GetValue(manager), Is.True); // 단순 시간 정지는 기존 표시 유지
            Time.timeScale = 1f; // 정상 전투 복원
            Assert.That(overlay.TryPause(), Is.True); // 실제 일시정지와 사운드 상위 모달
            Assert.That((bool)visible.GetValue(manager), Is.False); // 실제 모달 위 라벨 차단
            manager.Advance(1f); // 정지 중 드롭 시간 진행 시도
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 정지 중 드롭 보존
            overlay.Resume(); // 실제 모달 닫기
            Assert.That((bool)visible.GetValue(manager), Is.True); // 라벨 표시 복구
            progress.CollectExperience(10); // 실제 카드 선택 열기
            Assert.That(progress.Offer, Is.Not.Null); // 실제 카드 모달 확인
            Assert.That((bool)visible.GetValue(manager), Is.False); // 카드 모달 위 라벨 차단
            int choice = Array.FindIndex(progress.Offer, card => card.Id != CardId.C06); // 교체 없는 카드
            Assert.That(progress.Select(choice), Is.True); // 실제 카드 선택 종료
            Assert.That((bool)visible.GetValue(manager), Is.True); // 카드 선택 후 라벨 복구
            Assert.That(items.Begin(player, camera, progress, pool), Is.True); // 실제 강화 선택 열기
            Assert.That((bool)visible.GetValue(manager), Is.False); // 강화 선택 위 라벨 차단
            items.Cancel(); // 실제 강화 선택 닫기
            Assert.That((bool)visible.GetValue(manager), Is.True); // 강화 선택 후 라벨 복구
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 모달 전후 원본 아이템 보존
            Rect view = new Rect(0f, 0f, 1440f, 1080f); // 실제 시험 전투 영역
            Rect label = UpgradeDropManager.DropLabelRect(view, new Vector2(1435f, 1060f)); // 오른쪽 아래 라벨 요청
            Assert.That(label.xMax, Is.LessThanOrEqualTo(view.xMax)); // 패널 쪽 경계 유지
            Assert.That(label.yMax, Is.LessThanOrEqualTo(view.yMax)); // 아래 화면 경계 유지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(holder); // 실제 드롭과 모달 제거
            UnityEngine.Object.DestroyImmediate(playerObject); // 실제 플레이어 제거
            Time.timeScale = oldScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
