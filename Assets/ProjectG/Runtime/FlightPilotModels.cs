using UnityEngine; // 에디터에서 배치한 조종사 모델과 입체 동작

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 비행체의 조종사 표시 한 곳
    public sealed class FlightPilotModels : MonoBehaviour // 전투 조종사의 외형과 기울기 표시
    {
        [SerializeField] private Transform[] riders; // 조종사 순서의 씬 편집 가능 모델
        [SerializeField] private Transform[] mantlePivots; // 같은 순서의 옷자락 회전축
        private FlightWeapon weapon; // 실제 출격에서 선택한 조종사
        private FirstFlightController flight; // 실제 비행 속도의 원본
        private int shown = -1; // 현재 표시된 모델 배열 위치
        private float bank; // 현재 비행 기울기
        private float incline; // 현재 상승과 하강 자세

        private void Awake() // 프리팹의 부모에서 발사와 비행 참조
        {
            weapon = GetComponentInParent<FlightWeapon>(); // 실제 캐릭터 선택 공급원
            flight = GetComponentInParent<FirstFlightController>(); // 실제 조종과 물리 속도
        }

        private void LateUpdate() // 각 캐릭터의 모델 선택 및 비행 자세 적용
        {
            int pilot = (int)weapon.ActivePilot; // 전투와 화면이 공유하는 조종사 번호
            if (pilot != shown) // 출격 또는 연습의 캐릭터 전환
            {
                for (int i = 0; i < riders.Length; i++) // 배열의 모델을 하나만 표시
                {
                    riders[i].gameObject.SetActive(i == pilot); // 해당 조종사만 활성
                }
                shown = pilot; // 현재 모델의 번호 기록
            }

            Vector3 localSpeed = transform.InverseTransformDirection(flight.VisualVelocity); // 진행 방향 기준 좌우와 상하 속도
            float speedLimit = Mathf.Max(1f, flight.MaximumSpeed); // 장비별 속도 차이의 표시 정규화
            float blend = 1f - Mathf.Exp(-8f * Time.deltaTime); // 프레임 속도와 독립적인 회전 부드러움
            bank = Mathf.Lerp(bank, -17f * Mathf.Clamp(localSpeed.x / speedLimit, -1f, 1f), blend); // 좌우 애니메이션
            incline = Mathf.Lerp(incline, -9f * Mathf.Clamp(localSpeed.y / speedLimit, -1f, 1f), blend); // 고도 애니메이션
            riders[shown].localRotation = Quaternion.Euler(incline, 0f, bank); // 화면 모델에만 적용하는 기울기
            float flutter = Mathf.Sin(Time.time * 5f) * 4f; // 부유와 추진에서의 작은 옷자락 움직임
            mantlePivots[shown].localRotation = Quaternion.Euler(flutter, 0f, -bank * 0.28f); // 기체와 독립적인 옷자락 회전
        }
    }
}
