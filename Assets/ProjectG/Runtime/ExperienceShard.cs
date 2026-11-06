using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class ExperienceShard : MonoBehaviour // 경험치 조각
    { // 클래스 시작
        public const float TrialFallSpeed = 0.5f; // 초당 낙하 속도 시험값
        private BattleProgress progress; // 전투 성장 참조
        private int value; // 조각 경험치
        private bool collected; // 수집 완료 여부
        private SpriteRenderer image; // 조각 표시
        private Vector2 spawnPlayerPosition; // 생성 시 플레이어 위치
        private bool firstAdvance; // 생성 후 첫 이동 여부
        public int AvailableValue => !collected && gameObject.activeInHierarchy ? value : 0; // 현재 회수 가능한 경험치
        public int Take() // 활성 경험치 즉시 회수
        { // 회수 시작
            if (collected || !gameObject.activeSelf || value <= 0) // 수집 가능 여부 확인
            { // 차단 시작
                return 0; // 중복 수집 차단
            } // 차단 종료
            collected = true; // 회수 완료 기록
            gameObject.SetActive(false); // 같은 프레임 재수집 차단
            return value; // 경험치 값 반환
        } // 회수 종료
        public void Begin(BattleProgress owner, Vector2 position, int amount) // 조각 초기화
        { // 초기화 시작
            progress = owner; // 성장 상태 연결
            value = Mathf.Max(0, amount); // 경험치 보관
            collected = false; // 수집 상태 초기화
            transform.position = position; // 생성 위치
            spawnPlayerPosition = progress != null && progress.Player != null ? (Vector2)progress.Player.transform.position : position; // 생성 이전 이동 제외 기준
            firstAdvance = true; // 새 조각의 첫 이동 기록
            if (image == null) // 표시 요소 확인
            { // 생성 시작
                image = GetComponentInChildren<SpriteRenderer>(true); // 프리팹 이미지 조회
            } // 생성 종료
            if (image == null || image.sprite == null) // 편집된 이미지 확인
            { // 오류 처리 시작
                Debug.LogError("경험치 조각 프리팹의 이미지가 없습니다", this); // 누락 오류 표시
                gameObject.SetActive(false); // 보이지 않는 조각 비활성화
                return; // 잘못된 조각 진행 차단
            } // 오류 처리 종료
            gameObject.SetActive(true); // 조각 활성화
        } // 초기화 종료
        private void LateUpdate() // 플레이어 이동 이후 진행
        { // 진행 시작
            if (Time.timeScale > 0f) // 전투 시간 확인
            { // 진행 시작
                Advance(Time.deltaTime); // 경과 시간 적용
            } // 진행 종료
        } // 진행 종료
        public void Advance(float elapsed) // 경험치 흡수 진행
        { // 진행 시작
            if (Time.timeScale <= 0f || collected || elapsed <= 0f || float.IsNaN(elapsed) || float.IsInfinity(elapsed) || progress == null || progress.Player == null || progress.Player.Build == null || progress.Player.Survival == null || !progress.Player.Survival.IsAlive) // 시간과 수집 조건 확인
            { // 중단 시작
                return; // 진행 생략
            } // 중단 종료
            Vector2 position = transform.position; // 조각 위치
            Vector2 playerStart = firstAdvance ? spawnPlayerPosition : progress.Player.PreviousPosition; // 생성 이후 또는 프레임 시작 위치
            firstAdvance = false; // 첫 이동 판정 완료
            Vector2 destination = progress.Player.transform.position; // 플레이어 현재 위치
            Vector2 next = position + Vector2.down * (TrialFallSpeed * elapsed); // 시험 속도의 아래쪽 이동
            float radius = progress.Player.Build.PickupRadius; // 카드 적용 흡수 반경
            Vector2 relativeStart = position - playerStart; // 시작 시 상대 위치
            Vector2 relativeFall = next - destination; // 낙하 후 상대 위치
            bool touched = CollisionMath.Intersects(Vector2.zero, relativeStart, relativeFall, 0.2f); // 낙하 중 수집 접촉
            if (!touched && CollisionMath.Intersects(Vector2.zero, relativeStart, relativeFall, radius)) // 이동 중 흡수 범위 접촉 확인
            { // 흡수 시작
                next = Vector2.MoveTowards(next, destination, 8f * elapsed); // 기존 플레이어 방향 흡수
                touched = CollisionMath.Intersects(Vector2.zero, relativeStart, next - destination, 0.2f); // 흡수 중 수집 접촉
            } // 흡수 종료
            transform.position = next; // 이동 위치 적용
            if (touched) // 수집 거리 확인
            { // 수집 시작
                progress.CollectExperience(Take()); // 조각 회수와 경험치 지급
                return; // 획득 후 추가 처리 방지
            } // 수집 종료
            Camera view = progress.BattleCamera; // 현재 전투 카메라
            if (view != null && image != null && image.bounds.max.y < view.transform.position.y - view.orthographicSize) // 표시 전체가 화면 아래로 이탈했는지 확인
            { // 화면 이탈 시작
                gameObject.SetActive(false); // 기존 조각 풀에서 재사용 대기
            } // 화면 이탈 종료
        } // 진행 종료
    } // 클래스 종료
} // 영역 종료
