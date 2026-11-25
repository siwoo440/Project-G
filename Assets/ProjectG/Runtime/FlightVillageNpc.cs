using System; // 하이러키에 저장하는 대화 자료
using UnityEngine; // 마을의 3D NPC와 자세

namespace ProjectG // 프로젝트 하늘섬 마을
{
    public enum FlightVillageService // 대화 뒤에 열리는 실제 시설
    {
        None, Leave, Market, Forge, Sortie // 문장 이동과 상점·공방·출격 준비
    }

    [DisallowMultipleComponent] // 마을 캐릭터의 대화 한 벌
    public sealed class FlightVillageNpc : MonoBehaviour // 하이러키에서 직접 편집하는 대화와 3D 몸체
    {
        [Serializable] // 각 문장에 붙는 화면 선택지
        public sealed class Choice // 화면 텍스트와 실행할 분기
        {
            public string label; // 실제 플레이어에게 보이는 선택
            public int next = -1; // 대화를 이어갈 문장 번호
            public FlightVillageService service; // 끝내거나 기존 시설을 열 동작
        }

        [Serializable] // 한 화면에서 보여주는 실제 NPC 대사
        public sealed class Line // 글자와 키/버튼으로 갈 다음 화면
        {
            [TextArea] public string text; // 테스트 대사의 한국어 문장
            public int next = -1; // 선택지가 없을 때 F로 갈 다음 문장
            public Choice[] choices = Array.Empty<Choice>(); // 대본의 응답이나 시설 진입 선택
        }

        [SerializeField] private string speaker; // 이야기 설정을 추가하지 않는 역할 이름
        [SerializeField] private Line[] dialogue = Array.Empty<Line>(); // 이 NPC의 편집 가능한 대화
        [SerializeField] private Transform body; // 하이러키의 실제 캐릭터 모델
        [SerializeField] private Transform focus; // 대화 카메라가 볼 몸의 기준
        [SerializeField, Min(.1f)] private float radius = 3.2f; // F 대화 가능 거리
        private Vector3 rest; // 실제 몸의 기본 발 위치
        private Quaternion original; // 대화를 시작하기 전의 시선
        private bool talking; // 고개와 카메라가 바뀌는 대화 상태

        internal string Speaker => speaker; // 현재 대화의 표시 이름
        internal float Radius => radius; // 하이러키의 개인 대화 범위
        internal Vector3 Focus => focus.position; // 모델 한가운데의 카메라 초점
        internal Vector3 Front => original * Vector3.forward; // 건물 바깥으로 미리 배치한 정면

        private void Awake() // 모델 자세의 기준과 원래 방향
        {
            rest = body.localPosition; // 발 고정 높이
            original = transform.rotation; // 시설을 기준으로 미리 둔 방향
        }

        internal Line At(int index) // 씬에서 바꾼 현재 문장 확인
        {
            return (uint)index < dialogue.Length ? dialogue[index] : null; // 끝 번호에서 자연스럽게 대화 종료
        }

        internal void SetTalking(bool active, Vector3 visitor) // 플레이어에게 고개를 돌려 응답
        {
            talking = active; // 현재 대화와 마을의 대기 상태 구분
            Vector3 direction = Vector3.ProjectOnPlane(visitor - transform.position, Vector3.up); // 높이차를 제외한 조종사 방향
            transform.rotation = active && direction.sqrMagnitude > .001f ? Quaternion.LookRotation(direction) : original; // 가까운 사람을 향하거나 원래 방향
        }

        private void LateUpdate() // 조작을 받지 않아도 조금 움직이는 대기 모션
        {
            float breath = Mathf.Sin(Time.time * 3f + transform.position.x) * (talking ? .025f : .012f); // 짧은 호흡과 정지감
            body.localPosition = rest + Vector3.up * breath; // 지형과 독립된 3D 모델 애니메이션
        }
    }
}
