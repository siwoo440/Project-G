using NUnit.Framework; // 시험 도구
using UnityEditor; // 글꼴 자산 확인
using UnityEditor.SceneManagement; // 씬 열기 기능
using UnityEngine; // 유니티 화면 기능
public class TitleFontSceneTests // 타이틀 에디터 글꼴 시험
{ // 시험 시작
    [Test] // 씬 글꼴 직렬화 시험
    public void SceneTextHasSavedKoreanFontBeforePlay() // 플레이 전 저장 글꼴 확인
    { // 시험 시작
        var setup = EditorSceneManager.GetSceneManagerSetup(); // 기존 씬 상태
        try // 시험 준비
        { // 준비 시작
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single); // 실제 타이틀 씬
            GameObject canvas = null; // 타이틀 객체
            foreach (GameObject root in scene.GetRootGameObjects()) // 씬 루트 확인
            { // 순회 시작
                if (root.name == "TitleCanvas") // 타이틀 이름 확인
                { // 보관 시작
                    canvas = root; // 타이틀 연결
                } // 보관 종료
            } // 순회 종료
            Assert.That(canvas, Is.Not.Null); // 저장된 캔버스 확인
            string[] paths = new string[] // 글꼴을 사용하는 씬 글자
            { // 목록 시작
                "Viewport/Title", // 타이틀 제목
                "Viewport/Start/Text", // 시작 버튼 글자
                "Viewport/Settings/Text", // 설정 버튼 글자
                "Viewport/Notice/Text" // 측정 저장 안내
            }; // 목록 종료
            foreach (string path in paths) // 글자 순회
            { // 확인 시작
                Component text = canvas.transform.Find(path).GetComponent("Text"); // 실제 글자 구성
                Font font = (Font)text.GetType().GetProperty("font").GetValue(text); // 직렬화된 글꼴
                Assert.That(font, Is.Not.Null, path); // 플레이 전 글꼴 존재
                Assert.That(AssetDatabase.Contains(font), Is.True, path); // 저장 가능한 프로젝트 글꼴
            } // 확인 종료
        } // 시험 범위 종료
        finally // 씬 상태 복원
        { // 복원 시작
            if (setup.Length > 0) // 이전 씬 확인
            { // 복원 범위
                EditorSceneManager.RestoreSceneManagerSetup(setup); // 이전 씬 복원
            } // 복원 종료
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
