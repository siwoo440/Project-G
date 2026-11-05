using System; // 문자열 비교 기능
using System.Collections.Generic; // 임시 설정 저장소
using NUnit.Framework; // 테스트 도구
using ProjectG; // 조작 설정 기능
public class KeyBindingSettingsTests // 키 설정 시험
{ // 시험 시작
    private sealed class MemoryStore : IKeyBindingStore // 격리된 시험 저장소
    { // 저장소 시작
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(); // 저장 값 목록
        public string Get(string key, string fallback) // 저장 값 조회
        { // 조회 시작
            return values.TryGetValue(key, out string value) ? value : fallback; // 저장 값 또는 기본값
        } // 조회 종료
        public void Set(string key, string value) // 값 저장
        { // 저장 시작
            values[key] = value; // 시험 값 보관
        } // 저장 종료
        public void Flush() // 저장 완료 처리
        { // 처리 시작
        } // 처리 종료
    } // 저장소 종료
    [Test] // 기본 키와 경로 시험
    public void DefaultsMatchDocumentAndControlPaths() // 기본 조작 확인
    { // 시험 시작
        KeyBindingSettings settings = new KeyBindingSettings(new MemoryStore(), "test."); // 격리된 키 설정
        Assert.That(settings.GetPath(PlayerAction.MoveUp), Is.EqualTo("<Keyboard>/w")); // 위쪽 이동 키
        Assert.That(settings.GetPath(PlayerAction.MoveLeft), Is.EqualTo("<Keyboard>/a")); // 왼쪽 이동 키
        Assert.That(settings.GetPath(PlayerAction.MoveDown), Is.EqualTo("<Keyboard>/s")); // 아래쪽 이동 키
        Assert.That(settings.GetPath(PlayerAction.MoveRight), Is.EqualTo("<Keyboard>/d")); // 오른쪽 이동 키
        Assert.That(settings.GetPath(PlayerAction.Focus), Is.EqualTo("<Keyboard>/leftShift")); // 집중 기본 키
        Assert.That(settings.GetPath(PlayerAction.Unique), Is.EqualTo("<Keyboard>/j")); // 고유 스킬 키
        Assert.That(settings.GetPath(PlayerAction.Common), Is.EqualTo("<Keyboard>/k")); // 공용 스킬 키
        Assert.That(settings.GetPath(PlayerAction.Ultimate), Is.EqualTo("<Keyboard>/l")); // 필살기 키
    } // 시험 종료
    [Test] // 중복과 예약 키 차단 시험
    public void DuplicateReservedAndInvalidControlsAreRejected() // 조작 충돌 방지
    { // 시험 시작
        KeyBindingSettings settings = new KeyBindingSettings(new MemoryStore(), "test."); // 격리된 키 설정
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "leftShift"), Is.False); // 다른 동작 키 중복
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "escape"), Is.False); // 일시정지 키 예약
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "enter"), Is.False); // 메뉴 확인 키 예약
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "numpadEnter"), Is.False); // 보조 확인 키 예약
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "1"), Is.False); // 첫 카드 선택 키 예약
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "2"), Is.False); // 둘째 카드 선택 키 예약
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "3"), Is.False); // 셋째 카드 선택 키 예약
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "f1"), Is.False); // 시험 캐릭터 키 예약
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "<Mouse>/leftButton"), Is.False); // 키보드 외 입력 차단
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "unknownKey"), Is.False); // 존재하지 않는 키 차단
        Assert.That(settings.GetControl(PlayerAction.MoveUp), Is.EqualTo("w")); // 기존 값 유지
    } // 시험 종료
    [Test] // 저장과 복원 시험
    public void SavesAndRestoresIndividualBindingsWithoutChangingOtherKeys() // 저장 값 복원
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 시험 저장소
        store.Set("other.project.value", "keep"); // 무관한 기존 값
        KeyBindingSettings first = new KeyBindingSettings(store, "test."); // 첫 설정 객체
        Assert.That(first.TryBind(PlayerAction.MoveUp, "i"), Is.True); // 위쪽 이동 변경
        Assert.That(first.TryBind(PlayerAction.MoveLeft, "w"), Is.True); // 비워진 키 재사용
        Assert.That(first.TryBind(PlayerAction.MoveUp, "a"), Is.True); // 이동 키 교환 완성
        Assert.That(first.TryBind(PlayerAction.Focus, "<Keyboard>/RightShift"), Is.True); // 대소문자 경로 입력
        first.Save(); // 동작별 키 저장
        KeyBindingSettings loaded = new KeyBindingSettings(store, "test."); // 새 설정 객체
        loaded.Load(); // 저장 값 복원
        Assert.That(loaded.GetControl(PlayerAction.MoveUp), Is.EqualTo("a")); // 교환된 위쪽 키
        Assert.That(loaded.GetControl(PlayerAction.MoveLeft), Is.EqualTo("w")); // 교환된 왼쪽 키
        Assert.That(loaded.GetControl(PlayerAction.Focus), Is.EqualTo("rightShift")); // 집중 키 복원
        Assert.That(store.Get("other.project.value", ""), Is.EqualTo("keep")); // 무관한 값 보존
    } // 시험 종료
    [Test] // 손상된 저장 값 시험
    public void LoadRepairsInvalidAndDuplicateSavedBindings() // 손상 설정 복구
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 시험 저장소
        store.Set("test.MoveUp", "escape"); // 예약된 저장 키
        store.Set("test.MoveLeft", "w"); // 기본키와 겹치는 저장 키
        store.Set("test.MoveDown", "w"); // 중복 저장 키
        KeyBindingSettings settings = new KeyBindingSettings(store, "test."); // 시험 설정 객체
        settings.Load(); // 손상된 값 복원
        HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 중복 확인 목록
        foreach (PlayerAction action in Enum.GetValues(typeof(PlayerAction))) // 모든 동작 순회
        { // 반복 시작
            string control = settings.GetControl(action); // 복원된 키 조회
            Assert.That(settings.IsReserved(control), Is.False); // 예약 키 제외
            Assert.That(keys.Add(control), Is.True); // 중복 키 제외
        } // 반복 종료
    } // 시험 종료
    [Test] // 설정 변경 알림 시험
    public void NotifiesOnlyWhenBindingsActuallyChange() // 실시간 적용 알림
    { // 시험 시작
        KeyBindingSettings settings = new KeyBindingSettings(new MemoryStore(), "test."); // 시험 설정 객체
        int changes = 0; // 알림 횟수
        settings.Changed += () => changes++; // 설정 변경 연결
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "w"), Is.True); // 같은 키 지정
        Assert.That(changes, Is.Zero); // 불필요한 알림 방지
        Assert.That(settings.TryBind(PlayerAction.MoveUp, "i"), Is.True); // 새 키 지정
        Assert.That(changes, Is.EqualTo(1)); // 변경 알림 확인
        settings.ResetDefaults(); // 기본값 복원
        Assert.That(changes, Is.EqualTo(2)); // 복원 알림 확인
    } // 시험 종료
} // 시험 종료
