using System; // 장착과 검증 환경 저장 키
using UnityEngine; // 실전 카드와 조합 저장

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCraftedSpellBook : MonoBehaviour // 제작 마법의 저장과 좌우 편성
    {
        [Serializable] // 현재 저장 마법과 장착 슬롯
        private sealed class SaveData
        {
            public int version; // 빈 칸과 시험 덱을 구분하는 저장 버전
            public string recipe; // 편집 화면에 복원할 저장된 조합
            public string left; // 왼쪽 전투 슬롯의 조합 ID
            public string right; // 오른쪽 전투 슬롯의 조합 ID
        }

        [SerializeField] private FlightCraftedSpellCaster caster; // 실제 전투에서 쓰는 공통 발사기
        [SerializeField] private FlightSkillController skills; // 스킬과 같은 실제 마력
        [SerializeField] private FlightBroomEquipment equipment; // 마법 공격력에 포함되는 빗자루 효과
        [SerializeField] private FlightEnemyEncounter encounter; // 실제 출격 시작 여부
        [SerializeField] private Texture2D[] elements; // 기존 카드의 5개 속성 이미지
        private readonly CraftedSpellRecipe[] lanes = new CraftedSpellRecipe[2]; // 좌우 편성 ID 복원본
        private readonly CraftedSpellRecipe[] runningLanes = new CraftedSpellRecipe[2]; // 이번 출격의 고정 편성
        private const string UserKey = "ProjectG.Flight.Crafted.v1"; // 제작 마법의 버전별 저장 키
        private static string SaveKey => Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY") ?? (FlightBindings.UsesPreferences ? UserKey : null); // 격리 검증과 사용자 저장 분리

        public CraftedSpellRecipe Saved { get; private set; } // 편집기로 불러올 가장 최근 조합
        internal int MagicBudget => 100 + Mathf.RoundToInt(equipment.GetBonus(FlightBroomStat.MagicPower)); // 실전 편성 기준과 장착 효과
        internal bool AppliesNow => encounter == null; // 출격 준비 씬에서만 현재 편성 변경

        private void Awake() // 저장된 제작 마법과 편성 읽기
        {
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
                        if (CraftedSpellRules.TryRead(data.recipe, out CraftedSpellRecipe saved)) // 유효한 편집기 조합
                        {
                            Saved = saved; // 최근 저장한 구성 복원
                        }
                        bool legacyTrialDeck = data.version < 2 && string.IsNullOrEmpty(data.left) && string.IsNullOrEmpty(data.right); // 이전 저장의 기본 시험 덱
                        if (!legacyTrialDeck) // 이전 시험 덱 이외의 명시적인 편성
                        {
                            lanes[0] = CraftedSpellRules.TryRead(data.left, out CraftedSpellRecipe left) && FlightCraftedSpellCaster.Supports(left) ? left : null; // 첫 칸 또는 빈 칸 복원
                            lanes[1] = CraftedSpellRules.TryRead(data.right, out CraftedSpellRecipe right) && FlightCraftedSpellCaster.Supports(right) ? right : null; // 둘째 칸 또는 빈 칸 복원
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
                Array.Copy(lanes, runningLanes, lanes.Length); // 선택 순서 그대로 준비 화면에 반영
            }
            else // 실제 출격 시작 시점
            {
                AssignRunLanes(); // 두 칸의 무작위 좌우 배정
            }
        }

        internal CraftedSpellRecipe Slot(int index) // 실제 카드의 조합
        {
            return lanes[index]; // 현재 좌우 칸의 유효 마법
        }

        internal CraftedSpellRecipe ActiveSlot(int index) // 이번 출격의 실제 사용 마법
        {
            return runningLanes[index]; // 출격 시작 전 갱신한 고정 편성
        }

        internal void Save(CraftedSpellRecipe recipe) // 편집기에서 검증한 조합 보존
        {
            Saved = recipe; // 마법 도감의 시험 저장 위치
            Write(); // 편집 화면을 떠나도 내용 복원
        }

        internal bool Equip(int index, CraftedSpellRecipe recipe) // 다음 출격의 좌우 조합 선택
        {
            if (!FlightCraftedSpellCaster.Supports(recipe) || !CraftedSpellRules.CanEquip(recipe, lanes[1 - index], MagicBudget)) // 판정 또는 편성 한도 검증
            {
                return false; // 기존 슬롯 보존
            }
            lanes[index] = recipe; // 지정한 슬롯의 새 마법
            ApplyDuringPreparation(); // 전투가 시작되기 전에는 즉시 반영
            Write(); // 장착 직후 유효 조합 저장
            return true; // 해당 마법의 실제 편성
        }

        internal void ClearSlot(int index) // 제작 마법 슬롯 비우기
        {
            lanes[index] = null; // 해당 조합의 사용 중단
            ApplyDuringPreparation(); // 출격 준비 중의 슬롯 갱신
            Write(); // 게임 재실행 후에도 편성 유지
        }

        internal bool CanCast(int index) // 현재 클릭과 카드의 사용 가능 상태
        {
            CraftedSpellRecipe recipe = runningLanes[index]; // 이번 출격의 실제 장착 마법
            return recipe != null && CraftedSpellRules.CanEquip(runningLanes[0], runningLanes[1], MagicBudget) // 부품 보정 후 실제 비용
                && skills.Mana >= FlightCraftedSpellCaster.Stats(recipe).Mana && caster.CooldownRemaining(recipe) <= 0f; // 마력과 동일 ID 재사용 시간
        }

        internal void Use(int index) // 기존 마우스 입력 경로의 제작 마법 사용
        {
            if (CanCast(index) && skills.CanSpendCraftedMana() && caster.Cast(runningLanes[index])) // 실패 시 효과와 비용 모두 유지
            {
                skills.SpendCraftedMana(FlightCraftedSpellCaster.Stats(runningLanes[index]).Mana); // 유효한 시전에만 실제 마력 소비
            }
        }

        internal void ResetRun() // 다시하기 선택한 출격의 발사 초기화
        {
            caster.ResetCasts(); // 이전 탄환과 쿨타임 제거
            AssignRunLanes(); // 새 출격마다 보존한 편성을 무작위 배정
        }

        private void AssignRunLanes() // 출격 때 편성한 마법의 좌우 배정
        {
            int first = UnityEngine.Random.Range(0, 2); // 첫 마법의 무작위 위치
            runningLanes[0] = lanes[first]; // 첫 번째 전투 칸
            runningLanes[1] = lanes[1 - first]; // 나머지 전투 칸
        }

        private void ApplyDuringPreparation() // 전투에서 이미 쓰는 조합과 저장한 조합 분리
        {
            if (AppliesNow) // 첫 웨이브가 아직 시작되지 않은 출격 준비
            {
                Array.Copy(lanes, runningLanes, lanes.Length); // 현재 준비 중인 출격에 반영
            }
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
                if (preset.Recipe.Id == recipe.Id) // 같은 구성
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
            var data = new SaveData { version = 2, recipe = Saved?.Id, left = lanes[0]?.Id, right = lanes[1]?.Id }; // 선택과 장착을 함께 복원하는 데이터
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data)); // 하나의 저장 항목으로 적용
            PlayerPrefs.Save(); // 종료와 재실행 후 복원
        }
    }
}
