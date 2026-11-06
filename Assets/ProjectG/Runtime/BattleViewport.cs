using UnityEngine; // 유니티 화면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    [RequireComponent(typeof(Camera))] // 카메라 구성 필수
    public sealed class BattleViewport : MonoBehaviour // 전투 화면 분할 유지
    { // 클래스 시작
        private Camera targetCamera; // 전투 카메라
        private void Awake() // 최초 카메라 설정
        { // 초기화 시작
            targetCamera = GetComponent<Camera>(); // 카메라 참조
            Apply(); // 첫 프레임 전 전투 영역 적용
        } // 초기화 종료
        private void OnEnable() // 출격 카메라 활성화
        { // 활성 시작
            if (targetCamera == null) // 카메라 참조 확인
            { // 조회 시작
                targetCamera = GetComponent<Camera>(); // 실제 카메라 조회
            } // 조회 종료
            Apply(); // 활성 직후 전투 영역 적용
        } // 활성 종료
        private void LateUpdate() // 화면 비율 갱신
        { // 갱신 시작
            Apply(); // 해상도 변경 반영
        } // 갱신 종료
        private void Apply() // 실제 전투 카메라 배치
        { // 적용 시작
            if (targetCamera == null || Screen.width <= 0 || Screen.height <= 0) // 카메라와 유효 화면 확인
            { // 생략 시작
                return; // 잘못된 화면 적용 차단
            } // 생략 종료
            targetCamera.rect = BattleLayout.BattleViewport(Screen.width, Screen.height); // 16대9 콘텐츠의 왼쪽 전투 영역
            targetCamera.aspect = 4f / 3f; // 전투 영역 종횡비 유지
        } // 적용 종료
    } // 클래스 종료
} // 영역 종료
