using UnityEditor; // 프로젝트의 Unity Game 뷰 해상도
using UnityEngine; // 편집기 창과 원본 미리보기

namespace ProjectG.Editor // 에디터 전용 프로젝트 기능
{
    internal static class FlightPreviewResolution // Game 뷰의 저해상도 비율 옵션
    {
        private const string LowResolutionProperty = "m_LowResolutionForAspectRatios"; // Unity 6000 Game 뷰 설정 이름
        private static double deadline; // 시작할 때 레이아웃이 복원될 때까지 짧은 대기

        [InitializeOnLoadMethod] // 이 프로젝트에서 스크립트를 로드하면 적용
        private static void RestoreNativePreview() // Game 뷰가 생성될 때 원본 렌더 크기 사용
        {
            if (Application.isBatchMode) // 화면이 없는 배치와 빌드에는 편집기 창 개입 없음
            {
                return; // 별도 Windows 검증과 에셋 처리 유지
            }
            deadline = EditorApplication.timeSinceStartup + 8d; // 레이아웃 초기화 대기 한도
            EditorApplication.update += ApplyToGameViews; // 열리는 프로젝트의 Game 뷰에만 한 번 적용
        }

        private static void ApplyToGameViews() // 현재 에디터에 복원된 실제 Game 뷰
        {
            System.Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); // 설치 Unity의 Game 뷰 클래스
            if (type == null) // 이후 버전의 클래스 변경에서 안전하게 종료
            {
                EditorApplication.update -= ApplyToGameViews; // 다른 레이아웃은 그대로 보존
                return; // 현재 편집기 방식을 모르는 경우
            }
            Object[] windows = Resources.FindObjectsOfTypeAll(type); // 사용자 프로젝트 내에 열린 인스턴스
            if (windows.Length == 0 && EditorApplication.timeSinceStartup < deadline) // 시작 레이아웃이 아직 배치되지 않음
            {
                return; // 설정 변경 전에 창 생성 대기
            }
            EditorApplication.update -= ApplyToGameViews; // 현재 프로젝트가 로드될 때 한 번만 적용
            foreach (EditorWindow window in windows) // Game 뷰의 모든 종횡비 상태
            {
                if (UseNativeResolution(window)) // 저해상도가 바뀐 창
                {
                    window.Repaint(); // 미리보기를 원래 화면 크기로 그리기
                }
            }
        }

        internal static bool UseNativeResolution(EditorWindow window) // Game 뷰에서 저해상도 옵션만 해제
        {
            var serialized = new SerializedObject(window); // 실제로 저장되는 게임 미리보기 상태
            SerializedProperty modes = serialized.FindProperty(LowResolutionProperty); // 화면 비율별 저해상도 여부
            if (modes == null || !modes.isArray) // Unity 버전의 다른 설정 건드리지 않음
            {
                return false; // 현재 프로젝트에 바꿀 수 있는 대상 없음
            }
            bool changed = false; // 저해상도에서 원본으로 실제 바뀐 여부
            for (int index = 0; index < modes.arraySize; index++) // Free Aspect와 고정 비율 옵션 전체
            {
                SerializedProperty flag = modes.GetArrayElementAtIndex(index); // 현재 비율의 실제 저해상도 여부
                if (flag.propertyType == SerializedPropertyType.Boolean && flag.boolValue) // 켜진 실제 저해상도 옵션
                {
                    flag.boolValue = false; // 원본 해상도로 미리보기
                    changed = true; // 실제 변경 저장 대상
                }
            }
            if (changed) // Game 뷰의 다른 상태는 변경하지 않음
            {
                serialized.ApplyModifiedPropertiesWithoutUndo(); // 현재 창의 저해상도 옵션만 반영
            }
            return changed; // 현재 창의 재렌더가 필요할지 알림
        }
    }
}
