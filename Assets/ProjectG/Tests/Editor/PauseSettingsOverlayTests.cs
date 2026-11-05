using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 화면 기능
using ProjectG; // 전투 화면 기능
public class PauseSettingsOverlayTests // 일시정지 상태 시험
{ // 시험 시작
    [Test] // 일시정지와 재개 시험
    public void PauseRestoresPreviousTimeScale() // 시간 배율 복원 확인
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("Player"); // 시험 플레이어
        GameObject overlayObject = new GameObject("Pause"); // 화면 객체
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 0.5f; // 시험 시간 배율
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            PauseSettingsOverlay overlay = overlayObject.AddComponent<PauseSettingsOverlay>(); // 정지 화면 구성
            overlay.Begin(player, null, null, null); // 전투 연결
            Assert.That(overlay.TryPause(), Is.True); // 일시정지 성공
            Assert.That(Time.timeScale, Is.Zero); // 전투 정지 확인
            Assert.That(overlay.TryPause(), Is.False); // 중복 정지 방지
            overlay.Resume(); // 전투 재개
            Assert.That(Time.timeScale, Is.EqualTo(0.5f)); // 원래 배율 복원
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(overlayObject); // 화면 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 카드 선택 충돌 시험
    public void PauseDoesNotInterruptCardChoiceOrTransition() // 선택 화면 충돌 방지
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("Camera"); // 시험 카메라
        GameObject holder = new GameObject("Player"); // 시험 플레이어
        GameObject overlayObject = new GameObject("Pause"); // 정지 화면
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 시간 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            BattleProgress progress = holder.AddComponent<BattleProgress>(); // 카드 구성
            progress.Begin(player, camera); // 카드 연결
            PauseSettingsOverlay overlay = overlayObject.AddComponent<PauseSettingsOverlay>(); // 정지 화면 구성
            overlay.Begin(player, camera, progress, null); // 전투 연결
            progress.Blocked = true; // 보스 전환 지연 설정
            Assert.That(overlay.TryPause(), Is.False); // 전환 중 정지 화면 제외
            progress.Blocked = false; // 전환 지연 해제
            progress.CollectExperience(10); // 카드 선택 열기
            Assert.That(progress.Offer, Is.Not.Null); // 카드 선택 확인
            Assert.That(overlay.TryPause(), Is.False); // 카드 선택과 충돌 방지
            progress.Shutdown(); // 카드 정지 복원
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 카드 종료 후 시간 복원
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(overlayObject); // 정지 화면 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 타이틀 설정 시험
    public void MenuSettingsRestoreAlreadyPausedTimeScale() // 메뉴 시간 상태 보존
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("Pause"); // 설정 화면 객체
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 0f; // 메뉴 정지 상태
            PauseSettingsOverlay overlay = holder.AddComponent<PauseSettingsOverlay>(); // 설정 화면 구성
            overlay.Begin(null, null, null, null); // 플레이어 없는 메뉴 연결
            Assert.That(overlay.OpenSettingsFromMenu(), Is.True); // 메뉴 설정 열기
            overlay.Resume(); // 메뉴 설정 닫기
            Assert.That(Time.timeScale, Is.Zero); // 기존 메뉴 시간 보존
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 화면 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
