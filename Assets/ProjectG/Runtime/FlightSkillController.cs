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
        SingleHitGuard // 리안의 1회 피해 방어
    }

    [DefaultExecutionOrder(130)] // 자동탄과 카메라 갱신 뒤 발동
    public sealed class FlightSkillController : MonoBehaviour // 스킬·카드의 공통 마력 처리
    {
        [SerializeField] private FirstFlightController pilot; // 현재 비행 상태
        [SerializeField] private FlightPilotVitals vitals; // 체력·그레이즈·보호 효과
        [SerializeField] private FlightWeapon weapon; // 기본탄 프리팹과 조준 기준
        [SerializeField] private FlightThreatPool threatPool; // 화면 안 적탄 소거 대상
        [SerializeField] private FlightCombatHud hud; // 제작 게이지와 쿨타임 표시
        private readonly float[] readyAt = new float[3]; // Q·E·R 사용 가능 시각
        private float mana = 100f; // 이번 출격의 실제 마력
        private int ultimate; // 필살기 에너지
        private int recordedGrazes; // 마지막으로 자원에 반영한 스침 수

        public int Mana => Mathf.FloorToInt(mana); // 화면과 카드가 확인하는 현재 마력

        private void LateUpdate() // 자원 회복과 키 입력 확인
        {
            Keyboard keyboard = Keyboard.current; // 실제 입력 장치
            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame) // 시험 캐릭터 전환
            {
                weapon.TogglePilot(); // 기존 자원을 유지한 전투 설정 변경
                hud.SetSniper(weapon.IsRian); // 전용 아이콘과 패시브 전환
            }
            if (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame) // 출격 재시작 입력
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
            }
            if (pilot.CanFly) // 실패 전 자원과 스킬만 갱신
            {
                mana = Mathf.Min(100f, mana + Time.deltaTime * 6f); // 시험 초당 마력 회복
                ultimate = Mathf.Min(100, ultimate + (vitals.Grazes - recordedGrazes) * 10); // 그레이즈당 필살기 에너지
                recordedGrazes = vitals.Grazes; // 중복 에너지 지급 방지
                if (keyboard != null) // 실제 키 입력 확인
                {
                    if (keyboard.qKey.wasPressedThisFrame) // Q 고유기 입력
                    {
                        TrySkill(0, weapon.IsRian ? FlightMagicEffect.PiercingShot : FlightMagicEffect.RapidCycle, 25, weapon.IsRian ? 10f : 8f, false, weapon.IsRian ? 0f : 3f); // 캐릭터의 관통 사격 또는 집중 포화
                    }
                    if (keyboard.eKey.wasPressedThisFrame) // E 보호막 입력
                    {
                        TrySkill(1, weapon.IsRian ? FlightMagicEffect.SingleHitGuard : FlightMagicEffect.Ward, 30, weapon.IsRian ? 12f : 6f, false); // 리안의 1회 보호 또는 아리아의 시간 보호
                    }
                    if (keyboard.rKey.wasPressedThisFrame) // R 필살기 입력
                    {
                        TrySkill(2, weapon.IsRian ? FlightMagicEffect.SniperBeam : FlightMagicEffect.Meteor, 100, 12f, true); // 천공 관측선 또는 유성 탄막
                    }
                }
            }
            ShowResources(); // 현재 마력과 에너지 표시
        }

        public bool TryUseCard(FlightMagicEffect effect, int cost) // 현재 카드의 성공한 실행에만 비용 차감
        {
            if (!pilot.CanFly || !pilot.IsAimView || mana < cost || !Activate(effect)) // 실제 사용 자원과 효과 확인
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
            hud.SetResources(Mana, ultimate); // 마력과 필살기 에너지 별도 표시
        }
    }
}
