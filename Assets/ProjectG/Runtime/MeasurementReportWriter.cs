using System; // 고유 식별자와 오류 처리
using System.Globalization; // 일정한 파일 이름 형식
using System.IO; // 보고서 파일 입출력
using System.Text; // 엄격한 UTF-8 인코딩
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public static class MeasurementReportWriter // 측정 보고서의 안전한 파일 저장
    { // 저장 기능 시작
        public static bool TryWrite(string directory, string report, out string path, out string error) // 지정 폴더의 새 보고서 저장
        { // 저장 시작
            path = null; // 성공 전 경로 비움
            error = null; // 이전 오류 비움
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(report)) // 필수 입력 확인
            { // 입력 오류 시작
                error = "보고서 폴더와 내용이 필요합니다"; // 빈 입력 오류 안내
                return false; // 파일 생성 차단
            } // 입력 오류 종료
            string candidate = null; // 새 보고서 후보 경로
            bool created = false; // 현재 호출에서 만든 파일 여부
            try // 파일 저장 시도
            { // 저장 범위 시작
                string folder = Path.GetFullPath(directory); // 지정 폴더의 절대 경로
                Directory.CreateDirectory(folder); // 보고서 폴더 준비
                string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture); // 저장 시각의 UTC 표시
                string name = "projectg-measurement-" + stamp + "-" + Guid.NewGuid().ToString("N") + ".txt"; // 충돌 방지 파일 이름
                candidate = Path.Combine(folder, name); // 지정 폴더의 새 파일 경로
                using (FileStream stream = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // 기존 파일 교체 없는 생성
                { // 파일 작성 시작
                    created = true; // 현재 호출의 새 파일 기록
                    using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false, true), 1024, true)) // 엄격한 UTF-8 텍스트 작성
                    { // 문자 작성 시작
                        writer.Write(report); // 전달된 보고서 원문 저장
                        writer.Flush(); // 문자 버퍼 기록
                        stream.Flush(true); // 파일 버퍼 저장 완료
                    } // 문자 작성 종료
                } // 파일 작성 종료
                path = candidate; // 저장 성공 경로 반환
                return true; // 보고서 저장 성공
            } // 저장 범위 종료
            catch (Exception exception) // 입력 경로 또는 파일 저장 실패
            { // 오류 처리 시작
                error = "측정 보고서를 저장하지 못했습니다: " + exception.Message; // 저장 실패 원인 안내
                if (created && candidate != null) // 이 호출이 만든 미완성 파일 확인
                { // 미완성 파일 처리 시작
                    try // 미완성 파일 제거 시도
                    { // 제거 범위 시작
                        File.Delete(candidate); // 현재 호출이 만든 파일만 제거
                    } // 제거 범위 종료
                    catch (Exception cleanupError) // 미완성 파일 정리 실패
                    { // 추가 오류 시작
                        path = candidate; // 남은 미완성 파일 경로 반환
                        error += " / 미완성 파일 정리 실패: " + cleanupError.Message; // 정리 실패 원인 안내
                    } // 추가 오류 종료
                } // 미완성 파일 처리 종료
                return false; // 성공 처리 방지
            } // 오류 처리 종료
        } // 저장 종료
    } // 저장 기능 종료
} // 영역 종료