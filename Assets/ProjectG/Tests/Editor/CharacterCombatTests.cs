using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 규칙
using UnityEngine; // 유니티 객체
public class CharacterCombatTests // 캐릭터 전투 시험
{ // 시험 시작
    [Test] // 누적과 감소 시험
    public void RapidBonusCapsAndDecaysAfterTwoSeconds() // 누적 피해 확인
    { // 시험 시작
        RapidCombo combo = new RapidCombo(); // 누적 상태 생성
        for (int index = 0; index < 60; index++) // 적중 반복
        { // 반복 시작
            combo.RegisterHit(); // 기본 공격 적중
        } // 반복 종료
        Assert.That(combo.Bonus, Is.EqualTo(0.5f).Within(0.0001f)); // 최대치 확인
        combo.Advance(2f); // 유지 시간 경과
        Assert.That(combo.Bonus, Is.EqualTo(0.5f).Within(0.0001f)); // 감소 전 확인
        combo.Advance(4f); // 감소 시간 경과
        Assert.That(combo.Bonus, Is.EqualTo(0.4f).Within(0.0001f)); // 선형 감소 확인
        combo.RegisterHit(); // 재적중 처리
        combo.Advance(1f); // 유지 시간 확인
        Assert.That(combo.Bonus, Is.EqualTo(0.41f).Within(0.0001f)); // 재적중 갱신 확인
        combo.Advance(50f); // 충분한 감소 시간
        Assert.That(combo.Bonus, Is.Zero.Within(0.0001f)); // 하한 확인
    } // 시험 종료
    [Test] // 저격 일반 사격 시험
    public void SniperFiresOnePiercingShotWithSpecifiedDamage() // 저격 탄환 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 전투 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 선택
            player.Step(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f); // 일반 공격
            Projectile[] shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None); // 탄환 목록
            Assert.That(shots.Length, Is.EqualTo(1)); // 탄환 수 확인
            Assert.That(shots[0].Damage, Is.EqualTo(14)); // 기본 피해 확인
            Assert.That(shots[0].RemainingHits, Is.EqualTo(2)); // 추가 관통 확인
        } // 시험 범위 종료
        finally // 정리 처리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 탄환 순회
            { // 순회 시작
                Object.DestroyImmediate(shot.gameObject); // 탄환 제거
            } // 순회 종료
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 약점과 중복 판정 시험
    public void PiercingShotHitsEachTargetOnceAndAppliesWeakPoint() // 표적별 판정 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 전투 구성 요소
        GameObject first = new GameObject("FirstTarget"); // 첫 표적 생성
        GameObject second = new GameObject("SecondTarget"); // 둘째 표적 생성
        GameObject bullet = new GameObject("TestBullet"); // 탄환 생성
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 선택
            TrainingTarget one = first.AddComponent<TrainingTarget>(); // 첫 표적 구성
            TrainingTarget two = second.AddComponent<TrainingTarget>(); // 둘째 표적 구성
            Projectile shot = bullet.AddComponent<Projectile>(); // 탄환 구성
            shot.Begin(Vector2.up, 5f, 14, true, player, 2); // 저격 탄환 초기화
            Assert.That(shot.TryHit(one, true), Is.True); // 첫 약점 적중
            Assert.That(one.TotalDamage, Is.EqualTo(21f).Within(0.001f)); // 약점 피해 확인
            Assert.That(shot.TryHit(one, false), Is.False); // 중복 판정 차단
            Assert.That(shot.TryHit(two, false), Is.True); // 둘째 표적 관통
            Assert.That(two.TotalDamage, Is.EqualTo(14f).Within(0.001f)); // 일반 피해 확인
            Assert.That(shot.RemainingHits, Is.Zero); // 관통 한도 확인
        } // 시험 범위 종료
        finally // 정리 처리
        { // 정리 시작
            Object.DestroyImmediate(bullet); // 탄환 제거
            Object.DestroyImmediate(first); // 첫 표적 제거
            Object.DestroyImmediate(second); // 둘째 표적 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 속사 기본 공격 누적 시험
    public void OnlyBasicShotsBuildAndReceiveRapidBonus() // 속사 적중 구분 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        GameObject targetObject = new GameObject("TestTarget"); // 표적 생성
        GameObject basicObject = new GameObject("BasicShot"); // 기본 탄환 생성
        GameObject skillObject = new GameObject("SkillShot"); // 스킬 탄환 생성
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 속사형 시작
            TrainingTarget target = targetObject.AddComponent<TrainingTarget>(); // 표적 구성
            Projectile basic = basicObject.AddComponent<Projectile>(); // 기본 탄환 구성
            basic.Begin(Vector2.up, 5f, 100, true, player); // 기본 탄환 초기화
            Assert.That(basic.TryHit(target, false), Is.True); // 기본탄 적중
            Assert.That(player.Combo.Bonus, Is.EqualTo(0.01f).Within(0.0001f)); // 누적 증가 확인
            Assert.That(basic.TryHit(target, true), Is.False); // 한 적 중복 차단
            Projectile skill = skillObject.AddComponent<Projectile>(); // 스킬 탄환 구성
            skill.Begin(Vector2.up, 5f, 100, false, player); // 스킬 탄환 초기화
            Assert.That(skill.TryHit(target, false), Is.True); // 스킬 적중
            Assert.That(player.Combo.Bonus, Is.EqualTo(0.01f).Within(0.0001f)); // 스킬 누적 제외
            Assert.That(target.TotalDamage, Is.EqualTo(200f).Within(0.001f)); // 스킬 피해 보너스 제외
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(basicObject); // 기본 탄환 제거
            Object.DestroyImmediate(skillObject); // 스킬 탄환 제거
            Object.DestroyImmediate(targetObject); // 표적 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 이동 구간 판정 시험
    public void WeakPointRequiresAlignmentEvenForFastShots() // 약점 위치 판정 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestTarget"); // 표적 생성
        try // 시험 시작
        { // 시험 범위 시작
            TrainingTarget target = holder.AddComponent<TrainingTarget>(); // 표적 구성
            Assert.That(target.Intersects(new Vector2(0f, -1f), new Vector2(0f, 1f), out bool aligned), Is.True); // 빠른 중앙 탄환 적중
            Assert.That(aligned, Is.True); // 중앙 약점 확인
            Assert.That(target.Intersects(new Vector2(0.5f, -1f), new Vector2(0.5f, 1f), out bool side), Is.True); // 측면 탄환 적중
            Assert.That(side, Is.False); // 측면 일반 피해 확인
            Assert.That(target.Intersects(new Vector2(2f, -1f), new Vector2(2f, 1f), out bool missed), Is.False); // 표적 바깥 확인
            Assert.That(missed, Is.False); // 약점 이탈 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 표적 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 저격 고유 스킬 시험
    public void SniperSkillFiresPiercingShotAndStartsCooldown() // 저격 스킬 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 시작
            Assert.That(player.TryUseRapid(), Is.True); // 고유 스킬 발동
            Projectile[] shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None); // 스킬 탄환 목록
            Assert.That(shots.Length, Is.EqualTo(1)); // 탄환 수 확인
            Assert.That(shots[0].Damage, Is.EqualTo(100)); // 시험 피해 확인
            Assert.That(shots[0].IsBasic, Is.False); // 기본 공격 구분
            Assert.That(shots[0].RemainingHits, Is.GreaterThan(2)); // 관통 능력 확인
            Assert.That(player.Skills.RapidCooldown, Is.EqualTo(10f)); // 쿨타임 확인
            Assert.That(player.TryUseRapid(), Is.False); // 중복 발동 차단
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 탄환 순회
            { // 순회 시작
                Object.DestroyImmediate(shot.gameObject); // 탄환 제거
            } // 순회 종료
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 저격 필살기 범위 시험
    public void SniperUltimateAffectsOnlyBeamAndClearableBullets() // 직선 광선 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(cameraObject); // 적탄 풀
        GameObject holder = new GameObject("TestPlayer"); // 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        GameObject targetObject = new GameObject("TestTarget"); // 중앙 표적 생성
        GameObject otherObject = new GameObject("OtherTarget"); // 측면 표적 생성
        try // 시험 시작
        { // 시험 범위 시작
            camera.orthographic = true; // 직교 카메라
            camera.orthographicSize = 5f; // 화면 세로 반경
            player.AssignCamera(camera); // 카메라 연결
            player.AssignBulletPool(pool); // 적탄 풀 연결
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 시작
            targetObject.transform.position = new Vector2(0f, 2f); // 중앙 표적 배치
            otherObject.transform.position = new Vector2(2f, 2f); // 측면 표적 배치
            TrainingTarget target = targetObject.AddComponent<TrainingTarget>(); // 중앙 표적 구성
            TrainingTarget other = otherObject.AddComponent<TrainingTarget>(); // 측면 표적 구성
            EnemyBullet inside = pool.Spawn(new Vector2(0f, 1f), Vector2.zero, 0.05f, 10, -5f, player); // 광선 안 적탄
            EnemyBullet outside = pool.Spawn(new Vector2(2f, 1f), Vector2.zero, 0.05f, 10, -5f, player); // 광선 밖 적탄
            EnemyBullet protectedBullet = pool.Spawn(new Vector2(0f, 3f), Vector2.zero, 0.05f, 10, -5f, player, false); // 소거 불가 적탄
            for (int index = 0; index < 100; index++) // 필살기 자원 충전
            { // 반복 시작
                player.RegisterGraze(); // 그레이즈 등록
            } // 반복 종료
            Assert.That(player.TryUseUltimate(), Is.True); // 필살기 발동
            Assert.That(target.TotalDamage, Is.EqualTo(330f).Within(0.001f)); // 약점 광선 피해 확인
            Assert.That(other.TotalDamage, Is.Zero); // 광선 밖 피해 제외
            Assert.That(inside.gameObject.activeSelf, Is.False); // 광선 안 적탄 소거
            Assert.That(outside.gameObject.activeSelf, Is.True); // 광선 밖 적탄 유지
            Assert.That(protectedBullet.gameObject.activeSelf, Is.True); // 소거 불가 적탄 유지
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (BeamEffect effect in Object.FindObjectsByType<BeamEffect>(FindObjectsSortMode.None)) // 광선 표시 순회
            { // 순회 시작
                Object.DestroyImmediate(effect.gameObject); // 시험 광선 제거
            } // 순회 종료
            Object.DestroyImmediate(targetObject); // 중앙 표적 제거
            Object.DestroyImmediate(otherObject); // 측면 표적 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라와 풀 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
