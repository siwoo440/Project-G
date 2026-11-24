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
        private FirstFlightCamera chase; // 조준과 둘러보기의 카메라 좌표
        private float nextShot; // 다음 자동 연사 가능 시각

        private void Awake() // 실제 추적 카메라 연결
        {
            chase = flightCamera.GetComponent<FirstFlightCamera>(); // 독립 둘러보기의 기준 조준
        }

        private void LateUpdate() // 조준 방향으로 자동 사격
        {
            reticle.enabled = pilot.CanFly && pilot.HasPointerControl && pilot.IsAimView; // 실제 조준점 표시 여부
            if (!pilot.CanFly || Time.time < nextShot) // 출격 상태와 발사 간격
            {
                return; // 현재 프레임 발사 생략
            }
            CastSpread(pilot.IsFocusHeld, 1, 0f); // 클릭 없이 기본 또는 집중탄 자동 발사
            nextShot = Time.time + (pilot.IsFocusHeld ? 0.27f : 0.18f); // 모드에 따른 시험 연사 간격
        }

        public bool CastSpread(bool focused, int count, float spread) // 스킬·카드의 마법탄 배열
        {
            Ray aim = chase != null ? chase.GetAttackRay() : flightCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 카메라 자유 회전과 독립된 실제 공격 방향
            Vector3 point = Physics.Raycast(aim, out RaycastHit surface, aimRange, aimLayers, QueryTriggerInteraction.Ignore) ? surface.point : aim.GetPoint(aimRange); // 조준 대상 또는 최대 거리
            Vector3 travel = point - muzzle.position; // 빗자루 발사점과 화면 조준 시차 보정
            if (Vector3.Dot(travel, aim.direction) <= 0.001f) // 총구 앞의 조준 지점인지 확인
            {
                return false; // 벽 앞에서 뒤쪽으로 발사 방지
            }

            Quaternion basis = Quaternion.LookRotation(travel); // 해당 발사의 중심 방향
            for (int index = 0; index < count; index++) // 실제 마법탄 생성
            {
                float yaw = count == 1 ? 0f : Mathf.Lerp(-spread, spread, (float)index / (count - 1)); // 탄막 전체의 좌우 각도
                float pitch = count >= 5 ? (index % 2 == 0 ? -3f : 3f) : 0f; // 넓은 마법탄의 입체 고도
                Quaternion direction = basis * Quaternion.Euler(pitch, yaw, 0f); // 화면 중앙 방향에서 분산
                FlightSpellBolt bolt = Instantiate(focused ? focusedBolt : standardBolt, muzzle.position, direction); // 기존 마법탄 프리팹 재사용
                bolt.Begin(aimLayers, aimRange); // 실제 발사체의 사거리와 충돌 설정
            }
            return true; // 성공한 발사에만 자원 비용 적용
        }
    }
}
