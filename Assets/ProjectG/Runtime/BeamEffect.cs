using UnityEngine; // 유니티 표시 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BeamEffect : MonoBehaviour // 임시 광선 표시
    { // 클래스 시작
        private float remaining = 0.25f; // 시험 표시 시간
        public static void Create(Rect area) // 광선 표시 생성
        { // 생성 시작
            GameObject beam = new GameObject("SniperBeam"); // 광선 객체
            beam.transform.position = area.center; // 광선 중심
            beam.transform.localScale = new Vector3(area.width, area.height, 1f); // 광선 크기
            SpriteRenderer image = beam.AddComponent<SpriteRenderer>(); // 광선 표시 요소
            image.sprite = Projectile.GetSprite(); // 임시 이미지
            image.color = new Color(0.4f, 0.9f, 1f, 0.65f); // 시험 광선 색상
            image.sortingOrder = 3; // 표시 순서
            beam.AddComponent<BeamEffect>(); // 표시 시간 연결
        } // 생성 종료
        private void Update() // 표시 시간 진행
        { // 진행 시작
            if (Time.timeScale <= 0f) // 전투 정지 확인
            { // 정지 처리 시작
                return; // 표시 시간 유지
            } // 정지 처리 종료
            remaining -= Time.deltaTime; // 경과 시간 차감
            if (remaining <= 0f) // 표시 종료 확인
            { // 제거 시작
                Destroy(gameObject); // 광선 표시 제거
            } // 제거 종료
        } // 진행 종료
    } // 클래스 종료
} // 영역 종료
