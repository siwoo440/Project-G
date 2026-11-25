using System; // 조종사별 기록 슬롯
using UnityEngine; // 저장 가능한 플레이 환경과 시간 표시

namespace ProjectG // 프로젝트 게임 영역
{
    internal static class FlightBossRushRecords // 본편과 분리한 조종사별 클리어 기록
    {
        private static readonly int[] Session = new int[Enum.GetValues(typeof(FlightBroomPilot)).Length]; // 설정 저장이 없는 실행의 임시 기록

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인을 유지한 Unity 재실행 초기화
        private static void ResetSession() // 새 세션의 임시 기록 삭제
        {
            Array.Clear(Session, 0, Session.Length); // 이전 임시 최고 시간을 제거
        }

        internal static int Best(FlightBroomPilot pilot) // 클리어 기록이 없으면 0
        {
            int index = (int)pilot; // 선택 조종사의 고유 슬롯
            return FlightTitleSettings.CanStore ? PlayerPrefs.GetInt(Key(pilot), 0) : Session[index]; // 독립 환경의 실제 기록
        }

        internal static bool TryRecord(FlightBroomPilot pilot, float seconds) // 격파 시간의 실제 단축 여부
        {
            if (seconds <= 0f || seconds > int.MaxValue / 100f || float.IsNaN(seconds)) // 유효한 양수 전투 시간
            {
                return false; // 빈 기록에 무효 값 저장 방지
            }

            int centiseconds = Mathf.Max(1, Mathf.RoundToInt(seconds * 100f)); // 화면 정밀도에 맞춘 결과
            int previous = Best(pilot); // 같은 조종사의 이미 저장된 기록
            if (previous > 0 && previous <= centiseconds) // 동점과 더 늦은 기록 제외
            {
                return false; // 기존 최고 기록 보존
            }

            if (FlightTitleSettings.CanStore) // 실제 Windows와 독립 검증용 저장
            {
                PlayerPrefs.SetInt(Key(pilot), centiseconds); // 이어서하기 키와 분리된 도전 기록
                PlayerPrefs.Save(); // 도전 성공 화면을 닫아도 보존
            }
            else
            {
                Session[(int)pilot] = centiseconds; // 읽기 전용 검증에서는 현재 실행에 보존
            }

            return true; // 첫 성공 또는 단축 기록 반영
        }

        internal static string Format(int centiseconds) // 결과와 최고 기록에 공통으로 쓰는 분·초
        {
            return $"{centiseconds / 6000:00}:{centiseconds / 100 % 60:00}.{centiseconds % 100:00}"; // 분과 초 및 백분의 일 초
        }

        private static string Key(FlightBroomPilot pilot) => FlightTitleSettings.Key($"BossRush.Best.{(int)pilot}"); // 조종사별 독립 키
    }
}
