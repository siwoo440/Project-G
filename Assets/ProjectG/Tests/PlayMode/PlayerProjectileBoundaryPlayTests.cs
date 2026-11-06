using System.Collections; // 실제 프레임 시험
using NUnit.Framework; // 시험 도구
using ProjectG; // 탄환과 표적 기능
using UnityEngine; // 카메라와 객체 기능
using UnityEngine.TestTools; // 플레이 모드 시험
public class PlayerProjectileBoundaryPlayTests // 아군 탄환 화면 경계 시험
{ // 시험 시작
    [UnityTest] // 보이지 않는 영역의 충돌 시험
    public IEnumerator ShotCannotDamageTargetEntirelyOutsideBattleView() // 화면 밖 표적의 숨은 피해 방지
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("BattleCamera"); // 전투 카메라 객체
        GameObject targetObject = new GameObject("OutsideTarget"); // 화면 밖 시험 표적
        GameObject shotObject = new GameObject("TestProjectile"); // 실제 아군 탄환
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 진행
            Camera camera = cameraObject.AddComponent<Camera>(); // 실제 카메라
            camera.orthographic = true; // 전투 직교 카메라
            camera.orthographicSize = 5f; // 기존 세로 반경
            cameraObject.AddComponent<BattleViewport>(); // 4대3 전투 영역
            float right = camera.orthographicSize * camera.aspect; // 실제 전투 오른쪽 경계
            targetObject.transform.position = new Vector3(right + 1.75f, 0f, 0f); // 표적 전체가 화면 밖인 위치
            TrainingTarget target = targetObject.AddComponent<TrainingTarget>(); // 실제 충돌 표적
            shotObject.transform.SetParent(cameraObject.transform, false); // 전투 생성물 소속
            shotObject.transform.position = new Vector3(right - 0.2f, 0f, 0f); // 전투 경계 안 탄환
            Projectile shot = shotObject.AddComponent<Projectile>(); // 실제 탄환 기능
            shot.Begin(Vector2.right * 10f, 5f, 10f); // 오른쪽 이동과 실제 피해
            yield return new WaitForSeconds(0.4f); // 화면 밖 경로 통과 대기
            Assert.That(target.TotalDamage, Is.Zero); // 보이지 않는 표적 피해 차단
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (shotObject != null) // 탄환 존재 확인
            { // 탄환 정리 시작
                Object.Destroy(shotObject); // 시험 탄환 제거
            } // 탄환 정리 종료
            Object.Destroy(targetObject); // 시험 표적 제거
            Object.Destroy(cameraObject); // 전투 카메라 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 경계 직전 관통과 연속 충돌 시험
    public IEnumerator FastPiercingShotHitsVisibleTargetsBeforeLeavingButNotOutside() // 경계 이전 피해와 화면 밖 차단
    { // 시험 시작
        float previousScale = Time.timeScale; // 기존 시간 배율
        GameObject cameraObject = new GameObject("BattleCamera"); // 전투 카메라 객체
        GameObject firstObject = new GameObject("FirstVisibleTarget"); // 첫 화면 안 표적
        GameObject secondObject = new GameObject("SecondVisibleTarget"); // 경계에 걸친 표적
        GameObject outsideObject = new GameObject("OutsideTarget"); // 화면 밖 시험 표적
        GameObject shotObject = new GameObject("PiercingProjectile"); // 실제 관통 탄환
        try // 시험 범위
        { // 범위 시작
            Time.timeScale = 1f; // 전투 시간 진행
            Camera camera = cameraObject.AddComponent<Camera>(); // 실제 카메라
            camera.orthographic = true; // 전투 직교 카메라
            camera.orthographicSize = 5f; // 기존 세로 반경
            cameraObject.AddComponent<BattleViewport>(); // 4대3 전투 영역
            float right = camera.orthographicSize * camera.aspect; // 실제 오른쪽 전투 경계
            firstObject.transform.position = new Vector3(right - 1.5f, 0f, 0f); // 첫 표적 위치
            secondObject.transform.position = new Vector3(right - 0.3f, 0f, 0f); // 화면 안에 걸친 둘째 표적
            outsideObject.transform.position = new Vector3(right + 1.75f, 0f, 0f); // 몸체 전체가 화면 밖인 위치
            TrainingTarget first = firstObject.AddComponent<TrainingTarget>(); // 첫 실제 표적
            TrainingTarget second = secondObject.AddComponent<TrainingTarget>(); // 둘째 실제 표적
            TrainingTarget outside = outsideObject.AddComponent<TrainingTarget>(); // 화면 밖 실제 표적
            shotObject.transform.SetParent(cameraObject.transform, false); // 전투 생성물 소속
            shotObject.transform.position = new Vector3(right - 3f, 0f, 0f); // 두 표적 앞 탄환 위치
            Projectile shot = shotObject.AddComponent<Projectile>(); // 실제 관통 탄환
            shot.Begin(Vector2.right * 200f, 5f, 10f, true, null, 3); // 빠른 탄환과 세 번 관통
            yield return new WaitForSeconds(0.15f); // 실제 충돌 구간 진행
            Assert.That(first.TotalDamage, Is.EqualTo(10f)); // 첫 화면 안 표적 피해
            Assert.That(second.TotalDamage, Is.EqualTo(10f)); // 둘째 화면 안 표적 관통 피해
            Assert.That(outside.TotalDamage, Is.Zero); // 화면 밖 표적 피해 차단
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (shotObject != null) // 탄환 존재 확인
            { // 탄환 정리 시작
                Object.Destroy(shotObject); // 시험 탄환 제거
            } // 탄환 정리 종료
            Object.Destroy(firstObject); // 첫 표적 제거
            Object.Destroy(secondObject); // 둘째 표적 제거
            Object.Destroy(outsideObject); // 화면 밖 표적 제거
            Object.Destroy(cameraObject); // 전투 카메라 제거
            Time.timeScale = previousScale; // 기존 시간 배율 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
