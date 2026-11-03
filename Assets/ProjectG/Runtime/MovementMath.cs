using UnityEngine; // 유니티 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class MovementMath // 이동 계산기
    { // 클래스 시작
        public static Vector2 Move(Vector2 position, Vector2 input, float left, float right, float bottom, float top, bool focused, float elapsed) // 위치 계산
        { // 계산 시작
            Vector2 direction = Vector2.ClampMagnitude(input, 1f); // 대각선 속도 보정
            float speed = (right - left) / 4f; // 일반 횡단 속도
            float focusScale = focused ? 0.5f : 1f; // 집중 속도 비율
            Vector2 next = position + direction * speed * focusScale * Mathf.Max(0f, elapsed); // 다음 위치 계산
            next.x = Mathf.Clamp(next.x, left, right); // 가로 경계 제한
            next.y = Mathf.Clamp(next.y, bottom, top); // 세로 경계 제한
            return next; // 제한된 위치 반환
        } // 계산 끝
    } // 클래스 끝
} // 영역 끝
