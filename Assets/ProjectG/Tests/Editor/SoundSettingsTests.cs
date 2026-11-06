using System; // 시험 예외 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 사운드 설정 기능
public class SoundSettingsTests // 음량 설정과 이벤트 시험
{ // 시험 시작
    private sealed class MemoryStore : ISoundSettingsStore // 격리된 음량 저장소
    { // 저장소 시작
        public string Value; // 저장 원문
        public bool FailRead; // 읽기 실패 여부
        public bool FailWrite; // 쓰기 실패 여부
        public string Read() // 격리 저장 조회
        { // 조회 시작
            if (FailRead) // 실패 여부 확인
            { // 실패 시작
                throw new InvalidOperationException(); // 읽기 오류 재현
            } // 실패 종료
            return Value; // 저장 원문 반환
        } // 조회 종료
        public void Write(string value) // 격리 저장 기록
        { // 기록 시작
            if (FailWrite) // 실패 여부 확인
            { // 실패 시작
                throw new InvalidOperationException(); // 기록 오류 재현
            } // 실패 종료
            Value = value; // 격리 저장소에 보관
        } // 기록 종료
    } // 저장소 종료
    private sealed class FakeSink : ISoundCueSink // 오디오 장치를 사용하지 않는 수신기
    { // 수신기 시작
        public int Calls; // 재생 요청 횟수
        public bool Allow = true; // 실제 음원 존재 가정
        public bool TryPlay(SoundCue cue) // 요청 기록
        { // 기록 시작
            Calls++; // 요청 횟수 증가
            return Allow; // 격리 결과 반환
        } // 기록 종료
    } // 수신기 종료
    [Test] // 기획서 시험 기본값
    public void DraftDefaultsAndFivePercentBoundsArePreserved() // 기본값과 경계 검사
    { // 시험 시작
        SoundSettings settings = new SoundSettings(new MemoryStore()); // 격리 음량 설정
        Assert.That(settings.Get(SoundChannel.Master), Is.EqualTo(80)); // 전체 시험 기본값
        Assert.That(settings.Get(SoundChannel.Music), Is.EqualTo(70)); // 음악 시험 기본값
        Assert.That(settings.Get(SoundChannel.Combat), Is.EqualTo(85)); // 전투 시험 기본값
        Assert.That(settings.Get(SoundChannel.Skill), Is.EqualTo(85)); // 스킬 시험 기본값
        Assert.That(settings.Get(SoundChannel.UserInterface), Is.EqualTo(75)); // UI 시험 기본값
        Assert.That(settings.TrySet(SoundChannel.Music, 0), Is.True); // 최소 음량 선택
        Assert.That(settings.TryStep(SoundChannel.Music, -1), Is.True); // 최소 경계 유지
        Assert.That(settings.Get(SoundChannel.Music), Is.Zero); // 음량 하한 확인
        Assert.That(settings.TrySet(SoundChannel.Music, 100), Is.True); // 최대 음량 선택
        Assert.That(settings.TryStep(SoundChannel.Music, 1), Is.True); // 최대 경계 유지
        Assert.That(settings.Get(SoundChannel.Music), Is.EqualTo(100)); // 음량 상한 확인
        Assert.That(settings.TryStep(SoundChannel.Music, -1), Is.True); // 한 단계 감소
        Assert.That(settings.Get(SoundChannel.Music), Is.EqualTo(95)); // 5% 간격 확인
        Assert.That(settings.TrySet(SoundChannel.Music, 94), Is.False); // 간격 외 값 거부
        Assert.That(settings.TrySet((SoundChannel)99, 50), Is.False); // 잘못된 범주 거부
        Assert.That(settings.TryStep(SoundChannel.Music, 2), Is.False); // 잘못된 방향 거부
    } // 시험 종료
    [Test] // 저장과 복원 확인
    public void ValidSettingsRoundTripThroughInjectedStore() // 격리 저장소 왕복
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        SoundSettings original = new SoundSettings(store); // 초기 음량 설정
        original.TrySet(SoundChannel.Master, 0); // 전체 음소거 설정
        original.TrySet(SoundChannel.Music, 100); // 음악 최대 설정
        original.TrySet(SoundChannel.Combat, 30); // 전투 음량 설정
        original.TrySet(SoundChannel.Skill, 40); // 스킬 음량 설정
        original.TrySet(SoundChannel.UserInterface, 50); // UI 음량 설정
        Assert.That(original.TrySave(), Is.True); // 격리 저장 성공
        SoundSettings restored = new SoundSettings(store); // 새 설정 객체
        Assert.That(restored.TryLoad(), Is.True); // 격리 설정 복원
        Assert.That(restored.Get(SoundChannel.Master), Is.Zero); // 전체 복원 확인
        Assert.That(restored.Get(SoundChannel.Music), Is.EqualTo(100)); // 음악 복원 확인
        Assert.That(restored.Get(SoundChannel.Combat), Is.EqualTo(30)); // 전투 복원 확인
        Assert.That(restored.Get(SoundChannel.Skill), Is.EqualTo(40)); // 스킬 복원 확인
        Assert.That(restored.Get(SoundChannel.UserInterface), Is.EqualTo(50)); // UI 복원 확인
    } // 시험 종료
    [TestCase("1|80|70|85|85|74")] // 간격이 다른 저장 값
    [TestCase("1|80|70|85|85|105")] // 범위를 벗어난 저장 값
    [TestCase("1|80|70|85|85")] // 잘린 저장 원문
    [TestCase("2|80|70|85|85|75")] // 알 수 없는 버전
    public void InvalidStorageDoesNotPartiallyOverwriteValues(string raw) // 손상 데이터 전체 거부
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        store.Value = raw; // 손상 원문 설정
        SoundSettings settings = new SoundSettings(store); // 시험 기본값
        Assert.That(settings.TryLoad(), Is.False); // 손상 원문 거부
        Assert.That(settings.Get(SoundChannel.Master), Is.EqualTo(80)); // 기존 전체 음량 보존
        Assert.That(settings.Get(SoundChannel.UserInterface), Is.EqualTo(75)); // 기존 UI 음량 보존
        Assert.That(store.Value, Is.EqualTo(raw)); // 기존 원문 무단 변경 방지
    } // 시험 종료
    [Test] // 저장소 예외 격리 시험
    public void StorageErrorsDoNotChangeGameStateOrClaimSuccess() // 읽기와 기록 오류
    { // 시험 시작
        MemoryStore store = new MemoryStore(); // 격리 저장소
        SoundSettings settings = new SoundSettings(store); // 기본 음량 설정
        store.FailRead = true; // 읽기 오류 설정
        Assert.That(settings.TryLoad(), Is.False); // 오류 상태 전달
        Assert.That(settings.Get(SoundChannel.Music), Is.EqualTo(70)); // 기존 음량 보존
        store.FailWrite = true; // 기록 오류 설정
        Assert.That(settings.TrySave(), Is.False); // 실패를 성공으로 표시하지 않음
        Assert.That(store.Value, Is.Null); // 격리 저장소 원문 보존
    } // 시험 종료
    [Test] // 실제 성공 발생의 중복 방지 시험
    public void OnlySuccessfulUniqueOccurrencesReachTheSink() // 성공과 중복 구분
    { // 시험 시작
        FakeSink sink = new FakeSink(); // 격리 재생 수신기
        SoundEventRouter router = new SoundEventRouter(sink); // 재생 라우터
        Assert.That(router.TryNotify(SoundCue.Skill, 1, false), Is.False); // 실패 입력은 재생 차단
        Assert.That(router.TryNotify(SoundCue.Skill, 1, true), Is.True); // 같은 식별자의 실제 성공
        Assert.That(router.TryNotify(SoundCue.Skill, 1, true), Is.False); // 동일 성공 중복 차단
        Assert.That(router.TryNotify(SoundCue.Skill, 2, true), Is.True); // 다른 성공은 별도 처리
        Assert.That(router.TryNotify(SoundCue.Hit, 2, true), Is.True); // 다른 범주 발생 허용
        Assert.That(router.TryNotify(SoundCue.Skill, 0, true), Is.False); // 잘못된 식별자 차단
        Assert.That(sink.Calls, Is.EqualTo(3)); // 실제 성공 요청 수 확인
    } // 시험 종료
    [Test] // 음원 누락 시 전투 보호
    public void MissingClipDoesNotClaimPlaybackOrRepeatAnOccurrence() // 누락 음원 결과 구분
    { // 시험 시작
        FakeSink sink = new FakeSink(); // 격리 재생 수신기
        sink.Allow = false; // 실제 음원 없음
        SoundEventRouter router = new SoundEventRouter(sink); // 재생 라우터
        Assert.That(router.TryNotify(SoundCue.Hit, 8, true), Is.False); // 재생하지 못함 확인
        Assert.That(router.TryNotify(SoundCue.Hit, 8, true), Is.False); // 동일 발생 재시도 차단
        Assert.That(sink.Calls, Is.EqualTo(1)); // 중복 재생 요청 방지
    } // 시험 종료
} // 시험 종료
