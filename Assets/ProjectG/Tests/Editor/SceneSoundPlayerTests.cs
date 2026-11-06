using System.Collections.Generic; // 격리 재생 기록
using System.Reflection; // 씬 참조 주입
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투와 사운드 기능
using UnityEngine; // 격리 객체 기능
public class SceneSoundPlayerTests // 씬 오디오 기반 시험
{ // 시험 시작
    private sealed class MemoryStore : ISoundSettingsStore // 격리 음량 저장소
    { // 저장소 시작
        public string Read() // 저장 값 조회
        { // 조회 시작
            return null; // 초기 기본값 유지
        } // 조회 종료
        public void Write(string value) // 저장 요청
        { // 저장 시작
        } // 저장 종료
    } // 저장소 종료
    private sealed class FakeSink : ISoundCueSink // 장치 없는 효과음 수신기
    { // 수신기 시작
        public readonly List<SoundCue> Cues = new List<SoundCue>(); // 발생한 소리 목록
        public bool TryPlay(SoundCue cue) // 실제 요청 기록
        { // 기록 시작
            Cues.Add(cue); // 성공 행동 기록
            return true; // 격리 요청 수락
        } // 기록 종료
    } // 수신기 종료
    private static void SetField(object target, string name, object value) // 씬 직렬화 참조 대체
    { // 연결 시작
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic); // 실제 직렬화 필드
        Assert.That(field, Is.Not.Null, name); // 필드 존재 확인
        field.SetValue(target, value); // 격리 객체 주입
    } // 연결 종료
    private static SceneSoundPlayer CreatePlayer(GameObject root, out AudioSource[] sources) // 격리된 씬 오디오 구성
    { // 구성 시작
        sources = new AudioSource[4]; // 네 범주 출력 목록
        for (int index = 0; index < sources.Length; index++) // 출력기 생성
        { // 생성 시작
            sources[index] = root.AddComponent<AudioSource>(); // 편집 가능한 실제 출력기
            sources[index].volume = 0.5f; // Inspector 음량 기준
            sources[index].playOnAwake = false; // 실제 자동 재생 방지
        } // 생성 종료
        sources[1].mute = true; // 편집된 전투 음소거
        SceneSoundPlayer player = root.AddComponent<SceneSoundPlayer>(); // 실제 씬 오디오
        SetField(player, "musicSource", sources[0]); // BGM 출력 연결
        SetField(player, "combatSource", sources[1]); // 전투 출력 연결
        SetField(player, "skillSource", sources[2]); // 스킬 출력 연결
        SetField(player, "uiSource", sources[3]); // UI 출력 연결
        return player; // 격리 오디오 반환
    } // 구성 종료
    [Test] // 마스터와 범주 음량 적용
    public void AppliesVolumeOncePerChangeAndPreservesAuthoredMute() // 초기 편집값과 음소거 확인
    { // 시험 시작
        GameObject root = new GameObject("SoundRoot"); // 격리된 실제 오디오 루트
        try // 시험 범위
        { // 범위 시작
            SceneSoundPlayer player = CreatePlayer(root, out AudioSource[] sources); // 네 편집 출력 연결
            SoundSettings settings = new SoundSettings(new MemoryStore()); // 격리된 시험 설정
            Assert.That(player.BindSettings(settings), Is.True); // 실제 출력 설정 연결
            Assert.That(sources[0].volume, Is.EqualTo(0.5f * 0.8f * 0.7f).Within(0.001f)); // 초기 BGM 음량
            Assert.That(sources[1].volume, Is.EqualTo(0.5f * 0.8f * 0.85f).Within(0.001f)); // 초기 전투 음량
            Assert.That(sources[1].mute, Is.True); // 사용자가 설정한 전투 음소거 보존
            settings.TrySet(SoundChannel.Master, 0); // 전체 음량 최소
            foreach (AudioSource source in sources) // 모든 출력 검사
            { // 검사 시작
                Assert.That(source.mute, Is.True); // 실제 전체 음소거
            } // 검사 종료
            settings.TrySet(SoundChannel.Master, 100); // 전체 음량 복원
            Assert.That(sources[0].mute, Is.False); // BGM 음소거 해제
            Assert.That(sources[1].mute, Is.True); // 원래 전투 음소거 유지
            settings.TrySet(SoundChannel.Skill, 0); // 스킬 범주 음소거
            Assert.That(sources[2].mute, Is.True); // 스킬 출력 음소거
            Assert.That(sources[3].mute, Is.False); // 다른 범주 출력 유지
            settings.TrySet(SoundChannel.Skill, 100); // 스킬 범주 복원
            Assert.That(sources[2].volume, Is.EqualTo(0.5f).Within(0.001f)); // 초기 출력기 음량 유지
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(root); // 격리 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 미정 음원 요청 시험
    public void MissingClipsDoNotStartPlaybackOrThrow() // 참조 없는 음원 처리
    { // 시험 시작
        GameObject root = new GameObject("SoundRoot"); // 격리 오디오 루트
        try // 시험 범위
        { // 범위 시작
            SceneSoundPlayer player = CreatePlayer(root, out AudioSource[] sources); // 실제 출력 구성
            Assert.That(player.BindSettings(new SoundSettings(new MemoryStore())), Is.True); // 시험 음량 연결
            Assert.That(player.TryPlay(SoundCue.Hit), Is.False); // 없는 전투 음원 요청 거부
            Assert.That(player.TryPlayUi(SoundCue.Confirm), Is.False); // 없는 UI 음원 요청 거부
            Assert.That(player.TryPlayMusic(null), Is.False); // 없는 음악 요청 거부
            foreach (AudioSource source in sources) // 실제 출력 상태 확인
            { // 확인 시작
                Assert.That(source.isPlaying, Is.False); // 미정 음원의 재생 방지
            } // 확인 종료
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(root); // 격리 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 실제 성공 이벤트 구독 시험
    public void RealSuccessEventsBindOnceAndUnbindAcrossRestart() // 발생·중복·재시작 확인
    { // 시험 시작
        GameObject root = new GameObject("SoundRoot"); // 격리 오디오 루트
        GameObject fighter = new GameObject("Fighter"); // 격리 플레이어
        try // 시험 범위
        { // 범위 시작
            SceneSoundPlayer sound = CreatePlayer(root, out AudioSource[] sources); // 실제 오디오 구성
            SoundSettings settings = new SoundSettings(new MemoryStore()); // 격리 음량 설정
            FakeSink sink = new FakeSink(); // 실제 장치 없는 재생 기록
            Assert.That(sound.BindSettings(settings, sink), Is.True); // 격리 수신기 연결
            BattlePlayer player = fighter.AddComponent<BattlePlayer>(); // 실제 전투 플레이어
            player.Begin(Vector2.zero); // 첫 출격 초기화
            sound.BindBattle(player); // 실제 성공 신호 연결
            sound.BindBattle(player); // 중복 연결 재요청
            PlayerSurvival oldSurvival = player.Survival; // 이전 체력 상태 보관
            Assert.That(oldSurvival.TryHit(5), Is.True); // 실제 체력 피해
            Assert.That(oldSurvival.TryHit(5), Is.False); // 무적 중 피해 차단
            player.RegisterGraze(); // 실제 그레이즈 성공
            Assert.That(player.TryUseRapid(), Is.True); // 실제 고유 스킬 성공
            Assert.That(player.TryUseRapid(), Is.False); // 재사용 중 발동 실패
            player.RegisterEnemyDefeat(EnemyRank.Normal); // 실제 처치 알림
            CollectionAssert.AreEqual(new SoundCue[] // 성공한 발생 순서
            { // 목록 시작
                SoundCue.Hit, SoundCue.Graze, SoundCue.Skill, SoundCue.Defeat // 실패와 중복 제외
            }, sink.Cues); // 각 성공에 한 번 연결
            player.Begin(Vector2.zero); // 새 체력 상태로 재출격
            oldSurvival.Advance(2f); // 이전 상태 무적 해제
            Assert.That(oldSurvival.TryHit(5), Is.True); // 이전 체력 객체 피해
            Assert.That(sink.Cues.Count, Is.EqualTo(4)); // 이전 구독 해제 확인
            Assert.That(player.Survival.TryHit(5), Is.True); // 새 체력 객체 피해
            Assert.That(sink.Cues.Count, Is.EqualTo(5)); // 새 출격 구독 확인
            sound.UnbindBattle(); // 출격 종료 연결 해제
            player.RegisterGraze(); // 종료 후 성공 신호
            Assert.That(sink.Cues.Count, Is.EqualTo(5)); // 종료 후 재생 차단
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(fighter); // 플레이어 제거
            Object.DestroyImmediate(root); // 오디오 루트 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
