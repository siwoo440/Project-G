using UnityEngine; // 실제 연습 항로와 조종 상태
using UnityEngine.InputSystem; // 재설정 가능한 키와 준비 복귀
using UnityEngine.SceneManagement; // 실제 준비와 입문 연습의 전환
using UnityEngine.UI; // 씬에 배치한 안내 텍스트와 건너뛰기

namespace ProjectG // 프로젝트 비행 입문
{
    public sealed class FlightFirstSortieGuide : MonoBehaviour // 실제 관찰값에 따른 선택형 훈련
    {
        internal const string SceneName = "FlightPrototype"; // 실전 영구 정산 없는 기존 연습 항로
        private enum Lesson // 조작을 익히는 순서
        {
            Move, // 기체 이동 확인
            Focus, // 집중 유지 확인
            Shoot, // 실제 자동탄 적중 확인
            Graze, // 스침 판정과 자원 확인
            Cast, // 실제 스킬 성공 확인
            Complete // 출격 준비 복귀
        }

        [SerializeField] private FirstFlightController flight; // 실제 기체 이동과 집중 입력
        [SerializeField] private FirstFlightCamera view; // 따라가는 카메라의 중앙 조준선
        [SerializeField] private FlightWeapon weapon; // 선택 조종사의 실제 시험 사격
        [SerializeField] private FlightPilotVitals pilot; // 실제 그레이즈 판정값
        [SerializeField] private FlightSkillController skills; // 마력을 소비한 시전 알림
        [SerializeField] private FlightEnemy target; // 기존에 씬에 배치한 시험 표적
        [SerializeField] private GameObject sandboxTargets; // 입문 훈련 중 사격선을 가리는 자유 시험 표적
        [SerializeField] private FlightThreatPool threatPool; // 실제 빠른 탄막의 충돌 판정
        [SerializeField] private FlightBindings bindings; // 현재 설정 키를 읽는 안내
        [SerializeField] private GameObject panel; // 연습에서만 열리는 별도 안내
        [SerializeField] private Text step; // 순서와 현재 연습
        [SerializeField] private Text instruction; // 행동과 장면에 대한 설명
        [SerializeField] private Button skip; // 연습을 끝내거나 건너뛰는 선택
        [SerializeField] private Text skipText; // 종료 시점에 맞춘 버튼 이름
        private static FlightBroomPilot? requested; // 출격 준비에서 선택했을 때만 시작
        private FlightBroomPilot selected; // 준비 화면에 되돌려줄 조종사
        private Lesson lesson; // 현재 실제 진행
        private bool active; // 일반 연습과 안내 실행 구분
        private bool pointerOpen; // 안내 버튼을 클릭할 수 있는 포인터 상태
        private bool cast; // Q 또는 E의 실제 시전 여부
        private Vector3 start; // 현재 출격의 처음 비행 위치
        private float focusSeconds; // 집중 입력을 유지한 시간
        private float nextAction; // 표적 보정과 그레이즈 탄막 발생 시각
        private int earlierGrazes; // 스침 연습 직전의 판정값

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 에디터 재실행에서 임시 선택 제거
        private static void Reset() // 안내 요청의 도메인 초기화
        {
            requested = null; // 실제 메뉴에서 누르기 전에는 시작하지 않음
        }

        internal static void Request(FlightBroomPilot selectedPilot) // 현재 준비에서 선택한 조종사
        {
            requested = selectedPilot; // 다음 연습 씬이 한 번 읽는 값
        }

        private void Start() // 씬의 원래 일반 연습에는 영향 없이 시작
        {
            if (!requested.HasValue) // 안내를 고르지 않은 기존 연습 씬
            {
                return; // 기본 전투 HUD와 시험 표적 그대로 유지
            }
            selected = requested.Value; // 출격 준비 화면에서 고른 캐릭터
            requested = null; // 다음 실행에 자동으로 다시 나타나지 않음
            active = true; // 안내 입력과 진행 활성화
            sandboxTargets.SetActive(false); // 입문 훈련의 조준선과 총구 사이 장애물 제거
            target.gameObject.SetActive(false); // 사격 연습 전까지 시험 표적 숨김
            if (weapon.IsRian != (selected == FlightBroomPilot.Rian)) // 기존 조종사가 준비 화면의 선택과 다른 경우
            {
                weapon.TogglePilot(); // 실제 스킬과 자동 사격의 조종사 변경
            }
            GetComponent<FlightCombatHud>().SetSniper(weapon.IsRian); // 사용 중인 조종사의 스킬 이미지와 패시브
            start = flight.transform.position; // 실제 이동 성공 확인 기준
            skip.onClick.AddListener(ReturnToBriefing); // 훈련 중 마우스 복귀
            skills.SkillActivated += ObserveCast; // 버튼 입력만으로는 성공 처리 금지
            bindings.Changed += RefreshText; // 일시정지에서 바꾼 실제 키 바로 적용
            panel.SetActive(true); // 독립된 안내 UI 열기
            RefreshText(); // 첫 연습과 실제 키 표시
        }

        private void OnDestroy() // 안내 또는 시험 씬이 닫힐 때 정리
        {
            skills.SkillActivated -= ObserveCast; // 전투를 마칠 때 스킬 알림 구독 정리
            bindings.Changed -= RefreshText; // 다음 연습에서 참조 중복 제거
        }

        private void Update() // 조작에 따른 실제 다섯 연습의 진행
        {
            if (!active) // 일반 시험 또는 복귀 시작 상태
            {
                return; // 원래 연습 로직에 영향 없음
            }
            bool playable = Time.timeScale > 0f; // 기존의 일시정지와 보상 메뉴 검사
            panel.SetActive(playable); // 정지 UI와 안내 조작의 겹침 방지
            if (!playable) // 훈련을 멈춘 동안 진행 금지
            {
                return; // 시간과 키 입력을 보존
            }

            Keyboard keyboard = Keyboard.current; // 현재 입력 장치
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) // 키보드로 언제든 준비 복귀
            {
                ReturnToBriefing(); // 마우스 버튼과 같은 이동
                return; // 씬이 바뀌기 전 연습 중단
            }
            if (lesson != Lesson.Complete && keyboard != null && keyboard.tabKey.wasPressedThisFrame) // 비행 시점과 안내 버튼의 포인터 전환
            {
                SetPointer(!pointerOpen); // 안내 선택 동안 비행과 시점 입력 중지
            }
            if (pointerOpen) // 버튼을 조작하고 있는 동안 훈련 판정 보류
            {
                return; // 포인터가 열린 동안 예측하지 않은 진행 제외
            }
            switch (lesson) // 현재 배우는 실제 전투 동작
            {
                case Lesson.Move: // 좌우·상하·전후 실제 이동량
                    if ((flight.transform.position - start).sqrMagnitude >= 4f) // 처음 위치에서 2m 이동
                    {
                        Advance(); // 집중 비행 안내
                    }
                    break; // 다른 훈련은 이 프레임에 제외
                case Lesson.Focus: // 화면의 실제 집중 조작 유지
                    focusSeconds = flight.IsFocusHeld ? focusSeconds + Time.deltaTime : 0f; // 놓으면 다시 집중
                    if (focusSeconds >= 0.4f) // 집중 비행을 충분히 관찰
                    {
                        Advance(); // 다음에 시험 표적 배치
                    }
                    break; // 현재 집중 훈련만 갱신
                case Lesson.Shoot: // 실제 자동 공격 적중을 기다림
                    if (target.Health < target.MaxHealth) // 원래 사격의 피해가 들어온 경우
                    {
                        Advance(); // 스침을 배우는 단계로 진행
                    }
                    else if (Time.time >= nextAction) // 표적을 잃은 사용자를 위한 재배치
                    {
                        PlaceTarget(); // 현재 중앙 시야에서 다시 시험
                    }
                    break; // 다른 훈련 발사 차단
                case Lesson.Graze: // 적탄의 실제 스침 판정과 에너지
                    if (pilot.Grazes > earlierGrazes) // 되감기나 훈련 외 기록 제외
                    {
                        Advance(); // 마지막 고유·공용 스킬
                    }
                    else if (Time.time >= nextAction) // 비행 위치를 바꾼 경우 새 기회
                    {
                        Transform body = pilot.transform; // 실제 플레이어의 판정 위치
                        Vector3 from = body.position + body.forward * 15f + body.right * 0.7f; // 기체 오른쪽을 통과하는 기존 0.7m 스침 간격
                        pilot.GrantProtection(2f); // 접촉해도 입문을 계속할 수 있게 보호
                        threatPool.TryFire(from, -body.forward, 16f); // 실제 판정에 들어가는 연습 적탄
                        nextAction = Time.time + 2.2f; // 다음 발사 전 관찰 시간
                    }
                    break; // 그레이즈 완료 또는 다음 탄까지 대기
                case Lesson.Cast: // 새로 성공한 고유 또는 공용 스킬
                    if (cast) // 자원을 실제 소비하고 쿨타임이 생긴 경우
                    {
                        Advance(); // 완료 문구와 준비 복귀 표시
                    }
                    break; // 성공 전까지 안내 유지
            }
        }

        private void Advance() // 다음에 관찰할 실제 훈련 준비
        {
            lesson++; // 다섯 동작을 설정 순서대로 진행
            if (lesson == Lesson.Shoot) // 자동탄을 익힐 표적 활성화
            {
                PlaceTarget(); // 현재 카메라 중앙의 거리 사용
            }
            if (lesson == Lesson.Graze) // 외부 표적을 정리하고 실제 적탄만 표시
            {
                target.gameObject.SetActive(false); // 그레이즈 학습에 집중
                earlierGrazes = pilot.Grazes; // 현재부터 발생한 스침만 확인
                nextAction = Time.time + 0.65f; // 설명을 읽을 첫 간격
            }
            if (lesson == Lesson.Complete) // 완료 후 바로 마우스 버튼을 선택할 수 있음
            {
                SetPointer(true); // 마지막 안내에서 자동으로 포인터 반환
            }
            RefreshText(); // 해당 훈련의 현재 조작 키 안내
        }

        private void SetPointer(bool visible) // 훈련과 별도 안내 조작 사이의 전환
        {
            pointerOpen = visible; // 안내 UI 입력을 하는 상태
            flight.SetSortieEnded(visible); // 안내 조작 중 카메라가 커서를 다시 잠그지 않음
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked; // 마우스와 조준 시점 분리
            Cursor.visible = visible; // 안내 버튼을 가리키는 포인터
        }

        private void PlaceTarget() // 기존 연습 표적을 화면 중심의 사격 거리에 배치
        {
            Ray aim = view.GetAttackRay(); // 실제 자동탄과 같은 중앙 조준선
            target.transform.position = aim.GetPoint(26f); // 현재 시야의 전방 훈련 거리
            target.gameObject.SetActive(true); // 실제 맞힐 수 있는 웨이브 표적
            nextAction = Time.time + 4f; // 위치를 다시 잡기 전의 간격
        }

        private void ObserveCast(int slot) // 실제 시전에서만 마지막 연습 확인
        {
            if (lesson == Lesson.Cast && slot < 2) // 현재 안내의 두 마력 스킬만 확인
            {
                cast = true; // 다음 프레임의 실제 성공 처리
            }
        }

        private string Keycap(FlightAction action) => FlightBindings.Label(bindings.GetKey(action)); // 설정을 반영한 현재 키 표기

        private void RefreshText() // 역할에 맞는 실제 조작과 현재 진행률
        {
            step.text = lesson switch // 현재 단계를 숫자와 함께 확인
            {
                Lesson.Move => "1/5   이동", // 첫 비행
                Lesson.Focus => "2/5   집중", // 정밀 조작
                Lesson.Shoot => "3/5   자동 사격", // 표적 적중
                Lesson.Graze => "4/5   그레이즈", // 안전한 근접 회피
                Lesson.Cast => "5/5   스킬", // 마력 소모
                _ => "훈련 완료" // 실제 준비로 돌아갈 수 있음
            };
            instruction.text = lesson switch // 씬에 들어와 수행할 행동
            {
                Lesson.Move => $"[{Keycap(FlightAction.Forward)}] [{Keycap(FlightAction.Left)}] [{Keycap(FlightAction.Back)}] [{Keycap(FlightAction.Right)}] 이동   [{Keycap(FlightAction.Rise)}] 상승   [{Keycap(FlightAction.Descend)}] 하강\n기체를 움직여 보세요.", // 실제 비행 키
                Lesson.Focus => $"[{Keycap(FlightAction.Focus)}] 길게 눌러 집중 비행을 익히세요.", // 집중 비행 유지
                Lesson.Shoot => "마우스로 중앙 조준점을 표적에 맞추세요.\n기본 마법탄은 자동으로 발사됩니다.", // 실제 사격의 조건
                Lesson.Graze => "가까운 탄이 기체를 스치면 그레이즈가 쌓입니다.\n탄이 옆을 통과할 때까지 기다려 보세요.", // 판정과 안전한 관찰
                Lesson.Cast => $"[{Keycap(FlightAction.Special)}] 고유 스킬 또는 [{Keycap(FlightAction.Common)}] 공용 스킬을 사용하세요.", // 실제 마력 스킬 키
                _ => "출격 준비로 돌아가 장비를 확인하고 출발하세요." // 실제 전투 전 복귀
            };
            skipText.text = lesson == Lesson.Complete ? "출격 준비로  Enter" : "안내 건너뛰기  Enter"; // 어느 단계에서도 키보드와 마우스 복귀
        }

        private void ReturnToBriefing() // 이전 조종사와 준비 화면 복귀
        {
            if (!active || Time.timeScale == 0f) // 전환 중 중복 입력 또는 일시정지 확인
            {
                return; // 기존 메뉴를 유지
            }
            active = false; // 씬을 열기 전 추가 진행 차단
            FlightSortieChoice.ReturnFromGuide(selected); // 선택한 연습 조종사를 준비에 바인딩
            SceneManager.LoadSceneAsync(FlightSortieChoice.BriefingScene); // 영구 보상 없는 연습 종료
        }
    }
}
