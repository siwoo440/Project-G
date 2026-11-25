using System; // 조립 화면 닫기 동작
using System.Collections.Generic; // 선택한 슬롯의 공용 부품 목록
using System.Globalization; // 비교 수치의 일관된 소수점
using System.Text; // 능력치 비교 문장 구성
using UnityEngine; // 미리보기와 씬 오브젝트
using UnityEngine.Rendering; // 그래픽 없는 검증 모드 확인
using UnityEngine.UI; // 실제 조립 화면의 버튼과 글자

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightBroomWorkshop : MonoBehaviour // 빗자루 시험 조립 화면
    {
        private static readonly string[] SlotNames = { "몸체", "손잡이", "마력석", "결합부", "추진부", "마법 장식", "선단부", "안정기" }; // 부착점 표시 순서
        private static readonly string[] PilotNames = { "아리아", "리안", "루미", "세라", "녹스", "베라", "이리스" }; // 캐릭터 이름
        private static readonly string[] Grades = { "I", "II", "III", "IV", "V" }; // 부품 희귀도
        private static readonly string[] Stats = { "최고 속도", "가속력", "제동력", "선회", "안정성", "상승", "하강", "최대 마력", "마력 회복", "마법 공격력", "탄환 속도", "마법 사거리", "피해 감소", "충격 저항" }; // 비교 항목
        private static readonly string[] Units = { " m/s", " m/s²", " m/s²", "°/초", "", " m/s", " m/s", "", "/초", "", "%p", "%p", "%p", "%p" }; // 각 수치의 실제 단위

        [SerializeField] private FlightBroomEquipment equipment; // 실전 장착 조합
        [SerializeField] private Button[] slotButtons; // 씬에서 배치한 여덟 슬롯 버튼
        [SerializeField] private Button[] partButtons; // 선택한 슬롯의 후보 표시
        [SerializeField] private MeshRenderer[] previewMounts; // 미리보기 전용 부착점
        [SerializeField] private Transform previewPivot; // 3D 모델의 회전 기준점
        [SerializeField] private Text heading; // 현재 캐릭터와 선택한 슬롯
        [SerializeField] private Text comparison; // 전후 능력치 변화
        [SerializeField] private Text status; // 장착 조건과 저장 상태
        [SerializeField] private Button back; // 정지 화면 복귀
        [SerializeField] private Button equip; // 장착 확정
        [SerializeField] private Button remove; // 장착 해제 후보
        private readonly List<FlightBroomEquipment.PartOption> options = new(); // 현재 슬롯에서 고를 수 있는 부품
        private readonly StringBuilder differences = new(); // 비교 문구의 재사용 버퍼
        private FlightBroomSlot slot; // 편집할 부착 부위
        private int selected = -1; // 목록 선택 또는 빈 자리
        private Text[] slotLabels; // 슬롯 버튼의 한글 설명
        private Text[] partLabels; // 보유 부품의 이름과 등급
        private Action close; // 현재 메뉴의 뒤로 가기

        internal void Initialize(Action closeWorkshop) // 정지 메뉴의 준비 단계에서 연결
        {
            Camera previewCamera = GetComponentInChildren<Camera>(true); // 조립 화면의 전용 카메라
            previewCamera.enabled = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null; // 실제 그래픽 장치가 있을 때만 미리보기 렌더
            close = closeWorkshop; // 되돌아갈 실제 메뉴
            back.onClick.AddListener(() => close()); // 뒤로 버튼에서 연결
            equip.onClick.AddListener(Confirm); // 장착을 확정할 때만 변경
            remove.onClick.AddListener(() => SelectPart(-1)); // 빈 슬롯을 미리 확인
            slotLabels = new Text[slotButtons.Length]; // 선택 강조를 저장할 여덟 행
            partLabels = new Text[partButtons.Length]; // 목록 글자 참조
            for (int i = 0; i < slotButtons.Length; i++) // 고정 슬롯 연결
            {
                int index = i; // 각 버튼의 고정 슬롯 번호
                slotLabels[i] = slotButtons[i].GetComponentInChildren<Text>(); // 슬롯 이름
                slotButtons[i].onClick.AddListener(() => SelectSlot((FlightBroomSlot)index)); // 슬롯을 변경하고 후보 표시
            }
            for (int i = 0; i < partButtons.Length; i++) // 프리배치한 후보 목록 연결
            {
                int index = i; // 현재 화면의 후보 자리
                partLabels[i] = partButtons[i].GetComponentInChildren<Text>(); // 부품 이름
                partButtons[i].onClick.AddListener(() => SelectPart(index)); // 장착 전 미리보기
            }
        }

        internal void Show() // 현재 활동 중인 캐릭터의 조합 표시
        {
            gameObject.SetActive(true); // 전용 프리뷰 카메라와 UI 켜기
            previewPivot.localRotation = Quaternion.Euler(8f, -32f, 0f); // 길이와 앞뒤 부착점이 보이는 초기 각도
            SelectSlot(FlightBroomSlot.Body); // 첫 번째 부위의 현재 장착 확인
        }

        private void Update() // 정지 상태에서도 미리보기만 회전
        {
            previewPivot.Rotate(Vector3.up, 18f * Time.unscaledDeltaTime, Space.World); // 비행 시간과 독립적인 3D 회전
        }

        private void SelectSlot(FlightBroomSlot value) // 선택한 부위의 보유 부품 읽기
        {
            slot = value; // 새 슬롯 선택
            equipment.ListParts(slot, options); // 조립 가능 목록 읽기
            string current = CurrentId(); // 현재 사용 중인 부품
            selected = options.FindIndex(part => part.Id == current); // 장착 부품으로 미리보기 초기화
            Refresh(); // 목록과 비교와 3D 갱신
        }

        private void SelectPart(int value) // 후보만 바꾸고 실제 조합 보존
        {
            selected = value; // 현재 비교 대상
            Refresh(); // 시각과 능력치 차이 반영
        }

        private void Confirm() // 사용자가 승인한 후보만 적용·저장
        {
            if (!equipment.ConfirmPart(slot, CandidateId())) // 장착 가능 조건 재검사
            {
                Refresh(); // 실패한 조건 표시 복원
                return; // 실제 장착 변경 없음
            }

            SelectSlot(slot); // 변경된 전체 조합 다시 읽기
            status.text = "장착 상태를 저장했습니다."; // 이번 교체를 저장한 결과
        }

        private void Refresh() // 편집 중인 부위·후보·비교 결과 출력
        {
            int pilot = (int)equipment.ActivePilot; // 현재 조작하는 캐릭터
            string current = CurrentId(); // 실제 장착 부품
            string candidate = CandidateId(); // 미리보기와 확정 후보
            heading.text = $"{PilotNames[pilot]}  /  {SlotNames[(int)slot]}"; // 선택한 캐릭터와 부착 부위
            for (int i = 0; i < slotLabels.Length; i++) // 현재 슬롯 시각 표시
            {
                slotLabels[i].text = i == (int)slot ? $"●  {SlotNames[i]}" : SlotNames[i]; // 선택 슬롯에 점 표시
            }
            for (int i = 0; i < partButtons.Length; i++) // 편집 가능한 목록 오브젝트 갱신
            {
                bool visible = i < options.Count; // 슬롯에 들어갈 후보가 있는 행
                partButtons[i].gameObject.SetActive(visible); // 후보 없는 행 감춤
                if (!visible) // 비어 있는 행
                {
                    continue; // 이전 이름을 다시 읽지 않음
                }
                FlightBroomEquipment.PartOption part = options[i]; // 행의 보유 부품 정보
                string marker = part.Id == current ? " · 장착" : part.Owner >= 0 && part.Owner != pilot ? $" · {PilotNames[part.Owner]}" : ""; // 장착된 캐릭터 표시
                string copy = part.Copies > 1 ? $" #{part.Id[(part.Id.LastIndexOf(':') + 1)..]}" : ""; // 동일 종류의 보유 순번 구분
                partLabels[i].text = $"{(i == selected ? "●  " : "")}{part.Name}{copy}  {Grades[part.Grade - 1]}+{part.Upgrade}{marker}"; // 이름·등급·강화·장착
            }

            equipment.ShowPreview(previewMounts, slot, candidate); // 시각 전용 장비에 후보 외형 적용
            bool usedElsewhere = selected >= 0 && options[selected].Owner >= 0 && options[selected].Owner != pilot; // 타 캐릭터의 부품 여부
            equip.interactable = candidate != current && !usedElsewhere; // 같은 부품과 다른 캐릭터 부품 확정 차단
            remove.interactable = current != null; // 장착한 부품이 있을 때 해제 미리보기
            status.text = usedElsewhere ? $"{PilotNames[options[selected].Owner]} 장착 중" : candidate == current ? "현재 장착 조합" : "확정 전 미리보기"; // 후보 상태
            differences.Clear(); // 이전 후보 비교 문구 제거
            for (int i = 0; i < Stats.Length; i++) // 모든 주 효과와 불이익 검사
            {
                float delta = equipment.ComparePart(slot, candidate, (FlightBroomStat)i); // 현재 조합에서 후보로 바뀌는 값
                if (Mathf.Abs(delta) > 0.0001f) // 바뀌는 항목만 표시
                {
                    differences.Append(Stats[i]).Append("  ").Append(delta.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture)).AppendLine(Units[i]); // 변화량과 단위
                }
            }
            comparison.text = differences.Length == 0 ? "능력치 변화 없음" : differences.ToString().TrimEnd(); // 달라지는 능력치 또는 동일 안내
        }

        private string CurrentId() // 실제 슬롯의 장착 ID
        {
            return equipment.GetEquippedId(equipment.ActivePilot, slot); // 현재 장착 상태
        }

        private string CandidateId() // 확정 전 선택한 개별 부품
        {
            return selected < 0 ? null : options[selected].Id; // 빈 장착 또는 보유 ID
        }
    }
}
