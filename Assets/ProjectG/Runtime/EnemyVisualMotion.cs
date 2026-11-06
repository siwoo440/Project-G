using UnityEngine; // 유니티 위치와 수학
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    internal sealed class EnemyVisualMotion // 단일 적 이미지의 시험 움직임
    { // 움직임 시작
        private Transform body; // 이미지 전용 변환
        private Vector3 baseScale; // 적 종류의 기본 이미지 크기
        private float phase; // 현재 반복 위치
        private float speed; // 움직임 속도
        private float bob; // 상하 부유 폭
        private float tilt; // 좌우 기울기 폭
        private float pulseX; // 가로 맥동 폭
        private float pulseY; // 세로 맥동 폭
        public void Begin(Transform image, EnemyKind kind) // 적 종류별 움직임 초기화
        { // 초기화 시작
            body = image; // 적 몸체 연결
            if (body == null) // 표시 대상 확인
            { // 생략 시작
                return; // 표시 없는 적 생략
            } // 생략 종료
            baseScale = body.localScale; // 해당 종류의 기본 크기
            body.localPosition = Vector3.zero; // 몸체 위치 초기화
            body.localRotation = Quaternion.identity; // 몸체 각도 초기화
            phase = 0f; // 반복 위치 초기화
            switch (kind) // 적 종류별 시험 연출
            { // 종류 분기 시작
                case EnemyKind.Aimed: // 조준형의 가벼운 부유
                    SetProfile(1.8f, 0.035f, 1.1f, 0.012f, 0.005f); // 조준형 움직임
                    break; // 조준형 종료
                case EnemyKind.Spread: // 확산형의 느린 맥동
                    SetProfile(1.5f, 0.045f, 0.8f, 0.018f, 0.009f); // 확산형 움직임
                    break; // 확산형 종료
                case EnemyKind.Dash: // 돌진형의 빠른 진동
                    SetProfile(2.5f, 0.03f, 1.4f, 0.014f, 0.005f); // 돌진형 움직임
                    break; // 돌진형 종료
                case EnemyKind.MidBoss: // 중간 보스의 완만한 부유
                    SetProfile(1.2f, 0.015f, 0.25f, 0.006f, 0.003f); // 중간 보스 움직임
                    break; // 중간 보스 종료
                default: // 최종 보스의 느린 맥동
                    SetProfile(0.9f, 0.012f, 0.2f, 0.005f, 0.002f); // 최종 보스 움직임
                    break; // 최종 보스 종료
            } // 종류 분기 종료
        } // 초기화 종료
        private void SetProfile(float motionSpeed, float bobAmount, float tiltAmount, float horizontalPulse, float verticalPulse) // 시험 동작 값 보관
        { // 보관 시작
            speed = motionSpeed; // 반복 속도
            bob = bobAmount; // 부유 폭
            tilt = tiltAmount; // 기울기 폭
            pulseX = horizontalPulse; // 가로 맥동 폭
            pulseY = verticalPulse; // 세로 맥동 폭
        } // 보관 종료
        public void Advance(float elapsed) // 전투 시간에 따른 이미지 움직임
        { // 진행 시작
            if (body == null || !body.gameObject.activeInHierarchy || Time.timeScale <= 0f || elapsed <= 0f || float.IsNaN(elapsed) || float.IsInfinity(elapsed)) // 표시와 시간 확인
            { // 생략 시작
                return; // 비활성 또는 정지 중 진행 차단
            } // 생략 종료
            float cycle = Mathf.PI * 2f; // 반복 각도
            float safeElapsed = Mathf.Repeat(elapsed, cycle / speed); // 큰 시간값의 반복 범위
            phase = Mathf.Repeat(phase + safeElapsed * speed, cycle); // 다음 반복 위치
            float wave = Mathf.Sin(phase); // 부드러운 왕복 신호
            body.localPosition = new Vector3(0f, bob * wave, 0f); // 이미지의 위아래 부유
            body.localRotation = Quaternion.Euler(0f, 0f, tilt * wave); // 이미지의 좌우 기울기
            body.localScale = new Vector3(baseScale.x * (1f + pulseX * wave), baseScale.y * (1f + pulseY * wave), baseScale.z); // 이미지의 가벼운 맥동
        } // 진행 종료
    } // 움직임 종료
} // 영역 종료
