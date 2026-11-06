using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 안내와 전투
using UnityEngine; // 유니티 객체
public class TutorialGuideTests // 첫 출격 안내 통합 시험
{ // 시험 시작
    private sealed class BrokenStore : ITutorialStateStore // 저장 실패 시험 저장소
    { // 저장소 시작
        public bool TryRead(out TutorialSaveState state) // 읽기 실패
        { // 조회 시작
            state = TutorialSaveState.Unseen; // 기본 출력
            return false; // 조회 실패
        } // 조회 종료
        public bool TryWrite(TutorialSaveState state) // 기록 실패
        { // 기록 시작
            return false; // 저장 실패
        } // 기록 종료
    } // 저장소 종료
    private sealed class MemoryKeys : IKeyBindingStore // 조작 키 격리 저장소
    { // 저장소 시작
        public string Get(string key, string fallback) // 시험 기본값
        { // 조회 시작
            return fallback; // 기본값 반환
        } // 조회 종료
        public void Set(string key, string value) // 시험 기록
        { // 기록 시작
        } // 기록 생략
        public void Flush() // 시험 기록 완료
        { // 완료 시작
        } // 완료 생략
    } // 저장소 종료
    [Test] // 재출격과 건너뛰기 시험
    public void SkippedGuideStaysFinishedAndExistingUsersAreNotMistakenForNew() // 저장과 기존 이용자 확인
    { // 시험 시작
        GameObject holder = new GameObject("TutorialPlayer"); // 격리 플레이어
        GameObject cameraObject = new GameObject("TutorialCamera"); // 격리 전투 화면
        try // 시험 범위
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.Begin(Vector2.zero); // 플레이어 초기화
            TutorialGuide guide = cameraObject.AddComponent<TutorialGuide>(); // 실제 안내 구성
            MemoryTutorialStateStore store = new MemoryTutorialStateStore(); // 격리 저장소
            Assert.That(guide.Begin(player, camera, null, null, null, store, true), Is.False); // 기존 이용자 자동 표시 차단
            Assert.That(guide.Begin(player, camera, null, null, null, new BrokenStore(), false), Is.False); // 저장 조회 실패 시 전투 보존
            Assert.That(guide.Begin(player, camera, null, null, null, store, false), Is.True); // 새 이용자 안내 시작
            guide.Skip(); // 사용자 건너뛰기
            Assert.That(guide.IsShowing, Is.False); // 안내 종료 확인
            Assert.That(guide.Begin(player, camera, null, null, null, store, false), Is.False); // 재출격 시 안내 미표시
        } // 시험 범위 종료
        finally // 객체 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 화면 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 모달과 키 재설정 시험
    public void PausedSignalsDoNotAdvanceAndLabelsUseCurrentBindings() // 시간 정지와 키 동기화
    { // 시험 시작
        GameObject holder = new GameObject("TutorialPlayer"); // 격리 플레이어
        GameObject cameraObject = new GameObject("TutorialCamera"); // 격리 전투 화면
        float previous = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.Begin(Vector2.zero); // 플레이어 초기화
            KeyBindingSettings keys = new KeyBindingSettings(new MemoryKeys(), "isolated."); // 격리된 키 설정
            Assert.That(keys.TryBind(PlayerAction.MoveUp, "q"), Is.True); // 이동 키 변경
            player.SetBindings(keys); // 실제 설정 연결
            TutorialGuide guide = cameraObject.AddComponent<TutorialGuide>(); // 실제 안내 구성
            Assert.That(guide.Begin(player, camera, null, null, null, new MemoryTutorialStateStore(), false), Is.True); // 안내 시작
            Assert.That(guide.Message(), Does.Contain("Q")); // 설정된 이동 키 표시
            Time.timeScale = 0f; // 외부 일시정지
            player.Step(Vector2.right, false, false, false, 0.1f, -8f, 8f, -5f, 5f); // 정지 중 직접 신호 시험
            guide.Skip(); // 정지 중 건너뛰기 시도
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Move)); // 안내 진행 보류
            Time.timeScale = 1f; // 외부 일시정지 복원
            player.Step(Vector2.right, false, false, false, 0.1f, -8f, 8f, -5f, 5f); // 실제 이동 재개
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Focus)); // 다음 안내 진입
            Object.DestroyImmediate(guide); // 안내 객체 파괴
            Assert.DoesNotThrow(() => player.Step(Vector2.right, false, false, false, 0.1f, -8f, 8f, -5f, 5f)); // 해제 후 행동 안전
        } // 시험 범위 종료
        finally // 객체 정리
        { // 정리 시작
            Time.timeScale = previous; // 기존 시간 배율 복원
            Object.DestroyImmediate(holder); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 화면 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
