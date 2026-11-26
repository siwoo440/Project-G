using System.Collections.Generic; // 활성 적탄과 회수 목록
using UnityEngine; // 씬에서 지정하는 풀 크기와 실제 위치

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightStoryThreatShot
    {
        public Vector3 Position, Direction;
        public FlightStoryShotPlan Plan;
        public FlightStoryCombatant Owner;
        public bool CanCancel = true, CanAbsorb;
    }

    [DefaultExecutionOrder(110)] // 기체 이동과 카메라 이후에 탄막 판정
    [DisallowMultipleComponent] // 풀 오브젝트 한 곳에서 적탄 관리
    public sealed class FlightThreatPool : MonoBehaviour // 활성 탄막의 재사용과 일괄 갱신
    {
        private readonly struct CancelRequest // 적탄 순회가 끝난 뒤 처리할 보호막 소거
        {
            public readonly Vector3 Origin; // 파괴 시점의 실제 기체 위치
            public readonly float Radius; // 소거할 3D 반경
            public readonly int Maximum; // 해당 효과의 최대 소거 수

            public CancelRequest(Vector3 origin, float radius, int maximum) // 실제 소거 범위의 불변 기록
            {
                Origin = origin; // 파괴 당시 위치 보존
                Radius = radius; // 기획된 실제 반경
                Maximum = maximum; // 기획된 소거 수 상한
            }
        }
        [SerializeField] private FlightThreatBolt template; // 실제 적탄 프리팹
        [SerializeField] private FlightPilotVitals target; // 현재 비행체의 피격 상태
        [SerializeField, Min(1)] private int capacity = 128; // 활성 및 전체 생성의 상한
        [SerializeField, Min(0)] private int warmCount = 24; // 출격 처음에 준비할 적탄 수
        private List<FlightThreatBolt> active; // 발사 순서로 움직이는 실제 탄
        private Stack<FlightThreatBolt> idle; // 즉시 재사용할 비활성 탄
        private Transform targetPosition; // 한 프레임에 한 번만 읽을 비행체 위치
        private readonly List<CancelRequest> pendingCancels = new(); // 충돌 처리 중 발생한 지연 소거
        private sealed class StoryFlight
        {
            internal FlightStoryThreatShot Shot;
            internal FlightStoryReservation Reservation;
            internal float Age, Delay, FirstStep = -1f;
            internal bool NeedsLaunchReset;
            internal Vector3 PreviousPilot;
        }
        private readonly Dictionary<FlightThreatBolt, StoryFlight> storyFlights = new Dictionary<FlightThreatBolt, StoryFlight>();
        private FlightStoryBurstBudget storyBudget;
        private readonly HashSet<FlightStoryCombatant> pendingOwnerReturns = new HashSet<FlightStoryCombatant>();
        private bool advancing, pendingReturnAll;

        public int Capacity => capacity; // 편집 가능한 탄 수 한도
        public int ActiveCount => active.Count; // 현재 이동하는 탄 수
        public int CreatedCount => active.Count + idle.Count; // 생성 개체의 전체 수
        public int ReservedStoryCount => storyBudget == null ? 0 : storyBudget.Pending;
        public int StoryEpoch => storyBudget == null ? 0 : storyBudget.Epoch;

        private void Awake() // 현재 씬의 미리 생성 수
        {
            active = new List<FlightThreatBolt>(capacity); // 활성 개체 증가 때 추가 할당 방지
            idle = new Stack<FlightThreatBolt>(capacity); // 사용한 탄 회수 공간 사전 준비
            storyBudget = new FlightStoryBurstBudget(capacity, Mathf.Min(96, capacity));
            targetPosition = target.transform; // 다음 프레임의 기체 위치 캐시
            int count = Mathf.Min(capacity, warmCount); // 전체 한도를 넘지 않는 초기 크기
            for (int index = 0; index < count; index++) // 출격 전에 먼저 재사용할 탄
            {
                idle.Push(CreateBolt()); // 비활성 상태에서 적탄 배치
            }
        }

        private void LateUpdate() // 활성 탄만 같은 표본 위치로 갱신
        {
            if (Time.timeScale == 0f) // 메뉴에서 시간이 정지된 상태
            {
                return; // 물리 이동 없이 현재 장면 유지
            }

            Vector3 currentPilot = targetPosition.position; // 이 프레임의 공통 기체 좌표
            float elapsed = Time.deltaTime; // 같은 프레임 시간 한 번만 조회
            int index = 0; // 첫 활성 탄부터 같은 발사 순서
            advancing = true;
            while (index < active.Count) // 회수하지 않은 다음 탄도 처리
            {
                if (pendingReturnAll) break;
                FlightThreatBolt current = active[index];
                if (storyFlights.TryGetValue(current, out StoryFlight story))
                {
                    if (AdvanceStory(index, current, story, currentPilot, elapsed)) index++;
                    continue;
                }
                if (!target.IsDefeated && active[index].Advance(target, currentPilot, elapsed)) // 피격·지형·수명 후 계속 가능한 탄
                {
                    index++; // 기존 배열에서 다음 탄
                }
                else
                {
                    Recycle(index); // 뺀 자리에 온 다음 탄을 이어서 검사
                }
            }
            advancing = false;
            if (pendingReturnAll)
            {
                pendingReturnAll = false;
                ReturnAll();
                return;
            }
            foreach (FlightStoryCombatant owner in pendingOwnerReturns) ReturnStoryOwner(owner);
            pendingOwnerReturns.Clear();
            foreach (CancelRequest request in pendingCancels) // 현재 적탄 순회가 모두 끝난 뒤
            {
                CancelWithin(request.Origin, request.Radius, request.Maximum); // 소거 가능한 적탄만 안전하게 회수
            }
            pendingCancels.Clear(); // 이미 처리한 소거 요청 제거
        }

        public bool TryFire(Vector3 position, Vector3 direction, float speed, int damage = 0) // 직접 발사와 훈련의 같은 기본탄
        {
            return TryFire(position, direction, speed, damage, FlightThreatPattern.Aimed); // 주황 조준탄 기본 상태
        }

        internal bool TryFire(Vector3 position, Vector3 direction, float speed, int damage, FlightThreatPattern pattern, bool canCancel = true) // 형태와 이동을 포함한 실제 적탄 발사
        {
            return TryFire(position, direction, speed, damage, pattern, canCancel, true); // 일반 적탄의 독립 흡수 허용
        }

        internal bool TryFire(Vector3 position, Vector3 direction, float speed, int damage, FlightThreatPattern pattern, bool canCancel, bool canAbsorb) // 상쇄와 흡수 허용을 분리한 실제 적탄 발사
        {
            if (target.IsDefeated || active.Count + ReservedStoryCount >= capacity || speed <= 0f || direction.sqrMagnitude < 0.0001f) // 예약된 분열 자식의 물리 슬롯도 보존
            {
                return false; // 잘못된 탄창 또는 용량 초과를 건너뜀
            }

            FlightThreatBolt bolt = idle.Count > 0 ? idle.Pop() : CreateBolt(); // 이미 회수한 물리 객체 우선 사용
            bolt.Location.SetPositionAndRotation(position, Quaternion.LookRotation(direction)); // 적탄의 출발 위치와 실제 방향
            bolt.Begin(targetPosition.position, speed, damage, pattern, canCancel, canAbsorb); // 적탄별 상호작용과 수명·외형
            active.Add(bolt); // 활성 목록에서 같은 발사 순서 유지
            bolt.View.SetActive(true); // 기존 씬 오브젝트 표시
            return true; // 새 탄 발사 확인
        }

        public void ReturnAll() // 재출격 또는 보스 패턴 종료의 일괄 회수
        {
            if (advancing)
            {
                storyBudget?.Clear();
                pendingReturnAll = true;
                return;
            }
            pendingCancels.Clear(); // 이전 전투에서 예약한 소거가 새 탄에 적용되지 않음
            pendingOwnerReturns.Clear();
            storyBudget?.Clear(); // 이전 세대의 지연 분열 예약 무효화
            for (int index = active.Count - 1; index >= 0; index--) // 활성 탄의 뒤부터 처리
            {
                Recycle(index); // 같은 활성 탄을 두 번 회수하지 않음
            }
        }

        internal bool TryFireStoryBurst(IReadOnlyList<FlightStoryThreatShot> shots)
        {
            if (shots == null || shots.Count == 0 || target == null || target.IsDefeated) return false;
            int count = 0;
            for (int i = 0; i < shots.Count; i++)
            {
                FlightStoryThreatShot shot = shots[i];
                if (!ValidStoryShot(shot)) return false;
                if (count > 96 - shot.Plan.ReservationCount) return false;
                count += shot.Plan.ReservationCount;
            }
            FlightStoryReservation reservation = storyBudget.TryReserve(count, active.Count);
            if (reservation == null) return false;
            // Every parameter and the complete logical burst were validated before creating any shot.
            for (int i = 0; i < shots.Count; i++) SpawnStory(shots[i], reservation);
            return true;
        }

        internal void ReturnStoryOwner(FlightStoryCombatant owner)
        {
            if (advancing)
            {
                pendingOwnerReturns.Add(owner);
                return;
            }
            for (int index = active.Count - 1; index >= 0; index--)
            {
                if (storyFlights.TryGetValue(active[index], out StoryFlight flight) && flight.Shot.Owner == owner) Recycle(index);
            }
        }

        internal FlightStoryCombatant StoryOwnerOf(FlightThreatBolt bolt)
        {
            return bolt != null && storyFlights.TryGetValue(bolt, out StoryFlight flight) ? flight.Shot.Owner : null;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);

        private static bool ValidStoryShot(FlightStoryThreatShot shot)
        {
            if (shot == null || shot.Plan == null || !Finite(shot.Position) || !Finite(shot.Direction) || shot.Direction.sqrMagnitude < 0.0001f) return false;
            FlightStoryShotPlan plan = shot.Plan;
            if (!Finite(plan.Speed) || plan.Speed <= 0f || plan.Damage <= 0 || plan.ChildYaw == null || !Finite(plan.LaunchDelay) || plan.LaunchDelay < 0f || !Finite(plan.TurnDegrees) || !Finite(plan.TurnSeconds) || plan.TurnSeconds < 0f || !Finite(plan.SplitAfter)) return false;
            if (plan.ChildYaw.Length > 0 && (plan.SplitAfter <= 0f || !Finite(plan.ChildSpeed) || plan.ChildSpeed <= 0f)) return false;
            foreach (float yaw in plan.ChildYaw) if (!Finite(yaw)) return false;
            return plan.Visual >= 0 && plan.Visual <= 3;
        }

        private FlightThreatBolt SpawnStory(FlightStoryThreatShot shot, FlightStoryReservation reservation, float firstStep = -1f, Vector3? startPilot = null)
        {
            if (!storyBudget.Commit(reservation, 1)) return null;
            FlightThreatBolt bolt = idle.Count > 0 ? idle.Pop() : CreateBolt();
            Vector3 direction = shot.Direction.normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
            bolt.Location.SetPositionAndRotation(shot.Position, Quaternion.LookRotation(direction, up));
            Vector3 pilotAtStart = startPilot ?? targetPosition.position;
            bolt.Begin(pilotAtStart, shot.Plan.Speed, shot.Plan.Damage, (FlightThreatPattern)shot.Plan.Visual, shot.CanCancel, shot.CanAbsorb);
            active.Add(bolt);
            storyFlights.Add(bolt, new StoryFlight { Shot = shot, Reservation = reservation, Delay = shot.Plan.LaunchDelay, NeedsLaunchReset = shot.Plan.LaunchDelay > 0f, FirstStep = firstStep, PreviousPilot = pilotAtStart });
            bolt.View.SetActive(true);
            return bolt;
        }

        private bool AdvanceStory(int index, FlightThreatBolt bolt, StoryFlight story, Vector3 pilotPosition, float delta)
        {
            if (target.IsDefeated || pendingOwnerReturns.Contains(story.Shot.Owner) || (story.Shot.Owner != null && !story.Shot.Owner.CanThreaten))
            {
                Recycle(index);
                return false;
            }
            float remaining = story.FirstStep >= 0f ? story.FirstStep : delta;
            story.FirstStep = -1f;
            Vector3 pilotAtStart = story.PreviousPilot;
            if (story.Delay > 0f)
            {
                float waited = Mathf.Min(story.Delay, remaining);
                if (remaining > 0f) pilotAtStart = Vector3.Lerp(pilotAtStart, pilotPosition, waited / remaining);
                story.Delay -= waited;
                remaining -= waited;
                if (remaining <= 0f) { story.PreviousPilot = pilotPosition; return true; }
            }
            if (story.NeedsLaunchReset)
            {
                // The projectile's lifetime and pilot sweep begin when its visible warning ends.
                bolt.Begin(pilotAtStart, story.Shot.Plan.Speed, story.Shot.Plan.Damage, (FlightThreatPattern)story.Shot.Plan.Visual, story.Shot.CanCancel, story.Shot.CanAbsorb);
                story.NeedsLaunchReset = false;
            }
            FlightStoryShotPlan plan = story.Shot.Plan;
            float duration = plan.ChildYaw.Length > 0 ? Mathf.Min(remaining, Mathf.Max(0f, plan.SplitAfter - story.Age)) : remaining;
            float turning = Mathf.Min(duration, Mathf.Max(0f, plan.TurnSeconds - story.Age));
            if (turning > 0f && plan.TurnDegrees != 0f) bolt.Location.rotation = Quaternion.AngleAxis(plan.TurnDegrees * turning, Vector3.up) * bolt.Location.rotation;
            Vector3 pilotAtEnd = remaining > 0f ? Vector3.Lerp(pilotAtStart, pilotPosition, duration / remaining) : pilotAtStart;
            if (duration > 0f && !bolt.Advance(target, pilotAtEnd, duration))
            {
                Recycle(index);
                return false;
            }
            story.PreviousPilot = pilotAtEnd;
            story.Age += duration;
            if (plan.ChildYaw.Length == 0 || story.Age + 0.00001f < plan.SplitAfter) return true;
            Vector3 position = bolt.Location.position;
            Quaternion basis = bolt.Location.rotation;
            storyFlights.Remove(bolt); // Keep the child entitlement while the parent is recycled.
            Recycle(index);
            for (int child = 0; child < plan.ChildYaw.Length; child++)
            {
                var childPlan = new FlightStoryShotPlan { Speed = plan.ChildSpeed, Damage = plan.Damage, Visual = 1 };
                var shot = new FlightStoryThreatShot
                {
                    Position = position,
                    Direction = basis * (Quaternion.Euler(0f, plan.ChildYaw[child], 0f) * Vector3.forward),
                    Plan = childPlan,
                    Owner = story.Shot.Owner,
                    CanCancel = story.Shot.CanCancel,
                    CanAbsorb = story.Shot.CanAbsorb
                };
                SpawnStory(shot, story.Reservation, Mathf.Max(0f, remaining - duration), pilotAtEnd);
            }
            return false;
        }

        public int ClearInView(Camera view) // 실제 화면에서만 가능한 적탄 지우기
        {
            int count = 0; // 화면에 포함해 지운 실제 수량
            float near = view.nearClipPlane; // 카메라 앞으로 보이는 최소 거리
            float far = view.farClipPlane; // 화면에 보이는 최대 거리
            for (int index = active.Count - 1; index >= 0; index--) // 활성 탄의 좌표만 검사
            {
                Vector3 screen = view.WorldToViewportPoint(active[index].Location.position); // 카메라 화면 위치
                if (screen.z >= near && screen.z <= far && screen.x >= 0f && screen.x <= 1f && screen.y >= 0f && screen.y <= 1f) // 실제 카메라 면 안에서 소거
                {
                    Recycle(index); // 적탄 풀로 돌아감
                    count++; // 실제 제거 수
                }
            }

            return count; // 스킬이 기록하는 화면 적탄 개수
        }

        public int ClearWithin(Vector3 origin, float radius) // 3D 반경으로 지울 활성 적탄
        {
            float limit = radius * radius; // 제곱 거리 범위
            int count = 0; // 이번에 제거한 탄
            for (int index = active.Count - 1; index >= 0; index--) // 이미 회수한 탄의 native 상태 조회 없음
            {
                if ((active[index].Location.position - origin).sqrMagnitude <= limit) // 가까운 일반 적탄의 위치만 확인
                {
                    Recycle(index); // 같은 발은 이후 검사에서 제거
                    count++; // 실제 제거 수
                }
            }

            return count; // 몇 발을 지웠는지 환산
        }

        internal int CancelWithin(Vector3 origin, float radius, int maximum) // 허용된 적탄만 정해진 수까지 상쇄
        {
            float limit = radius * radius; // 결계 중심의 제곱 반경
            int count = 0; // 실제로 상쇄한 적탄 수
            for (int index = active.Count - 1; index >= 0 && count < maximum; index--) // 활성 탄을 한도까지 확인
            {
                FlightThreatBolt bolt = active[index]; // 현재 활성 적탄
                if (bolt.CanCancel && (bolt.Location.position - origin).sqrMagnitude <= limit) // 상쇄 허용과 중심 거리 확인
                {
                    Recycle(index); // 적탄을 재사용 풀로 반환
                    count++; // 실제 상쇄 수 증가
                }
            }
            return count; // 이번 프레임의 상쇄 수
        }

        internal void QueueCancelWithin(Vector3 origin, float radius, int maximum) // 피격 처리 도중에도 안전한 적탄 소거 요청
        {
            if (radius > 0f && maximum > 0) // 실제 범위와 소거 가능 수
            {
                pendingCancels.Add(new CancelRequest(origin, radius, maximum)); // 현재 순회가 끝난 뒤 같은 위치에서 처리
            }
        }

        internal int AbsorbWithin(Vector3 origin, float radius, int maximum) // 흡수가 허용된 적탄만 정해진 수까지 회수
        {
            float limit = radius * radius; // 위성 중심의 제곱 반경
            int count = 0; // 실제로 흡수한 적탄 수
            for (int index = active.Count - 1; index >= 0 && count < maximum; index--) // 현재 활성 적탄과 남은 한도
            {
                FlightThreatBolt bolt = active[index]; // 현재 살아 있는 적탄
                if (bolt.CanAbsorb && (bolt.Location.position - origin).sqrMagnitude <= limit) // 독립 흡수 허용과 거리 확인
                {
                    Recycle(index); // 이후 중복 흡수가 불가능한 풀 회수
                    count++; // 이번 위성의 실제 흡수 수
                }
            }
            return count; // 이번 프레임의 유효한 흡수 수
        }

        public void ClearAlongBeam(Vector3 start, Vector3 direction, float length, float radius) // 지형 앞까지 광선에 닿은 탄을 회수
        {
            for (int index = active.Count - 1; index >= 0; index--) // 이미 비활성인 탄 제외
            {
                FlightThreatBolt bolt = active[index]; // 현재 살아 있는 실제 3D 탄
                Vector3 offset = bolt.Location.position - start; // 광선 시작과 적탄 위치
                float distance = Vector3.Dot(offset, direction); // 광선 위 직교 투영의 거리
                float combinedRadius = radius + bolt.Radius; // 실제 탄환 두께를 포함한 접촉 폭
                if (distance >= 0f && distance <= length && (offset - direction * distance).sqrMagnitude <= combinedRadius * combinedRadius) // 지형 앞 원통에 닿는지 확인
                {
                    Recycle(index); // 닿은 탄만 재사용 풀로
                }
            }
        }

        private void Recycle(int index) // 현재 활성 슬롯을 한번 비우고 객체 재사용
        {
            FlightThreatBolt bolt = active[index]; // 같은 발의 실제 객체
            if (storyFlights.TryGetValue(bolt, out StoryFlight flight))
            {
                if (flight.Shot.Plan.ChildYaw.Length > 0) storyBudget.Release(flight.Reservation, flight.Shot.Plan.ChildYaw.Length);
                storyFlights.Remove(bolt);
            }
            active.RemoveAt(index); // 이후의 발사 순서도 그대로 유지
            bolt.View.SetActive(false); // 화면에서 제거
            idle.Push(bolt); // 다음 패턴에서 재사용
        }

        private FlightThreatBolt CreateBolt() // 풀의 하이러키에 원본으로 탄을 생성
        {
            FlightThreatBolt bolt = Instantiate(template, transform); // 이전과 같은 3D 프리팹 배치
            bolt.CacheView(); // 부모와 현재 탄의 실제 표시 참조
            bolt.View.SetActive(false); // 꺼내기 전까지 비활성
            return bolt; // 최대 동시 생성 수 안에서 관리
        }
    }
}
