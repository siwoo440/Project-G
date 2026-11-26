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
        [SerializeField] private FlightFirstIncident incident; // 정확한 결과에 연동하는 첫 사건 기록 창
        private static FlightSortieReport staged; // 이전 씬으로부터 한 번 전달되는 결과
        private FlightSortieReport report; // 현재 보이는 결과
        private bool moving; // 버튼 중복 클릭과 씬 이동 보호
        private GameObject broomRewardOverlay; // 실제 획득 부품을 펼쳐 보는 창

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
            if (!report.BossRush) // 일반 항로의 실제 작전 결과만 보관
            {
                incident?.ShowResult(report.Cleared); // 같은 작전 기록에 현재 결과만 표시
            }
            outcome.text = $"{(report.BossRush ? "보스 러시" : "출격")} {(report.Cleared ? "완료" : "실패")}"; // 실제 최종 결과 구분
            pilotName.text = $"{FlightPilotRoster.Name(report.Pilot)}  ·  {(report.BossRush ? "보스 러시" : "하늘섬 외곽 항로")}"; // 종료한 조종사와 모드
            combatTime.text = report.BossRush ? FlightBossRushRecords.Format(Mathf.RoundToInt(report.CombatSeconds * 100f)) : FormatTime(report.CombatSeconds); // 보스 기록 초 아래 두 자리
            totalTime.text = FormatTime(report.TotalSeconds); // 정지 시간을 포함한 분과 초
            score.text = report.Score.ToString("N0"); // 실제 게임 점수
            defeats.text = report.Defeats.ToString(); // 최종 보스를 포함한 격파 수
            grazes.text = report.Grazes.ToString(); // 실제 스침 수
            level.text = $"LV. {report.Level}"; // 종료 시에 확정된 레벨
            FlightProgressReceipt receipt = report.Receipt; // 실제로 저장을 끝낸 출격 영수증
            string firstTask = receipt?.FirstTask == true ? "   ·   첫 과제 완료" : string.Empty; // 이번 출격에서만 지급한 증표의 이유
            rewards.text = receipt != null ? $"숙련도 +{receipt.Mastery}   ·   조율의 증표 +{receipt.Marks}{firstTask}" // 조종사 보상과 증표
                : FlightProgress.Current.Error ?? "전투 참여 보상 없음"; // 실패한 저장 또는 참여 전 종료
            if (report.BossRush) // 영구 보상 대신 조종사별 도전 기록 표시
            {
                rewards.text = report.BestTime == 0 ? "최고 기록 없음" : $"{(report.NewRecord ? "신기록" : "최고 기록")}  {FlightBossRushRecords.Format(report.BestTime)}"; // 성공 기록의 동일한 시간 단위
            }
            crystals?.Show(receipt?.Crystals, true); // 실제 확정한 출격 결정 증가량
            gold?.Show(receipt?.Gold, true); // 실제 확정한 출격 골드 증가량
            ShowBroomRewards(receipt?.BroomRewards); // 정산에서 실제로 저장한 개별 부품
        }

        private void ShowBroomRewards(FlightBroomReward[] items) // 실제 획득 목록을 여는 결과 버튼
        {
            if (items == null || items.Length == 0) // 지급한 부품이 없는 출격
            {
                return; // 빈 보상 창을 만들지 않음
            }
            Button open = Instantiate(retry, rewards.transform.parent); // 기존 결과 버튼의 테두리와 글꼴 재사용
            open.name = "BroomRewards"; // 획득 부품 버튼 식별
            open.onClick = new Button.ButtonClickedEvent(); // 재출격 동작 복사 방지
            RectTransform openRect = open.GetComponent<RectTransform>(); // 결과 화면의 버튼 위치
            openRect.anchorMin = openRect.anchorMax = new Vector2(0.5f, 0.5f); // 결과 패널 중심 기준
            openRect.anchoredPosition = new Vector2(250f, -193f); // 재화 옆의 빈 영역
            openRect.sizeDelta = new Vector2(180f, 34f); // 실제 목록을 여는 버튼 크기
            FlightCraftedUi.FitLabel(open, 19); // 긴 한국어 문구 크기 보정
            open.GetComponentInChildren<Text>().text = $"획득 부품 {items.Length}개"; // 저장된 개별 보유 수
            Canvas canvas = rewards.GetComponentInParent<Canvas>().rootCanvas; // 결과 전체를 가리는 최상위 캔버스
            broomRewardOverlay = new GameObject("BroomRewardOverlay", typeof(RectTransform), typeof(Image)); // 다른 결과 버튼을 차단할 배경
            broomRewardOverlay.transform.SetParent(canvas.transform, false); // 기존 UI 위의 자식 창
            RectTransform overlayRect = broomRewardOverlay.GetComponent<RectTransform>(); // 전체 배경의 위치
            overlayRect.anchorMin = Vector2.zero; // 왼쪽 아래 모서리 기준
            overlayRect.anchorMax = Vector2.one; // 오른쪽 위 모서리까지 확장
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero; // 화면 전체 영역
            broomRewardOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f); // 목록 뒤의 어두운 배경
            Text list = Instantiate(rewards, broomRewardOverlay.transform); // 게임의 한국어 글꼴과 효과 재사용
            list.name = "BroomRewardList"; // 실제 획득 내역 식별
            RectTransform listRect = list.rectTransform; // 목록 글자의 화면 위치
            listRect.anchorMin = listRect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중심 기준
            listRect.anchoredPosition = new Vector2(0f, 35f); // 닫기 버튼 위의 목록
            listRect.sizeDelta = new Vector2(780f, 330f); // 다섯 부품까지 표시할 영역
            list.alignment = TextAnchor.MiddleCenter; // 부품별 중앙 정렬
            list.resizeTextForBestFit = true; // 긴 이름의 화면 밖 넘침 방지
            list.resizeTextMinSize = 16; // 가장 작은 목록 글자
            list.resizeTextMaxSize = 26; // 기준 목록 글자
            list.text = "획득한 빗자루 부품"; // 실제 정산 내역 제목
            foreach (FlightBroomReward item in items) // 이번 출격의 개별 획득 부품
            {
                list.text += "\n" + FlightBroomCatalog.GradeName(item.Grade) + " · " + item.Name; // 희귀도와 실제 부품 이름
            }
            Button close = Instantiate(retry, broomRewardOverlay.transform); // 동일한 버튼 디자인의 닫기
            close.name = "CloseBroomRewards"; // 목록 닫기 버튼 식별
            close.onClick = new Button.ButtonClickedEvent(); // 재출격 동작 복사 방지
            RectTransform closeRect = close.GetComponent<RectTransform>(); // 닫기 버튼 위치
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중심 기준
            closeRect.anchoredPosition = new Vector2(0f, -185f); // 목록 아래 중앙
            closeRect.sizeDelta = new Vector2(180f, 48f); // 마우스로 누르기 쉬운 크기
            FlightCraftedUi.FitLabel(close, 21); // 한국어 닫기 글자 크기 보정
            close.GetComponentInChildren<Text>().text = "닫기"; // 실제 닫기 동작 안내
            close.onClick.AddListener(() => broomRewardOverlay.SetActive(false)); // 결과 화면으로 복귀
            open.onClick.AddListener(() => broomRewardOverlay.SetActive(true)); // 저장된 획득 목록 표시
            broomRewardOverlay.SetActive(false); // 처음에는 결과 화면을 가리지 않음
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
            if (broomRewardOverlay != null && broomRewardOverlay.activeSelf) // 실제 획득 부품 목록이 열린 상태
            {
                if (keyboard.escapeKey.wasPressedThisFrame) // 닫기 버튼과 같은 취소 키
                {
                    broomRewardOverlay.SetActive(false); // 결과 화면으로 복귀
                }
                return; // 같은 키로 재출격하거나 타이틀에 이동하지 않음
            }
            if (incident != null && incident.HandleArchiveKey(keyboard, true)) // 기록 창을 먼저 닫거나 열기
            {
                return; // 같은 키로 메인·재출격에 진입하지 않음
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
            if (report.BossRush) // 재도전 때 동일한 모드 보존
            {
                FlightSortieChoice.LaunchBossRush(report.Pilot); // 저장된 본편 이어서하기를 유지한 재도전
            }
            else
            {
                FlightSortieChoice.Launch(report.Pilot); // 이번에 끝난 실제 조종사로 준비
            }
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
