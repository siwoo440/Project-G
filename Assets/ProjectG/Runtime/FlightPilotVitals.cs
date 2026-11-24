using UnityEngine; // 3차원 피격과 체력

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    [DisallowMultipleComponent] // 비행체당 하나의 체력 상태
    public sealed class FlightPilotVitals : MonoBehaviour // 체력과 그레이즈 판정
    { // 상태 시작
        [SerializeField, Min(1)] private int maxHealth = 100; // 시험 출격 초기 체력
        [SerializeField, Min(0.01f)] private float hitRadius = 0.38f; // 구형 기체 피격 반경
        [SerializeField, Min(0.01f)] private float grazeRadius = 0.90f; // 구형 근접 회피 반경
        [SerializeField, Min(0f)] private float immuneSeconds = 1f; // 피격 후 시험 무적 시간
        private float immuneUntil; // 다음 피격 가능 시각

        public int Health { get; private set; } // 현재 출격 체력
        public int MaxHealth => maxHealth; // 표시용 최대 체력
        public int Grazes { get; private set; } // 유효한 근접 회피 수
        public bool IsDefeated => Health <= 0; // 비행 실패 상태

        private void Awake() // 씬 시작 때 체력 초기화
        { // 준비 시작
            ResetRun(); // 첫 체력과 그레이즈 설정
        } // 준비 종료

        public void ResetRun() // 출격 재시작의 체력과 판정 초기화
        { // 초기화 시작
            Health = maxHealth; // 최대 체력 복구
            Grazes = 0; // 근접 회피 초기화
            immuneUntil = 0f; // 이전 무적 종료
        } // 초기화 종료

        internal bool ResolveThreat(Vector3 start, Vector3 end, float projectileRadius, int damage, ref bool nearCandidate, ref bool grazeAwarded) // 탄환 이동 전체와 기체 상대 경로 판정
        { // 판정 시작
            if (IsDefeated) // 침몰한 기체의 추가 판정 제외
            { // 전투 종료 시작
                return false; // 실패 뒤 체력 보존
            } // 전투 종료 종료

            Vector3 travel = end - start; // 한 프레임의 상대 이동 구간
            float squaredLength = travel.sqrMagnitude; // 해당 구간의 제곱 거리
            float amount = squaredLength > 0f ? Mathf.Clamp01(-Vector3.Dot(start, travel) / squaredLength) : 0f; // 최근접점의 구간 비율
            float closestSquared = (start + travel * amount).sqrMagnitude; // 기체까지의 제곱 최소 거리
            float inner = hitRadius + projectileRadius; // 기체와 탄환의 접촉 반경 합
            if (closestSquared <= inner * inner) // 프레임 사이 실제 피격 여부
            { // 피격 시작
                if (Time.time >= immuneUntil) // 피격 후 무적 구간 확인
                { // 피해 반영 시작
                    Health = Mathf.Max(0, Health - damage); // 체력 하한 적용
                    immuneUntil = Time.time + immuneSeconds; // 다음 피격 보호
                } // 피해 반영 종료
                return true; // 충돌한 탄은 그레이즈 대상에서 제외
            } // 피격 종료

            float outer = Mathf.Max(hitRadius, grazeRadius) + projectileRadius; // 근접 회피 범위
            nearCandidate |= closestSquared <= outer * outer; // 해당 탄의 근접 진입 기록
            if (!grazeAwarded && nearCandidate && end.sqrMagnitude > outer * outer) // 몸에 닿지 않고 근접 구간을 다 지난 상태
            { // 그레이즈 시작
                grazeAwarded = true; // 같은 탄환에서 한 번만 인정
                Grazes++; // 현재 출격의 그레이즈 증가
            } // 그레이즈 종료

            return false; // 충돌하지 않은 탄환의 계속 이동
        } // 판정 종료
    } // 상태 종료
} // 영역 종료
