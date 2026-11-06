using System.Collections; // 코루틴 시험 기능
using NUnit.Framework; // 시험 도구
using ProjectG; // 강화 아이템 기능
using UnityEngine; // 유니티 객체
using UnityEngine.TestTools; // 플레이 모드 시험
[DefaultExecutionOrder(10000)] // 일반 업데이트보다 늦은 이동
public sealed class UpgradeDropFrameProbe : MonoBehaviour // 명시된 프레임 순서 관찰
{ // 관찰 시작
    public System.Action<int> OnTick; // 이동 프레임 처리
    public System.Action<int> OnLate; // 프레임 끝 결과 확인
    private int frame; // 경과한 시험 프레임
    private bool ticked; // 현재 프레임 이동 처리 여부
    private void Update() // 일반 업데이트 이후 이동
    { // 이동 시작
        OnTick?.Invoke(frame); // 현재 프레임 동작
        ticked = true; // 현재 프레임 이동 완료
    } // 이동 종료
    private void LateUpdate() // 이동 이후 결과 기록
    { // 기록 시작
        if (!ticked) // 첫 업데이트 대기 확인
        { // 조기 확인 시작
            return; // 업데이트 전 관찰 제외
        } // 조기 확인 종료
        OnLate?.Invoke(frame); // 현재 프레임 결과
        ticked = false; // 다음 업데이트 대기
        frame++; // 다음 프레임 진행
    } // 기록 종료
} // 관찰 종료
public class UpgradeDropOrderPlayTests // 실제 업데이트 순서 시험
{ // 시험 시작
    private sealed class ZeroRandom : System.Random // 고정 드롭 난수
    { // 난수 시작
        public override int Next(int maxValue) // 난수 요청
        { // 반환 시작
            return 0; // 첫 후보 선택
        } // 반환 종료
    } // 난수 종료
    private sealed class Effects : IUpgradeDropEffects // 격리된 획득 효과
    { // 효과 시작
        public int Acquired; // 획득 횟수
        public bool CanApply(UpgradeItemId id, UpgradeItemGrade grade) // 적용 가능 확인
        { // 확인 시작
            return true; // 시험 효과 허용
        } // 확인 종료
        public bool TryApply(UpgradeItemId id, UpgradeItemGrade grade, out string feedback) // 효과 적용
        { // 적용 시작
            Acquired++; // 획득 횟수 증가
            feedback = "획득"; // 획득 안내
            return true; // 획득 성공
        } // 적용 종료
    } // 효과 종료
    [UnityTest] // 이동 직후 획득 시험
    public IEnumerator PickupUsesMovementFromTheSameFrame() // 현재 프레임 접촉 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject root = new GameObject("DropOrderCamera"); // 격리 카메라
        GameObject playerObject = new GameObject("DropOrderPlayer"); // 격리 플레이어
        try // 시험 범위 준비
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 시간 진행
            Camera camera = root.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(-2f, 0f)); // 아이템 왼쪽 시작
            Effects effects = new Effects(); // 획득 관찰
            UpgradeDropManager manager = root.AddComponent<UpgradeDropManager>(); // 드롭 관리자
            manager.Begin(player, camera, effects, new ZeroRandom()); // 고정 드롭 연결
            Assert.That(manager.TryDrop(EnemyRank.Normal, Vector2.zero), Is.True); // 이동 전 아이템 생성
            int captured = -1; // 첫 프레임 결과
            UpgradeDropFrameProbe probe = root.AddComponent<UpgradeDropFrameProbe>(); // 늦은 이동 관찰
            probe.OnTick = frame => // 프레임별 이동 동작
            { // 이동 처리 시작
                if (frame == 0) // 첫 프레임 확인
                { // 첫 이동 시작
                    player.Step(Vector2.right, false, false, false, 0.5f, -8f, 8f, -5f, 5f); // 첫 프레임 이동
                } // 첫 이동 종료
            }; // 이동 처리 종료
            probe.OnLate = frame => // 프레임별 결과 기록
            { // 결과 기록 시작
                if (frame == 0) // 첫 프레임 확인
                { // 첫 결과 시작
                    captured = effects.Acquired; // 같은 프레임 획득 기록
                } // 첫 결과 종료
            }; // 결과 기록 종료
            for (int index = 0; index < 30 && captured < 0; index++) // 프레임 결과 대기
            { // 대기 시작
                yield return null; // 다음 프레임 확인
            } // 대기 종료
            Assert.That(captured, Is.EqualTo(1)); // 이동한 같은 프레임에 획득
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(root); // 카메라와 아이템 제거
            Object.Destroy(playerObject); // 플레이어 제거
            Time.timeScale = oldScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
    [UnityTest] // 이동 후 생성된 아이템 시험
    public IEnumerator PickupIgnoresMovementBeforeItemSpawn() // 생성 전 이동 제외 확인
    { // 시험 시작
        float oldScale = Time.timeScale; // 기존 시간 배율
        GameObject root = new GameObject("NewDropOrderCamera"); // 격리 카메라
        GameObject playerObject = new GameObject("NewDropOrderPlayer"); // 격리 플레이어
        try // 시험 범위 준비
        { // 시험 범위 시작
            Time.timeScale = 1f; // 전투 시간 진행
            Camera camera = root.AddComponent<Camera>(); // 카메라 구성
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 플레이어 구성
            player.Begin(new Vector2(-2f, 0f)); // 아이템 생성 전 왼쪽 위치
            Effects effects = new Effects(); // 획득 관찰
            UpgradeDropManager manager = root.AddComponent<UpgradeDropManager>(); // 드롭 관리자
            manager.Begin(player, camera, effects, new ZeroRandom()); // 고정 드롭 연결
            int captured = -1; // 두 번째 프레임 결과
            UpgradeDropFrameProbe probe = root.AddComponent<UpgradeDropFrameProbe>(); // 늦은 이동 관찰
            probe.OnTick = frame => // 프레임별 플레이어 이동
            { // 프레임 동작 시작
                if (frame == 0) // 첫 프레임 확인
                { // 생성 전 이동 시작
                    player.Step(Vector2.right, false, false, false, 1f, -8f, 8f, -5f, 5f); // 생성 위치를 지나 이동
                    Assert.That(manager.TryDrop(EnemyRank.Normal, Vector2.zero), Is.True); // 지나간 뒤 아이템 생성
                } // 생성 전 이동 종료
                else // 이후 프레임 확인
                { // 정지 처리 시작
                    player.Step(Vector2.zero, false, false, false, 0.001f, -8f, 8f, -5f, 5f); // 현재 위치 유지
                } // 정지 처리 종료
            }; // 프레임 동작 종료
            probe.OnLate = frame => // 프레임별 결과 기록
            { // 결과 기록 시작
                if (frame == 1) // 두 번째 프레임 확인
                { // 결과 보관 시작
                    captured = effects.Acquired; // 다음 프레임 결과 기록
                } // 결과 보관 종료
            }; // 결과 기록 종료
            for (int index = 0; index < 30 && captured < 0; index++) // 두 프레임 결과 대기
            { // 대기 시작
                yield return null; // 다음 프레임 확인
            } // 대기 종료
            Assert.That(captured, Is.Zero); // 생성 전 이동으로 획득 금지
            Assert.That(manager.ActiveCount, Is.EqualTo(1)); // 아이템 유지
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.Destroy(root); // 카메라와 아이템 제거
            Object.Destroy(playerObject); // 플레이어 제거
            Time.timeScale = oldScale; // 기존 시간 복원
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
