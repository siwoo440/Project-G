using UnityEngine; // 마법 효과와 자원 회복
using UnityEngine.InputSystem; // Q·E·R과 재시작 입력

namespace ProjectG // 프로젝트 게임 영역
{
    public enum FlightMagicEffect // 현재 시험 마법 카드의 효과
    {
        Volley, // 세 발의 마법탄
        Lance, // 집중 마법탄
        Ward, // 단시간 보호
        Nova, // 넓은 다발 마법탄
        Shade, // 다섯 갈래 어둠 마법
        RapidCycle, // 아리아 집중 포화
        Meteor, // 아리아 유성 탄막
        PiercingShot, // 리안 관통 사격
        SniperBeam, // 리안 천공 관측선
        SingleHitGuard, // 리안의 1회 피해 방어
        SlotGuard // 아리아 E가 주는 교체 가능한 시간 보호
    }

    [DefaultExecutionOrder(130)] // 자동탄과 카메라 갱신 뒤 발동
    public sealed class FlightSkillController : MonoBehaviour // 스킬·카드의 공통 마력 처리
    {
        [SerializeField] private FirstFlightController pilot; // 현재 비행 상태
        [SerializeField] private FlightPilotVitals vitals; // 체력·그레이즈·보호 효과
        [SerializeField] private FlightWeapon weapon; // 기본탄 프리팹과 조준 기준
        [SerializeField] private FlightThreatPool threatPool; // 화면 안 적탄 소거 대상
        [SerializeField] private FlightCombatHud hud; // 제작 게이지와 쿨타임 표시
        [SerializeField] private FlightUpgradeChoices upgrades; // 출격 카드의 Q 재사용과 E 교체
        [SerializeField] private FlightBindings bindings; // 설정에서 편집하는 세 스킬 키
        [SerializeField] private FlightEnhancements enhancements; // 근접 충전에 적용할 아이템 보너스
        [SerializeField] private FlightEnemy[] defeatRewards = System.Array.Empty<FlightEnemy>(); // 실전 스테이지의 처치 에너지 지급 대상
        private readonly float[] readyAt = new float[3]; // Q·E·R 사용 가능 시각
        private float mana = 100f; // 이번 출격의 실제 마력
        private float ultimate; // 소수 단위로 누적하는 필살기 에너지
        private int recordedGrazes; // 마지막으로 자원에 반영한 스침 수

        public int Mana => Mathf.FloorToInt(mana); // 화면과 카드가 확인하는 현재 마력
        public int Ultimate => Mathf.FloorToInt(ultimate); // HUD와 유효 보상이 보는 정수 값

        private void OnEnable() // 실제 스테이지의 처치 에너지 연결
        {
            foreach (FlightEnemy enemy in defeatRewards) // 씬에 연결한 적과 보스
            {
                enemy.Defeated += RewardDefeat; // 실제 처치에서만 에너지 지급
            }
        }

        private void OnDisable() // 씬 변경 전 처치 알림 정리
        {
            foreach (FlightEnemy enemy in defeatRewards) // 등록했던 에너지 출처
            {
                if (enemy != null) // 먼저 사라진 씬 객체 제외
                {
                    enemy.Defeated -= RewardDefeat; // 다음 출격에 중복 등록 방지
                }
            }
        }

        private void RewardDefeat(FlightEnemy enemy) // 적 타입에 맞는 에너지 한 번 지급
        {
            if (vitals.IsDefeated) // 이미 실패한 출격의 잔류탄 제외
            {
                return; // 출격 실패 시 에너지 보존
            }
            int amount = enemy.LootTier switch // 일반·중간·최종 보상의 배타적 선택
            {
                FlightLootTier.MidBoss => 10, // 중간 보스의 기획 시험값
                FlightLootTier.FinalBoss => 20, // 이후 최종 보스의 기획 시험값
                _ => 2 // 일반 적의 기획 시험값
            };
            AddUltimate(amount); // 실제 필살기 자원과 게이지 갱신
        }

        public void AddUltimate(int amount) // 레벨업 대체 보상의 필살기 에너지
        {
            ultimate = Mathf.Clamp(ultimate + amount, 0, 100); // 필살기 자원 상한 적용
            ShowResources(); // 모달 아래의 실제 게이지 반영
        }

        private void LateUpdate() // 자원 회복과 키 입력 확인
        {
            if (Time.timeScale == 0f) // 선택 중 전투 스킬 입력 차단
            {
                return; // 시간 정지 중 발동 불가
            }
            Keyboard keyboard = Keyboard.current; // 실제 입력 장치
            if (keyboard != null && bindings.Pressed(keyboard, FlightAction.Pilot)) // 설정한 시험 캐릭터 전환
            {
                weapon.TogglePilot(); // 기존 자원을 유지한 전투 설정 변경
                hud.SetSniper(weapon.IsRian); // 전용 아이콘과 패시브 전환
            }
            if (pilot.CanFly) // 실패 전 자원과 스킬만 갱신
            {
                mana = Mathf.Min(100f, mana + Time.deltaTime * 6f); // 시험 초당 마력 회복
                float charge = enhancements != null ? enhancements.GrazeScale : 1f; // 근접 충전의 실제 배율
                ultimate = Mathf.Min(100f, ultimate + (vitals.Grazes - recordedGrazes) * 10f * charge); // 소수도 보존하는 그레이즈 보상
                recordedGrazes = vitals.Grazes; // 중복 에너지 지급 방지
                if (keyboard != null) // 실제 키 입력 확인
                {
                    if (bindings.Pressed(keyboard, FlightAction.Special)) // 설정한 고유기 입력
                    {
                        float scale = upgrades != null ? upgrades.CooldownScale : 1f; // 선택 카드의 고유기 순환
                        TrySkill(0, weapon.IsRian ? FlightMagicEffect.PiercingShot : FlightMagicEffect.RapidCycle, 25, (weapon.IsRian ? 10f : 8f) * scale, false, weapon.IsRian ? 0f : 3f); // 관통 사격 또는 집중 포화
                    }
                    if (bindings.Pressed(keyboard, FlightAction.Common)) // 설정한 공용기 입력
                    {
                        bool fan = upgrades != null && upgrades.HasFan; // E가 산탄 카드로 교체된 상태
                        FlightMagicEffect effect = fan ? FlightMagicEffect.Nova : (weapon.IsRian ? FlightMagicEffect.SingleHitGuard : FlightMagicEffect.SlotGuard); // 산탄 또는 각 캐릭터의 보호막
                        TrySkill(1, effect, 30, fan ? 8f : (weapon.IsRian ? 12f : 6f), false); // 슬롯의 현재 재사용 시간
                    }
                    if (bindings.Pressed(keyboard, FlightAction.Ultimate)) // 설정한 필살기 입력
                    {
                        TrySkill(2, weapon.IsRian ? FlightMagicEffect.SniperBeam : FlightMagicEffect.Meteor, 100, 12f, true); // 천공 관측선 또는 유성 탄막
                    }
                }
            }
            ShowResources(); // 현재 마력과 에너지 표시
        }

        internal void ResetRun() // 새 출격의 자원과 사용 가능 시각
        {
            mana = 100f; // 새 출격의 마력
            ultimate = 0; // 새 출격의 필살기 에너지
            recordedGrazes = 0; // 스침 반영 초기화
            weapon.ResetCadence(); // 새 출격의 스킬과 패시브 초기화
            for (int slot = 0; slot < readyAt.Length; slot++) // 세 슬롯 초기화
            {
                readyAt[slot] = 0f; // 이전 쿨타임 제거
                hud.SetCooldown(slot, 0f); // 화면의 이전 숫자 제거
            }
            ShowResources(); // 마력과 필살기 에너지 갱신
        }

        public bool TryUseCard(FlightMagicEffect effect, int cost) // 현재 카드의 성공한 실행에만 비용 차감
        {
            if (Time.timeScale == 0f || !pilot.CanFly || !pilot.IsAimView || mana < cost || !Activate(effect)) // 실제 사용 자원과 효과 확인
            {
                return false; // 카드 순서와 마력을 보존
            }
            mana -= cost; // 해당 카드 보석에 표시한 마력 비용
            ShowResources(); // 클릭 다음 프레임에 실제 자원 갱신
            return true; // 카드 순환 가능 여부
        }

        private void TrySkill(int slot, FlightMagicEffect effect, int cost, float cooldown, bool useUltimate, float startsIn = 0f) // Q·E·R 공통 조건과 효과
        {
            float value = useUltimate ? ultimate : mana; // 해당 자원 선택
            if (Time.time < readyAt[slot] || value < cost || !Activate(effect, useUltimate)) // 대기·자원·실제 효과 검사
            {
                return; // 소모와 쿨타임 없이 종료
            }
            if (useUltimate) // 필살기 에너지 차감
            {
                ultimate -= cost; // 사용 후 남은 에너지
            }
            else // 일반 스킬의 마력 차감
            {
                mana -= cost; // 사용 후 남은 마력
            }
            readyAt[slot] = Time.time + startsIn + cooldown; // 재사용 시간 시작
            hud.SetDelayedCooldown(slot, cooldown, startsIn); // 슬롯 안 흰 숫자 갱신
        }

        private bool Activate(FlightMagicEffect effect, bool empowered = false) // 카드와 스킬이 공유하는 실제 효과
        {
            switch (effect) // 무기와 보호 효과 분기
            {
                case FlightMagicEffect.Volley: // 다발 화살
                    return weapon.CastSpread(false, 3, 10f); // 세 발 실제 마법탄
                case FlightMagicEffect.Lance: // 집중 마법탄
                    return weapon.CastSpread(true, 1, 0f); // 한 발의 빠른 관통형 실루엣
                case FlightMagicEffect.Ward: // 보호막
                    vitals.GrantProtection(1.4f); // 현재 적탄 피해 잠시 차단
                    return true; // 보호 발생 성공
                case FlightMagicEffect.SlotGuard: // 아리아의 공용 E
                    vitals.GrantSlotProtection(1.4f); // 교체 시 해제할 수 있는 보호
                    return true; // 보호 발생 성공
                case FlightMagicEffect.Nova: // 넓은 방사 사격
                    return weapon.CastSpread(empowered, empowered ? 9 : 7, 23f); // 넓게 퍼지는 카드 또는 필살기
                case FlightMagicEffect.Shade: // 어둠 부채꼴
                    return weapon.CastSpread(true, 5, 14f); // 다섯 발의 좁은 집중탄
                case FlightMagicEffect.RapidCycle: // 아리아의 집중 포화
                    weapon.BeginRapidFire(); // 자동 기본·집중탄 간격 30% 감소
                    return true; // 가속 발동
                case FlightMagicEffect.PiercingShot: // 리안의 관통 사격
                    return weapon.CastPiercingShot(); // 발사 성공 후 마력 소비
                case FlightMagicEffect.SniperBeam: // 리안의 천공 관측선
                    return weapon.CastSniperBeam(threatPool); // 광선 적중과 경로의 적탄 소거
                case FlightMagicEffect.SingleHitGuard: // 리안의 공용 보호막 시험값
                    vitals.GrantSingleHitGuard(1f); // 첫 피해 또는 1초에 만료되는 보호막
                    return true; // 보호 발생 성공
                case FlightMagicEffect.Meteor: // 아리아의 유성 탄막
                    threatPool.ClearInView(Camera.main); // 화면 안 적탄만 소거
                    foreach (FlightEnemy enemy in FindObjectsByType<FlightEnemy>(FindObjectsSortMode.None)) // 씬의 살아 있는 일반 적
                    {
                        enemy.ReceiveHit(80); // 각 적의 실제 시험 피해
                    }
                    return true; // 유성 탄막 발동
                default: // 정의되지 않은 마법
                    return false; // 불명확한 효과 사용 거절
            }
        }

        private void ShowResources() // 현재 소비한 자원을 전투 HUD에 연결
        {
            hud.SetResources(Mana, Ultimate); // 마력과 필살기 에너지 별도 표시
        }
    }
}
