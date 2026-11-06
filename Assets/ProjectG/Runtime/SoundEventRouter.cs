using System; // 예외 보호 기능
using System.Collections.Generic; // 중복 기록 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum SoundCue // 실제 행동별 재생 요청
    { // 종류 시작
        Shot, Hit, Graze, Defeat, Skill, Ultimate, Confirm, Cancel // 초기 효과음 후보
    } // 종류 종료
    public interface ISoundCueSink // 격리 가능한 실제 재생 대상
    { // 대상 시작
        bool TryPlay(SoundCue cue); // 참조된 음원의 재생 요청
    } // 대상 종료
    public sealed class SoundEventRouter // 실제 성공 행동의 중복 방지
    { // 라우터 시작
        private const int RetainedEvents = 256; // 최근 발생 기록 상한
        private readonly ISoundCueSink sink; // 주입된 실제 재생 대상
        private readonly HashSet<string> seen = new HashSet<string>(); // 최근 발생 식별자
        private readonly Queue<string> order = new Queue<string>(); // 오래된 발생 순서
        public SoundEventRouter(ISoundCueSink cueSink) // 격리 가능한 라우터 생성
        { // 생성 시작
            sink = cueSink ?? throw new ArgumentNullException(nameof(cueSink)); // 재생 대상 확인
        } // 생성 종료
        public bool TryNotify(SoundCue cue, long occurrenceId, bool succeeded) // 확인된 실제 행동 전달
        { // 전달 시작
            if (!succeeded || !Enum.IsDefined(typeof(SoundCue), cue) || occurrenceId <= 0) // 성공과 식별자 확인
            { // 거부 시작
                return false; // 실패 입력과 잘못된 요청 차단
            } // 거부 종료
            string key = ((int)cue).ToString() + ":" + occurrenceId.ToString(); // 범주와 발생 식별자 조합
            if (!seen.Add(key)) // 같은 발생의 중복 확인
            { // 중복 시작
                return false; // 중복 재생 차단
            } // 중복 종료
            order.Enqueue(key); // 최근 발생 순서 보관
            if (order.Count > RetainedEvents) // 기록 상한 확인
            { // 정리 시작
                seen.Remove(order.Dequeue()); // 가장 오래된 기록 제거
            } // 정리 종료
            try // 오디오 예외 보호
            { // 재생 시작
                return sink.TryPlay(cue); // 참조된 실제 음원만 요청
            } // 재생 종료
            catch (Exception) // 음원 또는 장치 예외
            { // 오류 시작
                return false; // 전투 흐름 보존
            } // 오류 종료
        } // 전달 종료
    } // 라우터 종료
} // 영역 종료
