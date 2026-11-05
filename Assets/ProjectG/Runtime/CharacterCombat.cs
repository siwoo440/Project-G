using UnityEngine; // 유니티 수학 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum CharacterType // 캐릭터 종류
    { // 종류 시작
        Rapid, // 속사형
        Sniper // 저격형
    } // 종류 종료
    public sealed class RapidCombo // 속사형 누적 화력
    { // 클래스 시작
        private float quietTime; // 마지막 적중 후 시간
        public float GracePeriod = 2f; // 누적 보너스 유지 시간
        public float MaximumBonus = 0.5f; // 누적 보너스 상한
        public float Bonus // 피해 증가 비율
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public void RegisterHit() // 기본 공격 적중
        { // 적중 시작
            Bonus = Mathf.Min(MaximumBonus, Bonus + 0.01f); // 적중당 증가와 상한
            quietTime = 0f; // 적중 간격 초기화
        } // 적중 종료
        public void Advance(float elapsed) // 누적 시간 진행
        { // 진행 시작
            float duration = Mathf.Max(0f, elapsed); // 경과 시간 보정
            float decay = Mathf.Max(0f, quietTime + duration - GracePeriod) - Mathf.Max(0f, quietTime - GracePeriod); // 유지 이후 시간
            quietTime += duration; // 마지막 적중 경과
            Bonus = Mathf.Max(0f, Bonus - decay * 0.025f); // 초당 감소와 하한
        } // 진행 종료
    } // 클래스 종료
} // 영역 종료
