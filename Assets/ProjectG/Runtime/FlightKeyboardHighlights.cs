using UnityEngine; // 정확한 키 하이라이트 위치
using UnityEngine.InputSystem; // 실제 키 구분
using UnityEngine.UI; // Unity UI의 평면 도형

namespace ProjectG // 프로젝트 게임 영역
{
    public sealed class FlightKeyboardHighlights : Graphic // 벡터의 키 위치에 투명 강조를 표시
    {
        [System.Serializable] // SVG 좌표를 씬에서 편집할 수 있게 저장
        private struct Cap // 한 물리 키의 실제 그림 범위
        {
            public Key key; // 키 이미지와 동작의 공통 식별자
            public Rect uv; // 키보드 이미지 기준의 정규화 위치
        }

        [SerializeField] private Cap[] caps; // SVG에서 읽은 실제 키 위치
        private FlightBindings bindings; // 현재 적용하는 조작 설정

        public void Show(FlightBindings current) // 실제 변경 키의 하이라이트 갱신
        {
            bindings = current; // 같은 설정을 가리키는 참조
            SetVerticesDirty(); // 그래픽 색과 위치 재생성
        }

        public bool ContainsBinding(Key key) // 키보드 다이어그램의 실제 활성 키 검증
        {
            return FindAction(key) >= 0; // 현재 키가 강조됐는지 확인
        }

        private int FindAction(Key key) // 하나의 실제 키의 현재 조작
        {
            if (bindings != null) // 화면이 조작 설정을 받은 상태
            {
                for (int index = 0; index <= (int)FlightAction.Pilot; index++) // 너비의 실제 키에 맞는 동작
                {
                    if (bindings.GetKey((FlightAction)index) == key) // 현재 키가 연결된 조작
                    {
                        return index; // 비행·전투 색 지정에 사용하는 번호
                    }
                }
            }
            return -1; // 현재 비활성 키
        }

        protected override void OnPopulateMesh(VertexHelper vertices) // 평면 키 위에 현재 조작만 강조
        {
            vertices.Clear(); // 이전 배치 색 제거
            Rect area = rectTransform.rect; // 실제 화면의 키보드 크기
            foreach (Cap cap in caps) // 지정된 실제 물리 키를 순회
            {
                int action = FindAction(cap.key); // 이 키에 배정된 조작
                if (action < 0) // 사용하지 않는 물리 키
                {
                    continue; // 평면 PNG의 기본색 유지
                }
                Color32 tone = action <= (int)FlightAction.Focus ? new Color32(63, 211, 215, 93) : new Color32(242, 181, 81, 106); // 이동과 전투의 구분
                float x = area.xMin + area.width * cap.uv.xMin; // 가로 시작 범위
                float right = area.xMin + area.width * cap.uv.xMax; // 가로 끝 범위
                float bottom = area.yMin + area.height * cap.uv.yMin; // 세로 하단 범위
                float top = area.yMin + area.height * cap.uv.yMax; // 세로 상단 범위
                int first = vertices.currentVertCount; // 이 키의 정점 시작점
                vertices.AddVert(new Vector3(x, bottom), tone, Vector2.zero); // 왼쪽 아래
                vertices.AddVert(new Vector3(x, top), tone, Vector2.zero); // 왼쪽 위
                vertices.AddVert(new Vector3(right, top), tone, Vector2.zero); // 오른쪽 위
                vertices.AddVert(new Vector3(right, bottom), tone, Vector2.zero); // 오른쪽 아래
                vertices.AddTriangle(first, first + 1, first + 2); // 사각형의 첫 삼각형
                vertices.AddTriangle(first + 2, first + 3, first); // 사각형의 두 번째 삼각형
            }
        }
    }
}
