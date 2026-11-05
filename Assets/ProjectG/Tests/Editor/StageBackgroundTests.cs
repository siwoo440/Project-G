using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 화면 기능
using ProjectG; // 스테이지 화면 기능
public class StageBackgroundTests // 하늘섬 배경 시험
{ // 시험 시작
    [Test] // 스테이지 배경 연결 시험
    public void BattleSceneUsesArtworkCoveringTheFullViewport() // 전투 화면 배경 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("Camera"); // 시험 카메라 객체
        GameObject playerObject = null; // 플레이어 객체 참조
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            playerObject = BattleSetup.Create(camera); // 전투 화면 구성
            Transform background = cameraObject.transform.Find("StageBackground"); // 배경 객체 조회
            Assert.That(background, Is.Not.Null); // 배경 객체 존재
            SpriteRenderer image = background.GetComponent<SpriteRenderer>(); // 배경 이미지 조회
            Assert.That(image.sprite.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/stage_sky_outskirts"))); // 생성 배경 연결
            Assert.That(image.sortingOrder, Is.LessThan(0)); // 전투 뒤 표시
            Assert.That(image.bounds.size.x, Is.GreaterThanOrEqualTo(camera.orthographicSize * 2f * 16f / 9f - 0.001f)); // 전투 가로 영역 적용
            Assert.That(image.bounds.size.y, Is.GreaterThanOrEqualTo(camera.orthographicSize * 2f - 0.001f)); // 전투 세로 영역 적용
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 존재 확인
            { // 제거 시작
                Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
            } // 제거 종료
            Object.DestroyImmediate(cameraObject); // 시험 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
