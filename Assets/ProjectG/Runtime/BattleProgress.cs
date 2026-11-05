using System; // 난수 기능
using System.Collections.Generic; // 경험치 조각 목록
using UnityEngine; // 유니티 화면 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BattleProgress : MonoBehaviour // 경험치와 카드 선택
    { // 클래스 시작
        private readonly List<ExperienceShard> shards = new List<ExperienceShard>(); // 경험치 조각 재사용 목록
        private readonly System.Random random = new System.Random(); // 카드 후보 난수
        private Camera battleCamera; // 전투 카메라
        private InputAction first; // 첫 카드 키
        private InputAction second; // 둘째 카드 키
        private InputAction third; // 셋째 카드 키
        private InputAction confirm; // 교체 확인 키
        private InputAction cancel; // 교체 취소 키
        private bool ownsPause; // 카드 화면의 정지 여부
        private float previousTimeScale; // 이전 시간 배율
        private int pendingSwap = -1; // 교체 확인 카드 번호
        private GUIStyle textStyle; // 카드 설명 글자
        private GUIStyle titleStyle; // 카드 제목 글자
        private Font koreanFont; // 한글 글꼴
        public BattlePlayer Player // 플레이어 참조
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public LevelState Level // 전투 경험치 상태
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public CardData[] Offer // 현재 카드 세 장
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public bool Blocked // 전환 중 카드 표시 제한
        { // 속성 시작
            get; // 값 조회
            set; // 값 설정
        } // 속성 종료
        public bool ConfirmingSwap => pendingSwap >= 0; // 공용 스킬 교체 확인
        public void Begin(BattlePlayer player, Camera camera) // 성장 상태 연결
        { // 초기화 시작
            if (Player != null) // 기존 연결 확인
            { // 기존 연결 정리 시작
                Player.ExperienceDropRequested -= SpawnExperience; // 경험치 연결 해제
                Player.BattleRestarted -= ResetBattle; // 출격 연결 해제
            } // 기존 연결 정리 종료
            ReleasePause(); // 기존 정지 해제
            Player = player; // 플레이어 보관
            battleCamera = camera; // 카메라 보관
            Level = new LevelState(); // 레벨 초기화
            Offer = null; // 기존 카드 제거
            pendingSwap = -1; // 교체 확인 초기화
            if (Player != null) // 플레이어 확인
            { // 연결 시작
                Player.ExperienceDropRequested += SpawnExperience; // 경험치 드롭 연결
                Player.BattleRestarted += ResetBattle; // 출격 초기화 연결
            } // 연결 종료
        } // 초기화 종료
        private void OnEnable() // 키보드 입력 연결
        { // 연결 시작
            if (!Application.isPlaying) // 편집 상태 확인
            { // 생략 시작
                return; // 편집 중 입력 생략
            } // 생략 종료
            first = new InputAction("CardOne", InputActionType.Button, "<Keyboard>/1"); // 첫 카드 입력
            second = new InputAction("CardTwo", InputActionType.Button, "<Keyboard>/2"); // 둘째 카드 입력
            third = new InputAction("CardThree", InputActionType.Button, "<Keyboard>/3"); // 셋째 카드 입력
            confirm = new InputAction("ConfirmSwap", InputActionType.Button, "<Keyboard>/enter"); // 교체 확인 입력
            cancel = new InputAction("CancelSwap", InputActionType.Button, "<Keyboard>/escape"); // 교체 취소 입력
            first.Enable(); // 첫 카드 입력 활성화
            second.Enable(); // 둘째 카드 입력 활성화
            third.Enable(); // 셋째 카드 입력 활성화
            confirm.Enable(); // 교체 확인 활성화
            cancel.Enable(); // 교체 취소 활성화
        } // 연결 종료
        private void OnDisable() // 성장 화면 정리
        { // 정리 시작
            first?.Dispose(); // 첫 카드 입력 정리
            second?.Dispose(); // 둘째 카드 입력 정리
            third?.Dispose(); // 셋째 카드 입력 정리
            confirm?.Dispose(); // 확인 입력 정리
            cancel?.Dispose(); // 취소 입력 정리
            Shutdown(); // 성장 화면 정리
        } // 정리 종료
        private void OnDestroy() // 객체 제거 시 정리
        { // 정리 시작
            Shutdown(); // 정지와 조각 정리
        } // 정리 종료
        public void Shutdown() // 성장 기능 정리
        { // 정리 시작
            if (Player != null) // 플레이어 연결 확인
            { // 연결 해제 시작
                Player.ExperienceDropRequested -= SpawnExperience; // 경험치 연결 해제
                Player.BattleRestarted -= ResetBattle; // 출격 연결 해제
            } // 연결 해제 종료
            ClearShards(); // 경험치 조각 정리
            Offer = null; // 선택 화면 정리
            pendingSwap = -1; // 교체 확인 정리
            ReleasePause(); // 전투 시간 복원
        } // 정리 종료
        private void Update() // 카드 입력과 대기 보상 처리
        { // 처리 시작
            TryOpen(); // 대기 보상 확인
            if (Offer == null || first == null) // 입력 조건 확인
            { // 생략 시작
                return; // 선택 화면 외 입력 생략
            } // 생략 종료
            Mouse pointer = Mouse.current; // 현재 포인터 장치
            if (pointer != null && pointer.leftButton.wasPressedThisFrame && battleCamera != null) // 포인터 클릭 확인
            { // 포인터 처리 시작
                Rect view = CardUiLayout.View(battleCamera, Screen.height); // 전투 화면 영역
                Vector2 position = pointer.position.ReadValue(); // 포인터 화면 위치
                position.y = Screen.height - position.y; // 위쪽 기준 좌표 변환
                if (ConfirmingSwap) // 교체 확인 화면
                { // 확인 처리 시작
                    if (CardUiLayout.Confirm(view).Contains(position)) // 확인 버튼 클릭
                    { // 확인 시작
                        ConfirmSwap(true); // 교체 확정
                    } // 확인 종료
                    else if (CardUiLayout.Cancel(view).Contains(position)) // 취소 버튼 클릭
                    { // 취소 시작
                        ConfirmSwap(false); // 교체 취소
                    } // 취소 종료
                } // 확인 처리 종료
                else // 카드 선택 화면
                { // 선택 처리 시작
                    for (int index = 0; index < Offer.Length; index++) // 카드 영역 순회
                    { // 순회 시작
                        if (CardUiLayout.Card(view, index).Contains(position)) // 카드 클릭 확인
                        { // 선택 시작
                            Select(index); // 카드 선택
                            break; // 추가 선택 방지
                        } // 선택 종료
                    } // 순회 종료
                } // 선택 처리 종료
                return; // 같은 프레임 중복 입력 방지
            } // 포인터 처리 종료
            if (ConfirmingSwap) // 스킬 교체 확인
            { // 확인 처리 시작
                if (confirm.WasPressedThisFrame()) // 확인 입력
                { // 확인 시작
                    ConfirmSwap(true); // 교체 승인
                } // 확인 종료
                else if (cancel.WasPressedThisFrame()) // 취소 입력
                { // 취소 시작
                    ConfirmSwap(false); // 교체 취소
                } // 취소 종료
                return; // 일반 선택 생략
            } // 확인 처리 종료
            if (first.WasPressedThisFrame()) // 첫 카드 입력
            { // 선택 시작
                Select(0); // 첫 카드 선택
            } // 선택 종료
            else if (second.WasPressedThisFrame()) // 둘째 카드 입력
            { // 선택 시작
                Select(1); // 둘째 카드 선택
            } // 선택 종료
            else if (third.WasPressedThisFrame()) // 셋째 카드 입력
            { // 선택 시작
                Select(2); // 셋째 카드 선택
            } // 선택 종료
        } // 처리 종료
        public void CollectExperience(int amount) // 경험치 조각 수집
        { // 수집 시작
            if (Level == null || Player == null || Player.Survival == null || !Player.Survival.IsAlive) // 수집 조건 확인
            { // 차단 시작
                return; // 수집 생략
            } // 차단 종료
            Level.Gain(amount); // 경험치와 레벨 반영
            TryOpen(); // 카드 선택 확인
        } // 수집 종료
        public bool Select(int index) // 카드 선택
        { // 선택 시작
            if (Offer == null || ConfirmingSwap || index < 0 || index >= Offer.Length) // 선택 조건 확인
            { // 차단 시작
                return false; // 잘못된 선택 차단
            } // 차단 종료
            if (Offer[index].Id == CardId.C06 && Player.Skills.Common != CommonSkill.Scatter) // 공용 스킬 교체 확인
            { // 교체 확인 시작
                pendingSwap = index; // 확인할 카드 보관
                return true; // 확인 화면 표시
            } // 교체 확인 종료
            return Commit(index); // 카드 효과 적용
        } // 선택 종료
        public bool ConfirmSwap(bool accepted) // 공용 스킬 교체 확정
        { // 확정 시작
            if (!ConfirmingSwap) // 확인 상태 확인
            { // 차단 시작
                return false; // 확인 없이 교체 차단
            } // 차단 종료
            int index = pendingSwap; // 카드 번호 보관
            pendingSwap = -1; // 확인 상태 종료
            return !accepted || Commit(index); // 취소 또는 효과 적용
        } // 확정 종료
        private bool Commit(int index) // 카드 효과와 선택 완료
        { // 적용 시작
            if (Offer == null || !Player.ApplyCard(Offer[index].Id)) // 카드 적용 확인
            { // 실패 시작
                return false; // 실패 시 보상 유지
            } // 실패 종료
            Level.ConsumeChoice(); // 보상 한 개 사용
            Offer = null; // 현재 선택 닫기
            if (Level.PendingChoices > 0) // 다음 보상 확인
            { // 연속 처리 시작
                TryOpen(); // 다음 세 장 표시
            } // 연속 처리 종료
            else // 모든 선택 완료
            { // 재개 시작
                ReleasePause(); // 전투 시간 복원
            } // 재개 종료
            return true; // 적용 성공
        } // 적용 종료
        private void TryOpen() // 대기 중 카드 선택 열기
        { // 열기 시작
            if (Level == null || Level.PendingChoices <= 0 || Offer != null || Blocked || Player == null || Player.Build == null || Player.Survival == null || !Player.Survival.IsAlive) // 표시 조건 확인
            { // 생략 시작
                return; // 선택 대기
            } // 생략 종료
            if (!ownsPause && Time.timeScale <= 0f) // 다른 일시 정지 확인
            { // 대기 시작
                return; // 기존 정지 해제까지 대기
            } // 대기 종료
            if (!ownsPause) // 카드 정지 확인
            { // 정지 시작
                previousTimeScale = Time.timeScale; // 이전 시간 배율 보관
                Time.timeScale = 0f; // 전투 정지
                ownsPause = true; // 카드 정지 소유
            } // 정지 종료
            Offer = CardCatalog.Offer(Player.Build.Cards, Player.Character, Player.Skills.Common, Player.Survival.Health < Player.Survival.MaximumHealth, random, Player.Skills.Energy < 100); // 유효 카드 세 장 추첨
        } // 열기 종료
        private void ReleasePause() // 카드 정지 해제
        { // 해제 시작
            if (ownsPause) // 정지 소유 확인
            { // 복원 시작
                Time.timeScale = previousTimeScale; // 이전 시간 배율 복원
                ownsPause = false; // 정지 소유 해제
            } // 복원 종료
        } // 해제 종료
        private void SpawnExperience(int value, Vector2 position) // 경험치 조각 생성
        { // 생성 시작
            if (battleCamera == null || value <= 0) // 생성 조건 확인
            { // 생략 시작
                return; // 생성 생략
            } // 생략 종료
            ExperienceShard shard = shards.Find(item => item != null && !item.gameObject.activeSelf); // 회수된 조각 검색
            if (shard == null) // 재사용 조각 확인
            { // 새 조각 생성 시작
                GameObject item = new GameObject("ExperienceShard"); // 경험치 객체 생성
                item.transform.SetParent(battleCamera.transform, false); // 전투 장면에 보관
                shard = item.AddComponent<ExperienceShard>(); // 조각 구성 요소
                shards.Add(shard); // 재사용 목록 등록
            } // 새 조각 생성 종료
            shard.Begin(this, position, value); // 조각 초기화
        } // 생성 종료
        private void ResetBattle() // 출격 상태 초기화
        { // 초기화 시작
            ClearShards(); // 이전 경험치 조각 제거
            Level = new LevelState(); // 레벨 초기화
            Offer = null; // 선택 화면 초기화
            pendingSwap = -1; // 교체 확인 초기화
            Blocked = false; // 표시 제한 초기화
            ReleasePause(); // 시간 배율 복원
        } // 초기화 종료
        private void ClearShards() // 경험치 조각 정리
        { // 정리 시작
            foreach (ExperienceShard shard in shards) // 조각 목록 순회
            { // 순회 시작
                if (shard == null) // 삭제된 조각 확인
                { // 생략 시작
                    continue; // 다음 조각 확인
                } // 생략 종료
                if (Application.isPlaying) // 플레이 상태 확인
                { // 플레이 정리 시작
                    Destroy(shard.gameObject); // 지연 제거
                } // 플레이 정리 종료
                else // 편집 시험 상태
                { // 시험 정리 시작
                    DestroyImmediate(shard.gameObject); // 즉시 제거
                } // 시험 정리 종료
            } // 순회 종료
            shards.Clear(); // 재사용 목록 비우기
        } // 정리 종료
        private void OnGUI() // 레벨업 카드 화면
        { // 표시 시작
            if (Offer == null || battleCamera == null || Player == null) // 표시 조건 확인
            { // 생략 시작
                return; // 선택 화면 생략
            } // 생략 종료
            PrepareStyles(); // 글자 스타일 준비
            Rect view = CardUiLayout.View(battleCamera, Screen.height); // 전투 화면 영역
            Color previous = GUI.color; // 기존 화면 색상
            GUI.color = new Color(0f, 0f, 0f, 0.84f); // 선택 화면 배경
            GUI.DrawTexture(view, Texture2D.whiteTexture); // 전투 위 어두운 배경
            GUI.color = Color.white; // 원래 밝기 복원
            titleStyle.fontSize = Mathf.Max(18, Mathf.RoundToInt(view.height * 0.04f)); // 제목 크기
            textStyle.fontSize = Mathf.Max(13, Mathf.RoundToInt(view.height * 0.022f)); // 본문 크기
            GUI.Label(new Rect(view.x, view.y + view.height * 0.08f, view.width, view.height * 0.08f), "레벨업 · 카드 1장 선택", titleStyle); // 선택 제목
            Rect reference = CardUiLayout.Card(view, 0); // 카드 크기 기준
            float width = reference.width; // 카드 너비
            float height = reference.height; // 카드 높이
            bool enabled = GUI.enabled; // 기존 입력 상태
            GUI.enabled = !ConfirmingSwap; // 교체 확인 중 카드 입력 차단
            CardData[] displayed = Offer; // 현재 표시 카드 보관
            for (int index = 0; index < displayed.Length; index++) // 카드 세 장 순회
            { // 카드 표시 시작
                CardData card = displayed[index]; // 현재 카드
                Rect area = CardUiLayout.Card(view, index); // 카드 위치
                GUI.Box(area, GUIContent.none); // 카드 배경 표시
                Texture2D icon = Resources.Load<Texture2D>(card.Category == CardCategory.Skill ? "SkillIcons/scatter" : card.Category == CardCategory.Survival ? "SkillIcons/shield" : "SkillIcons/rapid"); // 시험용 분류 이미지
                if (icon != null) // 이미지 자원 확인
                { // 이미지 표시 시작
                    GUI.DrawTexture(new Rect(area.x + width * 0.25f, area.y + height * 0.06f, width * 0.5f, height * 0.35f), icon, ScaleMode.ScaleToFit); // 카드 이미지 표시
                } // 이미지 표시 종료
                string tier = card.Tier == CardTier.Normal ? "일반" : card.Tier == CardTier.Rare ? "희귀" : "영웅"; // 등급 이름
                int current = Player.Build.Cards.Count(card.Id); // 현재 카드 단계
                string levels = card.Maximum == int.MaxValue ? "반복 가능" : "현재 " + current + " → " + (current + 1); // 강화 단계 표시
                string description = "[" + (index + 1) + "] " + card.Name + "\n" + tier + " · " + CategoryName(card.Category) + "\n\n" + card.Description + "\n" + CardCatalog.DescribeChange(card, Player) + "\n\n" + levels; // 카드 설명
                GUI.Label(new Rect(area.x + width * 0.08f, area.y + height * 0.43f, width * 0.84f, height * 0.52f), description, textStyle); // 카드 내용 표시
            } // 카드 표시 종료
            GUI.enabled = enabled; // 기존 입력 상태 복원
            if (ConfirmingSwap) // 스킬 교체 확인 표시
            { // 확인 화면 시작
                Rect box = CardUiLayout.SwapBox(view); // 확인 화면 영역
                GUI.Box(box, GUIContent.none); // 확인 배경
                GUI.Label(new Rect(box.x + 10f, box.y + 8f, box.width - 20f, box.height * 0.55f), "K 보호막: 1회 방어 · 12초\n→ 산탄: 3발 × 피해 12 · 10초\n남은 대기 " + Mathf.CeilToInt(Player.Skills.CommonCooldown) + "초 유지 · 강화 비계승", textStyle); // 교체 내용
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button); // 확인 버튼 글꼴
                buttonStyle.font = koreanFont != null ? koreanFont : GUI.skin.font; // 한글 글꼴 적용
                GUI.Box(CardUiLayout.Confirm(view), "확인 Enter", buttonStyle); // 확인 버튼 표시
                GUI.Box(CardUiLayout.Cancel(view), "취소 Esc", buttonStyle); // 취소 버튼 표시
            } // 확인 화면 종료
            GUI.color = previous; // 기존 화면 색상 복원
        } // 표시 종료
        private static string CategoryName(CardCategory category) // 카드 분류 이름
        { // 변환 시작
            switch (category) // 분류 확인
            { // 구분 시작
                case CardCategory.Attack: return "공격"; // 공격 강화
                case CardCategory.Skill: return "스킬"; // 스킬 강화
                case CardCategory.Support: return "보조"; // 보조 공격
                case CardCategory.Item: return "아이템"; // 패시브 아이템
                case CardCategory.Survival: return "생존"; // 생존 보상
                default: return "전용"; // 캐릭터 전용
            } // 구분 종료
        } // 변환 종료
        private void PrepareStyles() // 카드 글자 스타일 준비
        { // 준비 시작
            if (textStyle != null) // 기존 스타일 확인
            { // 재사용 시작
                return; // 기존 스타일 유지
            } // 재사용 종료
            string[] fontNames = new string[] // 글꼴 대체 순서
            { // 목록 시작
                "Malgun Gothic", // 윈도우 한글 글꼴
                "Arial" // 기본 대체 글꼴
            }; // 목록 종료
            koreanFont = Font.CreateDynamicFontFromOSFont(fontNames, 18); // 시스템 한글 글꼴
            textStyle = new GUIStyle(GUI.skin.label); // 카드 본문 스타일
            textStyle.font = koreanFont != null ? koreanFont : GUI.skin.font; // 사용 가능한 글꼴
            textStyle.wordWrap = true; // 설명 줄바꿈
            textStyle.alignment = TextAnchor.UpperCenter; // 본문 중앙 정렬
            textStyle.normal.textColor = Color.white; // 본문 흰색
            titleStyle = new GUIStyle(textStyle); // 제목 스타일
            titleStyle.fontStyle = FontStyle.Bold; // 제목 굵게
            titleStyle.alignment = TextAnchor.MiddleCenter; // 제목 중앙 정렬
        } // 준비 종료
    } // 클래스 종료
} // 영역 종료
