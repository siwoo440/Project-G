namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    internal sealed class RapidAchievementRun // 한 출격의 아리아 과제 증거
    { // 클래스 시작
        private readonly string runId; // 실제 출격 식별자
        private int basicHits; // 유효한 기본 공격 적중 수
        internal RapidAchievementRun(string id) // 출격 증거 초기화
        { // 초기화 시작
            runId = id; // 현재 출격 식별자 보관
        } // 초기화 종료
        internal void RecordHit() // 유효한 적중 기록
        { // 기록 시작
            if (basicHits < 50) // 목표 횟수 확인
            { // 누적 시작
                basicHits++; // 최초 과제 적중 증가
            } // 누적 종료
        } // 기록 종료
        internal bool Qualifies(string id) // 출격과 과제 조건 확인
        { // 확인 시작
            return !string.IsNullOrEmpty(runId) && runId == id && basicHits >= 50; // 같은 출격의 목표 달성 확인
        } // 확인 종료
    } // 클래스 종료
} // 영역 종료
