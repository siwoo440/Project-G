using UnityEngine; // 실제 시험 씬과 비행 위치
using UnityEngine.InputSystem; // 적탄 연습 키 입력
using UnityEngine.UI; // 제작 안내판 내부의 체력 글자

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    public sealed class FlightThreatPractice : MonoBehaviour // 출격 중 적탄 연습과 재시작
    { // 연습 시작
        [SerializeField] private FlightPilotVitals vitals; // 씬 비행체의 판정 상태
        [SerializeField] private FirstFlightController pilot; // 재시작 위치를 적용할 비행체
        [SerializeField] private FlightThreatPool pool; // 씬에서 편집할 적탄 재사용 풀
        private FlightThreatPattern pattern; // 현재 선택한 입체 탄막
        private float phase; // 순차적으로 바뀌는 나선 각도
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
            if (keyboard != null) // 키보드 선택 입력 확인
            { // 패턴 선택 시작
                ChoosePattern(keyboard); // 숫자 1~4의 패턴 전환
            } // 패턴 선택 종료
            if (!vitals.IsDefeated && keyboard != null && keyboard.tKey.wasPressedThisFrame) // 출격 적탄 연습 토글
            { // 연습 전환 시작
                active = !active; // 진행 상태 전환
                nextTrial = Time.time; // 활성화 시 첫 발 즉시 준비
            } // 연습 전환 종료
            if (!vitals.IsDefeated && active && Time.time >= nextTrial) // 시험 간격에 따른 적탄 발생
            { // 발사 시작
                Transform body = pilot.transform; // 현재 비행체의 좌표와 방향
                bool aimed = pattern == FlightThreatPattern.Aimed; // 조준탄의 스침 테스트 활성 여부
                Vector3 lateral = aimed && offset ? body.right * 0.7f : Vector3.zero; // 조준탄의 명중과 스침 비교
                EmitPattern(pattern, body.position + body.forward * 18f + lateral, -body.forward); // 정면 기준 입체 탄막
                offset = !offset; // 조준 모드의 근접탄 교대
                nextTrial = Time.time + (pattern == FlightThreatPattern.Spiral ? 0.32f : 1.8f); // 나선이 이어질 간격
            } // 발사 종료
        } // 입력 종료

        private void LateUpdate() // 상태 글자의 현재 출격 표시
        { // 표시 시작
            status.text = vitals.IsDefeated ? "FLIGHT FAILED   |   R  Restart" : $"GRAZE {vitals.Grazes}      T  {pattern} {(active ? "ON" : "OFF")}    1-4  Select    R  Restart"; // 체력과 현재 연습 표시
        } // 표시 종료

        public void EmitTrial(Vector3 position, Vector3 direction, float speed) // 직접 편집한 시험 탄으로 판정 재현
        { // 발사 시작
            pool.TryFire(position, direction, speed); // 시험 수치로 실제 풀에서 발사
        } // 발사 종료

        public int EmitPattern(FlightThreatPattern selection, Vector3 position, Vector3 direction) // 씬 또는 적의 입체 패턴 발사
        { // 패턴 발사 시작
            int fired = FlightThreatPatterns.Fire(pool, selection, position, direction, phase); // 현재 회전과 활성 탄수 반영
            if (fired > 0 && selection == FlightThreatPattern.Spiral) // 성공한 나선 발사의 회전 진행
            { // 회전 시작
                phase = (phase + 27f) % 360f; // 다음 네 갈래의 시작 각도
            } // 회전 종료
            return fired; // 실제 생성된 입체 탄막 개수
        } // 패턴 발사 종료

        private void ChoosePattern(Keyboard keyboard) // 1~4 숫자 키로 탄막 배열 선택
        { // 선택 시작
            if (keyboard.digit1Key.wasPressedThisFrame) // 1번 조준
            { // 1번 입력 시작
                pattern = FlightThreatPattern.Aimed; // 현재 조준 패턴
            } // 1번 입력 종료
            else if (keyboard.digit2Key.wasPressedThisFrame) // 2번 부채꼴
            { // 2번 입력 시작
                pattern = FlightThreatPattern.Fan; // 현재 부채꼴 패턴
            } // 2번 입력 종료
            else if (keyboard.digit3Key.wasPressedThisFrame) // 3번 입체 고리
            { // 3번 입력 시작
                pattern = FlightThreatPattern.Ring; // 현재 고리 패턴
            } // 3번 입력 종료
            else if (keyboard.digit4Key.wasPressedThisFrame) // 4번 회전 나선
            { // 4번 입력 시작
                pattern = FlightThreatPattern.Spiral; // 현재 나선 패턴
            } // 4번 입력 종료
        } // 선택 종료

        private void RestartRun() // 현 출격을 초기 상태로 되돌리기
        { // 초기화 시작
            active = false; // 다음 출격의 적탄 연습 중지
            offset = false; // 처음에 조준탄부터 시작
            phase = 0f; // 다음 출격의 나선 각도 초기화
            pool.ReturnAll(); // 이전 출격의 활성 적탄을 모두 회수
            vitals.ResetRun(); // 최대 체력과 그레이즈 복구
            pilot.ResetFlightPose(spawnPoint, spawnRotation); // 출격 위치와 카메라 시점 복구
        } // 초기화 종료
    } // 연습 종료
} // 영역 종료
