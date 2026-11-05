using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 카드 전투 기능
public class CardEffectTests // 카드 효과 시험
{ // 시험 시작
    [Test] // 공격 강화 시험
    public void BasicDamageAndFireRateApplyToRealShots() // 실제 탄환 강화 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
        try // 시험 시작
        { // 시험 범위 시작
            player.Begin(Vector2.zero); // 속사형 시작
            Assert.That(player.ApplyCard(CardId.C01), Is.True); // 피해 카드 선택
            Assert.That(player.ApplyCard(CardId.C02), Is.True); // 발사 속도 카드 선택
            Assert.That(player.ApplyCard(CardId.C03), Is.True); // 관통 카드 선택
            player.Step(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f); // 첫 자동 사격
            Projectile[] shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None); // 탄환 조회
            Assert.That(shots.Length, Is.EqualTo(2)); // 속사 탄환 수 확인
            Assert.That(shots[0].Damage, Is.EqualTo(3.3f).Within(0.001f)); // 소수 피해 유지
            Assert.That(shots[0].RemainingHits, Is.EqualTo(2)); // 추가 관통 확인
            player.Step(Vector2.zero, false, false, false, 0.23f, -8f, 8f, -5f, 5f); // 단축 간격 경과
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.EqualTo(4)); // 두 번째 사격 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 탄환 순회
            { // 정리 시작
                Object.DestroyImmediate(shot.gameObject); // 탄환 제거
            } // 정리 종료
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 전용 카드 시험
    public void CharacterCardsModifyComboAndWeakPoint() // 전용 효과 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        GameObject targetObject = new GameObject("Target"); // 표적 생성
        GameObject bulletObject = new GameObject("Bullet"); // 탄환 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 속사형 시작
            Assert.That(player.ApplyCard(CardId.R02), Is.True); // 첫 분기 즉시 선택
            Assert.That(player.ApplyCard(CardId.R01), Is.True); // 유지 카드 적용
            Assert.That(player.ApplyCard(CardId.R02), Is.False); // 전용 카드 중복 방지
            Assert.That(player.Combo.GracePeriod, Is.EqualTo(3f).Within(0.001f)); // 유지 시간 증가
            player.Step(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f); // 확산 사격
            Projectile[] rapidShots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None); // 확산 탄환 조회
            Assert.That(rapidShots.Length, Is.EqualTo(2)); // 확산 탄환 두 발
            Assert.That(Mathf.Abs(rapidShots[0].transform.position.x), Is.EqualTo(0.126f).Within(0.001f)); // 사격 위치 폭 증가
            foreach (Projectile rapidShot in rapidShots) // 확산 탄환 순회
            { // 정리 시작
                Object.DestroyImmediate(rapidShot.gameObject); // 확산 탄환 제거
            } // 정리 종료
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 전환
            Assert.That(player.ApplyCard(CardId.R01), Is.False); // 다른 캐릭터 카드 제외
            Assert.That(player.ApplyCard(CardId.S01), Is.True); // 약점 카드 적용
            TrainingTarget target = targetObject.AddComponent<TrainingTarget>(); // 표적 구성
            Projectile bullet = bulletObject.AddComponent<Projectile>(); // 탄환 구성
            bullet.Begin(Vector2.up, 5f, 100, true, player); // 시험 탄환
            bullet.TryHit(target, true); // 약점 적중
            Assert.That(target.TotalDamage, Is.EqualTo(165f).Within(0.001f)); // 약점 추가 피해 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(bulletObject); // 탄환 제거
            Object.DestroyImmediate(targetObject); // 표적 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 회복과 고유기 쿨타임 시험
    public void RecoveryAndUniqueCooldownCardsApply() // 생존과 스킬 효과 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 속사형 시작
            Assert.That(player.ApplyCard(CardId.C12), Is.False); // 최대 체력 회복 제외
            player.Survival.TryHit(30); // 시험 피해
            Assert.That(player.ApplyCard(CardId.C12), Is.True); // 회복 카드 적용
            Assert.That(player.Survival.Health, Is.EqualTo(90)); // 체력 20 회복
            Assert.That(player.ApplyCard(CardId.F04), Is.True); // 시험 대체 회복 적용
            Assert.That(player.Survival.Health, Is.EqualTo(100)); // 대체 회복량 확인
            Assert.That(player.ApplyCard(CardId.F04), Is.False); // 최대 체력 회복 제외
            Assert.That(player.ApplyCard(CardId.F05), Is.True); // 시험 에너지 충전 적용
            Assert.That(player.Skills.Energy, Is.EqualTo(5)); // 대체 충전량 확인
            Assert.That(player.ApplyCard(CardId.C05), Is.True); // 고유기 순환 적용
            Assert.That(player.TryUseRapid(), Is.True); // 고유 스킬 사용
            player.Skills.Advance(3f); // 스킬 활성 종료
            Assert.That(player.Skills.RapidCooldown, Is.EqualTo(7.2f).Within(0.001f)); // 쿨타임 10% 감소
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 저격 집중 카드 시험
    public void SniperFocusCardChangesActualFocusedProjectile() // 저격 집중 피해 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 시작
            Assert.That(player.ApplyCard(CardId.S02), Is.True); // 전용 집중 카드 선택
            player.Step(Vector2.zero, false, true, false, 0.001f, -8f, 8f, -5f, 5f); // 집중 사격
            Projectile[] shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None); // 탄환 조회
            Assert.That(shots.Length, Is.EqualTo(1)); // 집중 탄환 수
            Assert.That(shots[0].Damage, Is.EqualTo(30f).Within(0.001f)); // 집중 피해 유지
            Assert.That(shots[0].RemainingHits, Is.EqualTo(3)); // 집중 탄환 관통 증가
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 탄환 순회
            { // 정리 시작
                Object.DestroyImmediate(shot.gameObject); // 탄환 제거
            } // 정리 종료
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
