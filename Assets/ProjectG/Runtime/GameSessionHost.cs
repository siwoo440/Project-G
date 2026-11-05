using UnityEngine; // 유니티 장면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class GameSessionHost : MonoBehaviour // 메뉴와 출격 장면 연결
    { // 클래스 시작
        private Camera menuCamera; // 메뉴 카메라
        private GameObject battleRoot; // 현재 출격 객체 모음
        private PauseSettingsOverlay overlay; // 일시정지와 설정 화면
        public GameFlowController Flow // 현재 화면 흐름
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public BattlePlayer CurrentPlayer // 현재 출격 플레이어
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public StageDirector CurrentStage // 현재 출격 스테이지
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public void Begin(Camera camera) // 메뉴와 설정 초기화
        { // 초기화 시작
            if (Flow != null || camera == null) // 중복 초기화 확인
            { // 차단 시작
                return; // 기존 화면 유지
            } // 차단 종료
            menuCamera = camera; // 메뉴 카메라 보관
            overlay = gameObject.AddComponent<PauseSettingsOverlay>(); // 설정 화면 구성
            overlay.Begin(null, menuCamera, null, null); // 메뉴 설정 연결
            Flow = gameObject.AddComponent<GameFlowController>(); // 화면 흐름 구성
            Flow.Begin(menuCamera); // 타이틀 표시
            Flow.RunRequested += StartBattle; // 출격 요청 연결
            Flow.LeaveBattleRequested += ClearBattle; // 전투 정리 요청 연결
            Flow.SettingsRequested += OpenSettings; // 메뉴 설정 연결
            overlay.RetryRequested += RetryBattle; // 일시정지 재시도 연결
            overlay.ExitRequested += ReturnToPreparation; // 일시정지 복귀 연결
        } // 초기화 종료
        private void Update() // 모달 입력 상태 갱신
        { // 갱신 시작
            if (Flow != null && overlay != null) // 화면 연결 확인
            { // 입력 처리 시작
                Flow.InputBlocked = overlay.IsPaused; // 설정 중 메뉴 입력 차단
            } // 입력 처리 종료
        } // 갱신 종료
        private void StartBattle(CharacterType character, CommonSkill skill) // 선택한 조건으로 출격
        { // 출격 시작
            ClearBattle(); // 이전 출격 정리
            battleRoot = new GameObject("ProjectG Battle"); // 출격 객체 모음
            battleRoot.transform.SetParent(transform, false); // 메뉴 장면에 연결
            Camera camera = battleRoot.AddComponent<Camera>(); // 전투 카메라 생성
            camera.CopyFrom(menuCamera); // 메뉴 카메라 화면 설정 복사
            camera.enabled = true; // 전투 카메라 표시
            GameObject player = BattleSetup.Create(camera); // 전투 구성 생성
            player.transform.SetParent(battleRoot.transform, true); // 출격 객체에 연결
            CurrentPlayer = player.GetComponent<BattlePlayer>(); // 전투 플레이어 조회
            CurrentPlayer.Begin(new Vector2(0f, -3f), character); // 선택 캐릭터 출격
            CurrentPlayer.Skills.Equip(skill); // 선택 공용 스킬 장착
            CurrentPlayer.AllowDebugCharacterSwitch = false; // 출격 중 시험 캐릭터 전환 차단
            BattleProgress progress = player.GetComponent<BattleProgress>(); // 성장 상태 조회
            UpgradeItemSelection selection = battleRoot.GetComponent<UpgradeItemSelection>(); // 강화 선택 조회
            CurrentStage = battleRoot.GetComponent<StageDirector>(); // 스테이지 조회
            overlay.Begin(CurrentPlayer, camera, progress, selection); // 전투 일시정지 연결
            CurrentPlayer.SetBindings(overlay.Bindings); // 공유 키 설정 적용
            Flow.AttachBattle(CurrentPlayer, CurrentStage); // 결과 화면 연결
            menuCamera.enabled = false; // 메뉴 카메라 숨김
        } // 출격 종료
        private void OpenSettings() // 타이틀 설정 열기
        { // 설정 시작
            if (overlay != null) // 설정 화면 확인
            { // 열기 시작
                overlay.OpenSettingsFromMenu(); // 설정 화면 표시
            } // 열기 종료
        } // 설정 종료
        private void RetryBattle() // 일시정지 재시도
        { // 재시도 시작
            Flow?.RetryBattle(); // 확인된 출격 재시작
        } // 재시도 종료
        private void ReturnToPreparation() // 일시정지에서 준비 복귀
        { // 복귀 시작
            Flow?.ReturnFromBattle(); // 확인된 준비 화면 복귀
        } // 복귀 종료
        private void ClearBattle() // 이전 출격 정리
        { // 정리 시작
            if (overlay != null) // 일시정지 화면 확인
            { // 일시정지 정리 시작
                overlay.Resume(); // 소유한 시간 배율 복원
                overlay.Begin(null, menuCamera, null, null); // 메뉴 설정으로 복귀
            } // 일시정지 정리 종료
            if (CurrentStage != null) // 스테이지 존재 확인
            { // 스테이지 정리 시작
                CurrentStage.Shutdown(); // 전투 이벤트와 안전 상태 정리
            } // 스테이지 정리 종료
            CurrentPlayer = null; // 플레이어 참조 해제
            CurrentStage = null; // 스테이지 참조 해제
            if (battleRoot != null) // 출격 객체 확인
            { // 출격 정리 시작
                battleRoot.SetActive(false); // 같은 프레임 전투 중지
                if (Application.isPlaying) // 플레이 상태 확인
                { // 지연 정리 시작
                    Destroy(battleRoot); // 프레임 종료 후 제거
                } // 지연 정리 종료
                else // 편집 시험 상태
                { // 즉시 정리 시작
                    DestroyImmediate(battleRoot); // 시험 객체 즉시 제거
                } // 즉시 정리 종료
                battleRoot = null; // 출격 객체 참조 해제
            } // 출격 정리 종료
            if (menuCamera != null) // 메뉴 카메라 확인
            { // 메뉴 복귀 시작
                menuCamera.enabled = true; // 메뉴 카메라 표시
            } // 메뉴 복귀 종료
        } // 정리 종료
        private void OnDestroy() // 화면 연결 제거
        { // 제거 시작
            if (Flow != null) // 화면 흐름 확인
            { // 이벤트 해제 시작
                Flow.RunRequested -= StartBattle; // 출격 요청 해제
                Flow.LeaveBattleRequested -= ClearBattle; // 전투 정리 해제
                Flow.SettingsRequested -= OpenSettings; // 설정 요청 해제
            } // 이벤트 해제 종료
            if (overlay != null) // 일시정지 화면 확인
            { // 이벤트 해제 시작
                overlay.RetryRequested -= RetryBattle; // 재시도 연결 해제
                overlay.ExitRequested -= ReturnToPreparation; // 복귀 연결 해제
            } // 이벤트 해제 종료
            ClearBattle(); // 출격 객체 정리
        } // 제거 종료
    } // 클래스 종료
} // 영역 종료
