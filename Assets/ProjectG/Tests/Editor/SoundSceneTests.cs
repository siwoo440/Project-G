using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 실제 사운드와 UI
using UnityEditor; // 직렬화 참조 기능
using UnityEditor.SceneManagement; // 실제 씬 열기
using UnityEngine; // 실제 객체 기능
using UnityEngine.UI; // 편집 가능한 UI 기능
public class SoundSceneTests // 실제 메뉴 씬의 오디오와 음량 화면 시험
{ // 시험 시작
    [Test] // 실제 씬 패널과 출력 참조 시험
    public void SoundPanelIsAnEditableSceneObject() // 저장된 실제 구성 확인
    { // 시험 시작
        var previous = EditorSceneManager.GetSceneManagerSetup(); // 기존 씬 구성 보관
        try // 시험 범위
        { // 범위 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single); // 실제 메뉴 씬 열기
            SoundSettingsPanel panel = null; // 실제 음량 패널
            SceneSoundPlayer sound = null; // 실제 오디오 루트
            int listeners = 0; // 실제 리스너 개수
            foreach (GameObject root in scene.GetRootGameObjects()) // 실제 루트 순회
            { // 확인 시작
                SoundSettingsPanel panelCandidate = root.GetComponentInChildren<SoundSettingsPanel>(true); // 숨겨진 실제 패널
                SceneSoundPlayer soundCandidate = root.GetComponentInChildren<SceneSoundPlayer>(true); // 실제 오디오 루트
                listeners += root.GetComponentsInChildren<AudioListener>(true).Length; // 저장된 리스너 수
                if (panelCandidate != null) // 실제 패널 확인
                { // 보관 시작
                    Assert.That(panel, Is.Null); // 패널 중복 차단
                    panel = panelCandidate; // 실제 패널 보관
                } // 보관 종료
                if (soundCandidate != null) // 실제 오디오 확인
                { // 보관 시작
                    Assert.That(sound, Is.Null); // 오디오 루트 중복 차단
                    sound = soundCandidate; // 실제 오디오 보관
                } // 보관 종료
            } // 확인 종료
            Assert.That(panel, Is.Not.Null); // 실제 씬 패널 확인
            Assert.That(sound, Is.Not.Null); // 실제 씬 오디오 확인
            Assert.That(panel.gameObject.scene, Is.EqualTo(scene)); // 실제 메뉴 씬 소속
            Assert.That(panel.gameObject.activeSelf, Is.False); // 설정 진입 전 숨김
            Assert.That(sound.HasSceneReferences, Is.True); // 네 개의 독립 AudioSource
            Assert.That(listeners, Is.EqualTo(1)); // 메뉴 씬의 단일 리스너
            Assert.That(panel.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay)); // 비활성 메뉴 카메라와 독립 화면
            Assert.That(panel.GetComponent<GraphicRaycaster>(), Is.Null); // 기존 수동 입력과 중복 차단
            SerializedObject data = new SerializedObject(panel); // 실제 직렬화된 참조
            Assert.That(data.FindProperty("sound").objectReferenceValue, Is.SameAs(sound)); // 실제 오디오 설정 연결
            string[] fields = new string[] // 실제 다섯 항목 목록
            { // 목록 시작
                "decrease", "increase", "values", "labels" // 실제 버튼과 글자 참조
            }; // 목록 종료
            foreach (string name in fields) // 실제 필드 순회
            { // 확인 시작
                SerializedProperty array = data.FindProperty(name); // 실제 직렬화 배열
                Assert.That(array, Is.Not.Null, name); // 실제 필드 확인
                Assert.That(array.arraySize, Is.EqualTo(5), name); // 다섯 실제 요소
                for (int index = 0; index < array.arraySize; index++) // 다섯 참조 순회
                { // 확인 시작
                    Component component = array.GetArrayElementAtIndex(index).objectReferenceValue as Component; // 실제 UI 구성
                    Assert.That(component, Is.Not.Null, name); // 실제 연결 확인
                    Assert.That(component.gameObject.scene, Is.EqualTo(scene), name); // 실제 메뉴 씬 소속
                } // 확인 종료
            } // 확인 종료
            Assert.That(panel.transform.Find("Viewport/Panel/Back").GetComponent<Button>(), Is.Not.Null); // 실제 뒤로 버튼
            Assert.That(panel.transform.Find("Viewport/Panel/SaveHint").GetComponent<Text>(), Is.Not.Null); // 실제 저장 오류 안내
        } // 범위 종료
        finally // 기존 씬 복원
        { // 복원 시작
            if (previous.Length > 0) // 복원할 씬 확인
            { // 복원 시작
                EditorSceneManager.RestoreSceneManagerSetup(previous); // 기존 씬 구성 복원
            } // 복원 종료
        } // 복원 종료
    } // 시험 종료
} // 시험 종료
