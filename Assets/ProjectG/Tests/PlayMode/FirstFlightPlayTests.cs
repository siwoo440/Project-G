using System.Collections; // 프레임 대기
using NUnit.Framework; // 시험 단언
using UnityEngine; // 좌표와 씬 개체
using UnityEngine.InputSystem; // 가상 키보드
using UnityEngine.InputSystem.LowLevel; // 키 입력 상태
using UnityEngine.SceneManagement; // 실제 씬 로드
using UnityEngine.TestTools; // 플레이 모드 테스트

namespace ProjectG.Tests // 프로젝트 검증 영역
{ // 영역 시작
    public sealed class FirstFlightPlayTests // 실제 입력과 추적 카메라 시험
    { // 시험 시작
        private const string ScenePath = "Assets/Scenes/FlightPrototype.unity"; // 검증 대상 씬 경로
        private Keyboard keyboard; // 테스트 전용 가상 키보드
        private InputSettings.BackgroundBehavior backgroundBehavior; // 원래 입력 포커스 설정
        private InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior; // 에디터 입력 대상의 원래 설정
        private bool runInBackground; // 원래 백그라운드 실행 설정

        [SetUp] // 입력 장치 추가
        public void AddKeyboard() // 시스템 키보드와 분리된 입력
        { // 준비 시작
            backgroundBehavior = InputSystem.settings.backgroundBehavior; // 이전 입력 포커스 설정 보관
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 원래 에디터 입력 대상 보관
            runInBackground = Application.runInBackground; // 원래 백그라운드 실행 상태 보관
            Application.runInBackground = true; // 포커스와 별개로 테스트 프레임 진행
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 헤드리스 입력을 게임에 전달
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 그래픽 없는 배치 시험 입력 허용
            keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        } // 준비 종료

        [TearDown] // 입력 장치 정리
        public void RemoveKeyboard() // 다음 시험의 입력 상태 복원
        { // 정리 시작
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.settings.backgroundBehavior = backgroundBehavior; // 시험 전 입력 포커스 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior; // 시험 전 에디터 입력 대상 복원
            Application.runInBackground = runInBackground; // 시험 전 백그라운드 실행 복원
        } // 정리 종료

        [UnityTest] // 키 입력과 실제 씬 이동 검증
        public IEnumerator FlightSceneRespondsInAllSixDirections() // 전후·좌우·상하 조작
        { // 시나리오 시작
            yield return LoadFlight(); // 저장된 시험 씬 로드
            Transform pilot = FindPilot(); // 씬의 아리아 루트
            yield return CheckMove(pilot, Key.W, Vector3.forward); // 전진 검증
            yield return CheckMove(pilot, Key.S, Vector3.back); // 후진 검증
            yield return CheckMove(pilot, Key.A, Vector3.left); // 좌측 검증
            yield return CheckMove(pilot, Key.D, Vector3.right); // 우측 검증
            yield return CheckMove(pilot, Key.Space, Vector3.up); // 상승 검증
            yield return CheckMove(pilot, Key.LeftCtrl, Vector3.down); // 하강 검증
        } // 시나리오 종료

        [UnityTest] // 원근 카메라의 실제 추적 검증
        public IEnumerator CameraFollowsAndKeepsPilotInView() // 이동 후 카메라 구도 유지
        { // 시나리오 시작
            yield return LoadFlight(); // 저장된 시험 씬 로드
            Transform pilot = FindPilot(); // 씬의 아리아 루트
            Camera camera = Camera.main; // 실제 전투 카메라
            Assert.That(camera, Is.Not.Null); // 메인 카메라 태그 확인
            Assert.That(camera.orthographic, Is.False); // 원근 투영 확인
            Vector3 pilotStart = pilot.position; // 초기 플레이어 위치
            Vector3 cameraStart = camera.transform.position; // 초기 카메라 위치
            Vector3 initialScreen = camera.WorldToViewportPoint(pilot.position); // 화면의 초기 플레이어 위치
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D, Key.Space)); // 3축 동시 입력
            InputSystem.Update(); // 입력 이벤트 즉시 반영
            for (int frame = 0; frame < 15; frame++) // 이동과 카메라 갱신 프레임
            { // 프레임 대기 시작
                yield return null; // 다음 플레이 프레임
            } // 프레임 대기 종료
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 입력 해제
            yield return null; // 입력 해제 처리
            Assert.That((pilot.position - pilotStart).sqrMagnitude, Is.GreaterThan(0f)); // 실제 이동 확인
            Assert.That((camera.transform.position - cameraStart - (pilot.position - pilotStart)).sqrMagnitude, Is.LessThan(0.001f)); // 같은 거리만큼 카메라 이동
            Assert.That(Vector3.Dot(pilot.position - camera.transform.position, pilot.forward), Is.GreaterThan(0f)); // 캐릭터 뒤 카메라
            Vector3 screen = camera.WorldToViewportPoint(pilot.position); // 화면의 이동 후 플레이어 위치
            Assert.That(screen.z, Is.GreaterThan(0f)); // 카메라 앞 캐릭터
            Assert.That(screen.x, Is.InRange(0.1f, 0.9f)); // 화면 가로 범위
            Assert.That(screen.y, Is.InRange(0.1f, 0.9f)); // 화면 세로 범위
            Assert.That(screen.x, Is.EqualTo(initialScreen.x).Within(0.02f)); // 가로 구도 유지
            Assert.That(screen.y, Is.EqualTo(initialScreen.y).Within(0.02f)); // 세로 구도 유지
        } // 시나리오 종료

        private static IEnumerator LoadFlight() // 빌드에 등록된 실제 씬 확인
        { // 로드 시작
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(ScenePath); // 씬의 배포 순서
            Assert.That(buildIndex, Is.EqualTo(0), "첫 비행 씬의 Windows 시작 씬 등록"); // 새 프로젝트의 시작 씬 확인
            yield return SceneManager.LoadSceneAsync(buildIndex); // 등록된 씬을 정상 경로로 열기
            yield return null; // 카메라 초기화와 프레임 대기
        } // 로드 종료

        private static Transform FindPilot() // 실제 하이러키 검증
        { // 검색 시작
            GameObject pilot = GameObject.Find("AriaPilot"); // 저장된 아리아 오브젝트
            Assert.That(pilot, Is.Not.Null); // 씬 편집 가능 대상 확인
            Assert.That(pilot.transform.Find("TrainingBroom"), Is.Not.Null); // 빗자루 하이러키 확인
            return pilot.transform; // 비행 좌표 제공
        } // 검색 종료

        private IEnumerator CheckMove(Transform pilot, Key key, Vector3 direction) // 입력 키별 이동 검증
        { // 이동 시험 시작
            Vector3 start = pilot.position; // 이동 전 초기 위치
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 시험 키 입력
            InputSystem.Update(); // 입력 이벤트 즉시 반영
            for (int frame = 0; frame < 6; frame++) // 입력 처리 프레임
            { // 대기 시작
                yield return null; // 플레이 프레임 진행
            } // 대기 종료
            Assert.That(keyboard[key].isPressed, Is.True, "가상 키 입력 수신: " + key); // 키 이벤트 처리 확인
            Vector3 movement = pilot.position - start; // 실제 이동 벡터
            Assert.That(Vector3.Dot(movement, direction), Is.GreaterThan(0f), key.ToString()); // 요청 축으로 이동 확인
            Assert.That(Vector3.Cross(movement, direction).sqrMagnitude, Is.LessThan(0.001f)); // 다른 축 흔들림 검증
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 현재 키 해제
            yield return null; // 다음 입력 등록 전 해제 처리
        } // 이동 시험 종료
    } // 시험 종료
} // 영역 종료
