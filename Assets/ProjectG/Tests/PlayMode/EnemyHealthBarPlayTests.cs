using System.Collections; // 코루틴 기능
using NUnit.Framework; // 테스트 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 플레이 시험 도구
public class EnemyHealthBarPlayTests // 실행 중 체력바 시험
{ // 시험 시작
    [UnityTest] // 플레이 모드 시험
    public IEnumerator DisableAndReuseHidePreviousDamage() // 실행 중 비활성과 재사용 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        try // 시험 범위
        { // 범위 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Aimed, null, null, null); // 적 초기화
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 체력바 조회
            enemy.ApplyDamage(1f, null); // 실제 피해
            Assert.That(bar.gameObject.activeSelf, Is.True); // 피해 후 표시 확인
            holder.SetActive(false); // 적 비활성화
            Assert.That(bar.gameObject.activeSelf, Is.False); // 실행 중 비활성 콜백 확인
            holder.SetActive(true); // 적 재활성화
            Assert.That(bar.gameObject.activeSelf, Is.False); // 재활성 시 이전 표시 방지
            enemy.Begin(EnemyKind.Aimed, null, null, null); // 재사용 초기화
            enemy.ApplyDamage(100f, null); // 적 처치
            Assert.That(bar.gameObject.activeSelf, Is.False); // 처치 시 표시 초기화
            yield return null; // 실제 프레임 진행
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Object.Destroy(holder); // 적 제거
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 정지 중 종료 시험
    public IEnumerator PlayerDeathHidesBarEvenWhenTimeIsPaused() // 사망과 정지 동시 처리 확인
    { // 시험 시작
        GameObject holder = new GameObject("Enemy"); // 적 객체
        GameObject playerObject = new GameObject("Player"); // 플레이어 객체
        float previousTimeScale = Time.timeScale; // 기존 시간 배율
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 초기화
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 플레이어 초기화
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Spread, player, null, null); // 적 초기화
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 체력바 조회
            enemy.ApplyDamage(1f, null); // 실제 피해
            Time.timeScale = 0f; // 전투 시간 정지
            player.Survival.TryHit(10000); // 플레이어 사망
            yield return null; // 종료 후 프레임 처리
            yield return null; // 갱신 순서 반영
            Assert.That(bar.gameObject.activeSelf, Is.False); // 정지 중 종료 표시 확인
        } // 범위 종료
        finally // 자원 정리
        { // 정리 시작
            Time.timeScale = previousTimeScale; // 기존 시간 배율 복원
            Object.Destroy(holder); // 적 제거
            Object.Destroy(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
