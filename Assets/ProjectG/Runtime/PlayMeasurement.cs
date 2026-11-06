using System; // 숫자 검증 기능
using System.Collections.Generic; // 프레임 표본 목록
using System.Collections.ObjectModel; // 읽기 전용 표본
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum MeasurementSource // 시험 입력 종류
    { // 종류 시작
        Unknown, // 출처 미확인
        HumanPlay, // 실제 사용자 플레이
        Automation // 자동 시뮬레이션
    } // 종류 종료
    public sealed class MeasurementContext // 실제 확인한 측정 환경
    { // 환경 시작
        public MeasurementSource Source; // 측정 입력 출처
        public bool? RenderDeviceAvailable; // 그래픽 장치 사용 여부
        public CharacterType? Character; // 측정 캐릭터
        public int? Seed; // 재현용 난수 시드
        public int? Width; // 화면 가로 픽셀
        public int? Height; // 화면 세로 픽셀
        public bool? VerticalSync; // 수직 동기화 상태
        public int? FrameLimit; // 초당 프레임 제한
        public string Cpu; // 확인된 CPU 이름
        public string Gpu; // 확인된 GPU 이름
        public int? MemoryMegabytes; // 확인된 시스템 메모리
        public string GraphicsApi; // 확인된 그래픽 API
        public string OperatingSystem; // 확인된 운영체제
        public string UnityVersion; // 확인된 유니티 버전
        public string Stage; // 측정 스테이지 이름
        public string Notes; // 측정 조건 추가 설명
        public MeasurementContext Copy() // 환경 자료 독립 복사
        { // 복사 시작
            return (MeasurementContext)MemberwiseClone(); // 입력 값의 독립 사본
        } // 복사 종료
    } // 환경 종료
    public sealed class CombatMeasurementSnapshot // 한 시점의 전투 기록
    { // 기록 시작
        public double CombatSeconds; // 일시정지 제외 전투 시간
        public double TotalSeconds; // 일시정지 포함 전체 시간
        public int CardsSelected; // 선택한 카드 횟수
        public int UltimateActivations; // 사용한 필살기 횟수
        public int ItemDrops; // 생성된 강화 아이템 횟수
        public int ItemPickups; // 획득한 강화 아이템 횟수
        public int EnemyDefeats; // 처치한 적 총 횟수
        public int Hits; // 체력 피해를 받은 횟수
        public int Grazes; // 그레이즈 횟수
        public int MidBossDefeats; // 중간 보스 처치 횟수
        public int FinalBossDefeats; // 최종 보스 처치 횟수
        public int? BossPhase; // 확인된 보스 단계
        public string CurrentStagePhase; // 확인된 스테이지 구간
        public int ActiveEnemyProjectiles; // 현재 활성 적탄 수
        public int PeakEnemyProjectiles; // 출격 중 관측한 최대 활성 적탄 수
        public CombatMeasurementSnapshot Copy() // 전투 기록 독립 복사
        { // 복사 시작
            return (CombatMeasurementSnapshot)MemberwiseClone(); // 입력 값의 독립 사본
        } // 복사 종료
        internal void CopyFrom(CombatMeasurementSnapshot source) // 검증된 전투 기록 복사
        { // 복사 시작
            CombatSeconds = source.CombatSeconds; // 전투 시간 복사
            TotalSeconds = source.TotalSeconds; // 전체 시간 복사
            CardsSelected = source.CardsSelected; // 카드 선택 수 복사
            UltimateActivations = source.UltimateActivations; // 필살기 수 복사
            ItemDrops = source.ItemDrops; // 드롭 수 복사
            ItemPickups = source.ItemPickups; // 획득 수 복사
            EnemyDefeats = source.EnemyDefeats; // 처치 수 복사
            Hits = source.Hits; // 피격 수 복사
            Grazes = source.Grazes; // 그레이즈 수 복사
            MidBossDefeats = source.MidBossDefeats; // 중간 보스 기록 복사
            FinalBossDefeats = source.FinalBossDefeats; // 최종 보스 기록 복사
            BossPhase = source.BossPhase; // 보스 단계 복사
            CurrentStagePhase = source.CurrentStagePhase; // 스테이지 구간 복사
            ActiveEnemyProjectiles = source.ActiveEnemyProjectiles; // 활성 적탄 수 복사
            PeakEnemyProjectiles = source.PeakEnemyProjectiles; // 최대 적탄 수 복사
        } // 복사 종료
    } // 기록 종료
    public sealed class PlayMeasurement // 자동 또는 실제 플레이 측정 모델
    { // 측정 시작
        private readonly MeasurementContext context; // 확인된 측정 환경
        private readonly List<double> frameSeconds = new List<double>(); // 유효한 원시 프레임 초
        private readonly ReadOnlyCollection<double> frameView; // 수정할 수 없는 표본 목록
        private CombatMeasurementSnapshot combat; // 마지막 유효 전투 기록
        private double totalFrameSeconds; // 유효 표본의 총 실제 초
        private double longestFrameSeconds; // 가장 긴 유효 프레임 초
        public PlayMeasurement(MeasurementContext environment) // 측정 상태 생성
        { // 생성 시작
            context = environment != null ? environment.Copy() : new MeasurementContext(); // 입력 환경의 독립 사본
            frameView = frameSeconds.AsReadOnly(); // 외부 수정 방지 목록
        } // 생성 종료
        public MeasurementContext Context => context.Copy(); // 보존된 환경의 독립 사본
        public CombatMeasurementSnapshot Combat => combat != null ? combat.Copy() : null; // 기록이 있을 때만 독립 사본
        public IReadOnlyList<double> FrameSeconds => frameView; // 초 단위 원시 표본
        public int FrameCount => frameSeconds.Count; // 실제 유효 표본 수
        public bool EnvironmentChanged // 측정 중 표시 조건 변경 여부
        { // 속성 시작
            get; // 변경 여부 조회
            private set; // 변경 여부 기록
        } // 속성 종료
        public double? AverageFps => FrameCount > 0 ? (double?)FrameCount / totalFrameSeconds : null; // 표본 수와 실제 총 시간의 비율
        public double? MinimumFps => FrameCount > 0 ? (double?)(1d / longestFrameSeconds) : null; // 가장 긴 프레임의 역수
        public double? AverageFrameMilliseconds => FrameCount > 0 ? (double?)(totalFrameSeconds / FrameCount * 1000d) : null; // 평균 프레임 밀리초
        public double? MaximumFrameMilliseconds => FrameCount > 0 ? (double?)(longestFrameSeconds * 1000d) : null; // 최장 프레임 밀리초
        public bool RecordFrame(double elapsedSeconds, bool combatRunning) // 실제 프레임 초 기록
        { // 기록 시작
            if (EnvironmentChanged || !combatRunning || !ValidSeconds(elapsedSeconds) || elapsedSeconds == 0d || double.IsInfinity(1d / elapsedSeconds) || double.IsInfinity(elapsedSeconds * 1000d) || double.IsInfinity(totalFrameSeconds + elapsedSeconds)) // 조건 변경과 비유효 표본 확인
            { // 제외 시작
                return false; // 미측정 프레임 제외
            } // 제외 종료
            frameSeconds.Add(elapsedSeconds); // 초 단위 원시 표본 보관
            totalFrameSeconds += elapsedSeconds; // 실제 경과 시간 합산
            longestFrameSeconds = Math.Max(longestFrameSeconds, elapsedSeconds); // 최장 유효 프레임 갱신
            return true; // 유효 표본 기록
        } // 기록 종료
        public bool ObserveEnvironment(MeasurementContext current) // 측정 중 표시 조건 검사
        { // 검사 시작
            if (EnvironmentChanged) // 이미 변경된 출격 확인
            { // 중단 유지 시작
                return false; // 원래 설정 복귀에도 표본 혼합 차단
            } // 중단 유지 종료
            if (current == null || current.Width != context.Width || current.Height != context.Height || current.VerticalSync != context.VerticalSync || current.FrameLimit != context.FrameLimit || current.RenderDeviceAvailable != context.RenderDeviceAvailable || !string.Equals(current.Gpu, context.Gpu, StringComparison.Ordinal) || !string.Equals(current.GraphicsApi, context.GraphicsApi, StringComparison.Ordinal)) // 표시 조건 변경 확인
            { // 변경 기록 시작
                EnvironmentChanged = true; // 이후 표본 중단 상태 보관
                return false; // 서로 다른 조건 혼합 차단
            } // 변경 기록 종료
            return true; // 출격 시작과 같은 표시 조건
        } // 검사 종료
        public bool UpdateCombat(CombatMeasurementSnapshot snapshot) // 실제 전투 스냅샷 반영
        { // 반영 시작
            if (snapshot == null || !ValidSeconds(snapshot.CombatSeconds) || !ValidSeconds(snapshot.TotalSeconds) || snapshot.CardsSelected < 0 || snapshot.UltimateActivations < 0 || snapshot.ItemDrops < 0 || snapshot.ItemPickups < 0 || snapshot.EnemyDefeats < 0 || snapshot.Hits < 0 || snapshot.Grazes < 0 || snapshot.MidBossDefeats < 0 || snapshot.FinalBossDefeats < 0 || (snapshot.BossPhase.HasValue && snapshot.BossPhase.Value < 0) || snapshot.ActiveEnemyProjectiles < 0 || snapshot.PeakEnemyProjectiles < snapshot.ActiveEnemyProjectiles) // 시간과 현재·최대 횟수 범위 확인
            { // 거부 시작
                return false; // 기존 측정 기록 유지
            } // 거부 종료
            if (combat == null) // 첫 유효 기록 확인
            { // 저장소 준비 시작
                combat = new CombatMeasurementSnapshot(); // 출격별 독립 저장소 생성
            } // 저장소 준비 종료
            combat.CopyFrom(snapshot); // 검증된 입력을 기존 저장소에 복사
            return true; // 유효 스냅샷 반영
        } // 반영 종료
        private static bool ValidSeconds(double value) // 시간 값의 유효 범위
        { // 검사 시작
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d; // 음수와 비유효 수 제외
        } // 검사 종료
    } // 측정 종료
} // 영역 종료
