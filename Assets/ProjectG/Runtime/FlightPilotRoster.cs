namespace ProjectG // 프로젝트 게임 영역
{
    internal static class FlightPilotRoster // 실제 출격에 사용할 조종사와 메뉴 이름
    {
        internal const int Count = 3; // 현재 선택할 수 있는 조종사의 순서 범위

        internal static bool CanSelect(FlightBroomPilot pilot) // 기존 저장과 새 시험 조종사의 선택 가능 여부
        {
            return pilot switch // 등록된 실제 전투 프로필만 허용
            {
                FlightBroomPilot.Aria => FlightProgress.Current.HasUnlock("pilot.aria"), // 기존 아리아 저장 권한
                FlightBroomPilot.Rian => FlightProgress.Current.HasUnlock("pilot.rian"), // 기존 리안 저장 권한
                FlightBroomPilot.Lumi => true, // 32일차 시험 조종사
                _ => false // 준비 중인 캐릭터의 출격 차단
            };
        }

        internal static string Name(FlightBroomPilot pilot) // 준비부터 정산까지 표시할 이름
        {
            return pilot switch // 실제 출격을 마친 캐릭터의 이름
            {
                FlightBroomPilot.Rian => "리안", // 저격형 수호자
                FlightBroomPilot.Lumi => "루미", // 유도형 탐색자
                _ => "아리아" // 기본 출격의 속사형 조사원
            };
        }
    }
}
