using UnityEngine; // 원근 추적 카메라

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    [DisallowMultipleComponent] // 카메라 중복 추적 방지
    [RequireComponent(typeof(Camera))] // 추적을 적용할 카메라
    public sealed class FirstFlightCamera : MonoBehaviour // 비행체 뒤 고정 거리 추적
    { // 추적 시작
        [SerializeField] private Transform target; // 씬의 비행체 루트 참조
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.65f, -7.4f); // 뒤쪽 어깨 위 위치
        [SerializeField] private Vector3 viewPoint = new Vector3(0f, 0.5f, 7f); // 플레이어 앞의 시선 지점

        private void LateUpdate() // 비행 이동이 끝난 후 추적
        { // 프레임 추적 시작
            if (target == null) // 편집 중 연결 여부
            { // 연결 누락 처리 시작
                return; // 카메라의 현재 배치 보존
            } // 연결 누락 처리 종료

            Vector3 position = target.position + target.rotation * offset; // 비행체 방향 기준 카메라 위치
            Vector3 focus = target.position + target.rotation * viewPoint; // 앞쪽 비행 공간의 초점
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position, Vector3.up)); // 한 프레임에 추적과 구도 반영
        } // 프레임 추적 종료
    } // 추적 종료
} // 영역 종료
