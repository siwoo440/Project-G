using System; // 설정 변경 알림
using System.Collections.Generic; // 키 목록과 중복 검사
using UnityEngine; // 설정 저장 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum PlayerAction // 키 재설정 대상
    { // 목록 시작
        MoveUp, MoveLeft, MoveDown, MoveRight, // 네 방향 이동
        Focus, Unique, Common, Ultimate // 집중과 세 스킬
    } // 목록 종료
    public interface IKeyBindingStore // 조작 설정 저장소
    { // 인터페이스 시작
        string Get(string key, string fallback); // 저장 값 조회
        void Set(string key, string value); // 설정 값 저장
        void Flush(); // 저장 완료 처리
    } // 인터페이스 종료
    public sealed class PlayerPrefsKeyBindingStore : IKeyBindingStore // 유니티 설정 저장소
    { // 저장소 시작
        public string Get(string key, string fallback) // 저장 키 조회
        { // 조회 시작
            return PlayerPrefs.GetString(key, fallback); // 기존 값 또는 기본값
        } // 조회 종료
        public void Set(string key, string value) // 설정 값 저장
        { // 저장 시작
            PlayerPrefs.SetString(key, value); // 지정 항목만 갱신
        } // 저장 종료
        public void Flush() // 설정 저장 완료
        { // 저장 시작
            PlayerPrefs.Save(); // 변경 값 반영
        } // 저장 종료
    } // 저장소 종료
    public sealed class KeyBindingSettings // 충돌을 막는 조작 키 설정
    { // 클래스 시작
        private static readonly string[] defaults = new string[] // 확정 기본 키
        { // 목록 시작
            "w", "a", "s", "d", // 네 방향 이동 키
            "leftShift", "j", "k", "l" // 집중과 세 스킬 키
        }; // 목록 종료
        private static readonly string[] allowed = new string[] // 재지정 가능한 키 이름
        { // 목록 시작
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", // 알파벳 앞부분
            "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", // 알파벳 뒷부분
            "0", "4", "5", "6", "7", "8", "9", // 카드 선택 외 숫자 키
            "upArrow", "downArrow", "leftArrow", "rightArrow", // 방향 키
            "leftShift", "rightShift", "leftCtrl", "rightCtrl", "space", // 일반 조합 키
            "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", // 시험 선택 외 기능 키
            "numpad0", "numpad1", "numpad2", "numpad3", "numpad4", // 숫자판 앞부분
            "numpad5", "numpad6", "numpad7", "numpad8", "numpad9", // 숫자판 뒷부분
            "numpadPlus", "numpadMinus", "numpadMultiply", "numpadDivide", "numpadPeriod", // 숫자판 기호 키
            "backquote", "quote", "semicolon", "comma", "period", "slash", "backslash", // 문장 기호 키
            "leftBracket", "rightBracket", "minus", "equals", "pageUp", "pageDown", "home", "end", "insert", "delete" // 괄호와 이동 키
        }; // 목록 종료
        private static readonly HashSet<string> reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase) // 고정 메뉴와 선택 키
        { // 목록 시작
            "escape", "enter", "numpadEnter", // 일시정지와 메뉴 확인
            "1", "2", "3", // 레벨업 카드 선택
            "f1", "f2", "tab" // 시험 캐릭터와 메뉴 정보
        }; // 목록 종료
        private readonly string[] bindings = new string[8]; // 현재 동작별 키
        private readonly IKeyBindingStore store; // 사용자 설정 저장소
        private readonly string prefix; // 프로젝트별 저장 접두사
        public event Action Changed; // 실제 조작 변경 알림
        public KeyBindingSettings() : this(new PlayerPrefsKeyBindingStore(), "ProjectG.Keybind.") // 기본 저장소 사용
        { // 생성 시작
        } // 생성 종료
        public KeyBindingSettings(IKeyBindingStore settingsStore, string keyPrefix) // 저장소 주입 생성
        { // 생성 시작
            store = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore)); // 저장소 확인
            prefix = string.IsNullOrEmpty(keyPrefix) ? throw new ArgumentException("설정 접두사가 필요합니다.", nameof(keyPrefix)) : keyPrefix; // 설정 경로 확인
            Array.Copy(defaults, bindings, defaults.Length); // 확정 기본 키 복사
        } // 생성 종료
        public string GetControl(PlayerAction action) // 동작별 키 이름 조회
        { // 조회 시작
            int index = (int)action; // 동작 번호 변환
            if (index < 0 || index >= bindings.Length) // 동작 범위 확인
            { // 예외 시작
                throw new ArgumentOutOfRangeException(nameof(action)); // 잘못된 동작 차단
            } // 예외 종료
            return bindings[index]; // 유니티 키 이름 반환
        } // 조회 종료
        public string GetPath(PlayerAction action) // 동작별 입력 경로 조회
        { // 조회 시작
            return "<Keyboard>/" + GetControl(action); // 유니티 입력 경로 반환
        } // 조회 종료
        public bool IsReserved(string controlName) // 고정 메뉴 키 확인
        { // 검사 시작
            return reserved.Contains(StripPath(controlName)); // 예약 키 여부 반환
        } // 검사 종료
        public bool TryBind(PlayerAction action, string controlName) // 키 재지정 시도
        { // 지정 시작
            int index = (int)action; // 동작 번호 변환
            string control = Canonical(controlName); // 유효한 키 이름 정규화
            if (index < 0 || index >= bindings.Length || control == null) // 입력 범위 확인
            { // 차단 시작
                return false; // 잘못된 입력 거부
            } // 차단 종료
            for (int i = 0; i < bindings.Length; i++) // 다른 동작 순회
            { // 중복 확인 시작
                if (i != index && string.Equals(bindings[i], control, StringComparison.OrdinalIgnoreCase)) // 중복 키 확인
                { // 중복 처리 시작
                    return false; // 조작 충돌 차단
                } // 중복 처리 종료
            } // 중복 확인 종료
            if (!string.Equals(bindings[index], control, StringComparison.Ordinal)) // 실제 변경 확인
            { // 변경 시작
                bindings[index] = control; // 새 키 보관
                Changed?.Invoke(); // 실시간 적용 알림
            } // 변경 종료
            return true; // 유효한 지정 성공
        } // 지정 종료
        public void Save() // 각 동작 키 저장
        { // 저장 시작
            for (int i = 0; i < bindings.Length; i++) // 모든 동작 순회
            { // 순회 시작
                store.Set(prefix + ((PlayerAction)i), bindings[i]); // 해당 동작만 저장
            } // 순회 종료
            store.Flush(); // 저장 완료
        } // 저장 종료
        public void Load() // 각 동작 키 복원
        { // 복원 시작
            string[] values = new string[bindings.Length]; // 복원 후보 목록
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 이미 사용한 키
            for (int i = 0; i < values.Length; i++) // 저장된 동작 순회
            { // 순회 시작
                string value = Canonical(store.Get(prefix + ((PlayerAction)i), "")); // 저장된 키 검사
                if (value != null && used.Add(value)) // 유효한 미사용 키 확인
                { // 보관 시작
                    values[i] = value; // 정상 저장 키 보존
                } // 보관 종료
            } // 순회 종료
            for (int i = 0; i < values.Length; i++) // 복구할 동작 순회
            { // 순회 시작
                if (values[i] != null) // 보존된 키 확인
                { // 생략 시작
                    continue; // 다음 동작 확인
                } // 생략 종료
                string value = !used.Contains(defaults[i]) ? defaults[i] : FirstUnused(used); // 기본 또는 사용 가능한 키
                values[i] = value; // 복구 키 보관
                used.Add(value); // 중복 검사에 추가
            } // 순회 종료
            Apply(values); // 복원한 키 반영
        } // 복원 종료
        public void ResetDefaults() // 확정 기본 조작 복원
        { // 복원 시작
            Apply(defaults); // 기본 키 반영
        } // 복원 종료
        private void Apply(string[] values) // 설정 일괄 반영
        { // 반영 시작
            bool different = false; // 변경 여부
            for (int i = 0; i < bindings.Length; i++) // 모든 동작 순회
            { // 순회 시작
                different |= !string.Equals(bindings[i], values[i], StringComparison.Ordinal); // 변경 여부 누적
                bindings[i] = values[i]; // 키 값 반영
            } // 순회 종료
            if (different) // 실제 변경 확인
            { // 알림 시작
                Changed?.Invoke(); // 실시간 적용 알림
            } // 알림 종료
        } // 반영 종료
        private static string FirstUnused(HashSet<string> used) // 사용하지 않은 안전 키 선택
        { // 선택 시작
            foreach (string value in allowed) // 허용 키 순회
            { // 순회 시작
                if (!used.Contains(value)) // 사용 여부 확인
                { // 선택 시작
                    return value; // 첫 사용 가능 키
                } // 선택 종료
            } // 순회 종료
            throw new InvalidOperationException("사용 가능한 키가 없습니다."); // 예기치 않은 키 부족
        } // 선택 종료
        private static string Canonical(string controlName) // 입력 키 정규화
        { // 정규화 시작
            string control = StripPath(controlName); // 키보드 접두사 제거
            if (reserved.Contains(control)) // 예약 키 확인
            { // 예약 처리 시작
                return null; // 메뉴 안전 키 제외
            } // 예약 처리 종료
            foreach (string value in allowed) // 허용 키 순회
            { // 순회 시작
                if (string.Equals(value, control, StringComparison.OrdinalIgnoreCase)) // 키 이름 비교
                { // 일치 처리 시작
                    return value; // 정규 이름 반환
                } // 일치 처리 종료
            } // 순회 종료
            return null; // 잘못된 키 제외
        } // 정규화 종료
        private static string StripPath(string value) // 키보드 경로 접두사 제거
        { // 처리 시작
            string name = value != null ? value.Trim() : ""; // 빈 값과 공백 처리
            return name.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ? name.Substring(11) : name; // 순수 키 이름 반환
        } // 처리 종료
    } // 클래스 종료
} // 영역 종료
