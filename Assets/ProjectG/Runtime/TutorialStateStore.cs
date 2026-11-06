using System; // 저장 실패 처리
using UnityEngine; // 설정 저장 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public enum TutorialSaveState // 안내 저장 상태
    { // 목록 시작
        Unseen, Started, Finished // 미표시와 진행 및 종료
    } // 목록 종료
    public interface ITutorialStateStore // 안내 상태 저장 계약
    { // 계약 시작
        bool TryRead(out TutorialSaveState state); // 상태 읽기 성공 여부
        bool TryWrite(TutorialSaveState state); // 상태 저장 성공 여부
    } // 계약 종료
    public sealed class MemoryTutorialStateStore : ITutorialStateStore // 격리 시험 저장소
    { // 저장소 시작
        private TutorialSaveState state; // 메모리 상태
        public bool TryRead(out TutorialSaveState value) // 격리 상태 조회
        { // 조회 시작
            value = state; // 현재 상태 전달
            return true; // 조회 성공
        } // 조회 종료
        public bool TryWrite(TutorialSaveState value) // 격리 상태 기록
        { // 기록 시작
            state = value; // 상태 저장
            return true; // 기록 성공
        } // 기록 종료
    } // 저장소 종료
    public sealed class PlayerPrefsTutorialStateStore : ITutorialStateStore // 전투 성장과 분리한 안내 저장소
    { // 저장소 시작
        private const string Key = "ProjectG.Tutorial.State.v1"; // 안내 전용 저장 키
        public bool TryRead(out TutorialSaveState state) // 안내 상태 읽기
        { // 조회 시작
            state = TutorialSaveState.Unseen; // 기본 상태 준비
            try // 저장 조회 범위
            { // 조회 범위 시작
                int value = PlayerPrefs.GetInt(Key, 0); // 별도 저장 값
                if (value < 0 || value > (int)TutorialSaveState.Finished) // 알 수 없는 값 확인
                { // 오류 시작
                    return false; // 기존 설정 보존
                } // 오류 종료
                state = (TutorialSaveState)value; // 확인된 상태 전달
                return true; // 조회 성공
            } // 조회 범위 종료
            catch (Exception) // 저장 장치 오류
            { // 오류 시작
                return false; // 전투를 막지 않는 실패
            } // 오류 종료
        } // 조회 종료
        public bool TryWrite(TutorialSaveState state) // 안내 상태 저장
        { // 저장 시작
            try // 저장 범위
            { // 저장 범위 시작
                PlayerPrefs.SetInt(Key, (int)state); // 전용 키 변경
                PlayerPrefs.Save(); // 설정 기록
                return true; // 저장 성공
            } // 저장 범위 종료
            catch (Exception) // 저장 장치 오류
            { // 오류 시작
                return false; // 전투를 막지 않는 실패
            } // 오류 종료
        } // 저장 종료
    } // 저장소 종료
} // 영역 종료
