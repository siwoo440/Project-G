using System; // 정산 결과와 재현 가능한 추첨
using System.Collections.Generic; // 기획 세트 부품의 공용 후보
using System.Security.Cryptography; // 출격 ID 기반의 안정적인 추첨 시드
using System.Text; // 출격 ID의 바이트 변환

namespace ProjectG // 프로젝트 빗자루 보상 영역
{
    [Serializable] // 영수증과 공용 인벤토리에 함께 저장하는 부품
    public sealed class FlightBroomReward
    {
        public string Id; // 같은 종류라도 구분하는 개별 보유 ID
        public string DefinitionId; // 세트 부품 카탈로그의 고정 종류 ID
        public string Name; // 정산 당시의 실제 부품 이름
        public int Grade; // 일반부터 전설까지 획득한 희귀도
        public string Source; // 실제 보상을 발생시킨 적 유형
    }

    internal static class FlightBroomRewardTable // 기획서 14.6.1의 처치와 희귀도
    {
        internal enum EnemyKind // 보상 확률이 다른 지정 적
        {
            Normal, Elite, MidBoss, FinalBoss // 일반·정예·중간·최종 보스
        }

        private static readonly int[,] Weights = // 기본과 상위 난이도의 확정 백분율
        {
            { 80, 18, 2, 0, 0 }, // 기본 일반 적
            { 58, 32, 9, 1, 0 }, // 기본 정예 적
            { 35, 40, 22, 3, 0 }, // 기본 중간 보스
            { 10, 35, 40, 14, 1 }, // 기본 최종 보스
            { 70, 24, 6, 0, 0 }, // 상위 일반 적
            { 45, 36, 16, 3, 0 }, // 상위 정예 적
            { 24, 39, 29, 7, 1 }, // 상위 중간 보스
            { 5, 25, 43, 23, 4 } // 상위 최종 보스
        };

        internal static int GradeForRoll(EnemyKind enemy, bool high, int roll) // 정수 백분위의 실제 희귀도
        {
            if ((uint)enemy >= 4 || roll < 0 || roll >= 100) // 존재하지 않는 적 또는 백분위
            {
                return 0; // 유효한 등급을 추측하지 않음
            }
            int row = (high ? 4 : 0) + (int)enemy; // 난이도와 적 유형의 확정 행
            int boundary = 0; // 이전 희귀도까지의 누적 확률
            for (int grade = 0; grade < 5; grade++) // 일반부터 전설까지 차례로 확인
            {
                boundary += Weights[row, grade]; // 현재 등급까지의 구간 끝
                if (roll < boundary) // 실제 백분위가 속한 구간
                {
                    return grade + 1; // I부터 V까지의 유효 희귀도
                }
            }
            return 0; // 합계가 손상된 확률표는 지급하지 않음
        }

        internal static FlightBroomReward[] Draw(FlightBroomCatalog catalog, string runId, int normalKills, int eliteKills, bool midBoss, bool finalBoss, bool high) // 정상 출격의 실제 부품 추첨
        {
            if (catalog == null || string.IsNullOrEmpty(runId) || normalKills < 0 || eliteKills < 0) // 자료가 없거나 잘못된 처치 기록
            {
                return null; // 영구 보상과 출격을 확정하지 않음
            }
            var candidates = new List<FlightBroomPart>(); // 기본 부품을 제외한 공용 세트 후보
            var ids = new HashSet<string>(); // 종류 ID의 실제 중복 방지
            foreach (FlightBroomPart part in catalog.Parts) // 실제 Unity 카탈로그의 부품
            {
                if (part != null && !part.starter && !string.IsNullOrEmpty(part.setId)) // 기획된 세트 소속 부품
                {
                    if (string.IsNullOrEmpty(part.id) || !ids.Add(part.id)) // 유효하지 않거나 중복된 종류 ID
                    {
                        return null; // 후보가 손상된 상태로 정산하지 않음
                    }
                    candidates.Add(part); // 모든 적 유형이 공유하는 동일 후보
                }
            }
            if (candidates.Count != 80) // 확정된 열 세트의 여든 개가 아닌 상태
            {
                return null; // 누락된 후보를 임의로 대체하지 않음
            }
            var kinds = new List<EnemyKind>(); // 이 출격에서 실제로 달성한 지급 건
            for (int count = 0; count < Math.Min(2, normalKills / 20); count++) // 일반 적 스무 마리와 마흔 마리
            {
                kinds.Add(EnemyKind.Normal); // 경계마다 한 건
            }
            if (eliteKills > 0) // 지정 정예의 첫 처치
            {
                kinds.Add(EnemyKind.Elite); // 한 출격에 최대 한 건
            }
            if (midBoss) // 지정 중간 보스의 실제 첫 처치
            {
                kinds.Add(EnemyKind.MidBoss); // 한 출격에 최대 한 건
            }
            if (finalBoss) // 지정 최종 보스의 실제 첫 처치
            {
                kinds.Add(EnemyKind.FinalBoss); // 한 출격에 최대 한 건
            }
            byte[] hash; // 재시도와 재실행에서 같은 추첨을 위한 시드
            using (SHA256 algorithm = SHA256.Create()) // 운영체제의 실제 해시 구현
            {
                hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes("ProjectG.BroomReward.v1:" + runId)); // 출격 ID와 보상 규칙의 고정 시드
            }
            int seed = hash[0] | hash[1] << 8 | hash[2] << 16 | hash[3] << 24; // 플랫폼에 독립적인 네 바이트 정수
            var random = new Random(seed); // 동일 출격의 재시도에서도 같은 난수열
            var rewards = new FlightBroomReward[kinds.Count]; // 영수증과 인벤토리에 함께 저장할 지급 건
            for (int i = 0; i < kinds.Count; i++) // 실제로 달성한 각 지급 건
            {
                int grade = GradeForRoll(kinds[i], high, random.Next(100)); // 해당 적 유형의 기획 희귀도
                FlightBroomPart part = candidates[random.Next(candidates.Count)]; // 같은 확률의 80종 공용 후보
                rewards[i] = new FlightBroomReward // 종류가 같아도 독립된 실제 부품
                {
                    Id = "drop." + runId + "." + i, // 이 출격과 지급 순서의 고유 ID
                    DefinitionId = part.id, // 실제 카탈로그의 종류
                    Name = part.displayName, // 결과 화면에 보일 부품 이름
                    Grade = grade, // 이번 개별 부품의 희귀도
                    Source = kinds[i].ToString() // 정산을 발생시킨 지정 적 유형
                };
            }
            return rewards; // 재화와 같은 파일에 원자 저장할 실제 결과
        }
    }
}
