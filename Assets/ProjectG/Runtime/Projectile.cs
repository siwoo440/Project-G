using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class Projectile : MonoBehaviour // 플레이어 탄환
    { // 클래스 시작
        private Vector2 velocity; // 탄환 이동 속도
        private float upperBound; // 화면 위쪽 경계
        private static Sprite sharedSprite; // 공통 임시 이미지
        public void Begin(Vector2 speed, float top) // 탄환 초기화
        { // 초기화 시작
            velocity = speed; // 이동 속도 보관
            upperBound = top; // 위쪽 경계 보관
            SpriteRenderer image = gameObject.AddComponent<SpriteRenderer>(); // 표시 구성 요소
            image.sprite = GetSprite(); // 임시 이미지 적용
            image.color = new Color(1f, 0.85f, 0.35f); // 탄환 색상
            transform.localScale = new Vector3(0.10f, 0.24f, 1f); // 탄환 표시 크기
        } // 초기화 종료
        private void Update() // 탄환 이동
        { // 이동 시작
            transform.position += (Vector3)(velocity * Time.deltaTime); // 경과 시간 이동
            if (transform.position.y > upperBound + 1f) // 화면 이탈 확인
            { // 제거 시작
                Destroy(gameObject); // 이탈 탄환 제거
            } // 제거 종료
        } // 이동 종료
        public static Sprite GetSprite() // 공통 이미지 준비
        { // 이미지 시작
            if (sharedSprite == null) // 이미지 존재 확인
            { // 생성 시작
                Texture2D texture = Texture2D.whiteTexture; // 흰색 기본 텍스처
                sharedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width); // 사각형 이미지 생성
            } // 생성 종료
            return sharedSprite; // 공통 이미지 반환
        } // 이미지 종료
    } // 클래스 종료
} // 영역 종료
