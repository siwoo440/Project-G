using System; // 기본 부품 미리보기 요청
using System.Collections.Generic; // 개별 부품 이름과 화면 목록
using System.Globalization; // 능력치 변화의 일관된 소수 표시
using System.Text; // 프리셋 상세 설명
using UnityEngine; // 실제 조립 화면과 UI
using UnityEngine.UI; // 버튼과 이름 입력

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightBroomPresetPanel : MonoBehaviour // 조립 화면의 캐릭터별 프리셋 관리
    {
        private static readonly string[] Stats = { "최고 속도", "가속력", "제동력", "선회", "안정성", "상승", "하강", "최대 마력", "마력 회복", "마법 공격력", "탄환 속도", "마법 사거리", "피해 감소", "충격 저항" }; // 실제 비교 항목 이름
        private static readonly string[] Units = { " m/s", " m/s²", " m/s²", "°/초", "", " m/s", " m/s", "", "/초", "", "%p", "%p", "%p", "%p" }; // 능력치별 사용자 단위
        private readonly Button[] rows = new Button[5]; // 한 캐릭터의 다섯 프리셋 행
        private readonly List<int> visible = new(); // 필터 후 실제 저장 인덱스
        private readonly List<FlightBroomEquipment.InventoryItem> inventory = new(); // 실제 부품의 이름 조회
        private FlightBroomEquipment equipment; // 현재 캐릭터의 실제 장비
        private Action refreshed; // 전체 적용 뒤 조립 목록 갱신
        private FlightCraftedSpellBook book; // 같은 장비를 사용하는 실제 마법 도감
        private FlightBroomPresetStore store; // 현재 사용자의 프리셋 저장
        private GameObject panel; // 조립 화면 위의 독립 패널
        private InputField nameInput; // 저장과 이름 변경에 쓰는 직접 입력
        private Text detail; // 선택한 부품과 마법의 실제 내용
        private Text notice; // 저장 결과와 실패 이유
        private Button filterButton; // 전체 또는 적용 가능 목록 전환
        private Button compareButton; // 부품 상세와 능력치 비교 전환
        private bool onlyAvailable; // 적용 가능한 기록만 표시
        private bool comparing; // 선택 조합의 능력치 비교 표시
        private int selected = -1; // 현재 선택한 프리셋 인덱스

        internal void Initialize(FlightBroomEquipment source, Button template, Font font, Transform root, Action previewDefaults, Action refreshWorkshop) // 실제 조립 화면의 버튼과 패널 구성
        {
            equipment = source; // 같은 조종사의 장착 참조
            refreshed = refreshWorkshop; // 적용 후 실제 조립 상태 갱신
            Button open = ButtonAt(template, root, "OpenPresets", new Vector2(0f, 340f), new Vector2(240f, 48f), "프리셋"); // 조립 화면의 프리셋 진입
            open.onClick.AddListener(Open); // 실제 저장 목록 열기
            panel = new GameObject("BroomPresets", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 입력을 가리는 프리셋 패널
            RectTransform panelRect = panel.GetComponent<RectTransform>(); // 실제 패널의 기준 크기
            panelRect.SetParent(root, false); // 조립 화면 위의 독립 자식
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f); // 조립 화면 중앙 기준
            panelRect.anchoredPosition = Vector2.zero; // 패널의 가운데 배치
            panelRect.sizeDelta = new Vector2(1440f, 800f); // 기존 조립과 같은 기준 크기
            panel.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.09f, 1f); // 어두운 불투명 배경
            Text heading = Label(panel.transform, font, "Heading", new Vector2(0f, 335f), new Vector2(850f, 50f), 34, TextAnchor.MiddleCenter); // 선명한 화면 제목
            heading.text = "빗자루 프리셋"; // 현재 작업의 이름
            Button restore = ButtonAt(template, panel.transform, "RestoreDefault", new Vector2(-590f, 335f), new Vector2(190f, 48f), "기본 복원"); // 기본 부품 전체 미리보기
            restore.onClick.AddListener(() => { panel.SetActive(false); previewDefaults(); }); // 기존 조립 화면에서 확인하거나 취소
            Button close = ButtonAt(template, panel.transform, "Close", new Vector2(590f, 335f), new Vector2(190f, 48f), "돌아가기"); // 조립 화면 복귀
            close.onClick.AddListener(() => panel.SetActive(false)); // 프리셋 화면만 닫기
            for (int i = 0; i < rows.Length; i++) // 캐릭터별 최대 다섯 기록
            {
                int index = i; // 각 버튼의 고정 저장 위치
                rows[i] = ButtonAt(template, panel.transform, "Row" + i, new Vector2(-350f, 200f - i * 80f), new Vector2(540f, 58f), string.Empty); // 실제 프리셋 선택 행
                rows[i].onClick.AddListener(() => Select(index)); // 선택한 저장 기록의 상세 표시
            }
            filterButton = ButtonAt(template, panel.transform, "Filter", new Vector2(-350f, 270f), new Vector2(540f, 48f), "전체 보기"); // 적용 가능 상태별 목록
            filterButton.onClick.AddListener(ToggleFilter); // 실제 사용 가능한 프리셋 필터
            nameInput = MakeInput(panel.transform, font, "Name", new Vector2(350f, 230f), new Vector2(560f, 52f), "프리셋 이름"); // 저장할 이름 직접 입력
            compareButton = ButtonAt(template, panel.transform, "Compare", new Vector2(350f, 165f), new Vector2(560f, 48f), "능력치 비교"); // 부품 상세와 전체 수치 전환
            compareButton.onClick.AddListener(ToggleCompare); // 실제 후보의 전체 능력치 비교
            detail = Label(panel.transform, font, "Detail", new Vector2(350f, -50f), new Vector2(560f, 350f), 28, TextAnchor.UpperLeft); // 부품과 마법의 실제 내용
            detail.resizeTextForBestFit = true; // 긴 부품 이름의 잘림 방지
            detail.resizeTextMinSize = 20; // 작은 화면의 최소 글자 크기
            detail.resizeTextMaxSize = 28; // 기준 화면의 최대 글자 크기
            Button save = ButtonAt(template, panel.transform, "Save", new Vector2(-520f, -275f), new Vector2(230f, 52f), "현재 조합 저장"); // 명시적 프리셋 저장
            save.onClick.AddListener(Save); // 현재 여덟 부품과 두 마법 기록
            Button apply = ButtonAt(template, panel.transform, "Apply", new Vector2(-260f, -275f), new Vector2(230f, 52f), "적용"); // 선택 조합의 전체 적용
            apply.onClick.AddListener(Apply); // 실제 충돌 확인 후 확정
            Button copy = ButtonAt(template, panel.transform, "Copy", new Vector2(0f, -275f), new Vector2(230f, 52f), "복제"); // 선택한 저장 기록 복제
            copy.onClick.AddListener(Copy); // 새로운 개별 프리셋 생성
            Button rename = ButtonAt(template, panel.transform, "Rename", new Vector2(260f, -275f), new Vector2(230f, 52f), "이름 변경"); // 선택 기록의 이름 수정
            rename.onClick.AddListener(Rename); // 입력한 이름만 저장
            Button remove = ButtonAt(template, panel.transform, "Delete", new Vector2(520f, -275f), new Vector2(230f, 52f), "삭제"); // 선택한 프리셋 기록 삭제
            remove.onClick.AddListener(Remove); // 부품과 마법을 보존하는 기록 삭제
            notice = Label(panel.transform, font, "Notice", new Vector2(0f, -350f), new Vector2(1280f, 48f), 28, TextAnchor.MiddleCenter); // 실제 성공과 실패 원인
            notice.resizeTextForBestFit = true; // 긴 충돌 안내의 잘림 방지
            notice.resizeTextMinSize = 20; // 안내의 최소 가독 크기
            notice.resizeTextMaxSize = 28; // 안내의 기준 글자 크기
            panel.SetActive(false); // 조립 진입 시 프리셋은 숨김
        }

        private void Open() // 현재 캐릭터의 실제 프리셋 열기
        {
            book = null; // 이전 씬이나 캐릭터 참조 제거
            foreach (FlightCraftedSpellBook candidate in UnityEngine.Object.FindObjectsByType<FlightCraftedSpellBook>(FindObjectsInactive.Include, FindObjectsSortMode.None)) // 현재 씬의 실제 도감
            {
                if (candidate.UsesEquipment(equipment)) // 같은 조립 부품을 사용하는 도감
                {
                    book = candidate; // 다른 씬의 도감과 구분
                    break; // 정확한 연결 한 개 사용
                }
            }
            store = new FlightBroomPresetStore(); // 다른 화면의 최근 저장을 다시 읽기
            selected = -1; // 이전 선택을 다른 캐릭터에 적용하지 않음
            onlyAvailable = false; // 처음에는 전체 기록 표시
            comparing = false; // 처음에는 부품과 마법 상세 표시
            nameInput.text = string.Empty; // 새 캐릭터의 이름 입력 초기화
            panel.SetActive(true); // 실제 프리셋 화면 표시
            panel.transform.SetAsLastSibling(); // 다른 조립 요소보다 앞에 배치
            notice.text = book == null ? "현재 마법 편성을 찾을 수 없습니다." : string.Empty; // 연결 실패를 사용자에게 안내
            Render(); // 실제 저장 목록과 빈 선택 표시
        }

        private void ToggleFilter() // 전체 또는 실제 적용 가능한 저장 기록 표시
        {
            onlyAvailable = !onlyAvailable; // 현재 필터 상태 전환
            Render(); // 조건을 다시 검사한 목록 표시
        }

        private void ToggleCompare() // 선택 조합의 전체 능력치 차이 표시
        {
            comparing = !comparing; // 부품 상세와 비교 모드 전환
            Render(); // 현재 선택의 계산 결과 표시
        }

        private void Select(int row) // 현재 화면에서 고른 실제 프리셋 선택
        {
            if (row < 0 || row >= visible.Count) // 필터 후 비어 있는 행
            {
                return; // 기존 선택 보존
            }
            int index = visible[row]; // 필터 이전의 실제 저장 위치
            FlightBroomPresetStore.Preset item = store.At((int)equipment.ActivePilot, index); // 표시 중인 독립 기록
            if (item == null) // 삭제되거나 비어 있는 행
            {
                return; // 기존 선택 보존
            }
            selected = index; // 실제 저장 위치 선택
            nameInput.text = item.name; // 이름 변경에 현재 이름 표시
            notice.text = string.Empty; // 이전 동작 안내 초기화
            Render(); // 선택 상태와 실제 상세 갱신
        }

        private void Save() // 사용자가 입력한 이름으로 현재 조합 저장
        {
            bool saved = book != null && store.SaveCurrent(equipment, book, nameInput.text); // 실제 부품·마법·우선순위 저장
            notice.text = saved ? "현재 조합을 저장했습니다." : "이름과 프리셋 5개 제한을 확인하세요."; // 실제 저장 결과
            if (saved) // 새로운 기록의 저장 성공
            {
                selected = store.Count((int)equipment.ActivePilot) - 1; // 방금 저장한 기록 선택
            }
            Render(); // 저장 목록과 상세 갱신
        }

        private void Apply() // 선택한 기록의 사전 검증과 전체 적용
        {
            string reason = null; // 실패 시 사용자에게 표시할 사유
            bool applied = book != null && store.TryApply((int)equipment.ActivePilot, selected, equipment, book, out reason); // 부품과 마법을 함께 검사
            notice.text = applied ? "프리셋을 적용했습니다." : reason ?? "적용할 프리셋을 선택하세요."; // 실제 적용 결과
            if (applied) // 실제 전체 장착이 바뀐 경우
            {
                refreshed?.Invoke(); // 조립 화면의 목록과 미리보기 갱신
            }
            Render(); // 같은 선택의 부품과 마법 갱신
        }

        private void Copy() // 선택한 기록의 독립 사본 생성
        {
            bool copied = store.Copy((int)equipment.ActivePilot, selected); // 다섯 개 상한을 포함한 복제
            notice.text = copied ? "프리셋을 복제했습니다." : "선택과 프리셋 5개 제한을 확인하세요."; // 실제 복제 결과
            if (copied) // 새 사본이 만들어진 경우
            {
                selected = store.Count((int)equipment.ActivePilot) - 1; // 방금 생성한 기록 선택
            }
            Render(); // 실제 저장 목록 갱신
        }

        private void Rename() // 선택한 기록만 새 이름 저장
        {
            bool renamed = store.Rename((int)equipment.ActivePilot, selected, nameInput.text); // 빈 이름과 길이 확인
            notice.text = renamed ? "이름을 변경했습니다." : "프리셋과 이름을 확인하세요."; // 실제 변경 결과
            Render(); // 새로운 이름 표시
        }

        private void Remove() // 선택한 프리셋 기록만 삭제
        {
            bool removed = store.Remove((int)equipment.ActivePilot, selected); // 실제 저장 항목 삭제
            notice.text = removed ? "프리셋을 삭제했습니다." : "삭제할 프리셋을 선택하세요."; // 실제 삭제 결과
            selected = -1; // 삭제된 항목의 이전 선택 해제
            Render(); // 나머지 프리셋 표시
        }

        private void Render() // 현재 캐릭터의 저장 목록과 선택 내용
        {
            int pilot = (int)equipment.ActivePilot; // 현재 조종사의 실제 번호
            visible.Clear(); // 이전 필터의 실제 인덱스 제거
            for (int i = 0; i < store.Count(pilot); i++) // 해당 캐릭터의 저장 기록
            {
                if (!onlyAvailable || store.CanApply(pilot, i, equipment, book, out _)) // 전체 또는 실제 적용 가능한 기록
                {
                    visible.Add(i); // 원래 저장 위치 보존
                }
            }
            filterButton.GetComponentInChildren<Text>().text = onlyAvailable ? "적용 가능만" : "전체 보기"; // 현재 목록 필터 상태
            compareButton.GetComponentInChildren<Text>().text = comparing ? "부품과 마법 보기" : "능력치 비교"; // 다음에 열 화면 안내
            for (int i = 0; i < rows.Length; i++) // 각 프리셋 행 갱신
            {
                bool shown = i < visible.Count; // 필터 후 표시할 실제 기록
                rows[i].gameObject.SetActive(shown); // 존재하지 않는 행 숨김
                if (shown) // 저장된 기록의 표시
                {
                    int index = visible[i]; // 원래 저장 위치
                    FlightBroomPresetStore.Preset row = store.At(pilot, index); // 현재 캐릭터의 실제 기록
                    rows[i].GetComponentInChildren<Text>().text = (index == selected ? "●  " : string.Empty) + row.name; // 선택 표시와 사용자 이름
                }
            }
            FlightBroomPresetStore.Preset item = store.At(pilot, selected); // 현재 선택한 실제 기록
            if (item == null) // 아직 선택하지 않은 경우
            {
                detail.text = "프리셋을 선택하거나 현재 조합을 저장하세요."; // 다음 행동 안내
                return; // 다른 캐릭터의 기록 접근 방지
            }
            if (comparing) // 현재 조합과 선택한 프리셋의 전체 능력치
            {
                var differences = new float[Stats.Length]; // 현재 조합과 후보의 능력치 차이
                if (!equipment.ComparePreset(pilot, item.parts, differences, out string reason)) // 누락·충돌 부품의 비교 차단
                {
                    detail.text = "능력치 비교 불가\n" + reason; // 실제 비교 실패 사유
                    return; // 확인할 수 없는 수치 표시 금지
                }
                var comparison = new StringBuilder("능력치 변화\n"); // 현재 조합 대비 차이의 제목
                bool changed = false; // 실제 달라지는 능력치 여부
                for (int i = 0; i < differences.Length; i++) // 열네 가지 실제 능력치
                {
                    if (Mathf.Abs(differences[i]) > 0.001f) // 실제로 달라지는 수치
                    {
                        comparison.AppendLine(Stats[i] + "  " + differences[i].ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) + Units[i]); // 증감 부호와 원래 단위
                        changed = true; // 표시한 변경 기록
                    }
                }
                detail.text = changed ? comparison.ToString() : "능력치 변화 없음"; // 실제 비교 결과 또는 동일 안내
                return; // 부품 상세와 비교를 겹쳐 표시하지 않음
            }
            equipment.ListInventory(inventory); // 현재 보유한 개별 부품 이름
            var text = new StringBuilder(); // 실제 저장 내용을 표시할 설명
            for (int i = 0; i < item.parts.Length; i++) // 저장된 여덟 부위
            {
                string partName = string.IsNullOrEmpty(item.parts[i]) ? "비어 있음" : "보유하지 않은 부품"; // 빈 슬롯과 누락된 사본 구분
                foreach (FlightBroomEquipment.InventoryItem part in inventory) // 현재 실제 보유 목록
                {
                    if (part.Id == item.parts[i]) // 정확히 같은 개별 사본
                    {
                        partName = part.Definition.displayName + " " + FlightBroomCatalog.GradeName(part.Grade) + "+" + part.Upgrade; // 사용자에게 보일 이름과 등급
                        break; // 같은 사본 한 개만 사용
                    }
                }
                text.AppendLine(partName); // 저장된 부위의 실제 상태
            }
            for (int i = 0; i < item.recipes.Length; i++) // 저장된 두 마법
            {
                string magic = CraftedSpellRules.TryRead(item.recipes[i], out CraftedSpellRecipe recipe) ? FlightCraftedSpellBook.NameOf(recipe) : "빈 마법"; // 유효한 마법 이름 또는 빈 슬롯
                text.AppendLine((i == 0 ? "좌 " : "우 ") + magic + " · 유지 " + (item.priorities[i] == 2 ? "높음" : item.priorities[i] == 1 ? "보통" : "낮음")); // 슬롯과 유지 우선순위
            }
            detail.text = text.ToString(); // 실제 선택 내용 반영
        }

        private static Text Label(Transform parent, Font font, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment) // 프로젝트 한국어 글꼴의 실제 문구
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 텍스트와 화면 영역
            RectTransform rect = item.GetComponent<RectTransform>(); // 실제 기준 위치
            rect.SetParent(parent, false); // 해당 패널의 자식
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 패널 중앙 기준
            rect.anchoredPosition = position; // 실제 화면 위치
            rect.sizeDelta = size; // 잘림을 확인할 표시 영역
            Text label = item.GetComponent<Text>(); // 실제 한글 텍스트
            label.font = font; // 기존 화면의 한글 글꼴
            label.fontSize = fontSize; // 기준 해상도에서의 글자 크기
            label.alignment = alignment; // 제목과 상세의 정렬
            label.color = Color.white; // 어두운 배경의 선명한 글자
            label.raycastTarget = false; // 뒤쪽 버튼 입력 방해 금지
            return label; // 내용을 갱신할 문구
        }

        private static InputField MakeInput(Transform parent, Font font, string name, Vector2 position, Vector2 size, string hint) // 이름을 직접 입력할 실제 필드
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)); // 배경과 실제 키 입력
            RectTransform rect = item.GetComponent<RectTransform>(); // 입력 영역 위치
            rect.SetParent(parent, false); // 프리셋 패널의 자식
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 패널 중앙 기준
            rect.anchoredPosition = position; // 실제 입력 위치
            rect.sizeDelta = size; // 이름이 보이는 영역
            item.GetComponent<Image>().color = new Color(0.08f, 0.13f, 0.2f, 1f); // 밝은 글자와 구분되는 배경
            Text text = Label(item.transform, font, "Text", Vector2.zero, size - new Vector2(20f, 4f), 30, TextAnchor.MiddleLeft); // 실제 입력 글자
            Text placeholder = Label(item.transform, font, "Placeholder", Vector2.zero, size - new Vector2(20f, 4f), 30, TextAnchor.MiddleLeft); // 빈 입력의 안내
            placeholder.color = new Color(0.7f, 0.75f, 0.8f, 1f); // 입력 내용과 구분되는 밝기
            placeholder.text = hint; // 사용자에게 보일 입력 설명
            InputField input = item.GetComponent<InputField>(); // 실제 텍스트 입력 컴포넌트
            input.textComponent = text; // 입력한 글자의 표시
            input.placeholder = placeholder; // 빈 상태의 안내
            input.characterLimit = 32; // 저장 가능한 이름 길이와 일치
            return input; // 사용자 입력을 읽을 필드
        }

        private static Button ButtonAt(Button template, Transform parent, string name, Vector2 position, Vector2 size, string caption) // 기존 조립 디자인을 쓰는 실제 버튼
        {
            Button button = Instantiate(template, parent, false); // 기존 테두리와 한글 글꼴 재사용
            button.name = name; // 실제 화면과 시험의 식별자
            button.onClick = new Button.ButtonClickedEvent(); // 복제한 원래 동작 제거
            RectTransform rect = (RectTransform)button.transform; // 실제 버튼 영역
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 패널 중앙 기준
            rect.anchoredPosition = position; // 클릭할 실제 위치
            rect.sizeDelta = size; // 글자와 클릭 영역 크기
            Text label = button.GetComponentInChildren<Text>(); // 기존 버튼 글자
            label.name = "Label"; // 사용자 이름을 표시하는 실제 문구
            label.text = caption; // 화면에 보일 작업 이름
            FlightCraftedUi.FitLabel(button); // 길이에 맞는 선명한 글자 크기
            label.fontSize = 32; // 작은 해상도에서도 읽기 쉬운 기준 크기
            label.resizeTextMinSize = 22; // 긴 이름의 최소 가독 크기
            label.resizeTextMaxSize = 32; // 짧은 버튼 문구의 최대 크기
            return button; // 실제 동작을 연결할 버튼
        }
    }
}
