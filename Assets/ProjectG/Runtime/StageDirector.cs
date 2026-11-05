using System.Collections.Generic; // 활성 적 목록
using UnityEngine; // 유니티 전투 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class StageDirector : MonoBehaviour // 시험 스테이지 제어
    { // 클래스 시작
        private readonly List<EnemyActor> enemies = new List<EnemyActor>(); // 일반 적 목록
        private BattlePlayer player; // 플레이어 참조
        private Camera battleCamera; // 전투 카메라
        private EnemyBulletPool pool; // 적탄 재사용 풀
        private BattleProgress progress; // 카드 선택 상태
        private UpgradeItemSelection selection; // 강화 아이템 선택
        private EnemyActor boss; // 현재 보스
        private float spawnTimer; // 일반 적 등장 대기
        private float bossDelay; // 보스 등장 안전 시간
        private float phaseNoticeSeconds; // 보스 단계 알림 시간
        private int bossPhaseNotice; // 표시할 보스 단계
        private int spawnIndex; // 시험 웨이브 번호
        private bool choicePending; // 강화 선택 시작 대기
        private bool ownsProgressBlock; // 카드 표시 지연 소유
        private bool ownsTransitionSafety; // 보스 전환 안전 상태 소유
        private GUIStyle textStyle; // 화면 안내 글꼴
        private GUIStyle contextStyle; // 사건 안내 한글 글꼴
        public StageProgression State // 스테이지 진행 상태
        { // 속성 시작
            get; // 상태 조회
            private set; // 상태 설정
        } // 속성 종료
        public EnemyActor Boss => boss; // 현재 보스 조회
        public string ContextMessage // 첫 사건과 조사 안내
        { // 속성 시작
            get // 상황 안내 조회
            { // 조회 시작
                if (State == null) // 진행 상태 확인
                { // 생략 시작
                    return ""; // 초기화 전 안내 생략
                } // 생략 종료
                if (State.Phase == StagePhase.Preparation && State.PhaseSeconds < 8f) // 첫 사건 확인
                { // 사건 시작
                    return "하늘섬 근처 균열 조사 중 마물 습격"; // 첫 사건 시험 안내
                } // 사건 종료
                return State.Phase == StagePhase.Result ? "균열 주변에서 낯선 마법 흔적 발견" : ""; // 클리어 후 조사 안내
            } // 조회 종료
        } // 속성 종료
        public int ActiveEnemyCount // 생존 일반 적 수
        { // 속성 시작
            get // 생존 적 조회
            { // 조회 시작
                int count = 0; // 생존 적 집계
                foreach (EnemyActor enemy in enemies) // 적 목록 순회
                { // 순회 시작
                    if (enemy != null && enemy.gameObject.activeSelf && enemy.IsAlive) // 활성 적 확인
                    { // 집계 시작
                        count++; // 생존 적 증가
                    } // 집계 종료
                } // 순회 종료
                return count; // 생존 적 반환
            } // 조회 종료
        } // 속성 종료
        public void Begin(BattlePlayer target, Camera camera, EnemyBulletPool bulletPool, BattleProgress battleProgress, UpgradeItemSelection itemSelection) // 시험 출격 초기화
        { // 초기화 시작
            Disconnect(); // 이전 연결 정리
            player = target; // 플레이어 연결
            battleCamera = camera; // 전투 카메라 연결
            pool = bulletPool; // 적탄 풀 연결
            progress = battleProgress; // 성장 상태 연결
            selection = itemSelection; // 강화 선택 연결
            if (player != null) // 플레이어 확인
            { // 연결 시작
                player.BattleRestarted += ResetStage; // 새 출격 연결
            } // 연결 종료
            if (selection != null) // 강화 선택 확인
            { // 연결 시작
                selection.ChoiceCompleted += CompleteChoice; // 선택 완료 연결
            } // 연결 종료
            ResetStage(); // 스테이지 시작
        } // 초기화 종료
        private void Update() // 매 프레임 스테이지 진행
        { // 진행 시작
            float elapsed = Time.timeScale > 0f ? Time.deltaTime : 0f; // 일시정지 반영 시간
            Advance(elapsed, Time.unscaledDeltaTime); // 전투와 실제 시간 진행
        } // 진행 종료
        public void Advance(float combatDelta, float totalDelta) // 시험 스테이지 진행
        { // 진행 시작
            if (State == null || player == null || player.Survival == null || battleCamera == null || pool == null) // 필수 연결 확인
            { // 생략 시작
                return; // 초기화 전 진행 생략
            } // 생략 종료
            if (!player.Survival.IsAlive) // 플레이어 생존 확인
            { // 실패 시작
                State.Fail(); // 출격 실패
                return; // 전투 진행 종료
            } // 실패 종료
            State.Advance(combatDelta, totalDelta); // 단계와 시간 진행
            phaseNoticeSeconds = Mathf.Max(0f, phaseNoticeSeconds - Mathf.Max(0f, combatDelta)); // 단계 알림 시간 감소
            if ((State.Phase == StagePhase.UpgradeOne || State.Phase == StagePhase.UpgradeTwo) && selection != null && !selection.IsOpen) // 선택 화면 종료 여부 확인
            { // 재시도 준비 시작
                choicePending = true; // 유효한 선택까지 재시도
            } // 재시도 준비 종료
            if (choicePending && selection != null && !selection.IsOpen) // 강화 선택 대기 확인
            { // 선택 시작
                choicePending = !selection.Begin(player, battleCamera, progress, pool); // 일시정지 해제까지 재시도
            } // 선택 종료
            if (combatDelta <= 0f) // 전투 정지 확인
            { // 정지 시작
                return; // 적과 보스 진행 생략
            } // 정지 종료
            if (State.Phase == StagePhase.Preparation || State.Phase == StagePhase.CombatOne || State.Phase == StagePhase.CombatTwo) // 일반 전투 구간 확인
            { // 웨이브 시작
                AdvanceWaves(combatDelta); // 일반 적 등장 처리
            } // 웨이브 종료
            if (State.Phase == StagePhase.MidBoss || State.Phase == StagePhase.FinalBoss) // 보스 구간 확인
            { // 보스 처리 시작
                AdvanceBoss(combatDelta); // 보스 등장과 처치 확인
            } // 보스 처리 종료
        } // 진행 종료
        private void AdvanceWaves(float elapsed) // 일반 적 웨이브 처리
        { // 처리 시작
            spawnTimer -= elapsed; // 등장 대기 감소
            int safety = 0; // 한 프레임 생성 상한
            while (spawnTimer <= 0.000001f && safety++ < 4) // 등장 시점 확인
            { // 생성 반복 시작
                spawnTimer += State.Phase == StagePhase.Preparation ? 6f : State.Phase == StagePhase.CombatOne ? 4f : 3f; // 시험 등장 간격
                if (ActiveEnemyCount >= 6) // 화면 내 적 상한 확인
                { // 상한 시작
                    continue; // 추가 생성 생략
                } // 상한 종료
                EnemyKind kind = State.Phase == StagePhase.Preparation ? EnemyKind.Aimed : State.Phase == StagePhase.CombatOne ? (spawnIndex % 2 == 0 ? EnemyKind.Aimed : EnemyKind.Spread) : (EnemyKind)(spawnIndex % 3); // 구간별 적 종류
                EnemyActor enemy = Spawn(kind, false); // 일반 적 등장
                if (enemy != null) // 생성 성공 확인
                { // 등록 시작
                    enemies.Add(enemy); // 활성 적 등록
                    spawnIndex++; // 다음 웨이브 번호
                } // 등록 종료
            } // 생성 반복 종료
            for (int index = enemies.Count - 1; index >= 0; index--) // 이전 적 역순 확인
            { // 정리 순회 시작
                EnemyActor enemy = enemies[index]; // 현재 적 조회
                if (enemy == null || !enemy.gameObject.activeSelf) // 처치 또는 이탈 확인
                { // 정리 시작
                    if (enemy != null) // 객체 존재 확인
                    { // 객체 정리 시작
                        RemoveObject(enemy.gameObject); // 비활성 적 제거
                    } // 객체 정리 종료
                    enemies.RemoveAt(index); // 적 목록에서 제거
                } // 정리 종료
            } // 정리 순회 종료
        } // 처리 종료
        private void AdvanceBoss(float elapsed) // 보스 등장과 처치 처리
        { // 처리 시작
            if (boss == null) // 보스 등장 확인
            { // 등장 대기 시작
                bossDelay -= elapsed; // 안전 시간 감소
                if (bossDelay <= 0f) // 안전 시간 종료 확인
                { // 등장 시작
                    boss = Spawn(State.Phase == StagePhase.MidBoss ? EnemyKind.MidBoss : EnemyKind.FinalBoss, true); // 보스 생성
                    if (boss != null) // 생성 성공 확인
                    { // 완료 시작
                        boss.PhaseChanged += AnnounceBossPhase; // 보스 단계 알림 연결
                        ReleaseProgressBlock(); // 대기 카드 선택 허용
                        ReleaseTransitionSafety(); // 보스 등장 안전 해제
                    } // 완료 종료
                } // 등장 종료
                return; // 보스 등장 완료 대기
            } // 등장 대기 종료
            if (!boss.IsAlive) // 보스 처치 확인
            { // 처치 시작
                RemoveObject(boss.gameObject); // 처치 보스 정리
                boss = null; // 보스 참조 해제
                State.DefeatBoss(); // 다음 구간 알림
            } // 처치 종료
        } // 처리 종료
        private void AnnounceBossPhase(int phase) // 보스 단계 전환 표시
        { // 알림 시작
            bossPhaseNotice = phase + 1; // 표시용 단계 번호
            phaseNoticeSeconds = 2f; // 시험용 알림 시간
        } // 알림 종료
        private EnemyActor Spawn(EnemyKind kind, bool isBoss) // 안전한 적 등장
        { // 등장 시작
            float halfWidth = battleCamera.orthographicSize * 16f / 9f; // 전투 화면 가로 반경
            Vector2 center = battleCamera.transform.position; // 전투 화면 중심
            float y = center.y + battleCamera.orthographicSize - (isBoss ? 1.75f : 1.3f); // 종류별 등장 높이
            float x = isBoss ? center.x : center.x + ((spawnIndex % 3) - 1) * halfWidth * 0.55f; // 시험 등장 가로 위치
            Vector2 position = new Vector2(x, y); // 첫 등장 위치
            if (((Vector2)player.transform.position - position).sqrMagnitude < 2.25f) // 플레이어 근접 확인
            { // 안전 위치 시작
                position.x = center.x + (player.transform.position.x >= center.x ? -1f : 1f) * halfWidth * 0.55f; // 반대쪽 등장 위치
            } // 안전 위치 종료
            if (((Vector2)player.transform.position - position).sqrMagnitude < 2.25f) // 안전 거리 재확인
            { // 생략 시작
                return null; // 가까운 생성 차단
            } // 생략 종료
            GameObject holder = new GameObject(isBoss ? "StageBoss" : "StageEnemy"); // 적 객체 생성
            holder.transform.SetParent(transform, false); // 스테이지 소유로 연결
            holder.transform.position = position; // 등장 위치 설정
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성 요소
            enemy.Begin(kind, player, pool, battleCamera); // 종류별 상태 초기화
            return enemy; // 생성한 적 반환
        } // 등장 종료
        private void OnPhaseChanged(StagePhase phase) // 구간 전환 처리
        { // 전환 시작
            spawnTimer = 0f; // 새 구간 등장 시간 초기화
            if (phase == StagePhase.UpgradeOne || phase == StagePhase.UpgradeTwo) // 강화 선택 확인
            { // 선택 준비 시작
                ClearEnemies(); // 기존 일반 적 정리
                ClearBullets(); // 화면 적탄 정리
                choicePending = true; // 안전한 선택 시작 대기
            } // 선택 준비 종료
            else if (phase == StagePhase.MidBoss || phase == StagePhase.FinalBoss) // 보스 구간 확인
            { // 보스 준비 시작
                ClearEnemies(); // 이전 적 정리
                ClearBullets(); // 화면 적탄 정리
                bossDelay = 2f; // 시험용 등장 안전 시간
                if (player != null && player.Survival != null) // 플레이어 상태 확인
                { // 안전 처리 시작
                    player.Survival.SetUpgradeSafe(true); // 보스 전환 중 피격 방지
                    ownsTransitionSafety = true; // 안전 상태 소유
                } // 안전 처리 종료
                if (progress != null && !progress.Blocked) // 카드 표시 상태 확인
                { // 카드 지연 시작
                    progress.Blocked = true; // 보스 등장 중 카드 지연
                    ownsProgressBlock = true; // 지연 상태 소유
                } // 카드 지연 종료
            } // 보스 준비 종료
            else if (phase == StagePhase.Result || phase == StagePhase.Failed) // 출격 종료 확인
            { // 종료 처리 시작
                choicePending = false; // 대기 선택 취소
                if (selection != null) // 선택 객체 확인
                { // 선택 정리 시작
                    selection.Cancel(); // 강화 선택 닫기
                } // 선택 정리 종료
                ClearEnemies(); // 남은 적 정리
                ClearBullets(); // 남은 적탄 정리
                ReleaseProgressBlock(); // 카드 표시 지연 해제
                ReleaseTransitionSafety(); // 보스 전환 안전 해제
            } // 종료 처리 종료
        } // 전환 종료
        private void CompleteChoice() // 강화 선택 완료 알림
        { // 처리 시작
            choicePending = false; // 선택 대기 해제
            if (State != null) // 진행 상태 확인
            { // 단계 진행 시작
                State.CompleteUpgrade(); // 다음 보스 구간 이동
            } // 단계 진행 종료
        } // 처리 종료
        private void ResetStage() // 새 출격 진행 초기화
        { // 초기화 시작
            if (selection != null) // 선택 화면 확인
            { // 선택 정리 시작
                selection.Cancel(); // 이전 선택 닫기
            } // 선택 정리 종료
            ReleaseProgressBlock(); // 이전 카드 지연 해제
            ReleaseTransitionSafety(); // 이전 전환 안전 해제
            ClearEnemies(); // 이전 적 정리
            ClearBullets(); // 이전 적탄 정리
            if (State != null) // 이전 상태 확인
            { // 연결 정리 시작
                State.PhaseChanged -= OnPhaseChanged; // 이전 알림 해제
            } // 연결 정리 종료
            State = new StageProgression(); // 새 진행 상태 생성
            State.PhaseChanged += OnPhaseChanged; // 단계 변경 연결
            spawnTimer = 2f; // 첫 일반 적 등장 대기
            spawnIndex = 0; // 웨이브 번호 초기화
            bossDelay = 0f; // 보스 대기 초기화
            phaseNoticeSeconds = 0f; // 단계 알림 초기화
            choicePending = false; // 선택 대기 초기화
        } // 초기화 종료
        private void ReleaseProgressBlock() // 보스 전환 카드 지연 해제
        { // 해제 시작
            if (ownsProgressBlock && progress != null) // 지연 소유 확인
            { // 해제 처리 시작
                progress.Blocked = false; // 카드 선택 허용
            } // 해제 처리 종료
            ownsProgressBlock = false; // 소유 상태 초기화
        } // 해제 종료
        private void ReleaseTransitionSafety() // 보스 전환 안전 해제
        { // 해제 시작
            if (ownsTransitionSafety && player != null && player.Survival != null) // 안전 소유 확인
            { // 해제 처리 시작
                player.Survival.SetUpgradeSafe(false); // 전환 무적 해제
            } // 해제 처리 종료
            ownsTransitionSafety = false; // 소유 상태 초기화
        } // 해제 종료
        private void ClearEnemies() // 스테이지 적 정리
        { // 정리 시작
            foreach (EnemyActor enemy in enemies) // 일반 적 순회
            { // 순회 시작
                if (enemy != null) // 적 존재 확인
                { // 제거 시작
                    enemy.gameObject.SetActive(false); // 같은 프레임 공격 차단
                    RemoveObject(enemy.gameObject); // 적 객체 제거
                } // 제거 종료
            } // 순회 종료
            enemies.Clear(); // 일반 적 목록 초기화
            if (boss != null) // 보스 존재 확인
            { // 보스 정리 시작
                boss.gameObject.SetActive(false); // 같은 프레임 공격 차단
                RemoveObject(boss.gameObject); // 보스 객체 제거
                boss = null; // 보스 참조 초기화
            } // 보스 정리 종료
        } // 정리 종료
        private void ClearBullets() // 전투 화면 적탄 정리
        { // 정리 시작
            if (pool == null || battleCamera == null) // 필수 연결 확인
            { // 생략 시작
                return; // 정리 생략
            } // 생략 종료
            float height = battleCamera.orthographicSize; // 화면 세로 반경
            float width = height * 16f / 9f; // 화면 가로 반경
            Vector3 center = battleCamera.transform.position; // 화면 중심
            pool.ClearVisible(new Rect(center.x - width, center.y - height, width * 2f, height * 2f)); // 화면 안 소거 가능 적탄 회수
        } // 정리 종료
        private static void RemoveObject(GameObject item) // 시험 객체 제거
        { // 제거 시작
            if (item == null) // 객체 존재 확인
            { // 생략 시작
                return; // 제거 생략
            } // 생략 종료
            if (Application.isPlaying) // 플레이 상태 확인
            { // 지연 제거 시작
                Destroy(item); // 프레임 종료 후 제거
            } // 지연 제거 종료
            else // 편집 상태
            { // 즉시 제거 시작
                DestroyImmediate(item); // 시험 객체 즉시 제거
            } // 즉시 제거 종료
        } // 제거 종료
        private void Disconnect() // 기존 스테이지 연결 해제
        { // 해제 시작
            if (player != null) // 플레이어 존재 확인
            { // 해제 처리 시작
                player.BattleRestarted -= ResetStage; // 새 출격 연결 해제
            } // 해제 처리 종료
            if (selection != null) // 강화 선택 존재 확인
            { // 해제 처리 시작
                selection.ChoiceCompleted -= CompleteChoice; // 선택 완료 연결 해제
                selection.Cancel(); // 선택 상태 정리
            } // 해제 처리 종료
            if (State != null) // 단계 상태 확인
            { // 해제 처리 시작
                State.PhaseChanged -= OnPhaseChanged; // 단계 변경 연결 해제
            } // 해제 처리 종료
            ReleaseProgressBlock(); // 카드 지연 해제
            ReleaseTransitionSafety(); // 보스 전환 안전 해제
            ClearEnemies(); // 적 객체 정리
        } // 해제 종료
        public void Shutdown() // 스테이지 종료 처리
        { // 정리 시작
            Disconnect(); // 연결과 적 정리
        } // 정리 종료
        private void OnDestroy() // 스테이지 제거 처리
        { // 정리 시작
            Shutdown(); // 연결과 안전 상태 정리
        } // 정리 종료
        private void OnGUI() // 시험 스테이지 정보 표시
        { // 표시 시작
            if (State == null || battleCamera == null) // 표시 조건 확인
            { // 생략 시작
                return; // 초기화 전 표시 생략
            } // 생략 종료
            if (textStyle == null) // 글꼴 준비 확인
            { // 준비 시작
                textStyle = new GUIStyle(GUI.skin.label); // 안내 글꼴 생성
                textStyle.alignment = TextAnchor.MiddleCenter; // 중앙 정렬
                textStyle.normal.textColor = Color.white; // 흰색 글자
            } // 준비 종료
            Rect view = battleCamera.pixelRect; // 전투 화면 좌표
            textStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height / 50f)); // 화면 비례 글자 크기
            float top = Screen.height - view.yMax + 10f; // 전투 화면 상단
            string label = State.Phase == StagePhase.Result ? "STAGE CLEAR" : State.Phase == StagePhase.Failed ? "STAGE FAILED" : State.Phase.ToString().ToUpperInvariant(); // 시험 구간 이름
            string health = boss != null && boss.IsAlive ? "   BOSS " + Mathf.CeilToInt(boss.Health) + " / " + Mathf.CeilToInt(boss.MaximumHealth) : ""; // 보스 체력 표시
            string time = "   COMBAT " + Mathf.FloorToInt(State.CombatSeconds) + "s   TOTAL " + Mathf.FloorToInt(State.TotalSeconds) + "s"; // 전투와 전체 시간 구분
            GUI.Label(new Rect(view.center.x - 350f, top, 700f, 32f), label + time + health, textStyle); // 구간과 시간 및 체력 표시
            if (phaseNoticeSeconds > 0f) // 보스 단계 알림 확인
            { // 알림 표시 시작
                GUI.Label(new Rect(view.center.x - 200f, top + 34f, 400f, 32f), "BOSS PHASE " + bossPhaseNotice, textStyle); // 보스 단계 전환 안내
            } // 알림 표시 종료
            if (!string.IsNullOrEmpty(ContextMessage)) // 사건 안내 확인
            { // 사건 표시 시작
                if (contextStyle == null) // 한글 글꼴 확인
                { // 글꼴 준비 시작
                    string[] fontNames = new string[] // 시스템 글꼴 후보
                    { // 목록 시작
                        "Malgun Gothic", // 윈도우 한글 글꼴
                        "Arial" // 기본 대체 글꼴
                    }; // 목록 종료
                    Font font = Font.CreateDynamicFontFromOSFont(fontNames, 18); // 한글 글꼴 준비
                    contextStyle = new GUIStyle(textStyle); // 사건 안내 스타일
                    contextStyle.font = font != null ? font : GUI.skin.font; // 사용 가능한 글꼴
                } // 글꼴 준비 종료
                contextStyle.fontSize = textStyle.fontSize; // 화면 비례 글자 크기
                GUI.Label(new Rect(view.center.x - 350f, top + 70f, 700f, 32f), ContextMessage, contextStyle); // 첫 사건과 조사 안내 표시
            } // 사건 표시 종료
        } // 표시 종료
    } // 클래스 종료
} // 영역 종료
