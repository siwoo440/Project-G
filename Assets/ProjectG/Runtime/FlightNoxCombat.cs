using UnityEngine; // 녹스의 입력 결과와 3차원 효과

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 조종사당 하나의 표식 상태
    public sealed class FlightNoxCombat : MonoBehaviour // 반격 표식과 반경 소거
    {
        public const int MarkLimit = 10; // 34일차 시험 표식 상한
        private const float Radius = 3f; // 경계 역전의 실제 3D 반경
        private const float PulseTime = 0.24f; // 반격 반경의 실제 화면 표시 시간
        [SerializeField] private FlightPilotVitals vitals; // 중복 없이 확정된 스침의 이벤트
        [SerializeField] private FlightWeapon weapon; // 출격 조종사와 실제 반격 발사
        [SerializeField] private FlightThreatPool pool; // 화면의 소거 가능한 일반 적탄
        [SerializeField] private FlightUpgradeChoices upgrades; // 정밀 회피와 반격 전환 카드
        [SerializeField] private Transform pulse; // 하이러키의 세 방향 보라색 소거 경계
        private Vector3 pulseOrigin; // 필살기를 사용한 순간의 반경 중심
        private float pulseEnd; // 종료 시각
        private int precisionGrazes; // 카드 적용 뒤 다음 추가 표식까지의 누적 회피

        public int Marks { get; private set; } // HUD와 고유기에서 공유하는 반격 표식

        private void OnEnable() // 실전의 실제 정상 그레이즈 수신
        {
            vitals.Grazed += OnGraze; // 같은 적탄의 확정 1회 이벤트
        }

        private void OnDisable() // 이전 출격의 상대 경로 이벤트 정리
        {
            vitals.Grazed -= OnGraze; // 다시 들어올 때 중복 표식 방지
        }

        private void OnGraze() // 피격되지 않고 스친 적탄의 보상
        {
            if (weapon.ActivePilot != FlightBroomPilot.Nox) // 다른 조종사의 원래 그레이즈 보존
            {
                return; // 반격 표식 계산 제외
            }
            int amount = 1; // 기본 탄환 한 발의 표식
            if (upgrades != null && upgrades.NoxPrecision && ++precisionGrazes == 3) // 첫 성장 카드의 그레이즈 세 번
            {
                amount++; // 해당 회피에 추가 표식 하나
                precisionGrazes = 0; // 다음 세 번부터 다시 계산
            }
            Marks = Mathf.Min(MarkLimit, Marks + amount); // 기본과 추가 획득에 같은 상한
        }

        internal void ResetRun() // 새 출격에서 현재 보유한 반격 준비 제거
        {
            Marks = 0; // 이전 출격의 표식은 다시 사용 불가
            precisionGrazes = 0; // 정밀 회피의 이전 스침 횟수 제거
            pulse.gameObject.SetActive(false); // 화면의 이전 필살기 표현 삭제
        }

        internal bool CastCounter() // 실제 Q에서 표식을 성공한 탄환으로 교환
        {
            if (Marks == 0 || !weapon.CastNoxCounter(Marks, upgrades != null ? upgrades.NoxPierce : 0)) // 표식 부족 또는 무효 방향
            {
                return false; // 획득하지 않은 탄을 만들거나 마력을 소비하지 않음
            }
            Marks = 0; // 모든 보유 표식을 한 번에 소비
            return true; // 재사용 대기가 시작되는 정상 반격
        }

        internal bool CastReversal() // 경계 역전의 구형 탄 소거와 마물 반격
        {
            pulseOrigin = vitals.transform.position; // 현재 피격 구체의 실제 중심
            pool.ClearWithin(pulseOrigin, Radius); // 가까운 일반 탄만 소거
            foreach (FlightEnemy enemy in FindObjectsByType<FlightEnemy>(FindObjectsSortMode.None)) // 구체 범위와 비교할 실제 마물
            {
                if (enemy.IsCombatTarget && (enemy.transform.position - pulseOrigin).sqrMagnitude <= Radius * Radius) // 고도도 포함한 실제 반경
                {
                    enemy.ReceiveHit(150); // 경계 안 마물의 시험 피해
                }
            }
            pulseEnd = Time.time + PulseTime; // 잠깐 퍼지는 경계의 종료
            pulse.position = pulseOrigin; // 시전점에 남아 있을 시각 경계
            pulse.localScale = Vector3.one * 0.2f; // 작게 시작하는 시각적 소거
            pulse.gameObject.SetActive(true); // 씬에 배치한 소거 경계 표시
            return true; // 탄이 없는 방향에서도 사용 가능한 필살기
        }

        private void LateUpdate() // 현재 스킬의 짧은 보라색 파동
        {
            if (!pulse.gameObject.activeSelf) // 스킬을 사용하지 않은 평상시
            {
                return; // 불필요한 파동 계산 방지
            }
            float remaining = pulseEnd - Time.time; // 게임 시간의 현재 진행 상태
            if (remaining <= 0f || vitals.IsDefeated) // 시각 효과 종료 또는 출격 실패
            {
                pulse.gameObject.SetActive(false); // 활성 세계 모델에서 파동 제거
                return; // 오래 남지 않는 스킬 표시
            }
            pulse.position = pulseOrigin; // 비행 중에도 맞았던 실제 중심에 고정
            float scale = Mathf.Lerp(0.2f, Radius, 1f - remaining / PulseTime); // 실제 반경까지 부드럽게 확대
            pulse.localScale = Vector3.one * scale; // 직교하는 세 방향 모두 같은 반경
        }
    }
}
