using UnityEngine; // 실제 비행 시점과 3D 피해 범위

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 비행체당 하나의 근거리 검기 판정
    public sealed class FlightSeraCombat : MonoBehaviour // 돌파 검기와 천공 연참
    {
        private const float ChainInterval = 0.16f; // 전방 연참 사이의 시험 간격
        private const float ArcSeconds = 0.18f; // 검기의 밝은 붉은 궤적 표시 시간
        [SerializeField] private FirstFlightController flight; // 지형·고도 경계를 보존하는 기체
        [SerializeField] private FlightUpgradeChoices upgrades; // 근거리 결의와 검기 너비 노드
        [SerializeField] private LineRenderer arc; // 하이러키에 놓인 실제 붉은 검기
        [SerializeField] private LayerMask terrain = 1 << 8; // 검기와 돌파를 막는 지형
        private readonly Vector3[] points = new Vector3[25]; // 반복 시전의 궤적 표시 좌표
        private Quaternion chainAim; // 연참을 시작할 때의 시점 방향
        private float nextStrike; // 다음 연참이 적용될 게임 시각
        private float arcUntil; // 시각 효과 종료 시각
        private int remaining; // 첫 타격 이후 아직 남은 연참 수

        internal float DamageMultiplier(Vector3 target) // 실제 플레이어와 적 중심 거리
        {
            float range = upgrades != null ? upgrades.SeraCloseRange : 1.5f; // 현재 카드가 바꾼 근거리 범위
            return (target - flight.transform.position).sqrMagnitude <= range * range ? 1.25f : 1f; // 모든 방향의 실제 3D 거리
        }

        internal bool Cast(bool ultimate) // 실제 키 입력에서 확인한 고유기 또는 필살기
        {
            Quaternion aim = flight.AimRotation; // 둘러보기 카메라와 독립된 조준 방향
            if (ultimate) // 천공 연참의 이어지는 다섯 차례 피해
            {
                chainAim = aim; // 다른 스킬과 독립인 연참 조준 방향 보관
                Strike(chainAim, true, 0); // 발동 순간에 첫 참격
                remaining = 4; // 남은 네 번의 연속 검기
                nextStrike = Time.time + ChainInterval; // 다음 타격 예약
            }
            else // 돌파 검기
            {
                Strike(aim, false, 0); // 대상마다 한 번의 넓은 실제 피해
                flight.StartSkillDash(aim * Vector3.forward, 16f, 0.25f); // 독립 시험값인 최대 4m 돌파
            }
            return true; // 비어 있는 방향으로도 실제 검기 사용 가능
        }

        internal void ResetRun() // 재출격 중 이전 타격과 입체 효과 정리
        {
            remaining = 0; // 이전에 예약된 연참 제거
            arc.enabled = false; // 이전 화면의 궤적 제거
        }

        private void Update() // 실제 전투 시간의 연참 처리
        {
            if (Time.timeScale <= 0f) // 일시정지와 강화 카드를 보는 동안
            {
                return; // 남아 있는 연참 시간 유지
            }
            if (!flight.CanFly) // 출격 종료로 공격을 멈출 시점
            {
                ResetRun(); // 예약한 피해와 화면 효과 제거
                return; // 종료 뒤 스킬 추가 피해 없음
            }
            if (arc.enabled && Time.time >= arcUntil) // 검기 그림의 수명 완료
            {
                arc.enabled = false; // 화면에 남은 궤적 제거
            }
            if (remaining > 0 && Time.time >= nextStrike) // 다음으로 예약한 참격
            {
                Strike(chainAim, true, 5 - remaining); // 서로 다른 궤적으로 다음 타격
                remaining--; // 진행한 참격 한 번 반영
                nextStrike += ChainInterval; // 다음 참격의 동일 간격
            }
        }

        private void Strike(Quaternion aim, bool ultimate, int order) // 한 차례의 검기 및 벽 뒤 제외
        {
            Vector3 origin = flight.transform.position; // 실제 피해 시점의 조종사 위치
            Quaternion inverse = Quaternion.Inverse(aim); // 전방·수직·수평 비교를 위한 역회전
            float reach = ultimate ? 8.5f : 7f; // 실제 피해 검사에 쓰는 전방 사거리 시험값
            float halfWidth = ultimate ? 3.3f : 2.4f * (upgrades != null ? upgrades.SeraBladeWidth : 1f); // 전방 검기의 좌우 범위
            float halfHeight = ultimate ? 2f : 1.55f; // 고도를 넘지 않는 검사 범위
            int damage = ultimate ? 40 : 70; // 천공 연참 한 번과 돌파 검기의 시험 피해
            foreach (FlightEnemy enemy in FindObjectsByType<FlightEnemy>(FindObjectsSortMode.None)) // 현재 화면과 3차원 전장의 실제 마물
            {
                if (enemy.Health > 0 && CanStrike(enemy.transform.position, origin, inverse, reach, halfWidth, halfHeight)) // 정면 범위와 벽 검사
                {
                    enemy.ReceiveHit(Mathf.RoundToInt(damage * DamageMultiplier(enemy.transform.position))); // 같은 참격 한 번만 피해 적용
                }
            }
            foreach (FlightPracticeTarget target in FindObjectsByType<FlightPracticeTarget>(FindObjectsSortMode.None)) // 연습 항로의 실제 타격 표적
            {
                if (CanStrike(target.transform.position, origin, inverse, reach, halfWidth, halfHeight)) // 실전과 동일한 3D 위치 검사
                {
                    target.ReceiveHit(Mathf.RoundToInt(damage * DamageMultiplier(target.transform.position))); // 근거리 계수의 시험장 확인
                }
            }
            DrawArc(origin, aim, reach, halfWidth, order); // 실제 발동 방향에 보이는 궤적
        }

        private bool CanStrike(Vector3 target, Vector3 origin, Quaternion inverse, float reach, float width, float height) // 전방 상자와 지형 가림 판정
        {
            Vector3 local = inverse * (target - origin); // 조준 기준 상하좌우와 거리
            return local.z > 0f && local.z <= reach && Mathf.Abs(local.x) <= width && Mathf.Abs(local.y) <= height // 검기의 세 축 범위
                && !Physics.Linecast(origin, target, terrain, QueryTriggerInteraction.Ignore); // 벽 뒤에 있는 표적 보호
        }

        private void DrawArc(Vector3 origin, Quaternion aim, float reach, float width, int order) // 3D 위치에 실제 붉은 초승달 궤적
        {
            float tilt = order % 2 == 0 ? 20f : -25f; // 연참마다 바뀌는 좌우 궤적의 기울기
            Quaternion pose = aim * Quaternion.Euler(0f, 0f, tilt); // 실제 조준에 맞는 스타일 방향
            for (int i = 0; i < points.Length; i++) // 부드러운 초승달의 세계 좌표
            {
                float offset = (float)i / (points.Length - 1) * 2f - 1f; // 왼쪽 끝에서 오른쪽 끝까지의 비율
                float bulge = 1f - offset * offset; // 중앙이 앞쪽으로 굽는 궤적
                points[i] = origin + pose * new Vector3(offset * width, 0.1f + 0.42f * bulge, reach * (0.50f + 0.22f * bulge)); // 시전의 실제 길이와 폭
            }
            arc.SetPositions(points); // 씬의 편집 가능한 시각 효과 위치
            arc.enabled = true; // 현재 참격의 순간 표시
            arcUntil = Time.time + ArcSeconds; // 검기 외형의 독립 수명
        }
    }
}
