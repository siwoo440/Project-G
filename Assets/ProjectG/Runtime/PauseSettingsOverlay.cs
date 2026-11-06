using System; // 화면 전환 알림
using UnityEngine; // 유니티 화면 기능
using UnityEngine.InputSystem; // 키보드와 포인터 입력
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class PauseSettingsOverlay : MonoBehaviour // 일시정지와 조작 설정
    { // 구성 요소 시작
        private enum Page // 표시 화면 종류
        { // 종류 시작
            Pause, Settings, Bindings, Display, ConfirmRetry, ConfirmExit // 일시정지와 설정 화면
        } // 종류 종료
        private BattlePlayer player; // 전투 플레이어
        private Camera battleCamera; // 전투 화면 카메라
        private BattleProgress progress; // 카드 선택 상태
        private UpgradeItemSelection upgrades; // 강화 선택 상태
        private GameSettings settings; // 조작 설정 저장소
        private Texture2D frame; // 화면 배경 이미지
        private GUIStyle titleStyle; // 제목 글꼴
        private GUIStyle labelStyle; // 선택지 글꼴
        private Page page; // 현재 화면
        private int selected; // 키보드 선택 항목
        private float oldTimeScale; // 기존 전투 시간 배율
        private bool fromMenu; // 메뉴 설정 진입 여부
        private int openedFrame = -1; // 화면을 연 프레임
        private bool capturingKey; // 새 키 입력 대기
        private string bindingHint = ""; // 키 변경 결과 안내
        private string displayHint = ""; // 화면 설정 결과 안내
        private static readonly DisplayWindowMode[] displayModes = new DisplayWindowMode[] // 화면 모드 선택 순서
        { // 목록 시작
            DisplayWindowMode.ExclusiveFullscreen, DisplayWindowMode.Borderless, DisplayWindowMode.Windowed // 전체 화면과 창 모드
        }; // 목록 종료
        private static readonly int[] frameLimits = new int[] // 초당 프레임 선택 순서
        { // 목록 시작
            60, 120, 144, 0 // 지정 값과 무제한
        }; // 목록 종료
        public KeyBindingSettings Bindings // 공유 조작 키 설정
        { // 속성 시작
            get; // 키 설정 조회
            private set; // 키 설정 연결
        } // 속성 종료
        public DisplaySettings Display // 화면 설정 상태
        { // 속성 시작
            get; // 설정 조회
            private set; // 설정 연결
        } // 속성 종료
        public bool IsPaused // 일시정지 화면 열림 여부
        { // 속성 시작
            get; // 상태 조회
            private set; // 상태 변경
        } // 속성 종료
        public event Action RetryRequested; // 재시도 요청 알림
        public event Action ExitRequested; // 출격 준비 복귀 요청
        public void SetBindings(KeyBindingSettings keyBindings) // 공유 키 설정 연결
        { // 연결 시작
            Bindings = keyBindings; // 키 설정 보관
        } // 연결 종료
        public void SetDisplaySettings(DisplaySettings displaySettings) // 화면 설정 장치 연결
        { // 연결 시작
            Display = displaySettings; // 화면 설정 보관
        } // 연결 종료
        public void Begin(BattlePlayer target, Camera camera, BattleProgress battleProgress, UpgradeItemSelection selection) // 화면 연결
        { // 연결 시작
            Resume(); // 기존 정지 상태 정리
            player = target; // 플레이어 보관
            battleCamera = camera; // 카메라 보관
            progress = battleProgress; // 카드 선택 보관
            upgrades = selection; // 강화 선택 보관
            settings = new GameSettings(); // 조작 설정 연결
            EnsureBindings(); // 저장된 키 설정 연결
            EnsureDisplay(); // 저장된 화면 설정 연결
            frame = Resources.Load<Texture2D>("BattleArt/ui_card_frame"); // 기존 화면 이미지
        } // 연결 종료
        public bool TryPause() // 전투 일시정지 요청
        { // 요청 시작
            if (IsPaused || Time.timeScale <= 0f || player == null || player.Survival == null || !player.Survival.IsAlive) // 전투 상태 확인
            { // 차단 시작
                return false; // 일시정지 불가
            } // 차단 종료
            if ((progress != null && (progress.Offer != null || progress.Blocked)) || (upgrades != null && upgrades.IsOpen)) // 다른 선택 화면 확인
            { // 차단 시작
                return false; // 선택 화면과 충돌 방지
            } // 차단 종료
            StageDirector stage = battleCamera != null ? battleCamera.GetComponent<StageDirector>() : null; // 현재 스테이지 조회
            if (stage != null && stage.State != null && (stage.State.Phase == StagePhase.Result || stage.State.Phase == StagePhase.Failed)) // 종료된 출격 확인
            { // 차단 시작
                return false; // 결과 화면과 충돌 방지
            } // 차단 종료
            Open(Page.Pause, false); // 전투 일시정지 화면 열기
            return true; // 일시정지 성공
        } // 요청 종료
        public bool OpenSettingsFromMenu() // 메뉴에서 설정 열기
        { // 열기 시작
            if (IsPaused) // 다른 화면 열림 확인
            { // 차단 시작
                return false; // 중복 열기 차단
            } // 차단 종료
            Open(Page.Settings, true); // 메뉴 설정 화면 열기
            return true; // 설정 열기 성공
        } // 열기 종료
        public void Resume() // 이전 시간 상태 복원
        { // 복원 시작
            if (!IsPaused) // 정지 소유 확인
            { // 생략 시작
                return; // 변경 사항 없음
            } // 생략 종료
            Time.timeScale = oldTimeScale; // 이전 시간 배율 복원
            IsPaused = false; // 화면 닫힘 표시
            fromMenu = false; // 메뉴 진입 상태 해제
            selected = 0; // 선택 위치 초기화
        } // 복원 종료
        private void Open(Page target, bool menu) // 화면과 시간 정지 시작
        { // 열기 시작
            if (settings == null) // 설정 연결 확인
            { // 기본 설정 시작
                settings = new GameSettings(); // 기존 저장값 연결
            } // 기본 설정 종료
            if (frame == null) // 배경 이미지 확인
            { // 이미지 준비 시작
                frame = Resources.Load<Texture2D>("BattleArt/ui_card_frame"); // 기존 카드 이미지
            } // 이미지 준비 종료
            EnsureBindings(); // 조작 키 설정 준비
            EnsureDisplay(); // 화면 설정 준비
            oldTimeScale = Time.timeScale; // 현재 시간 배율 보관
            Time.timeScale = 0f; // 전투 정지
            IsPaused = true; // 정지 화면 표시
            fromMenu = menu; // 메뉴 진입 여부 보관
            openedFrame = Time.frameCount; // 같은 프레임 입력 차단
            page = target; // 시작 화면 보관
            selected = 0; // 첫 선택지 표시
            capturingKey = false; // 키 입력 대기 해제
            bindingHint = ""; // 이전 안내 제거
        } // 열기 종료
        private void EnsureBindings() // 저장된 키 설정 준비
        { // 준비 시작
            if (Bindings != null) // 기존 공유 설정 확인
            { // 재사용 시작
                return; // 기존 설정 유지
            } // 재사용 종료
            Bindings = new KeyBindingSettings(); // 키 설정 생성
            Bindings.Load(); // 저장된 키 읽기
        } // 준비 종료
        private void EnsureDisplay() // 저장된 화면 설정 준비
        { // 준비 시작
            if (Display != null) // 기존 화면 설정 확인
            { // 재사용 시작
                return; // 기존 장치 유지
            } // 재사용 종료
            Display = new DisplaySettings(); // 실제 장치와 저장소 연결
            Display.Load(); // 저장된 화면 설정 읽기
            if (Application.isPlaying && !Application.isEditor && !Display.Apply()) // 실제 빌드에서만 적용 요청
            { // 오류 안내 시작
                displayHint = "저장된 화면 설정 적용에 실패했습니다"; // 적용 실패 안내
            } // 오류 안내 종료
        } // 준비 종료
        private void Update() // 일시정지 입력 처리
        { // 처리 시작
            if (IsPaused && Time.frameCount == openedFrame) // 화면을 연 프레임 확인
            { // 입력 대기 시작
                return; // 열기 입력의 중복 처리 방지
            } // 입력 대기 종료
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (IsPaused && page == Page.Bindings && capturingKey) // 새 키 입력 대기 확인
            { // 키 입력 처리 시작
                CaptureBinding(keyboard); // 충돌 검사와 키 저장
                return; // 메뉴 입력과 충돌 방지
            } // 키 입력 처리 종료
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) // 뒤로 입력 확인
            { // 뒤로 처리 시작
                if (IsPaused) // 화면 열림 확인
                { // 열린 화면 시작
                    Back(); // 이전 화면 복귀
                } // 열린 화면 종료
                else // 전투 중 입력
                { // 전투 처리 시작
                    TryPause(); // 일시정지 시도
                } // 전투 처리 종료
                return; // 같은 프레임 추가 입력 차단
            } // 뒤로 처리 종료
            if (!IsPaused) // 화면 열림 확인
            { // 생략 시작
                return; // 메뉴 입력 생략
            } // 생략 종료
            int count = OptionCount(); // 현재 선택지 수
            if (keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)) // 위쪽 입력
            { // 위쪽 선택 시작
                selected = (selected + count - 1) % count; // 이전 항목 선택
            } // 위쪽 선택 종료
            else if (keyboard != null && (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)) // 아래쪽 입력
            { // 아래쪽 선택 시작
                selected = (selected + 1) % count; // 다음 항목 선택
            } // 아래쪽 선택 종료
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) // 선택 입력 확인
            { // 선택 처리 시작
                Select(selected); // 현재 항목 실행
                return; // 같은 프레임 중복 입력 차단
            } // 선택 처리 종료
            if (keyboard != null && (page == Page.Display || (page == Page.Settings && selected < 3))) // 좌우 변경 가능한 화면
            { // 방향 처리 시작
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) // 좌우 입력 확인
                { // 설정 변경 시작
                    Select(selected, keyboard.leftArrowKey.wasPressedThisFrame ? -1 : 1); // 이전 또는 다음 설정
                    return; // 같은 프레임 중복 입력 차단
                } // 설정 변경 종료
            } // 방향 처리 종료
            Mouse pointer = Mouse.current; // 현재 마우스
            if (pointer == null || !pointer.leftButton.wasPressedThisFrame) // 클릭 확인
            { // 생략 시작
                return; // 포인터 처리 생략
            } // 생략 종료
            Rect view = View(); // 전투 화면 영역
            Vector2 position = pointer.position.ReadValue(); // 포인터 위치
            position.y = Screen.height - position.y; // 화면 위쪽 기준 변환
            for (int index = 0; index < count; index++) // 선택지 순회
            { // 클릭 검사 시작
                if (OptionRect(view, index, count).Contains(position)) // 선택지 클릭 확인
                { // 클릭 처리 시작
                    selected = index; // 클릭 항목 선택
                    Select(index); // 해당 항목 실행
                    break; // 중복 선택 방지
                } // 클릭 처리 종료
            } // 클릭 검사 종료
        } // 처리 종료
        private void Select(int index, int direction = 1) // 선택 항목 실행
        { // 실행 시작
            if (page == Page.Pause) // 일시정지 메뉴 확인
            { // 메뉴 처리 시작
                if (index == 0) // 계속하기 확인
                { // 계속 시작
                    Resume(); // 전투 재개
                } // 계속 종료
                else if (index == 1) // 설정 확인
                { // 설정 시작
                    page = Page.Settings; // 설정 화면 변경
                    selected = 0; // 첫 항목 선택
                } // 설정 종료
                else // 재시도 또는 복귀 확인
                { // 확인 화면 시작
                    page = index == 2 ? Page.ConfirmRetry : Page.ConfirmExit; // 확인 화면 선택
                    selected = 1; // 기본 취소 선택
                } // 확인 화면 종료
                return; // 추가 처리 생략
            } // 메뉴 처리 종료
            if (page == Page.Settings) // 설정 화면 확인
            { // 설정 처리 시작
                if (index == 0) // 집중 방식 확인
                { // 집중 방식 시작
                    settings.SetFocusToggle(!settings.FocusToggle); // 홀드와 토글 전환
                } // 집중 방식 종료
                else if (index == 1) // 일반 피격 표시 확인
                { // 일반 표시 시작
                    settings.SetHitboxNormal(!settings.HitboxNormal); // 일반 표시 전환
                } // 일반 표시 종료
                else if (index == 2) // 집중 피격 표시 확인
                { // 집중 표시 시작
                    settings.SetHitboxFocus(!settings.HitboxFocus); // 집중 표시 전환
                } // 집중 표시 종료
                else if (index == 3) // 조작 키 변경 확인
                { // 키 설정 시작
                    page = Page.Bindings; // 키 설정 화면 변경
                    selected = 0; // 첫 동작 선택
                    bindingHint = ""; // 이전 안내 제거
                } // 키 설정 종료
                else if (index == 4) // 화면 설정 확인
                { // 화면 설정 시작
                    page = Page.Display; // 화면 설정 열기
                    selected = 0; // 첫 항목 선택
                    displayHint = ""; // 이전 결과 제거
                } // 화면 설정 종료
                else if (index == 5) // 조작 기본값 복원 확인
                { // 기본값 시작
                    settings.RestoreControlDefaults(); // 조작 기본값 복원
                    Bindings.ResetDefaults(); // 키 배치 기본값 복원
                    Bindings.Save(); // 기본 키 설정 저장
                } // 기본값 종료
                else // 뒤로 가기
                { // 복귀 시작
                    Back(); // 이전 화면 복귀
                } // 복귀 종료
                return; // 추가 처리 생략
            } // 설정 처리 종료
            if (page == Page.Bindings) // 키 설정 화면 확인
            { // 키 설정 처리 시작
                if (index < 8) // 재지정할 동작 확인
                { // 키 입력 시작
                    capturingKey = true; // 다음 키 입력 대기
                    bindingHint = "새 키 입력 · Esc 취소"; // 입력 안내
                } // 키 입력 종료
                else if (index == 8) // 기본 키 복원 확인
                { // 키 복원 시작
                    Bindings.ResetDefaults(); // 키 배치 기본값 복원
                    Bindings.Save(); // 기본 키 설정 저장
                    bindingHint = "기본 키를 복원했습니다"; // 복원 결과 안내
                } // 키 복원 종료
                else // 설정 화면 복귀
                { // 복귀 시작
                    Back(); // 이전 화면 복귀
                } // 복귀 종료
                return; // 추가 처리 생략
            } // 키 설정 처리 종료
            if (page == Page.Display) // 화면 설정 확인
            { // 화면 설정 처리 시작
                if (index < 5) // 변경 또는 기본값 확인
                { // 설정 변경 시작
                    ChangeDisplay(index, direction); // 화면 설정 적용과 저장
                } // 설정 변경 종료
                else // 설정 화면 복귀
                { // 복귀 시작
                    Back(); // 이전 화면 복귀
                } // 복귀 종료
                return; // 추가 처리 생략
            } // 화면 설정 처리 종료
            if (index != 0) // 확인 화면 취소
            { // 취소 시작
                Back(); // 일시정지 화면 복귀
                return; // 요청 생략
            } // 취소 종료
            bool retry = page == Page.ConfirmRetry; // 재시도 요청 여부
            Resume(); // 시간 배율 복원
            if (retry) // 재시도 확인
            { // 재시도 시작
                RetryRequested?.Invoke(); // 재시도 알림
            } // 재시도 종료
            else // 출격 준비 복귀
            { // 복귀 시작
                ExitRequested?.Invoke(); // 복귀 알림
            } // 복귀 종료
        } // 실행 종료
        private void Back() // 이전 화면으로 복귀
        { // 복귀 시작
            if (page == Page.Bindings) // 키 설정 복귀 확인
            { // 설정 복귀 시작
                capturingKey = false; // 키 입력 대기 해제
                page = Page.Settings; // 조작 설정 복귀
                selected = 3; // 키 설정 항목 선택
                return; // 추가 변경 생략
            } // 설정 복귀 종료
            if (page == Page.Display) // 화면 설정 복귀 확인
            { // 조작 설정 복귀 시작
                page = Page.Settings; // 설정 화면 복귀
                selected = 4; // 화면 설정 항목 선택
                return; // 추가 변경 생략
            } // 조작 설정 복귀 종료
            if (page == Page.Pause || (page == Page.Settings && fromMenu)) // 화면 닫기 확인
            { // 닫기 시작
                Resume(); // 이전 시간 상태 복원
                return; // 추가 변경 생략
            } // 닫기 종료
            page = Page.Pause; // 일시정지 메뉴 복귀
            selected = 0; // 첫 항목 선택
        } // 복귀 종료
        private void CaptureBinding(Keyboard keyboard) // 다음 키를 조작에 지정
        { // 입력 시작
            if (keyboard == null) // 키보드 연결 확인
            { // 대기 시작
                return; // 키보드 연결 대기
            } // 대기 종료
            if (keyboard.escapeKey.wasPressedThisFrame) // 입력 취소 확인
            { // 취소 시작
                capturingKey = false; // 키 입력 대기 해제
                bindingHint = "키 변경 취소"; // 취소 결과 안내
                return; // 다른 키 처리 생략
            } // 취소 종료
            foreach (var key in keyboard.allKeys) // 키보드 키 순회
            { // 입력 검사 시작
                if (!key.wasPressedThisFrame) // 새 입력 확인
                { // 생략 시작
                    continue; // 다음 키 확인
                } // 생략 종료
                if (Bindings.TryBind((PlayerAction)selected, key.name)) // 키 충돌 검사
                { // 저장 시작
                    Bindings.Save(); // 조작 키 저장
                    capturingKey = false; // 키 입력 대기 해제
                    bindingHint = "키 변경 완료"; // 변경 결과 안내
                } // 저장 종료
                else // 중복 또는 예약 키
                { // 오류 안내 시작
                    bindingHint = "사용 중이거나 메뉴에 예약된 키입니다"; // 기존 키 유지 경고
                } // 오류 안내 종료
                return; // 한 번에 한 키만 처리
            } // 입력 검사 종료
        } // 입력 종료
        private void ChangeDisplay(int index, int direction) // 화면 설정 변경과 실패 복구
        { // 변경 시작
            DisplayWindowMode oldMode = Display.Mode; // 이전 화면 모드
            DisplaySize oldSize = Display.Resolution; // 이전 해상도
            int oldFps = Display.FpsLimit; // 이전 프레임 제한
            bool oldVSync = Display.VSync; // 이전 수직 동기화
            bool valid = true; // 변경 값 유효 여부
            if (index == 0) // 화면 모드 변경 확인
            { // 모드 변경 시작
                int current = Array.IndexOf(displayModes, oldMode); // 현재 모드 위치
                int next = (current + direction + displayModes.Length) % displayModes.Length; // 다음 모드 위치
                valid = Display.TrySetMode(displayModes[next]); // 지원 모드 선택
            } // 모드 변경 종료
            else if (index == 1) // 해상도 변경 확인
            { // 해상도 변경 시작
                int count = Display.Resolutions.Count; // 지원 해상도 수
                if (count == 0) // 지원 해상도 확인
                { // 변경 불가 시작
                    valid = false; // 빈 목록 거부
                } // 변경 불가 종료
                else // 선택할 해상도 존재
                { // 목록 선택 시작
                    int current = 0; // 현재 해상도 위치
                    for (int position = 0; position < count; position++) // 해상도 목록 순회
                    { // 위치 검색 시작
                        if (Display.Resolutions[position].Equals(oldSize)) // 현재 크기 확인
                        { // 위치 기록 시작
                            current = position; // 현재 위치 보관
                            break; // 추가 검색 생략
                        } // 위치 기록 종료
                    } // 위치 검색 종료
                    DisplaySize next = Display.Resolutions[(current + direction + count) % count]; // 다음 해상도
                    valid = Display.TrySetResolution(next.Width, next.Height); // 지원 해상도 선택
                } // 목록 선택 종료
            } // 해상도 변경 종료
            else if (index == 2) // 프레임 제한 변경 확인
            { // 프레임 변경 시작
                int current = Array.IndexOf(frameLimits, oldFps); // 현재 제한 위치
                int next = (current + direction + frameLimits.Length) % frameLimits.Length; // 다음 제한 위치
                valid = Display.TrySetFpsLimit(frameLimits[next]); // 지원 제한 선택
            } // 프레임 변경 종료
            else if (index == 3) // 수직 동기화 변경 확인
            { // 동기화 변경 시작
                Display.SetVSync(!oldVSync); // 동기화 상태 전환
            } // 동기화 변경 종료
            else // 화면 기본값 복원
            { // 기본값 시작
                Display.ResetDefaults(); // 초안 기본값 선택
            } // 기본값 종료
            bool attempted = valid; // 장치 요청 시도 여부
            bool applied = false; // 설정 적용과 저장 성공 여부
            bool saveAttempted = false; // 저장 요청 시도 여부
            try // 화면 설정 요청
            { // 요청 시작
                if (valid && Display.Apply()) // 화면 적용 요청 확인
                { // 저장 시작
                    saveAttempted = true; // 저장 시도 기록
                    applied = Display.Save(); // 설정 저장 요청
                } // 저장 종료
            } // 요청 종료
            catch (Exception) // 설정 저장 오류
            { // 오류 처리 시작
                applied = false; // 실패 상태 보관
            } // 오류 처리 종료
            if (!applied) // 적용 실패 확인
            { // 복구 시작
                Display.TrySetMode(oldMode); // 이전 화면 모드 복원
                Display.TrySetResolution(oldSize.Width, oldSize.Height); // 이전 해상도 복원
                Display.TrySetFpsLimit(oldFps); // 이전 프레임 제한 복원
                Display.SetVSync(oldVSync); // 이전 동기화 상태 복원
                bool restored = !attempted || Display.Apply(); // 이전 화면 설정 재요청
                if (saveAttempted) // 저장 실패 확인
                { // 저장 안내 시작
                    displayHint = restored ? "저장 실패 · 재시작 후 설정 확인" : "저장 실패 · 화면 복구 실패 · 재시작 후 설정 확인"; // 저장 불확실 상태 안내
                } // 저장 안내 종료
                else // 화면 요청 실패 확인
                { // 화면 안내 시작
                    displayHint = restored ? "화면 설정 변경 실패 · 이전 설정 재요청" : "화면 복구 실패 · 화면 설정 확인"; // 화면 복구 결과 안내
                } // 화면 안내 종료
                return; // 성공 안내 생략
            } // 복구 종료
            displayHint = Display.VSync ? "수직 동기화가 FPS 제한보다 우선할 수 있습니다" : "화면 설정을 저장했습니다"; // 적용 결과 안내
        } // 변경 종료
        private int OptionCount() // 현재 화면 선택지 수
        { // 조회 시작
            return page == Page.Pause ? 4 : page == Page.Settings ? 7 : page == Page.Bindings ? 10 : page == Page.Display ? 6 : 2; // 화면별 항목 수
        } // 조회 종료
        private Rect View() // 화면 영역 계산
        { // 계산 시작
            if (battleCamera == null) // 전투 카메라 확인
            { // 기본 화면 시작
                return new Rect(0f, 0f, Screen.width, Screen.height); // 전체 화면 영역
            } // 기본 화면 종료
            Rect pixels = battleCamera.pixelRect; // 카메라 화소 영역
            return new Rect(pixels.x, Screen.height - pixels.yMax, pixels.width, pixels.height); // 위쪽 기준 화면 영역
        } // 계산 종료
        private static Rect PanelRect(Rect view) // 화면 중앙 패널 영역
        { // 계산 시작
            float width = Mathf.Min(view.width * 0.70f, 720f); // 패널 가로 크기
            float height = Mathf.Min(view.height * 0.80f, 760f); // 패널 세로 크기
            return new Rect(view.center.x - width * 0.5f, view.center.y - height * 0.5f, width, height); // 중앙 패널 반환
        } // 계산 종료
        private static Rect OptionRect(Rect view, int index, int count) // 화면 선택지 영역
        { // 계산 시작
            Rect panel = PanelRect(view); // 패널 영역
            float row = panel.height * 0.58f / count; // 선택지 세로 간격
            return new Rect(panel.x + panel.width * 0.1f, panel.y + panel.height * 0.28f + row * index, panel.width * 0.8f, row); // 선택지 영역 반환
        } // 계산 종료
        private string OptionText(int index) // 선택지 설명
        { // 설명 시작
            if (page == Page.Pause) // 일시정지 화면 확인
            { // 메뉴 설명 시작
                return index == 0 ? "계속하기" : index == 1 ? "설정" : index == 2 ? "재시도" : "출격 준비로"; // 메뉴 항목
            } // 메뉴 설명 종료
            if (page == Page.Settings) // 조작 설정 확인
            { // 설정 설명 시작
                return index == 0 ? "집중 방식: " + (settings.FocusToggle ? "토글" : "홀드") : index == 1 ? "일반 피격 표시: " + (settings.HitboxNormal ? "켜기" : "끄기") : index == 2 ? "집중 피격 표시: " + (settings.HitboxFocus ? "켜기" : "끄기") : index == 3 ? "조작 키 변경" : index == 4 ? "화면 설정" : index == 5 ? "조작 설정 기본값 복원" : "뒤로"; // 실제 적용 설정
            } // 설정 설명 종료
            if (page == Page.Bindings) // 키 설정 화면 확인
            { // 키 설정 설명 시작
                if (index >= 8) // 공통 동작 확인
                { // 공통 설명 시작
                    return index == 8 ? "키 기본값 복원" : "뒤로"; // 공통 선택지
                } // 공통 설명 종료
                string action = index == 0 ? "위 이동" : index == 1 ? "왼쪽 이동" : index == 2 ? "아래 이동" : index == 3 ? "오른쪽 이동" : index == 4 ? "집중" : index == 5 ? "고유 스킬" : index == 6 ? "공용 스킬" : "필살기"; // 동작 이름
                return action + ": " + Bindings.GetControl((PlayerAction)index); // 현재 지정 키 표시
            } // 키 설정 설명 종료
            if (page == Page.Display) // 화면 설정 확인
            { // 화면 설명 시작
                string mode = Display.Mode == DisplayWindowMode.ExclusiveFullscreen ? "전체 화면" : Display.Mode == DisplayWindowMode.Borderless ? "전체 창" : "창 모드"; // 화면 모드 이름
                string size = Display.Resolution.Width + " × " + Display.Resolution.Height; // 해상도 표시
                string fps = Display.FpsLimit == 0 ? "무제한" : Display.FpsLimit.ToString(); // 프레임 제한 표시
                return index == 0 ? "화면 모드: " + mode : index == 1 ? "해상도: " + size : index == 2 ? "FPS 제한: " + fps : index == 3 ? "수직 동기화: " + (Display.VSync ? "켜기" : "끄기") : index == 4 ? "화면 기본값 복원" : "뒤로"; // 실제 적용 항목
            } // 화면 설명 종료
            return index == 0 ? "확인" : "취소"; // 진행 손실 확인
        } // 설명 종료
        private void OnGUI() // 화면 표시
        { // 표시 시작
            if (!IsPaused) // 화면 열림 확인
            { // 생략 시작
                return; // 화면 표시 생략
            } // 생략 종료
            int previousDepth = GUI.depth; // 기존 표시 순서
            GUI.depth = -100; // 일시정지 화면 우선 표시
            PrepareStyles(); // 한글 글꼴 준비
            Rect view = View(); // 전투 화면 영역
            Rect panel = PanelRect(view); // 중앙 패널 영역
            Color previous = GUI.color; // 기존 화면 색상
            GUI.color = new Color(0f, 0f, 0f, 0.78f); // 정지 화면 음영
            GUI.DrawTexture(view, Texture2D.whiteTexture); // 전투 화면 어둡게 표시
            GUI.color = Color.white; // 원본 이미지 색상
            if (frame != null) // 배경 이미지 확인
            { // 이미지 표시 시작
                GUI.DrawTexture(panel, frame, ScaleMode.StretchToFill); // 설정 패널 이미지
            } // 이미지 표시 종료
            else // 이미지 누락 시 대체
            { // 대체 표시 시작
                GUI.Box(panel, GUIContent.none); // 기본 배경 표시
            } // 대체 표시 종료
            titleStyle.fontSize = Mathf.Max(18, Mathf.RoundToInt(panel.height * 0.055f)); // 제목 글자 크기
            labelStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(panel.height * (page == Page.Bindings ? 0.027f : 0.037f))); // 항목 글자 크기
            string title = page == Page.Pause ? "일시정지" : page == Page.Settings ? "설정" : page == Page.Bindings ? "조작 키 변경" : page == Page.Display ? "화면 설정" : page == Page.ConfirmRetry ? "재시도하면 현재 전투가 초기화됩니다" : "출격 준비로 돌아가면 현재 전투가 초기화됩니다"; // 화면 제목
            GUI.Label(new Rect(panel.x + 15f, panel.y + panel.height * 0.08f, panel.width - 30f, panel.height * 0.16f), title, titleStyle); // 제목 표시
            int count = OptionCount(); // 현재 항목 수
            for (int index = 0; index < count; index++) // 항목 표시 순회
            { // 표시 반복 시작
                GUI.color = index == selected ? new Color(0.45f, 1f, 1f) : Color.white; // 선택 항목 강조
                GUI.Label(OptionRect(view, index, count), (index == selected ? "▶  " : "") + OptionText(index), labelStyle); // 항목과 현재 값 표시
            } // 표시 반복 종료
            if (page == Page.Bindings && !string.IsNullOrEmpty(bindingHint)) // 키 입력 안내 확인
            { // 안내 표시 시작
                GUI.color = Color.white; // 안내 글자 색상
                GUI.Label(new Rect(panel.x + panel.width * 0.1f, panel.y + panel.height * 0.89f, panel.width * 0.8f, panel.height * 0.08f), bindingHint, labelStyle); // 입력 결과와 오류 표시
            } // 안내 표시 종료
            if (page == Page.Display && !string.IsNullOrEmpty(displayHint)) // 화면 설정 결과 확인
            { // 안내 표시 시작
                GUI.color = Color.white; // 안내 글자 색상
                GUI.Label(new Rect(panel.x + panel.width * 0.1f, panel.y + panel.height * 0.89f, panel.width * 0.8f, panel.height * 0.08f), displayHint, labelStyle); // 설정 결과와 오류 표시
            } // 안내 표시 종료
            GUI.color = previous; // 이전 화면 색상 복원
            GUI.depth = previousDepth; // 기존 표시 순서 복원
        } // 표시 종료
        private void PrepareStyles() // 한글 글꼴 설정
        { // 설정 시작
            if (titleStyle != null) // 기존 글꼴 확인
            { // 재사용 시작
                return; // 재생성 생략
            } // 재사용 종료
            string[] fontNames = new string[] // 시스템 글꼴 후보
            { // 목록 시작
                "Malgun Gothic", // 윈도우 한글 글꼴
                "Arial" // 기본 대체 글꼴
            }; // 목록 종료
            Font font = Font.CreateDynamicFontFromOSFont(fontNames, 18); // 사용 가능한 글꼴
            titleStyle = new GUIStyle(GUI.skin.label); // 제목 글꼴 생성
            titleStyle.font = font != null ? font : GUI.skin.font; // 한글 글꼴 적용
            titleStyle.fontStyle = FontStyle.Bold; // 제목 굵게 표시
            titleStyle.wordWrap = true; // 긴 문장 줄바꿈
            titleStyle.alignment = TextAnchor.MiddleCenter; // 제목 중앙 정렬
            titleStyle.normal.textColor = Color.white; // 제목 흰색
            labelStyle = new GUIStyle(titleStyle); // 항목 글꼴 생성
        } // 설정 종료
        private void OnDisable() // 구성 요소 비활성화
        { // 정리 시작
            Resume(); // 소유한 시간 상태 복원
        } // 정리 종료
        private void OnDestroy() // 구성 요소 제거
        { // 정리 시작
            Resume(); // 소유한 시간 상태 복원
        } // 정리 종료
    } // 구성 요소 종료
} // 영역 종료
