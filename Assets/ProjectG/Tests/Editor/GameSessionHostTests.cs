using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 화면 기능
using ProjectG; // 메뉴와 전투 기능
public class GameSessionHostTests // 출격 화면 통합 시험
{ // 시험 시작
    [Test] // 메뉴와 출격 연결 시험
    public void SelectedCharacterAndSkillReachBattleThenRetryResetsRun() // 선택과 재시도 확인
    { // 시험 시작
        GameObject holder = new GameObject("MenuCamera"); // 메뉴 카메라 객체
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = holder.AddComponent<Camera>(); // 메뉴 카메라 구성
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 화면 연결 구성
            host.Begin(camera); // 타이틀 시작
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 타이틀 표시
            Assert.That(host.CurrentPlayer, Is.Null); // 출격 전 플레이어 없음
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 열기
            Assert.That(host.Flow.SelectCharacter(CharacterType.Sniper), Is.True); // 저격형 선택
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 열기
            Assert.That(host.Flow.SelectCommonSkill(CommonSkill.Scatter), Is.True); // 산탄 선택
            Assert.That(host.Flow.StartRun(), Is.True); // 출격 요청
            Assert.That(host.CurrentPlayer.Character, Is.EqualTo(CharacterType.Sniper)); // 선택 캐릭터 적용
            Assert.That(host.CurrentPlayer.Skills.Common, Is.EqualTo(CommonSkill.Scatter)); // 선택 공용 스킬 적용
            Assert.That(host.CurrentPlayer.AllowDebugCharacterSwitch, Is.False); // 전투 중 시험 선택 차단
            host.CurrentPlayer.RegisterGraze(); // 전투 기록 생성
            host.CurrentStage.State.Fail(); // 출격 실패 처리
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 결과 화면 확인
            Assert.That(host.CurrentPlayer.Statistics.Grazes, Is.EqualTo(1)); // 기존 출격 기록 확인
            Assert.That(host.Flow.RequestRetry(), Is.True); // 재시도 확인 열기
            Assert.That(host.Flow.ConfirmResultExit(true), Is.True); // 재시도 확정
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle)); // 새 전투 시작
            Assert.That(host.CurrentPlayer.Character, Is.EqualTo(CharacterType.Sniper)); // 캐릭터 선택 유지
            Assert.That(host.CurrentPlayer.Skills.Common, Is.EqualTo(CommonSkill.Scatter)); // 스킬 선택 유지
            Assert.That(host.CurrentPlayer.Statistics.Grazes, Is.Zero); // 새 출격 기록 초기화
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 메뉴와 전투 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
