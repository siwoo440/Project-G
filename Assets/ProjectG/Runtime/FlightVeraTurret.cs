using System.Collections.Generic; // 포대 광선의 중복 적중 방지
using UnityEngine; // 설치 지점, 고도와 실제 목표

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 조종사당 하나의 배치 상태
    public sealed class FlightVeraTurret : MonoBehaviour // 한 곳에 설치하는 부유 마도 포대
    {
        private const float Range = 30f; // 포대가 표적을 찾고 공격할 3D 범위 시험값
        private const float SupportRadius = 2f; // 기본·집중 공격에 적용하는 설치 반경
        [SerializeField] private FirstFlightController pilot; // 실제 포대를 설치한 비행체
        [SerializeField] private FlightWeapon weapon; // 실제 포대 발사체와 지형 판정
        [SerializeField] private FlightUpgradeChoices upgrades; // 8초·1초의 성장 변경
        [SerializeField] private Transform site; // 월드에 고정할 직접 편집 설치 지점
        [SerializeField] private Transform hover; // 황동 본체와 회전자의 부유 모션
        [SerializeField] private Transform rotor; // 몸체 위의 실제 무다리 회전자
        [SerializeField] private Transform turretHead; // 추적 대상으로 향하는 포신
        [SerializeField] private Transform muzzle; // 3D 포대 발사와 광선의 정확한 시작점
        [SerializeField] private LineRenderer beam; // 설치 지점의 편집 가능한 교차 광선
        [SerializeField] private LayerMask terrain = 1 << 8; // 포대 표적을 가리는 월드 지형
        [SerializeField] private LayerMask beamLayers = (1 << 8) | (1 << 9); // 광선의 지형과 마물 충돌
        private readonly HashSet<Component> pierced = new HashSet<Component>(); // 여러 콜라이더를 가진 같은 적 제외
        private float expiresAt; // 현재 설치의 사용 종료 시각
        private float nextShot; // 실제 포대탄을 발사할 다음 시각
        private float beamUntil; // 교차 광선이 화면에 남는 시각

        public bool IsDeployed => hover.gameObject.activeSelf && Time.time < expiresAt; // 만료 시간과 표시 상태의 실제 포대 확인
        public int SecondsLeft => IsDeployed ? Mathf.CeilToInt(expiresAt - Time.time) : 0; // HUD의 현재 남은 설치 수명
        public float DamageMultiplier => IsDeployed && (site.position - pilot.transform.position).sqrMagnitude <= SupportRadius * SupportRadius ? 1.2f : 1f; // 베라 자동탄의 설치 근처 보너스

        internal bool Deploy() // 현재 월드 지점에 한 개를 설치하거나 교체
        {
            site.SetPositionAndRotation(pilot.transform.position, Quaternion.identity); // 플레이어 방향에서 떨어져 원래 월드 위치에 고정
            hover.localPosition = Vector3.zero; // 새 부유각의 기준 높이
            turretHead.rotation = pilot.AimRotation; // 첫 표적 전 현재 조준 방향
            expiresAt = Time.time + (upgrades != null ? upgrades.TurretSeconds : 8f); // 새 설치의 실제 수명
            nextShot = Time.time + Interval; // 설치 뒤 한 간격이 지난 첫 발사
            beam.enabled = false; // 이전 설치의 광선 끄기
            hover.gameObject.SetActive(true); // 투명 대기 중인 실제 씬 포대 활성
            return true; // 발동 가능 마력 검사에 전달할 정상 배치
        }

        internal void ResetRun() // 되돌아가거나 다시 출격할 때 회수
        {
            expiresAt = 0f; // 임시 배치와 공격 보너스 종료
            hover.gameObject.SetActive(false); // 기존 월드 위치의 포대 숨김
            beam.enabled = false; // 남은 광선 시각 효과 제거
        }

        internal bool CastBeam() // 포대가 남아 있을 때 설치 위치에서만 광선 시전
        {
            if (!IsDeployed) // 설치하지 않았거나 만료된 3D 포대
            {
                return false; // 에너지와 재사용 시간을 그대로 유지
            }
            Transform enemy = FindTarget(); // 현재 포대에서 가장 가까운 유효 목표
            Vector3 direction = enemy != null ? (enemy.position - muzzle.position).normalized : turretHead.forward; // 포대 위치의 실제 광선 방향
            Vector3 start = muzzle.position; // 플레이어가 멀어져도 유지되는 설치 위치
            pierced.Clear(); // 이번 광선에서 접촉한 마물만 기록
            FlightSpellBolt.ApplyPiercingPath(start, 0.45f, direction, Range, beamLayers, pierced, 200, 0f, int.MaxValue, null, out float distance); // 200 피해와 지형 앞 관통
            beam.SetPosition(0, start); // 하이러키 포구에서 발광 시작
            beam.SetPosition(1, start + direction * distance); // 실제 벽·표적까지의 광선 길이
            beam.enabled = true; // 순간적으로 보이는 설치 발사
            beamUntil = Time.time + 0.3f; // 넓은 광선 외형의 수명
            return true; // 실제 사용한 에너지 차감
        }

        private float Interval => upgrades != null ? upgrades.TurretInterval : 1f; // 실제 카드로 바뀌는 포대 공격 간격

        private void Update() // 설치물의 부유, 프로펠러, 자동 사격을 같은 월드 지점에서 갱신
        {
            if (Time.timeScale <= 0f) // 강화 선택이나 일시정지의 포대 시간 보존
            {
                return; // 시각 및 피해 갱신 중지
            }
            if (!IsDeployed || !pilot.CanFly) // 만료 또는 종료한 출격
            {
                if (hover.gameObject.activeSelf || beam.enabled) // 기존 설치만 회수
                {
                    ResetRun(); // 마물과 별개인 배치 종료
                }
                return; // 이후 발사 없음
            }
            if (beam.enabled && Time.time >= beamUntil) // 이미 때린 광선의 화면 종료
            {
                beam.enabled = false; // 기존 시각 효과만 종료
            }
            hover.localPosition = Vector3.up * (0.48f + 0.14f * Mathf.Sin(Time.time * 5f)); // 실제 다리 없는 공중 부유
            rotor.Rotate(0f, 1350f * Time.deltaTime, 0f); // 본체 윗부분의 독립 프로펠러 회전
            if (Time.time < nextShot) // 설치된 포대의 독립 사격 간격
            {
                return; // 새 피해 없이 부유 모션만 유지
            }
            Transform enemy = FindTarget(); // 주변 고도를 포함한 포대의 적 탐색
            if (enemy != null) // 지형에 막히지 않은 활성 표적만 사용
            {
                turretHead.rotation = Quaternion.LookRotation(enemy.position - turretHead.position); // 포신 방향을 실제 표적 히트박스로
                weapon.CastAssist(muzzle.position, enemy.position, 10); // 매 간격 한 발의 실제 10 피해
                nextShot = Time.time + Interval; // 같은 프레임에서 여러 탄을 쏘지 않는 상한
            }
            else // 주변에 공격할 대상이 없는 위치
            {
                nextShot = Time.time + 0.15f; // 무의미한 매 프레임 전장 검색 제한
            }
        }

        private Transform FindTarget() // 벽과 고도를 고려한 포대 중심의 실제 표적
        {
            float nearest = Range * Range; // 반경 밖 마물을 선택하지 않음
            Transform selected = null; // 마물 없는 위치의 정상 상태
            foreach (FlightEnemy enemy in FindObjectsByType<FlightEnemy>(FindObjectsSortMode.None)) // 현재 살아 있는 실전 표적
            {
                if (enemy.IsCombatTarget) // 퇴장한 적은 제외
                {
                    Consider(enemy.transform); // 현재의 최단 거리 후보
                }
            }
            foreach (FlightPracticeTarget target in FindObjectsByType<FlightPracticeTarget>(FindObjectsSortMode.None)) // 안내와 연습의 실제 표적
            {
                Consider(target.transform); // 동일한 지형과 사거리 적용
            }
            return selected; // 포대가 회전과 탄환에 반영할 목표

            void Consider(Transform target) // 실제 마물 또는 연습 표적 위치의 거리
            {
                float distance = (target.position - muzzle.position).sqrMagnitude; // 포구 중심의 좌우·높이·깊이
                if (distance > 0.001f && distance < nearest && !Physics.Linecast(muzzle.position, target.position, terrain, QueryTriggerInteraction.Ignore)) // 가까운 표적을 지형이 가리지 않는지 확인
                {
                    nearest = distance; // 다음 적과 비교할 최단 거리
                    selected = target; // 현재 가장 가까운 유효 표적
                }
            }
        }
    }
}
