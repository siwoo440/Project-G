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
        private bool shownBoss; // 중간 보스 전투의 표시 상태
        private bool shownFinal; // 최종 보스 전투의 표시 상태

        private void LateUpdate() // 처치와 웨이브 변화를 실제 HUD에 반영
        {
            int second = Mathf.FloorToInt(encounter.ElapsedSeconds); // 표시할 정수 초
            if (shownSecond != second) // 경과 시간이 바뀐 프레임
            {
                shownSecond = second; // 마지막 갱신 상태
                timer.text = $"{second / 60:00}:{second % 60:00}"; // 분·초 큰 숫자
            }
            if (shownWave != encounter.WaveNumber || shownComplete != encounter.IsStageComplete || shownBoss != encounter.IsMidBossActive || shownFinal != encounter.IsFinalBossActive) // 웨이브·보스·항로 상태의 변화
            {
                shownWave = encounter.WaveNumber; // 실제 출현 번호
                shownComplete = encounter.IsStageComplete; // 표시한 항로 완료 상태
                shownBoss = encounter.IsMidBossActive; // 표시한 중간 보스 상태
                shownFinal = encounter.IsFinalBossActive; // 표시한 최종 보스 상태
                if (encounter.IsBossRush) // 시험 보스 러시의 중간 상태 포함
                {
                    wave.text = shownComplete ? "도전 완료" : shownFinal ? "최종 보스" : shownBoss ? "중간 보스" : shownWave > 0 ? "보스 전환" : "보스 러시"; // 출격 준비·휴식·전투 단계
                }
                else if (encounter.IsExpedition) // 현재 탐험 지점에서의 전투 표시
                {
                    wave.text = shownComplete ? "구간 완료" : shownFinal ? "최종 보스" : shownWave == 0 ? "탐험 준비" : $"탐험 {shownWave}"; // 선택한 항로의 실제 진행
                }
                else
                {
                    wave.text = shownFinal ? "최종 보스" : shownBoss ? "중간 보스" : shownComplete ? "항로 완료" : shownWave == 0 ? "준비" : $"Wave : {shownWave}"; // 전투 구간에 맞는 표기
                }
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
