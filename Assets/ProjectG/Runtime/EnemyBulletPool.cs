using System.Collections.Generic; // 재사용 목록 기능
using UnityEngine; // 유니티 객체 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class EnemyBulletPool : MonoBehaviour // 적탄 재사용 풀
    { // 클래스 시작
        private readonly Stack<EnemyBullet> available = new Stack<EnemyBullet>(); // 회수된 적탄 목록
        private int created; // 생성된 적탄 수
        private const int MaximumBullets = 256; // 시험용 적탄 상한
        public EnemyBullet Spawn(Vector2 position, Vector2 velocity, float radius, int damage, float bottom, BattlePlayer player) // 적탄 생성
        { // 생성 시작
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
                GameObject item = new GameObject("EnemyBullet"); // 적탄 객체 생성
                item.transform.SetParent(transform, false); // 풀의 자식으로 보관
                bullet = item.AddComponent<EnemyBullet>(); // 적탄 구성 요소
                created++; // 생성 수 증가
            } // 생성 종료
            bullet.Begin(this, position, velocity, radius, damage, bottom, player); // 적탄 상태 초기화
            return bullet; // 준비된 적탄 반환
        } // 생성 종료
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
    } // 클래스 종료
} // 영역 종료
