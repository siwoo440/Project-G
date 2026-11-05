using UnityEngine; // 유니티 화면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class SkillHud : MonoBehaviour // 스킬 화면 표시
    { // 클래스 시작
        private BattlePlayer player; // 플레이어 스킬 상태
        private Camera battleCamera; // 전투 화면 기준
        private Texture2D rapidIcon; // 고유 스킬 이미지
        private Texture2D shieldIcon; // 보호막 이미지
        private Texture2D scatterIcon; // 산탄 이미지
        private Texture2D ultimateIcon; // 필살기 이미지
        private GUIStyle countdownStyle; // 흰색 쿨타임 글자
        private GUIStyle keyStyle; // 스킬 키 글자
        public void Begin(BattlePlayer target, Camera camera) // 스킬 화면 초기화
        { // 초기화 시작
            player = target; // 플레이어 참조
            battleCamera = camera; // 카메라 참조
            rapidIcon = Resources.Load<Texture2D>("SkillIcons/rapid"); // 고유 스킬 이미지 불러오기
            shieldIcon = Resources.Load<Texture2D>("SkillIcons/shield"); // 보호막 이미지 불러오기
            scatterIcon = Resources.Load<Texture2D>("SkillIcons/scatter"); // 산탄 이미지 불러오기
            ultimateIcon = Resources.Load<Texture2D>("SkillIcons/ultimate"); // 필살기 이미지 불러오기
        } // 초기화 종료
        private void OnGUI() // 스킬 화면 표시
        { // 표시 시작
            if (player == null || player.Skills == null || player.Survival == null || battleCamera == null) // 표시 조건 확인
            { // 표시 생략 시작
                return; // 초기화 전 표시 생략
            } // 표시 생략 종료
            Rect pixelView = battleCamera.pixelRect; // 카메라 화면 영역
            Rect view = new Rect(pixelView.xMin, Screen.height - pixelView.yMax, pixelView.width, pixelView.height); // 화면 좌표 변환
            Rect first = SkillHudLayout.Slot(view, 0); // J 사각형 위치
            Rect second = SkillHudLayout.Slot(view, 1); // K 사각형 위치
            Rect third = SkillHudLayout.Slot(view, 2); // L 사각형 위치
            bool blocked = !player.Survival.IsAlive || player.Skills.Blocked; // 공통 사용 제한
            DrawSlot(first, rapidIcon, player.ControlLabel(PlayerAction.Unique), player.Skills.RapidCooldown, blocked, player.Skills.RapidActive > 0f); // 고유 스킬 표시
            Texture2D commonIcon = player.Skills.Common == CommonSkill.Shield ? shieldIcon : scatterIcon; // 공용 스킬 이미지 선택
            DrawSlot(second, commonIcon, player.ControlLabel(PlayerAction.Common), player.Skills.CommonCooldown, blocked, player.Survival.HasShield); // 공용 스킬 표시
            DrawSlot(third, ultimateIcon, player.ControlLabel(PlayerAction.Ultimate), 0f, blocked || player.Skills.Energy < 100, false); // 필살기 에너지 상태 표시
        } // 표시 종료
        private void DrawSlot(Rect slot, Texture2D icon, string key, float cooldown, bool unavailable, bool active) // 스킬 한 칸 표시
        { // 표시 시작
            if (countdownStyle == null) // 글자 스타일 확인
            { // 스타일 생성 시작
                countdownStyle = new GUIStyle(GUI.skin.label); // 쿨타임 글자 생성
                countdownStyle.alignment = TextAnchor.MiddleCenter; // 숫자 중앙 정렬
                countdownStyle.fontStyle = FontStyle.Bold; // 숫자 굵게 표시
                countdownStyle.normal.textColor = Color.white; // 쿨타임 흰색 표시
                keyStyle = new GUIStyle(GUI.skin.label); // 키 글자 생성
                keyStyle.fontStyle = FontStyle.Bold; // 키 굵게 표시
                keyStyle.normal.textColor = Color.white; // 키 흰색 표시
            } // 스타일 생성 종료
            countdownStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(slot.height * 0.48f)); // 숫자 크기 설정
            keyStyle.fontSize = Mathf.Max(11, Mathf.RoundToInt(slot.height * 0.20f)); // 키 크기 설정
            Color previous = GUI.color; // 기존 화면 색상 보관
            float opacity = HudOcclusion.Alpha(slot, HudOcclusion.PlayerRect(battleCamera, player)); // 플레이어 겹침 투명도
            GUI.color = new Color(1f, 1f, 1f, opacity); // 원본 이미지와 투명도
            GUI.DrawTexture(slot, icon != null ? icon : Texture2D.whiteTexture, ScaleMode.ScaleToFit); // 스킬 이미지 표시
            if (cooldown > 0.000001f || unavailable) // 사용 불가 상태 확인
            { // 어두운 덮개 시작
                GUI.color = new Color(0f, 0f, 0f, 0.65f * opacity); // 사용 불가 음영
                GUI.DrawTexture(slot, Texture2D.whiteTexture); // 이미지 위 음영 표시
            } // 어두운 덮개 종료
            GUI.color = active ? new Color(0.3f, 1f, 1f, opacity) : new Color(1f, 1f, 1f, opacity); // 활성 상태 테두리 색상
            DrawFrame(slot); // 사각형 테두리 표시
            GUI.color = new Color(1f, 1f, 1f, opacity); // 흰색 글자와 투명도
            GUI.Label(slot, SkillHudLayout.Countdown(cooldown), countdownStyle); // 남은 쿨타임 숫자
            GUI.Label(new Rect(slot.x + 4f, slot.yMax - keyStyle.fontSize - 4f, slot.width - 8f, keyStyle.fontSize + 4f), key, keyStyle); // 스킬 입력 키 표시
            GUI.color = previous; // 기존 화면 색상 복원
        } // 표시 종료
        private static void DrawFrame(Rect slot) // 사각형 테두리 표시
        { // 테두리 시작
            float thickness = Mathf.Max(1f, slot.width * 0.025f); // 테두리 두께
            GUI.DrawTexture(new Rect(slot.x, slot.y, slot.width, thickness), Texture2D.whiteTexture); // 위쪽 선
            GUI.DrawTexture(new Rect(slot.x, slot.yMax - thickness, slot.width, thickness), Texture2D.whiteTexture); // 아래쪽 선
            GUI.DrawTexture(new Rect(slot.x, slot.y, thickness, slot.height), Texture2D.whiteTexture); // 왼쪽 선
            GUI.DrawTexture(new Rect(slot.xMax - thickness, slot.y, thickness, slot.height), Texture2D.whiteTexture); // 오른쪽 선
        } // 테두리 종료
    } // 클래스 종료
} // 영역 종료
