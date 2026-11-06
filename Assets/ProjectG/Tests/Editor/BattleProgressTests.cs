using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 경험치와 카드 기능
public class BattleProgressTests // 전투 성장 흐름 시험
{ // 시험 시작
    [Test] // 레벨업 일시 정지와 연속 선택 시험
    public void MultipleLevelUpsPauseUntilEachCardIsChosen() // 카드 순차 선택 확인
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 초기화
            progress.CollectExperience(30); // 두 단계 경험치 획득
            Assert.That(progress.Level.PendingChoices, Is.EqualTo(2)); // 선택 두 번 예약
            Assert.That(progress.Offer.Length, Is.EqualTo(3)); // 세 장 제시
            Assert.That(Time.timeScale, Is.Zero); // 전투 일시 정지
            int first = System.Array.FindIndex(progress.Offer, card => card.Id != CardId.C06); // 교체가 아닌 카드 선택
            Assert.That(progress.Select(first), Is.True); // 첫 카드 선택
            Assert.That(progress.Level.PendingChoices, Is.EqualTo(1)); // 다음 카드 예약 유지
            Assert.That(progress.Offer.Length, Is.EqualTo(3)); // 다음 세 장 제시
            Assert.That(Time.timeScale, Is.Zero); // 연속 선택 중 정지 유지
            int second = System.Array.FindIndex(progress.Offer, card => card.Id != CardId.C06); // 다음 카드 선택
            Assert.That(progress.Select(second), Is.True); // 둘째 카드 선택
            Assert.That(progress.Level.PendingChoices, Is.Zero); // 선택 예약 해제
            Assert.That(progress.Offer, Is.Null); // 카드 화면 닫힘
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 재개 확인
            Assert.That(progress.Select(0), Is.False); // 중복 선택 차단
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 경험치 조각 수집 시험
    public void ExperienceShardRequiresProximityBeforeCollection() // 경험치 흡수 거리 확인
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        GameObject shardObject = new GameObject("Experience"); // 경험치 조각 생성
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 초기화
            shardObject.AddComponent<SpriteRenderer>().sprite = BattleVisuals.Crystal; // 시험 조각 이미지
            ExperienceShard shard = shardObject.AddComponent<ExperienceShard>(); // 조각 구성
            shard.Begin(progress, new Vector2(3f, 0f), 6); // 먼 경험치 조각
            shard.Advance(1f); // 거리 밖 시간 진행
            Assert.That(progress.Level.Experience, Is.Zero); // 거리 밖 수집 방지
            shardObject.transform.position = new Vector2(1f, 0f); // 회수 범위로 이동
            shard.Advance(1f); // 흡수 시간 진행
            Assert.That(progress.Level.Experience, Is.EqualTo(6)); // 경험치 수집 확인
            shard.Advance(1f); // 중복 수집 시도
            Assert.That(progress.Level.Experience, Is.EqualTo(6)); // 중복 획득 방지
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(shardObject); // 경험치 조각 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 장면 종료 시 시간 복원 시험
    public void ShuttingDownProgressRestoresTimeScale() // 정지 상태 정리 확인
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 0.5f; // 기존 게임 속도 설정
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 초기화
            progress.CollectExperience(10); // 레벨업 선택 열기
            Assert.That(Time.timeScale, Is.Zero); // 전투 일시 정지
            progress.Shutdown(); // 성장 구성 정리
            Assert.That(Time.timeScale, Is.EqualTo(0.5f)); // 기존 시간 배율 복원
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 공용 스킬 교체 확인 시험
    public void SkillReplacementRequiresConfirmationAndSupportsCancel() // 교체 확인과 취소
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 초기화
            progress.CollectExperience(10); // 카드 선택 열기
            progress.Offer[0] = CardCatalog.Find(CardId.C06); // 공용 산탄 선택지 설정
            Assert.That(progress.Select(0), Is.True); // 교체 요청
            Assert.That(progress.ConfirmingSwap, Is.True); // 확인 화면 표시
            Assert.That(player.Skills.Common, Is.EqualTo(CommonSkill.Shield)); // 확인 전 기존 스킬 유지
            Assert.That(progress.ConfirmSwap(false), Is.True); // 교체 취소
            Assert.That(progress.Level.PendingChoices, Is.EqualTo(1)); // 취소 후 보상 유지
            Assert.That(progress.Select(0), Is.True); // 다시 교체 요청
            Assert.That(progress.ConfirmSwap(true), Is.True); // 교체 확인
            Assert.That(player.Skills.Common, Is.EqualTo(CommonSkill.Scatter)); // 산탄 장착 확인
            Assert.That(progress.Level.PendingChoices, Is.Zero); // 선택 완료
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 재개
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 자석 카드 흡수 거리 시험
    public void MagnetCardExpandsActualPickupDistance() // 경험치 회수 거리 확인
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        GameObject shardObject = new GameObject("Experience"); // 경험치 조각 생성
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 초기화
            shardObject.AddComponent<SpriteRenderer>().sprite = BattleVisuals.Crystal; // 시험 조각 이미지
            ExperienceShard shard = shardObject.AddComponent<ExperienceShard>(); // 조각 구성
            shard.Begin(progress, new Vector2(1.7f, 0f), 6); // 기본 범위 밖 조각
            shard.Advance(1f); // 기본 범위 시험
            Assert.That(progress.Level.Experience, Is.Zero); // 조기 수집 방지
            Assert.That(player.ApplyCard(CardId.C09), Is.True); // 자석 장착
            shard.Advance(1f); // 확장 범위 시험
            Assert.That(progress.Level.Experience, Is.EqualTo(6)); // 확장 범위 수집
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(shardObject); // 조각 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 새 출격 전투 성장 초기화 시험
    public void RestartClearsCardsExperienceAndPendingPause() // 출격 초기화 확인
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율
        GameObject playerObject = new GameObject("Player"); // 플레이어 생성
        GameObject cameraObject = new GameObject("Camera"); // 카메라 생성
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 시험 시간 설정
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera); // 성장 초기화
            Assert.That(player.ApplyCard(CardId.C01), Is.True); // 첫 출격 카드 적용
            progress.CollectExperience(10); // 카드 선택 정지
            Assert.That(Time.timeScale, Is.Zero); // 전투 정지 확인
            player.Begin(Vector2.zero, CharacterType.Sniper); // 새 출격 시작
            Assert.That(progress.Level.Level, Is.EqualTo(1)); // 시작 레벨 복원
            Assert.That(progress.Level.Experience, Is.Zero); // 전투 경험치 초기화
            Assert.That(progress.Level.PendingChoices, Is.Zero); // 선택 예약 초기화
            Assert.That(player.Build.Cards.Count(CardId.C01), Is.Zero); // 이전 카드 초기화
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 시간 배율 복원
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
