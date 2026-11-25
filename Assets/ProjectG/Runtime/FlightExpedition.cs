using System; // 탐험 체크포인트의 직렬화
using UnityEngine; // 저장 키와 새 실행 초기화

namespace ProjectG // 프로젝트 게임 영역
{
    internal enum FlightExpeditionNode // 지도에서 선택하는 다섯 구간
    {
        Dock, // 준비한 조합으로 첫 출발
        Fork, // 교전 또는 우회 보급 분기
        Battle, // 선택한 항로의 실제 교전
        Merchant, // 이번 탐험만의 임시 상점
        Final // 마지막 공중 전투
    }

    internal static class FlightExpedition // 본편과 분리한 지도·일시 성장·중단 상태
    {
        [Serializable] // 한 개의 키에 저장할 체크포인트
        internal sealed class Checkpoint // 한 번에 갱신하는 현재 탐험 전체
        {
            public int version = 1; // 현재 탐험 데이터의 형식
            public FlightBroomPilot pilot; // 첫 출발에서 결정한 조종사
            public FlightExpeditionNode node; // 마지막으로 확정할 지도 구간
            public int route; // 0 미선택, 1 교전, 2 우회 보급
            public int tokens; // 해당 탐험의 임시 보급표
            public int power; // 자동탄 피해의 임시 강화 단계
            public bool bought; // 상점의 한 번 구매한 상태
        }

        internal static Checkpoint Current { get; private set; } // 실전과 지도에서 함께 읽는 이번 상태
        private static string Key => FlightTitleSettings.Key("Expedition.v1"); // 일반 출격 및 영구 재화와 분리한 저장
        private static bool openMap; // 전투를 끝내 지도부터 열 때 한 번 전달
        private static string outcome; // 그 전투의 실제 결과 안내

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 앱을 새로 실행한 상태
        private static void ResetSession() // 메모리 상태만 복원할 준비
        {
            Current = null; // 저장한 체크포인트는 다시 읽기 전까지 비움
            openMap = false; // 부팅 때 자동으로 지도를 열지 않음
            outcome = null; // 지난 화면 안내 초기화
        }

        internal static void Restore() // 중단했던 경로를 저장에서 가져오기
        {
            if (Current != null || !FlightTitleSettings.CanStore || !PlayerPrefs.HasKey(Key)) // 활성 실행을 덮어쓰지 않음
            {
                return; // 현재 모드 또는 저장 없음
            }

            try // 손상된 체크포인트에서 원래 게임 계속 가능
            {
                Checkpoint saved = JsonUtility.FromJson<Checkpoint>(PlayerPrefs.GetString(Key)); // 하나의 원자적 탐험 스냅샷
                if (Valid(saved)) // 이전 버전 및 실제 진행 조건 검사
                {
                    Current = saved; // 마지막으로 확정한 지점
                }
            }
            catch (ArgumentException) // 프로젝트 외부에서 손상된 직렬화
            {
                Debug.LogWarning("탐험 체크포인트를 읽을 수 없습니다."); // 원래 본편은 정상 사용
            }
        }

        internal static bool StartFrom(FlightBroomPilot pilot) // 선택한 조종사로 하나의 탐험 시작
        {
            if (Current != null || !FlightPilotRoster.CanSelect(pilot)) // 이미 시작한 탐험 또는 잠긴 조종사
            {
                return false; // 기존 체크포인트 보존
            }

            Current = new Checkpoint { pilot = pilot }; // 항구와 초기 보급
            Save(); // 중단해도 같은 조종사로 복구
            return true; // 지도 갱신 허용
        }

        internal static bool Depart() // 항구에서만 첫 분기에 도착
        {
            if (Current?.node != FlightExpeditionNode.Dock) // 이미 선택을 진행한 경우
            {
                return false; // 같은 지점을 두 번 통과하지 않음
            }

            Current.node = FlightExpeditionNode.Fork; // 지도의 두 경로를 선택할 수 있는 위치
            Save(); // 뒤늦게 종료해도 경로 선택에서 출발
            return true; // 새 지점 갱신
        }

        internal static bool ChooseRoute(bool fight) // 첫 경로 선택 영구 반영
        {
            if (Current?.node != FlightExpeditionNode.Fork) // 같은 갈림길의 중복 선택 차단
            {
                return false; // 보급이나 교전 보상 반복 지급 방지
            }

            Current.route = fight ? 1 : 2; // 현재 탐험의 진행 경로
            Current.node = fight ? FlightExpeditionNode.Battle : FlightExpeditionNode.Merchant; // 교전 전 저장 또는 보급 후 상점
            if (!fight) // 우회 경로의 시험 보급
            {
                Current.tokens = 1; // 작은 보급으로 교전 건너뛰기
            }

            Save(); // 종료하면 같은 분기와 보급을 복구
            return true; // 다음 동작 허용
        }

        internal static bool Buy() // 임시 상점의 자동탄 강화 1회
        {
            if (Current?.node != FlightExpeditionNode.Merchant || Current.bought || Current.tokens < 1) // 충분한 임시 재화와 유일한 상품
            {
                return false; // 연구 중인 가격에 따른 제한
            }

            Current.tokens--; // 이번 탐험의 보급표 한 장
            Current.power++; // 같은 탐험의 끝까지 적용할 실제 자동탄 강화
            Current.bought = true; // 동일한 선택의 비용 중복 방지
            Save(); // 임시 강화와 비용을 함께 저장
            return true; // 상점 표시를 갱신
        }

        internal static bool LeaveMarket() // 임시 강화 후 마지막 전투 지점
        {
            if (Current?.node != FlightExpeditionNode.Merchant) // 상점을 통한 진행만 허용
            {
                return false; // 임의 지점에서 최종 전투 진입 금지
            }

            Current.node = FlightExpeditionNode.Final; // 최종 전투에서 이어갈 체크포인트
            Save(); // 전투 도중 종료해도 이 지점부터 재시작
            return true; // 마지막 구간 선택
        }

        internal static void Resolve(bool cleared) // 승리 또는 실패를 한 지점에 한 번 반영
        {
            if (Current == null) // 이미 처리한 결과
            {
                return; // 중복 저장 차단
            }

            if (cleared && Current.node == FlightExpeditionNode.Battle) // 교전 보상의 시험값
            {
                Current.node = FlightExpeditionNode.Merchant; // 임시 상점으로 이동
                Current.tokens = 2; // 교전에서 얻은 임시 재화
                Current.power = 1; // 교전으로 얻는 이번 도전의 자동탄 강화
                outcome = "교전 완료 · 보급표 2장과 자동탄 피해 +20%"; // 실제 보상과 간결한 안내
                Save(); // 같은 교전을 반복 지급하지 않음
            }
            else if (!cleared || Current.node == FlightExpeditionNode.Final) // 실패나 마지막 승리에서 이번 탐험 끝
            {
                Cancel(cleared ? "탐험 완료 · 임시 재화와 강화 초기화" : "탐험 실패 · 임시 재화와 강화 초기화"); // 원래 영구 저장은 유지
            }
        }

        internal static void Cancel(string message) // 포기와 최종 종료의 공통 진행 해제
        {
            Current = null; // 새로운 탐험은 초기 조합
            outcome = message; // 지도에 표시할 실제 종료 이유
            if (FlightTitleSettings.CanStore) // 격리 또는 사용자 저장이 가능한 실행
            {
                PlayerPrefs.DeleteKey(Key); // 탐험 체크포인트 한 개만 제거
                PlayerPrefs.Save(); // 재실행에서 끝난 탐험은 복원하지 않음
            }
        }

        internal static void RequestMap() => openMap = true; // 다음 출격 준비에서 탐험 지도 열기

        internal static bool ConsumeMapRequest() // 일반 출격 준비 화면과 구분
        {
            bool requested = openMap; // 전투에서 전달한 실제 전환
            openMap = false; // 다음 별도 준비는 기본 화면
            return requested; // 현재 선택 자동 복원
        }

        internal static string ConsumeOutcome() // 현재 지도에서 한 번 읽을 결과
        {
            string message = outcome; // 교전 또는 완료 안내
            outcome = null; // 다음 화면에 이전 결과 반복 제외
            return message; // 빈 메시지는 해당 지도 설명 사용
        }

        private static bool Valid(Checkpoint saved) // 실제 플레이에서 생성할 수 있는 유효 범위
        {
            if (saved == null || saved.version != 1 || (uint)saved.pilot >= FlightPilotRoster.Count || !FlightPilotRoster.CanSelect(saved.pilot)) // 버전과 조종사 범위 확인
            {
                return false; // 잘못된 파일과 잠긴 조종사 제외
            }

            if (saved.node is FlightExpeditionNode.Dock or FlightExpeditionNode.Fork) // 갈림길을 고르기 전 보상 없음
            {
                return saved.route == 0 && saved.tokens == 0 && saved.power == 0 && !saved.bought; // 첫 항구에서 실제 저장되는 값
            }

            if (saved.node == FlightExpeditionNode.Battle) // 교전 진입 전의 분기
            {
                return saved.route == 1 && saved.tokens == 0 && saved.power == 0 && !saved.bought; // 보상을 받기 전의 올바른 상태
            }

            if (saved.node is not (FlightExpeditionNode.Merchant or FlightExpeditionNode.Final) || saved.route is < 1 or > 2) // 실제 보급 및 상점 이후 지점
            {
                return false; // 유효한 분기 외 항로 금지
            }

            int supply = saved.route == 1 ? 2 : 1; // 선택한 분기의 원래 보급량
            int spent = saved.bought ? 1 : 0; // 단일 구매가 사용하는 수량
            return saved.tokens == supply - spent && saved.power == supply - 1 + spent; // 구매 및 강화가 일치하는 유효 지점
        }

        private static void Save() // 완성된 체크포인트 하나만 쓰기
        {
            if (FlightTitleSettings.CanStore) // 테스트와 배포에서 지정한 저장
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current)); // 동일한 경로와 보급을 한 항목으로 저장
                PlayerPrefs.Save(); // 중단 이후 지점부터 다시 시작
            }
        }
    }
}
