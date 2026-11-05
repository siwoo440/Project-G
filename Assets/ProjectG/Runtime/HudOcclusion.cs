using UnityEngine; // 유니티 화면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class HudOcclusion // 플레이어와 HUD 겹침 계산
    { // 클래스 시작
        public static float Alpha(Rect hud, Rect player) // 겹침 투명도 계산
        { // 계산 시작
            return player.width > 0f && player.height > 0f && hud.Overlaps(player) ? 0.35f : 1f; // 겹침 여부별 불투명도
        } // 계산 종료
        public static Rect PlayerRect(Camera camera, BattlePlayer player) // 화면 위 플레이어 범위
        { // 계산 시작
            if (camera == null || player == null) // 필수 객체 확인
            { // 생략 시작
                return new Rect(); // 빈 표시 영역
            } // 생략 종료
            SpriteRenderer image = player.GetComponent<SpriteRenderer>(); // 캐릭터 이미지 조회
            if (image == null || image.sprite == null) // 이미지 존재 확인
            { // 생략 시작
                return new Rect(); // 빈 표시 영역
            } // 생략 종료
            Bounds bounds = image.bounds; // 캐릭터 월드 영역
            Vector3 low = camera.WorldToScreenPoint(bounds.min); // 왼쪽 아래 화면 위치
            Vector3 high = camera.WorldToScreenPoint(bounds.max); // 오른쪽 위 화면 위치
            return Rect.MinMaxRect(Mathf.Min(low.x, high.x), Screen.height - Mathf.Max(low.y, high.y), Mathf.Max(low.x, high.x), Screen.height - Mathf.Min(low.y, high.y)); // 화면 위쪽 기준 영역
        } // 계산 종료
    } // 클래스 종료
} // 영역 종료
