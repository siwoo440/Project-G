namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum TutorialSignal // 실제 행동 신호
    { // 목록 시작
        Move, FocusShot, Skill, Graze, Ultimate // 순서별 실제 행동
    } // 목록 종료
    public enum TutorialStep // 출격 안내 단계
    { // 목록 시작
        Move, Focus, Skill, Graze, Ultimate, Finished // 안내 진행 순서
    } // 목록 종료
    public sealed class TutorialSequence // 첫 출격 안내 상태
    { // 상태 시작
        public TutorialStep Current // 현재 안내 단계
        { // 속성 시작
            get; // 단계 조회
            private set; // 단계 변경
        } = TutorialStep.Move; // 첫 이동 단계
        public bool Observe(TutorialSignal signal) // 실제 행동 반영
        { // 반영 시작
            if (Current == TutorialStep.Finished || (int)Current != (int)signal) // 순서와 완료 상태 확인
            { // 무시 시작
                return false; // 다른 단계 행동 무시
            } // 무시 종료
            Current = (TutorialStep)((int)Current + 1); // 다음 단계 이동
            return true; // 단계 완료 반환
        } // 반영 종료
        public void Skip() // 전체 안내 건너뛰기
        { // 건너뛰기 시작
            Current = TutorialStep.Finished; // 안내 종료 상태
        } // 건너뛰기 종료
    } // 상태 종료
} // 영역 종료
