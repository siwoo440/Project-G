using UnityEngine; // 유니티 화면 기능
using UnityEngine.UI; // 씬에 저장된 화면 요소
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class SceneBattleHudPresenter : MonoBehaviour // 편집 가능한 전투 게이지
    { // 구성 시작
        [SerializeField] private Camera menuCamera; // 기존 출격 관리자 카메라
        [SerializeField] private CanvasGroup healthGroup; // 체력 영역 투명도
        [SerializeField] private CanvasGroup energyGroup; // 에너지 영역 투명도
        [SerializeField] private CanvasGroup experienceGroup; // 레벨과 경험치 영역 투명도
        [SerializeField] private Image healthFill; // 체력 게이지 채움
        [SerializeField] private Image energyFill; // 에너지 게이지 채움
        [SerializeField] private Image experienceFill; // 경험치 게이지 채움
        [SerializeField] private Text healthText; // 실제 체력 숫자
        [SerializeField] private Text energyText; // 실제 에너지 숫자
        [SerializeField] private Text levelText; // 현재 레벨 숫자
        [SerializeField] private Text experienceText; // 실제 경험치 숫자
        private readonly Vector3[] corners = new Vector3[4]; // 화면 영역 재사용 좌표
        private GameSessionHost host; // 현재 출격 관리자
        private Canvas canvas; // 편집된 캔버스
        private float healthAlpha; // 편집한 체력 투명도
        private float energyAlpha; // 편집한 에너지 투명도
        private float experienceAlpha; // 편집한 경험치 투명도
        public bool IsReady => menuCamera != null && healthGroup != null && energyGroup != null && experienceGroup != null && healthGroup.transform is RectTransform && energyGroup.transform is RectTransform && experienceGroup.transform is RectTransform && healthFill != null && energyFill != null && experienceFill != null && healthText != null && energyText != null && levelText != null && experienceText != null && Filled(healthFill) && Filled(energyFill) && Filled(experienceFill); // 필수 씬 연결과 채움 설정
        private void Awake() // 화면 구성 확인
        { // 확인 시작
            if (!IsReady) // 참조와 이미지 방식 확인
            { // 오류 시작
                Debug.LogError("전투 HUD의 카메라, 게이지 이미지, 숫자 또는 가로 채움 설정이 누락되었습니다.", this); // 편집 누락 안내
                enabled = false; // 불완전한 화면 갱신 중단
                return; // 잘못된 연결 차단
            } // 오류 종료
            canvas = GetComponentInParent<Canvas>(true); // 비활성 부모를 포함한 캔버스 확인
            if (canvas == null) // 캔버스 누락 확인
            { // 오류 시작
                Debug.LogError("전투 HUD를 표시할 Canvas가 없습니다.", this); // 편집 누락 안내
                enabled = false; // 불완전한 화면 갱신 중단
                return; // 잘못된 연결 차단
            } // 오류 종료
            healthAlpha = healthGroup.alpha; // 편집한 체력 투명도 보관
            energyAlpha = energyGroup.alpha; // 편집한 에너지 투명도 보관
            experienceAlpha = experienceGroup.alpha; // 편집한 경험치 투명도 보관
        } // 확인 종료
        private void LateUpdate() // 실제 전투 값 반영
        { // 갱신 시작
            if (menuCamera == null) // 제거된 메뉴 카메라 확인
            { // 숨김 시작
                Hide(); // 연결이 사라진 게이지 숨김
                return; // 파괴된 객체 접근 차단
            } // 숨김 종료
            if (host == null) // 출격 관리자 연결 확인
            { // 연결 시작
                host = menuCamera.GetComponent<GameSessionHost>(); // 씬의 실제 출격 관리자
            } // 연결 종료
            if (host == null || host.Flow == null || !host.Flow.IsBattleActive || host.Flow.InputBlocked || Time.timeScale <= 0f || host.CurrentPlayer == null) // 전투와 모달 상태 확인
            { // 숨김 시작
                Hide(); // 메뉴와 결과 화면 숨김
                return; // 전투 값 갱신 생략
            } // 숨김 종료
            BattlePlayer player = host.CurrentPlayer; // 현재 출격 플레이어
            BattleProgress progress = player.GetComponent<BattleProgress>(); // 현재 경험치 상태
            Camera camera = progress != null ? progress.BattleCamera : null; // 현재 전투 카메라
            Present(player, camera, progress); // 실제 상태 표시
        } // 갱신 종료
        public void Present(BattlePlayer player, Camera camera, BattleProgress progress) // 실제 전투 값과 겹침 반영
        { // 표시 시작
            if (!IsReady || player == null || player.Survival == null || player.Skills == null || progress == null || progress.Level == null || camera == null) // 필수 전투 참조 확인
            { // 숨김 시작
                Hide(); // 불완전한 정보 숨김
                return; // 잘못된 값 표시 차단
            } // 숨김 종료
            healthFill.fillAmount = Fraction(player.Survival.Health, player.Survival.MaximumHealth); // 실제 체력 비율
            energyFill.fillAmount = Fraction(player.Skills.ExactEnergy, 100f); // 소수 포함 실제 에너지 비율
            experienceFill.fillAmount = Fraction(progress.Level.Experience, progress.Level.Required); // 실제 경험치 비율
            healthText.text = "HP " + player.Survival.Health + " / " + player.Survival.MaximumHealth; // 체력 원본 수치
            energyText.text = "ENERGY " + player.Skills.Energy + " / 100"; // 표시용 에너지 수치
            levelText.text = "LV. " + progress.Level.Level; // 현재 레벨
            experienceText.text = "XP " + progress.Level.Experience + " / " + progress.Level.Required; // 경험치 이름과 원본 수치
            Rect playerRect = HudOcclusion.PlayerRect(camera, player); // 실제 플레이어 표시 범위
            healthGroup.alpha = healthAlpha * HudOcclusion.Alpha(ScreenBounds((RectTransform)healthGroup.transform), playerRect); // 체력 영역 겹침 처리
            energyGroup.alpha = energyAlpha * HudOcclusion.Alpha(ScreenBounds((RectTransform)energyGroup.transform), playerRect); // 에너지 영역 겹침 처리
            experienceGroup.alpha = experienceAlpha * HudOcclusion.Alpha(ScreenBounds((RectTransform)experienceGroup.transform), playerRect); // 경험치 영역 겹침 처리
        } // 표시 종료
        private void Hide() // 세 영역의 전투 표시 숨김
        { // 숨김 시작
            if (healthGroup != null) // 체력 영역 확인
            { // 체력 숨김 시작
                healthGroup.alpha = 0f; // 체력 표시 숨김
            } // 체력 숨김 종료
            if (energyGroup != null) // 에너지 영역 확인
            { // 에너지 숨김 시작
                energyGroup.alpha = 0f; // 에너지 표시 숨김
            } // 에너지 숨김 종료
            if (experienceGroup != null) // 경험치 영역 확인
            { // 경험치 숨김 시작
                experienceGroup.alpha = 0f; // 경험치 표시 숨김
            } // 경험치 숨김 종료
        } // 숨김 종료
        public static float Fraction(float current, float maximum) // 게이지 비율 계산
        { // 계산 시작
            if (float.IsNaN(current) || float.IsInfinity(current) || float.IsNaN(maximum) || float.IsInfinity(maximum) || maximum <= 0f) // 잘못된 입력 확인
            { // 기본값 시작
                return 0f; // 안전한 빈 게이지
            } // 기본값 종료
            return Mathf.Clamp01(current / maximum); // 표시 폭만 안전 범위 제한
        } // 계산 종료
        private static bool Filled(Image image) // 편집된 가로 게이지 확인
        { // 확인 시작
            return image.sprite != null && image.type == Image.Type.Filled && image.fillMethod == Image.FillMethod.Horizontal && image.fillOrigin == 0; // 스프라이트와 왼쪽 기준 가로 채움
        } // 확인 종료
        private Rect ScreenBounds(RectTransform bounds) // 편집된 HUD의 실제 화면 영역
        { // 계산 시작
            bounds.GetWorldCorners(corners); // 현재 편집 위치와 크기 조회
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera; // 캔버스 좌표 기준
            Vector2 first = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]); // 첫 모서리 화면 좌표
            float minX = first.x; // 최소 가로 좌표
            float maxX = first.x; // 최대 가로 좌표
            float minY = first.y; // 최소 세로 좌표
            float maxY = first.y; // 최대 세로 좌표
            for (int index = 1; index < corners.Length; index++) // 나머지 모서리 순회
            { // 순회 시작
                Vector2 point = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[index]); // 모서리 화면 좌표
                minX = Mathf.Min(minX, point.x); // 최소 가로 갱신
                maxX = Mathf.Max(maxX, point.x); // 최대 가로 갱신
                minY = Mathf.Min(minY, point.y); // 최소 세로 갱신
                maxY = Mathf.Max(maxY, point.y); // 최대 세로 갱신
            } // 순회 종료
            return Rect.MinMaxRect(minX, Screen.height - maxY, maxX, Screen.height - minY); // HUD 공통 좌표 변환
        } // 계산 종료
    } // 구성 종료
} // 영역 종료
