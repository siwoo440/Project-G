using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 전투 기능
using UnityEngine.TestTools; // 플레이 모드 시험
using ProjectG; // 출격 흐름 기능
public class BattleTransientCleanupTests // 출격 발사체 정리 시험
{ // 시험 시작
    [UnityTest] // 실제 재시도 정리 시험
    public IEnumerator RetryRemovesOldShotsAndBeamWithoutMovingThemWithPlayer() // 이전 전투 객체 정리 확인
    { // 시험 시작
        GameObject menu = new GameObject("TestMenu"); // 메뉴 객체 생성
        try // 시험 시작
        { // 시험 범위 시작
            Camera camera = menu.AddComponent<Camera>(); // 메뉴 카메라 생성
            GameSessionHost host = menu.AddComponent<GameSessionHost>(); // 출격 관리 구성
            host.Begin(camera); // 메뉴 초기화
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(host.Flow.SelectCharacter(CharacterType.Sniper), Is.True); // 저격형 선택
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(host.Flow.SelectCommonSkill(CommonSkill.Scatter), Is.True); // 산탄 선택
            Assert.That(host.Flow.StartRun(), Is.True); // 첫 출격
            BattlePlayer player = host.CurrentPlayer; // 첫 출격 플레이어
            Assert.That(player.TryUseRapid(), Is.True); // 고유 관통탄 발사
            Assert.That(player.TryUseCommon(), Is.True); // 공용 산탄 발사
            Assert.That(player.ApplyCard(CardId.C07), Is.True); // 드론 카드 획득
            player.Step(Vector2.zero, false, false, false, 0.01f, -8f, 8f, -5f, 5f); // 기본탄과 드론탄 발사
            player.Skills.GainEnergy(100); // 필살기 에너지 충전
            Assert.That(player.TryUseUltimate(), Is.True); // 저격 광선 발사
            Projectile[] shots = player.TransientRoot.GetComponentsInChildren<Projectile>(); // 해당 출격 탄환 조회
            BeamEffect beam = player.TransientRoot.GetComponentInChildren<BeamEffect>(); // 해당 출격 광선 조회
            Assert.That(shots.Length, Is.GreaterThanOrEqualTo(6)); // 모든 종류 탄환 생성
            Assert.That(beam, Is.Not.Null); // 광선 생성 확인
            Vector3[] positions = new Vector3[shots.Length]; // 이전 탄환 위치 목록
            for (int index = 0; index < shots.Length; index++) // 탄환 위치 순회
            { // 위치 기록 시작
                positions[index] = shots[index].transform.position; // 월드 위치 보관
            } // 위치 기록 종료
            player.transform.position += Vector3.right; // 플레이어만 이동
            for (int index = 0; index < shots.Length; index++) // 탄환 위치 재확인
            { // 위치 검사 시작
                Assert.That(shots[index].transform.position, Is.EqualTo(positions[index])); // 플레이어 이동과 독립
            } // 위치 검사 종료
            Assert.That(host.Flow.RetryBattle(), Is.True); // 같은 조건 재출격
            foreach (Projectile shot in shots) // 이전 탄환 순회
            { // 비활성 확인 시작
                Assert.That(shot == null || !shot.gameObject.activeInHierarchy, Is.True); // 같은 프레임 전투 중지
            } // 비활성 확인 종료
            Assert.That(beam == null || !beam.gameObject.activeInHierarchy, Is.True); // 광선 즉시 비활성화
            yield return null; // 출격 객체 파괴 대기
            foreach (Projectile shot in shots) // 이전 탄환 재확인
            { // 제거 확인 시작
                Assert.That(shot == null, Is.True); // 이전 탄환 파괴
            } // 제거 확인 종료
            Assert.That(beam == null, Is.True); // 이전 광선 파괴
            Assert.That(host.CurrentPlayer, Is.Not.SameAs(player)); // 새 출격 객체 확인
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(menu); // 메뉴와 출격 객체 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
