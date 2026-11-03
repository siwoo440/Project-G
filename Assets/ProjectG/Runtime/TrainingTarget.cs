using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class TrainingTarget : CombatTarget // 시험용 표적
    { // 클래스 시작
        public override bool CanTakeDamage => true; // 시험 표적 사용 가능
        public override void ReceiveDamage(float damage, BattlePlayer source) // 공통 피해 연결
        { // 적용 시작
            ApplyDamage(damage); // 시험 피해 기록
        } // 적용 종료
        public float TotalDamage // 누적 시험 피해
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public void ApplyDamage(float damage) // 시험 피해 적용
        { // 적용 시작
            TotalDamage += Mathf.Max(0f, damage); // 누적 피해 기록
        } // 적용 종료
        public override bool Intersects(Vector2 from, Vector2 to, out bool weak) // 탄환 이동 구간 판정
        { // 판정 시작
            Vector2 center = transform.position; // 표적 중심
            Rect body = new Rect(center.x - 0.65f, center.y - 0.35f, 1.3f, 0.7f); // 시험용 몸체 영역
            Rect weakArea = new Rect(center.x - 0.16f, center.y - 0.35f, 0.32f, 0.7f); // 시험용 약점 영역
            weak = SegmentHits(weakArea, from, to); // 약점 접촉 확인
            return SegmentHits(body, from, to); // 몸체 접촉 확인
        } // 판정 종료
        public override bool IntersectsBeam(Rect beam, out bool weak) // 광선 표적 판정
        { // 판정 시작
            Vector2 center = transform.position; // 표적 중심
            Rect body = new Rect(center.x - 0.65f, center.y - 0.35f, 1.3f, 0.7f); // 표적 몸체 영역
            Rect weakArea = new Rect(center.x - 0.16f, center.y - 0.35f, 0.32f, 0.7f); // 시험용 약점 영역
            weak = beam.Overlaps(weakArea); // 약점 겹침 확인
            return beam.Overlaps(body); // 몸체 겹침 확인
        } // 판정 종료
        public static bool SegmentHits(Rect area, Vector2 from, Vector2 to) // 선분과 사각형 접촉
        { // 계산 시작
            Vector2 delta = to - from; // 이동 방향
            float enter = 0f; // 진입 비율
            float exit = 1f; // 이탈 비율
            return Clip(-delta.x, from.x - area.xMin, ref enter, ref exit) && Clip(delta.x, area.xMax - from.x, ref enter, ref exit) && Clip(-delta.y, from.y - area.yMin, ref enter, ref exit) && Clip(delta.y, area.yMax - from.y, ref enter, ref exit); // 사각형 교차 계산
        } // 계산 종료
        private static bool Clip(float direction, float distance, ref float enter, ref float exit) // 선분 경계 계산
        { // 계산 시작
            if (Mathf.Abs(direction) < 0.000001f) // 평행 이동 확인
            { // 평행 처리 시작
                return distance >= 0f; // 경계 안쪽 확인
            } // 평행 처리 종료
            float ratio = distance / direction; // 경계 접촉 비율
            if (direction < 0f) // 진입 경계 확인
            { // 진입 처리 시작
                enter = Mathf.Max(enter, ratio); // 가장 늦은 진입 위치
            } // 진입 처리 종료
            else // 이탈 경계
            { // 이탈 처리 시작
                exit = Mathf.Min(exit, ratio); // 가장 빠른 이탈 위치
            } // 이탈 처리 종료
            return enter <= exit; // 겹친 구간 확인
        } // 계산 종료
        public static TrainingTarget Create(Vector2 position) // 임시 표적 생성
        { // 생성 시작
            GameObject body = new GameObject("TrainingTarget"); // 표적 객체
            body.transform.position = position; // 표적 위치
            SpriteRenderer image = body.AddComponent<SpriteRenderer>(); // 몸체 표시
            image.sprite = Projectile.GetSprite(); // 임시 이미지
            image.color = new Color(0.45f, 0.35f, 0.65f); // 몸체 색상
            body.transform.localScale = new Vector3(3.9f, 2.1f, 1f); // 시험 몸체 표시 크기 세 배
            GameObject mark = new GameObject("WeakPoint"); // 약점 표시 객체
            mark.transform.SetParent(body.transform, false); // 몸체 자식 연결
            mark.transform.localScale = new Vector3(0.32f / 3.9f, 0.7f / 2.1f, 1f); // 기존 약점 판정 표시 크기 유지
            SpriteRenderer weakImage = mark.AddComponent<SpriteRenderer>(); // 약점 표시
            weakImage.sprite = Projectile.GetSprite(); // 임시 이미지
            weakImage.color = new Color(1f, 0.75f, 0.2f); // 약점 구별 색상
            weakImage.sortingOrder = 2; // 몸체 앞 표시
            return body.AddComponent<TrainingTarget>(); // 표적 구성 요소 반환
        } // 생성 종료
    } // 클래스 종료
} // 영역 종료
