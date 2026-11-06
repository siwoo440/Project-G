using System.Collections; // 실제 프레임 시험
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 전투와 안내
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 플레이 모드 시험
public class TutorialFramePlayTests // 안내 단계 표시 프레임 시험
{ // 시험 시작
    [UnityTest] // 동시 이동과 집중 사격 시험
    public IEnumerator SimultaneousMovementAndFocusDoNotSkipTheFocusGuide() // 한 프레임에 한 안내만 완료
    { // 시험 시작
        GameObject holder = new GameObject("TutorialFramePlayer"); // 격리 플레이어
        GameObject cameraObject = new GameObject("TutorialFrameCamera"); // 격리 카메라
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 활성화
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.AssignCamera(camera); // 전투 화면 연결
            player.Begin(Vector2.zero, CharacterType.Rapid); // 플레이어 초기화
            int moves = 0; // 실제 이동 신호 수
            int focusedShots = 0; // 실제 집중 사격 신호 수
            player.TutorialAction += signal => // 전투 행동 신호 확인
            { // 확인 시작
                if (signal == TutorialSignal.Move) // 이동 신호 확인
                { // 이동 집계 시작
                    moves++; // 이동 신호 기록
                } // 이동 집계 종료
                if (signal == TutorialSignal.FocusShot) // 집중탄 신호 확인
                { // 집중 집계 시작
                    focusedShots++; // 집중탄 신호 기록
                } // 집중 집계 종료
            }; // 확인 종료
            TutorialGuide guide = cameraObject.AddComponent<TutorialGuide>(); // 실제 안내
            Assert.That(guide.Begin(player, camera, null, null, null, new MemoryTutorialStateStore(), false), Is.True); // 첫 출격 안내 시작
            int startFrame = Time.frameCount; // 동시 행동 프레임
            player.Step(Vector2.right, true, true, false, 0.5f, -8f, 8f, -5f, 5f); // 같은 프레임의 이동과 집중탄
            Assert.That(moves, Is.EqualTo(1)); // 실제 이동 신호 발생
            Assert.That(focusedShots, Is.EqualTo(1)); // 실제 집중탄 신호 발생
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Focus)); // 집중 안내 표시 기회 보장
            yield return null; // 실제 다음 프레임 대기
            Assert.That(Time.frameCount, Is.GreaterThan(startFrame)); // 실제 프레임 이동 확인
            player.Step(Vector2.zero, false, true, false, 0.5f, -8f, 8f, -5f, 5f); // 집중 유지와 다음 집중탄
            Assert.That(guide.Current, Is.EqualTo(TutorialStep.Skill)); // 실제 다음 프레임에 스킬 단계 진입
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = previousScale; // 기존 시간 배율 복원
            Object.Destroy(holder); // 플레이어 정리
            Object.Destroy(cameraObject); // 화면 정리
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
