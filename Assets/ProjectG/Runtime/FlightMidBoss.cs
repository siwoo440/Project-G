using UnityEngine; // 보스 진입 위치와 실제 탄막 방향

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 중간 보스당 한 공격 실행기
    public sealed class FlightMidBoss : FlightStageBoss // 직선 연사와 부채탄을 교대하는 중간 보스
    {
        [SerializeField] private Transform arrivalPoint; // 씬에서 편집하는 보스 진입 위치
        [SerializeField] private Transform combatPoint; // 실제 보스가 공격하는 공중 좌표
        [SerializeField] private Transform muzzle; // 보스 앞의 탄막 출발점
        [SerializeField] private GameObject weakPoint; // 표시와 충돌을 함께 여닫는 약점
        [SerializeField, Min(0.01f)] private float arrivalSeconds = 1.2f; // 탄막 없이 진입하는 시간
        [SerializeField, Min(0.01f)] private float weakSeconds = 1f; // 패턴 뒤의 약점 노출 시간
        [SerializeField, Min(1)] private int damage = 10; // 보스 탄막 한 발의 시험 피해
        private FlightThreatPool pool; // 스테이지에서 공유하는 적탄 풀
        private Transform pilot; // 보스가 조준할 플레이어 위치
        private Vector3 patternDirection; // 연속 발사 동안 고정한 수평 방향
        private float enteredAt; // 진입을 시작한 전투 시각
        private float nextAction; // 다음 발사 또는 약점 닫기 시각
        private int volleyCount; // 현재 패턴에서 발사한 차례 수
        private bool entering; // 공격 위치로 이동 중인지 구분
        private FlightThreatPattern pattern; // 이번 차례의 탄막 배열

        public int AimedBursts { get; private set; } // 실제 직선 연사 횟수
        public int FanBursts { get; private set; } // 실제 부채탄 발사 횟수

        internal override void Begin(FlightThreatPool sharedPool, Transform target) // 웨이브에서 실제 보스 진입
        {
            pool = sharedPool; // 전체 적과 상한 공유
            pilot = target; // 공격 각도 계산 대상
            enteredAt = Time.time; // 현재 전투 시각
            nextAction = enteredAt + arrivalSeconds; // 진입 완료 후 첫 발
            entering = true; // 씬의 진입 경로 사용
            volleyCount = 0; // 새 패턴의 첫 발
            pattern = FlightThreatPattern.Aimed; // 직선 연사부터 시작
            AimedBursts = 0; // 이전 출격의 직선 발사 기록 제거
            FanBursts = 0; // 이전 출격의 부채탄 발사 기록 제거
            weakPoint.SetActive(false); // 진입과 공격 중 약점 닫기
            transform.SetPositionAndRotation(arrivalPoint.position, combatPoint.rotation); // Hierarchy에서 설정한 진입 자세
            gameObject.SetActive(true); // 보스 모델과 실제 체력 활성화
        }

        private void Update() // 진입과 패턴 교대의 실제 전투 시간 처리
        {
            if (Time.timeScale == 0f) // 메뉴와 카드 선택 중
            {
                return; // 보스 이동과 약점 수명 동결
            }
            float now = Time.time; // 현재 전투 시각
            if (entering) // 공격 위치에 도달하기 전
            {
                float progress = Mathf.SmoothStep(0f, 1f, (now - enteredAt) / arrivalSeconds); // 진입의 실제 진행률
                transform.position = Vector3.Lerp(arrivalPoint.position, combatPoint.position, progress); // 공중의 진입 경로
                entering = now < nextAction; // 진입 동안 탄막 방지
            }
            if (now < nextAction) // 다음 발사 또는 약점 전환 시각 이전
            {
                return; // 현재 자세와 약점 유지
            }
            if (weakPoint.activeSelf) // 열린 약점의 유지 시간이 끝남
            {
                weakPoint.SetActive(false); // 보이는 표시와 실제 약점 충돌 닫기
                volleyCount = 0; // 새 패턴 발사 횟수 초기화
                pattern = pattern == FlightThreatPattern.Aimed ? FlightThreatPattern.Fan : FlightThreatPattern.Aimed; // 직선과 부채탄 교대
                nextAction = now + 0.15f; // 패턴이 바뀌는 짧은 여유
                return; // 약점 닫기 프레임의 발사 생략
            }
            if (volleyCount == 0) // 해당 패턴에서 첫 발을 쏠 때
            {
                patternDirection = Vector3.ProjectOnPlane(pilot.position - muzzle.position, Vector3.up); // 위아래를 피할 수 있는 수평 조준
                if (patternDirection.sqrMagnitude < 0.0001f) // 기체가 보스 바로 위아래에 있는 경우
                {
                    patternDirection = transform.forward; // 수직 직격 대신 보스의 정면 사용
                }
            }
            bool aimed = pattern == FlightThreatPattern.Aimed; // 발사 횟수와 속도의 패턴 구분
            int fired = FlightThreatPatterns.Fire(pool, pattern, muzzle.position, patternDirection, 0f, damage); // 실제 물리 탄막 발사
            if (fired > 0) // 공유 풀에서 실제로 활성화된 발사
            {
                AimedBursts += aimed ? 1 : 0; // 직선 연사의 성공한 발사 수
                FanBursts += aimed ? 0 : 1; // 부채탄의 성공한 발사 수
            }
            volleyCount++; // 풀 상한과 무관한 공격 진행
            if (volleyCount >= (aimed ? 4 : 2)) // 직선 네 발 또는 부채 두 번을 마친 경우
            {
                weakPoint.SetActive(true); // 공격 뒤 시각·피격 약점 노출
                nextAction = now + weakSeconds; // 노출 종료 시각
                return; // 약점 유지 시간 뒤에 다음 공격
            }
            nextAction = now + (aimed ? 0.24f : 0.55f); // 같은 패턴의 다음 발사 간격
        }
    }
}
