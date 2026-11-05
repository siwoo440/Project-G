using System; // 선택 완료 알림
using System.Collections.Generic; // 아이템 후보 목록
using UnityEngine; // 유니티 화면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class UpgradeItemSelection : MonoBehaviour // 필드 강화 아이템 선택
    { // 구성 요소 시작
        private readonly System.Random random = new System.Random(); // 시험용 후보 난수
        private readonly List<UpgradeItemId> choices = new List<UpgradeItemId>(); // 현재 후보 목록
        private readonly List<GameObject> visuals = new List<GameObject>(); // 아이템 표시 목록
        private BattlePlayer player; // 플레이어 참조
        private Camera battleCamera; // 전투 카메라
        private BattleProgress progress; // 카드 선택 상태
        private bool oldBlocked; // 이전 카드 선택 제한
        private float armTime; // 획득 전 안전 시간
        private UpgradeItemId? lastChoice; // 직전 선택 종류
        private Font koreanFont; // 한글 글꼴
        private GUIStyle labelStyle; // 아이템 설명 글꼴
        private static Sprite shieldSprite; // 보호막 이미지 재사용
        public bool IsOpen // 아이템 선택 진행 여부
        { // 속성 시작
            get; // 상태 조회
            private set; // 상태 변경
        } // 속성 종료
        public int ChoiceCount => choices.Count; // 현재 선택지 수
        public event Action ChoiceCompleted; // 선택 완료 알림
        public UpgradeItemId GetChoice(int index) // 선택지 종류 조회
        { // 조회 시작
            return choices[index]; // 지정 아이템 반환
        } // 조회 종료
        public Vector2 GetPosition(int index) // 선택지 위치 조회
        { // 조회 시작
            return visuals[index].transform.position; // 지정 위치 반환
        } // 조회 종료
        public bool Begin(BattlePlayer target, Camera camera, BattleProgress cardProgress, EnemyBulletPool pool) // 아이템 선택 시작
        { // 시작 처리
            if (IsOpen || target == null || camera == null || target.Build == null || target.Survival == null || !target.Survival.IsAlive || Time.timeScale <= 0f) // 전투 상태 확인
            { // 차단 시작
                return false; // 시작 불가
            } // 차단 종료
            if (cardProgress != null && (cardProgress.Offer != null || cardProgress.Blocked || (cardProgress.Level != null && cardProgress.Level.PendingChoices > 0))) // 카드 또는 다른 전환 확인
            { // 차단 시작
                return false; // 기존 선택 종료 대기
            } // 차단 종료
            List<UpgradeItemId> candidates = new List<UpgradeItemId>(); // 유효 후보 목록
            foreach (UpgradeItemId id in Enum.GetValues(typeof(UpgradeItemId))) // 초기 여섯 종류 순회
            { // 후보 검사 시작
                if (target.Build.Upgrades.CanApply(id, target)) // 실제 효과 확인
                { // 후보 추가 시작
                    candidates.Add(id); // 유효 후보 보관
                } // 후보 추가 종료
            } // 후보 검사 종료
            if (candidates.Count == 0) // 유효 후보 확인
            { // 차단 시작
                return false; // 선택 불가
            } // 차단 종료
            choices.Clear(); // 이전 선택지 제거
            List<UpgradeItemId> firstPool = candidates.FindAll(id => !lastChoice.HasValue || id != lastChoice.Value); // 직전 보상 외 후보
            if (firstPool.Count == 0) // 다른 종류 확인
            { // 대체 시작
                firstPool = candidates; // 남은 유효 후보 사용
            } // 대체 종료
            UpgradeItemId first = firstPool[random.Next(firstPool.Count)]; // 첫 종류 선택
            choices.Add(first); // 첫 후보 보관
            candidates.Remove(first); // 중복 후보 제거
            if (candidates.Count > 0) // 두 번째 유효 후보 확인
            { // 둘째 선택 시작
                choices.Add(candidates[random.Next(candidates.Count)]); // 다른 두 번째 후보
            } // 둘째 선택 종료
            player = target; // 플레이어 보관
            battleCamera = camera; // 카메라 보관
            progress = cardProgress; // 카드 화면 보관
            oldBlocked = progress != null && progress.Blocked; // 기존 제한 보관
            if (progress != null) // 카드 선택 상태 확인
            { // 카드 제한 시작
                progress.Blocked = true; // 아이템 선택 중 카드 대기
            } // 카드 제한 종료
            player.Survival.SetUpgradeSafe(true); // 아이템 선택 중 피격 방지
            player.BattleRestarted += Cancel; // 재출격 시 안전 정리
            if (pool != null) // 적탄 풀 확인
            { // 적탄 정리 시작
                float halfHeight = camera.orthographicSize; // 화면 세로 반경
                float halfWidth = halfHeight * camera.aspect; // 화면 가로 반경
                Vector2 center = camera.transform.position; // 화면 중심 위치
                pool.ClearVisible(new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f)); // 화면 내 소거 가능 적탄 정리
            } // 적탄 정리 종료
            armTime = 0.5f; // 선택 전 시험 안전 시간
            IsOpen = true; // 선택 시작 상태
            CreateVisuals(); // 선택지 이미지 생성
            return true; // 시작 성공
        } // 시작 종료
        public bool TryChoose(int index) // 아이템 한 개 선택
        { // 선택 시작
            if (!IsOpen || index < 0 || index >= choices.Count || player == null || player.Build == null) // 선택 조건 확인
            { // 차단 시작
                return false; // 무효 선택 제외
            } // 차단 종료
            UpgradeItemId chosen = choices[index]; // 선택 아이템 보관
            if (!player.Build.Upgrades.TryApply(chosen, player)) // 효과 적용 확인
            { // 차단 시작
                return false; // 무효 효과 제외
            } // 차단 종료
            lastChoice = chosen; // 연속 등장 보정 기록
            PlayerSurvival survival = player.Survival; // 선택 플레이어 생존 상태
            Close(); // 선택과 안전 상태 정리
            survival.GrantTransitionSafety(0.5f); // 선택 직후 시험 안전 시간
            ChoiceCompleted?.Invoke(); // 선택 완료 알림
            return true; // 단일 선택 성공
        } // 선택 종료
        public void Cancel() // 선택 취소와 안전 복원
        { // 취소 시작
            Close(); // 선택 상태 정리
        } // 취소 종료
        private void OnDisable() // 구성 요소 비활성화
        { // 정리 시작
            Cancel(); // 안전 상태 복원
        } // 정리 종료
        private void OnDestroy() // 구성 요소 제거
        { // 정리 시작
            Cancel(); // 안전 상태 복원
        } // 정리 종료
        private void Update() // 플레이어 접촉 확인
        { // 진행 시작
            if (!IsOpen || Time.timeScale <= 0f) // 정지 상태 확인
            { // 중단 시작
                return; // 선택 진행 보류
            } // 중단 종료
            Advance(Time.deltaTime); // 경과 시간 진행
        } // 진행 종료
        public void Advance(float elapsed) // 선택 충돌 처리
        { // 처리 시작
            if (!IsOpen || elapsed <= 0f) // 선택 진행 조건
            { // 중단 시작
                return; // 진행 생략
            } // 중단 종료
            if (player == null || player.Survival == null || !player.Survival.IsAlive) // 플레이어 상태 확인
            { // 취소 시작
                Cancel(); // 선택과 안전 정리
                return; // 추가 처리 생략
            } // 취소 종료
            armTime = Mathf.Max(0f, armTime - elapsed); // 안전 시간 감소
            if (armTime > 0f) // 획득 대기 확인
            { // 대기 시작
                return; // 접촉 처리 대기
            } // 대기 종료
            for (int index = 0; index < visuals.Count; index++) // 아이템 접촉 순회
            { // 접촉 검사 시작
                if (CollisionMath.Intersects(GetPosition(index), player.PreviousPosition, player.transform.position, 0.65f)) // 이동 구간 접촉 확인
                { // 획득 시작
                    TryChoose(index); // 해당 강화 선택
                    break; // 중복 획득 방지
                } // 획득 종료
            } // 접촉 검사 종료
        } // 처리 종료
        private void Close() // 선택 표시와 제한 복원
        { // 정리 시작
            if (player != null) // 플레이어 확인
            { // 플레이어 정리 시작
                player.BattleRestarted -= Cancel; // 출격 이벤트 해제
                if (player.Survival != null) // 생존 상태 확인
                { // 안전 정리 시작
                    player.Survival.SetUpgradeSafe(false); // 선택 무적 해제
                } // 안전 정리 종료
            } // 플레이어 정리 종료
            if (progress != null) // 카드 상태 확인
            { // 카드 정리 시작
                progress.Blocked = oldBlocked; // 기존 카드 제한 복원
            } // 카드 정리 종료
            foreach (GameObject visual in visuals) // 이전 표시 순회
            { // 제거 시작
                if (visual != null && Application.isPlaying) // 플레이 상태 확인
                { // 지연 제거 시작
                    visual.SetActive(false); // 같은 프레임 표시 중지
                    Destroy(visual); // 아이템 표시 제거
                } // 지연 제거 종료
                else if (visual != null) // 편집 시험 확인
                { // 즉시 제거 시작
                    DestroyImmediate(visual); // 시험 표시 제거
                } // 즉시 제거 종료
            } // 제거 종료
            visuals.Clear(); // 표시 목록 초기화
            choices.Clear(); // 후보 목록 초기화
            player = null; // 플레이어 연결 해제
            progress = null; // 카드 연결 해제
            battleCamera = null; // 카메라 연결 해제
            IsOpen = false; // 선택 종료 상태
        } // 정리 종료
        private void CreateVisuals() // 필드 아이템 생성
        { // 생성 시작
            Vector2 center = battleCamera.transform.position; // 화면 중심
            float distance = Mathf.Min(3f, battleCamera.orthographicSize * battleCamera.aspect * 0.45f); // 좌우 간격
            for (int index = 0; index < choices.Count; index++) // 후보 순회
            { // 표시 시작
                float x = choices.Count == 1 ? 0f : (index == 0 ? -distance : distance); // 선택지 가로 위치
                GameObject visual = new GameObject("UpgradeItemChoice"); // 아이템 표시 객체
                visual.transform.SetParent(transform, false); // 선택 구성 요소에 연결
                visual.transform.position = new Vector3(center.x + x, center.y, 0f); // 필드 위치 설정
                visual.transform.localScale = new Vector3(1.2f, 1.2f, 1f); // 아이템 이미지 크기
                SpriteRenderer image = visual.AddComponent<SpriteRenderer>(); // 표시 구성 요소
                image.sprite = Icon(choices[index]); // 종류별 이미지
                image.color = Color.white; // 원본 색상
                image.sortingOrder = 6; // 전투 위 선택 표시
                GameObject ring = new GameObject("PickupRange"); // 획득 범위 표시 객체
                ring.transform.SetParent(visual.transform, false); // 아이템 중심에 연결
                SpriteRenderer ringImage = ring.AddComponent<SpriteRenderer>(); // 범위 표시 구성 요소
                ringImage.sprite = BattleVisuals.Weak; // 원형 안내 이미지
                Vector3 ringBounds = ringImage.sprite.bounds.size; // 원형 이미지 비율
                ring.transform.localScale = new Vector3(1.3f / (1.2f * ringBounds.x), 1.3f / (1.2f * ringBounds.y), 1f); // 획득 지름에 맞춘 안내 크기
                ringImage.color = new Color(0.5f, 0.9f, 1f, 0.45f); // 반투명 범위 색상
                ringImage.sortingOrder = 5; // 아이템 뒤 범위 표시
                visuals.Add(visual); // 표시 목록 보관
            } // 표시 종료
        } // 생성 종료
        private static Sprite Icon(UpgradeItemId id) // 종류별 아이템 이미지
        { // 조회 시작
            switch (id) // 종류 확인
            { // 분기 시작
                case UpgradeItemId.Damage: return BattleSprites.Load("bullet_player"); // 화력 이미지
                case UpgradeItemId.FireRate: return BattleVisuals.Drone; // 연사 이미지
                case UpgradeItemId.Pierce: return BattleSprites.Load("beam_magic"); // 관통 이미지
                case UpgradeItemId.UniqueCooldown: return BattleVisuals.Weak; // 기술 순환 이미지
                case UpgradeItemId.Graze: return BattleVisuals.Crystal; // 근접 충전 이미지
                default: return ShieldIcon(); // 방어막 이미지
            } // 분기 종료
        } // 조회 종료
        private static Sprite ShieldIcon() // 보호막 스프라이트 준비
        { // 준비 시작
            if (shieldSprite == null) // 기존 이미지 확인
            { // 생성 시작
                Texture2D texture = Resources.Load<Texture2D>("SkillIcons/shield"); // 기존 보호막 이미지
                if (texture == null) // 자원 누락 확인
                { // 대체 시작
                    return BattleVisuals.Weak; // 기본 안내 이미지
                } // 대체 종료
                shieldSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), Mathf.Max(texture.width, texture.height)); // 보호막 스프라이트
            } // 생성 종료
            return shieldSprite; // 보호막 이미지 반환
        } // 준비 종료
        private void OnGUI() // 필드 선택 설명 표시
        { // 표시 시작
            if (!IsOpen || battleCamera == null) // 화면 표시 조건
            { // 생략 시작
                return; // 선택 외 표시 생략
            } // 생략 종료
            if (labelStyle == null) // 글꼴 준비 확인
            { // 글꼴 준비 시작
                string[] fontNames = new string[] // 한글 글꼴 대체 목록
                { // 글꼴 목록 시작
                    "Malgun Gothic", // 윈도우 한글 글꼴
                    "Arial" // 대체 글꼴
                }; // 글꼴 목록 종료
                koreanFont = Font.CreateDynamicFontFromOSFont(fontNames, 18); // 한글 글꼴 준비
                labelStyle = new GUIStyle(GUI.skin.label); // 설명 글자 설정
                labelStyle.font = koreanFont != null ? koreanFont : GUI.skin.font; // 사용 가능한 글꼴
                labelStyle.alignment = TextAnchor.UpperCenter; // 설명 중앙 정렬
                labelStyle.fontStyle = FontStyle.Bold; // 설명 글자 강조
                labelStyle.normal.textColor = Color.white; // 설명 흰색
            } // 글꼴 준비 종료
            labelStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(battleCamera.pixelRect.height * 0.022f)); // 화면 비례 글자
            for (int index = 0; index < choices.Count; index++) // 선택 설명 순회
            { // 표시 시작
                Vector3 screen = battleCamera.WorldToScreenPoint(GetPosition(index)); // 화면 위치 변환
                float y = Screen.height - screen.y; // 화면 위쪽 기준 변환
                float offset = battleCamera.pixelRect.height / (2f * battleCamera.orthographicSize) * 0.75f; // 이미지 아래쪽 여백
                float width = Mathf.Max(220f, battleCamera.pixelRect.height * 0.25f); // 화면 비례 설명 너비
                GUI.Label(new Rect(screen.x - width * 0.5f, y + offset, width, labelStyle.fontSize * 3f), UpgradeItemState.Label(choices[index]), labelStyle); // 이름과 효과 표시
            } // 표시 종료
        } // 표시 종료
    } // 구성 요소 종료
} // 영역 종료
