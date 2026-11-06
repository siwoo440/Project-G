using UnityEngine; // 유니티 장면 기능
using UnityEngine.SceneManagement; // 장면 확인 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class BattleSetup // 전투 장면 구성
    { // 클래스 시작
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 장면 시작 전 실행
        private static void RegisterSceneHandler() // 장면 연결 등록
        { // 등록 시작
            SceneManager.sceneLoaded -= InitializeSampleScene; // 이전 연결 제거
            SceneManager.sceneLoaded += InitializeSampleScene; // 장면 불러오기 연결
        } // 등록 종료
        private static void InitializeSampleScene(Scene scene, LoadSceneMode mode) // 기본 장면 연결
        { // 연결 시작
            if (scene.path != "Assets/Scenes/SampleScene.unity" || Object.FindFirstObjectByType<BattlePlayer>() != null || Object.FindFirstObjectByType<GameSessionHost>() != null) // 장면과 중복 확인
            { // 생략 시작
                return; // 다른 장면 또는 기존 플레이어 유지
            } // 생략 종료
            if (Camera.main != null) // 주 카메라 확인
            { // 생성 시작
                Camera.main.gameObject.AddComponent<GameSessionHost>().Begin(Camera.main); // 타이틀과 출격 화면 연결
            } // 생성 종료
        } // 연결 종료
        public static Rect CalculateViewport(float width, float height) // 표시 영역 계산
        { // 계산 시작
            if (width <= 0f || height <= 0f) // 잘못된 화면 크기 확인
            { // 기본값 시작
                return new Rect(0f, 0f, 1f, 1f); // 기본 표시 영역
            } // 기본값 종료
            float actual = width / height; // 실제 화면 비율
            float target = 16f / 9f; // 목표 화면 비율
            if (actual > target) // 넓은 화면 확인
            { // 가로 조정 시작
                float scale = target / actual; // 가로 표시 비율
                return new Rect((1f - scale) / 2f, 0f, scale, 1f); // 좌우 중앙 정렬
            } // 가로 조정 종료
            float verticalScale = actual / target; // 세로 표시 비율
            return new Rect(0f, (1f - verticalScale) / 2f, 1f, verticalScale); // 상하 중앙 정렬
        } // 계산 종료
        public static GameObject Create(Camera camera) // 전투 화면 생성
        { // 생성 시작
            camera.orthographic = true; // 직교 카메라 사용
            camera.orthographicSize = 5f; // 시험용 화면 높이
            camera.aspect = 16f / 9f; // 목표 화면 비율
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.16f); // 임시 배경 색상
            camera.gameObject.AddComponent<BattleViewport>(); // 화면 비율 유지
            GameObject background = new GameObject("StageBackground"); // 하늘섬 배경 객체
            background.transform.SetParent(camera.transform, false); // 카메라에 배경 연결
            background.transform.localPosition = new Vector3(0f, 0f, 10f); // 카메라 앞 배경 배치
            SpriteRenderer backdrop = background.AddComponent<SpriteRenderer>(); // 배경 표시 요소
            backdrop.sprite = BattleSprites.Load("stage_sky_outskirts"); // 하늘섬 외곽 이미지
            backdrop.sortingOrder = -100; // 전투 요소 뒤에 표시
            Vector3 backgroundSize = backdrop.sprite.bounds.size; // 배경 원본 비율
            float cover = Mathf.Max(camera.orthographicSize * 2f * 16f / 9f / backgroundSize.x, camera.orthographicSize * 2f / backgroundSize.y); // 전투 화면을 채우는 배율
            background.transform.localScale = new Vector3(cover, cover, 1f); // 이미지 비율을 유지한 확대
            GameObject player = new GameObject("ProjectG Player"); // 플레이어 객체 생성
            SpriteRenderer image = player.AddComponent<SpriteRenderer>(); // 플레이어 표시 요소
            image.sprite = BattleSprites.Player(CharacterType.Rapid); // 속사형 시험 이미지
            image.color = Color.white; // 원본 이미지 색상
            player.transform.localScale = new Vector3(1.125f, 1.125f, 1f); // 플레이어 표시 크기 절반
            BattlePlayer controller = player.AddComponent<BattlePlayer>(); // 플레이어 조작 요소
            controller.AssignCamera(camera); // 전투 카메라 연결
            controller.Begin(new Vector2(0f, -3f)); // 플레이어 조작 시작
            GameObject marker = new GameObject("HitboxIndicator"); // 피격 중심 표시 객체
            marker.transform.SetParent(player.transform, false); // 플레이어 중심에 배치
            marker.AddComponent<HitboxIndicator>().Begin(controller); // 상태별 피격 표시 연결
            player.AddComponent<BattleHud>().Begin(controller, camera); // 시험용 체력 표시
            player.AddComponent<SkillHud>().Begin(controller, camera); // 스킬 세 칸 표시
            BattleProgress progress = player.AddComponent<BattleProgress>(); // 경험치와 카드 구성
            progress.Begin(controller, camera); // 경험치와 카드 연결
            EnemyBulletPool pool = camera.gameObject.AddComponent<EnemyBulletPool>(); // 적탄 재사용 풀 생성
            controller.AssignBulletPool(pool); // 필살기 적탄 소거 연결
            UpgradeDropManager drops = camera.gameObject.AddComponent<UpgradeDropManager>(); // 전투 중 아이템 드롭 구성
            drops.Begin(controller, camera, new UpgradeDropEffects(controller, progress)); // 등급 효과와 경험치 회수 연결
            camera.gameObject.AddComponent<StageDirector>().Begin(controller, camera, pool, progress, null); // 고정 선택 없는 스테이지 연결
            return player; // 생성 플레이어 반환
        } // 생성 종료
    } // 클래스 종료
} // 영역 종료
