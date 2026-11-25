using System; // 결과의 시각 표시
using UnityEngine; // 별도 결과 씬의 실제 상태
using UnityEngine.InputSystem; // 다시 출격 및 메인 복귀 바로가기
using UnityEngine.SceneManagement; // 새 런과 타이틀 진입
using UnityEngine.UI; // 결과와 버튼 UI

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightSortieResult : MonoBehaviour // 완료 또는 실패 결과와 재도전
    {
        public const string SceneName = "FlightSortieResult"; // 배포 결과 씬
        [SerializeField] private FlightPilotPortraits portraits; // 출격 준비와 공유하는 캐릭터별 원본
        [SerializeField] private RawImage standing; // 왼쪽 전신을 편집할 실제 씬 그림
        [SerializeField] private AspectRatioFitter standingRatio; // 스탠딩의 원래 가로세로를 유지
        [SerializeField] private Text outcome; // 출격 완료 또는 실패 문구
        [SerializeField] private Text pilotName; // 이번 출격에서 조작한 이름
        [SerializeField] private Text combatTime; // 자동으로 멈춘 전투 시간
        [SerializeField] private Text totalTime; // 비행과 메뉴를 포함한 전체 시간
        [SerializeField] private Text score; // 확정 출격 점수
        [SerializeField] private Text defeats; // 격파한 적
        [SerializeField] private Text grazes; // 유효한 그레이즈 수
        [SerializeField] private Text level; // 마지막 레벨
        [SerializeField] private FlightCurrencyLabel crystals; // 현재 출격의 결정 지급량
        [SerializeField] private FlightCurrencyLabel gold; // 현재 출격의 골드 지급량
        [SerializeField] private Text rewards; // 이번 출격의 영구 보상
        [SerializeField] private Button retry; // 같은 조합으로 재출격
        [SerializeField] private Button title; // 실제 메인 타이틀 복귀
        private static FlightSortieReport staged; // 이전 씬으로부터 한 번 전달되는 결과
        private FlightSortieReport report; // 현재 보이는 결과
        private bool moving; // 버튼 중복 클릭과 씬 이동 보호

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 에디터 도메인 유지에서도 초기화
        private static void Clear() // 이전 시험의 종료 결과 정리
        {
            staged = null; // 별도의 새로운 런
        }

        internal static void StageReport(FlightSortieReport finished) // 실제 전투에서만 결과 등록
        {
            staged = finished; // 현재 종료한 출격의 스냅샷
        }

        private void Start() // 새 씬의 UI 및 이전 일시정지 정상화
        {
            Time.timeScale = 1f; // 전투와 다른 새로운 씬 시간
            AudioListener.pause = false; // 메뉴의 소리 정상화
            Cursor.lockState = CursorLockMode.None; // 결과 버튼을 누르는 포인터
            Cursor.visible = true; // 실제 PC 화면의 결과 조작
            report = staged; // 현재 종료한 씬에서 전달받은 정보
            staged = null; // 다음 독립 결과로 넘어가지 않도록 초기화
            title.onClick.AddListener(Return); // 메인 씬으로 복귀
            retry.onClick.AddListener(Retry); // 같은 조합으로 다시
            retry.interactable = report != null; // 직접 열었을 때의 안전한 편집 상태
            ShowPortrait(report != null && portraits != null ? portraits.For(report.Pilot) : null); // 이번 출격의 실제 조종사
            if (report == null) // 편집기에서 독립으로 씬을 실행
            {
                outcome.text = "출격 기록 없음"; // 정상 경로에서 결과가 표시되는 상태
                return; // 이전 씬의 결과가 아직 없음
            }

            FlightAudio.ShowResult(report.Cleared); // 실제 클리어와 실패의 다른 결과곡
            outcome.text = report.Cleared ? "출격 완료" : "출격 실패"; // 실제 최종 결과 구분
            pilotName.text = $"{FlightPilotRoster.Name(report.Pilot)}  ·  하늘섬 외곽 항로"; // 종료한 캐릭터 확인
            combatTime.text = FormatTime(report.CombatSeconds); // 실제 전투 시계의 분과 초
            totalTime.text = FormatTime(report.TotalSeconds); // 정지 시간을 포함한 분과 초
            score.text = report.Score.ToString("N0"); // 실제 게임 점수
            defeats.text = report.Defeats.ToString(); // 최종 보스를 포함한 격파 수
            grazes.text = report.Grazes.ToString(); // 실제 스침 수
            level.text = $"LV. {report.Level}"; // 종료 시에 확정된 레벨
            FlightProgressReceipt receipt = report.Receipt; // 실제로 저장을 끝낸 출격 영수증
            string firstTask = receipt?.FirstTask == true ? "   ·   첫 과제 완료" : string.Empty; // 이번 출격에서만 지급한 증표의 이유
            rewards.text = receipt != null ? $"숙련도 +{receipt.Mastery}   ·   조율의 증표 +{receipt.Marks}{firstTask}" // 조종사 보상과 증표
                : FlightProgress.Current.Error ?? "전투 참여 보상 없음"; // 실패한 저장 또는 참여 전 종료
            crystals?.Show(receipt?.Crystals, true); // 실제 확정한 출격 결정 증가량
            gold?.Show(receipt?.Gold, true); // 실제 확정한 출격 골드 증가량
        }

        private void ShowPortrait(Texture2D art) // 출격 캐릭터의 화면 왼쪽 스탠딩
        {
            standing.gameObject.SetActive(art != null); // 결과 기록이 없는 직접 실행에서 가짜 인물 숨김
            standing.texture = art; // 전투가 끝날 때 저장한 스탠딩 참조 적용
            if (art != null) // 원본 파일이 연결된 실제 캐릭터
            {
                standingRatio.aspectRatio = (float)art.width / art.height; // 체형과 장비의 원래 비율 유지
            }
        }

        private void Update() // 마우스와 같은 결과 조작 키
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard == null || moving) // 이미 화면 이동 중인 상태
            {
                return; // 물리 키 입력 처리 없음
            }
            if (keyboard.escapeKey.wasPressedThisFrame) // 메인으로 복귀
            {
                Return(); // 오른쪽 메뉴 버튼과 같은 흐름
            }
            else if (keyboard.enterKey.wasPressedThisFrame) // 현재 조합으로 다시 출격
            {
                Retry(); // 왼쪽 재출격 버튼과 같은 흐름
            }
        }

        private static string FormatTime(float seconds) // 사용자에게 읽기 쉬운 종료 시각
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.FloorToInt(Mathf.Max(0f, seconds))); // 음수를 제외하고 정수 절삭
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}"; // 분 단위와 초의 실제 표기
        }

        private void Retry() // 같은 조종사·저장 장비로 새 출격을 시작
        {
            if (moving || report == null) // 중복 로드 또는 실제 기록 없음
            {
                return; // 직접 열었을 때는 메인 이동만 가능
            }
            moving = true; // 클릭과 Enter 중복 취소
            FlightSortieChoice.Launch(report.Pilot); // 이번에 끝난 실제 조종사로 준비
            SceneManager.LoadSceneAsync(FlightSortieChoice.StageScene); // 실전 씬을 새로 로드하여 전투 상태 초기화
        }

        private void Return() // 일시정지 없는 시작 화면으로 돌아가기
        {
            if (moving) // 이미 같은 씬 이동 작업
            {
                return; // 부드러운 한 번의 씬 이동
            }
            moving = true; // 다른 버튼과 확인 키 차단
            SceneManager.LoadSceneAsync(FlightTitleMenu.SceneName); // 타이틀로 이동
        }
    }
}
