using UnityEngine; // 적중 비율과 전투 시간

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightRapidFire : MonoBehaviour // 아리아의 연속 적중 패시브
    {
        [SerializeField, Min(0.01f)] private float increase = 1f; // 적중당 피해 퍼센트포인트
        [SerializeField, Min(0.01f)] private float limit = 50f; // 피해 보너스 상한
        [SerializeField, Min(0f)] private float hold = 2f; // 마지막 적중 뒤 유지 시간
        [SerializeField, Min(0f)] private float decay = 2.5f; // 유지 뒤 초당 감소
        private float lastHit = float.NegativeInfinity; // 최근 유효 적중 시각

        public float BonusPercent { get; private set; } // 현재 피해 보너스
        public bool IsDecaying => BonusPercent > 0f && Time.time > lastHit + hold; // 감소 상태

        public void RecordHit() // 실제 기본·집중탄의 적중
        {
            BonusPercent = Mathf.Min(limit, BonusPercent + increase); // 유효 적중과 상한
            lastHit = Time.time; // 현재 화력 유지 시간 갱신
        }

        public int ScaleDamage(int baseDamage) // 발사 시점의 피해량
        {
            return Mathf.RoundToInt(baseDamage * (1f + BonusPercent * 0.01f)); // 정수 체력 기준 피해
        }

        public void ResetRun() // 출격 시 보너스 초기화
        {
            BonusPercent = 0f; // 이전 보너스 제거
            lastHit = float.NegativeInfinity; // 최근 사격 시간 제거
        }

        private void Update() // 보너스 유지와 감쇠
        {
            if (IsDecaying) // 미적중 유예 종료
            {
                BonusPercent = Mathf.Max(0f, BonusPercent - decay * Time.deltaTime); // 천천히 줄어드는 피해 보너스
            }
        }
    }
}
