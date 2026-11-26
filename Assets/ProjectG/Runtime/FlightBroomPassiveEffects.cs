namespace ProjectG // 프로젝트 빗자루 효과 영역
{
    internal sealed class FlightBroomPassiveEffects // 확정된 개별 상시 효과의 상위 두 개 합산
    {
        private readonly struct Effect // 한 부품과 희귀도에서 적용하는 고정 효과
        {
            public readonly string Set; // 기획 세트의 고정 ID
            public readonly FlightBroomSlot Slot; // 기획한 개별 부품 부위
            public readonly FlightBroomStat Stat; // 같은 효과 ID를 구분하는 능력치
            public readonly float Value; // 기획된 실제 가산량
            public readonly int Grade; // 희귀 또는 전설의 최소 발동 등급
            public readonly int Condition; // 무조건·집중·낮은 마력의 구분

            public Effect(string set, FlightBroomSlot slot, FlightBroomStat stat, float value, int grade, int condition = 0) // 기획표의 상시 효과 한 개
            {
                Set = set; // 정확한 세트 구분
                Slot = slot; // 정확한 부품 부위
                Stat = stat; // 가산할 실제 능력치
                Value = value; // 실제 효과 크기
                Grade = grade; // 필요한 최소 희귀도
                Condition = condition; // 같은 능력치의 실제 발동 조건
            }
        }

        private static readonly Effect[] Effects = // 14.10의 무조건 상시 효과만 명시
        {
            new("storm", FlightBroomSlot.Body, FlightBroomStat.Speed, 0.5f, 3), // 질풍 몸체의 일반 속도
            new("storm", FlightBroomSlot.Body, FlightBroomStat.Acceleration, 2f, 5), // 질풍 몸체의 전설 가속
            new("storm", FlightBroomSlot.Grip, FlightBroomStat.Turn, 20f, 3), // 바람 손잡이의 선회
            new("storm", FlightBroomSlot.Grip, FlightBroomStat.Braking, 2f, 5), // 바람 손잡이의 전설 제동
            new("storm", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 5f, 3), // 폭풍 마력석의 최대 마력
            new("storm", FlightBroomSlot.Coupling, FlightBroomStat.Acceleration, 2f, 3), // 하늘길 결합부의 가속
            new("storm", FlightBroomSlot.Coupling, FlightBroomStat.Rise, 0.5f, 5), // 하늘길 결합부의 전설 상승
            new("storm", FlightBroomSlot.Tip, FlightBroomStat.BoltSpeed, 5f, 3), // 바람 선단부의 탄속
            new("storm", FlightBroomSlot.Tip, FlightBroomStat.Range, 5f, 5), // 바람 선단부의 전설 사거리
            new("storm", FlightBroomSlot.Stabilizer, FlightBroomStat.Turn, 15f, 5), // 천풍 안정기의 전설 선회
            new("guard", FlightBroomSlot.Body, FlightBroomStat.ImpactResistance, 3f, 3), // 수호 몸체의 충격 저항
            new("guard", FlightBroomSlot.Grip, FlightBroomStat.Braking, 2f, 5), // 맹세 손잡이의 전설 제동
            new("guard", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 8f, 3), // 성벽 마력석의 최대 마력
            new("guard", FlightBroomSlot.Coupling, FlightBroomStat.ImpactResistance, 2f, 3), // 불굴 결합부의 충격 저항
            new("guard", FlightBroomSlot.Coupling, FlightBroomStat.Stability, 4f, 5), // 불굴 결합부의 전설 안정성
            new("guard", FlightBroomSlot.Propulsion, FlightBroomStat.Braking, 3f, 3), // 수호 추진부의 제동
            new("guard", FlightBroomSlot.Propulsion, FlightBroomStat.Fall, 0.5f, 5), // 수호 추진부의 전설 하강
            new("guard", FlightBroomSlot.Tip, FlightBroomStat.Range, 5f, 3), // 새벽 선단부의 사거리
            new("guard", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 10f, 3), // 수호 안정기의 안정성
            new("flow", FlightBroomSlot.Body, FlightBroomStat.Mana, 5f, 3), // 순환 몸체의 최대 마력
            new("flow", FlightBroomSlot.Body, FlightBroomStat.ManaRecovery, 1f, 5), // 순환 몸체의 전설 회복
            new("flow", FlightBroomSlot.Grip, FlightBroomStat.Turn, 10f, 3), // 고요 손잡이의 선회
            new("flow", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 8f, 3), // 샘솟는 마력석의 최대 마력
            new("flow", FlightBroomSlot.ManaStone, FlightBroomStat.ManaRecovery, 1f, 5), // 샘솟는 마력석의 전설 회복
            new("flow", FlightBroomSlot.Coupling, FlightBroomStat.ManaRecovery, 1f, 3), // 흐름 결합부의 마력 회복
            new("flow", FlightBroomSlot.Coupling, FlightBroomStat.Mana, 4f, 5), // 흐름 결합부의 전설 최대 마력
            new("flow", FlightBroomSlot.Propulsion, FlightBroomStat.Rise, 0.5f, 3), // 수맥 추진부의 상승
            new("flow", FlightBroomSlot.Ornament, FlightBroomStat.Mana, 5f, 5), // 순환 장식의 전설 최대 마력
            new("flow", FlightBroomSlot.Tip, FlightBroomStat.Range, 5f, 3), // 원천 선단부의 사거리
            new("flow", FlightBroomSlot.Stabilizer, FlightBroomStat.ManaRecovery, 0.5f, 5), // 유수 안정기의 전설 회복
            new("starlight", FlightBroomSlot.Body, FlightBroomStat.MagicPower, 3f, 3), // 별자리 몸체의 마법 공격력
            new("starlight", FlightBroomSlot.Body, FlightBroomStat.Range, 3f, 5), // 별자리 몸체의 전설 사거리
            new("starlight", FlightBroomSlot.Grip, FlightBroomStat.Turn, 10f, 3), // 별빛 손잡이의 선회
            new("starlight", FlightBroomSlot.ManaStone, FlightBroomStat.MagicPower, 4f, 3), // 북극성 마력석의 마법 공격력
            new("starlight", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 5f, 5), // 북극성 마력석의 전설 마력
            new("starlight", FlightBroomSlot.Coupling, FlightBroomStat.MagicPower, 2f, 3), // 천체 결합부의 마법 공격력
            new("starlight", FlightBroomSlot.Coupling, FlightBroomStat.Braking, 2f, 5), // 천체 결합부의 전설 제동
            new("starlight", FlightBroomSlot.Ornament, FlightBroomStat.MagicPower, 2f, 5), // 별 장식의 전설 마법 공격력
            new("starlight", FlightBroomSlot.Tip, FlightBroomStat.BoltSpeed, 5f, 3), // 천체 선단부의 탄속
            new("starlight", FlightBroomSlot.Tip, FlightBroomStat.MagicPower, 2f, 5), // 천체 선단부의 전설 마법 공격력
            new("starlight", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 8f, 3), // 별궤도 안정기의 안정성
            new("starlight", FlightBroomSlot.Stabilizer, FlightBroomStat.Range, 5f, 5), // 별궤도 안정기의 전설 사거리
            new("hawk", FlightBroomSlot.Body, FlightBroomStat.Range, 5f, 3), // 매 몸체의 사거리
            new("hawk", FlightBroomSlot.Body, FlightBroomStat.Speed, 0.5f, 5), // 매 몸체의 전설 속도
            new("hawk", FlightBroomSlot.Grip, FlightBroomStat.Braking, 2f, 5), // 사냥꾼 손잡이의 전설 제동
            new("hawk", FlightBroomSlot.ManaStone, FlightBroomStat.BoltSpeed, 5f, 3), // 매안 마력석의 탄속
            new("hawk", FlightBroomSlot.ManaStone, FlightBroomStat.Range, 5f, 5), // 매안 마력석의 전설 사거리
            new("hawk", FlightBroomSlot.Coupling, FlightBroomStat.Turn, 10f, 5), // 정밀 결합부의 전설 선회
            new("hawk", FlightBroomSlot.Propulsion, FlightBroomStat.Fall, 0.5f, 3), // 급강하 추진부의 하강
            new("hawk", FlightBroomSlot.Ornament, FlightBroomStat.Range, 3f, 5), // 사냥 장식의 전설 사거리
            new("hawk", FlightBroomSlot.Tip, FlightBroomStat.BoltSpeed, 7f, 3), // 매 선단부의 탄속
            new("hawk", FlightBroomSlot.Tip, FlightBroomStat.Range, 5f, 5), // 매 선단부의 전설 사거리
            new("hawk", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 6f, 5), // 정밀 안정기의 전설 안정성
            new("shadow", FlightBroomSlot.Body, FlightBroomStat.Speed, 0.5f, 3), // 밤 몸체의 일반 속도
            new("shadow", FlightBroomSlot.Body, FlightBroomStat.Turn, 10f, 5), // 밤 몸체의 전설 선회
            new("shadow", FlightBroomSlot.Grip, FlightBroomStat.Turn, 20f, 3), // 그림자 손잡이의 선회
            new("shadow", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 5f, 5), // 월식 마력석의 전설 최대 마력
            new("shadow", FlightBroomSlot.Coupling, FlightBroomStat.Stability, 6f, 3), // 경계 결합부의 안정성
            new("shadow", FlightBroomSlot.Coupling, FlightBroomStat.Braking, 2f, 5), // 경계 결합부의 전설 제동
            new("shadow", FlightBroomSlot.Tip, FlightBroomStat.BoltSpeed, 5f, 3), // 월광 선단부의 탄속
            new("shadow", FlightBroomSlot.Tip, FlightBroomStat.Range, 5f, 5), // 월광 선단부의 전설 사거리
            new("shadow", FlightBroomSlot.Stabilizer, FlightBroomStat.Turn, 15f, 5), // 밤안개 안정기의 전설 선회
            new("flame", FlightBroomSlot.Body, FlightBroomStat.Speed, 0.5f, 5), // 잿불 몸체의 전설 속도
            new("flame", FlightBroomSlot.Grip, FlightBroomStat.Turn, 10f, 3), // 화승 손잡이의 선회
            new("flame", FlightBroomSlot.ManaStone, FlightBroomStat.MagicPower, 3f, 3), // 불씨 마력석의 마법 공격력
            new("flame", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 5f, 5), // 불씨 마력석의 전설 마력
            new("flame", FlightBroomSlot.Coupling, FlightBroomStat.Acceleration, 2f, 3), // 화로 결합부의 가속
            new("flame", FlightBroomSlot.Coupling, FlightBroomStat.ImpactResistance, 2f, 5), // 화로 결합부의 전설 충격 저항
            new("flame", FlightBroomSlot.Propulsion, FlightBroomStat.Acceleration, 2f, 5), // 홍염 추진부의 전설 가속
            new("flame", FlightBroomSlot.Tip, FlightBroomStat.MagicPower, 2f, 5), // 용화 선단부의 전설 마법 공격력
            new("flame", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 6f, 3), // 열풍 안정기의 안정성
            new("frost", FlightBroomSlot.Body, FlightBroomStat.ImpactResistance, 2f, 5), // 서리 몸체의 전설 충격 저항
            new("frost", FlightBroomSlot.Grip, FlightBroomStat.Turn, 10f, 5), // 한기 손잡이의 전설 선회
            new("frost", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 5f, 3), // 빙벽 마력석의 최대 마력
            new("frost", FlightBroomSlot.ManaStone, FlightBroomStat.MagicPower, 2f, 5), // 빙벽 마력석의 전설 마법 공격력
            new("frost", FlightBroomSlot.Coupling, FlightBroomStat.Braking, 2f, 3), // 동결 결합부의 제동
            new("frost", FlightBroomSlot.Coupling, FlightBroomStat.ImpactResistance, 2f, 5), // 동결 결합부의 전설 충격 저항
            new("frost", FlightBroomSlot.Propulsion, FlightBroomStat.Braking, 3f, 3), // 설풍 추진부의 제동
            new("frost", FlightBroomSlot.Ornament, FlightBroomStat.Mana, 5f, 5), // 설원 장식의 전설 마력
            new("frost", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 8f, 3), // 동토 안정기의 안정성
            new("thunder", FlightBroomSlot.Body, FlightBroomStat.Acceleration, 2f, 5), // 뇌광 몸체의 전설 가속
            new("thunder", FlightBroomSlot.Grip, FlightBroomStat.Turn, 15f, 3), // 번개 손잡이의 선회
            new("thunder", FlightBroomSlot.ManaStone, FlightBroomStat.MagicPower, 3f, 3), // 천뢰 마력석의 마법 공격력
            new("thunder", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 5f, 5), // 천뢰 마력석의 전설 마력
            new("thunder", FlightBroomSlot.Coupling, FlightBroomStat.Acceleration, 2f, 3), // 전류 결합부의 가속
            new("thunder", FlightBroomSlot.Ornament, FlightBroomStat.MagicPower, 2f, 5), // 연쇄 장식의 전설 마법 공격력
            new("thunder", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 6f, 3), // 천둥 안정기의 안정성
            new("thunder", FlightBroomSlot.Stabilizer, FlightBroomStat.Braking, 2f, 5), // 천둥 안정기의 전설 제동
            new("gravity", FlightBroomSlot.Body, FlightBroomStat.Stability, 6f, 5), // 공허 몸체의 전설 안정성
            new("gravity", FlightBroomSlot.Grip, FlightBroomStat.Braking, 2f, 3), // 질량 손잡이의 제동
            new("gravity", FlightBroomSlot.Grip, FlightBroomStat.Turn, 10f, 5), // 질량 손잡이의 전설 선회
            new("gravity", FlightBroomSlot.ManaStone, FlightBroomStat.MagicPower, 3f, 3), // 중력 마력석의 마법 공격력
            new("gravity", FlightBroomSlot.ManaStone, FlightBroomStat.Mana, 5f, 5), // 중력 마력석의 전설 마력
            new("gravity", FlightBroomSlot.Coupling, FlightBroomStat.ImpactResistance, 2f, 3), // 궤도 결합부의 충격 저항
            new("gravity", FlightBroomSlot.Coupling, FlightBroomStat.Stability, 4f, 5), // 궤도 결합부의 전설 안정성
            new("gravity", FlightBroomSlot.Propulsion, FlightBroomStat.Rise, 0.5f, 3), // 반중력 추진부의 상승
            new("gravity", FlightBroomSlot.Propulsion, FlightBroomStat.Fall, 0.5f, 5), // 반중력 추진부의 전설 하강
            new("gravity", FlightBroomSlot.Ornament, FlightBroomStat.MagicPower, 2f, 5), // 특이점 장식의 전설 마법 공격력
            new("gravity", FlightBroomSlot.Tip, FlightBroomStat.BoltSpeed, 5f, 5), // 공간 선단부의 전설 탄속
            new("gravity", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 8f, 3), // 궤도 안정기의 안정성
            new("storm", FlightBroomSlot.Stabilizer, FlightBroomStat.Braking, 3f, 3, 1), // 집중 중 천풍 안정기의 제동
            new("guard", FlightBroomSlot.Grip, FlightBroomStat.Stability, 8f, 3, 1), // 집중 중 맹세 손잡이의 안정성
            new("guard", FlightBroomSlot.Tip, FlightBroomStat.ImpactResistance, 2f, 5, 1), // 집중 중 새벽 선단부의 전설 충격 저항
            new("guard", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 6f, 5, 1), // 집중 중 수호 안정기의 전설 안정성
            new("flow", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 8f, 3, 1), // 집중 중 유수 안정기의 안정성
            new("starlight", FlightBroomSlot.Grip, FlightBroomStat.MagicPower, 2f, 5, 1), // 집중 중 별빛 손잡이의 전설 마법 공격력
            new("hawk", FlightBroomSlot.Grip, FlightBroomStat.Turn, 15f, 3, 1), // 집중 중 사냥꾼 손잡이의 선회
            new("hawk", FlightBroomSlot.Coupling, FlightBroomStat.Stability, 8f, 3, 1), // 집중 중 정밀 결합부의 안정성
            new("hawk", FlightBroomSlot.Stabilizer, FlightBroomStat.Braking, 3f, 3, 1), // 집중 중 정밀 안정기의 제동
            new("shadow", FlightBroomSlot.Grip, FlightBroomStat.Braking, 2f, 5, 1), // 집중 중 그림자 손잡이의 전설 제동
            new("shadow", FlightBroomSlot.Stabilizer, FlightBroomStat.Stability, 8f, 3, 1), // 집중 중 밤안개 안정기의 안정성
            new("frost", FlightBroomSlot.Grip, FlightBroomStat.Stability, 8f, 3, 1), // 집중 중 한기 손잡이의 안정성
            new("frost", FlightBroomSlot.Stabilizer, FlightBroomStat.Braking, 2f, 5, 1), // 집중 중 동토 안정기의 전설 제동
            new("gravity", FlightBroomSlot.Stabilizer, FlightBroomStat.Braking, 3f, 5, 1), // 집중 중 궤도 안정기의 전설 제동
            new("flow", FlightBroomSlot.Grip, FlightBroomStat.Braking, 2f, 5, 2) // 마력 30 이하의 고요한 손잡이 전설 제동
        };

        private readonly float[,] first = new float[3, System.Enum.GetValues(typeof(FlightBroomStat)).Length]; // 조건과 능력치별 가장 큰 효과
        private readonly float[,] second = new float[3, System.Enum.GetValues(typeof(FlightBroomStat)).Length]; // 조건과 능력치별 두 번째 효과

        internal void Include(FlightBroomPart part, int grade) // 실제 장착한 한 사본의 희귀도별 효과
        {
            foreach (Effect effect in Effects) // 기획된 상시 효과 목록
            {
                if (effect.Set != part.setId || effect.Slot != part.slot || grade < effect.Grade) // 다른 부품 또는 미달 등급
                {
                    continue; // 실제로 발동하지 않는 효과
                }
                int stat = (int)effect.Stat; // 같은 능력치와 무조건 발동의 효과 ID
                int condition = effect.Condition; // 같은 능력치라도 분리할 발동 조건
                if (effect.Value > first[condition, stat]) // 현재 가장 큰 효과를 넘는 경우
                {
                    second[condition, stat] = first[condition, stat]; // 기존 최대를 두 번째로 이동
                    first[condition, stat] = effect.Value; // 더 큰 효과를 첫 번째로 보존
                }
                else if (effect.Value > second[condition, stat]) // 두 번째보다 큰 유효 효과
                {
                    second[condition, stat] = effect.Value; // 같은 ID의 두 번째 효과 보존
                }
            }
        }

        internal void AddTo(float[] result, bool focused, bool lowMana) // 실제 조건에 맞는 개별 효과 반영
        {
            for (int i = 0; i < result.Length; i++) // 모든 실제 능력치
            {
                result[i] += first[0, i] + second[0, i]; // 무조건 효과의 가장 큰 두 개
                if (focused) // 현재 실제 집중 비행 중인 경우
                {
                    result[i] += first[1, i] + second[1, i]; // 집중 조건의 별도 상위 두 개
                }
                if (lowMana) // 현재 실제 마력이 30 이하인 경우
                {
                    result[i] += first[2, i] + second[2, i]; // 낮은 마력 조건의 별도 상위 두 개
                }
            }
        }
    }
}
