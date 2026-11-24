using UnityEngine; // 탄환 이동과 접촉

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightSpellBolt : MonoBehaviour // 아군 마법탄의 실제 3D 이동
    {
        [SerializeField, Min(0.01f)] private float speed = 40f; // 탄환의 월드 이동 속도
        [SerializeField, Min(0.01f)] private float radius = 0.12f; // 탄환의 충돌 반경
        [SerializeField, Min(1)] private int damage = 8; // 표적에 기록할 시험 피해
        private FlightRapidFire rapid; // 누적에 포함되는 자동탄 출처
        private int effectiveDamage; // 발사 당시 피해량
        private float remaining; // 탄환의 남은 진행 거리
        private LayerMask collisionLayers; // 지형과 표적의 구분

        public void Begin(LayerMask hitLayers, float range, FlightRapidFire source) // 발사체 초기화
        {
            collisionLayers = hitLayers; // 현재 전장의 충돌 대상
            remaining = range; // 발사 후 최대 진행 거리
            rapid = source; // 카드와 스킬의 적중 제외
            effectiveDamage = rapid != null ? rapid.ScaleDamage(damage) : damage; // 발사 당시 피해
        }

        private void Update() // 한 프레임의 탄환 이동
        {
            float step = Mathf.Min(remaining, speed * Time.deltaTime); // 사거리 안에서 이동할 거리
            if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit, step, collisionLayers, QueryTriggerInteraction.Ignore)) // 프레임 사이의 표적과 지형 검사
            {
                if (hit.collider.TryGetComponent<FlightEnemy>(out var enemy)) // 실제 일반 마물 충돌
                {
                    enemy.ReceiveHit(effectiveDamage); // 실제 마물 체력 갱신
                    rapid?.RecordHit(); // 기본·집중 자동탄 적중
                }
                else if (hit.collider.TryGetComponent<FlightPracticeTarget>(out var target)) // 기존 시험 표적 접촉
                {
                    target.ReceiveHit(effectiveDamage); // 해당 탄환의 실제 피해 기록
                    rapid?.RecordHit(); // 훈련 표적의 실제 적중
                }
                Destroy(gameObject); // 표적 또는 지형 접촉 후 종료
                return; // 같은 탄환의 중복 처리 차단
            }

            transform.position += transform.forward * step; // 실제 월드 탄환 위치
            remaining -= step; // 남은 진행 거리 감소
            if (remaining <= 0f) // 최대 진행 거리 도달
            {
                Destroy(gameObject); // 화면 밖 탄환 정리
            }
        }
    }
}
