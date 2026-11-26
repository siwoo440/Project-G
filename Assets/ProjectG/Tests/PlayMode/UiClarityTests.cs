using System.Collections; // 씬 전환 대기
using NUnit.Framework; // 검증 조건
using UnityEngine; // 캔버스 조회
using UnityEngine.SceneManagement; // 실제 게임 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험

namespace ProjectG.Tests // 프로젝트 시험 영역
{
    public sealed class UiClarityTests // 화면별 UI 정렬 검증
    {
        [UnityTest] // 게임의 각 화면에서 동일한 픽셀 정렬 확인
        public IEnumerator ScreenOverlayCanvasesUsePixelAlignment() // 작은 화면의 캔버스 기준 검증
        {
            string[] scenes = { "FlightMainMenu", "FlightIslandVillage", "FlightSortieBriefing", "OuterIslandsStage", "FlightSortieResult", "FlightPrototype" }; // 실제 화면 목록
            foreach (string scene in scenes) // 각 화면의 독립 확인
            {
                yield return SceneManager.LoadSceneAsync(scene); // 해당 씬 로드
                Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 비활성 화면 포함 캔버스 조회
                bool found = false; // 화면 캔버스 발견 여부
                foreach (Canvas canvas in canvases) // 씬에 속한 캔버스 순회
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) // 화면 오버레이만 검사
                    {
                        continue; // 독립 월드 공간 제외
                    }
                    found = true; // 실제 오버레이 존재 확인
                    Assert.That(canvas.pixelPerfect, Is.True, scene + "/" + canvas.name); // 글자와 테두리 픽셀 정렬
                }
                Assert.That(found, Is.True, scene); // 씬 누락 방지
            }
        }

        [UnityTest] // 상점의 작은 상품명 표시 검증
        public IEnumerator MarketItemLabelsUseReadableFontSizeAndWeight() // 작은 화면의 상품 이름 가독성
        {
            yield return SceneManager.LoadSceneAsync("FlightIslandVillage"); // 실제 마을 씬 로드
            MonoBehaviour[] components = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 비활성 기능 포함 조회
            foreach (MonoBehaviour component in components) // 시설 컴포넌트 탐색
            {
                if (component.GetType().Name != "FlightVillageFacilities") // 상점이 아닌 기능 제외
                {
                    continue; // 다음 기능 확인
                }
                var field = component.GetType().GetField("stockText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 실제 상품 텍스트 연결
                var labels = (UnityEngine.UI.Text[])field.GetValue(component); // 상품 이름 배열
                Assert.That(labels.Length, Is.GreaterThan(0)); // 실제 상품 표시 존재
                foreach (UnityEngine.UI.Text label in labels) // 모든 상품 이름 확인
                {
                    Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(25)); // 작은 글자 크기 확보
                    Assert.That(label.fontStyle, Is.EqualTo(FontStyle.Normal)); // 작은 글자 획 분리
                }
                yield break; // 시설 확인 완료
            }
            Assert.Fail("마을 상점 표시를 찾지 못했습니다."); // 상점 기능 누락
        }

        [UnityTest] // 캐릭터 선택 버튼과 실제 선택 기능 검증
        public IEnumerator VillagePilotButtonsDisplayPortraitsAndSelectEachPilot() // 상점과 공방의 캐릭터 초상화
        {
            yield return SceneManager.LoadSceneAsync("FlightIslandVillage"); // 실제 마을 씬 로드
            yield return null; // 시설 버튼 연결 대기
            MonoBehaviour[] components = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 마을 시설 조회
            MonoBehaviour facility = System.Array.Find(components, item => item.GetType().Name == "FlightVillageFacilities"); // 실제 상점 기능
            Assert.That(facility, Is.Not.Null); // 상점 기능 존재
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 시설 내부 연결 조회
            facility.GetType().GetMethod("Open", flags).Invoke(facility, new object[] { true }); // 실제 상점 열기
            string[] names = { "aria", "rian", "lumi", "sera", "nox", "vera", "iris" }; // 캐릭터 선택 순서
            var textures = new System.Collections.Generic.HashSet<Texture>(); // 중복 초상화 검사
            var backgrounds = new System.Collections.Generic.HashSet<Color32>(); // 캐릭터별 배경색 검사
            UnityEngine.UI.Button previous = null; // 직전 선택 버튼
            foreach (string name in names) // 일곱 캐릭터 확인
            {
                var button = (UnityEngine.UI.Button)facility.GetType().GetField(name, flags).GetValue(facility); // 실제 선택 버튼
                RectTransform area = (RectTransform)button.transform; // 선택 영역 크기
                Assert.That(area.rect.width, Is.GreaterThanOrEqualTo(100f), name); // 확대된 버튼 너비
                Assert.That(area.rect.height, Is.GreaterThanOrEqualTo(100f), name); // 얼굴이 충분히 보이는 버튼 높이
                Transform portrait = button.transform.Find("Portrait"); // 독립 초상화 레이어
                Transform background = button.transform.Find("PortraitBackground"); // 독립 단색 배경 레이어
                Assert.That(portrait, Is.Not.Null, name); // 초상화 배치 확인
                Assert.That(background, Is.Not.Null, name); // 배경 배치 확인
                var picture = portrait.GetComponent<UnityEngine.UI.RawImage>(); // 실제 얼굴 이미지
                Assert.That(picture.texture, Is.Not.Null, name); // 인물 이미지 연결
                Assert.That(textures.Add(picture.texture), Is.True, name); // 캐릭터별 다른 이미지
                var fill = background.GetComponent<UnityEngine.UI.Image>(); // 독립 단색 배경
                Assert.That(fill, Is.Not.Null, name); // 별도 색상 배경
                Assert.That(backgrounds.Add((Color32)fill.color), Is.True, name); // 서로 다른 배경색
                Assert.That(picture.raycastTarget || fill.raycastTarget, Is.False, name); // 이미지가 버튼 클릭을 가리지 않음
                if (button.interactable) // 현재 선택되지 않은 캐릭터
                {
                    button.onClick.Invoke(); // 실제 버튼 선택 동작
                }
                Assert.That(button.interactable, Is.False, name); // 선택 표시 반영
                if (previous != null && previous != button) // 이전에 선택한 캐릭터
                {
                    Assert.That(previous.interactable, Is.True, name); // 이전 선택 해제
                }
                previous = button; // 다음 선택의 비교 대상
            }
            facility.GetType().GetMethod("Open", flags).Invoke(facility, new object[] { false }); // 같은 버튼을 쓰는 강화 공방 열기
            var forge = (GameObject)facility.GetType().GetField("forge", flags).GetValue(facility); // 실제 강화 화면
            Assert.That(forge.activeSelf, Is.True); // 공방 표시 확인
            var first = (UnityEngine.UI.Button)facility.GetType().GetField("aria", flags).GetValue(facility); // 공방의 첫 캐릭터
            if (first.interactable) // 현재 다른 캐릭터가 선택된 상태
            {
                first.onClick.Invoke(); // 실제 공방 선택 동작
            }
            Assert.That(first.interactable, Is.False); // 공방에서도 선택 상태 반영
        }
    }
}
