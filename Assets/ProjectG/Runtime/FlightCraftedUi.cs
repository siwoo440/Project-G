using UnityEngine; // 버튼 글자의 크기와 정렬
using UnityEngine.UI; // 실제 제작 화면 버튼

namespace ProjectG // 프로젝트 제작 마법 영역
{
    internal static class FlightCraftedUi // 복제한 버튼의 글자 영역 보정
    {
        internal static void FitLabel(Button button, int maximum = 26) // 버튼 크기에 맞는 선명한 한글 문구
        {
            Text label = button.GetComponentInChildren<Text>(); // 실제 표시할 버튼 글자
            RectTransform rect = label.rectTransform; // 복제된 글자의 원래 고정 폭
            rect.anchorMin = Vector2.zero; // 버튼의 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 버튼의 오른쪽 위까지 확장
            rect.offsetMin = new Vector2(5f, 2f); // 테두리 안쪽의 최소 여백
            rect.offsetMax = new Vector2(-5f, -2f); // 테두리 안쪽의 최대 여백
            label.alignment = TextAnchor.MiddleCenter; // 버튼 중앙의 한글 정렬
            label.resizeTextForBestFit = true; // 긴 문구도 실제 버튼 폭 안에 표시
            label.resizeTextMinSize = 12; // 가장 작은 허용 글자 크기
            label.resizeTextMaxSize = maximum; // 실제 버튼에 사용할 최대 글자 크기
            label.fontSize = maximum; // 기준 화면의 기본 글자 크기
        }
    }
}
