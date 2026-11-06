using System; // 난수와 콜백 기능
using System.Collections.Generic; // 시험 난수 목록
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 전투 기능
using ProjectG; // 드롭과 전투 기능
public class UpgradeDropTests // 강화 아이템 드롭 시험
{ // 시험 시작
    private sealed class SequenceRandom : System.Random // 제어 가능한 난수
    { // 난수 시작
        private readonly Queue<int> values; // 순서대로 반환할 값
        public int Calls; // 난수 호출 수
        public SequenceRandom(params int[] sequence) // 시험 난수 생성
        { // 생성 시작
            values = new Queue<int>(sequence); // 시험 난수 보관
        } // 생성 종료
        public override int Next(int maxValue) // 지정 범위의 난수
        { // 선택 시작
            Calls++; // 호출 횟수 기록
            Assert.That(values.Count, Is.GreaterThan(0)); // 시험 난수 존재 확인
            int value = values.Dequeue(); // 다음 난수 꺼내기
            Assert.That(value, Is.GreaterThanOrEqualTo(0).And.LessThan(maxValue)); // 난수 범위 확인
            return value; // 지정된 값 반환
        } // 선택 종료
    } // 난수 종료
    private sealed class FakeEffects : IUpgradeDropEffects // 격리된 시험 효과
    { // 효과 시작
        public bool Eligible = true; // 획득 가능 상태
        public int Applied; // 실제 획득 횟수
        public bool CanApply(UpgradeItemId id, UpgradeItemGrade grade) // 현재 유효성 조회
        { // 조회 시작
            return Eligible; // 시험 유효성 반환
        } // 조회 종료
        public bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, out string feedback) // 효과 즉시 적용
        { // 적용 시작
            feedback = "획득 시험 +1"; // 시험 피드백
            if (!Eligible) // 획득 불가 확인
            { // 차단 시작
                return false; // 적용 생략
            } // 차단 종료
            Applied++; // 획득 횟수 증가
            return true; // 적용 성공
        } // 적용 종료
    } // 효과 종료
    [Test] // 확률 경계 시험
    public void RollUsesTenPercentAndOnlyNormalDefeats() // 드롭과 보스 경계 확인
    { // 시험 시작
        SequenceRandom random = new SequenceRandom(10, 9, 0, 0); // 실패와 성공 순서
        UpgradeDropRoller roller = new UpgradeDropRoller(random); // 드롭 추첨기
        Func<UpgradeItemId, UpgradeItemGrade, bool> available = (id, grade) => id == UpgradeItemId.Damage; // 하나의 유효 종류
        Assert.That(roller.TryRoll(EnemyRank.MidBoss, available, out _), Is.False); // 보스 드롭 미구현
        Assert.That(random.Calls, Is.Zero); // 보스 난수 미사용
        Assert.That(roller.TryRoll(EnemyRank.Normal, available, out _), Is.False); // 10 이상 드롭 실패
        Assert.That(roller.TryRoll(EnemyRank.Normal, available, out UpgradeDrop result), Is.True); // 9 이하 드롭 성공
        Assert.That(result.Id, Is.EqualTo(UpgradeItemId.Damage)); // 유효한 아이템
        Assert.That(result.Grade, Is.EqualTo(UpgradeItemGrade.I)); // 첫 등급 경계
    } // 시험 종료
    [Test] // 등급 가중 재분배 시험
    public void EmptyGradesRedistributeWeightsAndAllEmptyReturnsNoDrop() // 유효 등급 가중치 확인
    { // 시험 시작
        SequenceRandom random = new SequenceRandom(0, 24, 0, 0, 25, 0, 0); // 등급 경계와 빈 후보
        UpgradeDropRoller roller = new UpgradeDropRoller(random); // 드롭 추첨기
        Func<UpgradeItemId, UpgradeItemGrade, bool> available = (id, grade) => id == UpgradeItemId.Damage && (grade == UpgradeItemGrade.II || grade == UpgradeItemGrade.V); // 두 등급만 유효
        Assert.That(roller.TryRoll(EnemyRank.Normal, available, out UpgradeDrop first), Is.True); // 첫 재분배 추첨
        Assert.That(first.Grade, Is.EqualTo(UpgradeItemGrade.II)); // 25 가중치 마지막 값
        Assert.That(roller.TryRoll(EnemyRank.Normal, available, out UpgradeDrop second), Is.True); // 둘째 재분배 추첨
        Assert.That(second.Grade, Is.EqualTo(UpgradeItemGrade.V)); // 나머지 3 가중치 시작
        Assert.That(roller.TryRoll(EnemyRank.Normal, (id, grade) => false, out _), Is.False); // 모든 후보 무효 시 미드롭
    } // 시험 종료
    [Test] // 등급별 종류 균등 추첨 시험
    public void EligibleItemsAreSelectedWithoutDuplicatesOrBias() // 후보 선택 확인
    { // 시험 시작
        SequenceRandom random = new SequenceRandom(0, 0, 1); // 성공과 두 번째 종류
        UpgradeDropRoller roller = new UpgradeDropRoller(random); // 드롭 추첨기
        Assert.That(roller.TryRoll(EnemyRank.Normal, (id, grade) => grade == UpgradeItemGrade.I && (id == UpgradeItemId.Damage || id == UpgradeItemId.FireRate), out UpgradeDrop result), Is.True); // 두 후보 추첨
        Assert.That(result.Id, Is.EqualTo(UpgradeItemId.FireRate)); // 두 번째 후보 선택
    } // 시험 종료
    [Test] // 픽업과 재검증 시험
    public void PickupRechecksEligibilityAndDisplaysOnlySuccessfulEffect() // 획득 유효성과 피드백
    { // 시험 시작
        GameObject root = new GameObject("DropCamera"); // 격리 전투 카메라
        GameObject playerObject = new GameObject("DropPlayer"); // 격리 플레이어
        float oldScale = Time.timeScale; // 기존 시간 배율
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 진행 상태
            Camera camera = root.AddComponent<Camera>(); // 전투 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            FakeEffects effects = new FakeEffects(); // 시험 적용기
            UpgradeDropManager manager = root.AddComponent<UpgradeDropManager>(); // 드롭 관리자
            manager.Begin(player, camera, effects, new SequenceRandom(0, 0, 0)); // 유효한 드롭 연결
            Assert.That(manager.TryDrop(EnemyRank.Normal, Vector2.zero), Is.True); // 필드 아이템 생성
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 필드 보상 보관
            effects.Eligible = false; // 획득 전 조건 변경
            manager.Advance(0.1f); // 이동 접촉 검사
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 무효 보상 유지
            Assert.That(effects.Applied, Is.Zero); // 무효 효과 미적용
            Assert.That(manager.LastFeedback, Is.Null.Or.Empty); // 성공 안내 미표시
            effects.Eligible = true; // 효과 유효 복원
            manager.Advance(0.1f); // 재접촉 검사
            Assert.That(manager.ActiveCount, Is.Zero); // 성공 아이템 회수
            Assert.That(effects.Applied, Is.EqualTo(1)); // 단 한 번 적용
            Assert.That(manager.LastFeedback, Is.EqualTo("획득 시험 +1")); // 실제 효과 안내
            manager.Advance(0.1f); // 중복 검사
            Assert.That(effects.Applied, Is.EqualTo(1)); // 획득 중복 방지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(root); // 전투 객체 정리
            UnityEngine.Object.DestroyImmediate(playerObject); // 플레이어 정리
            Time.timeScale = oldScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [Test] // 정지와 출격 초기화 시험
    public void PauseAndRestartPreserveOrClearDropsAppropriately() // 정지·재출격 상태 확인
    { // 시험 시작
        GameObject root = new GameObject("DropCamera"); // 격리 전투 카메라
        GameObject playerObject = new GameObject("DropPlayer"); // 격리 플레이어
        float oldScale = Time.timeScale; // 기존 시간 배율
        try // 시험 시작
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 진행 상태
            Camera camera = root.AddComponent<Camera>(); // 전투 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            FakeEffects effects = new FakeEffects(); // 시험 적용기
            UpgradeDropManager manager = root.AddComponent<UpgradeDropManager>(); // 드롭 관리자
            manager.Begin(player, camera, effects, new SequenceRandom(0, 0, 0)); // 유효한 드롭 연결
            Assert.That(manager.TryDrop(EnemyRank.Normal, Vector2.zero), Is.True); // 필드 아이템 생성
            Time.timeScale = 0f; // 카드 또는 일시정지
            manager.Advance(10f); // 정지 중 진행 요청
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 아이템 상태 보존
            Assert.That(effects.Applied, Is.Zero); // 정지 중 효과 없음
            Time.timeScale = 1f; // 전투 진행 복원
            player.Begin(Vector2.zero); // 같은 객체에서 출격 초기화
            Assert.That(manager.ActiveCount, Is.Zero); // 이전 드롭 전체 정리
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(root); // 전투 객체 정리
            UnityEngine.Object.DestroyImmediate(playerObject); // 플레이어 정리
            Time.timeScale = oldScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [TestCase(UpgradeItemId.Heal, 6, "item_heal")] // 회복 물약 이미지 시험
    [TestCase(UpgradeItemId.Energy, 7, "item_energy")] // 에너지 결정 이미지 시험
    [TestCase(UpgradeItemId.Magnet, 8, "item_magnet")] // 회수 자석 이미지 시험
    public void NewItemsUseTheirGeneratedImage(UpgradeItemId id, int choice, string imageName) // 전용 이미지 연결 확인
    { // 시험 시작
        GameObject root = new GameObject("DropImageCamera"); // 격리 전투 객체
        GameObject playerObject = new GameObject("DropImagePlayer"); // 격리 플레이어
        try // 시험 범위 시작
        { // 시험 시작
            Camera camera = root.AddComponent<Camera>(); // 전투 카메라
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 출격 초기화
            UpgradeDropManager manager = root.AddComponent<UpgradeDropManager>(); // 드롭 관리자
            manager.Begin(player, camera, new FakeEffects(), new SequenceRandom(0, 0, choice)); // 지정 종류 드롭 연결
            Assert.That(manager.TryDrop(EnemyRank.Normal, Vector2.zero), Is.True); // 필드 아이템 생성
            Transform drop = root.transform.Find("UpgradeDrop"); // 드롭 표시 조회
            Assert.That(drop, Is.Not.Null); // 드롭 객체 확인
            Texture2D expected = Resources.Load<Texture2D>("BattleArt/" + imageName); // 전용 이미지 조회
            Assert.That(expected, Is.Not.Null); // 이미지 자산 존재 확인
            Assert.That(drop.GetComponent<SpriteRenderer>().sprite.texture, Is.EqualTo(expected)); // 해당 전용 이미지 사용
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(root); // 전투 객체 제거
            UnityEngine.Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 적 처치 이벤트 연결 시험
    public void RebindingDropsOnlyFromCurrentPlayerOnce() // 출격 이벤트 중복 방지 확인
    { // 시험 시작
        GameObject root = new GameObject("DropEventCamera"); // 격리 전투 객체
        GameObject firstObject = new GameObject("FirstDropPlayer"); // 첫 플레이어 객체
        GameObject secondObject = new GameObject("SecondDropPlayer"); // 다음 플레이어 객체
        try // 시험 범위 시작
        { // 시험 시작
            Camera camera = root.AddComponent<Camera>(); // 전투 카메라 구성
            BattlePlayer first = firstObject.AddComponent<BattlePlayer>(); // 첫 플레이어 구성
            first.Begin(Vector2.zero); // 첫 출격 초기화
            BattlePlayer second = secondObject.AddComponent<BattlePlayer>(); // 다음 플레이어 구성
            second.Begin(Vector2.zero); // 다음 출격 초기화
            UpgradeDropManager manager = root.AddComponent<UpgradeDropManager>(); // 드롭 관리자 구성
            FakeEffects effects = new FakeEffects(); // 유효한 시험 효과
            manager.Begin(first, camera, effects, new SequenceRandom(0, 0, 0)); // 첫 출격 연결
            manager.Begin(second, camera, effects, new SequenceRandom(0, 0, 0)); // 다음 출격 연결
            manager.Begin(second, camera, effects, new SequenceRandom(0, 0, 0)); // 같은 출격 재연결
            first.RegisterEnemyDefeat(EnemyRank.Normal, 0, Vector2.zero); // 이전 출격 처치 알림
            Assert.That(manager.ActiveCount, Is.Zero); // 이전 플레이어 이벤트 해제
            second.RegisterEnemyDefeat(EnemyRank.Normal, 0, Vector2.right); // 현재 출격 처치 알림
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 한 번만 드롭 추첨
            Assert.That(root.transform.Find("UpgradeDrop").position.x, Is.EqualTo(1f)); // 실제 처치 위치 표시
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(root); // 전투 객체 제거
            UnityEngine.Object.DestroyImmediate(firstObject); // 첫 플레이어 제거
            UnityEngine.Object.DestroyImmediate(secondObject); // 다음 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
