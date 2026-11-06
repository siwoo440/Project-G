using System; // 발생 신호 기능
using UnityEngine; // 실제 오디오 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class SceneSoundPlayer : MonoBehaviour, ISoundCueSink // 편집 가능한 실제 오디오 루트
    { // 구성 시작
        [SerializeField] private AudioSource musicSource; // 배경 음악 출력
        [SerializeField] private AudioSource combatSource; // 전투 효과 출력
        [SerializeField] private AudioSource skillSource; // 스킬 효과 출력
        [SerializeField] private AudioSource uiSource; // UI 효과 출력
        [SerializeField] private AudioClip menuMusic; // 미정 메뉴 음악
        [SerializeField] private AudioClip battleMusic; // 미정 전투 음악
        [SerializeField] private AudioClip bossMusic; // 미정 보스 음악
        [SerializeField] private AudioClip resultMusic; // 미정 결과 음악
        [SerializeField] private AudioClip shotClip; // 미정 사격 효과
        [SerializeField] private AudioClip hitClip; // 미정 피해 효과
        [SerializeField] private AudioClip grazeClip; // 미정 그레이즈 효과
        [SerializeField] private AudioClip defeatClip; // 미정 적 처치 효과
        [SerializeField] private AudioClip skillClip; // 미정 스킬 효과
        [SerializeField] private AudioClip ultimateClip; // 미정 필살기 효과
        [SerializeField] private AudioClip confirmClip; // 미정 확인 효과
        [SerializeField] private AudioClip cancelClip; // 미정 취소 효과
        private AudioSource[] capturedSources; // 원래 설정을 보관한 출력기
        private float[] baseVolumes; // Inspector의 원래 출력 음량
        private bool[] baseMutes; // Inspector의 원래 음소거
        private BattlePlayer battle; // 현재 출격 플레이어
        private PlayerSurvival survival; // 현재 출격 체력 상태
        private SoundEventRouter router; // 단일 성공 알림 경로
        private ISoundCueSink output; // 실제 출력 또는 격리 수신기
        private long occurrence; // 세션 내 발생 식별자
        public SoundSettings Settings // 공유 사운드 설정
        { // 속성 시작
            get; // 현재 설정 조회
            private set; // 내부 설정 연결
        } // 속성 종료
        public bool LoadSucceeded // 초기 설정 읽기 결과
        { // 속성 시작
            get; // 읽기 결과 조회
            private set; // 내부 결과 보관
        } // 속성 종료
        public bool HasSceneReferences => musicSource != null && combatSource != null && skillSource != null && uiSource != null && musicSource != combatSource && musicSource != skillSource && musicSource != uiSource && combatSource != skillSource && combatSource != uiSource && skillSource != uiSource; // 네 개의 독립 출력 확인
        private sealed class MemoryStore : ISoundSettingsStore // 자동 시험용 독립 저장소
        { // 저장소 시작
            private string value; // 시험 설정 원문
            public string Read() // 시험 저장 조회
            { // 조회 시작
                return value; // 격리 원문 반환
            } // 조회 종료
            public void Write(string next) // 시험 저장 기록
            { // 기록 시작
                value = next; // 격리 원문 보관
            } // 기록 종료
        } // 저장소 종료
        private void Awake() // 실제 씬 오디오 준비
        { // 초기화 시작
            ISoundSettingsStore store = Application.isEditor && Application.isBatchMode ? (ISoundSettingsStore)new MemoryStore() : new PlayerPrefsSoundSettingsStore(); // 자동 시험 저장 격리
            Settings = new SoundSettings(store); // 독립 설정 생성
            LoadSucceeded = Settings.TryLoad(); // 설정 최초 복원
            Settings.Changed += ApplyVolumes; // 사용자 변경 연결
            output = this; // 씬의 실제 재생 대상
            router = new SoundEventRouter(output); // 중복 방지 경로 생성
            CaptureSources(); // 직렬화된 초기 출력 보관
            ApplyVolumes(); // 복원 음량 적용
        } // 초기화 종료
        public bool BindSettings(SoundSettings settings, ISoundCueSink cueSink = null) // 격리 또는 공유 설정 연결
        { // 연결 시작
            if (settings == null || !HasSceneReferences) // 설정과 실제 출력 확인
            { // 거부 시작
                return false; // 불완전한 참조 차단
            } // 거부 종료
            if (!CaptureSources()) // 초기 출력 구성 확인
            { // 거부 시작
                return false; // 잘못된 출력 차단
            } // 거부 종료
            if (Settings != null) // 이전 설정 확인
            { // 해제 시작
                Settings.Changed -= ApplyVolumes; // 이전 설정 연결 해제
            } // 해제 종료
            Settings = settings; // 주입된 설정 공유
            Settings.Changed += ApplyVolumes; // 새 설정 변경 연결
            output = cueSink ?? this; // 격리 또는 실제 수신기
            router = new SoundEventRouter(output); // 새로운 중복 방지 경로
            ApplyVolumes(); // 실제 출력 음량 반영
            return true; // 설정 연결 완료
        } // 연결 종료
        private bool CaptureSources() // Inspector의 원래 출력 상태 보관
        { // 보관 시작
            if (!HasSceneReferences) // 네 출력 확인
            { // 거부 시작
                return false; // 불완전한 출력 방지
            } // 거부 종료
            if (capturedSources != null && capturedSources[0] == musicSource && capturedSources[1] == combatSource && capturedSources[2] == skillSource && capturedSources[3] == uiSource) // 기존 출력 확인
            { // 재사용 시작
                return true; // 이미 보관한 원래 설정 유지
            } // 재사용 종료
            capturedSources = new AudioSource[] // 편집된 출력기 목록
            { // 목록 시작
                musicSource, combatSource, skillSource, uiSource // 네 가지 실제 출력
            }; // 목록 종료
            baseVolumes = new float[capturedSources.Length]; // 원래 음량 목록
            baseMutes = new bool[capturedSources.Length]; // 원래 음소거 목록
            for (int index = 0; index < capturedSources.Length; index++) // 각 출력기 순회
            { // 기록 시작
                baseVolumes[index] = capturedSources[index].volume; // 편집된 기본 음량
                baseMutes[index] = capturedSources[index].mute; // 편집된 기본 음소거
            } // 기록 종료
            return true; // 초기 출력 보관 완료
        } // 보관 종료
        private void ApplyVolumes() // 설정 변경 시 실제 출력 반영
        { // 적용 시작
            if (Settings == null || !CaptureSources()) // 설정과 출력 확인
            { // 생략 시작
                return; // 불완전 출력에 접근 방지
            } // 생략 종료
            float master = Settings.Get(SoundChannel.Master) / 100f; // 전체 음량 비율
            SoundChannel[] channels = new SoundChannel[] // 출력 순서의 설정 범주
            { // 목록 시작
                SoundChannel.Music, SoundChannel.Combat, SoundChannel.Skill, SoundChannel.UserInterface // 네 출력 범주
            }; // 목록 종료
            for (int index = 0; index < capturedSources.Length; index++) // 각 범주 순회
            { // 적용 시작
                float gain = Settings.Get(channels[index]) / 100f; // 현재 범주 음량
                capturedSources[index].volume = baseVolumes[index] * master * gain; // 편집 음량과 두 설정 적용
                capturedSources[index].mute = baseMutes[index] || master <= 0f || gain <= 0f; // 기존 음소거와 0 유지
            } // 적용 종료
        } // 적용 종료
        public void BindBattle(BattlePlayer target) // 실제 출격 성공 신호 연결
        { // 연결 시작
            UnbindBattle(); // 이전 출격과 중복 구독 제거
            if (target == null) // 플레이어 확인
            { // 생략 시작
                return; // 연결할 출격 없음
            } // 생략 종료
            battle = target; // 현재 출격 보관
            battle.BattleRestarted += RebindSurvival; // 체력 교체 연결
            battle.TutorialAction += OnTutorialAction; // 실제 성공 행동 연결
            battle.EnemyDefeated += OnEnemyDefeated; // 실제 적 처치 연결
            RebindSurvival(); // 현재 체력에 피해 연결
        } // 연결 종료
        public void UnbindBattle() // 이전 출격 성공 신호 해제
        { // 해제 시작
            if (survival != null) // 이전 체력 확인
            { // 해제 시작
                survival.Damaged -= OnDamaged; // 피해 알림 해제
                survival = null; // 이전 체력 참조 해제
            } // 해제 종료
            if (battle != null) // 이전 플레이어 확인
            { // 해제 시작
                battle.BattleRestarted -= RebindSurvival; // 재출격 알림 해제
                battle.TutorialAction -= OnTutorialAction; // 행동 알림 해제
                battle.EnemyDefeated -= OnEnemyDefeated; // 처치 알림 해제
                battle = null; // 이전 플레이어 참조 해제
            } // 해제 종료
        } // 해제 종료
        private void RebindSurvival() // 재출격 시 체력 구독 교체
        { // 교체 시작
            if (survival != null) // 이전 체력 확인
            { // 해제 시작
                survival.Damaged -= OnDamaged; // 이전 피해 구독 해제
            } // 해제 종료
            survival = battle != null ? battle.Survival : null; // 현재 체력 조회
            if (survival != null) // 현재 체력 확인
            { // 연결 시작
                survival.Damaged += OnDamaged; // 실제 피해 알림 연결
            } // 연결 종료
        } // 교체 종료
        private void OnDamaged() // 실제 체력 피해 성공
        { // 알림 시작
            Notify(SoundCue.Hit); // 피해 효과 요청
        } // 알림 종료
        private void OnEnemyDefeated(EnemyRank rank, Vector2 position) // 실제 적 처치 성공
        { // 알림 시작
            Notify(SoundCue.Defeat); // 처치 효과 요청
        } // 알림 종료
        private void OnTutorialAction(TutorialSignal signal) // 실제 전투 행동 성공
        { // 확인 시작
            if (signal == TutorialSignal.Graze) // 실제 그레이즈 확인
            { // 그레이즈 시작
                Notify(SoundCue.Graze); // 그레이즈 효과 요청
            } // 그레이즈 종료
            else if (signal == TutorialSignal.Skill) // 실제 스킬 확인
            { // 스킬 시작
                Notify(SoundCue.Skill); // 스킬 효과 요청
            } // 스킬 종료
            else if (signal == TutorialSignal.Ultimate) // 실제 필살기 확인
            { // 필살기 시작
                Notify(SoundCue.Ultimate); // 필살기 효과 요청
            } // 필살기 종료
        } // 확인 종료
        private bool Notify(SoundCue cue) // 실제 성공 발생의 단일 요청
        { // 요청 시작
            if (router == null) // 초기화 여부 확인
            { // 생략 시작
                return false; // 수신기 없는 요청 차단
            } // 생략 종료
            occurrence = occurrence == long.MaxValue ? 1 : occurrence + 1; // 세션 내 발생 식별자
            return router.TryNotify(cue, occurrence, true); // 성공 발생을 한 번 전달
        } // 요청 종료
        public bool TryPlayUi(SoundCue cue) // 확인과 취소 성공 전달
        { // 요청 시작
            if (cue != SoundCue.Confirm && cue != SoundCue.Cancel) // UI 범주 확인
            { // 거부 시작
                return false; // 다른 종류 요청 차단
            } // 거부 종료
            return Notify(cue); // 확인된 UI 성공 요청
        } // 요청 종료
        public bool TryPlay(SoundCue cue) // 실제 효과음 출력 요청
        { // 요청 시작
            AudioSource source = null; // 해당 범주 출력기
            AudioClip clip = null; // 실제 편집 음원
            switch (cue) // 효과 종류 확인
            { // 분기 시작
                case SoundCue.Shot: // 기본 사격
                    source = combatSource; // 전투 출력 선택
                    clip = shotClip; // 편집된 사격 음원
                    break; // 분기 종료
                case SoundCue.Hit: // 실제 피해
                    source = combatSource; // 전투 출력 선택
                    clip = hitClip; // 편집된 피해 음원
                    break; // 분기 종료
                case SoundCue.Graze: // 그레이즈
                    source = combatSource; // 전투 출력 선택
                    clip = grazeClip; // 편집된 그레이즈 음원
                    break; // 분기 종료
                case SoundCue.Defeat: // 적 처치
                    source = combatSource; // 전투 출력 선택
                    clip = defeatClip; // 편집된 처치 음원
                    break; // 분기 종료
                case SoundCue.Skill: // 고유 및 공용 스킬
                    source = skillSource; // 스킬 출력 선택
                    clip = skillClip; // 편집된 스킬 음원
                    break; // 분기 종료
                case SoundCue.Ultimate: // 필살기
                    source = skillSource; // 스킬 출력 선택
                    clip = ultimateClip; // 편집된 필살기 음원
                    break; // 분기 종료
                case SoundCue.Confirm: // UI 확인
                    source = uiSource; // UI 출력 선택
                    clip = confirmClip; // 편집된 확인 음원
                    break; // 분기 종료
                case SoundCue.Cancel: // UI 취소
                    source = uiSource; // UI 출력 선택
                    clip = cancelClip; // 편집된 취소 음원
                    break; // 분기 종료
            } // 분기 종료
            if (source == null || clip == null || !source.isActiveAndEnabled) // 음원과 출력 확인
            { // 생략 시작
                return false; // 실제 재생 요청 없음
            } // 생략 종료
            source.PlayOneShot(clip); // 설정된 실제 음원 재생 요청
            return true; // 출력 요청 완료만 표시
        } // 요청 종료
        public bool TryPlayMusic(AudioClip clip) // 편집된 배경 음악 요청
        { // 요청 시작
            if (musicSource == null || clip == null || !musicSource.isActiveAndEnabled) // 음악과 출력 확인
            { // 생략 시작
                return false; // 없는 음악 재생 방지
            } // 생략 종료
            if (musicSource.clip == clip && musicSource.isPlaying) // 같은 음악 재생 확인
            { // 유지 시작
                return true; // 중복 시작 방지
            } // 유지 종료
            musicSource.clip = clip; // 요청된 음악 연결
            musicSource.Play(); // 실제 음악 재생 요청
            return true; // 출력 요청 완료만 표시
        } // 요청 종료
        public void StopMusic() // 이전 장면 음악 정리
        { // 정리 시작
            if (musicSource != null) // 실제 음악 출력 확인
            { // 정지 시작
                musicSource.Stop(); // 이전 음악 중복 재생 방지
            } // 정지 종료
        } // 정리 종료
        public bool PlayMenuMusic() // 메뉴 음악 선택
        { // 선택 시작
            return TryPlayMusic(menuMusic); // 편집된 메뉴 음악 요청
        } // 선택 종료
        public bool PlayBattleMusic() // 전투 음악 선택
        { // 선택 시작
            return TryPlayMusic(battleMusic); // 편집된 전투 음악 요청
        } // 선택 종료
        public bool PlayBossMusic() // 보스 음악 선택
        { // 선택 시작
            return TryPlayMusic(bossMusic); // 편집된 보스 음악 요청
        } // 선택 종료
        public bool PlayResultMusic() // 결과 음악 선택
        { // 선택 시작
            return TryPlayMusic(resultMusic); // 편집된 결과 음악 요청
        } // 선택 종료
        private void OnDisable() // 씬 오디오 비활성 처리
        { // 정리 시작
            UnbindBattle(); // 이전 출격 구독 해제
        } // 정리 종료
        private void OnDestroy() // 씬 오디오 제거
        { // 정리 시작
            UnbindBattle(); // 전투 알림 해제
            if (Settings != null) // 기존 설정 확인
            { // 해제 시작
                Settings.Changed -= ApplyVolumes; // 음량 변경 알림 해제
            } // 해제 종료
        } // 정리 종료
    } // 구성 종료
} // 영역 종료
