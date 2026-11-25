using UnityEngine; // 출격 준비와 정산이 공유하는 작전 기록 원본

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightIncidentScript : ScriptableObject // 세계관 확정 사실과 시험용 작전 통신
    {
        [TextArea] public string Archive; // 균열과 첫 만남의 승인 사실
        [TextArea] public string Opening; // 아리아와 리안의 첫 전투 화면
        [TextArea] public string MidBoss; // 실제 중간 보스가 진입한 뒤의 통신
        [TextArea] public string FinalBoss; // 실제 최종 보스와 대면한 뒤의 통신
        public string ClearSummary; // 완료 표시에 연동할 임시 작전 기록
        public string FailureSummary; // 실패 표시에 연동할 임시 작전 기록
    }
}
