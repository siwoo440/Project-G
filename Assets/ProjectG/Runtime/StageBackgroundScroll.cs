using UnityEngine; // 유니티 화면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class StageBackgroundScroll : MonoBehaviour // 전투 배경 스크롤
    { // 클래스 시작
        private const float TrialScrollSpeed = 0.035f; // 초당 배경 이미지 이동 비율
        private Camera targetCamera; // 전투 카메라 참조
        private BattlePlayer player; // 재출격 알림 대상
        private SpriteRenderer sprite; // 배경 표시 요소
        private Material material; // 전투 전용 배경 재질
        private MaterialPropertyBlock properties; // 개별 화면 속성
        private float phase; // 반복 이미지의 현재 위치
        public void Begin(Camera camera, BattlePlayer battlePlayer) // 배경과 재출격 연결
        { // 연결 시작
            targetCamera = camera; // 전투 카메라 보관
            player = battlePlayer; // 플레이어 참조 보관
            sprite = GetComponent<SpriteRenderer>(); // 배경 표시 요소 조회
            properties = new MaterialPropertyBlock(); // 개별 화면 속성 생성
            Shader shader = Resources.Load<Shader>("BattleArt/StageBackgroundScroll"); // 스크롤 셰이더 조회
            if (sprite != null && shader != null && shader.isSupported) // 사용 가능한 셰이더 확인
            { // 재질 연결 시작
                material = new Material(shader); // 전투 전용 재질 생성
                sprite.sharedMaterial = material; // 배경 셰이더 연결
            } // 재질 연결 종료
            if (player != null) // 플레이어 연결 확인
            { // 초기화 연결 시작
                player.BattleRestarted += ResetScroll; // 재출격 위치 초기화 연결
            } // 초기화 연결 종료
            ResetScroll(); // 첫 출격 화면 위치
        } // 연결 종료
        private void Update() // 프레임별 배경 갱신
        { // 갱신 시작
            Advance(Time.deltaTime); // 정지 상태를 반영한 이동 시간
        } // 갱신 종료
        public void Advance(float seconds) // 전투 시간만큼 배경 이동
        { // 이동 시작
            if (seconds > 0f && !float.IsNaN(seconds) && !float.IsInfinity(seconds)) // 유효한 진행 시간 확인
            { // 위치 이동 시작
                phase = Mathf.Repeat(phase + seconds * TrialScrollSpeed, 1f); // 아래 방향의 반복 위치
            } // 위치 이동 종료
            ApplyLayout(); // 위치와 화면 크기 반영
        } // 이동 종료
        private void ResetScroll() // 재출격 배경 초기화
        { // 초기화 시작
            phase = 0f; // 첫 배경 위치 복원
            ApplyLayout(); // 초기 위치와 크기 적용
        } // 초기화 종료
        private void ApplyLayout() // 배경 위치와 화면 범위 갱신
        { // 갱신 시작
            if (sprite == null || sprite.sprite == null || targetCamera == null) // 필수 화면 요소 확인
            { // 생략 시작
                return; // 요소가 없는 화면 유지
            } // 생략 종료
            Vector3 imageSize = sprite.sprite.bounds.size; // 원본 이미지 크기
            if (imageSize.x <= 0f || imageSize.y <= 0f) // 유효한 원본 크기 확인
            { // 생략 시작
                return; // 크기 계산 차단
            } // 생략 종료
            float height = targetCamera.orthographicSize * 2f; // 전투 화면 높이
            float cover = Mathf.Max(height * 16f / 9f / imageSize.x, height / imageSize.y); // 16대9 전투 영역 배율
            transform.localScale = new Vector3(cover, cover, 1f); // 화면을 덮는 이미지 크기
            sprite.GetPropertyBlock(properties); // 기존 개별 속성 보존
            properties.SetFloat("_ScrollPhase", phase); // 반복 이미지 이동 위치
            sprite.SetPropertyBlock(properties); // 개별 표시 속성 적용
        } // 갱신 종료
        private void OnDestroy() // 배경 제거 처리
        { // 제거 시작
            if (player != null) // 플레이어 존재 확인
            { // 알림 해제 시작
                player.BattleRestarted -= ResetScroll; // 재출격 연결 해제
            } // 알림 해제 종료
            if (material != null) // 생성한 재질 확인
            { // 재질 제거 시작
                if (Application.isPlaying) // 실제 플레이 확인
                { // 실행 중 제거 시작
                    Destroy(material); // 프레임 종료 시 재질 제거
                } // 실행 중 제거 종료
                else // 편집기 시험 확인
                { // 편집기 제거 시작
                    DestroyImmediate(material); // 시험 재질 즉시 제거
                } // 편집기 제거 종료
            } // 재질 제거 종료
        } // 제거 종료
    } // 클래스 종료
} // 영역 종료
