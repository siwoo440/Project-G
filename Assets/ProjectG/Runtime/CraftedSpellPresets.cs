using System; // 불변 시험 도감과 구성 검사
using System.Collections.Generic; // 읽기 전용 카드 목록

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class CraftedSpellPreset // 이름과 검증된 구성의 쌍
    {
        public string Key { get; } // 도감의 변하지 않는 항목 키
        public string Name { get; } // 기획서의 한국어 마법 이름
        public CraftedSpellRecipe Recipe { get; } // 동일 규칙을 통과한 구성

        internal CraftedSpellPreset(string key, string name, CraftedSpellRecipe recipe) // 기획서 예시의 등록 정보
        {
            Key = key; // 에디터와 저장에 쓰는 카드 키
            Name = name; // 선택 화면의 이름
            Recipe = recipe; // 발사와 편성에서 공유할 조합
        }
    }

    public static class CraftedSpellPresets // 문서의 15종과 유도 시험 구성
    {
        public static IReadOnlyList<CraftedSpellPreset> All { get; } = Array.AsReadOnly(new[] // 도감 순서의 검증된 예시
        {
            Define("fire-bolt", "파이어 볼트", "fire", "orb", "single", "straight", "single", "front", "manual", "hit", "explode", "none", 1, 0, 0), // 파이어 볼트의 고정 조합
            Define("flame-spiral", "플레임 스파이럴", "fire", "small", "spiral", "straight", "single", "front", "manual", "hit", "burn", "none", 12, 0, 0), // 플레임 스파이럴의 고정 조합
            Define("ember-trail", "잿불 항로", "fire", "placed", "single", "stationary", "placed", "path", "manual", "immediate", "linger", "none", 1, 0, 0), // 잿불 항로의 고정 조합
            Define("water-pressure", "수압 파동", "water", "wave", "fan", "straight", "single", "front", "manual", "hit", "knockback", "none", 3, 0, 0), // 수압 파동의 고정 조합
            Define("torrent-needle", "급류 관통탄", "water", "needle", "converge", "straight", "single", "front", "manual", "hit", "pierce", "none", 3, 0, 0), // 급류 관통탄의 고정 조합
            Define("returning-ring", "회류 고리", "water", "disc", "ring", "return", "single", "front", "manual", "immediate", "none", "none", 6, 0, 0), // 회류 고리의 고정 조합
            Define("vine-ring", "덩굴 고리", "grass", "placed", "single", "stationary", "placed", "point", "manual", "immediate", "root", "none", 1, 0, 0), // 덩굴 고리의 고정 조합
            Define("thorn-volley", "가시 산탄", "grass", "needle", "cone", "straight", "single", "front", "manual", "immediate", "none", "none", 8, 0, 0), // 가시 산탄의 고정 조합
            Define("seed-turret", "씨앗 포대", "grass", "placed", "single", "stationary", "placed", "front", "manual", "time", "delay", "split", 1, 3, 1), // 씨앗 포대의 고정 조합
            Define("holy-shower", "홀리 샤워", "light", "arrow", "cone", "straight", "single", "front", "manual", "hit", "pierce", "none", 6, 0, 0), // 홀리 샤워의 고정 조합
            Define("luminous-ray", "루미너스 레이", "light", "beam", "single", "straight", "held", "front", "manual", "immediate", "dot", "none", 1, 0, 0), // 루미너스 레이의 고정 조합
            Define("purification", "정화 결계", "light", "barrier", "sphere", "stationary", "placed", "around", "manual", "immediate", "cancel", "none", 1, 0, 0), // 정화 결계의 고정 조합
            Define("dark-orbit", "다크 오비트", "dark", "orb", "satellite", "orbit", "single", "around", "manual", "immediate", "absorb", "none", 4, 0, 0), // 다크 오비트의 고정 조합
            Define("shadow-mark", "그림자 낙인", "dark", "needle", "single", "straight", "single", "front", "manual", "hit", "dot", "none", 1, 0, 0), // 그림자 낙인의 고정 조합
            Define("dusk-rupture", "황혼 파열", "dark", "bomb", "single", "stationary", "single", "point", "manual", "time", "delay", "explode", 1, 0, 0), // 황혼 파열의 고정 조합
            Define("homing-trial", "유도 시험탄", "water", "needle", "single", "homing", "single", "front", "homing", "immediate", "none", "none", 1, 0, 0) // 유도 시험탄의 고정 조합
        });

        private static CraftedSpellPreset Define(string key, string name, string element, string shape, string formation, string trajectory, string mode, string origin, string aim, string trigger, string first, string second, int initial, int children, int depth) // 기획 예시를 실제 편집 규칙으로 검증
        {
            var request = new CraftedSpellSelection // 제작 화면과 같은 선택 자료
            {
                element = element, shape = shape, formation = formation, trajectory = trajectory, // 발사체 구성
                fireMode = mode, origin = origin, aim = aim, trigger = trigger, // 생성·조준·발동
                firstEffect = first, secondEffect = second, // 별도 선택한 특수 효과
                initialShots = initial, splitChildren = children, splitDepth = depth // 생성량과 분열 한도
            };
            CraftedSpellIssue issue = CraftedSpellRules.Compose(request, out CraftedSpellRecipe recipe); // 문서 예시도 같은 조합 검증
            if (issue != CraftedSpellIssue.None) // 카탈로그와 충돌한 개발 예시
            {
                throw new InvalidOperationException($"제작 마법 예시 오류 {key} / {issue}"); // 틀어진 비용 표의 조용한 적용 방지
            }
            return new CraftedSpellPreset(key, name, recipe); // 검증을 마친 도감 항목
        }
    }
}
