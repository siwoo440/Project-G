using NUnit.Framework; // 시험 도구
using UnityEngine; // 유니티 이미지 기능
using ProjectG; // 전투 이미지 기능
public class BattleSpritesTests // 전투 이미지 시험
{ // 시험 시작
    [TestCase("player_rapid")] // 속사형 이미지
    [TestCase("player_sniper")] // 저격형 이미지
    [TestCase("enemy_aimed")] // 조준형 적 이미지
    [TestCase("enemy_spread")] // 부채탄 적 이미지
    [TestCase("enemy_dash")] // 돌진형 적 이미지
    [TestCase("boss_mid")] // 중간 보스 이미지
    [TestCase("boss_final")] // 최종 보스 이미지
    [TestCase("bullet_player")] // 플레이어 탄환 이미지
    [TestCase("bullet_enemy")] // 적탄 이미지
    public void SpriteLoadsGeneratedTexture(string name) // 이미지 자원 연결 확인
    { // 시험 시작
        Texture2D texture = Resources.Load<Texture2D>("BattleArt/" + name); // 생성 이미지 조회
        Assert.That(texture, Is.Not.Null); // 이미지 자원 존재 확인
        Assert.That(BattleSprites.Load(name).texture, Is.SameAs(texture)); // 생성 이미지 연결 확인
    } // 시험 종료
    [Test] // 캐릭터 변경 이미지 시험
    public void PlayerUsesCharacterSpecificImage() // 선택 캐릭터 이미지 확인
    { // 시험 시작
        GameObject holder = new GameObject("Player"); // 플레이어 객체
        try // 시험 시작
        { // 시험 범위 시작
            SpriteRenderer image = holder.AddComponent<SpriteRenderer>(); // 이미지 구성
            BattlePlayer player = holder.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero, CharacterType.Rapid); // 속사형 선택
            Assert.That(image.sprite.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/player_rapid"))); // 속사형 이미지
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형 선택
            Assert.That(image.sprite.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/player_sniper"))); // 저격형 이미지
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(EnemyKind.Aimed, "enemy_aimed")] // 조준형 적
    [TestCase(EnemyKind.Spread, "enemy_spread")] // 부채탄 적
    [TestCase(EnemyKind.Dash, "enemy_dash")] // 돌진형 적
    [TestCase(EnemyKind.MidBoss, "boss_mid")] // 중간 보스
    [TestCase(EnemyKind.FinalBoss, "boss_final")] // 최종 보스
    public void EnemyUsesMatchingImage(EnemyKind kind, string name) // 적 종류별 이미지 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        try // 시험 시작
        { // 시험 범위 시작
            EnemyActor enemy = EnemyActorTestFactory.AttachActor(holder, kind); // 적 구성
            enemy.Begin(kind, null, null, null); // 적 초기화
            SpriteRenderer image = holder.transform.Find("EnemyBody").GetComponent<SpriteRenderer>(); // 몸체 이미지 조회
            Assert.That(image.sprite.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/" + name))); // 적 이미지 확인
            bool boss = kind == EnemyKind.MidBoss || kind == EnemyKind.FinalBoss; // 보스 구분
            Assert.That(image.transform.localScale.x, Is.EqualTo(boss ? 6.6f : 3f).Within(0.0001f)); // 적 표시 가로 세 배
            Assert.That(image.transform.localScale.y, Is.EqualTo(boss ? 3.3f : 2.1f).Within(0.0001f)); // 적 표시 세로 세 배
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 아군과 적 탄환 구분 시험
    public void BulletsUseDifferentImages() // 탄환 이미지 확인
    { // 시험 시작
        GameObject holder = new GameObject("PlayerBullet"); // 플레이어 탄환 객체
        GameObject poolObject = new GameObject("Pool"); // 적탄 풀 객체
        try // 시험 시작
        { // 시험 범위 시작
            Projectile shot = holder.AddComponent<Projectile>(); // 플레이어 탄환 구성
            shot.Begin(Vector2.up, 5f); // 플레이어 탄환 초기화
            EnemyBulletPool pool = EnemyBulletEditorTestFactory.Create(poolObject); // 적탄 풀 구성
            EnemyBullet bullet = pool.Spawn(Vector2.zero, Vector2.down, 0.06f, 10, -5f, null); // 적탄 생성
            SpriteRenderer bulletImage = bullet.GetComponentInChildren<SpriteRenderer>(); // 실제 편집 이미지
            Assert.That(holder.GetComponent<SpriteRenderer>().sprite.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/bullet_player"))); // 플레이어 탄환 이미지
            Assert.That(bulletImage.sprite.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/bullet_enemy"))); // 적탄 이미지
            Assert.That(bullet.Radius, Is.EqualTo(0.06f).Within(0.0001f)); // 적탄 판정 크기 유지
            Assert.That(holder.transform.localScale.x, Is.EqualTo(0.3f).Within(0.0001f)); // 아군 탄환 가로 세 배
            Assert.That(holder.transform.localScale.y, Is.EqualTo(0.72f).Within(0.0001f)); // 아군 탄환 세로 세 배
            Assert.That(bulletImage.bounds.size.x, Is.EqualTo(0.36f).Within(0.0001f)); // 적탄 실제 표시 세 배
            Assert.That(bulletImage.sprite.rect.width, Is.EqualTo(bulletImage.sprite.texture.width * 0.44f).Within(0.01f)); // 적탄 이미지 투명 여백 제한
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 플레이어 탄환 제거
            Object.DestroyImmediate(poolObject); // 적탄 풀 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 시험 표적 표시 크기 시험
    public void TrainingTargetGrowsWithoutChangingWeakPointIndicator() // 표적 확대와 판정 표시 확인
    { // 시험 시작
        TrainingTarget target = TrainingTarget.Create(Vector2.zero); // 시험 표적 생성
        try // 시험 시작
        { // 시험 범위 시작
            Assert.That(target.transform.localScale.x, Is.EqualTo(3.9f).Within(0.0001f)); // 몸체 가로 세 배
            Assert.That(target.transform.localScale.y, Is.EqualTo(2.1f).Within(0.0001f)); // 몸체 세로 세 배
            Transform marker = target.transform.Find("WeakPoint"); // 약점 표시 조회
            Assert.That(marker.lossyScale.x, Is.EqualTo(0.32f).Within(0.0001f)); // 약점 가로 판정 표시 유지
            Assert.That(marker.lossyScale.y, Is.EqualTo(0.7f).Within(0.0001f)); // 약점 세로 판정 표시 유지
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(target.gameObject); // 시험 표적 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
