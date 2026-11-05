using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 객체 기능
using ProjectG; // 카드 전투 기능
public class CardSupportTests // 보조 공격과 아이템 시험
{ // 시험 시작
    [Test] // 공격 드론 시험
    public void DroneFiresSeparateSkillProjectile() // 보조 사격 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Assert.That(player.ApplyCard(CardId.C07), Is.True); // 드론 카드 적용
            player.Step(Vector2.zero, false, false, false, 0.01f, -8f, 8f, -5f, 5f); // 보조 사격 진행
            Projectile[] shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None); // 탄환 조회
            Assert.That(System.Array.Exists(shots, shot => !shot.IsBasic && shot.Damage == 6f), Is.True); // 보조 피해 확인
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
    [Test] // 회전 칼날 시험
    public void BladeDamagesNearbyTargetOncePerSecond() // 보조 접촉 피해 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        GameObject targetObject = new GameObject("Target"); // 표적 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            TrainingTarget target = targetObject.AddComponent<TrainingTarget>(); // 표적 구성
            targetObject.transform.position = Vector2.right * 0.5f; // 근접 표적 배치
            Assert.That(player.ApplyCard(CardId.C08), Is.True); // 칼날 카드 적용
            player.Step(Vector2.zero, false, false, false, 0.01f, -8f, 8f, -5f, 5f); // 첫 접촉
            Assert.That(target.TotalDamage, Is.EqualTo(8f)); // 첫 피해 확인
            player.Step(Vector2.zero, false, false, false, 0.1f, -8f, 8f, -5f, 5f); // 접촉 중 재공격 대기
            Assert.That(target.TotalDamage, Is.EqualTo(8f)); // 중복 피해 방지
            player.Step(Vector2.zero, false, false, false, 2f, -8f, 8f, -5f, 5f); // 한 바퀴 회전
            Assert.That(target.TotalDamage, Is.EqualTo(16f)); // 재접촉 피해 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 탄환 순회
            { // 정리 시작
                Object.DestroyImmediate(shot.gameObject); // 탄환 제거
            } // 정리 종료
            Object.DestroyImmediate(targetObject); // 표적 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 피격 후 보호 장치 시험
    public void ReactiveShieldBlocksOneLaterHitAndRecharges() // 보호 장치 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            Assert.That(player.ApplyCard(CardId.C10), Is.True); // 보호 장치 장착
            Assert.That(player.Survival.TryHit(10), Is.True); // 첫 피해 적용
            Assert.That(player.Survival.HasShield, Is.True); // 피격 후 방어막
            player.Survival.Advance(1f); // 무적 시간 종료
            Assert.That(player.Survival.TryHit(10), Is.False); // 다음 피해 방어
            Assert.That(player.Survival.HasShield, Is.False); // 방어막 소모 확인
            player.Survival.Advance(29f); // 재충전 완료
            Assert.That(player.Survival.TryHit(10), Is.True); // 재충전 후 피해
            Assert.That(player.Survival.HasShield, Is.True); // 보호 장치 재발동
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 집중 탄환 폭 시험
    public void FocusLensNarrowsActualTargetContact() // 집중 폭 판정 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        GameObject targetObject = new GameObject("Target"); // 표적 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            TrainingTarget target = targetObject.AddComponent<TrainingTarget>(); // 표적 구성
            Vector2 from = new Vector2(0.69f, -1f); // 판정 경계 시작
            Vector2 to = new Vector2(0.69f, 1f); // 판정 경계 끝
            Assert.That(target.Intersects(from, to, out bool beforeWeak, 0.05f), Is.True); // 기존 집중 폭 접촉
            Assert.That(player.ApplyCard(CardId.C11), Is.True); // 첫 렌즈 적용
            Assert.That(player.ApplyCard(CardId.C11), Is.True); // 둘째 렌즈 적용
            Assert.That(player.ApplyCard(CardId.C11), Is.True); // 셋째 렌즈 적용
            Assert.That(target.Intersects(from, to, out bool afterWeak, 0.05f * player.Build.FocusWidth), Is.False); // 축소된 집중 폭 확인
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(targetObject); // 표적 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 칼날 위치와 대상별 재공격 시험
    public void BladeHitsOnlyContactingTargetsWithIndependentCooldowns() // 실제 칼날 접촉 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 생성
        GameObject firstObject = new GameObject("FirstTarget"); // 첫 표적 생성
        GameObject secondObject = new GameObject("SecondTarget"); // 둘째 표적 생성
        try // 시험 시작
        { // 시험 범위 시작
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 전투 시작
            TrainingTarget first = firstObject.AddComponent<TrainingTarget>(); // 첫 표적 구성
            TrainingTarget second = secondObject.AddComponent<TrainingTarget>(); // 둘째 표적 구성
            firstObject.transform.position = Vector2.zero; // 칼날과 떨어진 표적
            secondObject.transform.position = Vector2.right * 5f; // 범위 밖 표적
            Assert.That(player.ApplyCard(CardId.C08), Is.True); // 칼날 카드 적용
            player.Step(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f); // 칼날 회전
            Assert.That(first.TotalDamage, Is.Zero); // 칼날 미접촉 피해 방지
            firstObject.transform.position = Vector2.right; // 첫 표적을 칼날 위치로 이동
            player.Step(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f); // 첫 접촉
            Assert.That(first.TotalDamage, Is.EqualTo(8f)); // 첫 표적 피해
            secondObject.transform.position = Vector2.right; // 둘째 표적을 칼날 위치로 이동
            player.Step(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f); // 둘째 접촉
            Assert.That(first.TotalDamage, Is.EqualTo(8f)); // 첫 표적 재공격 대기
            Assert.That(second.TotalDamage, Is.EqualTo(8f)); // 둘째 표적 독립 피해
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            foreach (Projectile shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) // 탄환 순회
            { // 정리 시작
                Object.DestroyImmediate(shot.gameObject); // 탄환 제거
            } // 정리 종료
            Object.DestroyImmediate(secondObject); // 둘째 표적 제거
            Object.DestroyImmediate(firstObject); // 첫 표적 제거
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
