using UnityEngine; // 유니티 이미지 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class BattleVisuals // 전투 안내 이미지
    { // 클래스 시작
        public static Sprite Crystal => BattleSprites.Load("experience_pickup"); // 경험치 결정 이미지
        public static Sprite Warning => BattleSprites.Load("attack_warning"); // 공격 경고 이미지
        public static Sprite Weak => BattleSprites.Load("weakpoint_reticle"); // 약점 조준 이미지
        public static Sprite Drone => BattleSprites.Load("support_drone"); // 보조 드론 이미지
        public static Sprite Blade => BattleSprites.Load("support_blade"); // 보조 칼날 이미지
    } // 클래스 종료
} // 영역 종료
