using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 스킬 연결 기능
public class SkillIntegrationTests // 플레이어 스킬 연결 시험
{ // 시험 시작
    [Test] // 그레이즈 자원 연결 시험
    public void GrazeChargesUltimateEnergy() // 그레이즈 충전 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 시험 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.Skills, Is.Not.Null); // 스킬 상태 확인
            player.RegisterGraze(); // 그레이즈 등록
            Assert.That(player.Skills.Energy, Is.EqualTo(1)); // 에너지 충전 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 보호막 연결 시험
    public void CommonShieldBlocksOneHit() // 보호막 발동 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 시험 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.TryUseCommon(), Is.True); // 보호막 발동
            Assert.That(player.Survival.HasShield, Is.True); // 보호막 상태 확인
            Assert.That(player.Survival.TryHit(10), Is.False); // 첫 피해 방어
            Assert.That(player.TryUseCommon(), Is.False); // 쿨타임 중 재사용 차단
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 공용 산탄 연결 시험
    public void EquippedScatterFiresThreeDamageTwelveProjectiles() // 공용 산탄 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 시험 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.Skills, Is.Not.Null); // 스킬 상태 확인
            player.Skills.Equip(CommonSkill.Scatter); // 공용 산탄 장착
            Assert.That(player.TryUseCommon(), Is.True); // 산탄 발동
            Projectile[] shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None); // 생성 탄환 확인
            Assert.That(shots.Length, Is.EqualTo(3)); // 산탄 세 발 확인
            foreach (Projectile shot in shots) // 산탄 순회
            { // 순회 시작
                Assert.That(shot.Damage, Is.EqualTo(12)); // 시험 피해량 확인
            } // 순회 종료
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 생성 탄환 순회
            { // 정리 시작
                Object.DestroyImmediate(shot.gameObject); // 시험 탄환 제거
            } // 정리 종료
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 필살기 연결 시험
    public void UltimateConsumesEnergyClearsBulletsAndEmitsDamage() // 필살기 효과 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("TestCamera"); // 시험 카메라 생성
        Camera camera = cameraObject.AddComponent<Camera>(); // 카메라 구성 요소
        EnemyBulletPool pool = cameraObject.AddComponent<EnemyBulletPool>(); // 적탄 풀 생성
        GameObject holder = new GameObject("TestPlayer"); // 시험 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.AssignCamera(camera); // 시험 카메라 연결
            player.AssignBulletPool(pool); // 시험 적탄 풀 연결
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.Skills, Is.Not.Null); // 스킬 상태 확인
            EnemyBullet bullet = pool.Spawn(Vector2.zero, Vector2.zero, 0.06f, 10, -5f, player); // 화면 안 적탄 생성
            for (int index = 0; index < 100; index++) // 에너지 충전 반복
            { // 반복 시작
                player.RegisterGraze(); // 그레이즈 등록
            } // 반복 종료
            int damage = 0; // 전달 피해량
            player.UltimateDamageRequested += value => damage = value; // 적 피해 요청 관찰
            Assert.That(player.TryUseUltimate(), Is.True); // 필살기 발동
            Assert.That(player.Skills.Energy, Is.Zero); // 에너지 소모 확인
            Assert.That(bullet.gameObject.activeSelf, Is.False); // 적탄 소거 확인
            Assert.That(damage, Is.EqualTo(80)); // 시험용 피해 요청 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(cameraObject); // 시험 카메라 제거
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 고유 스킬 발사 간격 시험
    public void RapidSkillShortensActualFireInterval() // 실제 발사 간격 확인
    { // 시험 시작
        GameObject holder = new GameObject("TestPlayer"); // 시험 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성 요소
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 전투 초기화
            Assert.That(player.TryUseRapid(), Is.True); // 고유 스킬 발동
            player.Step(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f); // 첫 자동 사격
            player.Step(Vector2.zero, false, false, false, 0.175f, -8f, 8f, -5f, 5f); // 단축된 발사 간격 경과
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.EqualTo(4)); // 두 차례 발사 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 생성 탄환 순회
            { // 정리 시작
                Object.DestroyImmediate(shot.gameObject); // 시험 탄환 제거
            } // 정리 종료
            Object.DestroyImmediate(holder); // 시험 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 사격 중 스킬 사용 시험
    public void RapidSkillScalesRemainingShotDelay() // 남은 사격 시간 단축 확인
    { // 시험 시작
        BattleSession session = new BattleSession(Vector2.zero); // 전투 상태 생성
        Assert.That(session.Advance(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f), Is.EqualTo(2)); // 첫 발사 확인
        Assert.That(session.Advance(Vector2.zero, false, false, false, 0.01f, -8f, 8f, -5f, 5f), Is.Zero); // 발사 간격 진행
        Assert.That(session.Advance(Vector2.zero, false, false, false, 0.169f, -8f, 8f, -5f, 5f, 0.7f), Is.EqualTo(2)); // 남은 간격 단축 확인
    } // 시험 종료
} // 시험 종료
