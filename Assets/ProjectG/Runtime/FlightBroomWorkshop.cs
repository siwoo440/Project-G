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
        private Button previousPage; // 이전 개별 부품 페이지
        private Button nextPage; // 다음 개별 부품 페이지
        private Text pageLabel; // 현재 목록과 전체 페이지
        private int page; // 같은 슬롯의 현재 부품 페이지
        private Button confirmDefaults; // 기본 부품 전체 복원 확정
        private Button cancelDefaults; // 기본 부품 외형 미리보기 취소
        private string[] pendingDefaults; // 확인 전의 여덟 기본 부품
        private Action close; // 현재 메뉴의 뒤로 가기

        internal void Initialize(Action closeWorkshop) // 정지 메뉴의 준비 단계에서 연결
        {
            Camera previewCamera = GetComponentInChildren<Camera>(true); // 조립 화면의 전용 카메라
            previewCamera.enabled = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null; // 실제 그래픽 장치가 있을 때만 미리보기 렌더
            close = closeWorkshop; // 되돌아갈 실제 메뉴
            gameObject.AddComponent<FlightBroomPresetPanel>().Initialize(equipment, back, heading.font, transform, PreviewDefaults, () => SelectSlot(slot)); // 같은 장비와 갱신을 공유하는 프리셋 화면
            gameObject.AddComponent<FlightBroomGalleryPanel>().Initialize(equipment, back, heading.font, transform); // 같은 부품 카탈로그의 실제 도감 화면
            confirmDefaults = DefaultButton(equip, "ConfirmDefaults", "기본 복원 확정"); // 기존 확정 위치의 실제 확인 버튼
            cancelDefaults = DefaultButton(remove, "CancelDefaults", "취소"); // 기존 미리보기 위치의 취소 버튼
            confirmDefaults.onClick.AddListener(ConfirmDefaults); // 여덟 부품을 검증 후 함께 확정
            cancelDefaults.onClick.AddListener(CancelDefaults); // 실제 장착은 바꾸지 않고 복귀
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
                partButtons[i].onClick.AddListener(() => SelectPart(page * partButtons.Length + index)); // 현재 페이지의 실제 사본 미리보기
            }
            Transform list = partButtons[0].transform.parent; // 두 조립 씬에서 공유하는 부품 목록
            previousPage = PageButton(partButtons[0], list, "PreviousPage", new Vector2(-125f, -250f), "이전"); // 목록 아래의 이전 페이지
            nextPage = PageButton(partButtons[0], list, "NextPage", new Vector2(125f, -250f), "다음"); // 목록 아래의 다음 페이지
            previousPage.onClick.AddListener(() => ChangePage(-1)); // 실제 이전 목록 표시
            nextPage.onClick.AddListener(() => ChangePage(1)); // 실제 다음 목록 표시
            pageLabel = Instantiate(partLabels[0], list, false); // 기존 글꼴과 색상을 공유하는 페이지 표시
            pageLabel.name = "Page"; // 실제 페이지 표시 식별자
            RectTransform pageRect = pageLabel.rectTransform; // 표시할 페이지의 UI 영역
            pageRect.anchorMin = pageRect.anchorMax = new Vector2(0.5f, 0.5f); // 목록 중앙 기준
            pageRect.anchoredPosition = new Vector2(0f, -250f); // 앞뒤 버튼 사이
            pageRect.sizeDelta = new Vector2(100f, 40f); // 읽기 쉬운 페이지 너비
            pageLabel.alignment = TextAnchor.MiddleCenter; // 페이지 숫자 중앙 정렬
            pageLabel.raycastTarget = false; // 양옆 버튼 입력 가림 방지
        }

        private Button DefaultButton(Button template, string name, string label) // 기존 조립 위치의 기본 복원 확인 버튼
        {
            Button button = Instantiate(template, transform, false); // 기존 이미지와 위치 재사용
            button.name = name; // 실제 화면과 시험 식별자
            button.onClick = new Button.ButtonClickedEvent(); // 원래 버튼의 조작 제거
            button.GetComponentInChildren<Text>().text = label; // 사용자가 선택할 확인 동작
            FlightCraftedUi.FitLabel(button); // 버튼에 맞는 선명한 글자
            button.gameObject.SetActive(false); // 기본 미리보기 전에는 숨김
            return button; // 실제 확인 동작 연결
        }

        private void PreviewDefaults() // 기본 부품 전체의 외형만 먼저 확인
        {
            string[] ids = equipment.StarterParts(); // 현재 캐릭터의 여덟 시작 부품
            if (Array.Exists(ids, string.IsNullOrEmpty)) // 캐릭터의 기본 부품 정의 누락
            {
                status.text = "기본 부품 구성을 확인하세요."; // 불완전한 기본 조합 안내
                return; // 확인 버튼과 실제 장착 변경 없음
            }
            if (!equipment.ShowPresetPreview(previewMounts, ids, out string reason)) // 미보유·다른 캐릭터 장착·미리보기 검사
            {
                status.text = reason; // 실패한 부위와 장착 충돌 안내
                return; // 확인 버튼과 실제 장착 변경 없음
            }
            pendingDefaults = ids; // 사용자 확인을 기다리는 전체 부품
            SetDefaultPending(true); // 다른 편집을 막고 확인·취소 표시
            status.text = "기본 부품 미리보기 · 확정 또는 취소"; // 실제 장착 전임을 명확히 표시
        }

        private void ConfirmDefaults() // 확인한 기본 부품의 전체 장착
        {
            bool applied = pendingDefaults != null && equipment.ApplyPresetParts((int)equipment.ActivePilot, pendingDefaults); // 최신 소유와 충돌을 다시 검사
            SetDefaultPending(false); // 일괄 확인 버튼 정리
            pendingDefaults = null; // 이전 미리보기 후보 제거
            SelectSlot(slot); // 실제 장착과 미리보기 다시 읽기
            status.text = applied ? "기본 부품을 모두 장착했습니다." : "기본 부품을 적용하지 못했습니다."; // 실제 확정 결과
        }

        private void CancelDefaults() // 실제 장착을 변경하지 않는 미리보기 취소
        {
            SetDefaultPending(false); // 일괄 확인 버튼 정리
            pendingDefaults = null; // 취소한 후보 제거
            SelectSlot(slot); // 원래 실제 장착의 외형 복원
            status.text = "기본 부품 복원을 취소했습니다."; // 실제 변경이 없음을 안내
        }

        private void SetDefaultPending(bool pending) // 일괄 미리보기 중 다른 조립 입력 제어
        {
            confirmDefaults.gameObject.SetActive(pending); // 확정 버튼 표시
            cancelDefaults.gameObject.SetActive(pending); // 취소 버튼 표시
            equip.gameObject.SetActive(!pending); // 일반 장착 확정은 잠시 숨김
            remove.gameObject.SetActive(!pending); // 단일 해제 미리보기는 잠시 숨김
            back.interactable = !pending; // 확인 또는 취소 전 화면 이탈 방지
            foreach (Button button in slotButtons) // 여덟 부위 선택
            {
                button.interactable = !pending; // 일괄 미리보기 중 선택 변경 방지
            }
            foreach (Button button in partButtons) // 보유 부품 행
            {
                button.interactable = !pending; // 일괄 미리보기 중 후보 변경 방지
            }
            previousPage.interactable = !pending && page > 0; // 기존 페이지 경계 유지
            int pages = Mathf.Max(1, (options.Count + partButtons.Length - 1) / partButtons.Length); // 현재 후보의 전체 페이지
            nextPage.interactable = !pending && page + 1 < pages; // 다음 페이지의 기존 경계 유지
            transform.Find("OpenPresets").GetComponent<Button>().interactable = !pending; // 일괄 확인 중 다른 프리셋 진입 방지
        }

        private static Button PageButton(Button template, Transform parent, string name, Vector2 position, string label) // 기존 조립 버튼과 같은 페이지 조작
        {
            Button button = Instantiate(template, parent, false); // 씬에서 사용하던 버튼 외형 복제
            button.name = name; // 실제 화면과 자동 시험 식별자
            button.onClick = new Button.ButtonClickedEvent(); // 원래 부품 선택 동작 제거
            RectTransform rect = (RectTransform)button.transform; // 페이지 버튼의 위치와 크기
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 목록 중앙 기준
            rect.anchoredPosition = position; // 아홉 후보 아래의 빈 공간
            rect.sizeDelta = new Vector2(120f, 40f); // 페이지 조작에 맞는 크기
            button.GetComponentInChildren<Text>().text = label; // 앞 또는 뒤 이동 안내
            FlightCraftedUi.FitLabel(button); // 버튼 폭에 맞는 선명한 글자
            return button; // 실제 페이지 동작을 연결할 버튼
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
            page = selected >= 0 ? selected / partButtons.Length : 0; // 현재 장착이 보이는 실제 페이지
            Refresh(); // 목록과 비교와 3D 갱신
        }

        private void ChangePage(int delta) // 같은 슬롯의 개별 부품 목록 이동
        {
            int pages = Mathf.Max(1, (options.Count + partButtons.Length - 1) / partButtons.Length); // 보유 수량에 필요한 페이지
            page = Mathf.Clamp(page + delta, 0, pages - 1); // 실제 목록 밖 이동 차단
            Refresh(); // 현재 선택을 보존하며 새 페이지 표시
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
                int index = page * partButtons.Length + i; // 전체 목록의 개별 사본 위치
                bool visible = index < options.Count; // 슬롯에 들어갈 후보가 있는 행
                partButtons[i].gameObject.SetActive(visible); // 후보 없는 행 감춤
                if (!visible) // 비어 있는 행
                {
                    continue; // 이전 이름을 다시 읽지 않음
                }
                FlightBroomEquipment.PartOption part = options[index]; // 현재 페이지의 실제 보유 부품
                string marker = part.Id == current ? " · 장착" : part.Owner >= 0 && part.Owner != pilot ? $" · {PilotNames[part.Owner]}" : ""; // 장착된 캐릭터 표시
                string copy = part.Copies > 1 ? $" #{part.Id[(part.Id.LastIndexOf(':') + 1)..]}" : ""; // 동일 종류의 보유 순번 구분
                partLabels[i].text = $"{(index == selected ? "●  " : "")}{part.Name}{copy}  {Grades[part.Grade - 1]}+{part.Upgrade}{marker}"; // 이름·등급·강화·장착
            }

            int pages = Mathf.Max(1, (options.Count + partButtons.Length - 1) / partButtons.Length); // 같은 슬롯의 전체 페이지
            bool paging = pages > 1; // 모든 후보가 한 화면에 들어가지 않는 경우
            previousPage.gameObject.SetActive(paging); // 필요한 경우에만 이전 버튼 표시
            nextPage.gameObject.SetActive(paging); // 필요한 경우에만 다음 버튼 표시
            pageLabel.gameObject.SetActive(paging); // 필요한 경우에만 현재 페이지 표시
            previousPage.interactable = page > 0; // 첫 페이지 이전 이동 방지
            nextPage.interactable = page + 1 < pages; // 마지막 페이지 이후 이동 방지
            pageLabel.text = $"{page + 1} / {pages}"; // 실제 페이지와 전체 수

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
