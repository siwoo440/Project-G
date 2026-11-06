using System.Collections; // 실제 프레임 시험
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체 기능
using UnityEngine.SceneManagement; // 씬 전환 기능
using UnityEngine.TestTools; // 플레이 시험 기능
public class ScenePlayerVisualPlayTests // 실제 출격의 플레이어 이미지 시험
{ // 시험 시작
    [UnityTest] // 아리아 실제 출격 시험
    public IEnumerator RapidUsesFullSceneSpriteAtPriorWorldSize() // 아리아 화면 크기 확인
    { // 시험 시작
        yield return RunCharacter(CharacterType.Rapid, "player_rapid"); // 실제 아리아 출격 확인
    } // 시험 종료
    [UnityTest] // 리안 실제 출격 시험
    public IEnumerator SniperUsesFullSceneSpriteAtPriorWorldSize() // 리안 화면 크기 확인
    { // 시험 시작
        yield return RunCharacter(CharacterType.Sniper, "player_sniper"); // 실제 리안 출격 확인
    } // 시험 종료
    private static IEnumerator RunCharacter(CharacterType character, string expectedTexture) // 실제 캐릭터 출격 확인
    { // 시험 시작
        GameSessionHost host = null; // 실제 메뉴 관리자
        float previousScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 정상 전투 시간
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 메뉴 씬 열기
            yield return null; // 메뉴 초기화 대기
            host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 메뉴 관리자 조회
            Assert.That(host, Is.Not.Null); // 메뉴 연결 확인
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(host.Flow.SelectCharacter(character), Is.True); // 해당 캐릭터 선택
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(host.Flow.StartRun(), Is.True); // 실제 출격 요청
            yield return BattleSceneTestWait.UntilReady(host); // 실제 씬 로드 완료
            BattlePlayer player = host.CurrentPlayer; // 씬에 저장된 실제 플레이어
            SpriteRenderer body = player.GetComponent<SpriteRenderer>(); // 실제 캐릭터 이미지
            Assert.That(body.enabled && body.gameObject.activeInHierarchy, Is.True); // 활성 렌더러 확인
            Assert.That(body.sprite.texture.name, Is.EqualTo(expectedTexture)); // 선택한 전체 원본 확인
            Assert.That(body.sprite.rect.width, Is.EqualTo(body.sprite.texture.width)); // 원본 전체 가로 범위
            Assert.That(body.sprite.rect.height, Is.EqualTo(body.sprite.texture.height)); // 원본 전체 세로 범위
            Assert.That(body.bounds.size.x, Is.EqualTo(1.125f).Within(0.001f)); // 기존 월드 표시 너비
            HitboxIndicator marker = player.GetComponentInChildren<HitboxIndicator>(true); // 실제 피격 표시
            SpriteRenderer markerImage = marker.GetComponent<SpriteRenderer>(); // 실제 표시 이미지
            Assert.That(markerImage.sprite.bounds.size.x * Mathf.Abs(marker.transform.lossyScale.x), Is.EqualTo(0.24f).Within(0.001f)); // 피격 반경 지름
            Color editedColor = new Color(0.25f, 0.7f, 0.9f, 0.8f); // 편집한 렌더러 색상
            Vector3 editedScale = new Vector3(1.4f, 1.6f, 1f); // 편집한 플레이어 크기
            Sprite editedSprite = body.sprite; // 씬에서 설정한 이미지
            body.color = editedColor; // 시험용 Inspector 색상 편집
            player.transform.localScale = editedScale; // 시험용 Inspector 크기 편집
            player.Begin(player.transform.position, character); // 실제 캐릭터 초기화 재실행
            Assert.That(body.color, Is.EqualTo(editedColor)); // 편집한 색상 보존
            Assert.That(player.transform.localScale, Is.EqualTo(editedScale)); // 편집한 크기 보존
            Assert.That(body.sprite, Is.SameAs(editedSprite)); // 편집된 씬 이미지 보존
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = previousScale; // 기존 시간 배율 복원
            if (host != null) // 실제 관리자 확인
            { // 제거 시작
                Object.Destroy(host.gameObject); // 메뉴와 실제 전투 정리
            } // 제거 종료
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
