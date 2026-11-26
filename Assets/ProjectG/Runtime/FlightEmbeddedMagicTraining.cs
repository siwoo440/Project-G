using System; // 현재 조종사와 편집 조합 조회
using System.Collections; // 연습 씬의 비동기 준비
using UnityEngine; // 실제 비행 카메라와 입력 포커스
using UnityEngine.EventSystems; // 중복 이벤트 시스템 제어
using UnityEngine.InputSystem; // 편집과 전체 화면 복귀 키
using UnityEngine.Rendering; // 그래픽 없는 배치 시험 구분
using UnityEngine.SceneManagement; // 독립 연습 씬의 추가 로드와 해제
using UnityEngine.UI; // 비행 화면과 조작 버튼

namespace ProjectG // 프로젝트 제작 마법 영역
{
    internal sealed class FlightEmbeddedMagicTraining : MonoBehaviour // 제작 화면 안의 독립 실제 비행 연습
    {
        private Transform editor; // 미리보기와 편집 버튼이 있는 화면
        private RawImage view; // 실제 비행 카메라를 표시할 중앙 영역
        private RectTransform viewRect; // 원래 크기와 전체 화면 크기
        private Texture originalTexture; // 기존 정적 시험 화면 보존
        private Vector2 originalPosition; // 중앙 미리보기의 원래 위치
        private Vector2 originalSize; // 중앙 미리보기의 원래 크기
        private int originalSibling; // 다른 제작 버튼 뒤의 원래 렌더 순서
        private Button start; // 내장 비행 세션 시작
        private Button fullscreen; // 같은 세션의 전체 화면 전환
        private Func<FlightBroomPilot> pilot; // 현재 출격 준비에서 선택한 조종사
        private Func<CraftedSpellRecipe> candidate; // 지금 편집 중인 마법 조합
        private Scene trainingScene; // 정식 출격과 분리한 추가 연습 씬
        private FlightTrainingMode training; // 실제 장비와 표적을 가진 자유 훈련
        private RenderTexture texture; // 비행 카메라의 독립 렌더 대상
        private string appliedId; // 현재 연습 세션에 적용한 편집 조합
        private float previousScale; // 제작 화면에 들어올 때의 게임 시간
        private bool previousAudio; // 기존 오디오 정지 상태
        private bool loading; // 비동기 연습 씬 로드 중
        private bool unloading; // 비동기 연습 씬 해제 중
        private bool canceled; // 로드 중 제작 화면을 닫은 상태
        private bool focused; // 비행 화면이 키와 마우스를 소유하는 상태
        private bool expanded; // 같은 연습을 전체 화면으로 보는 상태
        private bool allowed; // 현재 조합의 연구·지원·한도 충족 여부
        private float targetDistance = 10f; // 실제 시험 표적의 선택 거리
        internal bool IsBusy => loading || unloading || training != null || trainingScene.IsValid() && trainingScene.isLoaded; // 씬 로드와 해제 동안 다른 연습 진입 차단
        internal bool IsReady => training != null; // 실제 비행과 표적이 준비된 상태
        internal string Status => $"표적 체력 {training.PreviewTarget.Health}/{training.PreviewTarget.MaxHealth}  ·  마력 {training.PreviewSkills.Mana}/{training.PreviewSkills.MaxMana}"; // 현재 보이는 실제 표적과 자원

        internal void Initialize(Transform owner, Button template, Func<FlightBroomPilot> currentPilot, Func<CraftedSpellRecipe> currentRecipe) // 제작 화면과 실제 연습 자원 연결
        {
            editor = owner; // 편집 화면의 실제 UI
            pilot = currentPilot; // 출격 준비에서 선택한 조종사
            candidate = currentRecipe; // 저장하지 않은 현재 구성
            view = editor.Find("RangeView").GetComponent<RawImage>(); // 기존 중앙 3D 미리보기
            viewRect = (RectTransform)view.transform; // 미리보기의 실제 사각형
            originalTexture = view.texture; // 기존 시험용 정적 화면
            originalPosition = viewRect.anchoredPosition; // 원래 중앙 위치
            originalSize = viewRect.sizeDelta; // 원래 미리보기 크기
            originalSibling = viewRect.GetSiblingIndex(); // 원래 UI 배치 순서
            view.raycastTarget = true; // 중앙 화면의 실제 포인터 선택 허용
            Button focus = view.gameObject.GetComponent<Button>() ?? view.gameObject.AddComponent<Button>(); // 비행 화면의 입력 포커스 버튼
            focus.targetGraphic = view; // 중앙 화면을 클릭 대상에 사용
            focus.onClick.AddListener(FocusView); // 첫 클릭으로 입력 포커스 획득
            start = Clone(template, "PreviewFlight", new Vector2(125f, -339f), new Vector2(200f, 52f), "비행 미리보기"); // 제작 화면의 실제 연습 시작
            start.onClick.AddListener(Begin); // 현재 조합으로 독립 세션 준비
            fullscreen = Clone(template, "PreviewFullscreen", new Vector2(90f, -216f), new Vector2(200f, 40f), "전체 화면"); // 중앙 연습을 확대하는 버튼
            fullscreen.onClick.AddListener(ToggleFullscreen); // 같은 연습 세션의 화면 크기만 전환
            fullscreen.gameObject.SetActive(false); // 연습이 없으면 전체 화면 사용 금지
        }

        internal void SetCandidate(CraftedSpellRecipe recipe, bool valid) // 편집한 구성을 같은 연습 세션에 반영
        {
            allowed = valid; // 현재 연구와 실제 발사 지원 여부
            start.interactable = valid || training != null; // 유효한 조합으로 세션 시작
            if (training == null || appliedId == (valid ? recipe?.Id : null)) // 아직 준비 전이거나 같은 편집 구성
            {
                return; // 진행 중인 세션을 불필요하게 초기화하지 않음
            }
            CraftedSpellRecipe selected = valid ? recipe : null; // 잠금 또는 비호환 구성은 빈 시험 슬롯
            training.PreviewBook.SetTrainingRecipe(selected); // 정식 저장과 분리한 현재 조합
            training.PreviewDeck.ResetRun(); // 이전 시험 탄막과 쿨타임 제거
            appliedId = selected?.Id; // 실제 연습에서 사용 중인 구성
        }

        internal bool HandleKey(Keyboard keyboard) // 편집 화면과 비행 포커스의 입력 우선순위
        {
            if (training == null || keyboard == null) // 활성 연습 또는 키보드가 없는 상태
            {
                return false; // 기존 제작 화면의 단축키 사용
            }
            if (keyboard.tabKey.wasPressedThisFrame) // 같은 비행 세션의 화면 크기 전환
            {
                ToggleFullscreen(); // 현재 위치와 자원을 유지한 확대 또는 축소
                return true; // 같은 키의 다른 처리 방지
            }
            if (!keyboard.escapeKey.wasPressedThisFrame) // 포커스 해제나 복귀 입력이 아닌 경우
            {
                return false; // 원래 편집 키 처리
            }
            if (focused) // 비행 입력을 소유하고 있던 상태
            {
                SetFocus(false); // 마우스를 편집 화면에 반환
            }
            else if (expanded) // 전체 화면에서 편집 화면으로 돌아갈 상태
            {
                ToggleFullscreen(); // 같은 연습의 중앙 크기 복원
            }
            else // 중앙 미리보기에서 연습 종료
            {
                Stop(); // 실제 제작 화면과 정식 출격 상태 복원
            }
            return true; // 같은 Esc로 제작 화면까지 닫지 않음
        }

        internal void SetTargetDistance(float meters) // 제작 화면에서 선택한 실제 표적 거리
        {
            targetDistance = meters; // 아직 로드 중인 연습에도 적용할 거리
            if (training != null) // 실제 표적이 이미 준비된 상태
            {
                training.SetPreviewTargetDistance(meters); // 비행 카메라의 현재 조준선에 배치
            }
        }

        internal void ResetPreview() // 사용자가 선택한 실제 비행 연습 초기화
        {
            if (training == null) // 내장 연습이 없는 경우
            {
                return; // 기존 시험 발사 초기화만 사용
            }
            training.BeginEmbeddedPreview(); // 실제 비행 위치·표적·마력과 탄막 초기화
            training.SetPreviewTargetDistance(targetDistance); // 선택한 시험 거리를 실제 표적에 적용
            SetFocus(false); // 편집 화면의 포인터와 시간 동결 복원
        }

        internal void Stop() // 제작 화면을 떠날 때 독립 세션과 자원 회수
        {
            canceled = true; // 로드가 끝나더라도 새 연습 시작 금지
            focused = false; // 비행 입력 소유권 해제
            if (training != null) // 실제 연습을 이미 시작한 상태
            {
                training.PreviewPilot.SetSortieEnded(true); // 씬 해제 전 비행과 자동탄 차단
                training.PreviewDeck.enabled = false; // 같은 프레임의 마법 입력 차단
            }
            if (expanded) // 전체 화면으로 확대된 상태
            {
                SetExpanded(false); // 원래 중앙 UI 배치 복구
            }
            if (view != null) // 제작 화면이 아직 살아 있는 상태
            {
                view.texture = originalTexture; // 기존 시험 표적의 렌더 화면 복원
            }
            if (texture != null) // 실제 그래픽 장치에서 만든 독립 텍스처
            {
                texture.Release(); // GPU 렌더 대상 반환
                Destroy(texture); // 임시 텍스처 회수
                texture = null; // 중복 회수 방지
            }
            if (training != null || loading) // 이 연습이 바꾼 시간과 커서
            {
                Time.timeScale = previousScale; // 정식 메뉴의 이전 게임 시간
                AudioListener.pause = previousAudio; // 원래 오디오 상태
                Cursor.lockState = CursorLockMode.None; // 편집할 수 있는 포인터 복원
                Cursor.visible = true; // 제작 화면에서 마우스 표시
            }
            training = null; // 정식 출격과 분리한 연습 참조 제거
            appliedId = null; // 다음 연습의 조합 초기화
            if (fullscreen != null) // 제작 화면이 아직 살아 있는 경우
            {
                fullscreen.gameObject.SetActive(false); // 연습 종료 후 전체 화면 숨김
            }
            if (!loading && !unloading && trainingScene.IsValid() && trainingScene.isLoaded) // 이 구성 요소가 추가로 연 연습 씬
            {
                StartCoroutine(UnloadTraining(trainingScene)); // 실제 해제 완료까지 다른 연습 진입 차단
            }
        }

        private IEnumerator UnloadTraining(Scene scene) // 독립 연습 씬의 안전한 비동기 해제
        {
            unloading = true; // 같은 씬의 중복 해제와 재진입 차단
            yield return SceneManager.UnloadSceneAsync(scene); // 실제 정산 없이 독립 씬 해제
            trainingScene = default; // 해제가 끝난 씬 참조 제거
            unloading = false; // 새 연습 진입 허용
        }

        private void Begin() // 현재 캐릭터와 미저장 조합의 연습 준비
        {
            if (training != null) // 이미 시작한 같은 연습 세션
            {
                SetFocus(true); // 현재 세션의 비행 입력에 복귀
                return; // 새 씬이나 저장을 만들지 않음
            }
            if (loading || !allowed || candidate() == null || SceneManager.GetSceneByName(FlightFirstSortieGuide.SceneName).isLoaded) // 중복 로드·잠금·다른 연습 씬
            {
                return; // 기존 화면과 다른 연습을 보존
            }
            previousScale = Time.timeScale; // 세션 종료 뒤 복원할 게임 시간
            previousAudio = AudioListener.pause; // 세션 종료 뒤 복원할 오디오 상태
            canceled = false; // 새 세션의 유효한 로드 요청
            loading = true; // 추가 진입 방지
            FlightTrainingMode.RequestMagic(pilot(), candidate()); // 캐릭터와 미저장 조합의 독립 요청
            StartCoroutine(LoadTraining()); // 화면을 막지 않는 비동기 연습 씬 로드
        }

        private IEnumerator LoadTraining() // 정식 준비 씬을 유지한 실제 비행 씬 로드
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(FlightFirstSortieGuide.SceneName, LoadSceneMode.Additive); // 별도 시험 씬 추가
            yield return operation; // 실제 씬 생성 완료 대기
            trainingScene = SceneManager.GetSceneByName(FlightFirstSortieGuide.SceneName); // 이 세션이 추가한 씬
            yield return null; // 씬의 실제 캐릭터와 장비 시작 처리
            loading = false; // 로드 요청 처리 완료
            if (canceled || !editor.gameObject.activeInHierarchy) // 로드 중 사용자가 편집 화면을 닫은 경우
            {
                Time.timeScale = previousScale; // 연습 시작 중 변경된 시간 복원
                AudioListener.pause = previousAudio; // 원래 오디오 복원
                if (trainingScene.IsValid() && trainingScene.isLoaded) // 이 요청이 만든 씬만 해제
                {
                    yield return SceneManager.UnloadSceneAsync(trainingScene); // 정식 준비 화면 보존
                }
                trainingScene = default; // 해제된 씬 참조 제거
                yield break; // 닫은 화면에 연습을 연결하지 않음
            }
            foreach (GameObject root in trainingScene.GetRootGameObjects()) // 추가한 연습 씬의 실제 개체만 탐색
            {
                if (training == null) // 아직 자유 훈련을 찾지 못한 상태
                {
                    training = root.GetComponentInChildren<FlightTrainingMode>(true); // 실제 비행·장비·표적 소유자
                }
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true)) // 정식 제작 화면과 겹치는 훈련 HUD
                {
                    canvas.enabled = false; // 비행 카메라만 미리보기에 표시
                }
                foreach (EventSystem events in root.GetComponentsInChildren<EventSystem>(true)) // 중복 입력 이벤트 시스템
                {
                    events.enabled = false; // 원래 제작 화면의 이벤트 시스템 유지
                }
                foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true)) // 기존 준비 화면과 중복되는 청취 장치
                {
                    listener.enabled = false; // 독립 카메라 렌더의 중복 오디오 방지
                }
                foreach (FlightPauseMenu menu in root.GetComponentsInChildren<FlightPauseMenu>(true)) // 연습 씬의 별도 정지 메뉴
                {
                    menu.enabled = false; // 제작 화면과 Esc 입력을 중복 처리하지 않음
                }
                foreach (FlightThreatPractice practice in root.GetComponentsInChildren<FlightThreatPractice>(true)) // 독립 훈련의 적탄 단축키
                {
                    practice.enabled = false; // 제작 화면의 입력과 임의 적탄 발생 차단
                }
                foreach (FlightEnemyEncounter encounter in root.GetComponentsInChildren<FlightEnemyEncounter>(true)) // 원래 시험 씬의 웨이브 입력
                {
                    encounter.enabled = false; // 정식 웨이브와 진행 기록 차단
                }
            }
            if (training == null) // 필요한 실제 훈련 구성이 없는 씬
            {
                Stop(); // 임시 씬과 변경된 시간 정리
                yield break; // 추측한 대체 시험 실행 금지
            }
            training.BeginEmbeddedPreview(); // 실제 표적과 격리된 자원의 자유 조종 시작
            training.SetPreviewTargetDistance(targetDistance); // 제작 화면에서 선택한 실제 표적 거리
            Camera camera = training.PreviewCamera; // 실제 비행 시점 카메라
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) // 실제 그래픽 렌더가 가능한 경우
            {
                texture = new RenderTexture(960, 540, 24); // 중앙과 전체 화면에서 공유할 16대9 렌더 대상
                texture.Create(); // 실제 GPU 대상 생성
                camera.targetTexture = texture; // 화면을 건드리지 않는 독립 카메라 출력
                view.texture = texture; // 제작 화면의 중앙 비행 표시
            }
            else // Unity의 그래픽 없는 자동 검증
            {
                camera.enabled = false; // 존재하지 않는 그래픽 장치의 렌더 호출 방지
            }
            appliedId = training.PreviewBook.ActiveSlot(0)?.Id; // 로드 요청 시 실제 적용한 조합
            SetCandidate(candidate(), allowed); // 로드 중 변경된 편집 조합도 현재 세션에 반영
            fullscreen.gameObject.SetActive(true); // 준비된 같은 세션만 확대 가능
            SetFocus(false); // 첫 클릭 전에는 편집 패널이 입력 소유
        }

        private void FocusView() // 중앙 화면의 첫 클릭으로 비행 입력 획득
        {
            if (training == null) // 연습 세션이 아직 준비되지 않은 상태
            {
                Begin(); // 현재 편집 조합으로 실제 연습 시작
                return; // 로드 클릭을 시전 입력으로 쓰지 않음
            }
            SetFocus(true); // 준비된 세션의 실제 비행 입력 획득
        }

        private void SetFocus(bool value) // 편집과 비행의 시간·입력 소유권 전환
        {
            if (training == null) // 실제 비행 세션이 없는 상태
            {
                return; // 다른 화면의 시간과 커서 유지
            }
            focused = value; // 현재 입력 소유권
            training.PreviewPilot.SetSortieEnded(!value); // 포커스가 없으면 비행과 자동탄 정지
            training.PreviewDeck.enabled = false; // 첫 잠금 클릭의 마법 발사 방지
            Time.timeScale = value ? previousScale : 0f; // 편집 중 시험 시간과 탄환 동결
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None; // 비행 조준과 편집 포인터 전환
            Cursor.visible = !value; // 편집 중에만 포인터 표시
            if (value) // 포커스를 얻은 경우
            {
                StartCoroutine(EnableDeckNextFrame()); // 첫 클릭이 지난 뒤 마법 입력 허용
            }
        }

        private IEnumerator EnableDeckNextFrame() // 비행 포커스 클릭과 발사 입력 분리
        {
            yield return null; // 같은 프레임의 클릭 소비
            if (focused && training != null) // 그 사이 편집으로 돌아가지 않은 경우
            {
                training.PreviewDeck.enabled = true; // 실제 좌우 마법 입력 허용
            }
        }

        private void ToggleFullscreen() // 같은 연습 인스턴스의 표시 크기 변경
        {
            if (training != null) // 준비된 실제 비행 세션
            {
                SetExpanded(!expanded); // 카메라와 게임 상태를 유지한 화면 전환
            }
        }

        private void SetExpanded(bool value) // 기존 렌더 텍스처의 표시 사각형 변경
        {
            expanded = value; // 현재 화면 크기 상태
            viewRect.anchoredPosition = value ? Vector2.zero : originalPosition; // 전체 화면 또는 기존 중앙 위치
            viewRect.sizeDelta = value ? new Vector2(1440f, 800f) : originalSize; // 동일 카메라 출력의 표시 크기
            if (value) // 제작 패널 전체를 비행 화면으로 덮는 경우
            {
                viewRect.SetAsLastSibling(); // 기존 편집 위에 비행 화면 표시
                fullscreen.transform.SetAsLastSibling(); // 축소 버튼을 비행 화면 위에 유지
                ((RectTransform)fullscreen.transform).anchoredPosition = new Vector2(570f, 345f); // 전체 화면 오른쪽 위 복귀 버튼
            }
            else // 중앙 제작 화면으로 복귀하는 경우
            {
                viewRect.SetSiblingIndex(originalSibling); // 기존 버튼과 제목의 렌더 순서 복원
                ((RectTransform)fullscreen.transform).anchoredPosition = new Vector2(90f, -216f); // 중앙 미리보기 아래의 원래 버튼
            }
            fullscreen.GetComponentInChildren<Text>().text = value ? "편집 화면  Tab" : "전체 화면  Tab"; // 같은 세션의 현재 화면 전환 안내
        }

        private Button Clone(Button template, string name, Vector2 position, Vector2 size, string label) // 기존 제작 화면의 버튼 외형 공유
        {
            Button button = Instantiate(template, editor, false); // 기존 한국어 버튼 복제
            button.name = name; // 실제 미리보기 조작 식별자
            button.onClick = new Button.ButtonClickedEvent(); // 원본의 기본 구성 선택 제거
            RectTransform rect = (RectTransform)button.transform; // 실제 버튼 위치
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // 제작 화면 중심 기준
            rect.anchoredPosition = position; // 기준 화면의 위치
            rect.sizeDelta = size; // 버튼과 글자 폭
            button.GetComponentInChildren<Text>().text = label; // 사용자에게 보이는 조작 이름
            FlightCraftedUi.FitLabel(button); // 실제 버튼 폭 안에 한글 문구 표시
            return button; // 실제 클릭 이벤트 연결 참조
        }
    }
}
