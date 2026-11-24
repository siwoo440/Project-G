using UnityEngine; // 빠른 적탄의 월드 이동

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    [DefaultExecutionOrder(110)] // 비행체 이동과 카메라 적용 후 판정
    public sealed class FlightThreatBolt : MonoBehaviour // 3차원 적탄 프리팹
    { // 적탄 시작
        [SerializeField, Min(0.01f)] private float radius = 0.10f; // 시험 적탄의 실제 반경
        [SerializeField, Min(1)] private int damage = 20; // 시험 적탄의 피해
        private FlightPilotVitals vitals; // 이번 탄환이 겨누는 비행체
        private Vector3 previousPilot; // 이전 프레임의 판정 중심
        private float speed; // 초당 이동 거리
        private float remaining = 130f; // 만료 전 최대 월드 거리
        private bool nearCandidate; // 현재까지 근접 구간 진입 여부
        private bool grazeAwarded; // 상대 기체에 대한 중복 그레이즈 방지

        public void Begin(FlightPilotVitals target, float travelSpeed) // 실제 발사된 적탄 초기화
        { // 초기화 시작
            vitals = target; // 현재 씬 비행체 참조
            previousPilot = vitals.transform.position; // 탄환 출발 때 기체 좌표
            speed = travelSpeed; // 발사 시 지정한 시험 속도
        } // 초기화 종료

        private void LateUpdate() // 이동하는 탄환과 기체의 상대 궤적 검사
        { // 검사 시작
            if (vitals == null || vitals.IsDefeated) // 현재 출격 종료 여부
            { // 탄환 종료 시작
                Destroy(gameObject); // 실패 뒤 이전 출격의 적탄 종료
                return; // 이전 체력에 추가 영향 방지
            } // 탄환 종료 종료

            Vector3 start = transform.position; // 적탄의 이전 프레임 위치
            float step = Mathf.Min(remaining, speed * Time.deltaTime); // 최대 사거리 안의 진행량
            bool terrain = Physics.SphereCast(start, radius, transform.forward, out RaycastHit obstruction, step, 1 << 8, QueryTriggerInteraction.Ignore); // 하늘섬에 막히는 지점
            float travel = terrain ? obstruction.distance : step; // 지형 앞까지 실제 이동 거리
            Vector3 end = start + transform.forward * travel; // 이번 프레임의 실제 탄환 끝점
            Vector3 currentPilot = vitals.transform.position; // 움직인 기체의 현재 좌표
            float portion = step > 0f ? travel / step : 0f; // 지형에 막힐 때까지 상대 이동 시간 비율
            Vector3 pilotAtEnd = Vector3.Lerp(previousPilot, currentPilot, portion); // 탄환이 지형에 도달한 때의 기체 위치
            bool hit = vitals.ResolveThreat(start - previousPilot, end - pilotAtEnd, radius, damage, ref nearCandidate, ref grazeAwarded); // 체력과 그레이즈 판정
            if (hit || terrain || remaining <= step) // 접촉이나 만료 처리
            { // 제거 시작
                Destroy(gameObject); // 완료된 탄환이 다시 판정되지 않도록 종료
                return; // 적탄 진행 종료
            } // 제거 종료

            remaining -= step; // 남은 월드 사거리
            previousPilot = currentPilot; // 다음 상대 경로의 기체 출발점
            transform.position = end; // 실제 적탄 이동
        } // 검사 종료
    } // 적탄 종료
} // 영역 종료
