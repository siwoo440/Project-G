using System.Reflection; // 명시적 연결 API 조회
using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 적과 스테이지 기능
using UnityEditor; // 기존 연결 확인
using UnityEngine; // 유니티 객체 기능
using UnityEngine.TestTools; // 연결 오류 확인
public class StageSceneTemplateConfigurationTests // 스테이지 템플릿 연결 시험
{ // 시험 시작
    private static readonly string[] Names = // 종류별 프리팹 이름
    { // 배열 시작
        "Aimed", "Spread", "Dash", "MidBoss", "FinalBoss" // 배열 항목
    }; // 배열 종료
    private static readonly string[] Fields = // 종류별 연결 필드
    { // 배열 시작
        "aimedTemplate", "spreadTemplate", "dashTemplate", "midBossTemplate", "finalBossTemplate" // 배열 항목
    }; // 배열 종료
    private static EnemyActor[] MakeTemplates(Transform owner) // 비활성 직접 자식 준비
    { // 준비 시작
        EnemyActor[] result = new EnemyActor[Names.Length]; // 다섯 종류 목록
        for (int index = 0; index < Names.Length; index++) // 종류별 순회
        { // 순회 시작
            EnemyActor prefab = Resources.Load<EnemyActor>("EnemyTemplates/" + Names[index]); // 실제 종류별 프리팹
            Assert.That(prefab, Is.Not.Null); // 필수 자산 확인
            result[index] = Object.Instantiate(prefab, owner, false); // 비활성 직접 자식 생성
        } // 순회 종료
        return result; // 템플릿 목록 반환
    } // 준비 종료
    private static bool Configure(StageDirector stage, EnemyActor[] templates) // 명시적 연결 호출
    { // 연결 시작
        MethodInfo method = typeof(StageDirector).GetMethod("SetSceneTemplates"); // 명시적 연결 API 확인
        Assert.That(method, Is.Not.Null); // 연결 API 존재 확인
        return (bool)method.Invoke(stage, new object[] // 종류별 연결 결과
        { // 배열 시작
            templates[0], templates[1], templates[2], templates[3], templates[4] // 배열 항목
        }); // 배열 종료
    } // 연결 종료
    [Test] // 유효 템플릿 연결 시험
    public void SetSceneTemplatesAcceptsFiveDistinctDirectChildren() // 종류별 씬 참조 연결
    { // 시험 시작
        GameObject holder = new GameObject("Stage"); // 스테이지 객체
        try // 시험 범위
        { // 범위 시작
            StageDirector stage = holder.AddComponent<StageDirector>(); // 실제 스테이지
            EnemyActor[] templates = MakeTemplates(holder.transform); // 다섯 종류 템플릿
            Assert.That(Configure(stage, templates), Is.True); // 유효 연결 성공
            Assert.That(stage.HasSceneTemplates, Is.True); // 실제 연결 상태 확인
            SerializedObject fields = new SerializedObject(stage); // 저장된 필드 확인
            for (int index = 0; index < Fields.Length; index++) // 종류별 연결 검사
            { // 검사 시작
                Assert.That(fields.FindProperty(Fields[index]).objectReferenceValue, Is.SameAs(templates[index])); // 대응 템플릿 보관
            } // 검사 종료
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 스테이지와 템플릿 제거
        } // 정리 종료
    } // 시험 종료
    [TestCase("missing")] // 누락 템플릿
    [TestCase("duplicate")] // 중복 템플릿
    [TestCase("active")] // 활성 템플릿
    [TestCase("nested")] // 직접 자식이 아닌 템플릿
    [TestCase("wrong_dash")] // 돌진 표시 누락
    public void InvalidReplacementLeavesAllExistingReferencesUntouched(string invalid) // 잘못된 연결의 원자적 거부
    { // 시험 시작
        GameObject holder = new GameObject("Stage"); // 스테이지 객체
        try // 시험 범위
        { // 범위 시작
            StageDirector stage = holder.AddComponent<StageDirector>(); // 실제 스테이지
            EnemyActor[] original = MakeTemplates(holder.transform); // 기존 유효 템플릿
            Assert.That(Configure(stage, original), Is.True); // 최초 연결 성공
            EnemyActor[] replacement = MakeTemplates(holder.transform); // 새 유효 템플릿
            if (invalid == "missing") // 누락 조건 확인
            { // 누락 설정 시작
                replacement[0] = null; // 필수 참조 누락
            } // 누락 설정 종료
            else if (invalid == "duplicate") // 중복 조건 확인
            { // 중복 설정 시작
                replacement[1] = replacement[0]; // 다른 종류에 같은 객체 연결
            } // 중복 설정 종료
            else if (invalid == "active") // 활성 조건 확인
            { // 활성 설정 시작
                replacement[0].gameObject.SetActive(true); // 템플릿 활성화
            } // 활성 설정 종료
            else if (invalid == "nested") // 중간 부모 조건 확인
            { // 부모 변경 시작
                GameObject nested = new GameObject("Nested"); // 중간 부모 객체
                nested.transform.SetParent(holder.transform, false); // 스테이지 아래 배치
                replacement[0].transform.SetParent(nested.transform, false); // 직접 자식 조건 위반
            } // 부모 변경 종료
            else // 돌진 필수 표시 누락
            { // 돌진 변경 시작
                replacement[2] = Object.Instantiate(Resources.Load<EnemyActor>("EnemyTemplates/Aimed"), holder.transform, false); // 돌진 경고 없는 별도 조준형 전달
            } // 돌진 변경 종료
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("적.*템플릿")); // 명시적 설정 오류
            Assert.That(Configure(stage, replacement), Is.False); // 잘못된 연결 거부
            Assert.That(stage.HasSceneTemplates, Is.True); // 기존 유효 구성 유지
            SerializedObject fields = new SerializedObject(stage); // 현재 연결 조회
            for (int index = 0; index < Fields.Length; index++) // 기존 연결 순회
            { // 확인 시작
                Assert.That(fields.FindProperty(Fields[index]).objectReferenceValue, Is.SameAs(original[index])); // 기존 연결 원자적 보존
            } // 확인 종료
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(holder); // 스테이지와 템플릿 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
