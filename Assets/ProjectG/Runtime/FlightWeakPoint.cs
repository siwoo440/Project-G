using UnityEngine; // 편집 가능한 3D 약점 판정

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 피격체당 한 개의 약점 구분
    [RequireComponent(typeof(SphereCollider))] // Scene에서 조정할 구형 판정
    public sealed class FlightWeakPoint : MonoBehaviour // 마물의 노출된 약점
    {
        [SerializeField] private FlightEnemy enemy; // 같은 피격을 공유하는 몸통

        internal FlightEnemy Enemy => enemy; // 약점의 실제 피해 대상
    }
}
