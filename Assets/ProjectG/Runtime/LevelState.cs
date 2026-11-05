using System; // 숫자 범위 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class LevelState // 전투 경험치 상태
    { // 클래스 시작
        public int Level // 현재 전투 레벨
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } = 1; // 시작 레벨
        public int Experience // 현재 레벨 경험치
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public int PendingChoices // 대기 중인 카드 선택 수
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public int Required => 10 + 6 * (Level - 1); // 다음 레벨 시험 요구량
        public void Gain(int amount) // 경험치 획득
        { // 획득 시작
            long available = (long)Experience + Math.Max(0, amount); // 경험치 안전 합산
            while (available >= Required && Level < 10000) // 요구 경험치 확인
            { // 레벨업 시작
                available -= Required; // 요구량 차감
                Level++; // 레벨 증가
                PendingChoices++; // 카드 선택 예약
            } // 레벨업 종료
            Experience = (int)Math.Min(available, int.MaxValue); // 남은 경험치 보관
        } // 획득 종료
        public bool ConsumeChoice() // 카드 선택 완료
        { // 완료 시작
            if (PendingChoices <= 0) // 대기 보상 확인
            { // 차단 시작
                return false; // 추가 선택 차단
            } // 차단 종료
            PendingChoices--; // 선택 보상 하나 소비
            return true; // 처리 성공
        } // 완료 종료
    } // 클래스 종료
} // 영역 종료
