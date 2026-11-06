using NUnit.Framework; // 시험 도구
using ProjectG; // 경험치와 아이템 기능
using UnityEngine; // 유니티 객체
public class FallingPickupTests // 낙하 수집물 시험
{ // 시험 시작
    private sealed class ZeroRandom : System.Random // 고정 드롭 난수
    { // 난수 시작
        public override int Next(int maxValue) // 난수 요청
        { // 반환 시작
            return 0; // 첫 후보 선택
        } // 반환 종료
    } // 난수 종료
    private sealed class Effects : IUpgradeDropEffects // 격리된 획득 효과
    { // 효과 시작
        public int Acquired; // 실제 획득 횟수
        public bool CanApply(UpgradeItemId id, UpgradeItemGrade grade) // 적용 가능 확인
        { // 확인 시작
            return true; // 시험 효과 허용
        } // 확인 종료
        public bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, out string feedback) // 효과 적용
        { // 적용 시작
            Acquired++; // 획득 횟수 증가
            feedback = "획득"; // 시험 안내
            return true; // 획득 성공
        } // 적용 종료
    } // 효과 종료
    private GameObject cameraObject; // 격리 카메라 객체
    private GameObject playerObject; // 격리 플레이어 객체
    private Camera camera; // 전투 카메라
    private BattlePlayer player; // 플레이어 상태
    private BattleProgress progress; // 경험치와 카드 상태
    private UpgradeDropManager drops; // 강화 아이템 관리자
    private Effects effects; // 획득 확인 효과
    private float oldScale; // 기존 시간 배율
    [SetUp] // 매 시험 초기화
    public void SetUp() // 전투 상태 준비
    { // 준비 시작
        oldScale = Time.timeScale; // 기존 시간 보관
        Time.timeScale = 1f; // 전투 시간 시작
        cameraObject = new GameObject("FallingCamera"); // 격리 카메라 생성
        camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
        playerObject = new GameObject("FallingPlayer"); // 격리 플레이어 생성
        player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
        player.AssignCamera(camera); // 생성 탄환의 격리 장면 연결
        player.Begin(new Vector2(-2f, 0f)); // 전투 상태 초기화
        progress = playerObject.AddComponent<BattleProgress>(); // 경험치 구성
        progress.Begin(player, camera, 31); // 카드 시드와 경험치 연결
        effects = new Effects(); // 획득 효과 생성
        drops = cameraObject.AddComponent<UpgradeDropManager>(); // 강화 아이템 구성
        drops.Begin(player, camera, effects, new ZeroRandom()); // 고정 드롭 연결
    } // 준비 종료
    [TearDown] // 매 시험 정리
    public void TearDown() // 격리 상태 해제
    { // 정리 시작
        if (playerObject != null) // 플레이어 존재 확인
        { // 플레이어 제거 시작
            Object.DestroyImmediate(playerObject); // 플레이어와 경험치 상태 제거
        } // 플레이어 제거 종료
        if (cameraObject != null) // 카메라 존재 확인
        { // 카메라 제거 시작
            Object.DestroyImmediate(cameraObject); // 카메라와 수집물 제거
        } // 카메라 제거 종료
        Time.timeScale = oldScale; // 기존 시간 배율 복원
    } // 정리 종료
    private ExperienceShard MakeShard(Vector2 position, int amount) // 직접 시험 조각 생성
    { // 생성 시작
        GameObject holder = Object.Instantiate(Resources.Load<GameObject>("Prefabs/ExperienceShard")); // 실제 조각 프리팹 생성
        holder.transform.SetParent(cameraObject.transform, false); // 격리 카메라에 연결
        ExperienceShard shard = holder.GetComponent<ExperienceShard>(); // 조각 구성
        shard.Begin(progress, position, amount); // 조각 위치와 경험치 설정
        return shard; // 시험 조각 반환
    } // 생성 종료
    [Test] // 경험치 조각 낙하와 크기 시험
    public void ExperienceFallsAtTrialSpeedWithTripleDisplaySizeAndStopsWhenPaused() // 조각 낙하와 정지 확인
    { // 시험 시작
        ExperienceShard shard = MakeShard(new Vector2(4f, 3f), 3); // 회수 범위 밖 조각
        Assert.That(shard.transform.localScale.x, Is.EqualTo(0.9f).Within(0.0001f)); // 기존 크기의 세 배
        Assert.That(shard.transform.localScale.y, Is.EqualTo(0.9f).Within(0.0001f)); // 세로 크기의 세 배
        shard.Advance(1f); // 전투 시간 1초 진행
        Assert.That(shard.transform.position.y, Is.EqualTo(2.5f).Within(0.0001f)); // 초당 0.5 월드 단위 낙하
        Time.timeScale = 0f; // 카드 선택과 일시정지
        shard.Advance(2f); // 정지 중 진행 시도
        Assert.That(shard.transform.position.y, Is.EqualTo(2.5f).Within(0.0001f)); // 정지 중 낙하 방지
        Assert.That(progress.Level.Experience, Is.Zero); // 정지 중 경험치 미획득
    } // 시험 종료
    [Test] // 강화 아이템 낙하와 정지 시험
    public void UpgradeFallsAtTrialSpeedAndStopsWhenPaused() // 아이템 낙하와 정지 확인
    { // 시험 시작
        Assert.That(drops.TryDrop(EnemyRank.Normal, new Vector2(4f, 3f)), Is.True); // 범위 밖 아이템 생성
        Transform visual = cameraObject.transform.Find("UpgradeDrop"); // 아이템 표시 조회
        drops.Advance(1f); // 전투 시간 1초 진행
        Assert.That(visual.position.y, Is.EqualTo(2.5f).Within(0.0001f)); // 초당 0.5 월드 단위 낙하
        Time.timeScale = 0f; // 카드 선택과 일시정지
        drops.Advance(2f); // 정지 중 진행 시도
        Assert.That(visual.position.y, Is.EqualTo(2.5f).Within(0.0001f)); // 정지 중 낙하 방지
        Assert.That(effects.Acquired, Is.Zero); // 정지 중 획득 방지
        Assert.That(drops.ActiveCount, Is.EqualTo(1)); // 정지 중 아이템 유지
    } // 시험 종료
    [TestCase(0.85f, 1)] // 상대 이동 중 접촉
    [TestCase(-0.5f, 0)] // 고정 위치라면 생길 잘못된 접촉
    public void UpgradeCollisionUsesRelativeMovement(float height, int expected) // 플레이어와 아이템 동시 이동 판정
    { // 시험 시작
        Assert.That(drops.TryDrop(EnemyRank.Normal, new Vector2(0f, height)), Is.True); // 이동 경로 근처 아이템 생성
        player.Step(Vector2.right, false, false, false, 1f, -8f, 8f, -5f, 5f); // 플레이어 좌우 이동
        drops.Advance(1f); // 아이템 낙하와 접촉 판정
        Assert.That(effects.Acquired, Is.EqualTo(expected)); // 상대 이동 구간의 접촉만 획득
        Assert.That(drops.ActiveCount, Is.EqualTo(1 - expected)); // 획득 여부에 따른 유지
    } // 시험 종료
    [TestCase(0.69f, 1)] // 만료 직전 낙하 접촉
    [TestCase(0.75f, 0)] // 만료 후에만 닿는 낙하
    public void FallingUpgradeOnlyHitsDuringItsRemainingLifetime(float finalStart, int expected) // 만료 시간 내 상대 접촉
    { // 시험 시작
        player.Begin(Vector2.zero); // 화면 중심 대기
        float lifetime = UpgradeDropManager.TrialLifetime - 0.1f; // 만료 직전까지 경과 시간
        Assert.That(drops.TryDrop(EnemyRank.Normal, new Vector2(0f, finalStart + lifetime * 0.5f)), Is.True); // 유효 시간에 맞춘 시작 위치
        drops.Advance(lifetime); // 만료 전까지 낙하
        Assert.That(drops.ActiveCount, Is.EqualTo(1)); // 만료 전 아이템 유지
        drops.Advance(0.5f); // 남은 시간보다 긴 프레임
        Assert.That(effects.Acquired, Is.EqualTo(expected)); // 만료 전 접촉만 획득
        Assert.That(drops.ActiveCount, Is.Zero); // 획득 또는 만료 후 제거
        Assert.That(player.Statistics.DropsExpired, Is.EqualTo(1 - expected)); // 자연 만료 통계
    } // 시험 종료
    [Test] // 빠른 플레이어와 경험치 접촉 시험
    public void FastPlayerCanCollectFallingExperienceAlongMovement() // 상대 이동 중 조각 회수
    { // 시험 시작
        ExperienceShard shard = MakeShard(new Vector2(0f, 0.4f), 3); // 이동 경로의 조각 생성
        player.Step(Vector2.right, false, false, false, 1f, -8f, 8f, -5f, 5f); // 플레이어가 조각을 통과
        shard.Advance(1f); // 낙하와 회수 판정
        Assert.That(progress.Level.Experience, Is.EqualTo(3)); // 이동 구간의 경험치 회수
        Assert.That(shard.AvailableValue, Is.Zero); // 중복 회수 방지
    } // 시험 종료
    [Test] // 생성 이전 이동 제외 시험
    public void NewExperienceIgnoresPlayerMovementBeforeSpawn() // 생성 이전 경로 차단
    { // 시험 시작
        player.Step(Vector2.right, false, false, false, 1f, -8f, 8f, -5f, 5f); // 생성 위치를 먼저 지나감
        ExperienceShard shard = MakeShard(Vector2.zero, 3); // 플레이어가 지난 뒤 조각 생성
        shard.Advance(0.1f); // 생성 이후 낙하 판정
        Assert.That(progress.Level.Experience, Is.Zero); // 과거 이동으로 회수 차단
        Assert.That(shard.AvailableValue, Is.EqualTo(3)); // 미획득 조각 유지
    } // 시험 종료
    [Test] // 재출격 정리 시험
    public void RestartClearsFallingDropsAndExperience() // 새 출격 초기화 확인
    { // 시험 시작
        player.RegisterEnemyDefeat(EnemyRank.Normal, 3, new Vector2(4f, 3f)); // 경험치와 드롭 생성
        ExperienceShard shard = cameraObject.GetComponentInChildren<ExperienceShard>(); // 생성된 조각 조회
        Assert.That(shard, Is.Not.Null); // 경험치 조각 생성 확인
        Assert.That(drops.ActiveCount, Is.EqualTo(1)); // 강화 아이템 생성 확인
        player.Begin(Vector2.zero); // 같은 객체의 재출격
        Assert.That(shard == null || shard.AvailableValue == 0, Is.True); // 이전 조각 제거 또는 비활성화
        Assert.That(drops.ActiveCount, Is.Zero); // 이전 아이템 제거
        Assert.That(progress.Level.Experience, Is.Zero); // 새 출격 경험치 초기화
    } // 시험 종료
    [Test] // 화면 밖 경험치 재사용 시험
    public void OffscreenExperienceIsRecycledWithoutRemovingVisibleShards() // 화면 이탈과 풀 재사용 확인
    { // 시험 시작
        camera.orthographic = true; // 직교 전투 화면
        camera.orthographicSize = 5f; // 화면 아래 경계 설정
        player.RegisterEnemyDefeat(EnemyRank.Normal, 3, new Vector2(4f, -4.7f)); // 화면 아래쪽 조각 생성
        ExperienceShard shard = cameraObject.GetComponentInChildren<ExperienceShard>(); // 생성된 조각 조회
        Assert.That(shard, Is.Not.Null); // 조각 생성 확인
        shard.Advance(0.5f); // 경계 안쪽까지 낙하
        Assert.That(shard.AvailableValue, Is.EqualTo(3)); // 보이는 조각 유지
        Assert.That(progress.HasExperienceInRange(float.PositiveInfinity), Is.True); // 화면 안 자석 후보 유지
        shard.Advance(2f); // 조각 전체가 화면 아래로 이동
        Assert.That(shard.AvailableValue, Is.Zero); // 화면 밖 조각 비활성화
        Assert.That(progress.HasExperienceInRange(float.PositiveInfinity), Is.False); // 비활성 조각 자석 제외
        player.RegisterEnemyDefeat(EnemyRank.Normal, 4, new Vector2(4f, 3f)); // 새 조각 생성 요청
        ExperienceShard reused = cameraObject.GetComponentInChildren<ExperienceShard>(); // 현재 활성 조각 조회
        Assert.That(reused, Is.SameAs(shard)); // 기존 조각 재사용
        Assert.That(reused.AvailableValue, Is.EqualTo(4)); // 새 조각 경험치 초기화
        Assert.That(reused.transform.position.y, Is.EqualTo(3f).Within(0.0001f)); // 새 생성 위치 적용
    } // 시험 종료
} // 시험 종료
