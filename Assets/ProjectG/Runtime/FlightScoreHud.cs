using System.Globalization; // 해상도와 언어에 관계없는 점수 서식
using UnityEngine; // 출격 시간과 화면 좌표
using UnityEngine.UI; // 씬의 점수와 획득 숫자

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightScoreHud : MonoBehaviour // 출격 점수와 연속 획득 표시
    {
        [SerializeField] private FlightPilotVitals pilot; // 근접 회피를 판정하는 플레이어
        [SerializeField] private FlightEnemy[] rewardSources; // 점수를 지급하는 씬의 적
        [SerializeField] private Text totalLabel; // 출격 점수 합계
        [SerializeField] private Text[] popupLabels; // 씬에 배치한 획득 숫자 슬롯
        [SerializeField, Min(0)] private int defeatPoints = 100; // 마물 한 마리 처치 시험 점수
        [SerializeField, Min(0)] private int grazePoints = 10; // 유효 스침 한 번의 시험 점수
        [SerializeField, Min(0.01f)] private float lifetime = 1.6f; // 각 숫자의 전투 시간 기준 수명
        [SerializeField, Min(0f)] private float rise = 30f; // 새 획득 한 건당 위로 이동할 높이
        private const float RiseSpeed = 250f; // 획득 숫자의 초당 상승 거리
        private const float FadeSeconds = 0.36f; // 숫자가 사라지기 전 투명도 변화 시간
        private Vector2[] origins; // 각 숫자의 씬 편집 위치
        private float[] expiresAt; // 각 획득 숫자의 개별 만료 시각
        private int nextSlot; // 다음 획득에 재사용할 슬롯
        private int activeCount; // 현재 수명이 남은 숫자 개수

        public long Total { get; private set; } // 이번 출격의 누적 점수
        public int DefeatCount { get; private set; } // 결과에 표시할 실제 격파 마물 수

        private void Awake() // 씬에 배치한 각 획득 숫자의 시작 위치 보관
        {
            origins = new Vector2[popupLabels.Length]; // 슬롯별 원래 위치 배열
            expiresAt = new float[popupLabels.Length]; // 슬롯별 종료 시각 배열
            for (int index = 0; index < popupLabels.Length; index++) // 편집된 슬롯 순회
            {
                origins[index] = popupLabels[index].rectTransform.anchoredPosition; // 각 숫자의 최초 배치
            }
            ResetRun(); // 첫 출격 합계와 획득 숫자 준비
        }

        private void OnEnable() // 실제 전투 이벤트 구독
        {
            pilot.Grazed += OnGrazed; // 탄막에서 인정한 유효 스침 연결
            foreach (FlightEnemy enemy in rewardSources) // 씬에 등록된 적 순회
            {
                enemy.Defeated += OnDefeated; // 적의 실제 사망에만 연결
            }
        }

        private void OnDisable() // 씬이 닫히거나 점수 표시가 비활성화될 때 구독 해제
        {
            if (pilot != null) // 이미 해제된 씬의 플레이어 제외
            {
                pilot.Grazed -= OnGrazed; // 그레이즈 구독 해제
            }
            foreach (FlightEnemy enemy in rewardSources) // 등록한 적 순회
            {
                if (enemy != null) // 씬에서 먼저 제거된 적 제외
                {
                    enemy.Defeated -= OnDefeated; // 처치 구독 해제
                }
            }
        }

        internal void ResetRun() // 출격을 다시 시작할 때 점수 초기화
        {
            Total = 0; // 새 출격의 합계
            DefeatCount = 0; // 새 출격의 실제 격파 수 초기화
            totalLabel.text = "점수 0"; // 화면 합계 초기화
            nextSlot = 0; // 첫 표시 슬롯으로 복귀
            activeCount = 0; // 이전 출격의 표시 수 제거
            foreach (Text label in popupLabels) // 남은 모든 숫자 순회
            {
                label.gameObject.SetActive(false); // 이전 획득 숫자 숨김
            }
        }

        private void OnDefeated(FlightEnemy enemy) // 등록된 적의 처치 보상
        {
            DefeatCount++; // 결과에서 표시할 실제 마물 수
            Award(defeatPoints); // 적 처치 시험 점수
        }

        private void OnGrazed() // 근접 회피 확정 보상
        {
            Award(grazePoints); // 그레이즈 시험 점수
        }

        private void Award(int points) // 합계를 갱신하고 새 획득 숫자 배치
        {
            if (points <= 0 || pilot.IsDefeated) // 보상이 없거나 출격에 실패한 상태
            {
                return; // 전투 점수 유지
            }
            Total += points; // 모든 유효 획득을 출격 점수에 누적
            totalLabel.text = $"점수 {Total.ToString("N0", CultureInfo.InvariantCulture)}"; // 기존 언어와 천 단위 구분
            if (popupLabels.Length == 0) // 숫자 생략을 선택한 씬
            {
                return; // 합계 표시만 유지
            }
            Text label = popupLabels[nextSlot]; // 새 획득이 쓸 씬 슬롯
            label.text = $"+{points.ToString("N0", CultureInfo.InvariantCulture)}"; // 새 줄의 획득 점수
            label.rectTransform.anchoredPosition = origins[nextSlot]; // 목록 맨 아래에 등장
            label.gameObject.SetActive(true); // 비활성 획득 숫자 재사용
            label.canvasRenderer.SetAlpha(1f); // 이전 획득에 사용한 투명도 초기화
            expiresAt[nextSlot] = Time.time + lifetime; // 새 숫자의 개별 종료 시각
            nextSlot = (nextSlot + 1) % popupLabels.Length; // 모자라면 가장 오래된 슬롯 재사용
            activeCount = Mathf.Min(activeCount + 1, popupLabels.Length); // 동시에 보이는 숫자 개수
        }

        private void LateUpdate() // 달라진 숫자의 위치와 투명도를 화면에 반영
        {
            int visibleCount = 0; // 아직 살아 있는 획득 숫자 수
            for (int rank = 0; rank < activeCount; rank++) // 최신 숫자부터 오래된 숫자 순회
            {
                int slot = (nextSlot + popupLabels.Length - rank - 1) % popupLabels.Length; // 표시 순서에 대응하는 슬롯
                Text label = popupLabels[slot]; // 해당 숫자 UI
                float remaining = expiresAt[slot] - Time.time; // 일시정지를 제외한 남은 수명
                if (remaining <= 0f) // 각각의 표시 시간이 지난 숫자
                {
                    label.gameObject.SetActive(false); // 수명이 지난 숫자 숨김
                    continue; // 화면 위치 갱신 생략
                }
                visibleCount++; // 다음 프레임에도 표시할 숫자
                RectTransform rect = label.rectTransform; // 이동하는 숫자의 씬 좌표
                Vector2 target = origins[slot] + Vector2.up * (rank * rise); // 새 획득 수만큼 높아진 목적지
                rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, target, RiseSpeed * Time.deltaTime); // 기존 줄의 부드러운 상승
                label.canvasRenderer.SetAlpha(Mathf.Clamp01(remaining / FadeSeconds)); // 수명 종료 직전의 페이드아웃
            }
            activeCount = visibleCount; // 만료된 줄을 다음 프레임 순회에서 제외
        }
    }
}
