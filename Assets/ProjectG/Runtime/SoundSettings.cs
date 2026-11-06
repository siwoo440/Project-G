using System; // 설정 변경 기능
using System.Globalization; // 숫자 변환 기능
using UnityEngine; // 유니티 설정 저장 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum SoundChannel // 사운드 음량 범주
    { // 범주 시작
        Master, Music, Combat, Skill, UserInterface // 전체와 네 가지 하위 음량
    } // 범주 종료
    public interface ISoundSettingsStore // 격리 가능한 음량 저장소
    { // 저장소 시작
        string Read(); // 저장된 음량 원문 조회
        void Write(string value); // 음량 원문 저장 요청
    } // 저장소 종료
    public sealed class PlayerPrefsSoundSettingsStore : ISoundSettingsStore // 실제 음량 설정 저장소
    { // 저장소 시작
        private const string Key = "ProjectG.Sound.Values.v1"; // 독립 음량 저장 키
        public string Read() // 저장된 음량 조회
        { // 조회 시작
            return PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetString(Key) : null; // 기존 원문 또는 없음
        } // 조회 종료
        public void Write(string value) // 음량 저장 요청
        { // 저장 시작
            PlayerPrefs.SetString(Key, value); // 다른 설정과 분리된 단일 기록
            PlayerPrefs.Save(); // 설정 저장 요청
        } // 저장 종료
    } // 저장소 종료
    public sealed class SoundSettings // 시험값 기반 음량 설정
    { // 설정 시작
        private readonly ISoundSettingsStore store; // 주입된 음량 저장소
        private readonly int[] levels = new int[] // 기획서의 시험 기본값
        { // 목록 시작
            80, 70, 85, 85, 75 // 전체와 네 가지 하위 음량
        }; // 목록 종료
        public event Action Changed; // 실제 값 변경 알림
        public SoundSettings(ISoundSettingsStore settingsStore) // 시험 가능한 설정 생성
        { // 생성 시작
            store = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore)); // 저장소 확인
        } // 생성 종료
        public int Get(SoundChannel channel) // 현재 음량 조회
        { // 조회 시작
            int index = (int)channel; // 범주 번호
            if (index < 0 || index >= levels.Length) // 범주 유효성 확인
            { // 오류 시작
                throw new ArgumentOutOfRangeException(nameof(channel)); // 잘못된 범주 차단
            } // 오류 종료
            return levels[index]; // 현재 음량 반환
        } // 조회 종료
        public bool TrySet(SoundChannel channel, int value) // 지정 음량 선택
        { // 변경 시작
            int index = (int)channel; // 범주 번호
            if (index < 0 || index >= levels.Length || !Valid(value)) // 범주와 음량 확인
            { // 거부 시작
                return false; // 잘못된 값 거부
            } // 거부 종료
            if (levels[index] != value) // 실제 변경 확인
            { // 변경 시작
                levels[index] = value; // 선택 음량 보관
                Changed?.Invoke(); // 실제 변경 알림
            } // 변경 종료
            return true; // 유효한 선택 확인
        } // 변경 종료
        public bool TryStep(SoundChannel channel, int direction) // 한 단계 음량 변경
        { // 단계 시작
            if ((int)channel < 0 || (int)channel >= levels.Length || (direction != -1 && direction != 1)) // 범주와 방향 확인
            { // 거부 시작
                return false; // 잘못된 요청 거부
            } // 거부 종료
            int next = Math.Min(100, Math.Max(0, Get(channel) + direction * 5)); // 경계 안의 다음 음량
            return TrySet(channel, next); // 유효한 음량 적용
        } // 단계 종료
        public bool TrySave() // 현재 음량 저장
        { // 저장 시작
            try // 저장소 보호
            { // 저장 범위
                store.Write("1|" + string.Join("|", Array.ConvertAll(levels, value => value.ToString(CultureInfo.InvariantCulture)))); // 다섯 값을 하나로 저장
                return true; // 저장 요청 완료
            } // 저장 종료
            catch (Exception) // 저장 오류
            { // 오류 시작
                return false; // 저장 실패 전달
            } // 오류 종료
        } // 저장 종료
        public bool TryLoad() // 저장된 음량 복원
        { // 복원 시작
            try // 저장소 보호
            { // 복원 범위
                string raw = store.Read(); // 독립 음량 원문 조회
                if (raw == null) // 저장된 설정 없음
                { // 기본값 시작
                    return true; // 현재 시험 기본값 유지
                } // 기본값 종료
                string[] parts = raw.Split('|'); // 버전과 다섯 값 분리
                if (parts.Length != 6 || parts[0] != "1") // 저장 형식 확인
                { // 거부 시작
                    return false; // 기존 값 보존
                } // 거부 종료
                int[] loaded = new int[levels.Length]; // 검증된 임시 값
                for (int index = 0; index < levels.Length; index++) // 다섯 음량 검사
                { // 검사 시작
                    if (!int.TryParse(parts[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out loaded[index]) || !Valid(loaded[index])) // 숫자와 범위 확인
                    { // 거부 시작
                        return false; // 일부 값만 덮어쓰기 방지
                    } // 거부 종료
                } // 검사 종료
                bool changed = false; // 실제 변경 여부
                for (int index = 0; index < levels.Length; index++) // 검증 값 적용
                { // 적용 시작
                    changed |= levels[index] != loaded[index]; // 변경 여부 기록
                    levels[index] = loaded[index]; // 검증된 음량 보관
                } // 적용 종료
                if (changed) // 실제 값 변경 확인
                { // 알림 시작
                    Changed?.Invoke(); // 복원 결과 알림
                } // 알림 종료
                return true; // 복원 성공
            } // 복원 종료
            catch (Exception) // 읽기 또는 변환 오류
            { // 오류 시작
                return false; // 기존 값과 저장 원문 보존
            } // 오류 종료
        } // 복원 종료
        private static bool Valid(int value) // 음량 수치 검사
        { // 검사 시작
            return value >= 0 && value <= 100 && value % 5 == 0; // 기획 범위와 간격 확인
        } // 검사 종료
    } // 설정 종료
} // 영역 종료
