using System; // 인자 오류 기능
using UnityEngine; // 유니티 설정 저장 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public interface IGamePreferenceStore // 설정 저장소 규약
    { // 규약 시작
        int GetInt(string key, int fallback); // 정수 설정 조회
        void SetInt(string key, int value); // 정수 설정 기록
        void Save(); // 설정 저장
    } // 규약 종료
    public sealed class UnityGamePreferenceStore : IGamePreferenceStore // 유니티 설정 저장소
    { // 저장소 시작
        public int GetInt(string key, int fallback) // 정수 설정 조회
        { // 조회 시작
            return PlayerPrefs.GetInt(key, fallback); // 기존 설정 또는 기본값
        } // 조회 종료
        public void SetInt(string key, int value) // 정수 설정 기록
        { // 기록 시작
            PlayerPrefs.SetInt(key, value); // 해당 설정만 변경
        } // 기록 종료
        public void Save() // 설정 저장
        { // 저장 시작
            PlayerPrefs.Save(); // 설정 파일 저장
        } // 저장 종료
    } // 저장소 종료
    public sealed class GameSettings // 실제 동작하는 조작 설정
    { // 설정 시작
        public const string FocusToggleKey = "ProjectG.FocusToggle"; // 집중 방식 저장 키
        public const string HitboxNormalKey = "ProjectG.HitboxNormal"; // 일반 표시 저장 키
        public const string HitboxFocusKey = "ProjectG.HitboxFocus"; // 집중 표시 저장 키
        private readonly IGamePreferenceStore store; // 설정 저장소
        public GameSettings() : this(new UnityGamePreferenceStore()) // 유니티 저장소 사용
        { // 생성 시작
        } // 생성 종료
        public GameSettings(IGamePreferenceStore preferences) // 설정 저장소 주입
        { // 생성 시작
            store = preferences ?? throw new ArgumentNullException(nameof(preferences)); // 저장소 보관
        } // 생성 종료
        public bool FocusToggle => store.GetInt(FocusToggleKey, 0) == 1; // 집중 토글 여부
        public bool HitboxNormal => store.GetInt(HitboxNormalKey, 0) == 1; // 일반 피격 표시
        public bool HitboxFocus => store.GetInt(HitboxFocusKey, 1) == 1; // 집중 피격 표시
        public void SetFocusToggle(bool enabled) // 집중 방식 변경
        { // 변경 시작
            Set(FocusToggleKey, enabled); // 홀드 또는 토글 저장
        } // 변경 종료
        public void SetHitboxNormal(bool enabled) // 일반 피격 표시 변경
        { // 변경 시작
            Set(HitboxNormalKey, enabled); // 일반 표시 설정 저장
        } // 변경 종료
        public void SetHitboxFocus(bool enabled) // 집중 피격 표시 변경
        { // 변경 시작
            Set(HitboxFocusKey, enabled); // 집중 표시 설정 저장
        } // 변경 종료
        public void RestoreControlDefaults() // 확정 조작 표시 기본값 복원
        { // 복원 시작
            store.SetInt(FocusToggleKey, 0); // 집중 홀드 기본값
            store.SetInt(HitboxNormalKey, 0); // 일반 표시 끄기
            store.SetInt(HitboxFocusKey, 1); // 집중 표시 켜기
            store.Save(); // 변경 설정 저장
        } // 복원 종료
        private void Set(string key, bool value) // 개별 설정 저장
        { // 저장 시작
            store.SetInt(key, value ? 1 : 0); // 선택 값 기록
            store.Save(); // 변경 설정 저장
        } // 저장 종료
    } // 설정 종료
} // 영역 종료
