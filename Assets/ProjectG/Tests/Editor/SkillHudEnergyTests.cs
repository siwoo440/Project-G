using System.Reflection; // 표시 상태 조회 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 스킬 화면 기능
using UnityEngine; // 화면 색상과 객체
public class SkillHudEnergyTests // 필살기 에너지 표시 시험
{ // 시험 시작
    [Test] // 충전 상태별 아이콘 시험
    public void UltimateIconDarkensUntilActualEnergyIsFull() // 실제 에너지 경계 확인
    { // 시험 시작
        GameObject playerObject = new GameObject("EnergyPlayer"); // 시험 플레이어 생성
        GameObject cameraObject = new GameObject("EnergyCamera"); // 시험 카메라 생성
        try // 시험 범위 준비
        { // 시험 범위 시작
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero, CharacterType.Rapid); // 시험 출격 시작
            SkillHud hud = playerObject.AddComponent<SkillHud>(); // 스킬 화면 구성
            hud.Begin(player, cameraObject.AddComponent<Camera>()); // 화면 연결
            PropertyInfo property = typeof(SkillHud).GetProperty("UltimateIconTint"); // 표시 색상 조회
            Assert.That(property, Is.Not.Null); // 표시 상태 제공 확인
            Color empty = (Color)property.GetValue(hud); // 빈 에너지 색상
            Assert.That(empty.r, Is.LessThan(0.5f)); // 부족 시 어두운 아이콘
            player.Skills.GainEnergy(99); // 충전 직전 에너지
            Color almost = (Color)property.GetValue(hud); // 충전 직전 색상
            Assert.That(almost.r, Is.LessThan(0.5f)); // 부족 상태 유지
            player.Skills.GainEnergy(1); // 최대 에너지 충전
            Color full = (Color)property.GetValue(hud); // 충전 완료 색상
            Assert.That(full.r, Is.EqualTo(1f).Within(0.001f)); // 원래 밝기 복원
            Assert.That(player.Skills.TryUltimate(true), Is.True); // 필살기 사용
            Color spent = (Color)property.GetValue(hud); // 사용 후 색상
            Assert.That(spent.r, Is.LessThan(0.5f)); // 사용 후 어두운 아이콘 복원
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
