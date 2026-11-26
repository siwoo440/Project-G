using System.Collections.Generic; // 강화 대상과 드롭 풀
using UnityEngine; // 3D 낙하·획득·장비 공격
using UnityEngine.UI; // 실제 획득한 강화 안내

namespace ProjectG // 프로젝트 게임 영역
{
    public enum FlightLootTier // 적 처치의 드롭 규칙
    {
        Normal, MidBoss, FinalBoss // 시험 씬과 앞으로의 보스 설정
    }

    internal enum FlightEnhancementKind // 등급별 아이템 종류
    {
        Damage, Cadence, Pierce, Cycle, Shield, Graze, Heal, Energy, Magnet // 전투 도감의 순서
    }

    [DefaultExecutionOrder(116)] // 경험치 드롭 위치 확인 뒤 강화 아이템 처리
    public sealed class FlightEnhancements : MonoBehaviour // 등급별 강화 드롭과 보조 장비
    {
        private struct Drop // 현재 필드의 강화 아이템
        {
            public Transform view; // 편집 가능한 월드 프리팹
            public FlightEnhancementKind kind; // 획득할 효과
            public int grade; // I부터 V까지
            public float remaining; // 획득할 수 있는 잔여 시간
            public int born; // 처음 드롭한 프레임
        }

        private static readonly int[] GradeWeights = { 50, 25, 15, 7, 3 }; // 드롭 등급 확률
        private static readonly string[] Grades = { "I", "II", "III", "IV", "V" }; // 획득 HUD 등급
        private static readonly string[] Names = { "화력", "연사", "관통", "기술 순환", "방어막", "근접 충전", "회복 물약", "에너지 결정", "회수 자석" }; // 기획의 아이템 이름
        private static readonly float[][] Effects = // I~V 효과 수치
        {
            new[] { 0.10f, 0.12f, 0.14f, 0.16f, 0.18f }, // 화력
            new[] { 0.08f, 0.10f, 0.12f, 0.14f, 0.16f }, // 연사
            new[] { 1f, 1f, 2f, 2f, 3f }, // 관통
            new[] { 0.06f, 0.10f, 0.12f, 0.14f, 0.16f }, // 기술 순환
            new[] { 1f, 2f, 3f, 4f, 5f }, // 방어 횟수
            new[] { 0.10f, 0.15f, 0.20f, 0.25f, 0.30f }, // 근접 충전
            new[] { 10f, 15f, 20f, 25f, 30f }, // 회복
            new[] { 8f, 12f, 16f, 20f, 25f }, // 필살기 에너지
            new[] { 2f, 4f, 6f, 8f, float.PositiveInfinity } // 경험치 회수
        };
        private static readonly Color[] Tints = // 색으로 구분하는 시험 등급
        {
            new Color(0.8f, 0.86f, 0.91f), new Color(0.29f, 0.88f, 0.53f), // I·II
            new Color(0.3f, 0.66f, 1f), new Color(0.77f, 0.45f, 1f), new Color(1f, 0.77f, 0.24f) // III·IV·V
        };
        internal const float DamageLimit = 0.60f; // 카드·아이템 합산 피해
        internal const float CadenceLimit = 0.40f; // 카드·아이템 합산 연사
        internal const int PierceLimit = 3; // 카드·아이템 합산 관통
        internal const float CooldownLimit = 0.40f; // 카드·아이템 합산 재사용
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); // URP 등급 색

        [SerializeField] private FlightEnemy[] sources; // 출격 씬의 적·보스 처치
        [SerializeField] private FlightPilotVitals pilot; // 강화 획득 위치와 보호막
        [SerializeField] private FlightExperience experience; // 회수 자석으로 모을 조각
        [SerializeField] private FlightUpgradeChoices cards; // 공유 상한과 보조 장비
        [SerializeField] private FlightSkillController skills; // 필살기 에너지
        [SerializeField] private FlightWeapon weapon; // 드론이 발사할 기존 마법탄
        [SerializeField] private Transform template; // 등급 색이 적용되는 월드 프리팹
        [SerializeField] private Transform drone; // 무다리 프로펠러 보조체
        [SerializeField] private Transform propeller; // 드론의 위쪽 프로펠러
        [SerializeField] private Transform blades; // 회전하는 보조 칼날
        [SerializeField] private Text pickupLabel; // 실제 획득 또는 장착 확인
        [SerializeField, Range(0f, 1f)] private float normalChance = 0.10f; // 일반 적 10%
        [SerializeField, Min(0.01f)] private float pickupRadius = 0.65f; // 획득 합산 반경
        [SerializeField, Min(0.01f)] private float life = 20f; // 20초 드롭 유지
        private readonly List<Drop> active = new List<Drop>(); // 살아 있는 강화 아이템
        private readonly Stack<Transform> idle = new Stack<Transform>(); // 회수 후 재사용
        private readonly float[] bonuses = new float[Names.Length]; // 실제로 쌓인 아이템 수치
        private readonly int[] counts = new int[Names.Length]; // 종류별 획득 횟수
        private readonly int[] validGradeCounts = new int[GradeWeights.Length]; // 현재 효과가 있는 등급 후보 수
        private MaterialPropertyBlock tint; // 사용한 프리팹 재질을 공유하는 등급 색
        private Vector3 previousPilot; // 빠르게 접근하는 기체의 이전 위치
        private float announceUntil; // 최근 획득 안내 종료
        private float nextDrone; // 드론 다음 공격 시간
        private float nextBlade; // 칼날 다음 공격 시간

        public int ActiveCount => active.Count; // 실제로 획득 가능한 아이템 수
        public string LastPickup { get; private set; } = string.Empty; // 최근 강화 이름과 실제 등급
        public float GrazeScale => 1f + Stored(FlightEnhancementKind.Graze); // 소수 단위 근접 충전

        private void Awake() // 풀의 공통 설정
        {
            tint = new MaterialPropertyBlock(); // 등급 색을 인스턴스에만 적용
            previousPilot = pilot.transform.position; // 첫 프레임의 기체 위치
        }

        private void OnEnable() // 실제 적 처치와 강화 연결
        {
            foreach (FlightEnemy source in sources) // 씬에 배치한 드롭 출처
            {
                source.Defeated += OnDefeated; // 실제 적 사망 경로에 연결
            }
        }

        private void OnDisable() // 출격 종료 시 적의 참조 반환
        {
            foreach (FlightEnemy source in sources) // 다음 씬에 연결을 남기지 않음
            {
                if (source != null) // 아직 씬 객체가 남아 있는 경우
                {
                    source.Defeated -= OnDefeated; // 등록한 사망 이벤트 해제
                }
            }
        }

        internal float Stored(FlightEnhancementKind kind) // 카드에서 더할 실제 강화량
        {
            return bonuses[(int)kind]; // 공통 상한 이전에 정산한 값
        }

        public void ResetRun() // 재출격할 때 모든 임시 장비 정리
        {
            for (int index = active.Count - 1; index >= 0; index--) // 월드의 남은 강화 아이템
            {
                Recycle(index); // 풀로 정상 회수
            }
            System.Array.Clear(bonuses, 0, bonuses.Length); // 이전 강화 효과 삭제
            System.Array.Clear(counts, 0, counts.Length); // 이전 획득 이력 삭제
            drone.gameObject.SetActive(false); // 드론 슬롯 정리
            blades.gameObject.SetActive(false); // 칼날 슬롯 정리
            nextDrone = 0f; // 새 출격의 최초 발사
            nextBlade = 0f; // 새 출격의 최초 근접 판정
            LastPickup = string.Empty; // 이전 획득 기록 삭제
            announceUntil = 0f; // 이전 화면 안내 제거
            previousPilot = pilot.transform.position; // 초기 이동에 이전 궤적 미포함
            pickupLabel.text = string.Empty; // 전투의 이전 획득 표시 삭제
        }

        private void OnDefeated(FlightEnemy enemy) // 등급을 고르고 실제 드롭 생성
        {
            if (storyNoRewards.Contains(enemy)) return;
            if (pilot.IsDefeated || enemy.LootTier == FlightLootTier.FinalBoss || (enemy.LootTier == FlightLootTier.Normal && Random.value >= normalChance)) // 일반 확률과 보스 차이
            {
                return; // 최종 보스·실패·미당첨에서 드롭하지 않음
            }
            Vector3 position = enemy.transform.position + Vector3.up * 0.65f; // 처치 위치에서 보이는 강화 보석
            int weight = 0; // 효과가 있는 등급만 더할 합계
            for (int grade = 0; grade < GradeWeights.Length; grade++) // I부터 V까지 적합 후보 수 계산
            {
                int count = 0; // 이번 등급의 유효 아이템
                for (int index = 0; index < Names.Length; index++) // 여러 종류의 독립 판정
                {
                    if (CanApply((FlightEnhancementKind)index, grade + 1, position)) // 올릴 수 있는 실제 강화
                    {
                        count++; // 등급 안의 후보
                    }
                }
                validGradeCounts[grade] = count; // 등급에서 선택 가능한 크기
                if (count > 0) // 최소 한 개의 유효 효과
                {
                    weight += GradeWeights[grade]; // 남은 등급의 재정규화
                }
            }
            if (weight == 0) // 보스라도 모든 강화가 포화
            {
                return; // 무효 보상 생성을 방지
            }
            int selection = Random.Range(0, weight); // 유효 등급 샘플
            for (int grade = 0; grade < GradeWeights.Length; grade++) // 표에 적힌 등급 차례
            {
                if (validGradeCounts[grade] == 0) // 효과 없는 등급 제외
                {
                    continue; // 다음 가중치
                }
                selection -= GradeWeights[grade]; // 선택 값의 등급 범위 검사
                if (selection < 0) // 해당 등급 선택
                {
                    SpawnGrade(position, grade + 1, Random.Range(0, validGradeCounts[grade])); // 등급 내에서 균등 선택
                    return; // 처치당 한 개만 지급
                }
            }
        }

        private readonly System.Collections.Generic.HashSet<FlightEnemy> storyNoRewards = new System.Collections.Generic.HashSet<FlightEnemy>();

        internal void RegisterStorySource(FlightEnemy enemy) => RegisterStoryTarget(enemy, true);

        internal void RegisterStoryTarget(FlightEnemy enemy, bool allowReward)
        {
            if (enemy == null || System.Array.IndexOf(sources, enemy) >= 0) return;
            System.Array.Resize(ref sources, sources.Length + 1);
            sources[sources.Length - 1] = enemy;
            if (!allowReward) storyNoRewards.Add(enemy);
            else if (isActiveAndEnabled) enemy.Defeated += OnDefeated;
        }

        internal void UnregisterStorySource(FlightEnemy enemy)
        {
            if (enemy != null) enemy.Defeated -= OnDefeated;
            storyNoRewards.Remove(enemy);
            sources = System.Array.FindAll(sources, value => value != null && value != enemy);
        }

        private void SpawnGrade(Vector3 position, int grade, int candidate) // 선택한 등급 안의 실제 유효한 종류
        {
            for (int index = 0; index < Names.Length; index++) // 기획 표의 아이템 종류
            {
                var kind = (FlightEnhancementKind)index; // 실제 적용할 아이템
                if (!CanApply(kind, grade, position) || candidate-- != 0) // 무효 아이템을 제외한 인덱스
                {
                    continue; // 선택하지 않은 후보
                }
                Transform view = idle.Count == 0 ? Instantiate(template, transform) : idle.Pop(); // 풀이 비면 새 모델 사용
                view.SetPositionAndRotation(position, Quaternion.identity); // 처치 위치에 낙하 시작
                Renderer renderer = view.GetComponentInChildren<Renderer>(true); // 보석의 현재 등급 색
                tint.SetColor(BaseColor, Tints[grade - 1]); // 등급별 식별
                renderer.SetPropertyBlock(tint); // 프리팹 공통 재질 보존
                view.gameObject.SetActive(true); // 현재 활성 드롭
                active.Add(new Drop { view = view, kind = kind, grade = grade, remaining = life, born = Time.frameCount }); // 획득과 만료 등록
                return; // 첫 유효 선택 피켓만 생성
            }
        }

        private void LateUpdate() // 실제 비행 후 강화 위치·획득 판정
        {
            if (Time.timeScale == 0f) // 정지와 카드 화면에 드롭·보조 공격 동결
            {
                return; // 일시정지 시간은 유효하지 않음
            }
            Vector3 current = pilot.transform.position; // 이동이 반영된 기체
            for (int index = active.Count - 1; index >= 0; index--) // 현재 월드 드롭을 역순으로 처리
            {
                Drop drop = active[index]; // 해당 아이템의 생존 상태
                Vector3 start = drop.view.position; // 직전 프레임의 월드 위치
                bool newDrop = drop.born == Time.frameCount; // 드롭 이전의 기체 궤적 제외
                float duration = newDrop ? 0f : Mathf.Min(Time.deltaTime, drop.remaining); // 일시정지·만료를 포함하지 않는 실제 시간
                Vector3 end = FlightExperience.Fall(start, duration, 0.5f, 1 << 8); // 경험치와 동일한 3D 낙하
                Vector3 prior = newDrop ? current : previousPilot; // 조각이 없던 시간의 비행 제외
                Vector3 finish = Vector3.Lerp(prior, current, Time.deltaTime > 0f ? duration / Time.deltaTime : 1f); // 만료 전 상대 이동
                if (!pilot.IsDefeated && FlightExperience.WithinSweep(prior - start, finish - end, pickupRadius)) // 유효 시간에 지나간 실제 거리
                {
                    Recycle(index); // 같은 효과의 중복 획득 금지
                    TryApply(drop.kind, drop.grade); // 여전히 유효하면 즉시 적용
                    if (Time.timeScale == 0f) // 자석 경험치로 열린 카드 선택
                    {
                        break; // 이어서 하기 전 추가 드롭 처리 중지
                    }
                    continue; // 획득 상태 종료
                }
                drop.remaining -= duration; // 현재 낙하에 사용한 생존 시간
                if (drop.remaining <= 0f) // 20초 지나면 만료
                {
                    Recycle(index); // 별도 수치나 회복 없이 회수
                    continue; // 만료 시 월드 표시 생략
                }
                drop.view.position = end; // 현재 낙하한 위치
                drop.view.Rotate(0f, 80f * duration, 0f); // 월드에서 읽히는 회전
                active[index] = drop; // 현재 수명 갱신
            }
            previousPilot = current; // 다음 유효 프레임의 시작점
            if (Time.timeScale > 0f) // 자석 획득으로 선택 창이 열린 경우 제외
            {
                UpdateAssists(); // 장착한 보조 공격과 획득 표시
            }
        }

        private bool CanApply(FlightEnhancementKind kind, int grade, Vector3 position) // 실제로 효과를 줄 수 있는 후보
        {
            int index = (int)kind; // 기획의 효과 행
            float value = Effects[index][grade - 1]; // 현재 등급 숫자
            return kind switch // 아이템 종류에 따른 유효 조건
            {
                FlightEnhancementKind.Shield => value > pilot.ItemShield, // 더 높은 방어막만 갱신
                FlightEnhancementKind.Heal => pilot.Health < pilot.MaxHealth, // 체력에 여유
                FlightEnhancementKind.Energy => skills.Ultimate < 100, // 에너지에 여유
                FlightEnhancementKind.Magnet => experience.HasDropWithin(position, value), // 실제 회수할 조각
                _ => counts[index] < 3 && Remaining(kind) > 0.0001f // 최대 횟수와 카드 합산 상한
            };
        }

        private float Remaining(FlightEnhancementKind kind) // 유효한 카드 공통 용량
        {
            float ceiling = kind switch // 기획의 동일 효과 상한
            {
                FlightEnhancementKind.Damage => DamageLimit, // 화력 +60%
                FlightEnhancementKind.Cadence => CadenceLimit, // 간격 -40%
                FlightEnhancementKind.Pierce => PierceLimit, // 관통 +3
                FlightEnhancementKind.Cycle => CooldownLimit, // 기술 순환 -40%
                _ => float.PositiveInfinity // 근접 충전은 횟수로 제한
            };
            return Mathf.Max(0f, ceiling - Stored(kind) - cards.ItemSharedCardBonus(kind)); // 지금 실제로 올릴 부분
        }

        internal bool TryApply(FlightEnhancementKind kind, int grade) // 획득 때 현재 조건을 다시 확인
        {
            int index = (int)kind; // 효과 종류의 자료 위치
            Vector3 position = pilot.transform.position; // 실제 회수를 시작한 위치
            if (!CanApply(kind, grade, position)) // 등장 후 아이템 후보 상태 변화 확인
            {
                return false; // 무효 효과는 중복으로 더하지 않음
            }
            float value = Effects[index][grade - 1]; // 확정된 숫자
            switch (kind) // 즉시 효과와 출격 누적 효과
            {
                case FlightEnhancementKind.Shield: // 다음 피격에서 사용할 보호막
                    pilot.AddItemShield((int)value); // 더 큰 차단 횟수 사용
                    break; // 단일 보유량만 유지
                case FlightEnhancementKind.Heal: // 즉시 회복
                    pilot.Heal((int)value); // 최대 체력까지만
                    break; // 체력과 HUD 갱신
                case FlightEnhancementKind.Energy: // 즉시 필살기 충전
                    skills.AddUltimate((int)value); // 최대 100까지
                    break; // 누적 횟수는 없음
                case FlightEnhancementKind.Magnet: // 기존 필드 조각 회수
                    experience.CollectWithin(position, value); // 현재 등급의 반경
                    break; // 경험치 이월과 선택 처리
                default: // 출격 동안 유지되는 아이템 수치
                    bonuses[index] += Mathf.Min(value, Remaining(kind)); // 공통 상한의 남은 범위만 더함
                    counts[index]++; // 아이템 종류별 최대 3회
                    break; // 이후 실제 공격과 마력 처리에 반영
            }
            FlightAudio.Play(FlightSfx.Pickup); // 강화 적용이 완료된 획득 소리
            LastPickup = $"{Names[index]} {Grades[grade - 1]}"; // 실제 획득한 종류와 등급
            announceUntil = Time.time + 2.4f; // 전투 HUD에 잠시 표시
            pickupLabel.text = $"{LastPickup} 획득"; // 현재 전투에서 올라간 강화
            return true; // 성공적으로 소비한 강화
        }

        private void Recycle(int index) // 활성 드롭의 재사용
        {
            Transform view = active[index].view; // 실제 월드 오브젝트
            int tail = active.Count - 1; // 리스트에서 순서는 무관
            active[index] = active[tail]; // 마지막 값을 빈 자리로 이동
            active.RemoveAt(tail); // 현재 활성 개수 감소
            view.gameObject.SetActive(false); // 획득하거나 만료된 모델 숨김
            idle.Push(view); // 다음 처치에 쓰는 오브젝트
        }

        private void UpdateAssists() // 장착한 보조 슬롯 공격과 드롭 안내
        {
            int droneRank = pilot.IsDefeated ? 0 : cards.DroneRank; // 출격 중의 드론 단계
            int bladeRank = pilot.IsDefeated ? 0 : cards.BladeRank; // 출격 중의 칼날 단계
            drone.gameObject.SetActive(droneRank > 0); // 해당 슬롯만 표시
            blades.gameObject.SetActive(bladeRank > 0); // 해당 슬롯만 표시
            if (droneRank > 0) // 장착한 무다리 프로펠러 드론
            {
                propeller.Rotate(0f, 1200f * Time.deltaTime, 0f); // 실제 위쪽 프로펠러 회전
                if (Time.time >= nextDrone) // 다음 자동 발사
                {
                    FireDrone(3 + droneRank * 3); // 단계별 6·9·12 피해
                    nextDrone = Time.time + 1f; // 초당 하나의 시도
                }
            }
            if (bladeRank > 0) // 장착한 회전 칼날
            {
                blades.Rotate(0f, 220f * Time.deltaTime, 0f); // 기체 주위 칼날 회전
                if (Time.time >= nextBlade) // 한 초에 한 번 접촉 판정
                {
                    HitBlades(4 + bladeRank * 4); // 단계별 8·12·16 피해
                    nextBlade = Time.time + 1f; // 같은 적 짧은 시간 중복 방지
                }
            }
            if (Time.time >= announceUntil && pickupLabel.text.Length > 0) // 최근 강화 알림 만료
            {
                pickupLabel.text = string.Empty; // 상태 글자만 제거
            }
        }

        private void FireDrone(int damage) // 지형으로 가리지 않은 가장 가까운 적을 사격
        {
            float distance = 35f * 35f; // 드론의 사격 범위
            FlightEnemy target = null; // 가시 적이 있는 경우만 발사
            foreach (FlightEnemy enemy in sources) // 현재 전투의 시험 적 목록
            {
                if (enemy == null || !enemy.IsCombatTarget) continue;
                float square = (enemy.transform.position - drone.position).sqrMagnitude; // 드론에서 실제 거리
                if (enemy.isActiveAndEnabled && square < distance && ClearPath(drone.position, enemy.transform.position)) // 활성과 지형 가림 검증
                {
                    target = enemy; // 지금까지 가장 가까운 적
                    distance = square; // 더 먼 표적 제외
                }
            }
            if (target != null) // 근처에 자동 공격할 적이 있는 상태
            {
                weapon.CastAssist(drone.position, target.transform.position, damage); // 실제 이동하는 3D 보조 탄환
            }
        }

        private void HitBlades(int damage) // 현재 기체 주변 칼날의 충돌 범위
        {
            foreach (FlightEnemy enemy in sources) // 주변의 현재 활성 적
            {
                if (enemy == null || !enemy.IsCombatTarget) continue;
                Vector3 offset = enemy.transform.position - pilot.transform.position; // 기체의 실제 몸통 거리
                if (enemy.isActiveAndEnabled && offset.sqrMagnitude <= 9f && ClearPath(pilot.transform.position, enemy.transform.position)) // 반경 3의 접촉과 가림
                {
                    enemy.ReceiveHit(damage); // 한 초에 한 번 주변 적 피해
                }
            }
        }

        private static bool ClearPath(Vector3 start, Vector3 end) // 섬 뒤에서 자동 피해를 주지 않는 판정
        {
            return !Physics.Linecast(start, end, 1 << 8, QueryTriggerInteraction.Ignore); // 기존 지형 레이어의 실제 가림
        }
    }
}
