using UnityEngine; // 월드 방향과 탄막 각도

namespace ProjectG // 프로젝트 게임 영역
{
    public enum FlightThreatPattern // 적탄의 기본 입체 배열
    {
        Aimed, // 한 발 조준탄
        Fan, // 일곱 발 부채꼴탄
        Ring, // 열두 발 입체 고리탄
        Spiral // 발사마다 회전하는 네 갈래 탄
    }

    public static class FlightThreatPatterns // 적과 연습에서 공유하는 발사 방향
    {
        public static int Fire(FlightThreatPool pool, FlightThreatPattern pattern, Vector3 position, Vector3 direction, float phase, int damage = 0) // 지정 패턴과 개별 피해의 실제 발사
        {
            if (direction.sqrMagnitude < 0.0001f) // 영벡터에 대한 방향 검사
            {
                return 0; // 방향이 없는 탄막 생략
            }
            Vector3 forward = direction.normalized; // 전체 탄막의 중심 방향
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up; // 수직 조준에서도 안정적인 기준축
            Quaternion basis = Quaternion.LookRotation(forward, up); // 월드 조준의 로컬 탄막 좌표계
            int count = pattern switch // 패턴의 기본 탄 수
            {
                FlightThreatPattern.Fan => 7, // 부채꼴 공격 탄 수
                FlightThreatPattern.Ring => 12, // 고리 공격 탄 수
                FlightThreatPattern.Spiral => 4, // 나선의 발사당 갈래 수
                _ => 1 // 일대일 조준탄
            };
            int fired = 0; // 실제 풀에서 발사한 탄 수
            for (int index = 0; index < count; index++) // 기본 배열 순서대로 발사
            {
                Vector3 local = PatternDirection(pattern, index, count, phase); // 해당 배열의 로컬 방향
                if (!pool.TryFire(position, basis * local, pattern == FlightThreatPattern.Aimed ? 15f : 12f, damage, pattern)) // 상한과 피해를 적용한 실제 발사
                {
                    break; // 상한에 닿으면 남은 발사 중단
                }
                fired++; // 성공한 적탄 수 기록
            }
            return fired; // 시스템이 활성화한 실제 탄 수
        }

        private static Vector3 PatternDirection(FlightThreatPattern pattern, int index, int count, float phase) // 고도와 각도를 나누는 로컬 방향 계산
        {
            if (pattern == FlightThreatPattern.Fan) // 좌우·상하 분산 부채꼴
            {
                float yaw = Mathf.Lerp(-32f, 32f, (float)index / (count - 1)); // 부채꼴 좌우 각도
                float pitch = index % 2 == 0 ? -6f : 6f; // 하나씩 교차하는 고도
                return Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward; // 입체 부채꼴 방향
            }

            if (pattern == FlightThreatPattern.Ring || pattern == FlightThreatPattern.Spiral) // 원형과 회전 배열
            {
                float roll = 360f * index / count + (pattern == FlightThreatPattern.Spiral ? phase : 0f); // 갈래별 각도와 나선 회전
                float cone = pattern == FlightThreatPattern.Ring ? 24f : 17f; // 발사 중심으로부터 원뿔 기울기
                return Quaternion.AngleAxis(roll, Vector3.forward) * (Quaternion.AngleAxis(cone, Vector3.up) * Vector3.forward); // 앞쪽에서 벌어지는 입체 방향
            }

            return Vector3.forward; // 중앙 조준 방향
        }
    }
}
