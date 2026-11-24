using UnityEngine; // 원근 추적 카메라와 지형 검사

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    [DisallowMultipleComponent] // 카메라 중복 추적 방지
    [RequireComponent(typeof(Camera))] // 추적을 적용할 카메라
    public sealed class FirstFlightCamera : MonoBehaviour // 시점과 가림을 반영한 추적 카메라
    { // 추적 시작
        [SerializeField] private Transform target; // 씬의 비행체 루트 참조
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.65f, -7.4f); // 뒤쪽 어깨 위 위치
        [SerializeField] private Vector3 viewPoint = new Vector3(0f, 0.5f, 7f); // 플레이어 앞의 시선 지점
        [SerializeField] private LayerMask environmentLayer = 1 << 8; // 시야를 가리는 지형 레이어
        private FirstFlightController pilot; // 비행 방향과 독립 카메라 시점
        private float distance; // 현재 장애물을 반영한 추적 거리

        private void Awake() // 씬에 연결된 비행체 참조 확인
        { // 초기화 시작
            pilot = target != null ? target.GetComponent<FirstFlightController>() : null; // 시점 입력 컴포넌트 저장
        } // 초기화 종료

        private void LateUpdate() // 비행 이동이 끝난 후 추적
        { // 프레임 추적 시작
            if (target == null) // 편집 중 연결 여부
            { // 연결 누락 처리 시작
                return; // 카메라의 현재 배치 보존
            } // 연결 누락 처리 종료

            Quaternion view = pilot != null ? pilot.ViewRotation : target.rotation; // 추적 및 자유 시점 회전
            Vector3 pivot = target.position + Vector3.up * 0.65f; // 비행체 상체의 가림 검사 중심
            Vector3 preferred = target.position + view * offset; // 지형이 없을 때 위치
            Vector3 arm = preferred - pivot; // 가림 검사 방향과 길이
            float desired = arm.magnitude; // 기본 추적 거리
            if (desired < 0.001f) // 오프셋을 잘못 편집한 경우
            { // 축 퇴화 처리 시작
                return; // 회전 계산 오류 방지
            } // 축 퇴화 처리 종료

            Vector3 direction = arm / desired; // 피벗에서 카메라로 향하는 방향
            float visible = desired; // 가림이 없는 최대 거리
            if (Physics.SphereCast(pivot, 0.24f, direction, out RaycastHit hit, desired, environmentLayer, QueryTriggerInteraction.Ignore)) // 실제 지형의 시야 가림
            { // 지형 대응 시작
                visible = Mathf.Max(0.28f, hit.distance - 0.04f); // 지형의 바로 앞에 카메라 배치
            } // 지형 대응 종료
            distance = distance <= 0f || visible < distance ? visible : Mathf.MoveTowards(distance, visible, 12f * Time.deltaTime); // 즉시 당기고 서서히 복귀
            Vector3 position = pivot + direction * distance; // 가려지지 않는 카메라 좌표
            Vector3 focus = Vector3.Lerp(pivot, target.position + view * viewPoint, Mathf.Clamp01(distance / desired)); // 가까운 상태에서 아리아 쪽으로 시선 보정
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position, Vector3.up)); // 실제 추적과 시야 반영
        } // 프레임 추적 종료
    } // 추적 종료
} // 영역 종료
