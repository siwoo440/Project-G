using System; // 독립 검증용 설정 키
using UnityEngine; // 화면 해상도와 전역 음량
using UnityEngine.UI; // 실제 화면 설정 버튼과 슬라이더

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightTitleSettings : MonoBehaviour // 메인 화면의 화면 및 음량 설정
    {
        private static readonly Vector2Int[] Resolutions = { new(1920, 1080), new(1600, 900), new(1280, 720), new(1024, 576), new(1280, 800), new(1024, 768) }; // 저장 순서를 보존한 16대9·16대10·4대3
        [SerializeField] private Slider volume; // 실제 마스터 음량 값
        [SerializeField] private Slider sensitivity; // 비행 마우스 감도 값
        [SerializeField] private Button displayMode; // 창 모드와 전체 화면 전환
        [SerializeField] private Button resolution; // 해상도 순환
        [SerializeField] private Button apply; // 저장 확정
        [SerializeField] private Text volumeLabel; // 음량의 실제 퍼센트
        [SerializeField] private Text sensitivityLabel; // 비행 마우스 감도의 퍼센트
        [SerializeField] private Text modeLabel; // 현재 선택한 화면 유형
        [SerializeField] private Text resolutionLabel; // 실제 해상도
        [SerializeField] private Text feedback; // 저장 결과
        private int selectedResolution; // 지원 해상도 배열 위치
        private bool full; // 전체 화면 선택

        internal static string TestPrefix => Environment.GetEnvironmentVariable("PROJECTG_MENU_TEST_KEY"); // 자동 검증 편의 키
        internal static bool CanStore => !string.IsNullOrEmpty(TestPrefix) || FlightBindings.UsesPreferences; // 사용자 또는 독립 검증 저장
        internal static string Key(string suffix) => (TestPrefix ?? "ProjectG.Flight") + "." + suffix; // 일시정지와 같은 사용자 설정 이름

        internal void Initialize() // 실제 메뉴를 시작할 때 설정 버튼 연결
        {
            displayMode.onClick.AddListener(ToggleDisplay); // 지원 창 모드 선택
            resolution.onClick.AddListener(CycleResolution); // 다음 화면 크기 선택
            apply.onClick.AddListener(Apply); // 현재 선택 저장
            volume.onValueChanged.AddListener(_ => UpdateLabels()); // 조정된 음량 표시
            sensitivity.onValueChanged.AddListener(_ => UpdateLabels()); // 조정된 마우스 배율 표시
            Restore(); // 최초 표시의 현재 설정
            ApplyAudio(); // 저장된 실제 소리 설정
            if (CanStore && PlayerPrefs.HasKey(Key("Screen"))) // 기존 사용자 표시 설정
            {
                ApplyDisplay(); // 다시 열어도 같은 모드 사용
            }
        }

        internal void Restore() // 적용하지 않고 나간 설정은 보존값으로 복원
        {
            selectedResolution = 0; // 지원되는 1080p 기본 크기
            full = Screen.fullScreenMode != FullScreenMode.Windowed; // 처음 화면의 현재 상태
            if (CanStore) // 저장한 사용자의 화면 설정
            {
                string[] screen = PlayerPrefs.GetString(Key("Screen"), "").Split(':'); // 선택 번호와 창 모드
                if (screen.Length == 2 && int.TryParse(screen[0], out int index) && index >= 0 && index < Resolutions.Length) // 유효한 저장 크기
                {
                    selectedResolution = index; // 다시 사용할 화면 크기
                    full = screen[1] == "1"; // 원래 선택한 창 또는 전체 화면
                }
            }
            volume.SetValueWithoutNotify(CanStore ? PlayerPrefs.GetFloat(Key("Volume"), 1f) : 1f); // 기존 일시정지의 같은 음량
            sensitivity.SetValueWithoutNotify(CanStore ? PlayerPrefs.GetFloat(Key("Sensitivity"), 100f) : 100f); // 기존 비행 감도
            feedback.text = ""; // 지난 화면에서 남긴 확인 제거
            UpdateLabels(); // 저장에서 읽은 표시 적용
        }

        private void ToggleDisplay() // 전체 화면 선택 전환
        {
            full = !full; // 두 창 유형의 순환
            UpdateLabels(); // 변경한 창 종류 표시
        }

        private void CycleResolution() // 다음 지원 크기 선택
        {
            selectedResolution = (selectedResolution + 1) % Resolutions.Length; // 표시할 다음 크기
            UpdateLabels(); // 변경한 해상도 반영
        }

        private void Apply() // 사용자 승인 후 화면 및 소리 저장
        {
            if (CanStore) // 현재 시작화면의 저장 사용
            {
                PlayerPrefs.SetString(Key("Screen"), $"{selectedResolution}:{(full ? 1 : 0)}"); // 전체 화면·해상도 설정
                PlayerPrefs.SetFloat(Key("Volume"), volume.value); // 일시정지와 공유하는 음량
                PlayerPrefs.SetFloat(Key("Sensitivity"), sensitivity.value); // 기존 비행 감도 저장
                PlayerPrefs.Save(); // 다음 실행에 즉시 반영
            }
            ApplyAudio(); // 해당 화면과 다음 출격의 실제 음량
            ApplyDisplay(); // 실제 실행의 화면 설정
            feedback.text = "설정을 저장했습니다."; // 적용 메시지
        }

        private void UpdateLabels() // 화면에 보이는 조작값
        {
            Vector2Int size = Resolutions[selectedResolution]; // 현재 선택한 화면 크기
            resolutionLabel.text = $"{size.x} × {size.y}"; // 해상도 수치
            modeLabel.text = full ? "전체 화면" : "창 모드"; // 표시 유형
            volumeLabel.text = $"{Mathf.RoundToInt(volume.value * 100f)}%"; // 마스터 음량 백분율
            sensitivityLabel.text = $"{Mathf.RoundToInt(sensitivity.value)}%"; // 마우스 감도 백분율
        }

        private void ApplyAudio() // 기존 사용자 음소거도 함께 확인
        {
            bool muted = CanStore && PlayerPrefs.GetInt(Key("Muted"), 0) != 0; // 일시정지에서 저장한 음소거 여부
            AudioListener.volume = muted ? 0f : volume.value; // PC에서 실제로 사용하는 전역 소리
        }

        private void ApplyDisplay() // 실제 출격과 메뉴의 창 크기
        {
            if (Application.isBatchMode || (!FlightBindings.UsesPreferences && Environment.GetEnvironmentVariable("PROJECTG_MENU_APPLY_DISPLAY") != "1")) // 격리된 자동 테스트의 직접 디스플레이 변경 방지
            {
                return; // 검증 중에는 저장 설정만 확인
            }
            Vector2Int size = Resolutions[selectedResolution]; // 선택한 화면 해상도
            Screen.SetResolution(size.x, size.y, full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed); // 저장한 창 유형으로 적용
        }
    }
}
