using UnityEngine; // 표적 메시의 명중 반응

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightPracticeTarget : MonoBehaviour // 반복 사격용 시험 표적
    {
        [SerializeField] private Transform indicator; // 표시 모델의 실제 씬 참조
        private Vector3 restScale; // 표적 표시의 기본 크기
        private float hitUntil; // 명중 반응 종료 시점

        public int HitCount { get; private set; } // 현재 출격의 명중 수
        public int LastDamage { get; private set; } // 마지막 명중의 시험 피해

        private void Awake() // 표시 모델 초기화
        {
            restScale = indicator.localScale; // 기본 표시 크기 저장
        }

        public void ReceiveHit(int damage) // 실제 탄환 명중 처리
        {
            HitCount++; // 명중 횟수 누적
            LastDamage = damage; // 일반탄과 집중탄의 시험 피해 구분
            hitUntil = Time.time + 0.16f; // 짧은 시각 반응 예약
        }

        private void Update() // 표적의 명중 반응 갱신
        {
            float factor = Time.time < hitUntil ? 1.20f : 1f; // 명중 직후 크기 변화
            indicator.localScale = restScale * factor; // 충돌 범위와 구분된 모델 표시
        }
    }
}
