using System; // 편집 화면 복귀 동작
using System.Collections.Generic; // 검색한 실제 도감 목록
using UnityEngine; // UI 개체와 위치
using UnityEngine.UI; // 버튼과 검색 입력

namespace ProjectG // 프로젝트 제작 마법 영역
{
    internal sealed class FlightCraftedGalleryPanel : MonoBehaviour // 저장한 마법의 검색과 관리
    {
        private const int PageSize = 7; // 한 화면의 도감 카드 수
        private static readonly string[] Elements = { "전체", "화염", "물", "풀", "빛", "어둠" }; // 실제 속성과 전체 선택
        private readonly List<int> matches = new(); // 검색 조건에 맞는 실제 저장 순번
        private readonly Button[] rows = new Button[PageSize]; // 화면에 보이는 카드
        private FlightCraftedSpellBook book; // 실전 편성과 공유하는 도감 저장
        private Action<CraftedSpellRecipe> load; // 선택한 구성을 편집기로 전달
        private Action close; // 편집 상태 갱신
        private GameObject panel; // 실제 도감 화면
        private GameObject confirmation; // 삭제 전 확인 화면
        private InputField search; // 마법 이름 검색
        private InputField rename; // 선택한 카드의 새 이름
        private Text detail; // 선택한 카드의 구성 정보
        private Text notice; // 실제 저장 결과와 참조 안내
        private Text pageLabel; // 현재 검색 결과의 페이지
        private Text elementLabel; // 현재 속성 필터
        private Text costLabel; // 현재 비용 필터
        private Button loadButton; // 편집기로 불러오기
        private Button duplicateButton; // 선택한 카드를 복제
        private Button renameButton; // 선택한 카드의 이름 변경
        private Button deleteButton; // 삭제 확인 열기
        private int selected = -1; // 실제 도감에서 선택한 순번
        private int page; // 현재 검색 페이지
        private int elementFilter; // 전체 또는 특정 속성
        private int costFilter; // 전체 또는 비용 범위

        internal void Initialize(Button template, Font font, FlightCraftedSpellBook source, Action<CraftedSpellRecipe> onLoad, Action onClose) // 제작 화면과 실제 저장 연결
        {
            book = source; // 같은 편성과 영구 저장 사용
            load = onLoad; // 도감 선택의 편집 화면 복귀
            close = onClose; // 닫은 뒤 상태 표시 갱신
            Button open = MakeButton(template, transform, "OpenGallery", new Vector2(-223f, 335f), new Vector2(205f, 49f), "마법 도감"); // 제작 화면의 도감 진입
            open.onClick.AddListener(Open); // 현재 저장 목록 표시
            panel = MakePanel(transform, "GalleryPanel", new Vector2(1440f, 800f)); // 제작 화면 위의 전체 도감
            Label(panel.transform, font, "Heading", new Vector2(0f, 340f), new Vector2(700f, 55f), 34).text = "마법 도감"; // 실제 화면 제목
            Button back = MakeButton(template, panel.transform, "Close", new Vector2(590f, 340f), new Vector2(190f, 48f), "돌아가기"); // 편집 화면 복귀
            back.onClick.AddListener(Hide); // 저장 상태를 유지하고 닫기
            search = MakeInput(panel.transform, font, "Search", new Vector2(-400f, 260f), new Vector2(440f, 48f), "이름 검색"); // 저장 이름 검색 입력
            search.onValueChanged.AddListener(_ => ResetFilter()); // 입력 즉시 검색 결과 갱신
            Button element = MakeButton(template, panel.transform, "ElementFilter", new Vector2(-25f, 260f), new Vector2(220f, 48f), "속성 전체"); // 속성별 필터
            elementLabel = element.GetComponentInChildren<Text>(); // 현재 속성 필터 표시
            element.onClick.AddListener(() => { elementFilter = (elementFilter + 1) % Elements.Length; ResetFilter(); }); // 다음 속성으로 전환
            Button cost = MakeButton(template, panel.transform, "CostFilter", new Vector2(230f, 260f), new Vector2(220f, 48f), "비용 전체"); // 편성 비용별 필터
            costLabel = cost.GetComponentInChildren<Text>(); // 현재 비용 필터 표시
            cost.onClick.AddListener(() => { costFilter = (costFilter + 1) % 4; ResetFilter(); }); // 다음 비용 범위로 전환
            for (int i = 0; i < rows.Length; i++) // 현재 페이지에 표시할 카드
            {
                int index = i; // 각 버튼의 고정 줄 번호
                rows[i] = MakeButton(template, panel.transform, "Entry" + i, new Vector2(-300f, 180f - i * 58f), new Vector2(730f, 50f), string.Empty); // 이름과 비용이 보이는 카드
                rows[i].onClick.AddListener(() => Select(index)); // 실제 도감 항목 선택
            }
            Button previous = MakeButton(template, panel.transform, "Previous", new Vector2(-520f, -285f), new Vector2(135f, 45f), "이전"); // 앞 페이지
            previous.onClick.AddListener(() => ChangePage(-1)); // 실제 검색 결과의 앞쪽
            Button next = MakeButton(template, panel.transform, "Next", new Vector2(-80f, -285f), new Vector2(135f, 45f), "다음"); // 다음 페이지
            next.onClick.AddListener(() => ChangePage(1)); // 실제 검색 결과의 뒤쪽
            pageLabel = Label(panel.transform, font, "Page", new Vector2(-300f, -285f), new Vector2(260f, 45f), 22); // 현재 페이지 표시
            detail = Label(panel.transform, font, "Detail", new Vector2(405f, 165f), new Vector2(500f, 130f), 23); // 선택한 마법의 실제 정보
            rename = MakeInput(panel.transform, font, "Name", new Vector2(405f, 75f), new Vector2(450f, 48f), "새 마법 이름"); // 이름 변경 입력
            loadButton = MakeButton(template, panel.transform, "Load", new Vector2(405f, 5f), new Vector2(450f, 48f), "불러오기"); // 현재 마법을 편집기로 불러오기
            loadButton.onClick.AddListener(LoadSelected); // 선택한 유효 조합 복원
            duplicateButton = MakeButton(template, panel.transform, "Duplicate", new Vector2(405f, -52f), new Vector2(450f, 48f), "복제"); // 같은 구성의 새 카드
            duplicateButton.onClick.AddListener(Duplicate); // 영구 도감에 독립 항목 추가
            renameButton = MakeButton(template, panel.transform, "Rename", new Vector2(405f, -109f), new Vector2(450f, 48f), "이름 변경"); // 이 카드의 이름만 변경
            renameButton.onClick.AddListener(Rename); // 입력한 이름 검증과 저장
            deleteButton = MakeButton(template, panel.transform, "Delete", new Vector2(405f, -166f), new Vector2(450f, 48f), "삭제"); // 저장 카드 삭제 확인
            deleteButton.onClick.AddListener(() => confirmation.SetActive(true)); // 실제 삭제 전 확인
            notice = Label(panel.transform, font, "Notice", new Vector2(0f, -360f), new Vector2(1250f, 44f), 22); // 성공·실패와 편성 참조 안내
            confirmation = MakePanel(panel.transform, "Confirm", new Vector2(1440f, 800f)); // 실수로 다른 버튼을 누르지 않는 확인 화면
            Label(confirmation.transform, font, "Message", new Vector2(0f, 55f), new Vector2(850f, 100f), 27).text = "선택한 저장 마법을 삭제할까요?\n편성 중인 구성은 먼저 해제해야 합니다."; // 참조 보호를 포함한 삭제 안내
            Button accept = MakeButton(template, confirmation.transform, "Accept", new Vector2(180f, -80f), new Vector2(230f, 55f), "삭제"); // 사용자 삭제 확정
            accept.onClick.AddListener(Delete); // 편성 참조를 다시 검사한 삭제
            Button cancel = MakeButton(template, confirmation.transform, "Cancel", new Vector2(-180f, -80f), new Vector2(230f, 55f), "취소"); // 목록을 변경하지 않는 취소
            cancel.onClick.AddListener(() => confirmation.SetActive(false)); // 삭제 없이 이전 화면
            confirmation.SetActive(false); // 최초에는 삭제 확인 숨김
            panel.SetActive(false); // 제작 화면 진입만으로 도감 열기 금지
        }

        internal void Hide() // 도감을 닫고 제작 화면 복원
        {
            confirmation.SetActive(false); // 미확정 삭제 취소
            panel.SetActive(false); // 제작 화면 표시
            close?.Invoke(); // 실제 저장과 편성 상태 갱신
        }

        private void Open() // 현재 저장과 검색 조건 초기화
        {
            panel.SetActive(true); // 실제 도감 표시
            panel.transform.SetAsLastSibling(); // 뒤쪽 제작 버튼보다 앞에 표시
            confirmation.SetActive(false); // 이전 확인 상태 제거
            search.SetTextWithoutNotify(string.Empty); // 이전 검색어 초기화
            elementFilter = 0; // 모든 속성 표시
            costFilter = 0; // 모든 비용 표시
            notice.text = string.Empty; // 이전 작업 결과 제거
            ResetFilter(); // 전체 저장 카드 표시
        }

        private void ResetFilter() // 현재 검색 조건에 맞는 첫 페이지
        {
            page = 0; // 필터 변경 시 첫 페이지
            selected = -1; // 숨겨진 이전 항목의 오조작 방지
            Render(); // 실제 도감 목록 갱신
        }

        private void Render() // 저장 카드와 필터 상태를 실제 값으로 표시
        {
            matches.Clear(); // 이전 검색 결과 제거
            string query = search.text.Trim(); // 앞뒤 공백을 제거한 검색어
            for (int i = 0; i < book.GalleryCount; i++) // 실제 저장된 모든 항목
            {
                FlightCraftedSpellBook.GalleryEntry entry = book.GalleryAt(i); // 해당 도감 카드
                bool valid = CraftedSpellRules.TryRead(entry.recipe, out CraftedSpellRecipe recipe); // 알 수 없는 저장 구성 구분
                string element = valid ? ElementName(recipe.Element) : "알 수 없음"; // 실제 속성의 한국어 이름
                if (!string.IsNullOrEmpty(query) && (entry.name ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) // 이름 검색 불일치
                {
                    continue; // 결과 목록에서 제외
                }
                if (elementFilter > 0 && element != Elements[elementFilter]) // 선택한 속성 불일치
                {
                    continue; // 다른 속성 제외
                }
                if (costFilter > 0 && (!valid || costFilter == 1 && recipe.Cost > 50 || costFilter == 2 && (recipe.Cost <= 50 || recipe.Cost > 100) || costFilter == 3 && recipe.Cost <= 100)) // 실제 구성 비용의 선택 범위
                {
                    continue; // 다른 비용과 알 수 없는 구성 제외
                }
                matches.Add(i); // 원래 저장 순번 보존
            }
            int pages = Mathf.Max(1, (matches.Count + PageSize - 1) / PageSize); // 결과가 없어도 한 페이지 표시
            page = Mathf.Clamp(page, 0, pages - 1); // 삭제 뒤 페이지 범위 보정
            elementLabel.text = "속성 " + Elements[elementFilter]; // 현재 속성 조건
            costLabel.text = costFilter == 0 ? "비용 전체" : costFilter == 1 ? "비용 50 이하" : costFilter == 2 ? "비용 51~100" : "비용 101 이상"; // 현재 비용 조건
            pageLabel.text = $"{page + 1} / {pages}  ·  {matches.Count}개"; // 실제 결과와 페이지 수
            for (int i = 0; i < rows.Length; i++) // 현재 페이지의 각 카드
            {
                int position = page * PageSize + i; // 전체 검색 결과의 위치
                bool exists = position < matches.Count; // 실제 저장 항목 여부
                rows[i].gameObject.SetActive(exists); // 빈 줄의 클릭과 표시 제외
                if (exists) // 실제 카드 내용 표시
                {
                    int index = matches[position]; // 원래 저장 목록의 위치
                    FlightCraftedSpellBook.GalleryEntry entry = book.GalleryAt(index); // 표시할 실제 저장
                    bool valid = CraftedSpellRules.TryRead(entry.recipe, out CraftedSpellRecipe recipe); // 실제 구성 파싱
                    string info = valid ? $"{ElementName(recipe.Element)}  ·  비용 {recipe.Cost}  ·  {(book.ResearchUnlocked(recipe) ? "해금" : "연구 필요")}" : "구성 확인 필요"; // 저장과 연구 상태
                    rows[i].GetComponentInChildren<Text>().text = $"{(selected == index ? "▶ " : string.Empty)}{entry.name}   ·   {info}"; // 검색 가능한 이름과 요약
                }
            }
            FlightCraftedSpellBook.GalleryEntry chosen = book.GalleryAt(selected); // 선택된 실제 항목
            bool usable = chosen != null && CraftedSpellRules.TryRead(chosen.recipe, out CraftedSpellRecipe selectedRecipe); // 읽을 수 있는 구성
            detail.text = chosen == null ? "마법을 선택하세요." : usable ? $"{chosen.name}\n같은 구성의 쿨타임과 강화 공유" : "이 저장 구성은 현재 읽을 수 없습니다."; // 실제 선택의 설명
            loadButton.interactable = usable; // 읽을 수 있는 마법만 불러오기
            duplicateButton.interactable = usable; // 읽을 수 있는 마법만 복제
            renameButton.interactable = chosen != null; // 저장 항목만 이름 변경
            deleteButton.interactable = chosen != null; // 저장 항목만 삭제 확인
        }

        private void Select(int row) // 보이는 카드의 실제 저장 순번 선택
        {
            int position = page * PageSize + row; // 검색 결과의 위치
            if (position >= matches.Count) // 빈 줄 선택 방지
            {
                return; // 기존 상태 유지
            }
            selected = matches[position]; // 실제 도감의 항목
            rename.SetTextWithoutNotify(book.GalleryAt(selected).name ?? string.Empty); // 현재 이름을 편집 입력에 복사
            notice.text = string.Empty; // 이전 작업 결과 제거
            Render(); // 선택한 항목과 동작 표시
        }

        private void ChangePage(int delta) // 실제 검색 결과의 앞뒤 이동
        {
            page += delta; // 요청한 페이지 이동
            selected = -1; // 숨겨진 항목의 오조작 방지
            Render(); // 유효 범위로 보정하여 표시
        }

        private void LoadSelected() // 현재 저장을 편집기로 복원
        {
            if (book.LoadGallery(selected)) // 실제 저장을 읽은 경우
            {
                load(book.Saved); // 편집 초안에 선택한 조합 전달
                Hide(); // 도감 닫기
            }
        }

        private void Duplicate() // 선택한 구성을 독립 카드로 복제
        {
            bool result = book.DuplicateGallery(selected); // 같은 구성의 별도 이름과 식별자 저장
            notice.text = result ? "마법을 복제했습니다." : "복제할 수 없습니다."; // 실제 저장 결과
            Render(); // 저장된 카드 수 갱신
        }

        private void Rename() // 선택한 카드의 표시 이름 변경
        {
            bool result = book.RenameGallery(selected, rename.text); // 공백과 길이를 검증한 이름 저장
            notice.text = result ? "마법 이름을 변경했습니다." : "이름을 1~32자로 입력하세요."; // 실제 결과 표시
            Render(); // 새 이름과 현재 필터 갱신
        }

        private void Delete() // 사용자가 확인한 저장 카드 삭제
        {
            confirmation.SetActive(false); // 확인 화면 종료
            bool result = book.DeleteGallery(selected); // 현재 편성 참조를 다시 확인
            notice.text = result ? "마법을 삭제했습니다." : "편성 중인 마법은 먼저 편성을 해제하세요."; // 실제 삭제 결과
            selected = -1; // 삭제 전 선택의 오조작 방지
            Render(); // 나머지 저장 카드 표시
        }

        private static string ElementName(string id) // 저장 속성의 기존 한국어 이름
        {
            foreach (CraftedSpellChoice choice in CraftedSpellCatalog.Choices(CraftedSpellGroup.Element)) // 실제 속성 목록
            {
                if (choice.Id == id) // 같은 속성 식별자
                {
                    return choice.Label; // 기존 제작 화면과 같은 이름
                }
            }
            return "알 수 없음"; // 미래 또는 손상된 저장의 보존 표시
        }

        private static GameObject MakePanel(Transform parent, string name, Vector2 size) // 다른 입력을 차단하는 어두운 화면
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 실제 UI 배경
            RectTransform rect = item.GetComponent<RectTransform>(); // 기준 위치와 크기
            rect.SetParent(parent, false); // 실제 제작 화면의 자식
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중앙 기준
            rect.anchoredPosition = Vector2.zero; // 화면 중앙 위치
            rect.sizeDelta = size; // 기준 해상도 크기
            item.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.09f, 1f); // 뒤쪽 화면이 비치지 않는 선명한 글자 배경
            return item; // 생성한 실제 화면
        }

        private static Text Label(Transform parent, Font font, string name, Vector2 position, Vector2 size, int fontSize) // 기존 한글 글꼴의 텍스트
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 실제 텍스트 개체
            RectTransform rect = item.GetComponent<RectTransform>(); // 위치와 크기
            rect.SetParent(parent, false); // 지정한 UI의 자식
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 기준 정렬
            rect.anchoredPosition = position; // 실제 화면 위치
            rect.sizeDelta = size; // 글자 표시 영역
            Text text = item.GetComponent<Text>(); // 표시 텍스트
            text.font = font; // 기존 한글 글꼴
            text.fontSize = fontSize; // 실제 표시 크기
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬
            text.color = Color.white; // 어두운 배경과 대비
            text.raycastTarget = false; // 버튼 입력을 가리지 않음
            return text; // 표시 내용을 갱신할 참조
        }

        private static Button MakeButton(Button template, Transform parent, string name, Vector2 position, Vector2 size, string label) // 기존 버튼 외형을 공유하는 조작
        {
            Button button = Instantiate(template, parent, false); // 기존 프로젝트 버튼 복제
            button.name = name; // 실제 화면의 버튼 식별자
            button.onClick.RemoveAllListeners(); // 원본의 기존 동작 제거
            RectTransform rect = (RectTransform)button.transform; // 위치와 크기
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 기준 정렬
            rect.anchoredPosition = position; // 실제 화면 위치
            rect.sizeDelta = size; // 이름이 들어갈 버튼 크기
            Text text = button.GetComponentInChildren<Text>(); // 기존 한글 버튼 글자
            text.text = label; // 사용자에게 보이는 동작
            FlightCraftedUi.FitLabel(button); // 버튼 크기에 맞춘 한글 문구
            return button; // 클릭 동작을 연결할 버튼
        }

        private static InputField MakeInput(Transform parent, Font font, string name, Vector2 position, Vector2 size, string hint) // 이름과 검색의 직접 입력
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)); // 실제 키 입력을 받는 UI
            RectTransform rect = item.GetComponent<RectTransform>(); // 입력 칸 위치
            rect.SetParent(parent, false); // 지정한 도감 화면의 자식
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 기준 정렬
            rect.anchoredPosition = position; // 실제 화면 위치
            rect.sizeDelta = size; // 입력 문구의 표시 크기
            Image image = item.GetComponent<Image>(); // 입력 칸의 배경
            image.color = new Color(0.12f, 0.17f, 0.24f, 1f); // 어두운 배경과 구분되는 색상
            Text value = Label(item.transform, font, "Text", Vector2.zero, size - new Vector2(20f, 4f), 22); // 실제 입력한 글자
            value.alignment = TextAnchor.MiddleLeft; // 이름의 시작 위치
            Text placeholder = Label(item.transform, font, "Placeholder", Vector2.zero, size - new Vector2(20f, 4f), 22); // 비어 있을 때의 안내
            placeholder.alignment = TextAnchor.MiddleLeft; // 입력 글자와 같은 시작 위치
            placeholder.color = new Color(0.7f, 0.75f, 0.8f, 1f); // 입력 내용과 구별되는 안내 색상
            placeholder.text = hint; // 입력할 내용 안내
            InputField input = item.GetComponent<InputField>(); // 실제 키 입력 제어
            input.targetGraphic = image; // 클릭 가능한 입력 배경
            input.textComponent = value; // 실제 문자열 표시
            input.placeholder = placeholder; // 빈 입력의 안내
            input.characterLimit = 32; // 도감 이름과 검색의 최대 길이
            return input; // 검색과 이름 변경의 입력 참조
        }
    }
}
