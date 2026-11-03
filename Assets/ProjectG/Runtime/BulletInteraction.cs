using UnityEngine; // 벡터 자료형
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum BulletContact // 적탄 접촉 결과
    { // 결과 시작
        None, // 접촉 없음
        Graze, // 그레이즈
        Hit, // 피해 적용
        BlockedHit // 무적 중 접촉
    } // 결과 종료
    public sealed class BulletInteraction // 적탄별 판정 상태
    { // 클래스 시작
        public bool Grazed // 그레이즈 처리 여부
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public void Reset() // 재사용 초기화
        { // 초기화 시작
            Grazed = false; // 그레이즈 상태 초기화
        } // 초기화 종료
        public BulletContact Resolve(Vector2 playerFrom, Vector2 playerTo, Vector2 bulletFrom, Vector2 bulletTo, float bulletRadius, int damage, PlayerSurvival player) // 적탄 접촉 판정
        { // 판정 시작
            if (player == null || !player.IsAlive) // 생존 상태 확인
            { // 판정 생략 시작
                return BulletContact.None; // 판정 생략
            } // 판정 생략 종료
            Vector2 relativeFrom = bulletFrom - playerFrom; // 이전 상대 위치
            Vector2 relativeTo = bulletTo - playerTo; // 현재 상대 위치
            float radius = Mathf.Max(0f, bulletRadius); // 탄환 반경 보정
            if (CollisionMath.Intersects(Vector2.zero, relativeFrom, relativeTo, 0.12f + radius)) // 피격 범위 확인
            { // 피격 판정 시작
                return player.TryHit(damage) ? BulletContact.Hit : BulletContact.BlockedHit; // 피해 또는 무적 접촉
            } // 피격 판정 종료
            if (!Grazed && CollisionMath.Intersects(Vector2.zero, relativeFrom, relativeTo, 0.40f + radius)) // 첫 그레이즈 확인
            { // 그레이즈 시작
                Grazed = true; // 중복 방지 표시
                return BulletContact.Graze; // 그레이즈 처리
            } // 그레이즈 종료
            return BulletContact.None; // 접촉 없음
        } // 판정 종료
    } // 클래스 종료
} // 영역 종료
