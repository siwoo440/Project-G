using NUnit.Framework; // 시험 도구
using ProjectG; // 적 전투 기능
using UnityEngine; // 화면 객체와 수학
public class EnemyVisualMotionTests // 적 이미지 움직임 시험
{ // 시험 시작
    [TestCase(EnemyKind.Aimed)] // 조준형 움직임
    [TestCase(EnemyKind.Spread)] // 확산형 움직임
    [TestCase(EnemyKind.Dash)] // 돌진형 움직임
    [TestCase(EnemyKind.MidBoss)] // 중간 보스 움직임
    [TestCase(EnemyKind.FinalBoss)] // 최종 보스 움직임
    public void EachEnemyAnimatesOnlyItsBodyAndResetsOnReuse(EnemyKind kind) // 종류별 움직임과 초기화 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("MotionEnemy"); // 적 시험 객체
        try // 시험 범위 준비
        { // 시험 시작
            Time.timeScale = 1f; // 전투 시간 진행
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(kind, null, null, null); // 종류별 적 시작
            Transform body = holder.transform.Find("EnemyBody"); // 표시 이미지
            Transform weak = holder.transform.Find("EnemyWeakPoint"); // 약점 표시
            Transform bar = holder.transform.Find("EnemyHealthBar"); // 체력 표시
            Vector3 baseScale = body.localScale; // 초기 이미지 크기
            Vector3 weakPosition = weak.localPosition; // 약점 기준 위치
            Vector3 barPosition = bar.localPosition; // 체력바 기준 위치
            enemy.Advance(0.37f); // 시험 시간 진행
            Assert.That(body.localPosition.y, Is.Not.EqualTo(0f).Within(0.00001f)); // 몸체 부유 확인
            Assert.That(Mathf.Abs(body.localEulerAngles.z), Is.GreaterThan(0.00001f)); // 몸체 기울기 확인
            Assert.That(body.localScale.x, Is.Not.EqualTo(baseScale.x).Within(0.00001f)); // 몸체 맥동 확인
            Assert.That(weak.localPosition, Is.EqualTo(weakPosition)); // 약점 기준 보존
            Assert.That(bar.localPosition, Is.EqualTo(barPosition)); // 체력바 기준 보존
            enemy.Begin(kind, null, null, null); // 동일 적 재사용
            Assert.That(body.localPosition, Is.EqualTo(Vector3.zero)); // 부유 위치 초기화
            Assert.That(body.localRotation, Is.EqualTo(Quaternion.identity)); // 기울기 초기화
            Assert.That(body.localScale, Is.EqualTo(baseScale)); // 원래 종류 크기 복원
        } // 시험 범위 종료
        finally // 시험 객체 정리
        { // 정리 시작
            Time.timeScale = oldScale; // 기존 시간 복원
            Object.DestroyImmediate(holder); // 적 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 적 종류별 움직임 차이 시험
    public void ReusedEnemyChangesMotionForEachKind() // 종류 변경과 움직임 차이 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("ReusedMotionEnemy"); // 재사용 적 객체
        try // 시험 준비
        { // 시험 시작
            Time.timeScale = 1f; // 전투 시간 진행
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            EnemyKind[] kinds = new EnemyKind[] // 시험 적 종류
            { // 종류 목록 시작
                EnemyKind.Aimed, EnemyKind.Spread, EnemyKind.Dash, EnemyKind.MidBoss, EnemyKind.FinalBoss // 다섯 종류
            }; // 종류 목록 종료
            float[] positions = new float[kinds.Length]; // 종류별 부유 위치
            for (int index = 0; index < kinds.Length; index++) // 종류별 반복
            { // 반복 시작
                enemy.Begin(kinds[index], null, null, null); // 다른 종류로 재사용
                Transform body = holder.transform.Find("EnemyBody"); // 몸체 이미지
                Assert.That(body.localPosition, Is.EqualTo(Vector3.zero)); // 재사용 부유 초기화
                Assert.That(body.localRotation, Is.EqualTo(Quaternion.identity)); // 재사용 각도 초기화
                enemy.Advance(0.37f); // 같은 시험 시간 진행
                positions[index] = body.localPosition.y; // 부유 위치 기록
            } // 반복 종료
            for (int first = 0; first < positions.Length; first++) // 첫 종류 선택
            { // 첫 반복 시작
                for (int second = first + 1; second < positions.Length; second++) // 다른 종류 비교
                { // 비교 시작
                    Assert.That(positions[first], Is.Not.EqualTo(positions[second]).Within(0.00001f)); // 서로 다른 부유 움직임
                } // 비교 종료
            } // 첫 반복 종료
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = oldScale; // 시간 배율 복원
            Object.DestroyImmediate(holder); // 적 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 전투 판정 독립성 시험
    public void MotionDoesNotMoveRootOrChangeCollision() // 몸체 연출과 판정 분리 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("StaticEnemy"); // 시험 적 객체
        try // 시험 준비
        { // 시험 시작
            Time.timeScale = 1f; // 전투 시간 진행
            holder.transform.position = new Vector3(2f, 3f, 0f); // 시험 월드 위치
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Spread, null, null, null); // 고정 위치 적 시작
            Vector3 before = holder.transform.position; // 적 기준 위치
            bool first = enemy.Intersects(new Vector2(2f, 2f), new Vector2(2f, 4f), out bool weakBefore); // 초기 탄환 판정
            enemy.Advance(0.31f); // 몸체 움직임 진행
            bool second = enemy.Intersects(new Vector2(2f, 2f), new Vector2(2f, 4f), out bool weakAfter); // 갱신 후 탄환 판정
            Assert.That(holder.transform.position, Is.EqualTo(before)); // 적 기준 위치 보존
            Assert.That(second, Is.EqualTo(first)); // 피해 판정 보존
            Assert.That(weakAfter, Is.EqualTo(weakBefore)); // 약점 판정 보존
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = oldScale; // 시간 배율 복원
            Object.DestroyImmediate(holder); // 적 객체 제거
        } // 정리 종료
    } // 시험 종료
    [Test] // 정지와 사망 시험
    public void PauseAndPlayerDeathStopVisualMotion() // 정지 중 몸체 변환 유지
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("PausedEnemy"); // 시험 적 객체
        GameObject playerObject = new GameObject("MotionPlayer"); // 시험 플레이어
        try // 시험 준비
        { // 시험 시작
            Time.timeScale = 1f; // 정상 시간 배율
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(Vector2.zero); // 시험 출격 시작
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 적 구성
            enemy.Begin(EnemyKind.Spread, player, null, null); // 적 시작
            Transform body = holder.transform.Find("EnemyBody"); // 표시 몸체
            enemy.Advance(0.31f); // 첫 움직임 진행
            Vector3 position = body.localPosition; // 첫 몸체 위치
            Quaternion rotation = body.localRotation; // 첫 몸체 각도
            Vector3 scale = body.localScale; // 첫 몸체 크기
            Time.timeScale = 0f; // 전투 일시정지
            enemy.Advance(1f); // 정지 중 명시적 진행 시도
            Assert.That(body.localPosition, Is.EqualTo(position)); // 정지 중 위치 보존
            Assert.That(body.localRotation, Is.EqualTo(rotation)); // 정지 중 각도 보존
            Assert.That(body.localScale, Is.EqualTo(scale)); // 정지 중 크기 보존
            Time.timeScale = 1f; // 전투 재개
            player.Survival.TryHit(10000); // 플레이어 사망
            enemy.Advance(1f); // 사망 후 진행 시도
            Assert.That(body.localPosition, Is.EqualTo(position)); // 사망 후 위치 보존
            Assert.That(body.localRotation, Is.EqualTo(rotation)); // 사망 후 각도 보존
            Assert.That(body.localScale, Is.EqualTo(scale)); // 사망 후 크기 보존
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = oldScale; // 기존 시간 복원
            Object.DestroyImmediate(holder); // 적 객체 제거
            Object.DestroyImmediate(playerObject); // 플레이어 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase(EnemyKind.MidBoss)] // 중간 보스 간격
    [TestCase(EnemyKind.FinalBoss)] // 최종 보스 간격
    public void BossMotionKeepsHealthBarBelowVisual(EnemyKind kind) // 체력바와 몸체 간격 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject holder = new GameObject("BossMotion"); // 보스 시험 객체
        try // 시험 준비
        { // 시험 시작
            Time.timeScale = 1f; // 전투 시간 진행
            EnemyActor enemy = holder.AddComponent<EnemyActor>(); // 보스 구성
            enemy.Begin(kind, null, null, null); // 보스 초기화
            enemy.ApplyDamage(1f, null); // 체력바 표시
            SpriteRenderer body = holder.transform.Find("EnemyBody").GetComponent<SpriteRenderer>(); // 몸체 표시
            SpriteRenderer frame = holder.transform.Find("EnemyHealthBar/Frame").GetComponent<SpriteRenderer>(); // 체력바 외곽
            for (int index = 0; index < 120; index++) // 다양한 동작 단계 확인
            { // 반복 시작
                enemy.Advance(0.05f); // 시험 동작 진행
                Assert.That(frame.bounds.max.y, Is.LessThan(body.bounds.min.y), "보스 체력바가 몸체와 겹침"); // 표시 사이 여백
            } // 반복 종료
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Time.timeScale = oldScale; // 기존 시간 복원
            Object.DestroyImmediate(holder); // 보스 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
