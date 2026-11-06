using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 시간과 화면
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 전투 구성 기능
public class StageBackgroundScrollPlayTests // 배경 실제 시간 정지 시험
{ // 시험 시작
    [UnityTest] // 배경 시간 정지 시험
    public IEnumerator BackgroundStopsWhileBattleTimeIsPausedAndResumes() // 정지와 재개 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율 보관
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라
        GameObject playerObject = null; // 전투 플레이어 참조
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 전투 시간 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 시험 카메라 구성
            playerObject = BattleSetup.Create(camera); // 전투 화면 구성
            SpriteRenderer sprite = cameraObject.transform.Find("StageBackground").GetComponent<SpriteRenderer>(); // 배경 표시 조회
            MaterialPropertyBlock properties = new MaterialPropertyBlock(); // 개별 표시 속성
            yield return null; // 첫 프레임 진행
            Time.timeScale = 0f; // 전투 시간 정지
            yield return null; // 정지 프레임 반영
            sprite.GetPropertyBlock(properties); // 정지 위치 조회
            float paused = properties.GetFloat("_ScrollPhase"); // 정지 위치 보관
            yield return null; // 추가 정지 프레임 진행
            sprite.GetPropertyBlock(properties); // 정지 후 위치 조회
            Assert.That(properties.GetFloat("_ScrollPhase"), Is.EqualTo(paused).Within(0.000001f)); // 시간 정지 중 이동 차단
            Time.timeScale = 1f; // 전투 시간 재개
            yield return null; // 재개 프레임 진행
            sprite.GetPropertyBlock(properties); // 재개 후 위치 조회
            Assert.That(properties.GetFloat("_ScrollPhase"), Is.GreaterThan(paused)); // 재개 후 아래 방향 이동
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = previousScale; // 기존 시간 배율 복원
            if (playerObject != null) // 플레이어 존재 확인
            { // 제거 시작
                Object.Destroy(playerObject); // 플레이어 제거
            } // 제거 종료
            Object.Destroy(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
