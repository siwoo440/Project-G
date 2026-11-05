using UnityEngine; // 유니티 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum UpgradeItemId // 강화 아이템 종류
    { // 종류 시작
        Damage, FireRate, Pierce, UniqueCooldown, Shield, Graze // 초기 여섯 종류
    } // 종류 종료
    public sealed class UpgradeItemState // 출격 중 아이템 보유 상태
    { // 상태 시작
        private readonly int[] counts = new int[6]; // 종류별 획득 횟수
        public int Count(UpgradeItemId id) // 아이템 획득 횟수
        { // 조회 시작
            int index = (int)id; // 종류 번호
            return index >= 0 && index < counts.Length ? counts[index] : 0; // 유효 횟수 반환
        } // 조회 종료
        public bool CanApply(UpgradeItemId id, BattlePlayer player) // 효과 적용 가능 확인
        { // 확인 시작
            if (player == null || player.Build == null || player.Skills == null || player.Survival == null || !player.Survival.IsAlive) // 플레이어 상태 확인
            { // 차단 시작
                return false; // 사용 불가
            } // 차단 종료
            int index = (int)id; // 종류 번호
            if (index < 0 || index >= counts.Length) // 종류 범위 확인
            { // 차단 시작
                return false; // 알 수 없는 종류
            } // 차단 종료
            if (id == UpgradeItemId.Shield) // 일회 방어막 확인
            { // 갱신 시작
                return true; // 반복 갱신 허용
            } // 갱신 종료
            if (counts[index] >= 3) // 종류별 상한 확인
            { // 차단 시작
                return false; // 최대 단계 제외
            } // 차단 종료
            switch (id) // 공유 상한 확인
            { // 분기 시작
                case UpgradeItemId.Damage: return player.Build.Damage(1f, false) < 1.5999f; // 기본 피해 상한
                case UpgradeItemId.FireRate: return player.Build.IntervalScale > 0.6001f; // 발사 간격 상한
                case UpgradeItemId.Pierce: return player.Build.ExtraPierce < 3; // 관통 상한
                case UpgradeItemId.UniqueCooldown: return player.Build.UniqueCooldownReduction < 0.3999f; // 고유기 대기 상한
                default: return true; // 유효한 그레이즈 강화
            } // 분기 종료
        } // 확인 종료
        public bool TryApply(UpgradeItemId id, BattlePlayer player) // 아이템 효과 적용
        { // 적용 시작
            if (!CanApply(id, player) || player.Build.Upgrades != this) // 효과 대상과 유효성 확인
            { // 차단 시작
                return false; // 유효하지 않은 선택 제외
            } // 차단 종료
            if (id == UpgradeItemId.Shield) // 방어막 선택 확인
            { // 방어막 시작
                counts[(int)id] = 1; // 최대 한 개 기록
                player.Survival.ActivateShield(float.PositiveInfinity); // 다음 피해 1회 방어
            } // 방어막 종료
            else // 중첩 강화
            { // 강화 시작
                counts[(int)id]++; // 획득 단계 증가
            } // 강화 종료
            player.Skills.UniqueCooldownReduction = player.Build.UniqueCooldownReduction; // 고유 스킬 강화 동기화
            return true; // 선택 적용 완료
        } // 적용 종료
        public static string Label(UpgradeItemId id) // 아이템 이름과 시험 효과
        { // 변환 시작
            switch (id) // 종류 확인
            { // 분기 시작
                case UpgradeItemId.Damage: return "화력\n기본·집중 피해 +10%"; // 피해 강화 설명
                case UpgradeItemId.FireRate: return "연사\n발사 간격 -8%"; // 발사 강화 설명
                case UpgradeItemId.Pierce: return "관통\n탄환 관통 +1"; // 관통 강화 설명
                case UpgradeItemId.UniqueCooldown: return "기술 순환\nJ 쿨타임 -10%"; // 고유기 강화 설명
                case UpgradeItemId.Shield: return "방어막\n다음 피해 1회 방어"; // 방어막 설명
                default: return "근접 충전\n그레이즈 에너지 +20%"; // 그레이즈 강화 설명
            } // 분기 종료
        } // 변환 종료
    } // 상태 종료
} // 영역 종료
