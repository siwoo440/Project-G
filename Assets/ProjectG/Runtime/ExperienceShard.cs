using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class ExperienceShard : MonoBehaviour // 경험치 조각
    { // 클래스 시작
        private BattleProgress progress; // 전투 성장 참조
        private int value; // 조각 경험치
        private bool collected; // 수집 완료 여부
        private SpriteRenderer image; // 조각 표시
        public void Begin(BattleProgress owner, Vector2 position, int amount) // 조각 초기화
        { // 초기화 시작
            progress = owner; // 성장 상태 연결
            value = Mathf.Max(0, amount); // 경험치 보관
            collected = false; // 수집 상태 초기화
            transform.position = position; // 생성 위치
            transform.localScale = new Vector3(0.3f, 0.3f, 1f); // 시험 표시 크기
            if (image == null) // 표시 요소 확인
            { // 생성 시작
                image = gameObject.AddComponent<SpriteRenderer>(); // 표시 요소 생성
                image.sprite = Projectile.GetSprite(); // 시험용 조각 도형
                image.color = new Color(0.3f, 1f, 0.7f); // 경험치 구별 색상
                image.sortingOrder = 3; // 배경 위 표시
            } // 생성 종료
            gameObject.SetActive(true); // 조각 활성화
        } // 초기화 종료
        private void Update() // 매 프레임 진행
        { // 진행 시작
            if (Time.timeScale > 0f) // 전투 시간 확인
            { // 진행 시작
                Advance(Time.deltaTime); // 경과 시간 적용
            } // 진행 종료
        } // 진행 종료
        public void Advance(float elapsed) // 경험치 흡수 진행
        { // 진행 시작
            if (collected || elapsed <= 0f || progress == null || progress.Player == null || progress.Player.Build == null || progress.Player.Survival == null || !progress.Player.Survival.IsAlive) // 수집 조건 확인
            { // 중단 시작
                return; // 진행 생략
            } // 중단 종료
            Vector2 position = transform.position; // 조각 위치
            Vector2 destination = progress.Player.transform.position; // 플레이어 위치
            float radius = progress.Player.Build.PickupRadius; // 카드 적용 흡수 반경
            if ((destination - position).sqrMagnitude > radius * radius) // 흡수 범위 확인
            { // 범위 밖 시작
                return; // 조각 유지
            } // 범위 밖 종료
            Vector2 next = Vector2.MoveTowards(position, destination, 8f * elapsed); // 플레이어 방향 흡수
            transform.position = next; // 이동 위치 적용
            if ((destination - next).sqrMagnitude <= 0.04f) // 수집 거리 확인
            { // 수집 시작
                collected = true; // 중복 수집 방지
                gameObject.SetActive(false); // 조각 회수
                progress.CollectExperience(value); // 경험치 지급
            } // 수집 종료
        } // 진행 종료
    } // 클래스 종료
} // 영역 종료
