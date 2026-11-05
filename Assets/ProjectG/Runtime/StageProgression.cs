using System; // 단계 변경 알림
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum StagePhase // 시험 스테이지 구간
    { // 구간 목록 시작
        Preparation, // 출격 준비와 약한 적
        CombatOne, // 첫 일반 전투
        UpgradeOne, // 첫 강화 아이템 선택
        MidBoss, // 중간 보스
        CombatTwo, // 두 번째 일반 전투
        UpgradeTwo, // 두 번째 강화 아이템 선택
        FinalBoss, // 최종 보스
        Result, // 스테이지 완료
        Failed // 출격 실패
    } // 구간 목록 종료
    public sealed class StageProgression // 시험 스테이지 진행 상태
    { // 클래스 시작
        public event Action<StagePhase> PhaseChanged; // 구간 변경 알림
        public StagePhase Phase // 현재 구간
        { // 속성 시작
            get; // 구간 조회
            private set; // 구간 변경
        } // 속성 종료
        public float PhaseSeconds // 현재 구간 진행 시간
        { // 속성 시작
            get; // 시간 조회
            private set; // 시간 변경
        } // 속성 종료
        public float CombatSeconds // 일시정지 제외 진행 시간
        { // 속성 시작
            get; // 시간 조회
            private set; // 시간 변경
        } // 속성 종료
        public float TotalSeconds // 일시정지 포함 전체 시간
        { // 속성 시작
            get; // 시간 조회
            private set; // 시간 변경
        } // 속성 종료
        public void Advance(float combatDelta, float totalDelta) // 경과 시간 진행
        { // 진행 시작
            if (Phase == StagePhase.Result || Phase == StagePhase.Failed) // 종료 상태 확인
            { // 종료 시작
                return; // 종료 후 시간 고정
            } // 종료 종료
            float scaled = ValidDelta(combatDelta); // 유효한 전투 경과
            float real = ValidDelta(totalDelta); // 유효한 실제 경과
            TotalSeconds += real; // 전체 시간 누적
            CombatSeconds += scaled; // 전투 시간 누적
            float remaining = scaled; // 구간 진행 시간
            while (remaining > 0f) // 시간 경계 순회
            { // 순회 시작
                float limit = Phase == StagePhase.Preparation ? 20f : Phase == StagePhase.CombatOne ? 45f : Phase == StagePhase.CombatTwo ? 50f : 0f; // 시험 구간 시간
                if (limit <= 0f) // 선택 또는 보스 구간 확인
                { // 대기 시작
                    PhaseSeconds += remaining; // 구간 시간 누적
                    return; // 명시적 완료 대기
                } // 대기 종료
                float step = Math.Min(remaining, Math.Max(0f, limit - PhaseSeconds)); // 이번 구간 시간
                PhaseSeconds += step; // 구간 시간 누적
                remaining -= step; // 남은 시간 차감
                if (PhaseSeconds < limit) // 구간 종료 확인
                { // 진행 대기 시작
                    return; // 다음 프레임 대기
                } // 진행 대기 종료
                Change(Phase == StagePhase.Preparation ? StagePhase.CombatOne : Phase == StagePhase.CombatOne ? StagePhase.UpgradeOne : StagePhase.UpgradeTwo); // 다음 구간 진입
            } // 순회 종료
        } // 진행 종료
        public bool CompleteUpgrade() // 강화 아이템 선택 완료
        { // 완료 시작
            if (Phase != StagePhase.UpgradeOne && Phase != StagePhase.UpgradeTwo) // 선택 구간 확인
            { // 무효 처리 시작
                return false; // 중복 또는 잘못된 완료
            } // 무효 처리 종료
            Change(Phase == StagePhase.UpgradeOne ? StagePhase.MidBoss : StagePhase.FinalBoss); // 다음 보스 구간
            return true; // 선택 완료
        } // 완료 종료
        public bool DefeatBoss() // 보스 처치 알림
        { // 처리 시작
            if (Phase != StagePhase.MidBoss && Phase != StagePhase.FinalBoss) // 보스 구간 확인
            { // 무효 처리 시작
                return false; // 잘못된 처치 신호
            } // 무효 처리 종료
            Change(Phase == StagePhase.MidBoss ? StagePhase.CombatTwo : StagePhase.Result); // 처치 후 구간
            return true; // 처치 반영
        } // 처리 종료
        public void Fail() // 출격 실패 처리
        { // 처리 시작
            if (Phase != StagePhase.Result && Phase != StagePhase.Failed) // 진행 상태 확인
            { // 실패 시작
                Change(StagePhase.Failed); // 실패 구간 변경
            } // 실패 종료
        } // 처리 종료
        private void Change(StagePhase next) // 구간 변경
        { // 변경 시작
            Phase = next; // 새 구간 보관
            PhaseSeconds = 0f; // 구간 시간 초기화
            PhaseChanged?.Invoke(next); // 변경 알림 전달
        } // 변경 종료
        private static float ValidDelta(float value) // 시간 입력 검증
        { // 검증 시작
            return float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 0f : value; // 잘못된 시간 제외
        } // 검증 종료
    } // 클래스 종료
} // 영역 종료
