using System; // 열거형 항목 수
using System.Collections.Generic; // 고유 ID 검색
using UnityEngine; // 비행체와 부품 렌더러

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 비행체별 장착 구성 한 개
    public sealed class FlightBroomEquipment : MonoBehaviour // 보유 부품과 캐릭터별 조합 관리
    {
        private static readonly string[] SlotNames = { "몸체", "손잡이", "마력석", "결합부", "추진부", "마법 장식", "선단부", "안정기" }; // 오류 안내의 실제 부위 이름

        private sealed class OwnedPart // 같은 종류도 분리한 개별 보유 부품
        {
            public string Id; // 종류와 개체 순번을 포함한 ID
            public FlightBroomPart Definition; // 같은 종류가 공유하는 효과와 외형
            public int Grade; // 개별 등급
            public int Upgrade; // 개별 강화 단계
            public int Owner = -1; // 미장착 또는 장착 캐릭터 번호
        }

        [SerializeField] private FlightBroomCatalog catalog; // 씬에 연결한 부품 목록
        [SerializeField] private MeshRenderer[] mounts; // 슬롯 순서의 씬 렌더러
        private readonly Dictionary<string, OwnedPart> owned = new(); // 개별 보유 ID로 빠른 조회
        private readonly OwnedPart[,] equipped = new OwnedPart[Enum.GetValues(typeof(FlightBroomPilot)).Length, Enum.GetValues(typeof(FlightBroomSlot)).Length]; // 캐릭터별 슬롯
        private readonly float[] bonuses = new float[Enum.GetValues(typeof(FlightBroomStat)).Length]; // 활성 조합의 효과 합계
        private Material[] defaultMaterials; // 부착점 기본 재질
        private Vector3[] defaultScales; // 부착점 기본 크기
        private FirstFlightController flight; // 집중 비행의 실제 상태
        private FlightSkillController skills; // 낮은 마력의 실제 상태
        private bool bonusFocus; // 마지막으로 계산한 집중 조건
        private bool bonusLowMana; // 마지막으로 계산한 낮은 마력 조건
        private float dashSpeedUntil; // 폭풍 장식의 한시 비행 속도 종료 시각
        private float dashSpeedReadyAt; // 폭풍 장식의 내부 재사용 시각
        private int dashSpeedTriggers; // 이번 출격의 장식 속도 발동 횟수
        private const string LoadoutKey = "ProjectG.Flight.Broom.Loadouts.v1"; // 버전별 현재 장착 저장 키

        [Serializable] // 캐릭터·슬롯 순서로 기록하는 저장 형식
        private sealed class LoadoutData
        {
            public string[] ids; // 선형 배열의 개별 장착 ID
        }

        internal readonly struct PartOption // 조립 화면에서 쓰는 읽기 전용 부품 정보
        {
            public readonly string Id; // 개별 보유 부품
            public readonly string Name; // 부품 목록 표시 이름
            public readonly int Grade; // 등급
            public readonly int Upgrade; // 강화 단계
            public readonly int Owner; // 장착 캐릭터 또는 미장착
            public readonly int Copies; // 같은 종류로 보유한 수량

            public PartOption(string id, string name, int grade, int upgrade, int owner, int copies) // 보유 부품의 화면 표시 정보
            {
                Id = id; // 보유 ID 기록
                Name = name; // 표시 이름 기록
                Grade = grade; // 등급 기록
                Upgrade = upgrade; // 강화 기록
                Owner = owner; // 현재 소유자 기록
                Copies = copies; // 복수 보유 여부 기록
            }
        }

        internal readonly struct InventoryItem // 제작·분해 화면의 개별 보유 정보
        {
            public readonly string Id; // 같은 종류를 구분하는 실제 보유 ID
            public readonly FlightBroomPart Definition; // 부품 종류와 보호 여부
            public readonly int Grade; // 개별 희귀도
            public readonly int Upgrade; // 개별 강화 단계
            public readonly int Owner; // 미장착 또는 실제 장착 캐릭터
            public readonly bool Locked; // 저장된 분해 잠금

            internal InventoryItem(string id, FlightBroomPart definition, int grade, int upgrade, int owner, bool locked) // 사용자에게 표시할 실제 사본
            {
                Id = id; // 개별 부품 ID
                Definition = definition; // 실제 카탈로그의 종류
                Grade = grade; // 실제 획득 등급
                Upgrade = upgrade; // 저장된 강화 단계
                Owner = owner; // 현재 장착 상태
                Locked = locked; // 영구 잠금 상태
            }
        }

        private static string SaveKey => Environment.GetEnvironmentVariable("PROJECTG_BROOM_TEST_KEY") ?? (FlightBindings.UsesPreferences ? LoadoutKey : null); // 실제 설정과 격리 검증 분리

        public int OwnedCount => owned.Count; // 전체 보유 부품 수
        internal FlightBroomCatalog Catalog => catalog; // 실제 출격의 보상 후보
        public FlightBroomPilot ActivePilot { get; private set; } // 씬에서 조작하는 캐릭터

        private void Awake() // 씬 참조와 시작 장착 초기화
        {
            TryGetComponent(out flight); // 현재 캐릭터의 집중 비행 상태
            TryGetComponent(out skills); // 현재 캐릭터의 소수 마력 상태
            defaultMaterials = new Material[mounts.Length]; // 렌더러별 원본 재질 공간
            defaultScales = new Vector3[mounts.Length]; // 렌더러별 원본 크기 공간
            for (int i = 0; i < mounts.Length; i++) // 부착점 원본 기록
            {
                defaultMaterials[i] = mounts[i].sharedMaterial; // 적용 전 재질
                defaultScales[i] = mounts[i].transform.localScale; // 적용 전 크기
            }

            foreach (FlightBroomPart definition in catalog.Parts) // 카탈로그에서 시작 보유 부품 구성
            {
                for (int copy = 1; copy <= definition.ownedCopies; copy++) // 같은 종류의 개별 보유 수량
                {
                    if (FlightProgress.Current.IsBroomRemoved($"{definition.id}:{copy}")) // 기존 세트 부품의 실제 분해 기록
                    {
                        continue; // 분해한 사본을 다시 만들지 않음
                    }
                    OwnedPart item = AddOwned(definition, copy); // 보유 ID와 이전 강화 상태 등록
                    if (definition.starter && copy == 1) // 기본 조합에 첫 번째 부품 장착
                    {
                        Equip(item, (int)definition.pilot, (int)definition.slot); // 최초 캐릭터 장착
                    }
                }
                if (definition.stocked && FlightProgress.Current.OwnsBroomPart(definition.id + ":1") && !owned.ContainsKey(definition.id + ":1")) // 해당 판매 부품을 실제로 산 기록
                {
                    AddOwned(definition, 1); // 선택 화면·마을·실전이 같은 프로필에서 복원
                }
            }

            foreach (FlightBroomReward reward in FlightProgress.Current.RewardBrooms()) // 정산에서 영구 저장한 개별 부품
            {
                FlightBroomPart definition = catalog.Find(reward.DefinitionId); // 저장된 종류의 실제 정의
                if (definition != null && !owned.ContainsKey(reward.Id)) // 유효하고 아직 복원하지 않은 사본
                {
                    AddOwned(definition, reward.Id, reward.Grade); // 개별 등급과 강화 상태 복원
                }
            }
            RestoreLoadouts(); // 재실행 시 마지막으로 확정한 부품 적용
            ApplyLoadout(); // 첫 비행의 외형과 수치 적용
        }

        private OwnedPart AddOwned(FlightBroomPart definition, int copy) // 시작 부품과 상점 구입을 같은 목록에 등록
        {
            string id = $"{definition.id}:{copy}"; // 해당 종류의 개별 보유 ID
            return AddOwned(definition, id, definition.grade); // 기존 초기·상점 부품의 고정 등급
        }

        private OwnedPart AddOwned(FlightBroomPart definition, string id, int grade) // 개별 보상과 기존 부품의 공통 복원
        {
            var item = new OwnedPart // UI와 비행이 함께 읽는 실제 부품
            {
                Id = id, // 각 보유 사본의 고정 ID
                Definition = definition, // 재질과 능력치 정의
                Grade = grade, // 개별 사본의 실제 등급
                Upgrade = FlightProgress.Current.BroomUpgrade(id, definition.upgrade) // 개별 부품의 확정 강화
            };
            owned.Add(item.Id, item); // 실제 장착에서 조회할 보유 목록
            return item; // 시작 장착 또는 상점 구매에서 사용할 개체
        }

        internal void ListStock(List<FlightBroomPart> result) // 에디터에서 고른 상점 초기 재고
        {
            result.Clear(); // 화면을 다시 열 때 이전 조회를 제거
            foreach (FlightBroomPart definition in catalog.Parts) // 현재 프로젝트의 유효한 부품
            {
                if (definition.stocked && FlightProgressStore.ShopPrice(definition.grade) > 0) // 일반·고급·희귀 등록 부품
                {
                    result.Add(definition); // 상점 화면의 구매 후보
                }
            }
        }

        internal bool IsOwned(string id) => owned.ContainsKey(id); // 실제 장착 가능한 인벤토리 조회

        internal void ListInventory(List<InventoryItem> result) // 모든 캐릭터의 실제 개별 사본
        {
            result.Clear(); // 이전 화면의 목록 제거
            foreach (OwnedPart item in owned.Values) // 현재 공용 인벤토리 순회
            {
                result.Add(new InventoryItem(item.Id, item.Definition, item.Grade, item.Upgrade, item.Owner, FlightProgress.Current.IsBroomLocked(item.Id))); // 개별 상태와 영구 잠금 표시
            }
            result.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id)); // 새로고침에도 같은 순서 유지
        }

        internal void ListCraftable(List<FlightBroomPart> result) // 획득 이력으로 제작법이 열린 세트 종류
        {
            result.Clear(); // 이전 제작 목록 제거
            foreach (FlightBroomPart part in catalog.Parts) // 실제 기획된 카탈로그 순회
            {
                if (part != null && CanCraft(part.id, 1)) // 일반 등급 이상의 제작법이 열린 종류
                {
                    result.Add(part); // 획득한 종류만 선택 가능
                }
            }
        }

        internal bool CanCraft(string definitionId, int grade) // 보유 이력에 따른 종류·등급 제작 자격
        {
            FlightBroomPart definition = catalog.Find(definitionId); // 실제 카탈로그의 부품 종류
            return definition != null && !definition.starter && (!string.IsNullOrEmpty(definition.setId) || definition.stocked) && grade >= 1 && grade <= 5 // 기획된 세트·상점 부품과 유효 등급
                && (FlightProgress.Current.HasBroomRecipe(definitionId, grade) || (grade <= definition.grade && (definition.ownedCopies > 0 || (definition.stocked && FlightProgress.Current.OwnsBroomPart(definition.id + ":1"))))); // 신규 획득·기존 보유·기존 구매의 해금
        }

        internal string CraftItem(string definitionId, int grade) // 확정 제작한 부품을 현재 인벤토리에 추가
        {
            string id = FlightProgress.Current.CraftBroom(catalog, definitionId, grade); // 해금과 재화를 검사한 원자 저장
            if (id != null) // 실제 저장에 성공한 경우
            {
                AddOwned(catalog.Find(definitionId), id, grade); // 다음 씬 전에도 실제 사본 사용
            }
            return id; // 저장한 고유 사본 또는 실패
        }

        internal bool SetItemLocked(string id, bool locked) // 현재 보유한 개별 부품의 분해 잠금
        {
            if (!owned.TryGetValue(id, out OwnedPart item) || item.Definition.starter || (string.IsNullOrEmpty(item.Definition.setId) && !item.Definition.stocked)) // 존재하지 않거나 지원하지 않는 부품
            {
                return false; // 다른 아이템의 임의 저장 금지
            }
            return (item.Definition.ownedCopies > 0 || item.Definition.stocked) && id.StartsWith(item.Definition.id + ":", StringComparison.Ordinal) // 기존 버전의 원래 사본
                ? FlightProgress.Current.SetLegacyBroomLock(id, locked) // 기존 ID의 영구 잠금
                : FlightProgress.Current.SetBroomLock(id, locked); // 새 획득·제작 사본의 잠금
        }

        internal bool DismantleItem(string id) // 실제 장착 상태를 검사한 개별 분해
        {
            if (!owned.TryGetValue(id, out OwnedPart item) || item.Definition.starter || (string.IsNullOrEmpty(item.Definition.setId) && !item.Definition.stocked)) // 시작 부품과 알 수 없는 종류 보호
            {
                return false; // 소유와 재화 유지
            }
            bool legacy = (item.Definition.ownedCopies > 0 || item.Definition.stocked) && id.StartsWith(item.Definition.id + ":", StringComparison.Ordinal); // 이전 버전에서 지급하거나 구매한 고정 사본
            bool saved = legacy // 개별 ID의 실제 획득 방식
                ? FlightProgress.Current.DismantleLegacyBroom(id, item.Definition, item.Grade, item.Owner >= 0) // 고정 사본의 영구 삭제 표시
                : FlightProgress.Current.DismantleBroom(id, item.Owner >= 0); // 새 사본의 삭제와 반환
            if (saved) // 디스크 저장이 성공한 경우에만
            {
                owned.Remove(id); // 현재 인벤토리에서도 정확한 사본 제거
            }
            return saved; // 원자 정산의 실제 결과
        }

        internal bool Purchase(FlightBroomPart definition) // 전용 재고 한 개를 골드로 구입
        {
            string id = definition.id + ":1"; // 같은 상점 등록 부품의 개별 ID
            if (!definition.stocked || owned.ContainsKey(id) || !FlightProgress.Current.BuyBroomPart(id, definition.grade)) // 현재 재고·보유·재화 검증
            {
                return false; // 가격과 장착 변경 없이 이전 화면
            }
            AddOwned(definition, 1); // 같은 실행에서도 장착 가능
            return true; // 실제 영구 저장 성공
        }

        internal PartOption? CurrentPart(FlightBroomSlot slot) // 공방이 선택한 장착 부품 조회
        {
            OwnedPart item = equipped[(int)ActivePilot, (int)slot]; // 현재 조종사의 실제 장착 슬롯
            return item == null ? null : new PartOption(item.Id, item.Definition.displayName, item.Grade, item.Upgrade, item.Owner, Mathf.Max(1, item.Definition.ownedCopies)); // 구입한 사본을 포함한 보유 정보
        }

        internal bool Enhance(FlightBroomSlot slot) // 현재 장착 부품의 단계와 비용 갱신
        {
            OwnedPart item = equipped[(int)ActivePilot, (int)slot]; // 현재 공방에서 고른 실제 부품
            if (item == null || !FlightProgress.Current.UpgradeBroomPart(item.Id, item.Upgrade)) // 재화 및 최대 +5 검증과 저장
            {
                return false; // 미장착·저장 문제·재화 부족에서 변동 없음
            }
            item.Upgrade++; // 실제 구입한 개별 아이템의 다음 단계
            ApplyLoadout(); // 같은 실행에서도 능력치와 외형 갱신
            return true; // 저장된 강화 적용
        }

        public bool TryEquip(FlightBroomPilot pilot, FlightBroomSlot slot, string ownedId) // 지정 부위로 개별 부품 교체
        {
            int pilotIndex = (int)pilot; // 캐릭터 배열 위치
            int slotIndex = (int)slot; // 부위 배열 위치
            if (!HasSlot(pilotIndex, slotIndex) || string.IsNullOrEmpty(ownedId) || !owned.TryGetValue(ownedId, out OwnedPart item)) // 유효한 캐릭터·슬롯·보유 ID 확인
            {
                return false; // 알 수 없는 부품 요청 거절
            }

            if (item.Definition.slot != slot || (item.Owner != -1 && item.Owner != pilotIndex)) // 다른 부위 또는 타 캐릭터 장착 여부
            {
                return false; // 슬롯 불일치와 중복 장착 방지
            }

            if (equipped[pilotIndex, slotIndex] == item) // 이미 같은 슬롯에 장착한 부품
            {
                return true; // 기존 부품과 표시 상태 유지
            }

            Equip(item, pilotIndex, slotIndex); // 이전 부품 해제와 신규 장착
            if (pilot == ActivePilot) // 현재 비행체의 변경만 출력
            {
                ApplyLoadout(); // 외형과 비행 수치 재계산
            }

            return true; // 장착 성공
        }

        public bool Unequip(FlightBroomPilot pilot, FlightBroomSlot slot) // 캐릭터의 슬롯 비우기
        {
            int pilotIndex = (int)pilot; // 캐릭터 배열 위치
            int slotIndex = (int)slot; // 부위 배열 위치
            if (!HasSlot(pilotIndex, slotIndex) || equipped[pilotIndex, slotIndex] == null) // 없는 캐릭터·부위 또는 빈 슬롯
            {
                return false; // 해제할 부품이 없는 요청 거절
            }

            Equip(null, pilotIndex, slotIndex); // 부품을 미장착으로 반환
            if (pilot == ActivePilot) // 현재 눈에 보이는 조합
            {
                ApplyLoadout(); // 제거된 부품의 외형과 수치 반영
            }

            return true; // 장착 해제 성공
        }

        public string GetEquippedId(FlightBroomPilot pilot, FlightBroomSlot slot) // 슬롯의 개별 부품 조회
        {
            int pilotIndex = (int)pilot; // 캐릭터 배열 위치
            int slotIndex = (int)slot; // 부위 배열 위치
            return HasSlot(pilotIndex, slotIndex) ? equipped[pilotIndex, slotIndex]?.Id : null; // 장착 ID 또는 빈 슬롯
        }

        public float GetBonus(FlightBroomStat stat) // 활성 조합의 수치 합
        {
            RefreshConditions(); // 집중이나 마력 경계 변경의 즉시 반영
            return bonuses[(int)stat] + (stat == FlightBroomStat.Speed ? DashSpeedFor(-1, null) : 0f); // 세트·개별 효과와 실제 한시 속도
        }

        internal void OnDashFinished() // 실제 돌파 종료의 한시 비행 속도
        {
            if (HasSpecial("storm", FlightBroomSlot.Ornament, 5) && Time.time >= dashSpeedReadyAt && dashSpeedTriggers < 25) // 전설 폭풍 장식의 실제 발동 제한
            {
                dashSpeedUntil = Time.time + 1f; // 실제 종료 뒤 1초 동안 속도 증가
                dashSpeedReadyAt = Time.time + 5f; // 장식의 내부 재사용 간격
                dashSpeedTriggers++; // 이번 출격의 최대 25회 기록
            }
        }

        internal void ResetBroomRun() // 새 출격의 한시 장비 효과 초기화
        {
            dashSpeedUntil = 0f; // 이전 출격의 속도 증가 제거
            dashSpeedReadyAt = 0f; // 이전 출격의 장식 쿨타임 제거
            dashSpeedTriggers = 0; // 이전 출격의 발동 횟수 제거
        }

        private float DashSpeedFor(int replaceSlot, OwnedPart candidate) // 현재 또는 교체 후보의 한시 비행 속도
        {
            int slot = (int)FlightBroomSlot.Ornament; // 한시 효과를 가진 실제 장식 부위
            OwnedPart item = slot == replaceSlot ? candidate : equipped[(int)ActivePilot, slot]; // 교체 후보 또는 현재 장착
            return Time.time < dashSpeedUntil && item != null && item.Definition.setId == "storm" && item.Grade >= 5 ? 0.5f : 0f; // 유효 시간과 실제 전설 장식 확인
        }

        private void RefreshConditions() // 실시간 조건이 바뀐 경우에만 보너스 재계산
        {
            bool focused = flight != null && flight.IsFocusHeld; // 실제 집중 비행 여부
            bool lowMana = skills != null && skills.IsLowMana; // 실제 소수 마력이 30 이하
            if (focused != bonusFocus || lowMana != bonusLowMana) // 마지막 계산과 다른 조건
            {
                bonusFocus = focused; // 새 집중 조건 기록
                bonusLowMana = lowMana; // 새 마력 조건 기록
                CalculateBonuses((int)ActivePilot, -1, null, bonuses); // 현재 장착의 조건부 효과 재계산
            }
        }

        internal float ElementSpeedBonus(string element) // 확정된 속성 세트의 탄속 보정
        {
            return element == "fire" ? (SetPieces("flame") >= 2 ? 5f : 0f) + FirePartBonus(false) : 0f; // 화염 세트와 개별 탄속을 별도 합산
        }

        internal float ElementRangeBonus(string element) // 확정된 속성별 개별 사거리
        {
            return element == "fire" ? FirePartBonus(true) : 0f; // 화염 부품만 해당 속성에 적용
        }

        private float FirePartBonus(bool range) // 화염 속성의 같은 효과 중 상위 두 개
        {
            float first = 0f; // 현재까지 가장 큰 해당 속성 효과
            float second = 0f; // 현재까지 두 번째로 큰 해당 속성 효과
            for (int slot = 0; slot < equipped.GetLength(1); slot++) // 실제 조종사의 여덟 부위
            {
                OwnedPart item = equipped[(int)ActivePilot, slot]; // 실제 장착한 개별 사본
                if (item == null || item.Definition.setId != "flame") // 화염 세트가 아닌 부품
                {
                    continue; // 다른 속성의 효과 적용 금지
                }
                bool active = range // 요청한 속성 사거리 또는 속도
                    ? item.Definition.slot == FlightBroomSlot.Body && item.Grade >= 3 || item.Definition.slot == FlightBroomSlot.Ornament && item.Grade >= 5 // 잿불 몸체와 불사조 장식의 사거리
                    : item.Definition.slot == FlightBroomSlot.Grip && item.Grade >= 5 || item.Definition.slot == FlightBroomSlot.Tip && item.Grade >= 3; // 화승 손잡이와 용화 선단부의 탄속
                if (active) // 해당 희귀도에서 실제 열린 속성 효과
                {
                    second = first; // 기존 최대를 두 번째로 이동
                    first = 5f; // 모두 같은 기획 가산량 5%p
                }
            }
            return first + second; // 같은 속성과 능력치의 최대 두 효과
        }

        internal int SetPieces(string setId) // 활성 캐릭터의 실제 세트별 장착 수
        {
            int count = 0; // 같은 세트의 서로 다른 부위 수
            for (int slot = 0; slot < equipped.GetLength(1); slot++) // 실제 여덟 장착 부위
            {
                if (equipped[(int)ActivePilot, slot]?.Definition.setId == setId) // 같은 세트의 실제 장착
                {
                    count++; // 희귀도와 무관한 부위 한 개
                }
            }
            return count; // 현재 조종사의 실제 장착 수
        }

        internal bool HasSpecial(string setId, FlightBroomSlot slot, int minimumGrade) // 조건부 효과의 실제 부품과 희귀도
        {
            OwnedPart item = equipped[(int)ActivePilot, (int)slot]; // 현재 캐릭터의 정확한 부위
            return item != null && item.Definition.setId == setId && item.Grade >= minimumGrade; // 해당 세트와 희귀도가 모두 충족
        }

        internal float DashDurationBonus() // 실제 돌파 지속시간의 동일 효과 상위 두 개
        {
            return LargestTwo( // 기획된 세 부품의 지속시간 증가
                HasSpecial("storm", FlightBroomSlot.Propulsion, 3) ? 0.05f : 0f, // 질풍 추진부의 희귀 효과
                HasSpecial("flame", FlightBroomSlot.Stabilizer, 5) ? 0.03f : 0f, // 열풍 안정기의 전설 효과
                HasSpecial("thunder", FlightBroomSlot.Propulsion, 5) ? 0.03f : 0f); // 뇌전 추진부의 전설 효과
        }

        internal float DashSpeedBonus() // 실제 돌파 속도의 기획 퍼센트포인트
        {
            return HasSpecial("starlight", FlightBroomSlot.Propulsion, 3) ? 10f : 0f; // 유성 추진부의 희귀 이상 효과
        }

        internal float DashCooldownReduction() // 실제 돌파 재사용 감소의 동일 효과 상위 두 개
        {
            return LargestTwo( // 기획된 네 부품의 고정 초 감소
                HasSpecial("storm", FlightBroomSlot.Ornament, 3) ? 0.2f : 0f, // 돌풍 장식의 희귀 효과
                HasSpecial("hawk", FlightBroomSlot.Propulsion, 5) ? 0.2f : 0f, // 급강하 추진부의 전설 효과
                HasSpecial("shadow", FlightBroomSlot.Propulsion, 3) ? 0.2f : 0f, // 야행 추진부의 희귀 효과
                HasSpecial("thunder", FlightBroomSlot.Grip, 5) ? 0.15f : 0f); // 번개 손잡이의 전설 효과
        }

        private static float LargestTwo(params float[] values) // 같은 조건과 능력치의 큰 두 효과
        {
            float first = 0f; // 가장 큰 효과
            float second = 0f; // 두 번째로 큰 효과
            foreach (float value in values) // 실제 장착한 후보들
            {
                if (value > first) // 현재 최댓값을 넘는 효과
                {
                    second = first; // 기존 최댓값을 두 번째로 보존
                    first = value; // 더 큰 효과 기록
                }
                else if (value > second) // 두 번째보다 큰 효과
                {
                    second = value; // 같은 ID의 두 번째 효과 기록
                }
            }
            return first + second; // 승인된 상위 두 효과만 합산
        }

        internal string GetEquippedName(FlightBroomSlot slot) // 출격 요약의 실제 장착 이름
        {
            return equipped[(int)ActivePilot, (int)slot]?.Definition.displayName ?? "미장착"; // 준비 화면과 같은 장착 상태
        }

        internal void ListParts(FlightBroomSlot slot, List<PartOption> result) // 선택한 슬롯에 장착할 수 있는 종류 목록
        {
            result.Clear(); // 앞서 선택한 슬롯 결과 제거
            foreach (OwnedPart item in owned.Values) // 공용 인벤토리의 모든 보유 개체
            {
                if (item.Definition.slot == slot) // 현재 보고 있는 부위
                {
                    result.Add(new PartOption(item.Id, item.Definition.displayName, item.Grade, item.Upgrade, item.Owner, Mathf.Max(1, item.Definition.ownedCopies))); // 목록의 실제 보유 사본
                }
            }
            result.Sort((left, right) => CompareOptions(left, right)); // 현재 장착·장착 가능·타 캐릭터 순서
        }

        private int CompareOptions(PartOption left, PartOption right) // 장착 상태와 보유 ID 기준 목록 정렬
        {
            int active = (int)ActivePilot; // 현재 캐릭터 번호
            int leftOrder = left.Owner == active ? 0 : left.Owner < 0 ? 1 : 2; // 왼쪽 부품의 장착 상태 순서
            int rightOrder = right.Owner == active ? 0 : right.Owner < 0 ? 1 : 2; // 오른쪽 부품의 장착 상태 순서
            return leftOrder == rightOrder ? string.CompareOrdinal(left.Id, right.Id) : leftOrder.CompareTo(rightOrder); // 동일 상태에서는 개별 ID 순서
        }

        internal bool ConfirmPart(FlightBroomSlot slot, string id) // 조립 화면에서 현재 조합 확정
        {
            bool applied = id == null ? Unequip(ActivePilot, slot) : TryEquip(ActivePilot, slot, id); // 같은 장착 규칙과 외형 적용
            if (applied) // 유효한 변경만 저장
            {
                SaveLoadouts(); // 게임 재시작 후 장착 복원
            }
            return applied; // 적용 결과 표시
        }

        internal string[] PresetParts() // 현재 캐릭터의 여덟 개별 장착 참조
        {
            var result = new string[equipped.GetLength(1)]; // 빈 슬롯을 포함한 저장 배열
            for (int slot = 0; slot < result.Length; slot++) // 실제 부착 부위 순서
            {
                result[slot] = equipped[(int)ActivePilot, slot]?.Id; // 개별 보유 ID 또는 빈 칸
            }
            return result; // 원래 장착을 변경하지 않는 독립 배열
        }

        internal string[] StarterParts() // 현재 캐릭터의 기획된 여덟 시작 부품
        {
            var ids = new string[equipped.GetLength(1)]; // 기본 부위 순서의 개별 사본
            foreach (FlightBroomPart part in catalog.Parts) // 실제 카탈로그의 시작 부품
            {
                if (part.starter && part.pilot == ActivePilot) // 현재 캐릭터 전용 부품
                {
                    ids[(int)part.slot] = part.id + ":1"; // 최초로 지급한 개별 사본
                }
            }
            return ids; // 실제 소유·중복은 적용 전에 검증
        }

        internal bool ShowPresetPreview(MeshRenderer[] preview, string[] ids, out string reason) // 여덟 부품의 외형만 미리 표시
        {
            if (preview == null || preview.Length != equipped.GetLength(1)) // 실제 미리보기 부착점 검사
            {
                reason = "미리보기 상태를 확인하세요."; // 표시할 위치가 없는 경우 안내
                return false; // 실제 장착과 외형 보존
            }
            if (!CanApplyPreset((int)ActivePilot, ids, out _, out reason)) // 누락과 다른 캐릭터 장착 검사
            {
                return false; // 구체적인 부품 문제를 그대로 안내
            }
            for (int slot = 0; slot < preview.Length; slot++) // 여덟 미리보기 부착점
            {
                OwnedPart item = string.IsNullOrEmpty(ids[slot]) ? null : owned[ids[slot]]; // 검증한 개별 사본
                ApplyAppearance(preview[slot], item, slot); // 실제 기체가 아닌 미리보기 렌더러만 변경
            }
            reason = null; // 성공한 미리보기의 오류 없음
            return true; // 후보 외형 표시 완료
        }

        internal bool CanApplyPreset(int pilot, string[] ids, out float magicAttack, out string reason) // 여덟 부품의 사전 검증과 예상 마법 공격력
        {
            magicAttack = 0f; // 실패할 때 유효한 공격력 없음
            reason = null; // 성공 시 실패 사유 없음
            if (pilot != (int)ActivePilot || ids == null || ids.Length != equipped.GetLength(1)) // 현재 캐릭터와 부위 수 확인
            {
                reason = "현재 캐릭터 또는 부품 슬롯을 확인하세요."; // 적용 불가능한 조합 안내
                return false; // 실제 장착을 바꾸지 않음
            }
            var selected = new OwnedPart[ids.Length]; // 실제로 적용할 독립 후보 배열
            var seen = new HashSet<string>(); // 같은 개별 사본 중복 방지
            for (int slot = 0; slot < ids.Length; slot++) // 여덟 부위의 현재 보유 확인
            {
                string id = ids[slot]; // 해당 위치의 저장 사본
                if (string.IsNullOrEmpty(id)) // 사용자가 저장한 빈 슬롯
                {
                    continue; // 빈 부착부는 그대로 허용
                }
                if (!seen.Add(id)) // 동일한 사본을 두 부위에 지정한 경우
                {
                    reason = "같은 부품을 여러 부위에 장착할 수 없습니다."; // 중복된 조합 안내
                    return false; // 한 부위도 바꾸지 않음
                }
                if (!owned.TryGetValue(id, out OwnedPart item)) // 분해되었거나 보유하지 않은 사본
                {
                    reason = SlotNames[slot] + " 부품을 보유하지 않습니다."; // 사용자가 확인할 실제 부위
                    return false; // 한 부위도 바꾸지 않음
                }
                if ((int)item.Definition.slot != slot) // 다른 위치에 장착하는 부품
                {
                    reason = SlotNames[slot] + " 부위에 사용할 수 없는 부품입니다."; // 부위 불일치 안내
                    return false; // 한 부위도 바꾸지 않음
                }
                if (item.Owner >= 0 && item.Owner != pilot) // 다른 조종사가 쓰는 실제 사본
                {
                    reason = item.Definition.displayName + " 부품을 다른 캐릭터가 장착 중입니다."; // 실제 부품 이름과 충돌 안내
                    return false; // 다른 캐릭터의 장착 보존
                }
                selected[slot] = item; // 검증된 개별 사본 보관
            }
            RefreshConditions(); // 같은 시점의 집중과 마력 조건 확인
            var result = new float[bonuses.Length]; // 기존 장착을 건드리지 않는 예상 능력치
            CalculateBonuses(pilot, -1, null, result, selected); // 여덟 후보와 세트·조건부 효과 동시 합산
            magicAttack = Mathf.Max(0f, 100f + result[(int)FlightBroomStat.MagicPower]); // 실제 소수 마법 공격력
            return true; // 모든 부품과 예상 공격력 확인
        }

        internal bool ComparePreset(int pilot, string[] ids, float[] difference, out string reason) // 전체 프리셋과 현재 조합의 능력치 비교
        {
            if (difference == null || difference.Length != bonuses.Length || !CanApplyPreset(pilot, ids, out _, out reason)) // 후보와 실제 능력치 배열 검증
            {
                reason = "비교할 부품 조합을 확인하세요."; // 유효하지 않은 후보 안내
                return false; // 현재 장착과 출력 보존
            }
            var selected = new OwnedPart[ids.Length]; // 모든 부위의 검증된 후보
            for (int slot = 0; slot < ids.Length; slot++) // 여덟 개별 부품
            {
                selected[slot] = string.IsNullOrEmpty(ids[slot]) ? null : owned[ids[slot]]; // 실제 보유 사본 또는 빈 칸
            }
            var proposed = new float[bonuses.Length]; // 후보 조합의 전체 수치
            CalculateBonuses(pilot, -1, null, proposed, selected); // 세트와 현재 조건까지 포함한 계산
            for (int i = 0; i < proposed.Length; i++) // 실제 능력치 항목별 차이
            {
                difference[i] = proposed[i] - bonuses[i]; // 현재 조합 대비 증가와 감소
            }
            reason = null; // 성공한 비교의 오류 없음
            return true; // 전체 능력치 비교 완료
        }

        internal bool ApplyPresetParts(int pilot, string[] ids) // 검증한 여덟 부품의 동시 교체와 현재 장착 저장
        {
            if (!CanApplyPreset(pilot, ids, out _, out _)) // 적용 직전에도 소유·충돌 재검사
            {
                return false; // 실제 장착과 저장 보존
            }
            for (int slot = 0; slot < ids.Length; slot++) // 현재 캐릭터의 기존 부품 반환
            {
                Equip(null, pilot, slot); // 다른 캐릭터의 장착은 변경하지 않음
            }
            for (int slot = 0; slot < ids.Length; slot++) // 이미 검증한 전체 후보 확정
            {
                Equip(string.IsNullOrEmpty(ids[slot]) ? null : owned[ids[slot]], pilot, slot); // 개별 사본 또는 빈 슬롯
            }
            ApplyLoadout(); // 여덟 부위의 외형과 효과를 한 번에 갱신
            SaveLoadouts(); // 현재 장착 상태의 영구 저장
            return true; // 전체 부품 적용 완료
        }

        internal float ComparePart(FlightBroomSlot slot, string id, FlightBroomStat stat) // 후보 장착 시 활성 조합과의 차이
        {
            RefreshConditions(); // 현재 집중과 마력 조건을 미리보기에도 적용
            OwnedPart candidate = id == null ? null : owned[id]; // 선택한 후보 또는 해제
            var proposed = new float[bonuses.Length]; // 교체 후의 별도 계산 결과
            CalculateBonuses((int)ActivePilot, (int)slot, candidate, proposed); // 세트 활성화까지 포함한 후보 능력치
            float temporary = stat == FlightBroomStat.Speed ? DashSpeedFor((int)slot, candidate) - DashSpeedFor(-1, null) : 0f; // 현재와 교체 후보의 한시 속도 차이
            return proposed[(int)stat] - bonuses[(int)stat] + temporary; // 실제 현재 조합과 교체 후 차이
        }

        internal void ShowPreview(MeshRenderer[] preview, FlightBroomSlot slot, string id) // 미리보기 전용 렌더러에 부품 외형 적용
        {
            for (int i = 0; i < preview.Length; i++) // 모든 미리보기 부착점
            {
                OwnedPart item = i == (int)slot ? (id == null ? null : owned[id]) : equipped[(int)ActivePilot, i]; // 현재 후보를 포함한 시험 조합
                ApplyAppearance(preview[i], item, i); // 본체를 건드리지 않는 외형 적용
            }
        }

        internal void SelectPilot(FlightBroomPilot pilot) // 기존 캐릭터 전환에 장착 조합 연결
        {
            ActivePilot = pilot; // 현재 캐릭터 갱신
            ApplyLoadout(); // 선택 캐릭터의 외형과 수치 복원
        }

        private bool HasSlot(int pilot, int slot) // 배열 경계 검사
        {
            return pilot >= 0 && pilot < equipped.GetLength(0) && slot >= 0 && slot < equipped.GetLength(1); // 지원하는 캐릭터와 부위
        }

        private void Equip(OwnedPart item, int pilot, int slot) // 단일 슬롯의 소유자 갱신
        {
            OwnedPart previous = equipped[pilot, slot]; // 기존 장착 부품
            if (previous != null) // 이전 부품 반환
            {
                previous.Owner = -1; // 다른 캐릭터에 장착 가능
            }

            equipped[pilot, slot] = item; // 새 부품 또는 빈 슬롯
            if (item != null) // 신규 부품 소유자 기록
            {
                item.Owner = pilot; // 현재 장착 캐릭터
            }
        }

        private void ApplyLoadout() // 활성 캐릭터의 부착점과 효과 갱신
        {
            bonusFocus = flight != null && flight.IsFocusHeld; // 실제 장착 시점의 집중 조건
            bonusLowMana = skills != null && skills.IsLowMana; // 실제 장착 시점의 마력 조건
            int pilot = (int)ActivePilot; // 활성 캐릭터 배열 위치
            for (int slot = 0; slot < equipped.GetLength(1); slot++) // 전체 장착 슬롯 순회
            {
                OwnedPart item = equipped[pilot, slot]; // 슬롯의 개별 부품
                ApplyAppearance(mounts[slot], item, slot); // 실전 부착점 외형 적용
            }
            CalculateBonuses(pilot, -1, null, bonuses); // 현재 장착의 부품과 세트 효과를 중복 없이 계산
        }

        private void CalculateBonuses(int pilot, int replaceSlot, OwnedPart candidate, float[] result, OwnedPart[] proposed = null) // 현재 장착 또는 전체 후보의 순수 수치 계산
        {
            Array.Clear(result, 0, result.Length); // 이전 계산값을 누적하지 않음
            var sets = new Dictionary<string, int>(); // 이 캐릭터의 세트별 실제 장착 수
            var passive = new FlightBroomPassiveEffects(); // 같은 능력치의 상위 두 개별 상시 효과
            for (int slot = 0; slot < equipped.GetLength(1); slot++) // 서로 다른 여덟 부위
            {
                OwnedPart item = proposed != null ? proposed[slot] : slot == replaceSlot ? candidate : equipped[pilot, slot]; // 전체 후보·단일 교체·현재 장착
                if (item == null) // 미장착 상태
                {
                    continue; // 부품과 세트 수에 포함하지 않음
                }
                FlightBroomPart part = item.Definition; // 실제 장착한 부품의 기획 정보
                passive.Include(part, item.Grade); // 희귀도와 부품별 무조건 상시 효과 수집
                result[(int)part.stat] += part.GetValue(item.Grade, item.Upgrade); // 개별 등급과 강화의 기본 보너스
                result[(int)part.penaltyStat] += part.penalty; // 개별 부품의 고정 불이익
                if (!string.IsNullOrEmpty(part.setId)) // 세트에 소속된 실제 부품
                {
                    sets.TryGetValue(part.setId, out int count); // 같은 세트의 기존 장착 수
                    sets[part.setId] = count + 1; // 희귀도와 무관하게 한 부위 추가
                }
            }
            passive.AddTo(result, bonusFocus, bonusLowMana); // 실제 조건별 개별 효과 최대 두 개 반영
            foreach (KeyValuePair<string, int> set in sets) // 동시에 장착한 모든 세트
            {
                if (set.Value < 2) // 두 부품 미만인 세트
                {
                    continue; // 세트 효과 없음
                }
                switch (set.Key) // 기획서의 세트별 상시 효과
                {
                    case "storm": // 폭풍의 질주
                        result[(int)FlightBroomStat.Speed] += 1f; // 두 부품의 일반 비행 속도
                        if (set.Value >= 4) // 네 부품의 추가 단계
                        {
                            result[(int)FlightBroomStat.Acceleration] += 4f; // 가속력 증가
                            result[(int)FlightBroomStat.Braking] += 4f; // 제동력 증가
                        }
                        break; // 해당 세트 계산 종료
                    case "guard": // 천공의 수호
                        result[(int)FlightBroomStat.ImpactResistance] += 5f; // 두 부품의 충격 저항
                        break; // 조건부 보호막은 별도 전투 효과
                    case "flow": // 마력의 순환
                        result[(int)FlightBroomStat.Mana] += 10f; // 두 부품의 최대 마력
                        if (set.Value >= 4) // 네 부품의 추가 단계
                        {
                            result[(int)FlightBroomStat.ManaRecovery] += 2f; // 초당 마력 회복량
                        }
                        break; // 해당 세트 계산 종료
                    case "starlight": // 별빛의 조율
                        result[(int)FlightBroomStat.MagicPower] += set.Value >= 4 ? 20f : 8f; // 두 단계의 마법 공격력 누적
                        break; // 해당 세트 계산 종료
                    case "hawk": // 매의 시선
                        result[(int)FlightBroomStat.Range] += 10f; // 두 부품의 마법 사거리
                        if (set.Value >= 4) // 네 부품의 추가 단계
                        {
                            result[(int)FlightBroomStat.BoltSpeed] += 15f; // 탄환 속도 증가
                        }
                        break; // 해당 세트 계산 종료
                    case "shadow": // 그림자의 비행
                        result[(int)FlightBroomStat.Turn] += 30f; // 두 부품의 선회 속도
                        break; // 그레이즈 효과는 별도 전투 효과
                }
            }
        }

        private void ApplyAppearance(MeshRenderer mount, OwnedPart item, int slot) // 실전과 미리보기에서 공유하는 부착점 표시
        {
            mount.enabled = item != null; // 비어 있는 슬롯 숨김
            if (item != null) // 실제 부품 외형
            {
                FlightBroomPart part = item.Definition; // 공통 부품 재질과 배율
                mount.sharedMaterial = part.material != null ? part.material : defaultMaterials[slot]; // 지정 재질 우선
                mount.transform.localScale = Vector3.Scale(defaultScales[slot], part.scale); // 원본 크기에서 계산한 배율
            }
        }

        private void SaveLoadouts() // 확정 장착 형태 저장
        {
            string key = SaveKey; // 현재 실행의 환경별 저장 키
            if (string.IsNullOrEmpty(key)) // 별도 저장을 쓰지 않는 시험
            {
                return; // 전역 사용자 설정 보존
            }
            string[] ids = new string[equipped.Length]; // 캐릭터별 전체 슬롯
            int slots = equipped.GetLength(1); // 한 캐릭터의 슬롯 수
            for (int i = 0; i < ids.Length; i++) // 캐릭터와 슬롯을 고정 순서로 저장
            {
                ids[i] = equipped[i / slots, i % slots]?.Id ?? string.Empty; // 빈 슬롯도 기록
            }
            PlayerPrefs.SetString(key, JsonUtility.ToJson(new LoadoutData { ids = ids })); // 전체 장착을 한 값으로 저장
            PlayerPrefs.Save(); // 실전 재실행에 대비한 즉시 기록
        }

        private void RestoreLoadouts() // 장착 ID 검증 후 한 번에 복원
        {
            string key = SaveKey; // 실제 실행 또는 격리 테스트의 저장 키
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key)) // 첫 실행과 독립 검증
            {
                return; // 기본 시작 장착 유지
            }

            LoadoutData data; // 복원 전 검사할 원본
            try // 손상된 저장도 게임 시작 허용
            {
                data = JsonUtility.FromJson<LoadoutData>(PlayerPrefs.GetString(key)); // 장착 ID 목록 읽기
            }
            catch (ArgumentException) // 잘못된 JSON 형식
            {
                return; // 유효한 기본 조합 유지
            }

            if (data?.ids == null || data.ids.Length != equipped.Length) // 전체 슬롯 배열 확인
            {
                return; // 일부 장착만 복원되는 상황 방지
            }
            int slots = equipped.GetLength(1); // 캐릭터별 슬롯 크기
            OwnedPart[] restored = new OwnedPart[data.ids.Length]; // 아직 게임에 적용하지 않은 안전한 복원본
            HashSet<string> seen = new(); // 중복된 개별 보유 부품 확인
            for (int i = 0; i < restored.Length; i++) // 저장한 각 슬롯의 부품 검사
            {
                string id = data.ids[i]; // 개별 장착 ID
                if (string.IsNullOrEmpty(id)) // 비워 둔 장착 슬롯
                {
                    continue; // 빈 슬롯의 정상 복원
                }
                if (!owned.TryGetValue(id, out OwnedPart item) || (int)item.Definition.slot != i % slots || !seen.Add(id)) // 미보유·슬롯 불일치·중복 장착 검사
                {
                    return; // 오류 없는 기존 기본 조합 유지
                }
                restored[i] = item; // 유효한 부품 복원 후보
            }
            for (int i = 0; i < restored.Length; i++) // 원래 보유자의 장착 해제
            {
                Equip(null, i / slots, i % slots); // 다른 캐릭터로 옮겨진 부품의 이전 상태 제거
            }
            for (int i = 0; i < restored.Length; i++) // 검증된 조합을 일괄 반영
            {
                Equip(restored[i], i / slots, i % slots); // 복원된 부품과 현재 소유자
            }
        }
    }
}
