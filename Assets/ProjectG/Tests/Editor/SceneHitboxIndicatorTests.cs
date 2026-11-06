using NUnit.Framework; // 시험 도구
using ProjectG; // 실제 피격 표시
using UnityEngine; // 유니티 객체와 이미지
public class SceneHitboxIndicatorTests // 직렬화된 피격 표시 시험
{ // 시험 시작
    [Test] // 실제 씬 이미지 보존 시험
    public void ExistingRendererAndAuthoredGeometryRemainUnchanged() // 편집한 표시와 반경 보존
    { // 시험 시작
        GameObject playerObject = new GameObject("HitboxPlayer"); // 격리 플레이어
        GameObject indicatorObject = new GameObject("HitboxVisual"); // 편집 가능한 표시 객체
        Texture2D texture = new Texture2D(2, 2); // 격리 이미지
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 10f); // 실제 이미지
        try // 시험 범위
        { // 범위 시작
            playerObject.transform.localScale = new Vector3(2f, 2f, 1f); // 편집한 플레이어 크기
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 실제 플레이어
            player.Begin(Vector2.zero); // 전투 상태 준비
            indicatorObject.transform.SetParent(playerObject.transform, false); // 플레이어 중심 표시
            indicatorObject.transform.localScale = new Vector3(0.6f, 0.6f, 1f); // 실제 지름 0.24에 맞춘 편집 크기
            SpriteRenderer image = indicatorObject.AddComponent<SpriteRenderer>(); // 직렬화할 실제 이미지
            image.sprite = sprite; // 편집한 스프라이트
            image.color = new Color(0.4f, 0.7f, 1f, 0.8f); // 편집한 표시 색상
            HitboxIndicator indicator = indicatorObject.AddComponent<HitboxIndicator>(); // 실제 표시 동작
            indicator.Begin(player); // 실제 출격 표시 연결
            Assert.That(indicatorObject.GetComponents<SpriteRenderer>().Length, Is.EqualTo(1)); // 중복 렌더러 방지
            Assert.That(image.sprite, Is.SameAs(sprite)); // 편집한 이미지 유지
            Assert.That(image.color, Is.EqualTo(new Color(0.4f, 0.7f, 1f, 0.8f))); // 편집한 색상 유지
            Assert.That(indicatorObject.transform.localScale, Is.EqualTo(new Vector3(0.6f, 0.6f, 1f))); // 편집 크기 유지
            Assert.That(image.bounds.size.x, Is.EqualTo(0.24f).Within(0.001f)); // 피격 반경 0.12의 표시 지름
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어와 표시 제거
            Object.DestroyImmediate(sprite); // 시험 스프라이트 제거
            Object.DestroyImmediate(texture); // 시험 텍스처 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
