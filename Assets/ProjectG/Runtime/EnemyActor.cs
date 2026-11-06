using UnityEngine; // 유니티 전투 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum EnemyKind // 초기 적 종류
    { // 종류 시작
        Aimed, // 조준형
        Spread, // 확산형
        Dash, // 돌진형
        MidBoss, // 중간 보스
        FinalBoss // 최종 보스
    } // 종류 종료
    public sealed class EnemyActor : CombatTarget // 적과 보스 시험 구현
    { // 클래스 시작
        private enum BossState // 보스 패턴 상태
        { // 상태 시작
            Warning, // 공격 예고
            Burst, // 직선 연사
            Recovery // 약점 노출
        } // 상태 종료
        private EnemyKind kind; // 현재 적 종류
        private BattlePlayer player; // 공격 대상
        private EnemyBulletPool pool; // 적탄 풀
        private Camera battleCamera; // 전투 카메라
        private float timer; // 다음 행동 대기
        private float originX; // 이동 기준 위치
        private float age; // 생성 후 경과 시간
        private int midPattern; // 중간 보스 패턴
        private int burstRemaining; // 남은 연사 수
        private BossState bossState; // 보스 패턴 상태
        private Vector2 dashVelocity; // 돌진 속도
        private bool dashContact; // 돌진 접촉 처리 여부
        [SerializeField] private SpriteRenderer bodyImage; // 편집 가능한 몸체 이미지
        private readonly EnemyVisualMotion visualMotion = new EnemyVisualMotion(); // 몸체 움직임 상태
        [SerializeField] private GameObject warningMark; // 편집 가능한 돌진 경고
        [SerializeField] private GameObject dashPath; // 편집 가능한 돌진 경로
        [SerializeField] private GameObject weakMark; // 편집 가능한 약점 표시
        [SerializeField] private EnemyHealthBar healthBar; // 편집 가능한 적 체력 표시
        public event System.Action<int> PhaseChanged; // 보스 단계 변경 알림
        public float Health // 현재 체력
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public float MaximumHealth // 최대 체력
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public int ExperienceReward // 경험치 시험 보상
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public EnemyRank Rank // 적 보상 구분
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public bool IsAlive => Health > 0f; // 생존 여부
        public override bool CanTakeDamage => IsAlive; // 피해 가능 여부
        public bool IsTelegraphing => kind == EnemyKind.Dash ? timer > 0f : IsBoss ? bossState == BossState.Warning : timer <= 0.4f; // 공격 예고 상태
        public bool WeakExposed => IsBoss && bossState == BossState.Recovery && IsAlive; // 보스 약점 노출
        private bool IsBoss => kind == EnemyKind.MidBoss || kind == EnemyKind.FinalBoss; // 보스 구분
        public int PatternIndex => kind == EnemyKind.FinalBoss ? (Health <= MaximumHealth / 3f ? 2 : Health <= MaximumHealth * 2f / 3f ? 1 : 0) : midPattern; // 보스 시험 패턴
        public bool HasSceneVisual(EnemyKind type) // 종류별 씬 표시 유효성
        { // 확인 시작
            bool body = bodyImage != null && bodyImage.sprite != null && bodyImage.transform.IsChildOf(transform); // 편집 몸체 확인
            bool bar = healthBar != null && healthBar.transform.IsChildOf(transform) && healthBar.HasValidVisual; // 편집 체력바 확인
            bool weak = weakMark != null && weakMark.transform.IsChildOf(transform) && weakMark.GetComponent<SpriteRenderer>() != null && weakMark.GetComponent<SpriteRenderer>().sprite != null; // 편집 약점 확인
            bool dash = type != EnemyKind.Dash || (warningMark != null && warningMark.transform.IsChildOf(transform) && warningMark.GetComponent<SpriteRenderer>() != null && warningMark.GetComponent<SpriteRenderer>().sprite != null && dashPath != null && dashPath.transform.IsChildOf(transform) && dashPath.GetComponent<SpriteRenderer>() != null && dashPath.GetComponent<SpriteRenderer>().sprite != null); // 돌진 표시 확인
            return body && bar && weak && dash; // 필수 표시 연결 결과
        } // 확인 종료
        public void Begin(EnemyKind type, BattlePlayer target, EnemyBulletPool bulletPool, Camera camera) // 적 초기화
        { // 초기화 시작
            if (!HasSceneVisual(type)) // 씬 표시 연결 확인
            { // 오류 시작
                Debug.LogError("적 템플릿의 필수 표시 요소가 연결되지 않았습니다", this); // 누락 오류 표시
                gameObject.SetActive(false); // 불완전 적 활성화 차단
                return; // 임의 표시 생성 차단
            } // 오류 종료
            kind = type; // 적 종류 보관
            player = target; // 대상 보관
            pool = bulletPool; // 적탄 풀 보관
            battleCamera = camera; // 카메라 보관
            MaximumHealth = type == EnemyKind.Aimed ? 50f : type == EnemyKind.Spread ? 60f : type == EnemyKind.Dash ? 80f : type == EnemyKind.MidBoss ? 600f : 1500f; // 종류별 시험 체력
            ExperienceReward = type == EnemyKind.Aimed ? 6 : type == EnemyKind.Spread ? 8 : type == EnemyKind.Dash ? 10 : type == EnemyKind.MidBoss ? 30 : 50; // 종류별 시험 경험치
            Rank = type == EnemyKind.MidBoss ? EnemyRank.MidBoss : type == EnemyKind.FinalBoss ? EnemyRank.FinalBoss : EnemyRank.Normal; // 보상 종류
            Health = MaximumHealth; // 체력 초기화
            timer = type == EnemyKind.Aimed ? 1.5f : type == EnemyKind.Spread ? 2f : 0.8f; // 첫 공격 대기
            originX = transform.position.x; // 초기 가로 위치
            age = 0f; // 경과 시간 초기화
            midPattern = 0; // 초기 패턴
            burstRemaining = 0; // 연사 횟수 초기화
            bossState = BossState.Warning; // 초기 보스 예고
            Vector2 dashDirection = player != null ? (Vector2)player.transform.position - (Vector2)transform.position : Vector2.down; // 예고 시작 대상 방향
            dashVelocity = (dashDirection.sqrMagnitude > 0.0001f ? dashDirection.normalized : Vector2.down) * 5f; // 예고와 일치하는 돌진 속도
            dashContact = false; // 돌진 접촉 초기화
            InitializeVisual(); // 편집된 표시 초기화
            UpdateVisual(); // 초기 상태 표시
        } // 초기화 종료
        public override void ReceiveDamage(float damage, BattlePlayer source) // 공통 피해 적용
        { // 적용 시작
            ApplyDamage(damage, source); // 적 체력 처리
        } // 적용 종료
        public void ApplyDamage(float damage, BattlePlayer source) // 적 체력 처리
        { // 처리 시작
            if (!IsAlive || !gameObject.activeInHierarchy || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) // 피해 조건 확인
            { // 무시 시작
                return; // 중복 피해 차단
            } // 무시 종료
            float previousHealth = Health; // 피해 전 체력
            int previousPhase = PatternIndex; // 피해 전 단계
            float boundary = kind == EnemyKind.FinalBoss && previousPhase < 2 ? MaximumHealth * (2 - previousPhase) / 3f : 0f; // 다음 체력 경계
            Health = Mathf.Max(boundary, Health - damage); // 한 타격의 경계 초과 피해 제한
            if (Health > 0f && Health < previousHealth && healthBar != null) // 실제 생존 피해 확인
            { // 표시 시작
                healthBar.Show(Health / MaximumHealth); // 현재 체력 표시
            } // 표시 종료
            if (kind == EnemyKind.FinalBoss && PatternIndex != previousPhase) // 단계 변경 확인
            { // 전환 알림 시작
                PhaseChanged?.Invoke(PatternIndex); // 현재 단계 알림
            } // 전환 알림 종료
            if (Health <= 0f) // 처치 확인
            { // 처치 시작
                if (source != null) // 공격자 확인
                { // 보상 시작
                    source.RegisterEnemyDefeat(Rank, ExperienceReward, transform.position); // 처치 위치와 보상 전달
                } // 보상 종료
                gameObject.SetActive(false); // 적 퇴장
            } // 처치 종료
            UpdateVisual(); // 현재 상태 표시
        } // 처리 종료
        public override bool Intersects(Vector2 from, Vector2 to, out bool weak, float padding = 0f, float verticalPadding = 0f) // 탄환 구간 판정
        { // 판정 시작
            Vector2 center = transform.position; // 적 중심
            float halfWidth = IsBoss ? 1.1f : 0.5f; // 종류별 몸체 폭
            float halfHeight = IsBoss ? 0.55f : 0.35f; // 종류별 몸체 높이
            Rect body = new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f); // 몸체 판정 영역
            Rect weakArea = new Rect(center.x - 0.2f, center.y - halfHeight, 0.4f, halfHeight * 2f); // 시험 약점 영역
            float extra = Mathf.Max(0f, padding); // 탄환 가로 폭
            body.xMin -= extra; // 몸체 왼쪽 확장
            body.xMax += extra; // 몸체 오른쪽 확장
            weakArea.xMin -= extra; // 약점 왼쪽 확장
            weakArea.xMax += extra; // 약점 오른쪽 확장
            float vertical = Mathf.Max(0f, verticalPadding); // 탄환 세로 폭
            body.yMin -= vertical; // 몸체 아래쪽 확장
            body.yMax += vertical; // 몸체 위쪽 확장
            weakArea.yMin -= vertical; // 약점 아래쪽 확장
            weakArea.yMax += vertical; // 약점 위쪽 확장
            weak = WeakExposed && TrainingTarget.SegmentHits(weakArea, from, to); // 노출 중 약점 확인
            return IsAlive && TrainingTarget.SegmentHits(body, from, to); // 몸체 접촉 확인
        } // 판정 종료
        public override bool IntersectsBeam(Rect beam, out bool weak) // 광선 판정
        { // 판정 시작
            Vector2 center = transform.position; // 적 중심
            float halfWidth = IsBoss ? 1.1f : 0.5f; // 종류별 몸체 폭
            float halfHeight = IsBoss ? 0.55f : 0.35f; // 종류별 몸체 높이
            Rect body = new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f); // 몸체 판정 영역
            Rect weakArea = new Rect(center.x - 0.2f, center.y - halfHeight, 0.4f, halfHeight * 2f); // 시험 약점 영역
            weak = WeakExposed && beam.Overlaps(weakArea); // 노출 중 약점 확인
            return IsAlive && beam.Overlaps(body); // 몸체 접촉 확인
        } // 판정 종료
        private void Update() // 매 프레임 진행
        { // 진행 시작
            if (player != null && player.Survival != null && !player.Survival.IsAlive && healthBar != null) // 정지 중 플레이어 사망 확인
            { // 종료 표시 시작
                healthBar.Hide(); // 전투 종료 후 체력바 숨김
            } // 종료 표시 종료
            if (Time.timeScale > 0f && kind != EnemyKind.Dash) // 일반 적과 보스 진행 확인
            { // 진행 시작
                Advance(Time.deltaTime); // 경과 시간 적용
            } // 진행 종료
        } // 진행 종료
        private void LateUpdate() // 플레이어 이동 후 돌진 처리
        { // 처리 시작
            if (Time.timeScale > 0f && kind == EnemyKind.Dash) // 돌진 적과 전투 시간 확인
            { // 돌진 진행 시작
                Advance(Time.deltaTime); // 현재 프레임 이동 접촉 적용
            } // 돌진 진행 종료
        } // 처리 종료
        private void OnDisable() // 적 비활성 처리
        { // 처리 시작
            if (healthBar != null) // 체력바 존재 확인
            { // 숨김 시작
                healthBar.Hide(); // 남은 표시 초기화
            } // 숨김 종료
        } // 처리 종료
        public void Advance(float elapsed) // 적 행동 진행
        { // 진행 시작
            if (player != null && player.Survival != null && !player.Survival.IsAlive && healthBar != null) // 플레이어 사망 확인
            { // 종료 표시 시작
                healthBar.Hide(); // 종료 후 체력바 숨김
            } // 종료 표시 종료
            if (!IsAlive || elapsed <= 0f || (player != null && player.Survival != null && !player.Survival.IsAlive)) // 정지와 생존 확인
            { // 중단 시작
                return; // 행동 생략
            } // 중단 종료
            visualMotion.Advance(elapsed); // 전투 시간의 몸체 움직임
            if (healthBar != null && Time.timeScale > 0f) // 전투 시간 진행 확인
            { // 표시 진행 시작
                healthBar.Advance(elapsed); // 체력바 표시 시간 진행
            } // 표시 진행 종료
            age += elapsed; // 경과 시간 증가
            if (kind == EnemyKind.Dash) // 돌진형 확인
            { // 돌진 처리 시작
                AdvanceDash(elapsed); // 돌진 행동
            } // 돌진 처리 종료
            else if (IsBoss) // 보스 확인
            { // 보스 처리 시작
                AdvanceBoss(elapsed); // 보스 패턴
            } // 보스 처리 종료
            else // 일반 사격형
            { // 사격 처리 시작
                if (kind == EnemyKind.Aimed) // 조준형 이동 확인
                { // 이동 시작
                    transform.position = new Vector3(originX + Mathf.Sin(age) * 1.25f, transform.position.y, 0f); // 시험 좌우 이동
                } // 이동 종료
                timer -= elapsed; // 사격 대기 감소
                int safety = 0; // 반복 상한
                while (timer <= 0f && safety++ < 16) // 발사 시점 확인
                { // 발사 시작
                    FirePattern(kind == EnemyKind.Aimed ? 0 : 1); // 일반 적 공격
                    timer += kind == EnemyKind.Aimed ? 1.5f : 2f; // 다음 발사 시간
                } // 발사 종료
            } // 사격 처리 종료
            UpdateVisual(); // 예고와 약점 표시 갱신
        } // 진행 종료
        private void AdvanceDash(float elapsed) // 돌진 행동
        { // 돌진 시작
            float remaining = elapsed; // 남은 진행 시간
            if (timer > 0f) // 예고 시간 확인
            { // 예고 진행 시작
                float warningTime = Mathf.Min(timer, remaining); // 이번 예고 시간
                timer -= warningTime; // 남은 예고 시간
                remaining -= warningTime; // 남은 진행 시간
                if (timer <= 0.000001f) // 예고 종료 확인
                { // 돌진 준비 시작
                    timer = 0f; // 예고 종료
                } // 돌진 준비 종료
            } // 예고 진행 종료
            if (remaining <= 0f) // 돌진 시간 확인
            { // 대기 시작
                return; // 이동 생략
            } // 대기 종료
            Vector2 from = transform.position; // 이동 전 위치
            Vector2 to = from + dashVelocity * remaining; // 이동 후 위치
            transform.position = to; // 돌진 이동
            if (!dashContact && player != null && player.Survival != null && CollisionMath.Intersects(Vector2.zero, from - player.PreviousPosition, to - (Vector2)player.transform.position, 0.47f)) // 연속 접촉 확인
            { // 접촉 처리 시작
                dashContact = true; // 중복 접촉 방지
                player.Survival.TryHit(15); // 돌진 접촉 피해
            } // 접촉 처리 종료
            Vector2 center = battleCamera != null ? (Vector2)battleCamera.transform.position : Vector2.zero; // 화면 중심
            float height = battleCamera != null ? battleCamera.orthographicSize : 0f; // 화면 세로 반경
            float width = height * (battleCamera != null ? battleCamera.aspect : 0f); // 실제 전투 가로 반경
            bool escaped = battleCamera != null && (to.y < center.y - height - 1f || to.y > center.y + height + 1f || to.x < center.x - width - 1f || to.x > center.x + width + 1f); // 화면 네 방향 이탈 확인
            if (escaped) // 화면 이탈 확인
            { // 퇴장 시작
                gameObject.SetActive(false); // 보상 없는 이탈
            } // 퇴장 종료
        } // 돌진 종료
        private void AdvanceBoss(float elapsed) // 보스 행동
        { // 행동 시작
            float remaining = elapsed; // 남은 시간
            int safety = 0; // 반복 상한
            while (remaining > 0f && safety++ < 64) // 패턴 시간 처리
            { // 반복 시작
                float step = Mathf.Min(timer, remaining); // 현재 단계 진행
                timer -= step; // 단계 시간 감소
                remaining -= step; // 남은 시간 감소
                if (timer > 0.000001f) // 단계 진행 중 확인
                { // 대기 시작
                    break; // 다음 프레임 대기
                } // 대기 종료
                if (bossState == BossState.Warning) // 공격 예고 종료
                { // 공격 시작
                    int pattern = PatternIndex; // 현재 패턴 확인
                    FirePattern(pattern); // 패턴 발사
                    if (pattern == 0) // 직선 연사 확인
                    { // 연사 시작
                        burstRemaining = 2; // 남은 연사 수
                        bossState = BossState.Burst; // 연사 상태
                        timer = 0.18f; // 다음 연사 시간
                    } // 연사 종료
                    else // 확산 또는 교차 공격
                    { // 회복 시작
                        bossState = BossState.Recovery; // 약점 노출 상태
                        timer = 1f; // 시험 약점 노출 시간
                    } // 회복 종료
                } // 공격 종료
                else if (bossState == BossState.Burst) // 직선 연사 진행
                { // 연사 처리 시작
                    FirePattern(0); // 직선탄 발사
                    burstRemaining--; // 남은 발사 수 감소
                    bossState = burstRemaining > 0 ? BossState.Burst : BossState.Recovery; // 다음 상태
                    timer = burstRemaining > 0 ? 0.18f : 1f; // 연사 또는 회복 시간
                } // 연사 처리 종료
                else // 약점 노출 종료
                { // 다음 패턴 시작
                    if (kind == EnemyKind.MidBoss) // 중간 보스 확인
                    { // 교대 시작
                        midPattern = (midPattern + 1) % 2; // 직선과 확산 교대
                    } // 교대 종료
                    bossState = BossState.Warning; // 다음 예고 상태
                    timer = 0.8f; // 시험 예고 시간
                } // 다음 패턴 종료
            } // 반복 종료
        } // 행동 종료
        private void FirePattern(int pattern) // 시험 탄막 발사
        { // 발사 시작
            if (pool == null || player == null || battleCamera == null) // 발사 연결 확인
            { // 생략 시작
                return; // 시험 연결 전 생략
            } // 생략 종료
            Vector2 position = transform.position; // 발사 위치
            Vector2 aim = (Vector2)player.transform.position - position; // 플레이어 방향
            if (aim.sqrMagnitude < 1f) // 근접 생성 확인
            { // 안전 처리 시작
                return; // 근접 탄환 생성 차단
            } // 안전 처리 종료
            float bottom = battleCamera.transform.position.y - battleCamera.orthographicSize; // 화면 아래 경계
            if (pattern == 0) // 조준 직선탄
            { // 직선 발사 시작
                pool.Spawn(position, aim.normalized * 3f, 0.06f, 10, bottom, player); // 시험 직선탄
            } // 직선 발사 종료
            else if (pattern == 1 && kind != EnemyKind.FinalBoss) // 일반 또는 중간 보스 부채탄
            { // 부채 발사 시작
                for (int index = -1; index <= 1; index++) // 세 방향 순회
                { // 발사 반복 시작
                    Vector2 velocity = Quaternion.Euler(0f, 0f, index * 20f) * Vector2.down * 3f; // 부채 방향
                    pool.Spawn(position, velocity, 0.06f, 10, bottom, player); // 시험 부채탄
                } // 발사 반복 종료
            } // 부채 발사 종료
            else if (pattern == 1) // 최종 보스 원형 확산
            { // 원형 발사 시작
                for (int index = 0; index < 12; index++) // 열두 방향 순회
                { // 발사 반복 시작
                    Vector2 velocity = Quaternion.Euler(0f, 0f, index * 30f) * Vector2.down * 2.4f; // 원형 방향
                    pool.Spawn(position, velocity, 0.06f, 15, bottom, player); // 시험 원형탄
                } // 발사 반복 종료
            } // 원형 발사 종료
            else // 최종 보스 교차 탄막
            { // 교차 발사 시작
                for (int index = 0; index < 3; index++) // 세 쌍 순회
                { // 발사 반복 시작
                    float offset = 0.5f + index * 0.35f; // 양쪽 발사 간격
                    pool.Spawn(position + Vector2.left * offset, new Vector2(2f, -3f), 0.06f, 20, bottom, player); // 오른쪽 교차탄
                    pool.Spawn(position + Vector2.right * offset, new Vector2(-2f, -3f), 0.06f, 20, bottom, player); // 왼쪽 교차탄
                } // 발사 반복 종료
            } // 교차 발사 종료
        } // 발사 종료
        private void InitializeVisual() // 적 시각 요소 초기화
        { // 초기화 시작
            visualMotion.Begin(bodyImage.transform, kind); // 편집 변환 기준 몸체 움직임
            healthBar.Initialize(bodyImage, IsBoss); // 편집한 체력바 초기화
            if (kind == EnemyKind.Dash && dashPath != null) // 돌진 경로 갱신 확인
            { // 갱신 시작
                Vector2 direction = dashVelocity.normalized; // 확정된 돌진 방향
                SpriteRenderer pathImage = dashPath.GetComponent<SpriteRenderer>(); // 편집한 경로 이미지
                float pathLength = Mathf.Abs(pathImage.sprite.bounds.size.y * dashPath.transform.localScale.y); // 편집된 경로 길이
                dashPath.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction); // 경로 방향 적용
                dashPath.transform.localPosition = direction * (pathLength * 0.5f); // 적 중심부터 경로 배치
            } // 갱신 종료
        } // 초기화 종료
        private void UpdateVisual() // 표시 상태 갱신
        { // 갱신 시작
            if (weakMark != null) // 약점 표시 확인
            { // 약점 처리 시작
                weakMark.SetActive(WeakExposed); // 약점 노출 상태 반영
            } // 약점 처리 종료
            if (warningMark != null) // 예고 표시 확인
            { // 예고 처리 시작
                warningMark.SetActive(kind == EnemyKind.Dash && IsTelegraphing && IsAlive); // 돌진형 경고 상태 반영
            } // 예고 처리 종료
            if (dashPath != null) // 경로 표시 확인
            { // 경로 처리 시작
                dashPath.SetActive(kind == EnemyKind.Dash && IsTelegraphing && IsAlive); // 예고 중에만 경로 표시
            } // 경로 처리 종료
        } // 갱신 종료
    } // 클래스 종료
} // 영역 종료
