using System.Collections.Generic; // 이미지 보관 기능
using UnityEngine; // 유니티 이미지 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class BattleSprites // 전투 이미지 관리
    { // 클래스 시작
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>(); // 생성된 이미지 보관
        public static Sprite Load(string name) // 전투 이미지 불러오기
        { // 조회 시작
            if (cache.TryGetValue(name, out Sprite existing) && existing != null) // 기존 이미지 확인
            { // 재사용 시작
                return existing; // 기존 이미지 반환
            } // 재사용 종료
            Texture2D texture = Resources.Load<Texture2D>("BattleArt/" + name); // 이미지 자원 조회
            if (texture == null) // 자원 누락 확인
            { // 대체 시작
                return Projectile.GetSprite(); // 기본 도형 사용
            } // 대체 종료
            Rect area = new Rect(0f, 0f, texture.width, texture.height); // 원본 표시 영역
            if (name == "bullet_enemy") // 적탄 여백 확인
            { // 적탄 범위 시작
                area = new Rect(texture.width * 0.28f, texture.height * 0.28f, texture.width * 0.44f, texture.height * 0.44f); // 판정에 맞춘 적탄 이미지 영역
            } // 적탄 범위 종료
            else if (name == "bullet_player") // 아군 탄환 여백 확인
            { // 아군 범위 시작
                area = new Rect(texture.width * 0.35f, texture.height * 0.18f, texture.width * 0.30f, texture.height * 0.69f); // 아군 탄환 이미지 영역
            } // 아군 범위 종료
            Sprite sprite = Sprite.Create(texture, area, new Vector2(0.5f, 0.5f), Mathf.Max(area.width, area.height)); // 이미지 중심과 표시 크기
            cache[name] = sprite; // 이미지 재사용 등록
            return sprite; // 이미지 반환
        } // 조회 종료
        public static Sprite Player(CharacterType character) // 캐릭터 이미지 선택
        { // 선택 시작
            return Load(character == CharacterType.Sniper ? "player_sniper" : "player_rapid"); // 캐릭터별 이미지
        } // 선택 종료
        public static Sprite Enemy(EnemyKind kind) // 적 이미지 선택
        { // 선택 시작
            string name = kind == EnemyKind.Aimed ? "enemy_aimed" : kind == EnemyKind.Spread ? "enemy_spread" : kind == EnemyKind.Dash ? "enemy_dash" : kind == EnemyKind.MidBoss ? "boss_mid" : "boss_final"; // 종류별 이미지 이름
            return Load(name); // 적 이미지 반환
        } // 선택 종료
    } // 클래스 종료
} // 영역 종료
