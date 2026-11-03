using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 새 입력 시스템
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BattlePlayer : MonoBehaviour // 플레이어 구성 요소
    { // 클래스 시작
        private BattleSession session; // 전투 상태
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
            moveAction.Enable(); // 이동 입력 시작
            focusAction.Enable(); // 집중 입력 시작
        } // 활성화 종료
        private void OnDisable() // 입력 비활성화
        { // 비활성화 시작
            moveAction?.Dispose(); // 이동 입력 정리
            focusAction?.Dispose(); // 집중 입력 정리
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
            Step(moveAction.ReadValue<Vector2>(), focusAction.WasPressedThisFrame(), focusAction.IsPressed(), toggle, Time.deltaTime, center.x - halfWidth, center.x + halfWidth, center.y - halfHeight, center.y + halfHeight); // 입력과 경계 적용
        } // 처리 종료
        public void Begin(Vector2 start) // 플레이어 시작
        { // 초기화 시작
            session = new BattleSession(start); // 전투 상태 생성
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
            int shots = session.Advance(input, pressed, held, toggle, elapsed, left, right, bottom, top); // 이동과 공격 계산
            transform.position = new Vector3(session.Position.x, session.Position.y, 0f); // 계산 위치 적용
            for (int index = 0; index < shots; index++) // 발사 탄환 순회
            { // 발사 시작
                float spread = shots == 2 ? (index == 0 ? -0.7f : 0.7f) : 0f; // 시험용 확산 방향
                GameObject bullet = new GameObject("PlayerBullet"); // 탄환 객체 생성
                bullet.transform.position = transform.position + new Vector3(spread * 0.15f, 0.25f, 0f); // 발사 시작 위치
                bullet.AddComponent<Projectile>().Begin(new Vector2(spread, 10f), top); // 탄환 이동 시작
            } // 발사 종료
        } // 진행 종료
        public void RegisterGraze() // 그레이즈 기록
        { // 기록 시작
            if (Survival != null && Survival.IsAlive) // 생존 상태 확인
            { // 기록 처리 시작
                GrazeCount++; // 그레이즈 횟수 증가
            } // 기록 처리 종료
        } // 기록 종료
    } // 클래스 종료
} // 영역 종료
