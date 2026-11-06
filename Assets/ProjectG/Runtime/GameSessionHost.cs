using System; // 출격 식별자 기능
using System.Collections.Generic; // 미저장 보고서 대기열
using UnityEngine; // 유니티 장면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class GameSessionHost : MonoBehaviour // 메뉴와 출격 장면 연결
    { // 클래스 시작
        private sealed class BatchMemoryStorage : IProgressStorage // 배치 시험 전용 저장소
        { // 저장소 시작
            private string contents; // 시험 저장 내용
            public ProgressReadStatus Read(out string data, out string error) // 시험 내용 읽기
            { // 읽기 시작
                data = contents; // 현재 내용 반환
                error = null; // 읽기 오류 없음
                return contents == null ? ProgressReadStatus.Missing : ProgressReadStatus.Found; // 내용 존재 상태
            } // 읽기 종료
            public bool Write(string expectedData, string data, out string error) // 시험 내용 비교 저장
            { // 저장 시작
                if (contents != expectedData) // 기존 내용 변경 확인
                { // 충돌 처리 시작
                    error = "저장 내용이 변경되었습니다."; // 변경 충돌 안내
                    return false; // 다른 내용 덮어쓰기 차단
                } // 충돌 처리 종료
                contents = data; // 메모리 내용 보관
                error = null; // 저장 오류 없음
                return true; // 저장 성공
            } // 저장 종료
        } // 저장소 종료
        public static Func<IProgressStorage> StorageFactoryOverride; // 자동 시험 저장소 주입
        public static Func<ITutorialStateStore> TutorialStorageFactoryOverride; // 안내 저장소 시험 주입
        public Func<string, bool> MeasurementWriterOverride; // 격리된 측정 보고 시험 연결
        private readonly Queue<string> pendingMeasurementReports = new Queue<string>(); // 순서대로 보관할 미저장 측정
        public int PendingMeasurementCount => pendingMeasurementReports.Count; // 저장 대기 보고서 수
        private Camera menuCamera; // 메뉴 카메라
        private string currentRunId; // 현재 출격 식별자
        private string pendingStartId; // 저장 재시도 출격 식별자
        private bool ownsProgress; // 기본 저장 서비스 소유 여부
        private bool progressLoaded; // 영구 성장 복원 상태
        private bool recoveryChecked; // 미정산 출격 복구 상태
        private bool progressAttempted; // 메뉴 초기 복원 시도 상태
        private string progressError; // 영구 성장 복원 오류
        public PermanentProgressService Progress // 영구 성장 서비스
        { // 속성 시작
            get; // 서비스 조회
            private set; // 서비스 지정
        } // 속성 종료
        public SettlementReceipt CurrentReceipt // 현재 정산 영수증
        { // 속성 시작
            get; // 영수증 조회
            private set; // 영수증 지정
        } // 속성 종료
        private GameObject battleRoot; // 현재 출격 객체 모음
        private PauseSettingsOverlay overlay; // 일시정지와 설정 화면
        private ITutorialStateStore tutorialStore; // 세션별 안내 상태 저장소
        public GameFlowController Flow // 현재 화면 흐름
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public BattlePlayer CurrentPlayer // 현재 출격 플레이어
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public StageDirector CurrentStage // 현재 출격 스테이지
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public void Begin(Camera camera) // 기본 저장소로 초기화
        { // 초기화 시작
            Begin(camera, null); // 기본 영구 저장소 연결
        } // 초기화 종료
        public void Begin(Camera camera, PermanentProgressService progress) // 메뉴와 저장소 초기화
        { // 초기화 시작
            if (Flow != null || camera == null) // 중복 초기화 확인
            { // 차단 시작
                return; // 기존 화면 유지
            } // 차단 종료
            menuCamera = camera; // 메뉴 카메라 보관
            Progress = progress; // 주입된 서비스 보관
            overlay = gameObject.AddComponent<PauseSettingsOverlay>(); // 설정 화면 구성
            overlay.Begin(null, menuCamera, null, null); // 메뉴 설정 연결
            Flow = gameObject.AddComponent<GameFlowController>(); // 화면 흐름 구성
            Flow.Begin(menuCamera); // 타이틀 표시
            Flow.SettlementRequested = SettleBattle; // 결과 저장 요청 연결
            Flow.ProgressDescription = DescribeProgress; // 선택 캐릭터 성장 표시 연결
            Flow.MeasurementRetryRequested = RetryMeasurementReports; // 측정 저장 재시도 연결
            Flow.RunRequested += StartBattle; // 출격 요청 연결
            Flow.LeaveBattleRequested += ClearBattle; // 전투 정리 요청 연결
            Flow.SettingsRequested += OpenSettings; // 메뉴 설정 연결
            overlay.RetryRequested += RetryBattle; // 일시정지 재시도 연결
            overlay.ExitRequested += ReturnToPreparation; // 일시정지 복귀 연결
            if (progress != null) // 명시적 서비스 주입 확인
            { // 복원 시작
                EnsureProgress(); // 주입 저장소 즉시 복원
            } // 복원 종료
        } // 초기화 종료
        private void Update() // 모달 입력 상태 갱신
        { // 갱신 시작
            if (Flow != null && overlay != null) // 화면 연결 확인
            { // 입력 처리 시작
                Flow.InputBlocked = overlay.IsPaused; // 설정 중 메뉴 입력 차단
            } // 입력 처리 종료
        } // 갱신 종료
        private void StartBattle(CharacterType character, CommonSkill skill) // 선택한 조건으로 출격
        { // 출격 시작
            if (!EnsureProgress()) // 기존 진행과 미정산 복원 확인
            { // 출격 차단 시작
                Flow.ReportStartError(progressError); // 저장 오류와 준비 화면 표시
                return; // 기존 진행 데이터 보존
            } // 출격 차단 종료
            bool hasRunHistory = Progress.HasRunHistory; // 새 출격 저장 이전 이력
            if (string.IsNullOrEmpty(pendingStartId)) // 새 출격 식별자 확인
            { // 식별 시작
                pendingStartId = Guid.NewGuid().ToString("N"); // 중복 없는 출격 식별자
            } // 식별 종료
            if (!Progress.TryBegin(pendingStartId, character, out string error)) // 출격 시작 기록 저장
            { // 저장 실패 시작
                Flow.ReportStartError(error); // 오류와 준비 화면 표시
                return; // 전투 생성 차단
            } // 저장 실패 종료
            currentRunId = pendingStartId; // 저장된 출격 식별자 확정
            pendingStartId = null; // 다음 출격 식별자 준비
            CurrentReceipt = null; // 이전 영수증 초기화
            ClearBattle(); // 이전 출격 정리
            battleRoot = new GameObject("ProjectG Battle"); // 출격 객체 모음
            battleRoot.transform.SetParent(transform, false); // 메뉴 장면에 연결
            Camera camera = battleRoot.AddComponent<Camera>(); // 전투 카메라 생성
            camera.CopyFrom(menuCamera); // 메뉴 카메라 화면 설정 복사
            camera.enabled = true; // 전투 카메라 표시
            GameObject player = BattleSetup.Create(camera); // 전투 구성 생성
            player.transform.SetParent(battleRoot.transform, true); // 출격 객체에 연결
            CurrentPlayer = player.GetComponent<BattlePlayer>(); // 전투 플레이어 조회
            CurrentPlayer.Begin(new Vector2(0f, -3f), character); // 선택 캐릭터 출격
            CurrentPlayer.BindAchievementRun(currentRunId); // 저장된 출격의 독립 과제 기록
            CurrentPlayer.Skills.Equip(skill); // 선택 공용 스킬 장착
            CurrentPlayer.AllowDebugCharacterSwitch = false; // 출격 중 시험 캐릭터 전환 차단
            BattleProgress progress = player.GetComponent<BattleProgress>(); // 성장 상태 조회
            UpgradeItemSelection selection = battleRoot.GetComponent<UpgradeItemSelection>(); // 강화 선택 조회
            CurrentStage = battleRoot.GetComponent<StageDirector>(); // 스테이지 조회
            overlay.Begin(CurrentPlayer, camera, progress, selection); // 전투 일시정지 연결
            CurrentPlayer.SetBindings(overlay.Bindings); // 공유 키 설정 적용
            ITutorialStateStore tutorialState = GetTutorialStore(); // 별도 안내 상태 조회
            if (tutorialState != null) // 안내 저장소 확인
            { // 안내 시작
                battleRoot.AddComponent<TutorialGuide>().Begin(CurrentPlayer, camera, progress, selection, overlay, tutorialState, hasRunHistory); // 실제 첫 출격 안내 연결
            } // 안내 종료
            Flow.AttachBattle(CurrentPlayer, CurrentStage); // 결과 화면 연결
            menuCamera.enabled = false; // 메뉴 카메라 숨김
        } // 출격 종료
        private ITutorialStateStore GetTutorialStore() // 주입 또는 별도 안내 저장소
        { // 조회 시작
            if (tutorialStore != null) // 기존 세션 저장소 확인
            { // 재사용 시작
                return tutorialStore; // 동일 저장소 재사용
            } // 재사용 종료
            try // 저장소 생성 보호
            { // 생성 범위 시작
                tutorialStore = TutorialStorageFactoryOverride != null ? TutorialStorageFactoryOverride() : Application.isEditor && Application.isBatchMode || StorageFactoryOverride != null ? (ITutorialStateStore)new MemoryTutorialStateStore() : new PlayerPrefsTutorialStateStore(); // 시험 격리와 실제 설정 분리
                return tutorialStore; // 생성한 저장소 전달
            } // 생성 범위 종료
            catch (Exception) // 저장소 생성 오류
            { // 오류 시작
                return null; // 기존 전투 보존
            } // 오류 종료
        } // 조회 종료
        private bool EnsureProgress() // 영구 성장 로드와 중단 출격 복구
        { // 복원 시작
            progressAttempted = true; // 초기 복원 시도 기록
            if (Progress == null) // 서비스 생성 확인
            { // 생성 시작
                IProgressStorage storage = StorageFactoryOverride != null ? StorageFactoryOverride() : Application.isEditor && Application.isBatchMode ? (IProgressStorage)new BatchMemoryStorage() : new FileProgressStorage(); // 시험 주입과 실제 저장소 선택
                if (storage == null) // 시험 저장소 확인
                { // 오류 시작
                    progressError = "저장소를 준비하지 못했습니다."; // 저장소 오류 안내
                    return false; // 저장소 오류 차단
                } // 오류 종료
                Progress = new PermanentProgressService(storage); // 영구 성장 서비스 생성
                ownsProgress = true; // 생성 서비스 소유 기록
            } // 생성 종료
            if (!progressLoaded) // 기존 저장 로드 확인
            { // 로드 시작
                progressLoaded = Progress.Load(out progressError); // 저장 데이터 읽기
            } // 로드 종료
            if (!progressLoaded) // 읽기 실패 확인
            { // 차단 시작
                return false; // 기존 데이터 보존
            } // 차단 종료
            if (!recoveryChecked) // 미정산 출격 확인
            { // 복구 시작
                if (!Progress.TryRecoverPending(out SettlementReceipt recovered, out progressError)) // 참여 보상 복구 저장
                { // 복구 실패 시작
                    progressLoaded = false; // 다음 요청에 저장 내용 재확인
                    return false; // 출격 차단과 재시도 허용
                } // 복구 실패 종료
                recoveryChecked = true; // 미정산 확인 완료
                if (recovered != null) // 복구된 출격 확인
                { // 복구 안내 시작
                    Flow.SetRecoveryNotice("이전 출격 실패 정산 복구: 숙련도 +" + recovered.MasteryGained + " · 공통 재료 +" + recovered.CommonMaterialsGained + " · 전용 재료 +" + recovered.CharacterMaterialsGained); // 실제 복구 보상 안내
                } // 복구 안내 종료
            } // 복구 종료
            progressError = null; // 이전 저장 오류 해제
            return true; // 진행 데이터 사용 가능
        } // 복원 종료
        private bool SettleBattle(bool cleared) // 현재 출격 정산
        { // 정산 시작
            if (CurrentPlayer == null || CurrentStage == null || string.IsNullOrEmpty(currentRunId)) // 출격 데이터 확인
            { // 정산 차단 시작
                Flow.ReportSettlementError("정산할 출격 정보를 찾을 수 없습니다."); // 오류 원인 표시
                return false; // 출격 데이터 보존
            } // 정산 차단 종료
            if (CurrentPlayer.Statistics != null && !CurrentPlayer.Statistics.Finished) // 중도 종료 전투 기록 확인
            { // 기록 확정 시작
                CurrentPlayer.Statistics.Complete(false, CurrentStage.State.CombatSeconds, CurrentStage.State.TotalSeconds); // 포기 출격 실패 기록
            } // 기록 확정 종료
            bool resultPhase = CurrentStage.State != null && (CurrentStage.State.Phase == StagePhase.Result || CurrentStage.State.Phase == StagePhase.Failed); // 실제 결과 구간 확인
            RapidAchievementRun evidence = resultPhase ? CurrentPlayer.AchievementRun : null; // 중도 포기 과제 지급 제외
            if (!Progress.TrySettle(currentRunId, CurrentPlayer.Character, cleared, evidence, out SettlementReceipt receipt, out string error)) // 결과와 지급 내역 저장
            { // 저장 실패 시작
                Flow.ReportSettlementError(error); // 오류 내용 표시
                return false; // 현재 전투 유지
            } // 저장 실패 종료
            CurrentReceipt = receipt; // 저장된 영수증 보관
            Flow.SetReceipt(receipt); // 결과 화면 영수증 연결
            if (!receipt.AlreadySettled) // 최초 정산 확인
            { // 보고 시작
                WriteMeasurementReport(); // 출격 측정 기록 저장
            } // 보고 종료
            return true; // 정산 완료
        } // 정산 종료
        private void WriteMeasurementReport() // 실제 출격 측정 보고 저장
        { // 저장 시작
            if (!Application.isPlaying || CurrentPlayer == null) // 실제 출격 상태 확인
            { // 생략 시작
                return; // 편집 또는 누락된 출격 제외
            } // 생략 종료
            BattleMeasurementRecorder recorder = CurrentPlayer.GetComponent<BattleMeasurementRecorder>(); // 출격 측정 수집기
            if (recorder == null) // 수집기 확인
            { // 생략 시작
                return; // 없는 측정 기록 제외
            } // 생략 종료
            recorder.RefreshSnapshot(); // 정산된 출격의 최종 통계
            string report = recorder.BuildReport(); // 실제 수집값 보고
            if (MeasurementWriterOverride == null && (Application.isBatchMode || StorageFactoryOverride != null || !ownsProgress)) // 격리 시험 중 실제 파일 접근 확인
            { // 격리 시작
                return; // 명시적 시험 기록기가 없는 사용자 파일 접근 차단
            } // 격리 종료
            pendingMeasurementReports.Enqueue(report); // 원본 보고를 출격 순서로 보관
            RetryMeasurementReports(); // 기존 보고부터 저장 시도
        } // 저장 종료
        public bool RetryMeasurementReports() // 미저장 보고서 순서대로 재시도
        { // 재시도 시작
            while (pendingMeasurementReports.Count > 0) // 대기 보고 확인
            { // 저장 반복 시작
                string report = pendingMeasurementReports.Peek(); // 가장 오래된 원본 조회
                if (!TryWriteMeasurement(report, out string error)) // 실제 저장 결과 확인
                { // 저장 실패 시작
                    if (Flow != null) // 화면 연결 확인
                    { // 안내 시작
                        Flow.SetMeasurementNotice("측정 보고 저장 실패 " + pendingMeasurementReports.Count + "건 · Tab 재시도 · 종료 시 대기 기록 삭제"); // 실패와 임시 보관 안내
                    } // 안내 종료
                    Debug.LogWarning(error); // 저장 실패 원인 기록
                    return false; // 실패 보고와 이후 순서 보존
                } // 저장 실패 종료
                pendingMeasurementReports.Dequeue(); // 저장에 성공한 보고만 제거
            } // 저장 반복 종료
            if (Flow != null) // 화면 연결 확인
            { // 안내 해제 시작
                Flow.SetMeasurementNotice(""); // 저장 성공 후 오류 해제
            } // 안내 해제 종료
            return true; // 모든 대기 보고 저장 완료
        } // 재시도 종료
        private bool TryWriteMeasurement(string report, out string error) // 격리 또는 실제 보고서 저장
        { // 저장 시작
            error = null; // 이전 오류 비움
            try // 저장 경로 확인
            { // 저장 범위 시작
                if (MeasurementWriterOverride != null) // 명시적 시험 저장소 확인
                { // 시험 저장 시작
                    if (MeasurementWriterOverride(report)) // 격리 저장 성공 확인
                    { // 성공 시작
                        return true; // 시험 보고 저장 성공
                    } // 성공 종료
                    error = "측정 보고서 저장 실패"; // 시험 저장 실패 안내
                    return false; // 미저장 원본 보존
                } // 시험 저장 종료
                if (Application.isBatchMode || StorageFactoryOverride != null || !ownsProgress) // 자동 시험의 실제 파일 접근 확인
                { // 격리 시작
                    error = "격리 시험에서 실제 측정 파일 접근 차단"; // 격리 실패 사유
                    return false; // 실제 사용자 파일 보호
                } // 격리 종료
                string directory = System.IO.Path.Combine(Application.persistentDataPath, "ProjectGMeasurements"); // 별도 측정 보고 폴더
                if (!MeasurementReportWriter.TryWrite(directory, report, out string path, out error)) // 새 보고서 생성 확인
                { // 실패 시작
                    return false; // 보고 대기열 유지
                } // 실패 종료
                Debug.Log("전투 측정 기록: " + path); // 확인 가능한 저장 위치
                return true; // 실제 보고 저장 성공
            } // 저장 범위 종료
            catch (Exception exception) // 시험 저장기 또는 경로 오류
            { // 오류 처리 시작
                error = "측정 보고서 저장 실패: " + exception.Message; // 오류 사유 기록
                return false; // 정산과 미저장 보고 보존
            } // 오류 처리 종료
        } // 저장 종료
        private string DescribeProgress(CharacterType character) // 선택 캐릭터 성장 요약
        { // 요약 시작
            if (!progressAttempted) // 초기 저장 복원 확인
            { // 복원 시작
                EnsureProgress(); // 준비 화면에서 기존 성장 확인
            } // 복원 종료
            if (!progressLoaded || !recoveryChecked) // 저장 데이터와 중단 출격 복원 확인
            { // 오류 표시 시작
                return "영구 성장 복구 대기: " + progressError; // 수치 임의 표시 차단
            } // 오류 표시 종료
            string achievement = character == CharacterType.Rapid ? "\n아리아 첫 과제: " + (Progress.HasAchievement(CharacterAchievement.RapidBasicHits50) ? "완료" : "미완료") : ""; // 저장된 아리아 과제 상태
            return "숙련도 " + Progress.Mastery(character) + "  전용 재료 " + Progress.CharacterMaterials(character) + "  공통 재료 " + Progress.CommonMaterials + "\n후속 해금 선택지 미정" + achievement; // 실제 성장과 과제 표시
        } // 요약 종료
        private void OpenSettings() // 타이틀 설정 열기
        { // 설정 시작
            if (overlay != null) // 설정 화면 확인
            { // 열기 시작
                overlay.OpenSettingsFromMenu(); // 설정 화면 표시
            } // 열기 종료
        } // 설정 종료
        private void RetryBattle() // 일시정지 재시도
        { // 재시도 시작
            Flow?.RetryBattle(); // 확인된 출격 재시작
        } // 재시도 종료
        private void ReturnToPreparation() // 일시정지에서 준비 복귀
        { // 복귀 시작
            Flow?.ReturnFromBattle(); // 확인된 준비 화면 복귀
        } // 복귀 종료
        private void ClearBattle() // 이전 출격 정리
        { // 정리 시작
            if (overlay != null) // 일시정지 화면 확인
            { // 일시정지 정리 시작
                overlay.Resume(); // 소유한 시간 배율 복원
                overlay.Begin(null, menuCamera, null, null); // 메뉴 설정으로 복귀
            } // 일시정지 정리 종료
            if (CurrentStage != null) // 스테이지 존재 확인
            { // 스테이지 정리 시작
                CurrentStage.Shutdown(); // 전투 이벤트와 안전 상태 정리
            } // 스테이지 정리 종료
            CurrentPlayer = null; // 플레이어 참조 해제
            CurrentStage = null; // 스테이지 참조 해제
            if (battleRoot != null) // 출격 객체 확인
            { // 출격 정리 시작
                battleRoot.SetActive(false); // 같은 프레임 전투 중지
                if (Application.isPlaying) // 플레이 상태 확인
                { // 지연 정리 시작
                    Destroy(battleRoot); // 프레임 종료 후 제거
                } // 지연 정리 종료
                else // 편집 시험 상태
                { // 즉시 정리 시작
                    DestroyImmediate(battleRoot); // 시험 객체 즉시 제거
                } // 즉시 정리 종료
                battleRoot = null; // 출격 객체 참조 해제
            } // 출격 정리 종료
            if (menuCamera != null) // 메뉴 카메라 확인
            { // 메뉴 복귀 시작
                menuCamera.enabled = true; // 메뉴 카메라 표시
            } // 메뉴 복귀 종료
        } // 정리 종료
        private void OnDestroy() // 화면 연결 제거
        { // 제거 시작
            if (Flow != null) // 화면 흐름 확인
            { // 이벤트 해제 시작
                Flow.RunRequested -= StartBattle; // 출격 요청 해제
                Flow.LeaveBattleRequested -= ClearBattle; // 전투 정리 해제
                Flow.SettingsRequested -= OpenSettings; // 설정 요청 해제
                Flow.SettlementRequested = null; // 정산 요청 해제
                Flow.ProgressDescription = null; // 성장 표시 연결 해제
                Flow.MeasurementRetryRequested = null; // 측정 재시도 연결 해제
            } // 이벤트 해제 종료
            if (overlay != null) // 일시정지 화면 확인
            { // 이벤트 해제 시작
                overlay.RetryRequested -= RetryBattle; // 재시도 연결 해제
                overlay.ExitRequested -= ReturnToPreparation; // 복귀 연결 해제
            } // 이벤트 해제 종료
            ClearBattle(); // 출격 객체 정리
            if (ownsProgress && Progress != null) // 소유한 저장 서비스 확인
            { // 저장소 정리 시작
                Progress.Dispose(); // 세션 저장 잠금 해제
                ownsProgress = false; // 서비스 소유 해제
            } // 저장소 정리 종료
        } // 제거 종료
    } // 클래스 종료
} // 영역 종료
