using UnityEngine; // 유니티 장면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class EnemyDemoEncounter : MonoBehaviour // 적과 보스 순차 시험
    { // 클래스 시작
        private static readonly EnemyKind[] sequence = new EnemyKind[] // 시험 순서
        { // 순서 시작
            EnemyKind.Aimed, // 조준형
            EnemyKind.Spread, // 확산형
            EnemyKind.Dash, // 돌진형
            EnemyKind.MidBoss, // 중간 보스
            EnemyKind.FinalBoss // 최종 보스
        }; // 순서 종료
        private EnemyBulletPool pool; // 적탄 풀
        private BattlePlayer player; // 플레이어 대상
        private Camera battleCamera; // 전투 카메라
        private DemoBulletSpawner demo; // 기존 시험 발사기
        private TrainingTarget training; // 기존 시험 표적
        private float timer = 2f; // 첫 적 등장 대기
        private int next; // 다음 적 번호
        private bool started; // 적 시험 시작 여부
        public EnemyActor Current // 현재 시험 적
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public void Begin(EnemyBulletPool bulletPool, BattlePlayer target, Camera camera, DemoBulletSpawner previous, TrainingTarget dummy) // 시험 초기화
        { // 초기화 시작
            pool = bulletPool; // 적탄 풀 연결
            player = target; // 플레이어 연결
            battleCamera = camera; // 전투 카메라 연결
            demo = previous; // 이전 발사기 연결
            training = dummy; // 이전 표적 연결
        } // 초기화 종료
        private void Update() // 시험 진행
        { // 진행 시작
            if (Time.timeScale > 0f) // 전투 시간 확인
            { // 진행 시작
                Advance(Time.deltaTime); // 경과 시간 적용
            } // 진행 종료
        } // 진행 종료
        public void Advance(float elapsed) // 등장 진행
        { // 진행 시작
            if (elapsed <= 0f || player == null || player.Survival == null || !player.Survival.IsAlive || battleCamera == null || pool == null) // 진행 조건 확인
            { // 중단 시작
                return; // 진행 생략
            } // 중단 종료
            if (Current != null && Current.gameObject.activeSelf && Current.IsAlive) // 생존 적 확인
            { // 대기 시작
                return; // 처치 또는 이탈 대기
            } // 대기 종료
            timer -= elapsed; // 등장 대기 감소
            if (timer > 0f) // 대기 시간 확인
            { // 대기 시작
                return; // 다음 프레임 대기
            } // 대기 종료
            if (!started) // 첫 적 등장 확인
            { // 시험 전환 시작
                started = true; // 적 시험 시작
                if (training != null) // 임시 표적 확인
                { // 표적 정리 시작
                    training.gameObject.SetActive(false); // 이전 표적 숨김
                } // 표적 정리 종료
                if (demo != null) // 기존 발사기 확인
                { // 발사기 정리 시작
                    demo.StopSpawning(); // 기존 시험탄 중단
                } // 발사기 정리 종료
            } // 시험 전환 종료
            if (Current != null) // 이전 적 확인
            { // 이전 적 정리 시작
                Destroy(Current.gameObject); // 이전 적 제거
            } // 이전 적 정리 종료
            float halfWidth = battleCamera.orthographicSize * 16f / 9f; // 화면 가로 반경
            Vector2 center = battleCamera.transform.position; // 화면 중심
            EnemyKind kind = sequence[next % sequence.Length]; // 등장 적 종류
            bool boss = kind == EnemyKind.MidBoss || kind == EnemyKind.FinalBoss; // 보스 구분
            float padding = boss ? 1.75f : 1.3f; // 확대된 표시를 위한 상단 여백
            Vector2 position = new Vector2(center.x, center.y + battleCamera.orthographicSize - padding); // 기본 등장 위치
            if (((Vector2)player.transform.position - position).sqrMagnitude < 2.25f) // 플레이어 근접 확인
            { // 안전 위치 시작
                position.x = center.x + (player.transform.position.x >= center.x ? -1f : 1f) * halfWidth * 0.5f; // 먼 쪽 등장 위치
            } // 안전 위치 종료
            GameObject enemyObject = new GameObject("EnemyDemo"); // 시험 적 객체
            enemyObject.transform.SetParent(transform, false); // 시험 장면에 연결
            enemyObject.transform.position = position; // 안전한 등장 위치
            Current = enemyObject.AddComponent<EnemyActor>(); // 적 구성 요소
            Current.Begin(kind, player, pool, battleCamera); // 종류별 시험 시작
            next++; // 다음 시험 적 선택
            timer = 1.5f; // 다음 등장 대기
        } // 진행 종료
        private void OnGUI() // 시험 적 체력 표시
        { // 표시 시작
            if (Current == null || !Current.IsAlive || !Current.gameObject.activeSelf || battleCamera == null) // 표시 조건 확인
            { // 생략 시작
                return; // 표시 생략
            } // 생략 종료
            Rect view = battleCamera.pixelRect; // 전투 화면
            GUIStyle text = new GUIStyle(GUI.skin.label); // 글자 설정
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬
            text.fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height / 45f)); // 화면 비례 크기
            text.normal.textColor = Color.white; // 글자 색상
            float top = Screen.height - view.yMax + 10f; // 위쪽 여백
            GUI.Label(new Rect(view.center.x - 180f, top, 360f, 30f), "ENEMY HP " + Mathf.CeilToInt(Current.Health) + " / " + Mathf.CeilToInt(Current.MaximumHealth), text); // 시험 체력 표시
        } // 표시 종료
    } // 클래스 종료
} // 영역 종료
