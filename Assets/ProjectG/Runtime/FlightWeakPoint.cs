using UnityEngine; // 편집 가능한 3D 약점 판정

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 피격체당 한 개의 약점 구분
    [RequireComponent(typeof(SphereCollider))] // Scene에서 조정할 구형 판정
    public sealed class FlightWeakPoint : MonoBehaviour // 마물의 노출된 약점
    {
        [SerializeField] private FlightEnemy enemy; // 같은 피격을 공유하는 몸통
        [SerializeField, Min(0f)] private float minimumDamageBonus; // 모든 캐릭터에 적용하는 약점의 최소 추가 피해

        internal FlightEnemy Enemy => enemy; // 약점의 실제 피해 대상
        internal float MinimumDamageBonus => minimumDamageBonus; // 약점의 공통 추가 피해

        internal void Configure(FlightEnemy owner, float bonus)
        {
            enemy = owner;
            minimumDamageBonus = Mathf.Max(0f, bonus);
        }
    }
}
