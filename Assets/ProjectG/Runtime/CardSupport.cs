using System.Collections.Generic; // 대상별 재공격 시간
using UnityEngine; // 유니티 전투 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class CardSupport // 카드 보조 공격
    { // 클래스 시작
        private float droneTimer; // 드론 사격 대기
        private float bladeClock; // 칼날 경과 시간
        private readonly Dictionary<CombatTarget, float> bladeReady = new Dictionary<CombatTarget, float>(); // 대상별 재공격 시점
        private float angle; // 칼날 회전 각도
        private Transform drone; // 드론 임시 표시
        private Transform blade; // 칼날 임시 표시
        public void Advance(float elapsed, BattlePlayer player, float top) // 보조 공격 진행
        { // 진행 시작
            if (elapsed <= 0f || player == null || player.Build == null || player.Survival == null || !player.Survival.IsAlive) // 진행 조건 확인
            { // 중단 시작
                return; // 공격 생략
            } // 중단 종료
            if (player.Build.DroneLevel > 0) // 드론 보유 확인
            { // 드론 시작
                if (drone == null) // 드론 표시 확인
                { // 생성 시작
                    drone = CreateVisual(player, "CardDrone"); // 드론 표시 생성
                } // 생성 종료
                drone.position = player.transform.position + new Vector3(-0.7f, 0.1f, 0f); // 드론 위치
                droneTimer -= elapsed; // 사격 대기 감소
                if (droneTimer <= 0f) // 사격 시점 확인
                { // 사격 시작
                    droneTimer = 1f; // 다음 사격 시간
                    GameObject shot = new GameObject("DroneBullet"); // 보조 탄환 생성
                    shot.transform.position = drone.position; // 보조 탄환 위치
                    shot.AddComponent<Projectile>().Begin(Vector2.up * 10f, top, 3f + player.Build.DroneLevel * 3f, false, player); // 드론 시험 피해
                } // 사격 종료
            } // 드론 종료
            if (player.Build.BladeLevel > 0) // 칼날 보유 확인
            { // 칼날 시작
                if (blade == null) // 칼날 표시 확인
                { // 생성 시작
                    blade = CreateVisual(player, "CardBlade"); // 칼날 표시 생성
                } // 생성 종료
                float previousAngle = angle; // 이동 전 회전 각도
                float turn = elapsed * 180f; // 이번 프레임 회전량
                angle += turn; // 회전 각도 증가
                bladeClock += elapsed; // 대상별 대기 시간 진행
                int segments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(turn) / 15f), 1, 48); // 연속 접촉 분할 수
                Vector2 startCenter = player.PreviousPosition; // 플레이어 이동 전 위치
                Vector2 endCenter = player.transform.position; // 플레이어 이동 후 위치
                float endRadians = angle * Mathf.Deg2Rad; // 현재 각도 변환
                blade.position = (Vector3)(endCenter + new Vector2(Mathf.Cos(endRadians), Mathf.Sin(endRadians))); // 시험 회전 표시
                foreach (CombatTarget target in Object.FindObjectsByType<CombatTarget>(FindObjectsSortMode.None)) // 대상 순회
                { // 대상 검사 시작
                    if (!target.CanTakeDamage || (bladeReady.TryGetValue(target, out float ready) && bladeClock < ready)) // 대상별 재공격 확인
                    { // 생략 시작
                        continue; // 대기 중 대상 제외
                    } // 생략 종료
                    float startRadians = previousAngle * Mathf.Deg2Rad; // 이동 전 각도 변환
                    Vector2 from = startCenter + new Vector2(Mathf.Cos(startRadians), Mathf.Sin(startRadians)); // 칼날 이동 시작점
                    for (int index = 1; index <= segments; index++) // 회전 경로 순회
                    { // 경로 검사 시작
                        float fraction = (float)index / segments; // 진행 비율
                        float radians = (previousAngle + turn * fraction) * Mathf.Deg2Rad; // 구간 각도
                        Vector2 center = Vector2.Lerp(startCenter, endCenter, fraction); // 플레이어 이동 보간
                        Vector2 to = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)); // 칼날 이동 끝점
                        if (target.Intersects(from, to, out bool weak, 0.125f, 0.125f)) // 실제 칼날 접촉 확인
                        { // 적중 시작
                            float multiplier = player.Character == CharacterType.Sniper && weak ? player.Build.WeakMultiplier : 1f; // 저격 약점 배율
                            target.ReceiveDamage((4f + player.Build.BladeLevel * 4f) * multiplier, player); // 칼날 시험 피해
                            bladeReady[target] = bladeClock + 1f; // 대상별 재공격 대기
                            break; // 같은 회전 중복 피해 방지
                        } // 적중 종료
                        from = to; // 다음 구간 시작점
                    } // 경로 검사 종료
                } // 대상 검사 종료
            } // 칼날 종료
        } // 진행 종료
        public void Clear() // 이전 보조 표시 제거
        { // 제거 시작
            RemoveVisual(drone); // 드론 표시 제거
            RemoveVisual(blade); // 칼날 표시 제거
            drone = null; // 드론 참조 초기화
            blade = null; // 칼날 참조 초기화
            bladeReady.Clear(); // 대상별 재공격 상태 초기화
        } // 제거 종료
        private static Transform CreateVisual(BattlePlayer player, string name) // 보조 표시 생성
        { // 생성 시작
            GameObject marker = new GameObject(name); // 표시 객체 생성
            marker.transform.SetParent(player.transform, false); // 플레이어에 연결
            float scale = Mathf.Max(0.0001f, Mathf.Abs(player.transform.lossyScale.x)); // 플레이어 표시 배율
            marker.transform.localScale = new Vector3(0.25f / scale, 0.25f / scale, 1f); // 고정 월드 표시 크기
            SpriteRenderer image = marker.AddComponent<SpriteRenderer>(); // 표시 구성 요소
            image.sprite = name == "CardDrone" ? BattleVisuals.Drone : BattleVisuals.Blade; // 보조 공격별 이미지
            image.color = Color.white; // 보조 이미지 원본 색상
            image.sortingOrder = 2; // 플레이어 위 표시
            return marker.transform; // 표시 객체 반환
        } // 생성 종료
        private static void RemoveVisual(Transform marker) // 임시 표시 정리
        { // 정리 시작
            if (marker == null) // 표시 존재 확인
            { // 생략 시작
                return; // 정리 생략
            } // 생략 종료
            if (Application.isPlaying) // 플레이 상태 확인
            { // 플레이 정리 시작
                Object.Destroy(marker.gameObject); // 지연 제거
            } // 플레이 정리 종료
            else // 편집 시험 상태
            { // 즉시 정리 시작
                Object.DestroyImmediate(marker.gameObject); // 시험 객체 제거
            } // 즉시 정리 종료
        } // 정리 종료
    } // 클래스 종료
} // 영역 종료
