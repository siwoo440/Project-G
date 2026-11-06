using System; // 저장 오류 재현
using System.Collections; // 실제 프레임 시험
using System.Reflection; // 실제 구독 확인
using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 전투와 사운드
using UnityEngine; // 유니티 객체 기능
using UnityEngine.InputSystem; // 격리 입력 장치
using UnityEngine.InputSystem.LowLevel; // 실제 키와 포인터 상태
using UnityEngine.SceneManagement; // 실제 씬 전환
using UnityEngine.TestTools; // 플레이 시험 기능
using UnityEngine.UI; // 편집 가능한 UI 요소
public class SoundSettingsPanelPlayTests // 실제 씬 음량 모달 시험
{ // 시험 시작
    private sealed class MemoryStore : ISoundSettingsStore // 실제 파일과 격리된 저장소
    { // 저장소 시작
        public string Value; // 시험용 저장 원문
        public bool Fail; // 저장 실패 여부
        public string Read() // 저장 원문 조회
        { // 조회 시작
            return Value; // 격리 저장 원문 반환
        } // 조회 종료
        public void Write(string value) // 격리 음량 저장
        { // 기록 시작
            if (Fail) // 저장 실패 확인
            { // 오류 시작
                throw new InvalidOperationException("시험 저장 실패"); // 실제 실패 재현
            } // 오류 종료
            Value = value; // 격리 원문 기록
        } // 기록 종료
    } // 저장소 종료
    private Keyboard keyboard; // 격리 키보드
    private Mouse mouse; // 격리 포인터
    private GameSessionHost host; // 실제 메뉴 관리자
    private SoundSettingsPanel panel; // 실제 씬 음량 화면
    private SceneSoundPlayer sound; // 실제 씬 오디오
    private PauseSettingsOverlay overlay; // 기존 설정 모달
    private MemoryStore store; // 실제 파일과 격리된 저장소
    private float oldScale; // 이전 시간 배율
    private InputSettings.EditorInputBehaviorInPlayMode oldInput; // 이전 편집기 입력 설정
    private InputSettings.BackgroundBehavior oldBackground; // 이전 백그라운드 입력 설정
    [UnitySetUp] // 실제 씬과 격리 입력 준비
    public IEnumerator Setup() // 시험 환경 초기화
    { // 준비 시작
        oldScale = Time.timeScale; // 기존 시간 보관
        Time.timeScale = 1f; // 정상 전투 시간
        oldInput = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력 보관
        oldBackground = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 입력 보관
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 비대화형 입력 허용
        keyboard = InputSystem.AddDevice<Keyboard>(); // 격리 가상 키보드
        mouse = InputSystem.AddDevice<Mouse>(); // 격리 가상 포인터
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 메뉴 씬 열기
        yield return null; // 실제 메뉴 초기화
        host = UnityEngine.Object.FindFirstObjectByType<GameSessionHost>(); // 실제 출격 관리자
        Assert.That(host, Is.Not.Null); // 실제 관리자 확인
        overlay = host.GetComponent<PauseSettingsOverlay>(); // 기존 설정 모달
        Scene menu = SceneManager.GetSceneByName("SampleScene"); // 실제 메뉴 씬
        foreach (GameObject root in menu.GetRootGameObjects()) // 실제 씬 루트 순회
        { // 확인 시작
            SoundSettingsPanel candidate = root.GetComponentInChildren<SoundSettingsPanel>(true); // 실제 숨겨진 음량 화면
            SceneSoundPlayer candidateSound = root.GetComponentInChildren<SceneSoundPlayer>(true); // 실제 오디오 루트
            if (candidate != null) // 실제 화면 확인
            { // 보관 시작
                panel = candidate; // 실제 패널 보관
            } // 보관 종료
            if (candidateSound != null) // 실제 오디오 확인
            { // 보관 시작
                sound = candidateSound; // 실제 오디오 보관
            } // 보관 종료
        } // 확인 종료
        Assert.That(panel, Is.Not.Null); // 실제 씬 패널 확인
        Assert.That(sound != null && sound.HasSceneReferences, Is.True); // 실제 네 출력 참조 확인
        store = new MemoryStore(); // 사용자 파일과 격리된 저장소
        Assert.That(sound.BindSettings(new SoundSettings(store)), Is.True); // 격리 음량 설정 주입
    } // 준비 종료
    private IEnumerator Press(Key key) // 실제 키 한 번 입력
    { // 입력 시작
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 실제 키 누르기
        yield return null; // 입력 처리 프레임
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 실제 키 해제
        yield return null; // 해제 처리 프레임
    } // 입력 종료
    private IEnumerator OpenSoundFromSettings() // 기존 설정에서 실제 패널 열기
    { // 열기 시작
        yield return null; // 기존 설정 열린 프레임 종료
        for (int index = 0; index < 5; index++) // 사운드 항목 이동
        { // 이동 시작
            yield return Press(Key.DownArrow); // 다음 설정 항목
        } // 이동 종료
        yield return Press(Key.Enter); // 실제 음량 화면 진입
        Assert.That(panel.IsOpen, Is.True); // 실제 씬 패널 표시
        Assert.That(overlay.IsPaused, Is.True); // 기존 모달 시간 소유 유지
    } // 열기 종료
    [UnityTest] // 메뉴에서 키와 마우스 및 저장 실패 시험
    public IEnumerator MenuPanelAdjustsOnceAndWarnsOnSaveFailure() // 실제 음량 조절 확인
    { // 시험 시작
        Assert.That(overlay.OpenSettingsFromMenu(), Is.True); // 실제 메뉴 설정 진입
        yield return OpenSoundFromSettings(); // 실제 씬 음량 화면 열기
        Assert.That(sound.Settings.Get(SoundChannel.Master), Is.EqualTo(80)); // 기획서 시험 기본값
        yield return Press(Key.RightArrow); // 실제 키보드 5% 증가
        Assert.That(sound.Settings.Get(SoundChannel.Master), Is.EqualTo(85)); // 중복 없는 한 단계 변경
        Assert.That(store.Value, Is.Not.Null); // 격리 저장 성공
        Canvas.ForceUpdateCanvases(); // 실제 편집 위치 확정
        Button plus = panel.transform.Find("Viewport/Panel/Volume0/Increase").GetComponent<Button>(); // 실제 씬 증가 버튼
        Vector3 point = plus.transform.TransformPoint(((RectTransform)plus.transform).rect.center); // 실제 버튼 중심
        MouseState click = new MouseState(); // 가상 포인터 상태
        click.position = RectTransformUtility.WorldToScreenPoint(null, point); // 화면 기준 실제 버튼 중심
        InputSystem.QueueStateEvent(mouse, click.WithButton(MouseButton.Left)); // 실제 버튼 누르기
        yield return null; // 포인터 입력 처리
        InputSystem.QueueStateEvent(mouse, new MouseState()); // 포인터 버튼 해제
        yield return null; // 해제 처리 대기
        Assert.That(sound.Settings.Get(SoundChannel.Master), Is.EqualTo(90)); // 실제 포인터 한 번 처리
        store.Fail = true; // 저장 오류 유도
        yield return Press(Key.RightArrow); // 실제 실패 음량 변경 요청
        Assert.That(sound.Settings.Get(SoundChannel.Master), Is.EqualTo(90)); // 적용 값 복원
        Assert.That(panel.transform.Find("Viewport/Panel/SaveHint").GetComponent<Text>().text, Does.Contain("저장 실패")); // 실제 오류 안내
        yield return Press(Key.Escape); // 음량 화면 뒤로
        Assert.That(panel.IsOpen, Is.False); // 실제 씬 패널 숨김
        Assert.That(overlay.IsPaused, Is.True); // 기존 설정 화면으로 복귀
        yield return Press(Key.Escape); // 실제 메뉴 설정 닫기
        Assert.That(overlay.IsPaused, Is.False); // 기존 모달 닫힘
        Assert.That(Time.timeScale, Is.EqualTo(1f)); // 원래 시간 배율 복원
    } // 시험 종료
    [UnityTest] // 전투 일시정지와 재출격 시험
    public IEnumerator BattlePanelKeepsPauseAndRebindsAudioOnRetry() // 실제 전투 모달과 오디오 수명
    { // 시험 시작
        Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 실제 캐릭터 선택
        Assert.That(host.Flow.OpenPreparation(), Is.True); // 실제 출격 준비
        Assert.That(host.Flow.StartRun(), Is.True); // 실제 전투 시작 요청
        yield return BattleSceneTestWait.UntilReady(host); // 실제 씬 시작 대기
        BattlePlayer first = host.CurrentPlayer; // 첫 출격 플레이어
        UpgradeDropManager drops = host.CurrentStage.GetComponent<UpgradeDropManager>(); // 실제 전투 드롭 관리자
        Assert.That(drops.TryDrop(EnemyRank.MidBoss, new Vector2(4f, 3f)), Is.True); // 실제 필드 강화 아이템 생성
        PropertyInfo labels = typeof(UpgradeDropManager).GetProperty("CanDisplayHud"); // 실제 아이템 문구 표시 조건
        Assert.That(labels, Is.Not.Null); // 실제 모달 표시 조건 확인
        Assert.That((bool)labels.GetValue(drops), Is.True); // 일반 전투 아이템 문구 표시
        Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1)); // 실제 단일 리스너
        yield return Press(Key.Escape); // 실제 전투 일시정지
        Assert.That(overlay.IsPaused, Is.True); // 전투 정지 확인
        yield return Press(Key.DownArrow); // 설정 항목 선택
        yield return Press(Key.Enter); // 기존 설정 화면 진입
        yield return OpenSoundFromSettings(); // 실제 음량 패널 열기
        Assert.That(host.CurrentStage.IsHudVisible, Is.False); // 사운드 모달 위 스테이지 문구 차단
        Assert.That((bool)labels.GetValue(drops), Is.False); // 사운드 모달 위 아이템 문구 차단
        yield return Press(Key.RightArrow); // 실제 음량 조절
        Assert.That(sound.Settings.Get(SoundChannel.Master), Is.EqualTo(85)); // 전투 중 음량 변경
        Assert.That(Time.timeScale, Is.Zero); // 음량 설정 중 전투 정지 유지
        yield return Press(Key.Escape); // 음량 화면 닫기
        Assert.That(overlay.IsPaused, Is.True); // 기존 설정 유지
        Assert.That((bool)labels.GetValue(drops), Is.False); // 기존 설정 모달 위 아이템 문구 차단
        overlay.Resume(); // 실제 설정과 일시정지 닫기
        Assert.That((bool)labels.GetValue(drops), Is.True); // 모달 종료 후 아이템 문구 복원
        Assert.That(drops.ActiveCount, Is.EqualTo(1)); // 모달 전후 실제 아이템 보존
        Assert.That(overlay.TryPause(), Is.True); // 실제 재출격을 위한 일시정지
        Assert.That(host.Flow.RetryFromPause(), Is.True); // 실제 재출격 요청
        yield return BattleSceneTestWait.UntilReady(host); // 실제 새 씬 대기
        Assert.That(host.CurrentPlayer, Is.Not.SameAs(first)); // 새로운 플레이어 연결
        FieldInfo field = typeof(SceneSoundPlayer).GetField("battle", BindingFlags.NonPublic | BindingFlags.Instance); // 실제 음향 구독 상태
        Assert.That(field.GetValue(sound), Is.SameAs(host.CurrentPlayer)); // 새 출격 음향 연결
        Assert.That(panel.IsOpen, Is.False); // 이전 설정 화면 정리
        Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1)); // 재출격 후 단일 리스너
    } // 시험 종료
    [UnityTearDown] // 성공과 실패 후 사용자 환경 복원
    public IEnumerator Teardown() // 실제 씬과 입력 정리
    { // 정리 시작
        if (panel != null) // 실제 음량 패널 확인
        { // 닫기 시작
            panel.Dismiss(); // 실패 시 남은 모달 정리
        } // 닫기 종료
        if (host != null) // 실제 관리자 확인
        { // 제거 시작
            UnityEngine.Object.Destroy(host.gameObject); // 실제 메뉴와 전투 종료
        } // 제거 종료
        yield return null; // 실제 호스트 파괴 대기
        float deadline = Time.realtimeSinceStartup + 15f; // 시간 배율과 무관한 제한
        Scene battle = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 실제 전투 씬 조회
        while (battle.IsValid() && battle.isLoaded && Time.realtimeSinceStartup < deadline) // 실제 언로드 대기
        { // 대기 시작
            yield return null; // 실제 다음 프레임
            battle = SceneManager.GetSceneByPath("Assets/Scenes/BattleScene.unity"); // 씬 상태 재조회
        } // 대기 종료
        if (keyboard != null && keyboard.added) // 가상 키보드 확인
        { // 제거 시작
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
        } // 제거 종료
        if (mouse != null && mouse.added) // 가상 포인터 확인
        { // 제거 시작
            InputSystem.RemoveDevice(mouse); // 시험 포인터 제거
        } // 제거 종료
        InputSystem.settings.editorInputBehaviorInPlayMode = oldInput; // 기존 입력 설정 복원
        InputSystem.settings.backgroundBehavior = oldBackground; // 기존 포커스 설정 복원
        Time.timeScale = oldScale; // 기존 시간 배율 복원
        Assert.That(!battle.IsValid() || !battle.isLoaded, Is.True); // 시험 전투 씬 정리 확인
    } // 정리 종료
} // 시험 종료
