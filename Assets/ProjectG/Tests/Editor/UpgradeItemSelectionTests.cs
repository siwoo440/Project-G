using NUnit.Framework; // 테스트 도구
using UnityEngine; // 유니티 전투 기능
using ProjectG; // 강화 아이템 기능
public class UpgradeItemSelectionTests // 강화 아이템 선택 시험
{ // 시험 시작
    private GameObject holder; // 플레이어 객체
    private GameObject cameraObject; // 카메라 객체
    private GameObject selectionObject; // 선택 객체
    private BattlePlayer player; // 플레이어 상태
    private Camera camera; // 전투 카메라
    private BattleProgress progress; // 카드 선택 상태
    private UpgradeItemSelection selection; // 강화 선택 상태
    [SetUp] // 시험 준비
    public void SetUp() // 전투와 선택 구성
    { // 준비 시작
        Time.timeScale = 1f; // 시험 시간 초기화
        holder = new GameObject("Player"); // 플레이어 객체 생성
        player = holder.AddComponent<BattlePlayer>(); // 전투 상태 추가
        player.Begin(Vector2.zero); // 출격 상태 초기화
        cameraObject = new GameObject("Camera"); // 카메라 객체 생성
        camera = cameraObject.AddComponent<Camera>(); // 카메라 구성
        camera.orthographic = true; // 직교 화면 설정
        camera.orthographicSize = 5f; // 화면 높이 설정
        camera.aspect = 16f / 9f; // 화면 비율 설정
        progress = holder.AddComponent<BattleProgress>(); // 카드 진행 추가
        progress.Begin(player, camera); // 카드 진행 연결
        selectionObject = new GameObject("UpgradeSelection"); // 선택 객체 생성
        selection = selectionObject.AddComponent<UpgradeItemSelection>(); // 선택 구성 요소
    } // 준비 종료
    [TearDown] // 시험 정리
    public void TearDown() // 시험 객체 제거
    { // 정리 시작
        Object.DestroyImmediate(selectionObject); // 선택 객체 제거
        Object.DestroyImmediate(holder); // 플레이어 객체 제거
        Object.DestroyImmediate(cameraObject); // 카메라 객체 제거
        Time.timeScale = 1f; // 전투 시간 복원
    } // 정리 종료
    [Test] // 필드 선택 시험
    public void MovementChoosesExactlyOneAndRestoresSafety() // 이동 선택과 복귀 확인
    { // 시험 시작
        int completed = 0; // 완료 알림 횟수
        selection.ChoiceCompleted += () => completed++; // 완료 알림 연결
        Assert.That(selection.Begin(player, camera, progress, null), Is.True); // 선택 시작
        Assert.That(selection.ChoiceCount, Is.EqualTo(2)); // 서로 다른 두 아이템
        Assert.That(selection.GetChoice(0), Is.Not.EqualTo(selection.GetChoice(1))); // 중복 후보 방지
        Assert.That(progress.Blocked, Is.True); // 카드 선택 대기
        Assert.That(player.Survival.IsInvulnerable, Is.True); // 선택 중 안전 확인
        UpgradeItemId selected = selection.GetChoice(0); // 첫 아이템 종류
        player.transform.position = selection.GetPosition(0); // 아이템 위치 이동
        selection.Advance(0.6f); // 안전 시간 후 접촉 처리
        Assert.That(selection.IsOpen, Is.False); // 선택 종료 확인
        Assert.That(player.Build.Upgrades.Count(selected), Is.EqualTo(1)); // 아이템 한 개 적용
        Assert.That(completed, Is.EqualTo(1)); // 완료 알림 1회
        Assert.That(progress.Blocked, Is.False); // 카드 선택 복원
        Assert.That(player.Survival.IsInvulnerable, Is.True); // 선택 직후 안전 유예
        player.Survival.Advance(0.51f); // 안전 유예 시간 경과
        Assert.That(player.Survival.IsInvulnerable, Is.False); // 안전 유예 종료
        Assert.That(selection.TryChoose(0), Is.False); // 중복 선택 차단
    } // 시험 종료
    [Test] // 카드 선택 충돌 시험
    public void PendingCardChoiceDefersUpgradeItems() // 카드 대기 중 중복 표시 방지
    { // 시험 시작
        progress.Level.Gain(10); // 레벨업 선택 예약
        Assert.That(selection.Begin(player, camera, progress, null), Is.False); // 카드 우선 처리
        Assert.That(selection.IsOpen, Is.False); // 아이템 화면 미표시
        Assert.That(player.Survival.IsInvulnerable, Is.False); // 불필요한 무적 방지
    } // 시험 종료
    [Test] // 재출격 정리 시험
    public void RestartCancelsChoiceAndRestoresCardState() // 출격 초기화 확인
    { // 시험 시작
        Assert.That(selection.Begin(player, camera, progress, null), Is.True); // 선택 시작
        player.Begin(Vector2.zero); // 새 출격 시작
        Assert.That(selection.IsOpen, Is.False); // 기존 아이템 제거
        Assert.That(progress.Blocked, Is.False); // 카드 제한 복원
        Assert.That(player.Survival.IsInvulnerable, Is.False); // 새 출격 안전 상태
    } // 시험 종료
} // 시험 종료
