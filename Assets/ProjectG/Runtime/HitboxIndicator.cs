using UnityEngine; // 유니티 화면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class HitboxIndicator : MonoBehaviour // 피격 중심 표시
    { // 클래스 시작
        private BattlePlayer player; // 플레이어 상태
        private SpriteRenderer image; // 피격 표시 이미지
        private static Sprite marker; // 공통 원형 이미지
        public bool HasSceneVisual => GetComponent<SpriteRenderer>() != null && GetComponent<SpriteRenderer>().sprite != null; // 씬에 저장된 표시 확인
        public bool BeginScene(BattlePlayer target) // 편집된 씬 표시 연결
        { // 연결 시작
            image = GetComponent<SpriteRenderer>(); // 씬에 저장된 이미지 조회
            if (target == null || image == null || image.sprite == null) // 실제 표시 참조 확인
            { // 오류 시작
                return false; // 잘못된 씬 연결 차단
            } // 오류 종료
            player = target; // 실제 플레이어 연결
            Refresh(); // 현재 집중 상태 반영
            return true; // 씬 표시 연결 완료
        } // 연결 종료
        public void Begin(BattlePlayer target) // 기존 시험용 표시 초기화
        { // 초기화 시작
            if (HasSceneVisual) // 이미 편집한 이미지 확인
            { // 씬 연결 시작
                BeginScene(target); // 편집된 구성 재사용
                return; // 중복 렌더러 생성 차단
            } // 씬 연결 종료
            player = target; // 플레이어 참조
            image = gameObject.AddComponent<SpriteRenderer>(); // 기존 시험용 표시 생성
            image.sprite = GetMarker(); // 원형 이미지 적용
            image.color = Color.white; // 표시 색상
            image.sortingOrder = 10; // 플레이어 위에 표시
            Vector3 scale = player.transform.lossyScale; // 플레이어 화면 크기
            transform.localScale = new Vector3(0.24f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)), 0.24f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 1f); // 반경 0.12 표시 크기
            Refresh(); // 초기 표시 상태 적용
        } // 초기화 종료
        private void Update() // 표시 갱신
        { // 갱신 시작
            Refresh(); // 현재 상태 설정 반영
        } // 갱신 종료
        private void Refresh() // 표시 상태 계산
        { // 계산 시작
            if (player == null || image == null) // 초기화 여부 확인
            { // 생략 시작
                return; // 표시 계산 생략
            } // 생략 종료
            bool normal = PlayerPrefs.GetInt("ProjectG.HitboxNormal", 0) == 1; // 일반 표시 기본값
            bool focus = PlayerPrefs.GetInt("ProjectG.HitboxFocus", 1) == 1; // 집중 표시 기본값
            image.enabled = HitboxVisibility.IsVisible(player.IsFocused, normal, focus); // 현재 모드 표시 적용
        } // 계산 종료
        private static Sprite GetMarker() // 원형 이미지 준비
        { // 준비 시작
            if (marker != null) // 기존 이미지 확인
            { // 재사용 시작
                return marker; // 기존 이미지 반환
            } // 재사용 종료
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false); // 임시 원형 텍스처
            texture.filterMode = FilterMode.Bilinear; // 가장자리 보간
            for (int y = 0; y < 32; y++) // 세로 픽셀 순회
            { // 세로 순회 시작
                for (int x = 0; x < 32; x++) // 가로 픽셀 순회
                { // 가로 순회 시작
                    float dx = (x + 0.5f - 16f) / 16f; // 중심에서 가로 거리
                    float dy = (y + 0.5f - 16f) / 16f; // 중심에서 세로 거리
                    float alpha = dx * dx + dy * dy <= 1f ? 1f : 0f; // 원 내부 불투명도
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha)); // 원형 픽셀 색상
                } // 가로 순회 종료
            } // 세로 순회 종료
            texture.Apply(); // 텍스처 변경 적용
            marker = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f); // 원형 스프라이트 생성
            return marker; // 원형 이미지 반환
        } // 준비 종료
    } // 클래스 종료
} // 영역 종료
