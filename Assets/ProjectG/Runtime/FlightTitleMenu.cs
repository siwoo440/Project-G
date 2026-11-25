using System; // 확인창에 연결할 실제 동작
using UnityEngine; // 메뉴 화면과 시간 상태
using UnityEngine.EventSystems; // 현재 키보드 버튼 강조
using UnityEngine.InputSystem; // 메뉴 이동과 승인
using UnityEngine.SceneManagement; // 정식 출격으로 전환
using UnityEngine.UI; // 편집 가능한 타이틀 버튼

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightTitleMenu : MonoBehaviour // 메인 메뉴의 입력과 대화 흐름
    {
        public const string SceneName = "FlightMainMenu"; // 배포 첫 화면의 고정 이름
        [SerializeField] private GameObject home; // 네 개의 기본 메뉴
        [SerializeField] private GameObject settings; // 화면·음량 설정 보기
        [SerializeField] private GameObject dialog; // 새 게임 또는 종료 확인
        [SerializeField] private Text prompt; // 확인해야 하는 실제 상황
        [SerializeField] private Text recoveredSortie; // 중단된 출격의 실제 복구 안내
        [SerializeField] private FlightTitleSettings options; // 화면과 음량의 저장
        [SerializeField] private Button newGame; // 새 게임 버튼
        [SerializeField] private Button continueGame; // 저장한 출격 이어서하기
        [SerializeField] private Button openSettings; // 설정 창 입장
        [SerializeField] private Button quit; // 종료 확인
        [SerializeField] private Button settingsBack; // 설정에서 뒤로가기
        [SerializeField] private Button accept; // 확인창 확정
        [SerializeField] private Button cancel; // 확인창 닫기
        [SerializeField] private Selectable[] homeOrder; // 메뉴에서 키보드가 이동할 순서
        [SerializeField] private Selectable[] settingsOrder; // 화면 설정 조작 순서
        private Selectable[] order; // 현재 열려 있는 화면의 포커스 순서
        private Action onConfirm; // 새 게임 또는 종료 동작
        private bool transitioning; // 버튼 중복 클릭을 막는 씬 불러오기

        private void Start() // 첫 실행과 일시정지 복귀에서 공용 설정
        {
            Time.timeScale = 1f; // 이전 출격의 정지 시간 정상화
            AudioListener.pause = false; // 기존 메뉴의 소리 정지 해제
            Cursor.lockState = CursorLockMode.None; // 메인 메뉴는 마우스 사용 가능
            Cursor.visible = true; // 처음부터 버튼을 선택할 수 있는 커서
            EventSystem.current.sendNavigationEvents = false; // 키보드의 메뉴 클릭 중복 방지
            options.Initialize(); // 현재 환경 설정 읽기
            FlightProgressReceipt recovered = FlightProgress.Current.Recover(); // 이번 실행에서 미정산 출격만 한 번 복구
            recoveredSortie.text = recovered != null ? $"중단 출격 정산\n숙련도 +{recovered.Mastery}   ·   천공 결정 +{recovered.Crystals}   ·   조율의 증표 +{recovered.Marks}" // 제목과 새로 지급한 참여 보상
                : FlightProgress.Current.Error ?? string.Empty; // 저장 문제만 별도로 안내
            recoveredSortie.transform.parent.gameObject.SetActive(recoveredSortie.text.Length > 0); // 실제 복구 기록이나 저장 오류가 있을 때만 표시
            newGame.onClick.AddListener(NewGame); // 저장 여부에 따른 새 출격
            continueGame.onClick.AddListener(() => Begin(true)); // 저장한 출격을 계속
            openSettings.onClick.AddListener(ShowSettings); // 화면 및 소리 설정
            quit.onClick.AddListener(() => Confirm("게임을 종료할까요?", Application.Quit)); // 종료 확인
            settingsBack.onClick.AddListener(ShowHome); // 조작을 저장하고 기본 메뉴로 돌아가기
            cancel.onClick.AddListener(ShowHome); // 기존 출격 기록 유지
            accept.onClick.AddListener(() => onConfirm?.Invoke()); // 보고 있던 확인의 실제 승인
            ShowHome(); // 네 버튼을 먼저 표시
        }

        private void Update() // 키보드에서 현재 메뉴에 입력
        {
            Keyboard keyboard = Keyboard.current; // 연결된 물리 또는 가상 키보드
            if (transitioning || keyboard == null) // 마우스만 사용하거나 이미 씬 로딩
            {
                return; // 추가 조작 차단
            }
            if (keyboard.escapeKey.wasPressedThisFrame && !home.activeSelf) // 설정과 확인창에서 Esc 취소
            {
                ShowHome(); // 실제 메인 메뉴로 복귀
                return; // 이전 포커스에서 입력이 중복되지 않음
            }
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) // 이전 포커스
            {
                Navigate(-1); // 사용할 수 있는 이전 항목
            }
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) // 다음 포커스
            {
                Navigate(1); // 잠긴 항목을 건너뛴 다음 조작
            }
            GameObject current = EventSystem.current.currentSelectedGameObject; // 마우스 또는 키보드가 선택한 오브젝트
            if (current == null) // 빈 화면을 클릭해 포커스가 사라짐
            {
                return; // 이 프레임에는 선택할 대상 없음
            }
            if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) && current.TryGetComponent(out Button focused) && focused.IsInteractable()) // 활성 버튼의 선택
            {
                focused.onClick.Invoke(); // 마우스와 같은 리스너
            }
            if (current.TryGetComponent(out Slider slider)) // 실제 설정 슬라이더의 키 조절
            {
                float step = (slider.maxValue - slider.minValue) * 0.05f; // 전체 범위의 5%
                if (keyboard.leftArrowKey.wasPressedThisFrame) // 수치 낮추기
                {
                    slider.value -= step; // 슬라이더의 실제 값
                }
                if (keyboard.rightArrowKey.wasPressedThisFrame) // 수치 높이기
                {
                    slider.value += step; // 슬라이더의 실제 값
                }
            }
        }

        private void Navigate(int change) // 잠긴 항목을 제외하고 포커스 이동
        {
            GameObject current = EventSystem.current.currentSelectedGameObject; // 실제 포커스 오브젝트
            int index = Array.FindIndex(order, item => item.gameObject == current); // 마우스와 키보드가 공유하는 현재 위치
            if (index < 0) // 아직 화면에서 포커스 없음
            {
                index = change > 0 ? -1 : 0; // 올바른 방향의 첫 위치
            }
            for (int count = 0; count < order.Length; count++) // 현재 화면의 사용 가능한 버튼
            {
                index = (index + change + order.Length) % order.Length; // 목록의 끝과 시작을 연결
                if (order[index].IsInteractable()) // 비활성 이어서하기 등을 건너뜀
                {
                    order[index].Select(); // 실제 버튼 강조
                    return; // 한 항목씩 이동
                }
            }
        }

        private void ShowHome() // 기본 페이지와 현재 이어서하기 상태
        {
            home.SetActive(true); // 메뉴 4개 표시
            settings.SetActive(false); // 보조 패널 숨김
            dialog.SetActive(false); // 현재 승인 취소
            continueGame.interactable = HasHistory(); // 저장 없으면 이어서하기 비활성
            order = homeOrder; // 키보드의 원래 버튼 순서
            newGame.Select(); // 기본 위치 포커스
        }

        private void ShowSettings() // 저장한 화면 및 음량 설정 열기
        {
            home.SetActive(false); // 메인 버튼 뒤에 숨김
            settings.SetActive(true); // 실제 환경 설정 페이지
            dialog.SetActive(false); // 확인창 비활성
            options.Restore(); // 이전 확정 설정에서 시작
            order = settingsOrder; // 설정 항목의 키 이동
            order[0].Select(); // 첫 항목의 현재 포커스
        }

        private bool HasHistory() // 현재 출격 기록 또는 이전 조립 저장 존재
        {
            if (!FlightTitleSettings.CanStore) // 배치 시험에서는 일반 데이터 접근 제외
            {
                return false; // 일반 사용자 이력과 분리
            }
            return PlayerPrefs.HasKey(FlightTitleSettings.Key("Resume")) // 타이틀을 통해 시작했던 출격
                || (string.IsNullOrEmpty(FlightTitleSettings.TestPrefix) && (PlayerPrefs.HasKey("ProjectG.Flight.Broom.Loadouts.v1") || PlayerPrefs.HasKey("ProjectG.Flight.Crafted.v1"))); // 이전 버전에서 사용한 출격 구성
        }

        private void NewGame() // 같은 버튼에서 최초 실행과 기존 기록 보호
        {
            if (HasHistory()) // 기존 출격 기록이 있는 사용자
            {
                Confirm("기존 출격 대신 새 출격을 시작할까요?\n저장한 장비·마법과 영구 성장은 보존됩니다.", () => Begin(false)); // 현재 작업 흐름 교체 확인
            }
            else // 게임 사용 이력이 없는 최초 실행
            {
                Begin(false); // 확인 없이 첫 출격 준비
            }
        }

        private void Confirm(string title, Action action) // 사용자 승인 전 실제 행동 대기
        {
            home.SetActive(false); // 다른 타이틀 버튼에 중복 입력 차단
            settings.SetActive(false); // 환경 설정 숨김
            dialog.SetActive(true); // 확인 화면
            prompt.text = title; // 새 게임과 종료의 다른 사용자 문구
            onConfirm = action; // 확정 이후 실제 동작
            order = new Selectable[] { cancel, accept }; // 취소에 기본 포커스
            cancel.Select(); // 사용자의 의도 없는 진행 교체 방지
        }

        private void Begin(bool resume) // 선택과 준비로 연결하는 메인 화면 진입
        {
            if (transitioning) // 화면 전환 중 다른 클릭
            {
                return; // 한 씬 전환으로 통합
            }
            transitioning = true; // 시작 요청 확정
            FlightSortieChoice.Open(resume); // 새 캐릭터 선택 또는 저장 조종사 준비
            SceneManager.LoadSceneAsync(FlightSortieChoice.BriefingScene); // 별도의 실제 출격 준비 화면
        }
    }
}
