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
        private void OnGUI() // 전투 정보 표시
        { // 표시 시작
            if (player == null || player.Survival == null || battleCamera == null) // 표시 조건 확인
            { // 생략 시작
                return; // 표시 생략
            } // 생략 종료
            Rect view = battleCamera.pixelRect; // 전투 화면 영역
            float left = view.xMin + 12f; // 왼쪽 여백
            float top = Screen.height - view.yMax + 10f; // 위쪽 여백
            GUIStyle text = new GUIStyle(GUI.skin.label); // 기본 글자 설정
            text.fontSize = Mathf.Max(14, Mathf.RoundToInt(view.height / 45f)); // 화면 비례 글자 크기
            text.normal.textColor = Color.white; // 글자 색상
            GUI.Label(new Rect(left, top, 500f, 34f), "HP " + player.Survival.Health + "   GRAZE " + player.GrazeCount + "   ENERGY " + (player.Skills != null ? player.Skills.Energy : 0) + "/100", text); // 체력과 그레이즈 표시
            if (!player.Survival.IsAlive) // 출격 실패 확인
            { // 실패 표시 시작
                GUI.Label(new Rect(left, top + 34f, 260f, 34f), "MISSION FAILED", text); // 실패 상태 표시
            } // 실패 표시 종료
        } // 표시 종료
    } // 클래스 종료
} // 영역 종료
