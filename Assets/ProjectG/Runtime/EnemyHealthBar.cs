using UnityEngine; // 유니티 표시 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class EnemyHealthBar : MonoBehaviour // 적 체력 표시
    { // 클래스 시작
        private const float DisplaySeconds = 2f; // 피해 후 표시 시험 시간
        private SpriteRenderer fill; // 체력 이미지
        private float width; // 표시 전체 폭
        private float remaining; // 남은 표시 시간
        public void Initialize(SpriteRenderer body, bool boss) // 몸체 기준 초기화
        { // 초기화 시작
            if (fill == null) // 표시 객체 최초 생성 확인
            { // 생성 시작
                GameObject frameObject = new GameObject("Frame"); // 외곽 이미지 객체
                frameObject.transform.SetParent(transform, false); // 외곽 연결
                SpriteRenderer frame = frameObject.AddComponent<SpriteRenderer>(); // 외곽 표시 구성
                frame.sprite = BattleSprites.Load("ui_card_frame"); // 기존 금색 장식 사용
                frame.sortingOrder = 4; // 적 앞 표시 순서
                GameObject fillObject = new GameObject("Fill"); // 체력 이미지 객체
                fillObject.transform.SetParent(transform, false); // 체력 이미지 연결
                fill = fillObject.AddComponent<SpriteRenderer>(); // 체력 표시 구성
                fill.sprite = Projectile.GetSprite(); // 빈틈 없는 기본 이미지
                fill.color = new Color(0.12f, 0.84f, 0.95f, 1f); // 읽기 쉬운 청록색
                fill.sortingOrder = 5; // 외곽 위 표시 순서
            } // 생성 종료
            width = Mathf.Max(1f, body.sprite.bounds.size.x * body.transform.localScale.x * 0.7f); // 몸체 비례 가로 폭
            float height = boss ? 0.24f : 0.18f; // 적 종류별 표시 높이
            float bottom = body.transform.localPosition.y - body.sprite.bounds.size.y * body.transform.localScale.y * 0.5f; // 확대 몸체 아래 경계
            transform.localPosition = new Vector3(body.transform.localPosition.x, bottom - height * 0.5f - 0.08f, 0f); // 몸체 아래 여백
            SpriteRenderer border = transform.Find("Frame").GetComponent<SpriteRenderer>(); // 외곽 이미지 조회
            border.transform.localScale = new Vector3(width / border.sprite.bounds.size.x, height / border.sprite.bounds.size.y, 1f); // 외곽 크기 조정
            fill.transform.localScale = new Vector3(width * 0.9f / fill.sprite.bounds.size.x, height * 0.55f / fill.sprite.bounds.size.y, 1f); // 금색 외곽 안쪽 크기
            Hide(); // 초기 표시 숨김
        } // 초기화 종료
        public void Show(float ratio) // 피해 후 체력 표시
        { // 표시 시작
            float amount = Mathf.Clamp01(ratio); // 체력 비율 제한
            fill.transform.localScale = new Vector3(width * 0.9f * amount / fill.sprite.bounds.size.x, fill.transform.localScale.y, 1f); // 남은 체력 폭
            fill.transform.localPosition = new Vector3(-width * 0.45f * (1f - amount), 0f, 0f); // 안쪽 왼쪽 기준 감소
            remaining = DisplaySeconds; // 시험 표시 시간 갱신
            gameObject.SetActive(true); // 체력바 표시
        } // 표시 종료
        public void Advance(float elapsed) // 표시 시간 진행
        { // 진행 시작
            if (!gameObject.activeSelf || elapsed <= 0f) // 표시와 경과 시간 확인
            { // 생략 시작
                return; // 시간 처리 생략
            } // 생략 종료
            remaining -= elapsed; // 남은 시간 감소
            if (remaining <= 0f) // 표시 만료 확인
            { // 만료 시작
                Hide(); // 체력바 숨김
            } // 만료 종료
        } // 진행 종료
        public void Hide() // 체력바 숨김
        { // 숨김 시작
            remaining = 0f; // 남은 시간 초기화
            gameObject.SetActive(false); // 체력바 비활성
        } // 숨김 종료
    } // 클래스 종료
} // 영역 종료
