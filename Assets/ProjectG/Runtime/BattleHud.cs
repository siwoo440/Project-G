using UnityEngine; // 유니티 화면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class BattleHud : MonoBehaviour // 시험용 전투 정보 표시
    { // 클래스 시작
        private BattlePlayer player; // 플레이어 상태
        private Camera battleCamera; // 전투 화면 기준
        public void Begin(BattlePlayer target, Camera camera) // 표시 초기화
        { // 초기화 시작
            player = target; // 플레이어 참조
            battleCamera = camera; // 카메라 참조
        } // 초기화 종료
        public static Rect LineRect(Rect view, int index) // 화면 크기에 맞는 정보 줄 배치
        { // 배치 시작
            int fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height / 45f)); // 화면 비례 글자 크기
            float height = fontSize + 6f; // 글자와 아래 여백 높이
            float width = Mathf.Max(0f, Mathf.Min(view.width - 24f, fontSize * 25f)); // 글자 크기에 맞춘 너비
            return new Rect(view.xMin + 12f, view.yMin + 10f + Mathf.Max(0, index) * height, width, height); // 분리된 정보 영역
        } // 배치 종료
        private void OnGUI() // 전투 정보 표시
        { // 표시 시작
            if (player == null || player.Survival == null || battleCamera == null) // 표시 조건 확인
            { // 생략 시작
                return; // 표시 생략
            } // 생략 종료
            Rect pixelView = battleCamera.pixelRect; // 전투 화면 영역
            Rect view = new Rect(pixelView.xMin, Screen.height - pixelView.yMax, pixelView.width, pixelView.height); // 화면 위쪽 기준 영역
            Rect first = LineRect(view, 4); // 게이지 아래 그레이즈 영역
            Rect second = LineRect(view, 5); // 캐릭터 전투 정보 영역
            Rect fourth = LineRect(view, 6); // 실패 안내 영역
            Color previous = GUI.color; // 기존 화면 색상
            Rect occupied = Rect.MinMaxRect(first.xMin, first.yMin, first.xMax, player.Survival.IsAlive ? second.yMax : fourth.yMax); // 남은 정보 영역 전체 범위
            float opacity = HudOcclusion.Alpha(occupied, HudOcclusion.PlayerRect(battleCamera, player)); // 플레이어 겹침 투명도
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * opacity); // 전투 HUD 투명도 적용
            GUIStyle text = new GUIStyle(GUI.skin.label); // 기본 글자 설정
            text.fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height / 45f)); // 화면 비례 글자 크기
            text.normal.textColor = Color.white; // 글자 색상
            GUI.Label(first, "GRAZE " + player.GrazeCount, text); // 그레이즈 표시
            string debugKeys = player.AllowDebugCharacterSwitch ? "   F1 RAPID / F2 SNIPER" : ""; // 시험 선택 키 안내
            GUI.Label(second, (player.Character == CharacterType.Rapid ? "RAPID BONUS " + Mathf.RoundToInt(player.Combo.Bonus * 100f) + "%" : "SNIPER WEAK POINT") + debugKeys, text); // 캐릭터 전투 상태
            if (!player.Survival.IsAlive) // 출격 실패 확인
            { // 실패 표시 시작
                GUI.Label(fourth, "MISSION FAILED", text); // 실패 상태 표시
            } // 실패 표시 종료
            GUI.color = previous; // 기존 화면 색상 복원
        } // 표시 종료
    } // 클래스 종료
} // 영역 종료
