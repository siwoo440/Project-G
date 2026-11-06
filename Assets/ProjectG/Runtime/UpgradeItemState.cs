using System; // 회수 콜백 기능
using UnityEngine; // 유니티 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum UpgradeItemId // 강화 아이템 종류
    { // 종류 시작
        Damage, FireRate, Pierce, UniqueCooldown, Shield, Graze, Heal, Energy, Magnet // 기존 여섯 종류와 즉시 아이템
    } // 종류 종료
    public enum UpgradeItemGrade // 강화 아이템 등급
    { // 등급 시작
        I = 1, II, III, IV, V // 희귀도 순서
    } // 등급 종료
    public sealed class UpgradeItemState // 출격 중 아이템 보유 상태
    { // 상태 시작
        private readonly int[] counts = new int[9]; // 종류별 획득 횟수
        public float DamageBonus // 피해 증가 합계
        { // 속성 시작
            get; // 증가량 조회
            private set; // 증가량 변경
        } // 속성 종료
        public float FireRateBonus // 발사 간격 감소 합계
        { // 속성 시작
            get; // 감소량 조회
            private set; // 감소량 변경
        } // 속성 종료
        public int PierceBonus // 추가 관통 합계
        { // 속성 시작
            get; // 관통 조회
            private set; // 관통 변경
        } // 속성 종료
        public float UniqueCooldownBonus // 고유기 대기 감소 합계
        { // 속성 시작
            get; // 감소량 조회
            private set; // 감소량 변경
        } // 속성 종료
        public float GrazeBonus // 근접 충전 증가 합계
        { // 속성 시작
            get; // 증가량 조회
            private set; // 증가량 변경
        } // 속성 종료
        public int Count(UpgradeItemId id) // 아이템 획득 횟수
        { // 조회 시작
            int index = (int)id; // 종류 번호
            return index >= 0 && index < counts.Length ? counts[index] : 0; // 유효 횟수 반환
        } // 조회 종료
        public bool CanApply(UpgradeItemId id, BattlePlayer player) // 기존 시험 후보 확인
        { // 확인 시작
            if (id == UpgradeItemId.Heal || id == UpgradeItemId.Energy || id == UpgradeItemId.Magnet) // 신규 아이템 등급 확인
            { // 기존 후보 제외 시작
                return false; // 등급 없는 신규 보상 차단
            } // 기존 후보 제외 종료
            if (id == UpgradeItemId.Shield && ValidPlayer(player)) // 기존 보호막 시험 호환
            { // 호환 시작
                return true; // 기존 반복 갱신 허용
            } // 호환 종료
            return CanApply(id, LegacyGrade(id), player); // 기존 효과 기준 확인
        } // 확인 종료
        public bool CanApply(UpgradeItemId id, UpgradeItemGrade grade, BattlePlayer player, bool hasExperience = false) // 등급별 유효 후보 확인
        { // 확인 시작
            int index = (int)id; // 종류 번호
            if (!ValidPlayer(player) || player.Build.Upgrades != this || index < 0 || index >= counts.Length || !ValidGrade(grade)) // 대상과 등급 확인
            { // 차단 시작
                return false; // 유효하지 않은 후보 제외
            } // 차단 종료
            if ((id == UpgradeItemId.Damage || id == UpgradeItemId.FireRate || id == UpgradeItemId.Pierce || id == UpgradeItemId.UniqueCooldown || id == UpgradeItemId.Graze) && counts[index] >= 3) // 누적 획득 상한 확인
            { // 차단 시작
                return false; // 최대 획득 횟수 제외
            } // 차단 종료
            switch (id) // 효과별 유효성 확인
            { // 분기 시작
                case UpgradeItemId.Damage: return player.Build.Damage(1f, false) < 1.5999f; // 기본 피해 상한
                case UpgradeItemId.FireRate: return player.Build.IntervalScale > 0.6001f; // 발사 간격 상한
                case UpgradeItemId.Pierce: return player.Build.ExtraPierce < 3; // 추가 관통 상한
                case UpgradeItemId.UniqueCooldown: return player.Build.UniqueCooldownReduction < 0.3999f; // 고유기 쿨타임 상한
                case UpgradeItemId.Shield: return player.Survival.UpgradeShieldCharges < (int)grade; // 현재보다 높은 보호 횟수
                case UpgradeItemId.Graze: return true; // 근접 충전 효과
                case UpgradeItemId.Heal: return player.Survival.Health < player.Survival.MaximumHealth; // 체력 회복 가능
                case UpgradeItemId.Energy: return player.Skills.ExactEnergy < 100f; // 실제 소수 에너지 충전 가능
                case UpgradeItemId.Magnet: return hasExperience; // 회수 가능한 조각 존재
                default: return false; // 알 수 없는 아이템
            } // 분기 종료
        } // 확인 종료
        public bool TryApply(UpgradeItemId id, BattlePlayer player) // 기존 시험 효과 적용
        { // 적용 시작
            if (id == UpgradeItemId.Heal || id == UpgradeItemId.Energy || id == UpgradeItemId.Magnet) // 신규 아이템 등급 확인
            { // 구형 적용 차단 시작
                return false; // 등급 없는 신규 보상 차단
            } // 구형 적용 차단 종료
            if (id == UpgradeItemId.Shield && ValidPlayer(player) && player.Build.Upgrades == this) // 기존 보호막 재획득
            { // 호환 적용 시작
                counts[(int)id] = 1; // 기존 단일 보호 기록
                player.Survival.RefreshUpgradeShield(1); // 한 번 방어 갱신
                return true; // 기존 시험 효과 적용
            } // 호환 적용 종료
            return TryApply(id, LegacyGrade(id), player); // 기존 등급 효과 적용
        } // 적용 종료
        public bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, BattlePlayer player, Func<bool> collectExperience = null) // 등급별 아이템 획득
        { // 적용 시작
            if (!CanApply(id, grade, player, collectExperience != null)) // 현재 유효성 재확인
            { // 차단 시작
                return false; // 효과 없는 획득 제외
            } // 차단 종료
            float amount = Amount(id, grade); // 등급별 기본 수치
            switch (id) // 효과별 적용
            { // 분기 시작
                case UpgradeItemId.Damage: // 기본 피해 증가
                    DamageBonus += Mathf.Min(amount, Mathf.Max(0f, 0.6f - (player.Build.Damage(1f, false) - 1f))); // 남은 피해 한도만 합산
                    break; // 피해 적용 종료
                case UpgradeItemId.FireRate: // 발사 간격 감소
                    FireRateBonus += Mathf.Min(amount, Mathf.Max(0f, player.Build.IntervalScale - 0.6f)); // 남은 간격 한도만 합산
                    break; // 발사 적용 종료
                case UpgradeItemId.Pierce: // 추가 관통 증가
                    PierceBonus += Mathf.Min((int)amount, 3 - player.Build.ExtraPierce); // 남은 관통 한도만 합산
                    break; // 관통 적용 종료
                case UpgradeItemId.UniqueCooldown: // 고유기 대기 감소
                    UniqueCooldownBonus += Mathf.Min(amount, Mathf.Max(0f, 0.4f - player.Build.UniqueCooldownReduction)); // 남은 쿨타임 한도만 합산
                    break; // 대기 적용 종료
                case UpgradeItemId.Shield: // 등급 보호막 획득
                    player.Survival.RefreshUpgradeShield((int)grade); // 기존과 새 횟수 중 큰 값
                    counts[(int)id] = 1; // 단일 보호막 기록
                    return true; // 보호막 적용 완료
                case UpgradeItemId.Graze: // 근접 충전 증가
                    GrazeBonus += amount; // 등급별 소수 증가 합산
                    break; // 충전 적용 종료
                case UpgradeItemId.Heal: // 체력 즉시 회복
                    player.Survival.Heal((int)amount); // 최대 체력 내 회복
                    break; // 회복 적용 종료
                case UpgradeItemId.Energy: // 필살기 에너지 충전
                    player.Skills.GainEnergy((int)amount); // 최대 에너지 내 충전
                    break; // 에너지 적용 종료
                case UpgradeItemId.Magnet: // 경험치 즉시 회수
                    if (collectExperience == null || !collectExperience()) // 실제 회수 결과 확인
                    { // 회수 실패 시작
                        return false; // 효과 없는 획득 제외
                    } // 회수 실패 종료
                    break; // 회수 적용 종료
            } // 분기 종료
            counts[(int)id] = Math.Min(int.MaxValue - 1, counts[(int)id]) + 1; // 종류별 획득 기록
            player.Skills.UniqueCooldownReduction = player.Build.UniqueCooldownReduction; // 고유기 쿨타임 동기화
            return true; // 효과 적용 완료
        } // 적용 종료
        public static float MagnetRadius(UpgradeItemGrade grade) // 자석 등급별 회수 범위
        { // 조회 시작
            if (!ValidGrade(grade)) // 등급 유효성 확인
            { // 실패 시작
                return 0f; // 유효하지 않은 반경
            } // 실패 종료
            return grade == UpgradeItemGrade.V ? float.PositiveInfinity : (int)grade * 2f; // 등급 반경 또는 전 화면
        } // 조회 종료
        public static string Label(UpgradeItemId id) // 기존 시험 설명
        { // 변환 시작
            return Label(id, LegacyGrade(id)); // 기존 효과 기준 설명
        } // 변환 종료
        public static string Label(UpgradeItemId id, UpgradeItemGrade grade) // 등급별 이름과 효과 설명
        { // 변환 시작
            if (!ValidGrade(grade)) // 등급 유효성 확인
            { // 실패 시작
                return "알 수 없는 강화"; // 유효하지 않은 등급
            } // 실패 종료
            float raw = Amount(id, grade); // 표시용 원본 효과
            int amount = float.IsInfinity(raw) ? 0 : Mathf.RoundToInt(raw * (id == UpgradeItemId.Damage || id == UpgradeItemId.FireRate || id == UpgradeItemId.UniqueCooldown || id == UpgradeItemId.Graze ? 100f : 1f)); // 표시용 효과 수치
            switch (id) // 종류별 설명
            { // 분기 시작
                case UpgradeItemId.Damage: return "화력\n기본·집중 피해 +" + amount + "%"; // 피해 강화 설명
                case UpgradeItemId.FireRate: return "연사\n발사 간격 -" + amount + "%"; // 발사 강화 설명
                case UpgradeItemId.Pierce: return "관통\n탄환 관통 +" + amount; // 관통 강화 설명
                case UpgradeItemId.UniqueCooldown: return "기술 순환\nJ 쿨타임 -" + amount + "%"; // 고유기 강화 설명
                case UpgradeItemId.Shield: return "방어막\n피해 " + amount + "회 방어"; // 보호막 설명
                case UpgradeItemId.Graze: return "근접 충전\n그레이즈 에너지 +" + amount + "%"; // 근접 강화 설명
                case UpgradeItemId.Heal: return "회복 물약\n체력 +" + amount; // 회복 아이템 설명
                case UpgradeItemId.Energy: return "에너지 결정\n필살기 에너지 +" + amount; // 에너지 아이템 설명
                case UpgradeItemId.Magnet: return "회수 자석\n" + (grade == UpgradeItemGrade.V ? "전 화면" : "반경 " + amount) + " 경험치 회수"; // 회수 아이템 설명
                default: return "알 수 없는 강화"; // 유효하지 않은 종류
            } // 분기 종료
        } // 변환 종료
        private static bool ValidPlayer(BattlePlayer player) // 전투 대상 확인
        { // 확인 시작
            return player != null && player.Build != null && player.Skills != null && player.Survival != null && player.Survival.IsAlive; // 살아 있는 유효 플레이어
        } // 확인 종료
        private static bool ValidGrade(UpgradeItemGrade grade) // 등급 범위 확인
        { // 확인 시작
            return (int)grade >= 1 && (int)grade <= 5; // 다섯 등급 범위
        } // 확인 종료
        private static UpgradeItemGrade LegacyGrade(UpgradeItemId id) // 구형 시험 효과와 등급 대응
        { // 조회 시작
            return id == UpgradeItemId.UniqueCooldown ? UpgradeItemGrade.II : id == UpgradeItemId.Graze ? UpgradeItemGrade.III : UpgradeItemGrade.I; // 기존 10%와 20% 유지
        } // 조회 종료
        private static float Amount(UpgradeItemId id, UpgradeItemGrade grade) // 등급별 시험 수치
        { // 조회 시작
            int index = (int)grade - 1; // 등급 배열 번호
            switch (id) // 종류별 수치 선택
            { // 분기 시작
                case UpgradeItemId.Damage: return 0.10f + index * 0.02f; // 피해 증가
                case UpgradeItemId.FireRate: return 0.08f + index * 0.02f; // 발사 간격 감소
                case UpgradeItemId.Pierce: return index < 2 ? 1f : index < 4 ? 2f : 3f; // 추가 관통
                case UpgradeItemId.UniqueCooldown: return index == 0 ? 0.06f : 0.08f + index * 0.02f; // 고유기 대기 감소
                case UpgradeItemId.Shield: return (int)grade; // 방어 횟수
                case UpgradeItemId.Graze: return 0.10f + index * 0.05f; // 근접 충전 증가
                case UpgradeItemId.Heal: return 10f + index * 5f; // 체력 회복
                case UpgradeItemId.Energy: return index == 4 ? 25f : 8f + index * 4f; // 필살기 에너지
                case UpgradeItemId.Magnet: return MagnetRadius(grade); // 경험치 회수 범위
                default: return 0f; // 유효하지 않은 종류
            } // 분기 종료
        } // 조회 종료
    } // 상태 종료
} // 영역 종료
