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
        private GameObject warningMark; // 공격 예고 표시
        private GameObject weakMark; // 약점 표시
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
        public void Begin(EnemyKind type, BattlePlayer target, EnemyBulletPool bulletPool, Camera camera) // 적 초기화
        { // 초기화 시작
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
            dashVelocity = Vector2.zero; // 돌진 속도 초기화
            dashContact = false; // 돌진 접촉 초기화
            CreateVisual(); // 임시 도형 생성
            UpdateVisual(); // 초기 상태 표시
        } // 초기화 종료
        public override void ReceiveDamage(float damage, BattlePlayer source) // 공통 피해 적용
        { // 적용 시작
            ApplyDamage(damage, source); // 적 체력 처리
        } // 적용 종료
        public void ApplyDamage(float damage, BattlePlayer source) // 적 체력 처리
        { // 처리 시작
            if (!IsAlive || damage <= 0f) // 피해 조건 확인
            { // 무시 시작
                return; // 중복 피해 차단
            } // 무시 종료
            int previousPhase = PatternIndex; // 피해 전 단계
            float boundary = kind == EnemyKind.FinalBoss && previousPhase < 2 ? MaximumHealth * (2 - previousPhase) / 3f : 0f; // 다음 체력 경계
            Health = Mathf.Max(boundary, Health - damage); // 한 타격의 경계 초과 피해 제한
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
            if (Time.timeScale > 0f) // 전투 시간 확인
            { // 진행 시작
                Advance(Time.deltaTime); // 경과 시간 적용
            } // 진행 종료
        } // 진행 종료
        public void Advance(float elapsed) // 적 행동 진행
        { // 진행 시작
            if (!IsAlive || elapsed <= 0f || (player != null && player.Survival != null && !player.Survival.IsAlive)) // 정지와 생존 확인
            { // 중단 시작
                return; // 행동 생략
            } // 중단 종료
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
                    Vector2 direction = player != null ? (Vector2)player.transform.position - (Vector2)transform.position : Vector2.down; // 시험 돌진 방향
                    dashVelocity = (direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down) * 5f; // 고정 돌진 속도
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
            float width = height * 16f / 9f; // 화면 가로 반경
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
        private void CreateVisual() // 임시 도형 생성
        { // 생성 시작
            GameObject body = new GameObject("EnemyBody"); // 적 몸체 객체
            body.transform.SetParent(transform, false); // 적에 연결
            body.transform.localScale = IsBoss ? new Vector3(6.6f, 3.3f, 1f) : new Vector3(3f, 2.1f, 1f); // 종류별 표시 크기 세 배
            SpriteRenderer image = body.AddComponent<SpriteRenderer>(); // 몸체 표시
            image.sprite = BattleSprites.Enemy(kind); // 종류별 시험 이미지
            image.color = Color.white; // 원본 이미지 색상
            weakMark = new GameObject("EnemyWeakPoint"); // 약점 표시 객체
            weakMark.transform.SetParent(transform, false); // 적에 연결
            weakMark.transform.localScale = new Vector3(0.5f, 0.5f, 1f); // 약점 중심 표시 크기
            SpriteRenderer weakImage = weakMark.AddComponent<SpriteRenderer>(); // 약점 표시 요소
            weakImage.sprite = BattleVisuals.Weak; // 약점 조준 이미지
            weakImage.color = Color.white; // 약점 이미지 원본 색상
            weakImage.sortingOrder = 2; // 몸체 앞 표시
            warningMark = new GameObject("EnemyAttackWarning"); // 공격 예고 객체
            warningMark.transform.SetParent(transform, false); // 적에 연결
            warningMark.transform.localPosition = new Vector3(0f, -1.95f, 0f); // 확대된 몸체 아래 예고
            warningMark.transform.localScale = new Vector3(0.5f, 0.5f, 1f); // 경고 아이콘 크기
            SpriteRenderer warningImage = warningMark.AddComponent<SpriteRenderer>(); // 예고 표시 요소
            warningImage.sprite = BattleVisuals.Warning; // 공격 경고 아이콘
            warningImage.color = Color.white; // 경고 이미지 원본 색상
            warningImage.sortingOrder = 3; // 전경 표시
        } // 생성 종료
        private void UpdateVisual() // 표시 상태 갱신
        { // 갱신 시작
            if (weakMark != null) // 약점 표시 확인
            { // 약점 처리 시작
                weakMark.SetActive(WeakExposed); // 약점 노출 상태 반영
            } // 약점 처리 종료
            if (warningMark != null) // 예고 표시 확인
            { // 예고 처리 시작
                warningMark.SetActive(IsTelegraphing && IsAlive); // 공격 예고 상태 반영
            } // 예고 처리 종료
        } // 갱신 종료
    } // 클래스 종료
} // 영역 종료
