using System; // 경험치 회수 콜백
using UnityEngine; // 유니티 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class UpgradeDropEffects : IUpgradeDropEffects // 실제 전투 강화 효과 연결
    { // 클래스 시작
        private readonly BattlePlayer player; // 현재 전투 플레이어
        private readonly BattleProgress progress; // 현재 경험치 상태
        public UpgradeDropEffects(BattlePlayer target, BattleProgress battleProgress) // 전투 효과 구성
        { // 구성 시작
            player = target; // 플레이어 보관
            progress = battleProgress; // 경험치 상태 보관
        } // 구성 종료
        public bool CanApply(UpgradeItemId id, UpgradeItemGrade grade) // 현재 효과 가능 여부
        { // 확인 시작
            if (player == null || player.Build == null) // 전투 상태 확인
            { // 차단 시작
                return false; // 플레이어 없는 효과 차단
            } // 차단 종료
            bool experience = id == UpgradeItemId.Magnet && progress != null && progress.HasExperienceInRange(UpgradeItemState.MagnetRadius(grade)); // 등급별 회수 가능 조각
            return player.Build.Upgrades.CanApply(id, grade, player, experience); // 등급과 상한 검증
        } // 확인 종료
        public bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, out string feedback) // 실제 효과와 변화량
        { // 적용 시작
            feedback = null; // 이전 안내 제거
            if (!CanApply(id, grade)) // 현재 획득 가능 확인
            { // 차단 시작
                return false; // 무효 효과 차단
            } // 차단 종료
            float before = CurrentValue(id); // 적용 전 실제 값
            float radius = UpgradeItemState.MagnetRadius(grade); // 자석 등급별 범위
            int experience = id == UpgradeItemId.Magnet ? progress.AvailableExperienceInRange(radius) : 0; // 자석 실제 회수량
            Func<bool> collect = id == UpgradeItemId.Magnet ? () => progress.CollectExperienceInRange(radius) : null; // 등급별 회수 콜백
            if (!player.Build.Upgrades.TryApply(id, grade, player, collect)) // 아이템 효과 적용
            { // 실패 시작
                return false; // 적용 실패 유지
            } // 실패 종료
            float difference = id == UpgradeItemId.Magnet ? experience : CurrentValue(id) - before; // 실제 증가량
            if (id == UpgradeItemId.FireRate || id == UpgradeItemId.UniqueCooldown || id == UpgradeItemId.Damage || id == UpgradeItemId.Graze) // 비율 표시 확인
            { // 비율 표시 시작
                difference *= 100f; // 퍼센트 단위 환산
            } // 비율 표시 종료
            string name = UpgradeItemState.Label(id, grade).Split('\n')[0]; // 아이템 이름 조회
            string effect = id == UpgradeItemId.Damage ? "탄환 피해 +" : id == UpgradeItemId.FireRate ? "발사 간격 -" : id == UpgradeItemId.Pierce ? "추가 관통 +" : id == UpgradeItemId.UniqueCooldown ? "고유기 대기 -" : id == UpgradeItemId.Shield ? "방어 횟수 +" : id == UpgradeItemId.Graze ? "그레이즈 충전 +" : id == UpgradeItemId.Heal ? "체력 +" : id == UpgradeItemId.Energy ? "필살기 에너지 +" : "경험치 +"; // 실제 효과와 방향
            string suffix = id == UpgradeItemId.FireRate || id == UpgradeItemId.UniqueCooldown || id == UpgradeItemId.Damage || id == UpgradeItemId.Graze ? "%" : id == UpgradeItemId.Shield ? "회" : ""; // 효과 단위
            feedback = name + " " + grade + "  " + effect + difference.ToString("0.##") + suffix; // 실제 효과와 적용량 안내
            return true; // 획득 성공
        } // 적용 종료
        private float CurrentValue(UpgradeItemId id) // 표시할 실제 전투 수치
        { // 조회 시작
            switch (id) // 아이템 효과 확인
            { // 분기 시작
                case UpgradeItemId.Damage: return player.Build.Damage(1f, false) - 1f; // 기본 피해 증가량
                case UpgradeItemId.FireRate: return 1f - player.Build.IntervalScale; // 발사 간격 감소량
                case UpgradeItemId.Pierce: return player.Build.ExtraPierce; // 추가 관통 수
                case UpgradeItemId.UniqueCooldown: return player.Build.UniqueCooldownReduction; // 고유기 대기 감소량
                case UpgradeItemId.Shield: return player.Survival.UpgradeShieldCharges; // 남은 강화 방어 횟수
                case UpgradeItemId.Graze: return player.Build.Upgrades.GrazeBonus; // 그레이즈 충전 증가량
                case UpgradeItemId.Heal: return player.Survival.Health; // 현재 체력
                case UpgradeItemId.Energy: return player.Skills.ExactEnergy; // 소수 포함 에너지
                default: return 0f; // 자석과 미지원 종류
            } // 분기 종료
        } // 조회 종료
    } // 클래스 종료
} // 영역 종료
