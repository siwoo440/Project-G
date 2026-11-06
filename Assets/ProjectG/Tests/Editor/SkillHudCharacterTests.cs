using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 화면 기능
using UnityEngine; // 유니티 객체 기능
public class SkillHudCharacterTests // 캐릭터별 고유기 화면 시험
{ // 시험 시작
    [Test] // 캐릭터 변경 시험
    public void UniqueSkillIconFollowsCurrentCharacter() // 캐릭터별 아이콘 확인
    { // 시험 시작
        GameObject cameraObject = new GameObject("IconCamera"); // 시험 카메라 생성
        GameObject playerObject = new GameObject("IconPlayer"); // 시험 플레이어 생성
        try // 시험 범위 준비
        { // 시험 범위 시작
            Camera camera = cameraObject.AddComponent<Camera>(); // 시험 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            SkillHud hud = playerObject.AddComponent<SkillHud>(); // 스킬 화면 구성
            player.Begin(Vector2.zero, CharacterType.Rapid); // 속사형 출격
            hud.Begin(player, camera); // 스킬 화면 연결
            Texture2D rapid = Resources.Load<Texture2D>("SkillIcons/rapid"); // 속사형 아이콘
            Texture2D sniper = Resources.Load<Texture2D>("SkillIcons/sniper"); // 저격형 아이콘
            Assert.That(rapid, Is.Not.Null); // 속사형 이미지 존재
            Assert.That(sniper, Is.Not.Null); // 저격형 이미지 존재
            Assert.That(sniper, Is.Not.SameAs(rapid)); // 서로 다른 이미지 사용
            Assert.That(hud.UniqueIcon, Is.SameAs(rapid)); // 속사형 고유기 표시
            player.Begin(Vector2.zero, CharacterType.Sniper); // 저격형으로 변경
            Assert.That(hud.UniqueIcon, Is.SameAs(sniper)); // 저격형 고유기 표시
            player.Begin(Vector2.zero, CharacterType.Rapid); // 속사형으로 복귀
            Assert.That(hud.UniqueIcon, Is.SameAs(rapid)); // 캐릭터 복귀 반영
        } // 시험 범위 종료
        finally // 시험 자원 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 시험 플레이어 제거
            Object.DestroyImmediate(cameraObject); // 시험 카메라 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
