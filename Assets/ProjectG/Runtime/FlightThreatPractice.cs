using UnityEngine; // 실제 시험 씬과 비행 위치
using UnityEngine.InputSystem; // 적탄 연습 키 입력
using UnityEngine.UI; // 제작 안내판 내부의 체력 글자

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    public sealed class FlightThreatPractice : MonoBehaviour // 출격 중 적탄 연습과 재시작
    { // 연습 시작
        [SerializeField] private FlightPilotVitals vitals; // 씬 비행체의 판정 상태
        [SerializeField] private FirstFlightController pilot; // 재시작 위치를 적용할 비행체
        [SerializeField] private FlightThreatBolt threat; // 편집 가능한 시험 적탄
        [SerializeField] private Text status; // 기존 제작 안내판의 체력 문구
        private Vector3 spawnPoint; // 씬에 배치한 출격 위치
        private Quaternion spawnRotation; // 씬에 배치한 최초 진행 방향
        private float nextTrial; // 다음 연습 적탄 발사 시각
        private bool active; // 연습 탄막 토글 여부
        private bool offset; // 명중탄과 그레이즈 후보의 교대

        private void Start() // 씬의 초기 출격 설정
        { // 준비 시작
            spawnPoint = pilot.transform.position; // 실제 씬 초기 위치 보존
            spawnRotation = pilot.transform.rotation; // 최초 진행 방향 보존
        } // 준비 종료

        private void Update() // 훈련 키와 적탄 처리
        { // 입력 시작
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) // 현재 출격 다시 시작
            { // 재시작 시작
                RestartRun(); // 적탄과 체력 및 카메라 복귀
            } // 재시작 종료
            if (!vitals.IsDefeated && keyboard != null && keyboard.tKey.wasPressedThisFrame) // 출격 적탄 연습 토글
            { // 연습 전환 시작
                active = !active; // 진행 상태 전환
                nextTrial = Time.time; // 활성화 시 첫 발 즉시 준비
            } // 연습 전환 종료
            if (!vitals.IsDefeated && active && Time.time >= nextTrial) // 시험 간격에 따른 적탄 발생
            { // 발사 시작
                Transform body = pilot.transform; // 현재 비행체의 좌표와 방향
                Vector3 lateral = offset ? body.right * 0.7f : Vector3.zero; // 명중과 스침을 비교할 옆 거리
                EmitTrial(body.position + body.forward * 18f + lateral, -body.forward, 15f); // 플레이어 정면에서 오는 시험탄
                offset = !offset; // 명중 연습과 근접 연습 교대
                nextTrial = Time.time + 2f; // 다음 단발까지의 시험 간격
            } // 발사 종료
        } // 입력 종료

        private void LateUpdate() // 상태 글자의 현재 출격 표시
        { // 표시 시작
            status.text = vitals.IsDefeated ? "FLIGHT FAILED   |   R  Restart" : $"HP {vitals.Health} / {vitals.MaxHealth}      GRAZE {vitals.Grazes}      T  Practice {(active ? "ON" : "OFF")}     R  Restart"; // 체력과 현재 연습 표시
        } // 표시 종료

        public void EmitTrial(Vector3 position, Vector3 direction, float speed) // 직접 편집한 시험 탄으로 판정 재현
        { // 발사 시작
            FlightThreatBolt bolt = Instantiate(threat, position, Quaternion.LookRotation(direction), transform); // 지정 위치에서 적탄 프리팹 복제
            bolt.Begin(vitals, speed); // 현재 비행체와 시험 속도 연결
        } // 발사 종료

        private void RestartRun() // 현 출격을 초기 상태로 되돌리기
        { // 초기화 시작
            active = false; // 다음 출격의 적탄 연습 중지
            offset = false; // 처음에 조준탄부터 시작
            foreach (FlightThreatBolt bolt in GetComponentsInChildren<FlightThreatBolt>()) // 남은 적탄 모두 조회
            { // 남은 탄환 회수 시작
                bolt.enabled = false; // 이번 프레임 추가 충돌 방지
                Destroy(bolt.gameObject); // 이전 출격 탄환 제거
            } // 남은 탄환 회수 종료
            vitals.ResetRun(); // 최대 체력과 그레이즈 복구
            pilot.ResetFlightPose(spawnPoint, spawnRotation); // 출격 위치와 카메라 시점 복구
        } // 초기화 종료
    } // 연습 종료
} // 영역 종료
