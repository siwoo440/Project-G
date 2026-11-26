using UnityEngine; // 출격 준비 씬의 시간과 캐릭터 이미지
using UnityEngine.EventSystems; // 버튼 포커스 기준
using UnityEngine.InputSystem; // 씬 안의 뒤로·출발 입력
using UnityEngine.SceneManagement; // 제목과 실제 항로 전환
using UnityEngine.UI; // 씬의 편집 가능한 출격 요소

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightSortieBriefing : MonoBehaviour // 조종사와 장비가 이어지는 출격 준비
    {
        [SerializeField] private FlightBroomEquipment gear; // 아리아·리안의 실제 기본·저장 장착
        [SerializeField] private FlightCraftedSpellBook book; // 출발할 조합의 실제 마법 편성
        [SerializeField] private FlightBroomWorkshop workshop; // 전에 구현한 8슬롯 조립
        [SerializeField] private FlightCraftedSpellEditor editor; // 전에 구현한 제작 마법과 3D 시험
        [SerializeField] private GameObject ready; // 조종사 선택과 출발 개체
        [SerializeField] private RawImage portrait; // 기획서의 실제 스탠딩
        [SerializeField] private FlightPilotPortraits portraits; // 출격 준비와 결과가 공유하는 원본 일러스트
        [SerializeField] private Text title; // 활동 캐릭터 이름과 전투 역할
        [SerializeField] private Text details; // 이미 확정한 설명과 Q·E·R
        [SerializeField] private Text equipped; // 출발에 반영되는 현재 실제 조합
        [SerializeField] private FlightCurrencyLabel crystals; // 현재 공용 천공 결정 보유량
        [SerializeField] private FlightCurrencyLabel gold; // 현재 공용 골드 보유량
        [SerializeField] private Text progression; // 누적 숙련도와 실제 보유 재료
        [SerializeField] private Button aria; // 조사팀 속사형 선택
        [SerializeField] private Button iris; // 수호팀 파동·연쇄형 선택
        [SerializeField] private Button vera; // 수호팀 설치형 선택
        [SerializeField] private Button nox; // 조사팀 회피·반격형 선택
        [SerializeField] private Button sera; // 수호팀 근접형 선택
        [SerializeField] private Button lumi; // 조사팀 유도형 선택
        [SerializeField] private Button rian; // 수호팀 저격형 선택
        [SerializeField] private Button back; // 왼쪽 위의 메뉴 복귀
        [SerializeField] private Button depart; // 오른쪽 아래 실제 출발
        [SerializeField] private Button editGear; // 장착 부품의 선택
        [SerializeField] private Button editMagic; // 실제 좌우 제작 마법 변경
        [SerializeField] private Button firstSortieGuide; // 선택형 첫 비행 안내
        [SerializeField] private FlightFirstIncident incident; // 출격 준비에서 읽는 첫 사건 기록
        [SerializeField] private Button freeTraining; // 정식 출격과 따로 여는 연습 메뉴
        [SerializeField] private Button bossRush; // 두 보스를 연속으로 만나 기록할 별도 모드
        [SerializeField] private Button expedition; // 편성 화면에서 입장할 탐험 지도
        [SerializeField] private FlightExpeditionMap map; // 임시 경로·상점·저장 지도
        private FlightBroomPilot pilot; // 선택 화면의 현재 조종사
        private bool moving; // 중복 씬 전환 방지
        private FlightStoryMenu storyMenu;

        private void OnGUI()
        {
            if (moving || ready == null || !ready.activeSelf || storyMenu == null || storyMenu.IsOpen) return;
            if (GUI.Button(new Rect(Screen.width - 200, 72, 176, 36), "본편 임무 (F7)")) storyMenu.Open(pilot);
        }

        private void Start() // 두 개의 검증된 캐릭터와 저장 흐름
        {
            storyMenu = gameObject.AddComponent<FlightStoryMenu>();
            Time.timeScale = 1f; // 메인 화면과 같이 실제 메뉴 시간
            AudioListener.pause = false; // 탐험 전투를 일시정지한 뒤 돌아오면 메뉴 소리 복원
            Cursor.lockState = CursorLockMode.None; // UI 마우스 포인터
            Cursor.visible = true; // 첫 선택에서 커서 표시
            EventSystem.current.sendNavigationEvents = false; // 자체 선택 키와 UI 제출 중복 방지
            workshop.Initialize(ShowSummary); // 저장한 장비로 출격 요약 갱신
            editor.Initialize(ShowSummary, StartMagicTraining, () => pilot); // 제작 화면과 실제 캐릭터의 자유 연습 연결
            aria.interactable = FlightPilotRoster.CanSelect(FlightBroomPilot.Aria); // 저장된 아리아 선택 자격
            rian.interactable = FlightPilotRoster.CanSelect(FlightBroomPilot.Rian); // 저장된 리안 선택 자격
            iris.interactable = FlightPilotRoster.CanSelect(FlightBroomPilot.Iris); // 이리스 시범 출격의 실제 선택
            iris.onClick.AddListener(() => Choose(FlightBroomPilot.Iris)); // 이리스로 편성과 스탠딩 전환
            vera.interactable = FlightPilotRoster.CanSelect(FlightBroomPilot.Vera); // 베라의 실제 포대 출격 자격
            vera.onClick.AddListener(() => Choose(FlightBroomPilot.Vera)); // 베라로 준비 창 갱신
            nox.interactable = FlightPilotRoster.CanSelect(FlightBroomPilot.Nox); // 녹스의 실제 반격 출격 자격
            nox.onClick.AddListener(() => Choose(FlightBroomPilot.Nox)); // 녹스의 스탠딩과 장비 미리보기
            sera.interactable = FlightPilotRoster.CanSelect(FlightBroomPilot.Sera); // 세라의 시험 출격 자격
            sera.onClick.AddListener(() => Choose(FlightBroomPilot.Sera)); // 세라로 미리보기
            lumi.interactable = FlightPilotRoster.CanSelect(FlightBroomPilot.Lumi); // 루미의 시험 출격 자격
            lumi.onClick.AddListener(() => Choose(FlightBroomPilot.Lumi)); // 루미로 미리보기
            aria.onClick.AddListener(() => Choose(FlightBroomPilot.Aria)); // 아리아로 미리보기
            rian.onClick.AddListener(() => Choose(FlightBroomPilot.Rian)); // 리안으로 미리보기
            back.onClick.AddListener(Return); // 타이틀로 돌아가기
            depart.onClick.AddListener(Depart); // 선택한 조합으로 출격
            editGear.onClick.AddListener(OpenWorkshop); // 기존 빗자루 조립기 연결
            editMagic.onClick.AddListener(OpenMagic); // 기존 제작 마법 화면 연결
            firstSortieGuide.onClick.AddListener(OpenGuide); // 독립된 비행 연습 연결
            freeTraining?.onClick.AddListener(OpenTraining); // 자유 훈련으로 현재 조종사 전달
            bossRush?.onClick.AddListener(OpenBossRush); // 기존 보스와 같은 충돌·패턴의 연속 도전
            Choose(FlightPilotRoster.CanSelect(FlightSortieChoice.Initial) ? FlightSortieChoice.Initial : FlightBroomPilot.Aria); // 이어서하기 또는 첫 조종사
            map?.Initialize(); // 실제 지도 선택지 연결
            expedition?.onClick.AddListener(OpenExpedition); // 마우스로 탐험 지도
            string draft = FlightTrainingMode.ConsumeDraft(); // 연습에서 돌아온 미저장 조합
            if (draft != null) // 제작 화면에서 진입한 연습의 복귀
            {
                ready.SetActive(false); // 출격 요약 대신 편집 화면 표시
                editor.ShowDraft(draft); // 저장하지 않은 조합 복원
            }
            if (map != null && FlightExpedition.ConsumeMapRequest()) // 전투 결과 뒤 같은 지도 열기
            {
                OpenExpedition(); // 정산하고 저장한 다음 위치
            }
        }

        private void StartMagicTraining(CraftedSpellRecipe recipe) // 편집 중인 조합의 독립 자유 연습
        {
            if (moving || recipe == null || editor.HasPreview) // 중복 전환과 내장 연습 씬 충돌 방지
            {
                return; // 기존 화면 유지
            }
            moving = true; // 중복 씬 전환 차단
            FlightTrainingMode.RequestMagic(pilot, recipe); // 선택한 조종사와 미저장 조합 전달
            SceneManager.LoadSceneAsync(FlightFirstSortieGuide.SceneName); // 실제 조종과 표적이 있는 연습 씬
        }

        private void Update() // 실제 키보드로 캐릭터와 이동 선택
        {
            Keyboard keyboard = Keyboard.current; // 현재 물리 키보드
            if (keyboard == null || moving) // 마우스 단독 또는 씬 전환 중
            {
                return; // 추가 출발 없음
            }
            if (storyMenu != null && storyMenu.IsOpen)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) storyMenu.Close();
                return;
            }
            if (ready.activeSelf && keyboard.f7Key.wasPressedThisFrame)
            {
                storyMenu.Open(pilot);
                return;
            }
            if (editor.gameObject.activeSelf && editor.HandlePreviewKey(keyboard)) // 내장 비행의 포커스와 화면 전환 우선 처리
            {
                return; // 같은 키로 제작 화면을 닫거나 출격하지 않음
            }
            if (map != null && map.HandleKey(keyboard)) // 지도 열림 동안 다른 준비 조작 차단
            {
                return; // 지도의 저장 및 선택으로 입력 제한
            }
            if (incident != null && incident.HandleArchiveKey(keyboard, ready.activeSelf)) // 첫 사건 창 열람에 우선 키 전달
            {
                return; // 기록 표시 중 출발·장비 키 처리하지 않음
            }
            if (keyboard.escapeKey.wasPressedThisFrame) // 메뉴에서 뒤로
            {
                if (ready.activeSelf) // 요약 화면에서 메인으로
                {
                    Return(); // 타이틀 이동
                }
                else // 장비 또는 마법 편집
                {
                    ShowSummary(); // 출격 준비 화면으로 복귀
                }
                return; // 같은 키가 다른 UI에 영향을 주지 않게 종료
            }
            if (!ready.activeSelf) // 세부 화면에서 캐릭터와 출발 입력 차단
            {
                return; // 편집 중인 상태 보존
            }
            if (keyboard.f6Key.wasPressedThisFrame) // 현재 조종사로 탐험 지도 열기
            {
                OpenExpedition(); // 버튼과 같은 독립 탐험
                return; // 승인을 한 프레임에 중복 처리하지 않음
            }
            if (keyboard.f5Key.wasPressedThisFrame) // 두 보스를 연이어 만나는 별도 모드
            {
                OpenBossRush(); // 배치된 보스 러시 버튼의 같은 행동
                return; // 출격 키와 중복 진입 방지
            }
            if (keyboard.f4Key.wasPressedThisFrame) // 현재 조종사와 자유 훈련 열기
            {
                OpenTraining(); // 배치한 버튼과 같은 연습 진입
                return; // 같은 프레임의 출격 키와 중복 방지
            }
            if (keyboard.f1Key.wasPressedThisFrame) // 마우스 없이 첫 비행 안내 열기
            {
                OpenGuide(); // 버튼과 같은 격리 연습 경로
                return; // 같은 프레임의 출격 키와 중복 방지
            }
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) // 왼쪽 캐릭터
            {
                Choose((FlightBroomPilot)(((int)pilot + FlightPilotRoster.Count - 1) % FlightPilotRoster.Count)); // 앞 조종사 보기
            }
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) // 오른쪽 캐릭터
            {
                Choose((FlightBroomPilot)(((int)pilot + 1) % FlightPilotRoster.Count)); // 다음 조종사 보기
            }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) // 실제 출발 입력
            {
                Depart(); // 클릭과 같은 씬 이동
            }
        }

        private void Choose(FlightBroomPilot selected) // 장비 편집과 스탠딩 캐릭터 동기화
        {
            if (!ChoiceButton(selected).interactable) // 키 입력과 버튼의 해금 조건 공유
            {
                return; // 저장된 영구 선택지만 적용
            }
            pilot = selected; // 현재 선택 화면의 캐릭터
            gear.SelectPilot(pilot); // 같은 캐릭터의 8슬롯 조립 상태
            ShowSummary(); // 실제 장비를 포함한 요약 표시
        }

        private void ShowSummary() // 저장한 부품·마법을 표시하고 세부 화면 종료
        {
            workshop.gameObject.SetActive(false); // 조립 카메라와 화면 닫기
            editor.gameObject.SetActive(false); // 연습 사격과 화면 닫기
            ready.SetActive(true); // 포트레이트와 출격 정보
            FlightProgressStore profile = FlightProgress.Current; // 실제 원자 정산으로 저장한 성장
            string common = profile.GetCommonFan(pilot) ? "산탄" : pilot != FlightBroomPilot.Aria ? "1회 보호" : "보호막"; // 마을에서 저장한 공용기와 캐릭터 기본값
            portrait.texture = portraits != null ? portraits.For(pilot) : null; // 캐릭터 결과와 같은 기획서 원본
            (title.text, details.text) = pilot switch // 선택한 캐릭터의 현재 스킬과 역할
            {
                FlightBroomPilot.Iris => ("이리스 · 파동·연쇄형 마도사", $"수호팀   ·   여러 적에게 적중하면 파동 범위가 넓어집니다.\nQ  확산 파동    E  {common}    R  창공의 공명"), // 공명과 연쇄 파동의 실제 조작
                FlightBroomPilot.Vera => ("베라 · 설치형 마도 공학자", $"외부 기술자   ·   부유 포대를 설치해 전장을 제어합니다.\nQ  마도 포대    E  {common}    R  교차 광선"), // 실제 마도 포대와 비행 기술
                FlightBroomPilot.Nox => ("녹스 · 회피·반격형 감시자", $"조사팀   ·   스친 적탄의 표식을 모아 반격합니다.\nQ  그림자 반격    E  {common}    R  경계 역전"), // 녹스 표식과 3D 소거
                FlightBroomPilot.Sera => ("세라 · 근접 돌파형 검사", $"수호팀   ·   가까운 적에게 더 큰 피해를 줍니다.\nQ  돌파 검기    E  {common}    R  천공 연참"), // 세라의 수호 역할과 검기
                FlightBroomPilot.Lumi => ("루미 · 유도형 탐색자", $"조사팀   ·   같은 적을 추적할수록 유도가 강해집니다.\nQ  별빛 추적탄    E  {common}    R  별무리 항로"), // 루미의 유도와 공용기
                FlightBroomPilot.Rian => ("리안 · 저격형 수호자", $"수호팀   ·   노출된 약점을 정확하게 노립니다.\nQ  관통 사격    E  {common}    R  천공 관측선"), // 리안의 약점 공격
                _ => ("아리아 · 속사형 조사원", $"조사팀   ·   연속 적중으로 기본 사격의 화력을 높입니다.\nQ  집중 포화    E  {common}    R  유성 탄막") // 아리아의 사격과 공용기
            };
            equipped.text = $"빗자루 몸체  {gear.GetEquippedName(FlightBroomSlot.Body)}\n마법  좌 {SlotName(0)}  /  우 {SlotName(1)}"; // 편성한 부품과 마법의 실제 준비 상태
            progression.text = $"숙련도 {profile.GetMastery(pilot)} · 조율의 증표 {profile.GetMarks(pilot)}"; // 조종사별 성장과 남은 증표
            crystals?.Show(profile.SharedCrystals); // 천공 결정 아이콘과 실제 보유 수치
            gold?.Show(profile.Gold); // 골드 아이콘과 실제 보유 수치
            ChoiceButton(pilot).Select(); // 현재 캐릭터 버튼에 포커스
        }

        private Button ChoiceButton(FlightBroomPilot selected) // 하이러키의 실제 선택 버튼
        {
            return selected switch // 선택한 조종사에 맞는 버튼
            {
                FlightBroomPilot.Rian => rian, // 리안 선택 위치
                FlightBroomPilot.Lumi => lumi, // 루미 선택 위치
                FlightBroomPilot.Sera => sera, // 세라 선택 위치
                FlightBroomPilot.Nox => nox, // 녹스의 실제 선택 위치
                FlightBroomPilot.Vera => vera, // 베라의 실제 선택 위치
                FlightBroomPilot.Iris => iris, // 이리스의 실제 출격 버튼
                _ => aria // 기본 아리아 선택 위치
            };
        }

        private string SlotName(int index) // 현재 제작 마법의 출격 표시
        {
            CraftedSpellRecipe recipe = book.Slot(index); // 저장한 왼쪽과 오른쪽
            return recipe == null ? "빈 슬롯" : book.ActiveSlot(index) != null ? FlightCraftedSpellBook.NameOf(recipe) : "빈 슬롯 (저장 보존)"; // 자동 해제와 잠긴 저장의 실제 빈 칸 표시
        }

        private void OpenWorkshop() // 현재 조종사의 조립 화면
        {
            ready.SetActive(false); // 요약 화면 버튼 숨김
            workshop.Show(); // 선택 조종사의 8슬롯 부품 미리보기
        }

        private void OpenMagic() // 출격용 제작 마법을 편집
        {
            ready.SetActive(false); // 요약과 스탠딩 숨김
            editor.Show(); // 독립 렌더·시험 창
        }

        private void Depart() // 현재 조종사로 실제 전투 시작
        {
            if (moving) // 앞에서 요청한 씬 전환
            {
                return; // 중복 출격 없음
            }
            moving = true; // 다른 버튼과 키 입력 차단
            FlightSortieChoice.Launch(pilot); // 출발을 확정하고 플레이어에 전달
            SceneManager.LoadSceneAsync(FlightSortieChoice.StageScene); // 실제 하늘섬 외곽 항로
        }

        private void OpenExpedition() // 캐릭터를 유지하며 탐험 지도 표시
        {
            if (!moving && ready.activeSelf && map != null) // 출격 준비가 표시될 때만 열기
            {
                FlightAudio.Play(FlightSfx.UiConfirm); // 같은 메뉴의 클릭 소리
                map.Show(pilot); // 현재 또는 저장한 조종사의 지도
            }
        }

        private void OpenBossRush() // 장비 편성으로 실제 첫 보스에 들어가는 진입
        {
            if (moving) // 한 프레임에 입력이 겹쳐도 한 번만
            {
                return; // 이미 시작된 화면 전환 보존
            }
            moving = true; // 중복 준비 버튼 차단
            FlightSortieChoice.LaunchBossRush(pilot); // 지금 선택한 장비와 마법의 조종사
            SceneManager.LoadSceneAsync(FlightSortieChoice.StageScene); // 실제 보스의 월드 위치와 동작
        }

        private void OpenTraining() // 첫 항로에 참여하지 않는 자유 훈련 요청
        {
            if (moving || editor.HasPreview) // 중복 입력과 아직 해제되지 않은 내장 연습 씬
            {
                return; // 새 씬 준비 한 번만 처리
            }
            moving = true; // 버튼과 키의 중복 진입 금지
            FlightTrainingMode.Request(pilot); // 현재 조종사를 자유 훈련으로 전달
            SceneManager.LoadSceneAsync(FlightFirstSortieGuide.SceneName); // 같은 물리와 키가 있는 독립 시험 항로
        }

        private void OpenGuide() // 실제 출격과 분리한 비행 훈련
        {
            if (moving || editor.HasPreview) // 중복 출발과 내장 연습 씬의 동시 로드 방지
            {
                return; // 씬 로드 한 번 유지
            }
            moving = true; // 중복 입력 차단
            FlightFirstSortieGuide.Request(pilot); // 현재 조종사로 안내 준비
            SceneManager.LoadSceneAsync(FlightFirstSortieGuide.SceneName); // 기존에 편집한 연습 항로로 이동
        }

        private void Return() // 출발을 확정하지 않고 제목으로 복귀
        {
            if (moving) // 씬 전환의 중복 처리 차단
            {
                return; // 같은 로드 작업만 유지
            }
            moving = true; // 메인 씬 로딩 시작
            if (FlightSortieChoice.ViaVillage) // 마을 출격소에서 들어온 준비 화면
            {
                FlightSortieChoice.RememberPilot(pilot); // 마을 방문 뒤 같은 조종사 유지
                FlightIslandVillage.ReturnFromBriefing(); // 북쪽 출격소 바로 앞 복귀
                return; // 일반 타이틀 복귀 제외
            }
            SceneManager.LoadSceneAsync(FlightTitleMenu.SceneName); // 직접 진입한 준비의 타이틀 복귀
        }
    }
}
