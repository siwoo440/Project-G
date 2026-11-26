using UnityEngine; // 발사 각도와 궤적 벡터

namespace ProjectG // 프로젝트 게임 영역
{
    public static class CraftedSpellVectors // 편집기와 전투가 공유하는 기본 발사 방향
    {
        public static Vector3 Direction(CraftedSpellRecipe recipe, Quaternion aim, int index, float spreadDegrees) // 기본 조합의 개별 발사 방향
        {
            return DirectionForCount(recipe, aim, index, spreadDegrees, recipe.InitialShots); // 기본 탄 수의 배열 계산
        }

        internal static Vector3 DirectionForCount(CraftedSpellRecipe recipe, Quaternion aim, int index, float spreadDegrees, int count) // 강화 탄 수를 포함한 발사 방향
        {
            if (index < 0 || index >= count) // 존재하지 않는 발사 순번
            {
                return Vector3.zero; // 새 발사체의 방향 생성 없음
            }
            if (count == 1) // 단일 발사 또는 위치에 설치되는 효과
            {
                return aim * Vector3.forward; // 화면 또는 표적을 향한 방향
            }

            float half = Mathf.Clamp(spreadDegrees, 0f, 179f) * 0.5f; // 확산 전체 각도의 절반
            if (recipe.Formation == "fan") // 평면으로 퍼지는 부채꼴
            {
                float angle = Mathf.Lerp(-half, half, index / (float)(count - 1)); // 좌우 가장자리 사이 등간격
                float pitch = recipe.BaseId == CraftedSpellPresets.All[3].Recipe.Id ? Mathf.Lerp(-10f, 10f, index / (float)(count - 1)) : 0f; // 수압 파동의 수직 전체 이십 도
                return aim * (Quaternion.Euler(pitch, angle, 0f) * Vector3.forward); // 조준 좌표계에서의 확산 벡터
            }
            if (recipe.Formation == "cone") // 3D 원뿔의 둘레 배치
            {
                float around = index * 2f * Mathf.PI / count; // 조준축을 중심으로 고른 원주 각도
                float radial = Mathf.Sin(half * Mathf.Deg2Rad); // 원뿔의 옆 방향 크기
                return aim * new Vector3(Mathf.Cos(around) * radial, Mathf.Sin(around) * radial, Mathf.Cos(half * Mathf.Deg2Rad)); // 높이를 포함한 단위 발사 벡터
            }
            return aim * Vector3.forward; // 기본 직선 조합
        }

        public static Vector3 StepHeading(CraftedSpellRecipe recipe, Vector3 current, Vector3 towardTarget, float turnDegreesPerSecond, float seconds) // 유도 탄환의 프레임 이동 방향
        {
            Vector3 heading = current.normalized; // 현재 발사체의 단위 방향
            if (recipe.Trajectory != "homing" || towardTarget.sqrMagnitude < 0.0001f) // 직선 궤적 또는 표적 미지정
            {
                return heading; // 기존 방향 유지
            }
            float maxRadians = Mathf.Max(0f, turnDegreesPerSecond) * Mathf.Max(0f, seconds) * Mathf.Deg2Rad; // 프레임당 유도 회전 한도
            return Vector3.RotateTowards(heading, towardTarget.normalized, maxRadians, 0f); // 표적을 향한 제한된 회전
        }
    }
}
