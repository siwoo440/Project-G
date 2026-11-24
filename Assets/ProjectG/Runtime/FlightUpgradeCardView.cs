using UnityEngine; // 빌드 가능한 선택 카드
using UnityEngine.UI; // 제작 그림과 클릭 영역

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightUpgradeCardView : MonoBehaviour // 씬에서 편집하는 3지선다 카드
    {
        [SerializeField] private Button button; // 버튼의 실제 선택 이벤트
        [SerializeField] private RawImage icon; // 효과 종류를 보여 주는 그림
        [SerializeField] private Text title; // 카드 이름
        [SerializeField] private Text details; // 실제 효과와 단계
        [SerializeField] private Text key; // 선택 키 표시

        internal Button Button => button; // 선택 화면의 클릭 연결

        internal void Set(Texture art, string name, string description, int number) // 조건에 맞아 나온 카드 표시
        {
            icon.texture = art; // 실제 종류별 제작 이미지
            title.text = name; // 구분 가능한 이름
            details.text = description; // 효과 수치와 교체 안내
            key.text = number.ToString(); // 번호 선택
        }
    }
}
