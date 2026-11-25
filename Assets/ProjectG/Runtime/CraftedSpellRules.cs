using System; // 직렬화된 조합과 ID
using System.Globalization; // 수치의 고정 저장 형식

namespace ProjectG // 프로젝트 게임 영역
{
    public enum CraftedSpellIssue // 편집기와 시험 발사가 공유하는 거절 사유
    {
        None, // 유효한 조합
        InvalidChoice, // 존재하지 않는 구성 요소
        DuplicateEffect, // 동일 특수 효과 선택
        Incompatible, // 구조적으로 충돌하는 구성
        InitialLimit, // 최초 생성 수 초과
        SplitLimit // 분열 자탄 또는 깊이 초과
    }

    [Serializable] // 마법 편집기가 선택하는 조합
    public sealed class CraftedSpellSelection
    {
        public string element = "fire"; // 제작 마법의 필수 속성
        public string shape = "orb"; // 반드시 선택하는 탄환 형태
        public string formation = "single"; // 발사 배열
        public string trajectory = "straight"; // 이동 궤적
        public string fireMode = "single"; // 발사 방식
        public string origin = "front"; // 생성 위치
        public string aim = "manual"; // 조준 방식
        public string trigger = "immediate"; // 발동 조건
        public string firstEffect = "none"; // 첫 번째 특수 효과
        public string secondEffect = "none"; // 두 번째 특수 효과
        public int initialShots = 1; // 시전의 최초 생성 수
        public int splitChildren; // 모탄당 자탄 생성 수
        public int splitDepth; // 자탄 생성의 최대 깊이
    }

    public sealed class CraftedSpellRecipe // 검증에 성공한 변경 불가 제작 조합
    {
        public string Id { get; } // 구성 요소와 생성 수로 재현하는 ID
        public int Cost { get; } // 마법 공격력 한도에 쓰는 구성 비용
        public int InitialShots { get; } // 최초 발사체 수
        public int SplitChildren { get; } // 모탄당 자탄 수
        public int MaximumShots { get; } // 최초 발사와 한 단계 분열의 최대 합
        public string Element { get; } // 피해 상성의 공격 속성
        public string Shape { get; } // 발사체 또는 효과의 형태
        public string Formation { get; } // 생성할 위치와 방향의 배열
        public string Trajectory { get; } // 발사체의 이동 경로
        public string FireMode { get; } // 시전과 반복 발사 방식
        public string Origin { get; } // 생성 위치
        public string Aim { get; } // 목표 선택 방식
        public string Trigger { get; } // 특수 효과 발동 조건
        public string FirstEffect { get; } // 정렬된 첫 번째 효과
        public string SecondEffect { get; } // 정렬된 두 번째 효과

        internal CraftedSpellRecipe(string id, int cost, CraftedSpellSelection selection, string first, string second) // 검증을 끝낸 공통 발사 자료
        {
            Id = id; // 조합 식별자 기록
            Cost = cost; // 고정 비용 합계
            InitialShots = selection.initialShots; // 최초 발사 수
            SplitChildren = selection.splitChildren; // 모탄 하나의 자탄 수
            MaximumShots = InitialShots * (1 + SplitChildren); // 재귀 없는 시전 누적 한도
            Element = selection.element; // 속성 저장
            Shape = selection.shape; // 형태 저장
            Formation = selection.formation; // 배열 저장
            Trajectory = selection.trajectory; // 궤적 저장
            FireMode = selection.fireMode; // 발사 방식 저장
            Origin = selection.origin; // 생성 기준 저장
            Aim = selection.aim; // 조준 모드 저장
            Trigger = selection.trigger; // 효과 발동 시점 저장
            FirstEffect = first; // 정렬한 첫 효과
            SecondEffect = second; // 정렬한 두 번째 효과
        }
    }

    public static class CraftedSpellRules // 편집과 전투에서 공용하는 제작 마법 규칙
    {
        public const int MaxInitial = 24; // 시전당 최초 생성 상한
        public const int MaxPerCast = 96; // 시전당 누적 생성 상한
        public const int MaxPerSecond = 40; // 플레이어가 초당 생성할 수 있는 상한
        public const int MaxActive = 128; // 동시에 유지 가능한 발사체 상한
        public const int MaxSplitChildren = 3; // 모탄당 분열 자탄 상한
        public const int MaxPierceTargets = 5; // 발사체당 서로 다른 관통 대상 상한
        private const string Version = "spell-v1"; // 저장 시 사용하는 고정 식별자 형식
        private const string None = "none"; // 추가 효과 미선택
        private static readonly CraftedSpellGroup[] KeyGroups = // 구성 ID 안의 선택지 순서
        {
            CraftedSpellGroup.Element, CraftedSpellGroup.Shape, CraftedSpellGroup.Formation, CraftedSpellGroup.Trajectory, // 속성과 발사체 기본 조합
            CraftedSpellGroup.FireMode, CraftedSpellGroup.Origin, CraftedSpellGroup.Aim, CraftedSpellGroup.Trigger, // 생성과 발동 방법
            CraftedSpellGroup.Effect, CraftedSpellGroup.Effect // 순서 없는 최대 두 가지 효과
        };
        private static readonly float[,] Damage = // 화염·물·풀·빛·어둠 순서의 피해 배율
        {
            { 1f, 0.75f, 1.5f, 1f, 1f }, // 화염 공격
            { 1.5f, 1f, 0.75f, 1f, 1f }, // 물 공격
            { 0.75f, 1.5f, 1f, 1f, 1f }, // 풀 공격
            { 1f, 1f, 1f, 1f, 1.5f }, // 빛 공격
            { 1f, 1f, 1f, 1.5f, 1f } // 어둠 공격
        };

        public static CraftedSpellIssue Compose(CraftedSpellSelection selection, out CraftedSpellRecipe recipe) // 조합 ID와 고정 비용 계산
        {
            recipe = null; // 실패 시 이전 결과 사용 차단
            if (selection == null) // 선택 정보가 비어 있음
            {
                return CraftedSpellIssue.InvalidChoice; // 초기화되지 않은 마법 선택
            }

            string first = string.IsNullOrEmpty(selection.firstEffect) ? None : selection.firstEffect; // 효과 하나의 고정 ID
            string second = string.IsNullOrEmpty(selection.secondEffect) ? None : selection.secondEffect; // 효과 둘의 고정 ID
            if (first == second && first != None) // 효과 없음 이외의 동일 선택
            {
                return CraftedSpellIssue.DuplicateEffect; // 같은 특수 효과 중복 차단
            }
            if (string.CompareOrdinal(first, second) > 0) // 순서가 다른 동일 기능 통일
            {
                (first, second) = (second, first); // 효과를 ID 오름차순으로 저장
            }

            string[] choices = // 문서에서 정의한 선택지 9분류
            {
                selection.element, selection.shape, selection.formation, selection.trajectory, // 속성 및 발사체
                selection.fireMode, selection.origin, selection.aim, selection.trigger, // 환경 및 발동
                first, second // 최대 두 가지 특수 효과
            };
            int cost = 0; // 개별 비용의 고정 합
            for (int i = 0; i < choices.Length; i++) // 등재된 분류와 ID 검증
            {
                int partCost = CraftedSpellCatalog.FindCost(KeyGroups[i], choices[i]); // 해당 선택의 문서 비용
                if (partCost < 0) // 다른 분류의 ID 또는 미등재 요소
                {
                    return CraftedSpellIssue.InvalidChoice; // 구성에 해당하지 않는 선택지
                }
                cost += partCost; // 정상 선택의 비용 합산
            }

            bool splits = first == "split" || second == "split"; // 분열 효과가 있는 조합
            if (!Compatible(selection, first, second, splits)) // 동작 유형 간 충돌 확인
            {
                return CraftedSpellIssue.Incompatible; // 저장과 발사를 막을 조합
            }
            if (selection.initialShots < 1 || selection.initialShots > MaxInitial) // 최소 1개와 최초 생성 상한
            {
                return CraftedSpellIssue.InitialLimit; // 생성 수 범위 위반
            }
            if (splits ? (selection.splitChildren < 1 || selection.splitChildren > MaxSplitChildren || selection.splitDepth != 1) : (selection.splitChildren != 0 || selection.splitDepth != 0)) // 모탄 자탄 수와 재귀 차단
            {
                return CraftedSpellIssue.SplitLimit; // 분열 효과 없는 생성과 두 단계 분열 차단
            }

            if (splits && selection.origin == "projectile") // 분열에 포함된 기존 탄환 위치
            {
                cost -= CraftedSpellCatalog.FindCost(CraftedSpellGroup.Origin, "projectile"); // 이용한 동일 생성 기능 비용 제거
            }
            if (selection.formation == "spiral" && selection.fireMode == "burst") // 배열이 이미 순차 발사 담당
            {
                cost -= CraftedSpellCatalog.FindCost(CraftedSpellGroup.FireMode, "burst"); // 같은 순차 발사 비용 중복 방지
            }

            string id = Version + "/" + string.Join("/", choices) + "/" + EncodeNumber(selection.initialShots) + "/" + EncodeNumber(selection.splitChildren) + "/" + EncodeNumber(selection.splitDepth); // 문화권과 선택 순서에 독립적인 조합 식별자
            recipe = new CraftedSpellRecipe(id, cost, selection, first, second); // 다음 단계가 공유할 검증된 조합
            return CraftedSpellIssue.None; // 저장 및 편성에 사용할 수 있음
        }

        public static bool TryRead(string id, out CraftedSpellRecipe recipe) // 저장된 ID를 같은 조합으로 복원
        {
            recipe = null; // 잘못된 ID에서 기존 조합과 혼용 방지
            string[] parts = id?.Split('/'); // 저장 버전과 정해진 선택 순서
            if (parts == null || parts.Length != 14 || parts[0] != Version) // 형식과 버전 점검
            {
                return false; // 복원할 수 없는 저장 ID
            }
            if (!TryNumber(parts[11], out int shots) || !TryNumber(parts[12], out int children) || !TryNumber(parts[13], out int depth)) // 생성 정보의 숫자 형식 점검
            {
                return false; // 문자열과 범위를 검증한 뒤의 수치만 사용
            }

            var selection = new CraftedSpellSelection // 저장 ID에서 편집 화면의 선택 정보 복원
            {
                element = parts[1], shape = parts[2], formation = parts[3], trajectory = parts[4], // 발사체 구성
                fireMode = parts[5], origin = parts[6], aim = parts[7], trigger = parts[8], // 생성과 발동 방식
                firstEffect = parts[9], secondEffect = parts[10], // 고정 정렬된 특수 효과
                initialShots = shots, splitChildren = children, splitDepth = depth // 양과 분열
            };
            if (Compose(selection, out CraftedSpellRecipe candidate) != CraftedSpellIssue.None || candidate.Id != id) // 복원 시에도 본래 구성 규칙과 정규 형식 검사
            {
                return false; // 변조된 선택지와 비호환 조합 차단
            }
            recipe = candidate; // 모든 검증을 통과한 조합
            return true; // 저장 ID 그대로 복원
        }

        public static bool CanEquip(CraftedSpellRecipe left, CraftedSpellRecipe right, int magicAttack) // 최대 두 슬롯의 구성 비용 검사
        {
            return (left?.Cost ?? 0) + (right?.Cost ?? 0) <= magicAttack; // 같은 마법도 슬롯별 비용 합산
        }

        public static bool CanEmit(int requested, int castSoFar, int emittedInLastSecond, int active) // 실제 발사기가 사용할 추가 생성 가능 여부
        {
            return requested > 0 && castSoFar >= 0 && emittedInLastSecond >= 0 && active >= 0 // 모든 생성 계수의 유효성
                && requested <= MaxPerCast - castSoFar // 이번 시전의 누적 상한
                && requested <= MaxPerSecond - emittedInLastSecond // 최근 1초의 합산 상한
                && requested <= MaxActive - active; // 현재 활성 발사체 수 상한
        }

        public static float DamageMultiplier(string attack, string defense) // 기획서의 속성 상성 적용
        {
            int row = ElementIndex(attack); // 공격 속성 행
            int column = ElementIndex(defense); // 피격 속성 열
            return row >= 0 && column >= 0 ? Damage[row, column] : 1f; // 미지정 방어 속성의 중립 배율
        }

        private static bool Compatible(CraftedSpellSelection s, string first, string second, bool splits) // 문서에서 결정한 구조적 조합 제한
        {
            bool ray = s.shape == "laser" || s.shape == "beam"; // 순간·지속 직선 광선
            bool absorbConflict = (first == "absorb" && (second == "reflect" || second == "cancel")) || (second == "absorb" && (first == "reflect" || first == "cancel")); // 동일 적탄의 흡수와 반사·상쇄 중복
            return !(ray && (s.trajectory == "orbit" || splits)) // 광선의 공전과 분열 제외
                && !(s.shape == "barrier" && s.trajectory == "homing") // 고정 결계의 유도 제외
                && !(s.shape == "placed" && s.trajectory == "return") // 설치탄의 회귀 제외
                && !(s.aim == "homing" && s.trajectory != "homing") // 유도 조준은 유도 궤적 연동
                && !absorbConflict; // 적탄 한 개의 상호작용 중복 방지
        }

        private static int ElementIndex(string element) // 상성 표의 속성 순번 조회
        {
            var choices = CraftedSpellCatalog.Choices(CraftedSpellGroup.Element); // 기획서의 속성 차례
            for (int i = 0; i < choices.Count; i++) // 지정 ID의 속성 조회
            {
                if (choices[i].Id == element) // 해당 속성 확인
                {
                    return i; // 올바른 피해 배율 위치
                }
            }
            return -1; // 미지정 또는 미등재 속성
        }

        private static string EncodeNumber(int value) // 저장 지역과 별개인 숫자 문자
        {
            return value.ToString(CultureInfo.InvariantCulture); // 고정 형식의 숫자
        }

        private static bool TryNumber(string value, out int output) // 저장 ID의 부호 없는 숫자
        {
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out output); // 범위를 벗어난 숫자 입력 거절
        }
    }
}
