using System; // 저장소 주입 기능
using System.Collections; // 실제 프레임 시험
using System.Reflection; // 로드 완료 상태 조회
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 출격과 저장
using UnityEngine; // 유니티 객체 기능
using UnityEngine.SceneManagement; // 실제 전투 씬 확인
using UnityEngine.TestTools; // 플레이 모드 시험
public class BattleSceneFailurePlayTests // 전투 씬 오류와 중복 시험
{ // 시험 시작
    private sealed class MemoryStore : IProgressStorage // 사용자 저장과 격리된 저장소
    { // 저장소 시작
        private string content; // 현재 저장 원문
        public bool FailWrites; // 시작 기록 실패 설정
        public int WriteAttempts; // 실제 저장 시도 수
        public ProgressReadStatus Read(out string data, out string error) // 저장 내용 조회
        { // 조회 시작
            data = content; // 격리 원문 전달
            error = null; // 오류 없음
            return content == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 저장 상태 반환
        } // 조회 종료
        public bool Write(string expected, string value, out string error) // 격리된 시작 기록
        { // 기록 시작
            WriteAttempts++; // 실제 저장 요청 기록
            if (FailWrites || content != expected) // 쓰기 오류와 충돌 확인
            { // 실패 시작
                error = "시험 저장 실패"; // 재현 가능한 오류
                return false; // 기존 저장 보존
            } // 실패 종료
            content = value; // 정상 저장 원문
            error = null; // 오류 없음
            return true; // 정상 기록
        } // 기록 종료
    } // 저장소 종료
    private static IEnumerator WaitForLoad(GameSessionHost host) // 실제 로딩 상태 종료 대기
    { // 대기 시작
        float deadline = Time.realtimeSinceStartup + 15f; // 시간 배율과 무관한 제한
        while (host != null && host.Flow.Screen == GameFlowScreen.Loading && Time.realtimeSinceStartup < deadline) // 실제 완료 또는 오류 확인
        { // 대기 범위
            yield return null; // 실제 다음 프레임
        } // 대기 종료
        Assert.That(host != null && host.Flow.Screen != GameFlowScreen.Loading, Is.True, "씬 로딩이 종료되지 않았습니다."); // 완료 또는 실패 확인
    } // 대기 종료
    private static IEnumerator WaitForUnloadedScene() // 실제 씬 언로드 완료 대기
    { // 대기 시작
        float deadline = Time.realtimeSinceStartup + 15f; // 시간 배율과 무관한 제한
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 현재 전투 씬 조회
        while (scene.IsValid() && scene.isLoaded && Time.realtimeSinceStartup < deadline) // 실제 언로드 확인
        { // 대기 범위
            yield return null; // 실제 다음 프레임
            scene = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 현재 씬 다시 조회
        } // 대기 종료
        Assert.That(!scene.IsValid() || !scene.isLoaded, Is.True, "전투 씬이 정리되지 않았습니다."); // 언로드 완료 확인
    } // 대기 종료
    private static void OpenPreparation(GameSessionHost host) // 실제 출격 준비 진입
    { // 준비 시작
        Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택
        Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비
    } // 준비 종료
    [UnityTest] // 활성 전투의 호스트 제거 시험
    public IEnumerator DestroyedHostUnloadsItsActiveBattleScene() // 실제 진행 중 씬 정리
    { // 시험 시작
        GameObject holder = new GameObject("DestroyedActiveHost"); // 격리 메뉴 객체
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService progress = new PermanentProgressService(store); // 실제 저장 서비스
        try // 시험 범위
        { // 범위 시작
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 실제 출격 관리자
            host.Begin(holder.AddComponent<Camera>(), progress); // 격리 메뉴와 저장 연결
            OpenPreparation(host); // 출격 준비 화면
            Assert.That(host.Flow.StartRun(), Is.True); // 실제 출격 요청
            yield return WaitForLoad(host); // 실제 전투 완료 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle), host.Flow.ErrorText); // 실제 전투 시작 확인
            Scene battle = host.CurrentPlayer.gameObject.scene; // 정확한 전투 씬 핸들
            Assert.That(battle.IsValid() && battle.isLoaded, Is.True); // 실제 전투 씬 확인
            UnityEngine.Object.Destroy(holder); // 활성 호스트 제거
            yield return null; // 실제 호스트 파괴 대기
            float deadline = Time.realtimeSinceStartup + 15f; // 시간 배율과 무관한 제한
            while (battle.isLoaded && Time.realtimeSinceStartup < deadline) // 해당 씬 언로드 대기
            { // 대기 시작
                yield return null; // 실제 다음 프레임
            } // 대기 종료
            Assert.That(battle.isLoaded, Is.False); // 정확한 전투 씬 언로드 확인
            Assert.That(store.WriteAttempts, Is.EqualTo(1)); // 임의 정산 저장 방지
            Assert.That(progress.HasRunHistory, Is.True); // 기존 출격 기록 보존
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (holder != null) // 남은 메뉴 확인
            { // 제거 시작
                UnityEngine.Object.Destroy(holder); // 메뉴 제거
            } // 제거 종료
            progress.Dispose(); // 격리 저장 해제
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 기록 실패와 씬 정리 시험
    public IEnumerator FailedStartWriteKeepsMenuAndDoesNotCreateAnActiveRun() // 씬 로드 후 저장 실패
    { // 시험 시작
        GameObject holder = new GameObject("FailStartHost"); // 격리 메뉴 객체
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService progress = new PermanentProgressService(store); // 실제 저장 서비스
        try // 시험 범위
        { // 범위 시작
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 실제 출격 관리자
            host.Begin(holder.AddComponent<Camera>(), progress); // 격리 메뉴와 저장 연결
            OpenPreparation(host); // 출격 준비 화면
            store.FailWrites = true; // 시작 저장 실패 유도
            Assert.That(host.Flow.StartRun(), Is.True); // 비동기 출격 요청 수락
            yield return WaitForLoad(host); // 실제 저장 실패까지 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Preparation)); // 기존 메뉴 보존
            Assert.That(host.Flow.ErrorText, Does.Contain("시험 저장 실패")); // 실제 저장 오류 표시
            Assert.That(host.CurrentPlayer, Is.Null); // 실제 전투 미시작
            Assert.That(progress.HasRunHistory, Is.False); // 활성 출격이나 정산 기록 없음
            Assert.That(store.WriteAttempts, Is.EqualTo(1)); // 시작 기록 한 번 시도
            yield return WaitForUnloadedScene(); // 준비된 전투 씬 정리 확인
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.Destroy(holder); // 메뉴 객체 제거
            progress.Dispose(); // 격리 저장 해제
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 로딩 중 중복 요청 시험
    public IEnumerator DuplicateStartDuringLoadingDoesNotCreateAnotherRunOrScene() // 중복 입력 차단
    { // 시험 시작
        GameObject holder = new GameObject("DuplicateStartHost"); // 격리 메뉴 객체
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService progress = new PermanentProgressService(store); // 실제 저장 서비스
        try // 시험 범위
        { // 범위 시작
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 실제 출격 관리자
            host.Begin(holder.AddComponent<Camera>(), progress); // 격리 메뉴와 저장 연결
            OpenPreparation(host); // 출격 준비 화면
            Assert.That(host.Flow.StartRun(), Is.True); // 첫 실제 출격 요청
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Loading)); // 실제 로딩 상태
            Assert.That(host.Flow.StartRun(), Is.False); // 같은 프레임 중복 요청 차단
            Assert.That(host.Flow.Back(), Is.False); // 로딩 중 화면 이탈 차단
            yield return WaitForLoad(host); // 실제 전투 완료 대기
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Battle), host.Flow.ErrorText); // 실제 출격 성공
            Assert.That(store.WriteAttempts, Is.EqualTo(1)); // 단일 출격 기록
            int battleCount = 0; // 실제 전투 씬 수
            for (int index = 0; index < SceneManager.sceneCount; index++) // 로드된 씬 순회
            { // 순회 시작
                if (SceneManager.GetSceneAt(index).path == "Assets/Scenes/BattleScene.unity") // 실제 전투 씬 확인
                { // 계수 시작
                    battleCount++; // 전투 씬 증가
                } // 계수 종료
            } // 순회 종료
            Assert.That(battleCount, Is.EqualTo(1)); // 단일 전투 씬 유지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.Destroy(holder); // 메뉴와 전투 제거
            progress.Dispose(); // 격리 저장 해제
        } // 정리 종료
        yield return WaitForUnloadedScene(); // 종료 씬 실제 정리
    } // 시험 종료
    [UnityTest] // 로드 중 호스트 파괴 시험
    public IEnumerator DestroyedHostDiscardsLateSceneWithoutWritingRun() // 늦은 완료 무효화
    { // 시험 시작
        GameObject holder = new GameObject("DestroyedLoadHost"); // 격리 메뉴 객체
        MemoryStore store = new MemoryStore(); // 격리 저장소
        PermanentProgressService progress = new PermanentProgressService(store); // 실제 저장 서비스
        FieldInfo loading = typeof(GameSessionHost).GetField("battleSceneLoading", BindingFlags.Static | BindingFlags.NonPublic); // 실제 로드 상태 조회
        try // 시험 범위
        { // 범위 시작
            Assert.That(loading, Is.Not.Null); // 로드 상태 연결 확인
            GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 실제 출격 관리자
            host.Begin(holder.AddComponent<Camera>(), progress); // 격리 메뉴와 저장 연결
            OpenPreparation(host); // 출격 준비 화면
            Assert.That(host.Flow.StartRun(), Is.True); // 실제 로드 요청
            UnityEngine.Object.Destroy(holder); // 로드 완료 전 메뉴 파괴
            yield return null; // 실제 호스트 파괴 프레임
            float deadline = Time.realtimeSinceStartup + 15f; // 시간 배율과 무관한 제한
            while ((bool)loading.GetValue(null) && Time.realtimeSinceStartup < deadline) // 늦은 완료 이벤트 대기
            { // 대기 시작
                yield return null; // 실제 다음 프레임
            } // 대기 종료
            Assert.That((bool)loading.GetValue(null), Is.False); // 로드 수신기 정리 확인
            yield return WaitForUnloadedScene(); // 늦게 도착한 전투 씬 정리
            Assert.That(store.WriteAttempts, Is.Zero); // 파괴 후 출격 기록 차단
            Assert.That(progress.HasRunHistory, Is.False); // 기존 성장 이력 보존
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (holder != null) // 남은 메뉴 확인
            { // 제거 시작
                UnityEngine.Object.Destroy(holder); // 메뉴 제거
            } // 제거 종료
            progress.Dispose(); // 격리 저장 해제
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
