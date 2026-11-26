using System.Collections.Generic; // 시전 중 같은 대상과 발사량 관리
using UnityEngine; // 마법 사격 위치와 충돌

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightCraftedSpellCaster : MonoBehaviour // 연습과 실전이 공유하는 제작 마법 발사기
    {
        internal readonly struct Profile // 발사체의 실제 판정과 자원
        {
            public readonly int Mana; // 보석 숫자와 같은 마력 비용
            public readonly int Damage; // 직격 피해
            public readonly float Cooldown; // 사용 후 기다릴 시간
            public readonly float Speed; // 월드 단위 탄속
            public readonly float Range; // 최대 비행 거리
            public readonly float Radius; // 표적과 충돌하는 반경
            public readonly float Spread; // 초기 배열의 전체 각도

            public Profile(int mana, int damage, float cooldown, float speed, float range, float radius, float spread) // 문서의 시험값 연결
            {
                Mana = mana; // 마력 소비
                Damage = damage; // 명중 피해
                Cooldown = cooldown; // 재사용 시간
                Speed = speed; // 이동 속도
                Range = range; // 최대 이동 거리
                Radius = radius; // 판정 반경
                Spread = spread; // 배열각
            }
        }

        private sealed class Barrier // 한 번 시전한 고정 정화 결계
        {
            public Vector3 center; // 결계가 생성된 월드 위치
            public float expires; // 결계가 종료되는 시각
            public int cleared; // 이 결계가 상쇄한 적탄 수
            public GameObject view; // 월드에 고정된 결계 외형
            public Material material; // 외형에서만 사용하는 색상 재질
        }

        [SerializeField] private FlightCraftedProjectile projectile; // 제작 마법의 공유 프리팹
        [SerializeField] private Transform muzzle; // 이 장면의 실제 발사 위치
        [SerializeField] private LayerMask hitLayers = (1 << 8) | (1 << 9); // 지형과 표적 레이어
        [SerializeField] private bool practice; // 일시정지 속에서 독립적인 연습 시간
        [SerializeField] private FirstFlightCamera aimCamera; // 실전 화면의 조준 기준
        private FlightWeapon weapon; // 전투에서 처음 시전한 조종사의 근거리 판정
        private readonly Queue<float> emissions = new(); // 최근 1초의 생성 시간
        private readonly List<FlightCraftedProjectile> active = new(); // 살아 있는 발사체
        private readonly Dictionary<string, float> readyAt = new(); // 같은 조합 두 장이 공유하는 대기시간
        private readonly List<Barrier> barriers = new(); // 살아 있는 정화 결계
        private FlightThreatPool threatPool; // 실전 적탄만 관리하는 풀
        private float practiceTime; // 연습에서만 흐르는 시각
        private float practiceMana = 100f; // 실전 출격과 분리한 연습 마력
        public int CastCount { get; private set; } // 이 발사기에서 성공한 시전 수
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
        }

        internal static bool Supports(CraftedSpellRecipe recipe) // 실물 발사체와 효과를 지원하는 조합
        {
            if (IsPurification(recipe)) // 기획서의 초기 정화 결계
            {
                return true; // 고정 결계로 별도 처리
            }
            return recipe != null && recipe.FireMode == "single" && recipe.Origin == "front" // 현재 발사기에서 처리하는 출발 방식
                && (recipe.Trigger == "immediate" || recipe.Trigger == "hit") && recipe.Aim != "auto" // 입력과 명중 조건
                && (recipe.Shape == "orb" || recipe.Shape == "small" || recipe.Shape == "needle" || recipe.Shape == "arrow" || recipe.Shape == "wave") // 실제 발사체 형상
                && (recipe.Formation == "single" || recipe.Formation == "fan" || recipe.Formation == "cone") // 연습과 전투에서 공유하는 배열
                && (recipe.Trajectory == "straight" || recipe.Trajectory == "homing") // 직선 및 추적
                && SupportsEffect(recipe.FirstEffect) && SupportsEffect(recipe.SecondEffect); // 실제 판정이 있는 효과
        }

        private static bool IsPurification(CraftedSpellRecipe recipe) // 초기 정화 결계의 정확한 구성
        {
            return recipe != null && recipe.Id == CraftedSpellPresets.All[11].Recipe.Id; // 문서의 정화 결계 식별자
        }

        private static bool SupportsEffect(string effect) // 22일차에 실행하는 개별 효과
        {
            return effect == "none" || effect == "explode" || effect == "pierce" || effect == "knockback"; // 직격·폭발·관통
        }

        internal static Profile Stats(CraftedSpellRecipe recipe) // 지원하는 조합의 초기 시험 성능
        {
            if (IsPurification(recipe)) // 고정 정화 결계
            {
                return new Profile(30, 0, 12f, 0f, 0f, 3.5f, 0f); // 마력과 종료 후 대기시간
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
            if (IsPurification(recipe)) // 정화 결계의 고정 설치
            {
                if (practice && practiceMana < 30f) // 독립 연습의 실제 마력 부족
                {
                    return false; // 마력이 없으면 설치하지 않음
                }
                float started = Clock; // 결계 시작 시각
                Barrier barrier = CreateBarrier(practice ? muzzle.position : transform.position, started + 4f); // 시전 위치의 고정 결계
                barriers.Add(barrier); // 수명과 상쇄 한도 관리
                readyAt[recipe.Id] = started + 4f + 12f; // 결계 종료 후 십이 초 대기
                if (practice) // 연습 마력만 독립 차감
                {
                    practiceMana -= 30f; // 연습의 실제 소비
                }
                CastCount++; // 유효한 결계 시전 기록
                return true; // 결계 설치 성공
            }
            float now = Clock; // 이번 시전의 시간
            while (emissions.Count > 0 && emissions.Peek() <= now - 1f) // 최근 1초 이전의 기록 제거
            {
                emissions.Dequeue(); // 끝난 시간 창 정리
            }
            active.RemoveAll(shot => shot == null); // 회수한 발사체는 상한에서 제외
            if (!CraftedSpellRules.CanEmit(recipe.InitialShots, 0, emissions.Count, active.Count)) // 탄수와 활성 상한
            {
                return false; // 일부만 발사되는 상황 방지
            }

            Profile profile = Stats(recipe); // 사거리·회전·마력의 공통 프로필
            if (practice && practiceMana < profile.Mana) // 연습의 실제 소모 마력 검사
            {
                return false; // 연습 마력이 부족하면 발사체 생성 차단
            }
            Quaternion aim = AimRotation(profile.Range); // 카드 조준과 실제 투사체 방향 일치
            bool pressure = recipe.Id == CraftedSpellPresets.All[3].Recipe.Id; // 수압 파동의 시전별 중복 제한
            HashSet<Component> sharedHits = pressure ? new HashSet<Component>() : null; // 같은 파동의 공유 적중 기록
            for (int i = 0; i < recipe.InitialShots; i++) // 검증된 수만큼 새 탄환 생성
            {
                Vector3 direction = CraftedSpellVectors.Direction(recipe, aim, i, profile.Spread); // 재현 가능한 3D 배열
                FlightCraftedProjectile shot = Instantiate(projectile, muzzle.position, Quaternion.LookRotation(direction, aim * Vector3.up), practice ? transform : null); // 월드 비행을 유지하는 공용 발사 프리팹
                shot.Begin(recipe, profile, hitLayers, practice, sharedHits); // 같은 탄속·피해·접촉 처리
                if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Sera) // 세라가 실전에서 발사한 직접 마법
                {
                    shot.SetMeleeSource(weapon.Sword); // 실제 적과의 거리로 제작 마법 피해 조정
                }
                else if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Iris) // 실제 전투에서 이리스가 사용한 제작 마법
                {
                    shot.SetResonance(weapon.Resonance); // 마법 카드의 유효 적중도 공명에 합산
                }
                active.Add(shot); // 동시 활성 개수 검사에 반영
                emissions.Enqueue(now); // 초당 생성 검사에 반영
            }
            readyAt[recipe.Id] = now + profile.Cooldown; // 같은 조합의 양쪽 슬롯과 연결
            if (practice) // 실전은 기존 스킬의 공용 마력 사용
            {
                practiceMana -= profile.Mana; // 독립 연습의 마력 차감
            }
            CastCount++; // 실제 성공한 시전만 집계
            return true; // 소비 가능한 시전
        }

        private Barrier CreateBarrier(Vector3 center, float expires) // 월드에 고정된 결계 표시
        {
            var barrier = new Barrier { center = center, expires = expires }; // 결계의 위치와 수명
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
                    float a = Mathf.Cos(angle) * 3.5f; // 원주의 첫 축
                    float b = Mathf.Sin(angle) * 3.5f; // 원주의 둘째 축
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
                    barrier.cleared += threatPool.CancelWithin(barrier.center, 3.5f, 16 - barrier.cleared); // 허용 적탄만 남은 수까지 상쇄
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
            emissions.Clear(); // 초당 탄수 초기화
            readyAt.Clear(); // 재사용 대기 초기화
            practiceMana = 100f; // 표준 조건의 시험 마력 초기화
            CastCount = 0; // 현재 실행의 시험 통계 초기화
        }

        private void OnDestroy() // 씬 종료 시 결계의 임시 자원 정리
        {
            foreach (Barrier barrier in barriers) // 아직 유지 중인 결계
            {
                DestroyBarrier(barrier); // 남은 표시와 재질 반환
            }
            barriers.Clear(); // 종료한 시전 기록 제거
        }

        private Quaternion AimRotation(float range) // 카메라 중심과 발사 위치의 시차 보정
        {
            if (aimCamera == null) // 마법 연습의 씬 조준 위치
            {
                return muzzle.rotation; // 시험 표적을 향한 발사점
            }
            Ray view = aimCamera.GetAttackRay(); // 실제 화면 중심 조준선
            Vector3 goal = Physics.Raycast(view, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Ignore) ? hit.point : view.GetPoint(range); // 실제 표적 거리 또는 사거리 끝
            Vector3 direction = goal - muzzle.position; // 발사점에서 같은 표적을 향함
            return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : muzzle.rotation; // 발사 방향의 안전한 초기 각도
        }
    }
}
