using System; // 난수와 조건 기능
using System.Collections.Generic; // 등급별 후보 목록
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public readonly struct UpgradeDrop // 필드 드롭 정보
    { // 값 시작
        public readonly UpgradeItemId Id; // 아이템 종류
        public readonly UpgradeItemGrade Grade; // 아이템 등급
        public UpgradeDrop(UpgradeItemId id, UpgradeItemGrade grade) // 드롭 정보 생성
        { // 생성 시작
            Id = id; // 종류 보관
            Grade = grade; // 등급 보관
        } // 생성 종료
    } // 값 종료
    public sealed class UpgradeDropRoller // 일반 적 처치 드롭 추첨
    { // 클래스 시작
        private readonly System.Random random; // 격리 가능한 난수
        private static readonly int[] weights = new int[] // 초기 시험 등급 가중치
        { // 가중치 시작
            50, 25, 15, 7, 3 // I부터 V까지 비율
        }; // 가중치 종료
        public UpgradeDropRoller(System.Random source) // 난수 연결
        { // 연결 시작
            random = source ?? new System.Random(); // 지정 난수 또는 기본 난수
        } // 연결 종료
        public bool TryRoll(EnemyRank rank, Func<UpgradeItemId, UpgradeItemGrade, bool> eligible, out UpgradeDrop drop) // 유효 드롭 추첨
        { // 추첨 시작
            drop = default; // 미드롭 기본값
            if (rank != EnemyRank.Normal || eligible == null) // 보스와 무효 조건 확인
            { // 차단 시작
                return false; // 미정인 보스 드롭 제외
            } // 차단 종료
            if (random.Next(100) >= 10) // 초기 시험 드롭 확률
            { // 실패 시작
                return false; // 90퍼센트 미드롭
            } // 실패 종료
            List<UpgradeItemId>[] pools = new List<UpgradeItemId>[weights.Length]; // 등급별 유효 후보
            int total = 0; // 유효 등급 가중치 합
            for (int index = 0; index < weights.Length; index++) // I부터 V까지 확인
            { // 등급 순회 시작
                UpgradeItemGrade grade = (UpgradeItemGrade)(index + 1); // 현재 등급
                List<UpgradeItemId> candidates = new List<UpgradeItemId>(); // 현재 등급 후보
                foreach (UpgradeItemId id in Enum.GetValues(typeof(UpgradeItemId))) // 아이템 종류 순회
                { // 후보 확인 시작
                    if (eligible(id, grade)) // 현재 효과 유효 확인
                    { // 후보 추가 시작
                        candidates.Add(id); // 유효 종류 보관
                    } // 후보 추가 종료
                } // 후보 확인 종료
                pools[index] = candidates; // 해당 등급 후보 보관
                if (candidates.Count > 0) // 유효한 등급 확인
                { // 가중치 반영 시작
                    total += weights[index]; // 기존 비율대로 재분배
                } // 가중치 반영 종료
            } // 등급 순회 종료
            if (total == 0) // 모든 등급 무효 확인
            { // 미드롭 시작
                return false; // 유효한 효과 없음
            } // 미드롭 종료
            int roll = random.Next(total); // 유효 등급 비율 추첨
            for (int index = 0; index < pools.Length; index++) // 등급 추첨 순회
            { // 선택 시작
                if (pools[index].Count == 0) // 비어 있는 등급 확인
                { // 제외 시작
                    continue; // 빈 등급 생략
                } // 제외 종료
                if (roll < weights[index]) // 선택된 구간 확인
                { // 드롭 생성 시작
                    List<UpgradeItemId> candidates = pools[index]; // 선택된 등급 후보
                    drop = new UpgradeDrop(candidates[random.Next(candidates.Count)], (UpgradeItemGrade)(index + 1)); // 유효 종류 균등 선택
                    return true; // 드롭 성공
                } // 드롭 생성 종료
                roll -= weights[index]; // 다음 등급 구간
            } // 선택 종료
            return false; // 잘못된 추첨 결과 차단
        } // 추첨 종료
    } // 클래스 종료
} // 영역 종료
