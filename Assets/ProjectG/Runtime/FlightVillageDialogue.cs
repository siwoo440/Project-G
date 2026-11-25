using System; // 마을 쪽으로 전달할 선택 결과
using System.Globalization; // 조합 문자를 포함한 대사의 음절 나눔
using UnityEngine; // 씬 카메라와 NPC 대화 상태
using UnityEngine.EventSystems; // Enter와 기존 UI 제출의 충돌 방지
using UnityEngine.InputSystem; // F와 선택지 단축키
using UnityEngine.UI; // 대화 텍스트와 선택 버튼

namespace ProjectG // 프로젝트 하늘섬 마을
{
    [DisallowMultipleComponent] // 마을 전체에서 대화 화면 한 개
    public sealed class FlightVillageDialogue : MonoBehaviour // 대사 공개·선택 처리와 NPC 카메라
    {
        [SerializeField] private GameObject panel; // 미연시형 하단 대화의 실제 씬 그룹
        [SerializeField] private Text speaker; // 현재 역할 이름
        [SerializeField] private Text body; // 한 글자씩 올라오는 대사
        [SerializeField] private GameObject options; // 선택지를 보여줄 영역
        [SerializeField] private Button[] answers; // 화면과 숫자 키를 공유하는 세 버튼
        [SerializeField] private Text[] captions; // 버튼 위의 실제 선택 문구
        [SerializeField] private Button advance; // 클릭으로 대사 완료하거나 다음 줄
        [SerializeField] private Text hint; // 글자 출력 및 다음 선택의 도움말
        [SerializeField] private Button dismiss; // 대화를 취소하고 마을로 복귀
        [SerializeField] private Camera village; // 본래 탑뷰의 실제 카메라
        [SerializeField] private Camera sceneView; // 대화할 때 NPC를 가까이 보는 카메라
        [SerializeField] private GameObject visitorModel; // 대화 시점에서 가려지는 도보 조종사 모델
        [SerializeField] private GameObject walkingKeys; // 대화 아래에서 가려지는 마을 조작 설명
        private bool visitorVisible; // 대화가 끝나면 유지할 기존 외형의 표시
        private bool walkingKeysVisible; // 대화 전 마을 조작 설명의 표시
        private static readonly Color Raised = new Color(1f, .83f, .49f); // 현재 선택 항목의 강조
        private static readonly Color Normal = new Color(.94f, .97f, 1f); // 다른 선택 아이템
        private FlightVillageNpc npc; // 지금 실제로 대화하는 사람
        private FlightVillageNpc.Line line; // 화면에 올라오는 한 문장
        private int[] sections; // 글자 결합 단위의 시작 위치
        private string words; // 해당 대사의 전체 문자열
        private float elapsed; // 공개할 글자의 진행 시간
        private int visible; // 현재 실제 보이는 글자 수
        private int selected; // 화살표·숫자로 선택한 응답
        private bool navigation; // 대화 전의 UI 제출 상태
        internal bool IsOpen => npc != null; // 입력을 잠그는 실제 대화 상태
        internal event Action<FlightVillageService> ServiceChosen; // 마을의 기존 시설 열기 경로

        private void Start() // 씬에 미리 배치한 대화 인터랙션
        {
            advance.onClick.AddListener(Next); // 마우스의 다음 문장이나 빠른 공개
            dismiss.onClick.AddListener(Close); // ESC와 같은 대화 끝
            for (int index = 0; index < answers.Length; index++) // 실제 편집한 응답 버튼을 순서대로 연결
            {
                int choice = index; // 해당 단추의 현재 행 위치
                answers[index].onClick.AddListener(() => Choose(choice)); // 키와 동일한 분기 처리
            }
        }

        internal void Begin(FlightVillageNpc partner) // 실제 가까운 NPC를 현재 대화에 연결
        {
            if (IsOpen || partner.At(0) == null) // 이미 표시 중이거나 편집 중인 빈 대본
            {
                return; // 시설·마을 조작의 현재 화면 유지
            }
            npc = partner; // 이후의 현재 대사와 선택 대상
            speaker.text = npc.Speaker; // 대사보다 위의 실제 화자 이름
            Vector3 front = npc.Front; // 시설 바깥으로 미리 정한 NPC의 방향
            Vector3 side = Vector3.Cross(Vector3.up, front); // 인물을 조금 옆에서 보이는 방향
            Vector3 viewpoint = npc.Focus + front * 4.65f - side * .58f + Vector3.up * .65f; // 얼굴과 옷을 건물 밖에서 보는 위치
            sceneView.transform.position = viewpoint; // 현재 NPC의 읽기 쉬운 거리
            sceneView.transform.LookAt(npc.Focus - Vector3.up * .25f); // 모델이 하단 대사 뒤에 가려지지 않는 배치
            npc.SetTalking(true, viewpoint); // NPC 얼굴이 실제 대화 카메라를 향하게 회전
            visitorVisible = visitorModel.activeSelf; // 대화가 끝날 때 유지할 도보 외형 표시
            walkingKeysVisible = walkingKeys.activeSelf; // 마을의 원래 안내 상태
            visitorModel.SetActive(false); // 대화 카메라 앞에서 NPC를 가리지 않게 숨김
            walkingKeys.SetActive(false); // 대화창과 기존 조작 표시의 중복 제거
            sceneView.enabled = true; // 3D NPC의 실제 화면 표시
            village.enabled = false; // 한 화면에 카메라 하나만 렌더
            if (EventSystem.current != null) // 페이지가 배치된 실제 입력 시스템
            {
                navigation = EventSystem.current.sendNavigationEvents; // 원래 메뉴의 탭과 제출 상태
                EventSystem.current.SetSelectedGameObject(null); // 뒤의 버튼이 Enter를 받지 않게 제거
                EventSystem.current.sendNavigationEvents = false; // 이 대화가 직접 결정하는 키 제출
            }
            FlightAudio.Play(FlightSfx.UiConfirm); // 실제 대화 시작 버튼과 같은 신호
            panel.SetActive(true); // 화자와 대사 프레임 표시
            Show(0); // 첫 실제 문장부터 진행
        }

        internal void Handle(Keyboard keyboard) // 입력을 받은 마을의 현재 NPC 대화
        {
            if (keyboard.escapeKey.wasPressedThisFrame) // 선택을 확정하지 않고 나가기
            {
                Close(); // 마을만 복귀
                return; // 같은 키가 타이틀까지 전달되지 않음
            }
            if (ChoiceCount > 0 && visible == sections.Length) // 응답이 준비된 문장에서만 커서 사용
            {
                if (keyboard.digit1Key.wasPressedThisFrame) // 첫 응답 단축키
                {
                    Choose(0); // 같은 화면의 첫 응답
                    return; // 이미 실행한 선택 중복 차단
                }
                if (keyboard.digit2Key.wasPressedThisFrame) // 두 번째 응답 단축키
                {
                    Choose(1); // 같은 화면 두 번째 행
                    return; // 다른 키의 확정 경로 제외
                }
                if (keyboard.digit3Key.wasPressedThisFrame) // 세 번째 응답 단축키
                {
                    Choose(2); // 화면의 닫기나 추가 분기
                    return; // 같은 이벤트의 실제 수행 한 번
                }
                int shift = (keyboard.downArrowKey.wasPressedThisFrame ? 1 : 0) - (keyboard.upArrowKey.wasPressedThisFrame ? 1 : 0); // 위아래로 고를 응답
                if (shift != 0) // 현재 선택 위치가 달라진 경우
                {
                    selected = (selected + shift + ChoiceCount) % ChoiceCount; // 범위를 순환하는 방향
                    PaintSelection(); // 눈으로 읽는 현재 응답 표시
                }
            }
            if (keyboard.fKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) // 클릭과 같은 다음 또는 확정
            {
                Next(); // 출력 속도와 응답을 같은 규칙으로 처리
            }
        }

        private int ChoiceCount => Mathf.Min(line.choices.Length, answers.Length); // 씬에 실제 있는 응답 수

        private void Update() // 다 읽지 않은 문장의 짧은 타자 효과
        {
            if (!IsOpen || visible >= sections.Length) // 닫혔거나 문장 출력이 끝난 프레임
            {
                return; // 새 문자열이 없는 렌더 생략
            }
            elapsed += Time.unscaledDeltaTime; // 다른 메뉴 속도에 영향 없는 실제 초
            int next = Mathf.Min(sections.Length, (int)(elapsed * 40f)); // 초당 40글자
            if (next != visible) // 읽을 글자가 한 음절 이상 늘었음
            {
                Reveal(next); // 현재 읽을 유효 문자열만 갱신
            }
        }

        private void Show(int index) // 다음 대본이나 마지막 문장 종료
        {
            line = npc.At(index); // 씬에서 편집한 실제 기록
            if (line == null) // 대화를 끝내는 번호
            {
                Close(); // 마을과 원래 카메라로 복귀
                return; // 비어 있는 장면 제외
            }
            words = line.text ?? string.Empty; // 비어 있는 임시 문장도 허용
            sections = StringInfo.ParseCombiningCharacters(words); // 한글이나 여러 문자 음절의 온전한 문자 단위
            elapsed = 0f; // 새 줄의 글자 시간
            selected = 0; // 선택지의 처음 위치
            for (int i = 0; i < answers.Length; i++) // 새 문장의 실제 응답을 배치
            {
                bool exists = i < ChoiceCount; // 현재 문장에 작성된 응답
                answers[i].gameObject.SetActive(exists); // 남는 슬롯 감춤
                if (exists) // 화면 버튼에 현재 응답 지정
                {
                    captions[i].text = $"{i + 1}   {line.choices[i].label}"; // 숫자 키와 같은 버튼 순서
                }
            }
            PaintSelection(); // 대사가 끝난 상태의 기본 선택 색상
            Reveal(0); // 타자 효과의 첫 프레임부터
        }

        private void Reveal(int count) // 글자를 잘라 표시하고 선택과 다음 키 안내
        {
            visible = count; // 다음 프레임과 비교할 실제 표시 수
            bool done = visible == sections.Length; // 전체 문장을 읽을 수 있는 상태
            body.text = done ? words : words.Substring(0, sections[visible]); // 한글 결합 문자의 중간 분리 방지
            bool withChoices = done && ChoiceCount > 0; // 전체 문장이 나온 뒤에만 응답 표시
            if (options.activeSelf != withChoices) // 화살표 반응 예고가 바뀌는 경우
            {
                options.SetActive(withChoices); // 실제 응답 패널 표시
            }
            if (advance.gameObject.activeSelf == withChoices) // 선택지가 보이면 직접 답변 버튼만 표시
            {
                advance.gameObject.SetActive(!withChoices); // 대사 공개와 일반 진행에만 다음 버튼 표시
            }
            hint.text = withChoices ? "↑↓ 선택    F / Enter 확인    Esc 닫기" : done ? "F / Enter 다음    Esc 닫기" : "F / Enter 대사 펼치기    Esc 닫기"; // 현재 단계에서 실제 가능한 동작
        }

        private void PaintSelection() // 현재 선택지를 앞쪽 색으로 강조
        {
            for (int i = 0; i < ChoiceCount; i++) // 작동 중인 선택지 범위
            {
                captions[i].color = i == selected ? Raised : Normal; // 현재 입력을 받을 실제 버튼
            }
        }

        private void Next() // 한 번 눌러 남은 대사 또는 응답을 처리
        {
            if (visible < sections.Length) // 처음 클릭은 남은 문장 표시
            {
                Reveal(sections.Length); // 빠르게 읽는 사용자의 즉시 공개
            }
            else if (ChoiceCount > 0) // 읽은 문장에 실제 응답이 있는 경우
            {
                Choose(selected); // 강조해 둔 선택을 확정
            }
            else // 선택 없는 일반 문장
            {
                Show(line.next); // 다음 문장 또는 대화 종료
            }
        }

        private void Choose(int index) // 버튼과 키가 사용하는 실제 분기
        {
            if (!IsOpen || visible != sections.Length || (uint)index >= ChoiceCount) // 현재 표시된 응답만 유효
            {
                return; // 아직 출력 중인 문장과 없는 선택지 제외
            }
            FlightVillageNpc.Choice choice = line.choices[index]; // 실제 씬 데이터의 선택
            if (choice.service == FlightVillageService.None) // 상점 대신 대사를 잇는 선택
            {
                FlightAudio.Play(FlightSfx.UiConfirm); // 확인 입력
                Show(choice.next); // 정해 둔 대화 분기
                return; // 다른 화면 유지
            }
            Close(); // 다른 UI를 열기 전에 원래 마을 화면으로 복구
            ServiceChosen?.Invoke(choice.service); // 실제 상점·공방·출격 준비
        }

        private void Close() // 기존 NPC 자세와 원래 조작을 되돌림
        {
            if (!IsOpen) // 같은 클릭이나 외부 닫기에서 반복 방지
            {
                return; // 사용 가능한 화면만 복원
            }
            npc.SetTalking(false, Vector3.zero); // 모델은 원래 자세로 복귀
            npc = null; // 마을의 키 처리 소유권 반환
            panel.SetActive(false); // 다른 시설을 같은 프레임에 열 수 있음
            sceneView.enabled = false; // 현재 기록 카메라 비활성화
            village.enabled = true; // 원래 도보 시점으로 복귀
            visitorModel.SetActive(visitorVisible); // 대화 전 도보 외형의 표시 복원
            walkingKeys.SetActive(walkingKeysVisible); // 대화 전 마을 조작 설명 복원
            if (EventSystem.current != null) // 실제 메뉴 입력 시스템
            {
                EventSystem.current.sendNavigationEvents = navigation; // 대화 전의 메뉴 제출 상태
            }
            FlightAudio.Play(FlightSfx.UiBack); // 대화를 마친 실제 신호
        }
    }
}
