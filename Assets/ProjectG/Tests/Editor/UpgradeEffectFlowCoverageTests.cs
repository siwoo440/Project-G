using System; // 열거형 순회 기능
using NUnit.Framework; // 자동 시험 도구
using ProjectG; // 전투 강화 기능
using UnityEngine; // 유니티 객체
public sealed class UpgradeEffectFlowCoverageTests // 실제 아이템 적용 경로 시험
{ // 시험 시작
    [TestCase(UpgradeItemGrade.I, 0.10f, 0.08f, 1, 0.06f, 0.10f, 10, 8, 2f)] // 첫 등급 시험값
    [TestCase(UpgradeItemGrade.II, 0.12f, 0.10f, 1, 0.10f, 0.15f, 15, 12, 4f)] // 두 번째 등급 시험값
    [TestCase(UpgradeItemGrade.III, 0.14f, 0.12f, 2, 0.12f, 0.20f, 20, 16, 6f)] // 세 번째 등급 시험값
    [TestCase(UpgradeItemGrade.IV, 0.16f, 0.14f, 2, 0.14f, 0.25f, 25, 20, 8f)] // 네 번째 등급 시험값
    [TestCase(UpgradeItemGrade.V, 0.18f, 0.16f, 3, 0.16f, 0.30f, 30, 25, 0f)] // 다섯 번째 등급 시험값
    public void EachGradeFlowsThroughAllNineItemEffects(UpgradeItemGrade grade, float damage, float fire, int pierce, float cooldown, float graze, int heal, int energy, float radius) // 등급별 실제 효과 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("CoverageCamera"); // 격리 카메라
        GameObject playerObject = new GameObject("CoveragePlayer"); // 격리 플레이어
        try // 시험 범위
        { // 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 전투 카메라 구성
            camera.orthographicSize = 5f; // 시험 화면 세로 반경
            camera.aspect = 4f / 3f; // 실제 전투 화면 가로 비율
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 첫 출격 초기화
            BattleProgress progress = playerObject.AddComponent<BattleProgress>(); // 성장 구성
            progress.Begin(player, camera, 15); // 경험치 이벤트 연결
            UpgradeDropEffects effects = new UpgradeDropEffects(player, progress); // 실제 아이템 적용기
            foreach (UpgradeItemId id in Enum.GetValues(typeof(UpgradeItemId))) // 아홉 아이템 순회
            { // 아이템 반복 시작
                player.Begin(Vector2.zero); // 효과별 독립 출격
                if (id == UpgradeItemId.Heal) // 회복 시험 확인
                { // 손상 시작
                    Assert.That(player.Survival.TryHit(60), Is.True); // 회복 전 체력 감소
                } // 손상 종료
                if (id == UpgradeItemId.Magnet) // 자석 범위 시험 확인
                { // 조각 생성 시작
                    float edge = grade == UpgradeItemGrade.V ? camera.orthographicSize * camera.aspect : radius; // 실제 화면 또는 등급 경계
                    player.RegisterEnemyDefeat(EnemyRank.Normal, 3, new Vector2(edge - 0.1f, 0f)); // 경계 안 경험치
                    player.RegisterEnemyDefeat(EnemyRank.Normal, 4, new Vector2(edge + 0.1f, 0f)); // 경계 밖 경험치
                } // 조각 생성 종료
                Assert.That(effects.CanApply(id, grade), Is.True, id.ToString()); // 실제 후보 허용
                Assert.That(effects.TryApply(id, grade, out string feedback), Is.True, id.ToString()); // 실제 획득과 적용
                Assert.That(feedback, Is.Not.Null.And.Not.Empty); // 획득 결과 표시
                Assert.That(player.Build.Upgrades.Count(id), Is.EqualTo(1), id.ToString()); // 독립 획득 기록
                switch (id) // 실제 전투 효과 확인
                { // 효과 확인 시작
                    case UpgradeItemId.Damage: // 화력 효과
                        Assert.That(player.Build.Damage(100f, false), Is.EqualTo(100f * (1f + damage)).Within(0.001f)); // 일반 공격 피해
                        Assert.That(player.Build.Damage(100f, true), Is.EqualTo(100f * (1f + damage)).Within(0.001f)); // 집중 공격 피해
                        break; // 화력 확인 종료
                    case UpgradeItemId.FireRate: // 연사 효과
                        Assert.That(player.Build.IntervalScale, Is.EqualTo(1f - fire).Within(0.001f)); // 실제 간격 비율
                        break; // 연사 확인 종료
                    case UpgradeItemId.Pierce: // 관통 효과
                        Assert.That(player.Build.ExtraPierce, Is.EqualTo(pierce)); // 실제 추가 관통
                        break; // 관통 확인 종료
                    case UpgradeItemId.UniqueCooldown: // 고유기 효과
                        Assert.That(player.Skills.UniqueCooldownReduction, Is.EqualTo(cooldown).Within(0.001f)); // 실제 스킬 감소율
                        break; // 고유기 확인 종료
                    case UpgradeItemId.Shield: // 방어막 효과
                        Assert.That(player.Survival.UpgradeShieldCharges, Is.EqualTo((int)grade)); // 등급별 초기 방어 횟수
                        Assert.That(effects.CanApply(id, grade), Is.False); // 동일 횟수 무효 후보 제외
                        Assert.That(player.Survival.TryHit(10), Is.False); // 보호막 한 번 소모
                        Assert.That(effects.CanApply(id, grade), Is.True); // 횟수 감소 후 갱신 허용
                        Assert.That(effects.TryApply(id, grade, out _), Is.True); // 같은 등급 보호막 갱신
                        Assert.That(player.Survival.UpgradeShieldCharges, Is.EqualTo((int)grade)); // 합산 없는 최대값 복원
                        break; // 방어막 확인 종료
                    case UpgradeItemId.Graze: // 근접 충전 효과
                        player.RegisterGraze(); // 실제 근접 충전 요청
                        Assert.That(player.Skills.ExactEnergy, Is.EqualTo(1f + graze).Within(0.001f)); // 등급별 소수 충전
                        break; // 근접 충전 확인 종료
                    case UpgradeItemId.Heal: // 회복 효과
                        Assert.That(player.Survival.Health, Is.EqualTo(40 + heal)); // 실제 등급별 체력
                        break; // 회복 확인 종료
                    case UpgradeItemId.Energy: // 에너지 효과
                        Assert.That(player.Skills.ExactEnergy, Is.EqualTo(energy).Within(0.001f)); // 실제 등급별 충전
                        break; // 에너지 확인 종료
                    case UpgradeItemId.Magnet: // 자석 효과
                        Assert.That(progress.Level.Experience, Is.EqualTo(3)); // 범위 안 경험치 획득
                        Assert.That(progress.AvailableExperienceInRange(20f), Is.EqualTo(4)); // 범위 밖 경험치 보존
                        break; // 자석 확인 종료
                } // 효과 확인 종료
            } // 아이템 반복 종료
        } // 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            UnityEngine.Object.DestroyImmediate(playerObject); // 격리 플레이어 제거
            UnityEngine.Object.DestroyImmediate(cameraObject); // 격리 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 클래스 종료
