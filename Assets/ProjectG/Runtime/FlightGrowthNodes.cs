namespace ProjectG // 조종사별 후속 성장
{
    internal static class FlightGrowthNodes // 상점과 전투 카드가 공유하는 확정 노드
    {
        internal const string Chase = "growth.aria.chase-2"; // 아리아 추격 사격 2
        internal const string Spread = "growth.aria.spread-2"; // 아리아 확산 포화 2
        internal const string Weakness = "growth.rian.weakness-2"; // 리안 약점 사냥 2
        internal const int CrystalCost = 18; // 조종사별 후속 노드의 공통 재료
        internal const int MarkCost = 1; // 해당 조종사의 조율의 증표

        internal readonly struct Node // 공방이 표시하고 영구 저장이 검증할 실제 노드
        {
            internal readonly string Id; // 저장과 전투 후보의 같은 키
            internal readonly FlightBroomPilot Pilot; // 구매 가능한 한 조종사
            internal readonly string Name; // 기획서에 있는 현재 노드 이름
            internal readonly string Requirement; // 실제 출격에서 필요한 1단계 카드

            internal Node(string id, FlightBroomPilot pilot, string name, string requirement) // 공방의 확정 노드 초기값
            {
                Id = id; // 적용할 후보 ID
                Pilot = pilot; // 증표를 소모할 캐릭터
                Name = name; // 노드의 한글 이름
                Requirement = requirement; // 같은 가지의 전투 선행 카드
            }
        }

        internal static readonly Node[] All = // 3D 첫 후속 성장의 검증된 범위
        {
            new Node(Chase, FlightBroomPilot.Aria, "추격 사격 2", "추격 사격 1 선행"), // 누적과 집중 공격의 첫 성장
            new Node(Spread, FlightBroomPilot.Aria, "확산 포화 2", "확산 포화 1 선행"), // 고유 스킬 시간의 자동 추가탄
            new Node(Weakness, FlightBroomPilot.Rian, "약점 사냥 2", "약점 사냥 1 선행") // 약점 적중의 고유 스킬 재사용
        };

        internal static bool BelongsTo(string id, FlightBroomPilot pilot) // 다른 캐릭터의 노드 구매 방지
        {
            foreach (Node node in All) // 기획서에서 실제 연결하기로 정한 노드
            {
                if (node.Id == id && node.Pilot == pilot) // 현재 조종사와 실제 항목이 같음
                {
                    return true; // 조종사별 증표를 사용할 수 있음
                }
            }
            return false; // 설정 미정이거나 해당하지 않는 구매 요청
        }
    }
}
