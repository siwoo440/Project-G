using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 장면 기능
using ProjectG; // 전투 화면 기능
public class GameFlowControllerTests // 화면 이동 시험
{ // 시험 시작
    [Test] // 제목과 출격 준비 흐름 시험
    public void CharacterAndCommonSkillReachRunRequest() // 출격 선택 전달 확인
    { // 시험 시작
        GameObject holder = new GameObject("Menu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = holder.AddComponent<Camera>(); // 메뉴 카메라 구성
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 메뉴 화면 구성
            flow.Begin(camera); // 타이틀 시작
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 타이틀 표시
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(flow.SelectCharacter(CharacterType.Sniper), Is.True); // 저격형 선택
            Assert.That(flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(flow.SelectCommonSkill(CommonSkill.Scatter), Is.True); // 공용 산탄 선택
            CharacterType selected = CharacterType.Rapid; // 전달 캐릭터 기본값
            CommonSkill common = CommonSkill.Shield; // 전달 스킬 기본값
            int requests = 0; // 출격 요청 횟수
            flow.RunRequested += (character, skill) => // 출격 요청 수신
            { // 수신 시작
                selected = character; // 캐릭터 보관
                common = skill; // 공용 스킬 보관
                requests++; // 요청 횟수 증가
            }; // 수신 종료
            Assert.That(flow.StartRun(), Is.True); // 출격 요청
            Assert.That(requests, Is.EqualTo(1)); // 단일 출격 요청
            Assert.That(selected, Is.EqualTo(CharacterType.Sniper)); // 선택 캐릭터 전달
            Assert.That(common, Is.EqualTo(CommonSkill.Scatter)); // 선택 스킬 전달
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Battle)); // 전투 화면 진입
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 메뉴 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 결과 이탈 확인 시험
    public void ResultConfirmationControlsRetryAndRestoresTime() // 재시도 확인과 정지 복원
    { // 시험 시작
        float previous = Time.timeScale; // 이전 시간 배율
        GameObject holder = new GameObject("Menu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 0.75f; // 시험 시간 배율
            Camera camera = holder.AddComponent<Camera>(); // 메뉴 카메라 구성
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 구성
            flow.Begin(camera); // 타이틀 시작
            flow.OpenCharacterSelection(); // 캐릭터 선택 진입
            flow.OpenPreparation(); // 출격 준비 진입
            int requests = 0; // 출격 요청 횟수
            int leaves = 0; // 이전 전투 정리 횟수
            flow.RunRequested += (character, skill) => requests++; // 출격 요청 기록
            flow.LeaveBattleRequested += () => leaves++; // 전투 정리 기록
            flow.StartRun(); // 첫 출격 요청
            flow.ShowResult(true); // 시험 결과 진입
            Assert.That(Time.timeScale, Is.Zero); // 결과 중 전투 정지
            Assert.That(flow.RequestRetry(), Is.True); // 재시도 확인 열기
            Assert.That(flow.ConfirmResultExit(false), Is.True); // 재시도 취소
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 결과 화면 유지
            Assert.That(requests, Is.EqualTo(1)); // 재시도 없이 유지
            Assert.That(flow.RequestRetry(), Is.True); // 재시도 확인 재요청
            Assert.That(flow.ConfirmResultExit(true), Is.True); // 재시도 확정
            Assert.That(Time.timeScale, Is.EqualTo(0.75f).Within(0.001f)); // 이전 시간 배율 복원
            Assert.That(leaves, Is.EqualTo(1)); // 이전 전투 한 번 정리
            Assert.That(requests, Is.EqualTo(2)); // 같은 조건 재출격 요청
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Battle)); // 재출격 화면
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = previous; // 기존 시간 배율 복원
            Object.DestroyImmediate(holder); // 메뉴 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 다른 모달 입력 차단 시험
    public void SettingsOverlayBlocksMenuTransitions() // 모달 입력 차단 확인
    { // 시험 시작
        GameObject holder = new GameObject("Menu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 구성
            flow.Begin(holder.AddComponent<Camera>()); // 타이틀 시작
            flow.InputBlocked = true; // 설정 화면 입력 소유
            Assert.That(flow.OpenCharacterSelection(), Is.False); // 다른 화면 이동 차단
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Title)); // 타이틀 유지
            flow.InputBlocked = false; // 설정 화면 닫기
            Assert.That(flow.OpenCharacterSelection(), Is.True); // 이동 재개
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 메뉴 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 스테이지 결과 이벤트 통합 시험
    public void FailedStageOpensResultAfterStatisticsAreFinalized() // 실제 실패와 기록 확인
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject scene = new GameObject("Scene"); // 시험 장면 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 전투 시간
            Camera camera = scene.AddComponent<Camera>(); // 카메라 구성
            GameFlowController flow = scene.AddComponent<GameFlowController>(); // 화면 흐름 구성
            flow.Begin(camera); // 타이틀 시작
            flow.OpenCharacterSelection(); // 캐릭터 선택
            flow.OpenPreparation(); // 출격 준비
            flow.StartRun(); // 출격 요청
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 플레이어 초기화
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(scene); // 적탄 풀 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 상태 연결
            UpgradeItemSelection selection = scene.AddComponent<UpgradeItemSelection>(); // 강화 선택 구성
            StageDirector stage = EnemyActorTestFactory.AttachStage(scene); // 스테이지 구성
            stage.Begin(player, camera, pool, progress, selection); // 스테이지 시작
            flow.AttachBattle(player, stage); // 결과 감지 연결
            stage.Advance(2f, 3f); // 시험 경과 기록
            stage.State.Fail(); // 출격 실패 발생
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 결과 화면 진입
            Assert.That(player.Statistics.Finished, Is.True); // 결과 기록 확정
            Assert.That(player.Statistics.Cleared, Is.False); // 실패 상태 기록
            Assert.That(player.Statistics.CombatSeconds, Is.EqualTo(2f).Within(0.001f)); // 전투 시간 기록
            Assert.That(player.Statistics.TotalSeconds, Is.EqualTo(3f).Within(0.001f)); // 전체 시간 기록
            Assert.That(Time.timeScale, Is.Zero); // 결과 화면 정지
            flow.RequestReturn(); // 출격 준비 복귀 요청
            Assert.That(flow.ConfirmResultExit(true), Is.True); // 복귀 확정
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 출격 준비 복귀
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 시간 복원
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = previous; // 기존 시간 배율 복원
            Object.DestroyImmediate(scene); // 장면 객체 제거
            Object.DestroyImmediate(playerObject); // 플레이어 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 일시정지 후 복귀 시험
    public void ConfirmedPauseExitReturnsToPreparation() // 일시정지 복귀 확인
    { // 시험 시작
        GameObject holder = new GameObject("Menu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            GameFlowController flow = holder.AddComponent<GameFlowController>(); // 화면 흐름 구성
            flow.Begin(holder.AddComponent<Camera>()); // 타이틀 시작
            flow.OpenCharacterSelection(); // 캐릭터 선택
            flow.OpenPreparation(); // 출격 준비
            flow.StartRun(); // 출격 요청
            int leaves = 0; // 정리 이벤트 횟수
            flow.LeaveBattleRequested += () => leaves++; // 정리 이벤트 연결
            Assert.That(flow.ReturnFromBattle(), Is.True); // 확인된 출격 준비 복귀
            Assert.That(flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 준비 화면 표시
            Assert.That(leaves, Is.EqualTo(1)); // 전투 정리 한 번 요청
            Assert.That(flow.ReturnFromBattle(), Is.False); // 중복 복귀 차단
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 메뉴 객체 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
