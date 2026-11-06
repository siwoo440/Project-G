using System; // 화면 닫기 알림
using UnityEngine; // 유니티 화면 기능
using UnityEngine.Events; // 버튼 이벤트 기능
using UnityEngine.InputSystem; // 키보드와 포인터 입력
using UnityEngine.UI; // 편집 가능한 UI 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class SoundSettingsPanel : MonoBehaviour // 씬에 저장된 음량 설정 화면
    { // 화면 시작
        [SerializeField] private Canvas canvas; // 실제 화면 캔버스
        [SerializeField] private RectTransform viewport; // 편집 가능한 화면 영역
        [SerializeField] private SceneSoundPlayer sound; // 같은 씬의 실제 오디오 설정
        [SerializeField] private Button[] decrease; // 다섯 음량 감소 버튼
        [SerializeField] private Button[] increase; // 다섯 음량 증가 버튼
        [SerializeField] private Text[] values; // 다섯 음량 값 표시
        [SerializeField] private Text[] labels; // 다섯 범주 제목
        [SerializeField] private Text backLabel; // 뒤로 선택 표시
        [SerializeField] private Color selectedTint = Color.cyan; // 편집 가능한 선택 강조
        [SerializeField] private Button back; // 기존 설정으로 복귀
        [SerializeField] private Text hint; // 저장 실패 안내
        private readonly UnityAction[] decreaseActions = new UnityAction[5]; // 등록한 감소 이벤트
        private readonly UnityAction[] increaseActions = new UnityAction[5]; // 등록한 증가 이벤트
        private SoundSettings settings; // 현재 공유 음량 설정
        private Action closed; // 기존 설정 화면 복귀
        private int selected; // 키보드 선택 항목
        private int openedFrame; // 열린 프레임 기록
        private bool registered; // 버튼 연결 여부
        public bool IsReady => canvas != null && viewport != null && sound != null && sound.Settings != null && back != null && backLabel != null && hint != null && Complete(decrease) && Complete(increase) && Complete(values) && Complete(labels); // 실제 필수 씬 참조
        public bool IsOpen => gameObject.activeInHierarchy && closed != null; // 실제 설정 표시 상태
        private static bool Complete<T>(T[] items) where T : UnityEngine.Object // 다섯 참조 확인
        { // 확인 시작
            if (items == null || items.Length != 5) // 다섯 항목 확인
            { // 실패 시작
                return false; // 누락 또는 개수 오류
            } // 실패 종료
            foreach (T item in items) // 모든 항목 순회
            { // 확인 시작
                if (item == null) // 개별 참조 확인
                { // 실패 시작
                    return false; // 누락 참조 차단
                } // 실패 종료
            } // 확인 종료
            return true; // 모든 참조 확인
        } // 확인 종료
        public bool Open(Action onClosed) // 기존 모달에서 음량 설정 열기
        { // 열기 시작
            if (IsOpen || onClosed == null || !IsReady) // 모달 상태와 씬 연결 확인
            { // 거부 시작
                return false; // 불완전한 화면 차단
            } // 거부 종료
            settings = sound.Settings; // 실제 공유 설정 보관
            closed = onClosed; // 기존 설정 복귀 연결
            selected = 0; // 첫 음량 선택
            openedFrame = Time.frameCount; // 같은 프레임 입력 차단
            gameObject.SetActive(true); // 저장된 실제 화면 표시
            RegisterButtons(); // 실제 버튼 이벤트 연결
            hint.text = sound.LoadSucceeded ? "" : "저장된 음량을 확인하지 못했습니다"; // 복원 실패 안내
            RefreshValues(); // 다섯 현재 음량 표시
            RefreshFocus(); // 키보드 선택 항목 표시
            return true; // 실제 화면 열기 완료
        } // 열기 종료
        private void RegisterButtons() // 직렬화된 버튼 연결
        { // 연결 시작
            if (registered) // 기존 연결 확인
            { // 생략 시작
                return; // 중복 구독 방지
            } // 생략 종료
            for (int index = 0; index < 5; index++) // 다섯 범주 순회
            { // 연결 시작
                int channel = index; // 반복별 범주 보관
                decreaseActions[index] = () => Change(channel, -1); // 해당 음량 감소 동작
                increaseActions[index] = () => Change(channel, 1); // 해당 음량 증가 동작
                decrease[index].onClick.AddListener(decreaseActions[index]); // 실제 감소 버튼 연결
                increase[index].onClick.AddListener(increaseActions[index]); // 실제 증가 버튼 연결
            } // 연결 종료
            back.onClick.AddListener(Close); // 실제 뒤로 버튼 연결
            registered = true; // 중복 연결 차단
        } // 연결 종료
        private void Change(int index, int direction) // 선택 음량 변경
        { // 변경 시작
            if (!IsOpen || index < 0 || index >= 5) // 모달과 범주 확인
            { // 거부 시작
                return; // 숨긴 화면 변경 방지
            } // 거부 종료
            SoundChannel channel = (SoundChannel)index; // 실제 음량 범주
            int previous = settings.Get(channel); // 저장 전 음량 보관
            if (!settings.TryStep(channel, direction)) // 5% 변경 확인
            { // 거부 시작
                return; // 잘못된 방향 차단
            } // 거부 종료
            if (!settings.TrySave()) // 실제 설정 저장 확인
            { // 오류 시작
                settings.TrySet(channel, previous); // 메모리 음량 복원 요청
                hint.text = "음량 저장 실패 · 재시작 후 설정 확인"; // 저장 상태 불확실 안내
            } // 오류 종료
            else // 저장 요청 완료
            { // 성공 시작
                hint.text = ""; // 이전 오류 안내 해제
            } // 성공 종료
            sound.TryPlayUi(SoundCue.Confirm); // 유효한 음량 조작 소리 요청
            selected = index; // 선택 범주 보관
            RefreshValues(); // 현재 음량 표시
            RefreshFocus(); // 현재 선택 강조
        } // 변경 종료
        private void RefreshValues() // 실제 값 표시
        { // 갱신 시작
            for (int index = 0; index < 5; index++) // 다섯 음량 순회
            { // 갱신 시작
                values[index].text = settings.Get((SoundChannel)index) + "%"; // 현재 저장 설정 표시
            } // 갱신 종료
        } // 갱신 종료
        private void RefreshFocus() // 키보드 선택 강조
        { // 강조 시작
            for (int index = 0; index < 5; index++) // 다섯 제목 순회
            { // 강조 시작
                labels[index].canvasRenderer.SetColor(index == selected ? selectedTint : Color.white); // 기존 글자 색상에 선택 강조
            } // 강조 종료
            backLabel.canvasRenderer.SetColor(selected == 5 ? selectedTint : Color.white); // 실제 뒤로 항목 강조
        } // 강조 종료
        private void Update() // 실제 키와 포인터 처리
        { // 처리 시작
            if (!IsOpen || Time.frameCount == openedFrame) // 표시 상태와 열린 프레임 확인
            { // 생략 시작
                return; // 같은 입력의 중복 처리 차단
            } // 생략 종료
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard != null) // 키보드 연결 확인
            { // 입력 시작
                if (keyboard.escapeKey.wasPressedThisFrame) // 뒤로 입력 확인
                { // 복귀 시작
                    Close(); // 기존 설정으로 복귀
                    return; // 같은 프레임 추가 입력 차단
                } // 복귀 종료
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) // 이전 항목 입력
                { // 이동 시작
                    selected = (selected + 5) % 6; // 이전 음량 또는 뒤로 선택
                    RefreshFocus(); // 현재 선택 강조
                    return; // 한 프레임 한 입력 처리
                } // 이동 종료
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) // 다음 항목 입력
                { // 이동 시작
                    selected = (selected + 1) % 6; // 다음 음량 또는 뒤로 선택
                    RefreshFocus(); // 현재 선택 강조
                    return; // 한 프레임 한 입력 처리
                } // 이동 종료
                if (selected < 5 && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)) // 음량 조절 입력
                { // 조절 시작
                    Change(selected, keyboard.leftArrowKey.wasPressedThisFrame ? -1 : 1); // 선택 음량 조절
                    return; // 포인터 중복 처리 차단
                } // 조절 종료
                if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) && selected == 5) // 뒤로 버튼 선택
                { // 복귀 시작
                    Close(); // 기존 설정으로 복귀
                    return; // 포인터 중복 처리 차단
                } // 복귀 종료
            } // 입력 종료
            Mouse mouse = Mouse.current; // 현재 포인터
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) // 실제 클릭 확인
            { // 생략 시작
                return; // 중복 클릭 방지
            } // 생략 종료
            Vector2 position = mouse.position.ReadValue(); // 화면 기준 포인터 위치
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera; // 실제 캔버스 카메라
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, position, eventCamera)) // 전투 영역 밖 확인
            { // 차단 시작
                return; // 화면 여백 클릭 차단
            } // 차단 종료
            for (int index = 0; index < 5; index++) // 실제 버튼 영역 순회
            { // 확인 시작
                if (Hit(decrease[index], position, eventCamera)) // 감소 버튼 확인
                { // 감소 시작
                    decrease[index].onClick.Invoke(); // 사용자가 편집한 버튼 이벤트
                    return; // 한 번만 실행
                } // 감소 종료
                if (Hit(increase[index], position, eventCamera)) // 증가 버튼 확인
                { // 증가 시작
                    increase[index].onClick.Invoke(); // 사용자가 편집한 버튼 이벤트
                    return; // 한 번만 실행
                } // 증가 종료
            } // 확인 종료
            if (Hit(back, position, eventCamera)) // 뒤로 버튼 확인
            { // 복귀 시작
                back.onClick.Invoke(); // 실제 버튼 이벤트
            } // 복귀 종료
        } // 처리 종료
        private static bool Hit(Button button, Vector2 position, Camera camera) // 실제 버튼 영역 확인
        { // 확인 시작
            return button != null && button.isActiveAndEnabled && button.IsInteractable() && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, position, camera); // 편집된 위치와 상호작용 반영
        } // 확인 종료
        public void Close() // 기존 설정 화면으로 복귀
        { // 복귀 시작
            if (!IsOpen) // 실제 열린 상태 확인
            { // 생략 시작
                return; // 중복 닫기 방지
            } // 생략 종료
            sound.TryPlayUi(SoundCue.Cancel); // 실제 닫기 소리 요청
            Action callback = closed; // 복귀 동작 보관
            closed = null; // 중복 입력 차단
            gameObject.SetActive(false); // 실제 음량 화면 숨김
            callback(); // 기존 설정 화면 재개
        } // 복귀 종료
        public void Dismiss() // 기존 모달 종료 시 화면 정리
        { // 정리 시작
            closed = null; // 이전 복귀 동작 해제
            if (gameObject.activeSelf) // 현재 표시 여부 확인
            { // 숨김 시작
                gameObject.SetActive(false); // 실제 음량 화면 숨김
            } // 숨김 종료
        } // 정리 종료
        private void OnDestroy() // 씬 제거 시 이벤트 해제
        { // 해제 시작
            if (!registered) // 버튼 연결 여부 확인
            { // 생략 시작
                return; // 없는 이벤트 생략
            } // 생략 종료
            for (int index = 0; index < 5; index++) // 다섯 버튼 순회
            { // 해제 시작
                if (decrease[index] != null) // 감소 버튼 확인
                { // 해제 시작
                    decrease[index].onClick.RemoveListener(decreaseActions[index]); // 등록한 이벤트만 해제
                } // 해제 종료
                if (increase[index] != null) // 증가 버튼 확인
                { // 해제 시작
                    increase[index].onClick.RemoveListener(increaseActions[index]); // 등록한 이벤트만 해제
                } // 해제 종료
            } // 해제 종료
            if (back != null) // 뒤로 버튼 확인
            { // 해제 시작
                back.onClick.RemoveListener(Close); // 등록한 복귀 이벤트 해제
            } // 해제 종료
        } // 해제 종료
    } // 화면 종료
} // 영역 종료
