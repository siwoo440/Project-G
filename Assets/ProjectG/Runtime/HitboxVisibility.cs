namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class HitboxVisibility // 피격 표시 규칙
    { // 클래스 시작
        public static bool IsVisible(bool focused, bool normalVisible, bool focusVisible) // 표시 여부 확인
        { // 확인 시작
            return focused ? focusVisible : normalVisible; // 현재 상태 설정 선택
        } // 확인 종료
    } // 클래스 종료
} // 영역 종료
