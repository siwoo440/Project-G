using UnityEngine; // 조준선과 발사 위치
using UnityEngine.InputSystem; // 실제 마우스 사격
using UnityEngine.UI; // 이미지 조준점 표시

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    [DefaultExecutionOrder(100)] // 추적 카메라 갱신 이후 조준
    [DisallowMultipleComponent] // 비행체당 발사 컴포넌트 한 개
    public sealed class FlightWeapon : MonoBehaviour // 화면 중심 수동 사격
    { // 사격 시작
        [SerializeField] private FirstFlightController pilot; // 현재 비행·집중 입력
        [SerializeField] private Camera flightCamera; // 화면 조준 카메라
        [SerializeField] private Transform muzzle; // 씬의 빗자루 발사 위치
        [SerializeField] private Graphic reticle; // 제작 조준점 이미지
        [SerializeField] private FlightSpellBolt standardBolt; // 기본 마법탄 프리팹
        [SerializeField] private FlightSpellBolt focusedBolt; // 집중 마법탄 프리팹
        [SerializeField] private LayerMask aimLayers = (1 << 8) | (1 << 9); // 비행 지형과 조준 표적
        [SerializeField, Min(1f)] private float aimRange = 85f; // 화면 중심의 최대 조준 거리
        private float nextShot; // 다음 연사 가능 시각

        private void LateUpdate() // 실제 비행 화면 기준 사격 처리
        { // 처리 시작
            bool canAim = pilot.CanFly && pilot.HasPointerControl && pilot.IsAimView; // 자유 시점과 커서 해제 중 조준 제외
            reticle.enabled = canAim; // 실제로 발사할 수 있는 시점 표시
            Mouse mouse = Mouse.current; // 현재 마우스 장치
            if (!canAim || mouse == null || !mouse.leftButton.isPressed || Time.time < nextShot) // 입력과 발사 간격 확인
            { // 사격 생략 시작
                return; // 현재 프레임에 발사하지 않는 상태
            } // 사격 생략 종료

            Ray aim = flightCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 화면 정중앙의 시선
            Vector3 point = Physics.Raycast(aim, out RaycastHit surface, aimRange, aimLayers, QueryTriggerInteraction.Ignore) ? surface.point : aim.GetPoint(aimRange); // 조준 대상 또는 조준 거리 끝
            Vector3 travel = point - muzzle.position; // 총구와 조준점의 차이 보정
            if (Vector3.Dot(travel, flightCamera.transform.forward) <= 0.001f) // 총구가 화면 앞 장애물보다 앞에 있는 상태
            { // 근접 사격 제외 시작
                return; // 총구 뒤쪽으로 사격 방지
            } // 근접 사격 제외 종료

            bool focused = pilot.IsFocusHeld; // 현재 집중 모드
            FlightSpellBolt bolt = Instantiate(focused ? focusedBolt : standardBolt, muzzle.position, Quaternion.LookRotation(travel)); // 씬의 발사 위치에서 탄환 생성
            bolt.Begin(aimLayers, aimRange); // 탄환 충돌 면과 수명 초기화
            nextShot = Time.time + (focused ? 0.27f : 0.18f); // 집중탄과 기본탄의 시험 발사 간격
        } // 처리 종료
    } // 사격 종료
} // 영역 종료
