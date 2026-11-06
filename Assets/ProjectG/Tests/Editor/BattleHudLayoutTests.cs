using NUnit.Framework; // 시험 도구
using ProjectG; // 전투 화면 기능
using UnityEngine; // 유니티 화면 기능
public class BattleHudLayoutTests // 전투 정보 배치 시험
{ // 시험 시작
    [TestCase(1920f, 1080f)] // 일반 해상도
    [TestCase(3840f, 2160f)] // 고해상도
    public void ScaledTextAndFailureMessageHaveSeparateRows(float width, float height) // 글자와 실패 안내 분리
    { // 시험 시작
        Rect view = new Rect(0f, 0f, width, height); // 시험 화면 영역
        int font = Mathf.Max(14, Mathf.RoundToInt(height / 45f)); // 실제 화면 글자 크기
        Rect previous = new Rect(); // 이전 줄 영역
        for (int index = 0; index < 4; index++) // 체력과 실패 안내까지 확인
        { // 줄 확인 시작
            Rect current = BattleHud.LineRect(view, index); // 실제 줄 위치 계산
            Assert.That(current.height, Is.GreaterThanOrEqualTo(font)); // 글자 높이 확보
            Assert.That(current.xMin, Is.GreaterThanOrEqualTo(view.xMin)); // 화면 왼쪽 경계
            Assert.That(current.xMax, Is.LessThanOrEqualTo(view.xMax)); // 화면 오른쪽 경계
            Assert.That(current.yMax, Is.LessThanOrEqualTo(view.yMax)); // 화면 아래 경계
            if (index > 0) // 첫 줄 이후 확인
            { // 겹침 확인 시작
                Assert.That(current.yMin, Is.GreaterThanOrEqualTo(previous.yMax)); // 이전 줄과 분리
            } // 겹침 확인 종료
            previous = current; // 다음 비교용 영역
        } // 줄 확인 종료
    } // 시험 종료
} // 시험 종료
