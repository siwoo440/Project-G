using UnityEngine; // 상단 중앙 웨이브 상태
using UnityEngine.UI; // 씬에서 배치한 실제 표시 요소

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightWaveHud : MonoBehaviour // 큰 타이머와 작게 표시하는 웨이브 정보
    {
        [SerializeField] private FlightEnemyEncounter encounter; // 현재 시험 웨이브와 시간
        [SerializeField] private Text timer; // 경과 시간을 표시할 숫자
        [SerializeField] private Text wave; // 현재 웨이브 번호
        [SerializeField] private Text remaining; // 원형 적 아이콘 옆의 개체 수
        private int shownSecond = -1; // 표시한 마지막 경과 시간
        private int shownWave = -1; // 표시한 마지막 번호
        private int shownEnemies = -1; // 표시한 마지막 적 수

        private void LateUpdate() // 처치와 웨이브 변화를 실제 HUD에 반영
        {
            int second = Mathf.FloorToInt(encounter.ElapsedSeconds); // 표시할 정수 초
            if (shownSecond != second) // 경과 시간이 바뀐 프레임
            {
                shownSecond = second; // 마지막 갱신 상태
                timer.text = $"{second / 60:00}:{second % 60:00}"; // 분·초 큰 숫자
            }
            if (shownWave != encounter.WaveNumber) // 출현한 웨이브 번호가 바뀔 때
            {
                shownWave = encounter.WaveNumber; // 실제 시험 번호
                wave.text = $"Wave : {shownWave}"; // 타이머 아래의 웨이브 줄
            }
            int enemies = encounter.ActiveEnemyCount; // 현재 씬에 남은 마물 수
            if (shownEnemies != enemies) // 처치 또는 이탈로 달라진 값
            {
                shownEnemies = enemies; // 이전 개체 표시 저장
                remaining.text = $": {shownEnemies}"; // 적 아이콘 오른쪽 숫자
            }
        }
    }
}
