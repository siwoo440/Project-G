using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public abstract class CombatTarget : MonoBehaviour // 탄환 피해 대상
    { // 클래스 시작
        public abstract bool CanTakeDamage // 피해 가능 여부
        { // 속성 시작
            get; // 값 조회
        } // 속성 종료
        public abstract bool Intersects(Vector2 from, Vector2 to, out bool weak, float padding = 0f, float verticalPadding = 0f); // 탄환 이동 구간 판정
        public abstract bool IntersectsBeam(Rect beam, out bool weak); // 광선 범위 판정
        public abstract void ReceiveDamage(float damage, BattlePlayer source); // 피해와 공격자 전달
    } // 클래스 종료
} // 영역 종료
