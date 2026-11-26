using System; // 정지 메뉴로 돌아가는 연결
using UnityEngine; // 제작 화면과 연습 장치
using UnityEngine.Rendering; // 그래픽 없는 자동 검증
using UnityEngine.InputSystem; // 비행 미리보기의 화면 전환과 포커스
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
        private static readonly int[] Presets = { 0, 3, 7, 11, 9, 4, 1, 5, 8, 14, 6, 2, 13, 12, 10 }; // 실제 판정이 있는 기획 마법
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
        private FlightCraftedResearchPanel researchPanel; // 출격 전 계정 공용 연구 화면
        private FlightCraftedGalleryPanel galleryPanel; // 저장한 여러 마법의 도감 화면
        private FlightEmbeddedMagicTraining embedded; // 제작 화면 안의 실제 자유 비행
        private Button freeFlight; // 편집 중인 조합의 자유 비행 버튼
        private Button performanceButton; // 이동형 탄속과 사거리 프리셋
        private readonly Button[] priorityButtons = new Button[2]; // 두 슬롯의 유지 우선순위 선택
        private string performance = "standard"; // 현재 편집 중인 성능 선택
        internal bool HasPreview => embedded != null && embedded.IsBusy; // 내장 연습 씬의 로드 또는 실행 여부

        internal void Initialize(Action close, Action<CraftedSpellRecipe> train = null, Func<FlightBroomPilot> currentPilot = null) // 정지 메뉴와 자유 비행 연결
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
            performanceButton = Instantiate(preset, transform); // 기존 화면과 같은 프리셋 버튼 외형
            performanceButton.name = "Performance"; // 성능 교환 선택 버튼
            performanceButton.onClick = new Button.ButtonClickedEvent(); // 복제된 기본 선택 동작 제거
            RectTransform performanceRect = performanceButton.GetComponent<RectTransform>(); // 성능 선택의 위치와 크기
            performanceRect.anchoredPosition = new Vector2(-95f, -339f); // 화면 아래 성능 선택 위치
            performanceRect.sizeDelta = new Vector2(210f, 52f); // 이름과 선택을 표시할 폭
            FlightCraftedUi.FitLabel(performanceButton); // 성능 문구가 버튼 밖으로 넘치지 않도록 조정
            performanceButton.onClick.AddListener(CyclePerformance); // 표준·고속·장거리 순환
            if (train != null) // 출격 준비에서만 독립 연습 진입
            {
                freeFlight = Instantiate(preset, transform); // 기존 화면과 같은 버튼 외형
                freeFlight.name = "FreeFlight"; // 자유 비행 버튼 식별자
                freeFlight.onClick = new Button.ButtonClickedEvent(); // 복제된 기본 선택 동작 제거
                RectTransform rect = freeFlight.GetComponent<RectTransform>(); // 버튼 크기와 위치
                rect.anchoredPosition = new Vector2(-330f, -339f); // 화면 아래 연습 버튼 위치
                rect.sizeDelta = new Vector2(210f, 52f); // 문구가 보이는 버튼 크기
                freeFlight.GetComponentInChildren<Text>().text = "자유 비행 연습"; // 독립 시험 진입 안내
                FlightCraftedUi.FitLabel(freeFlight); // 자유 비행 문구를 실제 버튼 폭에 맞춤
                freeFlight.onClick.AddListener(() => train(candidate)); // 현재 조합 전달
                RectTransform recallRect = (RectTransform)recall.transform; // 저장 마법 불러오기 위치
                recallRect.anchoredPosition = new Vector2(350f, -339f); // 미리보기 버튼과 겹치지 않는 위치
                recallRect.sizeDelta = new Vector2(190f, 52f); // 독립된 버튼 폭
                FlightCraftedUi.FitLabel(recall); // 실제 폭에 맞춘 불러오기 문구
                RectTransform saveRect = (RectTransform)save.transform; // 현재 조합 저장 위치
                saveRect.anchoredPosition = new Vector2(560f, -339f); // 화면 오른쪽의 저장 버튼
                saveRect.sizeDelta = new Vector2(190f, 52f); // 독립된 저장 버튼 폭
                FlightCraftedUi.FitLabel(save); // 실제 폭에 맞춘 저장 문구
            }
            if (book.AppliesNow) // 실제 출격 전의 제작 화면에서만 연구 허용
            {
                RectTransform header = (RectTransform)transform.Find("Header"); // 기존 제작 화면의 제목 영역
                header.anchoredPosition = new Vector2(-630f, 335f); // 연구 버튼과 겹치지 않는 왼쪽 제목
                header.sizeDelta = new Vector2(150f, 57f); // 한글 제목이 들어갈 폭
                header.GetComponent<Text>().fontSize = 28; // 줄바꿈을 방지하는 제목 크기
                RectTransform titleRect = (RectTransform)title.transform; // 선택한 마법 이름 영역
                titleRect.anchoredPosition = new Vector2(60f, 335f); // 도감과 기본 구성 버튼 사이
                titleRect.sizeDelta = new Vector2(300f, 58f); // 버튼과 겹치지 않는 이름 폭
                for (int i = 0; i < priorityButtons.Length; i++) // 두 편성 슬롯의 유지 우선순위
                {
                    int index = i; // 각 버튼에 연결할 논리 슬롯
                    Button priority = Instantiate(preset, transform); // 기존 제작 버튼의 외형
                    priority.name = index == 0 ? "PriorityLeft" : "PriorityRight"; // 실제 슬롯에 대응하는 버튼
                    priority.onClick = new Button.ButtonClickedEvent(); // 기본 마법 순환 동작 제거
                    RectTransform priorityRect = (RectTransform)priority.transform; // 위치와 크기 조정
                    priorityRect.anchoredPosition = new Vector2(index == 0 ? 426f : 589f, -263f); // 각 편성 슬롯 아래의 우선순위
                    priorityRect.sizeDelta = new Vector2(144f, 38f); // 해제 버튼과 겹치지 않는 크기
                    FlightCraftedUi.FitLabel(priority, 19); // 우선순위 문구를 작은 버튼 안에 표시
                    priority.onClick.AddListener(() => ChangePriority(index)); // 해당 슬롯의 유지 순서 변경
                    priorityButtons[index] = priority; // 실제 화면 갱신 참조
                }
                researchPanel = gameObject.AddComponent<FlightCraftedResearchPanel>(); // 같은 화면의 연구·상점·보상 창
                researchPanel.Initialize(preset, title.font, Refresh); // 기존 글꼴과 버튼 외형 연결
                galleryPanel = gameObject.AddComponent<FlightCraftedGalleryPanel>(); // 같은 제작 화면의 마법 도감
                galleryPanel.Initialize(preset, title.font, book, Load, Refresh); // 실제 저장과 편집 화면 연결
                if (currentPilot != null) // 실제 출격 준비에서만 내장 비행 세션 지원
                {
                    embedded = transform.parent.gameObject.AddComponent<FlightEmbeddedMagicTraining>(); // 화면을 닫아도 비동기 씬 정리가 가능한 소유자
                    embedded.Initialize(transform, preset, currentPilot, () => candidate); // 현재 조종사와 미저장 조합 연결
                }
            }
        }

        internal void ShowDraft(string id) // 자유 훈련에서 미저장 조합 복원
        {
            Show(); // 일반 편집 화면 초기화
            if (CraftedSpellRules.TryRead(id, out CraftedSpellRecipe recipe)) // 유효한 복귀 조합
            {
                Load(recipe); // 저장을 변경하지 않고 편집 상태 복원
            }
        }

        internal void Show() // 저장 조합이 있으면 우선 편집
        {
            gameObject.SetActive(true); // 3D 미리보기와 UI 표시
            presetIndex = 0; // 기본 문서 조합의 처음 순서
            distanceIndex = 0; // 가까운 중립 표적
            MoveTarget(); // 해당 거리의 공통 표적
            ResetTest(); // 이 편집의 시험 피해와 쿨타임 초기화
            researchPanel?.Hide(); // 이전 방문의 확인 창과 연구 패널 닫기
            galleryPanel?.Hide(); // 이전 방문의 도감과 삭제 확인 닫기
            Load(book.Saved ?? CraftedSpellPresets.All[Presets[0]].Recipe); // 저장된 조합이나 파이어 볼트
        }

        private void OnDisable() // 전투로 돌아갈 때 연습 투사체 정리
        {
            if (embedded != null) // 실제 내장 연습 소유자가 남아 있는 경우
            {
                embedded.Stop(); // 내장 비행 씬과 입력 포커스 정리
            }
            if (range != null) // 씬 종료 중에 제거된 시험장은 제외
            {
                range.ResetCasts(); // 보존할 전투 데이터와 독립된 시험 시전
            }
        }

        internal bool HandlePreviewKey(Keyboard keyboard) // 비행 포커스의 Esc와 전체 화면 전환
        {
            return embedded != null && embedded.HandleKey(keyboard); // 연습이 처리한 키만 기존 편집에서 제외
        }

        private void Update() // 실제 연습 발사 결과와 대기시간
        {
            float wait = range.CooldownRemaining(candidate); // 연습에만 흐르는 재사용 시간
            testCast.interactable = (embedded == null || !embedded.IsReady) && FlightCraftedSpellCaster.Supports(candidate) && book.ResearchUnlocked(candidate) && wait <= 0f && range.PracticeMana >= FlightCraftedSpellCaster.Stats(candidate).Mana && range.HasEmissionRoom(candidate); // 내장 비행 중에는 실제 마우스 발사만 사용
            if (freeFlight != null) // 별도 전체 비행 연습 버튼
            {
                freeFlight.interactable = FlightCraftedSpellCaster.Supports(candidate) && book.ResearchUnlocked(candidate) && candidate.Cost <= book.MagicBudget && !HasPreview; // 내장 씬과 별도 연습의 중복 로드 차단
            }
            result.text = embedded != null && embedded.IsReady ? embedded.Status : $"누적 피해 {target.TotalDamage}  ·  연습 마력 {range.PracticeMana}/100  ·  대기 {wait:0.0}초"; // 현재 보이는 시험 표적과 자원 상태
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
            performance = recipe.Performance; // 저장한 탄속·사거리 선택 복원
            initialShots = recipe.InitialShots; // 같은 종류의 초기 생성 수
            Refresh(); // 공통 규칙으로 다시 계산
        }

        private void CyclePerformance() // 이동형 발사체의 성능 교환 선택
        {
            performance = performance == "standard" ? "fast" : performance == "fast" ? "long" : "standard"; // 확정된 세 성능을 순환
            Refresh(); // 실제 수치와 저장 가능 여부 갱신
        }

        private void ChangePriority(int index) // 자동 해제 시 유지할 슬롯의 순서 선택
        {
            book.SetPriority(index, (book.Priority(index) + 1) % 3); // 세 단계의 유지 우선순위 순환
            Refresh(); // 실제 저장된 우선순위 표시
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
                initialShots = initialShots, splitChildren = split ? 3 : 0, splitDepth = split ? 1 : 0, // 규칙이 제한하는 최초·분열 수
                performance = performance // 선택한 이동형 성능 프리셋
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
            bool unlocked = valid && book.ResearchUnlocked(candidate); // 모든 구성 요소의 공용 연구 해금
            embedded?.SetCandidate(candidate, supported && unlocked && candidate.Cost <= book.MagicBudget); // 현재 편집 조합과 실제 연습 가능 여부
            performanceButton.GetComponentInChildren<Text>().text = performance == "fast" ? "성능  고속형" : performance == "long" ? "성능  장거리형" : "성능  표준형"; // 현재 선택한 확정 성능
            for (int i = 0; i < priorityButtons.Length; i++) // 출격 전의 두 우선순위 버튼
            {
                if (priorityButtons[i] != null) // 실제 준비 화면에 생성된 버튼
                {
                    priorityButtons[i].GetComponentInChildren<Text>().text = book.Priority(i) == 2 ? "유지 높음" : book.Priority(i) == 1 ? "유지 보통" : "유지 낮음"; // 자동 해제의 보존 순서 표시
                }
            }
            if (freeFlight != null) // 출격 전의 독립 연습 버튼
            {
                freeFlight.interactable = supported && unlocked && candidate.Cost <= book.MagicBudget && !HasPreview; // 지원과 연구·한도 및 중복 연습 확인
            }
            bool saved = valid && candidate.Id == book.Saved?.Id; // 현재 선택을 저장했는지 확인
            title.text = valid ? FlightCraftedSpellBook.NameOf(candidate) : "구성 확인"; // 실제 조합의 이름
            save.interactable = valid && unlocked && !saved; // 해금된 유효 구성만 새로 저장
            recall.interactable = book.Saved != null; // 불러올 조합이 있는 경우
            equipLeft.interactable = supported && unlocked && saved && CraftedSpellRules.CanEquip(candidate, book.AppliesNow ? book.ActiveSlot(1) : book.Slot(1), book.MagicBudget); // 실제 유지 중인 우측 칸을 포함한 좌측 편성
            equipRight.interactable = supported && unlocked && saved && CraftedSpellRules.CanEquip(book.AppliesNow ? book.ActiveSlot(0) : book.Slot(0), candidate, book.MagicBudget); // 실제 유지 중인 좌측 칸을 포함한 우측 편성
            clearLeft.interactable = book.Slot(0) != null; // 해제할 제작 슬롯 여부
            clearRight.interactable = book.Slot(1) != null; // 해제할 제작 슬롯 여부
            lanes.text = $"좌  {LaneName(0)}\n우  {LaneName(1)}"; // 출격의 실제 두 마우스 슬롯
            summary.text = valid ? $"구성 비용 {candidate.Cost} / {book.MagicBudget}\n최초 {candidate.InitialShots}발  ·  최대 {candidate.MaximumShots}발" : "구성 요소를 확인하세요."; // 조합 비용과 생성 제한
            if (supported) // 실제 시험 발사 가능한 구성
            {
                FlightCraftedSpellCaster.Profile stats = range.EffectiveStats(candidate); // 선택한 성능의 실제 연습 마력·탄속
                summary.text += $"\n마력 {stats.Mana}  ·  대기 {stats.Cooldown:0.#}초\n사거리 {stats.Range:0.#} m  ·  탄속 {stats.Speed:0.#} m/s"; // 실제 프로필 정보
            }
            message.text = valid ? !unlocked ? "연구가 필요한 구성입니다." : supported ? saved ? "저장 완료" : "새 조합" : "현재 발사기에서 시험 대기 중인 구성" : Explain(issue); // 연구와 저장·실전 지원 안내
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
            if (recipe == null) // 사용자가 직접 비운 슬롯
            {
                return "빈 슬롯"; // 자동 복구 대상이 없는 빈 칸
            }
            string name = FlightCraftedSpellBook.NameOf(recipe); // 저장한 구성의 실제 이름
            if (!book.ResearchUnlocked(recipe)) // 연구가 아직 잠긴 이전 구성
            {
                return name + " (연구 잠금)"; // 저장은 유지하고 실제 사용은 제외
            }
            if (!book.IsSupported(recipe)) // 확정 수치가 없는 이전 구성
            {
                return name + " (지원 준비 중)"; // 임의 수치 없이 저장 보존
            }
            return book.AppliesNow && book.ActiveSlot(index) == null ? name + " (한도 초과 · 자동 해제)" : name; // 자동 해제 이유와 현재 편성 구분
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
            if (!book.ResearchUnlocked(candidate) || !range.Cast(candidate)) // 연구와 재사용 제한·조합 검증
            {
                return; // 실전과 동일하게 실패 시 소모 없음
            }
            message.text = "연습 표적에 시험 발사했습니다."; // 연습의 실제 실행 표시
        }

        private void ResetTest() // 독립 연습의 피해와 발사체 초기화
        {
            range.ResetCasts(); // 시험 발사체와 재사용 시간 초기화
            target.GetComponent<FlightCraftedStatus>()?.Clear(); // 표적에 남은 이전 지속 피해 제거
            target.ResetDamage(); // 연습 표적의 실제 누적 피해 초기화
            embedded?.ResetPreview(); // 내장 비행의 표적·마력·탄막도 독립 초기화
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
            embedded?.SetTargetDistance(Distances[distanceIndex]); // 내장 비행의 실제 표적 거리도 함께 변경
        }
    }
}
