using UnityEngine; // 월드 이동과 충돌
using UnityEngine.InputSystem; // 키보드와 마우스

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 한 비행체당 컨트롤러 한 개
    public sealed class FirstFlightController : MonoBehaviour // 3축 관성 비행과 시점 조작
    {
        [SerializeField, Min(0f)] private float speed = 12f; // 시험용 최고 이동 속도
        [SerializeField, Min(0.01f)] private float acceleration = 28f; // 초당 가속량
        [SerializeField, Min(0.01f)] private float braking = 26f; // 초당 제동량
        [SerializeField, Range(0.01f, 1f)] private float focusSpeed = 0.5f; // 집중 비행 속도 비율
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.18f; // 마우스 픽셀당 회전 각도
        [SerializeField] private FlightBindings bindings; // 설정에 표시된 실제 비행 키
        [SerializeField] private Bounds flightBounds = new Bounds(new Vector3(0f, 14f, 26f), new Vector3(92f, 46f, 122f)); // 월드 축 기준 비행 체적
        [SerializeField] private BoxCollider flightVolume; // 실전 씬에 배치한 축 정렬 전투 구역
        [SerializeField] private LayerMask environmentLayer = 1 << 8; // 비행 지형 충돌 대상
        private FlightPilotVitals vitals; // 현재 비행체의 생존 상태
        private FlightBroomEquipment broom; // 실전 비행체의 장착 부품
        private Vector3 velocity; // 관성이 포함된 월드 속도
        private float pitch; // 기본 시점의 상하 각도
        private float orbitYaw; // 둘러보기의 좌우 각도
        private float orbitPitch; // 둘러보기의 상하 각도
        private float sensitivityScale = 1f; // 게임 설정의 마우스 회전 배율
        private bool sortieEnded; // 종료 안내에서 비행과 커서 재잠금 차단

        public bool CanFly => !sortieEnded && (vitals == null || !vitals.IsDefeated); // 실제 출격 조작 가능 상태
        public bool IsFocusHeld { get; private set; } // 현재 집중 비행 입력
        public bool IsFreeLooking { get; private set; } // 현재 독립 시점 입력
        public bool PointerCapturedThisFrame { get; private set; } // 커서 재잠금 클릭 소비
        public Quaternion AimRotation => transform.rotation * Quaternion.Euler(pitch, 0f, 0f); // 둘러보기와 분리한 실제 조준
        public bool IsAimView => !IsFreeLooking && Mathf.Abs(orbitYaw) < 0.1f && Mathf.Abs(orbitPitch) < 0.1f; // 기본 조준 각도로 완전히 복귀한 상태
        public bool HasPointerControl => Application.isBatchMode || Cursor.lockState == CursorLockMode.Locked; // 마우스 제어 상태
        public Quaternion ViewRotation => transform.rotation * Quaternion.Euler(Mathf.Clamp(pitch + orbitPitch, -65f, 65f), orbitYaw, 0f); // 기본 시점과 둘러보기 합성
        public float MaximumSpeed => speed + PartBonus(FlightBroomStat.Speed); // 현재 조합의 수평 최고 속도

        private void Awake() // 선택적 비행 체력 참조
        {
            TryGetComponent(out vitals); // 현재 씬의 기체 상태
            TryGetComponent(out broom); // 씬에 배치한 빗자루 부품
        }

        public void ResetFlightPose(Vector3 position, Quaternion rotation) // 출격 재시작의 초기 비행 자세
        {
            velocity = Vector3.zero; // 이전 출격 관성 제거
            pitch = 0f; // 기본 시점 각도 복원
            orbitYaw = 0f; // 독립 좌우 시점 초기화
            orbitPitch = 0f; // 독립 상하 시점 초기화
            IsFreeLooking = false; // 출격 시작 시 둘러보기 초기 상태
            transform.SetPositionAndRotation(position, rotation); // 시작 위치와 회전 복귀
        }

        public void SetSensitivityScale(float scale) // 게임 설정의 마우스 감도
        {
            sensitivityScale = Mathf.Clamp(scale, 0.25f, 2f); // 설정에서 지원하는 감도 범위
        }

        internal void SetSortieEnded(bool ended) // 결과 확인 또는 같은 씬의 새 출격
        {
            sortieEnded = ended; // 현재 카메라와 비행 입력 상태
        }
        private void OnEnable() // 시험 씬 입력 시작
        {
            if (!Application.isBatchMode && Application.isFocused) // 실제 게임에 포커스가 있는 상태
            {
                Cursor.lockState = CursorLockMode.Locked; // 시점 조작용 커서 잠금
            }
        }

        private void OnDisable() // 시험 씬 입력 종료
        {
            if (!Application.isBatchMode && Application.isPlaying) // 실제 게임에서 씬 종료
            {
                Cursor.lockState = CursorLockMode.None; // 메뉴와 에디터에 커서 반환
            }
        }

        private void Update() // 매 프레임 비행 조작 갱신
        {
            if (Time.timeScale == 0f) // 레벨 선택은 비행 조작이 아님
            {
                return; // 카메라 선회와 클릭 잠금 방지
            }
            PointerCapturedThisFrame = false; // 누르기 전의 새 입력 프레임
            if (!CanFly) // 체력 소진 상태
            {
                velocity = Vector3.zero; // 실패 이후의 잔류 관성 차단
                return; // 재시작 전 이동과 선회 차단
            }
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            Mouse mouse = Mouse.current; // 현재 마우스
            UpdateLook(mouse, Time.deltaTime); // 선회와 독립 시점 적용
            IsFocusHeld = keyboard != null && bindings.Held(keyboard, FlightAction.Focus); // 현재 설정의 집중 이동
            Vector3 input = keyboard == null ? Vector3.zero : ReadFlightInput(keyboard); // 현재 비행 방향
            float horizontalSpeed = MaximumSpeed; // 부품 효과가 포함된 수평 속도
            float verticalSpeed = broom == null ? horizontalSpeed : 8f + PartBonus(input.y >= 0f ? FlightBroomStat.Rise : FlightBroomStat.Fall); // 방향별 고도 이동 속도
            Vector3 axisSpeeds = new Vector3(horizontalSpeed, verticalSpeed, horizontalSpeed); // 정규화된 입력의 축별 배율
            Vector3 desired = transform.rotation * Vector3.Scale(input, axisSpeeds) * (IsFocusHeld ? focusSpeed : 1f); // 집중 이동을 포함한 목표 속도
            bool slowing = desired.sqrMagnitude < velocity.sqrMagnitude || Vector3.Dot(desired, velocity) < 0f; // 감속과 반대 방향의 제동
            float rate = slowing ? braking + PartBonus(FlightBroomStat.Braking) : acceleration + PartBonus(FlightBroomStat.Acceleration); // 장착 효과가 포함된 속도 변화율
            velocity = Vector3.MoveTowards(velocity, desired, Mathf.Max(0.01f, rate) * Time.deltaTime); // 가속과 제동 적용
            MoveWithinBounds(velocity * Time.deltaTime); // 지형과 경계 안에서 이동
        }

        private float PartBonus(FlightBroomStat stat) // 선택적 부품 효과 조회
        {
            return broom != null ? broom.GetBonus(stat) : 0f; // 시험 씬의 기존 수치 보존
        }

        private Vector3 ReadFlightInput(Keyboard keyboard) // 키보드의 축별 방향
        {
            float lateral = (bindings.Held(keyboard, FlightAction.Right) ? 1f : 0f) - (bindings.Held(keyboard, FlightAction.Left) ? 1f : 0f); // 설정된 좌우 이동
            float vertical = (bindings.Held(keyboard, FlightAction.Rise) ? 1f : 0f) - (bindings.Held(keyboard, FlightAction.Descend) ? 1f : 0f); // 설정된 상승과 하강
            float depth = (bindings.Held(keyboard, FlightAction.Forward) ? 1f : 0f) - (bindings.Held(keyboard, FlightAction.Back) ? 1f : 0f); // 설정된 앞뒤 이동
            return Vector3.ClampMagnitude(new Vector3(lateral, vertical, depth), 1f); // 동시 입력의 최고 속도 유지
        }

        private void UpdateLook(Mouse mouse, float seconds) // 마우스 시점 처리
        {
            if (!HasPointerControl && Application.isFocused && mouse != null && mouse.leftButton.wasPressedThisFrame) // 게임 창 재선택
            {
                Cursor.lockState = CursorLockMode.Locked; // 시점 마우스 재사용
                PointerCapturedThisFrame = true; // 창 선택 클릭을 카드 사용에서 제외
                return; // 창 선택 클릭의 회전 억제
            }

            Vector2 look = HasPointerControl && mouse != null ? mouse.delta.ReadValue() * (mouseSensitivity * sensitivityScale) : Vector2.zero; // 설정된 프레임 회전 입력
            IsFreeLooking = HasPointerControl && mouse != null && mouse.middleButton.isPressed; // 휠 버튼 독립 시점
            if (IsFreeLooking) // 진행 방향과 분리된 카메라 입력
            {
                orbitYaw = Mathf.Clamp(orbitYaw + look.x, -165f, 165f); // 뒤까지 보는 좌우 시야
                orbitPitch = Mathf.Clamp(orbitPitch - look.y, -55f - pitch, 55f - pitch); // 상하 시야 범위
                return; // 비행 방향과 기본 각도 보존
            }

            float yaw = look.x; // 현재 프레임의 좌우 선회 입력
            if (broom != null) // 실전 씬의 장착 효과
            {
                float yawLimit = (360f + PartBonus(FlightBroomStat.Turn)) * seconds; // 초당 최대 선회 속도
                yaw = Mathf.Clamp(yaw, -yawLimit, yawLimit); // 프레임 길이 기준 회전 제한
            }

            transform.Rotate(0f, yaw, 0f, Space.World); // 마우스 선회와 수평 진행 방향
            pitch = Mathf.Clamp(pitch - look.y, -45f, 45f); // 위아래 조준 각도
            orbitYaw = Mathf.MoveTowards(orbitYaw, 0f, 250f * seconds); // 좌우 시점 복귀
            orbitPitch = Mathf.MoveTowards(orbitPitch, 0f, 180f * seconds); // 상하 시점 복귀
        }

        private void MoveWithinBounds(Vector3 step) // 시험 체적과 지형 충돌 처리
        {
            float distance = step.magnitude; // 현재 프레임의 이동 거리
            if (distance > 0.0001f && Physics.SphereCast(transform.position, 0.28f, step / distance, out RaycastHit hit, distance, environmentLayer, QueryTriggerInteraction.Ignore)) // 비행체 앞의 지형 검사
            {
                step *= Mathf.Max(0f, hit.distance - 0.025f) / distance; // 지형 표면 앞에서 정지
                velocity = Vector3.ProjectOnPlane(velocity, hit.normal); // 표면을 향한 관성 제거
            }

            Vector3 candidate = transform.position + step; // 지형 접촉 이후의 이동 목표
            Bounds activeBounds = flightVolume != null ? flightVolume.bounds : flightBounds; // 씬 전투 구역 또는 기존 시험 체적
            Vector3 bounded = activeBounds.ClosestPoint(candidate); // 비행 체적 안의 안전한 위치
            if (bounded.x != candidate.x) // 좌우 경계 접촉
            {
                velocity.x = 0f; // 좌우 경계의 관성 제거
            }
            if (bounded.y != candidate.y) // 고도 경계 접촉
            {
                velocity.y = 0f; // 고도 경계의 관성 제거
            }
            if (bounded.z != candidate.z) // 앞뒤 경계 접촉
            {
                velocity.z = 0f; // 앞뒤 경계의 관성 제거
            }
            transform.position = bounded; // 실제 비행체 위치 적용
        }
    }
}
