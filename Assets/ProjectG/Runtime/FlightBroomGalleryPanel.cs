using System.Collections.Generic; // 세트와 개별 부품 목록
using UnityEngine; // 실제 조립 화면과 그림
using UnityEngine.UI; // 도감의 버튼과 텍스트

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightBroomGalleryPanel : MonoBehaviour // 실제 빗자루 세트 부품 도감
    {
        private readonly List<FlightBroomSet> sets = new(); // 기획 순서의 열 세트
        private readonly List<FlightBroomPart> parts = new(); // 현재 세트의 여덟 부위
        private readonly List<FlightBroomEquipment.InventoryItem> inventory = new(); // 실제 보유한 개별 사본
        private readonly Button[] rows = new Button[8]; // 현재 세트의 부품 버튼
        private FlightBroomEquipment equipment; // 도감과 실제 보유 상태
        private GameObject panel; // 조립 화면 위의 도감
        private Text setName; // 현재 세트의 한글 이름
        private Text page; // 현재 세트와 전체 세트 수
        private Text partName; // 선택한 실제 부품 이름
        private Text details; // 해당 부품의 기획 효과와 보유 상태
        private Text placeholder; // 아직 없는 그림의 명확한 표시
        private RawImage partImage; // 현재 부품의 독립 투명 그림
        private RectTransform imageRect; // 원본 비율로 맞출 실제 그림 영역
        private Button previous; // 이전 세트 이동
        private Button next; // 다음 세트 이동
        private Button setEffects; // 부품과 세트 효과 표시 전환
        private bool showingSetEffects; // 현재 세트 단계별 설명 표시
        private int setIndex; // 현재 보는 실제 세트 번호
        private int selected; // 현재 세트의 실제 부위 번호

        internal void Initialize(FlightBroomEquipment source, Button template, Font font, Transform root) // 실제 조립 화면의 도감 연결
        {
            equipment = source; // 같은 캐릭터의 장비와 카탈로그
            foreach (FlightBroomSet set in equipment.Catalog.Sets) // 원본 카탈로그의 세트 순서
            {
                sets.Add(set); // 실제 도감에서 표시할 세트
            }
            Button open = ButtonAt(template, root, "OpenGallery", new Vector2(270f, 340f), new Vector2(240f, 48f), "부품 도감"); // 조립 화면의 도감 진입
            open.onClick.AddListener(Open); // 실제 세트 목록 표시
            panel = new GameObject("BroomGallery", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 사용자 입력을 받는 도감 배경
            RectTransform panelRect = panel.GetComponent<RectTransform>(); // 도감의 실제 영역
            panelRect.SetParent(root, false); // 조립 화면의 독립 자식
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f); // 기존 화면의 중앙 기준
            panelRect.anchoredPosition = Vector2.zero; // 화면 가운데 배치
            panelRect.sizeDelta = new Vector2(1440f, 800f); // 프리셋과 같은 기준 화면
            panel.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.09f, 1f); // 글자와 대비되는 불투명 배경
            setName = Label(panel.transform, font, "SetName", new Vector2(0f, 335f), new Vector2(850f, 50f), 34, TextAnchor.MiddleCenter); // 실제 세트 제목
            setEffects = ButtonAt(template, panel.transform, "SetEffects", new Vector2(-590f, 335f), new Vector2(190f, 48f), "세트 효과"); // 2·4·8단계의 실제 기획 설명
            setEffects.onClick.AddListener(ToggleSetEffects); // 부품 상세와 세트 단계 전환
            Button close = ButtonAt(template, panel.transform, "Close", new Vector2(590f, 335f), new Vector2(190f, 48f), "돌아가기"); // 조립 화면으로 복귀
            close.onClick.AddListener(Close); // 이미지 참조 정리 후 도감 닫기
            for (int i = 0; i < rows.Length; i++) // 한 세트의 실제 여덟 부위
            {
                int index = i; // 각 버튼의 고정 위치
                rows[i] = ButtonAt(template, panel.transform, "Row" + i, new Vector2(-360f, 245f - i * 63f), new Vector2(600f, 54f), string.Empty); // 선택 가능한 실제 부품
                rows[i].onClick.AddListener(() => Select(index)); // 해당 부위의 그림과 효과 표시
            }
            partName = Label(panel.transform, font, "PartName", new Vector2(350f, 255f), new Vector2(570f, 58f), 30, TextAnchor.MiddleCenter); // 선택한 부품 이름
            partName.resizeTextForBestFit = true; // 긴 부품 이름의 잘림 방지
            partName.resizeTextMinSize = 22; // 작은 화면의 최소 글자 크기
            partName.resizeTextMaxSize = 30; // 제목의 최대 글자 크기
            var imageObject = new GameObject("PartImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); // 투명 아이템 그림과 표시 영역
            imageRect = imageObject.GetComponent<RectTransform>(); // 실제 그림 영역
            imageRect.SetParent(panel.transform, false); // 도감 화면의 자식
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 기준 위치
            imageRect.anchoredPosition = new Vector2(350f, 40f); // 제목 아래의 실제 이미지
            imageRect.sizeDelta = new Vector2(560f, 310f); // 그림을 선명하게 표시할 영역
            partImage = imageObject.GetComponent<RawImage>(); // 실제 이미지 렌더러
            partImage.raycastTarget = false; // 버튼 입력을 가리지 않음
            placeholder = Label(panel.transform, font, "Placeholder", new Vector2(350f, 40f), new Vector2(560f, 70f), 26, TextAnchor.MiddleCenter); // 미제작 이미지 안내
            details = Label(panel.transform, font, "Details", new Vector2(350f, -210f), new Vector2(570f, 160f), 25, TextAnchor.UpperLeft); // 기획 효과와 실제 보유 상태
            details.resizeTextForBestFit = true; // 긴 한글 효과의 잘림 방지
            details.resizeTextMinSize = 19; // 작은 화면의 최소 글자 크기
            details.resizeTextMaxSize = 25; // 기준 화면의 최대 글자 크기
            previous = ButtonAt(template, panel.transform, "Previous", new Vector2(-420f, -335f), new Vector2(200f, 48f), "이전 세트"); // 이전 기획 세트
            previous.onClick.AddListener(() => Move(-1)); // 실제 세트 범위 안에서 이동
            next = ButtonAt(template, panel.transform, "Next", new Vector2(420f, -335f), new Vector2(200f, 48f), "다음 세트"); // 다음 기획 세트
            next.onClick.AddListener(() => Move(1)); // 실제 세트 범위 안에서 이동
            page = Label(panel.transform, font, "Page", new Vector2(0f, -335f), new Vector2(350f, 48f), 26, TextAnchor.MiddleCenter); // 현재 위치와 전체 세트 수
            panel.SetActive(false); // 조립 화면에서는 도감 숨김
        }

        private void Open() // 현재 실제 부품 도감 열기
        {
            setIndex = 0; // 첫 번째 기획 세트부터 표시
            selected = 0; // 첫 번째 부위 선택
            showingSetEffects = false; // 처음에는 개별 부품 표시
            panel.SetActive(true); // 실제 도감 패널 표시
            panel.transform.SetAsLastSibling(); // 조립 화면의 다른 요소보다 앞에 표시
            ShowSet(); // 현재 세트와 부품 읽기
        }

        private void Close() // 조립 화면으로 돌아가기
        {
            ReleaseImage(); // 닫은 도감의 큰 그림 메모리 해제
            panel.SetActive(false); // 도감의 사용자 입력 숨김
        }

        private void ReleaseImage() // 더 이상 표시하지 않는 도감 그림 정리
        {
            Texture previous = partImage.texture; // 현재 화면이 참조하는 실제 리소스
            partImage.texture = null; // UI의 텍스처 참조 먼저 해제
            if (previous != null) // 실제로 로드한 이전 그림
            {
                Resources.UnloadAsset(previous); // 다른 부품을 열 때 큰 원본 메모리 반환
            }
        }

        private void Move(int delta) // 현재 기획 세트의 앞뒤 이동
        {
            setIndex = Mathf.Clamp(setIndex + delta, 0, Mathf.Max(0, sets.Count - 1)); // 실제 열 세트 범위
            selected = 0; // 새 세트의 첫 부위 선택
            ShowSet(); // 실제 부품 목록과 이미지 갱신
        }

        private void ToggleSetEffects() // 현재 세트와 개별 부품의 설명 전환
        {
            showingSetEffects = !showingSetEffects; // 기획된 세트 효과 표시 상태 변경
            Render(); // 같은 세트의 실제 단계 설명 갱신
        }

        private void Select(int index) // 현재 세트의 실제 부품 선택
        {
            if (index < 0 || index >= parts.Count) // 없는 부위 클릭 방지
            {
                return; // 기존 선택 유지
            }
            selected = index; // 해당 부위의 실제 위치
            showingSetEffects = false; // 부품을 선택하면 해당 그림으로 복귀
            Render(); // 선택한 그림과 효과 갱신
        }

        private void ShowSet() // 카탈로그에서 현재 세트의 부품 조회
        {
            parts.Clear(); // 이전 세트의 부품 제거
            if (sets.Count > 0) // 실제 세트가 있는 카탈로그
            {
                string id = sets[setIndex].id; // 현재 기획 세트의 고유 ID
                foreach (FlightBroomPart part in equipment.Catalog.Parts) // 실제 게임 부품 정의
                {
                    if (part.setId == id) // 해당 세트에 속한 부품
                    {
                        parts.Add(part); // 희귀도와 무관한 부위 종류 추가
                    }
                }
                parts.Sort((left, right) => left.slot.CompareTo(right.slot)); // 몸체부터 안정기까지 실제 부위 순서
            }
            equipment.ListInventory(inventory); // 현재 개별 사본의 보유 여부
            Render(); // 현재 세트와 첫 부품 표시
        }

        private void Render() // 선택한 부품의 실제 그림과 기획 효과
        {
            setName.text = sets.Count > 0 ? sets[setIndex].displayName : "부품 도감"; // 실제 세트 이름
            page.text = sets.Count > 0 ? $"{setIndex + 1} / {sets.Count}" : "0 / 0"; // 현재 세트 위치
            previous.interactable = setIndex > 0; // 첫 세트 이전 이동 방지
            next.interactable = setIndex + 1 < sets.Count; // 마지막 세트 이후 이동 방지
            for (int i = 0; i < rows.Length; i++) // 여덟 부위의 실제 목록
            {
                bool visible = i < parts.Count; // 현재 세트에 존재하는 부품
                rows[i].gameObject.SetActive(visible); // 존재하지 않는 부위 숨김
                if (visible) // 실제 기획된 부품
                {
                    rows[i].GetComponentInChildren<Text>().text = (i == selected ? "●  " : string.Empty) + parts[i].displayName; // 현재 선택과 한글 이름
                }
            }
            setEffects.GetComponentInChildren<Text>().text = showingSetEffects ? "부품 보기" : "세트 효과"; // 다음에 열 설명 안내
            if (showingSetEffects && sets.Count > 0) // 선택한 세트의 기획 단계 설명
            {
                FlightBroomSet set = sets[setIndex]; // 실제 카탈로그의 현재 세트
                partName.text = "세트 효과"; // 현재 상세 영역의 내용 안내
                ReleaseImage(); // 세트 설명에서 사용하지 않는 그림 메모리 해제
                partImage.enabled = false; // 세트 설명과 그림이 겹치지 않음
                placeholder.text = string.Empty; // 이미지 제작 안내 숨김
                details.rectTransform.anchoredPosition = new Vector2(350f, 0f); // 그림을 대신할 넓은 설명 영역
                details.rectTransform.sizeDelta = new Vector2(570f, 420f); // 세 단계 설명의 충분한 높이
                details.text = "기획 효과 · 일부 발동 구현 중\n2세트  " + set.effectTwo + "\n4세트  " + set.effectFour + "\n8세트  " + set.effectEight; // 기획과 실제 미구현 범위 구분
                return; // 부품 효과를 덮어쓰지 않음
            }
            details.rectTransform.anchoredPosition = new Vector2(350f, -210f); // 부품 그림 아래의 실제 효과 위치
            details.rectTransform.sizeDelta = new Vector2(570f, 160f); // 부품별 효과의 표시 영역
            if (selected < 0 || selected >= parts.Count) // 표시할 실제 부품 없음
            {
                partName.text = "부품을 선택하세요."; // 빈 목록 안내
                ReleaseImage(); // 이전 이미지와 메모리 해제
                partImage.enabled = false; // 없는 이미지는 표시하지 않음
                placeholder.text = string.Empty; // 제작 중 문구 제거
                details.text = string.Empty; // 이전 효과 제거
                return; // 존재하지 않는 부품 접근 방지
            }
            FlightBroomPart part = parts[selected]; // 현재 선택한 실제 종류
            if (partImage.texture != null && partImage.texture.name != part.id) // 다른 부품의 이전 그림
            {
                ReleaseImage(); // 큰 이전 텍스처의 누적 방지
            }
            Texture2D texture = partImage.texture as Texture2D ?? Resources.Load<Texture2D>("BroomIcons/" + part.id); // 현재 그림 재사용 또는 정확한 ID의 그림 로드
            partName.text = part.displayName; // 실제 기획 부품 이름
            partImage.texture = texture; // 해당 부품의 투명 이미지
            partImage.enabled = texture != null; // 이미지가 없으면 잘못된 대체 그림 금지
            if (texture != null) // 실제 원본 이미지가 존재하는 경우
            {
                texture.mipMapBias = -0.5f; // 작은 화면에서 세부 무늬 선명도 유지
                float ratio = (float)texture.width / texture.height; // 원본의 가로세로 비율
                imageRect.sizeDelta = ratio >= 560f / 310f ? new Vector2(560f, 560f / ratio) : new Vector2(310f * ratio, 310f); // 지정 영역 안에서 원본 비율 유지
            }
            placeholder.text = texture == null ? "이미지 제작 중" : string.Empty; // 없는 그림을 완료로 표시하지 않음
            int owned = 0; // 이 종류의 실제 보유 사본 수
            foreach (FlightBroomEquipment.InventoryItem item in inventory) // 실제 개인 인벤토리
            {
                if (item.Definition.id == part.id) // 같은 종류의 개별 사본
                {
                    owned++; // 등급과 강화가 다른 사본도 합산
                }
            }
            details.text = (owned > 0 ? $"보유 {owned}개" : "미보유") + "\n기획 효과 · 일부 발동 구현 중\n" + part.specialPrimary + "\n" + part.specialLegendary; // 실제 보유와 아직 완성되지 않은 기획 효과 구분
        }

        private static Text Label(Transform parent, Font font, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment) // 기존 한국어 글꼴의 실제 문구
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 화면 영역과 텍스트
            RectTransform rect = item.GetComponent<RectTransform>(); // 실제 기준 위치
            rect.SetParent(parent, false); // 해당 도감의 자식
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 도감의 중앙 기준
            rect.anchoredPosition = position; // 실제 표시 위치
            rect.sizeDelta = size; // 글자가 보일 영역
            Text label = item.GetComponent<Text>(); // 실제 한글 글자
            label.font = font; // 기존 UI의 선명한 한글 글꼴
            label.fontSize = fontSize; // 기준 해상도 글자 크기
            label.alignment = alignment; // 제목과 효과의 정렬
            label.color = Color.white; // 어두운 배경의 높은 대비
            label.raycastTarget = false; // 뒤쪽 버튼 입력 방해 금지
            return label; // 내용을 갱신할 문구
        }

        private static Button ButtonAt(Button template, Transform parent, string name, Vector2 position, Vector2 size, string caption) // 기존 조립 디자인의 실제 버튼
        {
            Button button = Instantiate(template, parent, false); // 기존 테두리와 글꼴 복제
            button.name = name; // 실제 화면과 시험의 식별자
            button.onClick = new Button.ButtonClickedEvent(); // 원래 버튼 동작 제거
            RectTransform rect = (RectTransform)button.transform; // 실제 클릭 영역
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 도감 중앙 기준
            rect.anchoredPosition = position; // 실제 버튼 위치
            rect.sizeDelta = size; // 글자와 클릭 영역 크기
            Text label = button.GetComponentInChildren<Text>(); // 실제 버튼 문구
            label.name = "Label"; // 도감에서 갱신할 실제 텍스트
            label.text = caption; // 사용자가 볼 동작 이름
            FlightCraftedUi.FitLabel(button); // 이름 길이에 맞는 글자 크기
            label.fontSize = 30; // 작은 해상도에서 읽기 쉬운 기준 크기
            label.resizeTextMinSize = 22; // 긴 이름의 최소 가독 크기
            label.resizeTextMaxSize = 30; // 짧은 문구의 최대 크기
            return button; // 실제 입력 동작 연결
        }
    }
}
