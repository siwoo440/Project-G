using System.Collections.Generic; // 대상별 독립 상태와 중첩 관리
using UnityEngine; // 프레임 시간과 실제 피해 적용

namespace ProjectG // 프로젝트 게임 영역
{
    [DisallowMultipleComponent] // 같은 피해 대상의 상태 관리자 한 개
    internal sealed class FlightCraftedStatus : MonoBehaviour // 제작 마법의 지속 피해 상태
    {
        private sealed class Effect // 독립 만료되는 한 중첩
        {
            public string key; // 중복과 중첩을 구분하는 효과 ID
            public string element; // 시전 때 확정한 공격 속성
            public float damage; // 각 틱의 시전 시점 피해
            public float interval; // 틱 사이의 실제 시간
            public float elapsed; // 다음 틱까지 누적한 시간
            public int remaining; // 아직 적용하지 않은 틱 수
            public bool practice; // 일시정지와 독립된 연습 여부
        }

        private readonly List<Effect> effects = new(); // 현재 대상의 독립 중첩 목록
        private FlightEnemy enemy; // 실제 전투의 체력과 상성
        private FlightCraftedTarget target; // 정산과 분리된 연습 표적

        private void Awake() // 이 오브젝트의 실제 피해 경로 확인
        {
            TryGetComponent(out enemy); // 실전 마물 참조
            TryGetComponent(out target); // 독립 연습 표적 참조
        }

        internal bool TryApply(string key, string element, float damage, float interval, int ticks, int maxStacks, bool practice) // 수치가 확정된 지속 피해 등록
        {
            if (string.IsNullOrEmpty(key) || (enemy == null && target == null) || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) // 유효한 효과와 대상 확인
            {
                return false; // 대상이나 피해가 없으면 상태를 추가하지 않음
            }
            if (float.IsNaN(interval) || float.IsInfinity(interval) || interval < 0.5f || ticks < 1 || interval * ticks > 6f || maxStacks < 1 || maxStacks > 3) // 틱 간격·지속·중첩 상한
            {
                return false; // 문서의 공통 경계를 벗어난 효과 거절
            }
            int stacks = 0; // 같은 효과의 현재 중첩 수
            foreach (Effect effect in effects) // 아직 만료되지 않은 상태
            {
                if (effect.key == key) // 동일 효과의 독립 중첩
                {
                    stacks++; // 기존 중첩 수 증가
                }
            }
            if (stacks >= maxStacks) // 같은 효과의 허용 중첩 도달
            {
                return false; // 기존 지속 시간을 갱신하지 않음
            }
            effects.Add(new Effect { key = key, element = element, damage = damage, interval = interval, remaining = ticks, practice = practice }); // 첫 틱을 기다리는 독립 중첩
            return true; // 새로운 상태 한 개 적용
        }

        internal bool TryKnockback(Vector3 direction, float distance) // 실제 피해가 발생한 대상의 강제 이동
        {
            if (enemy != null && enemy.TryGetComponent(out FlightStoryCombatant story)) return story.TryKnockback(direction, distance);
            if (enemy == null || enemy.Health <= 0 || enemy.LootTier != FlightLootTier.Normal || direction.sqrMagnitude < 0.0001f || distance <= 0f) // 일반 마물과 유효한 이동 확인
            {
                return false; // 보스와 사라진 대상은 강제로 움직이지 않음
            }
            if (!enemy.TryGetComponent(out FlightEnemyRoute route)) // 이동 경로가 없는 대상
            {
                return false; // 편집된 경로 없이 위치를 바꾸지 않음
            }
            route.ApplyCraftedKnockback(direction.normalized * distance * (enemy.IsElite ? 0.5f : 1f)); // 정예 저항과 지형 제한을 적용한 이동
            return true; // 일반·정예 마물의 유효한 제어 적용
        }

        internal bool TryRoot(float duration) // 실제 일반 마물의 이동 속박
        {
            if (enemy != null && enemy.TryGetComponent(out FlightStoryCombatant story)) return story.TryRoot(duration);
            if (enemy == null || enemy.Health <= 0 || enemy.LootTier != FlightLootTier.Normal || float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f) // 유효한 일반 적과 속박 시간
            {
                return false; // 보스와 유효하지 않은 제어 차단
            }
            if (!enemy.TryGetComponent(out FlightEnemyRoute route)) // 이동 경로가 없는 마물
            {
                return false; // 실제 이동이 없는 대상은 제어하지 않음
            }
            route.ApplyCraftedRoot(duration * (enemy.IsElite ? 0.5f : 1f)); // 정예 저항을 적용한 이동 시간 정지
            return true; // 유효한 일반·정예 마물 속박
        }

        private void Update() // 유효한 상태의 시간과 피해 갱신
        {
            for (int i = effects.Count - 1; i >= 0; i--) // 만료 상태 삭제에 안전한 역순
            {
                Effect effect = effects[i]; // 대상의 현재 독립 중첩
                float delta = effect.practice ? FlightCraftedSpellCaster.PracticeDelta : Time.deltaTime; // 연습과 실전의 독립 시간
                effect.elapsed += delta; // 이 중첩의 다음 틱까지 경과
                while (effect.remaining > 0 && effect.elapsed + 0.00001f >= effect.interval) // 지연된 프레임의 유효 틱 처리
                {
                    effect.elapsed -= effect.interval; // 해당 틱의 시간 차감
                    effect.remaining--; // 남은 유효 피해 횟수 감소
                    ApplyDamage(effect); // 현재 대상의 상성과 실제 체력 처리
                    if (!isActiveAndEnabled) // 피해로 마물이 처치되어 회수된 경우
                    {
                        return; // 회수 시 초기화된 목록을 다시 읽지 않음
                    }
                }
                if (effect.remaining == 0) // 마지막 유효 틱까지 적용 완료
                {
                    effects.RemoveAt(i); // 이 중첩만 독립적으로 만료
                }
            }
        }

        private void ApplyDamage(Effect effect) // 실전 또는 연습의 실제 피해 반영
        {
            if (enemy != null) // 실전 마물과 보스
            {
                int damage = Mathf.RoundToInt(effect.damage * CraftedSpellRules.DamageMultiplier(effect.element, enemy.DefenseElement)); // 틱마다 실제 방어 속성 적용
                enemy.ReceiveHit(damage); // 기존 무적과 보스 경계를 통과하는 피해
            }
            else if (target != null) // 별도 연습 표적
            {
                int damage = Mathf.RoundToInt(effect.damage * CraftedSpellRules.DamageMultiplier(effect.element, target.Element)); // 연습의 같은 속성 판정
                target.Receive(damage); // 실제 누적 피해 기록
            }
        }

        internal void Clear() // 연습 초기화와 대상 회수의 공통 상태 제거
        {
            effects.Clear(); // 이전 피해와 중첩을 남기지 않음
        }

        private void OnDisable() // 적이 회수되거나 연습이 종료된 경우
        {
            Clear(); // 새 출현에 이전 피해와 중첩을 넘기지 않음
        }
    }
}
