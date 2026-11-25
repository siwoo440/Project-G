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
        [SerializeField] private Texture2D ariaStanding; // 아리아 원본 이미지
        [SerializeField] private Texture2D rianStanding; // 리안 원본 이미지
        [SerializeField] private Text title; // 활동 캐릭터 이름과 전투 역할
        [SerializeField] private Text details; // 이미 확정한 설명과 Q·E·R
        [SerializeField] private Text equipped; // 출발에 반영되는 현재 실제 조합
        [SerializeField] private Button aria; // 조사팀 속사형 선택
        [SerializeField] private Button rian; // 수호팀 저격형 선택
        [SerializeField] private Button back; // 왼쪽 위의 메뉴 복귀
        [SerializeField] private Button depart; // 오른쪽 아래 실제 출발
        [SerializeField] private Button editGear; // 장착 부품의 선택
        [SerializeField] private Button editMagic; // 실제 좌우 제작 마법 변경
        private FlightBroomPilot pilot; // 선택 화면의 현재 조종사
        private bool moving; // 중복 씬 전환 방지

        private void Start() // 두 개의 검증된 캐릭터와 저장 흐름
        {
            Time.timeScale = 1f; // 메인 화면과 같이 실제 메뉴 시간
            Cursor.lockState = CursorLockMode.None; // UI 마우스 포인터
            Cursor.visible = true; // 첫 선택에서 커서 표시
            EventSystem.current.sendNavigationEvents = false; // 자체 선택 키와 UI 제출 중복 방지
            workshop.Initialize(ShowSummary); // 저장한 장비로 출격 요약 갱신
            editor.Initialize(ShowSummary); // 마법 조합도 같은 화면으로 복귀
            aria.onClick.AddListener(() => Choose(FlightBroomPilot.Aria)); // 아리아로 미리보기
            rian.onClick.AddListener(() => Choose(FlightBroomPilot.Rian)); // 리안으로 미리보기
            back.onClick.AddListener(Return); // 타이틀로 돌아가기
            depart.onClick.AddListener(Depart); // 선택한 조합으로 출격
            editGear.onClick.AddListener(OpenWorkshop); // 기존 빗자루 조립기 연결
            editMagic.onClick.AddListener(OpenMagic); // 기존 제작 마법 화면 연결
            Choose(FlightSortieChoice.Initial); // 이어서하기 또는 최초 캐릭터
        }

        private void Update() // 실제 키보드로 캐릭터와 이동 선택
        {
            Keyboard keyboard = Keyboard.current; // 현재 물리 키보드
            if (keyboard == null || moving) // 마우스 단독 또는 씬 전환 중
            {
                return; // 추가 출발 없음
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
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) // 왼쪽 캐릭터
            {
                Choose(FlightBroomPilot.Aria); // 아리아 보기
            }
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) // 오른쪽 캐릭터
            {
                Choose(FlightBroomPilot.Rian); // 리안 보기
            }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) // 실제 출발 입력
            {
                Depart(); // 클릭과 같은 씬 이동
            }
        }

        private void Choose(FlightBroomPilot selected) // 장비 편집과 스탠딩 캐릭터 동기화
        {
            pilot = selected; // 현재 선택 화면의 캐릭터
            gear.SelectPilot(pilot); // 같은 캐릭터의 8슬롯 조립 상태
            ShowSummary(); // 실제 장비를 포함한 요약 표시
        }

        private void ShowSummary() // 저장한 부품·마법을 표시하고 세부 화면 종료
        {
            workshop.gameObject.SetActive(false); // 조립 카메라와 화면 닫기
            editor.gameObject.SetActive(false); // 연습 사격과 화면 닫기
            ready.SetActive(true); // 포트레이트와 출격 정보
            bool sniper = pilot == FlightBroomPilot.Rian; // 선택한 전투 역할
            portrait.texture = sniper ? rianStanding : ariaStanding; // 같은 캐릭터의 기획서 원본
            title.text = sniper ? "리안 · 저격형 수호자" : "아리아 · 속사형 조사원"; // 현재 사용 가능한 캐릭터
            details.text = sniper // 확정한 소속과 핵심 전투 방식
                ? "수호팀   ·   노출된 약점을 정확하게 노립니다.\nQ  관통 사격    E  1회 보호    R  천공 관측선" // 리안의 실제 현재 키와 기술
                : "조사팀   ·   연속 적중으로 기본 사격의 화력을 높입니다.\nQ  집중 포화    E  보호막    R  유성 탄막"; // 아리아의 실제 스킬
            equipped.text = $"빗자루 몸체  {gear.GetEquippedName(FlightBroomSlot.Body)}\n마법  좌 {SlotName(0)}  /  우 {SlotName(1)}"; // 편성한 부품과 마법의 실제 준비 상태
            (sniper ? rian : aria).Select(); // 현재 캐릭터 버튼에 포커스
        }

        private string SlotName(int index) // 현재 제작 마법의 출격 표시
        {
            CraftedSpellRecipe recipe = book.Slot(index); // 저장한 왼쪽과 오른쪽
            return recipe == null ? "초기 전투 카드" : FlightCraftedSpellBook.NameOf(recipe); // 출격에 반영할 실제 마법
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

        private void Return() // 출발을 확정하지 않고 제목으로 복귀
        {
            if (moving) // 씬 전환의 중복 처리 차단
            {
                return; // 같은 로드 작업만 유지
            }
            moving = true; // 메인 씬 로딩 시작
            SceneManager.LoadSceneAsync(FlightTitleMenu.SceneName); // 정지 없는 메인 화면 복귀
        }
    }
}
