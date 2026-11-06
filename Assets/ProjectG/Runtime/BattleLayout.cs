using UnityEngine; // 유니티 화면 좌표
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class BattleLayout // 전투와 정보 패널 분할
    { // 클래스 시작
        public static Rect BattleViewport(float width, float height) // 전체 콘텐츠의 왼쪽 전투 영역
        { // 계산 시작
            Rect content = BattleSetup.CalculateViewport(width, height); // 기존 16대9 콘텐츠 영역
            return new Rect(content.x, content.y, content.width * 0.75f, content.height); // 왼쪽 75퍼센트 전투 영역
        } // 계산 종료
        public static Rect PanelViewport(float width, float height) // 전체 콘텐츠의 오른쪽 정보 영역
        { // 계산 시작
            Rect content = BattleSetup.CalculateViewport(width, height); // 기존 16대9 콘텐츠 영역
            return new Rect(content.x + content.width * 0.75f, content.y, content.width * 0.25f, content.height); // 오른쪽 25퍼센트 정보 영역
        } // 계산 종료
        public static Rect ClampRect(Rect area, Rect bounds) // 표시 영역의 전투 화면 제한
        { // 계산 시작
            float width = Mathf.Min(Mathf.Max(0f, area.width), Mathf.Max(0f, bounds.width)); // 사용 가능한 가로 크기
            float height = Mathf.Min(Mathf.Max(0f, area.height), Mathf.Max(0f, bounds.height)); // 사용 가능한 세로 크기
            float x = Mathf.Clamp(area.x, bounds.xMin, bounds.xMax - width); // 전투 가로 경계 제한
            float y = Mathf.Clamp(area.y, bounds.yMin, bounds.yMax - height); // 전투 세로 경계 제한
            return new Rect(x, y, width, height); // 화면 안 표시 영역
        } // 계산 종료
    } // 클래스 종료
} // 영역 종료
