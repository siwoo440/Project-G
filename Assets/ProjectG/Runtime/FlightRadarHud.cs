using System.Collections.Generic; // 활성 적 위치 수집
using UnityEngine; // 레이더의 3D 방향과 2D 좌표
using UnityEngine.UI; // 적과 거리의 UI 레이어

namespace ProjectG // 프로젝트 게임 영역
{
    [DefaultExecutionOrder(175)] // 추적 카메라와 적 이동 뒤에 위치 갱신
    public sealed class FlightRadarHud : MonoBehaviour // 카메라 상대 방향의 원형 전투 레이더
    {
        [System.Serializable] // 씬에서 편집하는 점과 거리 레이블
        private sealed class EnemyMarker // 원형 레이더의 표시 슬롯
        {
            public RectTransform icon; // 현재 적 위치 마커
            public Text distance; // 해당 적의 실제 월드 거리
            [System.NonSerialized] public float distanceSquared; // 같은 방향에서 가까운 거리 우선순위
            [System.NonSerialized] public Rect labelBounds; // 레이더 안의 거리 글자 영역
        }

        [SerializeField] private FlightEnemyEncounter encounter; // 현재 웨이브의 활성 적
        [SerializeField] private Camera view; // 실제로 바라보는 추적과 둘러보기 시점
        [SerializeField] private Transform pilot; // 거리를 측정하는 기체 위치
        [SerializeField] private RectTransform north; // 월드 북쪽의 현재 방향
        [SerializeField] private RectTransform sweep; // 시간으로 회전하는 시각 효과
        [SerializeField] private EnemyMarker[] markers; // 편집 가능한 표시 슬롯
        [SerializeField, Min(1f)] private float range = 45f; // 표준 표시의 수평 범위
        private readonly List<Vector3> positions = new List<Vector3>(); // 할당 없이 프레임마다 적 조회
        private Vector3 facing = Vector3.forward; // 화면을 똑바로 위로 볼 때도 이전 수평 기준 유지

        private void LateUpdate() // 적의 이동과 카메라 선회 뒤의 화면 레이더
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up); // 시점의 수평 정면
            if (horizontal.sqrMagnitude > 0.0001f) // 수평 방향이 안정적인 경우
            {
                facing = horizontal.normalized; // 실제 둘러보기의 정면 갱신
            }

            Vector3 right = Vector3.Cross(Vector3.up, facing); // 카메라 기준 오른쪽
            float radius = ((RectTransform)transform).rect.width * 0.5f; // 레이더의 실제 크기
            north.anchoredPosition = new Vector2(Vector3.Dot(Vector3.forward, right), Vector3.Dot(Vector3.forward, facing)) * (radius * 0.85f); // 원 둘레의 실제 북쪽
            sweep.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Repeat(Time.time * 58f, 360f)); // 정지하는 게임 시간에 맞춘 레이더 스캔

            encounter.CopyActivePositions(positions); // 처치·이탈을 제외한 실제 위치 목록
            int visibleCount = Mathf.Min(positions.Count, markers.Length); // 현재 씬에서 표시할 수 있는 적 수
            for (int index = 0; index < markers.Length; index++) // 씬에서 정한 표시 슬롯 순서
            {
                EnemyMarker item = markers[index]; // 표시할 원 위의 적 표시
                bool active = index < visibleCount; // 현재 적과 연결된 슬롯만 사용
                item.icon.gameObject.SetActive(active); // 이탈하거나 처치한 적 숨김
                if (!active) // 더 이상 그릴 실제 적이 없는 경우
                {
                    continue; // 고정 슬롯은 다음 웨이브에 재사용
                }

                Vector3 offset = positions[index] - pilot.position; // 플레이어에서 적까지의 입체 거리
                Vector2 plane = new Vector2(Vector3.Dot(offset, right), Vector3.Dot(offset, facing)); // 위가 정면인 원형 표시
                item.icon.anchoredPosition = Vector2.ClampMagnitude(plane / range, 1f) * (radius * 0.73f); // 먼 적은 원 가장자리에 제한
                item.distanceSquared = offset.sqrMagnitude; // 숫자 반올림 전 실제 거리 비교
                int meters = Mathf.RoundToInt(offset.magnitude); // 적까지의 직선 월드 거리
                item.distance.text = $"{meters} m"; // 실제 적 표시와 거리
                RectTransform label = item.distance.rectTransform; // 마커 아래의 중앙 정렬 글자
                Vector2 size = new Vector2(Mathf.Min(label.rect.width, item.distance.preferredWidth) + 6f, label.rect.height); // 표시 폭과 글자 사이 여백
                Vector2 center = item.icon.anchoredPosition + label.anchoredPosition; // 같은 레이더 기준의 글자 위치
                item.labelBounds = new Rect(center - size * 0.5f, size); // 겹침 비교에 쓰는 글자 영역
            }

            for (int index = 0; index < visibleCount; index++) // 마커는 남기고 거리 글자만 비교
            {
                EnemyMarker item = markers[index]; // 현재 적의 거리 표기
                bool showDistance = true; // 단독 거리 글자의 기본 표시
                for (int other = 0; other < visibleCount; other++) // 같은 프레임의 인접 거리 점검
                {
                    EnemyMarker candidate = markers[other]; // 겹칠 수 있는 이웃 표기
                    bool higherPriority = candidate.distanceSquared < item.distanceSquared || (candidate.distanceSquared == item.distanceSquared && other < index); // 실제 거리와 동일 거리의 안정적 순서
                    if (higherPriority && item.labelBounds.Overlaps(candidate.labelBounds)) // 가까운 적의 글자와 겹친 경우
                    {
                        showDistance = false; // 먼 적의 거리만 숨김
                        break; // 추가 비교 생략
                    }
                }
                item.distance.enabled = showDistance; // 적이 멀어지면 다시 글자 표시
            }
        }
    }
}
