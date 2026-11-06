using System.Collections; // 실제 프레임 시험
using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 기능
using UnityEngine; // 유니티 객체
using UnityEngine.SceneManagement; // 씬 전환 기능
using UnityEngine.TestTools; // 플레이 시험 기능
public class SceneEnemySpawnPlayTests // 실제 전투 씬의 적 생성 시험
{ // 시험 시작
    [UnityTest] // 실제 씬 출격과 템플릿 시험
    public IEnumerator SpawnUsesTheEditedBattleSceneTemplate() // 씬에서 편집한 원본 사용 확인
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameSessionHost host = null; // 실제 메뉴 관리자
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 정상 전투 시간
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); // 실제 메뉴 씬 열기
            yield return null; // 실제 메뉴 초기화 대기
            host = Object.FindFirstObjectByType<GameSessionHost>(); // 실제 메뉴 관리자 조회
            Assert.That(host, Is.Not.Null); // 메뉴 연결 확인
            Assert.That(host.Flow.OpenCharacterSelection(), Is.True); // 캐릭터 선택 진입
            Assert.That(host.Flow.OpenPreparation(), Is.True); // 출격 준비 진입
            Assert.That(host.Flow.StartRun(), Is.True); // 실제 출격 요청
            yield return BattleSceneTestWait.UntilReady(host); // 전투 씬 연결 완료 대기
            StageDirector stage = host.CurrentStage; // 실제 씬의 스테이지
            Assert.That(stage.HasSceneTemplates, Is.True); // 실제 템플릿 연결 확인
            Transform template = stage.transform.Find("AimedTemplate"); // 실제 조준형 원본
            Assert.That(template, Is.Not.Null); // 씬의 원본 존재 확인
            SpriteRenderer edited = template.Find("EnemyBody").GetComponent<SpriteRenderer>(); // 편집 가능한 실제 몸체
            Color chosen = new Color(0.31f, 0.57f, 0.79f, 1f); // 고유 편집 색상
            Vector3 size = new Vector3(2.2f, 3.3f, 1f); // 고유 편집 크기
            edited.color = chosen; // 씬 인스턴스 색상 편집
            edited.transform.localScale = size; // 씬 인스턴스 크기 편집
            stage.Advance(7f, 7f); // 준비 구간 적 등장 진행
            EnemyActor[] spawned = stage.GetComponentsInChildren<EnemyActor>(); // 실제 활성 적 조회
            bool found = false; // 편집 원본 사용 여부
            foreach (EnemyActor enemy in spawned) // 등장 적 순회
            { // 확인 시작
                SpriteRenderer body = enemy.transform.Find("EnemyBody").GetComponent<SpriteRenderer>(); // 실제 적 몸체
                if (body.color == chosen && body.transform.localScale == size) // 실제 편집값 확인
                { // 발견 시작
                    found = true; // 편집 원본 사용 기록
                } // 발견 종료
            } // 확인 종료
            Assert.That(found, Is.True); // 실제 등장 적에 편집값 반영
            Assert.That(template.gameObject.activeSelf, Is.False); // 원본은 비활성 유지
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
