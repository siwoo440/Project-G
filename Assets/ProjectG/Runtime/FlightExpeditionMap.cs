using UnityEngine; // 실제 씬의 배치 오브젝트
using UnityEngine.InputSystem; // 마우스와 같은 행동의 지름길
using UnityEngine.SceneManagement; // 지도와 실전 항로 전환
using UnityEngine.UI; // 편집하는 다섯 노드와 동작 버튼

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightExpeditionMap : MonoBehaviour // 출격 준비에 연결한 독립 탐험 지도
    {
        [SerializeField] private GameObject panel; // 하이러키에 배치한 안내·선택지
        [SerializeField] private Text summary; // 현재 캐릭터·현재 경로·임시 보급
        [SerializeField] private Text detail; // 지점에서 할 수 있는 행동
        [SerializeField] private Text notice; // 저장과 전투 결과 안내
        [SerializeField] private Text[] nodes; // 실제 순서대로 편집한 다섯 노드
        [SerializeField] private Button primary; // 지점의 첫 선택
        [SerializeField] private Text primaryLabel; // 그 지점에서 할 행동
        [SerializeField] private Button secondary; // 경로 분기 또는 구매
        [SerializeField] private Text secondaryLabel; // 두 번째 행동의 이름
        [SerializeField] private Button close; // 저장하고 출격 준비로
        [SerializeField] private Button abandon; // 이번 탐험 포기
        [SerializeField] private Text abandonLabel; // 포기 확인을 안내할 단추
        private FlightBroomPilot selected; // 새로 시작할 경우 실제 준비 조종사
        private bool confirming; // 임시 성장 초기화 전 확인
        private bool loading; // 두 전투 전환 입력의 중복 방지
        private static readonly Color CurrentNode = new Color(1f, .78f, .40f); // 선택할 수 있는 지점
        private static readonly Color VisitedNode = new Color(.43f, .84f, .85f); // 통과한 지점
        private static readonly Color WaitingNode = new Color(.69f, .76f, .86f); // 다음 지점

        internal void Initialize() // 출격 준비 시작과 같이 실제 버튼에 연결
        {
            primary.onClick.AddListener(Act); // 첫 선택과 같은 단축키의 동작
            secondary.onClick.AddListener(Other); // 두 번째 선택
            close.onClick.AddListener(Close); // 실제 체크포인트를 유지하고 닫기
            abandon.onClick.AddListener(Abandon); // 별도 포기 확인
        }

        internal void Show(FlightBroomPilot pilot) // 새 탐험 또는 기존 체크포인트 선택
        {
            selected = pilot; // 아직 시작하지 않은 탐험의 실제 조종사
            confirming = false; // 이전 포기 클릭 초기화
            FlightExpedition.Restore(); // 저장된 지점에서 같은 조종사 복원
            notice.text = FlightExpedition.ConsumeOutcome() ?? "지점이 바뀔 때 탐험을 저장합니다."; // 필요한 현재 절차 또는 실제 전투 결과
            panel.SetActive(true); // 장비 화면 위로 별도 패널 표시
            Render(); // 실제 진행 경로와 다음 버튼
        }

        internal bool HandleKey(Keyboard key) // 출격 선택보다 먼저 처리하는 지도 입력
        {
            if (!panel.activeSelf) // 현재 지도 표시 여부
            {
                return false; // 기본 출격 동작 처리
            }

            if (key.escapeKey.wasPressedThisFrame || key.f6Key.wasPressedThisFrame) // 지도를 열 때와 같은 바로가기
            {
                Close(); // 저장한 지점을 유지하며 출격 준비로
            }
            else if (key.digit1Key.wasPressedThisFrame || key.enterKey.wasPressedThisFrame) // 현재 표시된 첫 행동
            {
                Act(); // 마우스가 선택하는 같은 버튼
            }
            else if (key.digit2Key.wasPressedThisFrame && secondary.gameObject.activeSelf && secondary.interactable) // 보급 또는 상점 구매가 가능한 경우
            {
                Other(); // 해당 경로의 두 번째 선택
            }

            return true; // Enter가 일반 출격을 동시에 시작하지 않도록 처리
        }

        private void Render() // 저장된 지점에서 실제 단계별로 버튼 구성
        {
            FlightExpedition.Checkpoint run = FlightExpedition.Current; // 화면에 현재 표시할 상태
            FlightExpeditionNode node = run?.node ?? FlightExpeditionNode.Dock; // 새 탐험의 기본 지도
            for (int i = 0; i < nodes.Length; i++) // 다섯 노드의 진행 하이라이트
            {
                nodes[i].color = i == (int)node ? CurrentNode : i < (int)node ? VisitedNode : WaitingNode; // 현재·통과·대기
            }

            nodes[2].text = run?.route == 1 ? "교전" : run?.route == 2 ? "우회 보급" : "교전 / 보급"; // 선택의 실제 결과
            string name = FlightPilotRoster.Name(run?.pilot ?? selected); // 복원하면 처음 선택한 조종사
            summary.text = $"{name}   ·   보급표 {run?.tokens ?? 0}장   ·   자동탄 피해 +{(run?.power ?? 0) * 20}%"; // 임시 상점 통화와 전투 강화
            abandon.gameObject.SetActive(run != null); // 시작한 탐험의 명시적 포기
            abandonLabel.text = confirming ? "탐험 포기 확정" : "탐험 포기"; // 실행 전 확인
            secondary.gameObject.SetActive(run != null && node is FlightExpeditionNode.Fork or FlightExpeditionNode.Merchant); // 지점에 따라 선택지 1개 또는 2개
            secondary.interactable = node == FlightExpeditionNode.Fork || run != null && run.tokens > 0 && !run.bought; // 유효한 경로와 단일 구매
            primary.interactable = !loading; // 씬 전환 도중 입력 차단

            if (run == null) // 저장과 실제 전투에 처음 참여하기 전
            {
                detail.text = "항구에서 출발해 경로를 고릅니다. 보급과 강화는 이 탐험에서만 사용합니다."; // 작품의 임시 실험 조건
                primaryLabel.text = "1   새 탐험 시작"; // 아직 저장을 만들지 않은 경우
                return; // 첫 지점 전용 표시
            }

            (detail.text, primaryLabel.text, secondaryLabel.text) = node switch // 각 지점에서 읽을 설명과 선택지
            {
                FlightExpeditionNode.Dock => ("현재 조종사의 빗자루와 마법 편성으로 출항합니다.", "1   갈림길로 이동", ""), // 편성이 실제 탐험에 적용
                FlightExpeditionNode.Fork => ("교전은 보급표 2장과 강화 1단계, 우회는 보급표 1장을 얻습니다.", "1   마물 교전", "2   우회 보급"), // 분기 차이 설명
                FlightExpeditionNode.Battle => ("교전을 끝까지 진행하면 임시 보급과 자동탄 강화가 적용됩니다.", "1   교전 시작", ""), // 중단하면 교전부터 복원
                FlightExpeditionNode.Merchant => ("보급표 1장으로 자동탄 피해 +20%를 한 번 올릴 수 있습니다.", "1   상점 나가기", run.bought ? "강화 구매 완료" : "2   자동탄 강화 1장"), // 구매와 다음 지점
                _ => ("최종 보스의 3페이즈를 통과하면 탐험이 끝납니다.", "1   최종 전투", "") // 마지막 전투 선택
            };
        }

        private void Act() // 현재 지점의 기본 행동
        {
            if (loading) // 다른 씬 진입 요청이 이미 성공
            {
                return; // 중복 전투 방지
            }

            confirming = false; // 지도에서 다른 행동을 한 경우 포기 확인 취소
            FlightExpedition.Checkpoint run = FlightExpedition.Current; // 현재 선택을 적용할 상태
            if (run == null) // 편성 화면의 조종사로 첫 체크포인트
            {
                FlightExpedition.StartFrom(selected); // 현재 선택한 조종사의 탐험
            }
            else switch (run.node) // 지도 단계마다 실제 동작 분리
            {
                case FlightExpeditionNode.Dock: // 항구 출발
                    FlightExpedition.Depart(); // 저장하고 갈림길 표시
                    break; // 지도 계속
                case FlightExpeditionNode.Fork: // 교전이라는 첫 번째 경로
                    if (FlightExpedition.ChooseRoute(true)) // 경로 선택을 먼저 저장
                    {
                        Launch(); // 실제 교전으로 출발
                    }
                    break; // 같은 입력으로 다음 단계 중복 차단
                case FlightExpeditionNode.Battle: // 중단한 교전
                case FlightExpeditionNode.Final: // 마지막 보스 구간
                    Launch(); // 현재 지도 지점을 실제 전투로
                    break; // 자동탄과 입체 탄막 전투로 전환
                case FlightExpeditionNode.Merchant: // 이번 탐험 상점
                    FlightExpedition.LeaveMarket(); // 저장하고 최종 전투 선택 표시
                    break; // 실제 전투 앞의 체크포인트
            }

            FlightAudio.Play(FlightSfx.UiConfirm); // 다른 출격 메뉴와 같은 단추 반응
            Render(); // 저장된 현재 화면 상태
        }

        private void Other() // 갈림길의 우회 또는 상점의 구매
        {
            confirming = false; // 포기 창에서 다른 행동의 입력
            if (FlightExpedition.Current?.node == FlightExpeditionNode.Fork) // 우회 경로의 임시 보급
            {
                FlightExpedition.ChooseRoute(false); // 보급표를 받고 임시 상점
            }
            else
            {
                FlightExpedition.Buy(); // 재화를 소모하고 탐험용 자동탄 강화
            }

            FlightAudio.Play(FlightSfx.UiConfirm); // 같은 UI 확정 효과
            Render(); // 남은 보급과 강화 수치
        }

        private void Launch() // 저장한 경로로 실제 공중 전투 진입
        {
            loading = true; // 클릭과 키의 겹치는 같은 프레임 방지
            FlightSortieChoice.LaunchExpedition(FlightExpedition.Current.pilot); // 보존할 캐릭터와 분리한 전투 모드
            SceneManager.LoadSceneAsync(FlightSortieChoice.StageScene); // 본편 마물과 최종 보스의 기존 씬 자산
        }

        private void Close() // 지도의 상태를 보존하고 출격 준비로 복귀
        {
            if (!loading) // 이미 전투로 건너가는 경우 닫기 금지
            {
                FlightAudio.Play(FlightSfx.UiBack); // 실제 UI 복귀 소리
                panel.SetActive(false); // 출격 준비의 이전 선택 유지
            }
        }

        private void Abandon() // 먼저 확인하고 이번 탐험만 포기
        {
            if (loading || FlightExpedition.Current == null) // 종료할 탐험이 있을 때만
            {
                return; // 중복 포기 차단
            }

            if (confirming) // 같은 버튼을 다시 눌러 실제 동의
            {
                FlightExpedition.Cancel("탐험 포기 · 임시 재화와 강화 초기화"); // 다른 영구 저장에 손대지 않음
                notice.text = FlightExpedition.ConsumeOutcome(); // 정확한 포기 결과
                confirming = false; // 다음 시작에서 초기 상태
            }
            else
            {
                confirming = true; // 다음 한 번만 확정
                notice.text = "같은 버튼을 다시 누르면 이번 탐험을 포기합니다."; // 사라질 진행 안내
            }

            Render(); // 실제 남은 상태와 다음 행동
        }
    }
}
