using System; // 접촉 거리 순서
using System.Collections.Generic; // 관통 대상 중복 방지
using UnityEngine; // 탄환 이동과 접촉

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightSpellBolt : MonoBehaviour // 아군 마법탄의 실제 3D 이동
    {
        [SerializeField, Min(0.01f)] private float speed = 40f; // 탄환의 월드 이동 속도
        [SerializeField, Min(0.01f)] private float radius = 0.12f; // 탄환의 충돌 반경
        [SerializeField, Min(1)] private int damage = 8; // 표적에 기록할 시험 피해
        private FlightRapidFire rapid; // 누적에 포함되는 자동탄 출처
        private HashSet<Component> piercedTargets; // 관통탄 한 발이 이미 맞힌 대상
        private bool usesWeakPoints; // 해당 발사의 약점 보너스
        private int effectiveDamage; // 발사 당시 피해량
        private float remaining; // 탄환의 남은 진행 거리
        private LayerMask collisionLayers; // 지형과 표적의 구분

        public void Begin(LayerMask hitLayers, float range, FlightRapidFire source, bool weakPoints = false, int piercingDamage = 0) // 탄환의 피해와 관통 초기화
        {
            collisionLayers = hitLayers; // 현재 전장의 충돌 대상
            remaining = range; // 발사 후 최대 진행 거리
            rapid = source; // 카드와 스킬의 적중 제외
            usesWeakPoints = weakPoints; // 현재 공격의 약점 적용 여부
            int baseDamage = piercingDamage > 0 ? piercingDamage : damage; // 고유탄 또는 기본 프리팹 피해
            effectiveDamage = rapid != null ? rapid.ScaleDamage(baseDamage) : baseDamage; // 발사 당시 피해
            piercedTargets = piercingDamage > 0 ? new HashSet<Component>() : null; // 관통탄에서만 맞힌 대상 기억
        }

        private void Update() // 한 프레임의 탄환 이동
        {
            float step = Mathf.Min(remaining, speed * Time.deltaTime); // 사거리 안에서 이동할 거리
            bool blocked = piercedTargets != null ? ApplyPiercingHits(step) : ApplyFirstHit(step); // 탄 종류에 맞는 접촉 경로
            if (blocked || remaining <= step) // 지형·표적에 막히거나 사거리 끝
            {
                Destroy(gameObject); // 탄환 수명 종료
                return; // 접촉 뒤 추가 이동 금지
            }

            transform.position += transform.forward * step; // 실제 월드 탄환 위치
            remaining -= step; // 남은 진행 거리 감소
        }

        private bool ApplyFirstHit(float step) // 기본·집중탄의 첫 접촉만 처리
        {
            if (!Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit, step, collisionLayers, QueryTriggerInteraction.Ignore)) // 프레임 사이의 접촉 검사
            {
                return false; // 탄환 계속 진행
            }

            Component target = FindTarget(hit.collider, out bool weakPoint); // 표적과 약점 구분
            if (target != null) // 피해를 받을 물체인지 확인
            {
                ApplyDamage(target, effectiveDamage, usesWeakPoints && weakPoint); // 출격 캐릭터의 선택적 약점 보너스
                rapid?.RecordHit(); // 아리아 기본·집중탄의 실제 명중
            }
            return true; // 첫 접촉으로 탄환 종료
        }

        private bool ApplyPiercingHits(float step) // Q 관통탄과 R 광선의 동일 순서
        {
            return ApplyPiercingPath(transform.position, radius, transform.forward, step, collisionLayers, piercedTargets, effectiveDamage, out _); // 이번 이동 범위 검사
        }

        internal static bool ApplyPiercingPath(Vector3 origin, float thickness, Vector3 heading, float length, LayerMask layers, HashSet<Component> targets, int damage, out float travel) // 거리순 관통과 지형 차단
        {
            travel = length; // 막힘이 없는 경우 전체 진행 거리
            RaycastHit[] hits = Physics.SphereCastAll(origin, thickness, heading, length, layers, QueryTriggerInteraction.Ignore); // 한 구간의 모든 접촉
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // 지형 너머의 적을 맞히지 않는 처리 순서
            foreach (RaycastHit hit in hits) // 첫 접촉부터 순서대로 확인
            {
                Component target = FindTarget(hit.collider, out bool weakPoint); // 몸통과 약점에 공통된 피해 대상
                if (target == null) // 지형이나 다른 충돌 물체
                {
                    travel = hit.distance; // 실제 차단 지점
                    return true; // 뒤쪽 접촉 처리 중단
                }
                if (targets.Add(target)) // 같은 적의 몸통·약점 중 첫 명중만 처리
                {
                    ApplyDamage(target, damage, weakPoint); // 첫 접촉 부위에 따른 피해
                }
            }
            return false; // 지나간 구간에 지형 없음
        }

        private static Component FindTarget(Collider collider, out bool weakPoint) // 충돌체에서 피해 대상 확인
        {
            weakPoint = collider.TryGetComponent<FlightWeakPoint>(out var marker) && marker.Enemy != null; // 유효한 약점 판정 여부
            if (weakPoint) // 별도 콜라이더가 연결된 마물
            {
                return marker.Enemy; // 몸통과 같은 피해 대상
            }
            if (collider.TryGetComponent<FlightEnemy>(out var enemy)) // 마물 몸통
            {
                return enemy; // 일반 피해 대상
            }
            return collider.GetComponent<FlightPracticeTarget>(); // 연습 표적 또는 지형
        }

        private static void ApplyDamage(Component target, int baseDamage, bool weakPoint) // 몸통과 약점의 피해량
        {
            int amount = weakPoint ? Mathf.RoundToInt(baseDamage * 1.5f) : baseDamage; // 리안의 약점 포착 50%
            if (target is FlightEnemy enemy) // 일반 마물
            {
                enemy.ReceiveHit(amount); // 실제 체력 갱신
            }
            else if (target is FlightPracticeTarget practice) // 기존 연습 표적
            {
                practice.ReceiveHit(amount); // 명중 수와 시험 피해 반영
            }
        }
    }
}
