using UnityEngine; // 일반 마물 시험 씬
using UnityEngine.InputSystem; // 전투 시험 시작 키

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightEnemyEncounter : MonoBehaviour // 마물 3종의 반복 시험
    {
        [SerializeField] private FlightPilotVitals pilot; // 실제 플레이어 생존 상태
        [SerializeField] private FlightThreatPool pool; // 마물들이 함께 쓰는 적탄 풀
        [SerializeField] private FlightEnemyRoute[] routes; // 씬에서 조정하는 마물 경로
        [SerializeField] private FlightEnemy openingEnemy; // 출격 직후 첫 웨이브의 시험 표적
        [SerializeField] private FlightBindings bindings; // 설정에서 지정한 웨이브 키
        private Vector3 openingPosition; // 재출격할 때 첫 적이 나타날 위치
        private Quaternion openingRotation; // 첫 적의 원래 비행 방향

        public int WaveNumber { get; private set; } = 1; // 시작 웨이브 번호
        public float ElapsedSeconds { get; private set; } // 설정 선택을 제외한 출격 경과 시간

        public int ActiveEnemyCount // 현재 웨이브에서 아직 보이는 적 수
        {
            get // 실제 활성 개체만 합산
            {
                if (WaveNumber == 1) // 시작 시험 표적
                {
                    return openingEnemy != null && openingEnemy.gameObject.activeInHierarchy ? 1 : 0; // 처치 후 실제 표시
                }

                int count = 0; // 경로형 웨이브의 활성 개체 누계
                foreach (FlightEnemyRoute route in routes) // 씬의 세 경로
                {
                    if (route.gameObject.activeInHierarchy) // 아직 처치·이탈하지 않은 적
                    {
                        count++; // 현재 웨이브에 남아 있는 수
                    }
                }
                return count; // 표시가 참조할 실제 값
            }
        }

        private void Awake() // 첫 웨이브의 배치 기억
        {
            if (openingEnemy != null) // 편집 가능한 시작 표적
            {
                openingPosition = openingEnemy.transform.position; // 초기 등장 위치
                openingRotation = openingEnemy.transform.rotation; // 초기 회전
            }
        }

        private void Update() // 연습 시나리오의 입력 확인
        {
            Keyboard keyboard = Keyboard.current; // 현재 입력 장치
            if (Time.timeScale == 0f) // 카드 선택 중 전투 명령 동결
            {
                return; // 웨이브를 시작하지 않는 시간
            }
            if (pilot.IsDefeated) // 출격 실패면 전투 시간 중단
            {
                StopTrial(); // 씬의 마물 초기화
                return; // 중단 상태의 시험 시작 차단
            }
            ElapsedSeconds += Time.deltaTime; // 출격 경과 시간 누적
            if (keyboard != null && bindings.Pressed(keyboard, FlightAction.Wave)) // 설정한 시험 웨이브 시작
            {
                StartTrial(); // 실제 출현 위치와 체력 복구
            }
        }

        public void StartTrial() // 실제 마물 출현 동작
        {
            StopTrial(); // 이전 마물의 상태 정리
            WaveNumber++; // 새로운 시험 웨이브 번호
            if (openingEnemy != null) // 시작 표적이 연결된 씬
            {
                openingEnemy.gameObject.SetActive(false); // 현재 웨이브 집계에서 정리
            }
            pool.ReturnAll(); // 이전 연습 탄막 정리
            foreach (FlightEnemyRoute route in routes) // 조준형·확산형·돌진형 순회
            {
                route.Begin(pool, pilot.transform); // 마물의 실제 입체 경로 활성화
            }
        }

        internal void RestartTrial() // 시험 출격을 다시 시작할 때
        {
            StopTrial(); // 이전 웨이브 정리
            WaveNumber = 1; // 첫 웨이브
            ElapsedSeconds = 0f; // 첫 타이머
            if (openingEnemy != null) // 저장된 시작 표적
            {
                openingEnemy.gameObject.SetActive(false); // 기존 적 체력 초기화 준비
                openingEnemy.transform.SetPositionAndRotation(openingPosition, openingRotation); // 시작 위치와 방향
                openingEnemy.gameObject.SetActive(true); // 새 출격의 첫 적 복귀
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
