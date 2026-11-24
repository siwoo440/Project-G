using UnityEngine; // 3D 이동 경로와 회전

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 마물마다 독립된 이동 경로
    public sealed class FlightEnemyRoute : MonoBehaviour // 적의 출현·공격·이탈
    {
        [SerializeField] private Vector3 entry; // 출현의 월드 좌표
        [SerializeField] private Vector3 attackPoint; // 실제 공중 공격 위치
        [SerializeField] private Vector3 exit; // 이탈 방향을 드러내는 월드 좌표
        [SerializeField, Min(0.1f)] private float entrySeconds = 1f; // 첫 공격까지의 출현 시간
        [SerializeField, Min(0.1f)] private float attackSeconds = 1f; // 두 패턴을 섞는 공격 시간
        [SerializeField, Min(0.1f)] private float exitSeconds = 2f; // 이탈 이동 시간
        [SerializeField] private FlightThreatPattern opening; // 첫 번째 탄막
        [SerializeField] private FlightThreatPattern following; // 두 번째 탄막
        private FlightThreatPool pool; // 적들이 공유하는 풀
        private Transform pilot; // 현재 비행체의 위치
        private float elapsed; // 해당 출현의 경과 시간
        private int attempts; // 시도한 발사 단계

        public int PatternsFired { get; private set; } // 발사한 유효 패턴 수

        public void Begin(FlightThreatPool sharedPool, Transform target) // 씬에 편집된 경로 시작
        {
            pool = sharedPool; // 기존 적탄 풀 참조
            pilot = target; // 실제 조준 기준
            elapsed = 0f; // 첫 등장 프레임
            attempts = 0; // 첫 탄막 시도 대기
            PatternsFired = 0; // 이전 출격의 패턴 수 제거
            transform.position = entry; // 설정된 진입 위치
            Face(pilot.position - entry); // 등장 즉시 비행체 쪽 시각 방향
            gameObject.SetActive(true); // 마물의 상태와 날개 활성
        }

        private void Update() // 시간에 따른 출현·공격·이탈 이동
        {
            elapsed += Time.deltaTime; // 해당 출격의 경로 시간
            if (elapsed < entrySeconds) // 등장 중인 구간
            {
                transform.position = Vector3.Lerp(entry, attackPoint, elapsed / entrySeconds); // 진입 위치에서 공격 위치로 이동
                return; // 등장 중 발사하지 않음
            }

            float attackAge = elapsed - entrySeconds; // 공격 단계의 경과 시간
            if (attackAge < attackSeconds) // 서로 다른 두 탄막을 발사할 구간
            {
                transform.position = attackPoint; // 공격 위치 유지
                Vector3 aim = pilot.position - attackPoint; // 현재 비행체를 기준으로 조준
                Face(aim); // 위와 아래를 향하는 마물 방향
                if (attempts == 0 || (attempts == 1 && attackAge >= attackSeconds * 0.52f)) // 두 탄막을 서로 다른 순간에 발사
                {
                    FlightThreatPattern pattern = attempts == 0 ? opening : following; // 이번 마물의 공격 배열
                    attempts++; // 처리한 공격 단계 수
                    if (FlightThreatPatterns.Fire(pool, pattern, attackPoint, aim, 0f) > 0) // 실제 풀에서 발사 확인
                    {
                        PatternsFired++; // 성공한 패턴 수
                    }
                }
                return; // 공격 구간의 진입 처리 종료
            }

            float exitProgress = (attackAge - attackSeconds) / exitSeconds; // 이탈 경로 이동 비율
            transform.position = Vector3.Lerp(attackPoint, exit, exitProgress); // 하강·측면·전방으로 정해진 움직임
            Face(exit - attackPoint); // 나가는 방향이 보이는 마물 회전
            if (exitProgress >= 1f) // 편집된 경로의 종료 위치 도달
            {
                gameObject.SetActive(false); // 화면 밖의 마물을 다음 연습까지 숨김
            }
        }

        private void Face(Vector3 forward) // 높이 변화에도 안정적인 마물 방향
        {
            if (forward.sqrMagnitude > 0.0001f) // 조준 방향의 최소 길이
            {
                Vector3 up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up; // 수직 이동의 보조 축
                transform.rotation = Quaternion.LookRotation(forward, up); // 실제 룩 방향 갱신
            }
        }
    }
}
