using System.Collections.Generic; // 적중 대상 목록
using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class Projectile : MonoBehaviour // 플레이어 탄환
    { // 클래스 시작
        public float Damage // 탄환 시험 피해량
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public bool IsBasic // 기본 공격 여부
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        public int RemainingHits // 남은 적중 횟수
        { // 속성 시작
            get; // 값 조회
            private set; // 값 설정
        } // 속성 종료
        private BattlePlayer owner; // 발사 플레이어
        private CharacterType character; // 발사 당시 캐릭터
        private RapidAchievementRun achievementRun; // 발사 당시 출격 과제
        private readonly HashSet<CombatTarget> hitTargets = new HashSet<CombatTarget>(); // 이미 맞힌 표적
        private Vector2 velocity; // 탄환 이동 속도
        private float upperBound; // 화면 위쪽 경계
        private float hitRadius; // 집중탄 가로 판정 반경
        private Camera battleCamera; // 탄환이 속한 전투 카메라
        private static Sprite sharedSprite; // 공통 임시 이미지
        public void Begin(Vector2 speed, float top, float damage = 0f, bool isBasic = true, BattlePlayer player = null, int hits = 1) // 탄환 초기화
        { // 초기화 시작
            velocity = speed; // 이동 속도 보관
            hitRadius = 0f; // 초기 탄환 판정 폭
            Damage = damage; // 시험 피해량 보관
            IsBasic = isBasic; // 기본 공격 구분
            owner = player; // 발사자 보관
            battleCamera = GetComponentInParent<Camera>(); // 생성물의 전투 카메라 확인
            achievementRun = player != null ? player.AchievementRun : null; // 발사 당시 출격 보관
            character = player != null ? player.Character : CharacterType.Rapid; // 발사 캐릭터 보관
            RemainingHits = Mathf.Max(1, hits); // 관통 한도 설정
            hitTargets.Clear(); // 적중 목록 초기화
            upperBound = top; // 위쪽 경계 보관
            SpriteRenderer image = gameObject.AddComponent<SpriteRenderer>(); // 표시 구성 요소
            image.sprite = BattleSprites.Load("bullet_player"); // 플레이어 탄환 이미지
            image.color = Color.white; // 원본 이미지 색상
            transform.localScale = new Vector3(0.30f, 0.72f, 1f); // 아군 탄환 표시 크기 세 배
        } // 초기화 종료
        public void SetFocusWidth(float scale) // 집중탄 표시와 판정 폭
        { // 설정 시작
            float width = Mathf.Clamp(scale, 0.1f, 1f); // 폭 비율 제한
            transform.localScale = new Vector3(transform.localScale.x * width, transform.localScale.y, transform.localScale.z); // 집중탄 표시 폭
            hitRadius = 0.05f * width; // 집중탄 시험 판정 반경
        } // 설정 종료
        public bool TryHit(CombatTarget target, bool weakPoint) // 시험 표적 적중
        { // 적중 처리 시작
            if (target == null || !target.CanTakeDamage || RemainingHits <= 0 || !hitTargets.Add(target)) // 표적 중복과 잔여 횟수 확인
            { // 중복 처리 시작
                return false; // 중복 적중 차단
            } // 중복 처리 종료
            float bonus = owner != null && character == CharacterType.Rapid && IsBasic ? owner.Combo.Bonus : 0f; // 속사 기본탄 보너스
            float weak = character == CharacterType.Sniper && weakPoint ? (owner != null && owner.Character == character && owner.Build != null ? owner.Build.WeakMultiplier : 1.5f) : 1f; // 저격형 약점 시험 배율
            EnemyActor enemy = target as EnemyActor; // 실제 적 대상 확인
            float previousHealth = enemy != null ? enemy.Health : 0f; // 적중 전 체력
            target.ReceiveDamage(Damage * (1f + bonus) * weak, owner); // 시험 피해 전달
            if (enemy != null && enemy.Health < previousHealth && IsBasic && character == CharacterType.Rapid && owner != null && owner.Character == CharacterType.Rapid && owner.Survival != null && owner.Survival.IsAlive && owner.Statistics != null && !owner.Statistics.Finished && achievementRun != null && ReferenceEquals(achievementRun, owner.AchievementRun)) // 같은 출격의 실제 기본 피해 확인
            { // 과제 적중 시작
                achievementRun.RecordHit(); // 유효 적마다 한 번 기록
            } // 과제 적중 종료
            if (owner != null && owner.Character == CharacterType.Rapid && character == CharacterType.Rapid && IsBasic) // 누적 대상 확인
            { // 누적 시작
                owner.Combo.RegisterHit(); // 기본탄 적중 누적
            } // 누적 종료
            RemainingHits--; // 남은 적중 횟수 차감
            return true; // 적중 성공
        } // 적중 처리 종료
        private void Update() // 탄환 이동
        { // 이동 시작
            if (Time.timeScale <= 0f) // 전투 정지 확인
            { // 정지 처리 시작
                return; // 탄환 진행 생략
            } // 정지 처리 종료
            Vector2 from = transform.position; // 이동 전 위치
            Vector2 to = from + velocity * Time.deltaTime; // 이동 후 위치
            bool bounded = battleCamera != null && battleCamera.orthographic; // 실제 직교 전투 화면 확인
            bool leaves = false; // 이번 프레임의 화면 이탈 여부
            Rect battle = default; // 실제 전투 월드 영역
            if (bounded) // 전투 카메라 확인
            { // 경계 처리 시작
                float height = battleCamera.orthographicSize; // 전투 세로 반경
                float width = height * battleCamera.aspect; // 전투 가로 반경
                Vector3 center = battleCamera.transform.position; // 카메라 월드 중심
                battle = new Rect(center.x - width, center.y - height, width * 2f, height * 2f); // 실제 전투 월드 경계
                Vector2 end = to; // 경계 적용 전 이동 끝점
                if (!ClipToBattle(battle, ref from, ref to)) // 이동 구간과 전투 화면 교차 확인
                { // 화면 밖 처리 시작
                    Destroy(gameObject); // 보이지 않는 탄환 제거
                    return; // 화면 밖 판정 차단
                } // 화면 밖 처리 종료
                leaves = end.x < battle.xMin || end.x > battle.xMax || end.y < battle.yMin || end.y > battle.yMax; // 실제 이동 끝점의 이탈 확인
            } // 경계 처리 종료
            CombatTarget[] targets = Object.FindObjectsByType<CombatTarget>(FindObjectsSortMode.None); // 시험 표적 목록
            System.Array.Sort(targets, (a, b) => ((Vector2)a.transform.position - from).sqrMagnitude.CompareTo(((Vector2)b.transform.position - from).sqrMagnitude)); // 가까운 표적 순서
            foreach (CombatTarget target in targets) // 표적 순회
            { // 판정 시작
                if (bounded && !target.IntersectsBeam(battle, out _)) // 표적이 전투 화면과 겹치는지 확인
                { // 화면 밖 표적 시작
                    continue; // 보이지 않는 표적 피해 차단
                } // 화면 밖 표적 종료
                if (target.Intersects(from, to, out bool weak, hitRadius) && TryHit(target, weak) && RemainingHits <= 0) // 소진 탄환 확인
                { // 제거 시작
                    Destroy(gameObject); // 적중 탄환 제거
                    return; // 추가 판정 생략
                } // 제거 종료
            } // 판정 종료
            transform.position = to; // 화면 안 마지막 위치 적용
            if (leaves || transform.position.y > upperBound + 1f) // 실제 전투 또는 기존 위쪽 이탈 확인
            { // 제거 시작
                Destroy(gameObject); // 이탈 탄환 제거
            } // 제거 종료
        } // 이동 종료
        private static bool ClipToBattle(Rect bounds, ref Vector2 from, ref Vector2 to) // 이동 구간의 전투 화면 제한
        { // 제한 시작
            Vector2 origin = from; // 기존 이동 시작점
            Vector2 delta = to - from; // 기존 이동 구간
            float enter = 0f; // 화면 진입 비율
            float exit = 1f; // 화면 이탈 비율
            if (!ClipAxis(-delta.x, origin.x - bounds.xMin, ref enter, ref exit) || !ClipAxis(delta.x, bounds.xMax - origin.x, ref enter, ref exit) || !ClipAxis(-delta.y, origin.y - bounds.yMin, ref enter, ref exit) || !ClipAxis(delta.y, bounds.yMax - origin.y, ref enter, ref exit)) // 네 방향 경계 교차 확인
            { // 화면 밖 시작
                return false; // 화면과 겹치지 않는 구간
            } // 화면 밖 종료
            from = origin + delta * enter; // 화면 안 첫 위치
            to = origin + delta * exit; // 화면 안 마지막 위치
            return true; // 화면 안 구간 확인
        } // 제한 종료
        private static bool ClipAxis(float direction, float distance, ref float enter, ref float exit) // 한 방향의 구간 제한
        { // 계산 시작
            if (Mathf.Abs(direction) < 0.000001f) // 경계와 평행한 이동
            { // 평행 시작
                return distance >= 0f; // 경계 안쪽 여부
            } // 평행 종료
            float fraction = distance / direction; // 경계 접촉 비율
            if (direction < 0f) // 화면 진입 방향
            { // 진입 시작
                enter = Mathf.Max(enter, fraction); // 가장 늦은 진입 위치
            } // 진입 종료
            else // 화면 이탈 방향
            { // 이탈 시작
                exit = Mathf.Min(exit, fraction); // 가장 빠른 이탈 위치
            } // 이탈 종료
            return enter <= exit; // 유효한 화면 안 구간
        } // 계산 종료
        public static Sprite GetSprite() // 공통 이미지 준비
        { // 이미지 시작
            if (sharedSprite == null) // 이미지 존재 확인
            { // 생성 시작
                Texture2D texture = Texture2D.whiteTexture; // 흰색 기본 텍스처
                sharedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width); // 사각형 이미지 생성
            } // 생성 종료
            return sharedSprite; // 공통 이미지 반환
        } // 이미지 종료
    } // 클래스 종료
} // 영역 종료
