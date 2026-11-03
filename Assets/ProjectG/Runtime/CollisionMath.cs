using UnityEngine; // 벡터 자료형
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class CollisionMath // 탄환 판정 계산
    { // 클래스 시작
        public static bool Intersects(Vector2 center, Vector2 from, Vector2 to, float radius) // 연속 원형 판정
        { // 계산 시작
            Vector2 segment = to - from; // 탄환 이동 구간
            float lengthSquared = segment.sqrMagnitude; // 구간 길이 제곱
            float fraction = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(center - from, segment) / lengthSquared) : 0f; // 가장 가까운 구간 위치
            Vector2 closest = from + segment * fraction; // 가장 가까운 점
            float safeRadius = Mathf.Max(0f, radius); // 음수 반경 방지
            float boundary = safeRadius + 0.000001f; // 경계 계산 오차 보정
            return (center - closest).sqrMagnitude <= boundary * boundary; // 원형 접촉 여부
        } // 계산 종료
    } // 클래스 종료
} // 영역 종료
