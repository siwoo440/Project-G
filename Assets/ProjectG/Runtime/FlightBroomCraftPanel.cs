using System; // 공방 복귀 콜백
using System.Collections.Generic; // 제작법과 개별 보유 목록
using UnityEngine; // 실제 공방 화면
using UnityEngine.UI; // 버튼과 한글 텍스트

namespace ProjectG // 프로젝트 빗자루 공방 영역
{
    internal sealed class FlightBroomCraftPanel : MonoBehaviour // 제작법과 개별 분해의 실제 화면
    {
        private const int PageSize = 8; // 한 번에 표시할 목록 수
        private readonly List<FlightBroomPart> recipes = new(); // 실제 해금한 세트 종류
        private readonly List<FlightBroomEquipment.InventoryItem> items = new(); // 실제 공용 보유 사본
        private readonly Button[] rows = new Button[PageSize]; // 현재 페이지의 선택 버튼
        private readonly Button[] grades = new Button[5]; // 일반부터 전설까지의 선택 버튼
        private FlightBroomEquipment equipment; // 공방과 전투가 공유하는 장비
        private GameObject panel; // 마을 시설 위의 제작 화면
        private GameObject confirm; // 실제 제작·분해의 별도 확인 창
        private Text detail; // 선택한 종류 또는 사본 설명
        private Text wallet; // 실제 보유 재화
        private Text pageText; // 현재 목록의 페이지
        private Text notice; // 성공 또는 실패 안내
        private Text confirmText; // 확인할 실제 부품과 비용
        private Button craft; // 선택한 등급의 제작
        private Button dismantle; // 미장착 사본의 분해
        private Button lockButton; // 개별 사본 잠금 변경
        private Action closed; // 시설 재화 표시 갱신
        private bool inventory; // 제작법 또는 보유 목록 구분
        private bool pendingDismantle; // 확인 중인 실제 작업
        private int selected = -1; // 전체 목록에서 고른 위치
        private int page; // 현재 목록 페이지
        private int grade = 1; // 제작할 희귀도

        internal void Initialize(FlightBroomEquipment source, Button template, Font font, Transform overlay, Action refresh) // 실제 시설과 화면 연결
        {
            equipment = source; // 동일 프로필의 장비 참조
            closed = refresh; // 시설로 돌아갈 때 재화 갱신
            Button open = CloneButton(template, transform, "OpenBroomCraft", new Vector2(0f, -189f), new Vector2(205f, 42f), "부품 제작 · 분해"); // 공방에서 제작 화면 진입
            open.onClick.AddListener(Open); // 실제 제작법 화면 열기
            panel = Panel(overlay, "BroomCraftPanel", new Color(0.025f, 0.055f, 0.09f, 1f)); // 기존 시설 위의 독립 화면
            Text heading = Label(panel.transform, font, "Heading", new Vector2(0f, 340f), new Vector2(850f, 55f), 34, TextAnchor.MiddleCenter); // 실제 공방 화면 제목
            heading.text = "빗자루 부품 제작 · 분해"; // 사용자에게 보이는 작업 이름
            wallet = Label(panel.transform, font, "Wallet", new Vector2(0f, 302f), new Vector2(1050f, 38f), 22, TextAnchor.MiddleCenter); // 공용 결정과 골드
            Button close = CloneButton(template, panel.transform, "Close", new Vector2(590f, 340f), new Vector2(190f, 48f), "돌아가기"); // 기존 공방으로 복귀
            close.onClick.AddListener(Hide); // 확인을 취소하고 시설 화면 표시
            Button recipesTab = CloneButton(template, panel.transform, "RecipeTab", new Vector2(-510f, 245f), new Vector2(260f, 48f), "제작법"); // 획득한 부품의 제작법
            recipesTab.onClick.AddListener(() => SelectMode(false)); // 제작 목록 선택
            Button inventoryTab = CloneButton(template, panel.transform, "InventoryTab", new Vector2(-225f, 245f), new Vector2(260f, 48f), "보유 · 분해"); // 개별 부품 목록
            inventoryTab.onClick.AddListener(() => SelectMode(true)); // 보유 목록 선택
            for (int i = 0; i < rows.Length; i++) // 실제 목록의 여덟 줄
            {
                int index = i; // 각 줄이 보존할 위치
                rows[i] = CloneButton(template, panel.transform, "Row" + i, new Vector2(-360f, 180f - i * 52f), new Vector2(600f, 46f), string.Empty); // 선택 가능한 목록 행
                rows[i].onClick.AddListener(() => SelectRow(index)); // 해당 페이지의 실제 항목 선택
            }
            Button previous = CloneButton(template, panel.transform, "Previous", new Vector2(-545f, -300f), new Vector2(130f, 45f), "이전"); // 앞 페이지
            previous.onClick.AddListener(() => ChangePage(-1)); // 범위 안의 이전 목록
            Button next = CloneButton(template, panel.transform, "Next", new Vector2(-175f, -300f), new Vector2(130f, 45f), "다음"); // 다음 페이지
            next.onClick.AddListener(() => ChangePage(1)); // 범위 안의 다음 목록
            pageText = Label(panel.transform, font, "Page", new Vector2(-360f, -300f), new Vector2(210f, 42f), 21, TextAnchor.MiddleCenter); // 전체 페이지와 현재 위치
            detail = Label(panel.transform, font, "Detail", new Vector2(350f, 155f), new Vector2(570f, 150f), 24, TextAnchor.UpperLeft); // 부품 이름·상태·가격
            detail.resizeTextForBestFit = true; // 긴 한국어 이름의 잘림 방지
            detail.resizeTextMinSize = 16; // 작은 화면의 최소 글자
            detail.resizeTextMaxSize = 24; // 기준 화면의 최대 글자
            for (int i = 0; i < grades.Length; i++) // 기획된 다섯 희귀도 버튼
            {
                int value = i + 1; // 버튼별 실제 등급
                grades[i] = CloneButton(template, panel.transform, "Grade" + value, new Vector2(100f + i * 125f, 45f), new Vector2(115f, 44f), FlightBroomCatalog.GradeName(value)); // 선택할 등급 이름
                grades[i].onClick.AddListener(() => SelectGrade(value)); // 유효한 제작 등급 선택
            }
            craft = CloneButton(template, panel.transform, "Craft", new Vector2(350f, -30f), new Vector2(450f, 48f), "제작하기"); // 선택한 부품 확정 제작
            craft.onClick.AddListener(() => Offer(false)); // 실제 비용의 확인 창
            lockButton = CloneButton(template, panel.transform, "Lock", new Vector2(350f, -95f), new Vector2(450f, 48f), "잠금"); // 개별 보유의 분해 잠금
            lockButton.onClick.AddListener(ToggleLock); // 실제 저장된 잠금 변경
            dismantle = CloneButton(template, panel.transform, "Dismantle", new Vector2(350f, -155f), new Vector2(450f, 48f), "분해하기"); // 선택한 사본 분해
            dismantle.onClick.AddListener(() => Offer(true)); // 실제 반환량의 확인 창
            notice = Label(panel.transform, font, "Notice", new Vector2(0f, -360f), new Vector2(1250f, 42f), 22, TextAnchor.MiddleCenter); // 저장 성공과 거절 이유
            confirm = Panel(panel.transform, "Confirm", new Color(0.025f, 0.04f, 0.075f, 1f)); // 다른 버튼을 차단할 확인 화면
            confirmText = Label(confirm.transform, font, "Message", new Vector2(0f, 55f), new Vector2(800f, 150f), 27, TextAnchor.MiddleCenter); // 확인할 대상과 실제 재화
            Button accept = CloneButton(template, confirm.transform, "Accept", new Vector2(180f, -80f), new Vector2(230f, 55f), "확인"); // 사용자의 최종 승인
            accept.onClick.AddListener(Apply); // 최신 상태를 재검사하고 저장
            Button cancel = CloneButton(template, confirm.transform, "Cancel", new Vector2(-180f, -80f), new Vector2(230f, 55f), "취소"); // 거래를 실행하지 않는 취소
            cancel.onClick.AddListener(() => confirm.SetActive(false)); // 이전 화면으로 복귀
            confirm.SetActive(false); // 초기 확인 창 숨김
            panel.SetActive(false); // 처음에는 기존 공방 표시
        }

        internal void Hide() // 원래 공방으로 복귀
        {
            confirm?.SetActive(false); // 미확정 거래 취소
            panel?.SetActive(false); // 제작 화면 숨김
            closed?.Invoke(); // 현재 재화와 강화 상태 갱신
        }

        private void Open() // 실제 공방 버튼으로 화면 열기
        {
            panel.SetActive(true); // 제작 화면 표시
            panel.transform.SetAsLastSibling(); // 다른 시설 UI보다 앞에 표시
            notice.text = string.Empty; // 이전 거래 결과 제거
            SelectMode(false); // 획득한 제작법부터 표시
        }

        private void SelectMode(bool showInventory) // 제작법과 보유 목록 전환
        {
            inventory = showInventory; // 현재 목록 종류 저장
            page = 0; // 새 목록의 첫 페이지
            selected = -1; // 이전 목록의 선택 해제
            Render(); // 실제 해금과 보유 목록 표시
        }

        private void ChangePage(int delta) // 목록 페이지 이동
        {
            int count = inventory ? items.Count : recipes.Count; // 현재 표시하는 실제 항목 수
            page = Mathf.Clamp(page + delta, 0, Mathf.Max(0, (count - 1) / PageSize)); // 유효한 페이지 범위
            selected = -1; // 이전 페이지의 선택 해제
            Render(); // 바뀐 목록과 페이지 표시
        }

        private void SelectRow(int row) // 현재 페이지의 실제 부품 선택
        {
            int index = page * PageSize + row; // 전체 목록의 실제 위치
            if (index < 0 || index >= (inventory ? items.Count : recipes.Count)) // 비어 있는 목록 행
            {
                return; // 현재 선택 유지
            }
            selected = index; // 선택한 실제 부품
            grade = 1; // 처음에는 최하위 해금 등급 선택
            Render(); // 실제 세부 상태 표시
        }

        private void SelectGrade(int value) // 제작할 해금 등급 변경
        {
            if (!inventory && selected >= 0 && selected < recipes.Count && equipment.CanCraft(recipes[selected].id, value)) // 실제로 해금된 등급
            {
                grade = value; // 사용자가 선택한 유효 등급
                Render(); // 해당 비용과 재화 표시
            }
        }

        private void ToggleLock() // 보유 사본의 분해 잠금 변경
        {
            if (!inventory || selected < 0 || selected >= items.Count) // 선택한 실제 보유 없음
            {
                return; // 다른 부품의 잠금 변경 방지
            }
            FlightBroomEquipment.InventoryItem item = items[selected]; // 화면에서 선택한 고유 사본
            bool saved = equipment.SetItemLocked(item.Id, !item.Locked); // 현재 상태를 반전해 영구 저장
            notice.text = saved ? "잠금 상태를 저장했습니다." : "잠금 상태를 저장하지 못했습니다."; // 실제 저장 결과
            Render(); // 새 잠금 상태와 분해 제한 갱신
        }

        private void Offer(bool isDismantle) // 실제 거래 전에 비용과 반환량 확인
        {
            if (selected < 0 || selected >= (inventory ? items.Count : recipes.Count) || isDismantle != inventory) // 현재 목록과 다른 요청
            {
                return; // 잘못된 부품의 거래 방지
            }
            pendingDismantle = isDismantle; // 확인할 실제 작업 기록
            string name = isDismantle ? items[selected].Definition.displayName : recipes[selected].displayName; // 선택한 실제 부품 이름
            int chosenGrade = isDismantle ? items[selected].Grade : grade; // 선택한 실제 희귀도
            (int crystals, int gold) = FlightProgressStore.BroomCraftPrice(chosenGrade); // 해당 등급의 기획 제작 비용
            confirmText.text = isDismantle ? $"{name} · {FlightBroomCatalog.GradeName(chosenGrade)}\n분해 반환: 결정 {crystals / 2} · 골드 {gold / 2:N0}" : $"{name} · {FlightBroomCatalog.GradeName(chosenGrade)}\n제작 비용: 결정 {crystals} · 골드 {gold:N0}"; // 실제 차감 또는 절반 반환 안내
            confirm.SetActive(true); // 사용자의 최종 확인 대기
            confirm.transform.SetAsLastSibling(); // 목록보다 위에서 입력 차단
        }

        private void Apply() // 확인한 부품을 최신 상태로 다시 검사
        {
            if (selected < 0 || selected >= (inventory ? items.Count : recipes.Count) || pendingDismantle != inventory) // 선택이 더 이상 유효하지 않은 경우
            {
                confirm.SetActive(false); // 확정하지 않고 확인 종료
                return; // 다른 부품의 변경 방지
            }
            bool saved = inventory ? equipment.DismantleItem(items[selected].Id) : equipment.CraftItem(recipes[selected].id, grade) != null; // 실제 장착·잠금·비용과 원자 저장
            confirm.SetActive(false); // 성공과 실패 모두 확인 종료
            notice.text = saved ? (inventory ? "부품을 분해했습니다." : "부품을 제작했습니다.") : "처리하지 못했습니다. 보유 상태와 재화를 확인하세요."; // 실제 저장 결과만 안내
            selected = -1; // 삭제 또는 추가 뒤 이전 선택 제거
            Render(); // 재화와 개별 인벤토리 다시 표시
        }

        private void Render() // 현재 저장에서 목록과 가능한 작업 다시 계산
        {
            equipment.ListCraftable(recipes); // 현재 해금된 제작법 전체
            equipment.ListInventory(items); // 현재 보유한 개별 사본 전체
            int count = inventory ? items.Count : recipes.Count; // 현재 목록의 실제 수량
            int pages = Mathf.Max(1, (count + PageSize - 1) / PageSize); // 빈 목록을 포함한 페이지 수
            page = Mathf.Clamp(page, 0, pages - 1); // 삭제 후 유효한 페이지 유지
            if (selected >= count) // 삭제로 사라진 선택
            {
                selected = -1; // 잘못된 인덱스 제거
            }
            wallet.text = $"천공 결정 {FlightProgress.Current.SharedCrystals:N0}   ·   골드 {FlightProgress.Current.Gold:N0}"; // 현재 저장의 실제 재화
            pageText.text = $"{page + 1} / {pages}"; // 현재 페이지와 전체 페이지
            for (int i = 0; i < rows.Length; i++) // 실제 목록 행 갱신
            {
                int index = page * PageSize + i; // 전체 목록에서의 위치
                rows[i].gameObject.SetActive(index < count); // 존재하지 않는 행 숨김
                if (index < count) // 실제 항목이 있는 행
                {
                    string name = inventory ? items[index].Definition.displayName + " · " + FlightBroomCatalog.GradeName(items[index].Grade) + "+" + items[index].Upgrade : recipes[index].displayName; // 실제 종류와 개별 등급
                    rows[i].GetComponentInChildren<Text>().text = (index == selected ? "●  " : string.Empty) + name; // 선택 상태와 항목 이름
                }
            }
            bool chosen = selected >= 0 && selected < count; // 현재 목록의 유효한 선택
            for (int i = 0; i < grades.Length; i++) // 제작 등급의 가용 상태
            {
                grades[i].gameObject.SetActive(!inventory); // 보유 목록에서는 등급 선택 숨김
                grades[i].interactable = !inventory && chosen && equipment.CanCraft(recipes[selected].id, i + 1); // 실제 획득 등급 이하만 선택
            }
            craft.gameObject.SetActive(!inventory); // 제작 화면의 실제 버튼
            lockButton.gameObject.SetActive(inventory); // 보유 화면의 실제 잠금 버튼
            dismantle.gameObject.SetActive(inventory); // 보유 화면의 실제 분해 버튼
            if (!chosen) // 아직 종류나 사본을 고르지 않은 경우
            {
                detail.text = inventory ? "보유 부품을 선택하세요." : "획득한 부품의 제작법을 선택하세요."; // 다음 행동 안내
                craft.interactable = lockButton.interactable = dismantle.interactable = false; // 임의 거래 차단
                return; // 다른 목록의 항목 접근 방지
            }
            if (!inventory) // 해금한 세트 종류의 제작 정보
            {
                FlightBroomPart recipe = recipes[selected]; // 현재 선택한 실제 종류
                (int crystals, int gold) = FlightProgressStore.BroomCraftPrice(grade); // 선택한 등급의 실제 가격
                detail.text = $"{recipe.displayName}\n{FlightBroomCatalog.GradeName(grade)} 제작 비용\n결정 {crystals} · 골드 {gold:N0}"; // 선택과 비용 안내
                craft.interactable = equipment.CanCraft(recipe.id, grade) && FlightProgress.Current.SharedCrystals >= crystals && FlightProgress.Current.Gold >= gold; // 해금과 실제 재화 검증
                return; // 보유 사본 화면을 처리하지 않음
            }
            FlightBroomEquipment.InventoryItem item = items[selected]; // 선택한 실제 개별 사본
            bool supported = !item.Definition.starter && (!string.IsNullOrEmpty(item.Definition.setId) || item.Definition.stocked); // 세트·상점 부품과 캐릭터 시작 부품 구분
            string state = item.Owner >= 0 ? "장착 중" : item.Locked ? "잠금 중" : supported ? "분해 가능" : "분해 대상 아님"; // 실제 장착과 보호 상태
            (int returnedCrystals, int returnedGold) = FlightProgressStore.BroomCraftPrice(item.Grade); // 획득 경로와 무관한 기준 가격
            detail.text = $"{item.Definition.displayName}\n{FlightBroomCatalog.GradeName(item.Grade)} +{item.Upgrade} · {state}\n분해 반환: 결정 {returnedCrystals / 2} · 골드 {returnedGold / 2:N0}"; // 개별 상태와 반환량
            lockButton.interactable = supported; // 보호할 수 있는 세트 사본만 잠금
            lockButton.GetComponentInChildren<Text>().text = item.Locked ? "잠금 해제" : "잠금"; // 현재 사본의 다음 동작
            dismantle.interactable = supported && !item.Locked && item.Owner < 0; // 시작·잠금·모든 캐릭터의 장착 보호
        }

        private static GameObject Panel(Transform parent, string name, Color color) // 입력을 가리는 실제 불투명 화면
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 배경과 UI 영역
            RectTransform rect = item.GetComponent<RectTransform>(); // 화면의 실제 크기
            rect.SetParent(parent, false); // 기존 시설 위의 자식 화면
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중앙 기준
            rect.anchoredPosition = Vector2.zero; // 원래 시설의 중앙
            rect.sizeDelta = new Vector2(1440f, 800f); // 기존 연구 화면과 같은 기준 크기
            item.GetComponent<Image>().color = color; // 글자와 대비되는 배경
            return item; // 생성한 화면
        }

        private static Text Label(Transform parent, Font font, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment) // 프로젝트의 한국어 글꼴을 쓰는 문구
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 실제 표시할 텍스트
            RectTransform rect = item.GetComponent<RectTransform>(); // 문구의 기준 위치
            rect.SetParent(parent, false); // 해당 공방 화면 안에 배치
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중앙 기준
            rect.anchoredPosition = position; // 실제 위치
            rect.sizeDelta = size; // 실제 표시 영역
            Text label = item.GetComponent<Text>(); // 사용자에게 보일 문구
            label.font = font; // 기존 한글 글꼴 공유
            label.fontSize = fontSize; // 기준 해상도의 글자 크기
            label.alignment = alignment; // 목록과 제목의 정렬
            label.color = Color.white; // 어두운 배경의 선명한 글자
            label.raycastTarget = false; // 버튼 입력 가림 방지
            return label; // 내용을 갱신할 실제 텍스트
        }

        private static Button CloneButton(Button template, Transform parent, string name, Vector2 position, Vector2 size, string label) // 기존 시설 디자인의 실제 버튼
        {
            Button button = Instantiate(template, parent, false); // 기존 버튼의 이미지와 글꼴 재사용
            button.name = name; // 실제 화면과 자동 시험의 식별자
            button.onClick = new Button.ButtonClickedEvent(); // 원본의 거래 동작 제거
            RectTransform rect = (RectTransform)button.transform; // 실제 버튼의 위치
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중앙 기준
            rect.anchoredPosition = position; // 기준 화면의 버튼 위치
            rect.sizeDelta = size; // 이름과 테두리의 실제 크기
            button.GetComponentInChildren<Text>().text = label; // 사용자에게 보일 작업 이름
            FlightCraftedUi.FitLabel(button); // 긴 한국어 이름의 크기 조정
            return button; // 실제 동작을 연결할 버튼
        }
    }
}
