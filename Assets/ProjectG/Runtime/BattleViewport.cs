using UnityEngine; // 유니티 화면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    [RequireComponent(typeof(Camera))] // 카메라 구성 필수
    public sealed class BattleViewport : MonoBehaviour // 화면 비율 유지
    { // 클래스 시작
        private Camera targetCamera; // 전투 카메라
        private void Awake() // 초기 설정
        { // 초기화 시작
            targetCamera = GetComponent<Camera>(); // 카메라 참조
        } // 초기화 종료
        private void LateUpdate() // 화면 비율 갱신
        { // 갱신 시작
            targetCamera.rect = BattleSetup.CalculateViewport(Screen.width, Screen.height); // 중앙 표시 영역 적용
            targetCamera.aspect = 16f / 9f; // 전투 화면 비율 유지
        } // 갱신 종료
    } // 클래스 종료
} // 영역 종료
