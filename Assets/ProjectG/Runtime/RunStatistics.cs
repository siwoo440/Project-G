using System.Collections.Generic; // 카드 선택 이력
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class RunStatistics // 한 출격의 전투 기록
    { // 클래스 시작
        private readonly List<CardId> cards = new List<CardId>(); // 순서대로 기록한 카드
        private readonly IReadOnlyList<CardId> cardView; // 외부 수정 방지 목록
        public int Defeats // 처치 횟수
        { // 속성 시작
            get; // 횟수 조회
            private set; // 횟수 보관
        } // 속성 종료
        public int Grazes // 그레이즈 횟수
        { // 속성 시작
            get; // 횟수 조회
            private set; // 횟수 보관
        } // 속성 종료
        public int Hits // 실제 체력 피해 횟수
        { // 속성 시작
            get; // 횟수 조회
            private set; // 횟수 보관
        } // 속성 종료
        public bool Finished // 출격 종료 여부
        { // 속성 시작
            get; // 종료 여부 조회
            private set; // 종료 여부 보관
        } // 속성 종료
        public bool Cleared // 클리어 여부
        { // 속성 시작
            get; // 클리어 여부 조회
            private set; // 클리어 여부 보관
        } // 속성 종료
        public float CombatSeconds // 전투 진행 시간
        { // 속성 시작
            get; // 시간 조회
            private set; // 시간 보관
        } // 속성 종료
        public float TotalSeconds // 전체 소요 시간
        { // 속성 시작
            get; // 시간 조회
            private set; // 시간 보관
        } // 속성 종료
        public IReadOnlyList<CardId> Cards => cardView; // 읽기 전용 카드 선택 이력
        public RunStatistics() // 출격 기록 생성
        { // 생성 시작
            cardView = cards.AsReadOnly(); // 외부 수정 방지 목록 준비
        } // 생성 종료
        public void RecordDefeat() // 적 처치 기록
        { // 기록 시작
            if (!Finished) // 진행 중 확인
            { // 증가 시작
                Defeats++; // 처치 횟수 증가
            } // 증가 종료
        } // 기록 종료
        public void RecordGraze() // 그레이즈 기록
        { // 기록 시작
            if (!Finished) // 진행 중 확인
            { // 증가 시작
                Grazes++; // 그레이즈 횟수 증가
            } // 증가 종료
        } // 기록 종료
        public void RecordHit() // 실제 체력 피해 기록
        { // 기록 시작
            if (!Finished) // 진행 중 확인
            { // 증가 시작
                Hits++; // 피격 횟수 증가
            } // 증가 종료
        } // 기록 종료
        public void RecordCard(CardId id) // 선택 카드 기록
        { // 기록 시작
            if (!Finished) // 진행 중 확인
            { // 추가 시작
                cards.Add(id); // 선택 순서 보관
            } // 추가 종료
        } // 기록 종료
        public bool Complete(bool cleared, float combatSeconds, float totalSeconds) // 출격 결과 확정
        { // 확정 시작
            if (Finished) // 중복 종료 확인
            { // 차단 시작
                return false; // 기존 결과 유지
            } // 차단 종료
            Cleared = cleared; // 클리어 또는 실패 보관
            CombatSeconds = ValidTime(combatSeconds); // 전투 시간 보관
            TotalSeconds = ValidTime(totalSeconds); // 전체 시간 보관
            Finished = true; // 출격 종료 확정
            return true; // 최초 종료 성공
        } // 확정 종료
        public void Reset() // 새 출격 초기화
        { // 초기화 시작
            Defeats = 0; // 처치 기록 초기화
            Grazes = 0; // 그레이즈 기록 초기화
            Hits = 0; // 피격 기록 초기화
            cards.Clear(); // 카드 이력 초기화
            Cleared = false; // 클리어 상태 초기화
            CombatSeconds = 0f; // 전투 시간 초기화
            TotalSeconds = 0f; // 전체 시간 초기화
            Finished = false; // 출격 진행 상태
        } // 초기화 종료
        private static float ValidTime(float value) // 시간 값 검사
        { // 검사 시작
            return float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 0f : value; // 잘못된 시간 제거
        } // 검사 종료
    } // 클래스 종료
} // 영역 종료
