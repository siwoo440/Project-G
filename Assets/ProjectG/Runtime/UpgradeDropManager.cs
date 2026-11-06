using System; // 콜백과 난수 기능
using System.Collections.Generic; // 드롭 객체 목록
using UnityEngine; // 유니티 전투 표시
using UnityEngine.InputSystem; // 전투 정보 키 입력
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public interface IUpgradeDropEffects // 전투 성장 효과 연결
    { // 계약 시작
        bool CanApply(UpgradeItemId id, UpgradeItemGrade grade); // 해당 등급의 현재 유효성
        bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, out string feedback); // 실제 효과와 변화 안내
    } // 계약 종료
    public sealed class UpgradeDropManager : MonoBehaviour // 전투 중 강화 아이템 드롭
    { // 구성 요소 시작
        private sealed class ActiveDrop // 필드에 남은 드롭
        { // 자료 시작
            public UpgradeDrop Drop; // 아이템 종류와 등급
            public GameObject Visual; // 전투 루트의 표시 객체
            public float Remaining; // 남은 시험 유지 시간
            public RunStatistics Statistics; // 드롭이 생성된 출격 기록
            public DropRecord Record; // 드롭의 종류 등급과 결과
        } // 자료 종료
        public const float TrialPickupRadius = 0.65f; // 미정인 획득 반경 시험값
        public const float TrialLifetime = 20f; // 미정인 유지 시간 시험값
        private readonly List<ActiveDrop> drops = new List<ActiveDrop>(); // 살아 있는 필드 아이템
        private readonly Dictionary<string, Sprite> skillSprites = new Dictionary<string, Sprite>(); // 기존 스킬 이미지 재사용
        private BattlePlayer player; // 출격 플레이어
        private Camera battleCamera; // 전투 카메라
        private IUpgradeDropEffects effects; // 등급 효과 연결
        private UpgradeDropRoller roller; // 유효 후보 추첨기
        private int? replaySeed; // 같은 출격 조건 재현 시드
        private GUIStyle labelStyle; // 아이템 안내 글꼴
        private GUIStyle feedbackStyle; // 획득 안내 글꼴
        private GUIStyle infoStyle; // 전투 정보 글꼴
        private Texture2D infoFrame; // 전투 정보 배경 이미지
        private float feedbackRemaining; // 획득 안내 남은 시간
        public int ActiveCount => drops.Count; // 현재 필드 아이템 수
        public bool IsInfoVisible => player != null && player.isActiveAndEnabled && player.Survival != null && player.Survival.IsAlive && Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.tabKey.isPressed; // 전투 중 탭을 누른 상태
        public string LastFeedback // 최근 획득 안내
        { // 속성 시작
            get; // 안내 조회
            private set; // 안내 변경
        } // 속성 종료
        public event Action<UpgradeDrop, string> Collected; // 실제 획득 알림
        public void Begin(BattlePlayer target, Camera camera, IUpgradeDropEffects effectHandler, System.Random random = null, int? seed = null) // 전투와 효과 연결
        { // 연결 시작
            Disconnect(); // 이전 출격 연결 해제
            player = target; // 현재 플레이어 보관
            battleCamera = camera; // 현재 카메라 보관
            effects = effectHandler; // 실제 효과 처리 보관
            replaySeed = seed; // 출격 재현 시드 보관
            roller = new UpgradeDropRoller(seed.HasValue ? new System.Random(seed.Value) : random); // 시드 또는 주입 난수 준비
            LastFeedback = null; // 이전 획득 안내 제거
            feedbackRemaining = 0f; // 이전 안내 시간 제거
            if (player != null) // 플레이어 확인
            { // 출격 연결 시작
                player.BattleRestarted += ResetForNextRun; // 재출격 정리와 시드 복원
                player.EnemyDefeated += HandleEnemyDefeated; // 적 처치 드롭 연결
            } // 출격 연결 종료
        } // 연결 종료
        private void ResetForNextRun() // 재출격 드롭과 난수 초기화
        { // 초기화 시작
            Clear(); // 이전 출격 드롭 정리
            if (replaySeed.HasValue) // 재현 시드 확인
            { // 복원 시작
                roller = new UpgradeDropRoller(new System.Random(replaySeed.Value)); // 같은 출격 시드 복원
            } // 복원 종료
        } // 초기화 종료
        private void HandleEnemyDefeated(EnemyRank rank, Vector2 position) // 적 처치 드롭 처리
        { // 처리 시작
            TryDrop(rank, position); // 처치 위치 추첨
        } // 처리 종료
        public bool TryDrop(EnemyRank rank, Vector2 position) // 처치 위치 드롭 시도
        { // 드롭 시작
            if (player == null || player.Survival == null || !player.Survival.IsAlive || battleCamera == null || effects == null || roller == null) // 전투와 효과 상태 확인
            { // 차단 시작
                return false; // 유효한 전투 없이 생성 차단
            } // 차단 종료
            if (rank == EnemyRank.Normal) // 일반 적 처치 확인
            { // 추첨 기록 시작
                player.Statistics?.RecordDropAttempt(); // 실제 일반 적 추첨 기록
            } // 추첨 기록 종료
            if (!roller.TryRoll(rank, effects.CanApply, out UpgradeDrop drop)) // 드롭 확률과 유효 후보 확인
            { // 미드롭 시작
                return false; // 보상 없는 처치
            } // 미드롭 종료
            GameObject visual = new GameObject("UpgradeDrop"); // 필드 아이템 객체
            visual.transform.SetParent(transform, false); // 전투 루트에 소속
            visual.transform.position = new Vector3(position.x, position.y, 0f); // 처치 위치에 표시
            visual.transform.localScale = new Vector3(0.8f, 0.8f, 1f); // 시험 아이템 표시 크기
            SpriteRenderer image = visual.AddComponent<SpriteRenderer>(); // 아이템 이미지 구성
            image.sprite = Icon(drop.Id); // 종류별 기존 이미지
            image.color = Color.white; // 이미지 원본 색상
            image.sortingOrder = 4; // 적탄 뒤의 아이템 표시
            GameObject ring = new GameObject("UpgradeDropGrade"); // 등급 테두리 객체
            ring.transform.SetParent(visual.transform, false); // 아이템 중심에 연결
            ring.transform.localScale = new Vector3(1.25f, 1.25f, 1f); // 이미지 바깥 표시
            SpriteRenderer border = ring.AddComponent<SpriteRenderer>(); // 등급 표시 구성
            border.sprite = BattleVisuals.Weak; // 기존 원형 이미지 재사용
            border.color = GradeColor(drop.Grade); // 등급별 시험 색상
            border.sortingOrder = 3; // 아이템 뒤 표시
            ActiveDrop entry = new ActiveDrop(); // 필드 아이템 자료 생성
            entry.Drop = drop; // 종류와 등급 보관
            entry.Visual = visual; // 이미지 객체 보관
            entry.Remaining = TrialLifetime; // 시험 유지 시간 보관
            entry.Statistics = player.Statistics; // 생성 당시 출격 기록 보관
            entry.Record = entry.Statistics?.RecordDropSpawned(drop); // 실제 드롭 종류와 등급 기록
            drops.Add(entry); // 필드 아이템 등록
            return true; // 필드 드롭 성공
        } // 드롭 종료
        private void Update() // 전투 시간 갱신
        { // 갱신 시작
            Advance(Time.deltaTime); // 이동 접촉과 유지 시간 진행
        } // 갱신 종료
        public void Advance(float elapsed) // 필드 드롭 시간과 획득 처리
        { // 처리 시작
            if (Time.timeScale <= 0f || float.IsNaN(elapsed) || float.IsInfinity(elapsed) || elapsed <= 0f) // 일시정지와 유효 시간 확인
            { // 중단 시작
                return; // 정지 중 획득과 시간 보류
            } // 중단 종료
            if (feedbackRemaining > 0f) // 획득 안내 시간 확인
            { // 안내 갱신 시작
                feedbackRemaining = Mathf.Max(0f, feedbackRemaining - elapsed); // 남은 안내 시간 갱신
                if (feedbackRemaining <= 0f) // 안내 종료 확인
                { // 안내 제거 시작
                    LastFeedback = null; // 지난 안내 제거
                } // 안내 제거 종료
            } // 안내 갱신 종료
            if (player == null || player.Survival == null || !player.Survival.IsAlive || effects == null) // 생존과 효과 처리 확인
            { // 중단 시작
                return; // 전투 종료 중 획득 차단
            } // 중단 종료
            for (int index = drops.Count - 1; index >= 0; index--) // 필드 아이템 역순 확인
            { // 순회 시작
                ActiveDrop entry = drops[index]; // 현재 필드 아이템
                if (entry.Visual == null) // 제거된 객체 확인
                { // 연결 정리 시작
                    RemoveAt(index, DropOutcome.Discarded); // 유실 드롭 폐기와 참조 정리
                    continue; // 다음 아이템 확인
                } // 연결 정리 종료
                entry.Remaining -= elapsed; // 시험 유지 시간 차감
                if (entry.Remaining <= 0f) // 시험 유지 시간 종료 확인
                { // 만료 시작
                    RemoveAt(index, DropOutcome.Expired); // 자연 만료 기록과 정리
                    continue; // 다음 아이템 확인
                } // 만료 종료
                Vector2 itemPosition = entry.Visual.transform.position; // 현재 필드 위치
                if (!CollisionMath.Intersects(itemPosition, player.PreviousPosition, player.transform.position, TrialPickupRadius)) // 이동 구간 접촉 확인
                { // 접촉 대기 시작
                    continue; // 다음 아이템 확인
                } // 접촉 대기 종료
                if (!effects.CanApply(entry.Drop.Id, entry.Drop.Grade) || !effects.TryApply(entry.Drop.Id, entry.Drop.Grade, out string feedback)) // 현재 효과 재검증과 적용
                { // 효과 없음 시작
                    continue; // 임시 시험 처리로 드롭 유지
                } // 효과 없음 종료
                UpgradeDrop acquired = entry.Drop; // 성공한 아이템 보관
                RemoveAt(index, DropOutcome.Acquired); // 획득 기록과 아이템 제거
                LastFeedback = string.IsNullOrWhiteSpace(feedback) ? UpgradeItemState.Label(acquired.Id, acquired.Grade) : feedback; // 실제 변화 또는 안전한 효과 안내
                feedbackRemaining = 2.5f; // 획득 안내 시험 시간
                Collected?.Invoke(acquired, LastFeedback); // 실제 획득 결과 전달
                if (Time.timeScale <= 0f) // 자석의 레벨업 정지 확인
                { // 정지 처리 시작
                    break; // 카드 화면 중 추가 획득 차단
                } // 정지 처리 종료
            } // 순회 종료
        } // 처리 종료
        public void Clear() // 출격 종료 또는 초기화 처리
        { // 정리 시작
            for (int index = drops.Count - 1; index >= 0; index--) // 이전 드롭 역순 확인
            { // 정리 반복 시작
                RemoveAt(index, DropOutcome.Discarded); // 이전 드롭 폐기와 제거
            } // 정리 반복 종료
            LastFeedback = null; // 이전 획득 안내 제거
            feedbackRemaining = 0f; // 이전 안내 시간 초기화
        } // 정리 종료
        private void RemoveAt(int index, DropOutcome outcome) // 드롭 결과 기록과 제거
        { // 제거 시작
            ActiveDrop entry = drops[index]; // 생성 당시 드롭 정보
            GameObject visual = entry.Visual; // 기존 표시 객체
            drops.RemoveAt(index); // 같은 프레임 재획득 차단
            entry.Statistics?.RecordDropOutcome(entry.Record, outcome); // 해당 출격에 최초 결과 기록
            if (visual == null) // 표시 객체 존재 확인
            { // 생략 시작
                return; // 제거 생략
            } // 생략 종료
            visual.SetActive(false); // 같은 프레임 표시 중단
            if (Application.isPlaying) // 실제 전투 확인
            { // 지연 제거 시작
                Destroy(visual); // 프레임 종료 후 제거
            } // 지연 제거 종료
            else // 편집 시험 확인
            { // 즉시 제거 시작
                DestroyImmediate(visual); // 시험 객체 즉시 제거
            } // 즉시 제거 종료
        } // 제거 종료
        private void Disconnect() // 기존 출격 연결 정리
        { // 연결 해제 시작
            if (player != null) // 이전 플레이어 확인
            { // 해제 시작
                player.BattleRestarted -= ResetForNextRun; // 재출격 연결 해제
                player.EnemyDefeated -= HandleEnemyDefeated; // 적 처치 연결 해제
            } // 해제 종료
            Clear(); // 이전 드롭 정리
            player = null; // 이전 플레이어 해제
            battleCamera = null; // 이전 카메라 해제
            effects = null; // 이전 효과 해제
            roller = null; // 이전 난수 해제
            replaySeed = null; // 이전 출격 시드 해제
        } // 연결 해제 종료
        private void OnDisable() // 전투 구성 비활성화
        { // 비활성 시작
            Disconnect(); // 드롭과 이벤트 정리
        } // 비활성 종료
        private void OnDestroy() // 전투 구성 제거
        { // 제거 시작
            Disconnect(); // 남은 참조 정리
        } // 제거 종료
        private Sprite Icon(UpgradeItemId id) // 아이템 종류별 기존 이미지
        { // 조회 시작
            switch (id) // 아이템 종류 확인
            { // 분기 시작
                case UpgradeItemId.Damage: return BattleSprites.Load("bullet_player"); // 화력 이미지
                case UpgradeItemId.FireRate: return BattleVisuals.Drone; // 연사 이미지
                case UpgradeItemId.Pierce: return BattleSprites.Load("beam_magic"); // 관통 이미지
                case UpgradeItemId.UniqueCooldown: return BattleVisuals.Weak; // 스킬 대기 이미지
                case UpgradeItemId.Shield: return SkillIcon("shield"); // 방어막 이미지
                case UpgradeItemId.Graze: return BattleVisuals.Crystal; // 그레이즈 이미지
                case UpgradeItemId.Heal: return BattleSprites.Load("item_heal"); // 회복 물약 시험 이미지
                case UpgradeItemId.Energy: return BattleSprites.Load("item_energy"); // 에너지 결정 시험 이미지
                case UpgradeItemId.Magnet: return BattleSprites.Load("item_magnet"); // 회수 자석 시험 이미지
                default: return BattleVisuals.Crystal; // 알 수 없는 종류 대체
            } // 분기 종료
        } // 조회 종료
        private Sprite SkillIcon(string name) // 기존 스킬 이미지를 스프라이트로 준비
        { // 준비 시작
            if (skillSprites.TryGetValue(name, out Sprite cached) && cached != null) // 재사용 이미지 확인
            { // 재사용 시작
                return cached; // 기존 이미지 반환
            } // 재사용 종료
            Texture2D texture = Resources.Load<Texture2D>("SkillIcons/" + name); // 기존 이미지 조회
            if (texture == null) // 이미지 누락 확인
            { // 대체 시작
                return BattleVisuals.Crystal; // 기존 결정 이미지 대체
            } // 대체 종료
            Sprite icon = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), Mathf.Max(texture.width, texture.height)); // 원본 이미지 스프라이트
            skillSprites[name] = icon; // 아이콘 재사용 보관
            return icon; // 준비된 이미지 반환
        } // 준비 종료
        private static Color GradeColor(UpgradeItemGrade grade) // 등급별 시험 테두리 색상
        { // 색상 선택 시작
            switch (grade) // 등급 확인
            { // 분기 시작
                case UpgradeItemGrade.I: return new Color(0.8f, 0.9f, 1f, 0.7f); // 첫 등급 은색
                case UpgradeItemGrade.II: return new Color(0.3f, 1f, 0.6f, 0.7f); // 둘째 등급 녹색
                case UpgradeItemGrade.III: return new Color(0.3f, 0.7f, 1f, 0.7f); // 셋째 등급 파랑
                case UpgradeItemGrade.IV: return new Color(0.8f, 0.4f, 1f, 0.7f); // 넷째 등급 보라
                default: return new Color(1f, 0.8f, 0.3f, 0.7f); // 다섯째 등급 금색
            } // 분기 종료
        } // 색상 선택 종료
        private void OnGUI() // 필드 이름과 획득 결과 표시
        { // 표시 시작
            bool showInfo = IsInfoVisible; // 전투 정보 표시 상태
            if (battleCamera == null || (drops.Count == 0 && string.IsNullOrEmpty(LastFeedback) && !showInfo)) // 표시 대상 확인
            { // 생략 시작
                return; // 안내 표시 생략
            } // 생략 종료
            PrepareStyles(); // 한글 안내 글꼴 준비
            Rect view = battleCamera.pixelRect; // 실제 전투 화면
            labelStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(view.height * 0.018f)); // 아이템 이름 크기
            feedbackStyle.fontSize = Mathf.Max(15, Mathf.RoundToInt(view.height * 0.025f)); // 획득 결과 글자 크기
            foreach (ActiveDrop entry in drops) // 활성 필드 드롭 순회
            { // 표시 반복 시작
                if (entry.Visual == null) // 표시 대상 확인
                { // 생략 시작
                    continue; // 유실된 객체 생략
                } // 생략 종료
                Vector3 screen = battleCamera.WorldToScreenPoint(entry.Visual.transform.position); // 화면 좌표 계산
                string description = UpgradeItemState.Label(entry.Drop.Id, entry.Drop.Grade); // 기획 시험 효과 이름
                string name = description.Split('\n')[0]; // 종류 이름만 표시
                string text = name + " " + entry.Drop.Grade; // 종류와 등급 구분
                GUI.Label(new Rect(screen.x - 90f, Screen.height - screen.y + 18f, 180f, 35f), text, labelStyle); // 아이템 바로 아래 안내
            } // 표시 반복 종료
            if (!string.IsNullOrEmpty(LastFeedback)) // 실제 획득 안내 확인
            { // 획득 안내 시작
                GUI.Label(FeedbackArea(view), LastFeedback, feedbackStyle); // 이름과 실제 변화 안내
            } // 획득 안내 종료
            if (showInfo) // 전투 정보 표시 확인
            { // 정보 표시 시작
                DrawInfo(view); // 아이템 누적 정보 표시
            } // 정보 표시 종료
        } // 표시 종료
        public static Rect InfoArea(Rect view) // 전투 정보의 화면 영역
        { // 계산 시작
            return new Rect(view.x + view.width * 0.02f, Screen.height - view.yMax + view.height * 0.20f, view.width * 0.38f, view.height * 0.45f); // 획득 안내 위쪽 영역
        } // 계산 종료
        public static Rect FeedbackArea(Rect view) // 획득 결과의 화면 영역
        { // 계산 시작
            return new Rect(view.x + view.width * 0.20f, Screen.height - view.yMax + view.height * 0.71f, view.width * 0.60f, view.height * 0.08f); // 전투 정보 아래쪽 영역
        } // 계산 종료
        public string InfoSummary() // 아이템 획득과 누적 효과 요약
        { // 요약 시작
            if (player == null || player.Build == null || player.Survival == null) // 전투 상태 확인
            { // 미준비 시작
                return "전투 정보 없음"; // 유효 상태 없음 안내
            } // 미준비 종료
            UpgradeItemState items = player.Build.Upgrades; // 현재 아이템 성장
            string damage = "화력 " + items.Count(UpgradeItemId.Damage) + "회 +" + Mathf.RoundToInt(items.DamageBonus * 100f) + "%"; // 피해 획득과 증가량
            string fire = "연사 " + items.Count(UpgradeItemId.FireRate) + "회 -" + Mathf.RoundToInt(items.FireRateBonus * 100f) + "%"; // 연사 획득과 감소량
            string pierce = "관통 " + items.Count(UpgradeItemId.Pierce) + "회 +" + items.PierceBonus; // 관통 획득과 증가량
            string cooldown = "고유기 대기 " + items.Count(UpgradeItemId.UniqueCooldown) + "회 -" + Mathf.RoundToInt(items.UniqueCooldownBonus * 100f) + "%"; // 고유기 대기 감소
            string graze = "근접 충전 " + items.Count(UpgradeItemId.Graze) + "회 +" + Mathf.RoundToInt(items.GrazeBonus * 100f) + "%"; // 근접 충전 증가
            string shield = "강화 방어막 " + player.Survival.UpgradeShieldCharges + "회"; // 남은 방어 횟수
            string consumables = "회복 " + items.Count(UpgradeItemId.Heal) + "회 · 충전 " + items.Count(UpgradeItemId.Energy) + "회 · 자석 " + items.Count(UpgradeItemId.Magnet) + "회"; // 즉시 아이템 획득 수
            return "아이템 누적 효과\n" + damage + "\n" + fire + "\n" + pierce + "\n" + cooldown + "\n" + graze + "\n" + shield + "\n" + consumables; // 실제 출격 정보 표시
        } // 요약 종료
        private void DrawInfo(Rect view) // 전투 정보 화면 표시
        { // 표시 시작
            Rect area = InfoArea(view); // 전투 정보 영역
            if (infoFrame != null) // 기존 배경 이미지 확인
            { // 이미지 표시 시작
                GUI.DrawTexture(area, infoFrame, ScaleMode.StretchToFill); // 전투 정보 프레임
            } // 이미지 표시 종료
            else // 배경 이미지 누락
            { // 대체 시작
                GUI.Box(area, GUIContent.none); // 기본 프레임 표시
            } // 대체 종료
            infoStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(view.height * 0.021f)); // 화면 비례 글자 크기
            GUI.Label(new Rect(area.x + area.width * 0.08f, area.y + area.height * 0.08f, area.width * 0.84f, area.height * 0.84f), InfoSummary(), infoStyle); // 획득 횟수와 효과 표시
        } // 표시 종료
        private void PrepareStyles() // 한글 글꼴 준비
        { // 준비 시작
            if (labelStyle != null) // 기존 글꼴 확인
            { // 재사용 시작
                return; // 글꼴 생성 생략
            } // 재사용 종료
            string[] names = new string[] // 한글 글꼴 후보
            { // 글꼴 목록 시작
                "Malgun Gothic", // 윈도우 한글 글꼴
                "Arial" // 대체 글꼴
            }; // 글꼴 목록 종료
            Font font = Font.CreateDynamicFontFromOSFont(names, 16); // 사용 가능한 한글 글꼴
            labelStyle = new GUIStyle(GUI.skin.label); // 아이템 안내 글꼴
            labelStyle.font = font != null ? font : GUI.skin.font; // 글꼴 적용
            labelStyle.alignment = TextAnchor.MiddleCenter; // 안내 중앙 정렬
            labelStyle.fontStyle = FontStyle.Bold; // 안내 글자 강조
            labelStyle.normal.textColor = Color.white; // 이름 흰색
            feedbackStyle = new GUIStyle(labelStyle); // 획득 안내 글꼴
            feedbackStyle.wordWrap = true; // 긴 획득 결과 줄바꿈
            infoStyle = new GUIStyle(labelStyle); // 전투 정보 글꼴 생성
            infoStyle.alignment = TextAnchor.UpperLeft; // 정보 왼쪽 정렬
            infoStyle.wordWrap = true; // 좁은 화면 줄바꿈
            infoFrame = Resources.Load<Texture2D>("BattleArt/ui_card_frame"); // 기존 정보 프레임 이미지
        } // 준비 종료
    } // 구성 요소 종료
} // 영역 종료
