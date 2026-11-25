using System.Collections.Generic; // 공명 대상과 연쇄의 중복 방지
using UnityEngine; // 고도를 포함한 파동과 시각 효과

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 실제 비행체당 하나의 공명 기록
    public sealed class FlightIrisCombat : MonoBehaviour // 3D 확산 파동과 창공의 공명
    {
        private const float Window = 2f; // 서로 다른 유효 적중의 최근 시간
        private const float DisplayTime = 0.3f; // 파동과 연쇄의 짧은 시각 범위
        [SerializeField] private FirstFlightController pilot; // 실제 발사 위치와 평상시 조준 방향
        [SerializeField] private FlightWeapon weapon; // 현재 실제 출격 캐릭터 비교
        [SerializeField] private FlightUpgradeChoices upgrades; // 범위 상한과 시험 성장
        [SerializeField] private LineRenderer wave; // 전방 파동의 하이러키 렌더러
        [SerializeField] private LineRenderer links; // 서로 다른 적에게 닿는 연쇄 경로
        [SerializeField] private LayerMask terrain = 1 << 8; // 장애물로 막힌 파동과 연쇄 검사
        private readonly Dictionary<FlightEnemy, float> seen = new Dictionary<FlightEnemy, float>(); // 살아 있는 최근의 서로 다른 적 기록
        private readonly List<FlightEnemy> expired = new List<FlightEnemy>(); // 끝난 기록을 정리할 버퍼
        private readonly HashSet<FlightEnemy> affected = new HashSet<FlightEnemy>(); // 이번 파동과 연쇄에 포함된 대상
        private readonly Vector3[] arc = new Vector3[33]; // 실제 전방 파동의 매끄러운 경로
        private Vector3 waveCenter; // 파동이 시작한 위치
        private Quaternion waveHeading; // 파동 발동 순간의 조준 기준
        private float waveReach; // 실제 이번 파동의 최대 길이
        private float waveAngle; // 현재 정면 원뿔 반각
        private float waveEnd; // 렌더가 끝날 실제 게임 시각

        public int ResonancePercent => Mathf.Min(upgrades != null ? upgrades.IrisLimit : 30, seen.Count * 10); // 적 하나마다 10%씩 증가한 현재 범위

        internal void Observe(FlightEnemy enemy, bool _) // 직접 스킬 또는 실제 탄환이 깎은 마물 체력
        {
            if (weapon.ActivePilot == FlightBroomPilot.Iris) // 전환 전 남긴 이전 마법과 기타 조종사 제외
            {
                seen[enemy] = Time.time + Window; // 같은 마물은 기간만 갱신하고 새 칸은 추가하지 않음
            }
        }

        internal void ResetRun() // 재출격이나 다른 조종사 선택 시 공명 정리
        {
            seen.Clear(); // 이전 전투의 적중 기록 삭제
            affected.Clear(); // 다른 출격의 대상 구분 제거
            wave.enabled = false; // 숨긴 화면의 이전 파동 종료
            links.enabled = false; // 이전 연쇄 경로 종료
        }

        internal bool CastWave(bool ultimate) // 전방 확산 또는 두 번의 추가 연쇄
        {
            Expire(); // 발동 순간의 실제 범위 보너스만 사용
            float scale = 1f + ResonancePercent * 0.01f; // 적중 이전에 고정한 이번 파동 배율
            waveHeading = pilot.AimRotation; // 자유 시점과 분리된 실제 조준 방향
            waveCenter = pilot.transform.position; // 현재 3D 마법 파동의 중심
            waveReach = (ultimate ? 16f : 10f) * scale; // 문서의 정면 파동을 입체 거리로 구현한 시험값
            waveAngle = ultimate ? 32f : 36f; // 고도를 포함한 실제 3D 원뿔의 반각
            Vector3 forward = waveHeading * Vector3.forward; // 원뿔 중심 축
            float cone = Mathf.Cos(waveAngle * Mathf.Deg2Rad); // 정면 경계 판정의 방향 계수
            affected.Clear(); // 한 차례 시전에 맞는 대상은 각 한 번으로 처리
            FlightEnemy[] enemies = FindObjectsByType<FlightEnemy>(FindObjectsSortMode.None); // 현재 활성 상태인 실제 마물
            FlightEnemy first = null; // 창공 연쇄가 이어지는 첫 적중
            float nearest = float.PositiveInfinity; // 첫 적중 시전 지점과의 거리
            foreach (FlightEnemy enemy in enemies) // 원뿔에 들어온 지형 앞 마물
            {
                Vector3 offset = enemy.transform.position - waveCenter; // 고도를 포함한 피해 위치
                if (enemy.Health <= 0 || !InsideWave(offset, forward, cone) || !Visible(waveCenter, enemy.transform.position)) // 실제 앞쪽 위치와 지형 확인
                {
                    continue; // 원뿔 바깥이나 장애물 뒤 공격 제외
                }
                affected.Add(enemy); // 직접 맞힌 대상은 연쇄에서 다시 선택하지 않음
                if (Hit(enemy, ultimate ? 140 : 65) && offset.sqrMagnitude < nearest) // 실제 체력이 감소한 가장 가까운 목표
                {
                    nearest = offset.sqrMagnitude; // 창공 연쇄의 시작 후보 거리
                    first = enemy; // 첫 연쇄의 실제 위치
                }
            }
            foreach (FlightPracticeTarget target in FindObjectsByType<FlightPracticeTarget>(FindObjectsSortMode.None)) // 첫 비행 안내와 독립 연습의 표적
            {
                if (InsideWave(target.transform.position - waveCenter, forward, cone) && Visible(waveCenter, target.transform.position)) // 실제 전방 파동과 같은 3D 범위
                {
                    target.ReceiveHit(ultimate ? 140 : 65); // 연습 표적의 실제 피격 표시
                }
            }
            links.enabled = false; // 전에 남았던 다른 파동의 연쇄 정리
            if (ultimate && first != null) // 피해를 받은 대상에서만 연쇄 시작
            {
                SendChain(enemies, first.transform.position, 4.2f * scale); // 고도별 가까운 서로 다른 적에게 전파
            }
            waveEnd = Time.time + DisplayTime; // 파동과 연쇄의 독립 표시 종료 시각
            DrawWave(0.25f); // 정면에서 펼쳐지는 처음 파동
            wave.enabled = true; // 실제 씬의 하이러키에서 표시
            return true; // 마력이 소비되는 정상 확산
        }

        private void SendChain(FlightEnemy[] enemies, Vector3 fork, float radius) // 실제 피해를 입힌 곳에서 두 차례 이어지는 광선
        {
            links.positionCount = 3; // 최대 두 연결의 기준 좌표
            links.SetPosition(0, fork); // 첫 타격한 마물의 실제 위치
            int count = 1; // 첫 점을 시작으로 기록한 실제 연쇄 경로 길이
            for (int hop = 0; hop < 2; hop++) // 시험값의 최대 두 번의 새로운 타격
            {
                FlightEnemy next = null; // 이번 홉에서 가장 가까운 새 대상
                float limit = radius * radius; // 고도를 포함한 실제 최대 전파 거리
                foreach (FlightEnemy enemy in enemies) // 이미 적중한 대상은 이 시전에서 제외
                {
                    float distance = (enemy.transform.position - fork).sqrMagnitude; // 현재 전파점부터 다른 적까지의 거리
                    if (enemy.Health > 0 && !affected.Contains(enemy) && distance <= limit && Visible(fork, enemy.transform.position)) // 같은 탄의 중복과 벽 차단
                    {
                        limit = distance; // 현재 새 후보의 실제 거리
                        next = enemy; // 이전에 맞히지 않은 가까운 적
                    }
                }
                if (next == null) // 주변에 새로운 유효 목표가 없는 상태
                {
                    break; // 같은 적에게 임의로 중복 피해를 적용하지 않음
                }
                affected.Add(next); // 다음 연쇄가 같은 적에게 돌아가지 않게 기록
                Hit(next, 30); // 추가 전파에서만 적용하는 실제 30 피해
                fork = next.transform.position; // 다음 연쇄의 화면 위치와 실제 범위
                links.SetPosition(count++, fork); // 다른 고도의 마물을 잇는 경로
            }
            links.positionCount = count; // 실행에서 확정된 실제 연결 수
            links.enabled = count > 1; // 연결이 없으면 빈 선 숨김
        }

        private bool Hit(FlightEnemy enemy, int damage) // 무적과 차단을 제외한 피해와 공명 기록
        {
            int before = enemy.Health; // 실제 피격 직전의 체력
            enemy.ReceiveHit(damage); // 원래 마물·보스의 체력과 페이즈 처리
            if (enemy.Health >= before) // 체력이 바뀌지 않은 공격
            {
                return false; // 시각 효과만 들어오고 새 표적 누적 없음
            }
            Observe(enemy, false); // 실제로 입힌 피해를 2초 창에 연결
            return true; // 연쇄가 시작될 수 있는 실제 적중
        }

        private bool InsideWave(Vector3 offset, Vector3 forward, float cone) // 정면과 실제 거리를 모두 따르는 원뿔
        {
            float distance = offset.magnitude; // 좌우·깊이·고도의 입체 거리
            return distance > 0f && distance <= waveReach && Vector3.Dot(offset, forward) >= cone * distance; // 범위 내의 서로 다른 고도 확인
        }

        private bool Visible(Vector3 from, Vector3 to) => !Physics.Linecast(from, to, terrain, QueryTriggerInteraction.Ignore); // 지형을 넘지 않는 파동 전파

        private void LateUpdate() // 현재 공명 시각과 전방 파동 렌더
        {
            if (weapon.ActivePilot != FlightBroomPilot.Iris || Time.timeScale <= 0f) // 다른 캐릭터와 정지 프레임의 작업 생략
            {
                return; // 정지 중 실제 2초 창도 유지
            }
            Expire(); // 최근 스침이 없는 실제 대상 정리
            if (wave.enabled && (!pilot.CanFly || Time.time >= waveEnd)) // 광원의 종료 또는 전투 종료
            {
                wave.enabled = false; // 화면의 전방 파동 지우기
                links.enabled = false; // 같은 마법의 연쇄 결과 지우기
            }
            else if (wave.enabled) // 남아 있는 파동 외형의 전방 이동
            {
                DrawWave(Mathf.Lerp(0.25f, 1f, 1f - (waveEnd - Time.time) / DisplayTime)); // 파동이 원뿔의 최대 위치까지 퍼지는 연출
            }
        }

        private void Expire() // 같은 적의 히트 타임을 보존하는 실제 2초 창
        {
            expired.Clear(); // 지난 프레임의 정리 버퍼 비우기
            foreach (var entry in seen) // 각 적중이 발생한 실제 마물과 종료 시각
            {
                if (entry.Value <= Time.time) // 2초 안에 다시 피해를 주지 않은 적
                {
                    expired.Add(entry.Key); // 현재 공명에서 제거할 대상
                }
            }
            foreach (FlightEnemy enemy in expired) // 항목 순회가 끝난 뒤 기록 삭제
            {
                seen.Remove(enemy); // 다음 발동의 실제 원뿔 범위에 반영
            }
        }

        private void DrawWave(float position) // 화면에서 읽히는 넓은 하늘색 부채꼴
        {
            float distance = waveReach * position; // 앞으로 이동하는 시각 파동의 거리
            for (int i = 0; i < arc.Length; i++) // 균등하게 펼친 부채꼴의 실제 곡선 좌표
            {
                float amount = (float)i / (arc.Length - 1); // 왼쪽 끝부터 오른쪽 끝까지의 진행 상태
                float yaw = Mathf.Lerp(-waveAngle, waveAngle, amount); // 실제 원뿔의 양끝 각도
                Vector3 heading = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; // 현재 조준과 합성할 바깥 경로
                arc[i] = waveCenter + waveHeading * (heading * distance + Vector3.up * (Mathf.Sin(amount * Mathf.PI) * 0.4f)); // 고도와 월드상의 곡선
            }
            wave.SetPositions(arc); // 하이러키의 전방 파동을 직접 렌더
        }
    }
}
