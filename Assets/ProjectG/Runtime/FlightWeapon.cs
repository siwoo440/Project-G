using System.Collections.Generic; // 광선에서 적 중복 판정 방지
using UnityEngine; // 자동 조준과 마법 발사
using UnityEngine.UI; // 제작 조준점의 표시 상태

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(100)] // 추적 카메라 갱신 이후 조준
    [DisallowMultipleComponent] // 비행체당 발사 컴포넌트 한 개
    public sealed class FlightWeapon : MonoBehaviour // 화면 중심 자동 사격
    {
        [SerializeField] private FirstFlightController pilot; // 현재 비행·집중 입력
        [SerializeField] private Camera flightCamera; // 화면 조준 카메라
        [SerializeField] private Transform muzzle; // 씬의 빗자루 발사 위치
        [SerializeField] private Graphic reticle; // 제작 조준점 이미지
        [SerializeField] private FlightSpellBolt standardBolt; // 기본 마법탄 프리팹
        [SerializeField] private FlightSpellBolt focusedBolt; // 집중 마법탄 프리팹
        [SerializeField] private LayerMask aimLayers = (1 << 8) | (1 << 9); // 비행 지형과 조준 표적
        [SerializeField, Min(1f)] private float aimRange = 85f; // 화면 중심의 최대 조준 거리
        [SerializeField] private LineRenderer sniperBeam; // 씬에서 조정하는 천공 관측선
        [SerializeField] private FlightUpgradeChoices upgrades; // 이번 출격의 자동탄 강화
        private static readonly Color WeakPointColor = new Color(1f, 0.76f, 0.25f); // 약점 조준점의 금색
        private const float BeamRadius = 0.55f; // 광선 판정 반경
        private FirstFlightCamera chase; // 조준과 둘러보기의 카메라 좌표
        private FlightRapidFire rapid; // 아리아 자동탄의 누적 피해
        private Color normalReticleColor; // 기존 조준점 색
        private float beamUntil; // 광선 표시 종료 시각
        private float rapidUntil; // Q 효과의 종료 시각
        private float nextShot; // 다음 자동 연사 가능 시각
        private float WeakRatio => IsRian ? (upgrades != null ? upgrades.WeakRatio : 0.5f) : 0f; // 실제 캐릭터의 약점 배율

        public float FireIntervalScale => Time.time < rapidUntil ? 0.7f : 1f; // 집중 포화의 30% 단축
        public bool IsRian { get; private set; } // 현재 시험 조종사

        public void TogglePilot() // 아리아와 리안의 전투 설정 전환
        {
            IsRian = !IsRian; // 다음 시험 캐릭터 선택
            ResetCadence(); // 이전 속사 효과와 광선 표시 제거
        }

        public void BeginRapidFire() // 아리아 Q 집중 포화
        {
            rapidUntil = Time.time + 3f; // 3초간 자동탄 가속
            nextShot = Time.time; // 다음 발사 시작 시각
        }

        public void ResetCadence() // 재출격 발사 설정 복구
        {
            rapidUntil = 0f; // 이전 집중 포화 제거
            nextShot = Time.time; // 새 첫 발 시각
            rapid?.ResetRun(); // 아리아 적중 보너스 초기화
            if (sniperBeam != null) // 관측선이 배치된 시험 씬
            {
                sniperBeam.enabled = false; // 이전 광선의 시각 효과 중단
            }
        }
        private void Awake() // 실제 추적 카메라 연결
        {
            chase = flightCamera.GetComponent<FirstFlightCamera>(); // 독립 둘러보기의 기준 조준
            TryGetComponent(out rapid); // 캐릭터의 선택적 자동탄 강화
            normalReticleColor = reticle.color; // 편집한 기본 조준점 색 설정
        }

        private void LateUpdate() // 조준 방향으로 자동 사격
        {
            if (Time.timeScale == 0f) // 카드 선택 동안 발사 금지
            {
                return; // 조준과 자동탄 모두 정지
            }
            reticle.enabled = pilot.CanFly && pilot.HasPointerControl && pilot.IsAimView; // 실제 조준점 표시 여부
            if (sniperBeam != null && sniperBeam.enabled && Time.time >= beamUntil) // 짧은 광선 시각 효과 종료
            {
                sniperBeam.enabled = false; // 프레임에 그리는 광선 제거
            }
            bool trackWeakPoint = IsRian && pilot.IsFocusHeld && reticle.enabled; // 실제로 보이는 집중 조준점
            bool canShoot = pilot.CanFly && Time.time >= nextShot; // 자동 발사 가능 시각
            if (!canShoot && !trackWeakPoint) // 명중 검사나 조준 안내가 필요 없는 프레임
            {
                reticle.color = normalReticleColor; // 집중 안내 종료 후 기본색 복구
                return; // 불필요한 물리 검사 생략
            }
            Ray aim = AttackRay(); // 화면 중심의 공격 방향
            Vector3 point = GetAimPoint(aim, out bool weakPoint); // 조준 지점과 노출 약점
            reticle.color = trackWeakPoint && weakPoint ? WeakPointColor : normalReticleColor; // 집중 중 약점 위치 안내
            if (!canShoot) // 아직 발사 대기 중인 조준 프레임
            {
                return; // 현재 프레임 발사 생략
            }
            if (TryDirection(aim, point, out Vector3 travel)) // 총구 앞에 있는 조준 위치
            {
                FireSpread(travel, pilot.IsFocusHeld, 1, 0f, true); // 기본 또는 집중탄 자동 발사
            }
            float interval = pilot.IsFocusHeld ? 0.27f : 0.18f; // 아리아의 기본 연사 간격
            if (IsRian) // 저격형은 샷 사이 조준 시간이 더 긴 시험 설정
            {
                interval = pilot.IsFocusHeld ? 0.48f : 0.30f; // 리안의 집중·기본 발사 간격
            }
            nextShot = Time.time + interval * FireIntervalScale * (upgrades != null ? upgrades.CadenceScale : 1f); // 전투 카드의 발사 간격 보너스
        }

        public bool CastSpread(bool focused, int count, float spread, bool isAutomatic = false) // 스킬·카드의 마법탄 배열
        {
            Ray aim = AttackRay(); // 카드·스킬 조준 시점
            if (!TryDirection(aim, GetAimPoint(aim, out _), out Vector3 travel)) // 총구 앞쪽 조준 방향 확인
            {
                return false; // 벽 앞에서 뒤쪽으로 발사 방지
            }

            FireSpread(travel, focused, count, spread, isAutomatic); // 실제 발사체 생성
            return true; // 성공한 발사에만 자원 비용 적용
        }

        public bool CastPiercingShot() // 리안 Q 관통 사격
        {
            Ray aim = AttackRay(); // 현재 화면 중심
            if (!TryDirection(aim, GetAimPoint(aim, out _), out Vector3 travel)) // 발사가 가능한 위치
            {
                return false; // 실패 때 자원 유지
            }

            FlightSpellBolt bolt = Instantiate(focusedBolt, muzzle.position, Quaternion.LookRotation(travel)); // 빠른 집중탄 외형 재사용
            bolt.Begin(aimLayers, aimRange, null, WeakRatio, 100); // 피해 100의 한 발 관통탄
            return true; // 관통 사격 성공
        }

        public void CastAssist(Vector3 origin, Vector3 target, int damage) // 공격 드론의 실제 보조 탄환
        {
            Vector3 route = target - origin; // 드론이 겨누는 적 방향
            if (route.sqrMagnitude <= 0.001f) // 너무 가까워 방향을 정할 수 없는 경우
            {
                return; // 무효 탄 생성 방지
            }
            FlightSpellBolt bolt = Instantiate(standardBolt, origin, Quaternion.LookRotation(route)); // 실제 이동하는 기존 마법탄
            bolt.Begin(aimLayers, 35f, null, 0f, assistDamage: damage); // 보조 슬롯의 독립 피해
        }

        public bool CastSniperBeam(FlightThreatPool pool) // 리안 R 천공 관측선
        {
            Ray aim = AttackRay(); // 실제 시점이 향하는 경로
            if (!TryDirection(aim, GetAimPoint(aim, out _), out Vector3 travel)) // 총구 뒤로 조준하는 경우 제외
            {
                return false; // 자원과 쿨타임 유지
            }

            Vector3 start = muzzle.position; // 광선 시작점
            Vector3 direction = travel.normalized; // 정규화한 조준 방향
            var hitTargets = new HashSet<Component>(); // 한 번만 피해를 받을 표적 집합
            FlightSpellBolt.ApplyPiercingPath(start, BeamRadius, direction, aimRange, aimLayers, hitTargets, 220, WeakRatio, int.MaxValue, null, out float length); // 표적마다 피해 220과 지형 차단
            pool.ClearAlongBeam(start, direction, length, BeamRadius); // 벽 앞 광선에 닿은 적탄 회수
            if (sniperBeam != null) // 씬의 편집 가능한 광선 참조
            {
                sniperBeam.SetPosition(0, start); // 광선의 총구 위치
                sniperBeam.SetPosition(1, start + direction * length); // 충돌하거나 최대 사거리까지 표시
                sniperBeam.enabled = true; // 짧은 광선 화면 표시
                beamUntil = Time.time + 0.24f; // 시각 효과만 남는 기간
            }
            return true; // 광선 발동 성공
        }

        private Ray AttackRay() // 둘러보기와 분리된 실제 조준선
        {
            return chase != null ? chase.GetAttackRay() : flightCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 이동 시점의 중앙
        }

        private Vector3 GetAimPoint(Ray aim, out bool weakPoint) // 첫 조준 접촉 또는 최대 거리
        {
            bool hit = Physics.Raycast(aim, out RaycastHit surface, aimRange, aimLayers, QueryTriggerInteraction.Ignore); // 실제 조준선과 장애물 검사
            weakPoint = hit && surface.collider.TryGetComponent<FlightWeakPoint>(out _); // 화면 중앙이 가리키는 약점
            return hit ? surface.point : aim.GetPoint(aimRange); // 마법탄이 향할 월드 위치
        }

        private bool TryDirection(Ray aim, Vector3 point, out Vector3 travel) // 발사 위치와 카메라 시차 보정
        {
            travel = point - muzzle.position; // 총구에서 조준점으로의 이동 벡터
            return Vector3.Dot(travel, aim.direction) > 0.001f; // 벽 앞에서 역발사 차단
        }

        private void FireSpread(Vector3 travel, bool focused, int count, float spread, bool isAutomatic) // 마법탄 배열 생성
        {
            bool grown = isAutomatic && upgrades != null; // 강화는 기본·집중 자동탄에만 적용
            float damage = grown ? upgrades.DamageScale(focused) : 1f; // 상대 피해
            float width = grown ? upgrades.WidthScale(focused) : 1f; // 기본 또는 집중탄의 충돌 폭
            int pierce = grown ? upgrades.AutoPierce : 0; // 추가 관통 가능한 대상
            Quaternion basis = Quaternion.LookRotation(travel); // 해당 발사의 중심 방향
            for (int index = 0; index < count; index++) // 실제 마법탄 생성
            {
                float yaw = count == 1 ? 0f : Mathf.Lerp(-spread, spread, (float)index / (count - 1)); // 탄막 전체의 좌우 각도
                float pitch = count >= 5 ? (index % 2 == 0 ? -3f : 3f) : 0f; // 넓은 마법탄의 입체 고도
                Quaternion direction = basis * Quaternion.Euler(pitch, yaw, 0f); // 화면 중앙 방향에서 분산
                FlightSpellBolt bolt = Instantiate(focused ? focusedBolt : standardBolt, muzzle.position, direction); // 기존 마법탄 프리팹 재사용
                bolt.Begin(aimLayers, aimRange, isAutomatic && !IsRian ? rapid : null, WeakRatio, 0, damage, width, pierce); // 발사 때 확정한 피해와 폭
            }
        }
    }
}
