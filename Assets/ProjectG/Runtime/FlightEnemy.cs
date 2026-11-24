using UnityEngine; // 마물 모델과 3차원 위치
using UnityEngine.UI; // 제작 체력 이미지의 현재 길이

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 마물당 하나의 피격·표현 상태
    public sealed class FlightEnemy : MonoBehaviour // 시험 마물의 체력과 비행 표현
    {
        [SerializeField, Min(1)] private int maxHealth = 80; // 일반 마물의 시험 체력
        [SerializeField, Min(0)] private int experienceReward = 12; // 처치 때 드롭할 경험치
        [SerializeField, Min(0.05f)] private float displaySeconds = 2f; // 피격 후 체력 표시 시간
        [SerializeField] private Transform model; // 판정과 분리된 렌더 하이러키
        [SerializeField] private Transform leftWing; // 왼쪽 날개의 회전축
        [SerializeField] private Transform rightWing; // 오른쪽 날개의 회전축
        [SerializeField] private Transform tail; // 몸 뒤 꼬리의 회전축
        [SerializeField] private Renderer core; // 명중 효과가 표시되는 핵
        [SerializeField] private Canvas healthBar; // 처음에 숨겨진 월드 체력 이미지
        [SerializeField] private RawImage fill; // 제작한 붉은 체력 바
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor"); // URP 코어 발광 속성
        private MaterialPropertyBlock properties; // 재질 복제 없는 피격 반응
        private Vector3 neutralPosition; // 날갯짓의 기본 높이
        private Quaternion leftRest; // 왼쪽 날개의 기준 회전
        private Quaternion rightRest; // 오른쪽 날개의 기준 회전
        private Quaternion tailRest; // 꼬리의 기준 회전
        private Color idleEmission; // 평상시 핵의 발광색
        private Camera view; // 체력바가 향하는 실제 카메라
        private float fullWidth; // 전체 체력 표시의 원래 너비
        private Rect fullImage; // 생성 PNG의 실제 그림 범위
        private float visibleUntil; // 마지막 피격 후 표시 종료 시점
        private float flashUntil; // 짧은 코어 강조의 종료 시점
        private bool flashing; // 재질을 변경할 필요가 있는 피격 반응 상태

        public int Health { get; private set; } // 현재 마물의 시험 체력
        public event System.Action<Vector3, int> Defeated; // 처치 위치와 경험치 보상 알림

        private void Awake() // 편집된 모델과 이미지의 기준 상태 보관
        {
            properties = new MaterialPropertyBlock(); // 런타임 메인 스레드에서 재질 속성 생성
            neutralPosition = model.localPosition; // 비행 애니메이션 시작 높이
            leftRest = leftWing.localRotation; // 왼쪽 날개 초기 방향
            rightRest = rightWing.localRotation; // 오른쪽 날개 초기 방향
            tailRest = tail.localRotation; // 꼬리 초기 방향
            idleEmission = core.sharedMaterial.GetColor(Emission); // 코어의 평상시 발광
            fullWidth = fill.rectTransform.rect.width; // 붉은 체력 이미지 원래 너비
            fullImage = fill.uvRect; // 투명 패딩을 제외한 실제 PNG 영역
        }

        private void OnEnable() // 새로 등장했을 때 체력과 표시 초기화
        {
            Health = maxHealth; // 새 마물의 전체 체력
            visibleUntil = 0f; // 첫 피격 전 체력바 숨김
            flashUntil = 0f; // 이전 피격 반응 제거
            SetFlash(false); // 코어 발광 원래대로
            UpdateFill(); // 전체 체력 표시 크기 복원
            healthBar.enabled = false; // 월드 체력 오버레이 처음에 숨김
        }

        public void ReceiveHit(int damage) // 탄환의 실제 마물 피격
        {
            if (damage <= 0 || Health <= 0) // 유효한 피해인지 확인
            {
                return; // 제로 피해로 체력바를 열지 않음
            }

            Health = Mathf.Max(0, Health - damage); // 실제 마물 체력 감소
            if (Health == 0) // 처치로 마물 활동이 끝난 상태
            {
                gameObject.SetActive(false); // 마물 모델과 피격 오브젝트 숨김
                Defeated?.Invoke(transform.position, experienceReward); // 같은 처치에만 한 번 조각 지급
                return; // 처치 뒤 체력 표시 생략
            }

            visibleUntil = Time.time + displaySeconds; // 표시 시간 연장
            flashUntil = Time.time + 0.14f; // 짧은 피격 표현
            SetFlash(true); // 코어 피격색 적용
            UpdateFill(); // 실제 체력에 따라 생성 이미지 너비 변경
            healthBar.enabled = true; // 첫 피격부터 세계 체력바 표시
        }

        private void LateUpdate() // 비행 모델과 체력바의 실제 화면 갱신
        {
            float beat = Mathf.Sin(Time.time * 8f); // 반복되는 날갯짓 위상
            model.localPosition = neutralPosition + Vector3.up * (beat * 0.13f); // 마물 몸체의 부유
            leftWing.localRotation = leftRest * Quaternion.Euler(0f, 0f, 22f * beat); // 왼쪽 날개 회전
            rightWing.localRotation = rightRest * Quaternion.Euler(0f, 0f, -22f * beat); // 오른쪽 날개 회전
            tail.localRotation = tailRest * Quaternion.Euler(0f, 12f * Mathf.Sin(Time.time * 5f), 0f); // 비행 꼬리 흔들림
            if (flashing && Time.time >= flashUntil) // 짧은 피격광 종료
            {
                SetFlash(false); // 평상시 코어 색 적용
            }
            healthBar.enabled = Time.time < visibleUntil; // 마지막 피격 후 일정 시간이 지나면 숨김
            if (healthBar.enabled) // 표시 중일 때 화면을 향함
            {
                view = view != null ? view : Camera.main; // 실제 비행 카메라 참조
                if (view != null) // 화면 카메라가 존재하는 씬
                {
                    healthBar.transform.rotation = view.transform.rotation; // 화면의 가로·세로 방향 유지
                }
            }
        }

        private void UpdateFill() // 현재 체력에 맞춘 직사각형 색상 이미지 길이
        {
            float portion = (float)Health / maxHealth; // 남은 체력 비율
            Rect cropped = fullImage; // 제작 PNG의 원래 투명 범위
            cropped.width *= portion; // 오른쪽을 잘라 체력 감소 표시
            fill.uvRect = cropped; // 원본을 늘이지 않는 실제 이미지 잘라내기
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * portion); // 왼쪽 기준의 게이지 길이
        }

        private void SetFlash(bool enabled) // 재질 공유 상태를 건드리지 않는 피격 코어
        {
            flashing = enabled; // 발광 상태 보관
            core.GetPropertyBlock(properties); // 현재 모델의 속성 참조
            properties.SetColor(Emission, enabled ? new Color(3.1f, 0.32f, 0.05f) : idleEmission); // 짧은 피격 발광과 원래 색
            core.SetPropertyBlock(properties); // 해당 마물의 색만 적용
        }
    }
}
