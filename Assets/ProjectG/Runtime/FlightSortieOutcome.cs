using UnityEngine; // 출격 종료와 기존 화면 상태
using UnityEngine.InputSystem; // 결과로 이동하는 바로가기
using UnityEngine.SceneManagement; // 전투에서 결과 씬 분리
using UnityEngine.UI; // 실제 하이러키에 배치한 결과 버튼

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightSortieReport // 끝난 출격의 불변 기록
    {
        internal readonly bool Cleared; // 최종 보스 격파 또는 실패
        internal readonly FlightBroomPilot Pilot; // 결과 화면과 재출격의 조종사
        internal readonly float CombatSeconds; // 보상·정지를 제외한 전투 시간
        internal readonly float TotalSeconds; // 실제 출격 전체 소요
        internal readonly long Score; // 이번 출격의 확정 점수
        internal readonly int Defeats; // 실제 격파한 마물 수
        internal readonly int Grazes; // 인정한 근접 회피 수
        internal readonly int Level; // 이번 출격에서 도달한 레벨

        internal FlightSortieReport(bool cleared, FlightBroomPilot pilot, float combat, float total, long score, int defeats, int grazes, int level) // 결과로 전달할 동결 값
        {
            Cleared = cleared; // 승리와 실패 표시
            Pilot = pilot; // 같은 조종사로 다시 출격
            CombatSeconds = combat; // 마지막 전투 시계
            TotalSeconds = total; // 마지막 전체 시계
            Score = score; // 종료 때 합산한 점수
            Defeats = defeats; // 전투 중 발생한 격파만 집계
            Grazes = grazes; // 전투의 실제 스침
            Level = level; // 마지막 성장 상태
        }
    }

    public sealed class FlightSortieOutcome : MonoBehaviour // 실전 종료와 결과 화면 진입
    {
        [SerializeField] private FlightEnemyEncounter stage; // 실제 최종 격파와 종료 시계
        [SerializeField] private FlightWeapon weapon; // 이번 출격의 실제 조종사
        [SerializeField] private FlightPilotVitals pilot; // 피격 실패와 그레이즈 수
        [SerializeField] private FlightExperience experience; // 마지막 출격 레벨
        [SerializeField] private FlightScoreHud score; // 출격 점수와 실제 격파
        [SerializeField] private GameObject panel; // 전투 HUD의 종료 안내
        [SerializeField] private GameObject pausePanel; // 일시정지 조작과 결과 입력 충돌 방지
        [SerializeField] private Text heading; // 클리어 또는 실패 요약
        [SerializeField] private Button open; // 결과 씬으로 이동하는 실제 버튼
        private FlightSortieReport snapshot; // 종료 이후에는 바뀌지 않는 실제 값
        private bool loading; // 같은 결과의 중복 진입 차단
        private CursorLockMode priorLock; // 다시하기 전의 원래 입력 잠금
        private bool priorVisible; // 원래 전투 포인터 표시
        private FirstFlightController flight; // 결과에서 비행과 커서 재잠금을 막는 조작

        private void Start() // 씬에서 편집한 결과 진입 연결
        {
            flight = pilot.GetComponent<FirstFlightController>(); // 이 출격의 실제 비행 입력
            open.onClick.AddListener(OpenResult); // 결과 씬의 실제 진입 버튼
        }

        private void LateUpdate() // 기존 전투 점수와 최종 시계 이후 종료 확인
        {
            if (!stage.IsStageComplete && !pilot.IsDefeated) // 이번 출격이 아직 진행 중
            {
                if (snapshot != null) // 기존 일시정지의 다시하기로 상태가 복원된 경우
                {
                    snapshot = null; // 이전 결과를 새 런에 전달하지 않음
                    panel.SetActive(false); // 전투 HUD 복귀
                    flight.SetSortieEnded(false); // 다시 출격한 실제 비행체 조작 복원
                    Cursor.lockState = priorLock; // 현재 조작 상태로 되돌리기
                    Cursor.visible = priorVisible; // 원래 플레이 포인터
                }
                return; // 종료가 아닌 전투에서는 다음 프레임
            }
            if (snapshot == null) // 종료된 바로 그 시점에 한 번 기록
            {
                Record(); // 확정한 시간·체력과 점수 읽기
            }
            Keyboard keyboard = Keyboard.current; // 현재 결과 바로가기
            if (!pausePanel.activeSelf && keyboard != null && keyboard.enterKey.wasPressedThisFrame) // 일시정지에서 다른 입력 소비 방지
            {
                OpenResult(); // 마우스 버튼과 같은 실제 씬 이동
            }
        }

        private void Record() // 끝난 출격의 결과를 캡처
        {
            bool cleared = stage.IsStageComplete && !pilot.IsDefeated; // 실제 전투 성공 여부
            FlightBroomPilot current = weapon.IsRian ? FlightBroomPilot.Rian : FlightBroomPilot.Aria; // 스킬·장비의 실제 조종사
            snapshot = new FlightSortieReport(cleared, current, stage.ElapsedSeconds, stage.TotalSeconds, score.Total, score.DefeatCount, pilot.Grazes, experience.Level); // 결과에 복제할 확정값
            heading.text = cleared ? "출격 완료" : "출격 실패"; // 전투에 표시할 실제 결과
            flight.SetSortieEnded(true); // 결과 확인에 쓸 클릭이 비행으로 넘어가지 않도록 분리
            priorLock = Cursor.lockState; // 현재 전투의 커서 제어 기록
            priorVisible = Cursor.visible; // 현재 표시 상태 저장
            Cursor.lockState = CursorLockMode.None; // 종료 후 결과 버튼을 누를 수 있게 설정
            Cursor.visible = true; // 결과 버튼의 실제 마우스 포인터
            panel.SetActive(true); // 하이러키에 있는 결과 열기
        }

        private void OpenResult() // 종료한 출격을 결과 화면에 전달
        {
            if (snapshot == null || loading) // 유효한 출격 끝과 첫 확인만 허용
            {
                return; // 같은 씬 전환 반복 방지
            }
            loading = true; // 새로 결과를 열고 있는 상태
            FlightSortieResult.StageReport(snapshot); // 실제 종료한 값만 결과로 전달
            Time.timeScale = 0f; // 새 씬이 열리기 전에 뒤늦은 전투 상태 변화 방지
            SceneManager.LoadSceneAsync(FlightSortieResult.SceneName); // 배포 등록된 별도 결과
        }
    }
}
