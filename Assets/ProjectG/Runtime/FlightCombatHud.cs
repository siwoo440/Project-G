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
            public int cost; // 현재 슬롯의 시험 자원 비용
            public bool usesUltimate; // 마력과 필살기 에너지 구분
            [System.NonSerialized] public float readyAt; // 런타임 재사용 가능 시각
            private int previousSeconds = -1; // 숫자의 갱신 필요 상태

            public void Render(int resource) // 최신 자원과 쿨타임 시각화
            {
                int left = Mathf.CeilToInt(Mathf.Max(0f, readyAt - Time.time)); // 올림된 남은 초
                art.color = resource < cost || left > 0 ? new Color(0.28f, 0.28f, 0.35f, 1f) : Color.white; // 부족·대기 중 어두운 실루엣
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
        [SerializeField] private SkillSlot[] slots; // J·K·L 순서의 제작 슬롯
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

        public void SetCooldown(int index, float seconds) // 대상 스킬의 남은 재사용 시간 전달
        {
            if (index >= 0 && index < slots.Length) // 존재하는 슬롯 확인
            {
                slots[index].readyAt = Time.time + Mathf.Max(0f, seconds); // 종료까지 표시할 시간
            }
        }

        private void LateUpdate() // 현재 전투 화면에 상태 적용
        {
            health.Set(pilot.Health, pilot.MaxHealth, "HP"); // 실제 피격 체력
            mana.Set(manaPoints, MaxResource, "MANA"); // 현재 마력
            ultimate.Set(ultimatePoints, MaxResource, "ULT"); // 필살기 에너지
            experience.Set(xp, nextXp, "XP", level); // 현재 레벨과 경험치
            foreach (SkillSlot slot in slots) // 편집된 세 슬롯 표시
            {
                slot.Render(slot.usesUltimate ? ultimatePoints : manaPoints); // 스킬별로 해당 자원 적용
            }
        }
    }
}
