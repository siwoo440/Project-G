using System; // 읽기 전용 연구 목록
using System.Collections.Generic; // 분류별 고정 연구 목록

namespace ProjectG // 프로젝트 게임 영역
{
    internal sealed class FlightCraftedResearchNode // 하나의 분류에 속한 영구 연구 항목
    {
        public readonly CraftedSpellGroup Group; // 같은 이름을 구분하는 제작 분류
        public readonly string Id; // 기존 제작 요소의 고정 ID
        public readonly int Tier; // 초기·일반·심화·특수 자격
        public readonly bool Linked; // 별도 구매하지 않는 무료 연동
        public readonly string[] Parents; // 모두 충족해야 하는 분류별 선행 ID
        public string Key => Group + "." + Id; // 같은 이름이 충돌하지 않는 공용 해금 ID

        public FlightCraftedResearchNode(CraftedSpellGroup group, string id, int tier, bool linked, params string[] parents) // 확정된 요소와 선행 연구
        {
            Group = group; // 실제 제작 분류 보존
            Id = id; // 해당 분류의 고정 식별자
            Tier = tier; // 연구 자격과 비용 단계
            Linked = linked; // 무료 자동 연동 여부
            Parents = parents; // 모든 선행 요소의 안정 ID
        }
    }

    internal static class FlightCraftedResearchCatalog // 15.12의 연구 단계와 선행 트리
    {
        private static FlightCraftedResearchNode N(CraftedSpellGroup group, string id, int tier, params string[] parents) // 일반 연구 노드 생성
        {
            return new FlightCraftedResearchNode(group, id, tier, false, parents); // 별도 연구로 얻는 구성 요소
        }

        private static readonly IReadOnlyList<FlightCraftedResearchNode> nodes = Array.AsReadOnly(new[] // 아홉 분류의 확정 연구 항목
        {
            N(CraftedSpellGroup.Element, "fire", 0), // 초기 화염
            N(CraftedSpellGroup.Element, "water", 0), // 초기 물
            N(CraftedSpellGroup.Element, "grass", 0), // 초기 풀
            N(CraftedSpellGroup.Element, "light", 0), // 초기 빛
            N(CraftedSpellGroup.Element, "dark", 0), // 초기 어둠
            N(CraftedSpellGroup.Shape, "orb", 0), // 초기 구체탄
            N(CraftedSpellGroup.Shape, "small", 1, "Shape.orb"), // 구체탄 이후 소형탄
            N(CraftedSpellGroup.Shape, "large", 1, "Shape.orb"), // 구체탄 이후 대형탄
            N(CraftedSpellGroup.Shape, "needle", 0), // 초기 침탄
            N(CraftedSpellGroup.Shape, "crystal", 1, "Shape.needle"), // 침탄 이후 결정탄
            N(CraftedSpellGroup.Shape, "arrow", 1, "Shape.needle"), // 침탄 이후 화살탄
            N(CraftedSpellGroup.Shape, "blade", 2, "Shape.needle"), // 침탄 이후 검형탄
            N(CraftedSpellGroup.Shape, "disc", 2, "Shape.orb"), // 구체탄 이후 원반탄
            N(CraftedSpellGroup.Shape, "missile", 2, "Shape.small", "Trajectory.homing"), // 소형탄과 유도형 이후 미사일탄
            N(CraftedSpellGroup.Shape, "bomb", 1, "Shape.orb"), // 구체탄 이후 폭발탄
            N(CraftedSpellGroup.Shape, "wave", 0), // 초기 파동탄
            N(CraftedSpellGroup.Shape, "laser", 2, "Shape.crystal"), // 결정탄 이후 레이저
            N(CraftedSpellGroup.Shape, "beam", 3, "Shape.laser", "FireMode.held"), // 레이저와 지속 방식 이후 광선
            N(CraftedSpellGroup.Shape, "placed", 1, "Shape.barrier"), // 결계탄 이후 설치탄
            N(CraftedSpellGroup.Shape, "summon", 3, "Shape.placed"), // 설치탄 이후 소환탄
            N(CraftedSpellGroup.Shape, "barrier", 0), // 초기 결계탄
            N(CraftedSpellGroup.Formation, "single", 0), // 초기 단발 배열
            N(CraftedSpellGroup.Formation, "parallel", 1, "Formation.single"), // 단발 이후 병렬 배열
            N(CraftedSpellGroup.Formation, "fan", 0), // 초기 부채꼴 배열
            N(CraftedSpellGroup.Formation, "cone", 0), // 초기 원뿔 배열
            N(CraftedSpellGroup.Formation, "ring", 1, "Formation.fan"), // 부채꼴 이후 원형 배열
            N(CraftedSpellGroup.Formation, "sphere", 0), // 초기 구형 배열
            N(CraftedSpellGroup.Formation, "cylinder", 2, "Formation.sphere", "Formation.ring"), // 구형과 원형 이후 원통형
            N(CraftedSpellGroup.Formation, "spiral", 2, "Formation.ring"), // 원형 이후 나선 배열
            N(CraftedSpellGroup.Formation, "cross", 1, "Formation.parallel"), // 병렬 이후 교차 배열
            N(CraftedSpellGroup.Formation, "grid", 2, "Formation.cross"), // 교차 이후 격자 배열
            N(CraftedSpellGroup.Formation, "wall", 2, "Formation.grid"), // 격자 이후 벽 배열
            N(CraftedSpellGroup.Formation, "converge", 1, "Formation.parallel"), // 병렬 이후 집중 배열
            N(CraftedSpellGroup.Formation, "random", 1, "Formation.fan"), // 부채꼴 이후 무작위 배열
            N(CraftedSpellGroup.Formation, "satellite", 2, "Formation.ring"), // 원형 이후 위성 배열
            N(CraftedSpellGroup.Trajectory, "straight", 0), // 초기 직선 궤적
            N(CraftedSpellGroup.Trajectory, "accelerate", 1, "Trajectory.straight"), // 직선 이후 가속 궤적
            N(CraftedSpellGroup.Trajectory, "decelerate", 1, "Trajectory.straight"), // 직선 이후 감속 궤적
            N(CraftedSpellGroup.Trajectory, "stationary", 0), // 초기 정지 궤적
            N(CraftedSpellGroup.Trajectory, "curve", 1, "Trajectory.straight"), // 직선 이후 곡선 궤적
            N(CraftedSpellGroup.Trajectory, "arc", 1, "Trajectory.curve"), // 곡선 이후 포물선 궤적
            N(CraftedSpellGroup.Trajectory, "zigzag", 1, "Trajectory.curve"), // 곡선 이후 지그재그 궤적
            N(CraftedSpellGroup.Trajectory, "wave", 1, "Trajectory.curve"), // 곡선 이후 파동 궤적
            N(CraftedSpellGroup.Trajectory, "spiral", 2, "Trajectory.wave"), // 파동 이후 나선 궤적
            N(CraftedSpellGroup.Trajectory, "homing", 2, "Trajectory.curve"), // 곡선 이후 유도 궤적
            N(CraftedSpellGroup.Trajectory, "orbit", 2, "Trajectory.curve"), // 곡선 이후 공전 궤적
            N(CraftedSpellGroup.Trajectory, "return", 2, "Trajectory.curve"), // 곡선 이후 회귀 궤적
            N(CraftedSpellGroup.Trajectory, "bounce", 2, "Trajectory.straight"), // 직선 이후 반사 궤적
            N(CraftedSpellGroup.Trajectory, "switch", 2, "Trajectory.curve"), // 곡선 이후 전환 궤적
            N(CraftedSpellGroup.Trajectory, "gravity", 3, "Trajectory.homing"), // 유도 이후 중력 궤적
            N(CraftedSpellGroup.Trajectory, "random", 1, "Trajectory.curve"), // 곡선 이후 무작위 궤적
            N(CraftedSpellGroup.FireMode, "single", 0), // 초기 단발 방식
            N(CraftedSpellGroup.FireMode, "rapid", 1, "FireMode.single"), // 단발 이후 연사 방식
            N(CraftedSpellGroup.FireMode, "burst", 1, "FireMode.single"), // 단발 이후 점사 방식
            N(CraftedSpellGroup.FireMode, "charge", 1, "FireMode.single"), // 단발 이후 충전 방식
            N(CraftedSpellGroup.FireMode, "held", 2, "FireMode.rapid"), // 연사 이후 지속 방식
            N(CraftedSpellGroup.FireMode, "placed", 0), // 초기 설치 방식
            N(CraftedSpellGroup.FireMode, "queued", 2, "FireMode.single"), // 단발 이후 예약 방식
            N(CraftedSpellGroup.FireMode, "conditional", 2, "FireMode.queued"), // 예약 이후 조건부 방식
            N(CraftedSpellGroup.FireMode, "summon", 3, "FireMode.placed", "Shape.summon"), // 설치와 소환탄 이후 소환 방식
            N(CraftedSpellGroup.Origin, "front", 0), // 초기 플레이어 전방
            N(CraftedSpellGroup.Origin, "around", 0), // 초기 플레이어 주변
            N(CraftedSpellGroup.Origin, "rear", 1, "Origin.front"), // 전방 이후 빗자루 후방
            N(CraftedSpellGroup.Origin, "target", 2, "Origin.point"), // 지정 좌표 이후 목표 주변
            N(CraftedSpellGroup.Origin, "point", 1, "Origin.front"), // 전방 이후 지정 좌표
            N(CraftedSpellGroup.Origin, "path", 1, "Origin.rear"), // 후방 이후 비행 경로
            N(CraftedSpellGroup.Origin, "summon", 3, "Shape.summon"), // 소환탄 이후 소환체 위치
            N(CraftedSpellGroup.Origin, "projectile", 2, "Origin.point"), // 지정 좌표 이후 기존 탄환 위치
            N(CraftedSpellGroup.Aim, "manual", 0), // 초기 수동 조준
            N(CraftedSpellGroup.Aim, "auto", 2, "Aim.manual"), // 수동 이후 자동 조준
            new FlightCraftedResearchNode(CraftedSpellGroup.Aim, "homing", 2, true, "Trajectory.homing"), // 유도 궤적의 무료 연동 조준
            N(CraftedSpellGroup.Effect, "none", 0), // 항상 선택 가능한 효과 없음
            N(CraftedSpellGroup.Effect, "knockback", 0), // 초기 넉백
            N(CraftedSpellGroup.Effect, "delay", 1, "Effect.explode"), // 폭발 이후 지연 발동
            N(CraftedSpellGroup.Effect, "slow", 1, "Effect.knockback"), // 넉백 이후 둔화
            N(CraftedSpellGroup.Effect, "retarget", 2, "Trajectory.homing"), // 유도 궤적 이후 추적 전환
            N(CraftedSpellGroup.Effect, "pierce", 1, "Shape.needle"), // 침탄 이후 관통
            N(CraftedSpellGroup.Effect, "burn", 1, "Effect.explode"), // 폭발 이후 화상
            N(CraftedSpellGroup.Effect, "root", 2, "Effect.slow"), // 둔화 이후 속박
            N(CraftedSpellGroup.Effect, "dot", 1, "Effect.burn"), // 화상 이후 지속 피해
            N(CraftedSpellGroup.Effect, "linger", 1, "Effect.dot"), // 지속 피해 이후 잔류
            N(CraftedSpellGroup.Effect, "explode", 0), // 초기 폭발
            N(CraftedSpellGroup.Effect, "stack-explosion", 2, "Effect.burn", "Effect.explode"), // 화상과 폭발 이후 중첩 폭발
            N(CraftedSpellGroup.Effect, "freeze", 2, "Effect.slow"), // 둔화 이후 빙결
            N(CraftedSpellGroup.Effect, "pull", 2, "Effect.knockback"), // 넉백 이후 끌어당기기
            N(CraftedSpellGroup.Effect, "chain", 3, "Effect.pierce", "Trajectory.homing"), // 관통과 유도 이후 연쇄
            N(CraftedSpellGroup.Effect, "cancel", 0), // 초기 탄막 상쇄
            N(CraftedSpellGroup.Effect, "reflect", 3, "Effect.cancel", "Trajectory.bounce"), // 상쇄와 반사 궤적 이후 탄막 반사
            N(CraftedSpellGroup.Effect, "slow-bullets", 2, "Effect.slow", "Effect.cancel"), // 둔화와 상쇄 이후 탄막 감속
            N(CraftedSpellGroup.Effect, "teleport", 3, "Trajectory.switch", "Origin.point"), // 전환과 지정 좌표 이후 좌표 전이
            N(CraftedSpellGroup.Effect, "split", 3, "Effect.pierce", "Effect.explode"), // 관통과 폭발 이후 분열
            N(CraftedSpellGroup.Effect, "absorb", 3, "Effect.cancel", "Trajectory.orbit"), // 상쇄와 공전 이후 탄막 흡수
            N(CraftedSpellGroup.Trigger, "immediate", 0), // 초기 즉시 발동
            N(CraftedSpellGroup.Trigger, "hit", 0), // 초기 명중 발동
            N(CraftedSpellGroup.Trigger, "collision", 0), // 초기 충돌 발동
            N(CraftedSpellGroup.Trigger, "time", 0), // 초기 시간 경과 발동
            N(CraftedSpellGroup.Trigger, "distance", 0), // 초기 거리 도달 발동
            N(CraftedSpellGroup.Trigger, "approach", 1, "Trigger.collision"), // 충돌 이후 적 접근 발동
            N(CraftedSpellGroup.Trigger, "kill", 2, "Trigger.hit"), // 명중 이후 적 처치 발동
            N(CraftedSpellGroup.Trigger, "cancel", 2, "Effect.cancel"), // 상쇄 효과 이후 상쇄 발동
            N(CraftedSpellGroup.Trigger, "repeat", 2, "Trigger.time") // 시간 경과 이후 재입력 발동
        });

        internal static IReadOnlyList<FlightCraftedResearchNode> All => nodes; // 공용 잠금과 연구 화면의 같은 목록

        internal static FlightCraftedResearchNode Find(CraftedSpellGroup group, string id) // 분류와 요소의 유효한 연구 노드
        {
            foreach (FlightCraftedResearchNode node in nodes) // 확정된 전체 구성 요소
            {
                if (node.Group == group && node.Id == id) // 분류까지 일치하는 안정 ID
                {
                    return node; // 해당 요소의 연구 조건
                }
            }
            return null; // 존재하지 않는 연구 요청
        }

        internal static FlightCraftedResearchNode FindKey(string key) // 선행 조건에 저장한 분류별 ID
        {
            foreach (FlightCraftedResearchNode node in nodes) // 확정된 전체 구성 요소
            {
                if (node.Key == key) // 분류와 요소가 모두 일치
                {
                    return node; // 실제 선행 연구 정보
                }
            }
            return null; // 잘못된 선행 ID 차단
        }
    }
}
