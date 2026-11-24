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
        [SerializeField] private Text total; // 실전에서만 표시하는 전체 소요 시간
        private int shownSecond = -1; // 표시한 마지막 경과 시간
        private int shownWave = -1; // 표시한 마지막 번호
        private int shownEnemies = -1; // 표시한 마지막 적 수
        private int shownTotal = -1; // 표시한 마지막 전체 소요 시간
        private bool shownComplete; // 현재 표시한 항로 완료 여부

        private void LateUpdate() // 처치와 웨이브 변화를 실제 HUD에 반영
        {
            int second = Mathf.FloorToInt(encounter.ElapsedSeconds); // 표시할 정수 초
            if (shownSecond != second) // 경과 시간이 바뀐 프레임
            {
                shownSecond = second; // 마지막 갱신 상태
                timer.text = $"{second / 60:00}:{second % 60:00}"; // 분·초 큰 숫자
            }
            if (shownWave != encounter.WaveNumber || shownComplete != encounter.IsStageComplete) // 웨이브 번호와 항로 완료 변경
            {
                shownWave = encounter.WaveNumber; // 실제 출현 번호
                shownComplete = encounter.IsStageComplete; // 표시한 항로 완료 상태
                wave.text = shownComplete ? "항로 완료" : shownWave == 0 ? "준비" : $"Wave : {shownWave}"; // 준비·전투·완료의 웨이브 줄
            }
            int enemies = encounter.ActiveEnemyCount; // 현재 씬에 남은 마물 수
            if (shownEnemies != enemies) // 처치 또는 이탈로 달라진 값
            {
                shownEnemies = enemies; // 이전 개체 표시 저장
                remaining.text = $": {shownEnemies}"; // 적 아이콘 오른쪽 숫자
            }
            if (total != null) // 실전 전체 시간 줄이 배치된 씬
            {
                int seconds = Mathf.FloorToInt(encounter.TotalSeconds); // 전체 소요를 정수 초로 표시
                if (seconds != shownTotal) // 실제 표시값이 달라진 경우
                {
                    shownTotal = seconds; // 마지막으로 표기한 전체 시간
                    total.text = $"전체 {seconds / 60:00}:{seconds % 60:00}"; // 정지까지 포함한 분·초
                }
            }
        }
    }
}
