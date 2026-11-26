using UnityEngine; // 실제 월드 위치와 벡터

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightCraftedOriginContext // 실제로 확인된 발사 기준 위치
    {
        public Vector3? Front; // 플레이어 전방의 발사 지점
        public Vector3? Around; // 플레이어 주변의 기준 위치
        public Vector3? Rear; // 빗자루 후방의 발사 지점
        public Vector3? Summon; // 유효한 소환체의 위치
        public Vector3? Projectile; // 기존 발사체의 실제 위치
        public Vector3? Point; // 사용자가 지정한 유효 좌표
        public Vector3? Target; // 확인된 목표물의 위치
        public Vector3? Path; // 기록된 실제 비행 경로의 위치
    }

    internal static class FlightCraftedOrigins // 구성 ID와 실제 출발 좌표 연결
    {
        internal static bool TryResolve(string origin, FlightCraftedOriginContext context, out Vector3 position) // 실제로 주어진 좌표만 반환
        {
            position = Vector3.zero; // 실패 시 사용하지 않을 기본 출력
            if (context == null) // 실제 시전 좌표가 없는 경우
            {
                return false; // 임의의 생성 위치를 만들지 않음
            }
            Vector3? candidate = origin switch // 기획서의 여덟 기준 위치
            {
                "front" => context.Front, // 비행체 전방 발사점
                "around" => context.Around, // 비행체 주변 기준점
                "rear" => context.Rear, // 빗자루 후방 지점
                "summon" => context.Summon, // 존재하는 소환체 지점
                "projectile" => context.Projectile, // 존재하는 발사체 지점
                "point" => context.Point, // 유효한 지정 좌표
                "target" => context.Target, // 확인한 목표물 위치
                "path" => context.Path, // 기록한 비행 경로 위치
                _ => null // 등록되지 않은 위치 ID
            };
            if (!candidate.HasValue) // 선택한 기준의 실제 좌표 부재
            {
                return false; // 없는 표적과 경로에서 발사하지 않음
            }
            Vector3 value = candidate.Value; // 실제로 제공된 월드 좌표
            if (!IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z)) // 계산할 수 없는 좌표 성분
            {
                return false; // 잘못된 위치의 발사체 생성 차단
            }
            position = value; // 유효성이 확인된 월드 좌표
            return true; // 확인된 생성 위치
        }

        private static bool IsFinite(float value) // 유효한 단일 좌표 성분
        {
            return !float.IsNaN(value) && !float.IsInfinity(value); // 숫자가 아닌 값과 무한대 제외
        }
    }
}
