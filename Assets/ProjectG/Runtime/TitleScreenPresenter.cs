using UnityEngine; // 유니티 화면 기능
using UnityEngine.UI; // 편집 가능한 화면 요소
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class TitleScreenPresenter : MonoBehaviour // 씬에 저장된 타이틀 연결
    { // 구성 시작
        [SerializeField] private Camera menuCamera; // 메뉴 카메라 연결
        [SerializeField] private Canvas titleCanvas; // 메뉴 카메라에 그리는 캔버스
        [SerializeField] private GameObject letterbox; // 화면 바깥의 검은 여백
        [SerializeField] private GameObject viewport; // 편집 가능한 화면 영역
        [SerializeField] private RawImage background; // 사용자가 편집하는 배경
        [SerializeField] private bool cropBackground = true; // 비율을 보존하는 배경 자르기
        [SerializeField] private Button startButton; // 시작 버튼 연결
        [SerializeField] private Button settingsButton; // 설정 버튼 연결
        [SerializeField] private Text titleText; // 제목 글자 연결
        [SerializeField] private Text startText; // 시작 글자 연결
        [SerializeField] private Text settingsText; // 설정 글자 연결
        [SerializeField] private GameObject noticePanel; // 측정 저장 안내 영역
        [SerializeField] private Text noticeText; // 측정 저장 안내 글자
        [SerializeField] private Color selectedTint = new Color(0.55f, 1f, 1f, 1f); // 선택 시 적용할 색상
        private GameFlowController flow; // 기존 화면 상태 연결
        private Font fallbackFont; // 시스템 한글 글꼴
        private Texture previousBackground; // 이전 배경 이미지
        private Vector2 previousSize; // 이전 배경 화면 크기
        public bool IsReady => viewport != null && letterbox != null && background != null && titleCanvas != null && startButton != null && settingsButton != null && titleText != null && startText != null && settingsText != null && menuCamera != null; // 필수 참조 상태
        public bool IsUsable => IsReady && isActiveAndEnabled && titleCanvas.isActiveAndEnabled && menuCamera.isActiveAndEnabled && (titleCanvas.renderMode == RenderMode.ScreenSpaceOverlay || titleCanvas.worldCamera == menuCamera); // 화면과 카메라 사용 상태
        private void Start() // 화면 연결 시작
        { // 시작 범위
            if (!IsReady) // 필수 참조 확인
            { // 오류 시작
                Debug.LogError("타이틀 화면의 필수 연결이 없습니다.", this); // 누락된 씬 참조 안내
                if (viewport != null) // 연결된 화면 확인
                { // 숨김 시작
                    viewport.SetActive(false); // 동작하지 않는 화면 숨김
                } // 숨김 종료
                if (letterbox != null) // 연결된 여백 확인
                { // 숨김 시작
                    letterbox.SetActive(false); // 동작하지 않는 배경 숨김
                } // 숨김 종료
                enabled = false; // 불완전한 화면 차단
                return; // 기존 화면 유지
            } // 오류 종료
            startButton.onClick.AddListener(StartSelected); // 시작 동작 연결
            settingsButton.onClick.AddListener(SettingsSelected); // 설정 동작 연결
            ApplyFallbackFonts(); // 비어 있는 한글 글꼴 준비
            Connect(); // 기존 메뉴 흐름 연결
        } // 시작 종료
        private void Connect() // 현재 화면 흐름 확인
        { // 연결 시작
            if (flow == null && menuCamera != null) // 미연결 상태 확인
            { // 검색 시작
                flow = menuCamera.GetComponent<GameFlowController>(); // 카메라의 메뉴 흐름 조회
                if (flow != null) // 흐름 구성 확인
                { // 연결 시작
                    flow.AttachTitleView(this); // 편집 가능한 화면 등록
                } // 연결 종료
            } // 검색 종료
        } // 연결 종료
        private void LateUpdate() // 표시 상태 갱신
        { // 갱신 시작
            Connect(); // 늦게 구성된 흐름 확인
            bool visible = flow != null && flow.Screen == GameFlowScreen.Title && !flow.SettlementPending; // 모달 뒤 타이틀 표시 조건
            if (letterbox.activeSelf != visible) // 검은 여백 상태 확인
            { // 전환 시작
                letterbox.SetActive(visible); // 타이틀 여백 표시
            } // 전환 종료
            if (viewport.activeSelf != visible) // 현재 표시 상태 확인
            { // 전환 시작
                viewport.SetActive(visible); // 다른 화면의 UI 차단
            } // 전환 종료
            if (!visible) // 표시 여부 확인
            { // 생략 시작
                return; // 숨겨진 화면 갱신 생략
            } // 생략 종료
            startText.canvasRenderer.SetColor(flow.TitleFocus == 0 ? selectedTint : Color.white); // 편집 색상을 보존한 시작 강조
            settingsText.canvasRenderer.SetColor(flow.TitleFocus == 1 ? selectedTint : Color.white); // 편집 색상을 보존한 설정 강조
            UpdateBackgroundCrop(); // 교체 이미지의 표시 비율 갱신
            if (noticePanel != null && noticeText != null) // 안내 참조 확인
            { // 안내 시작
                bool hasNotice = !string.IsNullOrEmpty(flow.MeasurementNotice); // 측정 저장 안내 확인
                noticePanel.SetActive(hasNotice); // 안내 표시 여부
                noticeText.text = flow.MeasurementNotice; // 현재 저장 안내
            } // 안내 종료
        } // 갱신 종료
        public bool HandlePointer(Vector2 position) // 실제 버튼 위치의 클릭 처리
        { // 처리 시작
            if (!IsUsable || !viewport.activeInHierarchy) // 표시와 참조 확인
            { // 생략 시작
                return false; // 숨겨진 버튼 차단
            } // 생략 종료
            Camera eventCamera = titleCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : titleCanvas.worldCamera; // 캔버스 좌표 카메라
            if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)viewport.transform, position, eventCamera)) // 실제 화면 영역 확인
            { // 생략 시작
                return false; // 여백 클릭 차단
            } // 생략 종료
            if (startButton.isActiveAndEnabled && startButton.IsInteractable() && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)startButton.transform, position, eventCamera)) // 시작 버튼 영역 확인
            { // 시작 처리
                return InvokeButton(0); // 같은 버튼 이벤트 처리
            } // 시작 종료
            if (settingsButton.isActiveAndEnabled && settingsButton.IsInteractable() && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)settingsButton.transform, position, eventCamera)) // 설정 버튼 영역 확인
            { // 설정 처리
                return InvokeButton(1); // 같은 버튼 이벤트 처리
            } // 설정 종료
            return false; // 버튼 외부 클릭
        } // 처리 종료
        public bool InvokeButton(int index) // 키보드와 포인터 공통 버튼
        { // 실행 시작
            if (!IsUsable || flow == null || flow.InputBlocked || flow.SettlementPending || !viewport.activeInHierarchy || index < 0 || index > 1) // 화면과 입력 상태 확인
            { // 차단 시작
                return false; // 숨겨진 화면 입력 차단
            } // 차단 종료
            Button selected = index == 0 ? startButton : settingsButton; // 선택된 버튼
            if (!selected.isActiveAndEnabled || !selected.IsInteractable()) // 버튼과 상위 상호작용 확인
            { // 차단 시작
                return false; // 비활성 버튼 입력 차단
            } // 차단 종료
            selected.onClick.Invoke(); // 사용자가 편집한 버튼 이벤트
            return true; // 버튼 실행 완료
        } // 실행 종료
        private void UpdateBackgroundCrop() // 교체한 텍스처의 화면 비율 보존
        { // 비율 시작
            Texture texture = background.texture; // 현재 배경 이미지
            RectTransform rect = (RectTransform)background.transform; // 배경 표시 영역
            Vector2 size = rect.rect.size; // 현재 화면 크기
            if (!cropBackground || texture == null || size.x <= 0f || size.y <= 0f || texture.width <= 0 || texture.height <= 0) // 자동 자르기 조건 확인
            { // 생략 시작
                previousBackground = null; // 자동 자르기 재활성 시 재계산
                return; // 사용자 UV 또는 준비 대기
            } // 생략 종료
            if (previousBackground == texture && previousSize == size) // 기존 화면 비율 확인
            { // 생략 시작
                return; // 변경 없는 프레임 생략
            } // 생략 종료
            previousBackground = texture; // 현재 이미지 보관
            previousSize = size; // 현재 표시 크기 보관
            float textureRatio = (float)texture.width / texture.height; // 원본 가로세로 비율
            float viewRatio = size.x / size.y; // 표시 가로세로 비율
            float width = textureRatio > viewRatio ? viewRatio / textureRatio : 1f; // 원본 가로 표시 비율
            float height = textureRatio < viewRatio ? textureRatio / viewRatio : 1f; // 원본 세로 표시 비율
            background.uvRect = new Rect((1f - width) * 0.5f, (1f - height) * 0.5f, width, height); // 중앙 영역을 비율대로 표시
        } // 비율 종료
        private void StartSelected() // 시작 버튼 요청
        { // 요청 시작
            if (flow != null) // 흐름 연결 확인
            { // 실행 시작
                flow.ActivateTitleButton(0); // 기존 시작 경로
            } // 실행 종료
        } // 요청 종료
        private void SettingsSelected() // 설정 버튼 요청
        { // 요청 시작
            if (flow != null) // 흐름 연결 확인
            { // 실행 시작
                flow.ActivateTitleButton(1); // 기존 설정 경로
            } // 실행 종료
        } // 요청 종료
        private void ApplyFallbackFonts() // 한글 글꼴 기본값 준비
        { // 준비 시작
            if ((titleText != null && titleText.font == null) || (startText != null && startText.font == null) || (settingsText != null && settingsText.font == null) || (noticeText != null && noticeText.font == null)) // 사용자 글꼴 누락 확인
            { // 대체 시작
                string[] names = new string[] // 대체 글꼴 목록
                { // 목록 시작
                    "Malgun Gothic", // 윈도우 한글 글꼴
                    "Arial" // 대체 영문 글꼴
                }; // 목록 종료
                fallbackFont = Font.CreateDynamicFontFromOSFont(names, 24); // 시스템 한글 대체 글꼴
                if (titleText != null && titleText.font == null) // 제목 글꼴 확인
                { // 적용 시작
                    titleText.font = fallbackFont; // 제목 대체 글꼴
                } // 적용 종료
                if (startText != null && startText.font == null) // 시작 글꼴 확인
                { // 적용 시작
                    startText.font = fallbackFont; // 시작 대체 글꼴
                } // 적용 종료
                if (settingsText != null && settingsText.font == null) // 설정 글꼴 확인
                { // 적용 시작
                    settingsText.font = fallbackFont; // 설정 대체 글꼴
                } // 적용 종료
                if (noticeText != null && noticeText.font == null) // 안내 글꼴 확인
                { // 적용 시작
                    noticeText.font = fallbackFont; // 안내 대체 글꼴
                } // 적용 종료
            } // 대체 종료
        } // 준비 종료
        private void OnDestroy() // 연결 정리
        { // 정리 시작
            if (flow != null) // 흐름 연결 확인
            { // 해제 시작
                flow.DetachTitleView(this); // 기존 흐름 등록 해제
            } // 해제 종료
            if (startButton != null) // 시작 버튼 확인
            { // 해제 시작
                startButton.onClick.RemoveListener(StartSelected); // 시작 이벤트 해제
            } // 해제 종료
            if (settingsButton != null) // 설정 버튼 확인
            { // 해제 시작
                settingsButton.onClick.RemoveListener(SettingsSelected); // 설정 이벤트 해제
            } // 해제 종료
            if (fallbackFont != null) // 대체 글꼴 확인
            { // 제거 시작
                Destroy(fallbackFont); // 임시 시스템 글꼴 해제
            } // 제거 종료
        } // 정리 종료
    } // 구성 종료
} // 영역 종료
