using System.Collections.Generic; // 재현 결과 목록
using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 전투 기능
using ProjectG; // 전투 드롭 기능
public class DropSeedReplayTests // 출격 시드의 드롭 재현 시험
{ // 시험 시작
    private sealed class SequenceRandom : System.Random // 지정한 순서의 시험 난수
    { // 난수 시작
        private readonly Queue<int> values = new Queue<int>(); // 순서대로 반환할 값
        public SequenceRandom(params int[] sequence) // 시험 난수 생성
        { // 생성 시작
            foreach (int value in sequence) // 지정 값 순회
            { // 추가 시작
                values.Enqueue(value); // 순서 보관
            } // 추가 종료
        } // 생성 종료
        public override int Next(int maxValue) // 다음 시험 난수 조회
        { // 조회 시작
            Assert.That(values.Count, Is.GreaterThan(0)); // 남은 난수 확인
            int value = values.Dequeue(); // 다음 시험 값
            Assert.That(value, Is.GreaterThanOrEqualTo(0).And.LessThan(maxValue)); // 요청 범위 확인
            return value; // 지정된 시험 값
        } // 조회 종료
    } // 난수 종료
    private sealed class ValidEffects : IUpgradeDropEffects // 항상 유효한 시험 효과
    { // 효과 시작
        public bool CanApply(UpgradeItemId id, UpgradeItemGrade grade) // 드롭 후보 확인
        { // 확인 시작
            return true; // 모든 시험 후보 허용
        } // 확인 종료
        public bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, out string feedback) // 시험 획득 처리
        { // 처리 시작
            feedback = "시험"; // 획득 안내
            return true; // 획득 성공
        } // 처리 종료
    } // 효과 종료
    [Test] // 실제 전투 구성의 재출격 시험
    public void BattleSetupSeedReplaysDropAttemptsAndEachItemAfterRestart() // 같은 시드의 출격 재현
    { // 시험 시작
        GameObject cameraObject = new GameObject("SeedReplayCamera"); // 시험 카메라 객체
        GameObject playerObject = null; // 전투 플레이어 참조
        try // 시험 범위 시작
        { // 시험 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라 구성
            playerObject = BattleSetup.Create(camera, 4421); // 지정 시드의 실제 전투 구성
            BattlePlayer player = playerObject.GetComponent<BattlePlayer>(); // 전투 플레이어 조회
            UpgradeDropManager manager = cameraObject.GetComponent<UpgradeDropManager>(); // 실제 드롭 관리자 조회
            List<string> first = DrawAttempts(manager, player); // 첫 출격 추첨 결과
            Assert.That(player.Statistics.DropsSpawned, Is.GreaterThan(0)); // 실제 드롭 존재 확인
            Assert.That(player.Statistics.DropAttempts, Is.EqualTo(100)); // 첫 출격 추첨 수
            player.Begin(new Vector2(0f, -3f)); // 같은 플레이어의 새 출격
            Assert.That(manager.ActiveCount, Is.Zero); // 이전 출격 드롭 정리
            List<string> second = DrawAttempts(manager, player); // 같은 입력의 재출격 추첨
            Assert.That(player.Statistics.DropAttempts, Is.EqualTo(100)); // 재출격 추첨 수
            CollectionAssert.AreEqual(first, second); // 각 추첨의 생성 여부·종류·등급 일치
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (playerObject != null) // 플레이어 존재 확인
            { // 제거 시작
                Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
            } // 제거 종료
            Object.DestroyImmediate(cameraObject); // 카메라와 드롭 제거
        } // 정리 종료
    } // 시험 종료
    private static List<string> DrawAttempts(UpgradeDropManager manager, BattlePlayer player) // 동일한 처치 입력의 드롭 수집
    { // 수집 시작
        List<string> results = new List<string>(); // 순서별 결과 목록
        for (int index = 0; index < 100; index++) // 일반 적 처치 백 회
        { // 추첨 반복 시작
            bool dropped = manager.TryDrop(EnemyRank.Normal, new Vector2(index, 0f)); // 같은 순서의 드롭 요청
            if (!dropped) // 드롭 실패 확인
            { // 미드롭 시작
                results.Add("없음"); // 미드롭 기록
                continue; // 다음 처치 확인
            } // 미드롭 종료
            DropRecord record = player.Statistics.Drops[player.Statistics.Drops.Count - 1]; // 생성된 종류와 등급
            results.Add(record.Id + ":" + record.Grade); // 드롭 결과 기록
        } // 추첨 반복 종료
        return results; // 전체 순서 반환
    } // 수집 종료
    [Test] // 사용자 정의 난수 연결 시험
    public void CustomRandomRemainsInUseAcrossSamePlayerRestart() // 주입 난수의 기존 동작 보존
    { // 시험 시작
        GameObject cameraObject = new GameObject("CustomRandomCamera"); // 시험 카메라 객체
        GameObject playerObject = new GameObject("CustomRandomPlayer"); // 시험 플레이어 객체
        try // 시험 범위 시작
        { // 시험 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            UpgradeDropManager manager = cameraObject.AddComponent<UpgradeDropManager>(); // 드롭 관리자 구성
            manager.Begin(player, camera, new ValidEffects(), new SequenceRandom(0, 0, 0, 10)); // 주입 난수 연결
            Assert.That(manager.TryDrop(EnemyRank.Normal, Vector2.zero), Is.True); // 지정된 첫 드롭
            player.Begin(Vector2.zero); // 같은 객체의 새 출격
            Assert.That(manager.ActiveCount, Is.Zero); // 이전 드롭 정리
            Assert.That(manager.TryDrop(EnemyRank.Normal, Vector2.zero), Is.False); // 주입 난수의 다음 값 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 카메라와 드롭 제거
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료