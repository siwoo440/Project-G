using System; // 고유 시험 경로
using System.IO; // 격리된 파일 검사
using NUnit.Framework; // 시험 도구
using ProjectG; // 측정 보고서 저장
public class MeasurementReportWriterTests // 보고서 저장 시험
{ // 시험 시작
    [Test] // 고유 파일과 기존 데이터 시험
    public void WritesDistinctUtf8ReportsWithoutReplacingExistingFile() // 별도 저장과 기존 파일 보존 확인
    { // 시험 시작
        string directory = Path.Combine(Path.GetTempPath(), "projectg-report-test-" + Guid.NewGuid().ToString("N")); // 고유한 시험 폴더
        string existing = Path.Combine(directory, "existing.txt"); // 기존 시험 파일
        string first = null; // 첫 보고서 경로
        string second = null; // 둘째 보고서 경로
        try // 시험 범위 시작
        { // 시험 시작
            Directory.CreateDirectory(directory); // 시험 전용 폴더 생성
            File.WriteAllText(existing, "기존 기록"); // 보존할 원본 생성
            Assert.That(MeasurementReportWriter.TryWrite(directory, "첫 측정 기록", out first, out string firstError), Is.True, firstError); // 첫 보고서 저장
            Assert.That(MeasurementReportWriter.TryWrite(directory, "둘째 측정 기록", out second, out string secondError), Is.True, secondError); // 둘째 보고서 저장
            Assert.That(first, Is.Not.EqualTo(second)); // 서로 다른 파일 이름
            Assert.That(Path.GetDirectoryName(first), Is.EqualTo(Path.GetFullPath(directory))); // 첫 보고서 위치
            Assert.That(Path.GetDirectoryName(second), Is.EqualTo(Path.GetFullPath(directory))); // 둘째 보고서 위치
            Assert.That(File.ReadAllText(first), Is.EqualTo("첫 측정 기록")); // 첫 한글 저장 내용
            Assert.That(File.ReadAllText(second), Is.EqualTo("둘째 측정 기록")); // 둘째 한글 저장 내용
            Assert.That(File.ReadAllText(existing), Is.EqualTo("기존 기록")); // 기존 파일 보존
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (first != null && string.Equals(Path.GetDirectoryName(first), Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase) && File.Exists(first)) // 첫 시험 파일 위치 확인
            { // 삭제 시작
                File.Delete(first); // 첫 시험 파일 제거
            } // 삭제 종료
            if (second != null && string.Equals(Path.GetDirectoryName(second), Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase) && File.Exists(second)) // 둘째 시험 파일 위치 확인
            { // 삭제 시작
                File.Delete(second); // 둘째 시험 파일 제거
            } // 삭제 종료
            if (File.Exists(existing)) // 기존 시험 파일 확인
            { // 삭제 시작
                File.Delete(existing); // 해당 시험 파일 제거
            } // 삭제 종료
            if (Directory.Exists(directory)) // 시험 폴더 확인
            { // 삭제 시작
                Directory.Delete(directory, false); // 비어 있는 시험 폴더만 제거
            } // 삭제 종료
        } // 정리 종료
    } // 시험 종료
    [Test] // 빈 경로와 내용 시험
    public void EmptyDirectoryOrReportIsRejectedWithoutAPath() // 빈 입력 거부 확인
    { // 시험 시작
        Assert.That(MeasurementReportWriter.TryWrite(null, "기록", out string first, out string firstError), Is.False); // 빈 폴더 거부
        Assert.That(first, Is.Null); // 빈 폴더에 출력 경로 없음
        Assert.That(firstError, Is.Not.Null.And.Not.Empty); // 빈 폴더 오류 설명
        Assert.That(MeasurementReportWriter.TryWrite(" ", "기록", out string second, out string secondError), Is.False); // 공백 폴더 거부
        Assert.That(second, Is.Null); // 공백 폴더에 출력 경로 없음
        Assert.That(secondError, Is.Not.Null.And.Not.Empty); // 공백 폴더 오류 설명
        Assert.That(MeasurementReportWriter.TryWrite(Path.GetTempPath(), " \r\n", out string third, out string thirdError), Is.False); // 빈 보고서 거부
        Assert.That(third, Is.Null); // 빈 보고서 출력 경로 없음
        Assert.That(thirdError, Is.Not.Null.And.Not.Empty); // 빈 보고서 오류 설명
    } // 시험 종료
    [Test] // 저장 실패와 원본 보존 시험
    public void FileUsedAsDirectoryReportsFailureWithoutChangingIt() // 잘못된 대상 오류 확인
    { // 시험 시작
        string blocker = Path.Combine(Path.GetTempPath(), "projectg-report-blocker-" + Guid.NewGuid().ToString("N")); // 고유한 시험 파일
        try // 시험 범위 시작
        { // 시험 시작
            File.WriteAllText(blocker, "원본 기록"); // 파일로 막힌 폴더 경로
            Assert.That(MeasurementReportWriter.TryWrite(blocker, "새 기록", out string path, out string error), Is.False); // 저장 실패 확인
            Assert.That(path, Is.Null); // 성공 경로 반환 방지
            Assert.That(error, Is.Not.Null.And.Not.Empty); // 실제 오류 안내
            Assert.That(File.ReadAllText(blocker), Is.EqualTo("원본 기록")); // 기존 파일 보존
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (File.Exists(blocker)) // 해당 시험 파일 확인
            { // 삭제 시작
                File.Delete(blocker); // 해당 시험 파일 제거
            } // 삭제 종료
        } // 정리 종료
    } // 시험 종료
    [Test] // 잘못된 텍스트와 미완성 파일 시험
    public void InvalidUnicodeDoesNotLeaveIncompleteReport() // 인코딩 실패 정리 확인
    { // 시험 시작
        string directory = Path.Combine(Path.GetTempPath(), "projectg-report-unicode-" + Guid.NewGuid().ToString("N")); // 고유한 시험 폴더
        try // 시험 범위 시작
        { // 시험 시작
            Directory.CreateDirectory(directory); // 격리된 시험 폴더 생성
            Assert.That(MeasurementReportWriter.TryWrite(directory, "\uD800", out string path, out string error), Is.False); // 잘못된 문자 거부
            Assert.That(path, Is.Null); // 미완성 파일 경로 없음
            Assert.That(error, Is.Not.Null.And.Not.Empty); // 인코딩 오류 안내
            Assert.That(Directory.GetFiles(directory).Length, Is.Zero); // 미완성 보고서 제거
        } // 시험 범위 종료
        finally // 시험 정리
        { // 정리 시작
            if (Directory.Exists(directory)) // 해당 시험 폴더 확인
            { // 삭제 시작
                Directory.Delete(directory, false); // 비어 있는 시험 폴더만 제거
            } // 삭제 종료
        } // 정리 종료
    } // 시험 종료
} // 시험 종료