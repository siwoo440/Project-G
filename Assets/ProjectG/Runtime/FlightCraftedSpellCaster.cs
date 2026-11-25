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

        [SerializeField] private FlightCraftedProjectile projectile; // 제작 마법의 공유 프리팹
        [SerializeField] private Transform muzzle; // 이 장면의 실제 발사 위치
        [SerializeField] private LayerMask hitLayers = (1 << 8) | (1 << 9); // 지형과 표적 레이어
        [SerializeField] private bool practice; // 일시정지 속에서 독립적인 연습 시간
        [SerializeField] private FirstFlightCamera aimCamera; // 실전 화면의 조준 기준
        private FlightWeapon weapon; // 전투에서 처음 시전한 조종사의 근거리 판정
        private readonly Queue<float> emissions = new(); // 최근 1초의 생성 시간
        private readonly List<FlightCraftedProjectile> active = new(); // 살아 있는 발사체
        private readonly Dictionary<string, float> readyAt = new(); // 같은 조합 두 장이 공유하는 대기시간
        private float practiceTime; // 연습에서만 흐르는 시각
        private float practiceMana = 100f; // 실전 출격과 분리한 연습 마력
        public int CastCount { get; private set; } // 이 발사기에서 성공한 시전 수
        internal int PracticeMana => Mathf.FloorToInt(practiceMana); // 연습 화면의 현재 실제 마력
        private float Clock => practice ? practiceTime : Time.time; // 발사기별 독립 시계
        internal static float PracticeDelta => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime; // 검증과 실제 연습의 렌더 시간

        private void Awake() // 발사기와 같은 실제 전투 비행체
        {
            TryGetComponent(out weapon); // 독립 제작 시험장에서는 비어 있는 실제 무기
        }

        private void Update() // 실전이 멈춰도 독립적으로 흐르는 연습
        {
            if (practice) // 전투 시간과 연습 전용 시간 분리
            {
                practiceTime += PracticeDelta; // 연습 재사용 시간만 갱신
                practiceMana = Mathf.Min(100f, practiceMana + 10f * PracticeDelta); // 문서의 초당 연습 마력 회복
            }
        }

        internal static bool Supports(CraftedSpellRecipe recipe) // 실물 발사체와 효과를 지원하는 조합
        {
            return recipe != null && recipe.FireMode == "single" && recipe.Origin == "front" // 현재 발사기에서 처리하는 출발 방식
                && (recipe.Trigger == "immediate" || recipe.Trigger == "hit") && recipe.Aim != "auto" // 입력과 명중 조건
                && (recipe.Shape == "orb" || recipe.Shape == "small" || recipe.Shape == "needle" || recipe.Shape == "arrow" || recipe.Shape == "wave") // 실제 발사체 형상
                && (recipe.Formation == "single" || recipe.Formation == "fan" || recipe.Formation == "cone") // 연습과 전투에서 공유하는 배열
                && (recipe.Trajectory == "straight" || recipe.Trajectory == "homing") // 직선 및 추적
                && SupportsEffect(recipe.FirstEffect) && SupportsEffect(recipe.SecondEffect); // 실제 판정이 있는 효과
        }

        private static bool SupportsEffect(string effect) // 22일차에 실행하는 개별 효과
        {
            return effect == "none" || effect == "explode" || effect == "pierce"; // 직격·폭발·관통
        }

        internal static Profile Stats(CraftedSpellRecipe recipe) // 지원하는 조합의 초기 시험 성능
        {
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
            for (int i = 0; i < recipe.InitialShots; i++) // 검증된 수만큼 새 탄환 생성
            {
                Vector3 direction = CraftedSpellVectors.Direction(recipe, aim, i, profile.Spread); // 재현 가능한 3D 배열
                FlightCraftedProjectile shot = Instantiate(projectile, muzzle.position, Quaternion.LookRotation(direction, aim * Vector3.up), practice ? transform : null); // 월드 비행을 유지하는 공용 발사 프리팹
                shot.Begin(recipe, profile, hitLayers, practice); // 같은 탄속·피해·접촉 처리
                if (!practice && weapon != null && weapon.ActivePilot == FlightBroomPilot.Sera) // 세라가 실전에서 발사한 직접 마법
                {
                    shot.SetMeleeSource(weapon.Sword); // 실제 적과의 거리로 제작 마법 피해 조정
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
            emissions.Clear(); // 초당 탄수 초기화
            readyAt.Clear(); // 재사용 대기 초기화
            practiceMana = 100f; // 표준 조건의 시험 마력 초기화
            CastCount = 0; // 현재 실행의 시험 통계 초기화
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
