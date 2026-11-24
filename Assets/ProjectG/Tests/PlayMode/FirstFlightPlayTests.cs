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
        private Mouse mouse; // 테스트 전용 가상 마우스
        private float previousCaptureStep; // 원래 프레임 시험 시간
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
            mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
            previousCaptureStep = Time.captureDeltaTime; // 원래 시간 설정 저장
            Time.captureDeltaTime = 1f / 60f; // 입력과 이동의 프레임 시간 고정
        } // 준비 종료

        [TearDown] // 입력 장치 정리
        public void RemoveKeyboard() // 다음 시험의 입력 상태 복원
        { // 정리 시작
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
            Time.captureDeltaTime = previousCaptureStep; // 시간 설정 복원
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
            yield return WaitFrames(30); // 제동과 마지막 LateUpdate까지 대기
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

        [UnityTest] // 집중 비행과 제동 검증
        public IEnumerator FocusHalvesSpeedAndReleasedFlightBrakes() // 최고 속도와 감속 확인
        { // 시나리오 시작
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 이동하는 대상
            SetKeys(Key.W); // 일반 전진
            yield return WaitFrames(45); // 최고 속도 도달
            Vector3 start = pilot.position; // 일반 속도 시작점
            yield return WaitFrames(6); // 같은 시간 동안 이동
            float regular = (pilot.position - start).magnitude; // 일반 이동량
            SetKeys(Key.W, Key.LeftShift); // 집중 전진
            yield return WaitFrames(45); // 집중 속도 도달
            start = pilot.position; // 집중 속도 시작점
            yield return WaitFrames(6); // 같은 시간 동안 이동
            float focused = (pilot.position - start).magnitude; // 집중 이동량
            Assert.That(focused / regular, Is.EqualTo(0.5f).Within(0.04f)); // 집중 속도 절반
            Vector3 releaseStart = pilot.position; // 제동 거리 측정 시작점
            SetKeys(); // 전진 입력 해제
            yield return WaitFrames(1); // 제동 프레임 시작
            start = pilot.position; // 제동 구간 시작점
            yield return WaitFrames(5); // 짧은 감속 구간
            Assert.That((pilot.position - start).magnitude, Is.GreaterThan(0.03f)); // 제동 중 잔류 이동
            yield return WaitFrames(35); // 속도 정지 대기
            start = pilot.position; // 정지 이후 기준점
            yield return WaitFrames(6); // 상태 유지 확인
            Assert.That((pilot.position - start).magnitude, Is.LessThan(0.002f)); // 완전 정지
            float brakingDistance = (pilot.position - releaseStart).magnitude; // 완전히 멈출 때까지 이동 거리
            SetKeys(Key.W, Key.D); // 대각선 전진
            yield return WaitFrames(45); // 대각선 최고 속도 도달
            start = pilot.position; // 측정 시작 좌표
            yield return WaitFrames(6); // 일반 전진과 같은 측정 시간
            float diagonalRatio = (pilot.position - start).magnitude / regular; // 일반 전진 대비 대각선 속도
            Assert.That(diagonalRatio, Is.EqualTo(1f).Within(0.04f)); // 대각선 속도 증가 방지
            Debug.Log($"FLIGHT_DAY02_METRICS speed={regular * 10f:F2} focus={focused * 10f:F2} diagonalRatio={diagonalRatio:F2} braking={brakingDistance:F3}"); // 재현한 비행 시험 수치
        } // 시나리오 종료

        [UnityTest] // 방향 전환과 독립 자유 시점
        public IEnumerator MouseTurnsAndRightButtonLooksAroundWithoutTurningPilot() // 주시 방향과 비행 방향 분리
        { // 시나리오 시작
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 기본 비행체
            Camera camera = Camera.main; // 실제 추적 카메라
            Vector3 forward = pilot.forward; // 비행 방향 기준
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(90f, 35f) }); // 일반 마우스 시점 입력
            yield return WaitFrames(3); // 게임 입력 프레임
            Assert.That(Vector3.Angle(forward, pilot.forward), Is.GreaterThan(4f)); // 비행체 실제 선회
            Assert.That(camera.transform.forward.y, Is.GreaterThan(-0.14f)); // 위쪽을 보는 마우스 입력 반영
            forward = pilot.forward; // 선회한 방향 유지 기준
            Vector3 previousView = camera.transform.forward; // 자유 시점 전 화면 방향
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2, delta = new Vector2(220f, 0f) }); // 우클릭 둘러보기
            yield return WaitFrames(3); // 자유 시점 적용 프레임
            Assert.That(Vector3.Angle(forward, pilot.forward), Is.LessThan(0.1f)); // 비행 방향 유지
            Assert.That(Vector3.Angle(previousView, camera.transform.forward), Is.GreaterThan(18f)); // 카메라만 회전
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 우클릭 해제
            yield return WaitFrames(50); // 카메라 복귀 대기
            Assert.That(Vector3.Angle(previousView, camera.transform.forward), Is.LessThan(0.3f)); // 기존 방향 복귀
        } // 시나리오 종료

        [UnityTest] // 비행 경계와 카메라 가림 검증
        public IEnumerator BoundaryClampsAltitudeAndWallRetractsCamera() // 경계 정지와 카메라 복귀
        { // 시나리오 시작
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 씬의 비행체
            Transform camera = Camera.main.transform; // 씬의 카메라
            Vector3 start = pilot.position; // 최초 위치 보관
            pilot.position = new Vector3(0f, 36f, start.z); // 상승 경계 직전 배치
            SetKeys(Key.Space); // 상승 유지
            yield return WaitFrames(45); // 경계 도달 프레임
            Assert.That(pilot.position.y, Is.EqualTo(37f).Within(0.01f)); // 고도 상한 고정
            SetKeys(); // 상승 해제
            pilot.position = start; // 가림 시험 위치 복원
            yield return WaitFrames(8); // 기본 카메라 추적
            int environment = LayerMask.NameToLayer("Environment"); // 씬의 실제 지형 레이어
            MeshCollider terrain = GameObject.Find("WesternTerrace").GetComponent<MeshCollider>(); // 저장된 섬의 충돌면
            Assert.That(terrain, Is.Not.Null); // 하늘섬 충돌 저장 검증
            Assert.That(terrain.gameObject.layer, Is.EqualTo(environment)); // 카메라 가림 레이어 연결
            float normal = Vector3.Distance(pilot.position, camera.position); // 정상 추적 거리
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // 가림 시험용 임시 벽
            wall.layer = environment; // 지형과 동일한 충돌 레이어
            wall.transform.position = start + new Vector3(0f, 1.2f, -3f); // 카메라와 아리아 사이
            wall.transform.localScale = new Vector3(6f, 4f, 0.4f); // 실제 카메라 광선 가림
            Physics.SyncTransforms(); // 벽의 위치 반영
            yield return WaitFrames(4); // 가림 대응 프레임
            Assert.That(Vector3.Distance(pilot.position, camera.position), Is.LessThan(normal - 2f)); // 벽 앞쪽으로 카메라 당김
            Vector3 screen = Camera.main.WorldToViewportPoint(pilot.position + Vector3.up * 0.5f); // 아리아 상체의 화면 위치
            Assert.That(screen.y, Is.InRange(0.03f, 0.97f)); // 가림 중 아리아 노출
            Object.Destroy(wall); // 임시 가림 해제
            yield return WaitFrames(65); // 카메라 원거리 복귀 대기
            Assert.That(Vector3.Distance(pilot.position, camera.position), Is.EqualTo(normal).Within(0.1f)); // 이전 카메라 거리 복원
        } // 시나리오 종료

        [UnityTest] // 시험 씬의 일반탄 도달
        public IEnumerator StandardShotHitsCenterTargetFromOffsetMuzzle() // 화면 중앙 표적과 총구 시차 검증
        { // 사격 시험 시작
            yield return LoadFlight(); // 실제 시작 씬
            Camera camera = Camera.main; // 실제 조준 카메라
            GameObject target = GameObject.Find("NearPracticeTarget"); // 낮고 가까운 시험 표적
            Assert.That(target, Is.Not.Null); // 씬의 표적 배치
            Vector3 viewport = camera.WorldToViewportPoint(target.transform.position); // 조준점과 표적의 화면 좌표
            Assert.That(viewport.x, Is.EqualTo(0.5f).Within(0.02f)); // 화면 중앙 표적의 가로 거리
            Assert.That(viewport.y, Is.EqualTo(0.5f).Within(0.02f)); // 화면 중앙 표적의 세로 거리
            yield return FireOne(); // 가상 좌클릭으로 기본탄 발사
            yield return WaitFrames(86); // 원거리 마법탄 도착
            Assert.That(ReadTargetMetric(target, "HitCount"), Is.EqualTo(1)); // 기본탄 한 발 명중
            Assert.That(ReadTargetMetric(target, "LastDamage"), Is.EqualTo(8)); // 일반탄 시험 피해
        } // 사격 시험 종료

        [UnityTest] // 높이와 거리가 다른 표적의 집중탄
        public IEnumerator FocusedShotHitsElevatedTargetAfterMouseTurn() // 마우스 선회 후 원거리 조준
        { // 시나리오 시작
            yield return LoadFlight(); // 실제 시작 씬
            GameObject high = GameObject.Find("HighPracticeTarget"); // 측면 고고도 표적
            GameObject near = GameObject.Find("NearPracticeTarget"); // 첫 조준 위치의 표적
            Assert.That(high, Is.Not.Null); // 선택 가능한 고고도 표적
            Assert.That(high.transform.position.y, Is.GreaterThan(near.transform.position.y + 2f)); // 고도 차이 확인
            Assert.That(Vector3.Distance(Camera.main.transform.position, high.transform.position), Is.GreaterThan(Vector3.Distance(Camera.main.transform.position, near.transform.position))); // 거리 차이 확인
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(100f, 77.7778f) }); // 표적의 좌우·상하 시점을 가상 입력
            yield return WaitFrames(20); // 카메라 추적과 위치 안정
            SetKeys(Key.LeftShift); // 집중 마법탄 선택
            yield return FireOne(); // 가상 좌클릭 발사
            SetKeys(); // 집중 입력 해제
            yield return WaitFrames(90); // 고고도 표적까지 진행
            Assert.That(ReadTargetMetric(high, "HitCount"), Is.EqualTo(1)); // 고고도 표적 명중
            Assert.That(ReadTargetMetric(high, "LastDamage"), Is.EqualTo(20)); // 집중탄 시험 피해
            Assert.That(ReadTargetMetric(near, "HitCount"), Is.Zero); // 조준 방향 전환 확인
        } // 시나리오 종료

        private IEnumerator FireOne() // 실제 입력을 한 번 전달
        { // 클릭 시작
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 }); // 좌클릭
            yield return WaitFrames(3); // 한 발이 나갈 프레임
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 좌클릭 해제
            yield return null; // 마지막 입력 처리
        } // 클릭 종료

        private static int ReadTargetMetric(GameObject target, string property) // 실제 표적의 피해 확인
        { // 상태 확인 시작
            Component tracker = target.GetComponent("FlightPracticeTarget"); // 시험 표적 스크립트
            Assert.That(tracker, Is.Not.Null); // 피해 상태 컴포넌트 확인
            return (int)tracker.GetType().GetProperty(property).GetValue(tracker); // 표적의 실제 시험값
        } // 상태 확인 종료
        private void SetKeys(params Key[] keys) // 가상 키보드 입력 반영
        { // 입력 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 새로운 키 상태 적용
            InputSystem.Update(); // 검증 프레임에 즉시 전달
        } // 입력 종료

        private static IEnumerator WaitFrames(int count) // 고정 시험 간격 대기
        { // 대기 시작
            for (int frame = 0; frame < count; frame++) // 지정된 프레임 반복
            { // 반복 시작
                yield return null; // 실제 게임 업데이트 대기
            } // 반복 종료
        } // 대기 종료
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
            yield return WaitFrames(24); // 다음 방향 검증 전 관성 정지
        } // 이동 시험 종료
    } // 시험 종료
} // 영역 종료
