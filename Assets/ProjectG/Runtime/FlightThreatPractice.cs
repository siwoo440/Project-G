using UnityEngine; // 실제 시험 씬과 비행 위치
using UnityEngine.InputSystem; // 적탄 연습 키 입력
using UnityEngine.Serialization; // 기존 실패 안내 참조 유지
using UnityEngine.UI; // 제작 안내판 내부의 체력 글자

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightThreatPractice : MonoBehaviour // 출격 중 적탄 연습과 재시작
    {
        [SerializeField] private FlightPilotVitals vitals; // 씬 비행체의 판정 상태
        [SerializeField] private FirstFlightController pilot; // 재시작 위치를 적용할 비행체
        [SerializeField] private FlightThreatPool pool; // 씬에서 편집할 적탄 재사용 풀
        [SerializeField] private FlightExperience experience; // 이번 출격의 레벨과 드롭
        [SerializeField] private FlightUpgradeChoices upgrades; // 출격별 카드 선택 초기화
        [SerializeField] private FlightBindings bindings; // 실제 탄막 연습 키 설정
        [SerializeField] private FlightEnhancements enhancements; // 이번 출격의 드롭과 장착 보조 장비
        [SerializeField] private FlightScoreHud score; // 이번 출격의 점수와 획득 숫자
        private FlightThreatPattern pattern; // 현재 선택한 입체 탄막
        private float phase; // 순차적으로 바뀌는 나선 각도
        [SerializeField, FormerlySerializedAs("status")] private Text failureNotice; // 출격에 실패했을 때만 표시
        private Vector3 spawnPoint; // 씬에 배치한 출격 위치
        private Quaternion spawnRotation; // 씬에 배치한 최초 진행 방향
        private float nextTrial; // 다음 연습 적탄 발사 시각
        private bool active; // 연습 탄막 토글 여부
        private bool offset; // 명중탄과 그레이즈 후보의 교대

        private void Start() // 씬의 초기 출격 설정
        {
            spawnPoint = pilot.transform.position; // 실제 씬 초기 위치 보존
            spawnRotation = pilot.transform.rotation; // 최초 진행 방향 보존
        }

        private void Update() // 훈련 키와 적탄 처리
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (Time.timeScale == 0f) // 레벨업 카드 선택 중 유지
            {
                return; // 연습 패턴과 발사 중지
            }
            if (keyboard != null) // 키보드 선택 입력 확인
            {
                ChoosePattern(keyboard); // 숫자 1~4의 패턴 전환
            }
            if (!vitals.IsDefeated && keyboard != null && bindings.Pressed(keyboard, FlightAction.Trial)) // 설정한 적탄 연습 토글
            {
                active = !active; // 진행 상태 전환
                nextTrial = Time.time; // 활성화 시 첫 발 즉시 준비
            }
            if (!vitals.IsDefeated && active && Time.time >= nextTrial) // 시험 간격에 따른 적탄 발생
            {
                Transform body = pilot.transform; // 현재 비행체의 좌표와 방향
                bool aimed = pattern == FlightThreatPattern.Aimed; // 조준탄의 스침 테스트 활성 여부
                Vector3 lateral = aimed && offset ? body.right * 0.7f : Vector3.zero; // 조준탄의 명중과 스침 비교
                EmitPattern(pattern, body.position + body.forward * 18f + lateral, -body.forward); // 정면 기준 입체 탄막
                offset = !offset; // 조준 모드의 근접탄 교대
                nextTrial = Time.time + (pattern == FlightThreatPattern.Spiral ? 0.32f : 1.8f); // 나선이 이어질 간격
            }
        }

        private void LateUpdate() // 진행 중 설명 패널 없이 출격 실패만 표시
        {
            failureNotice.enabled = vitals.IsDefeated; // 실제 실패 상태에서만 한 줄 표시
        }

        public void EmitTrial(Vector3 position, Vector3 direction, float speed) // 직접 편집한 시험 탄으로 판정 재현
        {
            pool.TryFire(position, direction, speed); // 시험 수치로 실제 풀에서 발사
        }

        public int EmitPattern(FlightThreatPattern selection, Vector3 position, Vector3 direction) // 씬 또는 적의 입체 패턴 발사
        {
            int fired = FlightThreatPatterns.Fire(pool, selection, position, direction, phase); // 현재 회전과 활성 탄수 반영
            if (fired > 0 && selection == FlightThreatPattern.Spiral) // 성공한 나선 발사의 회전 진행
            {
                phase = (phase + 27f) % 360f; // 다음 네 갈래의 시작 각도
            }
            return fired; // 실제 생성된 입체 탄막 개수
        }

        private void ChoosePattern(Keyboard keyboard) // 1~4 숫자 키로 탄막 배열 선택
        {
            if (keyboard.digit1Key.wasPressedThisFrame) // 1번 조준
            {
                pattern = FlightThreatPattern.Aimed; // 현재 조준 패턴
            }
            else if (keyboard.digit2Key.wasPressedThisFrame) // 2번 부채꼴
            {
                pattern = FlightThreatPattern.Fan; // 현재 부채꼴 패턴
            }
            else if (keyboard.digit3Key.wasPressedThisFrame) // 3번 입체 고리
            {
                pattern = FlightThreatPattern.Ring; // 현재 고리 패턴
            }
            else if (keyboard.digit4Key.wasPressedThisFrame) // 4번 회전 나선
            {
                pattern = FlightThreatPattern.Spiral; // 현재 나선 패턴
            }
        }

        internal void RestartRun() // 현 출격을 초기 상태로 되돌리기
        {
            active = false; // 다음 출격의 적탄 연습 중지
            offset = false; // 처음에 조준탄부터 시작
            phase = 0f; // 다음 출격의 나선 각도 초기화
            pool.ReturnAll(); // 이전 출격의 활성 적탄을 모두 회수
            vitals.ResetRun(); // 최대 체력과 그레이즈 복구
            pilot.ResetFlightPose(spawnPoint, spawnRotation); // 출격 위치와 카메라 시점 복구
            experience?.ResetRun(); // 이전 출격의 레벨과 조각 초기화
            upgrades?.ResetRun(); // 이전 출격의 선택 효과 제거
            enhancements?.ResetRun(); // 이전 출격의 강화 아이템과 장비 제거
            score?.ResetRun(); // 새 출격의 점수와 획득 숫자 초기화
        }
    }
}
