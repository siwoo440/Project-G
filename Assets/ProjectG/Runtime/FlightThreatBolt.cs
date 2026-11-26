using UnityEngine; // 빠른 적탄의 월드 이동

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightThreatBolt : MonoBehaviour // 3차원 적탄 프리팹
    {
        [System.Serializable] // 재사용 탄에 입힐 패턴별 에셋
        private struct Appearance // 프리팹에서 조정하는 형태와 표면
        {
            public Mesh mesh; // 미리 만든 삼각형 메시
            public Material material; // 위협을 구분하는 색과 발광
            public Vector3 scale; // 판정 반경과 분리된 표시 크기
        }

        [SerializeField] private MeshFilter renderMesh; // 씬에서 보이는 탄의 메시
        [SerializeField] private MeshRenderer renderSurface; // 탄의 공유 재질
        [SerializeField] private Appearance[] appearances = System.Array.Empty<Appearance>(); // 패턴 열거 순서의 에셋
        private FlightThreatPattern? lastAppearance; // 이전 발사에 적용한 외형
        [SerializeField, Min(0.01f)] private float radius = 0.10f; // 시험 적탄의 실제 반경
        [SerializeField, Min(1)] private int damage = 20; // 시험 적탄의 피해
        [SerializeField, Min(0.01f)] private float range = 130f; // 적탄의 최대 진행 거리
        [SerializeField, Min(0.01f)] private float lifetime = 8f; // 적탄의 최대 활성 시간
        private Transform visual; // 외형을 편집하는 자식의 위치와 크기
        private Vector3 previousPilot; // 이전 프레임의 판정 중심
        private float speed; // 초당 이동 거리
        private int impactDamage; // 현재 이 탄환의 실제 피해량
        private float remaining; // 사용 중 남은 월드 거리
        private float secondsLeft; // 사용 중 남은 프레임 시간
        private bool nearCandidate; // 현재까지 근접 구간 진입 여부
        private bool grazeAwarded; // 상대 기체에 대한 중복 그레이즈 방지

        internal Transform Location { get; private set; } // 프리팹의 실제 위치와 방향
        internal GameObject View { get; private set; } // 풀에서 켜고 끄는 표시 오브젝트
        internal float Radius => radius; // 적탄 소거의 실제 반경
        internal bool CanCancel { get; private set; } // 제작 마법 상쇄 허용 여부
        internal bool CanAbsorb { get; private set; } // 제작 마법 흡수 허용 여부

        internal void CacheView() // 탄환을 생성할 때만 오브젝트 참조 조회
        {
            Location = transform; // 월드 이동과 경로 회수에서 쓰는 좌표
            View = gameObject; // 활성화 상태가 필요할 때 쓰는 표시
            visual = renderMesh != null ? renderMesh.transform : null; // 패턴이 바뀔 때만 사용하는 자식 위치
        }

        internal void Begin(Vector3 startPilot, float travelSpeed, int shotDamage, FlightThreatPattern pattern, bool canCancel, bool canAbsorb) // 실제 발사된 적탄 초기화
        {
            CanCancel = canCancel; // 재사용 탄의 상쇄 허용 초기화
            CanAbsorb = canAbsorb; // 재사용 탄의 독립 흡수 허용 초기화
            remaining = range; // 이전 사용에서 소진한 거리 복구
            secondsLeft = lifetime; // 새 탄의 사용 시간 초기화
            nearCandidate = false; // 이전 탄의 근접 후보 초기화
            grazeAwarded = false; // 재사용 탄의 새 그레이즈 가능 상태
            previousPilot = startPilot; // 탄환 출발 때 기체 좌표
            speed = travelSpeed; // 발사 시 지정한 시험 속도
            impactDamage = shotDamage > 0 ? shotDamage : damage; // 새 발사의 지정 피해 또는 원본 피해
            UpdateAppearance(pattern); // 재사용 탄을 지금 발사한 패턴에 맞게 갱신
        }

        private void UpdateAppearance(FlightThreatPattern pattern) // 실제 판정과 분리된 공유 외형 적용
        {
            int index = (int)pattern; // 프리팹 배열에 대응하는 패턴 순서
            if (lastAppearance == pattern || (uint)index >= appearances.Length || renderMesh == null || renderSurface == null) // 같은 상태 또는 구형 편집 원본 확인
            {
                return; // 재질을 복제하거나 판정을 바꾸지 않는 경로
            }
            Appearance look = appearances[index]; // 시각 자식에 적용할 세 속성
            renderMesh.sharedMesh = look.mesh; // 원본 에셋 참조 그대로 사용
            renderSurface.sharedMaterial = look.material; // 새 재질 인스턴스 생성 방지
            visual.localScale = look.scale; // 3D 화면에서 패턴별 다른 폭과 길이
            lastAppearance = pattern; // 같은 종류로 회수한 탄의 중복 작업 제거
        }

        internal bool Advance(FlightPilotVitals vitals, Vector3 currentPilot, float elapsed) // 현재 경로의 적탄이 계속 살아 있는지 판정
        {
            Vector3 start = Location.position; // 적탄의 이전 프레임 위치
            Vector3 forward = Location.forward; // 지형 충돌과 끝점에 동일한 방향
            float duration = Mathf.Min(secondsLeft, elapsed); // 현재 탄환이 진행할 유효 시간
            float step = Mathf.Min(remaining, speed * duration); // 수명·사거리 안의 실제 이동량
            bool terrain = Physics.SphereCast(start, radius, forward, out RaycastHit obstruction, step, 1 << 8, QueryTriggerInteraction.Ignore); // 하늘섬에 막히는 지점
            float travel = terrain ? obstruction.distance : step; // 지형 앞까지 실제 이동 거리
            Vector3 end = start + forward * travel; // 이번 프레임의 실제 탄환 끝점
            float portion = elapsed > 0f ? travel / (speed * elapsed) : 0f; // 이번 프레임에서 실제로 움직인 시간 비율
            Vector3 pilotAtEnd = Vector3.Lerp(previousPilot, currentPilot, portion); // 탄환이 지형에 도달한 때의 기체 위치
            bool hit = vitals.ResolveThreat(start - previousPilot, end - pilotAtEnd, radius, impactDamage, ref nearCandidate, ref grazeAwarded); // 체력과 그레이즈 판정
            if (hit || terrain || remaining <= step || secondsLeft <= duration) // 접촉이나 거리·시간 만료 처리
            {
                return false; // 풀의 활성 목록에서 회수
            }

            remaining -= step; // 남은 월드 사거리
            secondsLeft -= duration; // 남은 활성 시간
            previousPilot = currentPilot; // 다음 상대 경로의 기체 출발점
            Location.position = end; // 실제 적탄 이동
            return true; // 다음 프레임의 계속 이동
        }
    }
}
