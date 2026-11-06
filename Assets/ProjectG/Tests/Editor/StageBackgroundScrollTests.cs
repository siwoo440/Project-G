using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 화면 기능
using ProjectG; // 전투 구성 기능
public class StageBackgroundScrollTests // 배경 스크롤 시험
{ // 시험 시작
    [Test] // 이동과 연결 시험
    public void BattleBackgroundScrollsDownAndStopsWhenTimeDoesNotAdvance() // 이동과 정지 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라
        GameObject playerObject = null; // 전투 플레이어 참조
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 시험 카메라 구성
            playerObject = BattleSetup.Create(camera); // 전투 화면 구성
            Transform background = cameraObject.transform.Find("StageBackground"); // 배경 조회
            StageBackgroundScroll scroll = background.GetComponent<StageBackgroundScroll>(); // 스크롤 구성 조회
            Assert.That(scroll, Is.Not.Null); // 스크롤 구성 확인
            SpriteRenderer sprite = background.GetComponent<SpriteRenderer>(); // 배경 표시 조회
            MaterialPropertyBlock properties = new MaterialPropertyBlock(); // 개별 표시 속성
            sprite.GetPropertyBlock(properties); // 초기 속성 조회
            float before = properties.GetFloat("_ScrollPhase"); // 초기 위치 확인
            scroll.Advance(1f); // 1초 전투 시간 진행
            sprite.GetPropertyBlock(properties); // 이동 후 속성 조회
            float after = properties.GetFloat("_ScrollPhase"); // 이동 위치 확인
            Assert.That(after, Is.GreaterThan(before)); // 배경의 아래쪽 이동 확인
            scroll.Advance(0f); // 정지 시간 전달
            sprite.GetPropertyBlock(properties); // 정지 후 속성 조회
            Assert.That(properties.GetFloat("_ScrollPhase"), Is.EqualTo(after).Within(0.00001f)); // 정지 위치 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 존재 확인
            { // 제거 시작
                Object.DestroyImmediate(playerObject); // 플레이어 제거
            } // 제거 종료
            Object.DestroyImmediate(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 반복 경계와 재출격 시험
    public void BackgroundPhaseWrapsAndResetsWhenBattleRestarts() // 위치 순환과 초기화 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라
        GameObject playerObject = null; // 전투 플레이어 참조
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 시험 카메라 구성
            playerObject = BattleSetup.Create(camera); // 전투 화면 구성
            Transform background = cameraObject.transform.Find("StageBackground"); // 배경 조회
            StageBackgroundScroll scroll = background.GetComponent<StageBackgroundScroll>(); // 스크롤 구성 조회
            Assert.That(scroll, Is.Not.Null); // 스크롤 구성 확인
            SpriteRenderer sprite = background.GetComponent<SpriteRenderer>(); // 배경 표시 조회
            MaterialPropertyBlock properties = new MaterialPropertyBlock(); // 개별 표시 속성
            scroll.Advance(1000f); // 긴 전투 시간 진행
            sprite.GetPropertyBlock(properties); // 반복 후 속성 조회
            float phase = properties.GetFloat("_ScrollPhase"); // 순환 위치 확인
            Assert.That(phase, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f)); // 반복 위치 범위
            playerObject.GetComponent<BattlePlayer>().Begin(Vector2.zero); // 재출격 초기화
            sprite.GetPropertyBlock(properties); // 재출격 후 속성 조회
            Assert.That(properties.GetFloat("_ScrollPhase"), Is.EqualTo(0f).Within(0.00001f)); // 첫 위치 복원
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 존재 확인
            { // 제거 시작
                Object.DestroyImmediate(playerObject); // 플레이어 제거
            } // 제거 종료
            Object.DestroyImmediate(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 카메라 크기 변경 시험
    public void BackgroundStillCoversTheViewportWhenCameraSizeChanges() // 화면 범위 재계산 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라
        GameObject playerObject = null; // 전투 플레이어 참조
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 시험 카메라 구성
            playerObject = BattleSetup.Create(camera); // 전투 화면 구성
            Transform background = cameraObject.transform.Find("StageBackground"); // 배경 조회
            StageBackgroundScroll scroll = background.GetComponent<StageBackgroundScroll>(); // 스크롤 구성 조회
            Assert.That(scroll, Is.Not.Null); // 스크롤 구성 확인
            camera.orthographicSize = 8f; // 더 넓은 화면 크기
            scroll.Advance(0f); // 이동 없이 화면 크기 갱신
            SpriteRenderer sprite = background.GetComponent<SpriteRenderer>(); // 배경 표시 조회
            Assert.That(sprite.bounds.size.x, Is.GreaterThanOrEqualTo(16f * 16f / 9f - 0.001f)); // 넓어진 가로 화면 적용
            Assert.That(sprite.bounds.size.y, Is.GreaterThanOrEqualTo(16f - 0.001f)); // 넓어진 세로 화면 적용
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 존재 확인
            { // 제거 시작
                Object.DestroyImmediate(playerObject); // 플레이어 제거
            } // 제거 종료
            Object.DestroyImmediate(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 렌더 셰이더 검사
    public void ScrollShaderImportsWithoutCompilerErrors() // 셰이더 자산 확인
    { // 시험 시작
        Shader shader = Resources.Load<Shader>("BattleArt/StageBackgroundScroll"); // 스크롤 셰이더 조회
        Assert.That(shader, Is.Not.Null); // 셰이더 자산 존재
        Assert.That(UnityEditor.ShaderUtil.ShaderHasError(shader), Is.False); // 편집기 컴파일 오류 확인
    } // 시험 종료
} // 시험 종료
