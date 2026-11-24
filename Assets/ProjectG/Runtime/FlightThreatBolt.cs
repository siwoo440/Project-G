using UnityEngine; // 빠른 적탄의 월드 이동

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(110)] // 비행체 이동과 카메라 적용 후 판정
    public sealed class FlightThreatBolt : MonoBehaviour // 3차원 적탄 프리팹
    {
        [SerializeField, Min(0.01f)] private float radius = 0.10f; // 시험 적탄의 실제 반경
        [SerializeField, Min(1)] private int damage = 20; // 시험 적탄의 피해
        [SerializeField, Min(0.01f)] private float range = 130f; // 적탄의 최대 진행 거리
        [SerializeField, Min(0.01f)] private float lifetime = 8f; // 적탄의 최대 활성 시간
        private FlightThreatPool owner; // 재사용할 풀의 소유자
        private FlightPilotVitals vitals; // 이번 탄환이 겨누는 비행체
        private Vector3 previousPilot; // 이전 프레임의 판정 중심
        private float speed; // 초당 이동 거리
        private float remaining; // 사용 중 남은 월드 거리
        private float secondsLeft; // 사용 중 남은 프레임 시간
        private bool nearCandidate; // 현재까지 근접 구간 진입 여부
        private bool grazeAwarded; // 상대 기체에 대한 중복 그레이즈 방지

        internal float Radius => radius; // 적탄 소거의 실제 반경

        internal void Begin(FlightThreatPool pool, FlightPilotVitals target, float travelSpeed) // 실제 발사된 적탄 초기화
        {
            owner = pool; // 해당 탄을 관리하는 풀 참조
            vitals = target; // 현재 씬 비행체 참조
            remaining = range; // 이전 사용에서 소진한 거리 복구
            secondsLeft = lifetime; // 새 탄의 사용 시간 초기화
            nearCandidate = false; // 이전 탄의 근접 후보 초기화
            grazeAwarded = false; // 재사용 탄의 새 그레이즈 가능 상태
            previousPilot = vitals.transform.position; // 탄환 출발 때 기체 좌표
            speed = travelSpeed; // 발사 시 지정한 시험 속도
        }

        private void LateUpdate() // 이동하는 탄환과 기체의 상대 궤적 검사
        {
            if (vitals.IsDefeated) // 현재 출격 종료 여부
            {
                owner.Return(this); // 실패 뒤 적탄을 비활성 풀로 회수
                return; // 이전 체력에 추가 영향 방지
            }

            Vector3 start = transform.position; // 적탄의 이전 프레임 위치
            float duration = Mathf.Min(secondsLeft, Time.deltaTime); // 현재 탄환이 진행할 유효 시간
            float step = Mathf.Min(remaining, speed * duration); // 수명·사거리 안의 실제 이동량
            bool terrain = Physics.SphereCast(start, radius, transform.forward, out RaycastHit obstruction, step, 1 << 8, QueryTriggerInteraction.Ignore); // 하늘섬에 막히는 지점
            float travel = terrain ? obstruction.distance : step; // 지형 앞까지 실제 이동 거리
            Vector3 end = start + transform.forward * travel; // 이번 프레임의 실제 탄환 끝점
            Vector3 currentPilot = vitals.transform.position; // 움직인 기체의 현재 좌표
            float portion = Time.deltaTime > 0f ? travel / (speed * Time.deltaTime) : 0f; // 이번 프레임에서 실제로 움직인 시간 비율
            Vector3 pilotAtEnd = Vector3.Lerp(previousPilot, currentPilot, portion); // 탄환이 지형에 도달한 때의 기체 위치
            bool hit = vitals.ResolveThreat(start - previousPilot, end - pilotAtEnd, radius, damage, ref nearCandidate, ref grazeAwarded); // 체력과 그레이즈 판정
            if (hit || terrain || remaining <= step || secondsLeft <= duration) // 접촉이나 거리·시간 만료 처리
            {
                owner.Return(this); // 종료된 탄을 풀로 돌려보냄
                return; // 적탄 진행 종료
            }

            remaining -= step; // 남은 월드 사거리
            secondsLeft -= duration; // 남은 활성 시간
            previousPilot = currentPilot; // 다음 상대 경로의 기체 출발점
            transform.position = end; // 실제 적탄 이동
        }
    }
}
