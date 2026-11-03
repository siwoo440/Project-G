using UnityEngine; // 유니티 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class CombatRules // 전투 규칙 계산
    { // 클래스 시작
        public static bool Focus(bool current, bool pressed, bool held, bool toggle) // 집중 상태 계산
        { // 계산 시작
            return toggle ? (pressed ? !current : current) : held; // 입력 방식별 집중 상태
        } // 계산 종료
        public static bool ShouldFire(ref float timer, float elapsed, float interval) // 발사 시간 계산
        { // 계산 시작
            timer -= Mathf.Max(0f, elapsed); // 경과 시간 차감
            if (timer > 0.000001f) // 발사 대기 확인
            { // 대기 시작
                return false; // 발사 대기
            } // 대기 종료
            timer = Mathf.Max(0f, timer) + Mathf.Max(0.001f, interval); // 다음 발사 시간
            return true; // 발사 허용
        } // 계산 종료
    } // 클래스 종료
} // 영역 종료
