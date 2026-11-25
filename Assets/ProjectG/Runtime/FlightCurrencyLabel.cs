using UnityEngine; // 씬에 배치한 아이콘과 수치 오브젝트
using UnityEngine.UI; // 편집 가능한 수치 텍스트

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCurrencyLabel : MonoBehaviour // 재화명 없이 아이콘 옆에 금액 표시
    {
        [SerializeField] private Text amount; // 아이콘 오른쪽의 숫자

        internal void Show(int? value, bool isReward = false) // 실제 금액 또는 비교할 수 없는 상태
        {
            gameObject.SetActive(value.HasValue); // 비용이나 보상이 없으면 같은 묶음을 숨김
            if (value.HasValue) // 정산과 비용에서만 편집 가능한 텍스트 변경
            {
                amount.text = isReward ? $"+{value.Value:N0}" : value.Value.ToString("N0"); // 보상은 더하기 기호, 보유량과 가격은 숫자
            }
        }
    }
}
