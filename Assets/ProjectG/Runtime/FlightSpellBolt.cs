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
        private FlightSeraCombat meleeSource; // 근거리 결의를 쓰는 발사의 조종사
        private FlightEnemy guidedTarget; // 유도탄만 추적하는 실제 표적
        private float guideRadians; // 초당 회전 거리의 라디안 값
        private FlightRapidFire rapid; // 누적에 포함되는 자동탄 출처
        private HashSet<Component> piercedTargets; // 관통탄 한 발이 이미 맞힌 대상
        private float weakRatio; // 발사 당시의 약점 피해 보너스
        private float castRadius; // 강화에 따라 변경된 충돌 두께
        private int maxHits; // 이번 탄이 맞힐 수 있는 최대 대상 수
        private int effectiveDamage; // 발사 당시 피해량
        private float remaining; // 탄환의 남은 진행 거리
        private LayerMask collisionLayers; // 지형과 표적의 구분
        private Action<FlightEnemy, bool> observeHit; // 유효한 체력 피해와 약점 여부 관찰

        public void Begin(LayerMask hitLayers, float range, FlightRapidFire source, float weakBonus, int piercingDamage = 0, float damageScale = 1f, float widthScale = 1f, int extraPierces = 0, int assistDamage = -1) // 피해와 관통 초기화
        {
            collisionLayers = hitLayers; // 현재 전장의 충돌 대상
            remaining = range; // 발사 후 최대 진행 거리
            rapid = source; // 카드와 스킬의 적중 제외
            weakRatio = weakBonus; // 공격마다 확정한 약점 배율
            castRadius = radius * widthScale; // 실제 강화 후 투사체 판정 폭
            int baseDamage = piercingDamage > 0 ? piercingDamage : (assistDamage >= 0 ? assistDamage : damage); // 고유탄·0을 포함한 보조탄·기본 피해
            float stacked = baseDamage * damageScale; // 강화 효과를 먼저 적용한 피해
            effectiveDamage = rapid != null ? rapid.ScaleDamage(stacked) : Mathf.RoundToInt(stacked); // 속사는 별도 반영
            maxHits = piercingDamage > 0 ? int.MaxValue : 1 + extraPierces; // 고유 관통과 일반탄 관통의 상한
            piercedTargets = maxHits > 1 ? new HashSet<Component>() : null; // 관통탄에서만 맞힌 대상 기억
            observeHit = null; // 이전 발사의 기록 콜백 제거
            guidedTarget = null; // 새 발사에는 기본 유도 표적 없음
            meleeSource = null; // 일반 투사체의 기본 피해 규칙
        }

        internal void Observe(Action<FlightEnemy, bool> onHit) // 게임 과제가 있는 실제 사격에만 등록
        {
            observeHit = onHit; // 이 발사의 실제 적중 종류
        }

        internal void SetMeleeSource(FlightSeraCombat source) // 세라 발사체에만 실제 근거리 효과 연결
        {
            meleeSource = source; // 충돌 때 플레이어와 적의 현재 간격으로 계산
        }

        internal void Guide(FlightEnemy target, float radiansPerSecond) // 별빛 탄환의 입체 유도 설정
        {
            guidedTarget = target; // 이 발사에 지정한 표적
            guideRadians = radiansPerSecond; // 캐릭터와 강화에 맞춘 회전 속도
        }

        private void Update() // 한 프레임의 탄환 이동
        {
            if (Time.timeScale == 0f) // 카드 선택 중 탄환 판정 정지
            {
                return; // 실제 충돌이 일어나지 않는 프레임
            }
            if (guidedTarget != null && guidedTarget.isActiveAndEnabled && guidedTarget.Health > 0) // 사라진 표적은 마지막 방향으로 통과
            {
                Vector3 route = guidedTarget.transform.position - transform.position; // 높이를 포함한 실제 충돌 지점 방향
                if (route.sqrMagnitude > 0.001f) // 같은 위치에서 회전을 정할 수 없는 경우 제외
                {
                    Vector3 heading = Vector3.RotateTowards(transform.forward, route.normalized, guideRadians * Time.deltaTime, 0f); // 급회전의 실제 한계
                    transform.rotation = Quaternion.LookRotation(heading); // 콜라이더 검사 전에 방향 반영
                }
            }
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
            if (!Physics.SphereCast(transform.position, castRadius, transform.forward, out RaycastHit hit, step, collisionLayers, QueryTriggerInteraction.Ignore)) // 프레임 사이의 접촉 검사
            {
                return false; // 탄환 계속 진행
            }

            Component target = FindTarget(hit.collider, out FlightWeakPoint weakPoint); // 표적과 약점 구분
            if (target != null) // 피해를 받을 물체인지 확인
            {
                ApplyDamage(target, effectiveDamage, weakPoint, weakRatio, rapid, observeHit, meleeSource); // 판정된 피해와 실제 출격 적중
            }
            return true; // 첫 접촉으로 탄환 종료
        }

        private bool ApplyPiercingHits(float step) // Q 관통탄과 R 광선의 동일 순서
        {
            return ApplyPiercingPath(transform.position, castRadius, transform.forward, step, collisionLayers, piercedTargets, effectiveDamage, weakRatio, maxHits, rapid, out _, observeHit, meleeSource); // 실제 대상마다 한 번 관통 적중
        }

        internal static bool ApplyPiercingPath(Vector3 origin, float thickness, Vector3 heading, float length, LayerMask layers, HashSet<Component> targets, int damage, float weakBonus, int maximumHits, FlightRapidFire rapid, out float travel, Action<FlightEnemy, bool> onHit = null, FlightSeraCombat melee = null) // 거리순 관통, 지형, 실제 약점 피해
        {
            travel = length; // 막힘이 없는 경우 전체 진행 거리
            RaycastHit[] hits = Physics.SphereCastAll(origin, thickness, heading, length, layers, QueryTriggerInteraction.Ignore); // 한 구간의 모든 접촉
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // 지형 너머의 적을 맞히지 않는 처리 순서
            foreach (RaycastHit hit in hits) // 첫 접촉부터 순서대로 확인
            {
                Component target = FindTarget(hit.collider, out FlightWeakPoint weakPoint); // 몸통과 약점에 공통된 피해 대상
                if (target == null) // 지형이나 다른 충돌 물체
                {
                    travel = hit.distance; // 실제 차단 지점
                    return true; // 뒤쪽 접촉 처리 중단
                }
                if (targets.Add(target)) // 같은 적의 몸통·약점 중 첫 명중만 처리
                {
                    ApplyDamage(target, damage, weakPoint, weakBonus, rapid, onHit, melee); // 몸통·약점 피해와 같은 대상 재적중 방지
                    if (targets.Count >= maximumHits) // 해당 탄의 관통 한계
                    {
                        travel = hit.distance; // 마지막 적 접촉 위치
                        return true; // 앞에 남은 다른 표적 보호
                    }
                }
            }
            return false; // 지나간 구간에 지형 없음
        }

        private static Component FindTarget(Collider collider, out FlightWeakPoint weakPoint) // 충돌체에서 피해 대상 확인
        {
            if (collider.TryGetComponent(out weakPoint) && weakPoint.Enemy != null) // 실제 피해 대상과 연결된 약점
            {
                return weakPoint.Enemy; // 몸통과 같은 피해 대상
            }
            weakPoint = null; // 일반 충돌에서 약점 보너스 제거
            if (collider.TryGetComponent<FlightEnemy>(out var enemy)) // 마물 몸통
            {
                return enemy; // 일반 피해 대상
            }
            return collider.GetComponent<FlightPracticeTarget>(); // 연습 표적 또는 지형
        }

        private static void ApplyDamage(Component target, int baseDamage, FlightWeakPoint weakPoint, float characterBonus, FlightRapidFire rapidFire, Action<FlightEnemy, bool> onHit, FlightSeraCombat melee) // 실제 피해와 변화한 체력 관찰
        {
            float bonus = weakPoint == null ? 0f : Mathf.Max(weakPoint.MinimumDamageBonus, characterBonus); // 보스 공통 보너스와 캐릭터 보너스 중 큰 값
            float close = melee != null ? melee.DamageMultiplier(target.transform.position) : 1f; // 피격 순간 근접 거리 적용
            int amount = Mathf.RoundToInt(baseDamage * (1f + bonus) * close); // 실제 판정에 반영할 피해량
            if (target is FlightEnemy enemy) // 일반 마물
            {
                int prior = enemy.Health; // 무적·페이즈 잠금 전의 체력
                enemy.ReceiveHit(amount); // 실제 체력 갱신
                if (enemy.Health < prior) // 무적과 제한으로 변화가 없으면 유효 적중에서 제외
                {
                    rapidFire?.RecordHit(); // 실제 피해가 발생한 속사 누적
                    onHit?.Invoke(enemy, weakPoint != null); // 마물의 실제 유효 적중만 성장에 전달
                }
            }
            else if (target is FlightPracticeTarget practice) // 기존 연습 표적
            {
                practice.ReceiveHit(amount); // 명중 수와 시험 피해 반영
                rapidFire?.RecordHit(); // 일반 시험 표적의 기존 사격 누적
            }
        }
    }
}
