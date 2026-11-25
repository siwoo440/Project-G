using System.Collections.Generic; // 필터링한 후보와 최대 랭크
using UnityEngine; // 전투 정지와 카드 자료
using UnityEngine.InputSystem; // 1·2·3과 클릭 선택
using UnityEngine.UI; // 선택 순서를 표시하는 제목

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(200)] // 선택을 처리한 프레임의 전투 입력 차단
    public sealed class FlightUpgradeChoices : MonoBehaviour // 출격 중 강화 후보와 선택 결과
    {
        private enum Reward // 기본 후보와 서로 다른 대체 보상
        {
            Damage, Cadence, Pierce, Focus, Cycle, Fan, Magnet, Lens, Heal, SlowDecay, Wide, Weak, SniperPierce, SmallHeal, Energy, HpOne, HpTwo, HpThree, Drone, Blade, ProtectionDevice // 단계와 보조 슬롯 구분
        }

        private readonly struct Card // 시험 후보의 적용 규칙
        {
            public readonly Reward reward; // 내부 효과 번호
            public readonly string title; // HUD 카드 이름
            public readonly string text; // 카드별 실제 적용 내용
            public readonly int cap; // 다시 나올 수 있는 최대 단계
            public readonly int pilot; // 공용 0·아리아 1·리안 2
            public readonly int rarity; // 일반 0·희귀 1·영웅 2
            public readonly int art; // 제작 효과 그림 번호

            public Card(Reward id, string name, string body, int maximum, int family, int tier, int icon) // 카드 데이터 설정
            {
                reward = id; // 선택 때 사용할 효과
                title = name; // 카드 이름
                text = body; // 단계별 효과 설명
                cap = maximum; // 랭크 상한
                pilot = family; // 캐릭터 제한
                rarity = tier; // 뽑기 등급
                art = icon; // 표시용 이미지
            }
        }

        private static readonly Card[] Core = // 기획서의 공용 강화와 캐릭터 첫 노드
        {
            new Card(Reward.Damage, "화력 조정", "자동 사격 피해 +10%", 3, 0, 0, 0), // 화력 조정
            new Card(Reward.Cadence, "발사 조정", "자동 사격 간격 -8%", 3, 0, 0, 0), // 발사 조정
            new Card(Reward.Pierce, "관통탄", "자동탄 관통 대상 +1", 3, 0, 1, 3), // 관통탄
            new Card(Reward.Focus, "집중 조율", "집중탄 피해 +8%", 3, 0, 0, 3), // 집중 조율
            new Card(Reward.Cycle, "고유기 순환", "Q 재사용 대기 -10%", 3, 0, 0, 1), // 고유기 순환
            new Card(Reward.Fan, "산탄 스킬", "E 보호막을 산탄으로 교체", 1, 0, 1, 0), // 공용 슬롯 교체
            new Card(Reward.Magnet, "자석", "경험치 획득 반경 +20%", 3, 0, 0, 2), // 자석
            new Card(Reward.Lens, "집속 렌즈", "집중탄 피해 +10%\n집중탄 폭 -10%", 3, 0, 1, 3), // 집속 렌즈
            new Card(Reward.Heal, "응급 회복", "체력 20 회복", 0, 0, 0, 2), // 응급 회복
            new Card(Reward.Drone, "공격 드론", "보조 슬롯\n매초 피해 6·9·12의 자동탄", 3, 0, 1, 0), // 프로펠러 공격 드론
            new Card(Reward.Blade, "회전 칼날", "보조 슬롯\n매초 주변 적 피해 8·12·16", 3, 0, 1, 3), // 기체 주위 근접 피해
            new Card(Reward.ProtectionDevice, "보호 장치", "패시브 슬롯\n피격 후 다음 피해 1회 방어\n재충전 30초", 1, 0, 1, 1), // 피격 뒤 장치 보호 충전
            new Card(Reward.SlowDecay, "추격 사격 1", "연속 적중 보너스\n감소 1초 지연", 1, 1, 1, 0), // 추격 사격 1
            new Card(Reward.Wide, "확산 포화 1", "아리아의 기본탄 폭 +20%", 1, 1, 2, 0), // 확산 포화 1
            new Card(Reward.Weak, "약점 사냥 1", "리안의 약점 피해\n보너스 +65%로 증가", 1, 2, 1, 4), // 약점 사냥 1
            new Card(Reward.SniperPierce, "관통 궤도 1", "리안의 자동탄\n관통 대상 +1", 1, 2, 2, 4) // 관통 궤도 1
        };

        private static readonly Card[] Fallback = // 유효 후보가 부족할 때만 쓰는 보상
        {
            new Card(Reward.SmallHeal, "소량 회복", "체력 10 회복", 0, 0, 0, 2), // 회복 가능한 경우의 대체 보상
            new Card(Reward.Energy, "필살기 충전", "필살기 에너지 +5", 0, 0, 0, 4), // 필살기 에너지 보상
            new Card(Reward.HpOne, "체력 증가 1", "최대 체력 +1", 0, 0, 0, 1), // 최대 체력 소폭 증가
            new Card(Reward.HpTwo, "체력 증가 2", "최대 체력 +2", 0, 0, 0, 1), // 최대 체력 증가
            new Card(Reward.HpThree, "체력 증가 3", "최대 체력 +3", 0, 0, 0, 1) // 세 번째 대체 선택
        };

        [SerializeField] private FlightExperience experience; // 실제 연속 레벨업 공급원
        [SerializeField] private FlightWeapon weapon; // 공용·전용 후보 구분
        [SerializeField] private FlightPilotVitals vitals; // 유효한 회복과 슬롯 해제
        [SerializeField] private FlightSkillController skills; // 교체할 E와 즉시 에너지
        [SerializeField] private FlightCombatHud hud; // E 교체 아이콘의 기존 슬롯
        [SerializeField] private FlightEnhancements enhancements; // 드롭 아이템의 카드 공통 상한
        [SerializeField] private GameObject panel; // 씬에 배치한 모달
        [SerializeField] private Text heading; // 현재 레벨 선택 번호
        [SerializeField] private FlightUpgradeCardView[] cards; // 씬에서 편집할 프리팹 슬롯
        [SerializeField] private Texture[] icons; // 종류를 나타낼 제작된 이미지
        private readonly int[] ranks = new int[System.Enum.GetValues(typeof(Reward)).Length]; // 이번 출격의 단계
        private readonly Card[] offered = new Card[3]; // 현재 중복 없는 선택 3장
        private readonly List<Card> candidates = new List<Card>(); // 최대 단계와 전용 조건을 검사한 목록
        private int pending; // 아직 남은 레벨업 선택 수
        private int choiceCount; // 현재 레벨업에서 표시한 카드 수
        private int picked = -1; // 같은 프레임에 중복 선택 방지
        private float priorScale; // 모달 전의 게임 속도
        private CursorLockMode priorLock; // 모달 전의 마우스 잠금
        private bool priorCursor; // 모달 전의 마우스 표시
        private bool paused; // 이 화면이 현재 정지를 관리하는 상태
        private int firstLevel; // 화면 제목에 표시하는 레벨 번호

        public float CadenceScale => 1f - Total(FlightEnhancementKind.Cadence, FlightEnhancements.CadenceLimit); // 카드·아이템 공통 연사
        public float CooldownScale => 1f - Total(FlightEnhancementKind.Cycle, FlightEnhancements.CooldownLimit); // 카드·아이템 공통 재사용
        public float PickupScale => 1f + 0.20f * Rank(Reward.Magnet); // 자석의 획득 반경
        public float WeakRatio => Rank(Reward.Weak) > 0 ? 0.65f : 0.5f; // 리안의 약점 첫 노드
        public float RapidHold => Rank(Reward.SlowDecay) > 0 ? 3f : 2f; // 아리아의 누적 유지
        public bool HasFan => Rank(Reward.Fan) > 0 || FlightProgress.Current.GetCommonFan(weapon.IsRian ? FlightBroomPilot.Rian : FlightBroomPilot.Aria); // 출격 카드 또는 마을의 공용 산탄 장착
        public int AutoPierce => (int)Total(FlightEnhancementKind.Pierce, FlightEnhancements.PierceLimit); // 최대 3명의 추가 관통
        public int DroneRank => Rank(Reward.Drone); // 장착한 보조 드론 슬롯
        public int BladeRank => Rank(Reward.Blade); // 장착한 두 번째 보조 슬롯
        public bool HasProtectionDevice => Rank(Reward.ProtectionDevice) > 0; // 패시브 보호 장치 슬롯

        internal float ItemSharedCardBonus(FlightEnhancementKind kind) // 드롭의 공통 상한에서 이미 사용한 카드 수치
        {
            return kind switch // 실제로 아이템과 같은 수치를 높이는 카드
            {
                FlightEnhancementKind.Damage => Rank(Reward.Damage) * 0.10f, // 기본 화력 카드
                FlightEnhancementKind.Cadence => Rank(Reward.Cadence) * 0.08f, // 연사 카드
                FlightEnhancementKind.Pierce => Rank(Reward.Pierce) + (weapon.IsRian ? Rank(Reward.SniperPierce) : 0), // 일반·리안 관통
                FlightEnhancementKind.Cycle => Rank(Reward.Cycle) * 0.10f, // Q 재사용
                _ => 0f // 독립적인 다른 아이템
            };
        }

        private float Total(FlightEnhancementKind kind, float ceiling) // 카드와 아이템의 실제 합산
        {
            return Mathf.Min(ceiling, ItemSharedCardBonus(kind) + (enhancements != null ? enhancements.Stored(kind) : 0f)); // 같은 상한을 한 번 적용
        }

        public float DamageScale(bool focused) // 공용·집중 피해의 합산 상한
        {
            float extra = Total(FlightEnhancementKind.Damage, FlightEnhancements.DamageLimit); // 공용 피해와 아이템 합산
            if (focused) // 집중탄 전용 증폭 적용
            {
                extra += Rank(Reward.Focus) * 0.08f + Rank(Reward.Lens) * 0.10f; // 집중 조율과 집속 렌즈
            }
            return 1f + Mathf.Min(FlightEnhancements.DamageLimit, extra); // 모든 카드·아이템 합산 +60%
        }

        public float WidthScale(bool focused) // 공격 폭의 증감
        {
            return focused ? 1f - Rank(Reward.Lens) * 0.10f : 1f + (weapon.IsRian ? 0f : Rank(Reward.Wide) * 0.20f); // 렌즈 또는 아리아 확산
        }

        private int Rank(Reward reward) // 동일한 효과의 현재 단계
        {
            return ranks[(int)reward]; // 현재 출격에서 선택한 값
        }

        private void Awake() // 저장된 카드 버튼의 실제 클릭 연결
        {
            for (int index = 0; index < cards.Length; index++) // 숫자와 마우스 공통 선택
            {
                int option = index; // 현재 버튼 번호 기억
                cards[index].Button.onClick.AddListener(() => RequestChoice(option)); // 클릭 때 동일 효과 선택
            }
        }

        private void OnEnable() // 하이러키에서 편집 가능한 경험치 연결
        {
            experience.LeveledUp += QueueChoices; // 1레벨마다 보상 선택 예약
        }

        private void Start() // 출격 준비에서 선택한 공용기 아이콘 적용
        {
            RefreshCommonSkill(); // 현재 조종사의 실제 E 상태
        }

        internal void RefreshCommonSkill() // 출격 또는 시험 캐릭터 전환
        {
            hud.SetFanSkill(HasFan ? icons[0] : null); // 산탄 또는 기존 보호막 이미지
        }

        private void OnDisable() // 씬을 나갈 때 정지와 마우스 복원
        {
            experience.LeveledUp -= QueueChoices; // 더 이상 새로운 선택 없음
            Resume(); // 다른 씬에 정지 시간이 남지 않음
        }

        public void ResetRun() // 새 출격의 카드 효과 제거
        {
            System.Array.Clear(ranks, 0, ranks.Length); // 이전 강화 단계 초기화
            pending = 0; // 남겨 둔 이전 보상 취소
            picked = -1; // 이전 입력 제거
            RefreshCommonSkill(); // 같은 조종사의 영구 장착으로 복구
            Resume(); // 정지 전 시간으로 복원
        }

        private void QueueChoices(int count, int newLevel) // 경험치 이월 뒤 실제로 상승한 레벨
        {
            if (pending == 0) // 첫 선택이 열리는 경우
            {
                firstLevel = newLevel - count + 1; // 첫 선택에 해당하는 레벨
                priorScale = Time.timeScale; // 이전 시간 규모 보관
                priorCursor = Cursor.visible; // 기존 커서 표시 기록
                priorLock = Cursor.lockState; // 마우스 조준 중 잠금 상태
                paused = true; // 현재 모달 정지 시작
                Time.timeScale = 0f; // 카드 선택 동안 전투 정지
                Cursor.lockState = CursorLockMode.None; // 마우스로 선택 가능한 상태
                Cursor.visible = true; // 선택 커서 표시
                panel.SetActive(true); // 씬에 배치한 실제 카드 창 표시
            }
            pending += count; // 연속 레벨업도 같은 순서로 예약
            Present(); // 현재 유효한 카드 추첨
        }

        private void Present() // 공용·전용·대체 보상에서 선택 후보 준비
        {
            candidates.Clear(); // 직전 선택의 임시 후보 제거
            int profile = weapon.IsRian ? 2 : 1; // 현재 출격 캐릭터
            foreach (Card card in Core) // 기획표 전체 후보 검증
            {
                bool family = card.pilot == 0 || card.pilot == profile; // 현재 캐릭터로 유효
                bool room = card.cap > 0 ? Rank(card.reward) < card.cap : vitals.Health < vitals.MaxHealth; // 최대 단계 또는 회복 제한
                if (family && room && HasRoom(card.reward)) // 실제 효과가 있는 카드만 추첨
                {
                    candidates.Add(card); // 현재 추첨에 포함
                }
            }

            choiceCount = 0; // 새로 표시할 후보 수
            while (choiceCount < offered.Length && candidates.Count > 0) // 세 장을 중복 없이 추첨
            {
                int index = PickCandidate(); // 75·20·5 등급 가중치
                offered[choiceCount++] = candidates[index]; // 이 보상으로 새 선택지 표시
                candidates.RemoveAt(index); // 중복 카드 방지
            }
            foreach (Card card in Fallback) // 후보 부족 시 종류가 다른 보상
            {
                if (choiceCount == offered.Length) // 세 장 모두 준비
                {
                    break; // 추가 후보 채우기 종료
                }
                if ((card.reward == Reward.SmallHeal && vitals.Health >= vitals.MaxHealth) || (card.reward == Reward.Energy && skills.Ultimate >= 100)) // 효과가 없는 즉시 보상
                {
                    continue; // 새로운 유효 보상 확인
                }
                offered[choiceCount++] = card; // 회복·에너지·최대 체력 대체
            }

            heading.text = $"레벨 {firstLevel}  ·  강화 하나 선택"; // 연속 레벨업의 현재 선택 차례
            for (int index = 0; index < choiceCount; index++) // 저장된 각 카드 프리팹 갱신
            {
                Card item = offered[index]; // 실제 클릭에 적용할 후보
                string description = item.cap > 1 ? $"{item.text}\n단계 {Rank(item.reward) + 1}/{item.cap}" : item.text; // 명확한 효과와 누적 상한
                cards[index].Set(icons[item.art], item.title, description, index + 1); // 카드의 레이아웃에 값 적용
            }
        }

        private bool HasRoom(Reward reward) // 공통 상한에 걸려 효과가 없는 후보 제외
        {
            return reward switch // 같은 능력의 저장 결과 검사
            {
                Reward.Damage => DamageScale(false) < 1.5999f, // 기본 화력에 여유
                Reward.Focus => DamageScale(true) < 1.5999f, // 집중탄에 여유
                Reward.Cadence => CadenceScale > 0.6001f, // 발사 간격에 여유
                Reward.Cycle => CooldownScale > 0.6001f, // 고유기 쿨타임에 여유
                Reward.Pierce or Reward.SniperPierce => AutoPierce < FlightEnhancements.PierceLimit, // 추가 관통에 여유
                Reward.Fan => !HasFan, // 이미 산탄을 쓰는 조종사에게 같은 교체 제외
                _ => true // 다른 종류의 독립 효과
            };
        }

        private int PickCandidate() // 해당하는 등급의 75·20·5 가중치
        {
            int[] tiers = { 0, 0, 0 }; // 현재 남은 등급별 후보 수
            foreach (Card card in candidates) // 유효한 후보 집계
            {
                tiers[card.rarity]++; // 해당 등급의 모집단
            }
            int common = tiers[0] > 0 ? 75 : 0; // 일반이 없으면 가중치 0
            int rare = tiers[1] > 0 ? 20 : 0; // 희귀가 없으면 가중치 0
            int roll = Random.Range(0, common + rare + (tiers[2] > 0 ? 5 : 0)); // 남은 등급 재정규화
            int tier = roll < common ? 0 : (roll < common + rare ? 1 : 2); // 이번 추첨 등급
            int offset = Random.Range(0, tiers[tier]); // 같은 등급에서는 균등 선택
            for (int index = 0; index < candidates.Count; index++) // 선택된 카드 위치 탐색
            {
                if (candidates[index].rarity == tier && offset-- == 0) // 해당 등급 후보 번호
                {
                    return index; // 현재 목록의 실제 인덱스
                }
            }
            return 0; // 유효 후보 목록의 기본 위치
        }

        private void RequestChoice(int index) // 버튼에서 결정한 선택 예약
        {
            if (paused && picked < 0 && index >= 0 && index < choiceCount) // 같은 프레임의 중복 입력 차단
            {
                picked = index; // 전투 명령이 끝난 뒤 적용
            }
        }

        private void LateUpdate() // 선택을 끝낸 다음 프레임에만 전투 재개
        {
            if (!paused) // 모달이 열린 동안만 확인
            {
                return; // 일반 전투 상태
            }
            Keyboard keyboard = Keyboard.current; // 숫자 선택 입력
            if (keyboard != null) // 사용 가능한 키보드
            {
                int number = keyboard.digit1Key.wasPressedThisFrame ? 0 : (keyboard.digit2Key.wasPressedThisFrame ? 1 : (keyboard.digit3Key.wasPressedThisFrame ? 2 : -1)); // 선택한 숫자 키
                RequestChoice(number); // 클릭과 같은 적용 경로
            }
            if (picked < 0) // 아직 선택을 기다리는 상태
            {
                return; // 현재 출격 계속 정지
            }

            Reward reward = offered[picked].reward; // 실제로 표시한 보상
            picked = -1; // 같은 입력으로 연속 선택 방지
            Apply(reward); // 선택한 효과 하나만 적용
            pending--; // 레벨 하나 소비
            if (pending > 0) // 한 번 획득에 레벨이 더 오른 상태
            {
                firstLevel++; // 다음 레벨의 별도 카드
                Present(); // 효과 적용 뒤 조건으로 다시 추첨
                return; // 다음 선택까지 전투 중단 유지
            }
            Resume(); // 마지막 카드 선택 후 복귀
        }

        private void Apply(Reward reward) // 이번 출격에 선택 효과 적용
        {
            switch (reward) // 즉시 효과와 누적 효과 구분
            {
                case Reward.Heal: // 응급 회복
                    vitals.Heal(20); // 최대 체력까지 20 회복
                    return; // 즉시 회복은 랭크 없음
                case Reward.SmallHeal: // 대체 회복
                    vitals.Heal(10); // 최대 체력까지 10 회복
                    return; // 즉시 회복은 랭크 없음
                case Reward.Energy: // 대체 필살기 충전
                    skills.AddUltimate(5); // 실제 필살기 자원
                    return; // 즉시 자원은 랭크 없음
                case Reward.HpOne: // 최대 체력 대체 보상
                case Reward.HpTwo: // 최대 체력 대체 보상
                case Reward.HpThree: // 최대 체력 대체 보상
                    vitals.AddCapacity(1 + (int)reward - (int)Reward.HpOne); // 선택한 최대 체력 1·2·3 적용
                    return; // 즉시 영구가 아닌 이번 출격의 체력
                case Reward.Fan: // 공용 E 교체
                    vitals.ClearSkillGuard(); // 기존 E에서 남은 보호막 해제
                    hud.SetFanSkill(icons[0]); // 교체된 산탄의 불꽃 아이콘
                    break; // 새 E를 사용할 수 있게 기록
            }
            ranks[(int)reward]++; // 선택 카드의 누적 단계 증가
        }

        private void Resume() // 전투 정지와 커서 복구
        {
            if (!paused) // 이 화면이 정지한 경우만 복원
            {
                return; // 다른 일시정지는 그대로 유지
            }
            paused = false; // 정지 보유 해제
            panel.SetActive(false); // 선택 화면 닫기
            Cursor.lockState = priorLock; // 원래 전투 마우스 상태
            Cursor.visible = priorCursor; // 이전 커서 표시 복귀
            Time.timeScale = priorScale; // 실제 전투 시간의 이전 배율
        }
    }
}
