using System.Globalization; // 체력 숫자의 천 단위 표시
using UnityEngine; // 씬에 저장한 전용 체력바
using UnityEngine.UI; // 이미지·상태와 페이즈 점

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightBossHud : MonoBehaviour // 실제 최종 보스의 체력과 남은 단계
    {
        [SerializeField] private FlightEnemyEncounter encounter; // 현재 전투의 보스 상태
        [SerializeField] private FlightFinalBoss boss; // 실제 체력과 페이즈 출처
        [SerializeField] private GameObject panel; // 씬에서 편집할 보스 전용 화면
        [SerializeField] private RawImage fill; // 제작한 붉은 게이지 채움
        [SerializeField] private Text health; // 현재 및 최대 체력 숫자
        [SerializeField] private Text status; // 현재 패턴 또는 전환 안내
        [SerializeField] private Text[] dots; // 노란 페이즈 점 세 개
        [SerializeField] private Color activeDot = new Color(1f, 0.83f, 0.31f); // 남아 있는 페이즈의 노란색
        [SerializeField] private Color spentDot = new Color(0.53f, 0.58f, 0.67f); // 끝난 페이즈의 윤곽색
        private FlightEnemy life; // 보스의 공통 체력 컴포넌트
        private Rect fullImage; // 제작된 채움 이미지의 전체 UV
        private float fullWidth; // 에디터에서 지정한 체력의 최대 폭
        private int shownHealth = -1; // 마지막으로 그린 실제 체력
        private int shownPhase; // 마지막으로 그린 현재 구간
        private string shownStatus; // 마지막으로 표시한 안내

        private void Awake() // 씬 배치와 초기 표시 범위 저장
        {
            life = boss.Enemy; // 실제 전투 수치 연결
            fullWidth = fill.rectTransform.rect.width; // 체력이 가득 찼을 때의 폭
            fullImage = fill.uvRect; // 원래 이미지의 전체 잘림 범위
        }

        private void LateUpdate() // 실제 보스가 있을 때만 전용 HUD 갱신
        {
            bool active = encounter.IsFinalBossActive; // 스테이지의 현재 전투 여부
            if (panel.activeSelf != active) // 등장 또는 격파·재출격 상태 변화
            {
                panel.SetActive(active); // 보스가 사라질 때 전용 정보 숨김
                shownHealth = -1; // 재등장 때 가득 찬 체력을 다시 표시
                shownPhase = 0; // 새 보스의 세 점 초기화
                shownStatus = null; // 진입 표시를 다시 갱신
            }
            if (!active) // 일반 전투 또는 클리어 화면
            {
                return; // 관련 그림과 숫자 변경 생략
            }
            if (shownHealth != life.Health) // 실제 체력이 달라진 프레임
            {
                shownHealth = life.Health; // 마지막 표시값 기억
                float ratio = (float)shownHealth / life.MaxHealth; // 최대 체력 대비 채움 비율
                Rect visible = fullImage; // 채움 이미지의 원본 영역
                visible.width *= ratio; // 실제 체력만큼 이미지를 잘라냄
                fill.uvRect = visible; // 남아 있는 부분만 표시
                fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ratio * fullWidth); // 왼쪽부터 줄어드는 체력
                health.text = $"{shownHealth.ToString("N0", CultureInfo.InvariantCulture)} / {life.MaxHealth.ToString("N0", CultureInfo.InvariantCulture)}"; // 실제 현재·최대 체력
            }
            if (shownPhase != boss.Phase) // 보스의 체력 구간이 바뀐 경우
            {
                shownPhase = boss.Phase; // 표시한 새 페이즈
                for (int index = 0; index < dots.Length; index++) // 끝난 단계부터 순서대로 소등
                {
                    bool left = index + 1 >= shownPhase; // 현재 구간을 포함한 남은 페이즈
                    dots[index].text = left ? "●" : "○"; // 색과 모양으로 점등 구분
                    dots[index].color = left ? activeDot : spentDot; // 밝은 노란색 또는 어두운 윤곽
                }
            }
            string nextStatus = StatusText(); // 지금 발사 또는 대기하는 전투 상태
            if (nextStatus != shownStatus) // 현재 상태가 바뀐 경우
            {
                shownStatus = nextStatus; // 마지막 표시 상태 저장
                status.text = shownStatus; // 전환과 약점의 화면 안내
            }
        }

        private string StatusText() // 패턴보다 먼저 읽을 수 있는 현재 상태
        {
            if (boss.IsEntering) // 공격하지 않는 진입 구간
            {
                return "접근 중"; // 최종 보스가 진입하는 상태
            }
            if (boss.IsTransitioning) // 피해를 차단하는 새 페이즈 진입
            {
                return "페이즈 전환"; // 경계 초과 피해가 멈추는 이유
            }
            if (boss.IsWeakPointOpen) // 공격 뒤 1초 동안의 공격 기회
            {
                return "약점 노출"; // 나타난 약점의 안내
            }
            return boss.Phase switch // 현재 체력 구간의 패턴 이름
            {
                1 => "직선 연사", // 1페이즈
                2 => "원형 확산", // 2페이즈
                _ => "교차 나선" // 3페이즈
            };
        }
    }
}
