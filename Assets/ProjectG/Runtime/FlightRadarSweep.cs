using UnityEngine; // 레이더 부채꼴의 좌표와 색
using UnityEngine.UI; // 화면의 투명한 스캔 면

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightRadarSweep : MaskableGraphic // 원형 레이더의 부드러운 탐색 효과
    {
        protected override void OnPopulateMesh(VertexHelper vertices) // 표시용 탐색 부채꼴의 면
        {
            const int segments = 12; // 부드러운 원형 모서리
            float radius = rectTransform.rect.width * 0.47f; // 생성한 프레임의 내부 반경
            vertices.Clear(); // 이전 판의 정점 제거
            vertices.AddVert(Vector3.zero, new Color32(60, 226, 232, 90), Vector2.zero); // 원 가운데의 투명한 빛
            for (int index = 0; index <= segments; index++) // 투명도 단계가 있는 외곽 꼭짓점
            {
                float fraction = (float)index / segments; // 스캔의 진행 비율
                float angle = Mathf.Lerp(-42f, 0f, fraction) * Mathf.Deg2Rad; // 위쪽의 좁고 읽기 쉬운 부채꼴
                var point = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle)) * radius; // 원형 가장자리의 좌표
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Lerp(0f, 63f, fraction)); // 한쪽에서 사라지는 레이더 효과
                vertices.AddVert(point, new Color32(44, 217, 223, alpha), Vector2.zero); // 투명한 청록 조각
                if (index > 0) // 첫 점 뒤에는 삼각형을 완성
                {
                    vertices.AddTriangle(0, index + 1, index); // 스캔 면 한 조각
                }
            }
        }
    }
}
