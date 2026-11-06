using System; // 저장 예외 처리
using UnityEngine; // 화면과 객체 기능
using UnityEngine.InputSystem; // 건너뛰기 입력
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class TutorialGuide : MonoBehaviour // 실제 전투 안내
    { // 안내 시작
        private BattlePlayer player; // 현재 출격 플레이어
        private Camera battleCamera; // 전투 화면 기준
        private BattleProgress progress; // 카드 선택 상태
        private UpgradeItemSelection selection; // 강화 선택 상태
        private PauseSettingsOverlay overlay; // 일시정지 상태
        private ITutorialStateStore store; // 격리 가능한 안내 저장소
        private TutorialSequence sequence; // 시험용 단계 순서
        private int lastTransitionFrame = -1; // 같은 프레임 중복 단계 방지
        private GUIStyle textStyle; // 한글 안내 글자
        private Font font; // 한글 시스템 글꼴
        public TutorialStep Current => sequence == null ? TutorialStep.Finished : sequence.Current; // 현재 안내 단계
        public bool IsShowing => sequence != null && sequence.Current != TutorialStep.Finished; // 안내 표시 상태
        public bool Begin(BattlePlayer target, Camera camera, BattleProgress cardProgress, UpgradeItemSelection itemSelection, PauseSettingsOverlay pause, ITutorialStateStore stateStore, bool hasHistory) // 실제 첫 출격 안내 연결
        { // 연결 시작
            Disconnect(); // 이전 이벤트 해제
            if (target == null || camera == null || stateStore == null) // 필수 참조 확인
            { // 차단 시작
                return false; // 전투 영향 없이 안내 생략
            } // 차단 종료
            TutorialSaveState state; // 이전 안내 상태
            try // 저장 조회 보호
            { // 조회 시작
                if (!stateStore.TryRead(out state)) // 저장 조회 실패
                { // 실패 시작
                    return false; // 기존 사용자 안내 오인 방지
                } // 실패 종료
            } // 조회 종료
            catch (Exception) // 저장소 오류
            { // 예외 시작
                return false; // 전투 영향 없이 생략
            } // 예외 종료
            if (state == TutorialSaveState.Finished || (state == TutorialSaveState.Unseen && hasHistory)) // 종료 또는 기존 이용자 확인
            { // 생략 시작
                return false; // 기존 출격 안내 생략
            } // 생략 종료
            player = target; // 플레이어 연결
            battleCamera = camera; // 카메라 연결
            progress = cardProgress; // 카드 선택 연결
            selection = itemSelection; // 강화 선택 연결
            overlay = pause; // 일시정지 연결
            store = stateStore; // 저장소 연결
            sequence = new TutorialSequence(); // 첫 단계 준비
            lastTransitionFrame = -1; // 새 출격의 단계 프레임 초기화
            player.TutorialAction += Observe; // 실제 행동 구독
            Save(TutorialSaveState.Started); // 중단 후 안내 재개 기록
            return true; // 안내 시작
        } // 연결 종료
        public void Skip() // 사용자의 안내 건너뛰기
        { // 건너뛰기 시작
            if (!CanInteract()) // 진행 가능한 전투 확인
            { // 차단 시작
                return; // 일시정지 중 변경 차단
            } // 차단 종료
            sequence.Skip(); // 안내 종료
            Save(TutorialSaveState.Finished); // 종료 상태 저장
        } // 건너뛰기 종료
        private void Observe(TutorialSignal signal) // 실제 행동 수신
        { // 수신 시작
            if (!CanInteract() || Time.frameCount == lastTransitionFrame) // 모달과 중복 단계 확인
            { // 차단 시작
                return; // 정지 중 신호 무시
            } // 차단 종료
            if (!sequence.Observe(signal)) // 현재 단계의 실제 행동 확인
            { // 무관한 행동 시작
                return; // 다른 단계 신호 무시
            } // 무관한 행동 종료
            lastTransitionFrame = Time.frameCount; // 이번 프레임의 단계 완료 기록
            if (sequence.Current == TutorialStep.Finished) // 마지막 실제 행동 확인
            { // 완료 시작
                Save(TutorialSaveState.Finished); // 완료 상태 기록
            } // 완료 종료
        } // 수신 종료
        private bool CanInteract() // 현재 안내 반응 가능 여부
        { // 확인 시작
            return IsShowing && player != null && player.Survival != null && player.Survival.IsAlive && Time.timeScale > 0f && (overlay == null || !overlay.IsPaused) && (progress == null || (progress.Offer == null && !progress.Blocked)) && (selection == null || !selection.IsOpen); // 생존 전투와 모달 상태
        } // 확인 종료
        private void Save(TutorialSaveState state) // 저장 실패 격리
        { // 저장 시작
            try // 저장 보호 범위
            { // 보호 시작
                store?.TryWrite(state); // 안내 전용 상태 기록
            } // 보호 종료
            catch (Exception) // 저장소 오류
            { // 예외 시작
            } // 전투 보존
        } // 저장 종료
        private void Update() // 건너뛰기 입력 확인
        { // 입력 시작
            if (CanInteract() && Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame) // 고정 건너뛰기 키
            { // 건너뛰기 시작
                Skip(); // 전체 안내 종료
            } // 건너뛰기 종료
        } // 입력 종료
        public string Message() // 현재 행동과 설정 키 안내
        { // 안내 시작
            if (player == null || sequence == null) // 연결 확인
            { // 빈 안내 시작
                return ""; // 표시 내용 없음
            } // 빈 안내 종료
            switch (sequence.Current) // 현재 단계 확인
            { // 단계 선택 시작
                case TutorialStep.Move: return "이동: " + player.ControlLabel(PlayerAction.MoveUp) + " / " + player.ControlLabel(PlayerAction.MoveLeft) + " / " + player.ControlLabel(PlayerAction.MoveDown) + " / " + player.ControlLabel(PlayerAction.MoveRight); // 실제 이동 키
                case TutorialStep.Focus: return "집중 공격: " + player.ControlLabel(PlayerAction.Focus) + (PlayerPrefs.GetInt("ProjectG.FocusToggle", 0) == 1 ? " 눌러 전환" : " 누르고 유지"); // 집중 방식과 실제 키
                case TutorialStep.Skill: return "스킬 사용: " + player.ControlLabel(PlayerAction.Unique) + " 또는 " + player.ControlLabel(PlayerAction.Common); // 실제 스킬 키
                case TutorialStep.Graze: return "그레이즈: 탄환을 스치며 피하기"; // 근접 회피 안내
                case TutorialStep.Ultimate: return "에너지 100 충전 후 " + player.ControlLabel(PlayerAction.Ultimate) + " 필살기 사용"; // 실제 필살기 키
                default: return ""; // 완료 단계 안내 없음
            } // 단계 선택 종료
        } // 안내 종료
        public static Rect PanelRect(Rect view) // 스테이지 문구 아래 안내 위치
        { // 계산 시작
            float top = Mathf.Max(120f, view.height * 0.13f); // 사건 안내 아래 여백 확보
            return new Rect(view.x + view.width * 0.30f, view.y + top, view.width * 0.40f, view.height * 0.11f); // 전투 중앙 상단 영역
        } // 계산 종료
        private void OnGUI() // 전투 안내 표시
        { // 표시 시작
            if (!CanInteract() || battleCamera == null) // 실제 전투 화면 확인
            { // 생략 시작
                return; // 정지와 모달에서 표시 생략
            } // 생략 종료
            if (textStyle == null) // 한글 글꼴 준비
            { // 생성 시작
                string[] names = new string[] // 시스템 글꼴 후보
                { // 후보 시작
                    "Malgun Gothic", // 윈도우 한글 글꼴
                    "Arial" // 기본 대체 글꼴
                }; // 후보 종료
                font = Font.CreateDynamicFontFromOSFont(names, 18); // 시스템 한글 글꼴
                textStyle = new GUIStyle(GUI.skin.label); // 안내 글자 스타일
                textStyle.font = font != null ? font : GUI.skin.font; // 한글 글꼴 적용
                textStyle.normal.textColor = Color.white; // 흰색 글자
                textStyle.alignment = TextAnchor.MiddleCenter; // 중앙 정렬
                textStyle.wordWrap = true; // 긴 키 이름 줄바꿈
            } // 생성 종료
            Rect pixel = battleCamera.pixelRect; // 실제 전투 화면 영역
            Rect view = new Rect(pixel.xMin, Screen.height - pixel.yMax, pixel.width, pixel.height); // 화면 좌표 변환
            Rect panel = PanelRect(view); // 스테이지 안내와 분리된 영역
            float alpha = HudOcclusion.Alpha(panel, HudOcclusion.PlayerRect(battleCamera, player)); // 플레이어 겹침 감쇄
            Color previous = GUI.color; // 이전 화면 색상
            GUI.color = new Color(1f, 1f, 1f, alpha); // 겹침 투명도 적용
            GUI.Box(panel, GUIContent.none); // 안내 배경
            textStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height * 0.022f)); // 화면별 글자 크기
            GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, panel.width - 16f, panel.height * 0.52f), Message(), textStyle); // 현재 단계와 조작 표시
            textStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(view.height * 0.017f)); // 건너뛰기 글자 크기
            GUI.Label(new Rect(panel.x + 8f, panel.y + panel.height * 0.52f, panel.width - 16f, panel.height * 0.36f), "Backspace: 안내 건너뛰기", textStyle); // 항상 사용 가능한 건너뛰기
            GUI.color = previous; // 기존 화면 색상 복원
        } // 표시 종료
        private void Disconnect() // 이전 출격 연결 해제
        { // 해제 시작
            if (player != null) // 이전 플레이어 확인
            { // 해제 시작
                player.TutorialAction -= Observe; // 행동 구독 제거
            } // 해제 종료
            player = null; // 플레이어 참조 정리
            sequence = null; // 단계 상태 정리
        } // 해제 종료
        private void OnDisable() // 출격 중지 시 정리
        { // 정리 시작
            Disconnect(); // 구독 해제
        } // 정리 종료
        private void OnDestroy() // 객체 파괴 시 정리
        { // 정리 시작
            Disconnect(); // 중복 없는 구독 해제
            if (font != null) // 생성 글꼴 확인
            { // 해제 시작
                Destroy(font); // 런타임 글꼴 정리
            } // 해제 종료
        } // 정리 종료
    } // 안내 종료
} // 영역 종료
