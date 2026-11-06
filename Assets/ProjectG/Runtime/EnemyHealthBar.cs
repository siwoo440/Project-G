using UnityEngine; // 유니티 표시 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class EnemyHealthBar : MonoBehaviour // 적 체력 표시
    { // 클래스 시작
        private const float DisplaySeconds = 2f; // 피해 후 표시 시험 시간
        [SerializeField] private SpriteRenderer frame; // 편집 가능한 체력바 외곽
        [SerializeField] private SpriteRenderer fill; // 편집 가능한 체력 이미지
        private Vector3 baseFillScale; // 편집한 체력 이미지 크기
        private Vector3 baseFillPosition; // 편집한 체력 이미지 위치
        private bool initialized; // 기본 표시값 보관 여부
        private float remaining; // 남은 표시 시간
        public bool HasValidVisual => frame != null && frame.sprite != null && frame.transform.IsChildOf(transform) && fill != null && fill.sprite != null && fill.transform.IsChildOf(transform); // 직렬화된 체력바 확인
        public void Initialize(SpriteRenderer body, bool boss) // 체력바 표시 초기화
        { // 초기화 시작
            if (!HasValidVisual) // 편집된 체력바 확인
            { // 오류 시작
                Debug.LogError("적 체력바의 외곽 또는 채움 이미지가 연결되지 않았습니다", this); // 연결 오류 표시
                return; // 임의 표시 생성 차단
            } // 오류 종료
            if (!initialized) // 기본 표시값 확인
            { // 보관 시작
                baseFillScale = fill.transform.localScale; // 편집한 채움 크기
                baseFillPosition = fill.transform.localPosition; // 편집한 채움 위치
                initialized = true; // 기본 표시값 보관 완료
            } // 보관 종료
            fill.transform.localScale = baseFillScale; // 이전 피해 비율 초기화
            fill.transform.localPosition = baseFillPosition; // 이전 피해 위치 초기화
            Hide(); // 초기 표시 숨김
        } // 초기화 종료
        public void Show(float ratio) // 피해 후 체력 표시
        { // 표시 시작
            if (!initialized || !HasValidVisual) // 연결과 초기화 확인
            { // 생략 시작
                return; // 잘못된 표시 차단
            } // 생략 종료
            float amount = Mathf.Clamp01(ratio); // 체력 비율 제한
            fill.transform.localScale = new Vector3(baseFillScale.x * amount, baseFillScale.y, baseFillScale.z); // 편집 크기 기준 남은 체력
            float offset = fill.sprite.bounds.size.x * baseFillScale.x * 0.5f * (1f - amount); // 왼쪽 기준 감소 거리
            fill.transform.localPosition = baseFillPosition + Vector3.left * offset; // 편집 위치 기준 감소
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
