using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 새 입력 시스템
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BattlePlayer : MonoBehaviour // 플레이어 구성 요소
    { // 클래스 시작
        private BattleSession session; // 전투 상태
        public CharacterType Character // 현재 캐릭터
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public CardBuild Build // 전투 카드 효과
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public RunStatistics Statistics // 현재 출격 기록
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        private CardSupport support; // 보조 공격 상태
        public event System.Action BattleRestarted; // 출격 초기화 알림
        public RapidCombo Combo // 속사 누적 상태
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public event System.Action<Rect, int> BeamDamageRequested; // 광선 피해 요청
        private EnemyBulletPool bulletPool; // 적탄 풀 참조
        public SkillState Skills // 스킬과 에너지 상태
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public event System.Action<int> UltimateDamageRequested; // 필살기 피해 요청
        public event System.Action<int> ExperienceRewarded; // 경험치 보상 알림
        public event System.Action<int, Vector2> ExperienceDropRequested; // 경험치 조각 생성 요청
        public void AssignBulletPool(EnemyBulletPool pool) // 적탄 풀 연결
        { // 연결 시작
            bulletPool = pool; // 적탄 풀 보관
        } // 연결 종료
        public bool ApplyCard(CardId id) // 카드 효과 적용
        { // 적용 시작
            if (Build == null || Skills == null || Survival == null || !Survival.IsAlive || !CardCatalog.IsEligible(id, Build.Cards, Character, Skills.Common, Survival.Health < Survival.MaximumHealth, Skills.Energy < 100)) // 적용 조건 확인
            { // 차단 시작
                return false; // 무효 카드 차단
            } // 차단 종료
            Build.Cards.Add(id); // 카드 단계 증가
            Skills.UniqueCooldownReduction = Build.UniqueCooldownReduction; // 고유기 쿨타임 효과
            Combo.GracePeriod = 2f + Build.Cards.Count(CardId.R01); // 속사 유지 시간 효과
            if (id == CardId.C06) // 공용 산탄 카드 확인
            { // 교체 시작
                Skills.Equip(CommonSkill.Scatter); // 공용 스킬 교체
            } // 교체 종료
            if (id == CardId.C10) // 보호 장치 확인
            { // 장착 시작
                Survival.InstallReactiveShield(); // 피격 후 보호 장치
            } // 장착 종료
            if (id == CardId.C12) // 응급 회복 확인
            { // 회복 시작
                Survival.Heal(20); // 시험 체력 회복
            } // 회복 종료
            if (id == CardId.F01 || id == CardId.F02 || id == CardId.F03) // 대체 보상 확인
            { // 대체 시작
                Survival.IncreaseMaximum(id == CardId.F01 ? 1 : id == CardId.F02 ? 2 : 3); // 시험용 안전 보급
            } // 대체 종료
            if (id == CardId.F04) // 대체 회복 확인
            { // 회복 시작
                Survival.Heal(10); // 시험 회복량
            } // 회복 종료
            if (id == CardId.F05) // 대체 충전 확인
            { // 충전 시작
                Skills.GainEnergy(5); // 시험 충전량
            } // 충전 종료
            Statistics?.RecordCard(id); // 선택 카드 기록
            return true; // 적용 성공
        } // 적용 종료
        public bool TryUseRapid() // 고유 스킬 사용
        { // 사용 시작
            if (Skills == null || Survival == null) // 초기화 확인
            { // 차단 시작
                return false; // 사용 불가
            } // 차단 종료
            if (Character == CharacterType.Rapid) // 속사형 확인
            { // 속사 처리 시작
                return Skills.TryRapid(Survival.IsAlive); // 집중 포화 발동
            } // 속사 처리 종료
            if (!Skills.TrySniper(Survival.IsAlive)) // 저격 스킬 조건 확인
            { // 차단 시작
                return false; // 관통 사격 차단
            } // 차단 종료
            float top = battleCamera != null ? battleCamera.transform.position.y + battleCamera.orthographicSize : 5f; // 화면 위 경계
            GameObject shot = new GameObject("SniperSkill"); // 관통 탄환 생성
            shot.transform.SetParent(TransientRoot, true); // 전투 생성물 소속
            shot.transform.position = transform.position + new Vector3(0f, 0.25f, 0f); // 발사 위치
            shot.AddComponent<Projectile>().Begin(new Vector2(0f, 12f), top, 100, false, this, int.MaxValue); // 시험 관통탄
            return true; // 고유 스킬 발동
        } // 사용 종료
        public bool TryUseCommon() // 공용 스킬 사용
        { // 사용 시작
            if (Skills == null || Survival == null || !Skills.TryCommon(Survival.IsAlive)) // 발동 조건 확인
            { // 차단 시작
                return false; // 공용 스킬 차단
            } // 차단 종료
            if (Skills.Common == CommonSkill.Shield) // 보호막 장착 확인
            { // 보호막 시작
                Survival.ActivateShield(1f); // 시험용 1초 보호막
            } // 보호막 종료
            else // 공용 산탄 장착
            { // 산탄 시작
                float top = battleCamera != null ? battleCamera.transform.position.y + battleCamera.orthographicSize : 5f; // 화면 위 경계
                for (int index = 0; index < 3; index++) // 산탄 세 발 순회
                { // 발사 시작
                    float spread = (index - 1) * 2f; // 시험용 산탄 방향
                    GameObject bullet = new GameObject("SkillScatter"); // 스킬 탄환 생성
                    bullet.transform.SetParent(TransientRoot, true); // 전투 생성물 소속
                    bullet.transform.position = transform.position + new Vector3(spread * 0.08f, 0.25f, 0f); // 발사 시작 위치
                    bullet.AddComponent<Projectile>().Begin(new Vector2(spread, 10f), top, 12, false, this); // 산탄 시험 피해량
                } // 발사 종료
            } // 산탄 종료
            return true; // 공용 스킬 발동
        } // 사용 종료
        public bool TryUseUltimate() // 필살기 사용
        { // 사용 시작
            if (Skills == null || Survival == null || battleCamera == null || bulletPool == null || !Skills.TryUltimate(Survival.IsAlive)) // 발동 조건 확인
            { // 차단 시작
                return false; // 필살기 차단
            } // 차단 종료
            float halfHeight = battleCamera.orthographicSize; // 화면 세로 반경
            float halfWidth = halfHeight * 16f / 9f; // 화면 가로 반경
            Vector3 center = battleCamera.transform.position; // 화면 중심
            Rect screen = new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f); // 전투 화면 범위
            if (Character == CharacterType.Rapid) // 속사형 필살기 확인
            { // 속사 필살기 시작
                bulletPool.ClearVisible(screen); // 화면 안 소거 가능 적탄 제거
                foreach (CombatTarget target in Object.FindObjectsByType<CombatTarget>(FindObjectsSortMode.None)) // 시험 표적 순회
                { // 피해 처리 시작
                    if (screen.Contains(target.transform.position)) // 화면 안 표적 확인
                    { // 화면 안 처리 시작
                        target.ReceiveDamage(80f, this); // 시험 피해 적용
                    } // 화면 안 처리 종료
                } // 피해 처리 종료
                UltimateDamageRequested?.Invoke(80); // 적 전체 시험 피해 요청
            } // 속사 필살기 종료
            else // 저격형 필살기
            { // 저격 필살기 시작
                float top = center.y + halfHeight; // 화면 위쪽
                Rect beam = new Rect(transform.position.x - 0.2f, transform.position.y, 0.4f, Mathf.Max(0f, top - transform.position.y)); // 시험용 광선 범위
                bulletPool.ClearVisible(beam); // 광선 안 소거 가능 적탄 제거
                foreach (CombatTarget target in Object.FindObjectsByType<CombatTarget>(FindObjectsSortMode.None)) // 시험 표적 순회
                { // 피해 처리 시작
                    if (target.IntersectsBeam(beam, out bool weak)) // 광선 접촉 확인
                    { // 접촉 처리 시작
                        target.ReceiveDamage(220f * (weak ? Build.WeakMultiplier : 1f), this); // 시험 약점 피해 적용
                    } // 접촉 처리 종료
                } // 피해 처리 종료
                BeamDamageRequested?.Invoke(beam, 220); // 실제 적 연결용 피해 요청
                BeamEffect.Create(beam, TransientRoot); // 전투 소속 광선 표시
            } // 저격 필살기 종료
            return true; // 필살기 발동
        } // 사용 종료
        public void RegisterEnemyDefeat(EnemyRank rank, int experience = 0, Vector2? dropPosition = null) // 적 처치 보상
        { // 보상 시작
            if (Skills != null && Survival != null && Survival.IsAlive) // 생존 상태 확인
            { // 충전 시작
                Skills.GainDefeat(rank); // 적 종류별 에너지 충전
                Statistics?.RecordDefeat(); // 적 처치 기록
                if (experience > 0) // 경험치 보상 확인
                { // 보상 알림 시작
                    ExperienceRewarded?.Invoke(experience); // 기존 경험치 보상 알림
                    ExperienceDropRequested?.Invoke(experience, dropPosition ?? (Vector2)transform.position); // 경험치 조각 생성 요청
                } // 보상 알림 종료
            } // 충전 종료
        } // 보상 종료
        public PlayerSurvival Survival // 플레이어 생존 상태
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public int GrazeCount // 누적 그레이즈 수
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public Vector2 PreviousPosition // 이전 플레이어 위치
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        private InputAction moveAction; // 이동 입력
        private InputAction focusAction; // 집중 입력
        private InputAction rapidAction; // 고유 스킬 입력
        private InputAction commonAction; // 공용 스킬 입력
        private InputAction ultimateAction; // 필살기 입력
        private InputAction rapidSelectAction; // 속사형 시험 선택
        private InputAction sniperSelectAction; // 저격형 시험 선택
        private KeyBindingSettings bindings; // 사용자 전투 키 설정
        private Camera battleCamera; // 전투 카메라
        private Transform transientRoot; // 전투 생성물 보관 위치
        public Transform TransientRoot // 고정 생성물 보관 위치
        { // 속성 시작
            get // 보관 위치 조회
            { // 조회 시작
                if (transientRoot == null) // 보관 위치 생성 확인
                { // 생성 시작
                    transientRoot = new GameObject("BattleTransientObjects").transform; // 독립 보관 객체 생성
                    transientRoot.SetParent(battleCamera != null ? battleCamera.transform : null, true); // 월드 좌표 유지 소속
                } // 생성 종료
                return transientRoot; // 보관 위치 반환
            } // 조회 종료
        } // 속성 종료
        public bool IsFocused => session != null && session.Focused; // 현재 집중 상태
        public bool AllowDebugCharacterSwitch // 시험용 캐릭터 변경 허용
        { // 속성 시작
            get; // 값 조회
            set; // 값 설정
        } = true; // 시험 선택 기본 허용
        public void AssignCamera(Camera camera) // 카메라 연결
        { // 연결 시작
            battleCamera = camera; // 전투 카메라 지정
            if (transientRoot != null) // 기존 생성물 확인
            { // 소속 변경 시작
                transientRoot.SetParent(camera != null ? camera.transform : null, true); // 월드 좌표 유지 변경
            } // 소속 변경 종료
        } // 연결 종료
        public void SetBindings(KeyBindingSettings settings) // 전투 키 설정 연결
        { // 연결 시작
            if (bindings != null) // 이전 설정 확인
            { // 해제 시작
                bindings.Changed -= ApplyBindings; // 이전 변경 알림 해제
            } // 해제 종료
            bindings = settings; // 새 설정 보관
            if (bindings != null) // 새 설정 확인
            { // 연결 처리 시작
                bindings.Changed += ApplyBindings; // 키 변경 알림 연결
                ApplyBindings(); // 현재 전투 입력 갱신
            } // 연결 처리 종료
        } // 연결 종료
        public string ControlLabel(PlayerAction action) // 현재 조작 키 표시
        { // 조회 시작
            if (bindings == null) // 키 설정 확인
            { // 기본 표시 시작
                return action == PlayerAction.Unique ? "J" : action == PlayerAction.Common ? "K" : action == PlayerAction.Ultimate ? "L" : ""; // 기본 스킬 키
            } // 기본 표시 종료
            return bindings.GetControl(action).ToUpperInvariant(); // 설정된 키 이름 표시
        } // 조회 종료
        private void ApplyBindings() // 전투 입력 키 적용
        { // 적용 시작
            if (bindings == null || moveAction == null || focusAction == null || rapidAction == null || commonAction == null || ultimateAction == null) // 입력 초기화 확인
            { // 대기 시작
                return; // 입력 생성 전 적용 생략
            } // 대기 종료
            bool enabled = moveAction.enabled; // 기존 입력 활성 상태
            moveAction.Disable(); // 이동 입력 잠시 정지
            focusAction.Disable(); // 집중 입력 잠시 정지
            rapidAction.Disable(); // 고유기 입력 잠시 정지
            commonAction.Disable(); // 공용기 입력 잠시 정지
            ultimateAction.Disable(); // 필살기 입력 잠시 정지
            moveAction.ApplyBindingOverride(1, bindings.GetPath(PlayerAction.MoveUp)); // 위 이동 키 적용
            moveAction.ApplyBindingOverride(2, bindings.GetPath(PlayerAction.MoveDown)); // 아래 이동 키 적용
            moveAction.ApplyBindingOverride(3, bindings.GetPath(PlayerAction.MoveLeft)); // 왼쪽 이동 키 적용
            moveAction.ApplyBindingOverride(4, bindings.GetPath(PlayerAction.MoveRight)); // 오른쪽 이동 키 적용
            focusAction.ApplyBindingOverride(0, bindings.GetPath(PlayerAction.Focus)); // 집중 키 적용
            rapidAction.ApplyBindingOverride(0, bindings.GetPath(PlayerAction.Unique)); // 고유기 키 적용
            commonAction.ApplyBindingOverride(0, bindings.GetPath(PlayerAction.Common)); // 공용기 키 적용
            ultimateAction.ApplyBindingOverride(0, bindings.GetPath(PlayerAction.Ultimate)); // 필살기 키 적용
            if (enabled) // 기존 활성 상태 확인
            { // 재개 시작
                moveAction.Enable(); // 이동 입력 재개
                focusAction.Enable(); // 집중 입력 재개
                rapidAction.Enable(); // 고유기 입력 재개
                commonAction.Enable(); // 공용기 입력 재개
                ultimateAction.Enable(); // 필살기 입력 재개
            } // 재개 종료
        } // 적용 종료
        private void OnEnable() // 입력 활성화
        { // 활성화 시작
            if (!Application.isPlaying) // 편집 상태 확인
            { // 편집 상태 시작
                return; // 편집 중 입력 생략
            } // 편집 상태 종료
            if (bindings == null) // 사용자 설정 확인
            { // 기본 설정 시작
                KeyBindingSettings saved = new KeyBindingSettings(); // 저장 키 설정 생성
                saved.Load(); // 사용자 키 복원
                SetBindings(saved); // 입력 설정 연결
            } // 기본 설정 종료
            else // 기존 설정 재사용
            { // 재연결 시작
                bindings.Changed -= ApplyBindings; // 중복 알림 방지
                bindings.Changed += ApplyBindings; // 키 변경 알림 연결
            } // 재연결 종료
            moveAction = new InputAction("Move", InputActionType.Value); // 이동 입력 생성
            moveAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d"); // WASD 기본 입력
            focusAction = new InputAction("Focus", InputActionType.Button, "<Keyboard>/leftShift"); // 집중 기본 입력
            rapidAction = new InputAction("Unique", InputActionType.Button, "<Keyboard>/j"); // 고유 스킬 기본 입력
            commonAction = new InputAction("Common", InputActionType.Button, "<Keyboard>/k"); // 공용 스킬 기본 입력
            ultimateAction = new InputAction("Ultimate", InputActionType.Button, "<Keyboard>/l"); // 필살기 기본 입력
            moveAction.Enable(); // 이동 입력 시작
            focusAction.Enable(); // 집중 입력 시작
            rapidAction.Enable(); // 고유 스킬 입력 시작
            commonAction.Enable(); // 공용 스킬 입력 시작
            ultimateAction.Enable(); // 필살기 입력 시작
            ApplyBindings(); // 저장된 사용자 키 적용
            rapidSelectAction = new InputAction("SelectRapid", InputActionType.Button, "<Keyboard>/f1"); // 속사형 선택 입력
            sniperSelectAction = new InputAction("SelectSniper", InputActionType.Button, "<Keyboard>/f2"); // 저격형 선택 입력
            rapidSelectAction.Enable(); // 속사형 선택 시작
            sniperSelectAction.Enable(); // 저격형 선택 시작
        } // 활성화 종료
        private void OnDisable() // 입력 비활성화
        { // 비활성화 시작
            if (bindings != null) // 키 설정 연결 확인
            { // 알림 해제 시작
                bindings.Changed -= ApplyBindings; // 변경 알림 해제
            } // 알림 해제 종료
            moveAction?.Dispose(); // 이동 입력 정리
            focusAction?.Dispose(); // 집중 입력 정리
            rapidAction?.Dispose(); // 고유 스킬 입력 정리
            commonAction?.Dispose(); // 공용 스킬 입력 정리
            ultimateAction?.Dispose(); // 필살기 입력 정리
            rapidSelectAction?.Dispose(); // 속사형 선택 정리
            sniperSelectAction?.Dispose(); // 저격형 선택 정리
        } // 비활성화 종료
        private void OnDestroy() // 플레이어 제거 처리
        { // 제거 시작
            ClearTransientObjects(); // 단독 생성물 정리
        } // 제거 종료
        private void ClearTransientObjects() // 전투 생성물 정리
        { // 정리 시작
            if (transientRoot == null) // 생성물 존재 확인
            { // 중단 시작
                return; // 정리 생략
            } // 중단 종료
            GameObject previous = transientRoot.gameObject; // 이전 보관 객체
            transientRoot = null; // 보관 위치 초기화
            previous.SetActive(false); // 이전 생성물 즉시 비활성화
            if (Application.isPlaying) // 실행 상태 확인
            { // 실행 정리 시작
                Destroy(previous); // 프레임 종료 시 제거
            } // 실행 정리 종료
            else // 편집 시험 상태
            { // 편집 정리 시작
                DestroyImmediate(previous); // 즉시 제거
            } // 편집 정리 종료
        } // 정리 종료
        private void Start() // 전투 시작
        { // 시작 처리
            if (battleCamera == null) // 카메라 지정 확인
            { // 카메라 확인 시작
                battleCamera = Camera.main; // 주 카메라 확인
            } // 카메라 확인 종료
            if (session == null) // 초기 상태 확인
            { // 초기 상태 시작
                Begin(transform.position); // 현재 위치로 시작
            } // 초기 상태 종료
        } // 시작 종료
        private void Update() // 매 프레임 처리
        { // 처리 시작
            if (Time.timeScale <= 0f || battleCamera == null || moveAction == null || focusAction == null) // 실행 조건 확인
            { // 실행 불가 시작
                return; // 입력 처리 생략
            } // 실행 불가 종료
            if (AllowDebugCharacterSwitch && Time.deltaTime > 0f && rapidSelectAction.WasPressedThisFrame()) // 속사형 시험 선택
            { // 선택 시작
                Begin(transform.position, CharacterType.Rapid); // 속사형 전투 초기화
            } // 선택 종료
            if (AllowDebugCharacterSwitch && Time.deltaTime > 0f && sniperSelectAction.WasPressedThisFrame()) // 저격형 시험 선택
            { // 선택 시작
                Begin(transform.position, CharacterType.Sniper); // 저격형 전투 초기화
            } // 선택 종료
            float halfHeight = battleCamera.orthographicSize; // 카메라 세로 반경
            float halfWidth = halfHeight * 16f / 9f; // 가로 16대9 반경
            Vector3 center = battleCamera.transform.position; // 카메라 중심
            bool toggle = PlayerPrefs.GetInt("ProjectG.FocusToggle", 0) == 1; // 집중 방식 설정
            if (Time.deltaTime > 0f && rapidAction.WasPressedThisFrame()) // 고유 스킬 입력 확인
            { // 고유 스킬 시작
                TryUseRapid(); // 고유 스킬 사용
            } // 고유 스킬 종료
            if (Time.deltaTime > 0f && commonAction.WasPressedThisFrame()) // 공용 스킬 입력 확인
            { // 공용 스킬 시작
                TryUseCommon(); // 공용 스킬 사용
            } // 공용 스킬 종료
            if (Time.deltaTime > 0f && ultimateAction.WasPressedThisFrame()) // 필살기 입력 확인
            { // 필살기 시작
                TryUseUltimate(); // 필살기 사용
            } // 필살기 종료
            Step(moveAction.ReadValue<Vector2>(), focusAction.WasPressedThisFrame(), focusAction.IsPressed(), toggle, Time.deltaTime, center.x - halfWidth, center.x + halfWidth, center.y - halfHeight, center.y + halfHeight); // 입력과 경계 적용
        } // 처리 종료
        public void Begin(Vector2 start, CharacterType type = CharacterType.Rapid) // 플레이어 시작
        { // 초기화 시작
            ClearTransientObjects(); // 이전 전투 생성물 정리
            Character = type; // 캐릭터 선택
            SpriteRenderer image = GetComponent<SpriteRenderer>(); // 캐릭터 표시 요소
            if (image != null) // 표시 요소 확인
            { // 이미지 변경 시작
                image.sprite = BattleSprites.Player(type); // 선택 캐릭터 이미지
                image.color = Color.white; // 원본 이미지 색상
            } // 이미지 변경 종료
            support?.Clear(); // 이전 보조 표시 정리
            Combo = new RapidCombo(); // 누적 화력 초기화
            Build = new CardBuild(); // 전투 카드 초기화
            Statistics = new RunStatistics(); // 전투 기록 초기화
            support = new CardSupport(); // 보조 공격 초기화
            session = new BattleSession(start, type); // 캐릭터별 전투 상태 생성
            Skills = new SkillState(); // 스킬과 에너지 초기화
            Survival = new PlayerSurvival(100); // 시험용 최대 체력
            Survival.Damaged += Statistics.RecordHit; // 실제 피격 기록 연결
            GrazeCount = 0; // 그레이즈 기록 초기화
            PreviousPosition = start; // 이전 위치 초기화
            transform.position = new Vector3(start.x, start.y, 0f); // 시작 위치 적용
            BattleRestarted?.Invoke(); // 새 출격 알림
        } // 초기화 종료
        public void Step(Vector2 input, bool pressed, bool held, bool toggle, float elapsed, float left, float right, float bottom, float top) // 플레이어 진행
        { // 진행 시작
            if (session == null) // 초기화 여부 확인
            { // 초기화 시작
                Begin(transform.position); // 현재 위치 초기화
            } // 초기화 종료
            PreviousPosition = transform.position; // 이동 전 위치 보관
            if (elapsed <= 0f || !Survival.IsAlive) // 정지 또는 출격 실패 확인
            { // 진행 중단 시작
                return; // 이동과 공격 중단
            } // 진행 중단 종료
            Survival.Advance(elapsed); // 무적 시간 진행
            Skills.Advance(elapsed); // 스킬 활성과 쿨타임 진행
            Combo.Advance(elapsed); // 누적 화력 유지와 감소
            int shots = session.Advance(input, pressed, held, toggle, elapsed, left, right, bottom, top, Skills.AttackIntervalScale * Build.IntervalScale); // 이동과 공격 계산
            transform.position = new Vector3(session.Position.x, session.Position.y, 0f); // 계산 위치 적용
            for (int index = 0; index < shots; index++) // 발사 탄환 순회
            { // 발사 시작
                float spread = shots == 2 ? (index == 0 ? -0.7f : 0.7f) : 0f; // 시험용 확산 방향
                spread *= Character == CharacterType.Rapid && !session.Focused && Build.Cards.Count(CardId.R02) > 0 ? 1.2f : 1f; // 기본 공격 사격 폭 강화
                GameObject bullet = new GameObject("PlayerBullet"); // 탄환 객체 생성
                bullet.transform.SetParent(TransientRoot, true); // 전투 생성물 소속
                bullet.transform.position = transform.position + new Vector3(spread * 0.15f, 0.25f, 0f); // 발사 시작 위치
                float baseDamage = Character == CharacterType.Sniper ? (session.Focused ? 30f : 14f) : (session.Focused ? 7f : 3f); // 캐릭터별 기본 피해
                Projectile projectile = bullet.AddComponent<Projectile>(); // 탄환 구성 요소
                projectile.Begin(new Vector2(spread, 10f), top, Build.Damage(baseDamage, session.Focused), true, this, (Character == CharacterType.Sniper ? 2 : 1) + Build.ExtraPierce); // 카드 효과 적용 탄환
                if (session.Focused) // 집중 공격 확인
                { // 집중 판정 시작
                    projectile.SetFocusWidth(Build.FocusWidth); // 집중탄 시험 폭 적용
                } // 집중 판정 종료
            } // 발사 종료
            support.Advance(elapsed, this, top); // 보조 공격 진행
        } // 진행 종료
        public void RegisterGraze() // 그레이즈 기록
        { // 기록 시작
            if (Survival != null && Survival.IsAlive) // 생존 상태 확인
            { // 기록 처리 시작
                GrazeCount++; // 그레이즈 횟수 증가
                Statistics?.RecordGraze(); // 그레이즈 기록 저장
                Skills.GainGraze(1f + Build.Upgrades.Count(UpgradeItemId.Graze) * 0.20f); // 강화 배율과 소수 충전
            } // 기록 처리 종료
        } // 기록 종료
    } // 클래스 종료
} // 영역 종료
