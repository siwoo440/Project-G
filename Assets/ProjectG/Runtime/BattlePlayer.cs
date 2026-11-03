using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 새 입력 시스템
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BattlePlayer : MonoBehaviour // 플레이어 구성 요소
    { // 클래스 시작
        private BattleSession session; // 전투 상태
        private EnemyBulletPool bulletPool; // 적탄 풀 참조
        public SkillState Skills // 스킬과 에너지 상태
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public event System.Action<int> UltimateDamageRequested; // 필살기 피해 요청
        public void AssignBulletPool(EnemyBulletPool pool) // 적탄 풀 연결
        { // 연결 시작
            bulletPool = pool; // 적탄 풀 보관
        } // 연결 종료
        public bool TryUseRapid() // 고유 스킬 사용
        { // 사용 시작
            return Skills != null && Survival != null && Skills.TryRapid(Survival.IsAlive); // 고유 스킬 발동
        } // 사용 종료
        public bool TryUseCommon() // 공용 스킬 사용
        { // 사용 시작
            if (Skills == null || Survival == null || !Skills.TryCommon(Survival.IsAlive)) // 발동 조건 확인
            { // 차단 시작
                return false; // 공용 스킬 차단
            } // 차단 종료
            if (Skills.Common == CommonSkill.Shield) // 보호막 장착 확인
            { // 보호막 시작
                Survival.ActivateShield(1f); // 시험용 1초 보호막
            } // 보호막 종료
            else // 공용 산탄 장착
            { // 산탄 시작
                float top = battleCamera != null ? battleCamera.transform.position.y + battleCamera.orthographicSize : 5f; // 화면 위 경계
                for (int index = 0; index < 3; index++) // 산탄 세 발 순회
                { // 발사 시작
                    float spread = (index - 1) * 2f; // 시험용 산탄 방향
                    GameObject bullet = new GameObject("SkillScatter"); // 스킬 탄환 생성
                    bullet.transform.position = transform.position + new Vector3(spread * 0.08f, 0.25f, 0f); // 발사 시작 위치
                    bullet.AddComponent<Projectile>().Begin(new Vector2(spread, 10f), top, 12, false); // 산탄 시험 피해량
                } // 발사 종료
            } // 산탄 종료
            return true; // 공용 스킬 발동
        } // 사용 종료
        public bool TryUseUltimate() // 필살기 사용
        { // 사용 시작
            if (Skills == null || Survival == null || battleCamera == null || bulletPool == null || !Skills.TryUltimate(Survival.IsAlive)) // 발동 조건 확인
            { // 차단 시작
                return false; // 필살기 차단
            } // 차단 종료
            float halfHeight = battleCamera.orthographicSize; // 화면 세로 반경
            float halfWidth = halfHeight * 16f / 9f; // 화면 가로 반경
            Vector3 center = battleCamera.transform.position; // 화면 중심
            bulletPool.ClearVisible(new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f)); // 화면 안 소거 가능 적탄 제거
            UltimateDamageRequested?.Invoke(80); // 적 전체 시험 피해 요청
            return true; // 필살기 발동
        } // 사용 종료
        public void RegisterEnemyDefeat(EnemyRank rank) // 적 처치 보상
        { // 보상 시작
            if (Skills != null && Survival != null && Survival.IsAlive) // 생존 상태 확인
            { // 충전 시작
                Skills.GainDefeat(rank); // 적 종류별 에너지 충전
            } // 충전 종료
        } // 보상 종료
        public PlayerSurvival Survival // 플레이어 생존 상태
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public int GrazeCount // 누적 그레이즈 수
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public Vector2 PreviousPosition // 이전 플레이어 위치
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        private InputAction moveAction; // 이동 입력
        private InputAction focusAction; // 집중 입력
        private InputAction rapidAction; // 고유 스킬 입력
        private InputAction commonAction; // 공용 스킬 입력
        private InputAction ultimateAction; // 필살기 입력
        private Camera battleCamera; // 전투 카메라
        public bool IsFocused => session != null && session.Focused; // 현재 집중 상태
        public void AssignCamera(Camera camera) // 카메라 연결
        { // 연결 시작
            battleCamera = camera; // 전투 카메라 지정
        } // 연결 종료
        private void OnEnable() // 입력 활성화
        { // 활성화 시작
            if (!Application.isPlaying) // 편집 상태 확인
            { // 편집 상태 시작
                return; // 편집 중 입력 생략
            } // 편집 상태 종료
            moveAction = new InputAction("Move", InputActionType.Value); // 이동 입력 생성
            moveAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d"); // WASD 기본 입력
            focusAction = new InputAction("Focus", InputActionType.Button, "<Keyboard>/leftShift"); // 집중 기본 입력
            rapidAction = new InputAction("Unique", InputActionType.Button, "<Keyboard>/j"); // 고유 스킬 기본 입력
            commonAction = new InputAction("Common", InputActionType.Button, "<Keyboard>/k"); // 공용 스킬 기본 입력
            ultimateAction = new InputAction("Ultimate", InputActionType.Button, "<Keyboard>/l"); // 필살기 기본 입력
            moveAction.Enable(); // 이동 입력 시작
            focusAction.Enable(); // 집중 입력 시작
            rapidAction.Enable(); // 고유 스킬 입력 시작
            commonAction.Enable(); // 공용 스킬 입력 시작
            ultimateAction.Enable(); // 필살기 입력 시작
        } // 활성화 종료
        private void OnDisable() // 입력 비활성화
        { // 비활성화 시작
            moveAction?.Dispose(); // 이동 입력 정리
            focusAction?.Dispose(); // 집중 입력 정리
            rapidAction?.Dispose(); // 고유 스킬 입력 정리
            commonAction?.Dispose(); // 공용 스킬 입력 정리
            ultimateAction?.Dispose(); // 필살기 입력 정리
        } // 비활성화 종료
        private void Start() // 전투 시작
        { // 시작 처리
            if (battleCamera == null) // 카메라 지정 확인
            { // 카메라 확인 시작
                battleCamera = Camera.main; // 주 카메라 확인
            } // 카메라 확인 종료
            if (session == null) // 초기 상태 확인
            { // 초기 상태 시작
                Begin(transform.position); // 현재 위치로 시작
            } // 초기 상태 종료
        } // 시작 종료
        private void Update() // 매 프레임 처리
        { // 처리 시작
            if (battleCamera == null || moveAction == null || focusAction == null) // 실행 조건 확인
            { // 실행 불가 시작
                return; // 입력 처리 생략
            } // 실행 불가 종료
            float halfHeight = battleCamera.orthographicSize; // 카메라 세로 반경
            float halfWidth = halfHeight * 16f / 9f; // 가로 16대9 반경
            Vector3 center = battleCamera.transform.position; // 카메라 중심
            bool toggle = PlayerPrefs.GetInt("ProjectG.FocusToggle", 0) == 1; // 집중 방식 설정
            if (Time.deltaTime > 0f && rapidAction.WasPressedThisFrame()) // 고유 스킬 입력 확인
            { // 고유 스킬 시작
                TryUseRapid(); // 고유 스킬 사용
            } // 고유 스킬 종료
            if (Time.deltaTime > 0f && commonAction.WasPressedThisFrame()) // 공용 스킬 입력 확인
            { // 공용 스킬 시작
                TryUseCommon(); // 공용 스킬 사용
            } // 공용 스킬 종료
            if (Time.deltaTime > 0f && ultimateAction.WasPressedThisFrame()) // 필살기 입력 확인
            { // 필살기 시작
                TryUseUltimate(); // 필살기 사용
            } // 필살기 종료
            Step(moveAction.ReadValue<Vector2>(), focusAction.WasPressedThisFrame(), focusAction.IsPressed(), toggle, Time.deltaTime, center.x - halfWidth, center.x + halfWidth, center.y - halfHeight, center.y + halfHeight); // 입력과 경계 적용
        } // 처리 종료
        public void Begin(Vector2 start) // 플레이어 시작
        { // 초기화 시작
            session = new BattleSession(start); // 전투 상태 생성
            Skills = new SkillState(); // 스킬과 에너지 초기화
            Survival = new PlayerSurvival(100); // 시험용 최대 체력
            GrazeCount = 0; // 그레이즈 기록 초기화
            PreviousPosition = start; // 이전 위치 초기화
            transform.position = new Vector3(start.x, start.y, 0f); // 시작 위치 적용
        } // 초기화 종료
        public void Step(Vector2 input, bool pressed, bool held, bool toggle, float elapsed, float left, float right, float bottom, float top) // 플레이어 진행
        { // 진행 시작
            if (session == null) // 초기화 여부 확인
            { // 초기화 시작
                Begin(transform.position); // 현재 위치 초기화
            } // 초기화 종료
            PreviousPosition = transform.position; // 이동 전 위치 보관
            if (elapsed <= 0f || !Survival.IsAlive) // 정지 또는 출격 실패 확인
            { // 진행 중단 시작
                return; // 이동과 공격 중단
            } // 진행 중단 종료
            Survival.Advance(elapsed); // 무적 시간 진행
            Skills.Advance(elapsed); // 스킬 활성과 쿨타임 진행
            int shots = session.Advance(input, pressed, held, toggle, elapsed, left, right, bottom, top, Skills.AttackIntervalScale); // 이동과 공격 계산
            transform.position = new Vector3(session.Position.x, session.Position.y, 0f); // 계산 위치 적용
            for (int index = 0; index < shots; index++) // 발사 탄환 순회
            { // 발사 시작
                float spread = shots == 2 ? (index == 0 ? -0.7f : 0.7f) : 0f; // 시험용 확산 방향
                GameObject bullet = new GameObject("PlayerBullet"); // 탄환 객체 생성
                bullet.transform.position = transform.position + new Vector3(spread * 0.15f, 0.25f, 0f); // 발사 시작 위치
                bullet.AddComponent<Projectile>().Begin(new Vector2(spread, 10f), top, session.Focused ? 7 : 3, true); // 기본 탄환 시험 피해
            } // 발사 종료
        } // 진행 종료
        public void RegisterGraze() // 그레이즈 기록
        { // 기록 시작
            if (Survival != null && Survival.IsAlive) // 생존 상태 확인
            { // 기록 처리 시작
                GrazeCount++; // 그레이즈 횟수 증가
                Skills.GainGraze(); // 필살기 에너지 충전
            } // 기록 처리 종료
        } // 기록 종료
    } // 클래스 종료
} // 영역 종료
