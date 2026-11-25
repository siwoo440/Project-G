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
        private FlightBroomEquipment broom; // 캐릭터별 빗자루 조합
        private FlightSeraCombat sword; // 세라의 검기 모션과 근거리 피해 판정
        private FlightSkillController skills; // 약점 적중의 고유기 재사용 조정
        private System.Action<FlightEnemy, bool> ariaHit; // 기본·집중 사격을 관찰할 아리아의 발사 콜백
        private System.Action<FlightEnemy, bool> rianHit; // 실제 약점의 리안 발사 콜백
        private bool measureTask; // 첫 웨이브 이후 유효한 실제 적중 인정
        private int taskHits; // 이번 출격의 첫 과제 적중 수
        private FlightEnemy chaseTarget; // 추격 후속에서 연속으로 맞힌 마물
        private int chaseHits; // 현재 같은 마물의 유효 연속 적중
        private float nextChase; // 초당 한 발 제한
        private float nextSpread; // 집중 포화 뒤 처음 후속탄 시각
        private Color normalReticleColor; // 기존 조준점 색
        private float beamUntil; // 광선 표시 종료 시각
        private float rapidUntil; // Q 효과의 종료 시각
        private readonly List<FlightEnemy> lumiTargets = new List<FlightEnemy>(); // 현재 조준 방향의 입체 유도 대상
        private FlightEnemy lumiTarget; // 유지한 표적과 회전 속도 보너스의 기준
        private float trackingSince; // 같은 표적의 연속 추적 시작 시각
        private float nextTargetScan; // 표적 탐색 갱신 시각
        private float nextShot; // 다음 자동 연사 가능 시각
        private float WeakRatio => IsRian ? (upgrades != null ? upgrades.WeakRatio : 0.5f) : 0f; // 실제 캐릭터의 약점 배율

        internal FlightSeraCombat Sword => sword; // 고유기와 필살기의 실제 검기 구성
        public float FireIntervalScale => Time.time < rapidUntil ? 0.7f : 1f; // 집중 포화의 30% 단축
        public FlightBroomPilot ActivePilot { get; private set; } // 출격에서 실제 조작하는 캐릭터
        public bool IsRian => ActivePilot == FlightBroomPilot.Rian; // 저격형 프로필 확인
        private bool IsAria => ActivePilot == FlightBroomPilot.Aria; // 속사형 프로필 확인
        internal int LumiTrackingBonus => lumiTarget != null && lumiTarget.isActiveAndEnabled && lumiTarget.Health > 0 ? Mathf.Clamp(Mathf.FloorToInt(Time.time - trackingSince), 0, 3) * 15 : 0; // 동일 표적 1초마다 15%, 최대 45%
        internal bool FirstTaskReady => measureTask && (IsAria ? taskHits >= 50 : IsRian && taskHits >= 10); // 두 조종사의 확정 첫 출격 과제

        internal void StartFirstTask() // 실제 첫 웨이브 시작의 과제 초기값
        {
            taskHits = 0; // 출격 사이에 누적하지 않는 적중 수
            measureTask = true; // 진행하는 정상 정식 출격
        }

        internal void StopFirstTask() // 다른 출격으로 넘어가는 시점
        {
            measureTask = false; // 준비와 연습 동안에는 집계 없음
        }

        public void TogglePilot() // 아리아와 리안의 전투 설정 전환
        {
            if (IsAria || IsRian) // 새 출격 조종사에게 기존 시험 전환 미적용
            {
                SelectPilot(IsRian ? FlightBroomPilot.Aria : FlightBroomPilot.Rian); // 기존 두 조종사의 빠른 시험 전환
            }
        }

        internal void SelectPilot(FlightBroomPilot selected) // 승인된 조종사의 전투 상태 설정
        {
            ActivePilot = selected; // 전투와 정산에 사용되는 실제 조종사
            broom?.SelectPilot(selected); // 캐릭터에 맞는 부품 조합 복원
            ResetCadence(); // 이전 효과와 표적 제거
        }

        public void BeginRapidFire() // 아리아 Q 집중 포화
        {
            rapidUntil = Time.time + 3f; // 3초간 자동탄 가속
            nextShot = Time.time; // 다음 발사 시작 시각
            nextSpread = Time.time + 0.5f; // 확산 후속탄의 첫 간격
        }

        public void ResetCadence() // 재출격 발사 설정 복구
        {
            rapidUntil = 0f; // 이전 집중 포화 제거
            nextShot = Time.time; // 새 첫 발 시각
            rapid?.ResetRun(); // 아리아 적중 보너스 초기화
            sword?.ResetRun(); // 이전 출격에 예약했던 연참과 검기 화면 제거
            lumiTarget = null; // 출격과 전환 뒤 추적 누적 제거
            lumiTargets.Clear(); // 이전 웨이브의 후보 제거
            trackingSince = Time.time; // 새 표적의 기본 누적 시각
            nextTargetScan = 0f; // 다음 프레임부터 정상 탐색
            chaseTarget = null; // 이전 출격의 추격 대상 제거
            chaseHits = 0; // 같은 대상의 연속 적중 초기화
            nextChase = 0f; // 이전 추격탄 제한 제거
            nextSpread = float.PositiveInfinity; // 새 집중 포화 전에 확산 없음
            if (sniperBeam != null) // 관측선이 배치된 시험 씬
            {
                sniperBeam.enabled = false; // 이전 광선의 시각 효과 중단
            }
        }
        private void Awake() // 실제 추적 카메라 연결
        {
            chase = flightCamera.GetComponent<FirstFlightCamera>(); // 독립 둘러보기의 기준 조준
            TryGetComponent(out rapid); // 캐릭터의 선택적 자동탄 강화
            TryGetComponent(out broom); // 실전 씬의 장착 구성
            TryGetComponent(out sword); // 씬에 배치한 세라의 검기와 돌파
            TryGetComponent(out skills); // 현재 조종사의 시전과 재사용 시간
            ariaHit = OnAriaHit; // 아리아 기본·집중의 실제 적중 처리
            rianHit = OnRianHit; // 리안의 실제 약점 사격 처리
            nextSpread = float.PositiveInfinity; // 고유기를 쓰기 전에는 추가탄 없음
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
            if (ActivePilot == FlightBroomPilot.Lumi && pilot.CanFly && Time.time >= nextTargetScan) // 제어 가능한 루미의 정기 표적 확인
            {
                RefreshLumiTargets(); // 같은 대상의 유도력 유지 확인
            }
            if (IsAria && pilot.CanFly && upgrades != null && upgrades.SpreadSecond && nextSpread <= rapidUntil && Time.time >= nextSpread) // 집중 포화의 0.5초 간격 후속탄
            {
                FireSpreadFollowup(); // 실제 근처 대상의 독립 추가탄
                nextSpread += 0.5f; // 같은 집중 포화 안의 다음 발사만 예약
            }
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

        internal bool CastLumiStars(bool ultimate) // 높이가 다른 적에게 유도 별빛 발사
        {
            RefreshLumiTargets(); // 입력 시점의 유효 표적 재확인
            if (lumiTargets.Count == 0) // 쫓을 적이 없는 상태
            {
                return false; // 탄환과 자원 소모 생략
            }
            int count = ultimate ? 12 : 6; // 별무리 항로와 별빛 추적탄 발사 수
            int damage = ultimate ? 18 : 12; // 기획의 발당 직접 피해
            bool spread = ultimate || (upgrades != null && upgrades.LumiSpread); // 필살기와 분산 1의 다중 표적
            float arc = upgrades != null ? upgrades.LumiTurnScale : 1f; // 집중 추적 1의 회전 배율
            Quaternion aim = Quaternion.LookRotation(AttackRay().direction); // 카메라에 맞춘 초기 발사 축
            for (int index = 0; index < count; index++) // 모든 별빛의 독립 추적 설정
            {
                FlightEnemy target = lumiTargets[spread ? index % lumiTargets.Count : 0]; // 가까운 유효 마물부터 순환 배정
                float bonus = target == lumiTarget ? LumiTrackingBonus * 0.01f : 0f; // 유지한 대상만 받는 회전 보너스
                float angle = index * 360f / count * Mathf.Deg2Rad; // 상하좌우로 펼치는 발사 위상
                Quaternion heading = aim * Quaternion.Euler(Mathf.Sin(angle) * 8f, Mathf.Cos(angle) * 10f, 0f); // 입체 부채꼴의 시작 방향
                FlightSpellBolt bolt = Instantiate(standardBolt, muzzle.position, heading); // 지형 충돌과 수명이 있는 실제 마법탄
                bolt.Begin(aimLayers, aimRange, null, 0f, assistDamage: damage); // 조종사별 기본 피해 적용
                bolt.Guide(target, Mathf.Deg2Rad * 220f * arc * (1f + bonus)); // 시험 기본 회전량과 표적 추적 강화
            }
            return true; // 자원과 재사용 시간을 적용할 정상 발사
        }

        private void RefreshLumiTargets() // 사거리와 조준 반구에 있는 살아 있는 마물 검색
        {
            nextTargetScan = Time.time + 0.2f; // 일반 프레임의 반복 탐색량 제한
            Vector3 origin = muzzle.position; // 거리와 고도를 재는 실제 총구
            Vector3 forward = AttackRay().direction; // 둘러보기와 구분되는 발사 방향
            lumiTargets.Clear(); // 움직이는 적의 새 후보 목록
            foreach (FlightEnemy enemy in FindObjectsByType<FlightEnemy>(FindObjectsSortMode.None)) // 현재 씬의 활성 마물
            {
                Vector3 offset = enemy.transform.position - origin; // 세 축의 상대 위치
                float distance = offset.magnitude; // 실제 입체 거리
                if (enemy.isActiveAndEnabled && enemy.Health > 0 && distance <= aimRange && Vector3.Dot(forward, offset) > distance * 0.3f) // 사거리와 조준 방향 확인
                {
                    lumiTargets.Add(enemy); // 다중 유도에서 재사용할 후보
                }
            }
            lumiTargets.Sort((a, b) => (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude)); // 가까운 적부터 우선 배분
            if (lumiTargets.Count == 0 || !lumiTargets.Contains(lumiTarget)) // 적이 죽거나 사거리·조준 방향을 벗어난 경우
            {
                lumiTarget = lumiTargets.Count > 0 ? lumiTargets[0] : null; // 다음 유효 표적 또는 잠금 해제
                trackingSince = Time.time; // 새 표적의 회전 증가를 0에서 시작
            }
            else if (lumiTargets[0] != lumiTarget) // 기존 적을 계속 조준하는 동안 유지
            {
                lumiTargets.Remove(lumiTarget); // 가까이 온 다른 대상으로 자동 교체 방지
                lumiTargets.Insert(0, lumiTarget); // 단일 추적탄의 실제 우선 표적
            }
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
            bolt.Observe(rianHit); // 약점에 실제 적용된 리안 피해
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
            FlightSpellBolt.ApplyPiercingPath(start, BeamRadius, direction, aimRange, aimLayers, hitTargets, 220, WeakRatio, int.MaxValue, null, out float length, rianHit); // 실제 약점과 피격 대상을 한 번씩 확인
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
                bolt.Begin(aimLayers, aimRange, isAutomatic && IsAria ? rapid : null, WeakRatio, 0, damage, width, pierce); // 발사 때 확정한 피해와 폭
                if (ActivePilot == FlightBroomPilot.Sera) // 세라가 사용한 기본·집중·공용탄
                {
                    bolt.SetMeleeSource(sword); // 실제 피격 위치에서 근거리 결의 판정
                }
                if (isAutomatic && (IsAria || IsRian)) // 두 캐릭터의 기본·집중 사격만 후속 적중을 관찰
                {
                    bolt.Observe(IsRian ? rianHit : ariaHit); // 시작 시점 조종사의 직접 공격 확인
                }
            }
        }

        private void OnAriaHit(FlightEnemy enemy, bool _) // 실제 기본·집중 사격으로 줄어든 적 체력
        {
            if (!IsAria) // 전환 전 남아 있는 서로 다른 조종사의 탄
            {
                return; // 다른 조종사 과제나 후속 카드에 반영하지 않음
            }
            if (measureTask && taskHits < 50) // 정식 출격의 최초 과제 범위
            {
                taskHits++; // 관통탄도 적 하나마다 한 번
            }
            if (upgrades == null || !upgrades.ChaseSecond || rapid == null || rapid.BonusPercent < 30f) // 선행·후속 카드와 누적 조건
            {
                chaseHits = 0; // 조건을 잃으면 같은 대상의 연속 조건 초기화
                return; // 해당 적중의 추격 효과 없음
            }
            chaseHits = chaseTarget == enemy ? Mathf.Min(5, chaseHits + 1) : 1; // 같은 적만 최대 다섯 회까지 집계
            chaseTarget = enemy; // 실제 명중한 현재 대상
            if (chaseHits >= 5 && enemy.Health > 0 && Time.time >= nextChase) // 생존한 대상에 초당 한 번
            {
                CastAssist(muzzle.position, enemy.transform.position, 5); // 누적과 과제 관찰을 붙이지 않은 별도 추가탄
                chaseHits = 0; // 같은 대상이라도 다시 다섯 회 필요
                nextChase = Time.time + 1f; // 다음 추격탄의 최소 간격
            }
        }

        private void OnRianHit(FlightEnemy _, bool weak) // 리안이 실제로 적의 약점 체력을 깎은 경우
        {
            if (!IsRian || !weak) // 몸통·다른 캐릭터의 공격 제외
            {
                return; // 노출 약점이 아닌 판정에는 보상 없음
            }
            if (measureTask && taskHits < 10) // 현재 한 출격을 확인하는 첫 과제
            {
                taskHits++; // 해당 표적의 실제 약점 적중
            }
            if (upgrades != null && upgrades.WeakSecond) // 같은 가지의 두 번째 카드가 선택된 경우
            {
                skills?.ReduceSniperCooldown(); // 실제 재사용 시간에 초당 한 번 반영
            }
        }

        private void FireSpreadFollowup() // 집중 포화 중 가까운 실제 마물로 후속탄 하나
        {
            FlightEnemy nearest = null; // 거리 안의 생존한 표적
            float best = 35f * 35f; // 보조탄 실제 최대 사거리
            foreach (FlightEnemy enemy in FindObjectsByType<FlightEnemy>(FindObjectsSortMode.None)) // 이번 프레임의 마물만 확인
            {
                float distance = (enemy.transform.position - muzzle.position).sqrMagnitude; // 현재 총구에서의 실제 거리
                if (enemy.Health > 0 && distance < best) // 아직 피해를 줄 수 있는 가장 가까운 적
                {
                    nearest = enemy; // 현재 가장 가까운 후속탄 표적
                    best = distance; // 더 가까운 적이 있으면 교체
                }
            }
            if (nearest != null) // 범위 안의 실제 대상이 있을 때만 추가
            {
                CastAssist(muzzle.position, nearest.transform.position, 5); // 기존 독립 보조탄을 쓰는 피해 5
            }
        }
    }
}
