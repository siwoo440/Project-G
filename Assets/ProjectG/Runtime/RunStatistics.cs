using System.Collections.Generic; // 카드 선택 이력
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum DropOutcome // 필드 아이템의 최종 상태
    { // 목록 시작
        Pending, Acquired, Expired, Discarded // 대기와 획득 만료 폐기 구분
    } // 목록 종료
    public sealed class DropRecord // 한 아이템의 종류와 획득 결과
    { // 기록 시작
        public UpgradeItemId Id // 아이템 종류
        { // 속성 시작
            get; // 종류 조회
        } // 속성 종료
        public UpgradeItemGrade Grade // 아이템 등급
        { // 속성 시작
            get; // 등급 조회
        } // 속성 종료
        public DropOutcome Outcome // 아이템 최종 상태
        { // 속성 시작
            get; // 상태 조회
            internal set; // 내부 상태 변경
        } // 속성 종료
        internal DropRecord(UpgradeDrop drop) // 새 필드 아이템 기록
        { // 생성 시작
            Id = drop.Id; // 종류 보관
            Grade = drop.Grade; // 등급 보관
            Outcome = DropOutcome.Pending; // 필드 대기 상태
        } // 생성 종료
    } // 기록 종료
    public sealed class RunStatistics // 한 출격의 전투 기록
    { // 클래스 시작
        private readonly List<CardId> cards = new List<CardId>(); // 순서대로 기록한 카드
        private readonly IReadOnlyList<CardId> cardView; // 외부 수정 방지 목록
        private readonly List<DropRecord> drops = new List<DropRecord>(); // 순서대로 기록한 드롭
        private readonly IReadOnlyList<DropRecord> dropView; // 외부 변경 방지 목록
        public int Ultimates // 실제 필살기 발동 수
        { // 속성 시작
            get; // 횟수 조회
            private set; // 횟수 보관
        } // 속성 종료
        public int DropAttempts // 일반 적 드롭 추첨 수
        { // 속성 시작
            get; // 횟수 조회
            private set; // 횟수 보관
        } // 속성 종료
        public int DropsSpawned => drops.Count; // 실제 생성한 드롭 수
        public int DropsAcquired // 실제 획득한 드롭 수
        { // 속성 시작
            get; // 횟수 조회
            private set; // 횟수 보관
        } // 속성 종료
        public int DropsExpired // 필드에서 자연 만료된 수
        { // 속성 시작
            get; // 횟수 조회
            private set; // 횟수 보관
        } // 속성 종료
        public int DropsDiscarded // 출격 종료 시 폐기된 수
        { // 속성 시작
            get; // 횟수 조회
            private set; // 횟수 보관
        } // 속성 종료
        public bool MidBossEncountered // 중간 보스 실제 등장 여부
        { // 속성 시작
            get; // 등장 상태 조회
            private set; // 등장 상태 보관
        } // 속성 종료
        public bool FinalBossEncountered // 최종 보스 실제 등장 여부
        { // 속성 시작
            get; // 등장 상태 조회
            private set; // 등장 상태 보관
        } // 속성 종료
        public bool MidBossCleared // 중간 보스 돌파 여부
        { // 속성 시작
            get; // 돌파 상태 조회
            private set; // 돌파 상태 보관
        } // 속성 종료
        public bool FinalBossCleared // 최종 보스 돌파 여부
        { // 속성 시작
            get; // 돌파 상태 조회
            private set; // 돌파 상태 보관
        } // 속성 종료
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
        public IReadOnlyList<DropRecord> Drops => dropView; // 읽기 전용 드롭 결과
        public RunStatistics() // 출격 기록 생성
        { // 생성 시작
            cardView = cards.AsReadOnly(); // 외부 수정 방지 목록 준비
            dropView = drops.AsReadOnly(); // 드롭 수정 방지 목록 준비
        } // 생성 종료
        public void RecordUltimate() // 실제 필살기 발동 기록
        { // 기록 시작
            if (!Finished) // 출격 진행 확인
            { // 증가 시작
                Ultimates++; // 성공 횟수 증가
            } // 증가 종료
        } // 기록 종료
        public void RecordDropAttempt() // 일반 적 드롭 추첨 기록
        { // 기록 시작
            if (!Finished) // 출격 진행 확인
            { // 증가 시작
                DropAttempts++; // 추첨 횟수 증가
            } // 증가 종료
        } // 기록 종료
        public DropRecord RecordDropSpawned(UpgradeDrop drop) // 실제 생성한 드롭 기록
        { // 기록 시작
            if (Finished) // 출격 종료 확인
            { // 차단 시작
                return null; // 종료 후 드롭 기록 차단
            } // 차단 종료
            DropRecord record = new DropRecord(drop); // 종류와 등급 기록
            drops.Add(record); // 출격 드롭 목록 추가
            return record; // 해당 드롭 기록 반환
        } // 기록 종료
        public bool RecordDropOutcome(DropRecord record, DropOutcome outcome) // 한 드롭의 최종 상태 확정
        { // 확정 시작
            if (Finished || record == null || record.Outcome != DropOutcome.Pending || !drops.Contains(record) || outcome == DropOutcome.Pending) // 기록 소속과 상태 확인
            { // 차단 시작
                return false; // 중복 또는 무효 상태 차단
            } // 차단 종료
            if (outcome != DropOutcome.Acquired && outcome != DropOutcome.Expired && outcome != DropOutcome.Discarded) // 최종 상태 범위 확인
            { // 차단 시작
                return false; // 알 수 없는 상태 차단
            } // 차단 종료
            record.Outcome = outcome; // 드롭 결과 확정
            if (outcome == DropOutcome.Acquired) // 실제 획득 확인
            { // 획득 시작
                DropsAcquired++; // 획득 수 증가
            } // 획득 종료
            else if (outcome == DropOutcome.Expired) // 시간 만료 확인
            { // 만료 시작
                DropsExpired++; // 자연 만료 수 증가
            } // 만료 종료
            else // 출격 정리 확인
            { // 폐기 시작
                DropsDiscarded++; // 미획득 폐기 수 증가
            } // 폐기 종료
            return true; // 최초 상태 확정 완료
        } // 확정 종료
        public void RecordBossEncounter(EnemyRank rank) // 실제 보스 등장 기록
        { // 기록 시작
            if (Finished) // 출격 종료 확인
            { // 차단 시작
                return; // 종료 후 기록 차단
            } // 차단 종료
            if (rank == EnemyRank.MidBoss) // 중간 보스 확인
            { // 중간 기록 시작
                MidBossEncountered = true; // 중간 보스 등장
            } // 중간 기록 종료
            else if (rank == EnemyRank.FinalBoss) // 최종 보스 확인
            { // 최종 기록 시작
                FinalBossEncountered = true; // 최종 보스 등장
            } // 최종 기록 종료
        } // 기록 종료
        public void RecordBossClear(EnemyRank rank) // 실제 보스 돌파 기록
        { // 기록 시작
            if (Finished) // 출격 종료 확인
            { // 차단 시작
                return; // 종료 후 기록 차단
            } // 차단 종료
            if (rank == EnemyRank.MidBoss) // 중간 보스 확인
            { // 중간 기록 시작
                MidBossCleared = true; // 중간 보스 돌파
            } // 중간 기록 종료
            else if (rank == EnemyRank.FinalBoss) // 최종 보스 확인
            { // 최종 기록 시작
                FinalBossCleared = true; // 최종 보스 돌파
            } // 최종 기록 종료
        } // 기록 종료
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
            foreach (DropRecord drop in drops) // 남은 필드 드롭 순회
            { // 폐기 처리 시작
                if (drop.Outcome == DropOutcome.Pending) // 미획득 드롭 확인
                { // 폐기 시작
                    RecordDropOutcome(drop, DropOutcome.Discarded); // 출격 종료 폐기 구분
                } // 폐기 종료
            } // 폐기 처리 종료
            Finished = true; // 출격 종료 확정
            return true; // 최초 종료 성공
        } // 확정 종료
        public void Reset() // 새 출격 초기화
        { // 초기화 시작
            Defeats = 0; // 처치 기록 초기화
            Grazes = 0; // 그레이즈 기록 초기화
            Hits = 0; // 피격 기록 초기화
            Ultimates = 0; // 필살기 기록 초기화
            DropAttempts = 0; // 드롭 시도 초기화
            DropsAcquired = 0; // 획득 기록 초기화
            DropsExpired = 0; // 만료 기록 초기화
            DropsDiscarded = 0; // 폐기 기록 초기화
            MidBossEncountered = false; // 중간 보스 등장 초기화
            FinalBossEncountered = false; // 최종 보스 등장 초기화
            MidBossCleared = false; // 중간 보스 기록 초기화
            FinalBossCleared = false; // 최종 보스 기록 초기화
            cards.Clear(); // 카드 이력 초기화
            drops.Clear(); // 드롭 이력 초기화
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
