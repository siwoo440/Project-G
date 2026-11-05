using UnityEngine; // 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class PlayerSurvival // 플레이어 생존 상태
    { // 클래스 시작
        private float invulnerability; // 남은 무적 시간
        private bool upgradeSafe; // 강화 선택 안전 상태
        private float transitionSafety; // 선택 후 안전 유예
        private float shieldTime; // 남은 보호막 시간
        private bool reactiveShield; // 피격 후 보호 장치
        private float reactiveCooldown; // 보호 장치 재충전
        public int MaximumHealth // 최대 체력
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public int Health // 현재 체력
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public bool HasShield => shieldTime > 0.000001f; // 보호막 활성 여부
        public void ActivateShield(float duration) // 보호막 활성화
        { // 활성화 시작
            if (IsAlive) // 생존 상태 확인
            { // 보호막 적용 시작
                shieldTime = Mathf.Max(0f, duration); // 보호막 지속 시간
            } // 보호막 적용 종료
        } // 활성화 종료
        public bool IsAlive => Health > 0; // 생존 여부
        public bool IsInvulnerable => upgradeSafe || transitionSafety > 0.000001f || invulnerability > 0.000001f; // 선택 안전과 피격 무적
        public void SetUpgradeSafe(bool safe) // 강화 선택 안전 설정
        { // 설정 시작
            upgradeSafe = safe; // 선택 안전 상태 보관
        } // 설정 종료
        public void GrantTransitionSafety(float duration) // 전환 후 안전 유예
        { // 설정 시작
            transitionSafety = Mathf.Max(transitionSafety, Mathf.Max(0f, duration)); // 더 긴 안전 시간 유지
        } // 설정 종료
        public PlayerSurvival(int maximum) // 초기 체력 설정
        { // 초기화 시작
            MaximumHealth = Mathf.Max(0, maximum); // 최대 체력 보관
            Health = MaximumHealth; // 초기 체력 설정
        } // 초기화 종료
        public void Advance(float elapsed) // 무적 시간 진행
        { // 진행 시작
            invulnerability = Mathf.Max(0f, invulnerability - Mathf.Max(0f, elapsed)); // 남은 무적 시간 갱신
            transitionSafety = Mathf.Max(0f, transitionSafety - Mathf.Max(0f, elapsed)); // 남은 전환 안전 시간 갱신
            shieldTime = Mathf.Max(0f, shieldTime - Mathf.Max(0f, elapsed)); // 남은 보호막 시간 갱신
            reactiveCooldown = Mathf.Max(0f, reactiveCooldown - Mathf.Max(0f, elapsed)); // 보호 장치 재충전
        } // 진행 종료
        public void Heal(int amount) // 체력 회복
        { // 회복 시작
            if (IsAlive) // 생존 확인
            { // 회복 처리 시작
                Health = Mathf.Min(MaximumHealth, Health + Mathf.Max(0, amount)); // 최대 체력 이내 회복
            } // 회복 처리 종료
        } // 회복 종료
        public void IncreaseMaximum(int amount) // 대체 보상 체력 증가
        { // 증가 시작
            int gain = Mathf.Max(0, amount); // 증가량 확인
            MaximumHealth += gain; // 최대 체력 증가
            Heal(gain); // 증가량만큼 회복
        } // 증가 종료
        public void InstallReactiveShield() // 보호 장치 장착
        { // 장착 시작
            reactiveShield = true; // 피격 후 보호 장치 활성화
        } // 장착 종료
        public bool TryHit(int damage) // 피해 적용
        { // 피해 시작
            if (!IsAlive || IsInvulnerable || damage <= 0) // 피해 불가 조건 확인
            { // 차단 시작
                return false; // 피해 차단
            } // 차단 종료
            if (HasShield) // 보호막 확인
            { // 방어 시작
                shieldTime = 0f; // 보호막 1회 소모
                return false; // 체력 피해 방어
            } // 방어 종료
            Health = Mathf.Max(0, Health - damage); // 체력 감소와 하한
            invulnerability = 1f; // 피격 후 무적 시간
            if (IsAlive && reactiveShield && reactiveCooldown <= 0f) // 보호 장치 발동 조건
            { // 보호 장치 시작
                ActivateShield(float.PositiveInfinity); // 다음 피격 1회 보호
                reactiveCooldown = 30f; // 재충전 시간 시작
            } // 보호 장치 종료
            return true; // 피해 적용 확인
        } // 피해 종료
    } // 클래스 종료
} // 영역 종료
