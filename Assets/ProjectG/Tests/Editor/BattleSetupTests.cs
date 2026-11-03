using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 장면 기능
using ProjectG; // 전투 장면 기능
public class BattleSetupTests // 장면 구성 시험
{ // 시험 시작
    [Test] // 초기 장면 시험
    public void CreateBuildsVisiblePlayerAndOrthographicCamera() // 전투 화면 구성 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        GameObject player = null; // 시험 플레이어 보관
        try // 시험 시작
        { // 시험 범위 시작
            player = BattleSetup.Create(camera); // 전투 화면 생성
            Assert.That(player, Is.Not.Null); // 플레이어 존재 확인
            Assert.That(player.GetComponent<BattlePlayer>(), Is.Not.Null); // 플레이어 입력 구성 확인
            Assert.That(player.GetComponent<SpriteRenderer>(), Is.Not.Null); // 플레이어 표시 확인
            Assert.That(player.GetComponent<SkillHud>(), Is.Not.Null); // 스킬 화면 연결 확인
            Assert.That(player.transform.localScale.x, Is.EqualTo(1.125f).Within(0.0001f)); // 플레이어 표시 절반 확인
            Assert.That(player.transform.localScale.y, Is.EqualTo(1.125f).Within(0.0001f)); // 플레이어 세로 크기 확인
            Assert.That(player.transform.Find("HitboxIndicator").lossyScale.x, Is.EqualTo(0.24f).Within(0.0001f)); // 피격 가로 크기 유지
            Assert.That(player.transform.Find("HitboxIndicator").lossyScale.y, Is.EqualTo(0.24f).Within(0.0001f)); // 피격 세로 크기 유지
            Assert.That(camera.orthographic, Is.True); // 직교 카메라 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            if (player != null) // 플레이어 존재 확인
            { // 플레이어 정리 시작
                Object.DestroyImmediate(player); // 시험 플레이어 제거
            } // 플레이어 정리 종료
            Object.DestroyImmediate(cameraObject); // 시험 카메라 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 화면 비율 시험
    public void WideScreenUsesCenteredSixteenByNineViewport() // 넓은 화면 비율 확인
    { // 시험 시작
        Rect viewport = BattleSetup.CalculateViewport(2560f, 1080f); // 화면 표시 영역 계산
        Assert.That(viewport.width, Is.EqualTo(0.75f).Within(0.0001f)); // 가로 표시 비율 확인
        Assert.That(viewport.x, Is.EqualTo(0.125f).Within(0.0001f)); // 중앙 정렬 확인
        Assert.That(viewport.height, Is.EqualTo(1f).Within(0.0001f)); // 세로 전체 확인
    } // 시험 종료
} // 시험 종료
