using System.Collections; // 실제 프레임 대기
using NUnit.Framework; // 시험 결과 확인
using ProjectG; // 실제 화면 상태
using UnityEngine; // 시간 기능
public static class BattleSceneTestWait // 실제 비동기 출격 시험 도우미
{ // 도우미 시작
    public static IEnumerator UntilReady(GameSessionHost host) // 실제 씬 로드 완료 대기
    { // 대기 시작
        float deadline = Time.realtimeSinceStartup + 15f; // 시간 배율과 무관한 제한
        while (host != null && host.Flow.Screen == GameFlowScreen.Loading && Time.realtimeSinceStartup < deadline) // 로딩 종료 확인
        { // 프레임 시작
            yield return null; // 실제 다음 프레임
        } // 프레임 종료
        Assert.That(host, Is.Not.Null); // 메뉴 관리자 존재
        Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle), host.Flow.ErrorText); // 실제 전투 시작
        Assert.That(host.CurrentPlayer, Is.Not.Null); // 씬 플레이어 연결 확인
    } // 대기 종료
} // 도우미 종료
