using System.Collections.Generic; // 최근 발사 시각의 순서 관리

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightCraftedEmissionBudget // 한 플레이어의 제작 발사체 생성 한도
    {
        internal sealed class CastToken // 한 번의 시전에서 공유하는 누적 기록
        {
            internal FlightCraftedEmissionBudget owner; // 이 기록을 만든 발사 한도
            internal int generation; // 재출격 전후의 기록 구분
            internal int emitted; // 이번 시전에서 이미 생성한 총 수
            internal bool hit; // 시전당 최초 유효 명중 여부
            internal FlightSkillController effects; // 실전 빗자루의 명중 효과
        }

        private readonly Queue<float> recent = new(); // 최근 일 초 동안 생성한 발사체
        private int generation; // 새 출격마다 바뀌는 기록 번호

        internal bool CanBegin(int requested, int active, float now) // 실제 예약 없이 최초 생성 가능 여부 확인
        {
            return requested >= 1 && requested <= CraftedSpellRules.MaxInitial && CanReserve(requested, 0, active, now); // 최초와 플레이어 공통 상한 확인
        }

        internal bool TryBegin(int requested, int active, float now, out CastToken token) // 최초 생성의 원자적 예약
        {
            token = null; // 실패할 때 유효한 시전 기록 없음
            if (!CanBegin(requested, active, now)) // 최초 발사와 공통 한도 확인
            {
                return false; // 일부만 예약하지 않음
            }
            token = new CastToken { owner = this, generation = generation, emitted = requested }; // 성공한 최초 생성 기록
            Record(requested, now); // 플레이어의 최근 생성 수에 반영
            return true; // 전체 최초 생성 허용
        }

        internal bool TryContinue(CastToken token, int requested, int active, float now) // 같은 시전의 후속 발사와 자탄 예약
        {
            if (token == null || token.owner != this || token.generation != generation || !CanReserve(requested, token.emitted, active, now)) // 원래 시전과 모든 상한 확인
            {
                return false; // 누적 수와 초당 수를 바꾸지 않음
            }
            token.emitted += requested; // 이 시전의 누적 생성 수 증가
            Record(requested, now); // 같은 플레이어의 초당 생성에 반영
            return true; // 요청한 후속 발사 전체 허용
        }

        internal void Reset() // 새 출격의 생성 기록 초기화
        {
            recent.Clear(); // 이전 초당 생성 기록 제거
            generation++; // 이전 시전의 후속 발사 무효화
        }

        private bool CanReserve(int requested, int emitted, int active, float now) // 유효 시간과 공통 생성 한도
        {
            if (float.IsNaN(now) || float.IsInfinity(now)) // 계산할 수 없는 발사 시각
            {
                return false; // 기록과 발사체를 변경하지 않음
            }
            while (recent.Count > 0 && recent.Peek() <= now - 1f) // 일 초가 지난 생성 기록
            {
                recent.Dequeue(); // 현재 초당 한도에서 제외
            }
            return CraftedSpellRules.CanEmit(requested, emitted, recent.Count, active); // 시전·초당·활성 상한 동시 확인
        }

        private void Record(int count, float now) // 성공한 생성만 시간 창에 기록
        {
            for (int i = 0; i < count; i++) // 예약한 개별 발사체 수
            {
                recent.Enqueue(now); // 같은 발사의 실제 시작 시각
            }
        }
    }
}
