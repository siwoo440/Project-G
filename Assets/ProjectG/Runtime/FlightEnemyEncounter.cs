using UnityEngine; // 일반 마물 시험 씬
using UnityEngine.InputSystem; // 전투 시험 시작 키

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightEnemyEncounter : MonoBehaviour // 마물 3종의 반복 시험
    {
        [SerializeField] private FlightPilotVitals pilot; // 실제 플레이어 생존 상태
        [SerializeField] private FlightThreatPool pool; // 마물들이 함께 쓰는 적탄 풀
        [SerializeField] private FlightEnemyRoute[] routes; // 씬에서 조정하는 마물 경로

        private void Update() // 연습 시나리오의 입력 확인
        {
            Keyboard keyboard = Keyboard.current; // 현재 입력 장치
            if (pilot.IsDefeated || (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)) // 실패와 출격 재시작
            {
                StopTrial(); // 씬의 마물 초기화
                return; // 중단 상태의 시험 시작 차단
            }
            if (keyboard != null && keyboard.yKey.wasPressedThisFrame) // 세 마물 동시 출현 시험
            {
                StartTrial(); // 실제 출현 위치와 체력 복구
            }
        }

        public void StartTrial() // 실제 마물 출현 동작
        {
            StopTrial(); // 이전 마물의 상태 정리
            pool.ReturnAll(); // 이전 연습 탄막 정리
            foreach (FlightEnemyRoute route in routes) // 조준형·확산형·돌진형 순회
            {
                route.Begin(pool, pilot.transform); // 마물의 실제 입체 경로 활성화
            }
        }

        private void StopTrial() // 모든 마물 코스 중단
        {
            foreach (FlightEnemyRoute route in routes) // 표시 중인 시험 마물 확인
            {
                route.gameObject.SetActive(false); // 마물의 피해와 이동을 일시 종료
            }
        }
    }
}
