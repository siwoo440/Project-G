using NUnit.Framework; // 시험 도구
using ProjectG; // 전투와 영구 성장
using UnityEngine; // 유니티 객체
public sealed class RapidAchievementSettlementTests // 아리아 과제 실제 정산 시험
{ // 시험 시작
    private sealed class MemoryStore : IProgressStorage // 격리 저장소
    { // 저장소 시작
        public string Data; // 저장 원문
        public int Writes; // 성공 저장 횟수
        public bool FailWrites; // 저장 실패 설정
        public bool LoseResponse; // 저장 응답 누락 설정
        public ProgressReadStatus Read(out string data, out string error) // 현재 원문 조회
        { // 조회 시작
            data = Data; // 저장 원문 반환
            error = null; // 조회 오류 없음
            return Data == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 원문 존재 상태
        } // 조회 종료
        public bool Write(string expectedData, string data, out string error) // 비교 후 저장
        { // 저장 시작
            error = null; // 오류 초기화
            if (FailWrites || expectedData != Data) // 쓰기 실패 또는 충돌 확인
            { // 실패 시작
                error = "격리 저장 실패"; // 시험 오류
                return false; // 원문 보존
            } // 실패 종료
            Data = data; // 원자 저장 원문
            Writes++; // 저장 횟수 증가
            if (LoseResponse) // 저장 후 응답 누락 확인
            { // 누락 시작
                LoseResponse = false; // 일회성 누락 해제
                error = "격리 응답 누락"; // 응답 오류
                return false; // 쓰기 후 실패 응답
            } // 누락 종료
            return true; // 정상 저장 완료
        } // 저장 종료
    } // 저장소 종료
    private static GameSessionHost Start(GameObject holder, MemoryStore store, int precedingRuns = 0) // 격리 출격 생성
    { // 생성 시작
        PermanentProgressService progress = new PermanentProgressService(store); // 격리 성장 서비스
        Assert.That(progress.Load(out string error), Is.True, error); // 초기 성장 복원
        for (int index = 0; index < precedingRuns; index++) // 선행 출격 반복
        { // 반복 시작
            string id = "preceding-" + index; // 선행 출격 식별자
            Assert.That(progress.TryBegin(id, CharacterType.Rapid, out error), Is.True, error); // 선행 출격 시작
            Assert.That(progress.TrySettle(id, CharacterType.Rapid, false, out _, out error), Is.True, error); // 선행 실패 정산
        } // 반복 종료
        Camera camera = holder.AddComponent<Camera>(); // 격리 카메라
        GameSessionHost host = holder.AddComponent<GameSessionHost>(); // 격리 출격 관리자
        host.Begin(camera, progress); // 격리 서비스 연결
        Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
        Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
        Assert.That(host.Flow.StartRun(), Is.True); // 실제 출격 시작
        return host; // 출격 관리자 반환
    } // 생성 종료
    private static void Hit(GameObject holder, BattlePlayer player, int count, bool basic = true, bool focused = false) // 실제 적에 탄환 적중
    { // 적중 시작
        GameObject enemyObject = new GameObject("AchievementEnemy"); // 시험 적 객체
        enemyObject.transform.SetParent(holder.transform, false); // 시험 객체 소속
        EnemyActor enemy = EnemyActorTestFactory.AttachActor(enemyObject, EnemyKind.MidBoss); // 적 구성
        enemy.Begin(EnemyKind.MidBoss, player, null, null); // 생존 가능한 적 초기화
        for (int index = 0; index < count; index++) // 탄환 적중 반복
        { // 반복 시작
            GameObject shotObject = new GameObject("AchievementShot"); // 시험 탄환 객체
            shotObject.transform.SetParent(holder.transform, false); // 시험 객체 소속
            Projectile shot = shotObject.AddComponent<Projectile>(); // 탄환 구성
            shot.Begin(Vector2.up, 10f, 1f, basic, player); // 기본 또는 스킬 탄환 초기화
            if (focused) // 집중탄 시험 확인
            { // 집중 처리 시작
                shot.SetFocusWidth(0.5f); // 집중탄 판정과 표시 적용
            } // 집중 처리 종료
            Assert.That(shot.TryHit(enemy, false), Is.True); // 실제 적 적중 확인
            Object.DestroyImmediate(shotObject); // 시험 탄환 정리
        } // 반복 종료
    } // 적중 종료
    [TestCase(49, 0)] // 미달 횟수
    [TestCase(50, 1)] // 완료 횟수
    public void FailureSettlementAwardsOnlyAtThreshold(int hits, int expected) // 실패 결과 정산 경계
    { // 시험 시작
        GameObject holder = new GameObject("AchievementHost"); // 격리 객체
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 설정
            MemoryStore store = new MemoryStore(); // 격리 저장소
            GameSessionHost host = Start(holder, store); // 격리 출격
            Hit(holder, host.CurrentPlayer, hits); // 기본탄 실제 적중
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 정산 전 지급 방지
            int beforeWrites = store.Writes; // 정산 전 저장 횟수
            host.CurrentStage.State.Fail(); // 실제 실패 결과 발생
            Assert.That(host.Flow.Screen, Is.EqualTo(GameFlowScreen.Result)); // 결과 화면 확인
            Assert.That(store.Writes, Is.EqualTo(beforeWrites + 1)); // 정산과 과제 한 번 저장
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(expected)); // 횟수별 최초 증표 확인
            Assert.That(host.Progress.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.EqualTo(expected == 1)); // 과제 기록 확인
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 격리 객체 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 저장 실패와 숙련 동시 보상 시험
    public void FailureRetryAwardsAchievementAndMasteryExactlyOnce() // 실패 복구와 동시 지급 확인
    { // 시험 시작
        GameObject holder = new GameObject("AchievementRetry"); // 격리 객체
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 설정
            MemoryStore store = new MemoryStore(); // 격리 저장소
            GameSessionHost host = Start(holder, store, 5); // 숙련도 50 출격
            Hit(holder, host.CurrentPlayer, 50); // 과제 충족
            string before = store.Data; // 기존 저장 원문
            store.FailWrites = true; // 정산 실패 설정
            host.CurrentStage.State.Fail(); // 실제 실패 결과
            Assert.That(host.Flow.SettlementPending, Is.True); // 저장 재시도 대기
            Assert.That(store.Data, Is.EqualTo(before)); // 실패 전 원문 유지
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 실패 시 미지급
            store.FailWrites = false; // 저장 오류 해제
            store.LoseResponse = true; // 저장 후 응답 누락
            int beforeWrites = store.Writes; // 재시도 전 저장 횟수
            Assert.That(host.Flow.RetrySettlement(), Is.True); // 재시도와 원문 확인
            Assert.That(store.Writes, Is.EqualTo(beforeWrites + 1)); // 원자 저장 한 번
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(2)); // 숙련과 과제 증표 합산
            Assert.That(host.CurrentReceipt.CharacterMaterialsGained, Is.EqualTo(1)); // 기존 숙련 영수증 유지
            Assert.That(host.Progress.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.True); // 별도 과제 영수증
            string id = host.CurrentReceipt.RunId; // 저장된 출격 식별자
            Assert.That(host.Progress.TrySettle(id, CharacterType.Rapid, false, out SettlementReceipt receipt, out string error), Is.True, error); // 같은 출격 재조회
            Assert.That(receipt.AlreadySettled, Is.True); // 중복 정산 확인
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(2)); // 중복 지급 방지
            PermanentProgressService restored = new PermanentProgressService(store); // 재시작 서비스
            Assert.That(restored.Load(out error), Is.True, error); // 버전 이행 후 복원
            Assert.That(restored.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(2)); // 재시작 증표 유지
            Assert.That(restored.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.True); // 재시작 과제 유지
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 격리 객체 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 미정 경계 시험
    public void PauseAbandonAndSkillHitsDoNotAward() // 중도 포기와 스킬 제외
    { // 시험 시작
        GameObject holder = new GameObject("AchievementAbandon"); // 격리 객체
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 설정
            MemoryStore store = new MemoryStore(); // 격리 저장소
            GameSessionHost host = Start(holder, store); // 격리 출격
            Hit(holder, host.CurrentPlayer, 50, false); // 스킬탄 적중
            Assert.That(host.Flow.ReturnFromPause(), Is.True); // 중도 포기 정산
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 미정 경계 지급 방지
            Assert.That(host.Progress.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.False); // 과제 미기록 확인
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 격리 객체 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 클리어와 재출격 시험
    public void ClearAwardsCombinedBasicFocusAndNeverAwardsAgain() // 기본과 집중탄 및 중복 지급 확인
    { // 시험 시작
        GameObject holder = new GameObject("AchievementClear"); // 격리 객체
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 설정
            MemoryStore store = new MemoryStore(); // 격리 저장소
            GameSessionHost host = Start(holder, store); // 격리 출격
            Hit(holder, host.CurrentPlayer, 25); // 기본탄 적중
            Hit(holder, host.CurrentPlayer, 25, true, true); // 집중탄 적중
            Assert.That(host.CurrentPlayer.Statistics.Hits, Is.Zero); // 플레이어 피격 수와 분리
            StageProgression stage = host.CurrentStage.State; // 실제 스테이지 상태
            stage.Advance(65f, 65f); // 중간 보스 진입
            Assert.That(stage.DefeatBoss(), Is.True); // 중간 보스 통과
            stage.Advance(50f, 50f); // 최종 보스 진입
            Assert.That(stage.DefeatBoss(), Is.True); // 클리어 결과 발생
            Assert.That(host.CurrentReceipt.AchievementMaterialsGained, Is.EqualTo(1)); // 최초 과제 영수증 표시
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 최초 과제 증표
            string id = host.CurrentReceipt.RunId; // 완료 출격 식별자
            Assert.That(host.Progress.TrySettle(id, CharacterType.Rapid, true, out SettlementReceipt repeated, out string error), Is.True, error); // 저장된 영수증 재조회
            Assert.That(repeated.AchievementMaterialsGained, Is.EqualTo(1)); // 재조회 과제 보상 표시
            Assert.That(host.Flow.RequestReturn(), Is.True); // 결과 이탈 확인 요청
            Assert.That(host.Flow.ConfirmResultExit(true), Is.True); // 준비 화면 복귀
            Assert.That(host.Flow.OpenGrowth(), Is.True); // 성장 화면 진입
            Assert.That(host.Flow.GrowthSummary(), Does.Contain("아리아 첫 과제: 완료")); // 저장된 완료 상태 표시
            Object.DestroyImmediate(holder); // 첫 출격 정리
            holder = new GameObject("AchievementAgain"); // 새 출격 객체
            Time.timeScale = 1f; // 결과 정지 해제
            host = Start(holder, store); // 저장된 진행으로 새 출격
            Hit(holder, host.CurrentPlayer, 50); // 같은 조건 재달성
            host.CurrentStage.State.Fail(); // 새 출격 실패 정산
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 과제 중복 증표 방지
            Assert.That(host.CurrentReceipt.AchievementMaterialsGained, Is.Zero); // 이번 출격 과제 미지급 표시
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 격리 객체 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 유효하지 않은 적중 시험
    public void SkillTrainingAndZeroDamageDoNotCompleteAchievement() // 제외 대상과 무효 피해 확인
    { // 시험 시작
        GameObject holder = new GameObject("AchievementExclusions"); // 격리 객체
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 설정
            MemoryStore store = new MemoryStore(); // 격리 저장소
            GameSessionHost host = Start(holder, store); // 격리 출격
            BattlePlayer player = host.CurrentPlayer; // 현재 플레이어
            Hit(holder, player, 49); // 유효 적중 미달
            Hit(holder, player, 50, false); // 스킬탄 적중 제외
            GameObject enemyObject = new GameObject("ZeroDamageEnemy"); // 무효 피해 적
            enemyObject.transform.SetParent(holder.transform, false); // 격리 객체 소속
            EnemyActor enemy = EnemyActorTestFactory.AttachActor(enemyObject, EnemyKind.Aimed); // 적 구성
            enemy.Begin(EnemyKind.Aimed, player, null, null); // 일반 적 초기화
            GameObject trainingObject = new GameObject("TrainingTarget"); // 훈련 표적 객체
            trainingObject.transform.SetParent(holder.transform, false); // 격리 객체 소속
            TrainingTarget training = trainingObject.AddComponent<TrainingTarget>(); // 훈련 표적 구성
            for (int index = 0; index < 50; index++) // 제외 판정 반복
            { // 반복 시작
                GameObject shotObject = new GameObject("ExcludedShot"); // 시험 탄환
                shotObject.transform.SetParent(holder.transform, false); // 격리 객체 소속
                Projectile shot = shotObject.AddComponent<Projectile>(); // 탄환 구성
                shot.Begin(Vector2.up, 10f, 0f, true, player, 2); // 피해 없는 기본탄
                Assert.That(shot.TryHit(enemy, false), Is.True); // 무효 피해 충돌
                Assert.That(shot.TryHit(training, false), Is.True); // 훈련 표적 충돌
                Object.DestroyImmediate(shotObject); // 탄환 제거
            } // 반복 종료
            host.CurrentStage.State.Fail(); // 실제 실패 결과
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 제외 판정 과제 미달
            Assert.That(host.Progress.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.False); // 과제 미기록
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 격리 객체 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 관통과 반복 대상 시험
    public void PiercingCountsDistinctEnemiesOncePerProjectile() // 관통 적마다 한 번 기록
    { // 시험 시작
        GameObject holder = new GameObject("AchievementPiercing"); // 격리 객체
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 설정
            MemoryStore store = new MemoryStore(); // 격리 저장소
            GameSessionHost host = Start(holder, store); // 격리 출격
            BattlePlayer player = host.CurrentPlayer; // 현재 플레이어
            GameObject firstObject = new GameObject("FirstEnemy"); // 첫 적 객체
            firstObject.transform.SetParent(holder.transform, false); // 격리 객체 소속
            EnemyActor first = EnemyActorTestFactory.AttachActor(firstObject, EnemyKind.MidBoss); // 첫 적 구성
            first.Begin(EnemyKind.MidBoss, player, null, null); // 첫 적 초기화
            GameObject secondObject = new GameObject("SecondEnemy"); // 둘째 적 객체
            secondObject.transform.SetParent(holder.transform, false); // 격리 객체 소속
            EnemyActor second = EnemyActorTestFactory.AttachActor(secondObject, EnemyKind.MidBoss); // 둘째 적 구성
            second.Begin(EnemyKind.MidBoss, player, null, null); // 둘째 적 초기화
            for (int index = 0; index < 25; index++) // 관통탄 반복
            { // 반복 시작
                GameObject shotObject = new GameObject("PiercingShot"); // 관통탄 객체
                shotObject.transform.SetParent(holder.transform, false); // 격리 객체 소속
                Projectile shot = shotObject.AddComponent<Projectile>(); // 탄환 구성
                shot.Begin(Vector2.up, 10f, 1f, true, player, 3); // 세 번 판정 가능한 기본탄
                Assert.That(shot.TryHit(first, false), Is.True); // 첫 적 유효 적중
                Assert.That(shot.TryHit(first, false), Is.False); // 동일 적 중복 차단
                Assert.That(shot.TryHit(second, false), Is.True); // 둘째 적 유효 적중
                Object.DestroyImmediate(shotObject); // 탄환 제거
            } // 반복 종료
            host.CurrentStage.State.Fail(); // 실제 실패 결과
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.EqualTo(1)); // 각 적의 유효 적중 합산
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 격리 객체 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 이전 출격 탄환 시험
    public void OldProjectileCannotAddHitsToNewRun() // 출격 세대 분리 확인
    { // 시험 시작
        GameObject holder = new GameObject("AchievementGeneration"); // 격리 객체
        try // 시험 범위
        { // 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero, CharacterType.Rapid); // 첫 출격 초기화
            player.BindAchievementRun("first"); // 첫 출격 식별자
            GameObject shotObject = new GameObject("OldShot"); // 이전 출격 탄환
            shotObject.transform.SetParent(holder.transform, false); // 격리 객체 소속
            Projectile oldShot = shotObject.AddComponent<Projectile>(); // 탄환 구성
            oldShot.Begin(Vector2.up, 10f, 1f, true, player); // 첫 출격 탄환 생성
            player.Begin(Vector2.zero, CharacterType.Rapid); // 새 출격 초기화
            player.BindAchievementRun("second"); // 새 출격 식별자
            Hit(holder, player, 49); // 새 출격 유효 적중
            GameObject enemyObject = new GameObject("NewEnemy"); // 새 출격 적
            enemyObject.transform.SetParent(holder.transform, false); // 격리 객체 소속
            EnemyActor enemy = EnemyActorTestFactory.AttachActor(enemyObject, EnemyKind.MidBoss); // 적 구성
            enemy.Begin(EnemyKind.MidBoss, player, null, null); // 적 초기화
            Assert.That(oldShot.TryHit(enemy, false), Is.True); // 이전 탄환 충돌
            Assert.That(player.AchievementRun.Qualifies("second"), Is.False); // 새 출격의 오염 방지
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 격리 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 유효 횟수 달성 후 포기 시험
    public void AbandonAfterFiftyHitsDoesNotAward() // 미정 중도 포기 지급 차단
    { // 시험 시작
        GameObject holder = new GameObject("AchievementAbandonQualified"); // 격리 객체
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 설정
            MemoryStore store = new MemoryStore(); // 격리 저장소
            GameSessionHost host = Start(holder, store); // 격리 출격
            Hit(holder, host.CurrentPlayer, 50); // 유효 적중 조건 충족
            Assert.That(host.Flow.ReturnFromPause(), Is.True); // 중도 포기 정산
            Assert.That(host.Progress.CharacterMaterials(CharacterType.Rapid), Is.Zero); // 중도 포기 과제 미지급
            Assert.That(host.Progress.HasAchievement(CharacterAchievement.RapidBasicHits50), Is.False); // 과제 미기록
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 격리 객체 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
