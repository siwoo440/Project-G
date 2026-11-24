using UnityEngine; // 월드 위치와 프레임 시간
using UnityEngine.InputSystem; // 키보드 비행 입력

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    [DisallowMultipleComponent] // 한 비행체당 컨트롤러 한 개
    public sealed class FirstFlightController : MonoBehaviour // 3축 자유 비행
    { // 컨트롤러 시작
        [SerializeField, Min(0f)] private float speed = 8f; // 시험용 초당 이동 거리

        private void Update() // 매 프레임 비행 처리
        { // 처리 시작
            Keyboard keyboard = Keyboard.current; // 현재 입력 장치
            if (keyboard == null) // 키보드 연결 여부
            { // 입력 생략 시작
                return; // 장치가 없는 상태 보호
            } // 입력 생략 종료

            float lateral = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f); // 좌우 입력
            bool descending = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed; // 양쪽 Ctrl 하강
            float vertical = (keyboard.spaceKey.isPressed ? 1f : 0f) - (descending ? 1f : 0f); // 상승과 하강 입력
            float depth = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f); // 앞뒤 입력
            Vector3 direction = Vector3.ClampMagnitude(new Vector3(lateral, vertical, depth), 1f); // 동시 입력의 최대 속도 유지
            transform.position += direction * (speed * Time.deltaTime); // 월드 공간 이동
        } // 처리 종료
    } // 컨트롤러 종료
} // 영역 종료
