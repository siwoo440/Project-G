using System; // 난수와 함수 기능
using NUnit.Framework; // 자동 시험 도구
using ProjectG; // 드롭 추첨 기능
public sealed class UpgradeDropSimulationTests // 재현 가능한 추첨 분포 시험
{ // 시험 시작
    [Test] // 초기 드롭 시험
    public void SeededTrialsMatchInitialDropAndGradeWeights() // 고정 시드의 전체 등급 비율
    { // 시험 시작
        const int trials = 100000; // 일반 적 추첨 횟수
        UpgradeDropRoller roller = new UpgradeDropRoller(new Random(151501)); // 재현 가능한 난수
        Func<UpgradeItemId, UpgradeItemGrade, bool> eligible = (id, grade) => id == UpgradeItemId.Damage; // 모든 등급의 단일 유효 종류
        int[] grades = new int[5]; // 등급별 드롭 합계
        int dropped = 0; // 전체 드롭 합계
        for (int index = 0; index < trials; index++) // 모든 추첨 순회
        { // 추첨 반복 시작
            if (roller.TryRoll(EnemyRank.Normal, eligible, out UpgradeDrop result)) // 일반 적 드롭 추첨
            { // 성공 집계 시작
                dropped++; // 드롭 횟수 증가
                grades[(int)result.Grade - 1]++; // 해당 등급 증가
            } // 성공 집계 종료
        } // 추첨 반복 종료
        double[] expected = new double[] // 기획서 초기 시험 비율
        { // 비율 목록 시작
            0.50, 0.25, 0.15, 0.07, 0.03 // 첫 등급부터 다섯 번째 등급
        }; // 비율 목록 종료
        Assert.That((double)dropped / trials, Is.InRange(0.095, 0.105)); // 초기 10퍼센트 표본 범위
        for (int index = 0; index < grades.Length; index++) // 등급별 관측 확인
        { // 확인 시작
            Assert.That((double)grades[index] / dropped, Is.InRange(expected[index] - 0.02, expected[index] + 0.02)); // 등급 비율 표본 범위
        } // 확인 종료
        TestContext.WriteLine("고정 시드 모의 추첨 " + trials + "회 / 드롭 " + dropped + "회 / I~V " + string.Join(", ", grades)); // 관찰 결과 기록
    } // 시험 종료
    [Test] // 빈 등급 재가중 시험
    public void SeededTrialsRedistributeOnlyAcrossEligibleGrades() // 유효 등급의 가중치 유지
    { // 시험 시작
        const int trials = 100000; // 일반 적 추첨 횟수
        UpgradeDropRoller roller = new UpgradeDropRoller(new Random(151502)); // 재현 가능한 난수
        Func<UpgradeItemId, UpgradeItemGrade, bool> eligible = (id, grade) => id == UpgradeItemId.Damage && (grade == UpgradeItemGrade.II || grade == UpgradeItemGrade.V); // 둘째와 다섯째 등급만 유효
        int second = 0; // 둘째 등급 합계
        int fifth = 0; // 다섯째 등급 합계
        for (int index = 0; index < trials; index++) // 모든 추첨 순회
        { // 추첨 반복 시작
            if (!roller.TryRoll(EnemyRank.Normal, eligible, out UpgradeDrop result)) // 일반 적 드롭 추첨
            { // 미드롭 시작
                continue; // 다음 추첨
            } // 미드롭 종료
            Assert.That(result.Grade == UpgradeItemGrade.II || result.Grade == UpgradeItemGrade.V, Is.True); // 제외된 등급 미출현
            if (result.Grade == UpgradeItemGrade.II) // 둘째 등급 확인
            { // 둘째 집계 시작
                second++; // 둘째 등급 증가
            } // 둘째 집계 종료
            else // 다섯째 등급
            { // 다섯째 집계 시작
                fifth++; // 다섯째 등급 증가
            } // 다섯째 집계 종료
        } // 추첨 반복 종료
        int dropped = second + fifth; // 유효 등급 드롭 합계
        Assert.That((double)dropped / trials, Is.InRange(0.095, 0.105)); // 초기 10퍼센트 표본 범위
        Assert.That((double)second / dropped, Is.InRange(25.0 / 28.0 - 0.02, 25.0 / 28.0 + 0.02)); // 둘째 등급 재가중 범위
        Assert.That((double)fifth / dropped, Is.InRange(3.0 / 28.0 - 0.02, 3.0 / 28.0 + 0.02)); // 다섯째 등급 재가중 범위
        TestContext.WriteLine("고정 시드 모의 추첨 " + trials + "회 / 드롭 " + dropped + "회 / II " + second + "회 / V " + fifth + "회"); // 관찰 결과 기록
    } // 시험 종료
} // 시험 클래스 종료
