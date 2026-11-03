using UnityEngine; // 유니티 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum EnemyRank // 처치 대상 구분
    { // 구분 시작
        Normal, // 일반 적
        MidBoss, // 중간 보스
        FinalBoss // 최종 보스
    } // 구분 종료
    public enum CommonSkill // 공용 스킬 종류
    { // 종류 시작
        Shield, // 보호막
        Scatter // 산탄
    } // 종류 종료
    public sealed class SkillState // 스킬과 자원 상태
    { // 클래스 시작
        public int Energy // 필살기 에너지
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public float RapidActive // 고유 스킬 남은 활성 시간
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public float RapidCooldown // 고유 스킬 남은 쿨타임
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public float CommonCooldown // 공용 스킬 남은 쿨타임
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public float AttackIntervalScale => RapidActive > 0.000001f ? 0.7f : 1f; // 발사 간격 비율
        public CommonSkill Common // 장착 공용 스킬
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public bool Blocked // 사용 제한 여부
        { // 속성 시작
            get; // 값 조회
            set; // 값 설정
        } // 속성 종료
        public void GainGraze() // 그레이즈 충전
        { // 충전 시작
            Energy = Mathf.Min(100, Energy + 1); // 적탄당 에너지 충전
        } // 충전 종료
        public void GainDefeat(EnemyRank rank) // 처치 충전
        { // 충전 시작
            int gain = rank == EnemyRank.Normal ? 2 : rank == EnemyRank.MidBoss ? 10 : rank == EnemyRank.FinalBoss ? 20 : 0; // 처치 대상별 시험 충전량
            Energy = Mathf.Min(100, Energy + gain); // 에너지 상한 적용
        } // 충전 종료
        public bool TryUltimate(bool alive) // 필살기 사용
        { // 사용 시작
            if (!alive || Blocked || Energy < 100) // 발동 조건 확인
            { // 차단 시작
                return false; // 발동 차단
            } // 차단 종료
            Energy -= 100; // 에너지 소모
            return true; // 필살기 발동
        } // 사용 종료
        public bool TryRapid(bool alive) // 고유 스킬 사용
        { // 사용 시작
            if (!alive || Blocked || RapidActive > 0.000001f || RapidCooldown > 0.000001f) // 발동 조건 확인
            { // 차단 시작
                return false; // 발동 차단
            } // 차단 종료
            RapidActive = 3f; // 시험용 활성 시간
            return true; // 고유 스킬 발동
        } // 사용 종료
        public bool TryCommon(bool alive) // 공용 스킬 사용
        { // 사용 시작
            if (!alive || Blocked || CommonCooldown > 0.000001f) // 발동 조건 확인
            { // 차단 시작
                return false; // 발동 차단
            } // 차단 종료
            CommonCooldown = Common == CommonSkill.Shield ? 12f : 10f; // 스킬별 시험 쿨타임
            return true; // 공용 스킬 발동
        } // 사용 종료
        public void Equip(CommonSkill skill) // 공용 스킬 교체
        { // 교체 시작
            if (skill == CommonSkill.Shield || skill == CommonSkill.Scatter) // 교체 대상 확인
            { // 교체 처리 시작
                Common = skill; // 기존 쿨타임을 유지한 교체
            } // 교체 처리 종료
        } // 교체 종료
        public void Advance(float elapsed) // 스킬 시간 진행
        { // 진행 시작
            float remaining = Mathf.Max(0f, elapsed); // 경과 시간 보정
            CommonCooldown = Mathf.Max(0f, CommonCooldown - remaining); // 공용 쿨타임 진행
            if (RapidActive > 0f) // 고유 스킬 활성 확인
            { // 활성 처리 시작
                float activeTime = Mathf.Min(RapidActive, remaining); // 이번 프레임 활성 시간
                RapidActive = Mathf.Max(0f, RapidActive - activeTime); // 남은 활성 시간
                remaining -= activeTime; // 남은 경과 시간
                if (RapidActive <= 0.000001f) // 활성 종료 확인
                { // 종료 처리 시작
                    RapidActive = 0f; // 활성 시간 종료
                    RapidCooldown = 8f; // 종료 후 시험 쿨타임 시작
                } // 종료 처리 종료
            } // 활성 처리 종료
            if (RapidActive <= 0f && remaining > 0f) // 쿨타임 진행 확인
            { // 쿨타임 처리 시작
                RapidCooldown = Mathf.Max(0f, RapidCooldown - remaining); // 남은 쿨타임 갱신
            } // 쿨타임 처리 종료
        } // 진행 종료
    } // 클래스 종료
} // 영역 종료
