using UnityEngine; // 씬 레이어 이동과 전투 시간

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCloudDrift : MonoBehaviour // 거리별 구름층의 느린 이동
    {
        [SerializeField] private Vector3 sweep = new Vector3(6f, -1f, 0f); // 씬 위치 기준으로 움직이는 거리
        [SerializeField, Min(0f)] private float rate = 0.2f; // 구름층의 이동 속도
        private Vector3 origin; // 씬에 배치한 원래 위치

        private void Awake() // 에디터에 저장한 레이어 기준 보관
        {
            origin = transform.localPosition; // 춤추는 구름층의 중심
        }

        private void Update() // 비행 중 배경에 완만한 움직임 적용
        {
            transform.localPosition = origin + sweep * Mathf.Sin(Time.time * rate); // 재배치 없이 반복되는 구름의 이동
        }
    }
}
