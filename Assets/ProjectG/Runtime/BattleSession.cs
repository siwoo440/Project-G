using UnityEngine; // 벡터 자료형
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BattleSession // 전투 상태
    { // 클래스 시작
        private float shotTimer; // 다음 발사 대기
        public Vector2 Position // 플레이어 위치
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public bool Focused // 집중 상태
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public BattleSession(Vector2 position) // 초기 위치 설정
        { // 초기화 시작
            Position = position; // 시작 위치 보관
        } // 초기화 종료
        public int Advance(Vector2 input, bool pressed, bool held, bool toggle, float elapsed, float left, float right, float bottom, float top) // 전투 상태 진행
        { // 계산 시작
            Focused = CombatRules.Focus(Focused, pressed, held, toggle); // 집중 상태 갱신
            Position = MovementMath.Move(Position, input, left, right, bottom, top, Focused, elapsed); // 이동 위치 갱신
            float interval = Focused ? 0.20f : 0.25f; // 시험용 발사 간격
            return CombatRules.ShouldFire(ref shotTimer, elapsed, interval) ? (Focused ? 1 : 2) : 0; // 발사 탄환 수
        } // 계산 종료
    } // 클래스 종료
} // 영역 종료
