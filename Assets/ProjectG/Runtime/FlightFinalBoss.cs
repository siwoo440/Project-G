using UnityEngine; // 최종 보스의 위치·조준·전투 시간

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 보스당 한 개의 공격 실행기
    public sealed class FlightFinalBoss : FlightStageBoss // 체력에 따라 세 패턴을 사용하는 최종 보스
    {
        public const int PhaseCount = 3; // 최종 보스의 전체 페이즈
        [SerializeField] private Transform arrivalPoint; // 씬에서 편집할 진입 위치
        [SerializeField] private Transform combatPoint; // 실제 전투 중심
        [SerializeField] private Transform centerMuzzle; // 중앙에서 나가는 조준과 고리탄
        [SerializeField] private Transform leftMuzzle; // 교차 나선의 왼쪽 출발점
        [SerializeField] private Transform rightMuzzle; // 교차 나선의 오른쪽 출발점
        [SerializeField] private GameObject weakPoint; // 한 공격을 끝낸 뒤 활성화하는 약점
        [SerializeField, Min(0.01f)] private float arrivalSeconds = 1.5f; // 발사 없는 진입 시간
        [SerializeField, Min(0.01f)] private float transitionSeconds = 0.9f; // 피해와 발사를 멈추는 전환 길이
        [SerializeField, Min(0.01f)] private float weakSeconds = 1f; // 한 차례 공격 뒤의 약점 시간
        private FlightThreatPool pool; // 전투에서 공유하는 적탄 풀
        private Transform pilot; // 패턴별로 조준할 비행체
        private float enteredAt; // 최종 보스 진입을 시작한 시각
        private float nextAction; // 다음 탄막 또는 약점 해제 시각
        private float protectionEnds; // 진입·페이즈 전환의 피해 차단 종료점
        private Vector3 direction; // 한 차례 공격에서 고정한 수평 방향
        private int burst; // 이번 패턴 안에서 진행한 발사
        private bool entered; // 공격 위치까지 이동 결과

        public int Phase { get; private set; } = 1; // 현재 보스의 전투 페이즈
        public bool IsEntering => !entered; // HUD의 출현 상태
        public bool IsTransitioning => entered && Time.time < protectionEnds; // HUD에 표시할 전환 무적
        public bool IsWeakPointOpen => weakPoint.activeSelf; // 공격 사이의 실제 약점 표시

        internal override void Begin(FlightThreatPool sharedPool, Transform target) // 실전 웨이브에서 최종 보스 시작
        {
            pool = sharedPool; // 실제 탄막 풀 연결
            pilot = target; // 조준하는 플레이어 위치
            Phase = 1; // 새 보스는 조준 연사부터 시작
            burst = 0; // 이전 출격의 발사 회차 초기화
            entered = false; // 편집한 진입점부터 비행
            enteredAt = Time.time; // 전투 시간 기준의 진입 시작
            protectionEnds = enteredAt + arrivalSeconds; // 진입을 마칠 때까지 피해 차단
            nextAction = protectionEnds; // 공격은 진입 완료 뒤 시작
            weakPoint.SetActive(false); // 진입 중 약점 숨김
            transform.SetPositionAndRotation(arrivalPoint.position, combatPoint.rotation); // 실제 위치와 자세 시작
            gameObject.SetActive(true); // 보스 외형과 체력을 시작 상태로 활성화
        }

        internal int ResolveHealth(int current, int incoming, int maximum) // 피격마다 다음 체력 경계를 확인
        {
            if (!entered || Time.time < protectionEnds) // 등장 또는 페이즈 전환 중의 추가 피해
            {
                return current; // 해당 프레임과 전환 구간의 피해 차단
            }
            int floor = (int)((long)maximum * (PhaseCount - Phase) / PhaseCount); // 다음 페이즈의 체력 경계
            if (Phase == PhaseCount || incoming > floor) // 마지막 페이즈 또는 현재 구간의 일반 피격
            {
                return incoming; // 현재 구간에서 받은 전체 피해 반영
            }
            Phase++; // 체력 경계를 한 번만 통과
            protectionEnds = Time.time + transitionSeconds; // 연속 피격의 즉시 차단
            nextAction = protectionEnds; // 이전 패턴 예약을 제거하고 새 패턴 대기
            burst = 0; // 다음 패턴의 첫 발 준비
            weakPoint.SetActive(false); // 이전 패턴의 약점 종료
            pool.ReturnAll(); // 남은 이전 페이즈 투사체 회수
            return floor; // 다음 체력 경계까지만 실제 피해 적용
        }

        private void Update() // 실제 전투 시간과 단계에 맞는 공격 실행
        {
            if (Time.timeScale == 0f) // 정지 메뉴와 카드 선택의 시간 정지
            {
                return; // 보스 위치와 다음 발사 유지
            }
            float now = Time.time; // 일시정지를 제외한 현재 시각
            if (!entered) // 최종 보스 진입 이동
            {
                float progress = Mathf.SmoothStep(0f, 1f, (now - enteredAt) / arrivalSeconds); // 진입 위치 사이의 비행 진행률
                transform.position = Vector3.Lerp(arrivalPoint.position, combatPoint.position, progress); // 편집한 진입에서 전투 위치로 이동
                entered = now >= protectionEnds; // 위치에 도달하면 전투 활성
            }
            if (now < nextAction) // 다음 발사·약점 시간의 대기
            {
                return; // 공격 및 약점 유지
            }
            if (weakPoint.activeSelf) // 공격 후 약점 시간이 끝난 경우
            {
                weakPoint.SetActive(false); // 약점과 충돌 동시 비활성화
                burst = 0; // 현재 페이즈의 새 공격 준비
                nextAction = now + 0.22f; // 약점 종료 후 짧은 회피 여유
                return; // 회복 프레임과 다음 발사 분리
            }
            if (burst == 0) // 한 차례의 첫 발에서 수평 조준 결정
            {
                direction = Vector3.ProjectOnPlane(pilot.position - centerMuzzle.position, Vector3.up); // 높이 방향의 회피 공간
                if (direction.sqrMagnitude < 0.0001f) // 플레이어가 보스 바로 위아래에 있는 경우
                {
                    direction = transform.forward; // 편집한 보스 전방을 조준 기준으로 사용
                }
            }
            FireBurst(); // 현재 페이즈의 실제 탄막 발사
            burst++; // 다음 발사 순서 기록
            int burstsPerPattern = Phase == 1 ? 4 : 3; // 조준 네 발과 후속 패턴 세 번
            if (burst >= burstsPerPattern) // 한 공격 차례의 마지막 탄막
            {
                weakPoint.SetActive(true); // 공격 종료를 알리는 약점 노출
                nextAction = now + weakSeconds; // 약점의 실제 유효 시간
                return; // 다음 공격 전까지 발사 생략
            }
            nextAction = now + (Phase == 1 ? 0.23f : 0.46f); // 읽을 수 있는 패턴별 발사 간격
        }

        private void FireBurst() // 페이즈별로 모양과 위치가 다른 실제 탄막
        {
            switch (Phase) // 현재 체력 구간의 공격
            {
                case 1: // 첫 체력 구간의 직선 연사
                    FlightThreatPatterns.Fire(pool, FlightThreatPattern.Aimed, centerMuzzle.position, direction, 0f, 10); // 중앙의 조준탄
                    break; // 첫 구간 발사 완료
                case 2: // 중간 체력 구간의 원형 확산
                    FlightThreatPatterns.Fire(pool, FlightThreatPattern.Ring, centerMuzzle.position, direction, 0f, 15); // 한 발사점에서 벌어지는 입체 고리
                    break; // 원형 확산 발사 완료
                default: // 마지막 체력 구간의 좌우 교차 나선
                    float roll = burst * 26f; // 새로운 발사마다 회전하는 나선 각도
                    Vector3 crossing = centerMuzzle.position + direction.normalized * 14f; // 좌우 탄막 중심이 교차할 위치
                    Vector3 left = Vector3.ProjectOnPlane(crossing - leftMuzzle.position, Vector3.up); // 왼쪽 발사점의 중심 방향
                    Vector3 right = Vector3.ProjectOnPlane(crossing - rightMuzzle.position, Vector3.up); // 오른쪽 발사점의 중심 방향
                    FlightThreatPatterns.Fire(pool, FlightThreatPattern.Spiral, leftMuzzle.position, left, roll, 20); // 왼쪽에서 중앙을 가로지르는 나선
                    FlightThreatPatterns.Fire(pool, FlightThreatPattern.Spiral, rightMuzzle.position, right, 45f - roll, 20); // 오른쪽에서 반대 방향으로 회전하는 나선
                    break; // 두 갈래의 교차 나선 완료
            }
        }
    }
}
