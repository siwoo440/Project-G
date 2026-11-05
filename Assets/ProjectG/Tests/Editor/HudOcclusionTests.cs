using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 화면 기능
using ProjectG; // 전투 HUD 기능
public class HudOcclusionTests // HUD 겹침 표시 시험
{ // 시험 시작
    [Test] // 겹침 표시 시험
    public void FadesOnlyWhenPlayerOverlapsHud() // 겹침별 투명도 확인
    { // 시험 시작
        Rect hud = new Rect(10f, 10f, 100f, 40f); // 시험 HUD 영역
        Assert.That(HudOcclusion.Alpha(hud, new Rect(50f, 20f, 10f, 10f)), Is.EqualTo(0.35f).Within(0.001f)); // 겹친 경우 투명 처리
        Assert.That(HudOcclusion.Alpha(hud, new Rect(200f, 20f, 10f, 10f)), Is.EqualTo(1f)); // 떨어진 경우 원래 표시
    } // 시험 종료
} // 시험 종료
