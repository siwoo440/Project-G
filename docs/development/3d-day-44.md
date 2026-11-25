---
# 3D 44일차 | Steam 도전 조건과 재연결 검증

---
## 로컬 과제와 연동 경계

| 로컬 조건 | 기준 | Steam 등록 상태 |
| --- | --- | --- |
| 하늘섬 외곽 항로 첫 완료 | 최종 보스를 격파한 본편 영구 정산에 성공 | 과제 API 이름·아이콘·표시 이름 확정 필요 |
| 첫 과제 | 조종사 한 명 이상이 기존 첫 과제를 영구 정산 | 과제 API 이름·아이콘·표시 이름 확정 필요 |

- 실패·중단 복구는 이미 확정한 첫 본편 완료 조건을 유지한다. 이전 저장은 마지막 영수증에 성공이 남아 있으면 복원한다. 더 오래된 성공만 있었던 이전 저장은 기존 형식에서 확인할 수 없다.
- `FlightAchievementSync`는 저장한 조건과 연결한 계정이 일치할 때만 미승인 항목을 조회한다. Steam API 이름은 생성자에 확정값을 주는 구조다. 배포 환경에서 실제 Steam SDK 및 어댑터 연결이 필요하다.
- 어댑터는 연결 준비와 계정 변경을 확인한 뒤 주요 화면·재연결에서 `Pump`를 호출한다. `SetAchievement`와 `StoreStats`의 결과를 구분하고, 서버 저장 콜백 `UserStatsStored_t`가 돌아오는 시점에 메인 스레드에서 확정한다. 제출 거절·저장 실패·계정 변경으로 끊긴 요청은 다음 유효한 체크포인트에서 재시도한다. [Steamworks 도전 과제](https://partner.steamgames.com/doc/features/achievements) · [ISteamUserStats](https://partner.steamgames.com/doc/api/ISteamUserStats)

---
## Steam Cloud 준비

- 현재 Unity 프로젝트의 Windows 영구 성장 파일 경로는 `WinAppDataLocalLow/DefaultCompany/Project-G/Flight/progress-v1.json`이다. 등록 시 파일 마스크는 `progress-v1.json` 하나를 대상으로 검토한다. 디스크 복구용 .bak, 쓰기 중 .tmp, 배타 잠금 .lock은 제외한다.
- 현재 로컬 파일을 Steam 사용자 계정에 연결하는 절차는 추가로 필요하다. 등록 전에 동일 PC의 두 Steam 계정과 사용자별 파일 위치를 검증한다. 해상도·감도·키 설정과 PlayerPrefs의 탐험 임시 체크포인트·보스 기록은 이 파일의 범위 밖이다.
- App ID와 배포 SDK, 게시한 과제 API 이름 및 개발 전용 Cloud 환경이 확보되면 Steam 클라이언트에서 정상 시작·재접속과 두 PC 간 업로드·다운로드·충돌을 검증한다. 이번 검증은 오프라인의 실제 파일과 모의 콜백을 대상으로 한다. [Steam Auto-Cloud](https://partner.steamgames.com/doc/features/cloud)

---
## 검증

Unity PlayMode 90개 통과. 실제 임시 프로필을 닫고 다시 연 뒤, 모의 SDK에서 오프라인, 다른 계정, 캐시에만 남은 해금, 저장 실패, 지연 응답과 재접속을 재현했다. 승인받은 항목은 중복 요청하지 않았고 천공 결정·골드는 로컬 정산과 같았다. 실제 Steam 서버 전송과 PC 간 Cloud 복원은 확인 대기다.

---
## 다음 일차

Unity 프로젝트의 파일·씬 참조를 점검하고 같은 탄막 장면을 비교해 할당과 프레임 비용을 줄인다.
