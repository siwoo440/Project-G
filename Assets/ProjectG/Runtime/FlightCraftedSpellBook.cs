using System; // 장착과 검증 환경 저장 키
using UnityEngine; // 실전 카드와 조합 저장

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCraftedSpellBook : MonoBehaviour // 제작 마법의 저장과 좌우 편성
    {
        internal enum Upgrade // 출격 중 제작 마법의 수치 강화
        {
            Damage, Cooldown, Mana, Count, Pierce, Width, Duration // 기획서의 일곱 가지 강화
        }

        [Serializable] // 현재 저장 마법과 장착 슬롯
        private sealed class SaveData
        {
            public int version; // 빈 칸과 시험 덱을 구분하는 저장 버전
            public string recipe; // 편집 화면에 복원할 저장된 조합
            public string left; // 왼쪽 전투 슬롯의 조합 ID
            public string right; // 오른쪽 전투 슬롯의 조합 ID
            public GalleryEntry[] gallery; // 여러 이름과 복제본을 보존하는 도감
            public int priorityLeft; // 첫 슬롯의 유지 우선순위
            public int priorityRight; // 둘째 슬롯의 유지 우선순위
            public int orderLeft; // 첫 슬롯의 최근 편성 순서
            public int orderRight; // 둘째 슬롯의 최근 편성 순서
        }

        [Serializable] // 이름이 다른 같은 구성도 별도로 보존
        internal sealed class GalleryEntry // 실제 마법 도감의 한 장
        {
            public string key; // 복제본을 구분하는 고유 식별자
            public string name; // 사용자가 정한 마법 이름
            public string recipe; // 쿨타임과 강화를 공유하는 구성 ID
        }

        [SerializeField] private FlightCraftedSpellCaster caster; // 실제 전투에서 쓰는 공통 발사기
        [SerializeField] private FlightSkillController skills; // 스킬과 같은 실제 마력
        [SerializeField] private FlightBroomEquipment equipment; // 마법 공격력에 포함되는 빗자루 효과
        [SerializeField] private FlightEnemyEncounter encounter; // 실제 출격 시작 여부
        [SerializeField] private Texture2D[] elements; // 기존 카드의 5개 속성 이미지
        private readonly CraftedSpellRecipe[] lanes = new CraftedSpellRecipe[2]; // 좌우 편성 ID 복원본
        private readonly CraftedSpellRecipe[] runningLanes = new CraftedSpellRecipe[2]; // 이번 출격의 고정 편성
        private readonly CraftedSpellRecipe[] desiredRun = new CraftedSpellRecipe[2]; // 자동 복구할 이번 출격의 원래 편성
        private readonly bool[] suspended = new bool[2]; // 자동 해제된 슬롯만 복구할 표시
        private readonly int[] priorities = { 1, 1 }; // 저장된 논리 슬롯의 유지 우선순위
        private readonly int[] equipOrder = { 0, 1 }; // 같은 우선순위에서 최근 편성한 순서
        private readonly int[] runPriorities = new int[2]; // 이번 출격의 고정 슬롯 우선순위
        private readonly int[] runOrder = new int[2]; // 이번 출격의 고정 편성 순서
        private int nextOrder = 1; // 다음 수동 편성의 순서 번호
        private bool runConfigured; // 실제 슬롯과 한도 검사 준비 여부
        private CraftedSpellRecipe trainingOverride; // 저장하지 않은 자유 훈련 조합
        private bool hasTrainingOverride; // 빈 미리보기 조합도 저장 편성과 분리
        private readonly System.Collections.Generic.Dictionary<string, int[]> growth = new(); // 구성 ID마다 공유하는 임시 강화
        private readonly System.Collections.Generic.List<GalleryEntry> gallery = new(); // 저장된 마법의 독립 목록
        private const string UserKey = "ProjectG.Flight.Crafted.v1"; // 제작 마법의 버전별 저장 키
        private static string SaveKey => Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY") ?? (FlightBindings.UsesPreferences ? UserKey : null); // 격리 검증과 사용자 저장 분리

        public CraftedSpellRecipe Saved { get; private set; } // 편집기로 불러올 가장 최근 조합
        internal int GalleryCount => gallery.Count; // 실제 저장된 마법 카드 수
        internal int MagicBudget => 100 + Mathf.RoundToInt(equipment.GetBonus(FlightBroomStat.MagicPower)); // 실전 편성 기준과 장착 효과
        internal float MagicAttack => Mathf.Max(0f, 100f + equipment.GetBonus(FlightBroomStat.MagicPower)); // 현재 장비를 포함한 실제 마법 공격력
        internal float ProjectileSpeedBonus => equipment.GetBonus(FlightBroomStat.BoltSpeed); // 이동형 제작 탄환의 합산 탄속 보정
        internal float ProjectileRangeBonus => equipment.GetBonus(FlightBroomStat.Range); // 이동형 제작 탄환의 합산 사거리 보정
        internal bool AppliesNow => encounter == null; // 출격 준비 씬에서만 현재 편성 변경

        private void Awake() // 저장된 제작 마법과 편성 읽기
        {
            caster?.BindGrowth(this); // 실전 발사기가 있을 때 출격별 강화 연결
            lanes[0] = CraftedSpellPresets.All[0].Recipe; // 기획서의 기본 파이어 볼트
            lanes[1] = CraftedSpellPresets.All[7].Recipe; // 기획서의 기본 가시 산탄
            string key = SaveKey; // 현재 실행 환경의 저장 키
            if (!string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key)) // 이전 저장이 있는 경우
            {
                try // 손상된 저장은 기본 편성 보존
                {
                    SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(key)); // 슬롯과 저장 조합 읽기
                    if (data != null) // 읽을 수 있는 저장만 복원
                    {
                        if (data.version >= 4) // 유지 우선순위가 포함된 저장
                        {
                            priorities[0] = Mathf.Clamp(data.priorityLeft, 0, 2); // 첫 슬롯의 저장 우선순위
                            priorities[1] = Mathf.Clamp(data.priorityRight, 0, 2); // 둘째 슬롯의 저장 우선순위
                            equipOrder[0] = Mathf.Max(0, data.orderLeft); // 첫 슬롯의 저장 편성 순서
                            equipOrder[1] = Mathf.Max(0, data.orderRight); // 둘째 슬롯의 저장 편성 순서
                            nextOrder = Mathf.Max(equipOrder[0], equipOrder[1]); // 다음 편성 순서의 기준
                        }
                        if (CraftedSpellRules.TryRead(data.recipe, out CraftedSpellRecipe saved)) // 유효한 편집기 조합
                        {
                            Saved = saved; // 최근 저장한 구성 복원
                        }
                        if (data.gallery != null) // 새 저장 형식의 도감 목록
                        {
                            foreach (GalleryEntry entry in data.gallery) // 저장 카드를 순서대로 복원
                            {
                                if (entry != null) // 비어 있지 않은 기존 항목
                                {
                                    entry.key = string.IsNullOrEmpty(entry.key) ? Guid.NewGuid().ToString("N") : entry.key; // 구형 또는 손상된 식별자의 임시 복구
                                    if (string.IsNullOrWhiteSpace(entry.name) && CraftedSpellRules.TryRead(entry.recipe, out CraftedSpellRecipe valid)) // 읽을 수 있는 무명 항목
                                    {
                                        entry.name = NameOf(valid); // 비어 있는 이름의 기본 표시
                                    }
                                    gallery.Add(entry); // 알 수 없는 구성도 원래 순서와 함께 보존
                                }
                            }
                        }
                        else if (Saved != null) // 단일 마법만 있던 이전 저장
                        {
                            gallery.Add(NewEntry(Saved, NameOf(Saved))); // 기존 조합을 도감 한 장으로 이관
                        }
                        bool legacyTrialDeck = data.version < 2 && string.IsNullOrEmpty(data.left) && string.IsNullOrEmpty(data.right); // 이전 저장의 기본 시험 덱
                        if (!legacyTrialDeck) // 이전 시험 덱 이외의 명시적인 편성
                        {
                            lanes[0] = CraftedSpellRules.TryRead(data.left, out CraftedSpellRecipe left) ? left : null; // 첫 칸 또는 빈 칸 복원
                            lanes[1] = CraftedSpellRules.TryRead(data.right, out CraftedSpellRecipe right) ? right : null; // 둘째 칸 또는 빈 칸 복원
                        }
                    }
                }
                catch (ArgumentException) // 손상된 저장 텍스트
                {
                    Saved = null; // 안전한 기본 편성 사용
                }
            }
            if (AppliesNow) // 아직 출격하지 않은 준비 화면
            {
                ApplyDuringPreparation(); // 해금된 편성만 실제 준비 화면에 반영
            }
            else // 실제 출격 시작 시점
            {
                AssignRunLanes(); // 두 칸의 무작위 좌우 배정
            }
        }

        private void Update() // 마법 공격력의 변경을 같은 출격의 슬롯에 반영
        {
            ReconcileBudget(); // 능력치 합산 뒤 자동 해제와 복구
        }

        internal CraftedSpellRecipe Slot(int index) // 실제 카드의 조합
        {
            return lanes[index]; // 현재 좌우 칸의 유효 마법
        }

        internal CraftedSpellRecipe ActiveSlot(int index) // 이번 출격의 실제 사용 마법
        {
            ReconcileBudget(); // 장비 변경 직후에도 실제 한도 반영
            return runningLanes[index]; // 출격 시작 전 갱신한 고정 편성
        }

        internal void Save(CraftedSpellRecipe recipe) // 편집기에서 검증한 조합 보존
        {
            if (!ResearchUnlocked(recipe)) // 아직 해금되지 않은 구성 요소
            {
                return; // 기존에 저장한 조합 보존
            }
            Saved = recipe; // 마법 도감의 시험 저장 위치
            if (!gallery.Exists(entry => entry.recipe == recipe.Id)) // 같은 구성의 기존 저장이 없는 경우
            {
                gallery.Add(NewEntry(recipe, NameOf(recipe))); // 실제 검증한 조합을 새 도감에 추가
            }
            Write(); // 편집 화면을 떠나도 내용 복원
        }

        internal GalleryEntry GalleryAt(int index) // 도감 순서의 실제 항목
        {
            return index >= 0 && index < gallery.Count ? gallery[index] : null; // 범위 밖의 항목은 없음
        }

        internal bool LoadGallery(int index) // 저장된 카드를 편집 대상으로 선택
        {
            GalleryEntry entry = GalleryAt(index); // 사용자가 선택한 실제 항목
            if (entry == null || !CraftedSpellRules.TryRead(entry.recipe, out CraftedSpellRecipe recipe)) // 삭제되었거나 손상된 항목
            {
                return false; // 현재 편집 저장 보존
            }
            Saved = recipe; // 같은 구성의 저장 편집 대상
            Write(); // 다음 진입에서도 마지막 선택 복원
            return true; // 유효한 도감 선택
        }

        internal bool DuplicateGallery(int index) // 같은 구성의 독립 도감 항목 복제
        {
            GalleryEntry entry = GalleryAt(index); // 복제할 실제 항목
            if (entry == null || !CraftedSpellRules.TryRead(entry.recipe, out CraftedSpellRecipe recipe)) // 유효하지 않은 원본
            {
                return false; // 기존 도감 보존
            }
            gallery.Add(NewEntry(recipe, entry.name + " 복제")); // 같은 구성과 별도 이름의 새 항목
            Write(); // 두 항목을 함께 저장
            return true; // 복제 완료
        }

        internal bool RenameGallery(int index, string name) // 선택한 카드의 표시 이름 변경
        {
            GalleryEntry entry = GalleryAt(index); // 이름을 바꿀 실제 항목
            string trimmed = name?.Trim(); // 앞뒤 공백을 제거한 이름
            if (entry == null || string.IsNullOrEmpty(trimmed) || trimmed.Length > 32) // 빈 이름과 지나치게 긴 이름 차단
            {
                return false; // 이전 이름 유지
            }
            entry.name = trimmed; // 이 복제본에만 표시할 이름
            Write(); // 편성과 같은 저장에 반영
            return true; // 이름 변경 완료
        }

        internal bool DeleteGallery(int index) // 편성 참조가 없는 저장 카드 삭제
        {
            GalleryEntry entry = GalleryAt(index); // 삭제할 실제 항목
            if (entry == null || Array.Exists(lanes, lane => lane != null && lane.Id == entry.recipe)) // 현재 편성에서 참조 중인 구성
            {
                return false; // 먼저 명시적으로 편성을 해제하도록 보존
            }
            gallery.RemoveAt(index); // 해당 복제본만 목록에서 제거
            if (Saved != null && Saved.Id == entry.recipe && !gallery.Exists(value => value.recipe == entry.recipe)) // 마지막으로 저장된 같은 구성까지 삭제
            {
                Saved = null; // 더 이상 존재하지 않는 편집 저장 해제
            }
            Write(); // 나머지 카드와 편성을 함께 저장
            return true; // 실제 삭제 완료
        }

        private static GalleryEntry NewEntry(CraftedSpellRecipe recipe, string name) // 새 도감 항목의 독립 식별자
        {
            return new GalleryEntry { key = Guid.NewGuid().ToString("N"), name = name, recipe = recipe.Id }; // 구성은 공유하고 이름과 항목은 독립
        }

        internal bool Equip(int index, CraftedSpellRecipe recipe) // 다음 출격의 좌우 조합 선택
        {
            CraftedSpellRecipe other = AppliesNow ? runningLanes[1 - index] : RunEligible(lanes[1 - index]); // 실제로 유지 중인 다른 칸의 구성
            if (!FlightCraftedSpellCaster.Supports(recipe) || !ResearchUnlocked(recipe) || !CraftedSpellRules.CanEquip(recipe, other, MagicBudget)) // 판정·연구 또는 편성 한도 검증
            {
                return false; // 기존 슬롯 보존
            }
            lanes[index] = recipe; // 지정한 슬롯의 새 마법
            equipOrder[index] = ++nextOrder; // 같은 우선순위에서 최근 편성한 슬롯
            ApplyDuringPreparation(index); // 전투 전 선택 슬롯만 바꾸고 다른 정상 편성 보존
            Write(); // 장착 직후 유효 조합 저장
            return true; // 해당 마법의 실제 편성
        }

        internal int Priority(int index) // 저장된 슬롯의 유지 우선순위
        {
            return (uint)index < 2 ? priorities[index] : 0; // 범위 밖 요청은 최저값
        }

        internal bool SetPriority(int index, int value) // 자동 해제와 복구의 유지 순서
        {
            if ((uint)index >= 2 || value < 0 || value > 2) // 실제 슬롯과 세 단계 범위
            {
                return false; // 기존 편성과 우선순위 보존
            }
            priorities[index] = value; // 지정한 논리 슬롯의 우선순위
            if (AppliesNow) // 출격 전에는 현재 준비에 바로 반영
            {
                runPriorities[index] = value; // 무작위 배정 전의 같은 준비 슬롯
                ReconcileBudget(); // 이미 부족한 한도 다시 확인
            }
            Write(); // 게임을 다시 열어도 우선순위 보존
            return true; // 유효한 사용자 선택
        }

        internal void ClearSlot(int index) // 제작 마법 슬롯 비우기
        {
            lanes[index] = null; // 해당 조합의 사용 중단
            ApplyDuringPreparation(index); // 직접 해제한 슬롯을 복구 대상에서도 제거
            Write(); // 게임 재실행 후에도 편성 유지
        }

        internal FlightCraftedSpellCaster.Profile CurrentStats(CraftedSpellRecipe recipe) // 전투 카드의 실제 시전 수치
        {
            return FlightCraftedSpellCaster.Supports(recipe) ? caster.EffectiveStats(recipe) : default; // 미지원 구성에는 임시 수치 미적용
        }

        internal bool IsSupported(CraftedSpellRecipe recipe) // 확정 수치와 실제 판정의 구현 여부
        {
            return FlightCraftedSpellCaster.Supports(recipe); // 시전 경로와 같은 지원 기준
        }

        internal bool ResearchUnlocked(CraftedSpellRecipe recipe) // 이 계정의 조합 요소 전체 해금 여부
        {
            return FlightProgress.Current.IsRecipeResearchUnlocked(recipe); // 연구 트리와 같은 공용 저장 확인
        }

        private CraftedSpellRecipe RunEligible(CraftedSpellRecipe recipe) // 저장 조합을 보존하면서 실전 슬롯 검증
        {
            return FlightCraftedSpellCaster.Supports(recipe) && ResearchUnlocked(recipe) ? recipe : null; // 미지원·미해금 조합은 빈 전투 칸
        }

        internal float Cooldown(CraftedSpellRecipe recipe) // 전투 카드의 남은 재사용 시간
        {
            return caster.CooldownRemaining(recipe); // 동일 구성의 실제 대기시간
        }

        internal bool HasMana(CraftedSpellRecipe recipe) // 전투 카드의 실제 자원 조건
        {
            return FlightCraftedSpellCaster.Supports(recipe) && skills.HasCraftedMana(caster.EffectiveStats(recipe).Mana); // 지원하는 구성의 소수점 비용 비교
        }

        internal bool HasEmissionRoom(CraftedSpellRecipe recipe) // 카드의 실제 발사체 생성 여유
        {
            return caster.HasEmissionRoom(recipe); // 현재 플레이어의 생성 한도 확인
        }

        internal bool WithinBudget() // 현재 출격의 편성 비용 한도
        {
            return CraftedSpellRules.CanEquip(runningLanes[0], runningLanes[1], MagicBudget); // 두 슬롯의 실제 비용 합계
        }

        internal bool CanCast(int index) // 현재 클릭과 카드의 사용 가능 상태
        {
            ReconcileBudget(); // 입력 직전에도 현재 한도 확인
            CraftedSpellRecipe recipe = runningLanes[index]; // 이번 출격의 실제 장착 마법
            return IsSupported(recipe) && WithinBudget() // 지원 상태와 실제 편성 비용
                && skills.HasCraftedMana(caster.EffectiveStats(recipe).Mana) && caster.CooldownRemaining(recipe) <= 0f && caster.HasEmissionRoom(recipe); // 실제 마력·대기시간·생성 한도
        }

        internal void Use(int index) // 기존 마우스 입력 경로의 제작 마법 사용
        {
            if (CanCast(index) && skills.CanSpendCraftedMana() && caster.Cast(runningLanes[index])) // 실패 시 효과와 비용 모두 유지
            {
                caster.SetBeamLane(runningLanes[index], index); // 유지형 마법의 실제 좌우 입력 연결
                skills.SpendCraftedMana(caster.EffectiveStats(runningLanes[index]).Mana); // 유효한 시전에만 실제 마력 소비
            }
        }

        internal void ResetRun() // 다시하기 선택한 출격의 발사 초기화
        {
            caster.ResetCasts(); // 이전 탄환과 쿨타임 제거
            growth.Clear(); // 이전 출격의 임시 마법 강화 제거
            AssignRunLanes(); // 새 출격마다 보존한 편성을 무작위 배정
        }

        internal int UpgradeRank(int slot) // 현재 칸의 동일 구성 강화 합계
        {
            CraftedSpellRecipe recipe = runningLanes[slot]; // 이번 출격의 실제 마법
            if (recipe == null || !growth.TryGetValue(recipe.Id, out int[] values)) // 빈 칸 또는 아직 강화하지 않은 구성
            {
                return 0; // 강화 단계 없음
            }
            int rank = 0; // 서로 다른 수치의 합계
            foreach (int value in values) // 각 수치의 선택 횟수
            {
                rank += value; // 같은 마법의 총 단계 합산
            }
            return rank; // 동일 ID 슬롯이 공유하는 단계
        }

        internal int UpgradeSteps(CraftedSpellRecipe recipe, Upgrade kind) // 시전에서 적용할 개별 수치 단계
        {
            return recipe != null && growth.TryGetValue(recipe.Id, out int[] values) ? values[(int)kind] : 0; // 저장되지 않은 마법은 기본 수치
        }

        internal bool CanUpgrade(int slot, Upgrade kind) // 실제 효과가 있는 강화만 후보로 허용
        {
            if ((uint)slot >= runningLanes.Length || (uint)kind > (uint)Upgrade.Duration) // 존재하지 않는 칸과 강화
            {
                return false; // 잘못된 선택 거절
            }
            CraftedSpellRecipe recipe = runningLanes[slot]; // 이번 출격의 고정 마법
            if (!IsSupported(recipe) || UpgradeRank(slot) >= 3) // 미지원 구성 또는 총 강화 상한
            {
                return false; // 유효한 네 번째 강화 없음
            }
            FlightCraftedSpellCaster.Profile profile = FlightCraftedSpellCaster.Stats(recipe); // 문서의 기본 마법 수치
            return kind switch // 마법의 실제 기능별 후보 조건
            {
                Upgrade.Damage => profile.Damage > 0, // 피해가 있는 마법만 화력 강화
                Upgrade.Cooldown => profile.Cooldown > 0f, // 대기시간이 있는 마법
                Upgrade.Mana => profile.Mana > 0f, // 마력을 소비하는 마법
                Upgrade.Count => recipe.InitialShots > 1 && recipe.BaseId != CraftedSpellPresets.All[12].Recipe.Id && recipe.InitialShots + UpgradeSteps(recipe, kind) < CraftedSpellRules.MaxInitial, // 방어 위성을 제외한 다중 공격의 최초 생성 상한
                Upgrade.Pierce => (recipe.FirstEffect == "pierce" || recipe.SecondEffect == "pierce") && 3 + UpgradeSteps(recipe, kind) < 5, // 원래 관통과 최대 다섯 명
                Upgrade.Width => profile.Radius > 0f || profile.Spread > 0f || recipe.FirstEffect == "explode" || recipe.SecondEffect == "explode", // 기존 판정 범위가 있는 마법
                Upgrade.Duration => recipe.BaseId == CraftedSpellPresets.All[10].Recipe.Id ? 3f + UpgradeSteps(recipe, kind) * 0.5f < 6f : (recipe.BaseId == CraftedSpellPresets.All[11].Recipe.Id || recipe.BaseId == CraftedSpellPresets.All[6].Recipe.Id || recipe.BaseId == CraftedSpellPresets.All[12].Recipe.Id) && 4f + UpgradeSteps(recipe, kind) * 0.5f < 6f, // 광선과 결계·영역·위성의 유지 상한
                _ => false // 등록하지 않은 수치
            };
        }

        internal bool TryUpgrade(int slot, Upgrade kind) // 카드나 아이템이 선택한 한 단계 반영
        {
            if (!CanUpgrade(slot, kind)) // 무효한 마법이나 이미 최대 단계
            {
                return false; // 기존 강화 기록 유지
            }
            string id = runningLanes[slot].Id; // 이름이 아니라 구성 ID 기준
            if (!growth.TryGetValue(id, out int[] values)) // 이 마법의 첫 강화
            {
                values = new int[7]; // 일곱 수치의 초기 단계
                growth.Add(id, values); // 동일 ID가 공유하는 기록
            }
            values[(int)kind]++; // 선택한 수치 한 단계 증가
            return true; // 유효한 강화 적용
        }

        private void AssignRunLanes() // 출격 때 편성한 마법의 좌우 배정
        {
            if (hasTrainingOverride) // 제작 중인 단일 마법의 독립 훈련
            {
                ConfigureRun(trainingOverride, null, 1, 1, 0, 1); // 저장 편성과 분리한 단일 시험 조합
                return; // 무작위 정식 편성 생략
            }
            int first = UnityEngine.Random.Range(0, 2); // 첫 마법의 무작위 위치
            ConfigureRun(lanes[first], lanes[1 - first], priorities[first], priorities[1 - first], equipOrder[first], equipOrder[1 - first]); // 구성과 유지 순서를 함께 무작위 배정
        }

        internal void SetTrainingRecipe(CraftedSpellRecipe recipe) // 저장하지 않은 훈련 조합 연결
        {
            trainingOverride = recipe; // 다음 훈련 초기화부터 적용
            hasTrainingOverride = true; // 빈 조합도 원래 저장 편성으로 대체하지 않음
        }

        private void ApplyDuringPreparation(int? changed = null) // 전투에서 이미 쓰는 조합과 저장한 조합 분리
        {
            if (AppliesNow) // 첫 웨이브가 아직 시작되지 않은 출격 준비
            {
                if (changed.HasValue && runConfigured) // 사용자가 직접 변경한 한 슬롯
                {
                    int index = changed.Value; // 수정할 실제 준비 슬롯
                    desiredRun[index] = lanes[index]; // 직접 선택하거나 비운 복구 대상
                    runningLanes[index] = RunEligible(lanes[index]); // 지원과 연구를 충족한 현재 선택
                    suspended[index] = lanes[index] != null && runningLanes[index] == null; // 직접 해제한 슬롯은 복구 금지
                    runPriorities[index] = priorities[index]; // 변경한 슬롯의 유지 순서
                    runOrder[index] = equipOrder[index]; // 변경한 슬롯의 최근 편성 순서
                    ReconcileBudget(); // 다른 정상 편성을 건드리지 않는 자동 복구
                    return; // 다른 슬롯의 현재 상태 보존
                }
                ConfigureRun(lanes[0], lanes[1], priorities[0], priorities[1], equipOrder[0], equipOrder[1]); // 준비의 논리 슬롯과 유지 순서
            }
        }

        private void ConfigureRun(CraftedSpellRecipe left, CraftedSpellRecipe right, int leftPriority, int rightPriority, int leftOrder, int rightOrder) // 이번 출격의 원래 편성 고정
        {
            desiredRun[0] = left; // 첫 실제 슬롯의 복구 대상
            desiredRun[1] = right; // 둘째 실제 슬롯의 복구 대상
            runPriorities[0] = leftPriority; // 첫 슬롯의 유지 우선순위
            runPriorities[1] = rightPriority; // 둘째 슬롯의 유지 우선순위
            runOrder[0] = leftOrder; // 첫 슬롯의 최근 편성 순서
            runOrder[1] = rightOrder; // 둘째 슬롯의 최근 편성 순서
            for (int i = 0; i < runningLanes.Length; i++) // 원래 편성 두 칸의 실제 사용 자격
            {
                runningLanes[i] = RunEligible(desiredRun[i]); // 지원과 연구를 충족한 조합
                suspended[i] = desiredRun[i] != null && runningLanes[i] == null; // 자동 제외된 저장 구성 보존
            }
            runConfigured = true; // 한도 검사에 필요한 슬롯 준비 완료
            ReconcileBudget(); // 현재 장비 한도에서 출격 가능하도록 정리
        }

        private void ReconcileBudget() // 현재 한도를 넘는 슬롯의 자동 해제와 복구
        {
            if (!runConfigured || equipment == null) // 출격 슬롯이나 능력치가 준비되지 않은 상태
            {
                return; // 이전 편성과 시전 상태 유지
            }
            for (int i = 0; i < runningLanes.Length; i++) // 연구 상태가 변한 슬롯 점검
            {
                if (runningLanes[i] != null && RunEligible(runningLanes[i]) == null) // 더 이상 사용 가능한 조합이 아닌 경우
                {
                    Suspend(i); // 사용 중단과 자동 복구 대상 보존
                }
            }
            while (!CraftedSpellRules.CanEquip(runningLanes[0], runningLanes[1], MagicBudget)) // 실제 편성 비용이 현재 한도를 초과
            {
                int remove = runningLanes[0] == null ? 1 : runningLanes[1] == null ? 0 : runPriorities[0] != runPriorities[1] ? (runPriorities[0] < runPriorities[1] ? 0 : 1) : (runOrder[0] > runOrder[1] ? 0 : 1); // 낮은 우선순위와 최근 편성부터 해제
                if (runningLanes[remove] == null) // 한도가 음수이고 두 슬롯이 이미 비어 있는 경우
                {
                    break; // 더 이상 해제할 마법 없음
                }
                Suspend(remove); // 유지 효과를 종료하고 저장 구성 보존
            }
            int first = runPriorities[0] != runPriorities[1] ? (runPriorities[0] > runPriorities[1] ? 0 : 1) : (runOrder[0] <= runOrder[1] ? 0 : 1); // 높은 우선순위와 먼저 편성한 슬롯부터 복구
            for (int turn = 0; turn < 2; turn++) // 기존 정상 편성을 바꾸지 않는 복구 검사
            {
                int index = turn == 0 ? first : 1 - first; // 선택 순서의 실제 슬롯
                CraftedSpellRecipe candidate = suspended[index] ? RunEligible(desiredRun[index]) : null; // 자동 해제된 유효 구성만 복구
                if (candidate != null && CraftedSpellRules.CanEquip(candidate, runningLanes[1 - index], MagicBudget)) // 남은 한도에서 기존 슬롯을 유지할 수 있는 경우
                {
                    runningLanes[index] = candidate; // 원래 좌우 위치에 재편성
                    suspended[index] = false; // 복구한 슬롯의 자동 해제 표시 제거
                }
            }
        }

        private void Suspend(int index) // 자동 해제할 슬롯의 실제 사용 중단
        {
            CraftedSpellRecipe removed = runningLanes[index]; // 종료할 유지 효과의 실제 구성
            runningLanes[index] = null; // 이 슬롯의 신규 시전과 전투 카드 차단
            suspended[index] = true; // 같은 슬롯의 후속 자동 복구 대상
            caster?.StopMaintained(removed, index); // 해당 슬롯의 유지 효과만 종료
        }

        internal Texture2D Symbol(CraftedSpellRecipe recipe) // 카드에 표시할 기존 원소 이미지
        {
            var choices = CraftedSpellCatalog.Choices(CraftedSpellGroup.Element); // 카드와 같은 속성 전체
            for (int i = 0; i < choices.Count; i++) // 선택한 속성의 에셋 순번
            {
                if (choices[i].Id == recipe.Element) // 일치하는 원소
                {
                    return elements[i]; // 기존 독립 속성 아이콘
                }
            }
            return null; // 미지정 표시
        }

        internal static string NameOf(CraftedSpellRecipe recipe) // 기준 마법의 이름과 직접 조합 표시
        {
            foreach (CraftedSpellPreset preset in CraftedSpellPresets.All) // 기획서의 기본 저장 마법
            {
                if (preset.Recipe.Id == recipe.BaseId) // 같은 구성
                {
                    return preset.Name; // 한국어 마법 이름
                }
            }
            return "제작 마법"; // 조합을 직접 바꾼 마법
        }

        private void Write() // 유효한 현재 조합을 한 번에 기록
        {
            string key = SaveKey; // 테스트나 실제 플레이의 구분
            if (string.IsNullOrEmpty(key)) // 저장을 사용하지 않는 검증
            {
                return; // 사용자 설정 파일을 건드리지 않음
            }
            var data = new SaveData { version = 4, recipe = Saved?.Id, left = lanes[0]?.Id, right = lanes[1]?.Id, gallery = gallery.ToArray(), priorityLeft = priorities[0], priorityRight = priorities[1], orderLeft = equipOrder[0], orderRight = equipOrder[1] }; // 도감과 편성·유지 순서를 함께 복원하는 데이터
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data)); // 하나의 저장 항목으로 적용
            PlayerPrefs.Save(); // 종료와 재실행 후 복원
        }
    }
}
