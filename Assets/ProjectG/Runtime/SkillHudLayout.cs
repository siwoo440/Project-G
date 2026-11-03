using UnityEngine; // 화면 좌표 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class SkillHudLayout // 스킬 화면 배치
    { // 클래스 시작
        public static Rect Slot(Rect view, int index) // 스킬 칸 위치
        { // 계산 시작
            float margin = Mathf.Clamp(view.height * 0.02f, 1f, 16f); // 화면 가장자리 여백
            float gap = Mathf.Clamp(view.height * 0.008f, 1f, 8f); // 스킬 칸 간격
            float widthLimit = Mathf.Max(1f, (view.width - 2f * margin - 2f * gap) / 3f); // 화면 너비 제한
            float heightLimit = Mathf.Max(1f, view.height - 2f * margin); // 화면 높이 제한
            float size = Mathf.Min(72f, Mathf.Min(view.height * 0.09f, Mathf.Min(widthLimit, heightLimit))); // 정사각형 크기
            int slot = Mathf.Clamp(index, 0, 2); // 스킬 칸 번호 제한
            float x = view.xMax - margin - size * (3 - slot) - gap * (2 - slot); // 오른쪽 정렬 위치
            float y = view.yMax - margin - size; // 아래쪽 정렬 위치
            return new Rect(x, y, size, size); // 정사각형 영역 반환
        } // 계산 종료
        public static string Countdown(float remaining) // 남은 시간 표시
        { // 계산 시작
            return remaining > 0.000001f ? Mathf.CeilToInt(remaining).ToString() : ""; // 초 단위 올림 숫자
        } // 계산 종료
    } // 클래스 종료
} // 영역 종료
