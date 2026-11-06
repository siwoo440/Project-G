using System.Reflection; // 시험 전용 참조 연결
using NUnit.Framework; // 에셋 확인
using ProjectG; // 실제 적과 스테이지
using UnityEngine; // 유니티 객체 기능
public static class EnemyActorTestFactory // 기존 시험의 명시 템플릿 연결
{ // 도우미 시작
    private static readonly string[] Names = // 종류별 프리팹 이름
    { // 배열 시작
        "Aimed", "Spread", "Dash", "MidBoss", "FinalBoss" // 배열 항목
    }; // 배열 종료
    private static readonly string[] Fields = // 스테이지 연결 필드
    { // 배열 시작
        "aimedTemplate", "spreadTemplate", "dashTemplate", "midBossTemplate", "finalBossTemplate" // 배열 항목
    }; // 배열 종료
    private static readonly string[] VisualFields = // 편집 표시 참조
    { // 배열 시작
        "bodyImage", "healthBar", "weakMark", "warningMark", "dashPath" // 배열 항목
    }; // 배열 종료
    private static FieldInfo Field(System.Type type, string name) // 시험용 필드 조회
    { // 조회 시작
        FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic); // 비공개 필드 확인
        Assert.That(field, Is.Not.Null); // 시험 대상 필드 확인
        return field; // 실제 필드 반환
    } // 조회 종료
    private static EnemyActor Load(EnemyKind kind) // 종류별 명시 자산 조회
    { // 조회 시작
        int index = (int)kind; // 종류 인덱스
        Assert.That(index, Is.InRange(0, Names.Length - 1)); // 유효 종류 확인
        EnemyActor asset = Resources.Load<EnemyActor>("EnemyTemplates/" + Names[index]); // 실제 프리팹 조회
        Assert.That(asset, Is.Not.Null); // 필수 에셋 누락 확인
        Assert.That(asset.HasSceneVisual(kind), Is.True); // 실제 표시 참조 확인
        return asset; // 실제 프리팹 반환
    } // 조회 종료
    public static EnemyActor AttachActor(GameObject holder, EnemyKind kind) // 기존 시험 객체에 표시 연결
    { // 연결 시작
        Assert.That(holder.GetComponent<EnemyActor>(), Is.Null); // 기존 구성 중복 확인
        EnemyActor template = Object.Instantiate(Load(kind)); // 비활성 프리팹 복제
        EnemyActor target = holder.AddComponent<EnemyActor>(); // 기존 시험 루트 유지
        foreach (string name in VisualFields) // 표시 참조 순회
        { // 순회 시작
            FieldInfo field = Field(typeof(EnemyActor), name); // 실제 참조 필드
            field.SetValue(target, field.GetValue(template)); // 복제된 표시 참조 전달
        } // 순회 종료
        while (template.transform.childCount > 0) // 복제된 표시 순회
        { // 이동 시작
            template.transform.GetChild(0).SetParent(holder.transform, false); // 실제 시험 루트로 연결
        } // 이동 종료
        if (Application.isPlaying) // 플레이 시험 확인
        { // 지연 제거 시작
            Object.Destroy(template.gameObject); // 비활성 임시 루트 제거
        } // 지연 제거 종료
        else // 편집 시험 확인
        { // 즉시 제거 시작
            Object.DestroyImmediate(template.gameObject); // 임시 루트 제거
        } // 즉시 제거 종료
        Assert.That(target.HasSceneVisual(kind), Is.True); // 실제 시험 참조 검증
        return target; // 연결된 적 반환
    } // 연결 종료
    public static StageDirector AttachStage(GameObject holder) // 기존 시험 스테이지에 템플릿 연결
    { // 연결 시작
        StageDirector stage = holder.AddComponent<StageDirector>(); // 실제 스테이지 생성
        for (int index = 0; index < Names.Length; index++) // 다섯 종류 순회
        { // 순회 시작
            EnemyActor template = Object.Instantiate(Load((EnemyKind)index), holder.transform, false); // 비활성 직접 자식 템플릿
            Field(typeof(StageDirector), Fields[index]).SetValue(stage, template); // 종류별 실제 참조 연결
        } // 순회 종료
        Assert.That(stage.HasSceneTemplates, Is.True); // 실제 스테이지 연결 검증
        return stage; // 연결된 스테이지 반환
    } // 연결 종료
} // 도우미 종료
