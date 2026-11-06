using System; // 숫자와 시드 기능
using System.Globalization; // 일관된 숫자 표시
using System.Text; // 측정 기록 작성
using UnityEngine; // 유니티 환경과 전투 기능
using UnityEngine.Rendering; // 그래픽 장치 구분
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BattleMeasurementRecorder : MonoBehaviour // 한 출격의 측정 수집기
    { // 수집기 시작
        private BattlePlayer player; // 현재 전투 플레이어
        private StageDirector stage; // 현재 스테이지 제어
        private StageProgression observedState; // 알림을 받는 진행 상태
        private EnemyBulletPool pool; // 활성 적탄 풀
        private int runSeed; // 현재 출격의 재현 시드
        private int peakEnemyProjectiles; // 출격 중 관측한 최대 적탄 수
        private int? lastObservedBossPhase; // 마지막으로 확인한 최종 보스 단계
        private Func<MeasurementContext> environmentProvider; // 격리 가능한 표시 환경 조회
        private readonly MeasurementContext currentEnvironment = new MeasurementContext(); // 매 프레임 재사용할 표시 조건
        private readonly CombatMeasurementSnapshot snapshot = new CombatMeasurementSnapshot(); // 매 프레임 재사용할 전투 기록
        private GraphicsDeviceType lastDeviceType; // 마지막으로 확인한 그래픽 장치 종류
        private bool hasDeviceType; // 장치 종류 초기 조회 여부
        private string lastGraphicsApi; // 재사용할 그래픽 API 이름
        public PlayMeasurement Measurement // 현재 출격의 측정 기록
        { // 속성 시작
            get; // 측정 기록 조회
            private set; // 측정 기록 변경
        } // 속성 종료
        public void Begin(BattlePlayer target, StageDirector director, EnemyBulletPool bulletPool, int seed, Func<MeasurementContext> observedEnvironment = null) // 전투와 측정 환경 연결
        { // 연결 시작
            Disconnect(); // 이전 출격 연결 해제
            player = target; // 플레이어 보관
            stage = director; // 스테이지 보관
            pool = bulletPool; // 적탄 풀 보관
            runSeed = seed; // 출격 시드 보관
            environmentProvider = observedEnvironment; // 선택적인 환경 조회 연결
            if (player != null) // 플레이어 연결 확인
            { // 연결 시작
                player.BattleRestarted += StartNewRun; // 새 출격 초기화 연결
            } // 연결 종료
            StartNewRun(); // 첫 출격 측정 시작
        } // 연결 종료
        private void StartNewRun() // 독립 출격 측정 초기화
        { // 초기화 시작
            peakEnemyProjectiles = 0; // 이전 출격의 최대 적탄 수 초기화
            lastObservedBossPhase = null; // 이전 출격의 보스 단계 초기화
            ObserveCurrentState(); // 새 스테이지 알림 연결
            Measurement = new PlayMeasurement(CaptureEnvironment()); // 실제 환경의 새 측정 생성
            RefreshSnapshot(); // 출격 초기 상태 기록
        } // 초기화 종료
        private MeasurementContext CaptureEnvironment() // 현재 측정 환경 수집
        { // 수집 시작
            if (environmentProvider != null) // 격리된 환경 제공 확인
            { // 시험 환경 시작
                MeasurementContext provided = environmentProvider(); // 주입된 환경 조회
                MeasurementContext copy = provided != null ? provided.Copy() : new MeasurementContext(); // 외부 수정 방지 사본
                copy.Character = player != null ? (CharacterType?)player.Character : null; // 실제 선택 캐릭터
                copy.Seed = runSeed; // 재현 가능한 출격 시드
                return copy; // 격리된 환경 반환
            } // 시험 환경 종료
            MeasurementContext context = new MeasurementContext(); // 환경 기록 생성
            context.Source = Application.isBatchMode ? MeasurementSource.Automation : MeasurementSource.Unknown; // 입력 출처를 확인된 수준으로 표시
            context.RenderDeviceAvailable = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null; // 실제 그래픽 장치 여부
            context.Character = player != null ? (CharacterType?)player.Character : null; // 실제 선택 캐릭터
            context.Seed = runSeed; // 재현 가능한 출격 시드
            context.Width = Screen.width > 0 ? (int?)Screen.width : null; // 확인된 화면 가로 크기
            context.Height = Screen.height > 0 ? (int?)Screen.height : null; // 확인된 화면 세로 크기
            context.VerticalSync = QualitySettings.vSyncCount > 0; // 현재 수직 동기화 설정
            context.FrameLimit = Application.targetFrameRate; // 현재 프레임 제한 설정
            context.Cpu = SystemInfo.processorType; // 확인된 CPU 정보
            context.Gpu = SystemInfo.graphicsDeviceName; // 확인된 그래픽 장치 정보
            context.MemoryMegabytes = SystemInfo.systemMemorySize > 0 ? (int?)SystemInfo.systemMemorySize : null; // 확인된 시스템 메모리
            context.GraphicsApi = SystemInfo.graphicsDeviceType.ToString(); // 현재 그래픽 장치 종류
            context.OperatingSystem = SystemInfo.operatingSystem; // 현재 운영체제
            context.UnityVersion = Application.unityVersion; // 현재 유니티 버전
            context.Stage = "하늘섬 외곽 시험 스테이지"; // 현재 시험 스테이지
            return context; // 수집한 환경 반환
        } // 수집 종료
        private MeasurementContext CurrentDisplayEnvironment() // 현재 표시 설정 조회
        { // 조회 시작
            if (environmentProvider != null) // 주입된 환경 확인
            { // 격리 조회 시작
                return environmentProvider(); // 시험에서 제공한 조건
            } // 격리 조회 종료
            GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType; // 실제 그래픽 장치 종류
            if (!hasDeviceType || lastDeviceType != deviceType) // API 이름 갱신 필요 확인
            { // 장치 갱신 시작
                hasDeviceType = true; // 장치 확인 기록
                lastDeviceType = deviceType; // 현재 장치 종류 보관
                lastGraphicsApi = deviceType.ToString(); // 현재 그래픽 API 이름
            } // 장치 갱신 종료
            currentEnvironment.Width = Screen.width > 0 ? (int?)Screen.width : null; // 현재 가로 해상도
            currentEnvironment.Height = Screen.height > 0 ? (int?)Screen.height : null; // 현재 세로 해상도
            currentEnvironment.VerticalSync = QualitySettings.vSyncCount > 0; // 현재 수직 동기화 설정
            currentEnvironment.FrameLimit = Application.targetFrameRate; // 현재 FPS 제한 설정
            currentEnvironment.RenderDeviceAvailable = deviceType != GraphicsDeviceType.Null; // 현재 장치 사용 여부
            currentEnvironment.Gpu = SystemInfo.graphicsDeviceName; // 현재 그래픽 장치 이름
            currentEnvironment.GraphicsApi = lastGraphicsApi; // 현재 그래픽 API 이름
            return currentEnvironment; // 재사용한 현재 조건 반환
        } // 조회 종료
        private void Update() // 매 프레임 측정 갱신
        { // 갱신 시작
            ObserveCurrentState(); // 재출격 상태 교체 확인
            RefreshSnapshot(); // 실제 전투 기록 갱신
            if (Measurement != null) // 측정 상태 확인
            { // 표시 조건 확인 시작
                Measurement.ObserveEnvironment(CurrentDisplayEnvironment()); // 정지 중에도 표시 조건 변경 감지
            } // 표시 조건 확인 종료
            if (Measurement == null || !Application.isPlaying || Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Time.timeScale <= 0f || player == null || player.Survival == null || !player.Survival.IsAlive || player.Statistics == null || player.Statistics.Finished || stage == null || stage.State == null || stage.State.Phase == StagePhase.Result || stage.State.Phase == StagePhase.Failed) // 실제 렌더 전투 프레임 조건
            { // 제외 시작
                return; // 비대화·정지·종료 표본 제외
            } // 제외 종료
            Measurement.RecordFrame(Time.unscaledDeltaTime, true); // 실제 프레임 초 기록
        } // 갱신 종료
        private void ObserveCurrentState() // 스테이지 상태의 이벤트 전환
        { // 연결 시작
            StageProgression current = stage != null ? stage.State : null; // 현재 진행 상태 조회
            if (ReferenceEquals(observedState, current)) // 기존 연결 확인
            { // 유지 시작
                return; // 동일 상태 재연결 생략
            } // 유지 종료
            if (observedState != null) // 이전 단계 확인
            { // 해제 시작
                observedState.PhaseChanged -= OnPhaseChanged; // 이전 단계 이벤트 해제
            } // 해제 종료
            observedState = current; // 현재 단계 보관
            if (observedState != null) // 새 단계 확인
            { // 연결 처리 시작
                observedState.PhaseChanged += OnPhaseChanged; // 새 단계 이벤트 연결
            } // 연결 처리 종료
        } // 연결 종료
        private void OnPhaseChanged(StagePhase phase) // 실제 스테이지 전환 알림
        { // 처리 시작
            RefreshSnapshot(); // 결과 화면 전 최신 통계 반영
        } // 처리 종료
        public void RefreshSnapshot() // 현재 출격 통계 기록
        { // 기록 시작
            if (Measurement == null || player == null || player.Statistics == null || stage == null || stage.State == null) // 필수 기록 확인
            { // 생략 시작
                return; // 초기화 전 기록 생략
            } // 생략 종료
            RunStatistics stats = player.Statistics; // 현재 출격 통계
            StageProgression progress = stage.State; // 현재 스테이지 상태
            snapshot.CombatSeconds = stats.Finished ? stats.CombatSeconds : progress.CombatSeconds; // 확정 또는 진행 중 전투 시간
            snapshot.TotalSeconds = stats.Finished ? stats.TotalSeconds : progress.TotalSeconds; // 확정 또는 진행 중 전체 시간
            snapshot.CardsSelected = stats.Cards.Count; // 실제 카드 선택 수
            snapshot.UltimateActivations = stats.Ultimates; // 실제 필살기 발동 수
            snapshot.ItemDrops = stats.DropsSpawned; // 실제 아이템 드롭 수
            snapshot.ItemPickups = stats.DropsAcquired; // 실제 아이템 획득 수
            snapshot.EnemyDefeats = stats.Defeats; // 실제 적 처치 수
            snapshot.Hits = stats.Hits; // 실제 체력 피해 수
            snapshot.Grazes = stats.Grazes; // 실제 그레이즈 수
            snapshot.MidBossDefeats = stats.MidBossCleared ? 1 : 0; // 실제 중간 보스 돌파 수
            snapshot.FinalBossDefeats = stats.FinalBossCleared ? 1 : 0; // 실제 최종 보스 돌파 수
            if (stage.Boss != null && stage.Boss.Rank == EnemyRank.FinalBoss) // 최종 보스 관측 확인
            { // 단계 기록 시작
                lastObservedBossPhase = stage.Boss.PatternIndex + 1; // 마지막으로 확인한 단계 보관
            } // 단계 기록 종료
            snapshot.BossPhase = lastObservedBossPhase; // 보스 제거 후에도 마지막 관측 보존
            snapshot.CurrentStagePhase = progress.Phase.ToString(); // 현재 스테이지 구간
            snapshot.ActiveEnemyProjectiles = pool != null ? pool.ActiveCount : 0; // 활성 적탄 개수
            peakEnemyProjectiles = Math.Max(peakEnemyProjectiles, snapshot.ActiveEnemyProjectiles); // 현재까지의 최대 적탄 수 갱신
            snapshot.PeakEnemyProjectiles = peakEnemyProjectiles; // 출격 중 최대 적탄 수 기록
            Measurement.UpdateCombat(snapshot); // 유효 전투 스냅샷 기록
        } // 기록 종료
        public string BuildReport() // 현재 측정값의 읽기 쉬운 보고
        { // 보고 시작
            if (Measurement == null) // 측정 시작 여부 확인
            { // 미측정 시작
                return "전투 측정: 미측정"; // 없는 기록 안내
            } // 미측정 종료
            MeasurementContext context = Measurement.Context; // 독립 환경 정보
            CombatMeasurementSnapshot combat = Measurement.Combat; // 독립 전투 정보
            RunStatistics stats = player != null ? player.Statistics : null; // 현재 출격 원본 통계
            StringBuilder report = new StringBuilder(); // 보고 문자열 준비
            report.Append("입력 출처: ").Append(context.Source == MeasurementSource.Automation ? "자동 시험" : "미확인"); // 확인된 입력 분류
            report.Append("\n출격 시드: ").Append(context.Seed.HasValue ? context.Seed.Value.ToString(CultureInfo.InvariantCulture) : "미확인"); // 재현 시드 표시
            report.Append("\n캐릭터: ").Append(context.Character.HasValue ? context.Character.Value.ToString() : "미확인"); // 선택 캐릭터 표시
            report.Append("\n스테이지: ").Append(string.IsNullOrWhiteSpace(context.Stage) ? "미확인" : context.Stage); // 확인된 스테이지 표시
            report.Append(" / 종료 구간: ").Append(combat != null && !string.IsNullOrWhiteSpace(combat.CurrentStagePhase) ? combat.CurrentStagePhase : "미확인"); // 기록된 마지막 구간 표시
            report.Append(" / 보스 단계: ").Append(combat != null && combat.BossPhase.HasValue ? combat.BossPhase.Value.ToString(CultureInfo.InvariantCulture) : "미확인"); // 마지막 관측 단계 또는 미확인 표시
            report.Append("\n화면: ").Append(context.Width.HasValue && context.Height.HasValue ? context.Width.Value + " × " + context.Height.Value : "미확인"); // 화면 크기 표시
            report.Append(" / 그래픽 장치: ").Append(context.RenderDeviceAvailable == true ? "사용 가능" : "미사용 또는 미확인"); // 렌더 장치 상태
            report.Append("\nCPU: ").Append(string.IsNullOrEmpty(context.Cpu) ? "미확인" : context.Cpu); // CPU 환경 표시
            report.Append(" / GPU: ").Append(string.IsNullOrEmpty(context.Gpu) ? "미확인" : context.Gpu); // GPU 환경 표시
            report.Append(" / 메모리: ").Append(context.MemoryMegabytes.HasValue ? context.MemoryMegabytes.Value + " MB" : "미확인"); // 시스템 메모리 표시
            report.Append("\n운영체제: ").Append(string.IsNullOrEmpty(context.OperatingSystem) ? "미확인" : context.OperatingSystem); // 운영체제 표시
            report.Append(" / Unity: ").Append(string.IsNullOrEmpty(context.UnityVersion) ? "미확인" : context.UnityVersion); // 엔진 버전 표시
            report.Append(" / 그래픽 API: ").Append(string.IsNullOrEmpty(context.GraphicsApi) ? "미확인" : context.GraphicsApi); // 그래픽 API 표시
            report.Append("\n수직 동기화: ").Append(context.VerticalSync == true ? "켜기" : context.VerticalSync == false ? "끄기" : "미확인"); // 동기화 설정 표시
            report.Append(" / FPS 설정: ").Append(context.FrameLimit.HasValue ? context.FrameLimit.Value.ToString(CultureInfo.InvariantCulture) : "미확인"); // 프레임 설정 표시
            if (Measurement.EnvironmentChanged) // 측정 중 표시 조건 변경 확인
            { // 변경 안내 시작
                report.Append("\n측정 중 화면 설정 또는 그래픽 장치 변경: 전체 출격 성능 미측정, 변경 전 표본만 표시"); // 혼합 조건과 불완전성 표시
            } // 변경 안내 종료
            if (Measurement.FrameCount > 0) // 실제 프레임 표본 확인
            { // 성능 표시 시작
                report.Append(Measurement.EnvironmentChanged ? "\n변경 전 구간 프레임 표본: " : "\n전투 프레임 표본: ").Append(Measurement.FrameCount); // 측정 조건에 따른 표본 구간
                report.Append(" / 평균 FPS: ").Append(Measurement.AverageFps.Value.ToString("0.0", CultureInfo.InvariantCulture)); // 실제 평균 FPS
                report.Append(" / 최저 FPS: ").Append(Measurement.MinimumFps.Value.ToString("0.0", CultureInfo.InvariantCulture)); // 실제 최저 FPS
                report.Append(" / 평균 프레임: ").Append(Measurement.AverageFrameMilliseconds.Value.ToString("0.00", CultureInfo.InvariantCulture)).Append(" ms"); // 평균 프레임 시간
                report.Append(" / 최장 프레임: ").Append(Measurement.MaximumFrameMilliseconds.Value.ToString("0.00", CultureInfo.InvariantCulture)).Append(" ms"); // 최장 프레임 시간
            } // 성능 표시 종료
            else // 표본이 없는 상태
            { // 미측정 표시 시작
                report.Append("\n렌더링 FPS·프레임 시간: 미측정"); // 추정값 없는 성능 표시
            } // 미측정 표시 종료
            if (combat != null) // 실제 전투 기록 확인
            { // 통계 표시 시작
                report.Append("\n전투 시간: ").Append(combat.CombatSeconds.ToString("0.0", CultureInfo.InvariantCulture)).Append("초 / 전체 시간: ").Append(combat.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)).Append("초"); // 시간 구분 표시
                report.Append("\n카드 ").Append(combat.CardsSelected).Append(" / 필살기 ").Append(combat.UltimateActivations).Append(" / 적 처치 ").Append(combat.EnemyDefeats); // 전투 행동 수 표시
                report.Append("\n드롭 ").Append(combat.ItemDrops).Append(" / 획득 ").Append(combat.ItemPickups).Append(" / 피격 ").Append(combat.Hits).Append(" / 그레이즈 ").Append(combat.Grazes); // 아이템과 생존 기록
                report.Append("\n중간 보스: ").Append(stats != null && stats.MidBossEncountered ? stats.MidBossCleared ? "돌파" : "등장" : "미등장"); // 중간 보스 진행 표시
                report.Append(" / 최종 보스: ").Append(stats != null && stats.FinalBossEncountered ? stats.FinalBossCleared ? "돌파" : "등장" : "미등장"); // 최종 보스 진행 표시
                report.Append(" / 활성 적탄: ").Append(combat.ActiveEnemyProjectiles); // 실제 활성 적탄 표시
                report.Append(" / 관측 최대 활성 적탄: ").Append(combat.PeakEnemyProjectiles); // 출격 중 관측 최대 적탄 표시
                if (stats != null) // 드롭 세부 기록 확인
                { // 세부 표시 시작
                    report.Append("\n드롭 시도: ").Append(stats.DropAttempts).Append(" / 만료: ").Append(stats.DropsExpired).Append(" / 출격 종료 폐기: ").Append(stats.DropsDiscarded); // 실제 드롭 결과 합계
                    foreach (DropRecord drop in stats.Drops) // 드롭 종류와 등급 순회
                    { // 세부 기록 시작
                        report.Append("\n아이템: ").Append(drop.Id).Append(" / 등급: ").Append(drop.Grade).Append(" / 결과: ").Append(drop.Outcome); // 추첨과 획득 결과
                    } // 세부 기록 종료
                } // 세부 표시 종료
            } // 통계 표시 종료
            return report.ToString(); // 실제 기록의 보고 반환
        } // 보고 종료
        private void Disconnect() // 기존 전투 알림 해제
        { // 해제 시작
            if (player != null) // 플레이어 존재 확인
            { // 연결 해제 시작
                player.BattleRestarted -= StartNewRun; // 재출격 알림 해제
            } // 연결 해제 종료
            if (observedState != null) // 이전 진행 상태 확인
            { // 상태 해제 시작
                observedState.PhaseChanged -= OnPhaseChanged; // 구간 변경 알림 해제
            } // 상태 해제 종료
            observedState = null; // 진행 상태 참조 해제
            player = null; // 플레이어 참조 해제
            stage = null; // 스테이지 참조 해제
            pool = null; // 적탄 풀 참조 해제
            environmentProvider = null; // 주입된 환경 조회 해제
        } // 해제 종료
        private void OnDestroy() // 수집기 제거 처리
        { // 제거 시작
            Disconnect(); // 남은 전투 알림 해제
        } // 제거 종료
    } // 수집기 종료
} // 영역 종료
