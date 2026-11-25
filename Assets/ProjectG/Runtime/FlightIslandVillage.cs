using UnityEngine; // 씬에 편집한 마을 지형과 이동
using UnityEngine.InputSystem; // 공용 이동 키와 시설 상호작용
using UnityEngine.SceneManagement; // 타이틀·마을·준비 전환
using UnityEngine.UI; // 하이러키의 도보 UI

namespace ProjectG // 프로젝트 하늘섬 마을
{
    public sealed class FlightIslandVillage : MonoBehaviour // 착륙장, 보행로와 출격소의 동선
    {
        public const string SceneName = "FlightIslandVillage"; // 배포 마을 씬의 이름
        [SerializeField] private CharacterController walker; // 씬에 배치한 도보 캐릭터
        [SerializeField] private Transform avatar; // 움직이는 외형과 방향
        [SerializeField] private Transform landing; // 새 게임과 이어서하기의 착륙장
        [SerializeField] private Transform sortieReturn; // 준비에서 돌아갈 출격소 앞
        [SerializeField] private Transform station; // 출격 준비가 가능한 월드의 위치
        [SerializeField] private Transform market; // 상점 섬의 길 안내 기준
        [SerializeField] private Transform workshop; // 공방 섬의 길 안내 기준
        [SerializeField] private Camera overhead; // 마을을 따라 보는 고정 각도 카메라
        [SerializeField] private FlightBindings bindings; // 전투와 같이 저장한 기본 이동 키
        [SerializeField] private Text locality; // 현재 구역과 플레이어 이동 안내
        [SerializeField] private Button boarding; // 출격소 주변에서만 나타나는 실제 버튼
        [SerializeField] private Text actionLabel; // 현재 시설에 맞는 상호작용 이름
        [SerializeField] private FlightVillageFacilities facilities; // 장비 상점과 강화 공방의 실행 패널
        [SerializeField] private FlightVillageDialogue dialogue; // 하이러키의 NPC 대화 화면
        [SerializeField] private FlightVillageNpc[] residents; // 실제 마을에 편집된 주민 목록
        [SerializeField] private Button talkButton; // 가까울 때 나타나는 F 대화 버튼
        [SerializeField] private Text talkLabel; // 지금 가까운 NPC의 정확한 이름
        private FlightVillageNpc nearby; // 실제 대화 범위 안에서 가장 가까운 사람
        [SerializeField] private LayerMask walkable = 1 << 8; // 섬과 다리 보행 가능 레이어
        [SerializeField, Min(0.1f)] private float speed = 7.25f; // 마을 도보 이동 속도
        [SerializeField, Min(0.1f)] private float interactionRadius = 3.2f; // 출격소 조작 거리
        private static bool fromBriefing; // 다음 마을 한 번에서만 복귀 위치 사용
        private Vector3 viewOffset; // 하이러키에서 편집한 카메라 위치
        private Vector3 avatarRest; // 걷는 외형의 기준 높이
        private bool movingScene; // 중복 씬 전환 방지
        private bool canBoard; // 현재 위치에서 실제 출격소 접근
        private bool canShop; // 현재 위치에서 실제 장비 상점 접근
        private bool canForge; // 현재 위치에서 실제 공방 접근
        private float gait; // 발걸음 높이의 반복 위상

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 연속 에디터 플레이의 복귀 지점 초기화
        private static void Reset() // 타이틀부터 시작하는 새 게임
        {
            fromBriefing = false; // 최초에는 착륙장 배치
        }

        internal static void ReturnFromBriefing() // 준비 선택을 취소하고 시설로 돌아감
        {
            fromBriefing = true; // 다음 마을에 출격소 복귀 표시
            SceneManager.LoadSceneAsync(SceneName); // 현재 선택을 보존한 마을
        }

        private void Start() // 실제 출발 위치와 카메라·UI 초기화
        {
            Time.timeScale = 1f; // 이전 전투에서 마을로 올 때 실제 시간 정상화
            AudioListener.pause = false; // 마을의 소리 재생 상태
            Cursor.lockState = CursorLockMode.None; // 마을의 출격 버튼을 클릭 가능
            Cursor.visible = true; // 시설 확인 포인터
            viewOffset = overhead.transform.position - landing.position; // 씬에서 편집한 카메라 상대 거리
            avatarRest = avatar.localPosition; // 작성한 기본 조종사 높이
            walker.enabled = false; // 첫 위치 지정에서 기존 충돌과 분리
            walker.transform.position = fromBriefing ? sortieReturn.position : landing.position; // 입장 동선에 맞는 실제 위치
            walker.enabled = true; // 시설 충돌과 도보 이동 활성
            fromBriefing = false; // 앞으로 오는 초기 착륙장은 같은 위치를 사용하지 않음
            overhead.transform.position = walker.transform.position + viewOffset; // 시작 카메라의 불필요한 이동 제외
            boarding.onClick.AddListener(Board); // 마우스와 E의 실제 출격소 조작
            talkButton.onClick.AddListener(Talk); // F와 같은 마우스 상호작용
            dialogue.ServiceChosen += OpenService; // 대화 선택에서 기존 시설로 연결
            RefreshLocation(); // 처음부터 구역과 조작 안내 표시
        }

        private void Update() // 도보 이동, 출격소와 타이틀 복귀
        {
            Keyboard keyboard = Keyboard.current; // 실제 키 상태
            if (movingScene || keyboard == null) // 이미 전환 중 또는 키 입력 미연결
            {
                return; // 기존 화면의 추가 입력 방지
            }
            if (dialogue.IsOpen) // 대사와 선택지가 열렸을 때 마을 걷기와 E 중지
            {
                dialogue.Handle(keyboard); // F·선택·Esc를 대화에서 먼저 받음
                return; // 원래 마을 이동과 동시에 처리하지 않음
            }
            if (facilities.IsOpen) // 매장을 조작하는 중 마을 이동 입력 중지
            {
                if (keyboard.escapeKey.wasPressedThisFrame) // 매장에서만 닫기로 작동
                {
                    facilities.Close(); // 타이틀로 나가지 않고 바로 시설 앞으로 복귀
                }
                return; // 뒤에 있는 이동과 입장 처리 차단
            }
            if (keyboard.escapeKey.wasPressedThisFrame) // 마을에서 메인으로 돌아가기
            {
                movingScene = true; // 사용자 조작 한 번으로 제한
                SceneManager.LoadSceneAsync(FlightTitleMenu.SceneName); // 실제 타이틀 화면 복귀
                return; // 이동 및 출격 키 점검 제외
            }
            Vector3 input = new Vector3(Stride(keyboard, FlightAction.Right, FlightAction.Left), 0f, Stride(keyboard, FlightAction.Forward, FlightAction.Back)); // 마을의 카메라에 맞는 네 방향
            Walk(Vector3.ClampMagnitude(input, 1f) * (Time.deltaTime * speed)); // 대각선 이동의 최고 속도 제한
            RefreshLocation(); // 도착한 구역의 이름과 출격 버튼 정리
            if (nearby != null && keyboard.fKey.wasPressedThisFrame) // 범위 안에 있을 때만 실제 대화
            {
                Talk(); // 마우스 대화 버튼과 동일한 경로
                return; // 같은 키 상태의 시설 즉시 열기 제외
            }
            if (keyboard.eKey.wasPressedThisFrame) // 가까운 출격소와 실제 상호작용
            {
                Board(); // 현재 시설의 입장 버튼과 같은 동작
            }
        }

        private float Stride(Keyboard keyboard, FlightAction positive, FlightAction negative) // 실제 저장한 키의 축 입력
        {
            return (bindings.Held(keyboard, positive) ? 1f : 0f) - (bindings.Held(keyboard, negative) ? 1f : 0f); // 반대 방향도 같은 우선순위
        }

        private void Walk(Vector3 requested) // 바닥을 확인하고 벽과 낭떠러지에 제한
        {
            Vector3 prior = walker.transform.position; // 움직이기 전의 실제 위치
            if (requested.sqrMagnitude > 0.000001f) // 움직임을 요청한 프레임
            {
                if (TryStep(prior, requested, out Vector3 step) || TryStep(prior, new Vector3(requested.x, 0f, 0f), out step) || TryStep(prior, new Vector3(0f, 0f, requested.z), out step)) // 낭떠러지 가장자리를 따라 걸을 수 있는 축
                {
                    walker.Move(step); // 계단과 시설 벽은 캐릭터 컨트롤러에서 충돌
                }
            }
            Vector3 travel = walker.transform.position - prior; // 실제 충돌을 적용한 최종 이동량
            travel.y = 0f; // 도보 방향에서 발 사이의 높이차 제외
            if (travel.sqrMagnitude > 0.000001f) // 걸어간 방향으로만 캐릭터 회전
            {
                avatar.rotation = Quaternion.LookRotation(travel.normalized, Vector3.up); // 씬의 도보 외형 방향
                gait += travel.magnitude * 9f; // 실제 달린 거리에서 발걸음 주기
                avatar.localPosition = avatarRest + Vector3.up * (0.055f * Mathf.Abs(Mathf.Sin(gait))); // 입문 마을용 가벼운 보행 동작
            }
            else // 멈췄을 때 발걸음 정지
            {
                avatar.localPosition = avatarRest; // 지면과 캐릭터의 일정한 위치
            }
        }

        private bool TryStep(Vector3 origin, Vector3 depth, out Vector3 delta) // 다음 발 위치 아래의 실제 보행 바닥 확인
        {
            delta = Vector3.zero; // 유효 위치를 찾지 못한 기본값
            if (depth.sqrMagnitude < 0.000001f) // 입력이 없는 축 제외
            {
                return false; // 벽을 따라 이동할 다음 축 확인
            }
            Vector3 sample = origin + depth + Vector3.up + depth.normalized * 0.18f; // 낭떠러지 방향에 발보다 작은 안전 폭
            if (!Physics.Raycast(sample, Vector3.down, out RaycastHit floor, 2f, walkable, QueryTriggerInteraction.Ignore)) // 실제 섬·다리 윗면
            {
                return false; // 보행 바닥 없는 방향 차단
            }
            delta = depth + Vector3.up * (floor.point.y + 0.04f - origin.y); // 보행판의 높이와 캐릭터 접지 보정
            return true; // 바닥 위의 다음 걸음
        }

        private void LateUpdate() // 이동이 끝난 뒤 카메라가 따라옴
        {
            Vector3 position = walker.transform.position + viewOffset; // 동일한 기준 각도로 마을의 길 보이기
            overhead.transform.position = Vector3.Lerp(overhead.transform.position, position, 1f - Mathf.Exp(-7f * Time.deltaTime)); // 전환 시에도 부드러운 도보 추적
        }

        private void RefreshLocation() // 구역을 이동한 사용자에게 현재 목적지 표시
        {
            Vector3 position = walker.transform.position; // 실제 보행자의 위치
            canBoard = Vector3.Distance(position, station.position) <= interactionRadius; // 출격소 조작의 실제 유효 거리
            float marketDistance = Vector3.Distance(position, market.position); // 장비 상점의 실제 접근
            float forgeDistance = Vector3.Distance(position, workshop.position); // 공방의 실제 접근
            canShop = marketDistance <= interactionRadius; // 가까운 장비 상점만 이용
            canForge = forgeDistance <= interactionRadius; // 가까운 공방만 이용
            boarding.gameObject.SetActive(canBoard || canShop || canForge); // 세 건물 중 가까운 시설의 조작
            actionLabel.text = canBoard ? "E  출격 준비" : canShop ? "E  장비 상점" : "E  강화 공방"; // 같은 키로 서로 다른 시설 입장
            locality.text = canBoard ? "출격소" : marketDistance < 9f ? "장비 상점" // 지금 진입한 실제 마을 구역
                : forgeDistance < 9f ? "강화 공방" : position.z < -5f ? "착륙장" : "중앙 광장"; // 시설과 광장의 위치
            RefreshTalk(position); // 가까운 주민으로 마을 상황 표시 갱신
        }

        private void RefreshTalk(Vector3 position) // 각 주민의 대화 범위를 계산
        {
            nearby = null; // 이전 구역의 주민 제외
            float distance = float.PositiveInfinity; // 실제 범위에서만 가장 가까운 대상
            foreach (FlightVillageNpc resident in residents) // 씬에 미리 둔 마을 주민
            {
                if (resident == null || !resident.isActiveAndEnabled) // 숨겨 둔 공개 전 NPC
                {
                    continue; // 유효하지 않은 대화는 제외
                }
                float squared = (resident.transform.position - position).sqrMagnitude; // 과도한 제곱근 없이 거리 확인
                if (squared <= resident.Radius * resident.Radius && squared < distance) // 범위 안의 가장 가까운 사람
                {
                    nearby = resident; // 대화할 실제 대상 갱신
                    distance = squared; // 다음 후보와 비교할 거리
                }
            }
            talkButton.gameObject.SetActive(nearby != null); // NPC 근처에서만 보이는 안내
            if (nearby != null) // 현재 대화 가능한 역할
            {
                string caption = $"F   {nearby.Speaker}  대화"; // 실제 상호작용 대상과 단축키
                if (talkLabel.text != caption) // 같은 프레임의 중복 문자 갱신 방지
                {
                    talkLabel.text = caption; // 다른 NPC의 안내로 교체
                }
            }
        }

        private void Talk() // 마우스와 F에서 현재 거리의 주민 택하기
        {
            if (!movingScene && !facilities.IsOpen && nearby != null) // 현재 마을에서만 실제 대화
            {
                dialogue.Begin(nearby); // 가까운 화자의 실제 대사와 전용 카메라
                talkButton.gameObject.SetActive(false); // 대화 컷에 구역의 중복 힌트 제거
                boarding.gameObject.SetActive(false); // 대화에서 구역 버튼을 같은 화면에 표시하지 않음
            }
        }

        private void OpenService(FlightVillageService service) // 이야기 선택과 기존 시설을 연결
        {
            switch (service) // 승인된 대화에서의 실제 편집 기능
            {
                case FlightVillageService.Market: // 상점 주인의 장비 재고
                    facilities.Open(true); // 기존 장비 상점 로직 사용
                    break;
                case FlightVillageService.Forge: // 공방 담당자의 부품 강화
                    facilities.Open(false); // 기존 강화 공방 로직 사용
                    break;
                case FlightVillageService.Sortie: // 출격 안내원의 실제 준비 화면
                    EnterBriefing(); // 기존 출격소와 같은 복귀 경로
                    break;
            }
        }

        private void Board() // 출격소에서 준비 화면 열기
        {
            if (movingScene || facilities.IsOpen || dialogue.IsOpen) // 대화와 시설을 같은 프레임에 열지 않음
            {
                return; // 화면을 여는 중 중복 방지
            }
            if (canShop || canForge) // 해당 건물의 실제 이동 위치에서만
            {
                facilities.Open(canShop); // 장비 상점과 강화 공방 중 가까운 시설
                return; // 현재 위치와 마을 상태 유지
            }
            if (!canBoard) // 출격소의 실제 상호작용 범위
            {
                return; // 먼 위치의 E 입력 제외
            }
            EnterBriefing(); // 기존 출격소와 같은 종료 경로
        }

        private void EnterBriefing() // 대화와 E가 공유하는 출격 준비
        {
            movingScene = true; // 같은 시설의 중복 입력 차단
            FlightSortieChoice.EnterViaVillage(); // 준비에서 해당 시설로 돌아가는 경로
            SceneManager.LoadSceneAsync(FlightSortieChoice.BriefingScene); // 실제 캐릭터와 장비 준비로 진입
        }
    }
}
