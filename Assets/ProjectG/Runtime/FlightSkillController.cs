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
        SlotGuard, // 아리아 E가 주는 교체 가능한 시간 보호
        StarlightSeekers, // 루미의 별빛 추적탄
        ConstellationRoute, // 루미의 별무리 항로
        BreakthroughBlade, // 세라의 돌파 검기와 비행
        SkyChainSlash, // 세라의 천공 연참
        ShadowCounter, // 녹스가 표식을 써서 발사하는 반격탄
        BoundaryReversal, // 녹스 주변의 적탄 소거와 방사 피해
        DeployVeraTurret, // 베라 마도 포대 설치
        TurretCrossBeam, // 배치한 포대에서 발사하는 교차 광선
        IrisSpreadWave, // 이리스의 넓은 확산 파동
        IrisSkyResonance // 두 차례 이어지는 창공의 공명
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
        private const int BaseMana = 100; // 빗자루를 장착하기 전 공통 마력
        private const float BaseRecovery = 6f; // 빗자루를 장착하기 전 초당 회복
        private FlightBroomEquipment equipment; // 마력 용량과 장식의 회복 효과
        private float mana = BaseMana; // 이번 출격의 실제 마력
        private float ultimate; // 소수 단위로 누적하는 필살기 에너지
        private int recordedGrazes; // 마지막으로 자원에 반영한 스침 수
        private float nextWeakReduction; // 실제 약점 적중의 초당 재사용 감소 제한
        private float flowSetReadyAt; // 마력 순환 여덟 세트의 내부 재사용 시각
        private int flowSetTriggers; // 이번 출격에서 실제로 발동한 횟수
        private float flowOrnamentReadyAt; // 순환 장식의 내부 재사용 시각
        private int flowOrnamentTriggers; // 이번 출격의 순환 장식 발동 횟수
        private float shadowSetReadyAt; // 그림자 네 세트의 내부 재사용 시각
        private int shadowSetTriggers; // 이번 출격의 그레이즈 회복 횟수
        private float shadowManaReadyAt; // 그림자 마력석의 내부 재사용 시각
        private int shadowManaTriggers; // 이번 출격의 마력석 그레이즈 회복 횟수
        private float shadowOrnamentReadyAt; // 그림자 장식의 내부 재사용 시각
        private int shadowOrnamentTriggers; // 이번 출격의 장식 회복 횟수
        private int shadowOrnamentGrazes; // 장식 발동까지 누적한 유효 스침
        private float shadowCounterReadyAt; // 그림자 여덟 세트의 반격탄 재사용 시각
        private int shadowCounterTriggers; // 이번 출격의 세트 반격탄 발동 횟수
        private int shadowCounterGrazes; // 세트 반격탄의 유효 스침 누적
        private float shadowPartCounterReadyAt; // 전설 장식의 반격탄 재사용 시각
        private int shadowPartCounterTriggers; // 이번 출격의 장식 반격탄 발동 횟수
        private sealed class DashTrigger // 한 장비 효과의 재사용과 출격별 횟수
        {
            public float ReadyAt; // 다음에 발동 가능한 실제 시각
            public int Count; // 현재 출격에서 발동한 횟수
        }
        private readonly System.Collections.Generic.Dictionary<string, DashTrigger> dashTriggers = new(); // 세트와 부품별 독립 발동 이력

        public int MaxMana => Mathf.Max(1, BaseMana + Mathf.RoundToInt(equipment != null ? equipment.GetBonus(FlightBroomStat.Mana) : 0f)); // 장착한 마력석과 마법 장식의 용량
        public float ManaRecovery => Mathf.Max(0f, BaseRecovery + (equipment != null ? equipment.GetBonus(FlightBroomStat.ManaRecovery) : 0f)); // 장착의 증가와 불이익을 합산한 초당 마력
        public int Mana => Mathf.FloorToInt(mana); // 화면과 카드가 확인하는 현재 마력
        internal bool IsLowMana => mana <= 30f; // 소수점을 보존한 낮은 마력 조건
        public int Ultimate => Mathf.FloorToInt(ultimate); // HUD와 유효 보상이 보는 정수 값
        internal event System.Action<int> SkillActivated; // 안내에서 성공 시전만 확인

        private void Awake() // 실제 출격 기체의 장착 확인
        {
            TryGetComponent(out equipment); // 부품이 없는 연습의 공통 마력 유지
        }

        internal void BindTrainingEquipment(FlightBroomEquipment value) // 자유 훈련의 저장 빗자루 연결
        {
            equipment = value; // 실전과 같은 마력 용량·회복 적용
        }

        private void Start() // 출격에서 조종사와 장비를 선택한 뒤 마력 초기화
        {
            mana = MaxMana; // 새 빗자루의 실제 마력 상한까지 충전
            ShowResources(); // 전투와 마력 게이지의 첫 표시
        }

        private void OnEnable() // 실제 스테이지의 처치 에너지 연결
        {
            if (pilot != null) // 실제 3D 돌파를 수행하는 비행체
            {
                pilot.DashFinished += HandleDashFinished; // 돌파 종료에만 장비 효과 적용
            }
            if (vitals != null) // 실제 그레이즈 판정이 연결된 경우
            {
                vitals.Grazed += RecoverShadowGraze; // 성공한 스침에서만 세트 효과 확인
            }
            foreach (FlightEnemy enemy in defeatRewards) // 씬에 연결한 적과 보스
            {
                enemy.Defeated += RewardDefeat; // 실제 처치에서만 에너지 지급
            }
        }

        private void OnDisable() // 씬 변경 전 처치 알림 정리
        {
            if (pilot != null) // 이전에 연결한 실제 비행체
            {
                pilot.DashFinished -= HandleDashFinished; // 씬 재활성화 시 중복 구독 방지
            }
            if (vitals != null) // 이전에 연결한 실제 그레이즈 판정
            {
                vitals.Grazed -= RecoverShadowGraze; // 재활성화 시 중복 구독 방지
            }
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

        internal void RegisterStorySource(FlightEnemy enemy)
        {
            if (enemy == null || System.Array.IndexOf(defeatRewards, enemy) >= 0) return;
            System.Array.Resize(ref defeatRewards, defeatRewards.Length + 1);
            defeatRewards[defeatRewards.Length - 1] = enemy;
            if (isActiveAndEnabled) enemy.Defeated += RewardDefeat;
        }

        internal void UnregisterStorySource(FlightEnemy enemy)
        {
            if (enemy != null) enemy.Defeated -= RewardDefeat;
            defeatRewards = System.Array.FindAll(defeatRewards, value => value != null && value != enemy);
        }

        private bool TriggerAvailable(string id, int maximum) // 한 효과의 현재 쿨타임과 횟수
        {
            return !dashTriggers.TryGetValue(id, out DashTrigger state) || Time.time >= state.ReadyAt && state.Count < maximum; // 처음 발동 또는 제한 안의 재사용
        }

        private void RecordTrigger(string id, float cooldown) // 실제로 성공한 장비 효과 기록
        {
            if (!dashTriggers.TryGetValue(id, out DashTrigger state)) // 이번 출격의 첫 발동
            {
                state = new DashTrigger(); // 해당 효과만의 재사용 상태
                dashTriggers.Add(id, state); // 다른 개별 효과와 분리해 보존
            }
            state.ReadyAt = Time.time + cooldown; // 기획된 내부 쿨타임
            state.Count++; // 해당 효과의 이번 출격 횟수
        }

        private void TriggerBolts(string id, int count, int damage, float cooldown, int maximum) // 실제로 생성한 장비 추가탄
        {
            if (weapon != null && TriggerAvailable(id, maximum) && weapon.FireBroomBolts(count, damage)) // 한도와 실제 조준·발사 성공
            {
                RecordTrigger(id, cooldown); // 발사한 경우에만 쿨타임과 횟수 반영
            }
        }

        private void DashMana(string id, float cooldown, int maximum) // 실제 종료 시 기획된 마력 3 회복
        {
            if (TriggerAvailable(id, maximum)) // 해당 개별 효과의 독립 제한
            {
                mana = Mathf.Min(MaxMana, mana + 3f); // 실제 마력 상한을 넘지 않는 회복
                RecordTrigger(id, cooldown); // 발동한 부품의 쿨타임과 횟수
            }
        }

        private void HandleDashFinished() // 실제 세라 돌파 종료의 장비 효과
        {
            if (equipment == null || !pilot.CanFly) // 실제 장비가 없거나 실패한 출격
            {
                return; // 중단된 이동의 추가 효과 없음
            }
            equipment.OnDashFinished(); // 전설 폭풍 장식의 한시 비행 속도
            if (equipment.SetPieces("storm") >= 8) // 폭풍 여덟 세트의 실제 장착
            {
                TriggerBolts("storm.set8", 3, 8, 3f, 30); // 세트의 전방 바람탄 세 발
            }
            if (equipment.HasSpecial("storm", FlightBroomSlot.ManaStone, 5)) // 전설 폭풍 마력석
            {
                DashMana("storm.manastone", 5f, 20); // 대시 종료의 마력 3 회복
            }
            if (equipment.HasSpecial("flow", FlightBroomSlot.Propulsion, 5)) // 전설 수맥 추진부
            {
                DashMana("flow.propulsion", 6f, 20); // 대시 종료의 독립 마력 3 회복
            }
            if (equipment.HasSpecial("storm", FlightBroomSlot.Propulsion, 5)) // 전설 질풍 추진부
            {
                TriggerBolts("storm.propulsion", 1, 5, 4f, 25); // 대시 종료의 바람탄 한 발
            }
            if (equipment.HasSpecial("starlight", FlightBroomSlot.Propulsion, 5)) // 전설 유성 추진부
            {
                TriggerBolts("starlight.propulsion", 1, 5, 6f, 20); // 대시 종료의 별빛탄 한 발
            }
            if (equipment.HasSpecial("shadow", FlightBroomSlot.Propulsion, 5)) // 전설 야행 추진부
            {
                TriggerBolts("shadow.propulsion", 1, 5, 5f, 25); // 대시 종료의 잔상탄 한 발
            }
            if (equipment.HasSpecial("flame", FlightBroomSlot.Propulsion, 3)) // 희귀 이상 홍염 추진부
            {
                TriggerBolts("flame.propulsion", 1, 5, 5f, 25); // 대시 종료의 화염탄 한 발
            }
            if (equipment.HasSpecial("thunder", FlightBroomSlot.Propulsion, 3)) // 희귀 이상 뇌전 추진부
            {
                TriggerBolts("thunder.propulsion", 1, 5, 5f, 25); // 대시 종료의 번개탄 한 발
            }
            ShowResources(); // 같은 프레임의 실제 마력 회복 표시
        }

        internal void OnCraftedFirstHit() // 실전 제작 마법의 시전당 최초 유효 명중
        {
            if (equipment != null && equipment.SetPieces("starlight") >= 8) // 별빛 여덟 세트의 실제 장착
            {
                TriggerBolts("starlight.set8.hit", 2, 8, 6f, 30); // 최초 명중의 독립 별빛탄 두 발
            }
            if (equipment != null && equipment.HasSpecial("flow", FlightBroomSlot.Tip, 5) && TriggerAvailable("flow.tip.hit", 20)) // 전설 수맥 선단부의 독립 제한
            {
                mana = Mathf.Min(MaxMana, mana + 2f); // 실제 마력 상한 안에서 2 회복
                RecordTrigger("flow.tip.hit", 6f); // 출격당 횟수와 6초 쿨타임
                ShowResources(); // 회복한 마력의 화면 반영
            }
        }

        private void RecoverShadowGraze() // 실제 스침에서 그림자 네 세트 적용
        {
            if (equipment == null) // 실제 빗자루가 없는 시험 기체
            {
                return; // 발동할 수 있는 장비 효과 없음
            }
            if (equipment.SetPieces("shadow") >= 4 && Time.time >= shadowSetReadyAt && shadowSetTriggers < 60) // 그림자 네 세트의 실제 발동 제한
            {
                mana = Mathf.Min(MaxMana, mana + 2f); // 실제 최대 마력을 넘지 않는 세트 회복
                shadowSetReadyAt = Time.time + 1f; // 세트 회복의 내부 재사용 간격
                shadowSetTriggers++; // 이번 출격의 최대 60회 기록
            }
            if (equipment.HasSpecial("shadow", FlightBroomSlot.ManaStone, 3) && Time.time >= shadowManaReadyAt && shadowManaTriggers < 60) // 희귀 이상 그림자 마력석의 독립 발동
            {
                mana = Mathf.Min(MaxMana, mana + 1f); // 실제 최대 마력을 넘지 않는 개별 회복
                shadowManaReadyAt = Time.time + 1f; // 마력석 회복의 내부 재사용 간격
                shadowManaTriggers++; // 해당 부품의 출격당 최대 60회 기록
            }
            if (equipment.HasSpecial("shadow", FlightBroomSlot.Ornament, 3)) // 희귀 이상 그림자 장식의 실제 장착
            {
                shadowOrnamentGrazes = Mathf.Min(3, shadowOrnamentGrazes + 1); // 대기 중에도 최대 발동 조건까지 스침 누적
                if (shadowOrnamentGrazes >= 3 && Time.time >= shadowOrnamentReadyAt && shadowOrnamentTriggers < 25) // 세 번의 스침과 독립 발동 제한
                {
                    mana = Mathf.Min(MaxMana, mana + 3f); // 실제 최대 마력 안에서 장식 회복
                    shadowOrnamentGrazes = 0; // 실제 발동한 뒤에만 누적 초기화
                    shadowOrnamentReadyAt = Time.time + 5f; // 장식 회복의 내부 쿨타임
                    shadowOrnamentTriggers++; // 해당 부품의 출격당 최대 25회 기록
                }
            }
            else // 해당 장식이 장착되지 않은 경우
            {
                shadowOrnamentGrazes = 0; // 다른 장비의 스침을 해당 장식에 누적하지 않음
            }
            if (equipment.SetPieces("shadow") >= 8) // 그림자 여덟 세트의 실제 장착
            {
                shadowCounterGrazes = Mathf.Min(5, shadowCounterGrazes + 1); // 쿨타임 중에도 다섯 스침까지 누적
                if (shadowCounterGrazes >= 5 && Time.time >= shadowCounterReadyAt && shadowCounterTriggers < 30 && weapon != null && weapon.FireBroomBolts(2, 8)) // 다음 유효 스침의 실제 두 발 발사
                {
                    shadowCounterGrazes = 0; // 실제 발동한 뒤 누적 초기화
                    shadowCounterReadyAt = Time.time + 4f; // 세트 반격탄의 내부 쿨타임
                    shadowCounterTriggers++; // 이번 출격의 최대 서른 번 발동
                }
            }
            else // 그림자 여덟 부위가 아닌 경우
            {
                shadowCounterGrazes = 0; // 다른 조합의 스침은 누적하지 않음
            }
            if (equipment.HasSpecial("shadow", FlightBroomSlot.Ornament, 5) && Time.time >= shadowPartCounterReadyAt && shadowPartCounterTriggers < 25 && weapon != null && weapon.FireBroomBolts(1, 4)) // 전설 그림자 장식의 실제 반격탄
            {
                shadowPartCounterReadyAt = Time.time + 4f; // 개별 반격탄의 내부 쿨타임
                shadowPartCounterTriggers++; // 해당 부품의 출격당 최대 25회
            }
            ShowResources(); // 실제 회복한 마력의 화면 갱신
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
            if (keyboard != null && bindings.Pressed(keyboard, FlightAction.Pilot) && FindFirstObjectByType<FlightEnemyEncounter>()?.CanSwitchPilot != false) // 연습과 참여 전 캐릭터 시험 전환
            {
                weapon.TogglePilot(); // 기존 자원을 유지한 전투 설정 변경
                hud.SetPilot(weapon); // 전용 아이콘과 패시브 전환
                upgrades?.RefreshCommonSkill(); // 시험 전환에 다른 조종사의 공용 아이콘 동기화
            }
            if (pilot.CanFly) // 실패 전 자원과 스킬만 갱신
            {
                mana = Mathf.Min(MaxMana, mana + Time.deltaTime * ManaRecovery); // 실제 장착 용량과 초당 회복 적용
                float charge = enhancements != null ? enhancements.GrazeScale : 1f; // 근접 충전의 실제 배율
                ultimate = Mathf.Min(100f, ultimate + (vitals.Grazes - recordedGrazes) * 10f * charge); // 소수도 보존하는 그레이즈 보상
                recordedGrazes = vitals.Grazes; // 중복 에너지 지급 방지
                if (keyboard != null) // 실제 키 입력 확인
                {
                    if (bindings.Pressed(keyboard, FlightAction.Special)) // 설정한 고유기 입력
                    {
                        float scale = upgrades != null ? upgrades.CooldownScale : 1f; // 선택 카드의 고유기 순환
                        bool lumi = weapon.ActivePilot == FlightBroomPilot.Lumi; // 별빛 추적탄의 전용 발사
                        FlightMagicEffect effect = weapon.ActivePilot switch // 현재 조종사의 고유기
                        {
                            FlightBroomPilot.Lumi => FlightMagicEffect.StarlightSeekers, // 별빛 추적탄
                            FlightBroomPilot.Iris => FlightMagicEffect.IrisSpreadWave, // 전방 확산 파동
                            FlightBroomPilot.Vera => FlightMagicEffect.DeployVeraTurret, // 마도 포대 배치
                            FlightBroomPilot.Nox => FlightMagicEffect.ShadowCounter, // 그림자 반격
                            FlightBroomPilot.Sera => FlightMagicEffect.BreakthroughBlade, // 돌파 검기
                            FlightBroomPilot.Rian => FlightMagicEffect.PiercingShot, // 관통 사격
                            _ => FlightMagicEffect.RapidCycle // 집중 포화
                        };
                        TrySkill(0, effect, 25, (lumi ? 9f : weapon.IsRian || effect == FlightMagicEffect.DeployVeraTurret ? 10f : 8f) * scale, false, effect == FlightMagicEffect.RapidCycle ? 3f : 0f); // 조종사별 실제 대기시간
                    }
                    if (bindings.Pressed(keyboard, FlightAction.Common)) // 설정한 공용기 입력
                    {
                        bool fan = upgrades != null && upgrades.HasFan; // E가 산탄 카드로 교체된 상태
                        FlightMagicEffect effect = fan ? FlightMagicEffect.Nova : (weapon.ActivePilot != FlightBroomPilot.Aria ? FlightMagicEffect.SingleHitGuard : FlightMagicEffect.SlotGuard); // 산탄 또는 각 캐릭터의 보호막
                        TrySkill(1, effect, 30, fan ? 8f : (weapon.ActivePilot != FlightBroomPilot.Aria ? 12f : 6f), false); // 슬롯의 현재 재사용 시간
                    }
                    if (bindings.Pressed(keyboard, FlightAction.Ultimate)) // 설정한 필살기 입력
                    {
                        FlightMagicEffect effect = weapon.ActivePilot switch // 현재 조종사의 필살기
                        {
                            FlightBroomPilot.Iris => FlightMagicEffect.IrisSkyResonance, // 주 파동과 추가 연쇄
                            FlightBroomPilot.Vera => FlightMagicEffect.TurretCrossBeam, // 포대에서만 발사하는 광선
                            FlightBroomPilot.Nox => FlightMagicEffect.BoundaryReversal, // 경계 역전의 3D 소거
                            FlightBroomPilot.Sera => FlightMagicEffect.SkyChainSlash, // 천공 연참
                            FlightBroomPilot.Lumi => FlightMagicEffect.ConstellationRoute, // 별무리 항로
                            FlightBroomPilot.Rian => FlightMagicEffect.SniperBeam, // 천공 관측선
                            _ => FlightMagicEffect.Meteor // 유성 탄막
                        };
                        TrySkill(2, effect, 100, 12f, true); // 충전한 에너지와 재사용 조건
                    }
                }
            }
            ShowResources(); // 현재 마력과 에너지 표시
        }

        internal void RefillMana() // 연속 보스 사이에 시전 조건만 복구
        {
            mana = MaxMana; // 현재 조합의 최대 마력
            ShowResources(); // 필살기 충전과 쿨타임을 유지한 HUD
        }

        internal void ResetRun() // 새 출격의 자원과 사용 가능 시각
        {
            mana = MaxMana; // 새 출격의 장착 마력으로 초기화
            ultimate = 0; // 새 출격의 필살기 에너지
            recordedGrazes = 0; // 스침 반영 초기화
            nextWeakReduction = 0f; // 이전 출격의 약점 감소 제한 제거
            flowSetReadyAt = 0f; // 새 출격의 순환 세트 대기 초기화
            flowSetTriggers = 0; // 새 출격의 순환 세트 횟수 초기화
            flowOrnamentReadyAt = 0f; // 새 출격의 순환 장식 대기 초기화
            flowOrnamentTriggers = 0; // 새 출격의 순환 장식 횟수 초기화
            shadowSetReadyAt = 0f; // 새 출격의 그림자 세트 대기 초기화
            shadowSetTriggers = 0; // 새 출격의 그림자 세트 횟수 초기화
            shadowManaReadyAt = 0f; // 새 출격의 그림자 마력석 대기 초기화
            shadowManaTriggers = 0; // 새 출격의 그림자 마력석 횟수 초기화
            shadowOrnamentReadyAt = 0f; // 새 출격의 그림자 장식 대기 초기화
            shadowOrnamentTriggers = 0; // 새 출격의 그림자 장식 횟수 초기화
            shadowOrnamentGrazes = 0; // 새 출격의 장식 스침 누적 초기화
            shadowCounterReadyAt = 0f; // 새 출격의 세트 반격탄 대기 초기화
            shadowCounterTriggers = 0; // 새 출격의 세트 반격탄 횟수 초기화
            shadowCounterGrazes = 0; // 새 출격의 세트 스침 누적 초기화
            shadowPartCounterReadyAt = 0f; // 새 출격의 개별 반격탄 대기 초기화
            shadowPartCounterTriggers = 0; // 새 출격의 개별 반격탄 횟수 초기화
            dashTriggers.Clear(); // 새 출격의 모든 개별 대시 발동 기록 제거
            equipment?.ResetBroomRun(); // 이전 대시의 한시 비행 속도 제거
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
            FlightAudio.Play(FlightSfx.MagicCard); // 실제 성공 소비에서만 카드 소리
            SpendMana(cost); // 카드 비용과 세트의 낮은 마력 진입
            ShowResources(); // 클릭 다음 프레임에 실제 자원 갱신
            return true; // 카드 순환 가능 여부
        }

        internal void ReduceSniperCooldown() // 리안 약점 사냥 2의 실제 Q 대기 감소
        {
            if (Time.time < nextWeakReduction || readyAt[0] <= Time.time) // 현재 쿨타임과 초당 1회 제한
            {
                return; // 가능 상태의 재사용 시간에는 변경 없음
            }
            readyAt[0] = Mathf.Max(Time.time, readyAt[0] - 0.2f); // 0초 아래로 내리지 않는 감소
            nextWeakReduction = Time.time + 1f; // 같은 프레임과 초당 중복 효과 제한
            hud.SetCooldown(0, readyAt[0] - Time.time); // Q의 실제 숫자에 즉시 반영
        }

        internal bool CanSpendCraftedMana() // 카드와 같은 조준·생존 조건
        {
            return Time.timeScale > 0f && pilot.CanFly && pilot.IsAimView; // 메뉴와 둘러보기에서 실전 제작 마법 차단
        }

        internal bool HasCraftedMana(float cost) // 소수점 마력을 포함한 시전 가능 여부
        {
            return mana >= cost; // 실제 보유량과 비용 비교
        }

        internal void RestoreCraftedMana(float amount) // 허용 적탄 흡수의 실제 마력 회복
        {
            mana = Mathf.Min(MaxMana, mana + Mathf.Max(0f, amount)); // 최대 마력 안에서만 회복
            ShowResources(); // 실제 전투 자원 표시 갱신
        }

        internal float ConsumeCraftedMaintenance(float amount) // 유지 마법의 연속 마력 소모
        {
            float spent = Mathf.Min(mana, Mathf.Max(0f, amount)); // 실제 보유량 안에서만 소비
            SpendMana(spent); // 소수점 마력 소비와 세트 효과
            ShowResources(); // 실전 자원 표시 갱신
            return spent; // 유지할 수 있었던 실제 소비량
        }

        internal void SpendCraftedMana(float cost) // 성공한 제작 마법의 실제 소비
        {
            FlightAudio.Play(FlightSfx.MagicCard); // 제작 조합을 실제로 발사한 소리
            SpendMana(cost); // 제작 마법 비용과 세트 효과
            ShowResources(); // 전투 게이지의 실제 마력 수치
        }

        private void SpendMana(float amount) // 정상 소비에서 낮은 마력 진입 감지
        {
            float before = mana; // 비용을 차감하기 전의 실제 마력
            mana -= amount; // 호출자가 승인한 카드·스킬·유지 비용
            if (before <= 30f || mana > 30f || equipment == null) // 새로 30 이하에 진입하지 않은 소비
            {
                return; // 같은 낮은 마력 상태에서 반복 발동 방지
            }
            if (equipment.SetPieces("flow") >= 8 && Time.time >= flowSetReadyAt && flowSetTriggers < 8) // 여덟 부위의 독립 발동 제한
            {
                mana = Mathf.Min(MaxMana, mana + 20f); // 실제 마력 상한을 넘지 않는 세트 회복
                flowSetReadyAt = Time.time + 20f; // 세트의 내부 쿨타임
                flowSetTriggers++; // 이번 출격의 최대 여덟 회 기록
            }
            if (equipment.HasSpecial("flow", FlightBroomSlot.Ornament, 3) && Time.time >= flowOrnamentReadyAt && flowOrnamentTriggers < 10) // 희귀 이상 순환 장식의 독립 발동 제한
            {
                mana = Mathf.Min(MaxMana, mana + 5f); // 같은 실제 경계에서 개별 장식 마력 회복
                flowOrnamentReadyAt = Time.time + 15f; // 장식의 내부 쿨타임
                flowOrnamentTriggers++; // 이번 출격의 최대 열 번 기록
            }
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
                SpendMana(cost); // 일반 스킬 비용과 낮은 마력 진입
            }
            if (effect == FlightMagicEffect.BreakthroughBlade && equipment != null) // 실제 대시가 있는 세라의 돌파 검기
            {
                cooldown = Mathf.Max(0f, cooldown - equipment.DashCooldownReduction()); // 기존 배율 이후 상위 두 고정 초 감소
            }
            readyAt[slot] = Time.time + startsIn + cooldown; // 재사용 시간 시작
            hud.SetDelayedCooldown(slot, cooldown, startsIn); // 슬롯 안 흰 숫자 갱신
            FlightAudio.Play(slot == 2 ? FlightSfx.Ultimate : FlightSfx.Skill); // 승인된 시전마다 일반·필살기 신호
            SkillActivated?.Invoke(slot); // 실제 마력을 쓴 스킬 완료 알림
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
                case FlightMagicEffect.IrisSpreadWave: // 이리스 Q의 실제 전방 원뿔
                    return weapon.Resonance != null && weapon.Resonance.CastWave(false); // 직접 파동만 적용
                case FlightMagicEffect.IrisSkyResonance: // 이리스 R의 직접 피해와 연쇄
                    return weapon.Resonance != null && weapon.Resonance.CastWave(true); // 첫 적중 이후 두 번 전파
                case FlightMagicEffect.DeployVeraTurret: // 베라의 실제 세계 좌표 포대
                    return weapon.Turret != null && weapon.Turret.Deploy(); // 한 포대의 배치 또는 교체
                case FlightMagicEffect.TurretCrossBeam: // 실제 포대가 살아 있는 상태의 광선
                    return weapon.Turret != null && weapon.Turret.CastBeam(); // 포대가 없으면 에너지와 재사용 보존
                case FlightMagicEffect.ShadowCounter: // 녹스의 표식 소비와 반격탄
                    return weapon.Counter != null && weapon.Counter.CastCounter(); // 표식 0일 때 자원 소비 방지
                case FlightMagicEffect.BoundaryReversal: // 주변 적탄을 지우고 마물 공격
                    return weapon.Counter != null && weapon.Counter.CastReversal(); // 충전된 에너지의 실제 3D 필살기
                case FlightMagicEffect.BreakthroughBlade: // 세라의 전방 검기와 고속 돌파
                    return weapon.Sword != null && weapon.Sword.Cast(false); // 돌파와 실제 범위 타격
                case FlightMagicEffect.SkyChainSlash: // 세라의 다섯 회 연참
                    return weapon.Sword != null && weapon.Sword.Cast(true); // 같은 방향으로 독립 참격 다섯 회
                case FlightMagicEffect.StarlightSeekers: // 루미의 여섯 발 유도탄
                    return weapon.CastLumiStars(false); // 단일 추적 또는 현재 분산 노드
                case FlightMagicEffect.ConstellationRoute: // 루미의 열두 발 필살기
                    return weapon.CastLumiStars(true); // 가까운 3D 적에 순환 배분
                case FlightMagicEffect.SingleHitGuard: // 리안의 공용 보호막 시험값
                    vitals.GrantSingleHitGuard(1f); // 첫 피해 또는 1초에 만료되는 보호막
                    return true; // 보호 발생 성공
                case FlightMagicEffect.Meteor: // 아리아의 유성 탄막
                    threatPool.ClearInView(Camera.main); // 화면 안 적탄만 소거
                    foreach (FlightEnemy enemy in FindObjectsByType<FlightEnemy>(FindObjectsSortMode.None)) // 씬의 살아 있는 일반 적
                    {
                        if (enemy.IsCombatTarget) enemy.ReceiveHit(80); // 안전 이탈·비살상 종료 대상 제외
                    }
                    return true; // 유성 탄막 발동
                default: // 정의되지 않은 마법
                    return false; // 불명확한 효과 사용 거절
            }
        }

        private void ShowResources() // 현재 소비한 자원을 전투 HUD에 연결
        {
            hud.SetManaCapacity(MaxMana); // 장착한 빗자루의 현재 게이지 분모
            hud.SetResources(Mana, Ultimate); // 마력과 필살기 에너지 별도 표시
        }
    }
}
