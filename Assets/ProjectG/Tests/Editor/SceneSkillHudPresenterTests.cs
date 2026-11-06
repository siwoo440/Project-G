using System.Reflection; // 직렬화 필드 주입
using NUnit.Framework; // 단위 시험 기능
using ProjectG; // 실제 전투 UI 기능
using UnityEngine; // 유니티 객체 기능
using UnityEngine.UI; // 실제 UI 요소
public class SceneSkillHudPresenterTests // 편집 가능한 스킬 슬롯 시험
{ // 시험 시작
    private static void Assign(object target, string field, object value) // 직렬화 참조 주입
    { // 주입 시작
        FieldInfo info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance); // 실제 필드 조회
        Assert.That(info, Is.Not.Null, field); // 필드 존재 확인
        info.SetValue(target, value); // 실제 참조 주입
    } // 주입 종료
    private static GameObject Child(GameObject root, string name) // 격리된 실제 UI 객체
    { // 생성 시작
        GameObject child = new GameObject(name, typeof(RectTransform)); // 실제 UI 객체 생성
        child.transform.SetParent(root.transform, false); // 격리 부모에 연결
        return child; // 실제 객체 반환
    } // 생성 종료
    [Test] // 키·자원·쿨타임과 편집 보존 시험
    public void RealSkillsUpdateOnlyContentAndAvailability() // 실제 상태와 편집 속성 확인
    { // 시험 시작
        GameObject root = new GameObject("SkillPanel", typeof(RectTransform)); // 격리 스킬 패널
        GameObject playerObject = new GameObject("Player"); // 격리 실제 플레이어
        Texture2D texture = new Texture2D(4, 4); // 격리 시험 이미지
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f, 4f); // 격리 시험 스프라이트
        try // 시험 범위
        { // 범위 시작
            SceneSkillHudPresenter view = root.AddComponent<SceneSkillHudPresenter>(); // 실제 씬 표시 구성
            string[] buttons = new string[] // 실제 버튼 필드
            { // 목록 시작
                "uniqueSlot", "commonSlot", "ultimateSlot" // 세 스킬 슬롯
            }; // 목록 종료
            foreach (string field in buttons) // 세 슬롯 순회
            { // 연결 시작
                Assign(view, field, Child(root, field).AddComponent<Button>()); // 실제 버튼 참조 주입
            } // 연결 종료
            string[] iconNames = new string[] // 실제 아이콘 필드
            { // 목록 시작
                "rapidIcon", "sniperIcon", "shieldIcon", "scatterIcon", "ultimateIcon" // 캐릭터와 장착별 아이콘
            }; // 목록 종료
            Image first = null; // 편집 대상 아이콘
            foreach (string field in iconNames) // 아이콘 순회
            { // 연결 시작
                Image icon = Child(root, field).AddComponent<Image>(); // 편집 가능한 아이콘
                icon.sprite = sprite; // 격리 Sprite 연결
                Assign(view, field, icon); // 실제 아이콘 주입
                if (first == null) // 첫 아이콘 확인
                { // 보관 시작
                    first = icon; // 편집 대상 보관
                } // 보관 종료
            } // 연결 종료
            string[] toggles = new string[] // 실제 상태 표시 필드
            { // 목록 시작
                "uniqueShade", "commonShade", "ultimateShade", "uniqueActive", "commonActive" // 비활성 음영과 활성 표시
            }; // 목록 종료
            foreach (string field in toggles) // 상태 표시 순회
            { // 연결 시작
                Assign(view, field, Child(root, field)); // 편집 가능한 표시 객체
            } // 연결 종료
            string[] texts = new string[] // 실제 텍스트 필드
            { // 목록 시작
                "uniqueCooldown", "commonCooldown", "ultimateCooldown", "uniqueKey", "commonKey", "ultimateKey" // 쿨타임과 조작 키
            }; // 목록 종료
            foreach (string field in texts) // 글자 순회
            { // 연결 시작
                Text label = Child(root, field).AddComponent<Text>(); // 편집 가능한 실제 글자
                label.color = Color.white; // 기본 흰 글자
                Assign(view, field, label); // 실제 글자 참조
            } // 연결 종료
            Assert.That(view.IsReady, Is.True); // 모든 실제 참조 확인
            BattlePlayer player = playerObject.AddComponent<BattlePlayer>(); // 실제 전투 플레이어
            player.Begin(Vector2.zero, CharacterType.Rapid); // 아리아 시작
            KeyBindingSettings bindings = new KeyBindingSettings(); // 독립 키 설정
            Assert.That(bindings.TryBind(PlayerAction.Unique, "u"), Is.True); // 실제 고유기 키 변경
            player.SetBindings(bindings); // 공유 재지정 키 연결
            Color edited = new Color(0.25f, 0.6f, 0.8f, 0.75f); // 편집 색상
            first.color = edited; // 사용자 아이콘 색상
            first.rectTransform.anchoredPosition = new Vector2(17f, 23f); // 사용자 아이콘 위치
            first.rectTransform.sizeDelta = new Vector2(93f, 81f); // 사용자 아이콘 크기
            view.Present(player); // 실제 스킬 상태 표시
            Assert.That(root.transform.Find("rapidIcon").gameObject.activeSelf, Is.True); // 아리아 아이콘 표시
            Assert.That(root.transform.Find("sniperIcon").gameObject.activeSelf, Is.False); // 리안 아이콘 숨김
            Assert.That(root.transform.Find("shieldIcon").gameObject.activeSelf, Is.True); // 보호막 아이콘 표시
            Assert.That(root.transform.Find("ultimateShade").gameObject.activeSelf, Is.True); // 에너지 부족 음영
            Assert.That(root.transform.Find("uniqueKey").GetComponent<Text>().text, Is.EqualTo("U")); // 실제 재지정 키 표시
            Assert.That(player.TryUseRapid(), Is.True); // 실제 고유기 성공
            Assert.That(player.TryUseCommon(), Is.True); // 실제 공용기 성공
            view.Present(player); // 실제 활성 상태 반영
            Assert.That(root.transform.Find("uniqueShade").gameObject.activeSelf, Is.True); // 활성 중 재사용 불가 음영
            player.Skills.Advance(3f); // 활성 종료와 쿨타임 시작
            view.Present(player); // 실제 쿨타임 반영
            Assert.That(root.transform.Find("uniqueShade").gameObject.activeSelf, Is.True); // 고유기 쿨타임 음영
            Assert.That(root.transform.Find("commonShade").gameObject.activeSelf, Is.True); // 공용기 쿨타임 음영
            Assert.That(root.transform.Find("uniqueCooldown").GetComponent<Text>().text, Is.EqualTo(SkillHudLayout.Countdown(player.Skills.RapidCooldown))); // 실제 흰 쿨타임 숫자
            Assert.That(root.transform.Find("uniqueCooldown").GetComponent<Text>().color, Is.EqualTo(Color.white)); // 기본 흰 숫자 유지
            player.Skills.GainEnergy(100); // 실제 필살기 에너지 충전
            view.Present(player); // 충전 상태 표시
            Assert.That(root.transform.Find("ultimateShade").gameObject.activeSelf, Is.False); // 충전 후 음영 해제
            Assert.That(first.color, Is.EqualTo(edited)); // 사용자 색상 보존
            Assert.That(first.sprite, Is.SameAs(sprite)); // 사용자 이미지 보존
            Assert.That(first.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(17f, 23f))); // 사용자 위치 보존
            Assert.That(first.rectTransform.sizeDelta, Is.EqualTo(new Vector2(93f, 81f))); // 사용자 크기 보존
            player.Begin(Vector2.zero, CharacterType.Sniper); // 리안 출격 초기화
            player.Skills.Equip(CommonSkill.Scatter); // 산탄 스킬 장착
            view.Present(player); // 실제 캐릭터 변경 표시
            Assert.That(root.transform.Find("sniperIcon").gameObject.activeSelf, Is.True); // 리안 아이콘 표시
            Assert.That(root.transform.Find("scatterIcon").gameObject.activeSelf, Is.True); // 산탄 아이콘 표시
            Assert.That(root.transform.Find("rapidIcon").gameObject.activeSelf, Is.False); // 이전 아이콘 숨김
        } // 범위 종료
        finally // 시험 정리
        { // 정리 시작
            Object.DestroyImmediate(playerObject); // 격리 플레이어 제거
            Object.DestroyImmediate(root); // 격리 UI 제거
            Object.DestroyImmediate(sprite); // 시험 스프라이트 제거
            Object.DestroyImmediate(texture); // 시험 이미지 제거
        } // 정리 종료
    } // 시험 종료
} // 시험 종료
