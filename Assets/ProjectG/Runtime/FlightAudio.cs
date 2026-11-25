using UnityEngine; // 배경 음악과 효과음의 Unity 재생

namespace ProjectG // 프로젝트 게임 영역
{
    internal enum FlightSfx // 실제 게임 행동에 대응하는 에셋 순서
    {
        AutoShot, PilotHit, Graze, EnemyDown, Skill, Ultimate, MagicCard, Pickup, LevelUp, UiConfirm, UiBack // 에디터에서 고른 효과음 목록
    }

    [DisallowMultipleComponent] // 씬마다 한 오디오 관리자
    public sealed class FlightAudio : MonoBehaviour // 현재 씬의 레벨·보스 음악과 효과 재생
    {
        [SerializeField] private AudioSource music; // 지속되는 배경과 결과 음악
        [SerializeField] private AudioSource combat; // 일시정지에서 멈추는 전투 소리
        [SerializeField] private AudioSource ui; // 일시정지에서 들리는 메뉴 입력
        [SerializeField] private AudioClip background; // 현재 씬의 기본 반복 음악
        [SerializeField] private AudioClip boss; // 보스 진입 때 사용하는 전투 변주
        [SerializeField] private AudioClip clear; // 출격 완료 음악
        [SerializeField] private AudioClip failed; // 출격 실패 음악
        [SerializeField] private AudioClip[] effects; // 실제 행동별 제작 파형
        [SerializeField] private FlightEnemyEncounter encounter; // 보스 상태를 관리하는 실제 항로
        private readonly float[] nextEffect = new float[(int)FlightSfx.UiBack + 1]; // 반복음별 다음 재생 순간
        private static FlightAudio current; // 현재 배포에 올라온 단일 씬의 관리자

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 유지 시에도 이전 씬 참조 제거
        private static void ClearActive() // 플레이 모드 초기 상태
        {
            current = null; // 이전 Unity 실행의 관리 객체 초기화
        }

        private void OnEnable() // 씬에서 제작된 오디오 소스 활성화
        {
            current = this; // 현재 전투와 UI가 공통 소스로 재생
            ui.ignoreListenerPause = true; // Esc 뒤에도 계속하기와 설정의 신호 유지
        }

        private void OnDisable() // 다른 씬을 열면 이전 배경과 효과 정리
        {
            if (current == this) // 이미 새 씬이 이어 받지 않은 경우
            {
                current = null; // 제거된 오브젝트 참조 해제
            }
        }

        private void Start() // 해당 씬의 초기 배경 시작
        {
            if (music.clip == null && background != null) // 결과에서 이미 성공·실패가 지정된 경우 제외
            {
                ChangeMusic(background, true); // 전투·메뉴·마을의 반복 음악
            }
        }

        private void LateUpdate() // 실제 중간·최종 보스 등장과 물러남 관찰
        {
            if (encounter != null && boss != null && !AudioListener.pause) // 일반 메뉴와 정지 중 전환 제외
            {
                bool threat = encounter.IsMidBossActive || encounter.IsFinalBossActive; // 타이머와 동일한 보스 출현 조건
                ChangeMusic(threat ? boss : background, true); // 현재 보스가 있을 때 한 곡으로 바꿈
            }
        }

        internal static void ShowResult(bool won) // 실제 출격 정산의 성공과 실패
        {
            if (current != null) // 결과 씬이 아직 편집 중이면 아무 음원 없음
            {
                current.ChangeMusic(won ? current.clear : current.failed, false); // 결과곡 한 번 재생
            }
        }

        internal static void Play(FlightSfx effect) // 실제로 일어난 행동의 효과음
        {
            if (current != null) // 제작 소스가 연결된 씬의 효과음만 출력
            {
                current.PlayEffect(effect); // 씬마다 유지되는 단일 재생 경로
            }
        }

        private void ChangeMusic(AudioClip clip, bool loop) // 같은 곡을 매 프레임 다시 시작하지 않는 전환
        {
            if (clip == null || music.clip == clip) // 같은 보스에서 진행 중인 음악 유지
            {
                return; // 재생 위치를 건드리지 않는 프레임
            }
            music.Stop(); // 기존 곡을 먼저 멈춰 중복 방지
            music.clip = clip; // 씬이나 보스의 새 음원
            music.loop = loop; // 일반 풍경은 반복, 결과곡은 한 번
            music.Play(); // 실제 Unity 오디오 출력
        }

        private void PlayEffect(FlightSfx effect) // 빠른 연사와 작은 충돌의 효과음 제한
        {
            int index = (int)effect; // 씬의 드래그 편집 가능한 배열 위치
            if (index >= effects.Length || effects[index] == null || Time.unscaledTime < nextEffect[index]) // 원본 에셋과 최근 재생 확인
            {
                return; // 같은 소리가 프레임마다 겹치는 문제 방지
            }
            bool interfaceSound = effect is FlightSfx.UiConfirm or FlightSfx.UiBack; // 일시정지 상태에서 가능한 입력
            if (!interfaceSound && AudioListener.pause) // 정지된 전투에서 들어온 효과 제외
            {
                return; // 장면을 멈춰도 구성 UI만 반응
            }
            nextEffect[index] = Time.unscaledTime + (effect == FlightSfx.AutoShot ? .155f : .085f); // 연사와 일반 알림의 음향 간격
            AudioSource source = interfaceSound ? ui : combat; // 현재 행동을 재생할 전용 소스
            source.PlayOneShot(effects[index], effect == FlightSfx.AutoShot ? .55f : 1f); // 중요한 스킬과 피격을 가리지 않는 사격
        }
    }
}
