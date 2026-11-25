using UnityEngine; // 제작 연습 장치의 씬 참조

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCraftedTarget : MonoBehaviour // 실제 피격 수치를 보여 주는 마법 연습 표적
    {
        [SerializeField] private string element; // 시험 대상을 위한 선택형 방어 속성
        [SerializeField] private Transform indicator; // 명중 순간의 외형 반응
        private Vector3 restScale; // 프리팹의 기본 표적 크기
        private float signal; // 연습 화면에서 움직이는 강조 시간

        public int TotalDamage { get; private set; } // 정산과 무관한 연습 누적 피해
        public string Element => element; // 미지정이면 중립 배율

        private void Awake() // 편집한 표적의 원래 크기 기록
        {
            restScale = indicator.localScale; // 명중 효과 전에 유지할 크기
        }

        internal void Receive(int damage) // 공용 발사기 명중의 피해
        {
            TotalDamage += damage; // 별도 결과 표시를 위한 연습 기록
            signal = 0.18f; // 실제 명중 순간의 확대
        }

        internal void ResetDamage() // 연습 모드 초기화
        {
            TotalDamage = 0; // 화면의 누적 피해 초기화
            signal = 0f; // 이전 명중 반응 제거
        }

        private void Update() // 전투가 정지해도 명중 반응 표시
        {
            signal = Mathf.Max(0f, signal - FlightCraftedSpellCaster.PracticeDelta); // 실제 연습 프레임의 반응 길이
            indicator.localScale = restScale * (1f + signal); // 충돌은 유지하고 외형만 강조
        }
    }
}
