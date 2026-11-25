using UnityEngine; // 실제 조종 가능 여부와 연습 씬
using UnityEngine.InputSystem; // 운영체제와 분리 가능한 조작 선택
using UnityEngine.SceneManagement; // 출격 준비와 연습의 왕복
using UnityEngine.UI; // 처음부터 배치한 훈련 메뉴

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(50)] // 기존 시험의 시작 위치와 자원 초기화 이후
    [DisallowMultipleComponent] // 한 씬의 자유 훈련 하나
    public sealed class FlightTrainingMode : MonoBehaviour // 실제 사격과 패턴을 재현하는 독립 훈련
    {
        private enum Trial // 볼 수 있는 연습 항목 순서
        {
            Free, Target, Aimed, Fan, Ring, Spiral, Routes // 자유 비행과 다섯 종류의 전투 외에 적 경로
        }

        [SerializeField] private GameObject panel; // 마우스와 키를 받는 시험 메뉴
        [SerializeField] private Button[] choices; // 항목 순서대로 실제 배치된 버튼
        [SerializeField] private Button repeatButton; // 계속 또는 한 번 토글
        [SerializeField] private Text repeatLabel; // 현재 연습 횟수 표시
        [SerializeField] private Button resetButton; // 선택한 세팅으로 다시 시작
        [SerializeField] private Button resumeButton; // 상태를 바꾸지 않고 돌아가기
        [SerializeField] private Button returnButton; // 현재 조종사로 출격 준비 복귀
        [SerializeField] private FirstFlightController flight; // 실제 입력 상태와 비행 위치
        [SerializeField] private FirstFlightCamera view; // 표적의 실제 조준 좌표
        [SerializeField] private FlightWeapon weapon; // 출격 화면에서 선택한 조종사
        [SerializeField] private FlightCombatHud hud; // 현재 키와 조종사 스킬 이미지
        [SerializeField] private FlightThreatPractice practice; // 실제 입체 적탄과 런 초기화
        [SerializeField] private FlightEnemyEncounter encounter; // 배치 마물의 경로와 새 웨이브
        [SerializeField] private FlightSkillController skills; // 재사용 시간과 마력 복구
        [SerializeField] private FlightMagicDeck deck; // 사용한 마법 카드 원래 순서
        [SerializeField] private FlightEnemy target; // 연습에서만 켜고 이동할 실제 표적
        [SerializeField] private GameObject sandboxTargets; // 자유 시험용 일반 표적 그룹
        [SerializeField] private FlightExperience experience; // 모드에서 정지할 경험치 공급자
        [SerializeField] private FlightEnhancements enhancements; // 모드에서 정지할 강화 공급자
        private static FlightBroomPilot? requested; // 다음 씬에 한 번 전달할 캐릭터
        private bool active; // 정식 준비에서 선택해 열린 모드
        private bool repeat = true; // 다음 선택의 시험용 기본 반복
        private bool runRepeat; // 시작 시점에 고른 현재 연습의 반복
        private int closedFrame = -1; // 같은 Esc 입력으로 기존 정지 화면이 열리지 않도록 구분
        private Trial current; // 마지막으로 시작한 현재 연습
        private float nextRound; // 서로 다른 반복을 너무 빨리 열지 않는 시간
        private float originalScale; // 연습을 열기 전 시간 상태
        private bool originalAudio; // 기존 오디오 소유권 복구값

        private bool IsConfiguring => active && panel.activeSelf; // 연습 화면의 입력 소유권
        internal bool BlocksPause => IsConfiguring || closedFrame == Time.frameCount; // 닫은 프레임까지 기존 ESC 화면과 충돌 방지

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 동일 도메인의 이전 씬 요청 청소
        private static void ResetRequest() // 다시 실행할 Unity 게임의 상태
        {
            requested = null; // 처음에는 일반 연습 씬 그대로
        }

        internal static void Request(FlightBroomPilot pilot) // 출격 준비에서 현재 조종사로 자유 훈련
        {
            requested = pilot; // 다음 연습 씬에서 한 번 소비
        }

        private void Start() // 첫 비행 안내가 아닌 독립 자유 훈련에서 시작
        {
            if (!requested.HasValue) // 직접 실행과 기존 조작 튜토리얼
            {
                return; // 원래 시험 씬 동작 보존
            }
            FlightBroomPilot pilot = requested.Value; // 선택한 조종사 복원
            requested = null; // 다른 씬으로 넘어갈 때 자동 실행 금지
            active = true; // 이 씬에서만 자유 훈련 활성화
            experience.enabled = false; // 이 모드에서 경험치 조각과 성장 카드 공급 중단
            enhancements.enabled = false; // 강화 아이템의 훈련 보상 공급 중단
            weapon.SelectPilot(pilot); // 칸에 맞는 실제 스킬과 자동탄
            hud.SetPilot(weapon); // 선택한 조종사의 스킬 이미지와 표시
            for (int index = 0; index < choices.Length; index++) // 씬에 있는 일곱 선택지 연결
            {
                Trial trial = (Trial)index; // 해당 버튼과 연습 유형
                choices[index].onClick.AddListener(() => StartTrial(trial)); // 실제 전투 연습 시작
            }
            resetButton.onClick.AddListener(() => StartTrial(current)); // 이 연습의 자원·적탄·타이머를 처음부터
            repeatButton.onClick.AddListener(ToggleRepeat); // 재생 횟수 조작
            resumeButton.onClick.AddListener(Close); // 진행 중인 연습으로 복귀
            returnButton.onClick.AddListener(ReturnToBriefing); // 현재 조종사로 준비 씬
            ClearTrial(); // 원래 시험용 표적과 보상 정리
            Open(); // 유형을 먼저 선택할 편집 메뉴 표시
        }

        private void Update() // 실전과 분리한 반복 및 메뉴 조작
        {
            if (!active) // 직접 시험이나 안내는 독립 진행
            {
                return; // 이 콜백의 제어 없음
            }
            Keyboard keyboard = Keyboard.current; // 실제 두 단축키 읽기
            if (IsConfiguring) // 구성 패널이 입력을 소유하는 중
            {
                if (keyboard != null && (keyboard.f4Key.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)) // 연습 설정을 닫을 키
                {
                    Close(); // 즉시 비행으로 복귀
                }
                return; // 설정한 동안 반복·단축키 배제
            }
            if (Time.timeScale <= 0f) // 기존 ESC 메뉴가 전투를 일시정지한 상태
            {
                return; // 다른 메뉴의 소유권 보존
            }
            if (keyboard != null && keyboard.f4Key.wasPressedThisFrame) // 연습 중 현재 라운드 조정
            {
                Open(); // 실제 입력을 받아 다시 설정
                return; // 시간 정지와 같은 프레임의 훈련 실행 제외
            }
            if (!runRepeat || !flight.CanFly || Time.time < nextRound) // 반복 선택, 생존과 휴식 시간
            {
                return; // 종료됐거나 아직 바꾸지 않는 타이밍
            }
            if (current == Trial.Target && !target.gameObject.activeSelf) // 실제 표적을 격파한 경우
            {
                DeployTarget(); // 정면에 다시 출현
            }
            else if (current == Trial.Routes && encounter.ActiveEnemyCount == 0) // 세 경로 마물이 이탈 또는 파괴
            {
                encounter.StartTrial(); // 정식 비행 경로로 다시 등장
                nextRound = Time.time + 1.3f; // 같은 파동의 연속 중복 방지
            }
        }

        private void Open() // 현재 연습에서 안전하게 설정 접근
        {
            originalScale = Time.timeScale; // 기존 런 시간을 다시 이어가기 위한 값
            originalAudio = AudioListener.pause; // 기존 소리 상태 복귀값
            Time.timeScale = 0f; // 훈련 시계와 발사체 동결
            AudioListener.pause = true; // 전투 효과음도 같이 정지
            flight.SetSortieEnded(true); // 조준 잠금과 메뉴 입력 분리
            Cursor.lockState = CursorLockMode.None; // 화면 옵션을 누르는 포인터
            Cursor.visible = true; // 패널에 포커스 이동
            panel.SetActive(true); // 씬에 배치한 훈련 메뉴
            choices[(int)current].Select(); // 현재 선택과 동일한 버튼
            ShowRepeat(); // 반복 상태의 현재 값
        }

        private void Close() // 비행과 적탄 시간 계속
        {
            closedFrame = Time.frameCount; // 메뉴 복귀와 같은 ESC 이벤트를 중복 사용하지 않음
            panel.SetActive(false); // 기존 전투 HUD 복귀
            Time.timeScale = originalScale; // 이 모드가 멈추기 전의 시간
            AudioListener.pause = originalAudio; // 전투와 효과음 이어 재생
            flight.SetSortieEnded(false); // 실제 이동과 자동탄 사용
            Cursor.lockState = CursorLockMode.Locked; // 기존 비행의 조준 잠금
            Cursor.visible = false; // 조준점만 보이는 전투
        }

        private void ToggleRepeat() // 구성 화면에서 반복 값 변경
        {
            repeat = !repeat; // 다음 연습을 한 번 또는 반복
            ShowRepeat(); // 현재 선택을 버튼에 쓰기
        }

        private void ShowRepeat() // 반복과 다음 시작 규칙 연결
        {
            repeatLabel.text = repeat ? "반복  켜짐" : "반복  꺼짐"; // 다음 시작·초기화에 쓰는 값
        }

        internal bool RestartActive() // 기존 다시하기 버튼과 BKSP의 자유 훈련 연동
        {
            if (!active) // 처음부터 연습에 들어오지 않은 기본 씬
            {
                return false; // 기존 입문과 개발 시험의 다시하기 유지
            }
            StartTrial(current); // 선택한 패턴과 자원으로 다시 시작
            return true; // 같은 행동에 두 번 초기화하지 않음
        }

        private void StartTrial(Trial trial) // 실제 시간과 판정을 초기화하고 선택한 훈련 적용
        {
            FlightAudio.Play(FlightSfx.UiConfirm); // 실제 연습 선택 버튼
            current = trial; // 새로 시작할 선택
            nextRound = Time.time; // 이전 연습에서 남은 재등장 시간을 제거
            runRepeat = repeat; // 이번 연습에 사용할 반복 조건 확정
            Close(); // 활성 조준과 비행 재개
            ClearTrial(); // 적·자원·시계·조작의 실제 원상복구
            if (trial == Trial.Target) // 조준 표적
            {
                DeployTarget(); // 실제 적중과 체력 판정
            }
            else if (trial == Trial.Routes) // 세 유형 경로
            {
                encounter.StartTrial(); // 기존 실전과 같은 입체 경로형 마물
            }
            else if (trial >= Trial.Aimed && trial <= Trial.Spiral) // 네 종류 실제 패턴
            {
                practice.ConfigureTrainingPattern((FlightThreatPattern)((int)trial - (int)Trial.Aimed), runRepeat); // 한 번 또는 반복 발사
            }
        }

        private void ClearTrial() // 기존 연습용 초기화를 공유
        {
            practice.RestartRun(); // 기체와 탄막, 성장·드롭 초기화
            encounter.RestartTrial(); // 정식 출격과 분리된 연습의 현재 웨이브만 재설정
            skills.ResetRun(); // 마력과 필살기 에너지 복구
            deck.ResetRun(); // 좌우 카드의 초기 조합과 순서
            sandboxTargets.SetActive(false); // 정식 자유 훈련에서 기본 디버그 표적 숨김
            target.gameObject.SetActive(false); // 표적을 고른 모드에서만 표시
        }

        private void DeployTarget() // 조준점 전방의 일반 마물 표적
        {
            target.transform.position = view.GetAttackRay().GetPoint(26f); // 플레이어가 실제 바라보는 현재 위치
            target.gameObject.SetActive(true); // 현재 체력을 복구한 시험 표적
            nextRound = Time.time + 1.3f; // 바로 처치해도 새 표적의 짧은 대기
        }

        private void ReturnToBriefing() // 실제 조종사를 전달하고 같은 준비로 돌아가기
        {
            if (IsConfiguring) // 씬 전환 후 전투의 시간 정지를 남기지 않음
            {
                Close(); // 게임 시간과 오디오 원상복구
            }
            active = false; // 한 번만 준비로 전환
            FlightSortieChoice.RememberPilot(weapon.ActivePilot); // 실제 조종사 선택을 계속 사용
            SceneManager.LoadSceneAsync(FlightSortieChoice.BriefingScene); // 기존 정식 출발과 같은 준비 진입
        }
    }
}
