using UnityEngine; // 캐릭터 일러스트와 저장된 에셋

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightPilotPortraits : ScriptableObject // 출격 준비와 결과의 공통 스탠딩 모음
    {
        [SerializeField] private Texture2D[] standing = System.Array.Empty<Texture2D>(); // 실제 캐릭터 열거형 순서의 원본 그림

        internal Texture2D For(FlightBroomPilot pilot) // 이번 출격의 실제 조종사 그림
        {
            int index = (int)pilot; // 에셋에 저장된 캐릭터 위치
            return index >= 0 && index < standing.Length ? standing[index] : null; // 미제작 그림은 해당 자리를 비움
        }
    }
}
