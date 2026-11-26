using System; // 접촉 거리 정렬
using System.Collections.Generic; // 관통과 폭발의 중복 표적 검사
using UnityEngine; // 실제 3D 충돌과 이동

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCraftedProjectile : MonoBehaviour // 제작 마법의 공용 비행체
    {
        [SerializeField] private Renderer image; // 에디터에서 편집하는 탄환 메시
        private readonly Collider[] nearby = new Collider[48]; // 근거리 유도와 폭발의 충돌 버퍼
        private readonly HashSet<Component> hits = new(); // 이 탄이 맞힌 직접 피해 대상
        private readonly HashSet<Component> splashHits = new(); // 폭발 한 번에 맞힌 대상
        private CraftedSpellRecipe recipe; // 캐스팅 시점의 검증된 조합
        private FlightCraftedSpellCaster.Profile profile; // 실제 자원과 투사체 시험 수치
        private LayerMask layers; // 이 장면의 충돌 범위
        private float remaining; // 남은 사거리
        private bool practice; // 시험과 실전의 독립된 시간
        private bool pierces; // 관통 선택 여부
        private FlightIrisCombat resonance; // 실제 체력을 바꿨는지 공명에 전달
        private FlightSeraCombat meleeSource; // 피격 시점의 세라와 적 사이 거리 계산
        private bool explodes; // 폭발 선택 여부
        private HashSet<Component> sharedHits; // 같은 시전의 공유 적중 기록
        private bool knocksBack; // 선택한 넉백 효과
        private int pierceLimit; // 시전 시 확정한 최대 관통 대상

        internal void Begin(CraftedSpellRecipe cast, FlightCraftedSpellCaster.Profile stats, LayerMask hitLayers, bool isPractice, HashSet<Component> castHits = null, int maxPierce = 3) // 장착 마법을 실제 탄환에 적용
        {
            recipe = cast; // 발사 중 편집에 영향 없는 원본
            profile = stats; // 한 발의 실제 이동과 피해
            layers = hitLayers; // 해당 장면에 허용된 충돌
            remaining = stats.Range; // 수명 대신 사용하는 최대 이동 거리
            practice = isPractice; // 연습에서만 일시정지와 독립
            sharedHits = castHits; // 같은 시전의 파동 적중 기록
            pierceLimit = maxPierce; // 이 탄의 관통 상한 고정
            knocksBack = cast.FirstEffect == "knockback" || cast.SecondEffect == "knockback"; // 넉백 선택 여부
            pierces = cast.FirstEffect == "pierce" || cast.SecondEffect == "pierce"; // 관통 효과의 한 발당 처리
            explodes = cast.FirstEffect == "explode" || cast.SecondEffect == "explode"; // 명중점 폭발
            MaterialPropertyBlock block = new MaterialPropertyBlock(); // 기존 공유 재질을 전환하지 않는 탄환 색
            block.SetColor("_BaseColor", ElementTint(cast.Element)); // 속성별 시각 구분
            image.SetPropertyBlock(block); // 이 개별 탄환에만 색 적용
            Vector3 silhouette = cast.Shape switch // 카드의 형상을 구분하는 시각 비율
            {
                "needle" or "arrow" => new Vector3(0.78f, 0.78f, 3f), // 가늘고 긴 침탄·화살
                "wave" => new Vector3(2.2f, 0.58f, 1f), // 옆으로 퍼지는 파동
                "small" => Vector3.one * 0.75f, // 작은 구체
                _ => Vector3.one // 실제 충돌 반경을 기준으로 한 구체
            };
            transform.localScale = silhouette * (stats.Radius * 2f); // 판정 수치와 구분한 메시 표시
        }

        internal void SetMeleeSource(FlightSeraCombat source) // 실전에서 세라가 발사한 직접 마법
        {
            meleeSource = source; // 제작 연습에 없고 실전 적중에 쓰는 참조
        }

        internal void SetResonance(FlightIrisCombat source) // 이리스가 실제 출격에서 발사한 직접 마법
        {
            resonance = source; // 같은 탄환에 붙은 공명의 조종사 참조
        }

        private void Update() // 탄환의 실제 비행과 충돌
        {
            float seconds = practice ? FlightCraftedSpellCaster.PracticeDelta : Time.deltaTime; // 전투 일시정지와 별개인 연습
            if (seconds <= 0f || recipe == null) // 아직 초기화 전 또는 전투 정지
            {
                return; // 충돌 범위를 감산하지 않는 프레임
            }
            if (recipe.Trajectory == "homing") // 현재 구성에 유도가 있을 때
            {
                Vector3 targetDirection = FindTargetDirection(); // 앞쪽의 가장 가까운 표적 방향
                Vector3 heading = CraftedSpellVectors.StepHeading(recipe, transform.forward, targetDirection, 120f, seconds); // 문서 조합의 유도 회전
                transform.rotation = Quaternion.LookRotation(heading); // 이 프레임의 탄환 경로
            }
            float step = Mathf.Min(remaining, profile.Speed * seconds); // 마지막 프레임이 사거리를 넘지 않는 계산
            RaycastHit[] contacts = Physics.SphereCastAll(transform.position, profile.Radius, transform.forward, step, layers, QueryTriggerInteraction.Ignore); // 지나간 범위의 실제 피격 검사
            Array.Sort(contacts, (left, right) => left.distance.CompareTo(right.distance)); // 지형과 적 사이의 접촉 순서
            foreach (RaycastHit contact in contacts) // 한 스텝에서 앞쪽부터 충돌 확인
            {
                Component target = Resolve(contact.collider); // 실전 마물이나 연습 표적
                if (target == null) // 지형 또는 다른 장애물
                {
                    Destroy(gameObject); // 환경 뒤의 적을 관통하지 않음
                    return; // 지형 이후 발사체 이동 중지
                }
                if (!hits.Add(target)) // 동일한 몸체·약점·프레임 접촉
                {
                    continue; // 관통 중 같은 대상의 중복 피해 방지
                }
                if (sharedHits != null && !sharedHits.Add(target)) // 같은 시전의 다른 파동에 이미 적중
                {
                    Destroy(gameObject); // 중복 피해 없이 접촉한 파동 종료
                    return; // 뒤쪽 대상으로 관통하지 않음
                }
                int previousHealth = target is FlightEnemy beforeHit ? beforeHit.Health : 0; // 실제 피해 전 체력
                Damage(target, profile.Damage); // 실제 표적 직격
                if (knocksBack && target is FlightEnemy enemy && enemy.Health > 0 && enemy.Health < previousHealth && enemy.LootTier == FlightLootTier.Normal && enemy.TryGetComponent(out FlightEnemyRoute route)) // 피해를 받은 일반 적의 경로 확인
                {
                    route.ApplyCraftedKnockback(transform.forward * 2f); // 일반 적의 기본 넉백 거리
                }
                if (explodes) // 충돌한 지점에서만 폭발
                {
                    Splash(contact.point); // 범위 피해는 별도 1회
                }
                if (!pierces || hits.Count >= pierceLimit) // 한 발의 강화된 관통 한도
                {
                    Destroy(gameObject); // 한 발 또는 세 번째 대상에서 종료
                    return; // 처리한 접촉 이후 이동 없음
                }
            }
            remaining -= step; // 갱신한 남은 사거리
            transform.position += transform.forward * step; // 실제 프레임 이동
            if (remaining <= 0f) // 한계 거리에서의 자연 소멸
            {
                Destroy(gameObject); // 명중하지 않은 발사체의 회수
            }
        }

        private Vector3 FindTargetDirection() // 앞쪽의 실제 피해 대상 탐색
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 14f, nearby, layers, QueryTriggerInteraction.Ignore); // 연습과 실전의 같은 유도 범위
            float distance = float.PositiveInfinity; // 현재 가장 가까운 거리
            Vector3 direction = Vector3.zero; // 표적이 없으면 직선 유지
            for (int i = 0; i < count; i++) // 유도 범위 안의 표적
            {
                Component target = Resolve(nearby[i]); // 지형과 피격 대상 구분
                if (target == null || hits.Contains(target)) // 지형 또는 이미 적중한 대상 제외
                {
                    continue; // 유도 대상 후보 아님
                }
                Vector3 offset = target.transform.position - transform.position; // 현재 탄환에서 표적까지의 방향
                if (Vector3.Dot(transform.forward, offset) > 0f && offset.sqrMagnitude < distance) // 뒤쪽 제외와 최단 거리 확인
                {
                    distance = offset.sqrMagnitude; // 가장 가까운 새 거리
                    direction = offset; // 해당 적이 있는 방향
                }
            }
            return direction; // 공통 유도 방향 계산 입력
        }

        private void Splash(Vector3 point) // 폭발 반경에 들어온 대상별 피해
        {
            splashHits.Clear(); // 직격과 별개로 세는 한 번의 폭발
            int count = Physics.OverlapSphereNonAlloc(point, 2.5f * profile.RadiusScale, nearby, layers, QueryTriggerInteraction.Ignore); // 파이어 볼트의 범위 시험값
            for (int i = 0; i < count; i++) // 같은 폭발 안의 실제 표적
            {
                Component target = Resolve(nearby[i]); // 같은 루트의 충돌체 통합
                if (target != null && splashHits.Add(target)) // 한 대상에게 폭발 피해 한 번
                {
                    Damage(target, 25f * profile.DamageScale); // 강화 시점의 파이어 볼트 폭발 피해량
                }
            }
        }

        private static Component Resolve(Collider shape) // 충돌체의 실제 피해 개체 찾기
        {
            if (shape.TryGetComponent(out FlightWeakPoint weak) && weak.Enemy != null) // 보스 약점은 실제 마물과 연결
            {
                return weak.Enemy; // 약점과 몸체가 공유하는 대상
            }
            if (shape.TryGetComponent(out FlightEnemy enemy)) // 실제 스테이지의 피해 대상
            {
                return enemy; // 마물의 원래 체력 경로
            }
            return shape.GetComponent<FlightCraftedTarget>(); // 제작 연습 중에만 존재하는 표적
        }

        private void Damage(Component target, float raw) // 실제 피해량과 속성 배율 전달
        {
            if (target is FlightEnemy enemy) // 실제 전투의 마물
            {
                float close = meleeSource != null ? meleeSource.DamageMultiplier(enemy.transform.position) : 1f; // 실제 적 중심과의 근거리 결의
                int before = enemy.Health; // 공명에서 확인할 실제 피격 전 체력
                enemy.ReceiveHit(Mathf.RoundToInt(raw * close * CraftedSpellRules.DamageMultiplier(recipe.Element, enemy.DefenseElement))); // 전투와 동일한 피해 상성
                if (enemy.Health < before) // 전환 무적과 차단 상태에서 공명 제외
                {
                    resonance?.Observe(enemy, false); // 실제로 한 번 피해를 입은 대상만 전달
                }
            }
            else if (target is FlightCraftedTarget practiceTarget) // 정산에 영향을 주지 않는 표적
            {
                practiceTarget.Receive(Mathf.RoundToInt(raw * CraftedSpellRules.DamageMultiplier(recipe.Element, practiceTarget.Element))); // 연습 피해 표시
            }
        }

        private static Color ElementTint(string element) // 기존 속성 구분의 탄환 색
        {
            return element switch // 5개 마법 속성
            {
                "fire" => new Color(1f, 0.4f, 0.2f), // 화염 주황색
                "water" => new Color(0.25f, 0.78f, 1f), // 물 하늘색
                "grass" => new Color(0.58f, 1f, 0.25f), // 풀 연두색
                "light" => new Color(1f, 0.9f, 0.5f), // 빛 금색
                _ => new Color(0.68f, 0.39f, 1f) // 어둠 보라색
            };
        }
    }
}
