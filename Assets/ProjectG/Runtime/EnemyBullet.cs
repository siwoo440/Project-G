using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    [DefaultExecutionOrder(100)] // 돌진 접촉 이후 적탄 판정
    public sealed class EnemyBullet : MonoBehaviour // 적탄 객체
    { // 클래스 시작
        private Vector2 velocity; // 적탄 이동 속도
        private float radius; // 시험용 탄환 반경
        public float Radius => radius; // 현재 탄환 판정 반경
        public float DisplayRadius // 현재 편집된 이미지의 표시 반경
        { // 조회 시작
            get // 표시 범위 확인
            { // 계산 시작
                if (image == null || image.sprite == null || !gameObject.activeInHierarchy) // 실제 표시 확인
                { // 기본값 시작
                    return 0f; // 보이지 않는 표시 범위
                } // 기본값 종료
                Bounds bounds = image.bounds; // 이미지의 현재 월드 범위
                Vector3 center = transform.position; // 탄환 판정 중심
                float horizontal = Mathf.Max(Mathf.Abs(bounds.min.x - center.x), Mathf.Abs(bounds.max.x - center.x)); // 중심에서 가로 표시 거리
                float vertical = Mathf.Max(Mathf.Abs(bounds.min.y - center.y), Mathf.Abs(bounds.max.y - center.y)); // 중심에서 세로 표시 거리
                return Mathf.Max(horizontal, vertical); // 편집된 표시 전체 반경
            } // 계산 종료
        } // 조회 종료
        public bool Clearable // 필살기 소거 가능 여부
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        private float bottom; // 화면 아래 경계
        private int damage; // 시험용 탄환 피해
        private BattlePlayer target; // 플레이어 대상
        [SerializeField] private SpriteRenderer image; // 씬에서 편집하는 적탄 이미지
        [SerializeField] private Transform radiusScale; // 판정 반경별 표시 배율
        private Vector3 baseRadiusScale; // 편집한 기본 표시 배율
        private bool scaleInitialized; // 기본 배율 보관 여부
        public bool HasValidVisual => image != null && image.sprite != null && radiusScale != null && radiusScale.IsChildOf(transform) && image.transform.IsChildOf(radiusScale); // 템플릿 이미지와 계층 확인
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
        public void Begin(EnemyBulletPool owner, Vector2 position, Vector2 speed, float size, int damage, float lowerBound, BattlePlayer player, bool clearable) // 적탄 초기화
        { // 초기화 시작
            Owner = owner; // 소유 풀 보관
            velocity = speed; // 속도 보관
            radius = Mathf.Max(0f, size); // 반경 보정
            bottom = lowerBound; // 아래 경계 보관
            this.damage = damage; // 피해량 보관
            target = player; // 플레이어 대상 보관
            Clearable = clearable; // 소거 가능 여부 초기화
            interaction.Reset(); // 그레이즈 상태 초기화
            transform.position = position; // 시작 위치 설정
            if (!HasValidVisual) // 편집된 표시 요소 확인
            { // 생성 시작
                Debug.LogError("적탄 템플릿의 이미지 또는 표시 계층이 없습니다", this); // 누락 오류 표시
                InUse = false; // 불완전 탄환 사용 차단
                gameObject.SetActive(false); // 보이지 않는 탄환 비활성화
                return; // 초기화 중단
            } // 생성 종료
            if (!scaleInitialized) // 기본 표시 배율 확인
            { // 보관 시작
                baseRadiusScale = radiusScale.localScale; // 사용자가 편집한 기본 배율
                scaleInitialized = true; // 기본 배율 보관 완료
            } // 보관 종료
            float ratio = radius / 0.06f; // 기존 기준 반경 대비 표시 배율
            radiusScale.localScale = new Vector3(baseRadiusScale.x * ratio, baseRadiusScale.y * ratio, baseRadiusScale.z); // 이미지 편집값을 보존한 크기 반영
            InUse = true; // 사용 중 표시
            gameObject.SetActive(true); // 적탄 활성화
        } // 초기화 종료
        private void LateUpdate() // 매 프레임 이동
        { // 이동 시작
            if (Time.timeScale > 0f) // 전투 시간 확인
            { // 진행 시작
                Simulate(Time.deltaTime); // 경과 시간 적용
            } // 진행 종료
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
            Camera view = Owner != null ? Owner.View : null; // 풀의 현재 전투 카메라
            float halfHeight = view != null ? view.orthographicSize : 0f; // 화면 세로 반경
            float halfWidth = halfHeight * (view != null ? view.aspect : 0f); // 실제 전투 가로 반경
            Vector3 center = view != null ? view.transform.position : Vector3.zero; // 화면 중심
            float visibleRadius = DisplayRadius; // 확대 표시 반경
            bool escaped = view != null && (transform.position.y - visibleRadius > center.y + halfHeight + 1f || transform.position.x + visibleRadius < center.x - halfWidth - 1f || transform.position.x - visibleRadius > center.x + halfWidth + 1f); // 위쪽과 좌우 이탈 확인
            if (transform.position.y + visibleRadius < bottom || escaped) // 표시가 완전히 화면 밖인지 확인
            { // 회수 시작
                Owner.Recycle(this); // 풀로 반환
            } // 회수 종료
        } // 이동 종료
    } // 클래스 종료
} // 영역 종료
