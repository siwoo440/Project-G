using UnityEngine; // 새 출격의 실제 캐릭터 적용

namespace ProjectG // 프로젝트 게임 영역
{
    internal static class FlightSortieChoice // 메인·준비·전투 사이의 캐릭터 선택
    {
        internal const string BriefingScene = "FlightSortieBriefing"; // 배포 출격 준비 씬
        internal const string StageScene = "OuterIslandsStage"; // 정식 하늘섬 전투
        private static FlightBroomPilot preparing; // 타이틀에서 선택 화면으로 가져갈 캐릭터
        private static FlightBroomPilot? pending; // 출발 승인 후 다음 항로에서 읽을 조종사

        internal static FlightBroomPilot Initial => preparing; // 출격 준비의 시작 선택
        internal static bool ViaVillage { get; private set; } // 준비에서 돌아갈 실제 입장 경로

        internal static void RememberPilot(FlightBroomPilot selected) // 연습과 마을을 오가는 임시 조종사 선택
        {
            preparing = selected; // 저장과 실제 전투 시작 없이 이전 선택 복원
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 재시작이 없는 플레이도 정리
        private static void Reset() // 이전 플레이의 임시 씬 전달값 삭제
        {
            preparing = FlightBroomPilot.Aria; // 최초 캐릭터의 기본값
            pending = null; // 승인된 출격 없음
            ViaVillage = false; // 시작하지 않은 마을 동선
        }

        internal static void Open(bool resume) // 새 게임 또는 이어서하기 버튼
        {
            preparing = resume && FlightTitleSettings.CanStore && PlayerPrefs.GetInt(FlightTitleSettings.Key("Pilot"), 0) == (int)FlightBroomPilot.Rian // 마지막에 확정한 실제 조종사
                ? FlightBroomPilot.Rian : FlightBroomPilot.Aria; // 현재 조작 가능한 두 캐릭터
            pending = null; // 새로운 시작 선택의 이전 값 제거
            ViaVillage = false; // 착륙장에서 출격소를 찾아가는 새 진입
        }

        internal static void EnterViaVillage() // 그 출격소에서 편성 화면을 연 경우
        {
            ViaVillage = true; // 준비 화면의 뒤로가기를 같은 시설에 연결
        }

        internal static void Launch(FlightBroomPilot selected) // 출발 확정에서만 저장
        {
            pending = selected; // 실제 출격 시작점까지 전달
            ViaVillage = false; // 마을과 준비의 임시 방문이 끝난 상태
            if (FlightTitleSettings.CanStore) // 플레이어 또는 독립 시험의 저장 환경
            {
                PlayerPrefs.SetString(FlightTitleSettings.Key("Resume"), StageScene); // 실제 출발 이후 이어서하기 상태
                PlayerPrefs.SetInt(FlightTitleSettings.Key("Pilot"), (int)selected); // 다음에 이어서 선택할 조종사
                PlayerPrefs.Save(); // 게임 재실행 후에도 출격 준비 복원
            }
        }

        internal static FlightBroomPilot Consume() // 다음 실제 전투가 선택을 한 번 읽기
        {
            FlightBroomPilot selected = pending ?? FlightBroomPilot.Aria; // 선택 없이 씬을 열면 기존 기본값
            pending = null; // 다음 독립 테스트로 조종사가 넘어가지 않게 제거
            return selected; // 이번 씬의 시작 조종사
        }
    }

    [DefaultExecutionOrder(-80)] // 기존 무기의 Awake가 끝난 뒤 첫 전투 프레임 전에 적용
    public sealed class FlightSortiePilot : MonoBehaviour // 출격 준비에서 고른 조종사를 전투에 반영
    {
        [SerializeField] private FlightWeapon weapons; // 실제 자동탄·고유기 프로필
        [SerializeField] private FlightCombatHud hud; // 선택 캐릭터의 스킬 이미지와 패시브

        private void Start() // 실제 하늘섬 전투 진입
        {
            bool rian = FlightSortieChoice.Consume() == FlightBroomPilot.Rian; // 확인한 조종사
            if (weapons.IsRian != rian) // 시작 설정과 다른 조종사 선택
            {
                weapons.TogglePilot(); // 기존 스킬과 빗자루 장착 전환 재사용
            }
            hud.SetSniper(rian); // 처음부터 선택한 캐릭터의 전투 HUD
        }
    }
}
