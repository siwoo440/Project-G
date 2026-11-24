using System.Collections.Generic; // 적탄 원본과 회수된 객체
using UnityEngine; // 편집 가능한 풀과 생성 위치

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 풀 오브젝트 한 곳에서 탄 관리
    public sealed class FlightThreatPool : MonoBehaviour // 상한이 있는 적탄 재사용 풀
    {
        [SerializeField] private FlightThreatBolt template; // 실제 적탄 프리팹
        [SerializeField] private FlightPilotVitals target; // 현재 피격 대상
        [SerializeField, Min(1)] private int capacity = 128; // 동시 활성 및 전체 생성 상한
        [SerializeField, Min(0)] private int warmCount = 24; // 출격 시작에 준비할 탄 수
        private readonly List<FlightThreatBolt> all = new List<FlightThreatBolt>(); // 생성한 탄 전체
        private readonly Stack<FlightThreatBolt> idle = new Stack<FlightThreatBolt>(); // 즉시 재사용할 비활성 탄

        public int Capacity => capacity; // 씬에서 지정한 테스트 상한
        public int ActiveCount { get; private set; } // 현재 실행 중인 적탄 수
        public int CreatedCount => all.Count; // 실제 생성된 적탄 수

        private void Awake() // 시험 시작 때 일부 적탄 준비
        {
            int count = Mathf.Min(capacity, warmCount); // 풀 상한 안의 준비 수
            for (int index = 0; index < count; index++) // 사전 생성할 적탄 반복
            {
                idle.Push(CreateBolt()); // 비활성 예비 탄 추가
            }
        }

        public bool TryFire(Vector3 position, Vector3 direction, float speed) // 지정 방향의 적탄 한 발
        {
            if (target.IsDefeated || ActiveCount >= capacity || speed <= 0f || direction.sqrMagnitude < 0.0001f) // 비행 상태·동시 상한·입력 검사
            {
                return false; // 초과 탄은 생성하지 않음
            }

            FlightThreatBolt bolt = idle.Count > 0 ? idle.Pop() : CreateBolt(); // 회수한 객체 우선 사용
            bolt.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction)); // 탄환 출발 위치와 진행 방향
            bolt.Begin(this, target, speed); // 모든 수명과 판정 데이터 초기화
            ActiveCount++; // 활성 탄 수 증가
            bolt.gameObject.SetActive(true); // 실제 탄환 진행 시작
            return true; // 발사 성공 여부
        }

        internal void Return(FlightThreatBolt bolt) // 종료된 적탄 회수
        {
            if (!bolt.gameObject.activeSelf) // 다음 프레임의 중복 회수 방지
            {
                return; // 비활성 탄은 이미 풀에 포함
            }
            bolt.gameObject.SetActive(false); // 탄환 업데이트와 표시 종료
            ActiveCount--; // 활성 개체 수 감소
            idle.Push(bolt); // 다음 패턴에 재사용할 탄
        }

        public void ReturnAll() // 출격 재시작과 패턴 시나리오 초기화
        {
            foreach (FlightThreatBolt bolt in all) // 모든 생성 탄 확인
            {
                Return(bolt); // 활성 상태인 탄만 회수
            }
        }

        public int ClearInView(Camera view) // 화면 안에서 지울 수 있는 적탄
        {
            int count = 0; // 실제 소거 수
            foreach (FlightThreatBolt bolt in all) // 생성한 적탄의 화면 좌표
            {
                if (!bolt.gameObject.activeSelf) // 회수된 탄은 제외
                {
                    continue; // 활성 탄만 확인
                }
                Vector3 screen = view.WorldToViewportPoint(bolt.transform.position); // 카메라 앞의 단위 화면 위치
                if (screen.z >= view.nearClipPlane && screen.z <= view.farClipPlane && screen.x >= 0f && screen.x <= 1f && screen.y >= 0f && screen.y <= 1f) // 실제 화면 범위
                {
                    Return(bolt); // 적탄 풀로 회수
                    count++; // 해당 탄의 소거 기록
                }
            }
            return count; // 실제 소거 결과
        }

        public void ClearAlongBeam(Vector3 start, Vector3 direction, float length, float radius) // 광선의 지형 앞 범위만 소거
        {
            foreach (FlightThreatBolt bolt in all) // 재사용 중인 모든 적탄 검사
            {
                if (!bolt.gameObject.activeSelf) // 이미 회수된 적탄 제외
                {
                    continue; // 활성 적탄만 범위 확인
                }
                Vector3 offset = bolt.transform.position - start; // 총구에서 적탄까지의 위치
                float distance = Vector3.Dot(offset, direction); // 광선 축 위의 투영 거리
                float combinedRadius = radius + bolt.Radius; // 광선과 탄환의 실제 접촉 범위
                if (distance >= 0f && distance <= length && (offset - direction * distance).sqrMagnitude <= combinedRadius * combinedRadius) // 막힘 앞 원통과 접촉
                {
                    Return(bolt); // 광선에 닿은 탄만 풀에 회수
                }
            }
        }

        private FlightThreatBolt CreateBolt() // 테스트 프리팹의 객체 생성
        {
            FlightThreatBolt bolt = Instantiate(template, transform); // Hierarchy의 풀 아래 배치
            bolt.gameObject.SetActive(false); // 발사를 기다리는 비활성 상태
            all.Add(bolt); // 전체 객체 이력에 추가
            return bolt; // 사용할 적탄 인스턴스
        }
    }
}
