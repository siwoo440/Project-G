using System.Collections.Generic; // 시험 저장소 기능
using NUnit.Framework; // 테스트 도구
using ProjectG; // 조작 설정 기능
public class GameSettingsTests // 조작 설정 시험
{ // 시험 시작
    private sealed class MemoryPreferences : IGamePreferenceStore // 메모리 시험 저장소
    { // 저장소 시작
        public readonly Dictionary<string, int> Values = new Dictionary<string, int>(); // 시험 설정 목록
        public int Saved; // 저장 호출 횟수
        public int GetInt(string key, int fallback) // 정수 설정 조회
        { // 조회 시작
            return Values.TryGetValue(key, out int value) ? value : fallback; // 기록 값 또는 기본값
        } // 조회 종료
        public void SetInt(string key, int value) // 정수 설정 기록
        { // 기록 시작
            Values[key] = value; // 시험 설정 기록
        } // 기록 종료
        public void Save() // 설정 저장 확인
        { // 저장 시작
            Saved++; // 저장 횟수 증가
        } // 저장 종료
    } // 저장소 종료
    [Test] // 기본 조작 설정 시험
    public void DefaultsMatchFocusAndHitboxRules() // 확정 기본값 확인
    { // 시험 시작
        MemoryPreferences store = new MemoryPreferences(); // 빈 저장소
        GameSettings settings = new GameSettings(store); // 조작 설정 생성
        Assert.That(settings.FocusToggle, Is.False); // 집중 홀드 기본값
        Assert.That(settings.HitboxNormal, Is.False); // 일반 피격 표시 기본값
        Assert.That(settings.HitboxFocus, Is.True); // 집중 피격 표시 기본값
        Assert.That(store.Saved, Is.Zero); // 조회 중 저장하지 않음
    } // 시험 종료
    [Test] // 설정 저장과 복원 시험
    public void ChangesSurviveNewInstanceAndResetPreservesOtherKeys() // 저장과 기본값 확인
    { // 시험 시작
        MemoryPreferences store = new MemoryPreferences(); // 시험 저장소
        store.SetInt("OtherProject.Value", 42); // 다른 설정 보관
        GameSettings settings = new GameSettings(store); // 첫 설정 객체
        settings.SetFocusToggle(true); // 집중 토글 선택
        settings.SetHitboxNormal(true); // 일반 표시 선택
        settings.SetHitboxFocus(false); // 집중 표시 해제
        GameSettings restored = new GameSettings(store); // 새 설정 객체
        Assert.That(restored.FocusToggle, Is.True); // 집중 방식 복원
        Assert.That(restored.HitboxNormal, Is.True); // 일반 표시 복원
        Assert.That(restored.HitboxFocus, Is.False); // 집중 표시 복원
        restored.RestoreControlDefaults(); // 조작 기본값 복원
        Assert.That(restored.FocusToggle, Is.False); // 집중 홀드 복원
        Assert.That(restored.HitboxNormal, Is.False); // 일반 표시 끄기
        Assert.That(restored.HitboxFocus, Is.True); // 집중 표시 켜기
        Assert.That(store.GetInt("OtherProject.Value", 0), Is.EqualTo(42)); // 다른 설정 유지
        Assert.That(store.Saved, Is.EqualTo(4)); // 항목 변경과 복원 저장
    } // 시험 종료
} // 시험 종료
