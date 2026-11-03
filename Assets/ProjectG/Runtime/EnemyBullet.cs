using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class EnemyBullet : MonoBehaviour // 적탄 객체
    { // 클래스 시작
        private Vector2 velocity; // 적탄 이동 속도
        private float radius; // 시험용 탄환 반경
        private float bottom; // 화면 아래 경계
        private int damage; // 시험용 탄환 피해
        private BattlePlayer target; // 플레이어 대상
        private SpriteRenderer image; // 적탄 표시 요소
        private readonly BulletInteraction interaction = new BulletInteraction(); // 적탄별 판정 상태
        public bool InUse // 풀 사용 여부
        { // 속성 시작
            get; // 값 조회
            set; // 값 설정
        } // 속성 종료
        public EnemyBulletPool Owner // 소유 풀
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public void Begin(EnemyBulletPool owner, Vector2 position, Vector2 speed, float size, int damage, float lowerBound, BattlePlayer player) // 적탄 초기화
        { // 초기화 시작
            Owner = owner; // 소유 풀 보관
            velocity = speed; // 속도 보관
            radius = Mathf.Max(0f, size); // 반경 보정
            bottom = lowerBound; // 아래 경계 보관
            this.damage = damage; // 피해량 보관
            target = player; // 플레이어 대상 보관
            interaction.Reset(); // 그레이즈 상태 초기화
            transform.position = position; // 시작 위치 설정
            transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f); // 탄환 표시 크기
            if (image == null) // 표시 요소 확인
            { // 생성 시작
                image = gameObject.AddComponent<SpriteRenderer>(); // 표시 요소 추가
                image.sprite = Projectile.GetSprite(); // 임시 이미지 사용
                image.color = new Color(1f, 0.25f, 0.35f); // 적탄 구별 색상
                image.sortingOrder = 5; // 적탄 우선 표시
            } // 생성 종료
            InUse = true; // 사용 중 표시
            gameObject.SetActive(true); // 적탄 활성화
        } // 초기화 종료
        private void LateUpdate() // 매 프레임 이동
        { // 이동 시작
            Simulate(Time.deltaTime); // 경과 시간 적용
        } // 이동 종료
        public void Simulate(float elapsed) // 적탄 이동
        { // 이동 시작
            if (!InUse || elapsed <= 0f) // 비활성 또는 정지 확인
            { // 생략 시작
                return; // 이동 생략
            } // 생략 종료
            Vector2 from = transform.position; // 이동 전 탄환 위치
            Vector2 to = from + velocity * elapsed; // 이동 후 탄환 위치
            transform.position = to; // 적탄 위치 적용
            if (target != null && target.Survival != null) // 플레이어 존재 확인
            { // 접촉 판정 시작
                BulletContact contact = interaction.Resolve(target.PreviousPosition, target.transform.position, from, to, radius, damage, target.Survival); // 상대 이동 접촉 판정
                if (contact == BulletContact.Graze) // 그레이즈 확인
                { // 그레이즈 처리 시작
                    target.RegisterGraze(); // 그레이즈 기록
                } // 그레이즈 처리 종료
                if (contact == BulletContact.Hit || contact == BulletContact.BlockedHit) // 피격 접촉 확인
                { // 접촉 회수 시작
                    Owner.Recycle(this); // 접촉 탄환 회수
                    return; // 추가 판정 생략
                } // 접촉 회수 종료
            } // 접촉 판정 종료
            if (transform.position.y + radius < bottom) // 화면 이탈 확인
            { // 회수 시작
                Owner.Recycle(this); // 풀로 반환
            } // 회수 종료
        } // 이동 종료
    } // 클래스 종료
} // 영역 종료
