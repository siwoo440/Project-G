using System.Collections.Generic; // 레이더에 전달할 활성 적 좌표
using UnityEngine; // 일반 마물 시험 씬
using UnityEngine.InputSystem; // 전투 시험 시작 키

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightEnemyEncounter : MonoBehaviour // 연습 또는 스테이지의 실제 적 전투
    {
        [System.Serializable] // 씬에서 웨이브마다 조정할 정보
        private sealed class StageWave // 예정된 출현 시각과 경로형 적
        {
            [Min(0f)] public float startsAt; // 전투 진행 시간 기준 출발 시각
            public FlightEnemyRoute[] enemies; // 이 웨이브의 적 배치 그룹
            public FlightStageBoss boss; // 일반 적 배치 사이 또는 마지막에 등장하는 보스
        }

        [SerializeField] private FlightPilotVitals pilot; // 실제 플레이어 생존 상태
        [SerializeField] private FlightThreatPool pool; // 마물들이 함께 쓰는 적탄 풀
        [SerializeField] private FlightEnemyRoute[] routes; // 씬에서 조정하는 마물 경로
        [SerializeField] private FlightEnemy openingEnemy; // 출격 직후 첫 웨이브의 시험 표적
        [SerializeField] private FlightBindings bindings; // 설정에서 지정한 웨이브 키
        [SerializeField] private StageWave[] stageWaves = System.Array.Empty<StageWave>(); // 실전 항로의 순차 출현 목록
        [SerializeField, Min(0f)] private float stageEndTime = 80f; // 드롭 회수 구간의 시험 종료 시각
        private Vector3 openingPosition; // 재출격할 때 첫 적이 나타날 위치
        private Quaternion openingRotation; // 첫 적의 원래 비행 방향
        private bool stopped; // 완료·실패·포기 뒤의 시간 고정 상태
        private FlightStageBoss activeBoss; // 현재 보스 전투와 격파 알림 대상
        private float phaseStartedAt; // 보스 격파 뒤 후속 일정의 기준 시각

        public int WaveNumber { get; private set; } = 1; // 시작 웨이브 번호
        public float ElapsedSeconds { get; private set; } // 전투 시계의 경과 시간
        public float TotalSeconds { get; private set; } // 메뉴와 카드 선택을 포함한 소요 시간
        public bool IsStageComplete { get; private set; } // 일반 전투 항로의 종료 상태
        public bool IsPreparing => IsStage && !stopped && ElapsedSeconds < stageWaves[0].startsAt; // 첫 적 출현 시점 전의 실제 준비 구간
        public bool IsMidBossActive => activeBoss is FlightMidBoss; // 웨이브 HUD의 중간 보스 표시
        public bool IsFinalBossActive => activeBoss is FlightFinalBoss; // 전용 HUD의 최종 보스 표시
        private bool IsStage => stageWaves.Length > 0; // 실전 웨이브의 씬 등록 여부
        private bool HasOpeningTarget => !IsStage && WaveNumber == 1; // 시험 씬의 시작 표적 표시 여부

        public int ActiveEnemyCount // 현재 웨이브에서 아직 보이는 적 수
        {
            get // 실제 활성 개체만 합산
            {
                if (HasOpeningTarget) // 시작 시험 표적
                {
                    return openingEnemy != null && openingEnemy.gameObject.activeInHierarchy ? 1 : 0; // 처치 후 실제 표시
                }

                int count = activeBoss != null ? 1 : 0; // 보스를 포함한 활성 개체 누계
                foreach (FlightEnemyRoute route in routes) // 씬의 세 경로
                {
                    if (route.gameObject.activeInHierarchy) // 아직 처치·이탈하지 않은 적
                    {
                        count++; // 현재 웨이브에 남아 있는 수
                    }
                }
                return count; // 표시가 참조할 실제 값
            }
        }

        public void CopyActivePositions(List<Vector3> positions) // 레이더에 현재 웨이브의 실제 적 위치 전달
        {
            positions.Clear(); // 이전 웨이브의 표시 제거
            if (HasOpeningTarget) // 시험 전투의 실제 표적
            {
                if (openingEnemy != null && openingEnemy.gameObject.activeInHierarchy) // 처치 전에만 표시
                {
                    positions.Add(openingEnemy.transform.position); // 시작 표적의 현재 월드 좌표
                }
                return; // 초기에는 경로 마물 없음
            }

            if (activeBoss != null) // 현재 활성 보스의 실제 위치
            {
                positions.Add(activeBoss.transform.position); // 레이더 및 전투 목표 갱신
            }
            foreach (FlightEnemyRoute route in routes) // 현재 웨이브의 이동 중인 적
            {
                if (route.gameObject.activeInHierarchy) // 처치·이탈 전의 실제 적
                {
                    positions.Add(route.transform.position); // 레이더와 같은 프레임의 위치
                }
            }
        }

        private void Awake() // 첫 웨이브의 배치 기억
        {
            if (IsStage) // 실전 씬의 준비 상태
            {
                WaveNumber = 0; // 첫 출현 전까지 웨이브 미시작
            }
            else if (openingEnemy != null) // 편집 가능한 시작 표적
            {
                openingPosition = openingEnemy.transform.position; // 초기 등장 위치
                openingRotation = openingEnemy.transform.rotation; // 초기 회전
            }
        }

        private void Update() // 연습 시나리오의 입력 확인
        {
            if (IsStage) // 실전 스테이지의 자동 진행
            {
                UpdateStage(); // 실전 기록과 출현 일정 갱신
                return; // 실전에서 연습 웨이브 키 제외
            }
            if (Time.timeScale == 0f) // 카드 선택 중 연습 진행 동결
            {
                return; // 연습 웨이브를 시작하지 않는 시간
            }
            if (pilot.IsDefeated) // 출격 실패면 전투 시간 중단
            {
                StopTrial(); // 씬의 마물 초기화
                return; // 중단 상태의 시험 시작 차단
            }
            ElapsedSeconds += Time.deltaTime; // 출격 경과 시간 누적
            Keyboard keyboard = Keyboard.current; // 현재 입력 장치
            if (keyboard != null && bindings.Pressed(keyboard, FlightAction.Wave)) // 설정한 시험 웨이브 시작
            {
                StartTrial(); // 실제 출현 위치와 체력 복구
            }
        }

        public void StartTrial() // 실제 마물 출현 동작
        {
            if (IsStage) // 실전 일정에 수동 출현 제외
            {
                return; // 씬에 등록된 자동 출현 유지
            }
            StopTrial(); // 이전 마물의 상태 정리
            WaveNumber++; // 새로운 시험 웨이브 번호
            if (openingEnemy != null) // 시작 표적이 연결된 씬
            {
                openingEnemy.gameObject.SetActive(false); // 현재 웨이브 집계에서 정리
            }
            pool.ReturnAll(); // 이전 연습 탄막 정리
            foreach (FlightEnemyRoute route in routes) // 조준형·확산형·돌진형 순회
            {
                route.Begin(pool, pilot.transform); // 마물의 실제 입체 경로 활성화
            }
        }

        internal void RestartTrial() // 시험 출격을 다시 시작할 때
        {
            StopTrial(); // 이전 웨이브 정리
            WaveNumber = IsStage ? 0 : 1; // 실전 준비 또는 시험 첫 웨이브
            ElapsedSeconds = 0f; // 첫 타이머
            TotalSeconds = 0f; // 전체 소요 시간 초기화
            phaseStartedAt = 0f; // 다음 출격의 최초 절대 시각 기준
            stopped = false; // 새 출격의 자동 진행 재개
            IsStageComplete = false; // 이전 항로의 완료 상태 초기화
            if (openingEnemy != null) // 저장된 시작 표적
            {
                openingEnemy.gameObject.SetActive(false); // 기존 적 체력 초기화 준비
                if (!IsStage) // 시험 씬의 첫 표적 복원
                {
                    openingEnemy.transform.SetPositionAndRotation(openingPosition, openingRotation); // 시작 위치와 방향
                    openingEnemy.gameObject.SetActive(true); // 새 연습의 첫 적 복귀
                }
            }
        }

        internal void StopStage() // 완료·실패·포기 시 실전 출격 닫기
        {
            if (!IsStage || stopped) // 연습 씬 또는 이미 종료된 출격
            {
                return; // 기존 상태 유지
            }
            stopped = true; // 이번 출격의 시간 고정
            StopTrial(); // 남아 있는 적 비활성화
            pool.ReturnAll(); // 이미 발사된 적탄 회수
        }

        private void UpdateStage() // 시간과 전투 배치의 순차 진행
        {
            if (pilot.IsDefeated) // 체력이 모두 소진된 경우
            {
                StopStage(); // 실패 위치에서 시간과 적탄 정리
            }
            if (stopped) // 출격이 종료된 경우
            {
                return; // 최종 시간을 보존
            }
            TotalSeconds += Mathf.Max(Time.unscaledDeltaTime, Time.deltaTime); // 전투와 정지 시간을 모두 포함
            if (Time.timeScale == 0f) // 메뉴와 보상 선택 중
            {
                return; // 전투 시간과 자동 출현은 정지
            }
            ElapsedSeconds += Time.deltaTime; // 실제 전투 진행 시간
            if (ActiveEnemyCount > 0) // 앞 웨이브의 적이 남아 있는 경우
            {
                return; // 진행 중인 적을 중복 출현시키지 않음
            }
            if (WaveNumber < stageWaves.Length) // 전장의 예정된 다음 적 배치
            {
                StageWave next = stageWaves[WaveNumber]; // 현재 번호에 이어지는 웨이브
                if (ElapsedSeconds >= phaseStartedAt + next.startsAt) // 현재 전투 구간의 출현 시간 이후
                {
                    WaveNumber++; // HUD의 새 출현 번호
                    if (next.boss != null) // 일반 적과 분리된 보스 웨이브
                    {
                        pool.ReturnAll(); // 지난 웨이브의 적탄 정리
                        activeBoss = next.boss; // HUD와 레이더가 추적할 보스
                        activeBoss.Enemy.Defeated += OnBossDefeated; // 실제 격파 뒤에만 다음 전투
                        activeBoss.Begin(pool, pilot.transform); // 씬의 진입점에서 보스 출현
                    }
                    foreach (FlightEnemyRoute enemy in next.enemies) // 해당 웨이브의 준비된 적
                    {
                        enemy.Begin(pool, pilot.transform); // 실제 월드 위치에서 등장
                    }
                }
                return; // 추가 웨이브를 같은 프레임에 시작하지 않음
            }
            if (ElapsedSeconds >= phaseStartedAt + stageEndTime) // 마지막 일반 전투와 회수 구간 종료
            {
                IsStageComplete = true; // 시험 항로 완료 표시
                StopStage(); // 두 시계와 잔류 적탄 정리
            }
        }

        private void OnBossDefeated(FlightEnemy enemy) // 보스의 실제 처치 경로
        {
            enemy.Defeated -= OnBossDefeated; // 이번 웨이브의 구독 정리
            activeBoss = null; // 전투 목표와 레이더 해제
            if (enemy.LootTier == FlightLootTier.FinalBoss) // 마지막 관문을 실제로 격파한 경우
            {
                IsStageComplete = true; // 출격 성공과 전투·전체 시각 확정
                StopStage(); // 보스 적탄과 이후 웨이브 종료
                return; // 후속 일반 전투 없음
            }
            phaseStartedAt = ElapsedSeconds; // 보스 격파가 늦어져도 이후 시간은 격파 기준
            pool.ReturnAll(); // 보스가 남긴 적탄 회수
        }

        private void OnDestroy() // 씬 이동 전 남은 보스 구독 정리
        {
            if (activeBoss != null) // 전투 종료 전에 씬이 닫히는 경우
            {
                activeBoss.Enemy.Defeated -= OnBossDefeated; // 보스의 이벤트 연결 해제
            }
        }

        private void StopTrial() // 모든 마물 코스 중단
        {
            if (activeBoss != null) // 재출격 또는 포기할 때 남은 보스
            {
                activeBoss.Enemy.Defeated -= OnBossDefeated; // 포기를 처치로 처리하지 않음
                activeBoss.gameObject.SetActive(false); // 남은 보스와 약점의 표시 제거
                activeBoss = null; // 다음 출격의 보스 참조 초기화
            }
            foreach (FlightEnemyRoute route in routes) // 표시 중인 시험 마물 확인
            {
                route.gameObject.SetActive(false); // 마물의 피해와 이동을 일시 종료
            }
        }
    }
}
