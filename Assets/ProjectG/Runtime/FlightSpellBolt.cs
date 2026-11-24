using UnityEngine; // 탄환 이동과 접촉

namespace ProjectG // 프로젝트 게임 영역
{ // 영역 시작
    public sealed class FlightSpellBolt : MonoBehaviour // 아군 마법탄의 실제 3D 이동
    { // 발사체 시작
        [SerializeField, Min(0.01f)] private float speed = 40f; // 탄환의 월드 이동 속도
        [SerializeField, Min(0.01f)] private float radius = 0.12f; // 탄환의 충돌 반경
        [SerializeField, Min(1)] private int damage = 8; // 표적에 기록할 시험 피해
        private float remaining; // 탄환의 남은 진행 거리
        private LayerMask collisionLayers; // 지형과 표적의 구분

        public void Begin(LayerMask hitLayers, float range) // 발사체 초기화
        { // 초기화 시작
            collisionLayers = hitLayers; // 현재 전장의 충돌 대상
            remaining = range; // 발사 후 최대 진행 거리
        } // 초기화 종료

        private void Update() // 한 프레임의 탄환 이동
        { // 이동 시작
            float step = Mathf.Min(remaining, speed * Time.deltaTime); // 사거리 안에서 이동할 거리
            if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit, step, collisionLayers, QueryTriggerInteraction.Ignore)) // 프레임 사이의 표적과 지형 검사
            { // 접촉 시작
                if (hit.collider.TryGetComponent<FlightPracticeTarget>(out var target)) // 시험 표적 접촉
                { // 표적 타격 시작
                    target.ReceiveHit(damage); // 해당 탄환의 실제 피해 기록
                } // 표적 타격 종료
                Destroy(gameObject); // 표적 또는 지형 접촉 후 종료
                return; // 같은 탄환의 중복 처리 차단
            } // 접촉 종료

            transform.position += transform.forward * step; // 실제 월드 탄환 위치
            remaining -= step; // 남은 진행 거리 감소
            if (remaining <= 0f) // 최대 진행 거리 도달
            { // 만료 시작
                Destroy(gameObject); // 화면 밖 탄환 정리
            } // 만료 종료
        } // 이동 종료
    } // 발사체 종료
} // 영역 종료
