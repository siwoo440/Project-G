using System; // 부품 데이터 직렬화
using UnityEngine; // 부품 에셋과 외형 자료

namespace ProjectG // 프로젝트 게임 영역
{
    public enum FlightBroomPilot // 부품 조합을 가진 캐릭터
    {
        Aria, // 아리아
        Rian, // 리안
        Lumi, // 루미
        Sera, // 세라
        Nox, // 녹스
        Vera, // 베라
        Iris // 이리스
    }

    public enum FlightBroomSlot // 빗자루의 장착 부위
    {
        Body, // 몸체
        Grip, // 손잡이
        ManaStone, // 마력석
        Coupling, // 결합부
        Propulsion, // 추진부
        Ornament, // 마법 장식
        Tip, // 선단부
        Stabilizer // 안정기
    }

    public enum FlightBroomStat // 부품으로 조정하는 수치
    {
        Speed, // 최대 비행 속도
        Acceleration, // 가속력
        Braking, // 제동력
        Turn, // 선회 속도
        Stability, // 비행 안정성
        Rise, // 상승 속도
        Fall, // 하강 속도
        Mana, // 최대 마력
        ManaRecovery, // 초당 마력 회복량
        MagicPower, // 마법 공격력
        BoltSpeed, // 탄환 속도 퍼센트포인트
        Range, // 마법 사거리 퍼센트포인트
        DamageReduction, // 피해 감소 퍼센트포인트
        ImpactResistance // 충격 저항 퍼센트포인트
    }

    [Serializable] // 카탈로그 안의 부품 종류
    internal sealed class FlightBroomPart
    {
        public string id; // 부품 종류의 고유 ID
        public string displayName; // 한글 부품 이름
        public FlightBroomSlot slot; // 장착 가능 부위
        public FlightBroomStat stat; // 주 효과 항목
        public float baseValue; // I등급의 기본 효과
        public float gradeStep; // 등급 한 단계 추가 효과
        public float upgradeStep; // 강화 한 단계 추가 효과
        public FlightBroomStat penaltyStat; // 불이익 적용 항목
        public float penalty; // 불이익의 고정값
        public bool starter; // 기본 장착 여부
        public FlightBroomPilot pilot; // 기본 장착 캐릭터
        [Min(0)] public int ownedCopies = 1; // 시작 보유 수량 또는 상점 전용 0개
        public bool stocked; // 마을 상점 초기 판매 목록
        [Range(1, 5)] public int grade = 1; // 시작 보유 등급
        [Min(0)] public int upgrade; // 시작 강화 단계
        public Material material; // 장착 시 렌더러 재질
        public Vector3 scale = Vector3.one; // 부착점 기준 외형 배율

        public float GetValue(int itemGrade, int itemUpgrade) // 개별 보유 부품의 강화 효과
        {
            return baseValue + gradeStep * (itemGrade - 1) + upgradeStep * itemUpgrade; // 기본값과 단계별 증가량
        }
    }

    public sealed class FlightBroomCatalog : ScriptableObject // 편집 가능한 빗자루 부품 목록
    {
        [SerializeField] private FlightBroomPart[] parts = Array.Empty<FlightBroomPart>(); // 기본 부품과 시험 부품
        internal ReadOnlySpan<FlightBroomPart> Parts => parts; // 장착 구성에서 읽는 부품 목록
    }
}
