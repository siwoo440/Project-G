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
        private FlightBroomEquipment equipment; // 실제 장착 부품의 피해 감소 퍼센트포인트
        private FlightSkillController skills; // 피격 효과가 회복할 실제 마력
        private float immuneUntil; // 다음 피격 가능 시각
        private float guardUntil; // 한 발만 막는 리안 보호막의 종료 시점
        private float slotProtectionUntil; // 교체할 수 있는 E 시간 보호막의 종료 시점
        private int capacityBonus; // 선택한 이번 출격의 최대 체력
        private bool deviceShield; // 피격 후 장치가 한 번 막을 수 있는 상태
        private float deviceReadyAt; // 장치 방어막의 다음 충전
        private readonly FlightBroomShield equipmentShield = new(); // 기여분별 시간과 잔여량을 지키는 장비 보호막
        private float guardSetReadyAt; // 수호 네 세트의 내부 재사용 시각
        private int guardSetTriggers; // 이번 출격의 수호 세트 발동 횟수
        private float guardBodyReadyAt; // 전설 수호 몸체의 내부 재사용 시각
        private int guardBodyTriggers; // 이번 출격의 수호 몸체 발동 횟수
        private float guardOrnamentReadyAt; // 수호 장식의 내부 재사용 시각
        private int guardOrnamentTriggers; // 이번 출격의 수호 장식 발동 횟수
        private float guardManaReadyAt; // 전설 수호 마력석의 내부 재사용 시각
        private int guardManaTriggers; // 이번 출격의 수호 마력석 발동 횟수
        private FlightThreatPool threatPool; // 파괴 시 소거할 실제 적탄 풀
        private float guardBreakReadyAt; // 수호 여덟 세트의 파괴 소거 대기 시각
        private int guardBreakTriggers; // 이번 출격의 세트 파괴 소거 횟수
        private float guardPartBreakReadyAt; // 전설 장식의 파괴 소거 대기 시각
        private int guardPartBreakTriggers; // 이번 출격의 장식 파괴 소거 횟수
        private float frostSetReadyAt; // 서리 여덟 세트의 피격 소거 대기 시각
        private int frostSetTriggers; // 이번 출격의 서리 세트 소거 횟수

        public int Health { get; private set; } // 현재 출격 체력
        public int MaxHealth => maxHealth + capacityBonus; // 임시 성장까지 포함한 최대 체력
        internal float CollisionRadius => hitRadius;
        public int Grazes { get; private set; } // 유효한 근접 회피 수
        public bool IsDefeated => Health <= 0; // 비행 실패 상태
        public int ItemShield { get; private set; } // 아이템의 남은 방어 횟수
        internal int EquipmentShield => equipmentShield.Total(Time.time); // 만료를 반영한 장비 보호막 잔여량
        public event System.Action Grazed; // 유효한 근접 회피 발생 알림

        private void Awake() // 씬 시작 때 체력 초기화
        {
            TryGetComponent(out equipment); // 장비 없는 시험 기체에서는 피해 감소 0%
            TryGetComponent(out skills); // 같은 기체의 실제 마력 자원
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
            equipmentShield.Clear(); // 이전 출격의 개별 장비 보호막 제거
            guardSetReadyAt = 0f; // 새 출격의 수호 세트 쿨타임 초기화
            guardSetTriggers = 0; // 새 출격의 수호 세트 횟수 초기화
            guardBodyReadyAt = 0f; // 새 출격의 수호 몸체 대기 초기화
            guardBodyTriggers = 0; // 새 출격의 수호 몸체 횟수 초기화
            guardOrnamentReadyAt = 0f; // 새 출격의 수호 장식 대기 초기화
            guardOrnamentTriggers = 0; // 새 출격의 수호 장식 횟수 초기화
            guardManaReadyAt = 0f; // 새 출격의 수호 마력석 대기 초기화
            guardManaTriggers = 0; // 새 출격의 수호 마력석 횟수 초기화
            guardBreakReadyAt = 0f; // 새 출격의 세트 파괴 소거 대기 초기화
            guardBreakTriggers = 0; // 새 출격의 세트 파괴 소거 횟수 초기화
            guardPartBreakReadyAt = 0f; // 새 출격의 장식 파괴 소거 대기 초기화
            guardPartBreakTriggers = 0; // 새 출격의 장식 파괴 소거 횟수 초기화
            frostSetReadyAt = 0f; // 새 출격의 서리 세트 대기 초기화
            frostSetTriggers = 0; // 새 출격의 서리 세트 횟수 초기화
        }

        internal int AddEquipmentShield(int amount, float duration) // 기획한 개별 보호막 기여분
        {
            return equipmentShield.Add(amount, duration, MaxHealth, Time.time); // 현재 최대 체력의 30% 상한 적용
        }

        private void TriggerEquipmentHit() // 실제 유효 피격 후 장비 보호막 발동
        {
            if (IsDefeated || equipment == null) // 실패했거나 실제 장비가 없는 경우
            {
                return; // 발동할 수 없는 접촉은 보호막을 만들지 않음
            }
            if (equipment.SetPieces("guard") >= 4 && Time.time >= guardSetReadyAt && guardSetTriggers < 20) // 수호 네 부위와 내부 발동 제한
            {
                AddEquipmentShield(10, 5f); // 기획된 세트 보호막 10과 지속 시간 5초
                guardSetReadyAt = Time.time + 8f; // 수호 세트의 내부 쿨타임
                guardSetTriggers++; // 이번 출격에서 최대 스무 번의 발동
            }
            if (equipment.HasSpecial("guard", FlightBroomSlot.Body, 5) && Time.time >= guardBodyReadyAt && guardBodyTriggers < 15) // 전설 수호 몸체의 실제 발동 조건
            {
                AddEquipmentShield(4, 5f); // 사용자가 확정한 독립 지속시간 5초
                guardBodyReadyAt = Time.time + 10f; // 수호 몸체의 내부 쿨타임
                guardBodyTriggers++; // 해당 개별 효과의 출격당 최대 15회
            }
            if (equipment.HasSpecial("guard", FlightBroomSlot.Ornament, 3) && Time.time >= guardOrnamentReadyAt && guardOrnamentTriggers < 15) // 희귀 이상 수호 장식의 실제 발동 조건
            {
                AddEquipmentShield(3, 5f); // 사용자가 확정한 독립 지속시간 5초
                guardOrnamentReadyAt = Time.time + 10f; // 수호 장식의 내부 쿨타임
                guardOrnamentTriggers++; // 해당 개별 효과의 출격당 최대 15회
            }
            if (skills != null && equipment.HasSpecial("guard", FlightBroomSlot.ManaStone, 5) && Time.time >= guardManaReadyAt && guardManaTriggers < 15) // 전설 수호 마력석의 유효 피격 효과
            {
                skills.RestoreCraftedMana(3f); // 실제 마력 상한 안에서 마력 3 회복
                guardManaReadyAt = Time.time + 8f; // 마력석의 내부 쿨타임
                guardManaTriggers++; // 해당 개별 효과의 출격당 최대 15회
            }
            if (equipment.SetPieces("frost") >= 8 && Time.time >= frostSetReadyAt && frostSetTriggers < 10) // 서리 여덟 부위의 실제 유효 피격 제한
            {
                if (threatPool == null) // 아직 찾지 않은 실제 적탄 풀
                {
                    threatPool = FindFirstObjectByType<FlightThreatPool>(); // 현재 씬의 재사용 적탄 관리
                }
                if (threatPool != null) // 실제 적탄 소거가 가능한 전투 씬
                {
                    threatPool.QueueCancelWithin(transform.position, 3f, 5); // 탄환 순회 후 3m 내 최대 다섯 발 소거
                    frostSetReadyAt = Time.time + 12f; // 서리 세트의 내부 쿨타임
                    frostSetTriggers++; // 이번 출격의 최대 열 번 기록
                }
            }
        }

        private void TriggerEquipmentShieldBreak() // 장비 보호막이 실제 피해로 모두 파괴된 경우
        {
            if (equipment == null) // 실제 장비가 없는 시험 기체
            {
                return; // 다른 보호막과 소거 효과를 만들지 않음
            }
            if (threatPool == null) // 아직 찾지 않은 실제 적탄 풀
            {
                threatPool = FindFirstObjectByType<FlightThreatPool>(); // 현재 씬의 재사용 적탄 관리
            }
            if (threatPool == null) // 소거할 수 있는 실제 전투 풀이 없음
            {
                return; // 없는 적탄 풀의 발동 기록을 소비하지 않음
            }
            if (equipment.SetPieces("guard") >= 8 && Time.time >= guardBreakReadyAt && guardBreakTriggers < 12) // 수호 여덟 세트의 실제 발동 제한
            {
                threatPool.QueueCancelWithin(transform.position, 2f, 5); // 현재 탄환 순회 후 2m 내 최대 다섯 발
                guardBreakReadyAt = Time.time + 10f; // 세트 소거의 내부 쿨타임
                guardBreakTriggers++; // 이번 출격의 최대 열두 번 기록
            }
            if (equipment.HasSpecial("guard", FlightBroomSlot.Ornament, 5) && Time.time >= guardPartBreakReadyAt && guardPartBreakTriggers < 10) // 전설 수호 장식의 별도 소거 제한
            {
                threatPool.QueueCancelWithin(transform.position, 2f, 1); // 같은 보호막 파괴의 추가 적탄 최대 한 발
                guardPartBreakReadyAt = Time.time + 12f; // 개별 장식의 내부 쿨타임
                guardPartBreakTriggers++; // 이번 출격의 최대 열 번 기록
            }
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
                if (Time.time >= immuneUntil) // 피격 후 무적이 끝난 유효 접촉
                {
                    if (Time.time < slotProtectionUntil) // 기존 시간형 스킬 보호가 먼저 차단
                    {
                        if (damage > 0) // 양의 피해를 지닌 실제 적탄
                        {
                            TriggerEquipmentHit(); // 기존 보호에 막힌 유효 피격도 장비 효과 발동
                        }
                        return true; // 해당 탄환은 실제 충돌 처리
                    }
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
                        float guard = equipment != null ? equipment.GetBonus(FlightBroomStat.DamageReduction) : 0f; // 장착한 몸체·마법 장식의 합계
                        float reduction = Mathf.Clamp(guard, 0f, 50f) * 0.01f; // 문서 확정 피해 감소 상한 50%
                        int incoming = damage > 0 ? Mathf.Max(1, Mathf.RoundToInt(damage * (1f - reduction))) : 0; // 장착 보너스가 반영된 실제 피격량
                        int shieldBefore = equipmentShield.Total(Time.time); // 피해가 닿기 전의 실제 장비 보호막
                        int remaining = equipmentShield.Absorb(incoming, Time.time); // 감소된 피해를 만료가 빠른 장비 보호막부터 흡수
                        if (shieldBefore > 0 && incoming > 0 && equipmentShield.Total(Time.time) == 0) // 실제 피해로 장비 보호막이 모두 파괴
                        {
                            TriggerEquipmentShieldBreak(); // 적탄 순회를 방해하지 않는 파괴 소거 예약
                        }
                        Health = Mathf.Max(0, Health - remaining); // 보호막을 통과한 실제 피해만 체력 차감
                        if (incoming > 0) // 장비와 보호막의 실제 피해가 남은 경우
                        {
                            FlightAudio.Play(FlightSfx.PilotHit); // 충돌과 피격 무적의 구분 신호
                        }
                        immuneUntil = Time.time + immuneSeconds; // 다음 피격 보호
                        if (!IsDefeated && upgrades != null && upgrades.HasProtectionDevice && Time.time >= deviceReadyAt) // 실제 피격 이후의 장치 충전
                        {
                            deviceShield = true; // 다음 유효 피격을 막을 보호막
                            deviceReadyAt = Time.time + 30f; // 기획의 장치 재충전
                        }
                    }
                    if (damage > 0) // 양의 피해를 지닌 유효한 실제 접촉
                    {
                        TriggerEquipmentHit(); // 기존 보호에 막혀도 실제 장비 효과 발동
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
                FlightAudio.Play(FlightSfx.Graze); // 한 번 확정된 스침 신호
                Grazed?.Invoke(); // 이번 탄환의 그레이즈 전달
            }

            return false; // 충돌하지 않은 탄환의 계속 이동
        }
    }
}
