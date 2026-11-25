using UnityEngine; // 전투 상태와 프레임 시간
using UnityEngine.UI; // 제작 이미지와 숫자 표시

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCombatHud : MonoBehaviour // 비행 중 게이지와 스킬 슬롯
    {
        [System.Serializable] // 인스펙터에 표시할 게이지 참조
        private sealed class Meter // 사각형 이미지 채움
        {
            public RawImage fill; // 제작된 게이지 색상 이미지
            public Text label; // 현재 수치 라벨
            private float width; // 최대 체력일 때의 가로 폭
            private Rect artwork; // 투명 패딩을 제외한 이미지 범위
            private int previousValue = -1; // 변경 여부 확인용 수치
            private int previousMax = -1; // 변경 여부 확인용 최대 수치
            private int previousLevel = -1; // 변경 여부 확인용 레벨

            public void Initialize() // 씬의 편집 영역 기억
            {
                width = fill.rectTransform.rect.width; // 원래 제작 이미지 너비
                artwork = fill.uvRect; // 원래 제작 이미지 영역
            }

            public void Set(int value, int maximum, string labelName, int level = 0) // 실제 체력과 자원 표시
            {
                if (value == previousValue && maximum == previousMax && level == previousLevel) // 이미지 변경 필요 확인
                {
                    return; // 같은 수치의 레이아웃 갱신 생략
                }
                previousValue = value; // 새 값 기록
                previousMax = maximum; // 새 범위 기록
                previousLevel = level; // 레벨 변경 기록
                float ratio = Mathf.Clamp01((float)value / maximum); // 현재 남은 비율
                Rect showing = artwork; // 제작 이미지 원래 범위
                showing.width *= ratio; // 오른쪽에서 남은 길이만 표시
                fill.uvRect = showing; // 채움 이미지를 늘이지 않는 범위
                fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * ratio); // 왼쪽 기준으로 줄어드는 길이
                label.text = level > 0 ? $"LV. {level}   {labelName} {value} / {maximum}" : $"{labelName} {value} / {maximum}"; // 줄 안의 실제 값
            }
        }

        [System.Serializable] // 씬에 배치된 스킬 아이콘 접근
        private sealed class SkillSlot // 제작한 스킬 이미지와 숫자
        {
            public RawImage art; // 해당 스킬의 제작 이미지
            public Text cooldown; // 흰색으로 표시할 남은 시간
            public Text keyLabel; // 설정에서 바뀌는 현재 스킬 키
            public int cost; // 현재 슬롯의 시험 자원 비용
            public bool usesUltimate; // 마력과 필살기 에너지 구분
            [System.NonSerialized] public float countdownFrom; // 효과 뒤 쿨타임의 시작 시각
            [System.NonSerialized] public float readyAt; // 런타임 재사용 가능 시각
            private int previousSeconds = -1; // 숫자의 갱신 필요 상태

            public void Render(int resource, bool blocked) // 최신 자원과 쿨타임 시각화
            {
                int left = Time.time >= countdownFrom ? Mathf.CeilToInt(Mathf.Max(0f, readyAt - Time.time)) : 0; // 올림된 남은 초
                art.color = blocked || resource < cost || Time.time < readyAt ? new Color(0.28f, 0.28f, 0.35f, 1f) : Color.white; // 부족·대기 중 어두운 실루엣
                if (left != previousSeconds) // 숫자가 실제 바뀐 프레임
                {
                    cooldown.text = left > 0 ? left.ToString() : string.Empty; // 흰색 숫자 또는 빈 슬롯
                    previousSeconds = left; // 이전 정수 초 보관
                }
            }
        }

        [SerializeField] private FlightPilotVitals pilot; // 현재 비행체 체력
        [SerializeField] private Meter health = new Meter(); // 빨간 체력 게이지
        [SerializeField] private Meter mana = new Meter(); // 파란 마력 게이지
        [SerializeField] private Meter ultimate = new Meter(); // 보라 필살기 에너지
        [SerializeField] private Meter experience = new Meter(); // 노란 경험치 게이지
        [SerializeField] private SkillSlot[] slots; // Q·E·R 순서의 제작 슬롯
        [SerializeField] private FlightRapidFire rapid; // 현재 캐릭터의 누적 화력 상태
        [SerializeField] private Text rapidLabel; // 게이지 아래의 보너스
        [SerializeField] private Texture sniperSkillArt; // 리안 Q·R 시험 아이콘 두 칸
        [SerializeField] private Texture trackedSkillAtlas; // 탐색자 스킬 아이콘 시안
        [SerializeField] private Texture trackedUltimateAtlas; // 탐색자 필살기 아이콘 시안
        private FlightWeapon currentWeapon; // 추적 시간에서 패시브 숫자를 읽는 실제 무기
        private Texture ariaSkillArt; // 아리아의 원래 Q 스킬 그림
        private Texture ariaUltimateArt; // 아리아의 원래 R 스킬 그림
        private Rect ariaSkillUv; // 아리아 Q의 아틀라스 영역
        private Rect ariaUltimateUv; // 아리아 R의 아틀라스 영역
        private Texture guardArt; // 기본 E 보호막의 원본 그림
        private Rect guardUv; // 기본 E 보호막의 아틀라스 범위

        private const int MaxResource = 100; // 초기 시험 자원의 최대값
        private int manaPoints = MaxResource; // 시작 시 마력
        private int ultimatePoints; // 시작 시 필살기 에너지
        private int level = 1; // 시작 레벨
        private int xp; // 현재 경험치
        private int nextXp = 10; // 시제품용 레벨 게이지 범위

        private void Awake() // 게이지의 편집 당시 크기 저장
        {
            health.Initialize(); // 체력 크기
            mana.Initialize(); // 마력 크기
            ultimate.Initialize(); // 필살기 에너지 크기
            experience.Initialize(); // 경험치 크기
            ariaSkillArt = slots[0].art.texture; // 기존 Q 아이콘의 원본 저장
            ariaUltimateArt = slots[2].art.texture; // 기존 R 아이콘의 원본 저장
            ariaSkillUv = slots[0].art.uvRect; // 기존 Q 아틀라스 좌표
            ariaUltimateUv = slots[2].art.uvRect; // 기존 R 아틀라스 좌표
            guardArt = slots[1].art.texture; // 기본 E 그림
            guardUv = slots[1].art.uvRect; // 기본 E 범위
        }

        public void SetFanSkill(Texture fan) // 교체한 E 선택과 화면 그림 동기화
        {
            slots[1].art.texture = fan != null ? fan : guardArt; // 산탄이나 원래 보호막 그림
            slots[1].art.uvRect = fan != null ? new Rect(0f, 0f, 1f, 1f) : guardUv; // 산탄 전체 그림 또는 아틀라스 조각
        }

        public void SetSkillKeys(string special, string common, string ultimateKey) // 설명과 일치하는 세 스킬 입력
        {
            slots[0].keyLabel.text = special; // 현재 고유기 키
            slots[1].keyLabel.text = common; // 현재 공용 키
            slots[2].keyLabel.text = ultimateKey; // 현재 필살기 키
        }

        public void SetPilot(FlightWeapon weapon) // 조종사의 Q·R 아이콘과 패시브 전환
        {
            currentWeapon = weapon; // 패시브 숫자를 계산하는 실제 무기
            int artIndex = (int)weapon.ActivePilot - (int)FlightBroomPilot.Lumi; // 아틀라스에 배치한 추가 조종사의 순서
            if (artIndex >= 0 && trackedSkillAtlas != null && trackedUltimateAtlas != null) // 추가 조종사의 두 전용 아이콘
            {
                slots[0].art.texture = trackedSkillAtlas; // 별빛 추적탄 아이콘 아틀라스
                slots[2].art.texture = trackedUltimateAtlas; // 별무리 항로 아이콘 아틀라스
                slots[0].art.uvRect = new Rect((artIndex % 3) / 3f, artIndex < 3 ? 0.5f : 0f, 1f / 3f, 0.5f); // 현재 조종사의 전용 Q 그림
                slots[2].art.uvRect = slots[0].art.uvRect; // 같은 위치의 전용 R 그림
                return; // 기본 조종사의 그림을 적용하지 않음
            }
            bool active = weapon.IsRian && sniperSkillArt != null; // 리안의 이미지가 연결된 현재 씬
            slots[0].art.texture = active ? sniperSkillArt : ariaSkillArt; // 리안 Q 또는 아리아 Q
            slots[0].art.uvRect = active ? new Rect(0f, 0f, 0.5f, 1f) : ariaSkillUv; // 왼쪽 아이콘만 표시
            slots[2].art.texture = active ? sniperSkillArt : ariaUltimateArt; // 리안 R 또는 아리아 R
            slots[2].art.uvRect = active ? new Rect(0.5f, 0f, 0.5f, 1f) : ariaUltimateUv; // 오른쪽 아이콘만 표시
        }

        public void SetResources(int currentMana, int currentUltimate) // 전투에서 변경된 자원 상태 전달
        {
            manaPoints = Mathf.Clamp(currentMana, 0, MaxResource); // 현재 마력 보정
            ultimatePoints = Mathf.Clamp(currentUltimate, 0, MaxResource); // 현재 필살기 에너지 보정
        }

        public void SetProgress(int currentLevel, int currentXp, int requiredXp) // 레벨과 경험치 상태 전달
        {
            level = Mathf.Max(1, currentLevel); // 현재 레벨 표시 기준
            nextXp = Mathf.Max(1, requiredXp); // 분모가 될 경험치 범위
            xp = Mathf.Clamp(currentXp, 0, nextXp); // 현재 경험치의 실제 채움
        }

        public void SetCooldown(int index, float seconds) // 즉시 시작하는 재사용 시간
        {
            SetDelayedCooldown(index, seconds, 0f); // 대기 없이 재사용 숫자 표시
        }

        public void SetDelayedCooldown(int index, float seconds, float startsIn) // 효과 뒤 시작하는 카운트다운
        {
            if (index >= 0 && index < slots.Length) // 존재하는 슬롯 확인
            {
                slots[index].countdownFrom = Time.time + Mathf.Max(0f, startsIn); // 효과 종료 뒤에만 숫자 표시
                slots[index].readyAt = slots[index].countdownFrom + Mathf.Max(0f, seconds); // 실제 다시 쓸 수 있는 시각
            }
        }
        private void LateUpdate() // 현재 전투 화면에 상태 적용
        {
            health.Set(pilot.Health, pilot.MaxHealth, "HP"); // 실제 피격 체력
            mana.Set(manaPoints, MaxResource, "MANA"); // 현재 마력
            ultimate.Set(ultimatePoints, MaxResource, "ULT"); // 필살기 에너지
            experience.Set(xp, nextXp, "XP", level); // 현재 레벨과 경험치
            bool nox = currentWeapon != null && currentWeapon.ActivePilot == FlightBroomPilot.Nox; // 반격 표식이 있는 실제 출격
            int marks = nox && currentWeapon.Counter != null ? currentWeapon.Counter.Marks : 0; // 현재 사용 가능한 녹스 표식
            bool vera = currentWeapon != null && currentWeapon.ActivePilot == FlightBroomPilot.Vera; // 현재 살아 있는 포대의 조종사
            FlightVeraTurret turret = vera ? currentWeapon.Turret : null; // 포대 상태를 읽는 실제 오브젝트
            bool hasTurret = turret != null && turret.IsDeployed; // 필살기의 설치 선행 조건
            if (vera) // 베라에게 보이는 포대 상태와 근처 보너스
            {
                rapidLabel.text = hasTurret ? $"포대 {turret.SecondsLeft}초{(turret.DamageMultiplier > 1f ? "  +20%" : "")}" : "포대 미설치"; // 실제 남은 시간과 반경 안 자동탄 보너스
            }
            else if (nox) // 표시할 녹스의 수치를 세 스킬과 공유
            {
                rapidLabel.text = $"반격 표식 {marks} / {FlightNoxCombat.MarkLimit}"; // 현재 출격에서 모은 실제 표식
            }
            else if (currentWeapon != null && currentWeapon.ActivePilot == FlightBroomPilot.Sera) // 세라의 근거리 효과 정보
            {
                rapidLabel.text = "근거리 결의 +25%"; // 검기와 직접 공격에 적용할 패시브 수치
            }
            else if (currentWeapon != null && currentWeapon.ActivePilot == FlightBroomPilot.Lumi) // 루미의 실제 추적 성능 표시
            {
                rapidLabel.text = $"표적 추적 +{currentWeapon.LumiTrackingBonus}%"; // 적을 유지한 실제 초 단위 보너스
            }
            else if (currentWeapon != null && currentWeapon.IsRian) // 리안의 약점 피해 안내
            {
                rapidLabel.text = "리안  약점 +50%"; // HUD 안에 들어가는 리안 약점 보너스
            }
            else if (rapid != null) // 아리아의 누적 값을 사용하는 슬롯
            {
                rapidLabel.text = $"연속 적중 +{Mathf.FloorToInt(rapid.BonusPercent + 0.003f)}%{(rapid.IsDecaying ? " 감소" : "")}"; // 감소하는 연속 적중 정보
            }
            for (int index = 0; index < slots.Length; index++) // 편집된 세 슬롯 표시
            {
                SkillSlot slot = slots[index]; // Q·E·R 순서의 실제 슬롯
                bool blocked = (nox && index == 0 && marks == 0) || (vera && index == 2 && !hasTurret); // 추가 캐릭터의 스킬 조건
                slot.Render(slot.usesUltimate ? ultimatePoints : manaPoints, blocked); // 표식 또는 자원 부족의 어두운 그림
            }
        }
    }
}
