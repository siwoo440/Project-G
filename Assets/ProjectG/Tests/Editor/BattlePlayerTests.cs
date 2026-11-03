using NUnit.Framework; // 테스트 도구
using UnityEngine; // 게임 오브젝트 기능
using ProjectG; // 플레이어 기능
public class BattlePlayerTests // 플레이어 연결 시험
{ // 시험 시작
    [Test] // 이동과 발사 시험
    public void StepMovesPlayerAndCreatesShots() // 플레이어 동작 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 시험 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 전투 시작
            player.Step(Vector2.right, false, false, false, 1f, -8f, 8f, -5f, 5f); // 일반 이동과 발사
            Assert.That(holder.transform.position.x, Is.EqualTo(4f).Within(0.0001f)); // 이동 위치 확인
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.EqualTo(2)); // 일반 탄환 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 생성 탄환 순회
            { // 탄환 정리 시작
                Object.DestroyImmediate(shot.gameObject); // 시험 탄환 제거
            } // 탄환 정리 종료
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
