using System; // 화면 닫기와 확인 동작
using System.Collections.Generic; // 분류별 실제 연구 목록
using System.Text; // 선행 조건과 보유 재화 표시
using UnityEngine; // 실제 UI 객체와 색상
using UnityEngine.UI; // 버튼과 한글 텍스트

namespace ProjectG // 프로젝트 제작 마법 영역
{
    internal sealed class FlightCraftedResearchPanel : MonoBehaviour // 공용 연구·상점·보상 선택 화면
    {
        private enum ActionKind // 최종 확인할 획득 경로
        {
            Research, Shop, Reward // 연구소·고정 상점·무료 선택권
        }

        private const int PageSize = 8; // 한 화면에서 읽을 수 있는 연구 노드 수
        private static readonly string[] GroupNames = { "속성", "형태", "배열", "궤적", "방식", "위치", "조준", "효과", "발동" }; // 기존 아홉 분류의 표시 순서
        private static readonly string[] TierNames = { "초기", "일반", "심화", "특수" }; // 기획서의 연구 자격 단계
        private readonly List<FlightCraftedResearchNode> visible = new(); // 선택한 분류의 실제 노드
        private readonly Button[] tabs = new Button[9]; // 전체 분류와 진행률 버튼
        private readonly Button[] rows = new Button[PageSize]; // 현재 페이지의 실제 노드 버튼
        private GameObject panel; // 제작 화면 위의 연구 화면
        private GameObject confirm; // 비용과 무료 선택의 최종 확인
        private Text detail; // 선택 요소의 조건과 가격
        private Text wallet; // 현재 자격과 실제 공용 재화
        private Text pageLabel; // 분류별 현재 페이지
        private Text notice; // 실제 저장 성공과 실패 안내
        private Text confirmText; // 실제 차감 또는 무료 수령의 확인 문구
        private Button research; // 연구 비용으로 해금
        private Button shop; // 고정 상점에서 구매
        private Button reward; // 보류된 무료 선택권 사용
        private Button parentLink; // 다른 분류 선행 노드로 이동
        private Button secondParentLink; // 두 번째 선행 노드로 직접 이동
        private CraftedSpellGroup group; // 현재 보고 있는 제작 분류
        private FlightCraftedResearchNode selected; // 실제로 선택한 연구 노드
        private int page; // 현재 분류의 목록 페이지
        private ActionKind pendingAction; // 마지막 확인을 기다리는 획득 경로
        private Action closed; // 제작 화면의 현재 잠금 표시 갱신

        internal void Initialize(Button template, Font font, Action close, Transform overlay = null, Vector2? entry = null) // 기존 카드 스타일과 연구 진입 연결
        {
            closed = close; // 연구를 닫은 뒤 원래 조합 갱신
            Button open = CloneButton(template, transform, "OpenResearch", entry ?? new Vector2(-445f, 335f), new Vector2(205f, 49f), "연구 · 보상"); // 제작 화면과 공방의 연구 진입
            open.onClick.AddListener(Open); // 실제 공용 연구 화면 열기
            panel = Panel(overlay != null ? overlay : transform, "ResearchPanel", Vector2.zero, new Vector2(1440f, 800f), new Color(0.025f, 0.055f, 0.09f, 1f)); // 편집 화면과 공방 위의 불투명 독립 패널
            Text heading = Label(panel.transform, font, "Heading", new Vector2(0f, 340f), new Vector2(860f, 55f), 34, TextAnchor.MiddleCenter); // 상단 연구 화면 제목
            heading.text = "마법 연구와 보상"; // 실제 사용자에게 보이는 화면명
            wallet = Label(panel.transform, font, "Wallet", new Vector2(0f, 303f), new Vector2(1050f, 35f), 22, TextAnchor.MiddleCenter); // 공용 자격과 보유 재화
            Button closeButton = CloneButton(template, panel.transform, "Close", new Vector2(590f, 340f), new Vector2(190f, 48f), "돌아가기"); // 연구를 닫는 실제 버튼
            closeButton.onClick.AddListener(Hide); // 원래 제작 화면 복귀
            for (int i = 0; i < tabs.Length; i++) // 아홉 제작 분류의 탭
            {
                int index = i; // 각 버튼이 보존할 실제 분류 순서
                tabs[i] = CloneButton(template, panel.transform, "Tab" + (CraftedSpellGroup)i, new Vector2(-600f + i * 150f, 248f), new Vector2(142f, 48f), GroupNames[i]); // 균등하게 배치한 분류 버튼
                tabs[i].onClick.AddListener(() => SelectGroup((CraftedSpellGroup)index)); // 해당 분류의 연구 목록 표시
            }
            for (int i = 0; i < rows.Length; i++) // 현재 페이지에서 선택하는 노드
            {
                int index = i; // 이 줄의 고정 위치
                rows[i] = CloneButton(template, panel.transform, "Node" + i, new Vector2(-300f, 176f - i * 54f), new Vector2(710f, 47f), string.Empty); // 충분한 글자 폭의 연구 항목
                rows[i].onClick.AddListener(() => SelectRow(index)); // 해당 줄의 실제 노드 선택
            }
            Button previous = CloneButton(template, panel.transform, "Previous", new Vector2(-520f, -305f), new Vector2(135f, 46f), "이전"); // 앞 페이지로 이동
            previous.onClick.AddListener(() => ChangePage(-1)); // 실제 목록의 이전 페이지
            Button next = CloneButton(template, panel.transform, "Next", new Vector2(-80f, -305f), new Vector2(135f, 46f), "다음"); // 다음 페이지로 이동
            next.onClick.AddListener(() => ChangePage(1)); // 실제 목록의 다음 페이지
            pageLabel = Label(panel.transform, font, "Page", new Vector2(-300f, -305f), new Vector2(260f, 42f), 22, TextAnchor.MiddleCenter); // 현재 페이지와 전체 수
            detail = Label(panel.transform, font, "Detail", new Vector2(405f, 90f), new Vector2(520f, 220f), 24, TextAnchor.UpperLeft); // 분류 탭과 겹치지 않는 자세한 조건
            detail.resizeTextForBestFit = true; // 긴 선행 조건도 설명 영역 안에 표시
            detail.resizeTextMinSize = 16; // 작은 화면의 최소 설명 글자 크기
            detail.resizeTextMaxSize = 24; // 기준 화면의 최대 설명 글자 크기
            parentLink = CloneButton(template, panel.transform, "Parent", new Vector2(290f, -78f), new Vector2(220f, 46f), "첫 선행 보기"); // 다른 분류로도 이동하는 첫 선행 버튼
            parentLink.onClick.AddListener(() => GoToParent(0)); // 첫 번째 실제 선행으로 이동
            secondParentLink = CloneButton(template, panel.transform, "Parent2", new Vector2(520f, -78f), new Vector2(220f, 46f), "둘째 선행 보기"); // 두 선행이 필요한 연구의 직접 이동
            secondParentLink.onClick.AddListener(() => GoToParent(1)); // 두 번째 실제 선행으로 이동
            research = CloneButton(template, panel.transform, "Research", new Vector2(405f, -137f), new Vector2(450f, 48f), "연구하기"); // 결정과 골드로 연구
            research.onClick.AddListener(() => Offer(ActionKind.Research)); // 실제 비용 확인
            shop = CloneButton(template, panel.transform, "Shop", new Vector2(405f, -194f), new Vector2(450f, 48f), "상점에서 구입"); // 일반·심화 고정 상점
            shop.onClick.AddListener(() => Offer(ActionKind.Shop)); // 상점 골드 확인
            reward = CloneButton(template, panel.transform, "Reward", new Vector2(405f, -251f), new Vector2(450f, 48f), "보류 보상 사용"); // 같은 단계의 무료 선택권
            reward.onClick.AddListener(() => Offer(ActionKind.Reward)); // 수령할 실제 보상 확인
            notice = Label(panel.transform, font, "Notice", new Vector2(0f, -360f), new Vector2(1250f, 42f), 22, TextAnchor.MiddleCenter); // 저장된 결과 또는 거절 사유
            confirm = Panel(panel.transform, "Confirm", Vector2.zero, new Vector2(1440f, 800f), new Color(0.025f, 0.04f, 0.075f, 1f)); // 다른 선택을 가리는 불투명 최종 확인 창
            confirmText = Label(confirm.transform, font, "Message", new Vector2(0f, 55f), new Vector2(700f, 130f), 27, TextAnchor.MiddleCenter); // 선택 대상과 실제 비용
            Button accept = CloneButton(template, confirm.transform, "Accept", new Vector2(180f, -80f), new Vector2(230f, 55f), "확인"); // 비용 또는 무료 수령 확정
            accept.onClick.AddListener(Apply); // 저장 성공 시에만 결과 반영
            Button cancel = CloneButton(template, confirm.transform, "Cancel", new Vector2(-180f, -80f), new Vector2(230f, 55f), "취소"); // 현재 재화와 선택 보존
            cancel.onClick.AddListener(() => confirm.SetActive(false)); // 실제 지급 없이 확인 창 닫기
            confirm.SetActive(false); // 처음에는 확인 창 숨김
            panel.SetActive(false); // 제작 화면 진입만으로 연구 화면을 열지 않음
        }

        internal void Hide() // 연구를 닫고 원래 제작 화면 복귀
        {
            if (confirm != null) // 화면 초기화 이후의 확인 창
            {
                confirm.SetActive(false); // 이전 거래 확인 취소
            }
            if (panel != null) // 이미 생성한 연구 화면
            {
                panel.SetActive(false); // 뒤쪽 제작 화면 표시
            }
            closed?.Invoke(); // 실제 해금 상태의 버튼 갱신
        }

        private void Open() // 실제 계정의 연구 화면 열기
        {
            panel.SetActive(true); // 뒤쪽 편집 조작과 분리
            panel.transform.SetAsLastSibling(); // 뒤쪽 제작과 도감 버튼보다 앞에 표시
            confirm.SetActive(false); // 이전 확인 상태 제거
            notice.text = string.Empty; // 지난 구매 결과 제거
            SelectGroup(CraftedSpellGroup.Element); // 첫 분류와 실제 공용 상태
        }

        private void SelectGroup(CraftedSpellGroup category) // 선택한 분류의 실제 노드 표시
        {
            group = category; // 현재 분류 보존
            page = 0; // 분류 변경 시 첫 페이지
            selected = null; // 다른 분류의 이전 선택 해제
            Render(); // 목록과 현재 자격 갱신
        }

        private void ChangePage(int delta) // 분류 안의 페이지 이동
        {
            int pages = Mathf.Max(1, (visible.Count + PageSize - 1) / PageSize); // 실제 항목 수에 필요한 페이지
            page = Mathf.Clamp(page + delta, 0, pages - 1); // 목록 범위 밖 이동 차단
            selected = null; // 이전 페이지의 선택 해제
            Render(); // 새 페이지와 버튼 상태 표시
        }

        private void SelectRow(int index) // 현재 페이지의 실제 연구 노드 선택
        {
            int position = page * PageSize + index; // 전체 분류 목록의 위치
            if (position < 0 || position >= visible.Count) // 빈 목록 줄이나 범위 밖 요청
            {
                return; // 실제 연구 선택 변경 없음
            }
            selected = visible[position]; // 선택한 분류와 요소의 고정 ID
            Render(); // 조건과 가능한 획득 경로 표시
        }

        private void GoToParent(int index) // 선택한 실제 선행 노드로 이동
        {
            if (selected == null || index < 0 || index >= selected.Parents.Length) // 요청한 선행 요소가 없는 연구
            {
                return; // 현재 분류 유지
            }
            FlightCraftedResearchNode parent = FlightCraftedResearchCatalog.FindKey(selected.Parents[index]); // 선택한 실제 선행 요소
            if (parent == null) // 잘못된 선행 ID 보호
            {
                return; // 알 수 없는 목록으로 이동하지 않음
            }
            group = parent.Group; // 다른 분류의 선행도 실제 분류로 이동
            visible.Clear(); // 이전 분류 목록 제거
            foreach (FlightCraftedResearchNode node in FlightCraftedResearchCatalog.All) // 확정된 전체 연구 노드
            {
                if (node.Group == group) // 이동한 선행의 제작 분류
                {
                    visible.Add(node); // 해당 분류의 실제 순서
                }
            }
            page = Mathf.Max(0, visible.IndexOf(parent)) / PageSize; // 선택한 선행이 보이는 페이지
            selected = parent; // 실제 선행 노드를 현재 선택
            Render(); // 이동 결과와 잠금 상태 표시
        }

        private void Render() // 현재 계정의 실제 연구 목록과 버튼 상태
        {
            FlightProgressStore profile = FlightProgress.Current; // 화면과 같은 공용 영구 저장
            int tier = profile.ResearchTier(); // 동일 조종사 조건을 통과한 공용 자격
            wallet.text = $"연구 자격 {TierNames[tier]}   ·   천공 결정 {profile.SharedCrystals}   ·   골드 {profile.Gold}"; // 현재 보유한 실제 자원
            visible.Clear(); // 선택 분류를 현재 저장에 맞춰 다시 구성
            foreach (FlightCraftedResearchNode node in FlightCraftedResearchCatalog.All) // 문서의 모든 분류별 노드
            {
                if (node.Group == group) // 현재 선택한 제작 분류
                {
                    visible.Add(node); // 기획서 순서의 실제 항목
                }
            }
            for (int i = 0; i < tabs.Length; i++) // 분류별 공용 해금 진행률
            {
                int total = 0; // 이 분류의 전체 요소
                int unlocked = 0; // 실제로 이용 가능한 요소
                foreach (FlightCraftedResearchNode node in FlightCraftedResearchCatalog.All) // 전체 분류 표
                {
                    if ((int)node.Group == i) // 현재 탭의 제작 분류
                    {
                        total++; // 해당 분류 전체 수
                        unlocked += profile.IsResearchUnlocked(node.Group, node.Id) ? 1 : 0; // 기본·구매·연동 해금 수
                    }
                }
                tabs[i].GetComponentInChildren<Text>().text = $"{GroupNames[i]} {unlocked}/{total}"; // 실제 공용 진행률
            }
            int pages = Mathf.Max(1, (visible.Count + PageSize - 1) / PageSize); // 현재 분류의 전체 페이지
            page = Mathf.Clamp(page, 0, pages - 1); // 목록이 바뀌어도 유효한 위치
            pageLabel.text = $"{page + 1} / {pages}"; // 사용자가 확인할 페이지
            for (int i = 0; i < rows.Length; i++) // 화면에 배치한 여덟 연구 줄
            {
                int position = page * PageSize + i; // 실제 분류 목록의 노드 위치
                bool present = position < visible.Count; // 현재 페이지의 유효한 항목
                rows[i].gameObject.SetActive(present); // 빈 줄은 선택할 수 없게 숨김
                if (present) // 실제 표시할 연구 노드
                {
                    FlightCraftedResearchNode node = visible[position]; // 이 줄의 정확한 구성 요소
                    bool unlocked = profile.IsResearchUnlocked(node.Group, node.Id); // 공용 저장의 실제 해금 상태
                    rows[i].GetComponentInChildren<Text>().text = $"{Name(node)}   ·   {TierNames[node.Tier]}   ·   {(unlocked ? "해금" : "잠금")}"; // 이름과 단계·상태 구분
                }
            }
            if (selected == null) // 아직 연구를 고르지 않은 경우
            {
                detail.text = "구성 요소를 선택하면\n선행 연구와 획득 조건이 표시됩니다."; // 실제 선택을 안내하는 설명
                research.interactable = false; // 선택 전 비용 차감 금지
                shop.interactable = false; // 선택 전 상점 구매 금지
                reward.interactable = false; // 선택 전 보상 사용 금지
                parentLink.interactable = false; // 선택 전 선행 이동 금지
                secondParentLink.interactable = false; // 선택 전 둘째 선행 이동 금지
                return; // 없는 항목의 비용 조회 방지
            }
            bool owned = profile.IsResearchUnlocked(selected.Group, selected.Id); // 현재 선택의 실제 공용 해금
            bool eligible = profile.CanSelectResearch(selected.Group, selected.Id); // 자격과 모든 선행을 검사한 결과
            var text = new StringBuilder(); // 줄바꿈이 있는 조건 설명
            text.Append(Name(selected)).Append("  ·  ").Append(TierNames[selected.Tier]).Append('\n'); // 구성 이름과 연구 단계
            text.Append(owned ? "해금 완료" : eligible ? "획득 가능" : "잠금 · 자격 또는 선행 필요").Append('\n'); // 저장과 선행 조건의 실제 상태
            if (selected.Linked) // 별도 구매가 없는 유도 조준
            {
                text.Append("선행 연구와 무료 연동\n"); // 중복 연구 비용이 없다는 설명
            }
            else if (selected.Tier > 0) // 연구 또는 상점으로 얻는 요소
            {
                int crystals = selected.Tier == 1 ? 3 : selected.Tier == 2 ? 6 : 12; // 단계별 연구 결정 비용
                int gold = selected.Tier == 1 ? 300 : selected.Tier == 2 ? 600 : 1200; // 단계별 연구 골드 비용
                text.Append($"연구  결정 {crystals} · 골드 {gold}\n"); // 실제 원자 저장의 가격
                text.Append(selected.Tier == 3 ? "상점 판매 제외\n" : $"상점  골드 {(selected.Tier == 1 ? 900 : 1800)}\n"); // 특수 판매 제외와 고정 상점가
                text.Append($"보류 선택권  {profile.PendingResearchRewards(selected.Tier).Length}건\n"); // 같은 단계의 남은 보상 건
            }
            foreach (string key in selected.Parents) // 선택 노드의 모든 실제 선행
            {
                FlightCraftedResearchNode parent = FlightCraftedResearchCatalog.FindKey(key); // 선행 ID가 가리키는 노드
                if (parent != null) // 유효한 기획 선행 요소
                {
                    text.Append("선행  ").Append(Name(parent)).Append(profile.IsResearchUnlocked(parent.Group, parent.Id) ? " ✓\n" : " · 잠김\n"); // 각 선행의 실제 상태
                }
            }
            detail.text = text.ToString(); // 선택한 구성의 조건과 재화 표시
            research.interactable = eligible && profile.SharedCrystals >= (selected.Tier == 1 ? 3 : selected.Tier == 2 ? 6 : 12) && profile.Gold >= (selected.Tier == 1 ? 300 : selected.Tier == 2 ? 600 : 1200); // 실제 연구 가능과 재화
            shop.interactable = eligible && selected.Tier <= 2 && profile.Gold >= (selected.Tier == 1 ? 900 : 1800); // 특수 판매 제외와 실제 골드
            reward.interactable = eligible && profile.PendingResearchRewards(selected.Tier).Length > 0; // 같은 단계의 남은 무료 보상
            parentLink.interactable = selected.Parents.Length > 0; // 실제 선행이 있는 항목만 이동
            secondParentLink.interactable = selected.Parents.Length > 1; // 둘째 선행이 있는 항목만 이동
        }

        private void Offer(ActionKind action) // 선택한 연구의 비용 또는 무료 보상 확인
        {
            if (selected == null) // 아직 획득할 구성을 고르지 않음
            {
                return; // 비용과 선택권 보존
            }
            FlightProgressStore profile = FlightProgress.Current; // 확인 시점의 실제 계정
            if (!profile.CanSelectResearch(selected.Group, selected.Id)) // 자격과 선행이 바뀌었거나 이미 해금
            {
                notice.text = "연구 자격과 선행 요소를 확인하세요."; // 실제 거절 이유 안내
                Render(); // 바뀐 계정 상태 재확인
                return; // 유효하지 않은 거래 중단
            }
            int crystals = selected.Tier == 1 ? 3 : selected.Tier == 2 ? 6 : 12; // 단계별 연구 결정 비용
            int gold = selected.Tier == 1 ? 300 : selected.Tier == 2 ? 600 : 1200; // 단계별 연구 골드 비용
            if (action == ActionKind.Shop) // 고정 상점의 실제 가격
            {
                crystals = 0; // 상점은 결정을 소모하지 않음
                gold = selected.Tier == 1 ? 900 : selected.Tier == 2 ? 1800 : 0; // 특수 요소는 판매 제외
            }
            if (action == ActionKind.Reward) // 보류된 무료 선택권
            {
                crystals = 0; // 보상 선택은 결정 무료
                gold = 0; // 보상 선택은 골드 무료
            }
            if ((action == ActionKind.Shop && selected.Tier > 2) || (action == ActionKind.Reward && profile.PendingResearchRewards(selected.Tier).Length == 0) || profile.SharedCrystals < crystals || profile.Gold < gold) // 실제 경로와 현재 자원 재확인
            {
                notice.text = "선택권 또는 재화가 부족합니다."; // 실제 획득 불가 이유 안내
                Render(); // 현재 계정과 버튼 상태 동기화
                return; // 잘못된 거래는 확인하지 않음
            }
            pendingAction = action; // 확인한 실제 획득 경로
            confirmText.text = $"{Name(selected)}\n결정 {crystals}  ·  골드 {gold}\n선택한 요소를 해금할까요?"; // 정확한 대상과 소모 재화
            confirm.SetActive(true); // 실제 저장 전 최종 확인
            confirm.transform.SetAsLastSibling(); // 다른 연구 버튼보다 위에 표시
        }

        private void Apply() // 확인한 거래를 실제 공용 저장에 적용
        {
            if (selected == null) // 확인 중 선택이 사라진 경우
            {
                confirm.SetActive(false); // 더 이상 유효하지 않은 확인 닫기
                return; // 재화와 해금 보존
            }
            FlightProgressStore profile = FlightProgress.Current; // 현재 실제 보유와 해금 상태
            bool applied = false; // 실제 원자 저장의 성공 여부
            if (pendingAction == ActionKind.Research) // 결정과 골드를 쓰는 연구
            {
                applied = profile.BuyResearch(selected.Group, selected.Id); // 자격과 선행을 다시 검사한 연구
            }
            else if (pendingAction == ActionKind.Shop) // 고정 상점에서 선택한 요소
            {
                applied = profile.BuyResearchShop(selected.Group, selected.Id); // 같은 해금 ID의 상점 구매
            }
            else // 같은 단계에 남은 무료 보상 선택권
            {
                string[] pending = profile.PendingResearchRewards(selected.Tier); // 확인 시점의 실제 미수령 보상
                applied = pending.Length > 0 && profile.ClaimResearchReward(pending[0], selected.Group, selected.Id); // 실제 보상 한 건만 수령
            }
            confirm.SetActive(false); // 성공과 실패 모두 확인 창 닫기
            notice.text = applied ? "구성 요소를 해금했습니다." : "해금하지 못했습니다. 자격과 저장 상태를 확인하세요."; // 실제 저장 성공 여부만 표시
            Render(); // 재화·자격·보상과 해금 갱신
        }

        private static string Name(FlightCraftedResearchNode node) // 고정 ID에 대응하는 실제 한국어 이름
        {
            foreach (CraftedSpellChoice choice in CraftedSpellCatalog.Choices(node.Group)) // 같은 제작 분류의 표시 이름
            {
                if (choice.Id == node.Id) // 분류와 ID가 모두 일치
                {
                    return choice.Label; // 기존 제작 화면과 같은 이름
                }
            }
            return node.Id; // 잘못된 데이터에서 다른 요소로 바꾸지 않는 표시
        }

        private static GameObject Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color) // 읽기 쉬운 배경 패널
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 실제 UI와 입력을 가리는 배경
            RectTransform rect = item.GetComponent<RectTransform>(); // 크기와 위치를 정할 UI 영역
            rect.SetParent(parent, false); // 실제 제작 화면의 자식
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중심을 기준으로 배치
            rect.anchoredPosition = position; // 기준 화면의 실제 위치
            rect.sizeDelta = size; // 기준 화면의 실제 크기
            item.GetComponent<Image>().color = color; // 글자와 구분되는 어두운 배경
            return item; // 화면과 확인 창의 공통 배경
        }

        private static Text Label(Transform parent, Font font, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment) // 기존 한글 글꼴을 쓰는 선명한 텍스트
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 실제 화면에 표시할 텍스트
            RectTransform rect = item.GetComponent<RectTransform>(); // 현재 화면의 텍스트 위치
            rect.SetParent(parent, false); // 지정한 연구 화면 안에 표시
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중심 기준 정렬
            rect.anchoredPosition = position; // 기준 화면에서의 위치
            rect.sizeDelta = size; // 줄바꿈에 사용할 실제 영역
            Text text = item.GetComponent<Text>(); // 사용자에게 표시할 글자
            text.font = font; // 기존 프로젝트의 한글 글꼴 공유
            text.fontSize = fontSize; // 작은 화면에서도 읽을 기준 크기
            text.alignment = alignment; // 각 영역에 맞는 텍스트 정렬
            text.color = Color.white; // 어두운 바탕과 대비되는 글자
            text.raycastTarget = false; // 뒤쪽 연구 버튼의 입력을 가리지 않음
            return text; // 실제 내용과 상태를 갱신할 참조
        }

        private static Button CloneButton(Button template, Transform parent, string name, Vector2 position, Vector2 size, string label) // 기존 버튼 외형을 공유하는 실제 조작
        {
            Button button = Instantiate(template, parent, false); // 프로젝트에서 쓰는 버튼 외형 복제
            button.name = name; // 자동 검증과 사용자 흐름의 실제 버튼 이름
            button.onClick.RemoveAllListeners(); // 원본 버튼의 이전 기능 제외
            RectTransform rect = (RectTransform)button.transform; // UI 위치와 크기 설정
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중심 기준 정렬
            rect.anchoredPosition = position; // 기준 화면에서의 위치
            rect.sizeDelta = size; // 항목별 텍스트를 담을 폭
            Text text = button.GetComponentInChildren<Text>(); // 원본과 같은 한글 글자
            text.text = label; // 실제 사용자 버튼 이름
            FlightCraftedUi.FitLabel(button); // 복제한 글자가 버튼 테두리 밖으로 넘치지 않도록 보정
            return button; // 실제 행동을 연결할 버튼
        }
    }
}
