using UnityEngine; // 게임 시간과 현재 설정
using UnityEngine.InputSystem; // ESC와 키 변경 입력
using UnityEngine.SceneManagement; // 포기 후 메인 화면으로 이동
using UnityEngine.UI; // 기존 씬의 버튼과 수치

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(-150)] // 비행보다 먼저 일시정지 처리
    public sealed class FlightPauseMenu : MonoBehaviour // 출격 일시정지와 조작·소리 설정
    {
        [SerializeField] private GameObject panel; // 전체 정지 화면
        [SerializeField] private GameObject controls; // 오른쪽 키 설명
        [SerializeField] private GameObject settings; // 오른쪽 환경 설정
        [SerializeField] private GameObject keySettings; // 설정의 키 변경 탭
        [SerializeField] private GameObject audioSettings; // 설정의 소리·감도 탭
        [SerializeField] private GameObject confirm; // 출격 포기 확인
        [SerializeField] private Text confirmLabel; // 포기 전후 안내
        [SerializeField] private Text keyHint; // 키 변경 입력 안내
        [SerializeField] private Button continueButton; // 계속하기
        [SerializeField] private Button restartButton; // 다시하기
        [SerializeField] private Button settingsButton; // 오른쪽 설정 전환
        [SerializeField] private Button abandonButton; // 포기 확인 열기
        [SerializeField] private Button acceptButton; // 포기 확정
        [SerializeField] private Button cancelButton; // 포기 취소
        [SerializeField] private Button guideButton; // 설정에서 키 설명
        [SerializeField] private Button keyTab; // 키 지정 탭
        [SerializeField] private Button audioTab; // 소리·감도 탭
        [SerializeField] private Button resetKeys; // 기본 키 복원
        [SerializeField] private Button[] bindingButtons; // 동작별 키 변경 버튼
        [SerializeField] private Text[] bindingLabels; // 설정에 표시한 현재 키
        [SerializeField] private Text[] guideLabels; // 조작 설명 옆 키 이미지 글자
        [SerializeField] private FlightKeyboardHighlights keyboardArt; // 현재 키를 강조하는 평면 그림
        [SerializeField] private Slider volume; // 실제 마스터 음량
        [SerializeField] private Slider sensitivity; // 실제 카메라 회전 감도
        [SerializeField] private Button muteButton; // 소리 켜기·끄기
        [SerializeField] private Text muteLabel; // 소리 상태
        [SerializeField] private Text volumeLabel; // 마스터 음량의 숫자
        [SerializeField] private Text sensitivityLabel; // 마우스 감도의 숫자
        [SerializeField] private FlightBindings bindings; // 표시와 실제 조작의 공통 설정
        [SerializeField] private FirstFlightController pilot; // 마우스 입력 설정
        [SerializeField] private FlightThreatPractice run; // 출격과 경험치 초기화
        [SerializeField] private FlightEnemyEncounter encounter; // 웨이브와 시간 초기화
        [SerializeField] private FlightSkillController skills; // 자원과 쿨타임 초기화
        [SerializeField] private FlightMagicDeck deck; // 마법 카드 초기화
        [SerializeField] private FlightCombatHud hud; // 실제 스킬 키캡
        [SerializeField] private FlightBroomWorkshop broomWorkshop; // 실제 조립 화면
        [SerializeField] private Button broomButton; // 빗자루 조립 화면 입장
        [SerializeField] private FlightCraftedSpellEditor spellEditor; // 제작 마법 시험 화면
        [SerializeField] private Button spellEditorButton; // 제작 마법 편집 화면 입장
        [SerializeField] private Button titleButton; // 포기한 출격의 메인 화면 복귀
        private int capture = -1; // 설정에서 새 키를 기다리는 동작
        private int closeFrame = -1; // 메뉴 클릭이 전투에 넘어가지 않는 프레임
        private bool abandon; // 이미 포기한 출격
        private bool muted; // 전체 소리 끄기
        private bool opened; // 이 메뉴가 전투를 멈춘 상태
        private bool changedSettings; // 실제 사용자가 변경한 저장 항목
        private float priorScale; // 일시정지 전의 전투 시간
        private bool priorAudioPause; // 일시정지 전의 음원 상태
        private bool priorCursor; // 이전 마우스 표시
        private CursorLockMode priorLock; // 이전 마우스 잠금
        private GameObject menuButtons; // 조립 화면 뒤에 숨길 기존 정지 메뉴
        [SerializeField] private FlightTrainingMode training; // 자유 훈련에서만 소유권을 공유할 별도 설정

        private void Start() // 씬에 배치한 정지 버튼을 실제 동작과 연결
        {
            continueButton.onClick.AddListener(RequestResume); // 계속하기
            restartButton.onClick.AddListener(Restart); // 현재 출격 다시 시작
            settingsButton.onClick.AddListener(ShowSettings); // 오른쪽만 설정으로 전환
            abandonButton.onClick.AddListener(AskAbandon); // 출격 포기 확인
            acceptButton.onClick.AddListener(Abandon); // 확정 버튼
            cancelButton.onClick.AddListener(ShowControls); // 키 설명으로 돌아가기
            guideButton.onClick.AddListener(ShowControls); // 설정에서 조작 설명 보기
            keyTab.onClick.AddListener(ShowKeySettings); // 키 변경 탭
            audioTab.onClick.AddListener(ShowAudioSettings); // 소리와 시점 감도 탭
            resetKeys.onClick.AddListener(bindings.ResetBindings); // 기본값으로 복구
            if (titleButton != null) // 메인 화면 이동이 연결된 항로
            {
                titleButton.onClick.AddListener(ReturnToTitle); // 포기한 출격의 복귀
            }
            if (broomWorkshop != null) // 조립을 연결한 실전 항로
            {
                menuButtons = panel.transform.Find("MenuButtons").gameObject; // 씬에 배치한 정지 버튼 열
                broomWorkshop.Initialize(ShowControls); // 조립에서 조작 안내로 복귀
                broomButton.onClick.AddListener(ShowWorkshop); // 조립 화면 열기
            }
            if (spellEditor != null) // 실전 항로의 제작 마법
            {
                spellEditor.Initialize(ShowControls); // 편집에서 조작 안내 복귀
                spellEditorButton.onClick.AddListener(TryShowSpells); // 전투 전 또는 독립 시험에서만 제작 화면 열기
            }
            for (int index = 0; index < bindingButtons.Length; index++) // 설정에 표시된 실제 조작
            {
                int action = index; // 버튼별 입력을 기억
                bindingButtons[index].onClick.AddListener(() => BeginBinding(action)); // 새 키 입력 대기
            }

            bool load = FlightBindings.UsesPreferences; // 격리 환경에서 사용자 설정 분리
            volume.SetValueWithoutNotify(load ? PlayerPrefs.GetFloat("ProjectG.Flight.Volume", 1f) : 1f); // 저장된 전체 음량
            sensitivity.SetValueWithoutNotify(load ? PlayerPrefs.GetFloat("ProjectG.Flight.Sensitivity", 100f) : 100f); // 저장된 감도 비율
            muted = load && PlayerPrefs.GetInt("ProjectG.Flight.Muted", 0) != 0; // 저장된 소리 끄기
            volume.onValueChanged.AddListener(_ => ChangeSettings()); // 실제 음량 적용
            sensitivity.onValueChanged.AddListener(_ => ChangeSettings()); // 실제 시점 감도 적용
            muteButton.onClick.AddListener(ToggleMute); // 전체 소리 토글
            ApplySettings(); // 시작부터 같은 환경 설정 적용
            bindings.Changed += RefreshBindings; // 변경과 원복을 실제 모든 표기에 반영
            RefreshBindings(); // 기본 또는 저장된 키로 표시
        }

        private void OnDestroy() // 다른 씬으로 이동할 때 이전 상태 복원
        {
            bindings.Changed -= RefreshBindings; // 키 변경 참조 반환
            Resume(); // 열린 경우에만 시간과 포인터 복원
        }

        private void Update() // ESC와 바인딩 입력
        {
            if (training != null && training.BlocksPause) // 연습 메뉴가 같은 ESC 입력을 받는 경우
            {
                return; // 원래 전투의 일시정지 입력과 중복 방지
            }
            if (closeFrame >= 0 && Time.frameCount > closeFrame) // 버튼을 누른 프레임과 전투 입력 분리
            {
                Resume(); // 실제 게임 시간 재개
                return; // 재개 프레임에 이전 입력 중복 방지
            }
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard == null) // 입력 장치 미연결
            {
                return; // 버튼만 사용할 수 있는 상태
            }
            if (capture >= 0) // 설정에서 새 키 입력 대기
            {
                ReadBinding(keyboard); // ESC 취소 또는 실제 입력의 키 변경
                return; // 메뉴 닫기와 동시에 처리하지 않음
            }
            if (keyboard.backspaceKey.wasPressedThisFrame) // 두 메뉴를 포함한 새 출격
            {
                Restart(); // 버튼과 동일한 출격 초기화
                return; // 해당 프레임의 정지 전환 제외
            }
            if (!keyboard.escapeKey.wasPressedThisFrame) // ESC가 아닌 일반 입력
            {
                return; // 정지 상태 유지
            }
            if (opened) // 메뉴가 이미 열린 상태
            {
                if ((broomWorkshop != null && broomWorkshop.gameObject.activeSelf) || (spellEditor != null && spellEditor.gameObject.activeSelf)) // 조립 또는 마법 편집 화면
                {
                    ShowControls(); // 확정하지 않은 미리보기 취소
                    return; // 정지 메뉴에 남기
                }
                if (confirm.activeSelf && !abandon) // 포기 확인 중 취소
                {
                    ShowControls(); // 오른쪽 키 설명으로 복귀
                    return; // 메뉴 정지 유지
                }
                RequestResume(); // 계속할 수 있는 경우 메뉴 닫기
            }
            else if (Time.timeScale > 0f) // 레벨업 선택과 정지 화면 중복 방지
            {
                Open(); // 실제 ESC 일시정지
            }
        }

        private void Open() // 상태를 보관하고 정지 화면 열기
        {
            priorScale = Time.timeScale; // 직전 전투 시간
            priorLock = Cursor.lockState; // 조준 마우스 상태
            priorCursor = Cursor.visible; // 전투의 마우스 표시
            priorAudioPause = AudioListener.pause; // 직전 오디오 진행 상태
            opened = true; // 현재 메뉴의 정지 소유권
            abandon = false; // 새로 열린 메뉴
            Time.timeScale = 0f; // 비행·탄막·시간 정지
            AudioListener.pause = true; // 전투 음원의 재생 정지
            Cursor.lockState = CursorLockMode.None; // 화면에서 버튼 선택
            Cursor.visible = true; // 마우스 포인터 표시
            continueButton.interactable = true; // 포기 이전에 재생 가능
            abandonButton.interactable = true; // 현재 출격을 포기할 수 있음
            settingsButton.interactable = true; // 오른쪽 환경 설정 표시 가능
            if (spellEditorButton != null) // 제작 마법 화면을 가진 씬
            {
                spellEditorButton.interactable = CanEditSpells(); // 전투 중 편집과 장착 변경 차단
            }
            ShowControls(); // 먼저 키보드 설명 표시
            panel.SetActive(true); // 전체 화면 표시
            FlightAudio.Play(FlightSfx.UiConfirm); // 전투가 정지해도 들리는 메뉴 진입
        }

        private void RequestResume() // 문 닫기와 같은 프레임의 카드 사용 방지
        {
            if (opened && !abandon) // 포기한 출격은 다시하기 필요
            {
                FlightAudio.Play(FlightSfx.UiBack); // 버튼 또는 키로 전투에 돌아가는 신호
                closeFrame = Time.frameCount; // 다음 프레임으로 재생 예약
            }
        }

        private void Resume() // 실제 게임 시간과 마우스 복원
        {
            if (!opened) // 다른 메뉴의 일시정지 유지
            {
                return; // 이 메뉴가 멈추지 않은 경우
            }
            opened = false; // 현재 메뉴의 정지 해제
            closeFrame = -1; // 이전 예약 초기화
            capture = -1; // 키 변경 대기 취소
            if (panel != null) // 빌드 종료 중 이미 제거된 패널은 제외
            {
                panel.SetActive(false); // 전투 화면에 복귀
            }
            Time.timeScale = priorScale; // 전투 시간 이어서 재생
            AudioListener.pause = priorAudioPause; // 이전 소리 재생 상태로 복귀
            Cursor.lockState = priorLock; // 카메라가 쓰던 잠금 복원
            Cursor.visible = priorCursor; // 전투 커서 표시
            if (changedSettings && FlightBindings.UsesPreferences) // 사용자 변경한 환경 설정만 저장
            {
                PlayerPrefs.Save(); // 다음 실행과 같은 감도·음량
            }
            changedSettings = false; // 저장 완료
        }

        private void Restart() // 다시하기 버튼과 BKSP의 공통 출격 초기화
        {
            Resume(); // 메뉴가 보였다면 우선 정리
            if (training != null && training.RestartActive()) // 실제 자유 시험의 현재 패턴과 표적 복원
            {
                return; // 별도 연습 환경의 초기화만 수행
            }
            if (encounter.IsExpedition && (encounter.IsStageComplete || FlightExpedition.Current == null)) // 이미 확정한 탐험 전투 반복 지급 방지
            {
                LeaveExpedition(); // 종료 상태의 기존 지도에서 다음 지점 선택
                return; // 완료한 전투의 재시작 대신 지도
            }
            if (encounter.Story != null)
            {
                encounter.Story.Conclude(false, FlightStoryFailureOrigin.Retry);
                if (encounter.StoryNeedsReceipt) return;
            }
            run.RestartRun(); // 이전 정산이 확정된 뒤 체력·레벨·드롭 초기화
            encounter.RestartTrial(); // 첫 웨이브와 타이머
            skills.ResetRun(); // 마력·필살기·쿨타임
            deck.ResetRun(); // 마법 카드 원래 순서
        }

        private void ShowControls() // 오른쪽에 키와 사용법 표시
        {
            capture = -1; // 설정에서 입력 대기 중단
            if (broomWorkshop != null) // 조립 화면을 지원하는 실전 항로
            {
                broomWorkshop.gameObject.SetActive(false); // 후보 미리보기 닫기
                menuButtons.SetActive(true); // 기존 정지 버튼 열 돌아오기
            }
            if (spellEditor != null) // 제작 화면을 지원하는 실전 항로
            {
                spellEditor.gameObject.SetActive(false); // 별도 미리보기와 투사체 정리
            }
            controls.SetActive(true); // 키보드와 키 목록 표시
            settings.SetActive(false); // 오른쪽 설정 숨김
            confirm.SetActive(false); // 포기 확인 숨김
        }

        private void ShowWorkshop() // 조립 작업 중 기존 안내 감춤
        {
            controls.SetActive(false); // 조작 설명 패널 전환
            menuButtons.SetActive(false); // 뒤에 비치는 중복 정지 버튼 숨김
            broomWorkshop.Show(); // 현재 장착과 후보 보기
        }

        private bool CanEditSpells() // 실제 전투 중의 제작 편집 제한
        {
            return encounter == null || encounter.IsPreparing || SceneManager.GetActiveScene().name == FlightFirstSortieGuide.SceneName; // 출격 전 준비 또는 독립 연습에서만 허용
        }

        private void TryShowSpells() // 실제 상태를 다시 확인한 제작 화면 진입
        {
            if (!CanEditSpells()) // 이미 전투가 시작된 경우
            {
                return; // 편집 화면과 영구 저장 변경 차단
            }
            ShowSpells(); // 유효한 준비와 독립 연습에서만 편집
        }

        private void ShowSpells() // 실전 화면에서 연습과 편집으로 전환
        {
            controls.SetActive(false); // 오른쪽 조작 설명 감춤
            menuButtons.SetActive(false); // 왼쪽 기존 버튼 감춤
            spellEditor.Show(); // 제작 조합과 시험 표적 표시
        }

        private void ShowSettings() // 오른쪽만 환경 설정으로 전환
        {
            FlightAudio.Play(FlightSfx.UiConfirm); // 멈춘 소리 위에서 설정 버튼 신호
            controls.SetActive(false); // 키보드 설명에서 전환
            settings.SetActive(true); // 설정 제목과 탭 표시
            confirm.SetActive(false); // 포기 확인 숨김
            ShowKeySettings(); // 키 변경을 첫 탭으로 표시
        }

        private void ShowKeySettings() // 동작별 키 설정 표시
        {
            capture = -1; // 이전 키 입력 대기 없음
            keyHint.text = "바꿀 동작을 고른 뒤 새 키를 누르세요."; // 조작 설정 방법
            keySettings.SetActive(true); // 조작 설정 행
            audioSettings.SetActive(false); // 다른 설정 탭 숨김
        }

        private void ShowAudioSettings() // 실제 적용되는 소리와 시점 감도
        {
            capture = -1; // 이전 설정의 입력 대기 끝
            keySettings.SetActive(false); // 키 설정 탭 숨김
            audioSettings.SetActive(true); // 소리·감도 행 표시
        }

        private void AskAbandon() // 현재 출격 중단 확정 여부
        {
            capture = -1; // 키 설정 대기 중단
            controls.SetActive(false); // 오른쪽 안내에서 전환
            settings.SetActive(false); // 오른쪽 설정 숨김
            confirm.SetActive(true); // 포기 확인 패널 표시
            confirmLabel.text = "현재 출격을 포기할까요?"; // 선택의 확인 문구
            acceptButton.gameObject.SetActive(true); // 포기 확정
            cancelButton.gameObject.SetActive(true); // 게임 유지
            if (titleButton != null) // 실전의 복귀 버튼 사용
            {
                titleButton.gameObject.SetActive(false); // 포기를 확정하기 전에는 비활성
            }
        }

        private void Abandon() // 끝난 출격을 다시 재생할 수 없도록 표시
        {
            if (encounter.IsExpedition) // 탐험 포기는 현재 임시 진행만 정리
            {
                FlightExpedition.Cancel("탐험 포기 · 임시 재화와 강화 초기화"); // 저장한 지도 체크포인트 해제
                encounter.StopStage(); // 남은 적과 탄막 정리
                LeaveExpedition(); // 실제 탐험 지도 복귀
                return; // 본편의 중단 정산은 따로 유지
            }
            encounter.Conclude(false); // 첫 전투에 참가한 출격의 참여 보상 확정
            if (encounter.StoryNeedsReceipt)
            {
                confirmLabel.text = "정산 기록을 저장하지 못했습니다. 다시 확인해 주세요.";
                return;
            }
            encounter.StopStage(); // 실전 포기 시 두 시간의 최종값 고정
            abandon = true; // 복귀 차단
            continueButton.interactable = false; // 기존 출격 계속하기 비활성
            abandonButton.interactable = false; // 중복 중단 방지
            settingsButton.interactable = false; // 포기 결과 유지
            confirmLabel.text = "출격을 포기했습니다.\n다시하기로 새 출격을 시작하세요."; // 시험 씬의 종료 상태
            acceptButton.gameObject.SetActive(false); // 확정 완료
            cancelButton.gameObject.SetActive(false); // 종료 뒤 이전 출격 복귀 불가
            if (titleButton != null) // 메인 화면이 연결된 정식 출격
            {
                titleButton.gameObject.SetActive(true); // 포기 확인 후 메인 화면 복귀
            }
        }

        private void LeaveExpedition() // 종료한 탐험 전투에서 같은 지도 열기
        {
            FlightSortieChoice.RememberPilot(pilot.GetComponent<FlightWeapon>().ActivePilot); // 전투에 참가한 조종사 복원
            FlightExpedition.RequestMap(); // 결과 안내와 현재 위치의 지도
            SceneManager.LoadSceneAsync(FlightSortieChoice.BriefingScene); // 별도 출격 선택 흐름으로 복귀
        }

        private void ReturnToTitle() // 종료한 출격에서 메인 화면으로 복귀
        {
            if (abandon) // 실제로 포기가 확정된 출격
            {
                SceneManager.LoadSceneAsync(FlightTitleMenu.SceneName); // 일시정지를 정리하며 타이틀 씬 진입
            }
        }

        private void BeginBinding(int action) // 다음 키 입력으로 바꿀 동작
        {
            capture = action; // 실제 조작 슬롯
            keyHint.text = "새 키를 누르세요. ESC로 취소합니다."; // 키 입력 대기 상태
        }

        private void ReadBinding(Keyboard keyboard) // 키보드 입력을 실제 새 조작으로 저장
        {
            if (keyboard.escapeKey.wasPressedThisFrame) // 대기 중 ESC는 취소
            {
                capture = -1; // 기존 키 유지
                keyHint.text = "키 변경을 취소했습니다."; // 사용자 입력 결과
                return; // 메뉴는 그대로 정지
            }
            foreach (var key in keyboard.allKeys) // 실제 눌린 물리 키 찾기
            {
                if (!key.wasPressedThisFrame) // 이번 프레임에 눌리지 않은 키
                {
                    continue; // 다른 물리 키 검사
                }
                if (bindings.TrySet((FlightAction)capture, key.keyCode)) // 유효하고 비어 있는 키
                {
                    capture = -1; // 새 설정 반영 완료
                    keyHint.text = "새 키를 적용했습니다."; // 성공한 입력 확인
                }
                else // 다른 행동에 할당됐거나 메뉴 예약 키
                {
                    keyHint.text = "이미 쓰는 키이거나 변경할 수 없는 키입니다."; // 기존 조작 유지
                }
                return; // 한 번의 키 입력만 사용
            }
        }

        private string Bound(FlightAction action) // 현재 유효한 키의 안내 내용
        {
            return FlightBindings.Label(bindings.GetKey(action)); // 키캡과 카드 슬롯에 쓰는 같은 문자
        }

        private void RefreshBindings() // 키 그림·설명·실제 HUD를 즉시 동기화
        {
            for (int index = 0; index < bindingLabels.Length; index++) // 모든 조작 안내
            {
                string value = Bound((FlightAction)index); // 사용 중인 실제 키
                bindingLabels[index].text = value; // 설정 버튼에 표시
                guideLabels[index].text = value; // 키 설명 옆의 그림에 표시
            }
            hud.SetSkillKeys(Bound(FlightAction.Special), Bound(FlightAction.Common), Bound(FlightAction.Ultimate)); // 전투 스킬 슬롯의 새 키
            keyboardArt.Show(bindings); // 실제 키보드 그림에서 재배치한 키 강조
        }

        private void ToggleMute() // 실제 소리의 켜기와 끄기
        {
            muted = !muted; // 새 소리 방침
            ChangeSettings(); // 새 음량과 저장값
        }

        private void ChangeSettings() // UI 입력이 실제 환경 설정에 적용
        {
            ApplySettings(); // 새 값 즉시 반영
            if (FlightBindings.UsesPreferences) // 실제 게임을 할 때만 기록
            {
                PlayerPrefs.SetFloat("ProjectG.Flight.Volume", volume.value); // 마스터 음량 저장
                PlayerPrefs.SetFloat("ProjectG.Flight.Sensitivity", sensitivity.value); // 시점 감도 저장
                PlayerPrefs.SetInt("ProjectG.Flight.Muted", muted ? 1 : 0); // 무음 설정 저장
                changedSettings = true; // 메뉴 종료 시 기록
            }
        }

        private void ApplySettings() // 수치 설정과 현재 하드웨어 효과
        {
            AudioListener.volume = muted ? 0f : volume.value; // 전역 마스터 음량
            pilot.SetSensitivityScale(sensitivity.value / 100f); // 실제 마우스 선회 배율
            volumeLabel.text = $"{Mathf.RoundToInt(volume.value * 100f)}%"; // 읽을 수 있는 소리 크기
            sensitivityLabel.text = $"{Mathf.RoundToInt(sensitivity.value)}%"; // 읽을 수 있는 감도 수치
            muteLabel.text = muted ? "전체 소리 꺼짐" : "전체 소리 켜짐"; // 현재 음소거 상태
        }
    }
}
