using System; // 화면 연결 알림
using System.Text; // 결과 카드 문자열
using UnityEngine; // 유니티 화면 기능
using UnityEngine.InputSystem; // 키보드와 마우스 입력
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum GameFlowScreen // 메뉴와 전투 화면 종류
    { // 목록 시작
        Title, CharacterSelect, Preparation, Battle, Result, Growth // 화면 순서
    } // 목록 종료
    public sealed class GameFlowController : MonoBehaviour // 타이틀부터 결과까지 흐름
    { // 클래스 시작
        private enum ExitChoice // 결과 화면 이동 확인
        { // 목록 시작
            None, Retry, Preparation // 대기와 이동 종류
        } // 목록 종료
        private enum SettlementAction // 저장 후 이어갈 화면
        { // 목록 시작
            None, ClearResult, FailedResult, Retry, Preparation // 정산 후 동작
        } // 목록 종료
        private Camera menuCamera; // 메뉴 화면 카메라
        private SettlementAction settlementAction; // 저장 재시도 대기 동작
        private float settlementPreviousScale; // 정산 대기 전 시간 배율
        private bool ownsSettlementPause; // 정산 대기 시간 정지 소유
        private SettlementReceipt receipt; // 저장된 정산 영수증
        public Func<bool, bool> SettlementRequested; // 출격 결과 저장 요청
        public Func<CharacterType, string> ProgressDescription; // 캐릭터 성장 설명 조회
        public Func<bool> MeasurementRetryRequested; // 측정 보고 저장 재시도 연결
        public bool SettlementPending => settlementAction != SettlementAction.None; // 정산 재시도 대기
        public string MeasurementNotice // 측정 보고 저장 안내
        { // 속성 시작
            get; // 안내 조회
            private set; // 안내 변경
        } // 속성 종료
        public string RecoveryNotice // 이전 출격 복구 안내
        { // 속성 시작
            get; // 안내 조회
            private set; // 안내 변경
        } // 속성 종료
        public string ErrorText // 저장 실패 안내
        { // 속성 시작
            get; // 오류 조회
            private set; // 오류 변경
        } // 속성 종료
        private BattlePlayer battlePlayer; // 마지막 출격 플레이어
        private StageProgression observedStage; // 결과 알림 연결
        private ExitChoice pendingExit; // 확인 대기 작업
        private bool confirmSelected; // 확인 버튼 선택 상태
        private bool ownsResultPause; // 결과 화면 시간 정지 소유
        private bool resultCleared; // 클리어 결과 상태
        private float previousTimeScale = 1f; // 결과 이전 시간 배율
        private int focus; // 현재 선택 메뉴 번호
        private int enteredFrame = -1; // 화면 전환 프레임
        private Font koreanFont; // 한글 글꼴
        private GUIStyle titleStyle; // 제목 글꼴
        private GUIStyle textStyle; // 본문 글꼴
        private GUIStyle buttonStyle; // 버튼 글꼴
        private Texture2D background; // 메뉴 배경 이미지
        private Texture2D panel; // 메뉴 프레임 이미지
        private Texture2D rapidImage; // 아리아 이미지
        private Texture2D sniperImage; // 리안 이미지
        public event Action<CharacterType, CommonSkill> RunRequested; // 선택한 캐릭터 출격 요청
        public event Action LeaveBattleRequested; // 기존 출격 정리 요청
        public event Action SettingsRequested; // 타이틀 설정 요청
        public GameFlowScreen Screen // 현재 화면
        { // 속성 시작
            get; // 화면 조회
            private set; // 화면 변경
        } // 속성 종료
        public CharacterType SelectedCharacter // 선택 캐릭터
        { // 속성 시작
            get; // 캐릭터 조회
            private set; // 캐릭터 변경
        } // 속성 종료
        public CommonSkill SelectedCommonSkill // 출격 공용 스킬
        { // 속성 시작
            get; // 스킬 조회
            private set; // 스킬 변경
        } // 속성 종료
        public bool InputBlocked // 다른 모달의 입력 차단
        { // 속성 시작
            get; // 차단 상태 조회
            set; // 차단 상태 변경
        } // 속성 종료
        public bool ConfirmingResultExit => pendingExit != ExitChoice.None; // 결과 이탈 확인 여부
        public bool IsBattleActive => Screen == GameFlowScreen.Battle; // 전투 화면 여부
        public void Begin(Camera camera) // 메뉴 흐름 초기화
        { // 초기화 시작
            ReleaseResultPause(); // 이전 결과 정지 복원
            ReleaseSettlementPause(); // 이전 저장 대기 정지 복원
            settlementAction = SettlementAction.None; // 이전 저장 대기 초기화
            ErrorText = ""; // 이전 오류 초기화
            RecoveryNotice = ""; // 이전 복구 안내 초기화
            MeasurementNotice = ""; // 이전 측정 안내 초기화
            receipt = null; // 이전 영수증 초기화
            DetachBattle(); // 이전 전투 알림 해제
            menuCamera = camera; // 메뉴 카메라 보관
            InputBlocked = false; // 이전 모달 제한 초기화
            SelectedCharacter = CharacterType.Rapid; // 초기 캐릭터 선택
            SelectedCommonSkill = CommonSkill.Shield; // 초기 공용 스킬 선택
            background = Resources.Load<Texture2D>("BattleArt/stage_sky_outskirts"); // 하늘섬 메뉴 배경
            panel = Resources.Load<Texture2D>("BattleArt/ui_card_frame"); // 메뉴 프레임 이미지
            rapidImage = Resources.Load<Texture2D>("BattleArt/player_rapid"); // 아리아 이미지
            sniperImage = Resources.Load<Texture2D>("BattleArt/player_sniper"); // 리안 이미지
            pendingExit = ExitChoice.None; // 이전 확인 초기화
            SetScreen(GameFlowScreen.Title); // 타이틀 표시
        } // 초기화 종료
        public bool OpenCharacterSelection() // 캐릭터 목록 열기
        { // 전환 시작
            if (Screen != GameFlowScreen.Title || InputBlocked) // 시작 조건 확인
            { // 차단 시작
                return false; // 다른 화면 전환 차단
            } // 차단 종료
            SetScreen(GameFlowScreen.CharacterSelect); // 캐릭터 선택 표시
            return true; // 전환 완료
        } // 전환 종료
        public bool SelectCharacter(CharacterType character) // 캐릭터 선택
        { // 선택 시작
            if (Screen != GameFlowScreen.CharacterSelect || InputBlocked || (character != CharacterType.Rapid && character != CharacterType.Sniper)) // 선택 조건 확인
            { // 차단 시작
                return false; // 잘못된 선택 차단
            } // 차단 종료
            SelectedCharacter = character; // 선택 캐릭터 보관
            focus = character == CharacterType.Rapid ? 0 : 1; // 선택 목록 위치
            return true; // 선택 완료
        } // 선택 종료
        public bool OpenPreparation() // 출격 준비 열기
        { // 전환 시작
            if (Screen != GameFlowScreen.CharacterSelect || InputBlocked) // 전환 조건 확인
            { // 차단 시작
                return false; // 다른 화면 전환 차단
            } // 차단 종료
            SetScreen(GameFlowScreen.Preparation); // 출격 준비 표시
            return true; // 전환 완료
        } // 전환 종료
        public bool OpenGrowth() // 영구 성장 화면 열기
        { // 전환 시작
            if (Screen != GameFlowScreen.Preparation || InputBlocked || SettlementPending) // 화면과 입력 확인
            { // 차단 시작
                return false; // 다른 상황의 전환 차단
            } // 차단 종료
            SetScreen(GameFlowScreen.Growth); // 선택 캐릭터 성장 화면
            return true; // 화면 전환 완료
        } // 전환 종료
        public bool SelectGrowthCharacter(CharacterType character) // 성장 화면 캐릭터 선택
        { // 선택 시작
            if (Screen != GameFlowScreen.Growth || InputBlocked || SettlementPending || (character != CharacterType.Rapid && character != CharacterType.Sniper)) // 화면과 선택 확인
            { // 차단 시작
                return false; // 잘못된 선택 차단
            } // 차단 종료
            SelectedCharacter = character; // 준비와 출격 캐릭터 동기화
            return true; // 캐릭터 선택 완료
        } // 선택 종료
        public string GrowthSummary() // 선택 캐릭터 성장 정보
        { // 정보 생성 시작
            string character = SelectedCharacter == CharacterType.Rapid ? "아리아" : "리안"; // 선택 캐릭터 이름
            CardData first = CardCatalog.Find(SelectedCharacter == CharacterType.Rapid ? CardId.R01 : CardId.S01); // 첫 기본 후보 조회
            CardData second = CardCatalog.Find(SelectedCharacter == CharacterType.Rapid ? CardId.R02 : CardId.S02); // 두 번째 기본 후보 조회
            string progress = ProgressDescription != null ? ProgressDescription(SelectedCharacter) : "영구 성장 정보를 불러올 수 없습니다."; // 실제 보유량 또는 오류 안내
            return character + "  영구 성장\n\n" + progress + "\n\n무료 기본 후보 · 레벨업 시 제시 가능\n" + first.Name + "  :  " + first.Description + "\n" + second.Name + "  :  " + second.Description + "\n\n유료 해금 상품: 미정\n첫 고유 과제: 준비 중"; // 저장된 성장과 준비 중인 과제
        } // 정보 생성 종료
        public bool SelectCommonSkill(CommonSkill skill) // 공용 스킬 선택
        { // 선택 시작
            if (Screen != GameFlowScreen.Preparation || InputBlocked || (skill != CommonSkill.Shield && skill != CommonSkill.Scatter)) // 선택 조건 확인
            { // 차단 시작
                return false; // 잘못된 선택 차단
            } // 차단 종료
            SelectedCommonSkill = skill; // 출격 스킬 보관
            return true; // 선택 완료
        } // 선택 종료
        public bool StartRun() // 출격 요청
        { // 출격 시작
            if (Screen != GameFlowScreen.Preparation || InputBlocked) // 출격 조건 확인
            { // 차단 시작
                return false; // 잘못된 출격 차단
            } // 차단 종료
            ErrorText = ""; // 이전 출격 오류 초기화
            BeginRun(); // 선택 캐릭터 출격
            return Screen == GameFlowScreen.Battle; // 출격 기록과 시작 결과
        } // 출격 종료
        public void AttachBattle(BattlePlayer player, StageDirector stage) // 출격 전투 연결
        { // 연결 시작
            DetachBattle(); // 이전 연결 정리
            battlePlayer = player; // 결과 기록 대상 보관
            observedStage = stage != null ? stage.State : null; // 스테이지 상태 보관
            if (observedStage != null) // 진행 상태 확인
            { // 구독 시작
                observedStage.PhaseChanged += OnPhaseChanged; // 결과 변경 연결
            } // 구독 종료
        } // 연결 종료
        public void ShowResult(bool cleared) // 시험 결과 화면 열기
        { // 전환 시작
            if (Screen != GameFlowScreen.Battle) // 전투 화면 확인
            { // 차단 시작
                return; // 중복 결과 차단
            } // 차단 종료
            RequestSettlement(cleared ? SettlementAction.ClearResult : SettlementAction.FailedResult, cleared); // 저장 후 결과 표시
        } // 전환 종료
        public bool RequestRetry() // 결과 재시도 확인
        { // 요청 시작
            if (Screen != GameFlowScreen.Result || InputBlocked || ConfirmingResultExit) // 요청 조건 확인
            { // 차단 시작
                return false; // 중복 요청 차단
            } // 차단 종료
            pendingExit = ExitChoice.Retry; // 재시도 확인 대기
            confirmSelected = false; // 안전한 취소 기본 선택
            enteredFrame = Time.frameCount; // 같은 프레임 입력 차단
            return true; // 확인 표시
        } // 요청 종료
        public bool RequestReturn() // 결과에서 출격 준비 복귀 확인
        { // 요청 시작
            if (Screen != GameFlowScreen.Result || InputBlocked || ConfirmingResultExit) // 요청 조건 확인
            { // 차단 시작
                return false; // 중복 요청 차단
            } // 차단 종료
            pendingExit = ExitChoice.Preparation; // 준비 화면 확인 대기
            confirmSelected = false; // 안전한 취소 기본 선택
            enteredFrame = Time.frameCount; // 같은 프레임 입력 차단
            return true; // 확인 표시
        } // 요청 종료
        public bool ConfirmResultExit(bool accepted) // 결과 이탈 확정
        { // 처리 시작
            if (Screen != GameFlowScreen.Result || pendingExit == ExitChoice.None || InputBlocked) // 확인 상태 검증
            { // 차단 시작
                return false; // 잘못된 확인 차단
            } // 차단 종료
            ExitChoice choice = pendingExit; // 대기 작업 보관
            pendingExit = ExitChoice.None; // 확인 상태 종료
            enteredFrame = Time.frameCount; // 같은 프레임 입력 차단
            if (!accepted) // 취소 확인
            { // 취소 시작
                return true; // 결과 화면 유지
            } // 취소 종료
            ReleaseResultPause(); // 결과 시간 정지 해제
            DetachBattle(); // 이전 전투 참조 해제
            LeaveBattleRequested?.Invoke(); // 이전 출격 정리
            if (choice == ExitChoice.Retry) // 재시도 확인
            { // 재시도 시작
                BeginRun(); // 같은 선택으로 출격
            } // 재시도 종료
            else // 출격 준비 복귀
            { // 복귀 시작
                SetScreen(GameFlowScreen.Preparation); // 준비 화면 표시
            } // 복귀 종료
            return true; // 확인 완료
        } // 처리 종료
        public bool Back() // 메뉴 이전 화면 이동
        { // 이동 시작
            if (InputBlocked || ConfirmingResultExit) // 입력 제한 확인
            { // 차단 시작
                return false; // 제한 중 이동 차단
            } // 차단 종료
            if (Screen == GameFlowScreen.CharacterSelect) // 캐릭터 화면 확인
            { // 타이틀 복귀 시작
                SetScreen(GameFlowScreen.Title); // 타이틀 표시
                return true; // 복귀 완료
            } // 타이틀 복귀 종료
            if (Screen == GameFlowScreen.Growth) // 영구 성장 화면 확인
            { // 준비 복귀 시작
                SetScreen(GameFlowScreen.Preparation); // 출격 준비 화면 표시
                focus = 2; // 성장 버튼 포커스 복원
                return true; // 복귀 완료
            } // 준비 복귀 종료
            if (Screen == GameFlowScreen.Preparation) // 준비 화면 확인
            { // 캐릭터 복귀 시작
                SetScreen(GameFlowScreen.CharacterSelect); // 캐릭터 화면 표시
                return true; // 복귀 완료
            } // 캐릭터 복귀 종료
            return Screen == GameFlowScreen.Result && RequestReturn(); // 결과 화면 이동 확인
        } // 이동 종료
        public bool ReturnFromPause() // 일시정지에서 출격 준비 복귀
        { // 복귀 시작
            if (Screen != GameFlowScreen.Battle) // 전투 상태 확인
            { // 차단 시작
                return false; // 다른 화면 복귀 차단
            } // 차단 종료
            return RequestSettlement(SettlementAction.Preparation, false); // 실패 정산 후 준비 화면 복귀
        } // 복귀 종료
        public bool RetryFromPause() // 일시정지에서 현재 조건 재출격
        { // 재시도 시작
            if (Screen != GameFlowScreen.Battle) // 전투 상태 확인
            { // 차단 시작
                return false; // 다른 화면 재시도 차단
            } // 차단 종료
            return RequestSettlement(SettlementAction.Retry, false); // 실패 정산 후 같은 조건 재출격
        } // 재시도 종료
        public bool ReturnFromBattle() => ReturnFromPause(); // 전투에서 출격 준비 복귀
        public bool RetryBattle() => RetryFromPause(); // 전투에서 같은 조건 재출격
        public void SetRecoveryNotice(string message) // 중단 출격 복구 결과 안내
        { // 안내 시작
            RecoveryNotice = message ?? ""; // 실제 복구 결과 보관
        } // 안내 종료
        public void ReportStartError(string error) // 출격 시작 오류 표시
        { // 오류 처리 시작
            ErrorText = string.IsNullOrWhiteSpace(error) ? "출격 기록을 저장하지 못했습니다." : error; // 읽기 가능한 오류 안내
            SetScreen(GameFlowScreen.Preparation); // 출격 준비 화면 유지
        } // 오류 처리 종료
        public void ReportSettlementError(string error) // 정산 저장 오류 표시
        { // 오류 처리 시작
            ErrorText = string.IsNullOrWhiteSpace(error) ? "정산 내용을 저장하지 못했습니다." : error; // 읽기 가능한 오류 안내
        } // 오류 처리 종료
        public void SetReceipt(SettlementReceipt value) // 저장된 영수증 연결
        { // 연결 시작
            receipt = value; // 정산 결과 보관
        } // 연결 종료
        private bool RequestSettlement(SettlementAction action, bool cleared) // 저장과 화면 전환 요청
        { // 요청 시작
            if (SettlementPending) // 기존 저장 대기 확인
            { // 차단 시작
                return false; // 중복 정산 차단
            } // 차단 종료
            if (SettlementRequested != null && !SettlementRequested(cleared)) // 실제 저장 성공 확인
            { // 저장 실패 시작
                settlementAction = action; // 저장 후 동작 보관
                settlementPreviousScale = Time.timeScale; // 이전 시간 배율 보관
                Time.timeScale = 0f; // 전투 정지 유지
                ownsSettlementPause = true; // 정산 정지 소유
                enteredFrame = Time.frameCount; // 같은 프레임 중복 입력 방지
                return false; // 출격 상태와 재시도 표시
            } // 저장 실패 종료
            ErrorText = ""; // 이전 오류 해제
            CompleteSettlementAction(action); // 저장 후 화면 진행
            return true; // 정산과 전환 성공
        } // 요청 종료
        public bool RetrySettlement() // 실패한 정산 재시도
        { // 재시도 시작
            if (!SettlementPending) // 대기 상태 확인
            { // 차단 시작
                return false; // 불필요한 재시도 차단
            } // 차단 종료
            SettlementAction action = settlementAction; // 대기 동작 보관
            bool cleared = action == SettlementAction.ClearResult; // 클리어 정산 여부
            if (SettlementRequested != null && !SettlementRequested(cleared)) // 저장 재시도 확인
            { // 실패 시작
                enteredFrame = Time.frameCount; // 같은 입력 재처리 방지
                return false; // 데이터와 대기 화면 유지
            } // 실패 종료
            settlementAction = SettlementAction.None; // 대기 동작 해제
            ReleaseSettlementPause(); // 이전 시간 배율 복원
            ErrorText = ""; // 오류 표시 해제
            CompleteSettlementAction(action); // 저장 후 화면 진행
            return true; // 정산 재시도 성공
        } // 재시도 종료
        private void CompleteSettlementAction(SettlementAction action) // 정산 성공 후 화면 전환
        { // 전환 시작
            if (action == SettlementAction.ClearResult || action == SettlementAction.FailedResult) // 결과 화면 확인
            { // 결과 표시 시작
                resultCleared = action == SettlementAction.ClearResult; // 클리어 여부 보관
                previousTimeScale = Time.timeScale; // 이전 시간 배율 보관
                Time.timeScale = 0f; // 결과 화면 전투 정지
                ownsResultPause = true; // 결과 정지 소유 기록
                pendingExit = ExitChoice.None; // 이전 확인 초기화
                SetScreen(GameFlowScreen.Result); // 저장된 결과 표시
                return; // 전투 정리 생략
            } // 결과 표시 종료
            DetachBattle(); // 기존 전투 알림 해제
            LeaveBattleRequested?.Invoke(); // 이전 출격 정리
            if (action == SettlementAction.Retry) // 재출격 확인
            { // 재출격 시작
                BeginRun(); // 같은 조건으로 재출격
            } // 재출격 종료
            else if (action == SettlementAction.Preparation) // 준비 복귀 확인
            { // 준비 복귀 시작
                SetScreen(GameFlowScreen.Preparation); // 출격 준비 표시
            } // 준비 복귀 종료
        } // 전환 종료
        private void ReleaseSettlementPause() // 저장 대기 시간 복원
        { // 복원 시작
            if (ownsSettlementPause) // 정지 소유 확인
            { // 복원 처리 시작
                Time.timeScale = settlementPreviousScale; // 이전 배율 복원
                ownsSettlementPause = false; // 정지 소유 해제
            } // 복원 처리 종료
        } // 복원 종료
        private void BeginRun() // 선택한 조건으로 출격
        { // 출격 시작
            ReleaseResultPause(); // 이전 결과 시간 복원
            receipt = null; // 이전 출격 영수증 해제
            SetScreen(GameFlowScreen.Battle); // 전투 화면 표시
            RunRequested?.Invoke(SelectedCharacter, SelectedCommonSkill); // 출격 요청 알림
        } // 출격 종료
        private void OnPhaseChanged(StagePhase phase) // 스테이지 결과 감지
        { // 감지 시작
            if (phase == StagePhase.Result || phase == StagePhase.Failed) // 출격 종료 확인
            { // 결과 시작
                ShowResult(phase == StagePhase.Result); // 결과 화면 표시
            } // 결과 종료
        } // 감지 종료
        private void SetScreen(GameFlowScreen next) // 화면과 포커스 변경
        { // 변경 시작
            Screen = next; // 현재 화면 보관
            focus = next == GameFlowScreen.CharacterSelect && SelectedCharacter == CharacterType.Sniper ? 1 : 0; // 화면별 기본 포커스
            enteredFrame = Time.frameCount; // 화면 전환 프레임 보관
        } // 변경 종료
        private void DetachBattle() // 스테이지 연결 정리
        { // 정리 시작
            if (observedStage != null) // 상태 존재 확인
            { // 구독 해제 시작
                observedStage.PhaseChanged -= OnPhaseChanged; // 결과 알림 해제
            } // 구독 해제 종료
            observedStage = null; // 상태 참조 해제
            battlePlayer = null; // 플레이어 참조 해제
        } // 정리 종료
        private void ReleaseResultPause() // 결과 시간 배율 복원
        { // 복원 시작
            if (ownsResultPause) // 정지 소유 확인
            { // 복원 처리 시작
                Time.timeScale = previousTimeScale; // 이전 시간 배율 복원
                ownsResultPause = false; // 정지 소유 해제
            } // 복원 처리 종료
        } // 복원 종료
        private void OnDestroy() // 메뉴 객체 제거
        { // 정리 시작
            DetachBattle(); // 스테이지 알림 해제
            ReleaseResultPause(); // 결과 정지 해제
            ReleaseSettlementPause(); // 정산 대기 정지 해제
        } // 정리 종료
        private void Update() // 메뉴 입력 처리
        { // 입력 시작
            Keyboard measurementKeyboard = Keyboard.current; // 측정 재시도 입력 확인
            if (!SettlementPending && !InputBlocked && !string.IsNullOrEmpty(MeasurementNotice) && measurementKeyboard != null && measurementKeyboard.tabKey.wasPressedThisFrame) // 측정 저장 재시도 키 확인
            { // 재시도 시작
                MeasurementRetryRequested?.Invoke(); // 대기 보고서 저장 재시도
            } // 재시도 종료
            if (SettlementPending) // 저장 재시도 대기 확인
            { // 재시도 입력 시작
                if (Time.frameCount == enteredFrame) // 동일 프레임 확인
                { // 입력 차단 시작
                    return; // 중복 입력 차단
                } // 입력 차단 종료
                Keyboard pendingKeyboard = Keyboard.current; // 현재 키보드 확인
                Mouse pendingMouse = Mouse.current; // 현재 포인터 확인
                if (pendingKeyboard != null && (pendingKeyboard.enterKey.wasPressedThisFrame || pendingKeyboard.numpadEnterKey.wasPressedThisFrame || pendingKeyboard.spaceKey.wasPressedThisFrame)) // 재시도 키 확인
                { // 키 입력 시작
                    RetrySettlement(); // 저장 다시 시도
                } // 키 입력 종료
                else if (pendingMouse != null && pendingMouse.leftButton.wasPressedThisFrame) // 마우스 입력 확인
                { // 마우스 입력 시작
                    Vector2 point = pendingMouse.position.ReadValue(); // 화면 좌표 읽기
                    point.y = UnityEngine.Screen.height - point.y; // 위쪽 기준 좌표 변환
                    if (SettlementRetryRect(View()).Contains(point)) // 재시도 버튼 확인
                    { // 버튼 시작
                        RetrySettlement(); // 저장 다시 시도
                    } // 버튼 종료
                } // 마우스 입력 종료
                return; // 다른 메뉴 입력 차단
            } // 재시도 입력 종료
            if (InputBlocked || Screen == GameFlowScreen.Battle || Time.frameCount == enteredFrame) // 입력 가능 확인
            { // 생략 시작
                return; // 전환 프레임과 전투 입력 제외
            } // 생략 종료
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            Mouse mouse = Mouse.current; // 현재 마우스
            Rect view = View(); // 전투 비율 화면 영역
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) // 포인터 선택 확인
            { // 클릭 시작
                Vector2 position = mouse.position.ReadValue(); // 마우스 위치
                position.y = UnityEngine.Screen.height - position.y; // 화면 위쪽 기준 변환
                HandleClick(view, position); // 클릭 위치 처리
                return; // 같은 프레임 중복 입력 방지
            } // 클릭 종료
            if (keyboard == null) // 키보드 확인
            { // 생략 시작
                return; // 키 입력 생략
            } // 생략 종료
            bool previous = keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame; // 이전 선택 입력
            bool next = keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame; // 다음 선택 입력
            bool submit = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame; // 확인 입력
            bool cancel = keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame; // 뒤로 입력
            if (ConfirmingResultExit) // 결과 이탈 확인 중
            { // 확인 입력 시작
                if (previous || next) // 버튼 이동 확인
                { // 선택 시작
                    confirmSelected = !confirmSelected; // 확인과 취소 전환
                } // 선택 종료
                if (cancel) // 취소 입력 확인
                { // 취소 시작
                    ConfirmResultExit(false); // 결과 화면 유지
                } // 취소 종료
                else if (submit) // 확인 입력 확인
                { // 확인 시작
                    ConfirmResultExit(confirmSelected); // 선택한 버튼 실행
                } // 확인 종료
                return; // 다른 입력 생략
            } // 확인 입력 종료
            if (cancel) // 뒤로 입력 확인
            { // 뒤로 시작
                Back(); // 이전 화면 이동
                return; // 추가 입력 생략
            } // 뒤로 종료
            if (Screen == GameFlowScreen.CharacterSelect && (previous || next)) // 캐릭터 이동 확인
            { // 선택 시작
                SelectCharacter(SelectedCharacter == CharacterType.Rapid ? CharacterType.Sniper : CharacterType.Rapid); // 두 캐릭터 전환
            } // 선택 종료
            else if (Screen == GameFlowScreen.Preparation && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)) // 공용 스킬 이동 확인
            { // 스킬 선택 시작
                SelectCommonSkill(SelectedCommonSkill == CommonSkill.Shield ? CommonSkill.Scatter : CommonSkill.Shield); // 공용 스킬 전환
            } // 스킬 선택 종료
            else if (Screen == GameFlowScreen.Growth && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)) // 성장 캐릭터 이동 확인
            { // 캐릭터 선택 시작
                SelectGrowthCharacter(SelectedCharacter == CharacterType.Rapid ? CharacterType.Sniper : CharacterType.Rapid); // 두 캐릭터 전환
            } // 캐릭터 선택 종료
            else if (previous || next) // 메뉴 포커스 이동 확인
            { // 이동 시작
                int count = Screen == GameFlowScreen.Preparation ? 3 : Screen == GameFlowScreen.Growth ? 1 : 2; // 화면별 버튼 수
                focus = (focus + (next ? 1 : -1) + count) % count; // 포커스 순환
            } // 이동 종료
            if (submit) // 확인 입력 확인
            { // 확인 시작
                ActivateFocused(); // 포커스 버튼 실행
            } // 확인 종료
        } // 입력 종료
        private void ActivateFocused() // 키보드 포커스 실행
        { // 실행 시작
            if (Screen == GameFlowScreen.Title) // 타이틀 확인
            { // 타이틀 시작
                if (focus == 0) // 시작 선택
                { // 시작 처리
                    OpenCharacterSelection(); // 캐릭터 화면 열기
                } // 시작 종료
                else // 설정 선택
                { // 설정 처리
                    if (SettingsRequested != null) // 설정 연결 확인
                    { // 설정 요청 시작
                        InputBlocked = true; // 즉시 메뉴 입력 차단
                        SettingsRequested.Invoke(); // 설정 열기 요청
                    } // 설정 요청 종료
                } // 설정 종료
            } // 타이틀 종료
            else if (Screen == GameFlowScreen.CharacterSelect) // 캐릭터 선택 확인
            { // 캐릭터 시작
                OpenPreparation(); // 선택 캐릭터 준비
            } // 캐릭터 종료
            else if (Screen == GameFlowScreen.Preparation) // 출격 준비 확인
            { // 준비 시작
                if (focus == 0) // 출격 버튼 확인
                { // 출격 시작
                    StartRun(); // 출격 요청
                } // 출격 종료
                else if (focus == 1) // 뒤로 버튼 확인
                { // 뒤로 시작
                    Back(); // 캐릭터 선택 복귀
                } // 뒤로 종료
                else // 성장 버튼 확인
                { // 성장 시작
                    OpenGrowth(); // 영구 성장 화면 열기
                } // 성장 종료
            } // 준비 종료
            else if (Screen == GameFlowScreen.Growth) // 영구 성장 화면 확인
            { // 복귀 시작
                Back(); // 출격 준비 화면 복귀
            } // 복귀 종료
            else if (Screen == GameFlowScreen.Result) // 결과 화면 확인
            { // 결과 시작
                if (focus == 0) // 재시도 버튼
                { // 재시도 시작
                    RequestRetry(); // 진행 손실 확인
                } // 재시도 종료
                else // 준비 복귀 버튼
                { // 복귀 시작
                    RequestReturn(); // 진행 손실 확인
                } // 복귀 종료
            } // 결과 종료
        } // 실행 종료
        private void HandleClick(Rect view, Vector2 position) // 클릭 위치 처리
        { // 처리 시작
            if (ConfirmingResultExit) // 결과 확인 화면
            { // 확인 클릭 시작
                if (ConfirmRect(view, true).Contains(position)) // 확인 버튼 클릭
                { // 확인 시작
                    ConfirmResultExit(true); // 이동 확정
                } // 확인 종료
                else if (ConfirmRect(view, false).Contains(position)) // 취소 버튼 클릭
                { // 취소 시작
                    ConfirmResultExit(false); // 결과 화면 유지
                } // 취소 종료
                return; // 배경 버튼 차단
            } // 확인 클릭 종료
            if (Screen == GameFlowScreen.Growth) // 성장 화면 클릭 확인
            { // 성장 클릭 시작
                if (GrowthCharacterRect(view, 0).Contains(position)) // 아리아 성장 선택
                { // 속사형 선택 시작
                    SelectGrowthCharacter(CharacterType.Rapid); // 속사형 성장 표시
                } // 속사형 선택 종료
                else if (GrowthCharacterRect(view, 1).Contains(position)) // 리안 성장 선택
                { // 저격형 선택 시작
                    SelectGrowthCharacter(CharacterType.Sniper); // 저격형 성장 표시
                } // 저격형 선택 종료
                else if (ContinueRect(view).Contains(position)) // 준비 화면 복귀 확인
                { // 성장 복귀 시작
                    Back(); // 출격 준비 복귀
                } // 성장 복귀 종료
                return; // 다른 화면 클릭 차단
            } // 성장 클릭 종료
            if (Screen == GameFlowScreen.Title || Screen == GameFlowScreen.Preparation || Screen == GameFlowScreen.Result) // 공통 버튼 화면
            { // 버튼 확인 시작
                int count = Screen == GameFlowScreen.Preparation ? 3 : 2; // 화면별 버튼 수
                for (int index = 0; index < count; index++) // 버튼 목록 순회
                { // 순회 시작
                    Rect button = Screen == GameFlowScreen.Preparation && index == 2 ? GrowthButtonRect(view) : ButtonRect(view, index); // 성장 버튼 위치 구분
                    if (button.Contains(position)) // 버튼 영역 확인
                    { // 선택 시작
                        focus = index; // 클릭 포커스 설정
                        ActivateFocused(); // 버튼 실행
                        return; // 중복 클릭 방지
                    } // 선택 종료
                } // 순회 종료
            } // 버튼 확인 종료
            if (Screen == GameFlowScreen.CharacterSelect) // 캐릭터 화면 확인
            { // 캐릭터 클릭 시작
                if (CharacterRect(view, 0).Contains(position)) // 아리아 선택
                { // 선택 시작
                    SelectCharacter(CharacterType.Rapid); // 속사형 선택
                } // 선택 종료
                else if (CharacterRect(view, 1).Contains(position)) // 리안 선택
                { // 선택 시작
                    SelectCharacter(CharacterType.Sniper); // 저격형 선택
                } // 선택 종료
                else if (ContinueRect(view).Contains(position)) // 준비 버튼 선택
                { // 준비 시작
                    OpenPreparation(); // 출격 준비 진입
                } // 준비 종료
            } // 캐릭터 클릭 종료
            else if (Screen == GameFlowScreen.Preparation) // 출격 준비 확인
            { // 스킬 클릭 시작
                if (SkillRect(view, 0).Contains(position)) // 보호막 선택
                { // 보호막 시작
                    SelectCommonSkill(CommonSkill.Shield); // 보호막 장착
                } // 보호막 종료
                else if (SkillRect(view, 1).Contains(position)) // 산탄 선택
                { // 산탄 시작
                    SelectCommonSkill(CommonSkill.Scatter); // 산탄 장착
                } // 산탄 종료
            } // 스킬 클릭 종료
        } // 처리 종료
        private void OnGUI() // 메뉴 화면 그리기
        { // 표시 시작
            if (SettlementPending) // 저장 재시도 화면 확인
            { // 오류 표시 시작
                DrawSettlementError(); // 저장 오류와 재시도 표시
                return; // 다른 메뉴 표시 생략
            } // 오류 표시 종료
            if (Screen == GameFlowScreen.Battle || menuCamera == null) // 메뉴 표시 조건
            { // 생략 시작
                if (Screen == GameFlowScreen.Battle && menuCamera != null && !InputBlocked) // 전투 중 측정 안내 확인
                { // 안내 표시 시작
                    DrawMeasurementNotice(View()); // 미저장 측정 안내 표시
                } // 안내 표시 종료
                return; // 전투 화면 제외
            } // 생략 종료
            PrepareStyles(); // 한글 글꼴 준비
            Rect view = View(); // 화면 비율 영역
            Color previous = GUI.color; // 기존 색상 보관
            GUI.color = Color.white; // 원본 색상
            if (background != null && Screen != GameFlowScreen.Result) // 메뉴 배경 확인
            { // 배경 시작
                GUI.DrawTexture(view, background, ScaleMode.ScaleAndCrop); // 하늘섬 배경
            } // 배경 종료
            GUI.color = new Color(0f, 0f, 0f, Screen == GameFlowScreen.Result ? 0.85f : 0.58f); // 읽기용 어두운 배경
            GUI.DrawTexture(view, Texture2D.whiteTexture); // 배경 음영
            GUI.color = Color.white; // 원본 색상 복원
            titleStyle.fontSize = Mathf.Max(22, Mathf.RoundToInt(view.height * 0.052f)); // 제목 화면 비례 크기
            textStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height * 0.025f)); // 본문 화면 비례 크기
            buttonStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height * 0.026f)); // 버튼 화면 비례 크기
            if (Screen == GameFlowScreen.Title) // 타이틀 화면
            { // 타이틀 표시 시작
                GUI.Label(new Rect(view.x, view.y + view.height * 0.20f, view.width, view.height * 0.12f), "PROJECT G", titleStyle); // 게임 제목
                DrawButton(ButtonRect(view, 0), "시작", focus == 0); // 시작 버튼
                DrawButton(ButtonRect(view, 1), "설정", focus == 1); // 설정 버튼
            } // 타이틀 표시 종료
            else if (Screen == GameFlowScreen.CharacterSelect || Screen == GameFlowScreen.Preparation) // 캐릭터와 준비 화면
            { // 준비 표시 시작
                DrawPreparation(view); // 선택 정보 표시
            } // 준비 표시 종료
            else if (Screen == GameFlowScreen.Growth) // 영구 성장 화면
            { // 성장 표시 시작
                DrawGrowth(view); // 선택 캐릭터 성장 표시
            } // 성장 표시 종료
            else if (Screen == GameFlowScreen.Result) // 결과 화면
            { // 결과 표시 시작
                DrawResult(view); // 결과와 기록 표시
            } // 결과 표시 종료
            if (Screen == GameFlowScreen.Preparation && !string.IsNullOrEmpty(RecoveryNotice)) // 이전 출격 복구 확인
            { // 복구 안내 시작
                GUI.Label(new Rect(view.x + view.width * 0.10f, view.y + view.height * 0.12f, view.width * 0.80f, view.height * 0.05f), RecoveryNotice, textStyle); // 복구된 참여 보상 표시
            } // 복구 안내 종료
            if (Screen == GameFlowScreen.Preparation && !string.IsNullOrEmpty(ErrorText)) // 출격 저장 오류 확인
            { // 오류 표시 시작
                GUI.Label(new Rect(view.x + view.width * 0.10f, view.y + view.height * 0.91f, view.width * 0.80f, view.height * 0.07f), "출격 불가: " + ErrorText, textStyle); // 출격 오류 표시
            } // 오류 표시 종료
            if (ConfirmingResultExit) // 결과 이동 확인
            { // 확인 표시 시작
                Rect box = new Rect(view.x + view.width * 0.25f, view.y + view.height * 0.32f, view.width * 0.5f, view.height * 0.32f); // 확인 패널 영역
                DrawPanel(box); // 확인 패널 이미지
                GUI.Label(new Rect(box.x + box.width * 0.08f, box.y + box.height * 0.12f, box.width * 0.84f, box.height * 0.35f), "전투 중 임시 강화가 초기화됩니다.\n계속하시겠습니까?", textStyle); // 진행 손실 안내
                DrawButton(ConfirmRect(view, true), "확인", confirmSelected); // 이동 확인
                DrawButton(ConfirmRect(view, false), "취소", !confirmSelected); // 이동 취소
            } // 확인 표시 종료
            if (!InputBlocked) // 설정 화면과 겹침 확인
            { // 안내 표시 시작
                DrawMeasurementNotice(view); // 미저장 측정 안내 표시
            } // 안내 표시 종료
            GUI.color = previous; // 기존 색상 복원
        } // 표시 종료
        public void SetMeasurementNotice(string notice) // 측정 저장 상태 안내 변경
        { // 변경 시작
            MeasurementNotice = notice ?? ""; // 빈 값과 오류 안내 보관
        } // 변경 종료
        private void DrawMeasurementNotice(Rect view) // 측정 저장 오류 표시
        { // 표시 시작
            if (string.IsNullOrEmpty(MeasurementNotice)) // 미저장 안내 확인
            { // 생략 시작
                return; // 안내 없는 경우 제외
            } // 생략 종료
            PrepareStyles(); // 한글 글꼴 준비
            textStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height * 0.022f)); // 화면 비례 안내 크기
            Rect area = new Rect(view.x + view.width * 0.02f, view.y + view.height * 0.91f, view.width * 0.70f, view.height * 0.07f); // 화면 하단 안내 영역
            DrawPanel(area); // 기존 이미지 프레임 표시
            GUI.Label(new Rect(area.x + area.width * 0.03f, area.y, area.width * 0.94f, area.height), MeasurementNotice, textStyle); // 오류와 재시도 안내
        } // 표시 종료
        private void DrawPreparation(Rect view) // 캐릭터와 출격 정보 표시
        { // 표시 시작
            bool choosing = Screen == GameFlowScreen.CharacterSelect; // 캐릭터 선택 여부
            string heading = choosing ? "캐릭터 선택" : "출격 준비"; // 화면 제목
            GUI.Label(new Rect(view.x, view.y + view.height * 0.04f, view.width, view.height * 0.09f), heading, titleStyle); // 화면 제목 표시
            if (choosing) // 캐릭터 목록 확인
            { // 목록 표시 시작
                DrawButton(CharacterRect(view, 0), "아리아 · 속사형", SelectedCharacter == CharacterType.Rapid); // 속사형 선택
                DrawButton(CharacterRect(view, 1), "리안 · 저격형", SelectedCharacter == CharacterType.Sniper); // 저격형 선택
                DrawButton(ContinueRect(view), "출격 준비  Enter", true); // 준비 화면 버튼
            } // 목록 표시 종료
            Texture2D portrait = SelectedCharacter == CharacterType.Rapid ? rapidImage : sniperImage; // 선택 캐릭터 그림
            if (portrait != null) // 이미지 확인
            { // 이미지 시작
                GUI.DrawTexture(new Rect(view.x + view.width * 0.20f, view.y + view.height * 0.17f, view.width * 0.33f, view.height * 0.58f), portrait, ScaleMode.ScaleToFit); // 캐릭터 그림
            } // 이미지 종료
            Rect info = new Rect(view.x + view.width * 0.53f, view.y + view.height * 0.17f, view.width * 0.40f, view.height * 0.53f); // 정보 패널 위치
            DrawPanel(info); // 정보 배경
            string details = SelectedCharacter == CharacterType.Rapid ? "아리아 · 속사형\n소속: 조사팀\n기본: 좌우 자동 사격\n집중: 전방 집중 사격\n패시브: 연속 적중 시 피해 누적\nJ: 집중 포화\nL: 유성 탄막" : "리안 · 저격형\n소속: 수호팀\n기본: 관통 자동 사격\n집중: 강력한 집중 사격\n패시브: 노출 약점 추가 피해\nJ: 관통 사격\nL: 천공 관측선"; // 기획서 기준 캐릭터 정보
            details += "\nK: 공용 교체 스킬\n스킬명은 기획안, 이미지는 시험용"; // 기획안과 시험 이미지 안내
            GUI.Label(new Rect(info.x + info.width * 0.08f, info.y + info.height * 0.08f, info.width * 0.84f, info.height * 0.84f), details, textStyle); // 캐릭터 상세 정보
            if (!choosing) // 출격 준비 화면 확인
            { // 스킬 표시 시작
                DrawButton(SkillRect(view, 0), "K 보호막", SelectedCommonSkill == CommonSkill.Shield); // 보호막 선택
                DrawButton(SkillRect(view, 1), "K 산탄", SelectedCommonSkill == CommonSkill.Scatter); // 산탄 선택
                DrawButton(ButtonRect(view, 0), "출격", focus == 0); // 출격 버튼
                DrawButton(ButtonRect(view, 1), "뒤로", focus == 1); // 뒤로 버튼
                DrawButton(GrowthButtonRect(view), "영구 성장", focus == 2); // 성장 화면 버튼
                GUI.Label(new Rect(view.x + view.width * 0.20f, view.y + view.height * 0.70f, view.width * 0.31f, view.height * 0.06f), "좌우: K 선택 · Enter: 확인", textStyle); // 키 조작 안내
                if (ProgressDescription != null) // 영구 성장 정보 연결 확인
                { // 성장 표시 시작
                    GUI.Label(new Rect(view.x + view.width * 0.53f, view.y + view.height * 0.70f, view.width * 0.40f, view.height * 0.09f), ProgressDescription(SelectedCharacter), textStyle); // 보유 성장과 해금 안내
                } // 성장 표시 종료
            } // 스킬 표시 종료
        } // 표시 종료
        private void DrawGrowth(Rect view) // 영구 성장 정보 표시
        { // 표시 시작
            GUI.Label(new Rect(view.x, view.y + view.height * 0.05f, view.width, view.height * 0.10f), "영구 성장", titleStyle); // 성장 화면 제목
            DrawButton(GrowthCharacterRect(view, 0), "아리아", SelectedCharacter == CharacterType.Rapid); // 속사형 성장 선택
            DrawButton(GrowthCharacterRect(view, 1), "리안", SelectedCharacter == CharacterType.Sniper); // 저격형 성장 선택
            Rect area = new Rect(view.x + view.width * 0.20f, view.y + view.height * 0.26f, view.width * 0.60f, view.height * 0.49f); // 성장 정보 영역
            DrawPanel(area); // 성장 정보 배경
            GUI.Label(new Rect(area.x + area.width * 0.08f, area.y + area.height * 0.08f, area.width * 0.84f, area.height * 0.84f), GrowthSummary(), textStyle); // 실제 보유량과 기본 후보
            GUI.Label(new Rect(view.x + view.width * 0.25f, view.y + view.height * 0.76f, view.width * 0.50f, view.height * 0.04f), "좌우 / A·D: 캐릭터 선택 · Esc: 돌아가기", textStyle); // 성장 화면 조작 안내
            DrawButton(ContinueRect(view), "출격 준비로 돌아가기", true); // 준비 화면 복귀 버튼
        } // 표시 종료
        private void DrawResult(Rect view) // 출격 결과와 기록 표시
        { // 표시 시작
            string heading = resultCleared ? "스테이지 클리어" : "출격 실패"; // 결과 제목
            GUI.Label(new Rect(view.x, view.y + view.height * 0.04f, view.width, view.height * 0.10f), heading, titleStyle); // 결과 제목 표시
            Rect info = new Rect(view.x + view.width * 0.20f, view.y + view.height * 0.16f, view.width * 0.60f, view.height * 0.51f); // 기록 패널 위치
            DrawPanel(info); // 기록 배경
            string summary = ResultSummary(); // 실제 전투 기록 생성
            GUI.Label(new Rect(info.x + info.width * 0.08f, info.y + info.height * 0.07f, info.width * 0.84f, info.height * 0.86f), summary, textStyle); // 전투 기록 표시
            DrawButton(ButtonRect(view, 0), "재시도", focus == 0); // 재시도 버튼
            DrawButton(ButtonRect(view, 1), "출격 준비", focus == 1); // 준비 화면 복귀
        } // 표시 종료
        private string ResultSummary() // 실제 결과 기록 요약
        { // 요약 시작
            if (battlePlayer == null || battlePlayer.Statistics == null || !battlePlayer.Statistics.Finished) // 기록 준비 확인
            { // 미준비 시작
                return "전투 기록을 확인할 수 없습니다.\n보상·영구 저장: 미구현"; // 미구현 구분
            } // 미준비 종료
            RunStatistics stats = battlePlayer.Statistics; // 출격 기록 조회
            StringBuilder cards = new StringBuilder(); // 카드 이름 목록
            foreach (CardId card in stats.Cards) // 선택 카드 순회
            { // 카드 기록 시작
                if (cards.Length > 0) // 구분자 확인
                { // 구분 시작
                    cards.Append(", "); // 이름 구분자
                } // 구분 종료
                cards.Append(CardCatalog.Find(card).Name); // 실제 선택 카드 이름
            } // 카드 기록 종료
            string chosen = cards.Length == 0 ? "없음" : cards.ToString(); // 카드 미선택 표시
            string reward = receipt == null ? "정산 정보 없음" : "숙련도 +" + receipt.MasteryGained + "   공통 재료 +" + receipt.CommonMaterialsGained + "   전용 재료 +" + receipt.CharacterMaterialsGained; // 저장된 지급 내역
            return "전투 시간: " + Mathf.FloorToInt(stats.CombatSeconds) + "초\n전체 시간: " + Mathf.FloorToInt(stats.TotalSeconds) + "초\n처치: " + stats.Defeats + "   그레이즈: " + stats.Grazes + "   피격: " + stats.Hits + "\n선택 카드: " + chosen + "\n\n" + reward + "\n첫 고유 과제: 준비 중\n전투 기록은 저장되지 않습니다."; // 실제 기록과 정산 표시
        } // 요약 종료
        private void DrawSettlementError() // 정산 오류와 재시도 화면
        { // 표시 시작
            if (menuCamera == null) // 화면 연결 확인
            { // 생략 시작
                return; // 화면 없이 표시 생략
            } // 생략 종료
            int previousDepth = GUI.depth; // 기존 표시 순서 보관
            GUI.depth = -200; // 오류 화면 우선 표시
            PrepareStyles(); // 한글 글꼴 준비
            Rect view = View(); // 전투 화면 영역
            Color previous = GUI.color; // 기존 색상 보관
            GUI.color = new Color(0f, 0f, 0f, 0.90f); // 오류 배경 색상
            GUI.DrawTexture(view, Texture2D.whiteTexture); // 전투 화면 가림
            GUI.color = Color.white; // 표시 색상 복원
            Rect panelArea = new Rect(view.x + view.width * 0.20f, view.y + view.height * 0.25f, view.width * 0.60f, view.height * 0.50f); // 오류 패널 영역
            DrawPanel(panelArea); // 오류 패널 표시
            textStyle.fontSize = Mathf.Max(16, Mathf.RoundToInt(view.height * 0.028f)); // 오류 글자 크기
            buttonStyle.fontSize = Mathf.Max(16, Mathf.RoundToInt(view.height * 0.028f)); // 버튼 글자 크기
            GUI.Label(new Rect(panelArea.x + panelArea.width * 0.08f, panelArea.y + panelArea.height * 0.10f, panelArea.width * 0.84f, panelArea.height * 0.50f), "보상 저장 실패: " + ErrorText + "\n출격 기록을 보존하고 있습니다. 저장 공간과 권한을 확인해 주세요.", textStyle); // 저장 실패 안내
            DrawButton(SettlementRetryRect(view), "저장 다시 시도  Enter", true); // 재시도 버튼
            GUI.color = previous; // 기존 색상 복원
            GUI.depth = previousDepth; // 기존 표시 순서 복원
        } // 표시 종료
        private void DrawPanel(Rect area) // 생성 프레임 표시
        { // 표시 시작
            if (panel != null) // 프레임 존재 확인
            { // 이미지 시작
                GUI.DrawTexture(area, panel, ScaleMode.StretchToFill); // 생성 프레임
            } // 이미지 종료
            else // 자원 누락
            { // 대체 시작
                GUI.Box(area, GUIContent.none); // 기본 패널 표시
            } // 대체 종료
        } // 표시 종료
        private void DrawButton(Rect area, string label, bool selected) // 버튼과 선택 표시
        { // 표시 시작
            DrawPanel(area); // 버튼 배경
            Color previous = GUI.color; // 이전 색상 보관
            GUI.color = selected ? new Color(0.55f, 1f, 1f) : Color.white; // 선택 강조 색상
            GUI.Label(area, label, buttonStyle); // 버튼 글자
            GUI.color = previous; // 이전 색상 복원
        } // 표시 종료
        private void PrepareStyles() // 한글 UI 글꼴 준비
        { // 준비 시작
            if (textStyle != null) // 준비 상태 확인
            { // 재사용 시작
                return; // 기존 글꼴 유지
            } // 재사용 종료
            string[] names = new string[] // 글꼴 후보 목록
            { // 목록 시작
                "Malgun Gothic", // 윈도우 한글 글꼴
                "Arial" // 기본 대체 글꼴
            }; // 목록 종료
            koreanFont = Font.CreateDynamicFontFromOSFont(names, 18); // 시스템 한글 글꼴
            textStyle = new GUIStyle(GUI.skin.label); // 본문 스타일 생성
            textStyle.font = koreanFont != null ? koreanFont : GUI.skin.font; // 사용 가능한 글꼴
            textStyle.normal.textColor = Color.white; // 흰색 글자
            textStyle.alignment = TextAnchor.UpperLeft; // 본문 왼쪽 정렬
            textStyle.wordWrap = true; // 설명 자동 줄바꿈
            titleStyle = new GUIStyle(textStyle); // 제목 스타일 복사
            titleStyle.fontStyle = FontStyle.Bold; // 제목 굵게 표시
            titleStyle.alignment = TextAnchor.MiddleCenter; // 제목 중앙 정렬
            buttonStyle = new GUIStyle(titleStyle); // 버튼 스타일 복사
        } // 준비 종료
        private static Rect View() // 화면 비율 영역 계산
        { // 계산 시작
            Rect viewport = BattleSetup.CalculateViewport(UnityEngine.Screen.width, UnityEngine.Screen.height); // 정규화 전투 영역
            return new Rect(viewport.x * UnityEngine.Screen.width, (1f - viewport.yMax) * UnityEngine.Screen.height, viewport.width * UnityEngine.Screen.width, viewport.height * UnityEngine.Screen.height); // 위쪽 기준 화면 영역
        } // 계산 종료
        private static Rect ButtonRect(Rect view, int index) // 공통 하단 버튼 위치
        { // 계산 시작
            return new Rect(view.x + view.width * (0.29f + index * 0.23f), view.y + view.height * 0.80f, view.width * 0.19f, view.height * 0.09f); // 버튼 영역 반환
        } // 계산 종료
        private static Rect GrowthCharacterRect(Rect view, int index) // 성장 화면 캐릭터 위치
        { // 계산 시작
            return new Rect(view.x + view.width * (0.30f + index * 0.25f), view.y + view.height * 0.16f, view.width * 0.19f, view.height * 0.08f); // 캐릭터 선택 버튼
        } // 계산 종료
        private static Rect GrowthButtonRect(Rect view) // 영구 성장 버튼 위치
        { // 계산 시작
            return new Rect(view.x + view.width * 0.75f, view.y + view.height * 0.80f, view.width * 0.19f, view.height * 0.09f); // 준비 화면 성장 버튼
        } // 계산 종료
        private static Rect CharacterRect(Rect view, int index) // 캐릭터 목록 위치
        { // 계산 시작
            return new Rect(view.x + view.width * 0.035f, view.y + view.height * (0.25f + index * 0.14f), view.width * 0.18f, view.height * 0.10f); // 캐릭터 버튼 영역
        } // 계산 종료
        private static Rect ContinueRect(Rect view) // 캐릭터 선택 완료 위치
        { // 계산 시작
            return new Rect(view.x + view.width * 0.36f, view.y + view.height * 0.82f, view.width * 0.28f, view.height * 0.09f); // 준비 버튼 영역
        } // 계산 종료
        private static Rect SkillRect(Rect view, int index) // 공용 스킬 버튼 위치
        { // 계산 시작
            return new Rect(view.x + view.width * (0.25f + index * 0.16f), view.y + view.height * 0.60f, view.width * 0.14f, view.height * 0.08f); // 스킬 버튼 영역
        } // 계산 종료
        private static Rect SettlementRetryRect(Rect view) // 저장 재시도 버튼 위치
        { // 계산 시작
            return new Rect(view.x + view.width * 0.36f, view.y + view.height * 0.61f, view.width * 0.28f, view.height * 0.09f); // 재시도 버튼 영역
        } // 계산 종료
        private static Rect ConfirmRect(Rect view, bool confirm) // 이탈 확인 버튼 위치
        { // 계산 시작
            return new Rect(view.x + view.width * (confirm ? 0.33f : 0.52f), view.y + view.height * 0.51f, view.width * 0.15f, view.height * 0.08f); // 확인 버튼 영역
        } // 계산 종료
    } // 클래스 종료
} // 영역 종료
