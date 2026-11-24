using System.Collections; // 프레임 대기
using NUnit.Framework; // 시험 단언
using UnityEngine; // 좌표와 씬 개체
using UnityEngine.InputSystem; // 가상 키보드
using UnityEngine.InputSystem.LowLevel; // 키 입력 상태
using UnityEngine.SceneManagement; // 실제 씬 로드
using UnityEngine.TestTools; // 플레이 모드 테스트

namespace ProjectG.Tests // 프로젝트 검증 영역
{
    public sealed class FirstFlightPlayTests // 실제 입력과 추적 카메라 시험
    {
        private const string ScenePath = "Assets/Scenes/FlightPrototype.unity"; // 검증 대상 씬 경로
        private Keyboard keyboard; // 테스트 전용 가상 키보드
        private Mouse mouse; // 테스트 전용 가상 마우스
        private float previousCaptureStep; // 원래 프레임 시험 시간
        private InputSettings.BackgroundBehavior backgroundBehavior; // 원래 입력 포커스 설정
        private InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior; // 에디터 입력 대상의 원래 설정
        private bool runInBackground; // 원래 백그라운드 실행 설정

        [SetUp] // 입력 장치 추가
        public void AddKeyboard() // 시스템 키보드와 분리된 입력
        {
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
        }

        [TearDown] // 입력 장치 정리
        public void RemoveKeyboard() // 다음 시험의 입력 상태 복원
        {
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
            Time.captureDeltaTime = previousCaptureStep; // 시간 설정 복원
            InputSystem.settings.backgroundBehavior = backgroundBehavior; // 시험 전 입력 포커스 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior; // 시험 전 에디터 입력 대상 복원
            Application.runInBackground = runInBackground; // 시험 전 백그라운드 실행 복원
        }

        [UnityTest] // 키 입력과 실제 씬 이동 검증
        public IEnumerator FlightSceneRespondsInAllSixDirections() // 전후·좌우·상하 조작
        {
            yield return LoadFlight(); // 저장된 시험 씬 로드
            Transform pilot = FindPilot(); // 씬의 아리아 루트
            yield return CheckMove(pilot, Key.W, Vector3.forward); // 전진 검증
            yield return CheckMove(pilot, Key.S, Vector3.back); // 후진 검증
            yield return CheckMove(pilot, Key.A, Vector3.left); // 좌측 검증
            yield return CheckMove(pilot, Key.D, Vector3.right); // 우측 검증
            yield return CheckMove(pilot, Key.Space, Vector3.up); // 상승 검증
            yield return CheckMove(pilot, Key.LeftCtrl, Vector3.down); // 하강 검증
        }

        [UnityTest] // 원근 카메라의 실제 추적 검증
        public IEnumerator CameraFollowsAndKeepsPilotInView() // 이동 후 카메라 구도 유지
        {
            yield return LoadFlight(); // 저장된 시험 씬 로드
            Transform pilot = FindPilot(); // 씬의 아리아 루트
            Camera camera = Camera.main; // 실제 전투 카메라
            Assert.That(camera, Is.Not.Null); // 메인 카메라 태그 확인
            Assert.That(camera.orthographic, Is.False); // 원근 투영 확인
            Vector3 pilotStart = pilot.position; // 초기 플레이어 위치
            Vector3 cameraStart = camera.transform.position; // 초기 카메라 위치
            Vector3 initialScreen = camera.WorldToViewportPoint(pilot.position); // 화면의 초기 플레이어 위치
            Vector3 chest = camera.WorldToViewportPoint(pilot.position + Vector3.up * 0.55f); // 몸통의 실제 화면 위치
            Assert.That(chest.x, Is.LessThan(0.45f)); // 조준점 기준 왼쪽으로 이동
            Assert.That(chest.y, Is.LessThan(0.45f)); // 조준점보다 아래쪽 유지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D, Key.Space)); // 3축 동시 입력
            InputSystem.Update(); // 입력 이벤트 즉시 반영
            for (int frame = 0; frame < 15; frame++) // 이동과 카메라 갱신 프레임
            {
                yield return null; // 다음 플레이 프레임
            }
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
        }

        [UnityTest] // 집중 비행과 제동 검증
        public IEnumerator FocusHalvesSpeedAndReleasedFlightBrakes() // 최고 속도와 감속 확인
        {
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 이동하는 대상
            SetKeys(Key.W); // 일반 전진
            yield return WaitFrames(45); // 최고 속도 도달
            Vector3 start = pilot.position; // 일반 속도 시작점
            yield return WaitFrames(6); // 같은 시간 동안 이동
            float regular = (pilot.position - start).magnitude; // 일반 이동량
            Assert.That(regular * 10f, Is.EqualTo(12f).Within(0.12f)); // 일반 최고 속도 1.5배
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
        }

        [UnityTest] // 방향 전환과 독립 자유 시점
        public IEnumerator MouseTurnsAndMiddleButtonLooksAroundWithoutTurningPilot() // 주시 방향과 비행 방향 분리
        {
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
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 4, delta = new Vector2(220f, 0f) }); // 휠 버튼 둘러보기
            yield return WaitFrames(3); // 자유 시점 적용 프레임
            Assert.That(Vector3.Angle(forward, pilot.forward), Is.LessThan(0.1f)); // 비행 방향 유지
            Assert.That(Vector3.Angle(previousView, camera.transform.forward), Is.GreaterThan(18f)); // 카메라만 회전
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 휠 버튼 해제
            yield return WaitFrames(50); // 카메라 복귀 대기
            Assert.That(Vector3.Angle(previousView, camera.transform.forward), Is.LessThan(0.3f)); // 기존 방향 복귀
        }

        [UnityTest] // 비행 경계와 카메라 가림 검증
        public IEnumerator BoundaryClampsAltitudeAndWallRetractsCamera() // 경계 정지와 카메라 복귀
        {
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
        }

        [UnityTest] // 시험 씬의 일반탄 도달
        public IEnumerator StandardShotHitsCenterTargetFromOffsetMuzzle() // 화면 중앙 표적과 총구 시차 검증
        {
            yield return LoadFlight(); // 실제 시작 씬
            Camera camera = Camera.main; // 실제 조준 카메라
            GameObject target = GameObject.Find("NearPracticeTarget"); // 낮고 가까운 시험 표적
            Assert.That(target, Is.Not.Null); // 씬의 표적 배치
            Vector3 viewport = camera.WorldToViewportPoint(target.transform.position); // 조준점과 표적의 화면 좌표
            Assert.That(viewport.x, Is.EqualTo(0.5f).Within(0.02f)); // 화면 중앙 표적의 가로 거리
            Assert.That(viewport.y, Is.EqualTo(0.5f).Within(0.02f)); // 화면 중앙 표적의 세로 거리
            yield return WaitFrames(1); // 좌클릭 없이 자동 발사 시작
            yield return WaitFrames(86); // 원거리 마법탄 도착
            Assert.That(ReadTargetMetric(target, "HitCount"), Is.GreaterThan(0)); // 자동 기본탄 명중
            Assert.That(ReadTargetMetric(target, "LastDamage"), Is.EqualTo(8)); // 일반탄 시험 피해
        }

        [UnityTest] // 높이와 거리가 다른 표적의 집중탄
        public IEnumerator FocusedShotHitsElevatedTargetAfterMouseTurn() // 마우스 선회 후 원거리 조준
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject high = GameObject.Find("HighPracticeTarget"); // 측면 고고도 표적
            GameObject near = GameObject.Find("NearPracticeTarget"); // 첫 조준 위치의 표적
            Assert.That(high, Is.Not.Null); // 선택 가능한 고고도 표적
            Assert.That(high.transform.position.y, Is.GreaterThan(near.transform.position.y + 2f)); // 고도 차이 확인
            Assert.That(Vector3.Distance(Camera.main.transform.position, high.transform.position), Is.GreaterThan(Vector3.Distance(Camera.main.transform.position, near.transform.position))); // 거리 차이 확인
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(100f, 77.7778f) }); // 표적의 좌우·상하 시점을 가상 입력
            yield return WaitFrames(20); // 카메라 추적과 위치 안정
            SetKeys(Key.LeftShift); // 집중 마법탄 선택
            yield return WaitFrames(90); // 고고도 표적까지 진행
            Assert.That(ReadTargetMetric(high, "HitCount"), Is.GreaterThan(0)); // 고고도 표적 명중
            Assert.That(ReadTargetMetric(high, "LastDamage"), Is.InRange(20, 30)); // 누적을 반영한 집중탄 시험 피해
            SetKeys(); // 집중 입력 해제
        }

        private static int ReadTargetMetric(GameObject target, string property) // 실제 표적의 피해 확인
        {
            Component tracker = target.GetComponent("FlightPracticeTarget"); // 시험 표적 스크립트
            Assert.That(tracker, Is.Not.Null); // 피해 상태 컴포넌트 확인
            return (int)tracker.GetType().GetProperty(property).GetValue(tracker); // 표적의 실제 시험값
        }
        [UnityTest] // 프레임을 건너는 탄환과 피격 후 무적
        public IEnumerator FastThreatHitsOnceAndInvulnerabilityExpires() // 고속 적탄 관통 누락 확인
        {
            yield return LoadFlight(); // 실제 전투 시험 씬
            Transform pilot = FindPilot(); // 판정 대상의 실제 위치
            Component vitals = FlightVitals(pilot); // 체력과 판정 상태
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 한 프레임에 기체를 통과하는 적탄
            yield return WaitFrames(3); // 투사체 판정 대기
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 고속 적탄에 실제 피해
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 무적 구간의 두 번째 적탄
            yield return WaitFrames(3); // 두 번째 접촉 처리
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 기존 체력 유지
            Assert.That(Metric(vitals, "Grazes"), Is.Zero); // 스친 탄과 직접 접촉 구분
            yield return WaitFrames(65); // 1초 피격 무적 종료
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 무적 종료 뒤 다시 접촉
            yield return WaitFrames(3); // 마지막 피격 처리
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(60)); // 무적 해제 후 피해
        }

        [UnityTest] // 스친 탄과 피격하는 탄의 분리
        public IEnumerator NearMissAwardsOneGrazeButEventualHitDoesNot() // 그레이즈 중복 및 유효 범위 검증
        {
            yield return LoadFlight(); // 실제 전투 시험 씬
            Transform pilot = FindPilot(); // 현재 아리아 곡면 기준
            Component vitals = FlightVitals(pilot); // 그레이즈 횟수
            EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 피격 반경보다 바깥의 근접탄
            yield return WaitFrames(6); // 완전히 스칠 때까지 진행
            Assert.That(Metric(vitals, "Grazes"), Is.EqualTo(1)); // 같은 탄의 그레이즈 1회
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(100)); // 근접 회피 중 체력 유지
            EmitThreat(pilot.position + Vector3.right * 1.4f + Vector3.forward * 12f, Vector3.back, 1500f); // 근접 반경 밖의 먼 탄
            EmitThreat(pilot.position + Vector3.forward * 1.1f, Vector3.back, 27f); // 그레이즈 구간에 들어간 뒤 몸통을 맞히는 느린 탄
            yield return WaitFrames(8); // 스침 후보와 실제 명중 처리
            Assert.That(Metric(vitals, "Grazes"), Is.EqualTo(1)); // 나중에 맞은 탄의 후보 취소
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 느린 직접 피격 확인
        }

        [UnityTest] // 출격 실패와 키보드 재시작
        public IEnumerator DefeatStopsFlightAndRestartRestoresVitals() // 체력 소진과 재시작 검증
        {
            yield return LoadFlight(); // 실제 전투 시험 씬
            Transform pilot = FindPilot(); // 이동 여부를 확인할 비행체
            Component vitals = FlightVitals(pilot); // 초기 체력 상태
            for (int hit = 0; hit < 5; hit++) // 시험 체력 100 소진
            {
                EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 하나의 고속 적탄 발사
                yield return WaitFrames(65); // 각각에 대한 무적 종료 대기
            }
            Assert.That(Metric(vitals, "Health"), Is.Zero); // 시험 출격 실패
            Vector3 defeated = pilot.position; // 실패 상태의 기체 좌표
            SetKeys(Key.W); // 실패 뒤 이동 시도
            yield return WaitFrames(9); // 조작 반영 대기
            Assert.That(pilot.position, Is.EqualTo(defeated)); // 실패 중 움직임 차단
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 다음 게임 입력 프레임의 재시작 키 누름
            yield return WaitFrames(5); // 출격 상태 및 HUD 재설정
            SetKeys(); // 재시작 입력 해제
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(100)); // 체력 완전 복구
            Assert.That(Metric(vitals, "Grazes"), Is.Zero); // 이전 출격 그레이즈 제거
        }

        private static Component FlightVitals(Transform pilot) // 씬에 등록된 체력 데이터 찾기
        {
            Component vitals = pilot.GetComponent("FlightPilotVitals"); // 비행체가 소유한 체력 컴포넌트
            Assert.That(vitals, Is.Not.Null); // 실제 씬 연결 확인
            return vitals; // 테스트의 체력 상태
        }

        private static int Metric(Component component, string property) // 체력과 그레이즈의 실제 수치
        {
            return (int)component.GetType().GetProperty(property).GetValue(component); // 테스트가 읽는 실행 수치
        }

        private static void EmitThreat(Vector3 position, Vector3 direction, float speed) // 씬의 연습용 적탄 프리팹 호출
        {
            GameObject scene = GameObject.Find("ThreatPractice"); // 편집 가능한 적탄 연습 루트
            Assert.That(scene, Is.Not.Null); // 실제 씬에서 연습 코스 존재 여부
            Component course = scene.GetComponent("FlightThreatPractice"); // 적탄 시나리오 컴포넌트
            Assert.That(course, Is.Not.Null); // 씬의 적탄 설정 참조
            course.GetType().GetMethod("EmitTrial").Invoke(course, new object[] { position, direction, speed }); // 실제 적탄 발사 로직 사용
        }
        [UnityTest] // 조준·부채·고리·나선 배치 검증
        public IEnumerator ThreatPatternsProduceDistinctThreeDimensionalTrajectories() // 입체 탄막 방향과 발사 수 확인
        {
            yield return LoadFlight(); // 실제 시작 씬
            Component pool = GetThreatPool(); // 씬의 적탄 재사용 풀
            Component course = GameObject.Find("ThreatPractice").GetComponent("FlightThreatPractice"); // 씬 연습 코스
            var emit = course.GetType().GetMethod("EmitPattern"); // 지정한 탄막 패턴의 발사 경로
            Assert.That(emit, Is.Not.Null); // 실제 조준·부채·고리·나선 기능 연결
            var kind = emit.GetParameters()[0].ParameterType; // 어셈블리를 넘는 패턴 값
            int[] expected = { 1, 7, 12, 4 }; // 패턴별 기본 발사 수
            Vector3 center = new Vector3(0f, 26f, 10f); // 지형 위의 안전한 발사 위치
            for (int index = 0; index < expected.Length; index++) // 네 패턴 차례로 시험
            {
                object selection = System.Enum.ToObject(kind, index); // 선택한 탄막 데이터
                int fired = (int)emit.Invoke(course, new[] { selection, (object)center, Vector3.back }); // 씬의 실제 적탄 발사
                Assert.That(fired, Is.EqualTo(expected[index])); // 각 패턴의 발사 수 확인
                if (index == 2) // 고리의 3차원 횡방향 검사
                {
                    float top = 0f; // 고리 상단의 수직 성분
                    float bottom = 0f; // 고리 하단의 수직 성분
                    foreach (Transform bolt in pool.transform) // 재사용 더미의 모든 적탄
                    {
                        if (!bolt.gameObject.activeSelf) // 회수된 탄 제외
                        {
                            continue; // 현재 탄막만 방향 검증
                        }
                        top = Mathf.Max(top, bolt.forward.y); // 가장 높은 고리 각도
                        bottom = Mathf.Min(bottom, bolt.forward.y); // 가장 낮은 고리 각도
                    }
                    Assert.That(top, Is.GreaterThan(0.15f)); // 위로 벌어진 고리
                    Assert.That(bottom, Is.LessThan(-0.15f)); // 아래로 벌어진 고리
                }
                pool.GetType().GetMethod("ReturnAll").Invoke(pool, null); // 다음 패턴의 밝은 외형과 탄환 상태 초기화
            }
        }

        [UnityTest] // 적탄 풀 상한과 사용 수명 검증
        public IEnumerator ThreatPoolCapsConcurrencyAndReusesExpiredBolts() // 최대 탄수·회수·다음 발사 확인
        {
            yield return LoadFlight(); // 실제 시작 씬
            Component pool = GetThreatPool(); // 씬의 적탄 풀
            int capacity = Metric(pool, "Capacity"); // 씬에서 지정한 동시 활성 상한
            Assert.That(capacity, Is.EqualTo(128)); // 시험용 동시 탄수 상한 검증
            Vector3 origin = new Vector3(25f, 28f, 0f); // 플레이어와 충돌하지 않는 시험 좌표
            for (int i = 0; i < capacity + 15; i++) // 상한 초과 발사 시도
            {
                EmitThreat(origin, Vector3.forward, 1000f); // 사거리 끝에서 회수할 빠른 탄
            }
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(capacity)); // 동시 활성 상한 유지
            Assert.That(Metric(pool, "CreatedCount"), Is.EqualTo(capacity)); // 불필요한 게임 오브젝트 생성 방지
            yield return WaitFrames(14); // 탄환이 사거리에 도달하도록 대기
            Assert.That(Metric(pool, "ActiveCount"), Is.Zero); // 사거리 만료로 전체 회수
            EmitThreat(origin, Vector3.forward, 1f); // 이전 객체를 느린 새 탄으로 다시 사용
            yield return WaitFrames(3); // 남은 수명 초기화 확인
            Assert.That(Metric(pool, "CreatedCount"), Is.EqualTo(capacity)); // 새 오브젝트 생성 없이 재사용
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(1)); // 재사용한 새 탄이 활성 상태
            yield return WaitFrames(490); // 멀리 가지 않은 적탄의 최대 수명 대기
            Assert.That(Metric(pool, "ActiveCount"), Is.Zero); // 사거리와 독립된 시간 만료 회수
            Debug.Log($"FLIGHT_DAY05_POOL capacity={capacity} created={Metric(pool, "CreatedCount")} active={Metric(pool, "ActiveCount")}"); // 실제 상한과 회수 수치
        }

        private static Component GetThreatPool() // 씬에 배치된 적탄 풀 찾기
        {
            GameObject owner = GameObject.Find("ThreatBoltPool"); // 실제 Hierarchy의 풀 루트
            Assert.That(owner, Is.Not.Null); // 씬에서 풀 편집 가능 여부
            Component pool = owner.GetComponent("FlightThreatPool"); // 풀 상태를 읽을 컴포넌트
            Assert.That(pool, Is.Not.Null); // 풀 로직 연결
            return pool; // 테스트 대상 상태
        }
        [UnityTest] // 마물의 비행과 편집 가능한 씬 상태 확인
        public IEnumerator EnemyHasAnimatedWingsAndInitiallyHiddenHealthBar() // 저장된 시험 마물의 애니메이션
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 씬에 배치한 시험 마물
            Assert.That(enemy, Is.Not.Null); // 실제 씬에 마물 저장 확인
            Transform wing = enemy.transform.Find("Model/LeftWingPivot"); // 편집 가능한 날개 회전축
            Assert.That(wing, Is.Not.Null); // 날개 하이러키 배치
            Quaternion start = wing.localRotation; // 시작 날개 각도
            float animation = 0f; // 첫 각도에서 관찰한 최대 움직임
            for (int frame = 0; frame < 35; frame++) // 날개의 전체 비행 구간 확인
            {
                yield return null; // 날갯짓 실제 프레임
                animation = Mathf.Max(animation, Quaternion.Angle(start, wing.localRotation)); // 가장 크게 움직인 날개 각도
            }
            Assert.That(animation, Is.GreaterThan(6f)); // 시작 위상에 관계없이 애니메이션 확인
            Canvas bar = enemy.transform.Find("HealthBar").GetComponent<Canvas>(); // 제작 이미지가 놓인 월드 체력바
            Assert.That(bar.enabled, Is.False); // 첫 피격 전 체력 숨김
        }

        [UnityTest] // 실제 아군탄에 의한 체력 표시와 재숨김 검증
        public IEnumerator EnemyHealthBarAppearsOnHitAndHidesAfterQuietTime() // 명중 피해와 표시 수명
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 표적 역할의 마물 인스턴스
            Assert.That(enemy, Is.Not.Null); // 씬 배치 확인
            enemy.transform.position = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)).GetPoint(18f); // 가까운 조준 위치의 마물
            Physics.SyncTransforms(); // 실제 발사와 마물 판정 위치 동기화
            Component life = enemy.GetComponent("FlightEnemy"); // 마물의 실제 체력 컴포넌트
            Canvas bar = enemy.transform.Find("HealthBar").GetComponent<Canvas>(); // 월드 공간 체력 프레임
            Transform fill = bar.transform.Find("Fill"); // 피격에 따라 줄어드는 이미지
            float full = ((RectTransform)fill).rect.width; // 정규화된 최대 표시 너비
            for (int frame = 0; frame < 60 && Metric(life, "Health") == 80; frame++) // 첫 자동탄 피격까지만 기다림
            {
                yield return null; // 실제 탄환 진행
            }
            Assert.That(Metric(life, "Health"), Is.EqualTo(72)); // 실제 마물에 피해 8 반영
            Assert.That(bar.enabled, Is.True); // 첫 피격 후 표시 조건
            Assert.That(((RectTransform)fill).rect.width / full, Is.EqualTo(0.9f).Within(0.02f)); // 체력에 비례한 붉은 이미지 너비
            enemy.transform.position += Vector3.left * 25f; // 이후 자동탄을 피하는 시험 위치
            Physics.SyncTransforms(); // 재숨김 상태 검증의 새 위치
            yield return WaitFrames(130); // 마지막 피격 뒤 표시 시간 종료
            Assert.That(bar.enabled, Is.False); // 추가 피격 없이 체력바 재숨김
        }
        [UnityTest] // 세 일반 마물의 경로와 복수 패턴
        public IEnumerator EnemyTrialUsesDescendingSideAndPassingRoutes() // 출현·턴별 공격·이탈 검증
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject encounter = GameObject.Find("EnemyEncounter"); // 씬의 마물 시험 구성
            Assert.That(encounter, Is.Not.Null); // 편집 가능한 전투 그룹 존재
            Transform aimed = encounter.transform.Find("AimedEnemy"); // 아래로 내려가는 적
            Transform fan = encounter.transform.Find("FanEnemy"); // 옆으로 나가는 적
            Transform dive = encounter.transform.Find("DiverEnemy"); // 전방을 돌진해 지나가는 적
            Assert.That(aimed, Is.Not.Null); // 조준형 씬 인스턴스 확인
            Assert.That(fan, Is.Not.Null); // 확산형 씬 인스턴스 확인
            Assert.That(dive, Is.Not.Null); // 돌진형 씬 인스턴스 확인
            Assert.That(aimed.gameObject.activeSelf, Is.False); // 시험 키 입력 전 등장 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 실제 시험 시작 입력
            yield return WaitFrames(13); // 세 마물 출현 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 시험 키 해제
            Assert.That(aimed.gameObject.activeSelf && fan.gameObject.activeSelf && dive.gameObject.activeSelf, Is.True); // 각 유형의 실제 출현 확인
            Vector3 a = aimed.position; // 조준형 등장 위치
            Vector3 f = fan.position; // 확산형 등장 위치
            Vector3 b = dive.position; // 돌진형 등장 위치
            yield return WaitFrames(177); // 공격 두 번과 각 이동 경로 진행
            Assert.That(Metric(aimed.GetComponent("FlightEnemyRoute"), "PatternsFired"), Is.GreaterThanOrEqualTo(2)); // 조준형 공격 전환
            Assert.That(Metric(fan.GetComponent("FlightEnemyRoute"), "PatternsFired"), Is.GreaterThanOrEqualTo(2)); // 확산형 공격 전환
            Assert.That(Metric(dive.GetComponent("FlightEnemyRoute"), "PatternsFired"), Is.GreaterThanOrEqualTo(2)); // 돌진형 공격 전환
            Assert.That(aimed.position.y, Is.LessThan(a.y - 5f)); // 조준형이 아래로 이탈 중
            Assert.That(fan.position.x, Is.GreaterThan(f.x + 4f)); // 확산형이 옆으로 이탈 중
            Assert.That(dive.position.z, Is.LessThan(b.z - 20f)); // 돌진형이 전방으로 통과 중
            yield return WaitFrames(88); // 이탈 완료
            Assert.That(aimed.gameObject.activeSelf || fan.gameObject.activeSelf || dive.gameObject.activeSelf, Is.False); // 이탈 후 원본 비활성화
        }
        [UnityTest] // 전투 게이지의 실제 상태와 생성 이미지
        public IEnumerator CombatHudShowsDamageAndKeepsAllGaugesLabeled() // HP와 자원별 표시 검증
        {
            yield return LoadFlight(); // 실제 비행 씬
            Transform hud = LocateCombatHud().transform; // 기존 안내와 별도 전투 HUD
            Transform hp = hud.Find("Health"); // 체력 게이지
            Transform mana = hud.Find("Mana"); // 마력 게이지
            Transform ultimate = hud.Find("Ultimate"); // 필살기 게이지
            Transform xp = hud.Find("Experience"); // 경험치 게이지
            Assert.That(HudText(hp.Find("Label")), Is.EqualTo("HP 100 / 100")); // 처음 체력 수치
            Assert.That(HudText(mana.Find("Label")), Is.EqualTo("MANA 100 / 100")); // 초기 마력 문구
            Assert.That(HudText(ultimate.Find("Label")), Is.EqualTo("ULT 0 / 100")); // 초기 필살기 게이지
            Assert.That(HudText(xp.Find("Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 초기 레벨과 경험치
            foreach (Transform meter in new[] { hp, mana, ultimate, xp }) // 실제 채움 이미지 모두 확인
            {
                Component image = meter.Find("Fill").GetComponent("RawImage"); // 기본 색 사각형 대신 실제 텍스처
                Assert.That(image.GetType().GetProperty("texture").GetValue(image), Is.Not.Null); // 생성 파일이 실제 씬에 연결
            }
            float full = ((RectTransform)hp.Find("Fill")).rect.width; // 최대 HP 표시 폭
            Transform pilot = FindPilot(); // 현재 시험 기체
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 고속 적탄 피격
            yield return WaitFrames(4); // 체력과 HUD 업데이트 대기
            Assert.That(HudText(hp.Find("Label")), Is.EqualTo("HP 80 / 100")); // 피격 후 새 체력 수치
            Assert.That(((RectTransform)hp.Find("Fill")).rect.width / full, Is.EqualTo(0.8f).Within(0.02f)); // 채움 길이 80%
        }

        [UnityTest] // 스킬 슬롯의 부족 표시와 흰색 카운트다운
        public IEnumerator SkillHudDimsUnavailableArtAndDisplaysCooldown() // 세 슬롯과 시험 상태 반영
        {
            yield return LoadFlight(); // 실제 비행 씬
            Component hud = LocateCombatHud(); // 전투 HUD의 표시 상태
            Transform first = hud.transform.Find("SkillSlots/Q"); // Q 첫 스킬 슬롯
            Transform last = hud.transform.Find("SkillSlots/R"); // R 필살기 슬롯
            Assert.That(SkillArtColor(first).r, Is.GreaterThan(0.8f)); // 충분한 마력의 일반 아이콘
            Assert.That(SkillArtColor(last).r, Is.LessThan(0.55f)); // 에너지가 부족한 필살기 실루엣
            ((Behaviour)FindPilot().GetComponent("FlightSkillController")).enabled = false; // 표시값만 주입하는 독립 시험
            hud.GetType().GetMethod("SetResources").Invoke(hud, new object[] { 10, 100 }); // 마력 부족과 필살기 용량 상태
            hud.GetType().GetMethod("SetCooldown").Invoke(hud, new object[] { 0, 2.4f }); // 숫자 카운트다운 표시
            yield return WaitFrames(5); // 실제 HUD 표시 적용
            Assert.That(SkillArtColor(first).r, Is.LessThan(0.55f)); // 부족한 마력의 어두운 이미지
            Assert.That(SkillArtColor(last).r, Is.GreaterThan(0.8f)); // 충전된 필살기 아이콘
            Assert.That(HudText(first.Find("Cooldown")), Is.EqualTo("3")); // 흰색 정수 카운트다운 문구
            Component countdown = first.Find("Cooldown").GetComponent("Text"); // 제작 슬롯 안의 동적 숫자
            Color number = (Color)countdown.GetType().GetProperty("color").GetValue(countdown); // 실제 글자 색
            Assert.That(number, Is.EqualTo(Color.white)); // 카운트다운 흰색 표시
            hud.GetType().GetMethod("SetCooldown").Invoke(hud, new object[] { 0, 0.05f }); // 만료 직전 상태
            yield return WaitFrames(12); // 쿨타임 종료
            Assert.That(HudText(first.Find("Cooldown")), Is.Empty); // 카운트다운 종료 후 사라짐
        }

        private static Component LocateCombatHud() // 씬의 전투 UI 로직 탐색
        {
            GameObject root = GameObject.Find("CombatHUD"); // Hierarchy에 저장한 게이지 루트
            Assert.That(root, Is.Not.Null); // 실제 UI 배치 확인
            Component hud = root.GetComponent("FlightCombatHud"); // 게이지 표시 로직
            Assert.That(hud, Is.Not.Null); // 실제 게이지 연결 확인
            return hud; // 상태 검증에 사용하는 컴포넌트
        }

        private static string HudText(Transform child) // 현재 화면의 실제 글자
        {
            Component label = child.GetComponent("Text"); // 씬에 저장된 글자 노드
            return (string)label.GetType().GetProperty("text").GetValue(label); // 현재 상태에 표시된 값
        }

        private static Color SkillArtColor(Transform child) // 스킬 이미지의 실루엣 계산 확인
        {
            Component art = child.Find("Art").GetComponent("RawImage"); // 생성 스킬 이미지
            return (Color)art.GetType().GetProperty("color").GetValue(art); // 실제 이미지 표시 색
        }
        [UnityTest] // 좌클릭 없이 자동탄 명중과 독립 카드 순환 검증
        public IEnumerator AutomaticFireAndTwoCardLanesRotateIndependently() // 마법 카드와 다음 미리보기
        {
            yield return LoadFlight(); // 실제 게임 씬
            yield return WaitFrames(55); // 클릭 없이 기본탄 도달
            GameObject near = GameObject.Find("NearPracticeTarget"); // 화면 중앙 연습 표적
            Assert.That(ReadTargetMetric(near, "HitCount"), Is.GreaterThan(0)); // 기본탄 자동 명중
            GameObject cards = GameObject.Find("MagicCards"); // 오른쪽 아래 제작 카드 그룹
            Assert.That(cards, Is.Not.Null); // 씬의 실제 카드 배치
            Component deck = cards.GetComponent("FlightMagicDeck"); // 좌우 카드 순서 컴포넌트
            Assert.That(HudText(cards.transform.Find("LeftActive/Title")), Is.EqualTo("ARC")); // 왼쪽 시작 카드
            Assert.That(HudText(cards.transform.Find("LeftNext/Title")), Is.EqualTo("LANCE")); // 왼쪽 다음 카드
            Assert.That(HudText(cards.transform.Find("RightActive/Title")), Is.EqualTo("AEGIS")); // 오른쪽 시작 카드
            Assert.That(HudText(cards.transform.Find("RightNext/Title")), Is.EqualTo("NOVA")); // 오른쪽 다음 카드
            Assert.That(HudText(cards.transform.Find("LeftActive/ManaCost")), Is.EqualTo("12")); // 왼쪽 보석의 실제 마나
            Assert.That(HudText(cards.transform.Find("RightActive/ManaCost")), Is.EqualTo("14")); // 오른쪽 보석의 실제 마나
            yield return Click(1); // 실제 왼쪽 마우스 클릭
            Assert.That(Metric(deck, "LeftIndex"), Is.EqualTo(1)); // 왼쪽 카드만 한 칸 이동
            Assert.That(Metric(deck, "RightIndex"), Is.Zero); // 오른쪽 카드 보존
            Assert.That(HudText(cards.transform.Find("LeftActive/Title")), Is.EqualTo("LANCE")); // 다음 카드가 현재로 등장
            Assert.That(HudText(cards.transform.Find("LeftActive/ManaCost")), Is.EqualTo("18")); // 교체된 비용 보석 갱신
            yield return Click(2); // 실제 오른쪽 마우스 클릭
            Assert.That(Metric(deck, "LeftIndex"), Is.EqualTo(1)); // 왼쪽 순서 보존
            Assert.That(Metric(deck, "RightIndex"), Is.EqualTo(1)); // 오른쪽 순서만 이동
            Assert.That(HudText(cards.transform.Find("RightActive/Title")), Is.EqualTo("NOVA")); // 오른쪽 다음 카드가 현재로 등장
            Assert.That(HudText(cards.transform.Find("RightNext/Title")), Is.EqualTo("SHADE")); // 어둠 카드가 다음에 등장
            yield return Click(2); // 기본 오른쪽 덱의 두 번째 사용
            Assert.That(HudText(cards.transform.Find("RightActive/Title")), Is.EqualTo("SHADE")); // 어둠 카드가 실제로 올라옴
            Assert.That(HudText(cards.transform.Find("RightActive/ManaCost")), Is.EqualTo("16")); // 어둠 비용의 실제 보석 표시
            Component element = cards.transform.Find("RightActive/Element").GetComponent("RawImage"); // 어둠 속성 레이어
            Assert.That(((Texture)element.GetType().GetProperty("texture").GetValue(element)).name, Is.EqualTo("CardElementDark")); // 첨부 아이콘 실제 참조
            Component magic = FindPilot().GetComponent("FlightSkillController"); // 카드와 공유하는 실제 마력
            magic.GetType().GetField("mana", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(magic, 0f); // 마력이 부족한 시험 상태
            yield return Click(2); // 부족한 오른쪽 어둠 카드 시도
            Assert.That(Metric(deck, "RightIndex"), Is.EqualTo(2)); // 카드 사용 실패에서 순서 보존
        }

        [UnityTest] // 정사각형 스킬과 작고 바깥쪽에 놓인 카드
        public IEnumerator SkillKeysAndSharpHudStayAboveCardPreviews() // Q·E·R 실제 발동과 UI 레이아웃
        {
            yield return LoadFlight(); // 실제 UI 씬 로드
            Transform hud = LocateCombatHud().transform; // 제작 HUD
            Transform cards = hud.Find("MagicCards"); // 오른쪽 카드 영역
            Transform skills = hud.Find("SkillSlots"); // 세 스킬 위치
            RectTransform cardBounds = cards.GetComponent<RectTransform>(); // 카드 전체 크기
            Assert.That(((RectTransform)cards.Find("LeftActive")).rect.width, Is.EqualTo(128.8f).Within(0.01f)); // 현재 카드 너비 70%
            Assert.That(((RectTransform)cards.Find("LeftActive")).rect.height, Is.EqualTo(193.2f).Within(0.01f)); // 현재 카드 높이 70%
            Assert.That(((RectTransform)cards.Find("LeftNext")).rect.width, Is.EqualTo(100.8f).Within(0.01f)); // 미리보기 너비 70%
            Assert.That(((RectTransform)cards.Find("RightNext")).rect.height, Is.EqualTo(151.2f).Within(0.01f)); // 미리보기 높이 70%
            RectTransform skillBounds = skills.GetComponent<RectTransform>(); // 스킬 전체 크기
            Assert.That(skillBounds.position.y - skillBounds.rect.height * skillBounds.lossyScale.y * 0.02f, Is.GreaterThan(cardBounds.position.y + cardBounds.rect.height * cardBounds.lossyScale.y)); // 스킬로 카드를 가리지 않음
            Vector3[] corners = new Vector3[4]; // 화면의 실제 사각형 모서리
            ((RectTransform)skills.Find("R")).GetWorldCorners(corners); // 맨 오른쪽 스킬의 경계
            float skillRight = corners[3].x; // R 슬롯의 실제 오른쪽 끝
            ((RectTransform)cards.Find("RightNext")).GetWorldCorners(corners); // 오른쪽 다음 카드 바깥 경계
            Assert.That(skillRight, Is.EqualTo(corners[3].x).Within(0.5f)); // 서로 같은 오른쪽 끝
            Assert.That(cards.Find("LeftNext").position.x, Is.LessThan(cards.Find("LeftActive").position.x)); // 왼쪽 바깥 미리보기
            Assert.That(cards.Find("RightNext").position.x, Is.GreaterThan(cards.Find("RightActive").position.x)); // 오른쪽 바깥 미리보기
            Assert.That(((RectTransform)cards.Find("LeftNext")).rect.width, Is.LessThan(((RectTransform)cards.Find("LeftActive")).rect.width)); // 작은 다음 카드 너비
            foreach (string key in new[] { "Q", "E", "R" }) // 요청된 세 입력 슬롯
            {
                RectTransform rect = (RectTransform)skills.Find(key); // 씬의 정사각형 요소
                Assert.That(rect.rect.width, Is.EqualTo(rect.rect.height).Within(0.01f)); // 스킬은 정사각형 유지
                Assert.That(HudText(rect.Find("Key")), Is.EqualTo(key)); // 실제 키 문구 일치
            }
            Canvas canvas = hud.GetComponentInParent<Canvas>(); // 제작 텍스처와 글자를 그리는 캔버스
            Assert.That(canvas.pixelPerfect, Is.True); // 글자와 이미지의 픽셀 기준 정렬
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 실제 Q 스킬 입력
            yield return WaitFrames(6); // 쿨타임·자원 갱신
            Assert.That(HudText(skills.Find("Q/Cooldown")), Is.Empty); // 집중 포화 효과 중에는 쿨타임 이전
            Component weapon = FindPilot().GetComponent("FlightWeapon"); // 아리아의 실제 발사 간격
            Assert.That((float)weapon.GetType().GetProperty("FireIntervalScale").GetValue(weapon), Is.EqualTo(0.7f)); // 30% 짧아진 발사 간격
            Assert.That(HudText(hud.Find("Mana/Label")), Is.Not.EqualTo("MANA 100 / 100")); // 실제 마력 소모
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // Q 누름 해제
            yield return WaitFrames(181); // 집중 포화 종료 이후
            Assert.That(HudText(skills.Find("Q/Cooldown")), Is.Not.Empty); // 효과 뒤 시작되는 흰색 쿨타임
        }

        private IEnumerator Click(ushort button) // 실제 게임의 마우스 입력 프레임
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = button }); // 선택한 버튼 누름
            yield return WaitFrames(3); // 입력 업데이트 대기
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 버튼 해제
            yield return WaitFrames(3); // 다음 입력 등록까지 대기
        }
        [UnityTest] // 아리아의 패시브 및 시험 출격 초기화
        public IEnumerator AriaRegularHitsStackAndDecayAfterMissing() // 기본·집중탄의 누적 화력
        {
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 속사형의 실제 비행체
            Component rapid = pilot.GetComponent("FlightRapidFire"); // 현재 누적 스크립트
            Assert.That(rapid, Is.Not.Null); // 씬에 편집 가능한 패시브 존재
            yield return WaitFrames(62); // 입력 없는 자동탄의 실제 명중
            Assert.That(Percent(rapid), Is.GreaterThan(0f)); // 자동탄의 실제 연속 적중
            var record = rapid.GetType().GetMethod("RecordHit"); // 실제 적중과 같은 갱신 경로
            for (int count = 0; count < 60; count++) // 한도 밖까지 누적
            {
                record.Invoke(rapid, null); // 연속 적중 재현
            }
            Assert.That(Percent(rapid), Is.EqualTo(50f)); // 최대 50% 상한
            var damage = (int)rapid.GetType().GetMethod("ScaleDamage").Invoke(rapid, new object[] { 8 }); // 최대 보너스의 실제 피해
            Assert.That(damage, Is.EqualTo(12)); // 기본 8에서 최대 +50%
            GameObject.Find("NearPracticeTarget").SetActive(false); // 명중을 중단할 실제 표적
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 발사 중단 상태 재현
            yield return WaitFrames(111); // 마지막 적중 이후 2초 이내
            Assert.That(Percent(rapid), Is.EqualTo(50f).Within(0.01f)); // 2초 유예
            yield return WaitFrames(80); // 2초를 넘겨 감소 시작
            Assert.That(Percent(rapid), Is.InRange(46.5f, 48.5f)); // 초당 2.5%p 감소
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 시험 출격 재시작
            yield return WaitFrames(4); // 초기화가 실제로 적용될 프레임
            Assert.That(Percent(rapid), Is.Zero); // 이전 출격의 피해 보너스 제거
        }

        private static float Percent(Component rapid) // 현재 화력 비율 확인
        {
            return (float)rapid.GetType().GetProperty("BonusPercent").GetValue(rapid); // 실제 패시브 표시값
        }
        [UnityTest] // 아리아 R 유성 탄막의 범위와 에너지
        public IEnumerator AriaMeteorClearsOnScreenThreatAndDamagesEnemy() // 실제 그레이즈 충전과 필살기
        {
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 기체 피격 중심
            for (int index = 0; index < 10; index++) // 필살기 에너지 100까지 충전
            {
                EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 실제 판정의 근접 회피 적탄
            }
            yield return WaitFrames(6); // 그레이즈의 에너지 수급
            Transform hud = LocateCombatHud().transform; // 전투 오버레이
            Assert.That(HudText(hud.Find("Ultimate/Label")), Is.EqualTo("ULT 100 / 100")); // 실제 에너지 충전 확인
            GameObject target = GameObject.Find("GlassfinEnemy"); // 시험 마물
            Camera camera = Camera.main; // 화면 안 탄막의 카메라
            EmitThreat(camera.ViewportPointToRay(new Vector3(0.64f, 0.63f)).GetPoint(20f), Vector3.forward, 1f); // 화면에 보이는 적탄
            EmitThreat(pilot.position + new Vector3(-40f, 10f, 3f), Vector3.forward, 1f); // 시야 바깥 적탄
            Component pool = GetThreatPool(); // 실제 남은 적탄
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(2)); // 두 위치의 적탄 준비
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 스킬 R 발동
            yield return WaitFrames(6); // 피해와 소거 대기
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(1)); // 보이는 적탄만 소거
            Assert.That(target.activeSelf, Is.False); // 시험 체력 80의 마물 피해
            Assert.That(HudText(hud.Find("Ultimate/Label")), Is.EqualTo("ULT 0 / 100")); // 에너지 100 소비
        }
        [UnityTest] // 시험 조작에서 두 캐릭터의 전환
        public IEnumerator RianSwitchesSkillArtAndWeakPointIndicator() // F2로 리안 선택 후 원복
        {
            yield return LoadFlight(); // 실제 씬과 저장된 아트 로드
            Transform hud = LocateCombatHud().transform; // 현재 HUD
            Transform slot = hud.Find("SkillSlots/Q"); // Q 전용 아이콘
            var art = slot.Find("Art").GetComponent<UnityEngine.UI.RawImage>(); // 현재 적용된 실제 그림
            var ariaArt = art.texture; // 원복할 아리아 원본 아이콘
            yield return SelectRian(); // 사용자가 누르는 실제 키
            Assert.That(art.texture, Is.Not.SameAs(ariaArt)); // 리안 Q 그림
            Assert.That(HudText(hud.Find("RapidBonus/Label")), Is.EqualTo("리안  약점 +50%")); // 리안 패시브 안내
            yield return SelectRian(); // 두 번째 입력으로 아리아 복구
            Assert.That(art.texture, Is.SameAs(ariaArt)); // 원래 그림과 속사 복귀
        }

        [UnityTest] // 집중 사격의 몸통과 약점 비교
        public IEnumerator RianFocusedBoltSeparatesExposedWeakPointFromBody() // 높이 차이가 20·30 피해로 반영
        {
            yield return LoadFlight(); // 실제 출격 경로
            yield return SelectRian(); // 저격 캐릭터 전환
            Transform pilot = FindPilot(); // 같은 비행체 기반
            Component weapon = pilot.GetComponent("FlightWeapon"); // 실제 자동·스킬 발사체
            ((Behaviour)weapon).enabled = false; // 측정 중 자동탄으로 인한 추가 피해 차단
            yield return WaitFrames(150); // 전환 이전에 발사된 탄의 최대 사거리 경과
            GameObject.Find("NearPracticeTarget").SetActive(false); // 앞의 기본 연습 표적 제거
            GameObject.Find("HighPracticeTarget")?.SetActive(false); // 고고도 표적 간섭 차단
            GameObject target = GameObject.Find("GlassfinEnemy"); // 프리팹이 저장된 시험 마물
            Transform marker = target.transform.Find("WeakPoint"); // 프리팹의 편집 가능한 약점
            Assert.That(marker, Is.Not.Null); // 별도 3D 약점
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 화면의 실제 중앙
            PlaceSniperTarget(target, ray.GetPoint(19f), 80); // 몸통만 중앙에 두고 체력 복원
            Physics.SyncTransforms(); // 이동한 콜라이더 등록
            InvokeFocusedCast(weapon); // 실제 집중 마법탄 생성
            yield return WaitFrames(48); // 월드 탄환이 표적에 도달
            Component life = target.GetComponent("FlightEnemy"); // 피해 적용
            Assert.That(Metric(life, "Health"), Is.EqualTo(60)); // 기본 집중 피해 20
            target.transform.position += ray.GetPoint(19f) - marker.position; // 같은 조준선에 노출된 약점 배치
            Physics.SyncTransforms(); // 약점 위치 즉시 반영
            InvokeFocusedCast(weapon); // 같은 집중탄으로 두 번째 측정
            yield return WaitFrames(48); // 새 명중 확인
            Assert.That(Metric(life, "Health"), Is.EqualTo(30)); // 약점 피해 30
        }

        [UnityTest] // 리안 전용 Q와 R의 실제 공간 범위
        public IEnumerator RianSkillsPierceTargetsButBeamStopsAtTerrain() // 관통 피해와 지형 뒤 탄막 보존
        {
            yield return LoadFlight(); // 실제 마물과 적탄 풀 로드
            yield return SelectRian(); // 리안 전투 설정
            Transform pilot = FindPilot(); // 기체의 실제 판정 중심
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 스킬 피해만 격리
            yield return WaitFrames(150); // 앞선 발사의 잔류 탄환 만료
            GameObject.Find("NearPracticeTarget").SetActive(false); // 기본 연습 표적 제거
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 실제 발사 방향 기준
            GameObject front = PlaceSniperTarget(GameObject.Find("GlassfinEnemy"), ray.GetPoint(16f), 400); // 첫 번째 중첩 관통 대상
            Transform muzzle = GameObject.Find("SpellMuzzle").transform; // 실제 발사 위치
            Physics.SyncTransforms(); // 선두 표적의 새 위치
            Assert.That(Physics.Raycast(ray, out RaycastHit impact, 85f, 1 << 9), Is.True); // 첫 표적 표면의 조준 접촉
            Vector3 direction = (impact.point - muzzle.position).normalized; // 총구에서 선두 표적 표면으로의 방향
            GameObject rear = PlaceSniperTarget(Object.Instantiate(front), impact.point + direction * 9f, 400); // 실제 직선 궤적 위의 두 번째 표적
            Physics.SyncTransforms(); // 관통 접촉 상태 준비

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 실제 관통 사격 키
            yield return WaitFrames(54); // 두 콜라이더를 통과할 시간
            Assert.That(Metric(front.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(300)); // 선두 적 한 번 피해
            Assert.That(Metric(rear.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(300)); // 뒤 적 한 번 피해
            Assert.That(HudText(LocateCombatHud().transform.Find("SkillSlots/Q/Cooldown")), Is.EqualTo("10")); // 시험 쿨타임

            for (int index = 0; index < 10; index++) // 실제 회피로 에너지 100 충전
            {
                EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 짧은 근접 회피 궤적
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // Q 해제
            yield return WaitFrames(6); // 그레이즈 자원 정산
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // 광선 중간의 실제 지형
            wall.layer = 8; // 지형 충돌 레이어
            wall.transform.position = ray.GetPoint(20f); // 두 표적 사이
            wall.transform.localScale = new Vector3(6f, 6f, 1f); // 광선과 벽의 접촉 면적
            Vector3 beamPoint = impact.point - direction; // 벽 앞쪽의 실제 광선 경로
            EmitThreat(beamPoint, direction, 0.001f); // 광선 안 적탄
            EmitThreat(beamPoint + Vector3.right * 4f, direction, 0.001f); // 광선 바깥 적탄
            EmitThreat(impact.point + direction * 10f, direction, 0.001f); // 벽 뒤 적탄
            Physics.SyncTransforms(); // 벽의 충돌 등록

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 실제 광선 필살기 키
            yield return WaitFrames(5); // 즉시 광선 피해와 적탄 회수
            Assert.That(Metric(front.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(80)); // 지형 앞 피해 220
            Assert.That(Metric(rear.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(300)); // 지형 뒤 남은 체력
            Assert.That(Metric(GetThreatPool(), "ActiveCount"), Is.EqualTo(2)); // 광선 안의 앞쪽 탄만 소거
            Assert.That(HudText(LocateCombatHud().transform.Find("Ultimate/Label")), Is.EqualTo("ULT 0 / 100")); // 필살기 에너지 100 소비
        }

        [UnityTest] // 공용 보호막의 1회 피해 방어
        public IEnumerator RianGuardBlocksOnlyTheFirstThreat() // 같은 보호막에 연속 두 번 접촉
        {
            yield return LoadFlight(); // 보호막과 실제 피격 판정이 연결된 씬
            yield return SelectRian(); // 저격형 보호막 설정
            Transform pilot = FindPilot(); // 적탄 피격의 현재 중심
            Transform hud = LocateCombatHud().transform; // 남은 재사용 시간 표시
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 리안 E 발동
            yield return WaitFrames(4); // 보호막 자원과 효과 반영
            Assert.That(HudText(hud.Find("SkillSlots/E/Cooldown")), Is.EqualTo("12")); // 기획서의 12초 재사용 시간
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 방어할 첫 적탄
            yield return WaitFrames(4); // 첫 접촉 완료
            Assert.That(Metric(FlightVitals(pilot), "Health"), Is.EqualTo(100)); // 첫 피해 차단
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 계속 접근하는 두 번째 적탄
            yield return WaitFrames(4); // 두 번째 접촉 완료
            Assert.That(Metric(FlightVitals(pilot), "Health"), Is.EqualTo(80)); // 이미 사용한 보호막에는 추가 방어 없음
        }

        private IEnumerator SelectRian() // 프로필 전환 입력 한 번
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F2)); // 시험 캐릭터 전환 키
            yield return WaitFrames(3); // 프로필과 HUD 적용
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 누름 해제
            yield return WaitFrames(3); // 다음 입력 대기
        }

        private static void InvokeFocusedCast(Component weapon) // 집중 사격의 동일 충돌 경로
        {
            var cast = weapon.GetType().GetMethod("CastSpread"); // 실제 발사 API
            Assert.That(cast.Invoke(weapon, new object[] { true, 1, 0f, true }), Is.True); // 집중 자동탄 하나 발사
        }

        private static GameObject PlaceSniperTarget(GameObject target, Vector3 position, int health) // 체력과 조준 좌표 설정
        {
            target.SetActive(false); // 부활 시 체력 재설정
            target.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f)); // 노출 방향과 위치 적용
            Component life = target.GetComponent("FlightEnemy"); // 실제 마물 체력
            life.GetType().GetField("maxHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(life, health); // 피해 차이를 확인할 체력
            target.SetActive(true); // 시험 대상 활성화
            return target; // 실제 월드 오브젝트
        }


        [UnityTest] // 실제 처치로 생긴 3D 경험치 조각
        public IEnumerator ExperienceDropFallsCollectsAndCarriesAcrossLevels() // 세 배 크기와 연속 레벨업
        {
            yield return LoadFlight(); // 씬에 저장된 경험치 시스템
            Transform pilot = FindPilot(); // 조각을 수집할 실제 기체
            Transform hud = LocateCombatHud().transform; // 레벨과 경험치 게이지
            GameObject drops = GameObject.Find("ExperienceDrops"); // 씬의 편집 가능한 드롭 시스템
            Assert.That(drops, Is.Not.Null); // 실제 출격 씬에 경험치 연결
            Transform dropRoot = drops.transform; // 조각의 부모 하이러키
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 경험치를 주는 시험 마물
            Component life = enemy.GetComponent("FlightEnemy"); // 마물의 실제 처치 처리
            life.GetType().GetField("experienceReward", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(life, 35); // 한 번에 두 레벨을 넘길 시험값
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 실제 죽음에서 경험치 발생
            Assert.That(dropRoot.childCount, Is.EqualTo(1)); // 처치 한 번에 조각 하나
            Transform shard = dropRoot.GetChild(0); // 살아 있는 경험치 조각
            float height = shard.position.y; // 드롭 순간 고도
            Assert.That(shard.localScale.x, Is.EqualTo(0.48f).Within(0.01f)); // 기준 0.16의 세 배 표시
            yield return WaitFrames(62); // 공중에서 약 1초 낙하
            Assert.That(height - shard.position.y, Is.InRange(0.45f, 0.57f)); // 초당 0.5 월드 단위 낙하
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 획득 전에는 초기 수치
            pilot.position = shard.position; // 실제 기체와 조각의 접촉
            yield return WaitFrames(3); // 획득과 HUD 반영
            Assert.That(shard.gameObject.activeSelf, Is.False); // 획득 뒤 조각 풀 회수
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 3   XP 5 / 30")); // 연속 레벨업과 남은 경험치
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 현재 시험 출격 재시작
            yield return WaitFrames(4); // 출격 상태 초기화
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 새 출격 경험치 분리
        }

        [UnityTest] // 획득하지 않은 조각의 만료
        public IEnumerator ExperienceDropExpiresAndReusesItsView() // 만료 뒤 경험치 지급 방지와 풀 재사용
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject drops = GameObject.Find("ExperienceDrops"); // 씬에서 편집할 처리 오브젝트
            Assert.That(drops, Is.Not.Null); // 실제 드롭 연결 확인
            Transform pilot = FindPilot(); // 획득 거리 밖 기체
            Transform hud = LocateCombatHud().transform; // 레벨업 여부 확인
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 경험치 드롭의 실제 공급원
            Component life = enemy.GetComponent("FlightEnemy"); // 반복 처치에 사용할 마물
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 첫 실제 처치
            Transform first = drops.transform.GetChild(0); // 고유 드롭 오브젝트
            yield return WaitFrames(488); // 8초의 공중 드롭 수명 경과
            Assert.That(first.gameObject.activeSelf, Is.False); // 채집되지 않은 조각의 회수
            pilot.position = first.position; // 만료된 조각과 겹치게 비행
            yield return WaitFrames(3); // 만료된 보상의 지급 여부 확인
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 만료 뒤 XP는 그대로
            enemy.SetActive(true); // 마물 시험 부활
            enemy.transform.position += Vector3.forward * 8f; // 두 번째 드롭만 기체와 떨어진 위치
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 같은 마물의 정상 재등장 후 처치
            Assert.That(drops.transform.childCount, Is.EqualTo(1)); // 추가 생성 없이 기존 조각 재사용
            Assert.That(first.gameObject.activeSelf, Is.True); // 반환된 조각이 다시 보임
        }

        [UnityTest] // 씬 상단 웨이브 표시와 실제 적 수
        public IEnumerator WaveHudCountsActiveEnemiesAndResetsElapsedTime() // 출격 시각과 웨이브 전환
        {
            yield return LoadFlight(); // 실제 시작 씬
            ((Behaviour)FindPilot().GetComponent("FlightWeapon")).enabled = false; // 표시 점검 중 임의 처치 방지
            Transform header = LocateCombatHud().transform.Find("WaveReadout"); // 편집 가능한 상단 중앙 HUD
            Assert.That(header, Is.Not.Null); // 씬에서 편집할 제목 그룹
            var layout = (RectTransform)header; // 그룹의 화면 기준 위치
            Assert.That(layout.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f))); // 화면 위쪽 가운데 고정
            Assert.That(layout.rect.width, Is.EqualTo(206.4f).Within(0.01f)); // 상단 전체의 가로 80%
            Assert.That(layout.rect.height, Is.EqualTo(76.8f).Within(0.01f)); // 상단 전체의 세로 80%
            Transform timer = header.Find("Timer"); // 큰 전투 시간
            Transform wave = header.Find("Wave"); // 시간 아래의 웨이브 번호
            Transform enemies = header.Find("Remaining"); // 아이콘 옆에 남은 적
            Assert.That(timer.GetComponent<UnityEngine.UI.Text>().fontSize, Is.GreaterThan(wave.GetComponent<UnityEngine.UI.Text>().fontSize)); // 타이머가 더 큰 글자
            Assert.That(timer.GetComponent<UnityEngine.UI.Text>().fontSize, Is.EqualTo(34)); // 43에서 80%로 바뀐 숫자
            Assert.That(wave.GetComponent<UnityEngine.UI.Text>().fontSize, Is.EqualTo(18)); // 22에서 80%로 바뀐 번호
            Assert.That(header.Find("MonsterIcon").GetComponent<UnityEngine.UI.RawImage>().texture, Is.Not.Null); // 원형 몬스터 그림
            Assert.That(HudText(wave), Is.EqualTo("Wave : 1")); // 첫 웨이브 번호
            Assert.That(HudText(enemies), Is.EqualTo(": 1")); // 첫 시험 적 한 마리
            yield return WaitFrames(64); // 1초 경과
            Assert.That(HudText(timer), Is.EqualTo("00:01")); // 정상적인 분·초 표기

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 실제 시험 웨이브 시작
            yield return WaitFrames(3); // 웨이브와 적 수 적용
            Assert.That(HudText(wave), Is.EqualTo("Wave : 2")); // 다음 시험 웨이브
            Assert.That(HudText(enemies), Is.EqualTo(": 3")); // 세 경로 마물
            Component life = GameObject.Find("AimedEnemy").GetComponent("FlightEnemy"); // 이번 웨이브의 실제 적
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 한 마리 처치
            yield return WaitFrames(3); // 처치 상태 반영
            Assert.That(HudText(enemies), Is.EqualTo(": 2")); // 실제로 남은 활성 마물
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 다시 출격
            yield return WaitFrames(4); // 초기 웨이브와 시간 적용
            Assert.That(HudText(timer), Is.EqualTo("00:00")); // 새 전투 시간
            Assert.That(HudText(wave), Is.EqualTo("Wave : 1")); // 첫 웨이브 복귀
            Assert.That(HudText(enemies), Is.EqualTo(": 1")); // 시작 표적 복귀
        }

        [UnityTest] // 연속 레벨업과 안전한 복귀
        public IEnumerator LevelUpChoicesPauseCombatAndPresentEveryEarnedLevel() // 선택 중 웨이브·조작 정지
        {
            yield return LoadFlight(); // XP와 카드 창이 부착된 실제 씬
            Transform pilot = FindPilot(); // 현재 비행체
            Transform canvas = LocateCombatHud().transform; // 전투와 보상 레이아웃
            Transform modal = canvas.Find("LevelUpChoices"); // 편집할 수 있는 실제 선택 창
            Assert.That(modal, Is.Not.Null); // 씬 편집된 카드 창
            Assert.That(modal.gameObject.activeSelf, Is.False); // 전투 시작 때 숨김
            GrantChoiceExperience(35); // 레벨 2와 3의 선택 둘 다 예약
            Assert.That(Time.timeScale, Is.Zero); // 실제 일시정지
            Assert.That(HudText(modal.Find("Heading")), Does.StartWith("레벨 2")); // 첫 번째 카드 차례
            string one = HudText(modal.Find("Choice1/Title")); // 세 선택지의 실제 이름
            string two = HudText(modal.Find("Choice2/Title")); // 중복 검증용 후보
            string three = HudText(modal.Find("Choice3/Title")); // 마지막 선택지
            Assert.That(new[] { one, two, three }, Is.Unique); // 동일 강화의 중복 등장 금지
            Vector3 start = pilot.position; // 선택 창을 띄울 때의 시작 위치
            string timer = HudText(canvas.Find("WaveReadout/Timer")); // 현재 출격 경과 시간
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Y, Key.F2)); // 모달 중 전투 조작 시도
            yield return WaitFrames(65); // 시간과 위치 유지 시험
            Assert.That(pilot.position, Is.EqualTo(start)); // 실제 비행 움직임 없음
            Assert.That(HudText(canvas.Find("WaveReadout/Wave")), Is.EqualTo("Wave : 1")); // Y가 새 웨이브를 만들지 않음
            Assert.That(HudText(canvas.Find("WaveReadout/Timer")), Is.EqualTo(timer)); // 일시정지 시간 제외
            modal.Find("Choice1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 마우스 버튼과 같은 선택 연결
            yield return WaitFrames(3); // 첫 번째 선택 적용
            Assert.That(HudText(modal.Find("Heading")), Does.StartWith("레벨 3")); // 남은 레벨업을 한 장씩 선택
            Assert.That(Time.timeScale, Is.Zero); // 두 번째 선택 전에는 아직 정지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit2)); // 실제 키보드 두 번째 카드
            yield return WaitFrames(4); // 게임 상태 복원
            Assert.That(modal.gameObject.activeSelf, Is.False); // 선택 창 종료
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 실제 전투 시간 복귀
        }

        [UnityTest] // 최대 단계와 후보 부족, E 교체 확인
        public IEnumerator LevelUpChoicesReplaceGuardAndFillMissingCandidates() // 대체 세 장과 원래 효과 해제
        {
            yield return LoadFlight(); // 실제 업그레이드 씬
            Transform canvas = LocateCombatHud().transform; // 보상 카드와 E 아이콘
            var upgrades = canvas.GetComponent("FlightUpgradeChoices"); // 출격 중 임시 성장
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; // 후보 시험용 필드
            int[] ranks = (int[])upgrades.GetType().GetField("ranks", flags).GetValue(upgrades); // 포화에 도달한 시험 상태
            for (int index = 0; index < ranks.Length; index++) // 모든 강화 후보 한도 설정
            {
                ranks[index] = 3; // 일반·희귀·영웅의 상한 도달
            }
            ranks[5] = 0; // 교체할 E 산탄만 유효
            Transform pilot = FindPilot(); // 실제 보호막 적용 대상
            var vitals = FlightVitals(pilot); // 피격 후 남은 체력
            vitals.GetType().GetMethod("GrantSlotProtection").Invoke(vitals, new object[] { 1.4f }); // 활성화된 아리아 E 보호
            GrantChoiceExperience(10); // 첫 레벨업
            Transform modal = canvas.Find("LevelUpChoices"); // 실제 선택 UI
            Assert.That(HudText(modal.Find("Choice1/Title")), Is.EqualTo("산탄 스킬")); // 유효한 교체 카드
            Assert.That(HudText(modal.Find("Choice2/Title")), Is.EqualTo("필살기 충전")); // 대체 충전 후보
            Assert.That(HudText(modal.Find("Choice3/Title")), Is.EqualTo("체력 증가 1")); // 효과가 다른 최대 체력 후보
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1)); // E 교체 선택
            yield return WaitFrames(4); // 이전 E 해제와 아이콘 변경
            Assert.That((bool)upgrades.GetType().GetProperty("HasFan").GetValue(upgrades), Is.True); // 새 공용 스킬 적용
            Assert.That(canvas.Find("SkillSlots/E/Art").GetComponent<UnityEngine.UI.RawImage>().texture.name, Is.EqualTo("CardElementFire")); // 현재 슬롯에 맞는 그림
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 예전 E 보호가 남았는지 확인
            yield return WaitFrames(4); // 실제 피격 처리
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 이전 보호막이 새 슬롯에 남지 않음
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 새 출격
            yield return WaitFrames(4); // 성장 초기화
            Assert.That((bool)upgrades.GetType().GetProperty("HasFan").GetValue(upgrades), Is.False); // 이전 E 교체 제거
        }

        private static void GrantChoiceExperience(int amount) // 후보 상태만 검사하기 위한 경험치 지급
        {
            Component experience = GameObject.Find("ExperienceDrops").GetComponent("FlightExperience"); // 기존 출격 성장
            experience.GetType().GetMethod("Grant", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(experience, new object[] { amount }); // 레벨업 이벤트를 발생시키는 정상 처리
        }

        [UnityTest] // ESC 정지 UI와 출격 제어
        public IEnumerator EscapePauseMenuSwitchesPanelsAndRestartsAfterAbandon() // 정지·설정·재출격
        {
            yield return LoadFlight(); // 실제 시작 씬의 정지 화면
            Transform hud = LocateCombatHud().transform; // 전투 화면과 입력 메뉴
            Transform pause = hud.Find("PauseMenu"); // 편집 가능한 정지 메뉴
            Assert.That(pause, Is.Not.Null); // 실제 씬의 정지 메뉴
            Assert.That(pause.gameObject.activeSelf, Is.False); // 출격 중에 숨겨진 메뉴
            Transform pilot = FindPilot(); // 전투 멈춤을 비교할 비행체
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 ESC
            yield return WaitFrames(4); // 정지 프레임
            Assert.That(pause.gameObject.activeSelf, Is.True); // 메뉴 표시
            Assert.That(Time.timeScale, Is.Zero); // 전투 시간 정지
            Assert.That(HudText(pause.Find("MenuButtons/RunStatus/Timer")), Is.EqualTo(HudText(hud.Find("WaveReadout/Timer")))); // 정지 순간의 실제 시간
            Assert.That(HudText(pause.Find("MenuButtons/RunStatus/Wave")), Is.EqualTo("Wave : 1")); // 현재 웨이브 번호
            Assert.That(HudText(pause.Find("MenuButtons/RunStatus/Remaining")), Is.EqualTo(": 1")); // 첫 마물 수
            Assert.That(((RectTransform)pause.Find("Controls")).rect.width, Is.EqualTo(1124f).Within(0.01f)); // 안내선의 오른쪽 패널 너비
            Assert.That(((RectTransform)pause.Find("Controls")).rect.height, Is.EqualTo(820f).Within(0.01f)); // 안내선의 오른쪽 패널 높이
            Assert.That(((RectTransform)pause.Find("MenuButtons")).anchoredPosition.x, Is.EqualTo(-590f).Within(0.01f)); // 파란 왼쪽 버튼 열 위치
            Assert.That(((RectTransform)pause.Find("MenuButtons/Continue")).rect.width, Is.EqualTo(280f).Within(0.01f)); // 크게 이동한 첫 번째 버튼
            Assert.That(pause.Find("Controls/PanelArt").GetComponent<UnityEngine.UI.RawImage>().texture.name, Is.EqualTo("PausePanelArt")); // 새로 제작한 패널 배경
            var buttons = new[] { "Continue", "Restart", "Settings", "Abandon" }; // 화면에 배치한 순서
            for (int index = 1; index < buttons.Length; index++) // 버튼 사이 세 간격
            {
                var before = (RectTransform)pause.Find("MenuButtons/" + buttons[index - 1]); // 위쪽 버튼
                var after = (RectTransform)pause.Find("MenuButtons/" + buttons[index]); // 아래쪽 버튼
                Assert.That(before.anchoredPosition.y - after.anchoredPosition.y, Is.EqualTo(107f).Within(0.01f)); // 일정한 간격
                Assert.That(after.GetComponent<UnityEngine.UI.RawImage>().texture.name, Is.EqualTo("PauseButtonArt")); // 새로 제작한 버튼 배경
            }
            Assert.That(pause.Find("Controls/Keyboard").GetComponent<UnityEngine.UI.RawImage>().texture.name, Is.EqualTo("PauseKeyboard")); // 실제 벡터 키 배치
            Assert.That(pause.Find("Controls/Keyboard").GetComponent<UnityEngine.UI.RawImage>().texture.width, Is.EqualTo(1824)); // 원본 평면 키보드의 두 배 렌더 해상도
            Assert.That(pause.Find("Controls/Forward/Action").GetComponent<UnityEngine.UI.Text>().fontStyle, Is.EqualTo(FontStyle.Normal)); // 작은 글자의 실제 글꼴 획
            Vector3 position = pilot.position; // 멈춘 동안의 출발 좌표
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Y)); // 멈춘 전투에 비행과 웨이브 시도
            yield return WaitFrames(15); // 미입력 상태 확인
            Assert.That(pilot.position, Is.EqualTo(position)); // 비행 중지
            ClickPauseButton(pause, "MenuButtons/Settings"); // 왼쪽 설정 버튼의 실제 이벤트
            Assert.That(pause.Find("Settings").gameObject.activeSelf, Is.True); // 오른쪽 설정 열림
            Assert.That(pause.Find("Controls").gameObject.activeSelf, Is.False); // 오른쪽 조작 패널 교체
            ClickPauseButton(pause, "Settings/AudioTab"); // 사운드 및 마우스 감도 탭
            var volume = pause.Find("Settings/AudioSettings/Volume").GetComponent<UnityEngine.UI.Slider>(); // 전체 음량 제어
            volume.value = 0.65f; // 설정에서 조절한 값
            Assert.That(AudioListener.volume, Is.EqualTo(0.65f).Within(0.01f)); // 실제 전역 소리 변화
            ClickPauseButton(pause, "Settings/AudioSettings/Mute"); // 음소거 켜기
            Assert.That(AudioListener.volume, Is.Zero); // 실제 전체 소리 끄기
            ClickPauseButton(pause, "Settings/AudioSettings/Mute"); // 음소거 해제
            volume.value = 1f; // 다음 테스트에 영향 없는 음량 복원
            var sensitivity = pause.Find("Settings/AudioSettings/Sensitivity").GetComponent<UnityEngine.UI.Slider>(); // 시점 회전 감도
            sensitivity.value = 150f; // 변경한 설정값
            Component controller = pilot.GetComponent("FirstFlightController"); // 실제 비행 시점
            var scale = controller.GetType().GetField("sensitivityScale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 마우스 선회 배율
            Assert.That(scale.GetValue(controller), Is.EqualTo(1.5f)); // 실제 감도 값 적용
            sensitivity.value = 100f; // 다음 테스트에 영향 없는 감도 복원
            ClickPauseButton(pause, "MenuButtons/Continue"); // 계속하기 입력
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 누른 전투 키 해제
            yield return WaitFrames(4); // 재생 상태 복구
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 다시 진행
            Assert.That(pause.gameObject.activeSelf, Is.False); // 메뉴 닫힘
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 다시 정지
            yield return WaitFrames(4); // 새 메뉴 표시
            ClickPauseButton(pause, "MenuButtons/Abandon"); // 왼쪽 포기 선택
            ClickPauseButton(pause, "Confirm/Accept"); // 실제 포기 확정
            Assert.That(pause.Find("MenuButtons/Continue").GetComponent<UnityEngine.UI.Button>().interactable, Is.False); // 포기 뒤 계속하기 금지
            ClickPauseButton(pause, "MenuButtons/Restart"); // 새 시험 출격
            yield return WaitFrames(4); // 초기 화면 복귀
            Assert.That(pause.gameObject.activeSelf, Is.False); // 새 출격 화면
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 새 웨이브 실제 재생
            Assert.That(HudText(hud.Find("WaveReadout/Wave")), Is.EqualTo("Wave : 1")); // 첫 웨이브 초기화
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 경험치와 레벨 초기화
        }

        private static void ClickPauseButton(Transform panel, string path) // 실제 정지 메뉴의 버튼
        {
            panel.Find(path).GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 연결된 클릭 이벤트 실행
        }

        [UnityTest] // 설정의 키 변경이 각 화면과 실제 스킬에 반영
        public IEnumerator PauseKeyRebindUpdatesKeycapsAndActualSkillInput() // Q에서 G로 변경하고 원복
        {
            yield return LoadFlight(); // 실제 배포 시작 씬
            Transform pilot = FindPilot(); // 실제 자동 무기와 스킬 상태
            Transform hud = LocateCombatHud().transform; // 전투 스킬의 키 이미지
            Transform pause = hud.Find("PauseMenu"); // ESC로 여는 실제 설정 창
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 설정을 열기 전 정지
            yield return WaitFrames(3); // 메뉴 표시
            ClickPauseButton(pause, "MenuButtons/Settings"); // 현재 환경 설정
            ClickPauseButton(pause, "Settings/KeySettings/Special/Key"); // 고유기의 기존 키캡 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 이미 사용 중인 키
            yield return WaitFrames(3); // 중복 키 판정
            Assert.That(HudText(pause.Find("Settings/KeySettings/Special/Key/Label")), Is.EqualTo("Q")); // 이전 Q 유지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G)); // 비어 있는 새 키 선택
            yield return WaitFrames(3); // 키와 화면 다시 적용
            Assert.That(HudText(pause.Find("Settings/KeySettings/Special/Key/Label")), Is.EqualTo("G")); // 설정 키 이미지
            Assert.That(HudText(hud.Find("SkillSlots/Q/Key")), Is.EqualTo("G")); // 전투 HUD 이미지
            ClickPauseButton(pause, "Settings/Guide"); // 조작 안내로 이동
            Assert.That(HudText(pause.Find("Controls/Special/Keycap/Label")), Is.EqualTo("G")); // 설명 옆 키캡 갱신
            Component art = pause.Find("Controls/KeyboardHighlights").GetComponent("FlightKeyboardHighlights"); // 평면 그림의 실제 색 표시
            var marked = art.GetType().GetMethod("ContainsBinding"); // 설정 변화에 대응하는 강조 위치
            Assert.That(marked.Invoke(art, new object[] { Key.G }), Is.True); // 새 G 강조
            Assert.That(marked.Invoke(art, new object[] { Key.Q }), Is.False); // 기존 Q 강조 해제
            ClickPauseButton(pause, "MenuButtons/Continue"); // 이어서 전투
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 설정 입력 해제
            yield return WaitFrames(4); // 실제 전투 시간
            Component weapon = pilot.GetComponent("FlightWeapon"); // 실제 아리아 고유 효과
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 변경 전 키는 미발동
            yield return WaitFrames(3); // 실제 스킬 입력 경과
            Assert.That(weapon.GetType().GetProperty("FireIntervalScale").GetValue(weapon), Is.EqualTo(1f)); // 이전 키 동작 없음
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G)); // 설정한 키로 스킬 발동
            yield return WaitFrames(3); // 발사 속도 보너스 반영
            Assert.That(weapon.GetType().GetProperty("FireIntervalScale").GetValue(weapon), Is.EqualTo(0.7f)); // 새 키로 전투 기능 작동
        }

        [UnityTest] // 중간·최종 보스의 실제 처치 드롭 규칙
        public IEnumerator EnhancementDropFollowsBossRulesAndCollectsWithoutPause() // 확정 드롭과 비행 획득
        {
            yield return LoadFlight(); // 제작한 3D 조사 씬
            GameObject drops = GameObject.Find("EnhancementDrops"); // 강화 아이템 풀
            Assert.That(drops, Is.Not.Null); // 실제 출격에서 사용하는 드롭 오브젝트
            Component loot = drops.GetComponent("FlightEnhancements"); // 실제 드롭과 적용
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 중간·최종 보스로 설정할 시험 표적
            Component life = enemy.GetComponent("FlightEnemy"); // 처치와 보상 종류
            life.GetType().GetField("experienceReward", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(life, 0); // 강화 아이템만 독립 확인
            var tier = life.GetType().GetField("lootTier", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 씬에서 편집하는 보상 종류
            tier.SetValue(life, System.Enum.Parse(tier.FieldType, "FinalBoss")); // 최종 보스는 지급하지 않음
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 실제 처치
            Assert.That(Metric(loot, "ActiveCount"), Is.Zero); // 최종 보스에서 강화 아이템 없음
            enemy.SetActive(true); // 새 체력의 시험 표적
            enemy.transform.position += Vector3.up * 2f; // 경험치와 분리된 공중 위치
            tier.SetValue(life, System.Enum.Parse(tier.FieldType, "MidBoss")); // 중간 보스는 유효 아이템 확정
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 기존 실제 처리 경로
            Assert.That(Metric(loot, "ActiveCount"), Is.EqualTo(1)); // 처치 시 확정 한 개
            Transform item = drops.transform.GetChild(0); // 실제 드롭의 월드 위치
            float height = item.position.y; // 출현 고도
            yield return WaitFrames(62); // 다른 아이템과 같은 속도로 하강
            Assert.That(height - item.position.y, Is.InRange(0.45f, 0.56f)); // 낙하 정상
            Transform pilot = FindPilot(); // 마물 처치 전 위치
            Vector3 approach = (pilot.position - item.position).normalized; // 조각을 지나치지 않고 같은 쪽에서 접근
            pilot.position = item.position + approach * 1.3f; // 수집 반경보다 먼 위치
            yield return WaitFrames(3); // 실제 획득 사거리 판정
            Assert.That(item.gameObject.activeSelf, Is.True); // 반경 0.65 밖에 있는 드롭
            pilot.position = item.position; // 실제 획득 범위
            yield return WaitFrames(3); // 회수 상태 확인
            Assert.That(item.gameObject.activeSelf, Is.False); // 즉시 획득하고 풀로 복귀
            Assert.That((string)loot.GetType().GetProperty("LastPickup").GetValue(loot), Is.Not.Empty); // 실제로 획득한 효과와 등급
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 강화 아이템은 별도 선택 없이 획득
            enemy.SetActive(true); // 같은 보스의 다른 시험 처치
            enemy.transform.position = pilot.position + Vector3.up * 9f + Vector3.forward * 8f; // 수집하지 않을 두 번째 드롭
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 강화 아이템을 한 번 더 드롭
            Assert.That(item.gameObject.activeSelf, Is.True); // 회수한 월드 오브젝트 재사용
            yield return WaitFrames(1140); // 드롭이 아직 만료되지 않는 19초
            Assert.That(item.gameObject.activeSelf, Is.True); // 유지 시간 20초 전
            yield return WaitFrames(68); // 20초를 지난 값
            Assert.That(item.gameObject.activeSelf, Is.False); // 만료되어 풀로 반환
        }

        [UnityTest] // 사격의 카드·아이템 공통 상한 및 보호막 순서
        public IEnumerator EnhancementRanksShareCardLimitsAndTimedShieldTakesPriority() // 화력·관통 상한과 보호막 갱신
        {
            yield return LoadFlight(); // 실제 강화 시스템 로드
            Component loot = GameObject.Find("EnhancementDrops")?.GetComponent("FlightEnhancements"); // 실제 아이템 효과
            Assert.That(loot, Is.Not.Null); // 씬에 연결한 강화
            Component upgrades = LocateCombatHud().GetComponent("FlightUpgradeChoices"); // 기존 카드의 공유 화력
            int[] cards = (int[])upgrades.GetType().GetField("ranks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(upgrades); // 시험 최대 카드 랭크
            cards[0] = 3; // 화력 카드 +30%
            Assert.That(ApplyEnhancement(loot, "Damage", 5), Is.True); // 화력 장비 +18%
            Assert.That(ApplyEnhancement(loot, "Damage", 5), Is.True); // 나머지 12%만 추가
            Assert.That((float)upgrades.GetType().GetMethod("DamageScale").Invoke(upgrades, new object[] { false }), Is.EqualTo(1.60f).Within(0.001f)); // 공통 피해 +60%
            Assert.That(ApplyEnhancement(loot, "Damage", 1), Is.False); // 상한 뒤 추가 아이템 제외
            cards[2] = 2; // 추가 관통 +2 기준
            Assert.That(ApplyEnhancement(loot, "Pierce", 5), Is.True); // 아이템은 남은 1만 추가
            Assert.That(Metric(upgrades, "AutoPierce"), Is.EqualTo(3)); // 공통 관통 상한 +3
            Assert.That(ApplyEnhancement(loot, "Shield", 3), Is.True); // 아이템 보호막 3회
            Assert.That(ApplyEnhancement(loot, "Shield", 2), Is.False); // 더 낮은 방어는 합산하지 않음
            Transform pilot = FindPilot(); // 실제 접촉 시험
            Component vitals = FlightVitals(pilot); // 현재 방어막과 체력
            vitals.GetType().GetMethod("GrantSlotProtection").Invoke(vitals, new object[] { 0.2f }); // 시간제 보호막 먼저 활성
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 보호막 동시 활성 시 접촉
            yield return WaitFrames(5); // 시간제 보호만 반영
            Assert.That(Metric(vitals, "ItemShield"), Is.EqualTo(3)); // 아이템 보호 횟수 유지
            yield return WaitFrames(15); // 시간제 보호 종료
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 아이템 보호만 남은 접촉
            yield return WaitFrames(4); // 방어막 자원 반영
            Assert.That(Metric(vitals, "ItemShield"), Is.EqualTo(2)); // 아이템 보호가 한 번 소모
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(100)); // 체력은 유지
        }

        private static bool ApplyEnhancement(Component loot, string kind, int grade) // 실제 획득 효과 경로를 직접 시험
        {
            var method = loot.GetType().GetMethod("TryApply", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 실제 효과 적용
            var type = method.GetParameters()[0].ParameterType; // 현재 아이템 종류
            return (bool)method.Invoke(loot, new[] { System.Enum.Parse(type, kind), (object)grade }); // 정해진 등급을 획득하는 검증
        }

        [UnityTest] // 두 보조 슬롯과 피격 보호 장치
        public IEnumerator EnhancementCardsEquipDroneBladesAndProtectionDevice() // 실제 추가 피해와 보호
        {
            yield return LoadFlight(); // 저장된 보조 장비와 각 공격
            Transform pilot = FindPilot(); // 실제 하이러키에 붙은 장비 대상
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 보조 공격만 독립 확인
            yield return WaitFrames(140); // 캐릭터 기본탄의 이전 발사 정리
            GameObject.Find("NearPracticeTarget").SetActive(false); // 드론에서 적까지 사선 확보
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 실제 공격 대상
            Component life = enemy.GetComponent("FlightEnemy"); // 이동한 마물의 실제 체력
            enemy.transform.position = pilot.position + Vector3.forward * 2.2f; // 칼날과 드론 사정거리
            Physics.SyncTransforms(); // 이동한 마물의 콜라이더 반영
            Component upgrades = LocateCombatHud().GetComponent("FlightUpgradeChoices"); // 보조 카드의 적용 경로
            ApplyGearCard(upgrades, "Drone"); // 첫 보조 슬롯
            ApplyGearCard(upgrades, "Blade"); // 두 번째 보조 슬롯
            ApplyGearCard(upgrades, "ProtectionDevice"); // 패시브 슬롯
            yield return WaitFrames(33); // 칼날 접촉과 보조 탄환 도달
            Assert.That(pilot.Find("AttackDrone").gameObject.activeSelf, Is.True); // 프로펠러 드론 활성화
            Assert.That(pilot.Find("OrbitBlades").gameObject.activeSelf, Is.True); // 실제 회전 칼날 표시
            Assert.That(Metric(life, "Health"), Is.EqualTo(66)); // 첫 칼날 8과 드론 탄 6
            Component vitals = FlightVitals(pilot); // 보조 보호 장치와 피해
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 장치가 충전될 첫 피해
            yield return WaitFrames(4); // 최초 피격 및 장치 충전
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 첫 피해 발생
            yield return WaitFrames(63); // 피격 후 공통 무적 시간이 지난 뒤
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 장치가 막을 새 탄
            yield return WaitFrames(4); // 실제 보호 장치 소모
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 다음 피해 한 번 방어
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 새 출격에서 보조 슬롯 초기화
            yield return WaitFrames(4); // 이전 시제품 장비 제거
            Assert.That(pilot.Find("AttackDrone").gameObject.activeSelf, Is.False); // 드론 장착 초기화
            Assert.That(pilot.Find("OrbitBlades").gameObject.activeSelf, Is.False); // 칼날 장착 초기화
        }

        private static void ApplyGearCard(Component upgrades, string name) // 보조 카드의 실제 적용 경로
        {
            var method = upgrades.GetType().GetMethod("Apply", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 카드 결과 적용 메서드
            var reward = System.Enum.Parse(method.GetParameters()[0].ParameterType, name); // 지정한 장비 카드
            method.Invoke(upgrades, new[] { reward }); // 실제 선택과 같은 장착
        }

        [UnityTest] // 우상단 레이더와 전투 조작 설명 패널 제거
        public IEnumerator RadarTracksEnemyBearingWithoutGameplayGuide() // 실제 적 좌표의 방향·거리·제거
        {
            yield return LoadFlight(); // 저장된 새 레이더가 있는 시작 씬
            Assert.That(GameObject.Find("FlightControls"), Is.Null); // 전투 중 조작 설명 제거
            Assert.That(GameObject.Find("ControlBackplate"), Is.Null); // 전투용 옛 배경도 제거
            Transform hud = LocateCombatHud().transform; // 화면 위치 확인
            var radar = (RectTransform)hud.Find("EnemyRadar"); // 별도 우상단 레이더
            Assert.That(radar, Is.Not.Null); // 씬의 원형 레이더
            Assert.That(radar.anchorMin, Is.EqualTo(Vector2.one)); // 오른쪽 위 실제 고정
            Assert.That(hud.Find("WaveReadout"), Is.Not.Null); // 원래 상단 중앙 타이머 유지
            Transform pilot = FindPilot(); // 적 위치를 측정하는 기준
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 표적 이동의 중복 처치 방지
            Transform enemy = GameObject.Find("GlassfinEnemy").transform; // 첫 활성 표적
            Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized; // 실제 카메라의 수평 방향
            Vector3 right = Vector3.Cross(Vector3.up, forward); // 현재 미리보기 오른쪽
            enemy.position = pilot.position + right * 12f + forward * 5f; // 오른쪽 12·앞 5의 실제 거리 13
            yield return WaitFrames(2); // 이동한 적 위치 반영
            var contact = (RectTransform)radar.Find("Contact0"); // 해당 적의 실제 UI 위치
            Assert.That(contact.gameObject.activeSelf, Is.True); // 활성 표적 표시
            Assert.That(contact.anchoredPosition.x, Is.GreaterThan(0f)); // 오른쪽 점 위치
            Assert.That(contact.anchoredPosition.y, Is.GreaterThan(0f)); // 정면보다 살짝 오른쪽 앞
            Assert.That(HudText(contact.Find("Distance")), Is.EqualTo("13 m")); // 월드 위치를 이용한 직선 거리
            enemy.position = pilot.position - right * 12f - forward * 5f; // 반대편 왼쪽 뒤
            yield return WaitFrames(2); // 현재 위치 갱신
            Assert.That(contact.anchoredPosition.x, Is.LessThan(0f)); // 왼쪽으로 이동한 적
            Assert.That(contact.anchoredPosition.y, Is.LessThan(0f)); // 뒤쪽도 원에 표시
            enemy.gameObject.SetActive(false); // 이탈하거나 사라진 표적
            yield return WaitFrames(2); // 현재 표적 수 동기화
            Assert.That(contact.gameObject.activeSelf, Is.False); // 제거한 적의 레이더 점 삭제

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 시험 웨이브의 경로 마물 소환
            yield return WaitFrames(3); // 웨이브 진입
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 시험 키 해제
            Transform encounter = GameObject.Find("EnemyEncounter").transform; // 씬의 경로 마물 그룹
            Transform aimed = encounter.Find("AimedEnemy"); // 첫 거리 마커의 실제 마물
            Transform fan = encounter.Find("FanEnemy"); // 둘째 거리 마커의 실제 마물
            ((Behaviour)aimed.GetComponent("FlightEnemyRoute")).enabled = false; // 고정 위치를 이용한 겹침 확인
            ((Behaviour)fan.GetComponent("FlightEnemyRoute")).enabled = false; // 고정 위치를 이용한 거리 비교
            encounter.Find("DiverEnemy").gameObject.SetActive(false); // 겹침 검증에 필요한 적만 유지
            aimed.position = pilot.position + forward * 25f; // 표시상 가까운 위치의 먼 적
            fan.position = pilot.position + forward * 24f + right; // 표시와 글자가 겹치는 가까운 적
            yield return WaitFrames(2); // 글자 표시 우선순위 갱신
            Transform secondContact = radar.Find("Contact1"); // 두 번째 표시 슬롯
            Assert.That(contact.gameObject.activeSelf && secondContact.gameObject.activeSelf, Is.True); // 두 마커 모두 유지
            Assert.That(((Behaviour)contact.Find("Distance").GetComponent("Text")).enabled, Is.False); // 겹치는 먼 거리 숫자만 숨김
            Assert.That(((Behaviour)secondContact.Find("Distance").GetComponent("Text")).enabled, Is.True); // 가까운 거리 우선 표시
            aimed.position = pilot.position - right * 35f; // 글자가 겹치지 않는 위치
            yield return WaitFrames(2); // 거리 표시 복원
            Assert.That(((Behaviour)contact.Find("Distance").GetComponent("Text")).enabled, Is.True); // 적마다 읽을 수 있는 경우 함께 표시
        }

        [UnityTest] // 점수 지급과 재출격 회귀
        public IEnumerator ScoreCountsDefeatAndGrazeOnceAndClearsOnRestart() // 처치·스침·이탈과 출격 초기화
        {
            yield return LoadFlight(); // 저장된 전투 씬
            Transform score = LocateCombatHud().transform.Find("ScoreReadout"); // 연속 적중 아래의 점수 표시
            Assert.That(score, Is.Not.Null, "출격 점수 HUD"); // 전투 화면의 점수 노드
            Transform label = score.Find("Label"); // 점수 합계
            Assert.That(HudText(label), Is.EqualTo("점수 0")); // 새 출격의 점수 초기값
            Transform pilot = FindPilot(); // 탄막 스침의 기준
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 사격의 임의 처치 차단
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 첫 마물을 처치 없이 이탈시킴
            yield return WaitFrames(3); // 다음 시험 웨이브 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 시험 키 해제
            Assert.That(HudText(label), Is.EqualTo("점수 0")); // 씬 비활성화에는 보상 없음
            Component enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy").GetComponent("FlightEnemy"); // 실제 웨이브 적
            enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 1 }); // 적에게 피해만 준 상태
            Assert.That(HudText(label), Is.EqualTo("점수 0")); // 명중에는 처치 점수 없음
            enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 1000 }); // 실제 적 처치
            enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 1000 }); // 같은 적에게 들어온 중복 명중
            EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 그레이즈 한 건
            EmitThreat(pilot.position + Vector3.forward * 10f, Vector3.back, 1300f); // 점수를 주지 않는 직접 피격
            yield return WaitFrames(6); // 적탄 판정과 합계 갱신
            Assert.That(HudText(label), Is.EqualTo("점수 110")); // 처치 100과 유효 스침 10의 합
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 실제 재출격 키
            yield return WaitFrames(4); // 새 출격 초기화
            Assert.That(HudText(label), Is.EqualTo("점수 0")); // 이전 출격 점수 제거
            foreach (Transform popup in score.Find("Popups")) // 프레임에 남을 수 있는 이전 획득 숫자
            {
                Assert.That(popup.gameObject.activeSelf, Is.False); // 남은 획득 숫자도 삭제
            }
        }

        [UnityTest] // 획득 숫자 이동과 각 줄의 만료
        public IEnumerator ScorePopupsMoveUpAndExpireInOrderAfterPause() // 실제 점수 이벤트와 전투 시간 검증
        {
            yield return LoadFlight(); // 저장된 전투 씬
            Transform stack = LocateCombatHud().transform.Find("ScoreReadout/Popups"); // 씬에 배치한 획득 숫자 영역
            Assert.That(stack, Is.Not.Null, "획득 점수 숫자 영역"); // 전투 화면의 숫자 영역
            Transform pilot = FindPilot(); // 적탄 지나는 위치의 기준
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 표시 시간의 임의 처치 차단
            Component enemy = GameObject.Find("GlassfinEnemy").GetComponent("FlightEnemy"); // 첫 웨이브의 실제 적
            enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 1000 }); // 첫 획득 숫자 발생
            yield return WaitFrames(36); // 두 이벤트의 개별 만료 간격
            var older = (RectTransform)stack.Find("Popup0"); // 처음 등장한 숫자
            float firstY = older.anchoredPosition.y; // 새 숫자 전의 높이
            Assert.That(older.gameObject.activeSelf, Is.True); // 첫 점수 표시 상태
            Assert.That(HudText(older), Is.EqualTo("+100")); // 처치의 실제 획득 수치
            EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 두 번째 획득 숫자
            yield return WaitFrames(9); // 새 줄과 기존 줄의 상승
            var newer = (RectTransform)stack.Find("Popup1"); // 아래에 생긴 새 획득 숫자
            Assert.That(HudText(newer), Is.EqualTo("+10")); // 그레이즈의 실제 획득 수치
            Assert.That(older.anchoredPosition.y, Is.GreaterThan(firstY)); // 이전 줄의 실제 위쪽 이동
            Assert.That(older.anchoredPosition.y, Is.GreaterThan(newer.anchoredPosition.y)); // 최신 숫자가 아래의 자리에 표시
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 전투 시간을 멈추는 실제 메뉴
            yield return WaitFrames(105); // 표시 시간보다 오래 유지
            Assert.That(Time.timeScale, Is.Zero); // 일시정지 입력 반영
            Assert.That(older.gameObject.activeSelf && newer.gameObject.activeSelf, Is.True); // 일시정지 중 숫자 수명 보존
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 키 해제
            yield return WaitFrames(2); // 복귀 입력 준비
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 전투 복귀
            yield return WaitFrames(60); // 첫 숫자만 수명을 넘긴 상태
            Assert.That(older.gameObject.activeSelf, Is.False); // 먼저 얻은 숫자가 먼저 사라짐
            Assert.That(newer.gameObject.activeSelf, Is.True); // 뒤에 얻은 숫자의 별도 수명
            yield return WaitFrames(41); // 새 숫자의 수명도 종료
            Assert.That(newer.gameObject.activeSelf, Is.False); // 남은 획득 숫자 종료
        }

        private void SetKeys(params Key[] keys) // 가상 키보드 입력 반영
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 새로운 키 상태 적용
            InputSystem.Update(); // 검증 프레임에 즉시 전달
        }

        private static IEnumerator WaitFrames(int count) // 고정 시험 간격 대기
        {
            for (int frame = 0; frame < count; frame++) // 지정된 프레임 반복
            {
                yield return null; // 실제 게임 업데이트 대기
            }
        }
        private static IEnumerator LoadFlight() // 빌드에 등록된 실제 씬 확인
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(ScenePath); // 씬의 배포 순서
            Assert.That(buildIndex, Is.EqualTo(0), "첫 비행 씬의 Windows 시작 씬 등록"); // 새 프로젝트의 시작 씬 확인
            yield return SceneManager.LoadSceneAsync(buildIndex); // 등록된 씬을 정상 경로로 열기
            yield return null; // 카메라 초기화와 프레임 대기
        }

        private static Transform FindPilot() // 실제 하이러키 검증
        {
            GameObject pilot = GameObject.Find("AriaPilot"); // 저장된 아리아 오브젝트
            Assert.That(pilot, Is.Not.Null); // 씬 편집 가능 대상 확인
            Assert.That(pilot.transform.Find("TrainingBroom"), Is.Not.Null); // 빗자루 하이러키 확인
            return pilot.transform; // 비행 좌표 제공
        }

        private IEnumerator CheckMove(Transform pilot, Key key, Vector3 direction) // 입력 키별 이동 검증
        {
            Vector3 start = pilot.position; // 이동 전 초기 위치
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 시험 키 입력
            InputSystem.Update(); // 입력 이벤트 즉시 반영
            for (int frame = 0; frame < 6; frame++) // 입력 처리 프레임
            {
                yield return null; // 플레이 프레임 진행
            }
            Assert.That(keyboard[key].isPressed, Is.True, "가상 키 입력 수신: " + key); // 키 이벤트 처리 확인
            Vector3 movement = pilot.position - start; // 실제 이동 벡터
            Assert.That(Vector3.Dot(movement, direction), Is.GreaterThan(0f), key.ToString()); // 요청 축으로 이동 확인
            Assert.That(Vector3.Cross(movement, direction).sqrMagnitude, Is.LessThan(0.001f)); // 다른 축 흔들림 검증
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 현재 키 해제
            yield return WaitFrames(24); // 다음 방향 검증 전 관성 정지
        }
    }
}
