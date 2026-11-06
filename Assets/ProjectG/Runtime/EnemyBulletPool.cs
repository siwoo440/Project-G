using System.Collections.Generic; // 재사용 목록 기능
using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class EnemyBulletPool : MonoBehaviour // 적탄 재사용 풀
    { // 클래스 시작
        private readonly Stack<EnemyBullet> available = new Stack<EnemyBullet>(); // 회수된 적탄 목록
        private readonly List<EnemyBullet> activeScan = new List<EnemyBullet>(); // 활성 적탄 조회 재사용 목록
        [SerializeField] private EnemyBullet template; // 씬에 둔 비활성 적탄 템플릿
        private Camera cachedView; // 재사용할 풀의 카메라
        private int created; // 생성된 적탄 수
        private const int MaximumBullets = 256; // 시험용 적탄 상한
        public bool IsConfigured => template != null && template.transform.parent == transform && !template.gameObject.activeSelf && template.HasValidVisual; // 직접 자식 템플릿 연결 상태
        public bool SetTemplate(EnemyBullet candidate) // 씬 템플릿 명시적 연결
        { // 연결 시작
            if (candidate == null || candidate.transform.parent != transform || candidate.gameObject.activeSelf || !candidate.HasValidVisual) // 직접 자식과 템플릿 유효성 확인
            { // 오류 시작
                Debug.LogError("적탄 템플릿은 풀의 비활성 직접 자식이며 이미지가 연결되어야 합니다", this); // 설정 오류 표시
                return false; // 잘못된 템플릿 차단
            } // 오류 종료
            if (created > 0 && template != candidate) // 기존 탄환과 다른 템플릿 확인
            { // 오류 시작
                Debug.LogError("적탄 템플릿은 출격 중 교체할 수 없습니다", this); // 기존 풀 혼합 방지
                return false; // 새 출격에서만 변경 허용
            } // 오류 종료
            template = candidate; // 씬에 편집한 템플릿 보관
            return true; // 연결 성공
        } // 연결 종료
        internal Camera View // 현재 풀의 카메라 조회
        { // 조회 시작
            get // 현재 카메라 반환
            { // 반환 시작
                if (cachedView == null) // 카메라 부재 또는 파괴 확인
                { // 재조회 시작
                    cachedView = GetComponent<Camera>(); // 새 카메라 조회
                } // 재조회 종료
                return cachedView; // 현재 카메라 반환
            } // 반환 종료
        } // 조회 종료
        public int ActiveCount // 현재 활성 적탄 수
        { // 조회 시작
            get // 활성 개수 조회
            { // 집계 시작
                activeScan.Clear(); // 이전 조회 목록 비움
                GetComponentsInChildren(false, activeScan); // 활성 자식 적탄 조회
                int count = 0; // 현재 활성 개수
                foreach (EnemyBullet bullet in activeScan) // 적탄 순회
                { // 순회 시작
                    if (bullet != null && bullet.InUse && bullet.gameObject.activeInHierarchy) // 실제 활성 상태 확인
                    { // 집계 시작
                        count++; // 활성 적탄 증가
                    } // 집계 종료
                } // 순회 종료
                return count; // 현재 개수 반환
            } // 집계 종료
        } // 조회 종료
        public EnemyBullet Spawn(Vector2 position, Vector2 velocity, float radius, int damage, float bottom, BattlePlayer player, bool clearable = true) // 적탄 생성
        { // 생성 시작
            if (!IsConfigured) // 씬 템플릿 상태 확인
            { // 오류 시작
                Debug.LogError("적탄 템플릿이 연결되지 않았거나 유효하지 않습니다", this); // 누락 오류 표시
                return null; // 임의 생성 차단
            } // 오류 종료
            EnemyBullet bullet; // 사용할 적탄
            if (available.Count > 0) // 회수된 적탄 확인
            { // 재사용 시작
                bullet = available.Pop(); // 기존 적탄 선택
            } // 재사용 종료
            else // 새 적탄 필요
            { // 생성 시작
                if (created >= MaximumBullets) // 생성 상한 확인
                { // 상한 처리 시작
                    return null; // 생성 생략
                } // 상한 처리 종료
                bullet = Instantiate(template, transform, false); // 씬에 편집한 템플릿 복제
                created++; // 생성 수 증가
            } // 생성 종료
            bullet.Begin(this, position, velocity, radius, damage, bottom, player, clearable); // 적탄 상태 초기화
            return bullet; // 준비된 적탄 반환
        } // 생성 종료
        public int ClearAll() // 출격 경계의 모든 탄환 회수
        { // 전체 회수 시작
            activeScan.Clear(); // 이전 조회 목록 비움
            GetComponentsInChildren(false, activeScan); // 현재 활성 탄환 조회
            int count = 0; // 회수한 탄환 수
            foreach (EnemyBullet bullet in activeScan) // 활성 탄환 순회
            { // 순회 시작
                if (bullet != null && bullet.InUse && bullet.Owner == this) // 이 풀의 활성 탄환 확인
                { // 회수 시작
                    Recycle(bullet); // 화면과 소거 조건 무관 회수
                    count++; // 실제 회수 수 증가
                } // 회수 종료
            } // 순회 종료
            return count; // 회수 수 반환
        } // 전체 회수 종료
        public void Recycle(EnemyBullet bullet) // 적탄 회수
        { // 회수 시작
            if (bullet == null || !bullet.InUse || bullet.Owner != this) // 중복 또는 다른 풀 확인
            { // 무시 시작
                return; // 회수 생략
            } // 무시 종료
            bullet.InUse = false; // 사용 종료 표시
            bullet.gameObject.SetActive(false); // 적탄 비활성화
            available.Push(bullet); // 재사용 목록 보관
        } // 회수 종료
        public int ClearVisible(Rect bounds) // 화면 안 적탄 소거
        { // 소거 시작
            int count = 0; // 소거한 탄환 수
            EnemyBullet[] bullets = GetComponentsInChildren<EnemyBullet>(); // 활성 적탄 목록
            foreach (EnemyBullet bullet in bullets) // 적탄 순회
            { // 순회 시작
                if (!bullet.InUse || !bullet.Clearable) // 소거 조건 확인
                { // 건너뛰기 시작
                    continue; // 소거 불가 탄환 유지
                } // 건너뛰기 종료
                Vector2 position = bullet.transform.position; // 탄환 중심 위치
                float closestX = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax); // 화면의 가까운 가로 위치
                float closestY = Mathf.Clamp(position.y, bounds.yMin, bounds.yMax); // 화면의 가까운 세로 위치
                Vector2 closest = new Vector2(closestX, closestY); // 화면의 가까운 점
                if ((position - closest).sqrMagnitude > bullet.DisplayRadius * bullet.DisplayRadius) // 표시 영역이 화면 밖인지 확인
                { // 화면 밖 처리 시작
                    continue; // 화면 밖 탄환 유지
                } // 화면 밖 처리 종료
                Recycle(bullet); // 화면 안 탄환 회수
                count++; // 소거 수 증가
            } // 순회 종료
            return count; // 소거 수 반환
        } // 소거 종료
    } // 클래스 종료
} // 영역 종료
