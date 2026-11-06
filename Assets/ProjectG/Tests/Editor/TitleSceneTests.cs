using NUnit.Framework; // 시험 도구
using UnityEditor; // 직렬화 참조 확인
using UnityEditor.SceneManagement; // 장면 열기 기능
using UnityEngine; // 유니티 객체 기능
public class TitleSceneTests // 직렬화된 타이틀 화면 시험
{ // 시험 시작
    [Test] // 장면 자산 검증
    public void TitleControlsExistAsEditableSceneObjects() // 타이틀 오브젝트 저장 확인
    { // 시험 시작
        var setup = EditorSceneManager.GetSceneManagerSetup(); // 기존 장면 구성 보관
        try // 시험 범위
        { // 확인 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single); // 실제 장면 열기
            GameObject canvas = null; // 타이틀 캔버스 참조
            foreach (GameObject root in scene.GetRootGameObjects()) // 루트 객체 순회
            { // 검색 시작
                if (root.name == "TitleCanvas") // 저장된 캔버스 이름 확인
                { // 발견 시작
                    canvas = root; // 캔버스 보관
                } // 발견 종료
            } // 검색 종료
            Assert.That(canvas, Is.Not.Null); // 편집 가능한 캔버스 존재
            Assert.That(canvas.GetComponent<Canvas>(), Is.Not.Null); // 캔버스 구성 확인
            Assert.That(canvas.GetComponent("TitleScreenPresenter"), Is.Not.Null); // 화면 연결 구성 확인
            SerializedObject presenter = new SerializedObject(canvas.GetComponent("TitleScreenPresenter")); // 씬에 저장된 화면 연결
            string[] fields = new string[] // 필수 연결 목록
            { // 목록 시작
                "menuCamera", "viewport", "startButton", "settingsButton", "titleText", "startText", "settingsText", "noticePanel", "noticeText" // 필수 화면 참조
            }; // 목록 종료
            foreach (string field in fields) // 연결 항목 순회
            { // 확인 시작
                Assert.That(presenter.FindProperty(field).objectReferenceValue, Is.Not.Null, field); // 실제 직렬화 연결
            } // 확인 종료
            Assert.That(canvas.transform.Find("Viewport/Background").GetComponent("RawImage"), Is.Not.Null); // 배경 이미지 확인
            Assert.That(canvas.transform.Find("Viewport/Title").GetComponent("Text"), Is.Not.Null); // 제목 글자 확인
            Assert.That(canvas.transform.Find("Viewport/Start").GetComponent("Button"), Is.Not.Null); // 시작 버튼 확인
            Assert.That(canvas.transform.Find("Viewport/Settings").GetComponent("Button"), Is.Not.Null); // 설정 버튼 확인
            Assert.That(canvas.transform.Find("Viewport").GetComponent("RectMask2D"), Is.Not.Null); // 타이틀 화면 밖 표시 차단
        } // 확인 종료
        finally // 기존 장면 복원
        { // 복원 시작
            if (setup.Length > 0) // 복원할 장면 확인
            { // 복원 시작
                EditorSceneManager.RestoreSceneManagerSetup(setup); // 원래 장면 구성 복원
            } // 복원 종료
        } // 복원 종료
    } // 시험 종료
} // 시험 종료
