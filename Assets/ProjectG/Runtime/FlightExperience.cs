using System.Collections.Generic; // 활성 조각과 재사용 오브젝트
using UnityEngine; // 비행 좌표와 월드 드롭

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(115)] // 비행 이동과 적탄 판정 뒤 경험치 갱신
    public sealed class FlightExperience : MonoBehaviour // 이번 출격의 경험치와 떨어지는 조각
    {
        private struct Drop // 활성 상태의 조각 하나
        {
            public Transform view; // 편집된 3D 프리팹의 런타임 표시
            public int reward; // 이 조각이 지급하는 경험치
            public int spawnedFrame; // 조각이 생성된 실제 프레임
            public float timeLeft; // 남은 수명
        }

        [SerializeField] private FlightPilotVitals pilot; // 조각을 획득하는 비행체
        [SerializeField] private FlightCombatHud hud; // 기존 레벨·경험치 표시
        [SerializeField] private FlightUpgradeChoices upgrades; // 자석이 바꾸는 현재 픽업 범위
        [SerializeField] private FlightEnemy[] rewardSources; // 이 씬에서 드롭하는 마물 목록
        [SerializeField] private Transform shardPrefab; // 경험치 조각 모델과 표시 크기
        [SerializeField, Min(0f)] private float fallSpeed = 0.5f; // 공중 드롭의 초당 낙하 거리
        [SerializeField, Min(0.01f)] private float lifetime = 8f; // 미획득 조각 만료 시간
        [SerializeField, Min(0.01f)] private float pickupRadius = 0.9f; // 조각과 비행체의 합산 획득 반경
        [SerializeField] private LayerMask terrain = 1 << 8; // 조각이 쉬어 갈 지형 레이어
        private const float GroundClearance = 0.39f; // 크리스털이 지형에 묻히지 않는 높이
        private readonly List<Drop> active = new List<Drop>(); // 이번 출격의 활성 조각
        private readonly Stack<Transform> idle = new Stack<Transform>(); // 회수된 조각 표시 오브젝트
        private Vector3 previousPilot; // 빠른 비행의 이전 프레임 위치
        private int level = 1; // 전투 시작 레벨
        private int experience; // 현재 레벨에서 누적한 경험치

        public event System.Action<int, int> LeveledUp; // 상승한 수와 새 레벨로 선택 창 예약
        public int Level => level; // 결과에 표시할 마지막 출격 레벨

        private void OnEnable() // 씬에 지정한 마물 처치 연결
        {
            previousPilot = pilot.transform.position; // 첫 프레임의 상대 좌표
            foreach (FlightEnemy source in rewardSources) // 저장된 경험치 공급원
            {
                source.Defeated += Spawn; // 실제 처치 시에만 조각 발생
            }
        }

        private void OnDisable() // 씬 종료 또는 드롭 기능 종료
        {
            foreach (FlightEnemy source in rewardSources) // 씬의 등록된 마물 확인
            {
                if (source != null) // 이미 파괴된 씬 객체 제외
                {
                    source.Defeated -= Spawn; // 경험치 이벤트 참조 해제
                }
            }
        }

        internal void RegisterStorySource(FlightEnemy enemy)
        {
            if (enemy == null || System.Array.IndexOf(rewardSources, enemy) >= 0) return;
            System.Array.Resize(ref rewardSources, rewardSources.Length + 1);
            rewardSources[rewardSources.Length - 1] = enemy;
            if (isActiveAndEnabled) enemy.Defeated += Spawn;
        }

        internal void UnregisterStorySource(FlightEnemy enemy)
        {
            if (enemy != null) enemy.Defeated -= Spawn;
            rewardSources = System.Array.FindAll(rewardSources, value => value != null && value != enemy);
        }

        public void ResetRun() // 시험 출격 재시작 상태
        {
            for (int index = active.Count - 1; index >= 0; index--) // 모든 미획득 조각 회수
            {
                Recycle(index); // 비활성 풀로 반환
            }
            level = 1; // 새 출격의 초기 레벨
            experience = 0; // 이전 출격 경험치 제거
            previousPilot = pilot.transform.position; // 재시작 이동을 획득 경로에서 제외
            ShowProgress(); // 기존 LV·XP 게이지 초기화
        }

        private void Spawn(FlightEnemy enemy) // 마물의 처치 좌표로 3D 조각 생성
        {
            int reward = enemy.ExperienceReward; // 실제 처치 경험치
            if (reward <= 0 || pilot.IsDefeated) // 경험치가 없거나 출격 실패한 상태
            {
                return; // 지급할 수 없는 처치
            }

            Transform view = idle.Count > 0 ? idle.Pop() : Instantiate(shardPrefab, transform); // 이미 회수한 모델 우선
            view.SetPositionAndRotation(enemy.transform.position, Quaternion.identity); // 처치 공간과 기준 회전
            view.gameObject.SetActive(true); // 현재 공중 조각 표시
            active.Add(new Drop { view = view, reward = reward, spawnedFrame = Time.frameCount, timeLeft = lifetime }); // 조각의 보상과 만료 시작
        }

        private void LateUpdate() // 마지막 프레임부터 조각과 기체의 상대 궤적
        {
            if (Time.timeScale == 0f) // 선택 중 드롭과 획득 정지
            {
                return; // 같은 프레임의 추가 경험치 보류
            }
            Vector3 currentPilot = pilot.transform.position; // 이번 프레임 비행 위치
            float frameTime = Time.deltaTime; // 움직일 수 있는 프레임 구간
            for (int index = active.Count - 1; index >= 0; index--) // 회수해도 다음 조각에 영향 없는 역순
            {
                Drop drop = active[index]; // 이 프레임에 진행하는 조각
                Vector3 start = drop.view.position; // 조각의 이전 월드 좌표
                bool justSpawned = drop.spawnedFrame == Time.frameCount; // 같은 프레임의 과거 경로 제외
                float duration = justSpawned ? 0f : Mathf.Min(frameTime, drop.timeLeft); // 수명 중 유효한 이동
                Vector3 end = Fall(start, duration, fallSpeed, terrain); // 지형을 넘지 않는 아래 방향 위치
                float fraction = frameTime > 0f ? duration / frameTime : 1f; // 만료 시점까지 이동한 비율
                Vector3 pilotStart = justSpawned ? currentPilot : previousPilot; // 조각이 없었던 이전 부분 제외
                Vector3 pilotEnd = Vector3.Lerp(pilotStart, currentPilot, fraction); // 조각과 공통으로 존재한 시점
                float reach = pickupRadius * (upgrades != null ? upgrades.PickupScale : 1f); // 자석에 따라 늘어난 회수 반경
                if (!pilot.IsDefeated && WithinSweep(pilotStart - start, pilotEnd - end, reach)) // 빠른 비행에서 놓치지 않는 획득
                {
                    Recycle(index); // 같은 조각의 중복 획득 방지
                    Grant(drop.reward); // 실제 획득의 임시 경험치
                    if (Time.timeScale == 0f) // 레벨업으로 선택 창이 열린 경우
                    {
                        break; // 다음 조각은 전투 재개 뒤 획득
                    }
                    continue; // 활성 조각에서 처리 완료
                }
                drop.timeLeft -= duration; // 이번 프레임에 소모한 수명
                if (drop.timeLeft <= 0f) // 수집 없이 만료
                {
                    Recycle(index); // 조각을 대기 풀로 반환
                    continue; // 만료된 조각 표시 처리 제외
                }

                drop.view.position = end; // 끝 위치에 표시
                drop.view.Rotate(0f, 105f * duration, 0f); // 회수 가능한 크리스털의 회전
                active[index] = drop; // 남은 수명 기록
            }
            previousPilot = currentPilot; // 다음 프레임의 상대 위치 저장
        }

        internal static Vector3 Fall(Vector3 start, float duration, float speed, LayerMask ground) // 경험치와 아이템의 바닥 충돌
        {
            float travel = speed * duration; // 최대 아래 이동 거리
            if (travel > 0f && Physics.Raycast(start, Vector3.down, out RaycastHit surface, travel + GroundClearance, ground, QueryTriggerInteraction.Ignore)) // 지형 표면의 정지 높이
            {
                travel = Mathf.Min(travel, Mathf.Max(0f, surface.distance - GroundClearance)); // 표면에서 살짝 떠서 정지
            }
            return start + Vector3.down * travel; // 세계 아래 방향으로만 이동
        }

        internal static bool WithinSweep(Vector3 start, Vector3 end, float reach) // 상대 이동 선분의 최근접점
        {
            Vector3 travel = end - start; // 기체와 조각의 상대 변화량
            float length = travel.sqrMagnitude; // 나누기에 사용할 제곱 거리
            float fraction = length > 0f ? Mathf.Clamp01(-Vector3.Dot(start, travel) / length) : 0f; // 선분 내부의 가장 가까운 시점
            return (start + travel * fraction).sqrMagnitude <= reach * reach; // 유효 구간의 실제 접촉
        }

        internal bool HasDropWithin(Vector3 center, float radius) // 회수 자석의 유효 후보
        {
            foreach (Drop drop in active) // 아직 만료되지 않은 경험치
            {
                if ((drop.view.position - center).sqrMagnitude <= radius * radius) // 실제 회수 반경
                {
                    return true; // 회수할 경험치 존재
                }
            }
            return false; // 무의미한 회수 자석 제외
        }

        internal void CollectWithin(Vector3 center, float radius) // 하나의 자석에서 경험치 합산
        {
            long reward = 0; // 한 번에 회수할 경험치 합계
            for (int index = active.Count - 1; index >= 0; index--) // 회수 중 조각 중복 방지
            {
                Drop drop = active[index]; // 현 위치의 경험치 조각
                if ((drop.view.position - center).sqrMagnitude <= radius * radius) // 등급별 유효 반경
                {
                    reward += drop.reward; // 각 조각의 가치를 한 번만 포함
                    Recycle(index); // HUD와 선택 전에 실제 조각 회수
                }
            }
            if (reward > 0) // 유효 회수에서만 레벨 반영
            {
                Grant((int)System.Math.Min(reward, int.MaxValue)); // 여러 레벨 선택도 한 번에 예약
            }
        }

        private void Grant(int reward) // 초과 경험치를 다음 전투 레벨에 이월
        {
            FlightAudio.Play(FlightSfx.Pickup); // 실제 경험치 조각 회수 소리
            long total = (long)experience + reward; // 한 조각의 큰 보상도 누락 없이 합산
            int previousLevel = level; // 이번 획득 전 레벨
            while (total >= RequiredExperience) // 한 번에 여러 레벨을 넘는 경우
            {
                total -= RequiredExperience; // 현재 레벨에 사용한 XP
                level++; // 전투 레벨 증가
            }
            experience = (int)total; // 다음 레벨까지 남은 일부 경험치
            ShowProgress(); // 현 레벨과 게이지를 함께 갱신
            if (level > previousLevel) // 실제로 레벨이 오른 횟수
            {
                FlightAudio.Play(FlightSfx.LevelUp); // 선택 화면이 열리기 전의 성장 소리
                LeveledUp?.Invoke(level - previousLevel, level); // 오른 레벨별 선택 하나
            }
        }

        private int RequiredExperience => 10 * level; // 10·20·30 순서의 시험 레벨 비용

        private void ShowProgress() // 전투 HUD에 실제 출격 경험치 전달
        {
            hud.SetProgress(level, experience, RequiredExperience); // 레벨 숫자와 경험치 바 동기화
        }

        private void Recycle(int index) // 획득 또는 만료 시 표시 오브젝트 회수
        {
            Transform view = active[index].view; // 수명이 끝난 조각
            int last = active.Count - 1; // 마지막 활성 항목
            active[index] = active[last]; // 순서를 유지할 필요 없는 풀 목록
            active.RemoveAt(last); // 마지막 항목 제거
            view.gameObject.SetActive(false); // 회수한 조각 숨김
            idle.Push(view); // 이후 처치 보상에 재사용
        }
    }
}
