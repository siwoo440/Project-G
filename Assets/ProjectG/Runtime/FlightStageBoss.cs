using UnityEngine; // 씬에 등록할 보스와 월드 좌표

namespace ProjectG // 프로젝트 게임 영역
{
    [RequireComponent(typeof(FlightEnemy))] // 일반 적과 공유하는 체력·처치 보상
    public abstract class FlightStageBoss : MonoBehaviour // 스테이지의 중간·최종 보스 공통 시작점
    {
        public FlightEnemy Enemy => GetComponent<FlightEnemy>(); // 보스의 실제 체력과 이벤트

        internal abstract void Begin(FlightThreatPool pool, Transform target); // 시간표에서 보스를 시작하는 경로
    }
}
