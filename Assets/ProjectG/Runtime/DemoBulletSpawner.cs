using UnityEngine; // 유니티 장면 기능
namespace ProjectG // 프로젝트 영역
{ // 영역 시작
    public sealed class DemoBulletSpawner : MonoBehaviour // 시험용 적탄 발사
    { // 클래스 시작
        private EnemyBulletPool pool; // 적탄 재사용 풀
        private BattlePlayer player; // 플레이어 대상
        private Camera battleCamera; // 전투 화면 기준
        private Vector2 spawnPosition; // 다음 발사 위치
        private float timer = 1f; // 첫 발사 대기
        private int lane; // 다음 발사 경로
        public void Begin(EnemyBulletPool bulletPool, BattlePlayer target, Camera camera) // 발사기 초기화
        { // 초기화 시작
            pool = bulletPool; // 적탄 풀 보관
            player = target; // 플레이어 보관
            battleCamera = camera; // 카메라 보관
            ChooseNextLane(); // 첫 발사 위치 선택
        } // 초기화 종료
        public void StopSpawning() // 시험 적탄 발사 종료
        { // 종료 시작
            enabled = false; // 이전 발사기 중단
        } // 종료 종료
        private void ChooseNextLane() // 다음 발사 위치 선택
        { // 선택 시작
            float halfHeight = battleCamera.orthographicSize; // 화면 세로 반경
            float halfWidth = halfHeight * 16f / 9f; // 화면 가로 반경
            Vector2 center = battleCamera.transform.position; // 화면 중심
            int selected = lane % 3; // 세 경로 중 선택
            lane++; // 다음 경로 이동
            spawnPosition = new Vector2(center.x + (selected - 1) * halfWidth * 0.6f, center.y + halfHeight - 0.15f); // 시험용 발사 위치
        } // 선택 종료
        private void Update() // 적탄 발사 진행
        { // 진행 시작
            if (pool == null || player == null || battleCamera == null || player.Survival == null) // 초기화 상태 확인
            { // 생략 시작
                return; // 발사 생략
            } // 생략 종료
            if (!player.Survival.IsAlive) // 출격 실패 확인
            { // 실패 처리 시작
                return; // 발사 중지
            } // 실패 처리 종료
            if (Time.timeScale <= 0f || Time.deltaTime <= 0f) // 일시 정지 확인
            { // 정지 처리 시작
                return; // 시간 진행 중지
            } // 정지 처리 종료
            timer -= Time.deltaTime; // 발사 대기 시간 감소
            if (timer > 0f) // 대기 시간 확인
            { // 대기 시작
                return; // 다음 프레임 대기
            } // 대기 종료
            timer = 1.25f; // 시험용 발사 간격
            if (((Vector2)player.transform.position - spawnPosition).sqrMagnitude >= 1f) // 생성 위치 안전 확인
            { // 발사 시작
                float bottom = battleCamera.transform.position.y - battleCamera.orthographicSize; // 화면 아래 경계
                pool.Spawn(spawnPosition, new Vector2(0f, -3f), 0.06f, 10, bottom, player); // 선택된 위치의 시험 적탄 생성
            } // 발사 종료
            ChooseNextLane(); // 다음 발사 위치 선택
        } // 진행 종료
    } // 클래스 종료
} // 영역 종료
