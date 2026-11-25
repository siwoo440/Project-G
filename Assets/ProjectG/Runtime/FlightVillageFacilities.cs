using System.Collections.Generic; // 상점의 편집된 판매 목록
using UnityEngine; // 실제 마을 패널과 조종사 선택
using UnityEngine.UI; // 씬에 배치한 상점·강화 버튼

namespace ProjectG // 프로젝트 하늘섬 시설
{
    public sealed class FlightVillageFacilities : MonoBehaviour // 가까운 장비 상점과 공방의 실제 작업
    {
        private static readonly string[] SlotNames = { "몸체", "손잡이", "마력석", "결합부", "추진부", "마법 장식", "선단부", "안정기" }; // 실제 8개 장착 부위
        private static readonly string[] Grades = { "일반", "고급", "희귀" }; // 상점의 유효 판매 등급
        [SerializeField] private FlightBroomEquipment equipment; // 준비와 전투에서 쓰는 동일한 개별 장착
        [SerializeField] private GameObject modal; // 마을을 떠나지 않고 작업하는 시설 UI
        [SerializeField] private GameObject market; // 재고와 공용기 장착 선택
        [SerializeField] private GameObject forge; // 장착 부위의 강화 조작
        [SerializeField] private GameObject growth; // 업적과 후속 전투 선택지 구매
        [SerializeField] private Text title; // 현재 연 시설의 명확한 이름
        [SerializeField] private FlightCurrencyLabel walletCrystals; // 마을의 공용 천공 결정 보유량
        [SerializeField] private FlightCurrencyLabel walletGold; // 마을의 공용 골드 보유량
        [SerializeField] private FlightCurrencyLabel forgeCrystals; // 다음 빗자루 강화의 결정 비용
        [SerializeField] private FlightCurrencyLabel forgeGold; // 다음 빗자루 강화의 골드 비용
        [SerializeField] private FlightCurrencyLabel[] offerGold; // 처음 구입할 상품의 각 골드 가격
        [SerializeField] private FlightCurrencyLabel[] nodeCrystals; // 후속 성장 노드의 각 결정 비용
        [SerializeField] private Text wallet; // 공용 골드와 천공 결정
        [SerializeField] private Text status; // 구매·강화·장착의 실제 결과
        [SerializeField] private Text currentSkill; // 선택 조종사의 저장된 공용기
        [SerializeField] private Text part; // 공방에 올린 장착 부품과 강화 단계
        [SerializeField] private Text price; // 다음 강화의 결정과 골드 비용
        [SerializeField] private Button close; // 마을 현재 위치로 복귀
        [SerializeField] private Button nox; // 녹스의 장착·강화 선택
        [SerializeField] private Button sera; // 근접 조종사의 장착·강화 선택
        [SerializeField] private Button lumi; // 별빛 빗자루와 공용기 선택
        [SerializeField] private Button aria; // 아리아의 장착을 편집하는 선택
        [SerializeField] private Button rian; // 리안의 장착을 편집하는 선택
        [SerializeField] private Button ward; // 캐릭터의 기본 공용 보호
        [SerializeField] private Button fan; // 마력 산탄 공용기
        [SerializeField] private Button previous; // 왼쪽 부위로 순환
        [SerializeField] private Button next; // 오른쪽 부위로 순환
        [SerializeField] private Button enhance; // 다음 단계 강화와 비용 확인
        [SerializeField] private Text[] stockText; // 재고마다의 이름과 현재 보유
        [SerializeField] private Button[] stockButtons; // 해당 개별 부품을 구매 또는 장착
        [SerializeField] private Button openGrowth; // 강화 공방의 후속 성장 화면
        [SerializeField] private Button closeGrowth; // 후속 성장에서 부품 강화로 복귀
        [SerializeField] private Text firstTask; // 현재 조종사의 최초 과제와 보상
        [SerializeField] private Text[] growthInfo; // 구매할 실제 노드와 현재 상태
        [SerializeField] private Button[] growthButtons; // 기획서에서 정한 노드의 개별 구매
        private readonly List<FlightBroomPart> stock = new List<FlightBroomPart>(); // 지금 마을에 표시할 실제 재고
        private FlightBroomPilot selected; // 현재 변경을 적용할 조종사
        private int slot; // 공방에서 고른 장착 부위
        public bool IsOpen => modal.activeSelf; // 뒤의 마을 조작을 잠그는 시설의 실제 표시

        private void Start() // 에디터에 배치한 시설 UI를 버튼과 연결
        {
            close.onClick.AddListener(Close); // Esc와 같은 마을 복귀
            nox.onClick.AddListener(() => SelectPilot(FlightBroomPilot.Nox)); // 녹스 전용 장비와 공용기
            sera.onClick.AddListener(() => SelectPilot(FlightBroomPilot.Sera)); // 세라의 독립 조합과 공용기
            lumi.onClick.AddListener(() => SelectPilot(FlightBroomPilot.Lumi)); // 루미의 장착과 재화 화면
            aria.onClick.AddListener(() => SelectPilot(FlightBroomPilot.Aria)); // 해당 조종사의 현재 장착
            rian.onClick.AddListener(() => SelectPilot(FlightBroomPilot.Rian)); // 해당 조종사의 현재 장착
            ward.onClick.AddListener(() => SelectSkill(false)); // 실제 기본 공용기
            fan.onClick.AddListener(() => SelectSkill(true)); // 실제 산탄으로 저장
            previous.onClick.AddListener(() => ChangeSlot(-1)); // 공방에서 이전 부위 선택
            next.onClick.AddListener(() => ChangeSlot(1)); // 공방에서 다음 부위 선택
            enhance.onClick.AddListener(Enhance); // 실제 공방 작업
            openGrowth.onClick.AddListener(ShowGrowth); // 같은 건물에서 성장 관리
            closeGrowth.onClick.AddListener(ShowForge); // 기존 강화 화면 복귀
            for (int i = 0; i < stockButtons.Length; i++) // 배치한 실제 매대마다 구매 연결
            {
                int index = i; // 이 줄의 구체적인 목록 위치
                stockButtons[index].onClick.AddListener(() => UseStock(index)); // 현재 재고와 해당 버튼 동기화
            }
            for (int i = 0; i < growthButtons.Length; i++) // 씬의 노드 배치 순서대로 구매
            {
                int index = i; // 실제 편집한 연구 위치
                growthButtons[index].onClick.AddListener(() => BuyGrowth(index)); // 캐릭터·비용의 실제 저장
            }
        }

        internal void Open(bool shop) // 현재 접근한 시설 열기
        {
            equipment.ListStock(stock); // 같은 카탈로그에서 실제 마을 판매 품목 조회
            selected = FlightSortieChoice.Initial; // 출격 준비와 공유하는 현재 조종사
            equipment.SelectPilot(selected); // 전체 8개 부위의 현재 장착값 반영
            status.text = string.Empty; // 이전 시설의 결과 알림 제거
            title.text = shop ? "장비 상점" : "강화 공방"; // 실제 방문한 시설
            market.SetActive(shop); // 상점의 재고와 보호막·산탄
            forge.SetActive(!shop); // 공방의 부위 선택과 단계 비용
            growth.SetActive(false); // 상점과 공방의 기본 화면으로 진입
            modal.SetActive(true); // 마을 뒤쪽에 별도로 배치한 씬 패널
            Refresh(); // 현재 저장에 맞는 가격·부품·재화 표시
            close.Select(); // 키보드로도 바로 닫을 수 있게 포커스
        }

        internal void Close() // 마을의 시설 앞에 그대로 돌아가기
        {
            modal.SetActive(false); // 장비와 공방 UI만 닫기
        }

        private void SelectPilot(FlightBroomPilot actor) // 해당 조종사의 장착·강화를 표시
        {
            selected = actor; // 시설에서 바꾼 현재 편집 대상
            equipment.SelectPilot(selected); // 저장된 부품을 실제 조종사로 전환
            FlightSortieChoice.RememberPilot(actor); // 이어지는 준비 화면에서도 같은 선택
            status.text = string.Empty; // 앞 조종사의 결과 숨김
            Refresh(); // 현재 재고와 장착을 다시 표시
        }

        private void SelectSkill(bool useFan) // 공용기 교체를 영구 저장
        {
            bool stored = FlightProgress.Current.SetCommonFan(selected, useFan); // 캐릭터가 혼동되지 않는 개별 저장
            status.text = stored ? "공용 스킬을 장착했습니다." : "공용 스킬을 저장하지 못했습니다."; // 저장 성공에만 성공 알림
            Refresh(); // 보호막 또는 산탄의 실제 장착 이름
        }

        private void UseStock(int index) // 등록된 부품의 구매와 실제 장착
        {
            FlightBroomPart offer = stock[index]; // 장착 슬롯과 등급을 같은 정보에서 읽음
            string id = offer.id + ":1"; // 개별 부품 보유 사본
            if (!equipment.IsOwned(id)) // 구매하지 않은 재고
            {
                bool purchased = equipment.Purchase(offer); // 실제 골드와 보유품 한 파일 저장
                status.text = purchased ? "구매했습니다. 장착을 눌러 교체할 수 있습니다." : "구매할 수 없습니다. 골드와 저장 상태를 확인하세요."; // 실제 재화 결과
            }
            else // 현재 계정의 보유 부품
            {
                bool equipped = equipment.ConfirmPart(offer.slot, id); // 준비와 같은 장착 충돌 검사
                if (equipped) // 실제로 장착한 부위를 공방에 이어 선택
                {
                    slot = (int)offer.slot; // 다음 강화에서 바로 작업
                }
                status.text = equipped ? "선택한 조종사에게 장착했습니다." : "다른 조종사가 장착 중인 부품입니다."; // 저장된 조합의 적용 여부
            }
            Refresh(); // 같은 실행의 보유량·골드·부품 재확인
        }

        private void ChangeSlot(int shift) // 공방의 여덟 부위를 순서대로 비교
        {
            slot = (slot + shift + SlotNames.Length) % SlotNames.Length; // 배열 끝에서 첫 부위로 순환
            status.text = string.Empty; // 이전 부위의 처리 내역 제거
            Refresh(); // 실제 장착과 다음 비용 변경
        }

        private void Enhance() // 현재 장착 부품의 최대 +5 강화
        {
            bool applied = equipment.Enhance((FlightBroomSlot)slot); // 단계와 재화를 함께 실제로 저장
            status.text = applied ? "부품을 강화했습니다." : "강화할 수 없습니다. 재화와 장착 상태를 확인하세요."; // 재화 부족·미장착 시 실제 결과
            Refresh(); // 부품 단계와 남은 두 재화
        }

        private void ShowGrowth() // 같은 공방에서 첫 과제와 전투 후속 선택지 확인
        {
            title.text = "후속 성장"; // 다른 종류의 영구 강화 구분
            status.text = string.Empty; // 부품 강화의 이전 결과 숨기기
            forge.SetActive(false); // 장착 부위의 기존 조작 숨김
            growth.SetActive(true); // 해당 조종사의 연구를 보여 줌
            Refresh(); // 해금 여부와 조율의 증표 표시
            closeGrowth.Select(); // 마우스 없이 공방으로 돌아올 수 있도록 포커스
        }

        private void ShowForge() // 기존 부품 강화로 계속 작업
        {
            title.text = "강화 공방"; // 원래 마을 작업의 제목
            status.text = string.Empty; // 앞서 구매한 연구의 표시 정리
            growth.SetActive(false); // 부품 강화 화면과 중복하지 않음
            forge.SetActive(true); // 현재 부위와 비용
            Refresh(); // 구매로 줄어든 재화 반영
            openGrowth.Select(); // 기존 선택의 실제 포커스 복구
        }

        private void BuyGrowth(int index) // 준비한 조종사의 확정 노드 한 개를 구입
        {
            FlightGrowthNodes.Node node = FlightGrowthNodes.All[index]; // 기획서의 이름과 조종사
            bool purchased = FlightProgress.Current.BuyGrowthNode(node.Id, selected); // 결정 18과 해당 증표 1을 함께 저장
            status.text = purchased ? "해금했습니다. 출격에서 같은 가지의 1단계를 고르세요." : "해금할 수 없습니다. 조종사와 재화를 확인하세요."; // 후속 카드가 나오는 실제 조건
            Refresh(); // 영구 권한과 새 보유액 반영
        }

        private void Refresh() // 현재 시설에 실제 장착·재화 대입
        {
            FlightProgressStore profile = FlightProgress.Current; // 상점과 공방에서 공유하는 영구 자산
            bool viewingGrowth = growth.activeSelf; // 현재 후속 성장을 보거나 시설의 기본 화면 사용
            wallet.text = viewingGrowth ? $"조율의 증표 {profile.GetMarks(selected):N0}" : string.Empty; // 후속 성장의 별도 증표 이름
            walletCrystals?.Show(profile.SharedCrystals); // 모든 시설에 표시하는 공용 보유량
            walletGold?.Show(viewingGrowth ? null : profile.Gold); // 골드를 쓰는 화면의 공용 보유량
            nox.interactable = selected != FlightBroomPilot.Nox; // 현재 감시자 선택 표시
            sera.interactable = selected != FlightBroomPilot.Sera; // 현재 검사 선택 상태
            lumi.interactable = selected != FlightBroomPilot.Lumi; // 현재 탐색자의 선택 상태
            aria.interactable = selected != FlightBroomPilot.Aria; // 선택된 아리아가 표시되도록 잠금
            rian.interactable = selected != FlightBroomPilot.Rian; // 선택된 리안이 표시되도록 잠금
            bool equippedFan = profile.GetCommonFan(selected); // 현재 조종사의 실제 공용기
            currentSkill.text = equippedFan ? "공용 스킬  산탄" : "공용 스킬  보호막"; // 마을과 출격 준비의 공용기
            ward.interactable = equippedFan; // 기존 보호막을 거듭 저장할 필요 없음
            fan.interactable = !equippedFan; // 기존 산탄을 거듭 저장할 필요 없음
            for (int i = 0; i < stockButtons.Length; i++) // 모든 에디터 매대 위치 초기화
            {
                bool exists = i < stock.Count; // 일반부터 희귀 재고 중 실제 항목
                stockButtons[i].transform.parent.gameObject.SetActive(exists); // 설정에서 빠진 재고 행 제거
                if (!exists) // 적용 재고가 없는 표시
                {
                    continue; // 목록 범위를 벗어나지 않음
                }
                FlightBroomPart offer = stock[i]; // 표시·가격·장착을 공유하는 현재 부품
                string id = offer.id + ":1"; // 현재 이 부품의 실제 보유 개체
                bool owned = equipment.IsOwned(id); // 현재 플레이어의 소유
                bool equipped = equipment.GetEquippedId(selected, offer.slot) == id; // 해당 조종사의 실제 장착
                stockText[i].text = $"{offer.displayName}   ·   {Grades[offer.grade - 1]}\n{(owned ? "보유" : string.Empty)}"; // 상품 이름과 구매 여부
                if (offerGold != null) // 편집 중인 씬의 가격 표시 유무
                {
                    offerGold[i].Show(owned ? null : FlightProgressStore.ShopPrice(offer.grade)); // 미구입 상품만 골드 가격 표시
                }
                stockButtons[i].GetComponentInChildren<Text>().text = equipped ? "장착 중" : owned ? "장착" : "구매"; // 실제 가능한 다음 작업
                stockButtons[i].interactable = !equipped; // 중복 장착 입력 방지
            }
            FlightBroomEquipment.PartOption? item = equipment.CurrentPart((FlightBroomSlot)slot); // 선택 부위의 실제 장착
            int level = item?.Upgrade ?? 0; // 최대 단계를 검증할 실제 강화
            part.text = $"{SlotNames[slot]}   ·   {(item.HasValue ? $"{item.Value.Name} +{level}" : "미장착")}"; // 현재 선택과 단계
            var cost = FlightProgressStore.ForgePrice(level); // 정확한 다음 단계 결정과 골드
            enhance.interactable = item.HasValue && cost.Gold > 0; // 장착한 부품의 +5까지만
            price.text = item.HasValue && level >= 5 ? "최대 강화 단계" : item.HasValue ? $"다음 단계 +{level + 1}" : "장착된 부품을 고르세요."; // 현재 단계에서 가능한 조작
            forgeCrystals?.Show(enhance.interactable ? cost.Crystals : null); // 다음 단계에 들어가는 결정
            forgeGold?.Show(enhance.interactable ? cost.Gold : null); // 다음 단계에 들어가는 골드
            string task = selected == FlightBroomPilot.Rian ? "약점 10회 적중" : "기본·집중 사격 50회 적중"; // 기획서의 조종사별 최초 과제
            firstTask.text = selected != FlightBroomPilot.Aria && selected != FlightBroomPilot.Rian ? $"{FlightPilotRoster.Name(selected)}의 첫 과제와 영구 노드는 준비 중입니다." : $"첫 과제   ·   한 출격에서 {task}\n{(profile.HasCompletedFirstTask(selected) ? "조율의 증표 지급 완료" : "최초 보상 조율의 증표 1개")}"; // 공개 과제 상태와 한 번의 보상
            for (int i = 0; i < growthButtons.Length; i++) // 설정한 후속 연구의 실제 표시
            {
                FlightGrowthNodes.Node node = FlightGrowthNodes.All[i]; // 같은 조종사의 확인된 항목
                bool belongs = node.Pilot == selected; // 다른 조종사 노드 제외
                bool unlocked = profile.HasUnlock(node.Id); // 영구 저장의 현재 권한
                growthButtons[i].transform.parent.gameObject.SetActive(belongs); // 현 시설에서 실제 구매 후보
                growthButtons[i].interactable = belongs && !unlocked; // 이미 해금한 비용을 다시 내지 않음
                growthButtons[i].GetComponentInChildren<Text>().text = unlocked ? "해금 완료" : "해금"; // 적용된 실제 저장
                growthInfo[i].text = $"{node.Name}   ·   {node.Requirement}"; // 전투의 선행 카드 조건
                if (nodeCrystals != null) // 씬에서 편집한 성장 비용 표시
                {
                    nodeCrystals[i].Show(FlightGrowthNodes.CrystalCost); // 후속 노드를 살 때 들어가는 결정
                }
            }
        }
    }
}
