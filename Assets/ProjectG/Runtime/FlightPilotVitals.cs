using UnityEngine; // 3차원 피격과 체력

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 비행체당 하나의 체력 상태
    public sealed class FlightPilotVitals : MonoBehaviour // 체력과 그레이즈 판정
    {
        [SerializeField, Min(1)] private int maxHealth = 100; // 시험 출격 초기 체력
        [SerializeField, Min(0.01f)] private float hitRadius = 0.38f; // 구형 기체 피격 반경
        [SerializeField, Min(0.01f)] private float grazeRadius = 0.90f; // 구형 근접 회피 반경
        [SerializeField, Min(0f)] private float immuneSeconds = 1f; // 피격 후 시험 무적 시간
        [SerializeField] private FlightUpgradeChoices upgrades; // 장착한 보호 장치
        private float immuneUntil; // 다음 피격 가능 시각
        private float guardUntil; // 한 발만 막는 리안 보호막의 종료 시점
        private float slotProtectionUntil; // 교체할 수 있는 E 시간 보호막의 종료 시점
        private int capacityBonus; // 선택한 이번 출격의 최대 체력
        private bool deviceShield; // 피격 후 장치가 한 번 막을 수 있는 상태
        private float deviceReadyAt; // 장치 방어막의 다음 충전

        public int Health { get; private set; } // 현재 출격 체력
        public int MaxHealth => maxHealth + capacityBonus; // 임시 성장까지 포함한 최대 체력
        public int Grazes { get; private set; } // 유효한 근접 회피 수
        public bool IsDefeated => Health <= 0; // 비행 실패 상태
        public int ItemShield { get; private set; } // 아이템의 남은 방어 횟수
        public event System.Action Grazed; // 유효한 근접 회피 발생 알림

        private void Awake() // 씬 시작 때 체력 초기화
        {
            ResetRun(); // 첫 체력과 그레이즈 설정
        }

        public void ResetRun() // 출격 재시작의 체력과 판정 초기화
        {
            capacityBonus = 0; // 이전 출격의 체력 증가 제거
            Health = maxHealth; // 최대 체력 복구
            Grazes = 0; // 근접 회피 초기화
            immuneUntil = 0f; // 이전 무적 종료
            guardUntil = 0f; // 이전 보호막 제거
            slotProtectionUntil = 0f; // 이전 공용 스킬 보호 삭제
            ItemShield = 0; // 이전 아이템의 차단 횟수 제거
            deviceShield = false; // 이전 출격의 장치 보호막 제거
            deviceReadyAt = 0f; // 장치 충전 초기화
        }

        public void AddItemShield(int charges) // 기존보다 큰 아이템 보호막만 적용
        {
            ItemShield = Mathf.Max(ItemShield, charges); // 방어 횟수를 합산하지 않음
        }

        public void Heal(int amount) // 출격 강화의 즉시 체력 회복
        {
            Health = Mathf.Min(MaxHealth, Health + amount); // 최대 체력 초과 방지
        }

        public void AddCapacity(int amount) // 이번 출격의 최대 체력 증가
        {
            capacityBonus += amount; // 선택한 임시 체력 상한
            Heal(amount); // 늘어난 체력만큼 현재 체력 반영
        }

        public void GrantProtection(float seconds) // E와 보호 카드의 시험 효과
        {
            immuneUntil = Mathf.Max(immuneUntil, Time.time + seconds); // 기존 보호 시간 보존
        }

        public void GrantSingleHitGuard(float seconds) // 제한 시간 동안 피해 한 번 방지
        {
            guardUntil = Time.time + seconds; // 리안 보호막의 유효 종료 시점
        }

        public void GrantSlotProtection(float seconds) // E 보호막의 시간형 효과
        {
            slotProtectionUntil = Time.time + seconds; // 교체할 수 있는 보호 효과
        }

        public void ClearSkillGuard() // E를 교체한 뒤 남은 효과 해제
        {
            slotProtectionUntil = 0f; // 아리아 시간형 E 해제
            guardUntil = 0f; // 리안 1회형 E 해제
        }
        internal bool ResolveThreat(Vector3 start, Vector3 end, float projectileRadius, int damage, ref bool nearCandidate, ref bool grazeAwarded) // 탄환 이동 전체와 기체 상대 경로 판정
        {
            if (IsDefeated) // 침몰한 기체의 추가 판정 제외
            {
                return false; // 실패 뒤 체력 보존
            }

            Vector3 travel = end - start; // 한 프레임의 상대 이동 구간
            float squaredLength = travel.sqrMagnitude; // 해당 구간의 제곱 거리
            float amount = squaredLength > 0f ? Mathf.Clamp01(-Vector3.Dot(start, travel) / squaredLength) : 0f; // 최근접점의 구간 비율
            float closestSquared = (start + travel * amount).sqrMagnitude; // 기체까지의 제곱 최소 거리
            float inner = hitRadius + projectileRadius; // 기체와 탄환의 접촉 반경 합
            if (closestSquared <= inner * inner) // 프레임 사이 실제 피격 여부
            {
                if (Time.time >= Mathf.Max(immuneUntil, slotProtectionUntil)) // 피격 후 무적과 장착 보호막 확인
                {
                    if (Time.time < guardUntil) // 보호막이 막을 첫 피해
                    {
                        guardUntil = 0f; // 보호막 즉시 소모
                    }
                    else if (ItemShield > 0) // 시간제 스킬 보호 뒤의 아이템 보호막
                    {
                        ItemShield--; // 실제 피격 하나를 소모
                    }
                    else if (deviceShield && upgrades != null && upgrades.HasProtectionDevice) // 카드 장치에서 남은 보호
                    {
                        deviceShield = false; // 피격 하나를 막음
                    }
                    else // 보호막 없이 피격
                    {
                        Health = Mathf.Max(0, Health - damage); // 체력 하한 적용
                        immuneUntil = Time.time + immuneSeconds; // 다음 피격 보호
                        if (!IsDefeated && upgrades != null && upgrades.HasProtectionDevice && Time.time >= deviceReadyAt) // 실제 피격 이후의 장치 충전
                        {
                            deviceShield = true; // 다음 유효 피격을 막을 보호막
                            deviceReadyAt = Time.time + 30f; // 기획의 장치 재충전
                        }
                    }
                }
                return true; // 충돌한 탄은 그레이즈 대상에서 제외
            }

            float outer = Mathf.Max(hitRadius, grazeRadius) + projectileRadius; // 근접 회피 범위
            nearCandidate |= closestSquared <= outer * outer; // 해당 탄의 근접 진입 기록
            if (!grazeAwarded && nearCandidate && end.sqrMagnitude > outer * outer) // 몸에 닿지 않고 근접 구간을 다 지난 상태
            {
                grazeAwarded = true; // 같은 탄환에서 한 번만 인정
                Grazes++; // 현재 출격의 그레이즈 증가
                Grazed?.Invoke(); // 이번 탄환의 그레이즈 전달
            }

            return false; // 충돌하지 않은 탄환의 계속 이동
        }
    }
}
