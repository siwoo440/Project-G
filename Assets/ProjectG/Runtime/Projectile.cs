using System.Collections.Generic; // 적중 대상 목록
using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class Projectile : MonoBehaviour // 플레이어 탄환
    { // 클래스 시작
        public int Damage // 탄환 시험 피해량
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
        private readonly HashSet<TrainingTarget> hitTargets = new HashSet<TrainingTarget>(); // 이미 맞힌 표적
        private Vector2 velocity; // 탄환 이동 속도
        private float upperBound; // 화면 위쪽 경계
        private static Sprite sharedSprite; // 공통 임시 이미지
        public void Begin(Vector2 speed, float top, int damage = 0, bool isBasic = true, BattlePlayer player = null, int hits = 1) // 탄환 초기화
        { // 초기화 시작
            velocity = speed; // 이동 속도 보관
            Damage = damage; // 시험 피해량 보관
            IsBasic = isBasic; // 기본 공격 구분
            owner = player; // 발사자 보관
            character = player != null ? player.Character : CharacterType.Rapid; // 발사 캐릭터 보관
            RemainingHits = Mathf.Max(1, hits); // 관통 한도 설정
            hitTargets.Clear(); // 적중 목록 초기화
            upperBound = top; // 위쪽 경계 보관
            SpriteRenderer image = gameObject.AddComponent<SpriteRenderer>(); // 표시 구성 요소
            image.sprite = GetSprite(); // 임시 이미지 적용
            image.color = character == CharacterType.Sniper ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.85f, 0.35f); // 캐릭터별 탄환 색상
            transform.localScale = new Vector3(0.10f, 0.24f, 1f); // 탄환 표시 크기
        } // 초기화 종료
        public bool TryHit(TrainingTarget target, bool weakPoint) // 시험 표적 적중
        { // 적중 처리 시작
            if (target == null || RemainingHits <= 0 || !hitTargets.Add(target)) // 표적 중복과 잔여 횟수 확인
            { // 중복 처리 시작
                return false; // 중복 적중 차단
            } // 중복 처리 종료
            float bonus = owner != null && character == CharacterType.Rapid && IsBasic ? owner.Combo.Bonus : 0f; // 속사 기본탄 보너스
            float weak = character == CharacterType.Sniper && weakPoint ? 1.5f : 1f; // 저격형 약점 시험 배율
            target.ApplyDamage(Damage * (1f + bonus) * weak); // 시험 피해 전달
            if (owner != null && owner.Character == CharacterType.Rapid && character == CharacterType.Rapid && IsBasic) // 누적 대상 확인
            { // 누적 시작
                owner.Combo.RegisterHit(); // 기본탄 적중 누적
            } // 누적 종료
            RemainingHits--; // 남은 적중 횟수 차감
            return true; // 적중 성공
        } // 적중 처리 종료
        private void Update() // 탄환 이동
        { // 이동 시작
            Vector2 from = transform.position; // 이동 전 위치
            Vector2 to = from + velocity * Time.deltaTime; // 이동 후 위치
            TrainingTarget[] targets = Object.FindObjectsByType<TrainingTarget>(FindObjectsSortMode.None); // 시험 표적 목록
            System.Array.Sort(targets, (a, b) => ((Vector2)a.transform.position - from).sqrMagnitude.CompareTo(((Vector2)b.transform.position - from).sqrMagnitude)); // 가까운 표적 순서
            foreach (TrainingTarget target in targets) // 표적 순회
            { // 판정 시작
                if (target.Intersects(from, to, out bool weak) && TryHit(target, weak) && RemainingHits <= 0) // 소진 탄환 확인
                { // 제거 시작
                    Destroy(gameObject); // 적중 탄환 제거
                    return; // 추가 판정 생략
                } // 제거 종료
            } // 판정 종료
            transform.position = to; // 경과 시간 이동
            if (transform.position.y > upperBound + 1f) // 화면 이탈 확인
            { // 제거 시작
                Destroy(gameObject); // 이탈 탄환 제거
            } // 제거 종료
        } // 이동 종료
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
