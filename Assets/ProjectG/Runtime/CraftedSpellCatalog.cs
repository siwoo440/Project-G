using System; // 수정 불가능한 목록
using System.Collections.Generic; // 고정 비용 조회

namespace ProjectG // 프로젝트 게임 영역
{
    public enum CraftedSpellGroup // 조합 편집기의 고정 분류
    {
        Element, // 속성
        Shape, // 탄환 형태
        Formation, // 발사 배열
        Trajectory, // 이동 궤적
        FireMode, // 발사 방식
        Origin, // 생성 위치
        Aim, // 조준 방식
        Effect, // 특수 효과
        Trigger // 발동 조건
    }

    public readonly struct CraftedSpellChoice // 분류 안의 조합 선택지
    {
        public readonly string Id; // 편성 ID로 쓰는 안정 키
        public readonly string Label; // 기획서의 한국어 명칭
        public readonly int Cost; // 고정 편성 비용

        public CraftedSpellChoice(string id, string label, int cost) // 데이터의 표시명과 원래 비용
        {
            Id = id; // 문자열 식별자
            Label = label; // 한국어 표시명
            Cost = cost; // 구성 비용 정수
        }
    }

    public static class CraftedSpellCatalog // 기획서의 9개 고정 비용표
    {
        private static readonly IReadOnlyList<CraftedSpellChoice>[] Groups = // 분류 순서의 선택지 목록
        {
            Array.AsReadOnly(new CraftedSpellChoice[] // 속성 문서 비용
            {
                new("fire", "화염", 0), // 화염의 고정 비용
                new("water", "물", 0), // 물의 고정 비용
                new("grass", "풀", 0), // 풀의 고정 비용
                new("light", "빛", 0), // 빛의 고정 비용
                new("dark", "어둠", 0) // 어둠의 고정 비용
            }),
            Array.AsReadOnly(new CraftedSpellChoice[] // 탄환 형태 문서 비용
            {
                new("orb", "구체탄", 10), // 구체탄의 고정 비용
                new("small", "소형탄", 8), // 소형탄의 고정 비용
                new("needle", "침탄", 12), // 침탄의 고정 비용
                new("crystal", "결정탄", 12), // 결정탄의 고정 비용
                new("arrow", "화살탄", 12), // 화살탄의 고정 비용
                new("large", "대형탄", 14), // 대형탄의 고정 비용
                new("blade", "검형탄", 14), // 검형탄의 고정 비용
                new("disc", "원반탄", 14), // 원반탄의 고정 비용
                new("missile", "미사일탄", 14), // 미사일탄의 고정 비용
                new("bomb", "폭발탄", 14), // 폭발탄의 고정 비용
                new("wave", "파동탄", 15), // 파동탄의 고정 비용
                new("laser", "레이저", 18), // 레이저의 고정 비용
                new("beam", "지속 광선", 24), // 지속 광선의 고정 비용
                new("placed", "설치탄", 14), // 설치탄의 고정 비용
                new("barrier", "결계탄", 14), // 결계탄의 고정 비용
                new("summon", "소환탄", 20) // 소환탄의 고정 비용
            }),
            Array.AsReadOnly(new CraftedSpellChoice[] // 발사 배열 문서 비용
            {
                new("single", "단발형", 0), // 단발형의 고정 비용
                new("random", "무작위형", 3), // 무작위형의 고정 비용
                new("parallel", "병렬형", 4), // 병렬형의 고정 비용
                new("fan", "부채꼴형", 4), // 부채꼴형의 고정 비용
                new("cross", "교차형", 4), // 교차형의 고정 비용
                new("converge", "집중형", 4), // 집중형의 고정 비용
                new("ring", "원형", 5), // 원형의 고정 비용
                new("cone", "원뿔형", 6), // 원뿔형의 고정 비용
                new("sphere", "구형", 6), // 구형의 고정 비용
                new("spiral", "나선형", 6), // 나선형의 고정 비용
                new("grid", "격자형", 6), // 격자형의 고정 비용
                new("wall", "벽형", 6), // 벽형의 고정 비용
                new("satellite", "위성형", 6), // 위성형의 고정 비용
                new("cylinder", "원통형", 7) // 원통형의 고정 비용
            }),
            Array.AsReadOnly(new CraftedSpellChoice[] // 이동 궤적 문서 비용
            {
                new("straight", "직선형", 0), // 직선형의 고정 비용
                new("stationary", "정지형", 0), // 정지형의 고정 비용
                new("accelerate", "가속형", 3), // 가속형의 고정 비용
                new("decelerate", "감속형", 3), // 감속형의 고정 비용
                new("curve", "곡선형", 3), // 곡선형의 고정 비용
                new("arc", "포물선형", 3), // 포물선형의 고정 비용
                new("zigzag", "지그재그형", 4), // 지그재그형의 고정 비용
                new("wave", "파동형", 4), // 파동형의 고정 비용
                new("random", "무작위형", 4), // 무작위형의 고정 비용
                new("spiral", "나선형", 5), // 나선형의 고정 비용
                new("return", "회귀형", 5), // 회귀형의 고정 비용
                new("switch", "전환형", 5), // 전환형의 고정 비용
                new("orbit", "공전형", 6), // 공전형의 고정 비용
                new("bounce", "반사형", 6), // 반사형의 고정 비용
                new("homing", "유도형", 8), // 유도형의 고정 비용
                new("gravity", "중력형", 8) // 중력형의 고정 비용
            }),
            Array.AsReadOnly(new CraftedSpellChoice[] // 발사 방식 문서 비용
            {
                new("single", "단발", 0), // 단발의 고정 비용
                new("placed", "설치", 2), // 설치의 고정 비용
                new("burst", "점사", 3), // 점사의 고정 비용
                new("rapid", "연사", 4), // 연사의 고정 비용
                new("charge", "충전", 4), // 충전의 고정 비용
                new("queued", "예약", 4), // 예약의 고정 비용
                new("conditional", "조건부", 4), // 조건부의 고정 비용
                new("held", "지속", 6), // 지속의 고정 비용
                new("summon", "소환", 6) // 소환의 고정 비용
            }),
            Array.AsReadOnly(new CraftedSpellChoice[] // 생성 위치 문서 비용
            {
                new("front", "플레이어 전방", 0), // 플레이어 전방의 고정 비용
                new("around", "플레이어 주변", 2), // 플레이어 주변의 고정 비용
                new("rear", "빗자루 후방", 2), // 빗자루 후방의 고정 비용
                new("summon", "소환체", 3), // 소환체의 고정 비용
                new("projectile", "기존 탄환 위치", 3), // 기존 탄환 위치의 고정 비용
                new("point", "지정 좌표", 4), // 지정 좌표의 고정 비용
                new("target", "목표물 주변", 5), // 목표물 주변의 고정 비용
                new("path", "비행 경로", 5) // 비행 경로의 고정 비용
            }),
            Array.AsReadOnly(new CraftedSpellChoice[] // 조준 방식 문서 비용
            {
                new("manual", "수동 조준", 0), // 수동 조준의 고정 비용
                new("auto", "자동 조준", 5), // 자동 조준의 고정 비용
                new("homing", "유도 조준", 0) // 유도 조준의 고정 비용
            }),
            Array.AsReadOnly(new CraftedSpellChoice[] // 특수 효과 문서 비용
            {
                new("none", "효과 없음", 0), // 효과 없음의 고정 비용
                new("knockback", "넉백", 4), // 넉백의 고정 비용
                new("delay", "지연 발동", 4), // 지연 발동의 고정 비용
                new("slow", "둔화", 5), // 둔화의 고정 비용
                new("retarget", "추적 전환", 5), // 추적 전환의 고정 비용
                new("pierce", "관통", 6), // 관통의 고정 비용
                new("burn", "화상", 6), // 화상의 고정 비용
                new("root", "속박", 8), // 속박의 고정 비용
                new("dot", "지속 피해", 8), // 지속 피해의 고정 비용
                new("linger", "잔류", 8), // 잔류의 고정 비용
                new("explode", "폭발", 10), // 폭발의 고정 비용
                new("stack-explosion", "중첩 폭발", 10), // 중첩 폭발의 고정 비용
                new("freeze", "빙결", 10), // 빙결의 고정 비용
                new("pull", "끌어당기기", 10), // 끌어당기기의 고정 비용
                new("chain", "연쇄", 12), // 연쇄의 고정 비용
                new("cancel", "탄막 상쇄", 12), // 탄막 상쇄의 고정 비용
                new("reflect", "탄막 반사", 12), // 탄막 반사의 고정 비용
                new("slow-bullets", "탄막 감속", 12), // 탄막 감속의 고정 비용
                new("teleport", "좌표 전이", 12), // 좌표 전이의 고정 비용
                new("split", "분열", 16), // 분열의 고정 비용
                new("absorb", "탄막 흡수", 16) // 탄막 흡수의 고정 비용
            }),
            Array.AsReadOnly(new CraftedSpellChoice[] // 발동 조건 문서 비용
            {
                new("immediate", "즉시", 0), // 즉시의 고정 비용
                new("hit", "명중", 0), // 명중의 고정 비용
                new("collision", "충돌", 0), // 충돌의 고정 비용
                new("time", "시간 경과", 0), // 시간 경과의 고정 비용
                new("distance", "거리 도달", 0), // 거리 도달의 고정 비용
                new("approach", "적 접근", 3), // 적 접근의 고정 비용
                new("kill", "적 처치", 4), // 적 처치의 고정 비용
                new("cancel", "탄막 상쇄", 4), // 탄막 상쇄의 고정 비용
                new("repeat", "재입력", 4) // 재입력의 고정 비용
            })
        };

        public static IReadOnlyList<CraftedSpellChoice> Choices(CraftedSpellGroup group) // 분류의 기본값과 선택할 항목
        {
            return Groups[(int)group]; // 현재 분류의 편집 불가 목록
        }

        internal static int FindCost(CraftedSpellGroup group, string id) // 유효한 선택지의 비용
        {
            foreach (CraftedSpellChoice choice in Choices(group)) // 순서대로 고정 선택지 확인
            {
                if (choice.Id == id) // 지정한 분류와 선택지
                {
                    return choice.Cost; // 해당 선택의 고정 비용
                }
            }
            return -1; // 다른 분류의 ID 또는 없는 선택지
        }
    }
}
