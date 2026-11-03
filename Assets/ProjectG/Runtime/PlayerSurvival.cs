using UnityEngine; // 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class PlayerSurvival // 플레이어 생존 상태
    { // 클래스 시작
        private float invulnerability; // 남은 무적 시간
        public int Health // 현재 체력
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public bool IsAlive => Health > 0; // 생존 여부
        public bool IsInvulnerable => invulnerability > 0.000001f; // 무적 여부
        public PlayerSurvival(int maximum) // 초기 체력 설정
        { // 초기화 시작
            Health = Mathf.Max(0, maximum); // 음수 체력 방지
        } // 초기화 종료
        public void Advance(float elapsed) // 무적 시간 진행
        { // 진행 시작
            invulnerability = Mathf.Max(0f, invulnerability - Mathf.Max(0f, elapsed)); // 남은 무적 시간 갱신
        } // 진행 종료
        public bool TryHit(int damage) // 피해 적용
        { // 피해 시작
            if (!IsAlive || IsInvulnerable || damage <= 0) // 피해 불가 조건 확인
            { // 차단 시작
                return false; // 피해 차단
            } // 차단 종료
            Health = Mathf.Max(0, Health - damage); // 체력 감소와 하한
            invulnerability = 1f; // 피격 후 무적 시간
            return true; // 피해 적용 확인
        } // 피해 종료
    } // 클래스 종료
} // 영역 종료
