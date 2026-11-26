using System; // 런타임의 조합 규칙 형식 조회
using System.Collections; // 제작 카드 목록 순회
using NUnit.Framework; // 조합과 경계 단언
using UnityEngine; // 비행 공간의 실제 방향과 각도

namespace ProjectG.Tests // 제작 마법 규칙 검증
{
    public sealed class CraftedSpellRulesTests // 제작 조합·비용·상한 시험
    {
        private static Type Runtime(string name) // 게임 어셈블리의 실제 규칙 형식
        {
            Type type = Type.GetType("ProjectG." + name + ", Assembly-CSharp"); // 기존 테스트 어셈블리와 분리된 게임 형식
            Assert.That(type, Is.Not.Null, "제작 마법 런타임 " + name); // 실제 게임 규칙 존재 확인
            return type; // 메서드 호출 대상
        }

        [Test] // 기획서의 15종과 첫 유도 시험 조합
        public void ReferenceCardsMatchFixedCostsAndRoundTrip() // 비용 표와 구성 ID 복원
        {
            int[] costs = { 20, 20, 29, 23, 22, 24, 28, 18, 36, 24, 38, 36, 40, 20, 32, 20 }; // 기획서 15종과 유도 시험탄의 고정 비용
            Type rules = Runtime("CraftedSpellRules"); // 실제 비용과 저장 검증
            Type presets = Runtime("CraftedSpellPresets"); // 편집 화면과 공유하는 시험 도감
            var choices = (IEnumerable)presets.GetProperty("All").GetValue(null); // 15종과 유도 시험 카드 조회
            var read = rules.GetMethod("TryRead"); // 구성 ID에서 동일 조합 복원
            int i = 0; // 비교할 원래 배열 위치
            foreach (object preset in choices) // 실제 참조 카드마다 검증
            {
                object recipe = preset.GetType().GetProperty("Recipe").GetValue(preset); // 검증된 제작 구성
                Type type = recipe.GetType(); // 보유 조합의 읽기 전용 속성
                string id = (string)type.GetProperty("Id").GetValue(recipe); // 재실행할 구성 식별자
                Assert.That(type.GetProperty("Cost").GetValue(recipe), Is.EqualTo(costs[i]), id); // 문서 값과 비용 일치
                object[] args = { id, null }; // 구성 ID와 파싱 결과
                Assert.That(read.Invoke(null, args), Is.True, id); // 같은 ID의 재현
                Assert.That(type.GetProperty("Id").GetValue(args[1]), Is.EqualTo(id)); // 정규화 형식 유지
                i++; // 다음 참조 카드
            }
            Assert.That(i, Is.EqualTo(costs.Length)); // 모든 참조 구성 포함
        }

        [TestCase("firstEffect=burn;secondEffect=burn", "DuplicateEffect")] // 동일 효과는 비용과 무관하게 거절
        [TestCase("shape=laser;trajectory=orbit", "Incompatible")] // 광선이 표적을 공전하는 조합 거절
        [TestCase("shape=barrier;trajectory=homing", "Incompatible")] // 고정 결계에 유도 이동 거절
        [TestCase("shape=placed;trajectory=return", "Incompatible")] // 설치탄의 자동 회귀 거절
        [TestCase("firstEffect=absorb;secondEffect=reflect", "Incompatible")] // 동일 적탄을 흡수와 반사하는 조합 거절
        [TestCase("aim=homing", "Incompatible")] // 유도 조준과 직선 궤적의 불일치
        [TestCase("initialShots=25", "InitialLimit")] // 최초 생성 상한 초과
        [TestCase("firstEffect=split;splitChildren=4;splitDepth=1", "SplitLimit")] // 모탄당 자탄 상한 초과
        [TestCase("firstEffect=split;splitChildren=3;splitDepth=2", "SplitLimit")] // 두 단계 재귀 분열 거절
        public void UnsafeOrIncompatibleCompositionsAreRejected(string fields, string issue) // 잘못된 제작 조합의 구체적 거절 사유
        {
            Assert.That(Compose(fields).Issue, Is.EqualTo(issue)); // 검증 실패 사유 일치
        }

        [Test] // 기능 중복 비용과 정규화
        public void HomingAndOrderedEffectsShareTheDocumentRules() // 동일 구성의 순서와 비용 비교
        {
            var homing = Compose("shape=needle;trajectory=homing;aim=homing"); // 무료 연동 유도 조준
            Assert.That(homing.Issue, Is.EqualTo("None")); // 유효 유도형
            Assert.That(Value(homing.Recipe, "Cost"), Is.EqualTo(20)); // 침탄 12와 유도 궤적 8
            var original = Compose("firstEffect=delay;secondEffect=explode"); // 지연과 폭발
            var reorder = Compose("firstEffect=explode;secondEffect=delay"); // 순서만 바꾼 같은 구성
            Assert.That(Value(reorder.Recipe, "Id"), Is.EqualTo(Value(original.Recipe, "Id"))); // 동일 기능 ID 일치
            var split = Compose("initialShots=24;firstEffect=split;origin=projectile;splitChildren=3;splitDepth=1"); // 분열 96발 경계와 기존 탄환 위치
            Assert.That(Value(split.Recipe, "Cost"), Is.EqualTo(26)); // 구체탄 10과 분열 16
            Assert.That(Value(split.Recipe, "MaximumShots"), Is.EqualTo(96)); // 최초 24와 자탄 72의 합
            var spiral = Compose("formation=spiral;fireMode=burst"); // 배열과 발사 방식의 순차 발사
            Assert.That(Value(spiral.Recipe, "Cost"), Is.EqualTo(16)); // 구체탄 10과 나선 6의 단일 비용
        }

        [Test] // 시전·초당·활성 한도를 각각 검사
        public void EmissionAndLoadoutLimitsRespectMinMaxEdges() // 발사체 경계와 두 장 동일 마법 장착
        {
            Type rules = Runtime("CraftedSpellRules"); // 공통 한도 함수
            var emit = rules.GetMethod("CanEmit"); // 생성 전 현재 사용량 검사
            Assert.That(emit.Invoke(null, new object[] { 1, 95, 39, 127 }), Is.True); // 96·40·128 모두 허용
            Assert.That(emit.Invoke(null, new object[] { 1, 96, 39, 127 }), Is.False); // 시전당 97 거절
            Assert.That(emit.Invoke(null, new object[] { 1, 95, 40, 127 }), Is.False); // 1초 내 41 거절
            Assert.That(emit.Invoke(null, new object[] { 1, 95, 39, 128 }), Is.False); // 활성 129 거절
            object magic = Compose("firstEffect=explode").Recipe; // 구성 비용 20
            var fits = rules.GetMethod("CanEquip"); // 편성 비용 범위 검사
            Assert.That(fits.Invoke(null, new[] { magic, magic, 39 }), Is.False); // 같은 마법이어도 두 비용 합산
            Assert.That(fits.Invoke(null, new[] { magic, magic, 40 }), Is.True); // 40 경계에서 허용
            Assert.That(fits.Invoke(null, new object[] { null, null, 0 }), Is.True); // 빈 편성은 비용 없음
        }

        [TestCase("water", "fire", 1.5f)] // 물이 화염에 유리
        [TestCase("fire", "water", 0.75f)] // 반대 상성 적용
        [TestCase("dark", "light", 1.5f)] // 어둠과 빛의 상호 약점
        [TestCase("light", "dark", 1.5f)] // 빛과 어둠의 상호 약점
        [TestCase("fire", null, 1f)] // 방어 속성이 없는 대상
        public void ElementChangesOnlyItsDamageMultiplier(string attack, string defense, float expected) // 실제 배율 표
        {
            var multiplier = Runtime("CraftedSpellRules").GetMethod("DamageMultiplier"); // 게임에서 사용할 상성 계산
            Assert.That(multiplier.Invoke(null, new object[] { attack, defense }), Is.EqualTo(expected)); // 기획서 수치 일치
        }

        [Test] // 확정된 오 속성 상성과 미지정 대상 전체 검증
        public void EveryCraftedElementMatchesTheConfirmedDamageTable() // 공격 행과 방어 열의 스물다섯 조합
        {
            string[] elements = { "fire", "water", "grass", "light", "dark" }; // 기획서의 공격과 방어 순서
            float[,] expected = { { 1f, 0.75f, 1.5f, 1f, 1f }, { 1.5f, 1f, 0.75f, 1f, 1f }, { 0.75f, 1.5f, 1f, 1f, 1f }, { 1f, 1f, 1f, 1f, 1.5f }, { 1f, 1f, 1f, 1.5f, 1f } }; // 기획서의 전체 피해 배율
            var multiplier = Runtime("CraftedSpellRules").GetMethod("DamageMultiplier"); // 실전 피해의 공통 상성 계산
            for (int attack = 0; attack < elements.Length; attack++) // 다섯 공격 속성
            {
                for (int defense = 0; defense < elements.Length; defense++) // 다섯 방어 속성
                {
                    Assert.That(multiplier.Invoke(null, new object[] { elements[attack], elements[defense] }), Is.EqualTo(expected[attack, defense])); // 각 공격과 방어의 확정 배율
                }
                Assert.That(multiplier.Invoke(null, new object[] { elements[attack], null }), Is.EqualTo(1f)); // 미지정 방어 속성의 중립 배율
            }
        }

        [Test] // 조합이 실제 3D 발사 방향으로 변환되는지 확인
        public void StraightFanConeAndHomingProduceExpectedVectors() // 3D 발사 방향과 유도 회전 한도
        {
            Type vectors = Runtime("CraftedSpellVectors"); // 마법 편집기와 발사의 공통 벡터
            var direction = vectors.GetMethod("Direction"); // 발사 배열의 초기 방향
            object straight = Compose("shape=orb").Recipe; // 직선 구체탄 한 발
            object fan = Compose("formation=fan;initialShots=3").Recipe; // 세 발의 평면 부채꼴
            object cone = Compose("formation=cone;initialShots=4").Recipe; // 위아래로도 퍼지는 네 발
            var exact = (Vector3)direction.Invoke(null, new[] { straight, (object)Quaternion.identity, 0, 60f }); // 첫 직선 벡터
            var left = (Vector3)direction.Invoke(null, new[] { fan, (object)Quaternion.identity, 0, 60f }); // 부채꼴의 왼쪽 끝
            var right = (Vector3)direction.Invoke(null, new[] { fan, (object)Quaternion.identity, 2, 60f }); // 부채꼴의 오른쪽 끝
            var upper = (Vector3)direction.Invoke(null, new[] { cone, (object)Quaternion.identity, 1, 60f }); // 위쪽으로 퍼지는 원뿔 한 발
            Assert.That(exact, Is.EqualTo(Vector3.forward)); // 중앙 방향 기본 사격
            Assert.That(Vector3.Angle(left, right), Is.EqualTo(60f).Within(0.01f)); // 문서의 전체 부채꼴 각도
            Assert.That(upper.y, Is.EqualTo(0.5f).Within(0.01f)); // 실제 수직 분산
            object homing = Compose("shape=needle;trajectory=homing;aim=homing").Recipe; // 유도 궤적
            var turn = vectors.GetMethod("StepHeading"); // 프레임의 호밍 회전
            var heading = (Vector3)turn.Invoke(null, new[] { homing, (object)Vector3.forward, Vector3.right, 60f, 0.5f }); // 0.5초의 초당 60도 유도 입력
            Assert.That(Vector3.Angle(Vector3.forward, heading), Is.EqualTo(30f).Within(0.01f)); // 화면 경과 시간에 따른 추적 한도
        }

        [Test] // 수압 파동의 수평과 수직 발사 범위
        public void WaterPressureSpreadsAcrossDocumentedHorizontalAndVerticalAngles() // 세 파동의 입체 배열
        {
            Type presets = Runtime("CraftedSpellPresets"); // 문서의 기본 마법 목록
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 고정 프리셋 목록
            object recipe = all[3].GetType().GetProperty("Recipe").GetValue(all[3]); // 수압 파동의 실제 구성
            var direction = Runtime("CraftedSpellVectors").GetMethod("Direction"); // 전투의 실제 발사 방향
            var first = (Vector3)direction.Invoke(null, new[] { recipe, (object)Quaternion.identity, 0, 60f }); // 첫 번째 파동 방향
            var middle = (Vector3)direction.Invoke(null, new[] { recipe, (object)Quaternion.identity, 1, 60f }); // 가운데 파동 방향
            var last = (Vector3)direction.Invoke(null, new[] { recipe, (object)Quaternion.identity, 2, 60f }); // 마지막 파동 방향
            Assert.That(Vector3.Angle(Vector3.ProjectOnPlane(first, Vector3.up), Vector3.ProjectOnPlane(last, Vector3.up)), Is.EqualTo(60f).Within(0.01f)); // 수평 전체 육십 도
            Assert.That(first.y - last.y, Is.EqualTo(2f * Mathf.Sin(10f * Mathf.Deg2Rad)).Within(0.001f)); // 수직 양끝의 십 도 분산
            Assert.That(middle.y, Is.EqualTo(0f).Within(0.001f)); // 가운데 파동의 중립 고도
        }

        [Test] // 기획 수치가 확인된 구현 마법만 실제 발사 허용
        public void CombatCasterRejectsUndocumentedGenericProfiles() // 미지원 조합의 시험값 추측 방지
        {
            Type presets = Runtime("CraftedSpellPresets"); // 기획서의 마법 목록
            var all = (IList)presets.GetProperty("All").GetValue(null); // 검증된 구성 목록
            Type caster = Runtime("FlightCraftedSpellCaster"); // 실제 전투의 제작 발사기
            var supports = caster.GetMethod("Supports", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); // 실제 발사 지원 확인
            foreach (int index in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }) // 이미 판정이 구현된 기획 마법
            {
                object recipe = all[index].GetType().GetProperty("Recipe").GetValue(all[index]); // 정확한 구성 ID
                Assert.That(supports.Invoke(null, new[] { recipe }), Is.True); // 구현된 명시적 시험값 사용
            }
            object generic = Compose("element=fire;shape=orb;trigger=immediate").Recipe; // 수치가 확정되지 않은 유효 조합
            Assert.That(supports.Invoke(null, new[] { generic }), Is.False); // 기존 구체탄 수치의 임의 적용 차단
            object homing = all[15].GetType().GetProperty("Recipe").GetValue(all[15]); // 열다섯 종에 없는 이전 유도 시험탄
            Assert.That(supports.Invoke(null, new[] { homing }), Is.False); // 임시 시험값을 정식 구성으로 제공하지 않음
        }

        [Test] // 아홉 분류 중 생성 위치의 명시적 좌표 처리
        public void CraftedOriginsResolveOnlyExplicitlyAvailableWorldPositions() // 여덟 위치와 미지정 대상 거절
        {
            Type contextType = Runtime("FlightCraftedOriginContext"); // 실제 시전의 위치 모음
            Type originsType = Runtime("FlightCraftedOrigins"); // 출발 위치 선택기
            object context = Activator.CreateInstance(contextType, true); // 아직 지정한 좌표가 없는 컨텍스트
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic; // 내부 위치 처리 API
            var resolve = originsType.GetMethod("TryResolve", hidden); // 입력한 종류의 실제 위치
            object[] absent = { "target", context, null }; // 실제 목표 좌표가 없는 경우
            Assert.That(resolve.Invoke(null, absent), Is.False); // 미지정 좌표를 임의로 만들지 않음
            string[] names = { "front", "around", "rear", "summon", "projectile", "point", "target", "path" }; // 기획서의 여덟 생성 위치
            string[] fields = { "Front", "Around", "Rear", "Summon", "Projectile", "Point", "Target", "Path" }; // 실제 시전 컨텍스트의 대응 좌표
            for (int i = 0; i < names.Length; i++) // 위치별 서로 다른 월드 좌표
            {
                Vector3 position = new Vector3(i + 1f, i * 2f, -i); // 다른 위치와 구별할 표본
                contextType.GetField(fields[i]).SetValue(context, position); // 해당 위치를 실제로 지정
                object[] args = { names[i], context, null }; // 선택한 생성 위치 조회
                Assert.That(resolve.Invoke(null, args), Is.True); // 명시된 좌표는 사용 가능
                Assert.That((Vector3)args[2], Is.EqualTo(position)); // 정확히 지정한 월드 좌표 반환
            }
            object[] unknown = { "unknown", context, null }; // 도감에 없는 위치 ID
            Assert.That(resolve.Invoke(null, unknown), Is.False); // 잘못된 생성 위치는 실패
            contextType.GetField("Front").SetValue(context, new Vector3(float.NaN, 0f, 0f)); // 계산할 수 없는 월드 좌표
            object[] invalid = { "front", context, null }; // 유효하지 않은 발사점 조회
            Assert.That(resolve.Invoke(null, invalid), Is.False); // 숫자가 아닌 좌표에서는 생성 차단
        }

        [Test] // 여러 시전이 공유하는 생성 수와 원자적 제한
        public void EmissionBudgetEnforcesInitialCastRateAndActiveLimits() // 최초·누적·초당·동시 생성 경계
        {
            Type ledgerType = Runtime("FlightCraftedEmissionBudget"); // 실제 발사기가 쓰는 생성 한도
            Assert.That(ledgerType, Is.Not.Null); // 발사 한도 관리 구현 확인
            object ledger = Activator.CreateInstance(ledgerType, true); // 실전과 같은 독립 한도 관리
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 발사 API
            var begin = ledgerType.GetMethod("TryBegin", hidden); // 새로운 시전 예약
            var emit = ledgerType.GetMethod("TryContinue", hidden); // 같은 시전의 추가 발사 예약
            var reset = ledgerType.GetMethod("Reset", hidden); // 새 출격 초기화
            object[] tooManyInitial = { 25, 0, 0f, null }; // 최초 발사 최대를 한 발 초과
            Assert.That(begin.Invoke(ledger, tooManyInitial), Is.False); // 최초 이십사 발 경계
            object[] first = { 24, 0, 0f, null }; // 최초 이십사 발
            Assert.That(begin.Invoke(ledger, first), Is.True); // 최초 발사 예약 성공
            object ticket = first[3]; // 같은 시전의 누적 기록
            object[] tooFast = { 17, 24, 0f, null }; // 초당 합계 사십일 발
            Assert.That(begin.Invoke(ledger, tooFast), Is.False); // 다른 마법도 초당 제한 공유
            object[] exactRate = { 16, 24, 0f, null }; // 초당 합계 정확히 사십 발
            Assert.That(begin.Invoke(ledger, exactRate), Is.True); // 실패한 예약은 자원을 점유하지 않음
            Assert.That(emit.Invoke(ledger, new object[] { ticket, 24, 40, 1.01f }), Is.True); // 첫 시전의 다음 스물네 발
            Assert.That(emit.Invoke(ledger, new object[] { ticket, 24, 64, 2.02f }), Is.True); // 같은 시전의 추가 스물네 발
            Assert.That(emit.Invoke(ledger, new object[] { ticket, 24, 88, 3.03f }), Is.True); // 누적 정확히 아흔여섯 발
            Assert.That(emit.Invoke(ledger, new object[] { ticket, 1, 112, 4.04f }), Is.False); // 동일 시전 누적 한 발 초과
            object[] activeOverflow = { 24, 105, 5.05f, null }; // 활성 포함 백스물아홉 발
            Assert.That(begin.Invoke(ledger, activeOverflow), Is.False); // 플레이어 동시 활성 상한
            object[] activeEdge = { 23, 105, 5.05f, null }; // 활성 포함 정확히 백스물여덟 발
            Assert.That(begin.Invoke(ledger, activeEdge), Is.True); // 동시 활성 경계 허용
            reset.Invoke(ledger, null); // 새 출격의 모든 생성 기록 초기화
            Assert.That(emit.Invoke(ledger, new object[] { ticket, 1, 0, 6f }), Is.False); // 이전 출격의 시전 기록 무효화
            object[] restarted = { 24, 0, 6f, null }; // 새 출격의 최초 발사
            Assert.That(begin.Invoke(ledger, restarted), Is.True); // 초당 제한도 새 출격에서 초기화
        }

        private static object Value(object recipe, string property) // 검증 결과 속성 읽기
        {
            Assert.That(recipe, Is.Not.Null, "유효한 제작 조합"); // 앞 단계의 조합 성공 확인
            return recipe.GetType().GetProperty(property).GetValue(recipe); // 게임 런타임의 원래 값
        }

        private static (string Issue, object Recipe) Compose(string fields) // 실제 API로 검증할 조합 작성
        {
            Type selectionType = Runtime("CraftedSpellSelection"); // 편집기에서 쓰는 실제 입력
            object selection = Activator.CreateInstance(selectionType); // 문서 기본 구성으로 초기화
            foreach (string entry in fields.Split(';')) // 테스트에 지정한 값만 덮어쓰기
            {
                string[] parts = entry.Split('='); // 바꿀 구성의 이름과 값
                var field = selectionType.GetField(parts[0]); // 실제 구성 속성
                field.SetValue(selection, field.FieldType == typeof(int) ? (object)int.Parse(parts[1]) : parts[1]); // 같은 편집기 입력 형식
            }
            object[] arguments = { selection, null }; // 원본 선택과 검증 결과
            var issue = Runtime("CraftedSpellRules").GetMethod("Compose").Invoke(null, arguments); // 동일 조합 검증기 실행
            return (issue.ToString(), arguments[1]); // 검증 사유와 유효 구성
        }
    }
}
