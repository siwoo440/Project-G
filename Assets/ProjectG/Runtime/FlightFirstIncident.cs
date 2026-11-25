using UnityEngine; // 첫 작전 패널의 표시와 실제 보스 조건
using UnityEngine.InputSystem; // 기존 키와 독립적인 작전 기록 단축키
using UnityEngine.UI; // 씬에 배치한 작전 기록과 통신 패널

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 한 씬에서 기록 또는 통신 하나
    public sealed class FlightFirstIncident : MonoBehaviour // 작전 기록 보기와 두 보스의 짧은 통신
    {
        [SerializeField] private FlightIncidentScript script; // 승인을 받은 원본 문장
        [SerializeField] private GameObject panel; // 하이러키에서 편집하는 그림과 텍스트
        [SerializeField] private Button open; // 준비·정산의 작전 기록 열기
        [SerializeField] private Button close; // 준비·정산의 기록 닫기
        [SerializeField] private Text heading; // 통신의 현재 분기 이름
        [SerializeField] private Text content; // 작전 기록과 화면 안내 본문
        [SerializeField] private Text result; // 실제 출격 결과에 대응한 추가 한 줄
        [SerializeField] private FlightEnemyEncounter encounter; // 통신이 연결된 정식 실전 항로
        [SerializeField] private FlightWeapon weapon; // 최초 만남에 참여하는 실제 조종사
        private static bool openingViewed; // 같은 실행의 재출격에서 도입 반복 방지
        private bool midSeen; // 현재 출격의 중간 보스 메시지 상태
        private bool finalSeen; // 현재 출격의 마지막 보스 메시지 상태
        private float previousCombatTime; // 같은 씬 안의 재시작을 알 수 있는 시간
        private float expires; // 다음 프레임에서 표시를 유지할 전투 시간

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 새 게임 실행의 도입 표시 상태
        private static void ResetOpening() // 이전 플레이 모드의 임시 기록 제거
        {
            openingViewed = false; // 새 프로세스에서 첫 도입 한 번 표시
        }

        private void Start() // 기록 열기와 실제 첫 출격 안내
        {
            if (encounter == null) // 준비와 정산의 열람 패널
            {
                open.onClick.AddListener(OpenArchive); // 기존 버튼과 같은 열기
                close.onClick.AddListener(CloseArchive); // 다시 출발이나 결과 버튼으로 돌아가기
                content.text = script.Archive; // 실제 게임이 공유하는 세계관 문장
                return; // 열람에 전투 시계 없음
            }
            if (encounter.Mode != FlightSortieMode.Story) // 보스 도전과 탐험에는 본편 서사 진행 없음
            {
                enabled = false; // 기록과 실제 서사는 이후 본편에서 실행
                return; // 보스 러시에는 본편의 조우 문구 제외
            }
            if (!openingViewed && (weapon.ActivePilot is FlightBroomPilot.Aria or FlightBroomPilot.Rian)) // 기획서의 첫 만남에 해당하는 두 조종사
            {
                openingViewed = true; // 재출격해도 같은 도입을 반복하지 않음
                Broadcast("외곽 항로 · 첫 사건", script.Opening); // 처음 출격할 때 실제 화면에 표시
            }
        }

        internal bool HandleArchiveKey(Keyboard keyboard, bool canOpen) // 준비·결과 조작의 단축키를 우선 처리
        {
            if (panel.activeSelf) // 열람 중 출격과 화면 이동의 중복 입력 차단
            {
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.f3Key.wasPressedThisFrame) // 같은 창만 닫는 입력
                {
                    CloseArchive(); // 원래 선택 화면과 버튼으로 복귀
                }
                return true; // 이전 화면의 Enter·Escape에 키를 중복 전달하지 않음
            }
            if (canOpen && keyboard.f3Key.wasPressedThisFrame) // 마우스 없이 작전 기록 열기
            {
                OpenArchive(); // 화면 버튼과 같은 읽기 진입점
                return true; // 같은 키를 다른 배포 조작에 전달하지 않음
            }
            return false; // 기존 출격의 일반 입력 진행
        }

        internal void ShowResult(bool cleared) // 기록 하단에 실제 정산의 결과 한 줄
        {
            result.text = cleared ? script.ClearSummary : script.FailureSummary; // 이번 출격에서 확인된 상태에 맞는 요약
        }

        private void OpenArchive() // 같은 출격에서 언제든 현재 승인 사실 재열람
        {
            FlightAudio.Play(FlightSfx.UiConfirm); // 작전 기록의 열기 신호
            panel.SetActive(true); // 편집 가능한 기록 UI 표시
            close.Select(); // 모달이 열린 동안 원래 버튼에 포커스가 남지 않도록 이동
        }

        private void CloseArchive() // 원래 화면에 있던 선택 상태 복귀
        {
            FlightAudio.Play(FlightSfx.UiBack); // 작전 기록의 닫기 신호
            panel.SetActive(false); // 기록만 닫고 출격·정산 상태 유지
            open.Select(); // 다시 읽거나 원래 캐릭터를 선택할 수 있는 위치
        }

        private void Update() // 정식 전투에서만 통신 발생과 건너뛰기
        {
            if (encounter == null) // 다른 화면은 별도 키 입력 처리
            {
                return; // 열람 창에 전투 시계 없음
            }
            float elapsed = encounter.ElapsedSeconds; // 실제 새 출격과 동일 런의 경계를 구분
            if (elapsed < previousCombatTime) // 일시정지 화면의 다시하기
            {
                midSeen = false; // 새 런에서 보스 등장하면 읽을 수 있음
                finalSeen = false; // 새 런의 최종 통신도 복원
                expires = 0f; // 이전 출격의 통신 숨김
            }
            previousCombatTime = elapsed; // 다음 프레임과 같은 실행 시간 비교
            if (!midSeen && encounter.IsMidBossActive) // 실제 먼저 등장한 중간 보스
            {
                midSeen = true; // 같은 보스를 마주친 동안 한 번만 표시
                Broadcast("작전 통신 · 중간 보스", script.MidBoss); // 확인된 대면 이후 사용
            }
            if (!finalSeen && encounter.IsFinalBossActive) // 실제 최종 구간 돌입
            {
                finalSeen = true; // 페이즈 전환에는 같은 안내 반복 제외
                Broadcast("작전 통신 · 최종 보스", script.FinalBoss); // 확정되지 않은 원인이나 정체를 포함하지 않는 안내
            }
            if (Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) // 훈련 키와 다른 통신 건너뛰기
            {
                expires = 0f; // 현재 창만 바로 닫기
            }
            bool visible = Time.timeScale > 0f && Time.time < expires; // 정지·레벨업과 겹치지 않는 통신
            if (panel.activeSelf != visible) // 상태가 바뀔 때만 하이러키 전환
            {
                panel.SetActive(visible); // 유효 통신이나 기존 게임 화면 표시
            }
        }

        private void Broadcast(string label, string line) // 한 화면의 편집된 제목과 메시지 재사용
        {
            heading.text = label; // 무엇을 막 만났는지 표시
            content.text = line; // 기획서 사실과 보스별 임시 작전 통신
            expires = Time.time + 5.5f; // 전투 시간에서 짧게 표시
        }
    }
}
