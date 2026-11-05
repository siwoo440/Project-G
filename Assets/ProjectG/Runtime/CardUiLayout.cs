using UnityEngine; // 유니티 화면 좌표
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class CardUiLayout // 카드 화면 위치 계산
    { // 클래스 시작
        public static Rect View(Camera camera, float screenHeight) // 카메라 화면 좌표 변환
        { // 변환 시작
            Rect pixels = camera.pixelRect; // 카메라 화면 영역
            return new Rect(pixels.xMin, screenHeight - pixels.yMax, pixels.width, pixels.height); // 위쪽 기준 좌표
        } // 변환 종료
        public static Rect Card(Rect view, int index) // 선택 카드 위치
        { // 계산 시작
            float gap = Mathf.Max(5f, view.width * 0.012f); // 카드 간격
            float width = Mathf.Min(view.width * 0.27f, 360f); // 카드 너비
            float height = Mathf.Min(view.height * 0.56f, 520f); // 카드 높이
            float left = view.center.x - (width * 3f + gap * 2f) * 0.5f; // 세 장 시작 위치
            float top = view.center.y - height * 0.43f; // 카드 위쪽 위치
            return new Rect(left + Mathf.Clamp(index, 0, 2) * (width + gap), top, width, height); // 카드 영역
        } // 계산 종료
        public static Rect SwapBox(Rect view) // 스킬 교체 확인 화면
        { // 계산 시작
            Rect card = Card(view, 0); // 카드 크기 참조
            return new Rect(view.center.x - card.width * 0.75f, view.center.y - card.height * 0.18f, card.width * 1.5f, card.height * 0.36f); // 확인 화면 영역
        } // 계산 종료
        public static Rect Confirm(Rect view) // 교체 확인 버튼
        { // 계산 시작
            Rect box = SwapBox(view); // 확인 화면 영역
            return new Rect(box.x + 10f, box.yMax - 42f, box.width * 0.45f - 10f, 32f); // 확인 버튼 영역
        } // 계산 종료
        public static Rect Cancel(Rect view) // 교체 취소 버튼
        { // 계산 시작
            Rect box = SwapBox(view); // 확인 화면 영역
            return new Rect(box.center.x + 4f, box.yMax - 42f, box.width * 0.45f - 10f, 32f); // 취소 버튼 영역
        } // 계산 종료
    } // 클래스 종료
} // 영역 종료
