using System.Collections.Generic; // 개별 보호막 기여분 목록

namespace ProjectG // 프로젝트 빗자루 보호막 영역
{
    internal sealed class FlightBroomShield // 같은 계열 보호막의 개별 만료와 흡수
    {
        private sealed class Contribution // 만료 시각을 따로 보존하는 보호막
        {
            public int Remaining; // 아직 흡수할 수 있는 정수 피해량
            public float Expires; // 이 기여분만 사라지는 실제 시각
        }

        private readonly List<Contribution> active = new(); // 현재 남아 있는 개별 기여분

        internal int Total(float now) // 만료한 기여분을 제거한 실제 잔여량
        {
            for (int i = active.Count - 1; i >= 0; i--) // 만료한 기여분을 뒤에서부터 확인
            {
                if (active[i].Expires <= now || active[i].Remaining <= 0) // 끝나거나 모두 소모한 기여분
                {
                    active.RemoveAt(i); // 다른 기여분의 잔여량과 만료 시각 보존
                }
            }
            int total = 0; // 유효한 보호막의 정수 합계
            foreach (Contribution value in active) // 현재 유지 중인 모든 기여분
            {
                total += value.Remaining; // 기여분별 실제 잔여량
            }
            return total; // 현재 시각의 총 흡수량
        }

        internal int Add(int amount, float duration, int maxHealth, float now) // 최대 체력 30% 안의 실제 추가량
        {
            if (amount <= 0 || duration <= 0f || maxHealth <= 0) // 무효한 기여분 또는 상한
            {
                return 0; // 잘못된 보호막은 추가하지 않음
            }
            int cap = (int)((long)maxHealth * 3 / 10); // 확정된 30% 상한의 정수 내림
            int current = Total(now); // 만료를 먼저 처리한 기존 잔여량
            int granted = System.Math.Min(amount, System.Math.Max(0, cap - current)); // 남은 상한 안에서만 추가
            if (granted > 0) // 실제로 받을 수 있는 양이 있는 경우
            {
                active.Add(new Contribution { Remaining = granted, Expires = now + duration }); // 이 기여분만의 잔여량과 만료 시각
            }
            return granted; // 실제로 추가한 보호막
        }

        internal int Absorb(int damage, float now) // 만료가 빠른 기여분부터 실제 피해 흡수
        {
            Total(now); // 이미 만료한 기여분 제거
            int remaining = System.Math.Max(0, damage); // 흡수 전 남은 정수 피해
            while (remaining > 0 && active.Count > 0) // 피해와 보호막이 모두 남은 동안
            {
                int earliest = 0; // 가장 먼저 만료할 기여분 위치
                for (int i = 1; i < active.Count; i++) // 서로 다른 만료 시각 비교
                {
                    if (active[i].Expires < active[earliest].Expires) // 더 먼저 끝나는 기여분
                    {
                        earliest = i; // 실제 우선 소모 위치 갱신
                    }
                }
                Contribution piece = active[earliest]; // 먼저 만료할 유효 기여분
                int used = System.Math.Min(remaining, piece.Remaining); // 이 기여분이 실제로 흡수할 피해
                piece.Remaining -= used; // 해당 기여분에서만 소모
                remaining -= used; // 아직 체력에 닿을 수 있는 피해
                if (piece.Remaining == 0) // 완전히 소모된 기여분
                {
                    active.RemoveAt(earliest); // 다른 기여분의 시간과 잔여량은 유지
                }
            }
            return remaining; // 모든 장비 보호막을 통과한 실제 피해
        }

        internal void Clear() // 새 출격의 장비 보호막 초기화
        {
            active.Clear(); // 이전 출격의 모든 기여분 제거
        }
    }
}
