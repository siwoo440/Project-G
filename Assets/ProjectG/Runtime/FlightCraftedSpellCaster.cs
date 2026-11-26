using System.Collections.Generic; // 시전 중 같은 대상과 발사량 관리
using UnityEngine; // 마법 사격 위치와 충돌
using UnityEngine.InputSystem; // 유지 광선의 실제 마우스 입력

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

        private sealed class Placement // 지연 효과를 기다리는 고정 설치체
        {
            public CraftedSpellRecipe recipe; // 시전 시점의 제작 구성
            public Profile profile; // 시전 시점의 자탄 수치
            public FlightCraftedEmissionBudget.CastToken token; // 모탄과 자탄의 공통 생성 기록
            public GameObject view; // 고정된 설치체 외형
            public Vector3 position; // 설치 시점의 월드 좌표
            public Quaternion aim; // 설치 시점의 자탄 방향
            public float splitAt; // 자탄 분열 시각
            public HashSet<Component> hits = new(); // 같은 설치체에서 이미 접촉한 대상
            public Material material; // 범위 윤곽의 독립 재질
        }

        private sealed class Trail // 시전 시점부터 기록한 하나의 잿불 영역
        {
            public CraftedSpellRecipe recipe; // 시전 당시 제작 구성
            public Profile profile; // 지속 피해와 반경의 고정 수치
            public FlightCraftedProjectile source; // 실제 대상 피해 처리
            public readonly List<Vector3> points = new(); // 제한된 비행 경로의 좌표
            public Vector3 lastSample; // 이전 프레임의 실제 비행 위치
            public float lastTime; // 이전 위치를 기록한 시각
            public float length; // 현재까지 기록한 경로 길이
            public float recordUntil; // 경로 기록 종료 시각
            public float endsAt; // 마지막 피해와 영역 종료 시각
            public float nextTick; // 다음 지속 피해 시각
            public int ticks; // 이미 처리한 지속 피해 횟수
            public GameObject view; // 고정된 경로 외형
            public LineRenderer line; // 기록 경로의 선 표시
            public Material material; // 경로 선의 임시 색상 재질
        }

        private sealed class Orbit // 시전당 한 개의 방어 위성 묶음
        {
            public CraftedSpellRecipe recipe; // 시전 시점의 제작 구성
            public Profile profile; // 흡수 반경과 재사용 수치
            public readonly List<GameObject> satellites = new(); // 함께 공전하는 네 위성
            public float started; // 공전 시작 시각
            public float endsAt; // 위성 묶음의 유지 종료 시각
            public int absorbed; // 이 시전에서 흡수한 적탄 수
        }

        private sealed class Beam // 한 번 유지하는 조준 광선
        {
            public CraftedSpellRecipe recipe; // 시전 당시 제작 구성
            public Profile profile; // 고정된 피해와 마력 수치
            public FlightCraftedProjectile source; // 실제 대상 피해 처리
            public GameObject view; // 유지 중인 광선 표시 객체
            public LineRenderer line; // 실제 조준 경로의 선
            public Material material; // 이 광선만 사용하는 임시 재질
            public float lastTime; // 유지 마력을 계산한 마지막 시각
            public float endsAt; // 최대 유지 종료 시각
            public float nextTick; // 다음 지속 피해 시각
            public int lane = -1; // 실제 유지 입력의 좌우 슬롯
        }

        private static readonly int[] Implemented = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }; // 실제 판정이 완성된 기획 마법
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
        private readonly List<Placement> placements = new(); // 지연 중인 고정 설치체
        private readonly List<Trail> trails = new(); // 경로를 기록하고 남기는 잿불 영역
        private readonly List<Orbit> orbits = new(); // 유지 중인 방어 위성 묶음
        private readonly List<Beam> beams = new(); // 유지 중인 광선 묶음
        private FlightThreatPool threatPool; // 실전 적탄만 관리하는 풀
        private FlightSkillController skills; // 적탄 흡수의 실제 마력 보상
        private FirstFlightController pilot; // 생존 상태와 위성 유지 조건
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
            TryGetComponent(out skills); // 실제 출격의 마력 관리
            TryGetComponent(out pilot); // 실제 출격 조종체 상태
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
            UpdatePlacements(); // 정지된 설치체의 지연 효과
            UpdateTrails(); // 비행 경로 기록과 독립 지속 피해
            UpdateOrbits(); // 방어 위성 공전과 허용 적탄 흡수
            UpdateBeams(); // 조준 광선의 유지 마력과 지속 피해
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

        private static bool IsPlacement(CraftedSpellRecipe recipe) // 설치·결계 공통 동시 한도 대상
        {
            return recipe != null && (recipe.FireMode == "placed" || recipe.Id == CraftedSpellPresets.All[14].Recipe.Id); // 설치형 구성과 지정 좌표 폭발체
        }

        private bool HasPlacementRoom(CraftedSpellRecipe recipe) // 실제로 유지하는 설치체와 결계의 합계
        {
            return !IsPlacement(recipe) || barriers.Count + placements.Count + trails.Count < 3; // 같은 플레이어의 동시 세 개 상한
        }

        internal static Profile Stats(CraftedSpellRecipe recipe) // 지원하는 조합의 초기 시험 성능
        {
            if (recipe.Id == CraftedSpellPresets.All[10].Recipe.Id) // 루미너스 레이의 확정 수치
            {
                return new Profile(10, 22, 10f, 0f, 30f, 0.35f, 0f); // 시작 마력과 지속 피해·길이·반경
            }
            if (recipe.Id == CraftedSpellPresets.All[12].Recipe.Id) // 다크 오비트의 확정 수치
            {
                return new Profile(30, 0, 14f, 180f, 2.2f, 0.6f, 0f); // 마력과 공전 속도·거리·흡수 반경
            }
            if (recipe.Id == CraftedSpellPresets.All[13].Recipe.Id) // 그림자 낙인의 확정 수치
            {
                return new Profile(18, 14, 5f, 30f, 28f, 0.12f, 0f); // 마력과 직격 피해·탄속·사거리
            }
            if (recipe.Id == CraftedSpellPresets.All[2].Recipe.Id) // 잿불 항로의 확정 수치
            {
                return new Profile(25, 9, 8f, 0f, 12f, 1.3f, 0f); // 마력과 지속 피해·기록 거리·반경
            }
            if (recipe.Id == CraftedSpellPresets.All[6].Recipe.Id) // 덩굴 고리의 확정 수치
            {
                return new Profile(24, 0, 10f, 0f, 18f, 3.2f, 0f); // 마력과 지정 거리·속박 반경
            }
            if (recipe.Id == CraftedSpellPresets.All[14].Recipe.Id) // 황혼 파열의 확정 수치
            {
                return new Profile(26, 70, 7f, 0f, 20f, 3.2f, 0f); // 마력과 폭발 피해·지정 거리·반경
            }
            if (recipe.Id == CraftedSpellPresets.All[8].Recipe.Id) // 씨앗 포대 자탄의 확정 수치
            {
                return new Profile(28, 26, 8f, 18f, 12f, 0.12f, 0f); // 마력과 자탄 피해·속도·거리
            }
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
            if (!Supports(recipe) || !HasPlacementRoom(recipe)) // 실행하지 못하는 구성과 설치 한도
            {
                return false; // 생성 가능 표시 없음
            }
            active.RemoveAll(shot => shot == null); // 이미 종료된 실제 발사체 제외
            int count = IsPurification(recipe) ? 1 : recipe.InitialShots + Steps(recipe, FlightCraftedSpellBook.Upgrade.Count); // 이 시전의 전체 최초 생성 수
            int immediate = IsFlameSpiral(recipe) ? 1 : count; // 순차 발사는 처음 한 발만 즉시 생성
            return count <= CraftedSpellRules.MaxInitial && emissions.CanBegin(immediate, active.Count + barriers.Count + placements.Count + trails.Count + orbits.Count * 4 + beams.Count, Clock); // 실제 첫 발과 최초 상한 확인
        }

        internal bool TryContinueEmission(FlightCraftedEmissionBudget.CastToken token, int count) // 같은 시전의 후속 생성 한도 예약
        {
            active.RemoveAll(shot => shot == null); // 종료한 발사체는 활성 한도에서 제외
            return emissions.TryContinue(token, count, active.Count + barriers.Count + placements.Count + trails.Count + orbits.Count * 4 + beams.Count, Clock); // 시전 누적과 플레이어 전체 한도 확인
        }

        internal float CooldownRemaining(CraftedSpellRecipe recipe) // 슬롯과 연습 UI의 재사용 시간
        {
            return recipe != null && readyAt.TryGetValue(recipe.Id, out float until) ? Mathf.Max(0f, until - Clock) : 0f; // 동일 구성 ID의 대기시간
        }

        internal bool Cast(CraftedSpellRecipe recipe) // 두 장면이 호출하는 공통 사격
        {
            if (!Supports(recipe) || !HasPlacementRoom(recipe) || CooldownRemaining(recipe) > 0f) // 미지원 구성과 설치 한도·재사용 대기
            {
                return false; // 자원이나 발사체를 소모하지 않음
            }
            Profile profile = EffectiveStats(recipe); // 이 시전의 강화된 수치 고정
            var origins = new FlightCraftedOriginContext // 현재 시점에 확인된 발사 기준
            {
                Front = muzzle.position, // 실제 마법 발사 지점
                Around = practice ? muzzle.position : transform.position, // 연습과 실전의 주변 기준점
                Point = recipe.Id == CraftedSpellPresets.All[14].Recipe.Id || recipe.Id == CraftedSpellPresets.All[6].Recipe.Id ? AimPoint(profile.Range) : null, // 지정 거리 내 실제 조준점
                Path = recipe.Id == CraftedSpellPresets.All[2].Recipe.Id ? practice ? muzzle.position : transform.position : null // 현재 비행 위치에서 시작하는 실제 경로
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
            if (count > CraftedSpellRules.MaxInitial || !emissions.TryBegin(immediate, active.Count + barriers.Count + placements.Count + trails.Count + orbits.Count * 4 + beams.Count, Clock, out FlightCraftedEmissionBudget.CastToken token)) // 최초·누적·초당·동시 생성 한도
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
            if (recipe.Id == CraftedSpellPresets.All[10].Recipe.Id) // 루미너스 레이의 유지 광선
            {
                GameObject view = Instantiate(projectile.gameObject, spawn, aim, practice ? transform : null); // 광선 피해와 표시의 공통 객체
                FlightCraftedProjectile source = view.GetComponent<FlightCraftedProjectile>(); // 공용 속성 피해 경로
                source.Begin(recipe, profile, hitLayers, practice, null, 3, token); // 시전 당시 공격력과 속성 보존
                source.enabled = false; // 발사체 이동과 직접 충돌 차단
                if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Sera) // 세라의 실전 지속 광선
                {
                    source.SetMeleeSource(weapon.Sword); // 실제 표적 거리의 피해 보정
                }
                else if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Iris) // 이리스의 실전 지속 광선
                {
                    source.SetResonance(weapon.Resonance); // 유효 피해의 공명 처리
                }
                view.GetComponentInChildren<Renderer>().enabled = false; // 구체 대신 광선 경로 표시
                LineRenderer line = view.AddComponent<LineRenderer>(); // 조준선의 임시 광선
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"); // 지원하는 단색 표시 셰이더
                Material material = shader != null ? new Material(shader) : null; // 이 시전의 독립 광선 재질
                if (material != null) // 사용 가능한 표시 재질
                {
                    material.color = new Color(1f, 0.9f, 0.5f); // 빛 속성의 밝은 금색
                    line.sharedMaterial = material; // 유지 중인 광선에 재질 적용
                }
                line.useWorldSpace = true; // 이동 중인 총구와 실제 조준선 사용
                line.widthMultiplier = profile.Radius * 2f; // 실제 판정 반경의 광선 폭
                line.positionCount = 2; // 광선의 시작점과 접점
                float duration = Mathf.Min(6f, 3f + Steps(recipe, FlightCraftedSpellBook.Upgrade.Duration) * 0.5f); // 유지 강화와 공통 시간 상한
                var beam = new Beam { recipe = recipe, profile = profile, source = source, view = view, line = line, material = material, lastTime = now, endsAt = now + duration, nextTick = now + 0.5f }; // 광선의 시전 당시 상태
                beams.Add(beam); // 현재 유지 중인 같은 마법 기록
                readyAt[recipe.Id] = beam.endsAt + profile.Cooldown; // 예상 종료와 이후 재사용 시간
                if (practice) // 독립 연습의 시작 마력
                {
                    practiceMana -= profile.Mana; // 실제 연습 시전 비용
                }
                EmissionCount++; // 실제 생성한 유지 광선 한 개
                CastCount++; // 하나의 성공한 마법 시전
                return true; // 광선 유지 시작
            }
            if (recipe.Id == CraftedSpellPresets.All[12].Recipe.Id) // 다크 오비트의 방어 위성
            {
                float duration = Mathf.Min(6f, 4f + Steps(recipe, FlightCraftedSpellBook.Upgrade.Duration) * 0.5f); // 강화와 공통 상한을 적용한 유지 시간
                var orbit = new Orbit { recipe = recipe, profile = profile, started = now, endsAt = now + duration }; // 시전 시점의 공전 수치 고정
                for (int i = 0; i < 4; i++) // 기획서의 방어 위성 네 개
                {
                    GameObject view = Instantiate(projectile.gameObject, spawn, aim, practice ? transform : null); // 실제 위치에 생성한 위성 외형
                    FlightCraftedProjectile satellite = view.GetComponent<FlightCraftedProjectile>(); // 공통 속성 표시 사용
                    satellite.Begin(recipe, profile, hitLayers, practice, null, 3, token); // 어둠 속성과 시전 기록 적용
                    satellite.enabled = false; // 접촉 피해와 직선 이동 차단
                    orbit.satellites.Add(view); // 같은 시전의 네 위성 관리
                }
                orbits.Add(orbit); // 공전 수명과 공통 흡수 한도 관리
                readyAt[recipe.Id] = orbit.endsAt + profile.Cooldown; // 유지 종료 후 재사용 시간
                if (practice) // 독립 연습의 마력
                {
                    practiceMana -= profile.Mana; // 실제 연습 시전 비용
                }
                EmissionCount += 4; // 실제 생성한 위성 네 개 기록
                CastCount++; // 하나의 성공한 마법 시전
                return true; // 공전 시작
            }
            if (recipe.Id == CraftedSpellPresets.All[2].Recipe.Id) // 이동 경로를 기록하는 잿불 항로
            {
                GameObject view = Instantiate(projectile.gameObject, spawn, aim, practice ? transform : null); // 시전 시점의 독립 영역 표시
                FlightCraftedProjectile source = view.GetComponent<FlightCraftedProjectile>(); // 공용 속성 피해 경로
                source.Begin(recipe, profile, hitLayers, practice, null, 3, token); // 시전 당시 공격력과 속성 보존
                source.enabled = false; // 이동 탄환과 직접 충돌 피해 차단
                if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Sera) // 세라의 실전 지속 피해
                {
                    source.SetMeleeSource(weapon.Sword); // 실제 표적 거리의 피해 보정
                }
                else if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Iris) // 이리스의 실전 지속 피해
                {
                    source.SetResonance(weapon.Resonance); // 유효 피해의 공명 처리
                }
                view.transform.localScale = Vector3.one * 0.2f; // 경로 시작점을 나타내는 작은 표시
                LineRenderer line = view.AddComponent<LineRenderer>(); // 남아 있는 경로의 임시 선
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"); // 지원하는 단색 셰이더
                Material material = shader != null ? new Material(shader) : null; // 다른 마법과 분리한 표시 재질
                if (material != null) // 사용 가능한 표시 재질
                {
                    material.color = new Color(1f, 0.4f, 0.15f); // 화염 경로의 주황색
                    line.sharedMaterial = material; // 이 경로에만 재질 적용
                }
                line.useWorldSpace = true; // 이동한 조종사와 분리한 월드 경로
                line.widthMultiplier = 0.1f; // 영역 중심의 가는 경로선
                line.positionCount = 1; // 실제 첫 기록 좌표
                line.SetPosition(0, spawn); // 시전 시점의 시작 위치
                var trail = new Trail { recipe = recipe, profile = profile, source = source, view = view, line = line, material = material, lastSample = spawn, lastTime = now, recordUntil = now + 1f, endsAt = now + 4f, nextTick = now + 0.5f }; // 제한된 경로와 시간 고정
                trail.points.Add(spawn); // 정지 중에도 유효한 시작 영역
                trails.Add(trail); // 이후 기록과 피해 갱신
                readyAt[recipe.Id] = now + 4f + profile.Cooldown; // 종료 예정 시각과 이후 재사용 시간
                if (practice) // 독립 연습의 실제 마력
                {
                    practiceMana -= profile.Mana; // 시전 마력 차감
                }
                EmissionCount++; // 시전한 잿불 영역 하나 기록
                CastCount++; // 실제 성공한 시전 기록
                return true; // 경로 기록 시작
            }
            if (recipe.Id == CraftedSpellPresets.All[6].Recipe.Id || recipe.Id == CraftedSpellPresets.All[8].Recipe.Id || recipe.Id == CraftedSpellPresets.All[14].Recipe.Id) // 지정 또는 지연 설치 마법
            {
                GameObject view = Instantiate(projectile.gameObject, spawn, aim, practice ? transform : null); // 원래 위치에 고정된 임시 외형
                FlightCraftedProjectile installed = view.GetComponent<FlightCraftedProjectile>(); // 피해 경로를 공유할 설치체
                installed.Begin(recipe, profile, hitLayers, practice, null, 3, token); // 시전 시점의 피해와 속성 고정
                installed.enabled = false; // 설치체의 이동과 충돌 피해 차단
                if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Sera) // 세라의 실전 마법
                {
                    installed.SetMeleeSource(weapon.Sword); // 실제 표적까지의 피해 보정
                }
                else if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Iris) // 이리스의 실전 마법
                {
                    installed.SetResonance(weapon.Resonance); // 유효 피해의 공명 처리
                }
                view.transform.localScale = Vector3.one * 0.5f; // 자탄과 구분되는 설치체 크기
                float duration = recipe.Id == CraftedSpellPresets.All[6].Recipe.Id ? Mathf.Min(6f, 4f + Steps(recipe, FlightCraftedSpellBook.Upgrade.Duration) * 0.5f) : 1f; // 덩굴 영역 또는 지연 시간
                var placement = new Placement { recipe = recipe, profile = profile, token = token, view = view, position = spawn, aim = aim, splitAt = now + duration }; // 유지 종료 또는 지연 효과 예약
                if (recipe.Id != CraftedSpellPresets.All[8].Recipe.Id) // 덩굴과 파열의 실제 영역 경계
                {
                    DrawPlacementOutline(placement); // 판정 반경의 세 축 윤곽
                }
                placements.Add(placement); // 실제 설치체의 시간 관리
                readyAt[recipe.Id] = now + profile.Cooldown + (recipe.Id == CraftedSpellPresets.All[14].Recipe.Id ? 0f : duration); // 파열은 시전부터, 설치형은 종료 후 대기
                if (practice) // 연습 공간의 독립 마력
                {
                    practiceMana -= profile.Mana; // 실제 연습 마력 차감
                }
                EmissionCount++; // 생성한 모탄 한 개 기록
                CastCount++; // 실제 성공한 설치 기록
                return true; // 설치 성공
            }
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
            if (recipe.Id == CraftedSpellPresets.All[8].Recipe.Id) // 씨앗 포대의 세 자탄
            {
                direction = Quaternion.AngleAxis(index * 120f, aim * Vector3.up) * (aim * Vector3.forward); // 수평면의 백이십 도 간격
            }
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
                        burst.next++; // 초과한 예정 탄환의 발사 순번 소진
                        burst.nextAt += 0.1f; // 다음 예정 발사의 원래 시각 유지
                        continue; // 초과 탄환의 지연 생성 금지
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

        private void UpdatePlacements() // 고정 설치체의 지연 효과와 종료
        {
            if (!practice && Time.timeScale == 0f) // 실전 정지 중 설치 시간 동결
            {
                return; // 전투 재개까지 분열 중지
            }
            for (int i = placements.Count - 1; i >= 0; i--) // 종료 중 목록의 안전한 순회
            {
                Placement placement = placements[i]; // 설치 당시 위치와 고정 수치
                if (placement.recipe.Id == CraftedSpellPresets.All[6].Recipe.Id) // 지정 좌표에 고정된 덩굴 영역
                {
                    if (Clock >= placement.splitAt) // 설치 영역의 유지 종료
                    {
                        DestroyPlacement(placement); // 고정 설치체와 재질 회수
                        placements.RemoveAt(i); // 종료한 영역 제거
                        readyAt[placement.recipe.Id] = Clock + placement.profile.Cooldown; // 종료 이후 재사용 시간 시작
                        continue; // 종료된 영역의 접촉 검사 제외
                    }
                    Collider[] contacts = Physics.OverlapSphere(placement.position, placement.profile.Radius, hitLayers, QueryTriggerInteraction.Ignore); // 영역 안에 실제로 접촉한 충돌체
                    foreach (Collider shape in contacts) // 들어온 적의 몸체와 약점 검사
                    {
                        Component target = FlightCraftedProjectile.Resolve(shape); // 실제 마물이나 연습 표적
                        if (target is FlightEnemy enemy && placement.hits.Add(target)) // 같은 시전에서 처음 닿은 실제 마물
                        {
                            if (!enemy.TryGetComponent(out FlightCraftedStatus status)) // 대상의 공통 제어 상태
                            {
                                status = enemy.gameObject.AddComponent<FlightCraftedStatus>(); // 첫 접촉에서 상태 연결
                            }
                            status.TryRoot(1.2f); // 일반 적 속박과 보스 저항 확인
                        }
                    }
                    continue; // 덩굴 영역의 폭발과 분열 제외
                }
                if (Clock + 0.00001f < placement.splitAt) // 아직 일 초가 지나지 않은 설치체
                {
                    continue; // 예정 시각까지 유지
                }
                active.RemoveAll(shot => shot == null); // 종료한 발사체 제외
                if (placement.recipe.Id == CraftedSpellPresets.All[14].Recipe.Id) // 황혼 파열의 지연 폭발
                {
                    Collider[] overlaps = Physics.OverlapSphere(placement.position, placement.profile.Radius, hitLayers, QueryTriggerInteraction.Ignore); // 지정 반경의 실제 충돌체
                    var hit = new HashSet<Component>(); // 몸체와 약점이 공유하는 대상 기록
                    FlightCraftedProjectile source = placement.view.GetComponent<FlightCraftedProjectile>(); // 설치 시점 피해를 고정한 발사체
                    foreach (Collider shape in overlaps) // 범위 안의 모든 실제 충돌체
                    {
                        Component target = FlightCraftedProjectile.Resolve(shape); // 실제 마물이나 연습 표적
                        if (target != null && hit.Add(target)) // 한 대상당 한 번의 폭발
                        {
                            source.Damage(target, placement.profile.Damage); // 상성과 캐릭터 효과가 반영된 폭발 피해
                        }
                    }
                    DestroyPlacement(placement); // 피해 적용 후 설치체와 재질 회수
                    placements.RemoveAt(i); // 종료한 폭발체 제거
                    continue; // 자탄 분열은 처리하지 않음
                }
                DestroyPlacement(placement); // 모탄 자체는 피해 없이 종료
                placements.RemoveAt(i); // 동시 활성 설치체에서 제외
                for (int shot = 0; shot < 3; shot++) // 재분열 없는 자탄 세 발
                {
                    if (TryContinueEmission(placement.token, 1)) // 남은 한도에 들어가는 자탄
                    {
                        SpawnShot(placement.recipe, placement.profile, placement.position, placement.aim, shot, 3, null, null, 3, placement.token); // 하나의 시전에 속한 자탄 생성
                    }
                }
                readyAt[placement.recipe.Id] = Clock + placement.profile.Cooldown; // 설치체 종료 후 재사용 시간 시작
            }
        }

        private static void DrawPlacementOutline(Placement placement) // 설치 마법의 실제 범위 표시
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"); // 지원하는 단색 표시 셰이더
            if (shader == null) // 그래픽 재질을 만들 수 없는 환경
            {
                return; // 실제 판정은 표시와 독립 유지
            }
            placement.material = new Material(shader); // 이 설치체의 임시 윤곽 재질
            placement.material.color = placement.recipe.Element == "grass" ? new Color(0.58f, 1f, 0.25f) : new Color(0.68f, 0.39f, 1f); // 마법 속성의 시각 구분
            for (int axis = 0; axis < 3; axis++) // 세 축의 구형 판정 윤곽
            {
                GameObject ring = new GameObject("Crafted Area Ring"); // 한 축의 범위 표시
                ring.transform.SetParent(placement.view.transform, false); // 고정 설치체 아래의 독립 윤곽
                LineRenderer line = ring.AddComponent<LineRenderer>(); // 실제 반경의 원형 선
                line.sharedMaterial = placement.material; // 같은 영역의 세 선에 재질 공유
                line.useWorldSpace = true; // 부모의 임시 메시 크기와 분리된 월드 좌표
                line.loop = true; // 닫힌 원형 경계
                line.widthMultiplier = 0.035f; // 화면을 가리지 않는 선 폭
                line.positionCount = 64; // 부드러운 원형의 점 수
                for (int i = 0; i < line.positionCount; i++) // 해당 평면의 모든 점
                {
                    float angle = i * 2f * Mathf.PI / line.positionCount; // 원주상의 균등한 각도
                    float a = Mathf.Cos(angle) * placement.profile.Radius; // 실제 판정 반경의 첫 축
                    float b = Mathf.Sin(angle) * placement.profile.Radius; // 실제 판정 반경의 둘째 축
                    Vector3 offset = axis == 0 ? new Vector3(a, b, 0f) : axis == 1 ? new Vector3(a, 0f, b) : new Vector3(0f, a, b); // 선택한 평면의 상대 좌표
                    line.SetPosition(i, placement.position + offset); // 지정 좌표 기준의 고정 경계
                }
            }
        }

        private static void DestroyPlacement(Placement placement) // 설치체와 임시 표시 자원 회수
        {
            if (placement.view != null) // 남아 있는 설치체 객체
            {
                Destroy(placement.view); // 메시와 범위 윤곽 회수
            }
            if (placement.material != null) // 설치체가 직접 만든 재질
            {
                Destroy(placement.material); // 공유하지 않는 임시 재질 회수
            }
        }

        private void UpdateTrails() // 실제 비행 위치 기록과 대상별 지속 피해
        {
            if (!practice && Time.timeScale == 0f) // 실전 정지 중 경로와 피해 중단
            {
                return; // 전투 재개까지 시간 보존
            }
            for (int i = trails.Count - 1; i >= 0; i--) // 종료된 영역을 안전하게 제거
            {
                Trail trail = trails[i]; // 하나의 독립적인 잿불 시전
                Vector3 current = practice ? muzzle.position : transform.position; // 현재 실제 비행 위치
                if (trail.lastTime < trail.recordUntil && Clock > trail.lastTime) // 최초 일 초의 비행 경로
                {
                    float portion = Mathf.Clamp01((trail.recordUntil - trail.lastTime) / (Clock - trail.lastTime)); // 기록 종료 시각에서 자른 이동 비율
                    Vector3 sample = Vector3.Lerp(trail.lastSample, current, portion); // 실제 시간 범위의 마지막 좌표
                    Vector3 delta = sample - trail.points[trail.points.Count - 1]; // 마지막으로 보존한 점에서 이동한 거리
                    float remaining = Mathf.Max(0f, trail.profile.Range - trail.length); // 십이 미터까지의 남은 기록 길이
                    if (delta.sqrMagnitude > 0.000001f && remaining > 0f) // 유효한 이동과 남은 거리
                    {
                        Vector3 step = Vector3.ClampMagnitude(delta, remaining); // 제한을 넘지 않는 마지막 선분
                        trail.points.Add(trail.points[trail.points.Count - 1] + step); // 실제 이동 경로 좌표 추가
                        trail.length += step.magnitude; // 기록한 선분 길이 합산
                        trail.line.positionCount = trail.points.Count; // 표시할 전체 경로 점 수
                        trail.line.SetPosition(trail.points.Count - 1, trail.points[trail.points.Count - 1]); // 새 좌표의 경로 표시
                    }
                }
                trail.lastSample = current; // 다음 프레임의 실제 위치 기준
                trail.lastTime = Clock; // 다음 프레임의 시간 기준
                while (trail.ticks < 8 && Clock + 0.00001f >= trail.nextTick && trail.nextTick <= trail.endsAt + 0.00001f) // 마지막 유효 틱까지 처리
                {
                    DamageTrail(trail); // 겹친 선분을 합친 대상별 피해
                    trail.ticks++; // 실제 처리한 틱 수
                    trail.nextTick += 0.5f; // 다음 영 점 오 초 간격
                }
                if (Clock + 0.00001f >= trail.endsAt) // 마지막 유효 피해를 처리한 뒤 종료
                {
                    readyAt[trail.recipe.Id] = Clock + trail.profile.Cooldown; // 영역 종료 후 재사용 대기
                    DestroyTrail(trail); // 표시와 임시 재질 회수
                    trails.RemoveAt(i); // 종료한 영역 제거
                }
            }
        }

        private void UpdateOrbits() // 위성 공전과 허용된 적탄만 흡수
        {
            if (!practice && Time.timeScale == 0f) // 전투 정지 중 공전과 흡수 중단
            {
                return; // 실전 시간과 위성 상태 유지
            }
            for (int i = orbits.Count - 1; i >= 0; i--) // 종료된 위성 묶음의 역순 처리
            {
                Orbit orbit = orbits[i]; // 같은 시전의 공전과 흡수 기록
                if (Clock >= orbit.endsAt || (!practice && pilot != null && !pilot.CanFly)) // 유지 시간 종료 또는 전투 불가
                {
                    readyAt[orbit.recipe.Id] = Clock + orbit.profile.Cooldown; // 실제 종료 후 재사용 시간
                    DestroyOrbit(orbit); // 네 위성의 외형 회수
                    orbits.RemoveAt(i); // 종료한 묶음 제거
                    continue; // 종료 후 적탄 흡수 금지
                }
                Transform center = practice ? muzzle : transform; // 연습 발사점 또는 실제 비행체 중심
                float phase = (Clock - orbit.started) * orbit.profile.Speed; // 초당 백팔십 도의 실제 공전 각도
                for (int satellite = 0; satellite < orbit.satellites.Count; satellite++) // 네 위성의 실제 위치
                {
                    float angle = (phase + satellite * 90f) * Mathf.Deg2Rad; // 서로 구십 도 간격의 공전 위치
                    Vector3 offset = center.right * Mathf.Cos(angle) + center.forward * Mathf.Sin(angle); // 조종체 주변의 수평 공전면
                    Vector3 position = center.position + offset * orbit.profile.Range; // 반경 이 점 이 미터의 실제 위치
                    orbit.satellites[satellite].transform.position = position; // 같은 시전 위성의 외형 갱신
                    if (!practice && threatPool != null && orbit.absorbed < 10) // 실전의 남은 흡수 한도
                    {
                        int absorbed = threatPool.AbsorbWithin(position, orbit.profile.Radius, 10 - orbit.absorbed); // 위성에 닿은 허용 적탄만 회수
                        orbit.absorbed += absorbed; // 네 위성이 공유하는 시전당 횟수
                        if (absorbed > 0) // 실제 흡수한 적탄이 있는 경우
                        {
                            skills?.RestoreCraftedMana(absorbed); // 발당 일 마력의 실제 환급
                        }
                    }
                }
            }
        }

        internal void SetBeamLane(CraftedSpellRecipe recipe, int lane) // 유지 광선을 실제 입력 슬롯에 연결
        {
            foreach (Beam beam in beams) // 현재 유지 중인 광선
            {
                if (beam.recipe.Id == recipe.Id && beam.lane < 0) // 방금 시작한 같은 마법
                {
                    beam.lane = lane; // 시작한 좌우 버튼의 유지 여부 사용
                    return; // 다른 시전의 입력은 변경하지 않음
                }
            }
        }

        private void UpdateBeams() // 광선의 실제 조준과 유지 자원·피해
        {
            if (!practice && Time.timeScale == 0f) // 실전 정지 중 광선 시간 동결
            {
                return; // 정지 해제까지 유지
            }
            for (int i = beams.Count - 1; i >= 0; i--) // 종료한 광선의 안전한 역순 처리
            {
                Beam beam = beams[i]; // 이번 시전의 고정된 수치
                Mouse mouse = Mouse.current; // 현재 실제 마우스 입력
                bool holding = practice || beam.lane < 0 || (mouse != null && (beam.lane == 0 ? mouse.leftButton.isPressed : mouse.rightButton.isPressed)); // 연습 자동 유지 또는 해당 버튼 유지
                if (!holding || (!practice && skills != null && !skills.CanSpendCraftedMana())) // 입력 해제 또는 실전 시전 불가 상태
                {
                    EndBeam(beam); // 실제 종료 시점부터 대기 시작
                    beams.RemoveAt(i); // 종료한 광선 제거
                    continue; // 종료된 광선의 피해 금지
                }
                float until = Mathf.Min(Clock, beam.endsAt); // 최대 유지 시각까지만 진행
                float delta = Mathf.Max(0f, until - beam.lastTime); // 아직 처리하지 않은 유지 시간
                float rate = 8f * beam.profile.Mana / 10f; // 마력 강화가 반영된 초당 유지 비용
                float requested = delta * rate; // 이번 프레임의 실제 유지 비용
                float spent = practice ? Mathf.Min(practiceMana, requested) : skills != null ? skills.ConsumeCraftedMaintenance(requested) : 0f; // 보유 마력 안에서만 차감
                if (practice) // 연습과 실전의 독립 마력
                {
                    practiceMana -= spent; // 연습 유지 비용만 별도 차감
                }
                float covered = rate > 0f ? spent / rate : delta; // 보유 마력으로 유지할 수 있었던 시간
                float processedUntil = beam.lastTime + Mathf.Min(delta, covered); // 자원이 보장된 마지막 유효 시각
                Vector3 origin = muzzle.position; // 현재 실전 또는 연습 발사점
                Vector3 direction = AimRotation(beam.profile.Range, origin) * Vector3.forward; // 매 프레임 갱신한 실제 조준 방향
                RaycastHit[] contacts = Physics.SphereCastAll(origin, beam.profile.Radius, direction, beam.profile.Range, hitLayers, QueryTriggerInteraction.Ignore); // 광선 폭과 사거리의 실제 충돌
                System.Array.Sort(contacts, (left, right) => left.distance.CompareTo(right.distance)); // 지형과 표적의 접촉 순서
                Component target = contacts.Length > 0 ? FlightCraftedProjectile.Resolve(contacts[0].collider) : null; // 첫 접촉만 피해 대상으로 사용
                float distance = contacts.Length > 0 ? contacts[0].distance : beam.profile.Range; // 첫 장애물까지의 실제 길이
                beam.line.SetPosition(0, origin); // 움직이는 총구에서 표시 시작
                beam.line.SetPosition(1, origin + direction * distance); // 장애물 앞에서 표시 종료
                while (beam.nextTick <= processedUntil + 0.00001f && beam.nextTick <= beam.endsAt + 0.00001f) // 자원이 보장된 마지막 유효 틱
                {
                    if (target != null) // 가장 가까운 접촉이 피해 대상인 경우
                    {
                        beam.source.Damage(target, beam.profile.Damage); // 시전 시점 수치와 실제 속성 피해
                    }
                    beam.nextTick += 0.5f; // 다음 영 점 오 초의 피해 시각
                }
                beam.lastTime = processedUntil; // 실제 유지한 마지막 시각
                if (spent + 0.00001f < requested || Clock + 0.00001f >= beam.endsAt) // 마력 부족 또는 최대 유지 종료
                {
                    EndBeam(beam); // 실제 종료 후 재사용 시간
                    beams.RemoveAt(i); // 종료한 광선 제거
                }
            }
        }

        private void EndBeam(Beam beam) // 유지 종료와 임시 광선 회수
        {
            readyAt[beam.recipe.Id] = Clock + beam.profile.Cooldown; // 종료 후 공유 재사용 시간
            if (beam.view != null) // 남아 있는 광선 객체
            {
                Destroy(beam.view); // 표시와 피해 원본 회수
            }
            if (beam.material != null) // 이 시전에서 만든 재질
            {
                Destroy(beam.material); // 임시 광선 재질 회수
            }
        }

        private static void DestroyOrbit(Orbit orbit) // 종료한 위성 외형 정리
        {
            foreach (GameObject satellite in orbit.satellites) // 같은 시전의 위성 네 개
            {
                if (satellite != null) // 아직 살아 있는 외형
                {
                    Destroy(satellite); // 공용 탄환의 임시 인스턴스 회수
                }
            }
            orbit.satellites.Clear(); // 종료한 객체 참조 제거
        }

        private void DamageTrail(Trail trail) // 전체 경로를 합친 한 번의 지속 피해
        {
            var hit = new HashSet<Component>(); // 겹친 영역에서 한 번만 처리할 대상
            for (int i = 0; i < trail.points.Count; i++) // 각 기록 점과 앞 선분
            {
                Vector3 end = trail.points[i]; // 이번 선분의 끝 위치
                Vector3 start = i == 0 ? end : trail.points[i - 1]; // 시작점도 구형 영역으로 검사
                Collider[] contacts = Physics.OverlapCapsule(start, end, trail.profile.Radius, hitLayers, QueryTriggerInteraction.Ignore); // 실제 경로 반경의 충돌체
                foreach (Collider shape in contacts) // 같은 틱의 모든 표적
                {
                    Component target = FlightCraftedProjectile.Resolve(shape); // 마물과 연습 표적의 실제 피해 대상
                    if (target != null && hit.Add(target)) // 겹친 선분과 약점의 중복 제외
                    {
                        trail.source.Damage(target, trail.profile.Damage); // 시전 당시 피해와 현재 대상 상성
                    }
                }
            }
        }

        private static void DestroyTrail(Trail trail) // 잿불 영역의 임시 표시 자원 회수
        {
            if (trail.view != null) // 아직 남아 있는 경로 객체
            {
                Destroy(trail.view); // 표시와 피해 원본 회수
            }
            if (trail.material != null) // 독립 생성한 경로 재질
            {
                Destroy(trail.material); // 공유하지 않는 임시 재질 회수
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
            foreach (Placement placement in placements) // 아직 분열하지 않은 설치체
            {
                DestroyPlacement(placement); // 남은 설치체와 표시 재질 회수
            }
            placements.Clear(); // 설치체와 예약된 자탄 취소
            foreach (Trail trail in trails) // 아직 유지 중인 잿불 영역
            {
                DestroyTrail(trail); // 이전 경로와 표시 재질 회수
            }
            trails.Clear(); // 이전 영역의 피해 예약 취소
            foreach (Orbit orbit in orbits) // 아직 공전 중인 방어 위성
            {
                DestroyOrbit(orbit); // 이전 출격의 네 위성 회수
            }
            orbits.Clear(); // 공전과 흡수 기록 초기화
            foreach (Beam beam in beams) // 아직 유지 중인 광선
            {
                EndBeam(beam); // 이전 광선과 표시 자원 회수
            }
            beams.Clear(); // 이전 시전의 유지 피해 취소
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
            foreach (Placement placement in placements) // 씬에 남은 씨앗 설치체
            {
                DestroyPlacement(placement); // 임시 설치체와 재질 회수
            }
            placements.Clear(); // 설치체 참조 정리
            foreach (Trail trail in trails) // 씬에 남은 잿불 경로
            {
                DestroyTrail(trail); // 남은 경로와 재질 회수
            }
            trails.Clear(); // 종료한 영역 참조 정리
            foreach (Orbit orbit in orbits) // 씬에 남아 있는 방어 위성
            {
                DestroyOrbit(orbit); // 위성 외형 회수
            }
            orbits.Clear(); // 종료한 시전 참조 제거
            foreach (Beam beam in beams) // 씬에 남아 있는 유지 광선
            {
                EndBeam(beam); // 광선과 임시 재질 회수
            }
            beams.Clear(); // 종료한 광선 참조 제거
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

        private Vector3 AimPoint(float range) // 조준선의 실제 장애물 또는 최대 지정 거리
        {
            if (aimCamera == null) // 독립 연습의 고정 발사 방향
            {
                return muzzle.position + muzzle.forward * range; // 발사점에서 지정 거리의 좌표
            }
            Ray view = aimCamera.GetAttackRay(); // 실제 카메라 조준선
            Vector3 goal = Physics.Raycast(view, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Ignore) ? hit.point : view.GetPoint(range); // 조준선에 닿는 첫 장애물 또는 끝점
            return muzzle.position + Vector3.ClampMagnitude(goal - muzzle.position, range); // 실제 발사점 기준의 최대 거리 제한
        }
    }
}
