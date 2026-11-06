using System; // 난수와 배열 기능
using System.Collections.Generic; // 카드 결과 목록
using System.Linq; // 카드 식별자 변환
using NUnit.Framework; // 자동 시험 기능
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 전투 성장 기능
public class CardSeedTests // 카드 후보 난수 재현 시험
{ // 시험 시작
    private static CardId[] SampleSequence(int seed, bool delay) // 독립 출격의 카드 후보 수집
    { // 수집 시작
        float previous = Time.timeScale; // 기존 시간 배율 보관
        GameObject playerObject = new GameObject("SeedPlayer"); // 시험 플레이어 객체
        GameObject cameraObject = new GameObject("SeedCamera"); // 시험 카메라 객체
        try // 시험 범위 시작
        { // 준비 시작
            Time.timeScale = 1f; // 전투 시간 설정
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 상태 초기화
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 카드 성장 구성
            progress.Begin(player, camera, seed); // 명시 시드로 성장 초기화
            progress.Blocked = delay; // 카드 열기 지연 여부
            progress.CollectExperience(10); // 첫 카드 선택 예약
            if (delay) // 전환 지연 시험 확인
            { // 지연 시작
                Assert.That(progress.Offer, Is.Null); // 지연 중 후보 미생성
                progress.CollectExperience(0); // 지연 중 재검사
                progress.Blocked = false; // 지연 해제
                progress.CollectExperience(0); // 선택 열기 요청
            } // 지연 종료
            Assert.That(progress.Offer, Is.Not.Null); // 첫 후보 확인
            List<CardId> result = progress.Offer.Select(card => card.Id).ToList(); // 첫 후보 기록
            progress.CollectExperience(0); // 정지 중 중복 열기 시도
            int choice = Array.FindIndex(progress.Offer, card => card.Id != CardId.C06); // 확인 창 없는 카드 선택
            Assert.That(choice, Is.GreaterThanOrEqualTo(0)); // 유효 선택 확인
            Assert.That(progress.Select(choice), Is.True); // 카드 선택과 재개
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 시간 복원
            progress.CollectExperience(16); // 다음 레벨 경험치 획득
            Assert.That(progress.Offer, Is.Not.Null); // 둘째 후보 확인
            result.AddRange(progress.Offer.Select(card => card.Id)); // 둘째 후보 기록
            return result.ToArray(); // 두 차례 결과 반환
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(playerObject); // 플레이어 제거
            UnityEngine.Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 배율 복원
        } // 정리 종료
    } // 수집 종료
    [Test] // 독립 출격과 선택 지연 시험
    public void SameSeedProducesSameCardsAfterPauseAndResume() // 시드별 두 후보 순서 확인
    { // 시험 시작
        CardId[] first = SampleSequence(7123, false); // 즉시 선택 출격
        CardId[] replay = SampleSequence(7123, true); // 지연된 독립 출격
        Assert.That(replay, Is.EqualTo(first)); // 같은 상태와 시드의 결과 일치
    } // 시험 종료
    [TestCase(7)] // 첫 시드 시험
    [TestCase(274)] // 다른 시드 시험
    public void SeedUsesTheSameRandomSequenceAsTheCardCatalog(int seed) // 실제 난수 시드 반영 확인
    { // 시험 시작
        CardId[] actual = SampleSequence(seed, false); // 해당 시드 출격 결과
        CardBuild build = new CardBuild(); // 초기 카드 보유 상태
        CardData[] expected = CardCatalog.Offer(build.Cards, CharacterType.Rapid, CommonSkill.Shield, false, new System.Random(seed), true, build); // 같은 난수의 직접 후보
        Assert.That(actual.Take(3), Is.EqualTo(expected.Select(card => card.Id))); // 시드별 첫 후보 일치
    } // 시험 종료
    [Test] // 같은 구성 요소의 재출격 시험
    public void SeededRestartBeginsTheSameIndependentSequence() // 재출격 난수 초기화 확인
    { // 시험 시작
        float previous = Time.timeScale; // 기존 시간 배율 보관
        GameObject playerObject = new GameObject("RestartSeedPlayer"); // 시험 플레이어 객체
        GameObject cameraObject = new GameObject("RestartSeedCamera"); // 시험 카메라 객체
        try // 시험 범위 시작
        { // 준비 시작
            Time.timeScale = 1f; // 전투 시간 설정
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 첫 출격 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 카드 성장 구성
            progress.Begin(player, camera, 981); // 고정 시드 설정
            progress.CollectExperience(10); // 첫 출격 후보 생성
            CardId[] first = progress.Offer.Select(card => card.Id).ToArray(); // 첫 후보 보관
            player.Begin(Vector2.zero); // 같은 객체의 새 출격
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 이전 선택의 시간 복원
            progress.CollectExperience(10); // 새 출격 후보 생성
            Assert.That(progress.Offer.Select(card => card.Id), Is.EqualTo(first)); // 독립 출격의 시드 재현
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(playerObject); // 플레이어 제거
            UnityEngine.Object.DestroyImmediate(cameraObject); // 카메라 제거
            Time.timeScale = previous; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 카드 난수 시드 기록 시험
    public void ActiveSeedIsExposedAndClearedForUnseededRuns() // 현재 시드와 일반 난수 구분
    { // 시험 시작
        GameObject playerObject = new GameObject("RecordedSeedPlayer"); // 시험 플레이어 객체
        GameObject cameraObject = new GameObject("RecordedSeedCamera"); // 시험 카메라 객체
        try // 시험 범위 시작
        { // 준비 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 상태 초기화
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 카드 성장 구성
            progress.Begin(player, camera, -981); // 음수 시드 설정
            Assert.That(progress.CardSeed, Is.EqualTo(-981)); // 실제 사용 시드 확인
            player.Begin(Vector2.zero); // 같은 구성의 새 출격
            Assert.That(progress.CardSeed, Is.EqualTo(-981)); // 재출격 시드 유지
            progress.Begin(player, camera); // 일반 난수 재초기화
            Assert.That(progress.CardSeed, Is.Null); // 시드 없는 동작 구분
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(playerObject); // 플레이어 제거
            UnityEngine.Object.DestroyImmediate(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
