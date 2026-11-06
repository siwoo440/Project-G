using UnityEngine; // 유니티 UI 객체
using UnityEngine.UI; // 실제 씬 UI 요소
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class SceneSkillHudPresenter : MonoBehaviour // 씬에 저장된 스킬 표시
    { // 구성 시작
        [SerializeField] private Button uniqueSlot; // 실제 고유기 사각형
        [SerializeField] private Button commonSlot; // 실제 공용기 사각형
        [SerializeField] private Button ultimateSlot; // 실제 필살기 사각형
        [SerializeField] private Image rapidIcon; // 편집 가능한 아리아 아이콘
        [SerializeField] private Image sniperIcon; // 편집 가능한 리안 아이콘
        [SerializeField] private Image shieldIcon; // 편집 가능한 보호막 아이콘
        [SerializeField] private Image scatterIcon; // 편집 가능한 산탄 아이콘
        [SerializeField] private Image ultimateIcon; // 편집 가능한 필살기 아이콘
        [SerializeField] private GameObject uniqueShade; // 고유기 사용 불가 음영
        [SerializeField] private GameObject commonShade; // 공용기 사용 불가 음영
        [SerializeField] private GameObject ultimateShade; // 필살기 자원 부족 음영
        [SerializeField] private GameObject uniqueActive; // 고유기 활성 표시
        [SerializeField] private GameObject commonActive; // 공용기 활성 표시
        [SerializeField] private Text uniqueCooldown; // 고유기 흰 쿨타임 숫자
        [SerializeField] private Text commonCooldown; // 공용기 흰 쿨타임 숫자
        [SerializeField] private Text ultimateCooldown; // 필살기 쿨타임 숫자 영역
        [SerializeField] private Text uniqueKey; // 고유기 실제 조작 키
        [SerializeField] private Text commonKey; // 공용기 실제 조작 키
        [SerializeField] private Text ultimateKey; // 필살기 실제 조작 키
        public bool IsReady => uniqueSlot != null && commonSlot != null && ultimateSlot != null && Valid(rapidIcon) && Valid(sniperIcon) && Valid(shieldIcon) && Valid(scatterIcon) && Valid(ultimateIcon) && uniqueShade != null && commonShade != null && ultimateShade != null && uniqueActive != null && commonActive != null && uniqueCooldown != null && commonCooldown != null && ultimateCooldown != null && uniqueKey != null && commonKey != null && ultimateKey != null; // 실제 씬 참조 확인
        private static bool Valid(Image image) // 실제 아이콘 참조 검사
        { // 검사 시작
            return image != null && image.sprite != null; // 편집 가능한 Sprite 확인
        } // 검사 종료
        public void Present(BattlePlayer player) // 실제 스킬 상태 표시
        { // 표시 시작
            if (!IsReady || player == null || player.Skills == null || player.Survival == null) // 필수 상태 확인
            { // 생략 시작
                return; // 불완전한 값 표시 차단
            } // 생략 종료
            bool sniper = player.Character == CharacterType.Sniper; // 실제 캐릭터 확인
            bool shield = player.Skills.Common == CommonSkill.Shield; // 실제 공용 스킬 확인
            bool blocked = !player.Survival.IsAlive || player.Skills.Blocked; // 전투 공통 사용 제한
            rapidIcon.gameObject.SetActive(!sniper); // 아리아 사용자 아이콘 표시
            sniperIcon.gameObject.SetActive(sniper); // 리안 사용자 아이콘 표시
            shieldIcon.gameObject.SetActive(shield); // 보호막 사용자 아이콘 표시
            scatterIcon.gameObject.SetActive(!shield); // 산탄 사용자 아이콘 표시
            uniqueShade.SetActive(blocked || player.Skills.RapidActive > 0.000001f || player.Skills.RapidCooldown > 0.000001f); // 고유기 사용 불가 표시
            commonShade.SetActive(blocked || player.Skills.CommonCooldown > 0.000001f); // 공용기 사용 불가 표시
            ultimateShade.SetActive(blocked || player.Skills.ExactEnergy < 100f); // 필살기 에너지 부족 표시
            uniqueActive.SetActive(player.Skills.RapidActive > 0f); // 실제 고유기 활성 표시
            commonActive.SetActive(player.Survival.HasShield); // 실제 보호막 활성 표시
            uniqueCooldown.text = SkillHudLayout.Countdown(player.Skills.RapidCooldown); // 고유기 남은 초 숫자
            commonCooldown.text = SkillHudLayout.Countdown(player.Skills.CommonCooldown); // 공용기 남은 초 숫자
            ultimateCooldown.text = ""; // 현재 필살기는 별도 쿨타임 없음
            uniqueKey.text = player.ControlLabel(PlayerAction.Unique); // 재설정된 고유기 키
            commonKey.text = player.ControlLabel(PlayerAction.Common); // 재설정된 공용기 키
            ultimateKey.text = player.ControlLabel(PlayerAction.Ultimate); // 재설정된 필살기 키
        } // 표시 종료
    } // 구성 종료
} // 영역 종료
