using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 이미지 기능
using ProjectG; // 전투 이미지 기능
public class BattleArtworkTests // 생성 이미지 연결 시험
{ // 시험 시작
    [TestCase("experience_pickup")] // 경험치 조각 이미지
    [TestCase("attack_warning")] // 공격 경고 이미지
    [TestCase("weakpoint_reticle")] // 약점 조준 이미지
    [TestCase("support_drone")] // 보조 드론 이미지
    [TestCase("support_blade")] // 보조 칼날 이미지
    [TestCase("beam_magic")] // 광선 이미지
    [TestCase("training_target")] // 시험 표적 이미지
    [TestCase("ui_card_frame")] // 카드 배경 이미지
    public void GeneratedArtworkExists(string name) // 생성 자원 확인
    { // 시험 시작
        Assert.That(Resources.Load<Texture2D>("BattleArt/" + name), Is.Not.Null); // 이미지 자원 존재
    } // 시험 종료
    [Test] // 게임 표시 연결 시험
    public void MarkersAndBeamUseGeneratedArtwork() // 표시 이미지 적용 확인
    { // 시험 시작
        GameObject target = null; // 시험 표적 참조
        try // 시험 시작
        { // 시험 범위 시작
            Assert.That(BattleVisuals.Crystal.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/experience_pickup"))); // 경험치 이미지
            Assert.That(BattleVisuals.Warning.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/attack_warning"))); // 경고 이미지
            Assert.That(BattleVisuals.Weak.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/weakpoint_reticle"))); // 약점 이미지
            Assert.That(BattleVisuals.Drone.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/support_drone"))); // 드론 이미지
            Assert.That(BattleVisuals.Blade.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/support_blade"))); // 칼날 이미지
            target = TrainingTarget.Create(Vector2.zero).gameObject; // 시험 표적 생성
            Assert.That(target.GetComponent<SpriteRenderer>().sprite.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/training_target"))); // 표적 이미지
            BeamEffect.Create(new Rect(0f, 0f, 1f, 4f)); // 시험 광선 생성
            BeamEffect beam = Object.FindFirstObjectByType<BeamEffect>(); // 광선 조회
            Assert.That(beam.GetComponent<SpriteRenderer>().sprite.texture, Is.SameAs(Resources.Load<Texture2D>("BattleArt/beam_magic"))); // 광선 이미지
            Assert.That(beam.GetComponent<SpriteRenderer>().bounds.size.x, Is.EqualTo(1f).Within(0.001f)); // 피해 영역 가로 유지
            Assert.That(beam.GetComponent<SpriteRenderer>().bounds.size.y, Is.EqualTo(4f).Within(0.001f)); // 피해 영역 세로 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (target != null) // 표적 존재 확인
            { // 정리 시작
                Object.DestroyImmediate(target); // 시험 표적 제거
            } // 정리 종료
            foreach (BeamEffect beam in Object.FindObjectsByType<BeamEffect>(FindObjectsSortMode.None)) // 광선 순회
            { // 정리 시작
                Object.DestroyImmediate(beam.gameObject); // 시험 광선 제거
            } // 정리 종료
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
