using System.Collections.Generic; // 시전 중 같은 대상과 발사량 관리
using UnityEngine; // 마법 사격 위치와 충돌

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCraftedSpellCaster : MonoBehaviour // 연습과 실전이 공유하는 제작 마법 발사기
    {
        internal readonly struct Profile // 발사체의 실제 판정과 자원
        {
            public readonly float Mana; // 소수점까지 반영하는 실제 마력 비용
            public readonly float Damage; // 적중 시 배율을 적용할 직격 피해
            public readonly float Cooldown; // 사용 후 기다릴 시간
            public readonly float Speed; // 월드 단위 탄속
            public readonly float Range; // 최대 비행 거리
            public readonly float Radius; // 표적과 충돌하는 반경
            public readonly float Spread; // 초기 배열의 전체 각도
            public readonly float DamageScale; // 폭발 등 별도 피해의 강화 배율
            public readonly float RadiusScale; // 범위 효과의 강화 배율

            public Profile(float mana, float damage, float cooldown, float speed, float range, float radius, float spread, float damageScale = 1f, float radiusScale = 1f) // 문서의 시험값 연결
            {
                Mana = mana; // 마력 소비
                Damage = damage; // 명중 피해
                Cooldown = cooldown; // 재사용 시간
                Speed = speed; // 이동 속도
                Range = range; // 최대 이동 거리
                Radius = radius; // 판정 반경
                Spread = spread; // 배열각
                DamageScale = damageScale; // 피해 계산의 강화 배율
                RadiusScale = radiusScale; // 범위 계산의 강화 배율
            }
        }

        private sealed class Barrier // 한 번 시전한 고정 정화 결계
        {
            public Vector3 center; // 결계가 생성된 월드 위치
            public float expires; // 결계가 종료되는 시각
            public float radius; // 시전 당시 확정한 결계 반경
            public int cleared; // 이 결계가 상쇄한 적탄 수
            public GameObject view; // 월드에 고정된 결계 외형
            public Material material; // 외형에서만 사용하는 색상 재질
        }

        private sealed class Burst // 한 번 시전한 순차 발사의 고정 수치
        {
            public CraftedSpellRecipe recipe; // 원래 제작 구성
            public Profile profile; // 시전 시점의 공격력과 탄환 수치
            public Quaternion aim; // 첫 발의 실제 조준 방향
            public FlightCraftedEmissionBudget.CastToken token; // 이 시전의 누적 생성 한도
            public int count; // 강화 이후 발사할 전체 탄 수
            public int next; // 다음 탄환의 발사 순번
            public float nextAt; // 다음 탄환의 예정 시각
        }

        private static readonly int[] Implemented = { 0, 1, 3, 4, 5, 7, 9, 11 }; // 실제 판정이 완성된 기획 마법
        [SerializeField] private FlightCraftedProjectile projectile; // 제작 마법의 공유 프리팹
        [SerializeField] private Transform muzzle; // 이 장면의 실제 발사 위치
        [SerializeField] private LayerMask hitLayers = (1 << 8) | (1 << 9); // 지형과 표적 레이어
        [SerializeField] private bool practice; // 일시정지 속에서 독립적인 연습 시간
        [SerializeField] private FirstFlightCamera aimCamera; // 실전 화면의 조준 기준
        private FlightWeapon weapon; // 전투에서 처음 시전한 조종사의 근거리 판정
        private FlightCraftedSpellBook growth; // 구성 ID별 출격 중 수치 강화
        private readonly FlightCraftedEmissionBudget emissions = new(); // 시전별·플레이어 공통 생성 한도
        private readonly List<FlightCraftedProjectile> active = new(); // 살아 있는 발사체
        private readonly Dictionary<string, float> readyAt = new(); // 같은 조합 두 장이 공유하는 대기시간
        private readonly List<Barrier> barriers = new(); // 살아 있는 정화 결계
        private readonly List<Burst> bursts = new(); // 시전 후 시간에 따라 발사하는 마법
        private FlightThreatPool threatPool; // 실전 적탄만 관리하는 풀
        private float practiceTime; // 연습에서만 흐르는 시각
        private float practiceMana = 100f; // 실전 출격과 분리한 연습 마력
        public int CastCount { get; private set; } // 이 발사기에서 성공한 시전 수
        public int EmissionCount { get; private set; } // 실제로 생성한 마법 발사체 수
        internal int PracticeMana => Mathf.FloorToInt(practiceMana); // 연습 화면의 현재 실제 마력
        private float Clock => practice ? practiceTime : Time.time; // 발사기별 독립 시계
        internal static float PracticeDelta => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime; // 검증과 실제 연습의 렌더 시간

        private void Awake() // 발사기와 같은 실제 전투 비행체
        {
            TryGetComponent(out weapon); // 독립 제작 시험장에서는 비어 있는 실제 무기
            if (!practice) // 독립 연습에서는 실전 적탄에 접근하지 않음
            {
                threatPool = FindFirstObjectByType<FlightThreatPool>(); // 현재 출격의 실제 적탄 풀
            }
        }

        private void Update() // 실전이 멈춰도 독립적으로 흐르는 연습
        {
            UpdateBarriers(); // 고정 결계의 유지와 적탄 상쇄
            if (practice) // 전투 시간과 연습 전용 시간 분리
            {
                practiceTime += PracticeDelta; // 연습 재사용 시간만 갱신
                practiceMana = Mathf.Min(100f, practiceMana + 10f * PracticeDelta); // 문서의 초당 연습 마력 회복
            }
            UpdateBursts(); // 실전 또는 독립 연습의 순차 발사
        }

        internal static bool Supports(CraftedSpellRecipe recipe) // 판정과 확정된 수치가 모두 준비된 마법
        {
            if (recipe == null) // 비어 있는 편성
            {
                return false; // 시전할 구성 없음
            }
            foreach (int index in Implemented) // 판정과 수치가 있는 정확한 구성
            {
                if (recipe.Id == CraftedSpellPresets.All[index].Recipe.Id) // 구성 요소와 탄수까지 일치
                {
                    return true; // 명시된 마법만 실제 발사 허용
                }
            }
            return false; // 미구현 또는 수치가 확정되지 않은 구성
        }

        private static bool IsFlameSpiral(CraftedSpellRecipe recipe) // 순차 발사와 화상이 있는 확정 구성
        {
            return recipe != null && recipe.Id == CraftedSpellPresets.All[1].Recipe.Id; // 플레임 스파이럴의 고유 구성
        }

        private static bool IsReturningRing(CraftedSpellRecipe recipe) // 왕복 경로와 공유 적중이 있는 확정 구성
        {
            return recipe != null && recipe.Id == CraftedSpellPresets.All[5].Recipe.Id; // 회류 고리의 고유 구성
        }

        private static bool IsPurification(CraftedSpellRecipe recipe) // 초기 정화 결계의 정확한 구성
        {
            return recipe != null && recipe.Id == CraftedSpellPresets.All[11].Recipe.Id; // 문서의 정화 결계 식별자
        }

        internal static Profile Stats(CraftedSpellRecipe recipe) // 지원하는 조합의 초기 시험 성능
        {
            if (IsFlameSpiral(recipe)) // 플레임 스파이럴의 확정 수치
            {
                return new Profile(20, 5, 6f, 24f, 24f, 0.15f, 0f); // 열두 탄환의 마력·피해·속도·거리
            }
            if (IsPurification(recipe)) // 고정 정화 결계
            {
                return new Profile(30, 0, 12f, 0f, 0f, 3.5f, 0f); // 마력과 종료 후 대기시간
            }
            if (recipe.Id == CraftedSpellPresets.All[4].Recipe.Id) // 급류 관통탄의 확정 수치
            {
                return new Profile(18, 22, 5f, 28f, 32f, 0.12f, 0f); // 세 침탄의 마력·피해·속도·거리
            }
            if (IsReturningRing(recipe)) // 회류 고리의 확정 수치
            {
                return new Profile(20, 28, 6f, 18f, 24f, 0.3f, 0f); // 여섯 원반의 마력·피해·왕복 거리
            }
            if (recipe.Shape == "wave") // 물결 파동의 투사체 규격
            {
                return new Profile(15, 36, 5f, 18f, 16f, 0.32f, 60f); // 파동 조합의 시험 탄속과 공격 범위
            }
            if (recipe.Shape == "arrow") // 홀리 샤워의 화살 규격
            {
                return new Profile(22, 12, 6f, 30f, 30f, 0.12f, 36f); // 기획서의 원뿔 화살
            }
            if (recipe.Shape == "needle") // 가시 산탄과 시험 유도 침탄
            {
                return recipe.Trajectory == "homing" ? new Profile(15, 16, 4f, 24f, 28f, 0.12f, 60f) : new Profile(15, 9, 4f, 24f, 14f, 0.12f, 60f); // 유도형 시제품 또는 가시 산탄
            }
            return new Profile(15, 35, 4f, 22f, 26f, 0.2f, 60f); // 구체탄 시험 기준
        }

        internal void BindGrowth(FlightCraftedSpellBook book) // 실전 발사기의 출격 강화 연결
        {
            growth = book; // 같은 구성의 강화 상태 공유
        }

        private int Steps(CraftedSpellRecipe recipe, FlightCraftedSpellBook.Upgrade kind) // 해당 시전의 강화 수치
        {
            return !practice && growth != null ? growth.UpgradeSteps(recipe, kind) : 0; // 제작 연습은 기본 성능 유지
        }

        internal Profile EffectiveStats(CraftedSpellRecipe recipe) // 다음 시전에 적용할 강화 수치
        {
            Profile basis = Stats(recipe); // 기획서의 강화 전 기준
            float power = !practice && growth != null ? growth.MagicAttack / 100f : 1f; // 기준 백에 대한 현재 마법 공격력 배율
            float damage = power * (1f + 0.1f * Steps(recipe, FlightCraftedSpellBook.Upgrade.Damage)); // 공격력과 임시 강화의 피해 배율
            float width = 1f + 0.05f * Steps(recipe, FlightCraftedSpellBook.Upgrade.Width); // 범위와 각도의 증가 배율
            float mana = 1f - 0.05f * Steps(recipe, FlightCraftedSpellBook.Upgrade.Mana); // 소수점 마력 감소 배율
            float cooldown = 1f - 0.1f * Steps(recipe, FlightCraftedSpellBook.Upgrade.Cooldown); // 다음 쿨타임 감소 배율
            return new Profile(basis.Mana * mana, basis.Damage * damage, basis.Cooldown * cooldown, basis.Speed, basis.Range, basis.Radius * width, basis.Spread * width, damage, width); // 현재 강화의 시전 스냅샷
        }

        internal bool HasEmissionRoom(CraftedSpellRecipe recipe) // 실제로 생성할 수 있는 플레이어 한도
        {
            if (!Supports(recipe)) // 실행하지 못하는 구성
            {
                return false; // 생성 가능 표시 없음
            }
            active.RemoveAll(shot => shot == null); // 이미 종료된 실제 발사체 제외
            int count = IsPurification(recipe) ? 1 : recipe.InitialShots + Steps(recipe, FlightCraftedSpellBook.Upgrade.Count); // 이 시전의 전체 최초 생성 수
            int immediate = IsFlameSpiral(recipe) ? 1 : count; // 순차 발사는 처음 한 발만 즉시 생성
            return count <= CraftedSpellRules.MaxInitial && emissions.CanBegin(immediate, active.Count + barriers.Count, Clock); // 실제 첫 발과 최초 상한 확인
        }

        internal bool TryContinueEmission(FlightCraftedEmissionBudget.CastToken token, int count) // 같은 시전의 후속 생성 한도 예약
        {
            active.RemoveAll(shot => shot == null); // 종료한 발사체는 활성 한도에서 제외
            return emissions.TryContinue(token, count, active.Count + barriers.Count, Clock); // 시전 누적과 플레이어 전체 한도 확인
        }

        internal float CooldownRemaining(CraftedSpellRecipe recipe) // 슬롯과 연습 UI의 재사용 시간
        {
            return recipe != null && readyAt.TryGetValue(recipe.Id, out float until) ? Mathf.Max(0f, until - Clock) : 0f; // 동일 구성 ID의 대기시간
        }

        internal bool Cast(CraftedSpellRecipe recipe) // 두 장면이 호출하는 공통 사격
        {
            if (!Supports(recipe) || CooldownRemaining(recipe) > 0f) // 미지원 구성과 재사용 대기
            {
                return false; // 자원이나 발사체를 소모하지 않음
            }
            Profile profile = EffectiveStats(recipe); // 이 시전의 강화된 수치 고정
            var origins = new FlightCraftedOriginContext // 현재 시점에 확인된 발사 기준
            {
                Front = muzzle.position, // 실제 마법 발사 지점
                Around = practice ? muzzle.position : transform.position // 연습과 실전의 주변 기준점
            };
            if (!FlightCraftedOrigins.TryResolve(recipe.Origin, origins, out Vector3 spawn)) // 실제로 존재하는 위치만 사용
            {
                return false; // 없는 지점에서 생성이나 자원 소비 금지
            }
            if (practice && practiceMana < profile.Mana) // 독립 연습의 실제 소모 마력 검사
            {
                return false; // 마력이 부족하면 생성 한도도 보존
            }
            active.RemoveAll(shot => shot == null); // 회수한 발사체는 활성 한도에서 제외
            int count = IsPurification(recipe) ? 1 : recipe.InitialShots + Steps(recipe, FlightCraftedSpellBook.Upgrade.Count); // 결계 또는 강화한 최초 생성 수
            int immediate = IsFlameSpiral(recipe) ? 1 : count; // 순차 발사의 처음 한 발만 예약
            if (count > CraftedSpellRules.MaxInitial || !emissions.TryBegin(immediate, active.Count + barriers.Count, Clock, out FlightCraftedEmissionBudget.CastToken token)) // 최초·누적·초당·동시 생성 한도
            {
                return false; // 일부 발사나 자원 차감 없이 거절
            }
            if (IsPurification(recipe)) // 정화 결계의 고정 설치
            {
                float started = Clock; // 결계 시작 시각
                float duration = Mathf.Min(6f, 4f + Steps(recipe, FlightCraftedSpellBook.Upgrade.Duration) * 0.5f); // 여섯 초 상한의 결계 유지시간
                Barrier barrier = CreateBarrier(spawn, started + duration, profile.Radius); // 시전 위치와 강화 반경의 고정 결계
                barriers.Add(barrier); // 수명과 상쇄 한도 관리
                readyAt[recipe.Id] = started + duration + profile.Cooldown; // 실제 유지 종료 후 재사용 대기
                if (practice) // 연습 마력만 독립 차감
                {
                    practiceMana -= profile.Mana; // 연습의 실제 소비
                }
                EmissionCount++; // 실제로 생성한 결계 기록
                CastCount++; // 유효한 결계 시전 기록
                return true; // 결계 설치 성공
            }
            float now = Clock; // 이번 시전의 시간
            Quaternion aim = AimRotation(profile.Range, spawn); // 실제 생성 위치에서 조준 목표 보정
            bool shared = recipe.Id == CraftedSpellPresets.All[3].Recipe.Id || IsReturningRing(recipe); // 수압과 회류의 시전별 중복 제한
            HashSet<Component> sharedHits = shared ? new HashSet<Component>() : null; // 같은 시전의 전진 적중 기록
            HashSet<Component> returnHits = IsReturningRing(recipe) ? new HashSet<Component>() : null; // 회류 고리의 별도 복귀 적중 기록
            int pierce = Mathf.Min(5, 3 + Steps(recipe, FlightCraftedSpellBook.Upgrade.Pierce)); // 관통 효과의 최대 대상 수
            for (int i = 0; i < immediate; i++) // 이번 프레임에 예약한 실제 탄환
            {
                SpawnShot(recipe, profile, spawn, aim, i, count, sharedHits, returnHits, pierce, token); // 같은 시전 수치와 생성 기록 적용
            }
            if (IsFlameSpiral(recipe) && count > 1) // 나머지 순차 발사가 있는 경우
            {
                bursts.Add(new Burst { recipe = recipe, profile = profile, aim = aim, token = token, count = count, next = 1, nextAt = now + 0.1f }); // 영 점 일 초 간격의 후속 발사
            }
            readyAt[recipe.Id] = now + profile.Cooldown; // 같은 조합의 양쪽 슬롯과 연결
            if (practice) // 실전은 기존 스킬의 공용 마력 사용
            {
                practiceMana -= profile.Mana; // 독립 연습의 마력 차감
            }
            CastCount++; // 실제 성공한 시전만 집계
            return true; // 소비 가능한 시전
        }

        private void SpawnShot(CraftedSpellRecipe recipe, Profile profile, Vector3 spawn, Quaternion aim, int index, int count, HashSet<Component> sharedHits, HashSet<Component> returnHits, int pierce, FlightCraftedEmissionBudget.CastToken token) // 예약된 개별 발사체 생성
        {
            Vector3 shotOrigin = spawn; // 해당 발사체의 실제 출발점
            Vector3 direction = CraftedSpellVectors.DirectionForCount(recipe, aim, index, profile.Spread, count); // 재현 가능한 입체 배열
            if (recipe.Id == CraftedSpellPresets.All[4].Recipe.Id) // 급류 관통탄의 집중 배열
            {
                float offset = (index - (count - 1) * 0.5f) * profile.Radius * 2f; // 탄환 지름 간격의 출발 위치
                shotOrigin += aim * Vector3.right * offset; // 서로 겹치지 않는 수평 발사점
                Vector3 focus = spawn + aim * Vector3.forward * profile.Range; // 사거리 끝의 공통 조준점
                direction = (focus - shotOrigin).normalized; // 확정 사거리 안에서 중앙으로 수렴
            }
            if (IsReturningRing(recipe)) // 원형으로 나뉜 여섯 원반의 출발 위치
            {
                float angle = index * 2f * Mathf.PI / count; // 발사체별 균등한 원주 위치
                shotOrigin += aim * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * profile.Radius * 2f; // 충돌 지름을 기준으로 배열
            }
            if (IsFlameSpiral(recipe)) // 순서에 따라 원주를 도는 나선 발사점
            {
                float angle = index * 2f * Mathf.PI / count; // 전체 탄 수에 맞춘 발사 순서
                shotOrigin += aim * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * profile.Radius * 2f; // 탄환 지름을 기준으로 회전
            }
            FlightCraftedProjectile shot = Instantiate(projectile, shotOrigin, Quaternion.LookRotation(direction, aim * Vector3.up), practice ? transform : null); // 월드 비행을 유지하는 공용 발사 프리팹
            shot.Begin(recipe, profile, hitLayers, practice, sharedHits, pierce, token, returnHits); // 같은 탄속·피해·접촉 처리
            if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Sera) // 세라가 실전에서 발사한 직접 마법
            {
                shot.SetMeleeSource(weapon.Sword); // 실제 적과의 거리로 제작 마법 피해 조정
            }
            else if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Iris) // 실제 전투에서 이리스가 사용한 제작 마법
            {
                shot.SetResonance(weapon.Resonance); // 마법 카드의 유효 적중도 공명에 합산
            }
            active.Add(shot); // 동시 활성 개수 검사에 반영
            EmissionCount++; // 실제 생성한 발사체 기록
        }

        private void UpdateBursts() // 실제 시간에 따라 순차 발사의 나머지 탄환 생성
        {
            if (!practice && Time.timeScale == 0f) // 정지된 실전에서는 후속 발사 차단
            {
                return; // 전투와 발사 시각 유지
            }
            for (int i = bursts.Count - 1; i >= 0; i--) // 종료한 순차 발사의 안전한 제거
            {
                Burst burst = bursts[i]; // 이 시전의 고정 수치와 진행 순서
                while (burst.next < burst.count && Clock + 0.00001f >= burst.nextAt) // 예정된 다음 발사의 시간이 된 경우
                {
                    if (!TryContinueEmission(burst.token, 1)) // 이 시전과 플레이어의 남은 생성 한도
                    {
                        break; // 한도가 비워질 때까지 후속 탄환 대기
                    }
                    SpawnShot(burst.recipe, burst.profile, muzzle.position, burst.aim, burst.next, burst.count, null, null, 3, burst.token); // 이동 중인 현재 발사점에서 한 발 생성
                    burst.next++; // 다음 나선 순번
                    burst.nextAt += 0.1f; // 기획서의 영 점 일 초 간격
                }
                if (burst.next >= burst.count) // 모든 후속 탄환 발사 완료
                {
                    bursts.RemoveAt(i); // 완료한 순차 발사 정리
                }
            }
        }

        private Barrier CreateBarrier(Vector3 center, float expires, float radius) // 월드에 고정된 결계 표시
        {
            var barrier = new Barrier { center = center, expires = expires, radius = radius }; // 결계의 위치와 수명
            barrier.view = new GameObject("Purification Barrier"); // 시전 위치의 독립 외형
            barrier.view.transform.position = center; // 움직이는 조종체와 분리된 좌표
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"); // 지원하는 단색 표시 셰이더
            if (shader == null) // 그래픽 자원이 없는 환경
            {
                return barrier; // 판정은 시각 자원과 독립 유지
            }
            barrier.material = new Material(shader); // 결계끼리 공유하지 않는 임시 재질
            barrier.material.color = new Color(1f, 0.88f, 0.45f, 1f); // 빛 속성의 금색 선
            for (int axis = 0; axis < 3; axis++) // 세 축의 구형 윤곽선
            {
                GameObject ring = new GameObject("Barrier Ring"); // 한 축의 결계 테두리
                ring.transform.SetParent(barrier.view.transform, false); // 고정 결계의 표시 자식
                LineRenderer line = ring.AddComponent<LineRenderer>(); // 연속 선으로 표시하는 윤곽
                line.sharedMaterial = barrier.material; // 세 윤곽의 동일 재질
                line.useWorldSpace = false; // 시전 위치 기준 로컬 좌표
                line.loop = true; // 마지막 선과 처음 선 연결
                line.widthMultiplier = 0.035f; // 지나치게 두껍지 않은 표시 폭
                line.positionCount = 64; // 부드러운 원형 선분 수
                for (int i = 0; i < line.positionCount; i++) // 구의 세 원주 좌표
                {
                    float angle = i * 2f * Mathf.PI / line.positionCount; // 원주에서의 각도
                    float a = Mathf.Cos(angle) * radius; // 원주의 첫 축
                    float b = Mathf.Sin(angle) * radius; // 원주의 둘째 축
                    line.SetPosition(i, axis == 0 ? new Vector3(a, b, 0f) : axis == 1 ? new Vector3(a, 0f, b) : new Vector3(0f, a, b)); // 선택한 평면의 점
                }
            }
            return barrier; // 시각과 판정을 함께 관리할 결계
        }

        private void UpdateBarriers() // 결계 수명과 허용 적탄 처리
        {
            if (!practice && Time.timeScale == 0f) // 정지 메뉴의 실전 판정 중단
            {
                return; // 멈춘 전투의 적탄과 수명 유지
            }
            for (int i = barriers.Count - 1; i >= 0; i--) // 결계 제거 중 안전한 역순 확인
            {
                Barrier barrier = barriers[i]; // 현재 고정 결계
                if (Clock >= barrier.expires) // 유지 시간 종료
                {
                    DestroyBarrier(barrier); // 외형과 임시 재질 제거
                    barriers.RemoveAt(i); // 종료한 결계 목록에서 제거
                    continue; // 종료 후 추가 상쇄 방지
                }
                if (!practice && threatPool != null && barrier.cleared < 16) // 실전에서만 남은 상쇄 한도 확인
                {
                    barrier.cleared += threatPool.CancelWithin(barrier.center, barrier.radius, 16 - barrier.cleared); // 허용 적탄만 남은 수까지 상쇄
                }
            }
        }

        private static void DestroyBarrier(Barrier barrier) // 결계 표시 자원 정리
        {
            if (barrier.view != null) // 아직 남아 있는 외형
            {
                Destroy(barrier.view); // 세 윤곽선을 함께 제거
            }
            if (barrier.material != null) // 생성한 임시 재질
            {
                Destroy(barrier.material); // 재질 자원 반환
            }
        }

        internal void ResetCasts() // 새 출격 또는 연습 초기화
        {
            foreach (FlightCraftedProjectile shot in active) // 남아 있는 제작 마법 회수
            {
                if (shot != null) // 아직 이동 중인 개체
                {
                    shot.gameObject.SetActive(false); // 다음 프레임 충돌 차단
                    Destroy(shot.gameObject); // 수명 종료
                }
            }
            active.Clear(); // 동시 활성 초기화
            foreach (Barrier barrier in barriers) // 이전 출격의 결계 정리
            {
                DestroyBarrier(barrier); // 결계 외형과 재질 반환
            }
            barriers.Clear(); // 결계 수명 기록 초기화
            bursts.Clear(); // 이전 시전의 후속 발사 취소
            emissions.Reset(); // 초당 생성과 이전 시전 기록 초기화
            readyAt.Clear(); // 재사용 대기 초기화
            practiceMana = 100f; // 표준 조건의 시험 마력 초기화
            CastCount = 0; // 현재 실행의 시험 통계 초기화
            EmissionCount = 0; // 실제 생성 수의 시험 통계 초기화
        }

        private void OnDestroy() // 씬 종료 시 결계의 임시 자원 정리
        {
            foreach (Barrier barrier in barriers) // 아직 유지 중인 결계
            {
                DestroyBarrier(barrier); // 남은 표시와 재질 반환
            }
            barriers.Clear(); // 종료한 시전 기록 제거
        }

        private Quaternion AimRotation(float range, Vector3 origin) // 카메라 중심과 실제 생성 위치의 시차 보정
        {
            if (aimCamera == null) // 마법 연습의 씬 조준 위치
            {
                return muzzle.rotation; // 시험 표적을 향한 발사점
            }
            Ray view = aimCamera.GetAttackRay(); // 실제 화면 중심 조준선
            Vector3 goal = Physics.Raycast(view, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Ignore) ? hit.point : view.GetPoint(range); // 실제 표적 거리 또는 사거리 끝
            Vector3 direction = goal - origin; // 실제 생성 위치에서 같은 표적을 향함
            return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : muzzle.rotation; // 발사 방향의 안전한 초기 각도
        }
    }
}
