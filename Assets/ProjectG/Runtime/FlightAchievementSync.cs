using System; // Steam 구현 없이 검증하는 연결 계약
using System.Collections.Generic; // 한 계정의 확인과 미확인 업로드

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightAchievementSync // 로컬 도전 조건과 선택한 계정의 동기화
    {
        private readonly FlightProgressStore progress; // 원자적으로 저장을 완료한 도전 조건
        private readonly string account; // 명시적으로 선택한 프로필 소유 계정
        private readonly string firstClear; // Steamworks 등록 뒤 매핑할 본편 성공 ID
        private readonly string firstTask; // Steamworks 등록 뒤 매핑할 첫 과제 ID
        private readonly Func<string> activeAccount; // 플랫폼이 현재 읽고 있는 계정
        private readonly Func<string, bool?> read; // 플랫폼 도전 상태 또는 아직 확인 불가
        private readonly Func<string, Action<bool>, bool> store; // 해금과 저장 요청 및 승인 콜백
        private readonly HashSet<string> confirmed = new HashSet<string>(StringComparer.Ordinal); // 승인받은 이번 계정의 원격 과제
        private readonly HashSet<string> attempted = new HashSet<string>(StringComparer.Ordinal); // 캐시에서 해금되었어도 저장 승인을 기다리는 과제
        private int sequence; // 로그아웃 또는 실패한 이전 콜백 무시
        private bool inFlight; // 게임 체크포인트의 요청 중복 방지

        internal FlightAchievementSync(FlightProgressStore progress, string account, string firstClear, string firstTask,
            Func<string> activeAccount, Func<string, bool?> read, Func<string, Action<bool>, bool> store) // 실제 프로젝트 확정 ID와 플랫폼 함수 연결
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress)); // 정상적인 로컬 저장 원본
            this.account = account; // 빈 값이면 온라인 요청 없음
            this.firstClear = firstClear; // 등록 후 지정한 첫 성공 맵
            this.firstTask = firstTask; // 등록 후 지정한 첫 과제 맵
            this.activeAccount = activeAccount ?? throw new ArgumentNullException(nameof(activeAccount)); // 현재 플랫폼 로그인 조건
            this.read = read ?? throw new ArgumentNullException(nameof(read)); // 상태 조회의 실제 결과
            this.store = store ?? throw new ArgumentNullException(nameof(store)); // 완료 콜백을 보내는 플랫폼 경로
        }

        internal void Pump() // 연결 복구나 출격 정산의 명시적 체크포인트
        {
            if (!HasAccount()) // 오프라인 또는 이 프로필과 다른 계정
            {
                sequence++; // 이동한 계정의 늦게 도착하는 응답 무효
                inFlight = false; // 같은 계정의 재접속에서 다음 요청 가능
                return; // 로컬 파일을 유지한 채 업로드 유예
            }

            if (inFlight) // 한 번에 하나의 실제 Steam 저장 콜백
            {
                return; // 중복 StoreStats 방지
            }

            if (progress.HasClearedStory && TrySync(firstClear)) // 먼저 저장된 본편 첫 성공
            {
                return; // 한 체크포인트에 한 요청
            }

            if (progress.HasAnyFirstTask) // 같은 프로필의 아무 조종사 첫 과제
            {
                TrySync(firstTask); // 아직 발급되지 않은 별도 과제
            }
        }

        private bool HasAccount() // 명시적으로 연결한 동일 계정만 허용
        {
            return !string.IsNullOrEmpty(account) && string.Equals(activeAccount(), account, StringComparison.Ordinal); // 다른 Steam 계정으로 진행이 이동하지 않음
        }

        private bool TrySync(string id) // 필요한 항목에만 플랫폼 요청
        {
            if (string.IsNullOrEmpty(id) || confirmed.Contains(id)) // 미등록 항목이나 이미 끝난 과제
            {
                return false; // 다음 유효한 과제 확인
            }

            bool? remote = read(id); // 플랫폼에서 확인할 수 있는 현재 해금 상태
            if (!remote.HasValue) // 아직 수신 전이거나 서버 접근 불가
            {
                return false; // 해당 항목은 나중에 재시도
            }

            if (remote.Value && !attempted.Contains(id)) // 첫 조회 때부터 이미 등록된 과제
            {
                confirmed.Add(id); // 중복 호출 없이 현재 완료로 기록
                return false; // 다른 대기 항목 처리 가능
            }

            attempted.Add(id); // 이후 로컬 플랫폼 캐시 해금을 저장 완료로 간주하지 않음
            int ticket = ++sequence; // 이번 신청의 세대 번호
            inFlight = true; // 저장 콜백이 오기 전까지 다음 업로드 유예
            if (!store(id, success => Complete(ticket, id, success)) && ticket == sequence && inFlight) // API 거절은 나중에 같은 과제를 시도
            {
                sequence++; // 이미 거절한 늦은 응답 무효
                inFlight = false; // 다음 명시적 체크포인트에서 재시도
            }

            return true; // 이 체크포인트의 원격 요청을 사용
        }

        private void Complete(int ticket, string id, bool success) // 실제 저장 결과가 도착한 메인 스레드 콜백
        {
            if (ticket != sequence || !inFlight) // 이미 끝났거나 다른 요청을 기다리는 이전 콜백
            {
                return; // 다른 과제 또는 재연결 상태 보호
            }

            inFlight = false; // 견적이나 로컬 캐시와 별도로 요청 종료
            if (success && HasAccount()) // 명시적 저장 확인과 같은 계정 유지
            {
                confirmed.Add(id); // 같은 계정에서 이미 완료한 과제
                attempted.Remove(id); // 로컬 캐시와 서버 상태가 이제 동일
            }
        }
    }
}
