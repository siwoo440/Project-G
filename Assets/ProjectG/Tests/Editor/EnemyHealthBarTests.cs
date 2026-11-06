using NUnit.Framework; // 테스트 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체
public class EnemyHealthBarTests // 적 체력바 시험
{ // 시험 시작
    [TestCase(EnemyKind.Aimed)] // 조준형 시험
    [TestCase(EnemyKind.Spread)] // 확산형 시험
    [TestCase(EnemyKind.Dash)] // 돌진형 시험
    [TestCase(EnemyKind.MidBoss)] // 중간 보스 시험
    [TestCase(EnemyKind.FinalBoss)] // 최종 보스 시험
    public void DamageShowsArtBelowBodyAtCurrentHealthRatio(EnemyKind kind) // 피해 후 위치와 비율 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(kind, null, null, null); // 적 초기화
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 체력바 조회
            Assert.That(bar, Is.Not.Null); // 체력바 존재 확인
            Assert.That(bar.gameObject.activeSelf, Is.False); // 최초 숨김 확인
            enemy.ApplyDamage(enemy.MaximumHealth * 0.2f, null); // 실제 체력 피해
            Assert.That(bar.gameObject.activeSelf, Is.True); // 피해 후 표시 확인
            SpriteRenderer frame = bar.Find("Frame").GetComponent<SpriteRenderer>(); // 외곽 이미지
            SpriteRenderer fill = bar.Find("Fill").GetComponent<SpriteRenderer>(); // 체력 이미지
            SpriteRenderer body = holder.transform.Find("EnemyBody").GetComponent<SpriteRenderer>(); // 몸체 이미지
            Assert.That(frame.sprite.texture.name, Is.EqualTo("ui_card_frame")); // 기존 시각 자산 확인
            Assert.That(fill.sprite.texture, Is.SameAs(Texture2D.whiteTexture)); // 연속 게이지 확인
            Assert.That(frame.bounds.max.y, Is.LessThan(body.bounds.min.y)); // 확대된 몸체 아래 배치 확인
            Assert.That(fill.bounds.size.x / frame.bounds.size.x, Is.EqualTo(0.72f).Within(0.02f)); // 가장자리 여백과 체력 비율 확인
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(0f)] // 빈 체력 시험
    [TestCase(0.2f)] // 이십 퍼센트 시험
    [TestCase(0.5f)] // 절반 체력 시험
    [TestCase(0.8f)] // 팔십 퍼센트 시험
    [TestCase(1f)] // 최대 체력 시험
    public void GaugeHasContinuousColorAndAnchoredLeftEdge(float ratio) // 연속 게이지와 고정 기준 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Spread, null, null, null); // 적 초기화
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 체력바 조회
            EnemyHealthBar gauge = bar.GetComponent<EnemyHealthBar>(); // 게이지 구성
            gauge.Show(ratio); // 체력 비율 적용
            SpriteRenderer frame = bar.Find("Frame").GetComponent<SpriteRenderer>(); // 외곽 표시
            SpriteRenderer fill = bar.Find("Fill").GetComponent<SpriteRenderer>(); // 내부 표시
            Assert.That(fill.sprite.texture, Is.SameAs(Texture2D.whiteTexture)); // 빈틈 없는 단색 원본
            Assert.That(fill.color.a, Is.EqualTo(1f).Within(0.001f)); // 불투명 게이지 확인
            Assert.That(fill.color.g, Is.GreaterThan(fill.color.r)); // 청록 계열 확인
            Assert.That(fill.bounds.size.x, Is.EqualTo(frame.bounds.size.x * 0.9f * ratio).Within(0.001f)); // 연속 게이지 폭 확인
            Assert.That(fill.bounds.min.x, Is.EqualTo(frame.bounds.min.x + frame.bounds.size.x * 0.05f).Within(0.001f)); // 왼쪽 기준 유지
            Assert.That(fill.bounds.max.y, Is.LessThan(frame.bounds.max.y)); // 금색 위쪽 테두리 보존
            Assert.That(fill.bounds.min.y, Is.GreaterThan(frame.bounds.min.y)); // 금색 아래쪽 테두리 보존
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 표시 시간 시험
    public void OnlyActualDamageShowsAndRefreshesTimer() // 피해와 남은 시간 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Spread, null, null, null); // 적 초기화
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 체력바 조회
            enemy.ApplyDamage(0f, null); // 영 피해 시도
            enemy.ApplyDamage(-1f, null); // 음수 피해 시도
            enemy.ApplyDamage(float.NaN, null); // 비정상 피해 시도
            enemy.ApplyDamage(float.PositiveInfinity, null); // 무한 피해 시도
            Assert.That(enemy.Health, Is.EqualTo(enemy.MaximumHealth)); // 비정상 피해 무시 확인
            Assert.That(bar.gameObject.activeSelf, Is.False); // 피해 없을 때 숨김 확인
            enemy.ApplyDamage(1f, null); // 최초 실제 피해
            enemy.Advance(1.5f); // 표시 시간 일부 경과
            enemy.ApplyDamage(1f, null); // 추가 실제 피해
            enemy.Advance(1f); // 최초 표시 시간 초과
            Assert.That(bar.gameObject.activeSelf, Is.True); // 추가 피해 갱신 확인
            enemy.Advance(1.01f); // 갱신 후 시험 시간 초과
            Assert.That(bar.gameObject.activeSelf, Is.False); // 표시 시간 만료 확인
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 재사용과 처치 시험
    public void DisabledOrDefeatedEnemyHidesAndReinitializesBar() // 비활성과 재사용 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Aimed, null, null, null); // 적 초기화
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 체력바 조회
            enemy.ApplyDamage(1f, null); // 실제 피해
            holder.SetActive(false); // 적 비활성화
            Assert.That(bar.gameObject.activeInHierarchy, Is.False); // 비활성 적의 표시 숨김 확인
            holder.SetActive(true); // 적 재활성화
            enemy.Begin(EnemyKind.Aimed, null, null, null); // 적 재사용 초기화
            Assert.That(holder.transform.Find("EnemyHealthBar"), Is.SameAs(bar)); // 동일 체력바 재사용 확인
            Assert.That(bar.gameObject.activeSelf, Is.False); // 재사용 초기 숨김 확인
            enemy.ApplyDamage(100f, null); // 적 처치
            Assert.That(holder.activeSelf, Is.False); // 적 퇴장 확인
            Assert.That(bar.gameObject.activeInHierarchy, Is.False); // 처치 시 표시 숨김
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 정지와 종료 시험
    public void PauseFreezesTimerAndPlayerDeathHidesBar() // 일시 정지와 종료 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        float previousTimeScale = Time.timeScale; // 원래 시간 배율
        try // 시험 범위
        { // 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 플레이어 초기화
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Spread, player, null, null); // 적 초기화
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 체력바 조회
            enemy.ApplyDamage(1f, null); // 실제 피해
            Time.timeScale = 0f; // 전투 시간 정지
            enemy.Advance(3f); // 정지 중 시간 진행 시도
            Assert.That(bar.gameObject.activeSelf, Is.True); // 정지 중 표시 유지
            Time.timeScale = 1f; // 전투 시간 재개
            enemy.Advance(1f); // 시간 진행
            Assert.That(bar.gameObject.activeSelf, Is.True); // 남은 시간 확인
            player.Survival.TryHit(10000); // 플레이어 사망
            enemy.Advance(0.1f); // 종료 상태 처리
            Assert.That(bar.gameObject.activeSelf, Is.False); // 게임 종료 시 숨김
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Time.timeScale = previousTimeScale; // 시간 배율 복원
            Object.DestroyImmediate(holder); // 적 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
