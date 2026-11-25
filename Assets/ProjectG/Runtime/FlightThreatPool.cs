using System.Collections.Generic; // 활성 적탄과 회수 목록
using UnityEngine; // 씬에서 지정하는 풀 크기와 실제 위치

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(110)] // 기체 이동과 카메라 이후에 탄막 판정
    [DisallowMultipleComponent] // 풀 오브젝트 한 곳에서 적탄 관리
    public sealed class FlightThreatPool : MonoBehaviour // 활성 탄막의 재사용과 일괄 갱신
    {
        [SerializeField] private FlightThreatBolt template; // 실제 적탄 프리팹
        [SerializeField] private FlightPilotVitals target; // 현재 비행체의 피격 상태
        [SerializeField, Min(1)] private int capacity = 128; // 활성 및 전체 생성의 상한
        [SerializeField, Min(0)] private int warmCount = 24; // 출격 처음에 준비할 적탄 수
        private List<FlightThreatBolt> active; // 발사 순서로 움직이는 실제 탄
        private Stack<FlightThreatBolt> idle; // 즉시 재사용할 비활성 탄
        private Transform targetPosition; // 한 프레임에 한 번만 읽을 비행체 위치

        public int Capacity => capacity; // 편집 가능한 탄 수 한도
        public int ActiveCount => active.Count; // 현재 이동하는 탄 수
        public int CreatedCount => active.Count + idle.Count; // 생성 개체의 전체 수

        private void Awake() // 현재 씬의 미리 생성 수
        {
            active = new List<FlightThreatBolt>(capacity); // 활성 개체 증가 때 추가 할당 방지
            idle = new Stack<FlightThreatBolt>(capacity); // 사용한 탄 회수 공간 사전 준비
            targetPosition = target.transform; // 다음 프레임의 기체 위치 캐시
            int count = Mathf.Min(capacity, warmCount); // 전체 한도를 넘지 않는 초기 크기
            for (int index = 0; index < count; index++) // 출격 전에 먼저 재사용할 탄
            {
                idle.Push(CreateBolt()); // 비활성 상태에서 적탄 배치
            }
        }

        private void LateUpdate() // 활성 탄만 같은 표본 위치로 갱신
        {
            if (active.Count == 0 || Time.timeScale == 0f) // 탄이 없거나 메뉴에서 시간이 정해진 상태
            {
                return; // 물리 이동 없이 현재 장면 유지
            }

            Vector3 currentPilot = targetPosition.position; // 이 프레임의 공통 기체 좌표
            float elapsed = Time.deltaTime; // 같은 프레임 시간 한 번만 조회
            int index = 0; // 첫 활성 탄부터 같은 발사 순서
            while (index < active.Count) // 회수하지 않은 다음 탄도 처리
            {
                if (!target.IsDefeated && active[index].Advance(target, currentPilot, elapsed)) // 피격·지형·수명 후 계속 가능한 탄
                {
                    index++; // 기존 배열에서 다음 탄
                }
                else
                {
                    Recycle(index); // 뺀 자리에 온 다음 탄을 이어서 검사
                }
            }
        }

        public bool TryFire(Vector3 position, Vector3 direction, float speed, int damage = 0) // 직접 발사와 훈련의 같은 기본탄
        {
            return TryFire(position, direction, speed, damage, FlightThreatPattern.Aimed); // 주황 조준탄 기본 상태
        }

        internal bool TryFire(Vector3 position, Vector3 direction, float speed, int damage, FlightThreatPattern pattern) // 형태와 이동을 포함한 실제 적탄 발사
        {
            if (target.IsDefeated || active.Count >= capacity || speed <= 0f || direction.sqrMagnitude < 0.0001f) // 살아 있는 기체와 유효한 속도·방향
            {
                return false; // 잘못된 탄창 또는 용량 초과를 건너뜀
            }

            FlightThreatBolt bolt = idle.Count > 0 ? idle.Pop() : CreateBolt(); // 이미 회수한 물리 객체 우선 사용
            bolt.Location.SetPositionAndRotation(position, Quaternion.LookRotation(direction)); // 적탄의 출발 위치와 실제 방향
            bolt.Begin(targetPosition.position, speed, damage, pattern); // 현재 기체 위치와 새 탄의 수명·외형
            active.Add(bolt); // 활성 목록에서 같은 발사 순서 유지
            bolt.View.SetActive(true); // 기존 씬 오브젝트 표시
            return true; // 새 탄 발사 확인
        }

        public void ReturnAll() // 재출격 또는 보스 패턴 종료의 일괄 회수
        {
            for (int index = active.Count - 1; index >= 0; index--) // 활성 탄의 뒤부터 처리
            {
                Recycle(index); // 같은 활성 탄을 두 번 회수하지 않음
            }
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
