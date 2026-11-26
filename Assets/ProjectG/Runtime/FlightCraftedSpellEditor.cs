using System; // 정지 메뉴로 돌아가는 연결
using UnityEngine; // 제작 화면과 연습 장치
using UnityEngine.Rendering; // 그래픽 없는 자동 검증
using UnityEngine.UI; // 씬에 작성한 버튼과 한글 정보

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCraftedSpellEditor : MonoBehaviour // 저장과 시험 발사를 연결한 제작 화면
    {
        private static readonly CraftedSpellGroup[] Groups = // 화면의 고정 속성 편집 순서
        {
            CraftedSpellGroup.Element, CraftedSpellGroup.Shape, CraftedSpellGroup.Formation, CraftedSpellGroup.Trajectory, // 발사체의 시각과 이동
            CraftedSpellGroup.FireMode, CraftedSpellGroup.Origin, CraftedSpellGroup.Aim, CraftedSpellGroup.Trigger, // 생성 위치와 발동 방식
            CraftedSpellGroup.Effect, CraftedSpellGroup.Effect // 중복을 검사하는 최대 두 가지 효과
        };
        private static readonly int[] Presets = { 0, 3, 7, 11, 9 }; // 실제 판정이 있는 기획 마법
        private static readonly float[] Distances = { 10f, 14f, 20f }; // 사거리 경계와 중립 시험 표적 거리

        [SerializeField] private FlightCraftedSpellBook book; // 실전에서 불러올 제작 마법
        [SerializeField] private FlightCraftedSpellCaster range; // 같은 발사기로 동작하는 연습장
        [SerializeField] private FlightCraftedTarget target; // 전투 점수와 구분한 시험 대상
        [SerializeField] private Transform targetOrigin; // 시험 거리 기준점
        [SerializeField] private Button[] choices; // 화면에 배치한 조합 항목 버튼
        [SerializeField] private Button preset; // 기본 시안 순환
        [SerializeField] private Button back; // 정지 메뉴 복귀
        [SerializeField] private Button testCast; // 연습장 시전
        [SerializeField] private Button resetTest; // 연습 상태 초기화
        [SerializeField] private Button distance; // 표적 거리 변경
        [SerializeField] private Button save; // 올바른 조합 저장
        [SerializeField] private Button recall; // 저장했던 내용 불러오기
        [SerializeField] private Button equipLeft; // 왼쪽 실전 슬롯 편성
        [SerializeField] private Button equipRight; // 오른쪽 실전 슬롯 편성
        [SerializeField] private Button clearLeft; // 좌측 제작 슬롯 해제
        [SerializeField] private Button clearRight; // 우측 제작 슬롯 해제
        [SerializeField] private Text title; // 제작 마법 이름
        [SerializeField] private Text summary; // 구성 비용과 성능
        [SerializeField] private Text result; // 연습 발사의 실제 피해 기록
        [SerializeField] private Text lanes; // 실제 왼쪽과 오른쪽 편성
        [SerializeField] private Text message; // 저장과 호환성 결과
        private readonly string[] draft = new string[Groups.Length]; // 확정 전 선택만 기록한 조합
        private Text[] choiceLabels; // 현재 선택한 한국어 이름을 보여 주는 라벨
        private Text distanceLabel; // 시험 표적 거리 버튼의 문구
        private CraftedSpellRecipe candidate; // 규칙을 통과한 현재 조합
        private int presetIndex; // 기본 구성의 순환 위치
        private int distanceIndex; // 다음 시험 표적의 위치
        private int initialShots; // 조합에서 실제 발사하는 탄 수

        internal void Initialize(Action close) // 정지 메뉴의 준비와 함께 버튼 연결
        {
            GetComponentInChildren<Camera>(true).enabled = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null; // 그래픽 장치에서만 텍스처 렌더링
            choiceLabels = new Text[choices.Length]; // 구성 버튼들의 텍스트 참조
            for (int i = 0; i < choices.Length; i++) // 각 분류를 순환하는 개별 클릭
            {
                int index = i; // 현재 선택 줄의 고정 위치
                choiceLabels[i] = choices[i].GetComponentInChildren<Text>(); // 현재 버튼에 보이는 값
                choices[i].onClick.AddListener(() => CycleChoice(index)); // 다음 유효한 문서 선택지
            }
            back.onClick.AddListener(() => close()); // 기존 조작 안내로 복귀
            preset.onClick.AddListener(CyclePreset); // 다른 시작 마법 선택
            testCast.onClick.AddListener(TestCast); // 격리된 연습 사격
            resetTest.onClick.AddListener(ResetTest); // 명중과 재사용 시간 초기화
            distance.onClick.AddListener(CycleDistance); // 다음 표적 거리
            distanceLabel = distance.GetComponentInChildren<Text>(); // 실제 시험 거리 표시
            save.onClick.AddListener(Save); // 현재 조합 저장
            recall.onClick.AddListener(() => Load(book.Saved)); // 가장 최근 저장 조합 복원
            equipLeft.onClick.AddListener(() => Equip(0)); // 처음 실전 마법 칸
            equipRight.onClick.AddListener(() => Equip(1)); // 두 번째 실전 마법 칸
            clearLeft.onClick.AddListener(() => Clear(0)); // 좌측 제작 슬롯 해제
            clearRight.onClick.AddListener(() => Clear(1)); // 우측 제작 슬롯 해제
        }

        internal void Show() // 저장 조합이 있으면 우선 편집
        {
            gameObject.SetActive(true); // 3D 미리보기와 UI 표시
            presetIndex = 0; // 기본 문서 조합의 처음 순서
            distanceIndex = 0; // 가까운 중립 표적
            MoveTarget(); // 해당 거리의 공통 표적
            ResetTest(); // 이 편집의 시험 피해와 쿨타임 초기화
            Load(book.Saved ?? CraftedSpellPresets.All[Presets[0]].Recipe); // 저장된 조합이나 파이어 볼트
        }

        private void OnDisable() // 전투로 돌아갈 때 연습 투사체 정리
        {
            if (range != null) // 씬 종료 중에 제거된 시험장은 제외
            {
                range.ResetCasts(); // 보존할 전투 데이터와 독립된 시험 시전
            }
        }

        private void Update() // 실제 연습 발사 결과와 대기시간
        {
            float wait = range.CooldownRemaining(candidate); // 연습에만 흐르는 재사용 시간
            testCast.interactable = FlightCraftedSpellCaster.Supports(candidate) && wait <= 0f && range.PracticeMana >= FlightCraftedSpellCaster.Stats(candidate).Mana; // 마력과 대기시간이 유효한 조합
            result.text = $"누적 피해 {target.TotalDamage}  ·  연습 마력 {range.PracticeMana}/100  ·  대기 {wait:0.0}초"; // 실제 명중과 마력과 대기 시간
        }

        private void Load(CraftedSpellRecipe recipe) // 저장 및 문서 조합을 편집 상태에 복원
        {
            if (recipe == null) // 사용자가 아직 저장하지 않은 상태
            {
                return; // 현재 입력 유지
            }
            draft[0] = recipe.Element; // 마법 속성 복원
            draft[1] = recipe.Shape; // 탄환 형태 복원
            draft[2] = recipe.Formation; // 초기 배열 복원
            draft[3] = recipe.Trajectory; // 이동 방식 복원
            draft[4] = recipe.FireMode; // 시전 방식 복원
            draft[5] = recipe.Origin; // 시작 위치 복원
            draft[6] = recipe.Aim; // 목표 선택 복원
            draft[7] = recipe.Trigger; // 발동 조건 복원
            draft[8] = recipe.FirstEffect; // 첫 효과 복원
            draft[9] = recipe.SecondEffect; // 두 번째 효과 복원
            initialShots = recipe.InitialShots; // 같은 종류의 초기 생성 수
            Refresh(); // 공통 규칙으로 다시 계산
        }

        private void CyclePreset() // 구현된 기획 마법의 기본 구성 전환
        {
            presetIndex = (presetIndex + 1) % Presets.Length; // 정해진 기본 구성 순환
            Load(CraftedSpellPresets.All[Presets[presetIndex]].Recipe); // 공용 제작 규칙을 통과한 조합
        }

        private void CycleChoice(int index) // 기획서의 해당 카테고리를 다음 항목으로 변경
        {
            var options = CraftedSpellCatalog.Choices(Groups[index]); // 현재 분류의 선택 가능한 ID
            int i = 0; // 기존 선택을 찾는 인덱스
            while (i + 1 < options.Count && options[i].Id != draft[index]) // 현재 선택까지 탐색
            {
                i++; // 다음 고정 항목 확인
            }
            draft[index] = options[(i + 1) % options.Count].Id; // 다음 구성의 고정 ID
            if (index == 2) // 배열을 바꾼 경우 초기 발사 수 변경
            {
                initialShots = draft[index] == "single" ? 1 : draft[index] == "cone" ? 6 : 3; // 편집기의 기본 탄환 수
            }
            Refresh(); // 바뀐 조합의 비용과 발사 지원
        }

        private void Refresh() // 편집 화면과 저장에서 검증 규칙 공유
        {
            bool split = draft[8] == "split" || draft[9] == "split"; // 선택한 분열 효과 여부
            var input = new CraftedSpellSelection // 화면에서 입력한 현재 마법
            {
                element = draft[0], shape = draft[1], formation = draft[2], trajectory = draft[3], // 발사체 구성
                fireMode = draft[4], origin = draft[5], aim = draft[6], trigger = draft[7], // 생성 위치와 방식
                firstEffect = draft[8], secondEffect = draft[9], // 선택한 특수 효과
                initialShots = initialShots, splitChildren = split ? 3 : 0, splitDepth = split ? 1 : 0 // 규칙이 제한하는 최초·분열 수
            };
            CraftedSpellIssue issue = CraftedSpellRules.Compose(input, out candidate); // 보존과 실제 발사 전 현재 구성 검증
            for (int i = 0; i < choiceLabels.Length; i++) // 사용자가 선택한 항목의 이름
            {
                foreach (CraftedSpellChoice choice in CraftedSpellCatalog.Choices(Groups[i])) // 해당 분류의 기획 항목
                {
                    if (choice.Id == draft[i]) // 이 줄의 현재 ID
                    {
                        choiceLabels[i].text = choice.Label; // 실제 한국어 선택지
                        break; // 일치하는 항목 표시 완료
                    }
                }
            }
            bool valid = issue == CraftedSpellIssue.None; // 구조적으로 저장할 수 있는 조합
            bool supported = FlightCraftedSpellCaster.Supports(candidate); // 실제 시험 탄환과 효과 지원
            bool saved = valid && candidate.Id == book.Saved?.Id; // 현재 선택을 저장했는지 확인
            title.text = valid ? FlightCraftedSpellBook.NameOf(candidate) : "구성 확인"; // 실제 조합의 이름
            save.interactable = valid && !saved; // 유효한 새 구성만 저장
            recall.interactable = book.Saved != null; // 불러올 조합이 있는 경우
            equipLeft.interactable = supported && saved && CraftedSpellRules.CanEquip(candidate, book.Slot(1), book.MagicBudget); // 좌측 편성 가능 여부
            equipRight.interactable = supported && saved && CraftedSpellRules.CanEquip(book.Slot(0), candidate, book.MagicBudget); // 우측 편성 가능 여부
            clearLeft.interactable = book.Slot(0) != null; // 해제할 제작 슬롯 여부
            clearRight.interactable = book.Slot(1) != null; // 해제할 제작 슬롯 여부
            lanes.text = $"좌  {LaneName(0)}\n우  {LaneName(1)}"; // 출격의 실제 두 마우스 슬롯
            summary.text = valid ? $"구성 비용 {candidate.Cost} / {book.MagicBudget}\n최초 {candidate.InitialShots}발  ·  최대 {candidate.MaximumShots}발" : "구성 요소를 확인하세요."; // 조합 비용과 생성 제한
            if (supported) // 실제 시험 발사 가능한 구성
            {
                FlightCraftedSpellCaster.Profile stats = FlightCraftedSpellCaster.Stats(candidate); // 실전과 같은 마력·탄속
                summary.text += $"\n마력 {stats.Mana}  ·  대기 {stats.Cooldown:0.#}초\n사거리 {stats.Range:0.#} m  ·  탄속 {stats.Speed:0.#} m/s"; // 실제 프로필 정보
            }
            message.text = valid ? supported ? saved ? "저장 완료" : "새 조합" : "현재 발사기에서 시험 대기 중인 구성" : Explain(issue); // 저장과 실전 지원 안내
        }

        private static string Explain(CraftedSpellIssue issue) // 조합이 거절된 이유
        {
            return issue switch // 편집기와 검증 규칙이 공유하는 결과
            {
                CraftedSpellIssue.DuplicateEffect => "같은 효과는 두 번 선택할 수 없습니다.", // 중복 효과
                CraftedSpellIssue.Incompatible => "서로 호환되지 않는 조합입니다.", // 구조적 충돌
                CraftedSpellIssue.InitialLimit => "최초 발사체 수를 확인하세요.", // 최초 생성 수 제한
                CraftedSpellIssue.SplitLimit => "분열 탄 수와 단계를 확인하세요.", // 자탄과 재귀 제한
                _ => "선택한 구성 요소를 확인하세요." // 존재하지 않는 선택
            };
        }

        private string LaneName(int index) // 양쪽 카드 칸의 이름
        {
            CraftedSpellRecipe recipe = book.Slot(index); // 실제 제작 마법 편성
            return recipe != null ? FlightCraftedSpellBook.NameOf(recipe) : "빈 슬롯"; // 시험 덱과 제작 슬롯 상태
        }

        private void Save() // 현재 규칙을 통과한 제작 마법 보존
        {
            if (candidate != null) // 유효한 조합
            {
                book.Save(candidate); // 실제 제작 마법 저장
                Refresh(); // 편성 버튼 사용 가능 상태
            }
        }

        private void Equip(int index) // 저장한 마법을 실제 마우스 슬롯에 지정
        {
            if (candidate != null && candidate.Id == book.Saved?.Id && book.Equip(index, candidate)) // 저장한 구성과 한도 확인
            {
                Refresh(); // 갱신한 실전 슬롯 이름
                string slot = index == 0 ? "왼쪽" : "오른쪽"; // 마우스의 편성 위치
                message.text = book.AppliesNow ? $"{slot}에 편성했습니다." : $"다음 출격의 {slot}에 편성했습니다."; // 실제 적용 시점을 포함한 저장 결과
            }
        }

        private void Clear(int index) // 한 쪽 제작 마법만 해제
        {
            book.ClearSlot(index); // 보존된 마법과 무관한 실전 편성
            Refresh(); // 현재 실제 카드 구성 표시
        }

        private void TestCast() // 실제 전투와 같은 발사기를 연습용으로 실행
        {
            if (!range.Cast(candidate)) // 재사용 제한과 조합 검증
            {
                return; // 실전과 동일하게 실패 시 소모 없음
            }
            message.text = "연습 표적에 시험 발사했습니다."; // 연습의 실제 실행 표시
        }

        private void ResetTest() // 독립 연습의 피해와 발사체 초기화
        {
            range.ResetCasts(); // 시험 발사체와 재사용 시간 초기화
            target.ResetDamage(); // 연습 표적의 실제 누적 피해 초기화
        }

        private void CycleDistance() // 동일 조합을 다른 거리로 시험
        {
            distanceIndex = (distanceIndex + 1) % Distances.Length; // 연습 거리를 순환
            MoveTarget(); // 시험 표적 실제 위치 변경
            ResetTest(); // 새 거리의 기록으로 시작
        }

        private void MoveTarget() // 발사선 위의 물리 표적을 이동
        {
            target.transform.position = targetOrigin.position + targetOrigin.forward * Distances[distanceIndex]; // 같은 기준에서의 실제 월드 거리
            Physics.SyncTransforms(); // 일시정지 중에도 시험 표적의 충돌 위치 반영
            distanceLabel.text = $"표적 {Distances[distanceIndex]:0} m"; // 버튼에 실제 거리 표시
        }
    }
}
