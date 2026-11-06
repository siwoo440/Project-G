from pathlib import Path  # 경로 처리
import json  # 결과 기록
import hashlib  # 시험 소스 무결성 확인
import os  # 자식 환경 복사
import re  # 격리 설정 수정
import shutil  # 프로젝트 복사
import stat  # 연결점 속성 확인
import subprocess  # 자식 프로세스 실행
import tempfile  # 고유 시험 폴더
import time  # 제한 시간 확인
import uuid  # 격리 식별자 생성

source = Path(__file__).resolve().parent.parent  # 이 스크립트가 속한 프로젝트
helper = Path(__file__).resolve().with_name("day17-recovery-probe-helper.cs")  # 격리 프로젝트용 시험 코드
unity = Path(r"C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe")  # 지정 Unity 실행 파일
if not unity.is_file():  # 지정 에디터 설치 확인
    raise FileNotFoundError("Unity Editor 6000.3.21f1을 찾을 수 없습니다: " + str(unity))  # 다른 버전 자동 대체 차단
if not helper.is_file():  # 격리 헬퍼 존재 확인
    raise FileNotFoundError("격리 시험 헬퍼를 찾을 수 없습니다: " + str(helper))  # 불완전한 실행 차단
for name in ("Assets", "Packages", "ProjectSettings"):  # 프로젝트 필수 폴더 확인
    folder = source / name  # 원본 폴더 경로
    if not folder.is_dir():  # 폴더 누락 확인
        raise FileNotFoundError("Unity 프로젝트 폴더를 찾을 수 없습니다: " + str(folder))  # 불완전한 복사 차단
    pending = [folder]  # 검사할 원본 디렉터리
    while pending:  # 모든 하위 항목 확인
        current = pending.pop()  # 다음 원본 항목
        attributes = getattr(current.lstat(), "st_file_attributes", 0)  # Windows 파일 속성
        if current.is_symlink() or attributes & getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x400):  # 심볼릭 링크와 연결점 확인
            raise RuntimeError("링크 또는 연결점이 있어 격리 복사를 중단했습니다: " + str(current))  # 외부 자원 추적 차단
        if current.is_dir():  # 일반 디렉터리 확인
            with os.scandir(current) as entries:  # 하위 항목 조회
                pending.extend(Path(entry.path) for entry in entries)  # 재귀 전 링크 속성 확인
root = Path(tempfile.mkdtemp(prefix="projectg-day17-recovery-")).resolve()  # 운영체제의 고유 임시 폴더
project = root / "project"  # 독립 Unity 프로젝트
save_dir = root / "data"  # 독립 성장 저장 폴더
log_dir = root / "logs"  # 독립 실행 로그 폴더
project.mkdir()  # 프로젝트 폴더 생성
save_dir.mkdir()  # 저장 폴더 생성
log_dir.mkdir()  # 로그 폴더 생성
for name in ("Assets", "Packages", "ProjectSettings"):  # 필요한 Unity 원본만 순회
    shutil.copytree(source / name, project / name)  # 원본을 바꾸지 않는 별도 복사
storage_relative = Path("Assets/ProjectG/Runtime/PermanentProgressService.cs")  # 시험할 저장 서비스 소스
source_hash = hashlib.sha256((source / storage_relative).read_bytes()).hexdigest()  # 원본 서비스 해시
copied_hash = hashlib.sha256((project / storage_relative).read_bytes()).hexdigest()  # 복사된 서비스 해시
if source_hash != copied_hash:  # 복사 도중 소스 변경 확인
    raise RuntimeError("복사된 저장 서비스가 원본과 다릅니다")  # 다른 코드 시험 차단
settings = project / "ProjectSettings" / "ProjectSettings.asset"  # 복사된 프로젝트 설정
raw = settings.read_text(encoding="utf-8-sig")  # 복사본 원문 조회
suffix = uuid.uuid4().hex  # 다른 프로젝트와 겹치지 않는 값
for pattern, replacement in ((r"(?m)^  companyName:.*$", "  companyName: ProjectGProbe" + suffix), (r"(?m)^  productName:.*$", "  productName: RecoveryProbe" + suffix), (r"(?m)^  productGUID:.*$", "  productGUID: " + suffix)):  # 사용자 환경과 구분할 설정
    raw, count = re.subn(pattern, replacement, raw)  # 복사본 설정만 변경
    if count != 1:  # 설정 항목 개수 확인
        raise RuntimeError("복사본의 격리 설정을 확인할 수 없습니다")  # 잘못된 설정 차단
settings.write_text(raw, encoding="utf-8", newline="\n")  # 복사본에 격리 설정 기록
editor_dir = project / "Assets" / "Editor"  # 격리 시험 전용 에디터 폴더
editor_dir.mkdir(exist_ok=True)  # 폴더 준비
shutil.copy2(helper, editor_dir / "ProjectGRecoveryProbe.cs")  # 격리 프로젝트에만 시험 코드 추가
environment = os.environ.copy()  # 현재 실행의 환경 사본
environment["PROJECTG_PROBE_DIR"] = str(save_dir)  # 시험 저장소 명시 경로
environment["PROJECTG_TEST_SAVE_DIR"] = str(save_dir)  # 우발적인 기본 시험 경로도 격리
flags = getattr(subprocess, "CREATE_NO_WINDOW", 0)  # 보이지 않는 자식 프로세스 설정
print("격리 시험 폴더:", root, flush=True)  # 재현 파일 위치 출력

def start_unity(method, log_name, quit_after):  # 격리 Unity 자식 시작
    args = [str(unity), "-batchmode", "-nographics", "-projectPath", str(project), "-executeMethod", "ProjectGRecoveryProbe." + method, "-logFile", str(log_dir / log_name)]  # 배치 실행 인수
    if quit_after:  # 정상 종료 단계 확인
        args.append("-quit")  # 메서드 완료 후 Unity 종료
    return subprocess.Popen(args, env=environment, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, creationflags=flags)  # 직접 시작한 자식 프로세스

first = start_unity("BeginAndWait", "01-begin.log", False)  # 첫 출격 저장 프로세스
print("첫 Unity PID:", first.pid, flush=True)  # 직접 시작한 식별자 출력
try:  # 첫 자식의 안전한 소유 범위
    deadline = time.monotonic() + 480  # 프로젝트 최초 가져오기 제한
    marker = save_dir / "ready.txt"  # 동기화된 준비 표식
    marker_pid = None  # 표식에 기록된 PID
    while time.monotonic() < deadline:  # 준비 완료까지 반복
        if first.poll() is not None:  # 자식 조기 종료 확인
            raise RuntimeError("첫 Unity가 준비 전에 종료됨: " + str(first.returncode))  # 로그 확인 안내
        if marker.exists():  # 준비 표식 생성 확인
            try:  # 완전한 표식 원문 읽기
                lines = marker.read_text(encoding="utf-8").splitlines()  # 표식 내용 조회
                if len(lines) >= 2 and lines[1] == "day17-crash-run":  # 출격 식별자 확인
                    marker_pid = int(lines[0])  # 표식 PID 변환
                    break  # 준비 확인 종료
            except (OSError, ValueError):  # 표식 생성 중 일시 읽기 실패
                pass  # 완전한 표식까지 재시도
        time.sleep(0.2)  # 자식 준비 확인 간격
    if marker_pid != first.pid:  # 직접 실행한 프로세스와 표식 대조
        raise RuntimeError("준비 PID 불일치 또는 제한 시간 초과")  # 다른 프로세스 종료 차단
    original = json.loads((save_dir / "projectg-probe-progress.json").read_text(encoding="utf-8"))  # 플러시 후 실제 성장 파일 조회
    if original.get("activeRunId") != "day17-crash-run":  # 저장 완료 출격 식별 확인
        raise RuntimeError("진행 출격 파일 검증 실패")  # 종료 전 확인 실패 차단
    print("저장 완료 표식과 PID 확인, 해당 자식만 강제 종료", flush=True)  # 안전한 강제 종료 안내
    first.kill()  # 직접 시작한 해당 Unity 자식만 강제 종료
    first.wait(timeout=30)  # 운영체제 종료 완료 대기
finally:  # 첫 자식 정리 범위
    if first.poll() is None:  # 직접 시작한 자식 생존 확인
        first.kill()  # 같은 자식만 강제 종료
        first.wait(timeout=30)  # 종료 완료 확인
for method, logfile, marker in (("RecoverAndSettle", "02-recover.log", "recovered.txt"), ("VerifyReload", "03-verify.log", "verified.txt")):  # 별도 복구와 재로드 단계
    process = start_unity(method, logfile, True)  # 다음 독립 Unity 자식 시작
    print(method, "Unity PID:", process.pid, flush=True)  # 현재 자식 식별 출력
    try:  # 자식 완료 대기
        code = process.wait(timeout=240)  # 메서드와 정상 종료 대기
    except subprocess.TimeoutExpired:  # 자식 제한 시간 초과
        process.kill()  # 직접 시작한 해당 자식만 종료
        process.wait(timeout=30)  # 자식 종료 확인
        raise RuntimeError(method + " 실행 제한 시간 초과")  # 실행 실패 표시
    if code != 0 or not (save_dir / marker).is_file():  # 종료 코드와 완료 표식 확인
        raise RuntimeError(method + " 실패: 종료 코드 " + str(code) + " / 로그 " + str(log_dir / logfile))  # 해당 단계 실패 보고
final = json.loads((save_dir / "projectg-probe-progress.json").read_text(encoding="utf-8"))  # 최종 저장 파일 독립 확인
summary = {"root": str(root), "killed_pid": first.pid, "first_exit_code": first.returncode, "storage_source_sha256": copied_hash, "first_run": "day17-crash-run", "next_run": "day17-next-run", "rapid_mastery": final.get("rapidMastery"), "sniper_mastery": final.get("sniperMastery"), "common_materials": final.get("commonMaterials"), "pending_run": final.get("activeRunId"), "receipt_count": len(final.get("receipts", [])), "scope": "저장 완료 및 표식 동기 기록 후 종료; 파일 교체 중 종료와 실제 플레이는 미검증"}  # 최종 확인 결과
(root / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")  # 재현 가능한 결과 기록
print(json.dumps(summary, ensure_ascii=False, indent=2), flush=True)  # 검증 결과 출력
