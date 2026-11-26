using System.Collections; // 프레임 대기
using NUnit.Framework; // 시험 단언
using UnityEngine; // 좌표와 씬 개체
using UnityEngine.InputSystem; // 가상 키보드
using UnityEngine.InputSystem.LowLevel; // 키 입력 상태
using UnityEngine.SceneManagement; // 실제 씬 로드
using UnityEngine.TestTools; // 플레이 모드 테스트
using UnityEngine.UI; // 메인 화면 버튼과 음량 슬라이더

namespace ProjectG.Tests // 프로젝트 검증 영역
{
    public sealed class FirstFlightPlayTests // 실제 입력과 추적 카메라 시험
    {
        private const string ScenePath = "Assets/Scenes/FlightPrototype.unity"; // 검증 대상 씬 경로
        private const string OuterIslandsPath = "Assets/Scenes/OuterIslandsStage.unity"; // Windows 출격용 하늘섬 외곽 항로
        private const string VillagePath = "Assets/Scenes/FlightIslandVillage.unity"; // 착륙장부터 출격소까지의 마을 씬
        private Keyboard keyboard; // 테스트 전용 가상 키보드
        private Mouse mouse; // 테스트 전용 가상 마우스
        private float previousCaptureStep; // 원래 프레임 시험 시간
        private InputSettings.BackgroundBehavior backgroundBehavior; // 원래 입력 포커스 설정
        private InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior; // 에디터 입력 대상의 원래 설정
        private bool runInBackground; // 원래 백그라운드 실행 설정

        [SetUp] // 입력 장치 추가
        public void AddKeyboard() // 시스템 키보드와 분리된 입력
        {
            backgroundBehavior = InputSystem.settings.backgroundBehavior; // 이전 입력 포커스 설정 보관
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 원래 에디터 입력 대상 보관
            runInBackground = Application.runInBackground; // 원래 백그라운드 실행 상태 보관
            Application.runInBackground = true; // 포커스와 별개로 테스트 프레임 진행
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 헤드리스 입력을 게임에 전달
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 그래픽 없는 배치 시험 입력 허용
            keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
            mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
            previousCaptureStep = Time.captureDeltaTime; // 원래 시간 설정 저장
            Time.captureDeltaTime = 1f / 60f; // 입력과 이동의 프레임 시간 고정
        }

        [TearDown] // 입력 장치 정리
        public void RemoveKeyboard() // 다음 시험의 입력 상태 복원
        {
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
            Time.captureDeltaTime = previousCaptureStep; // 시간 설정 복원
            InputSystem.settings.backgroundBehavior = backgroundBehavior; // 시험 전 입력 포커스 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior; // 시험 전 에디터 입력 대상 복원
            Application.runInBackground = runInBackground; // 시험 전 백그라운드 실행 복원
        }

        [UnityTest] // 일곱 조종사의 출격과 전용 성장, 다시 출격 연결
        public IEnumerator SevenPilotsKeepEquipmentChoicesAndResultsIsolated() // 같은 외곽 항로에서 실제 조종사별 조건 비교
        {
            string[] names = { "Aria", "Rian", "Lumi", "Sera", "Nox", "Vera", "Iris" }; // 실제 선택 버튼과 저장 이름
            string[] firstNodes = { "SlowDecay", "Weak", "LumiTracking", "SeraApproach", "NoxPrecision", "VeraDuration", "IrisRange" }; // 캐릭터 전용 첫 분기
            string[] secondNodes = { "Wide", "SniperPierce", "LumiSpread", "SeraWidth", "NoxPierce", "VeraCadence", "IrisFocus" }; // 다른 전용 첫 분기
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 게임 카드 목록의 접근 위치
            for (int index = 0; index < names.Length; index++) // 일곱 조종사에게 같은 실제 화면 경로
            {
                string actor = names[index]; // 이번 조종사의 이름
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 현재 플레이어가 쓰는 출격 화면
                yield return WaitFrames(3); // 씬의 실제 선택과 장비 미리보기
                Transform ready = GameObject.Find("SortieBriefing").transform; // 실제 캐릭터 선택 하이러키
                ClickPauseButton(ready, "Ready/Select" + actor); // 일곱 버튼 모두 실제 선택
                Assert.That(ready.Find("Ready/Portrait").GetComponent<RawImage>().texture.name, Is.EqualTo(actor + "Standing"), actor); // 다른 조종사의 스탠딩 혼입 방지
                ClickPauseButton(ready, "Ready/Depart"); // 실제 조종사의 바인딩과 슬롯으로 비행
                yield return WaitForActiveScene(OuterIslandsPath); // 실제 배포 전투에 진입
                yield return WaitFrames(2); // 첫 전투의 배치와 카드 상태 적용
                Transform pilot = FindPilot(); // 이 출격의 실제 기체
                Component weapon = pilot.GetComponent("FlightWeapon"); // 조종사와 공격
                Assert.That(weapon.GetType().GetProperty("ActivePilot").GetValue(weapon).ToString(), Is.EqualTo(actor)); // 결과까지 이어질 실제 조종사
                Transform models = pilot.Find("PilotModels"); // 씬에 미리 배치된 편집 가능한 조종사 모형
                Assert.That(models, Is.Not.Null, actor); // 모든 선택의 실제 비행 모형 묶음
                Assert.That(models.childCount, Is.EqualTo(names.Length), actor); // 배열로 참조할 일곱 3D 변형
                foreach (Transform model in models) // 다른 캐릭터의 외형 혼입을 방지
                {
                    Assert.That(model.gameObject.activeSelf, Is.EqualTo(model.name == actor), model.name); // 출격한 조종사 외형만 렌더
                }
                Assert.That(models.Find(actor + "/Mantle/OuterCloth").GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("FoldedMantle"), actor); // 실제 메시가 연결된 의상
                Component skills = pilot.GetComponent("FlightSkillController"); // 현재 기체의 장착 마력
                bool reserve = actor is "Aria" or "Lumi" or "Vera" or "Iris"; // 마력석 또는 장식이 더하는 5점
                bool recovery = actor is "Aria" or "Lumi" or "Nox" or "Vera"; // 장식이 더하는 초당 0.5점
                Assert.That(Metric(skills, "MaxMana"), Is.EqualTo(reserve ? 105 : 100), actor); // 실전의 실제 마력 상한
                Assert.That(Metric(skills, "Mana"), Is.EqualTo(Metric(skills, "MaxMana")), actor); // 출발 시 조합의 마력 최대 충전
                Assert.That(skills.GetType().GetProperty("ManaRecovery").GetValue(skills), Is.EqualTo(recovery ? 6.5f : 6f), actor); // 실제 초당 회복량
                Component gear = pilot.GetComponent("FlightBroomEquipment"); // 조종사별 빗자루 여덟 부위
                Assert.That(gear.GetType().GetProperty("ActivePilot").GetValue(gear).ToString(), Is.EqualTo(actor)); // 각 조종사의 독립 장비
                Assert.That(BroomAction(gear, "GetEquippedId", actor, "Body").ToString(), Does.StartWith(actor.ToLowerInvariant() + "."), actor); // 다른 조종사의 시작 장비 침범 없음
                Transform combat = LocateCombatHud().transform; // 씬의 기능별 HUD
                Component choices = combat.GetComponent("FlightUpgradeChoices"); // 실제 레벨업의 공용·전용 후보
                choices.GetType().GetMethod("CollectCandidates", hidden).Invoke(choices, null); // 실제 추첨 직전 동일 후보 생성
                var available = (IEnumerable)choices.GetType().GetField("candidates", hidden).GetValue(choices); // 필터 후 현 출격의 카드 목록
                var nodes = new System.Collections.Generic.HashSet<string>(); // 공용과 전용 노드를 확인할 실제 목록
                foreach (object card in available) // 현재 후보가 실제 캐릭터에만 등장하는지 확인
                {
                    int family = (int)card.GetType().GetField("pilot").GetValue(card); // 카드의 전용 조종사 식별자
                    Assert.That(family == 0 || family == index + 1, Is.True, actor); // 다른 조종사 카드 제외
                    nodes.Add(card.GetType().GetField("reward").GetValue(card).ToString()); // 현재 레벨에서 실제 뽑힐 강화
                }
                Assert.That(nodes.Contains(firstNodes[index]) && nodes.Contains(secondNodes[index]) && nodes.Contains("Damage"), Is.True, actor); // 두 전용 첫 분기와 같은 공용 화력
                Assert.That(nodes.Contains("ChaseSecond") || nodes.Contains("SpreadSecond") || nodes.Contains("WeakSecond"), Is.False, actor); // 같은 가지 첫 노드가 없으면 후속 제한
                Component items = GameObject.Find("EnhancementDrops").GetComponent("FlightEnhancements"); // 공용 강화 아이템의 실제 수치
                var applyItem = items.GetType().GetMethod("TryApply", hidden); // 픽업 밖에서도 동일한 강화 규칙
                object damage = System.Enum.Parse(applyItem.GetParameters()[0].ParameterType, "Damage"); // 단계별 공용 아이템 효과
                Assert.That(applyItem.Invoke(items, new[] { damage, (object)3 }), Is.True, actor); // 공용 아이템은 일곱 명에서 실제 적용
                var scale = choices.GetType().GetMethod("DamageScale"); // 공용 아이템의 최종 공격 배율
                Assert.That((float)scale.Invoke(choices, new object[] { false }), Is.GreaterThan(1f), actor); // 단계가 공격 수치에 연결
                if (actor == "Rian") // 전용 카드가 실제 HUD 수치를 바꾸는 회귀
                {
                    var apply = choices.GetType().GetMethod("Apply", hidden); // 카드 클릭 이후 실제 강화 계산
                    object reward = System.Enum.Parse(apply.GetParameters()[0].ParameterType, "Weak"); // 약점 사냥 1의 레벨업 카드
                    apply.Invoke(choices, new[] { reward }); // 실제 피해 배율을 65%로 높임
                    yield return WaitFrames(2); // 보너스의 화면 갱신
                    Component display = combat.GetComponent("FlightCombatHud"); // 약점 보너스가 보이는 실제 HUD
                    Text readout = (Text)display.GetType().GetField("rapidLabel", hidden).GetValue(display); // 공유 패시브 글자
                    Assert.That(readout.text, Does.Contain("65%"), "리안 약점 사냥의 실제 피해와 같은 안내"); // 잘못된 고정 50% 방지
                }
                Component encounter = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 실제 참여를 기록하는 스테이지
                yield return new WaitForSeconds(3.3f); // 첫 웨이브와 정상 출격 참여
                Assert.That(Metric(encounter, "WaveNumber"), Is.GreaterThan(0), actor); // 기록이 생긴 실제 출격만 사용
                Component vitals = pilot.GetComponent("FlightPilotVitals"); // 실제 입체 피격을 처리하는 기체
                var hit = vitals.GetType().GetMethod("ResolveThreat", hidden); // 실제 상대 경로의 접촉 판정
                hit.Invoke(vitals, new object[] { Vector3.zero, Vector3.forward, 0.1f, 40, false, false }); // 정상 피격에서 장착 피해 감소 시험
                Assert.That(Metric(vitals, "Health"), Is.EqualTo(actor is "Sera" or "Vera" ? 61 : 60), actor); // 장착 조합의 실제 피해량
                if (actor == "Sera") // 확정한 50% 방어 상한의 별도 경계 확인
                {
                    var bonus = gear.GetType().GetMethod("GetBonus"); // 피해 감소 항목의 장착 수치 유형
                    int type = System.Convert.ToInt32(System.Enum.Parse(bonus.GetParameters()[0].ParameterType, "DamageReduction")); // 확정한 장비의 피해 감소 인덱스
                    var bonuses = (float[])gear.GetType().GetField("bonuses", hidden).GetValue(gear); // 검증의 인위적인 상한 초과 부품 합계
                    bonuses[type] = 60f; // 50% 상한을 넘는 합계의 실제 판정
                    vitals.GetType().GetMethod("ResetRun").Invoke(vitals, null); // 이미 적용된 첫 피격 제거
                    hit.Invoke(vitals, new object[] { Vector3.zero, Vector3.forward, 0.1f, 100, false, false }); // 상한이 없으면 40만 소비되는 조건
                    Assert.That(Metric(vitals, "Health"), Is.EqualTo(50)); // 프로젝트에서 확정한 50% 최대 감소
                }
                vitals.GetType().GetMethod("ResetRun").Invoke(vitals, null); // 마지막 종료 시험의 체력과 무적 원상 복구
                hit.Invoke(vitals, new object[] { Vector3.zero, Vector3.forward, 0.1f, 500, false, false }); // 실패 정산의 테스트 피해 사용
                yield return WaitFrames(4); // 결과 오브젝트가 켜지는 실제 프레임
                ClickPauseButton(combat, "SortieEnd/Open"); // 실제 전투 종료의 별도 결과
                yield return WaitForActiveScene("Assets/Scenes/FlightSortieResult.unity"); // 관련 조종사의 실제 결과
                Transform result = GameObject.Find("SortieResult").transform; // 오른쪽 기록과 왼쪽 일러스트
                Assert.That(result.Find("PortraitArea/StandingIllustration").GetComponent<RawImage>().texture.name, Is.EqualTo(actor + "Standing"), actor); // 전투에서 사용한 일곱 캐릭터 표시
                Assert.That(HudText(result.Find("Panel/Crystals/Amount")), Is.EqualTo("+3"), actor); // 참여 보상이 실제 숫자로 표시
                ClickPauseButton(result, "Panel/Retry"); // 같은 참가자와 장비로 재출격
                yield return WaitForActiveScene(OuterIslandsPath); // 결과 뒤 실제 전투의 새 씬
                yield return WaitFrames(2); // 새 조종사 바인딩이 완료될 시간
                Component fresh = FindPilot().GetComponent("FlightWeapon"); // 재출격으로 새로 생긴 실제 기체
                Assert.That(fresh.GetType().GetProperty("ActivePilot").GetValue(fresh).ToString(), Is.EqualTo(actor)); // 재출격에서 현재 조종사 유지
                Component freshItems = GameObject.Find("EnhancementDrops").GetComponent("FlightEnhancements"); // 새 출격의 실제 임시 성장 상태
                Assert.That(Metric(freshItems, "ActiveCount"), Is.Zero, actor); // 지난 출격 아이템이 초기 화면에 남지 않음
            }
        }

        [UnityTest] // 이리스의 실제 파동과 고도가 다른 연쇄 전파
        public IEnumerator IrisSortieTracksDistinctHitsAndChainsToNewTargets() // 중복 공명과 연쇄 대상의 피해 확인
        {
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 이리스 출격 선택
            yield return WaitFrames(3); // 조종사 버튼과 스탠딩 초기화
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 출격 하이러키
            Assert.That(briefing.Find("Ready/SelectIris"), Is.Not.Null); // 일곱 번째 출격 조종사
            ClickPauseButton(briefing, "Ready/SelectIris"); // 파동 마도사의 실제 선택
            ClickPauseButton(briefing, "Ready/Depart"); // 선택한 조합으로 전투 진입
            yield return WaitForActiveScene(OuterIslandsPath); // 실전 외곽 섬의 실제 전투
            Transform pilot = FindPilot(); // 마법을 사용한 비행체
            Component weapon = pilot.GetComponent("FlightWeapon"); // 실제 조종사 및 자동탄
            Component skills = pilot.GetComponent("FlightSkillController"); // 실제 Q·R의 조건과 소모
            Component resonance = pilot.GetComponent("FlightIrisCombat"); // 공명과 실제 3D 파동
            Assert.That(weapon.GetType().GetProperty("ActivePilot").GetValue(weapon).ToString(), Is.EqualTo("Iris")); // 실제 출격 조종사
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 독립 회귀의 필드 접근
            ((MonoBehaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 시험 적 외 자동 출현 방지
            weapon.GetType().GetField("nextShot", hidden).SetValue(weapon, float.PositiveInfinity); // 공명 적중에 다른 탄이 섞이지 않도록 제한
            var boltType = weapon.GetType().Assembly.GetType("ProjectG.FlightSpellBolt"); // 이전 캐릭터 순간 사격으로 나간 탄환 타입
            foreach (Component shot in Object.FindObjectsByType(boltType, FindObjectsSortMode.None)) // 실전 시작의 기본 사격 정리
            {
                Object.Destroy(shot.gameObject); // 파동 시험의 독립적인 첫 적중
            }
            yield return WaitFrames(1); // 이전 자동탄이 제거된 실제 프레임
            var enemies = (Component[])skills.GetType().GetField("defeatRewards", hidden).GetValue(skills); // 실제 마물 피격의 원본
            foreach (Component enemy in enemies) // 현재 자동 웨이브에서 쓰던 원본
            {
                enemy.gameObject.SetActive(false); // 새 파동의 실제 후보를 따로 배치
            }
            Vector3 center = pilot.position; // 조준된 전파의 실제 중심
            Vector3 direction = pilot.forward; // 전방 판정의 깊이
            Vector3 right = pilot.right; // 다른 연쇄 위치의 실제 좌우
            GameObject first = CloneStationaryTarget(enemies[0], center + direction * 5f, 750); // 첫 직접 파동 적중
            GameObject second = CloneStationaryTarget(enemies[0], center + direction * 5.4f - right + Vector3.up * 0.9f, 750); // 다른 높이의 직접 파동 적중
            GameObject above = CloneStationaryTarget(enemies[0], center + direction * 3.2f + Vector3.up * 4.5f, 750); // 원뿔 밖의 높은 적
            Physics.SyncTransforms(); // 실제 3차원 표적의 충돌 위치
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 첫 부채꼴 확산 파동
            yield return WaitFrames(4); // 실제 피해 한 번 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 독립된 다음 스킬 입력
            Assert.That(Metric(first.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(685)); // 첫 표적의 65 피해
            Assert.That(Metric(second.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(685)); // 다른 높이의 두 번째 표적도 65 피해
            Assert.That(Metric(above.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(750)); // 더 높은 원뿔 외부의 적
            Assert.That(Metric(resonance, "ResonancePercent"), Is.EqualTo(20)); // 서로 다른 두 적에 10%씩
            yield return WaitFrames(3); // 실제 Q 키를 놓은 뒤 다시 입력
            var cooldown = (float[])skills.GetType().GetField("readyAt", hidden).GetValue(skills); // 같은 대상의 중복 검증용 재사용 간격
            cooldown[0] = Time.time; // 시험에서 같은 Q를 기다리지 않고 다시 사용
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 같은 두 표적을 재타격
            yield return WaitFrames(4); // 다시 들어온 피해와 공명 상태
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 실제 Q의 키 해제
            Assert.That(Metric(first.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(620)); // 같은 표적의 실제 두 번째 피해
            Assert.That(Metric(resonance, "ResonancePercent"), Is.EqualTo(20)); // 같은 적은 새 공명 없음
            yield return new WaitForSeconds(2.12f); // 실제 2초 공명 창 종료
            Assert.That(Metric(resonance, "ResonancePercent"), Is.Zero); // 새 적중이 없으면 초기화
            above.SetActive(false); // 두 번의 연쇄를 독립 확인할 외부 대상만 유지
            GameObject hopA = CloneStationaryTarget(enemies[0], center + direction * 4.5f + right * 3.25f + Vector3.up * 1.75f, 750); // 첫 직접 원뿔 밖의 연쇄 대상
            GameObject hopB = CloneStationaryTarget(enemies[0], center + direction * 4.5f + right * 5.75f + Vector3.up * 2.65f, 750); // 첫 연쇄의 새 인접 대상
            Physics.SyncTransforms(); // 서로 다른 높이와 거리의 실제 연쇄
            skills.GetType().GetMethod("AddUltimate").Invoke(skills, new object[] { 100 }); // 실제 필살기 조건 충전
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 창공의 공명 실제 스킬
            yield return WaitFrames(4); // 주 파동과 두 번의 피해 전파
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // R 사용이 반복되지 않도록 키 해제
            Assert.That(Metric(skills, "Ultimate"), Is.Zero); // 에너지 100의 실제 소모
            Assert.That(Metric(first.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(480)); // 직접 파동에 140 피해 한 번
            Assert.That(Metric(second.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(480)); // 두 번째 직접 대상에도 중복 전파 없음
            Assert.That(Metric(hopA.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(720)); // 첫 연쇄의 별도 피해 30
            Assert.That(Metric(hopB.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(720)); // 두 번째 다른 적의 별도 피해 30
            Assert.That(Metric(resonance, "ResonancePercent"), Is.EqualTo(30)); // 네 대상 적중에서도 공명 30% 상한
            first.SetActive(false); // 단일 보스 조건에서 다른 적을 제외
            second.SetActive(false); // 현재 원뿔의 다른 시작 후보 제거
            hopA.SetActive(false); // 첫 연쇄의 편향을 없앰
            hopB.SetActive(false); // 두 번째 연쇄의 편향을 없앰
            Component bossSource = System.Array.Find(enemies, enemy => enemy.GetType().GetProperty("LootTier").GetValue(enemy).ToString() == "MidBoss"); // 실전 중간 보스의 실제 충돌 원본
            GameObject boss = CloneStationaryTarget(bossSource, center + direction * 7f + Vector3.up, 750); // 다른 표적이 없는 단일 보스
            Physics.SyncTransforms(); // 보스의 실제 고도 좌표
            var cast = resonance.GetType().GetMethod("CastWave", hidden); // 같은 실제 스킬에서 쓰는 최종 물리 경로
            Assert.That(cast.Invoke(resonance, new object[] { true }), Is.True); // 단일 상대에게 시전 성공
            Assert.That(Metric(boss.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(610)); // 두 추가 연쇄가 같은 보스를 다시 맞히지 않음
            Assert.That(pilot.Find("IrisChainLinks").GetComponent<LineRenderer>().enabled, Is.False); // 단일 적에 허위 연쇄 선 표시 없음
            Object.Destroy(boss); // 실제 보스 시험 상태 정리
            Object.Destroy(first); // 직접 시전 표적 정리
            Object.Destroy(second); // 높이가 다른 두 번째 적 정리
            Object.Destroy(above); // 원뿔 밖의 적 정리
            Object.Destroy(hopA); // 첫 연쇄 대상 정리
            Object.Destroy(hopB); // 두 번째 연쇄 대상 정리
        }

        [UnityTest] // 베라의 한 포대 설치와 포대에서 시작하는 광선
        public IEnumerator VeraSortieTurretStaysPlacedAndGatesBeam() // 미설치·설치·이동·교체·만료의 실제 스킬
        {
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 베라 선택 씬
            yield return WaitFrames(3); // 현재 출격 버튼과 스탠딩 준비
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 편집 가능한 출격 준비
            Assert.That(briefing.Find("Ready/SelectVera"), Is.Not.Null); // 여섯 번째 시범 출격 조종사
            ClickPauseButton(briefing, "Ready/SelectVera"); // 실제 베라를 선택
            ClickPauseButton(briefing, "Ready/Depart"); // 공방의 선택 캐릭터를 실제 스테이지로
            yield return WaitForActiveScene(OuterIslandsPath); // 실제 하늘섬 전투
            Transform pilot = FindPilot(); // 출격에서 조작하는 비행체
            Component weapon = pilot.GetComponent("FlightWeapon"); // 조종사의 실제 사격
            Component skills = pilot.GetComponent("FlightSkillController"); // 마력·필살기 자원
            Component turret = pilot.GetComponent("FlightVeraTurret"); // 현재 마도 포대의 상태
            var body = GameObject.Find("VeraTurretSite"); // 실제 씬에 배치한 수정 가능한 설치 지점
            Assert.That(body, Is.Not.Null); // 코드에서 만들지 않는 포대 부품
            Assert.That(weapon.GetType().GetProperty("ActivePilot").GetValue(weapon).ToString(), Is.EqualTo("Vera")); // 실제 출격 캐릭터
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 독립된 회귀용 상태
            ((MonoBehaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 자동 웨이브의 다른 적 제외
            weapon.GetType().GetField("nextShot", hidden).SetValue(weapon, float.PositiveInfinity); // 포대 사격과 플레이어 사격을 분리
            var members = (Component[])skills.GetType().GetField("defeatRewards", hidden).GetValue(skills); // 실제 마물의 피격 원본
            foreach (Component original in members) // 자동 웨이브의 기존 적은 검증과 무관
            {
                original.gameObject.SetActive(false); // 배치한 포대의 단일 표적으로 분리
            }
            Vector3 start = pilot.position; // 포대가 고정될 최초 비행 좌표
            Vector3 direction = pilot.forward; // 포대 앞의 실제 표적 방향
            Vector3 right = pilot.right; // 플레이어만 움직일 위치
            var active = turret.GetType().GetProperty("IsDeployed"); // 포대가 남아 있는 현재 상태
            var power = turret.GetType().GetProperty("DamageMultiplier"); // 실제 포대 중심의 자동탄 피해 배율
            skills.GetType().GetMethod("AddUltimate").Invoke(skills, new object[] { 100 }); // 필살기 비용을 먼저 충전
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 포대 없는 상태의 필살기
            yield return WaitFrames(4); // 조건 확인과 새 입력 간격
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 입력 해제
            Assert.That(Metric(skills, "Ultimate"), Is.EqualTo(100)); // 실제 퇴장 전 자원 보존
            Assert.That(active.GetValue(turret), Is.False); // 없던 포대를 만들지 않음
            GameObject target = CloneStationaryTarget(members[0], start + direction * 10f, 800); // 설치된 포대 앞의 실제 구체 표적
            Physics.SyncTransforms(); // 마물의 실제 3D 충돌 좌표
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 마도 포대 Q 설치
            yield return WaitFrames(4); // 포대 오브젝트를 켤 시간
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이후에 계속 누르지 않음
            Assert.That(active.GetValue(turret), Is.True); // 첫 포대가 실제 배치됨
            Assert.That(Vector3.Distance(body.transform.position, start), Is.LessThan(0.02f)); // 설치 순간의 월드 위치
            Assert.That((float)power.GetValue(turret), Is.EqualTo(1.2f).Within(0.001f)); // 반경 2m 안 자동탄 피해 증가
            Transform rotor = body.transform.Find("Hover/TopPropeller"); // 상단의 편집 가능한 회전자
            Quaternion spin = rotor.localRotation; // 현재 프레임의 실제 방향
            yield return new WaitForSeconds(1.42f); // 첫 자동 포대탄의 실제 적중
            Assert.That(Quaternion.Angle(spin, rotor.localRotation), Is.GreaterThan(4f)); // 멈춰 있지 않은 프로펠러
            Assert.That(Metric(target.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(790)); // 1초 뒤 첫 포대탄 피해 10
            Component flight = pilot.GetComponent("FirstFlightController"); // 현재 조종사의 비행 지점
            Vector3 moved = start + right * 4f; // 포대 범위 밖으로만 이동
            flight.GetType().GetMethod("ResetFlightPose").Invoke(flight, new object[] { moved, pilot.rotation }); // 플레이어만 이동
            Assert.That(Vector3.Distance(body.transform.position, start), Is.LessThan(0.02f)); // 포대는 원래 지점 유지
            Assert.That((float)power.GetValue(turret), Is.EqualTo(1f)); // 바깥에서 자동탄 강화 해제
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 이동 뒤 독립 설치 지점의 광선
            yield return WaitFrames(4); // 실제 첫 광선 타격
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 같은 R 입력 반복 제한
            Assert.That(Metric(skills, "Ultimate"), Is.Zero); // 포대가 있을 때만 필살기 소비
            Assert.That(Metric(target.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(590)); // 포대 앞의 적에게 200 광선 피해
            var cooldown = (float[])skills.GetType().GetField("readyAt", hidden).GetValue(skills); // 순차 교체를 분리할 실제 슬롯
            cooldown[0] = Time.time; // 시험에서만 Q 다시 사용 가능
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 같은 포대가 새 위치로 교체되는 경로
            yield return WaitFrames(4); // 설치 지점 오브젝트 갱신
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 실제 새 입력 종료
            Assert.That(Vector3.Distance(body.transform.position, moved), Is.LessThan(0.02f)); // 기존 포대가 새 위치로 이동하며 교체
            yield return new WaitForSeconds(8.15f); // 기본 유지 시간이 실제 경과
            Assert.That(active.GetValue(turret), Is.False); // 만료 시 자동 공격과 상태 종료
            cooldown[2] = Time.time; // 만료 상태만 분리해서 필살기 사용
            skills.GetType().GetMethod("AddUltimate").Invoke(skills, new object[] { 100 }); // 새로 충전한 실제 자원
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 만료된 포대에서 광선 사용 시도
            yield return WaitFrames(4); // 실제 사용 실패 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 다음 시험의 입력 원상복구
            Assert.That(Metric(skills, "Ultimate"), Is.EqualTo(100)); // 만료된 포대도 에너지 보존
            Object.Destroy(target); // 독립 검증용 적 정리
        }

        [UnityTest] // 녹스의 스침·표식과 실제 3D 탄 소거 판정
        public IEnumerator NoxSortieCapsGrazeMarksAndClearsOnlyNearbyThreats() // 표식 0·상한·소비와 주변 소거
        {
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 녹스 출격 준비
            yield return WaitFrames(3); // 현재 씬의 캐릭터 선택을 활성화
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 선택과 출발 하이러키
            Assert.That(briefing.Find("Ready/SelectNox"), Is.Not.Null); // 다섯 번째 캐릭터 선택 버튼
            ClickPauseButton(briefing, "Ready/SelectNox"); // 실제 녹스 장비와 그림 선택
            ClickPauseButton(briefing, "Ready/Depart"); // 선택한 녹스의 실제 전투
            yield return WaitForActiveScene(OuterIslandsPath); // 실제 외곽 항로 진입
            Transform pilot = FindPilot(); // 실제 3D 비행체
            Component weapon = pilot.GetComponent("FlightWeapon"); // 현재 조종사와 자동탄 설정
            Component vitals = pilot.GetComponent("FlightPilotVitals"); // 실제 탄과 상대 궤적의 그레이즈
            Component skills = pilot.GetComponent("FlightSkillController"); // 실제 마력과 필살기
            Component counter = pilot.GetComponent("FlightNoxCombat"); // 현재 반격 표식과 소거
            Assert.That(weapon.GetType().GetProperty("ActivePilot").GetValue(weapon).ToString(), Is.EqualTo("Nox")); // 실제 출격한 조종사 기록
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 검증용 상태 접근
            ((MonoBehaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 시험용 적만 남김
            weapon.GetType().GetField("nextShot", hidden).SetValue(weapon, float.PositiveInfinity); // 실제 반격과 자동 공격 피해 분리
            var boltType = weapon.GetType().Assembly.GetType("ProjectG.FlightSpellBolt"); // 현재 씬이 사용한 아군 탄환 타입
            foreach (Component existing in Object.FindObjectsByType(boltType, FindObjectsSortMode.None)) // 조종사 초기화 전에 남은 자동탄
            {
                Object.Destroy(existing.gameObject); // 실제 반격탄 개수만 확인하도록 기존 탄 제외
            }
            Vector3 center = pilot.position; // 조종사 중심의 스침·소거 범위
            Vector3 ahead = pilot.forward; // 실제 앞뒤 방향
            Vector3 right = pilot.right; // 몸 옆과 빗겨 가는 적탄 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 표식이 전혀 없을 때 실제 Q 사용
            yield return WaitFrames(4); // Q 조건의 실제 처리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 다시 누르기 전 해제
            Assert.That(Metric(skills, "Mana"), Is.EqualTo(100)); // 자원이 소비되지 않는 표식 부족
            for (int shot = 0; shot < 12; shot++) // 상한을 넘는 12발의 실제 적탄
            {
                EmitThreat(center + ahead * 8f + right * (0.65f + shot * 0.009f), -ahead, 110f); // 피격 구체 밖의 정상 그레이즈 거리
            }
            yield return WaitFrames(19); // 모든 적탄이 스침 거리 밖으로 통과
            Assert.That(Metric(vitals, "Grazes"), Is.EqualTo(12)); // 같은 탄당 한 번만 그레이즈
            Assert.That(Metric(counter, "Marks"), Is.EqualTo(10)); // 자원이 올라가도 표식은 10개 제한
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 기록한 10표식으로 실제 반격
            yield return WaitFrames(4); // 실제 10발 생성과 표식 소비
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 중복 키 반복 방지
            Assert.That(Metric(counter, "Marks"), Is.Zero); // 표식 전부 소모
            Assert.That(Metric(skills, "Mana"), Is.LessThan(Metric(skills, "MaxMana") - 23)); // 실제 Q 비용과 쿨타임 소비
            int bolts = Object.FindObjectsByType(boltType, FindObjectsSortMode.None).Length; // 기본 자동탄을 끈 현재의 반격탄만
            Assert.That(bolts, Is.EqualTo(10)); // 표식마다 실제 반격탄 한 발
            var members = (Component[])skills.GetType().GetField("defeatRewards", hidden).GetValue(skills); // 시험 마물의 실제 모델과 체력
            GameObject near = CloneStationaryTarget(members[0], center + Vector3.up * 2.4f, 500); // 3D 소거 반경에 있는 높은 적
            GameObject outside = CloneStationaryTarget(members[0], center + Vector3.up * 3.4f + right * 0.4f, 500); // 범위 밖의 더 높은 적
            Physics.SyncTransforms(); // 3D 적의 충돌 좌표 적용
            Component pool = GetThreatPool(); // 실제 적탄 풀
            pool.GetType().GetMethod("ReturnAll").Invoke(pool, null); // 두 개의 소거 범위를 독립 검증
            EmitThreat(center + Vector3.up * 2.6f, right, 0.1f); // 위쪽의 소거 반경 안 적탄
            EmitThreat(center + Vector3.up * 3.3f, right, 0.1f); // 같은 방향으로 범위 밖의 적탄
            skills.GetType().GetMethod("AddUltimate").Invoke(skills, new object[] { 100 }); // 검증용 실제 필살기 에너지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 경계 역전 실제 시전
            yield return WaitFrames(4); // 적탄 회수와 마물의 범위 피해
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 다음 입력 정리
            Assert.That(Metric(skills, "Ultimate"), Is.Zero); // 실제 에너지 100 소비
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(1)); // 반경 밖 탄환이 유지됨
            Assert.That(Metric(near.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(350)); // 반경 안 마물에게 피해 150
            Assert.That(Metric(outside.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(500)); // 높은 외부 마물에게 피해 없음
            Object.Destroy(near); // 물리에 등록한 실제 시험 마물 정리
            Object.Destroy(outside); // 외부 3D 시험 마물 정리
        }

        [UnityTest] // 3D 검기 범위와 돌파 이동의 물리 판정
        public IEnumerator SeraSortieChecksCloseDamageHeightAndWall() // 근거리 계수와 높은 표적 제외
        {
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 세라 출격 선택 화면
            yield return WaitFrames(3); // 편집한 캐릭터 버튼의 준비
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 준비 하이러키
            Assert.That(briefing.Find("Ready/SelectSera"), Is.Not.Null); // 세라를 네 번째 버튼으로 선택
            ClickPauseButton(briefing, "Ready/SelectSera"); // 선택한 스탠딩과 실제 조종사 설정
            Assert.That(briefing.Find("Ready/Portrait").GetComponent<RawImage>().texture.name, Is.EqualTo("SeraStanding")); // 기획서 대표 전신
            ClickPauseButton(briefing, "Ready/Depart"); // 선택한 세라의 실제 출격
            yield return WaitForActiveScene(OuterIslandsPath); // 새 항로 로딩
            Transform pilot = FindPilot(); // 배포에서 움직이는 기체
            Component weapon = pilot.GetComponent("FlightWeapon"); // 기본 자동사격과 현재 조종사
            Component skills = pilot.GetComponent("FlightSkillController"); // 실제 마력과 에너지
            Assert.That(weapon.GetType().GetProperty("ActivePilot").GetValue(weapon).ToString(), Is.EqualTo("Sera")); // 선택한 조종사의 적용
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 출격 시험 조건
            var encounter = (MonoBehaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 무관한 스테이지 자동 웨이브
            encounter.enabled = false; // 근거리 검기만 독립 시험
            weapon.GetType().GetField("nextShot", hidden).SetValue(weapon, float.PositiveInfinity); // 자동탄 피해 제외
            var enemies = (Component[])skills.GetType().GetField("defeatRewards", hidden).GetValue(skills); // 씬에 배치한 원본 마물
            foreach (Component original in enemies) // 현재 웨이브 밖의 초기 마물
            {
                original.gameObject.SetActive(false); // 근거리 표적 피해와 분리
            }
            Vector3 start = pilot.position; // 검기와 이동 거리의 기준
            Vector3 forward = pilot.forward; // 처음 조준 방향
            Vector3 right = pilot.right; // 검기 너비의 가로 기준
            Component template = enemies[0]; // 일반 마물의 물리와 체력바 원본
            GameObject close = CloneStationaryTarget(template, start + forward * 1.2f + right * 0.2f, 500); // 근거리 결의 적용 범위 안
            GameObject farther = CloneStationaryTarget(template, start + forward * 5f + right * 1.7f, 500); // 근거리 밖이나 검기 안
            GameObject higher = CloneStationaryTarget(template, start + forward * 3.3f + Vector3.up * 3.4f, 500); // 높이 차이로 놓치는 적
            Physics.SyncTransforms(); // 세 입체 표적의 배치 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 실제 돌파 검기 입력
            yield return WaitFrames(5); // 발동과 피해 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 다음 물리 키 해제
            Assert.That(Metric(close.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(412)); // 70 피해에 근거리 25% 적용
            Assert.That(Metric(farther.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(430)); // 한 번만 적용되는 전방 피해
            Assert.That(Metric(higher.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(500)); // 더 높은 적 제외
            yield return new WaitForSeconds(0.4f); // 첫 돌파의 가속 이동 완료
            Assert.That(Vector3.Dot(pilot.position - start, forward), Is.InRange(3.7f, 4.4f)); // 돌파의 실제 이동 거리
            Component flight = pilot.GetComponent("FirstFlightController"); // 월드 지형 접촉을 처리하는 비행체
            flight.GetType().GetMethod("ResetFlightPose").Invoke(flight, new object[] { start, pilot.rotation }); // 같은 시작점에서 벽 시험
            skills.GetType().GetMethod("ResetRun", hidden).Invoke(skills, null); // 실제 새 출격의 스킬 재사용 준비
            weapon.GetType().GetField("nextShot", hidden).SetValue(weapon, float.PositiveInfinity); // 자동탄과 검기 통계 분리
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube); // 독립 검증의 지형 벽 생성
            barrier.layer = 8; // 배포의 실제 3D 지형 충돌 레이어
            barrier.transform.SetPositionAndRotation(start + forward * 2.1f, pilot.rotation); // 첫 검기 안의 장애물
            barrier.transform.localScale = new Vector3(6f, 6f, 0.35f); // 높이와 넓이가 충분한 얇은 벽
            Physics.SyncTransforms(); // 실제 지형 검사에 적용
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 같은 돌파를 벽 앞에서 시행
            yield return WaitFrames(4); // 검기와 이동 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 접촉 시험 후 키 해제
            yield return new WaitForSeconds(0.4f); // 실제 벽에서 멈출 때까지 대기
            Assert.That(Vector3.Dot(pilot.position - start, forward), Is.LessThan(1.8f)); // 벽과 충돌해 넘지 않는 돌파
            Assert.That(Metric(farther.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(430)); // 지형 너머의 적은 추가 피해 없음
            barrier.SetActive(false); // 연참에서 지형 효과 제거
            flight.GetType().GetMethod("ResetFlightPose").Invoke(flight, new object[] { start, pilot.rotation }); // 두 연속 스킬을 독립 시험
            skills.GetType().GetMethod("AddUltimate").Invoke(skills, new object[] { 100 }); // 필살기에 필요한 에너지 충전
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 천공 연참의 실제 키 입력
            yield return WaitFrames(4); // 첫 연참 발생 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 입력 반복 제거
            Assert.That(Metric(farther.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(390)); // 첫 40 피해와 남은 네 참격
            Assert.That(Metric(skills, "Ultimate"), Is.Zero); // 연참 발동의 실제 에너지 100 차감
            yield return new WaitForSeconds(0.75f); // 나머지 연참이 끝나도록 대기
            Assert.That(Metric(farther.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(230)); // 총 5회의 독립 40 피해
            Assert.That(Metric(higher.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(500)); // 검기와 연참 둘 다 고도 경계 밖
            Object.Destroy(barrier); // 독립 검증용 벽 정리
            Object.Destroy(close); // 낮은 표적 정리
            Object.Destroy(farther); // 먼 표적 정리
            Object.Destroy(higher); // 높은 표적 정리
        }

        [UnityTest] // 실제 출격 준비에서 두 재화 표시 검증
        public IEnumerator SortieBalanceUsesCurrencyArtForNumericLabels() // 투명 아이콘과 별도 수치
        {
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 빗자루 편성의 실제 보유량 화면
            yield return WaitFrames(3); // 현재 프로필이 수치를 채울 때까지 대기
            Transform ready = GameObject.Find("SortieBriefing").transform.Find("Ready"); // 에디터에 저장된 재화 부모
            Transform crystals = ready.Find("Crystals"); // 결정 다발과 숫자
            Transform gold = ready.Find("Gold"); // 동전과 숫자
            Assert.That(crystals.gameObject.activeInHierarchy && gold.gameObject.activeInHierarchy, Is.True); // 0이어도 보이는 잔액
            Assert.That(crystals.Find("Icon").GetComponent<RawImage>().texture.name, Is.EqualTo("SkyCrystalBundle")); // 일곱 결정의 제작 이미지
            Assert.That(gold.Find("Icon").GetComponent<RawImage>().texture.name, Is.EqualTo("GoldCoin")); // 태양 문양의 제작 이미지
            Assert.That(crystals.Find("Amount").GetComponent<Text>().text, Does.Match(@"^[0-9,]+$")); // 재화 이름이 없는 수치 글자
            Assert.That(gold.Find("Amount").GetComponent<Text>().text, Does.Match(@"^[0-9,]+$")); // 골드 이름이 없는 수치 글자
            Assert.That(ready.Find("Progress").GetComponent<Text>().text, Does.Contain("조율의 증표")); // 다른 재화·성장 설명 유지
        }

        [UnityTest] // 루미의 선택부터 실제 3D 유도탄까지
        public IEnumerator LumiSortieTracksAltitudeAndDistributesStars() // 출격과 두 고도 표적의 실제 발사
        {
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 정식 출격 선택으로 진입
            yield return WaitFrames(3); // 조종사 버튼과 이미지 연결 대기
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 출격 준비 하이러키
            Assert.That(briefing.Find("Ready/SelectLumi"), Is.Not.Null); // 에디터에서 이동 가능한 루미 버튼
            ClickPauseButton(briefing, "Ready/SelectLumi"); // 세 번째 조종사를 마우스 경로로 선택
            Assert.That(briefing.Find("Ready/Portrait").GetComponent<RawImage>().texture.name, Is.EqualTo("LumiStanding")); // 기획서 원본 불러오기
            ClickPauseButton(briefing, "Ready/Depart"); // 실제 전투에 루미 선택을 전달
            yield return WaitForActiveScene(OuterIslandsPath); // 배포 전투 로딩
            Transform pilot = FindPilot(); // 실제 비행체
            Component weapon = pilot.GetComponent("FlightWeapon"); // 루미 비행과 스킬의 공통 무기
            Assert.That(weapon.GetType().GetProperty("ActivePilot").GetValue(weapon).ToString(), Is.EqualTo("Lumi")); // 실제 출격 조종사
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 시뮬레이션 재현용 필드
            var muzzle = (Transform)weapon.GetType().GetField("muzzle", hidden).GetValue(weapon); // 물리 좌표의 실제 총구
            weapon.GetType().GetField("nextShot", hidden).SetValue(weapon, float.PositiveInfinity); // 자동탄 피해와 독립인 유도 판정
            var encounter = (MonoBehaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 시험 표적 외 스테이지 마물의 추가 생성
            encounter.enabled = false; // 두 높이의 적만 대상으로 분리
            Component skills = pilot.GetComponent("FlightSkillController"); // 실제 Q·R 자원과 쿨타임
            var enemies = (Component[])skills.GetType().GetField("defeatRewards", hidden).GetValue(skills); // 실제 일반 마물 모델과 충돌 원본
            foreach (Component original in enemies) // 씬에 이미 연결된 마물의 활성 여부
            {
                original.gameObject.SetActive(false); // 회귀에 등장한 두 표적만 남김
            }
            Component template = enemies[0]; // 낮고 높은 표적이 쓰는 동일한 일반 마물
            GameObject low = CloneStationaryTarget(template, muzzle.position + muzzle.forward * 17f + muzzle.right * 6f - muzzle.up * 3f, 500); // 낮은 고도의 가까운 표적
            GameObject high = CloneStationaryTarget(template, muzzle.position + muzzle.forward * 24f - muzzle.right * 8f + muzzle.up * 5f, 500); // 높은 고도의 두 번째 표적
            Physics.SyncTransforms(); // 두 마물의 시험 좌표를 물리에 반영
            var tracked = weapon.GetType().GetProperty("LumiTrackingBonus", hidden); // 실전 HUD의 추적 회전력
            yield return new WaitForSeconds(3.25f); // 같은 표적을 유지해 최대치 도달
            Assert.That(tracked.GetValue(weapon), Is.EqualTo(45)); // 회전 속도 보너스 상한
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 별빛 추적탄 실제 시전
            yield return WaitFrames(3); // Q 입력과 마력 소모 반영
            Assert.That(Metric(skills, "Mana"), Is.LessThan(Metric(skills, "MaxMana") - 23)); // 25 마력 소비 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 다음 스킬 입력 준비
            yield return new WaitForSeconds(0.95f); // 입체 이동과 실제 충돌 완료 대기
            Assert.That(Metric(low.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(428)); // 낮은 대상에 여섯 발의 실제 피해
            Assert.That(Metric(high.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(500)); // 분산 노드 미선택 상태
            skills.GetType().GetMethod("AddUltimate").Invoke(skills, new object[] { 100 }); // 필살기 조건만 분리해서 충전
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 별무리 항로 실제 시전
            yield return WaitFrames(3); // R 입력과 에너지 소비
            Assert.That(Metric(skills, "Ultimate"), Is.Zero); // 100 에너지 사용 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 필살기 키 해제
            yield return new WaitForSeconds(1.25f); // 여러 고도의 실제 유도 충돌 완료 대기
            Assert.That(Metric(low.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(320)); // 가까운 표적에 여섯 발 배정
            Assert.That(Metric(high.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(392)); // 높은 표적에도 여섯 발 적중
            low.SetActive(false); // 유지했던 적이 퇴장한 상태
            yield return new WaitForSeconds(0.25f); // 실제 탐색 간격을 지나 교체
            Assert.That(tracked.GetValue(weapon), Is.Zero); // 다른 고도 표적으로 바뀌면 누적 해제
            Object.Destroy(low); // 낮은 시험 마물 정리
            high.SetActive(false); // 다음 검증은 단일 보스만 배치
            Component midBoss = System.Array.Find(enemies, enemy => enemy.GetType().GetProperty("LootTier").GetValue(enemy).ToString() == "MidBoss"); // 실제 씬의 중간 보스
            Assert.That(midBoss, Is.Not.Null); // 실전 보스 원본의 물리와 체력 확인
            GameObject single = CloneStationaryTarget(midBoss, muzzle.position + muzzle.forward * 25f + muzzle.up * 5f, 1000); // 공격 가능한 단일 중간 보스
            Physics.SyncTransforms(); // 보스의 입체 위치 반영
            var cast = weapon.GetType().GetMethod("CastLumiStars", hidden); // 필살기가 실제 자원 검사 뒤 호출하는 발사 경로
            Assert.That(cast.Invoke(weapon, new object[] { true }), Is.True); // 모든 유도탄을 단일 보스에 배정
            yield return new WaitForSeconds(1.3f); // 열두 발의 충돌을 확인할 시간
            Assert.That(Metric(single.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(784)); // 18 피해 열두 발의 실제 충돌
            single.SetActive(false); // 대상이 하나도 없는 상황으로 변경
            Assert.That(cast.Invoke(weapon, new object[] { false }), Is.False); // 대상이 없으면 스킬 발동 거절
            Object.Destroy(single); // 실제 보스 복제 정리
            Object.Destroy(high); // 높은 시험 마물 정리
        }

        private static GameObject CloneStationaryTarget(Component template, Vector3 location, int health) // 실제 마물의 정지 충돌 표적
        {
            GameObject target = Object.Instantiate(template.gameObject); // 전장 원본의 충돌과 렌더링
            foreach (MonoBehaviour behavior in target.GetComponents<MonoBehaviour>()) // 경로와 사격을 분리한 정적 표적
            {
                behavior.enabled = behavior.GetType() == template.GetType(); // 체력과 피격만 유지
            }
            return PlaceSniperTarget(target, location, health); // 같은 활성화와 체력 설정
        }

        [UnityTest] // 키 입력과 실제 씬 이동 검증
        public IEnumerator FlightSceneRespondsInAllSixDirections() // 전후·좌우·상하 조작
        {
            yield return LoadFlight(); // 저장된 시험 씬 로드
            Transform pilot = FindPilot(); // 씬의 아리아 루트
            yield return CheckMove(pilot, Key.W, Vector3.forward); // 전진 검증
            yield return CheckMove(pilot, Key.S, Vector3.back); // 후진 검증
            yield return CheckMove(pilot, Key.A, Vector3.left); // 좌측 검증
            yield return CheckMove(pilot, Key.D, Vector3.right); // 우측 검증
            yield return CheckMove(pilot, Key.Space, Vector3.up); // 상승 검증
            yield return CheckMove(pilot, Key.LeftCtrl, Vector3.down); // 하강 검증
        }

        [UnityTest] // 원근 카메라의 실제 추적 검증
        public IEnumerator CameraFollowsAndKeepsPilotInView() // 이동 후 카메라 구도 유지
        {
            yield return LoadFlight(); // 저장된 시험 씬 로드
            Transform pilot = FindPilot(); // 씬의 아리아 루트
            Camera camera = Camera.main; // 실제 전투 카메라
            Assert.That(camera, Is.Not.Null); // 메인 카메라 태그 확인
            Assert.That(camera.orthographic, Is.False); // 원근 투영 확인
            Vector3 pilotStart = pilot.position; // 초기 플레이어 위치
            Vector3 cameraStart = camera.transform.position; // 초기 카메라 위치
            Vector3 initialScreen = camera.WorldToViewportPoint(pilot.position); // 화면의 초기 플레이어 위치
            Vector3 chest = camera.WorldToViewportPoint(pilot.position + Vector3.up * 0.55f); // 몸통의 실제 화면 위치
            Assert.That(chest.x, Is.LessThan(0.45f)); // 조준점 기준 왼쪽으로 이동
            Assert.That(chest.y, Is.LessThan(0.45f)); // 조준점보다 아래쪽 유지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D, Key.Space)); // 3축 동시 입력
            InputSystem.Update(); // 입력 이벤트 즉시 반영
            for (int frame = 0; frame < 15; frame++) // 이동과 카메라 갱신 프레임
            {
                yield return null; // 다음 플레이 프레임
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 입력 해제
            yield return WaitFrames(30); // 제동과 마지막 LateUpdate까지 대기
            Assert.That((pilot.position - pilotStart).sqrMagnitude, Is.GreaterThan(0f)); // 실제 이동 확인
            Assert.That((camera.transform.position - cameraStart - (pilot.position - pilotStart)).sqrMagnitude, Is.LessThan(0.001f)); // 같은 거리만큼 카메라 이동
            Assert.That(Vector3.Dot(pilot.position - camera.transform.position, pilot.forward), Is.GreaterThan(0f)); // 캐릭터 뒤 카메라
            Vector3 screen = camera.WorldToViewportPoint(pilot.position); // 화면의 이동 후 플레이어 위치
            Assert.That(screen.z, Is.GreaterThan(0f)); // 카메라 앞 캐릭터
            Assert.That(screen.x, Is.InRange(0.1f, 0.9f)); // 화면 가로 범위
            Assert.That(screen.y, Is.InRange(0.1f, 0.9f)); // 화면 세로 범위
            Assert.That(screen.x, Is.EqualTo(initialScreen.x).Within(0.02f)); // 가로 구도 유지
            Assert.That(screen.y, Is.EqualTo(initialScreen.y).Within(0.02f)); // 세로 구도 유지
        }

        [UnityTest] // 집중 비행과 제동 검증
        public IEnumerator FocusHalvesSpeedAndReleasedFlightBrakes() // 최고 속도와 감속 확인
        {
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 이동하는 대상
            SetKeys(Key.W); // 일반 전진
            yield return WaitFrames(45); // 최고 속도 도달
            Vector3 start = pilot.position; // 일반 속도 시작점
            yield return WaitFrames(6); // 같은 시간 동안 이동
            float regular = (pilot.position - start).magnitude; // 일반 이동량
            Assert.That(regular * 10f, Is.EqualTo(12f).Within(0.12f)); // 일반 최고 속도 1.5배
            SetKeys(Key.W, Key.LeftShift); // 집중 전진
            yield return WaitFrames(45); // 집중 속도 도달
            start = pilot.position; // 집중 속도 시작점
            yield return WaitFrames(6); // 같은 시간 동안 이동
            float focused = (pilot.position - start).magnitude; // 집중 이동량
            Assert.That(focused / regular, Is.EqualTo(0.5f).Within(0.04f)); // 집중 속도 절반
            Vector3 releaseStart = pilot.position; // 제동 거리 측정 시작점
            SetKeys(); // 전진 입력 해제
            yield return WaitFrames(1); // 제동 프레임 시작
            start = pilot.position; // 제동 구간 시작점
            yield return WaitFrames(5); // 짧은 감속 구간
            Assert.That((pilot.position - start).magnitude, Is.GreaterThan(0.03f)); // 제동 중 잔류 이동
            yield return WaitFrames(35); // 속도 정지 대기
            start = pilot.position; // 정지 이후 기준점
            yield return WaitFrames(6); // 상태 유지 확인
            Assert.That((pilot.position - start).magnitude, Is.LessThan(0.002f)); // 완전 정지
            float brakingDistance = (pilot.position - releaseStart).magnitude; // 완전히 멈출 때까지 이동 거리
            SetKeys(Key.W, Key.D); // 대각선 전진
            yield return WaitFrames(45); // 대각선 최고 속도 도달
            start = pilot.position; // 측정 시작 좌표
            yield return WaitFrames(6); // 일반 전진과 같은 측정 시간
            float diagonalRatio = (pilot.position - start).magnitude / regular; // 일반 전진 대비 대각선 속도
            Assert.That(diagonalRatio, Is.EqualTo(1f).Within(0.04f)); // 대각선 속도 증가 방지
            Debug.Log($"FLIGHT_DAY02_METRICS speed={regular * 10f:F2} focus={focused * 10f:F2} diagonalRatio={diagonalRatio:F2} braking={brakingDistance:F3}"); // 재현한 비행 시험 수치
        }

        [UnityTest] // 방향 전환과 독립 자유 시점
        public IEnumerator MouseTurnsAndMiddleButtonLooksAroundWithoutTurningPilot() // 주시 방향과 비행 방향 분리
        {
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 기본 비행체
            Camera camera = Camera.main; // 실제 추적 카메라
            Vector3 forward = pilot.forward; // 비행 방향 기준
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(90f, 35f) }); // 일반 마우스 시점 입력
            yield return WaitFrames(3); // 게임 입력 프레임
            Assert.That(Vector3.Angle(forward, pilot.forward), Is.GreaterThan(4f)); // 비행체 실제 선회
            Assert.That(camera.transform.forward.y, Is.GreaterThan(-0.14f)); // 위쪽을 보는 마우스 입력 반영
            forward = pilot.forward; // 선회한 방향 유지 기준
            Vector3 previousView = camera.transform.forward; // 자유 시점 전 화면 방향
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 4, delta = new Vector2(220f, 0f) }); // 휠 버튼 둘러보기
            yield return WaitFrames(3); // 자유 시점 적용 프레임
            Assert.That(Vector3.Angle(forward, pilot.forward), Is.LessThan(0.1f)); // 비행 방향 유지
            Assert.That(Vector3.Angle(previousView, camera.transform.forward), Is.GreaterThan(18f)); // 카메라만 회전
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 휠 버튼 해제
            yield return WaitFrames(50); // 카메라 복귀 대기
            Assert.That(Vector3.Angle(previousView, camera.transform.forward), Is.LessThan(0.3f)); // 기존 방향 복귀
        }

        [UnityTest] // 비행 경계와 카메라 가림 검증
        public IEnumerator BoundaryClampsAltitudeAndWallRetractsCamera() // 경계 정지와 카메라 복귀
        {
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 씬의 비행체
            Transform camera = Camera.main.transform; // 씬의 카메라
            Vector3 start = pilot.position; // 최초 위치 보관
            pilot.position = new Vector3(0f, 36f, start.z); // 상승 경계 직전 배치
            SetKeys(Key.Space); // 상승 유지
            yield return WaitFrames(45); // 경계 도달 프레임
            Assert.That(pilot.position.y, Is.EqualTo(37f).Within(0.01f)); // 고도 상한 고정
            SetKeys(); // 상승 해제
            pilot.position = start; // 가림 시험 위치 복원
            yield return WaitFrames(8); // 기본 카메라 추적
            int environment = LayerMask.NameToLayer("Environment"); // 씬의 실제 지형 레이어
            MeshCollider terrain = GameObject.Find("WesternTerrace").GetComponent<MeshCollider>(); // 저장된 섬의 충돌면
            Assert.That(terrain, Is.Not.Null); // 하늘섬 충돌 저장 검증
            Assert.That(terrain.gameObject.layer, Is.EqualTo(environment)); // 카메라 가림 레이어 연결
            float normal = Vector3.Distance(pilot.position, camera.position); // 정상 추적 거리
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // 가림 시험용 임시 벽
            wall.layer = environment; // 지형과 동일한 충돌 레이어
            wall.transform.position = start + new Vector3(0f, 1.2f, -3f); // 카메라와 아리아 사이
            wall.transform.localScale = new Vector3(6f, 4f, 0.4f); // 실제 카메라 광선 가림
            Physics.SyncTransforms(); // 벽의 위치 반영
            yield return WaitFrames(4); // 가림 대응 프레임
            Assert.That(Vector3.Distance(pilot.position, camera.position), Is.LessThan(normal - 2f)); // 벽 앞쪽으로 카메라 당김
            Vector3 screen = Camera.main.WorldToViewportPoint(pilot.position + Vector3.up * 0.5f); // 아리아 상체의 화면 위치
            Assert.That(screen.y, Is.InRange(0.03f, 0.97f)); // 가림 중 아리아 노출
            Object.Destroy(wall); // 임시 가림 해제
            yield return WaitFrames(65); // 카메라 원거리 복귀 대기
            Assert.That(Vector3.Distance(pilot.position, camera.position), Is.EqualTo(normal).Within(0.1f)); // 이전 카메라 거리 복원
        }

        [UnityTest] // 시험 씬의 일반탄 도달
        public IEnumerator StandardShotHitsCenterTargetFromOffsetMuzzle() // 화면 중앙 표적과 총구 시차 검증
        {
            yield return LoadFlight(); // 실제 시작 씬
            Camera camera = Camera.main; // 실제 조준 카메라
            GameObject target = GameObject.Find("NearPracticeTarget"); // 낮고 가까운 시험 표적
            Assert.That(target, Is.Not.Null); // 씬의 표적 배치
            Vector3 viewport = camera.WorldToViewportPoint(target.transform.position); // 조준점과 표적의 화면 좌표
            Assert.That(viewport.x, Is.EqualTo(0.5f).Within(0.02f)); // 화면 중앙 표적의 가로 거리
            Assert.That(viewport.y, Is.EqualTo(0.5f).Within(0.02f)); // 화면 중앙 표적의 세로 거리
            yield return WaitFrames(1); // 좌클릭 없이 자동 발사 시작
            yield return WaitFrames(86); // 원거리 마법탄 도착
            Assert.That(ReadTargetMetric(target, "HitCount"), Is.GreaterThan(0)); // 자동 기본탄 명중
            Assert.That(ReadTargetMetric(target, "LastDamage"), Is.EqualTo(8)); // 일반탄 시험 피해
        }

        [UnityTest] // 높이와 거리가 다른 표적의 집중탄
        public IEnumerator FocusedShotHitsElevatedTargetAfterMouseTurn() // 마우스 선회 후 원거리 조준
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject high = GameObject.Find("HighPracticeTarget"); // 측면 고고도 표적
            GameObject near = GameObject.Find("NearPracticeTarget"); // 첫 조준 위치의 표적
            Assert.That(high, Is.Not.Null); // 선택 가능한 고고도 표적
            Assert.That(high.transform.position.y, Is.GreaterThan(near.transform.position.y + 2f)); // 고도 차이 확인
            Assert.That(Vector3.Distance(Camera.main.transform.position, high.transform.position), Is.GreaterThan(Vector3.Distance(Camera.main.transform.position, near.transform.position))); // 거리 차이 확인
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(100f, 77.7778f) }); // 표적의 좌우·상하 시점을 가상 입력
            yield return WaitFrames(20); // 카메라 추적과 위치 안정
            SetKeys(Key.LeftShift); // 집중 마법탄 선택
            yield return WaitFrames(90); // 고고도 표적까지 진행
            Assert.That(ReadTargetMetric(high, "HitCount"), Is.GreaterThan(0)); // 고고도 표적 명중
            Assert.That(ReadTargetMetric(high, "LastDamage"), Is.InRange(20, 30)); // 누적을 반영한 집중탄 시험 피해
            SetKeys(); // 집중 입력 해제
        }

        private static int ReadTargetMetric(GameObject target, string property) // 실제 표적의 피해 확인
        {
            Component tracker = target.GetComponent("FlightPracticeTarget"); // 시험 표적 스크립트
            Assert.That(tracker, Is.Not.Null); // 피해 상태 컴포넌트 확인
            return (int)tracker.GetType().GetProperty(property).GetValue(tracker); // 표적의 실제 시험값
        }
        [UnityTest] // 프레임을 건너는 탄환과 피격 후 무적
        public IEnumerator FastThreatHitsOnceAndInvulnerabilityExpires() // 고속 적탄 관통 누락 확인
        {
            yield return LoadFlight(); // 실제 전투 시험 씬
            Transform pilot = FindPilot(); // 판정 대상의 실제 위치
            Component vitals = FlightVitals(pilot); // 체력과 판정 상태
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 한 프레임에 기체를 통과하는 적탄
            yield return WaitFrames(3); // 투사체 판정 대기
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 고속 적탄에 실제 피해
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 무적 구간의 두 번째 적탄
            yield return WaitFrames(3); // 두 번째 접촉 처리
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 기존 체력 유지
            Assert.That(Metric(vitals, "Grazes"), Is.Zero); // 스친 탄과 직접 접촉 구분
            yield return WaitFrames(65); // 1초 피격 무적 종료
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 무적 종료 뒤 다시 접촉
            yield return WaitFrames(3); // 마지막 피격 처리
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(60)); // 무적 해제 후 피해
        }

        [UnityTest] // 스친 탄과 피격하는 탄의 분리
        public IEnumerator NearMissAwardsOneGrazeButEventualHitDoesNot() // 그레이즈 중복 및 유효 범위 검증
        {
            yield return LoadFlight(); // 실제 전투 시험 씬
            Transform pilot = FindPilot(); // 현재 아리아 곡면 기준
            Component vitals = FlightVitals(pilot); // 그레이즈 횟수
            EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 피격 반경보다 바깥의 근접탄
            yield return WaitFrames(6); // 완전히 스칠 때까지 진행
            Assert.That(Metric(vitals, "Grazes"), Is.EqualTo(1)); // 같은 탄의 그레이즈 1회
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(100)); // 근접 회피 중 체력 유지
            EmitThreat(pilot.position + Vector3.right * 1.4f + Vector3.forward * 12f, Vector3.back, 1500f); // 근접 반경 밖의 먼 탄
            EmitThreat(pilot.position + Vector3.forward * 1.1f, Vector3.back, 27f); // 그레이즈 구간에 들어간 뒤 몸통을 맞히는 느린 탄
            yield return WaitFrames(8); // 스침 후보와 실제 명중 처리
            Assert.That(Metric(vitals, "Grazes"), Is.EqualTo(1)); // 나중에 맞은 탄의 후보 취소
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 느린 직접 피격 확인
        }

        [UnityTest] // 출격 실패와 키보드 재시작
        public IEnumerator DefeatStopsFlightAndRestartRestoresVitals() // 체력 소진과 재시작 검증
        {
            yield return LoadFlight(); // 실제 전투 시험 씬
            Transform pilot = FindPilot(); // 이동 여부를 확인할 비행체
            Component vitals = FlightVitals(pilot); // 초기 체력 상태
            for (int hit = 0; hit < 5; hit++) // 시험 체력 100 소진
            {
                EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 하나의 고속 적탄 발사
                yield return WaitFrames(65); // 각각에 대한 무적 종료 대기
            }
            Assert.That(Metric(vitals, "Health"), Is.Zero); // 시험 출격 실패
            Vector3 defeated = pilot.position; // 실패 상태의 기체 좌표
            SetKeys(Key.W); // 실패 뒤 이동 시도
            yield return WaitFrames(9); // 조작 반영 대기
            Assert.That(pilot.position, Is.EqualTo(defeated)); // 실패 중 움직임 차단
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 다음 게임 입력 프레임의 재시작 키 누름
            yield return WaitFrames(5); // 출격 상태 및 HUD 재설정
            SetKeys(); // 재시작 입력 해제
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(100)); // 체력 완전 복구
            Assert.That(Metric(vitals, "Grazes"), Is.Zero); // 이전 출격 그레이즈 제거
        }

        private static Component FlightVitals(Transform pilot) // 씬에 등록된 체력 데이터 찾기
        {
            Component vitals = pilot.GetComponent("FlightPilotVitals"); // 비행체가 소유한 체력 컴포넌트
            Assert.That(vitals, Is.Not.Null); // 실제 씬 연결 확인
            return vitals; // 테스트의 체력 상태
        }

        private static int Metric(Component component, string property) // 체력과 그레이즈의 실제 수치
        {
            return (int)component.GetType().GetProperty(property).GetValue(component); // 테스트가 읽는 실행 수치
        }

        private static void EmitThreat(Vector3 position, Vector3 direction, float speed) // 씬의 연습용 적탄 프리팹 호출
        {
            GameObject scene = GameObject.Find("ThreatPractice"); // 편집 가능한 적탄 연습 루트
            Assert.That(scene, Is.Not.Null); // 실제 씬에서 연습 코스 존재 여부
            Component course = scene.GetComponent("FlightThreatPractice"); // 적탄 시나리오 컴포넌트
            Assert.That(course, Is.Not.Null); // 씬의 적탄 설정 참조
            course.GetType().GetMethod("EmitTrial").Invoke(course, new object[] { position, direction, speed }); // 실제 적탄 발사 로직 사용
        }
        [UnityTest] // 조준·부채·고리·나선 배치 검증
        public IEnumerator ThreatPatternsProduceDistinctThreeDimensionalTrajectories() // 입체 탄막 방향과 발사 수 확인
        {
            yield return LoadFlight(); // 실제 시작 씬
            Component pool = GetThreatPool(); // 씬의 적탄 재사용 풀
            Component course = GameObject.Find("ThreatPractice").GetComponent("FlightThreatPractice"); // 씬 연습 코스
            var emit = course.GetType().GetMethod("EmitPattern"); // 지정한 탄막 패턴의 발사 경로
            Assert.That(emit, Is.Not.Null); // 실제 조준·부채·고리·나선 기능 연결
            var kind = emit.GetParameters()[0].ParameterType; // 어셈블리를 넘는 패턴 값
            int[] expected = { 1, 7, 12, 4 }; // 패턴별 기본 발사 수
            Vector3 center = new Vector3(0f, 26f, 10f); // 지형 위의 안전한 발사 위치
            for (int index = 0; index < expected.Length; index++) // 네 패턴 차례로 시험
            {
                object selection = System.Enum.ToObject(kind, index); // 선택한 탄막 데이터
                int fired = (int)emit.Invoke(course, new[] { selection, (object)center, Vector3.back }); // 씬의 실제 적탄 발사
                Assert.That(fired, Is.EqualTo(expected[index])); // 각 패턴의 발사 수 확인
                if (index == 2) // 고리의 3차원 횡방향 검사
                {
                    float top = 0f; // 고리 상단의 수직 성분
                    float bottom = 0f; // 고리 하단의 수직 성분
                    foreach (Transform bolt in pool.transform) // 재사용 더미의 모든 적탄
                    {
                        if (!bolt.gameObject.activeSelf) // 회수된 탄 제외
                        {
                            continue; // 현재 탄막만 방향 검증
                        }
                        top = Mathf.Max(top, bolt.forward.y); // 가장 높은 고리 각도
                        bottom = Mathf.Min(bottom, bolt.forward.y); // 가장 낮은 고리 각도
                    }
                    Assert.That(top, Is.GreaterThan(0.15f)); // 위로 벌어진 고리
                    Assert.That(bottom, Is.LessThan(-0.15f)); // 아래로 벌어진 고리
                }
                pool.GetType().GetMethod("ReturnAll").Invoke(pool, null); // 다음 패턴의 밝은 외형과 탄환 상태 초기화
            }
        }

        [UnityTest] // 적탄 풀 상한과 사용 수명 검증
        public IEnumerator ThreatPoolCapsConcurrencyAndReusesExpiredBolts() // 최대 탄수·회수·다음 발사 확인
        {
            yield return LoadFlight(); // 실제 시작 씬
            Component pool = GetThreatPool(); // 씬의 적탄 풀
            int capacity = Metric(pool, "Capacity"); // 씬에서 지정한 동시 활성 상한
            Assert.That(capacity, Is.EqualTo(128)); // 시험용 동시 탄수 상한 검증
            Vector3 origin = new Vector3(25f, 28f, 0f); // 플레이어와 충돌하지 않는 시험 좌표
            for (int i = 0; i < capacity + 15; i++) // 상한 초과 발사 시도
            {
                EmitThreat(origin, Vector3.forward, 1000f); // 사거리 끝에서 회수할 빠른 탄
            }
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(capacity)); // 동시 활성 상한 유지
            Assert.That(Metric(pool, "CreatedCount"), Is.EqualTo(capacity)); // 불필요한 게임 오브젝트 생성 방지
            yield return WaitFrames(14); // 탄환이 사거리에 도달하도록 대기
            Assert.That(Metric(pool, "ActiveCount"), Is.Zero); // 사거리 만료로 전체 회수
            EmitThreat(origin, Vector3.forward, 1f); // 이전 객체를 느린 새 탄으로 다시 사용
            yield return WaitFrames(3); // 남은 수명 초기화 확인
            Assert.That(Metric(pool, "CreatedCount"), Is.EqualTo(capacity)); // 새 오브젝트 생성 없이 재사용
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(1)); // 재사용한 새 탄이 활성 상태
            yield return WaitFrames(490); // 멀리 가지 않은 적탄의 최대 수명 대기
            Assert.That(Metric(pool, "ActiveCount"), Is.Zero); // 사거리와 독립된 시간 만료 회수
            Debug.Log($"FLIGHT_DAY05_POOL capacity={capacity} created={Metric(pool, "CreatedCount")} active={Metric(pool, "ActiveCount")}"); // 실제 상한과 회수 수치
        }

        private static Component GetThreatPool() // 씬에 배치된 적탄 풀 찾기
        {
            GameObject owner = GameObject.Find("ThreatBoltPool"); // 실제 Hierarchy의 풀 루트
            Assert.That(owner, Is.Not.Null); // 씬에서 풀 편집 가능 여부
            Component pool = owner.GetComponent("FlightThreatPool"); // 풀 상태를 읽을 컴포넌트
            Assert.That(pool, Is.Not.Null); // 풀 로직 연결
            return pool; // 테스트 대상 상태
        }
        [UnityTest] // 마물의 비행과 편집 가능한 씬 상태 확인
        public IEnumerator EnemyHasAnimatedWingsAndInitiallyHiddenHealthBar() // 저장된 시험 마물의 애니메이션
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 씬에 배치한 시험 마물
            Assert.That(enemy, Is.Not.Null); // 실제 씬에 마물 저장 확인
            Transform wing = enemy.transform.Find("Model/LeftWingPivot"); // 편집 가능한 날개 회전축
            Assert.That(wing, Is.Not.Null); // 날개 하이러키 배치
            Quaternion start = wing.localRotation; // 시작 날개 각도
            float animation = 0f; // 첫 각도에서 관찰한 최대 움직임
            for (int frame = 0; frame < 35; frame++) // 날개의 전체 비행 구간 확인
            {
                yield return null; // 날갯짓 실제 프레임
                animation = Mathf.Max(animation, Quaternion.Angle(start, wing.localRotation)); // 가장 크게 움직인 날개 각도
            }
            Assert.That(animation, Is.GreaterThan(6f)); // 시작 위상에 관계없이 애니메이션 확인
            Canvas bar = enemy.transform.Find("HealthBar").GetComponent<Canvas>(); // 제작 이미지가 놓인 월드 체력바
            Assert.That(bar.enabled, Is.False); // 첫 피격 전 체력 숨김
        }

        [UnityTest] // 실제 아군탄에 의한 체력 표시와 재숨김 검증
        public IEnumerator EnemyHealthBarAppearsOnHitAndHidesAfterQuietTime() // 명중 피해와 표시 수명
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 표적 역할의 마물 인스턴스
            Assert.That(enemy, Is.Not.Null); // 씬 배치 확인
            enemy.transform.position = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)).GetPoint(18f); // 가까운 조준 위치의 마물
            Physics.SyncTransforms(); // 실제 발사와 마물 판정 위치 동기화
            Component life = enemy.GetComponent("FlightEnemy"); // 마물의 실제 체력 컴포넌트
            Canvas bar = enemy.transform.Find("HealthBar").GetComponent<Canvas>(); // 월드 공간 체력 프레임
            Transform fill = bar.transform.Find("Fill"); // 피격에 따라 줄어드는 이미지
            float full = ((RectTransform)fill).rect.width; // 정규화된 최대 표시 너비
            for (int frame = 0; frame < 60 && Metric(life, "Health") == 80; frame++) // 첫 자동탄 피격까지만 기다림
            {
                yield return null; // 실제 탄환 진행
            }
            Assert.That(Metric(life, "Health"), Is.EqualTo(72)); // 실제 마물에 피해 8 반영
            Assert.That(bar.enabled, Is.True); // 첫 피격 후 표시 조건
            Assert.That(((RectTransform)fill).rect.width / full, Is.EqualTo(0.9f).Within(0.02f)); // 체력에 비례한 붉은 이미지 너비
            enemy.transform.position += Vector3.left * 25f; // 이후 자동탄을 피하는 시험 위치
            Physics.SyncTransforms(); // 재숨김 상태 검증의 새 위치
            yield return WaitFrames(130); // 마지막 피격 뒤 표시 시간 종료
            Assert.That(bar.enabled, Is.False); // 추가 피격 없이 체력바 재숨김
        }
        [UnityTest] // 세 일반 마물의 경로와 복수 패턴
        public IEnumerator EnemyTrialUsesDescendingSideAndPassingRoutes() // 출현·턴별 공격·이탈 검증
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject encounter = GameObject.Find("EnemyEncounter"); // 씬의 마물 시험 구성
            Assert.That(encounter, Is.Not.Null); // 편집 가능한 전투 그룹 존재
            Transform aimed = encounter.transform.Find("AimedEnemy"); // 아래로 내려가는 적
            Transform fan = encounter.transform.Find("FanEnemy"); // 옆으로 나가는 적
            Transform dive = encounter.transform.Find("DiverEnemy"); // 전방을 돌진해 지나가는 적
            Assert.That(aimed, Is.Not.Null); // 조준형 씬 인스턴스 확인
            Assert.That(fan, Is.Not.Null); // 확산형 씬 인스턴스 확인
            Assert.That(dive, Is.Not.Null); // 돌진형 씬 인스턴스 확인
            Assert.That(aimed.gameObject.activeSelf, Is.False); // 시험 키 입력 전 등장 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 실제 시험 시작 입력
            yield return WaitFrames(13); // 세 마물 출현 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 시험 키 해제
            Assert.That(aimed.gameObject.activeSelf && fan.gameObject.activeSelf && dive.gameObject.activeSelf, Is.True); // 각 유형의 실제 출현 확인
            Vector3 a = aimed.position; // 조준형 등장 위치
            Vector3 f = fan.position; // 확산형 등장 위치
            Vector3 b = dive.position; // 돌진형 등장 위치
            yield return WaitFrames(177); // 공격 두 번과 각 이동 경로 진행
            Assert.That(Metric(aimed.GetComponent("FlightEnemyRoute"), "PatternsFired"), Is.GreaterThanOrEqualTo(2)); // 조준형 공격 전환
            Assert.That(Metric(fan.GetComponent("FlightEnemyRoute"), "PatternsFired"), Is.GreaterThanOrEqualTo(2)); // 확산형 공격 전환
            Assert.That(Metric(dive.GetComponent("FlightEnemyRoute"), "PatternsFired"), Is.GreaterThanOrEqualTo(2)); // 돌진형 공격 전환
            Assert.That(aimed.position.y, Is.LessThan(a.y - 5f)); // 조준형이 아래로 이탈 중
            Assert.That(fan.position.x, Is.GreaterThan(f.x + 4f)); // 확산형이 옆으로 이탈 중
            Assert.That(dive.position.z, Is.LessThan(b.z - 20f)); // 돌진형이 전방으로 통과 중
            yield return WaitFrames(88); // 이탈 완료
            Assert.That(aimed.gameObject.activeSelf || fan.gameObject.activeSelf || dive.gameObject.activeSelf, Is.False); // 이탈 후 원본 비활성화
        }
        [UnityTest] // 이동 방향에 따른 조종사와 빗자루의 공중 자세
        public IEnumerator BroomAndPilotTiltTogetherWithoutTurningTheAim() // 좌우·상하 이동과 조준 분리
        {
            yield return LoadFlight(); // 실제 비행 시험 씬
            Transform pilot = FindPilot(); // 조준과 충돌의 비행체
            Transform broom = pilot.Find("TrainingBroom"); // 장착 부품이 달린 빗자루
            Transform rider = pilot.Find("PilotModels/Aria"); // 현재 조종사 모형
            Transform broomArt = broom.Find("Handle"); // 발사 지점과 분리된 빗자루 외형
            Transform muzzle = broom.Find("SpellMuzzle"); // 기존 마법 발사 지점
            Quaternion broomRest = broomArt.localRotation; // 원래 빗자루 자세
            Vector3 muzzleRest = pilot.InverseTransformPoint(muzzle.position); // 조종체 기준 발사 위치
            Quaternion aimRest = pilot.rotation; // 원래 조준 방향
            SetKeys(Key.D); // 오른쪽 비행 입력
            yield return WaitFrames(55); // 가속과 자세 회전 대기
            Assert.That(Quaternion.Angle(broomRest, broomArt.localRotation), Is.GreaterThan(10f)); // 빗자루도 오른쪽으로 기울기
            Assert.That(Quaternion.Angle(rider.localRotation * broomRest, broomArt.localRotation), Is.LessThan(1f)); // 탑승자와 빗자루의 같은 기울기
            Assert.That(Quaternion.Angle(aimRest, pilot.rotation), Is.LessThan(0.1f)); // 물리와 조준 회전 보존
            Assert.That(Vector3.Distance(muzzleRest, pilot.InverseTransformPoint(muzzle.position)), Is.LessThan(0.001f)); // 발사 위치 보존
            SetKeys(); // 좌우 입력 해제
            yield return WaitFrames(80); // 제동과 중립 복귀 대기
            Assert.That(Quaternion.Angle(broomRest, broomArt.localRotation), Is.LessThan(2f)); // 입력 해제 후 원래 자세
            SetKeys(Key.Space); // 상승 비행 입력
            yield return WaitFrames(55); // 상승과 자세 회전 대기
            Assert.That(Quaternion.Angle(broomRest, broomArt.localRotation), Is.GreaterThan(4f)); // 빗자루의 상승 자세
            Assert.That(Quaternion.Angle(aimRest, pilot.rotation), Is.LessThan(0.1f)); // 상승 중 조준 방향 보존
            SetKeys(); // 시험 입력 정리
        }

        [UnityTest] // 실제 출격에서 빗자루 부품과 발사 위치 확인
        public IEnumerator EquippedBroomBanksWithoutMovingTheMuzzle() // 장착 외형과 발사 판정 분리
        {
            yield return LoadOuterIslands(); // 실제 장착 부품이 있는 출격 씬
            Transform pilot = FindPilot(); // 물리와 조준의 비행체
            Transform broom = pilot.Find("TrainingBroom"); // 빗자루와 발사 지점
            Transform mounts = broom.Find("PartMounts"); // 모든 장착 부품의 표시 부모
            Transform muzzle = broom.Find("SpellMuzzle"); // 실제 제작 마법의 발사 지점
            Quaternion mountsRest = mounts.localRotation; // 장착 지점의 기본 방향
            Vector3 muzzleRest = pilot.InverseTransformPoint(muzzle.position); // 비행체 기준 발사 위치
            SetKeys(Key.A); // 왼쪽 비행 입력
            yield return WaitFrames(55); // 가속과 외형 회전 대기
            Assert.That(Quaternion.Angle(mountsRest, mounts.localRotation), Is.GreaterThan(10f)); // 장착 부품의 공중 기울기
            Assert.That(Vector3.Distance(muzzleRest, pilot.InverseTransformPoint(muzzle.position)), Is.LessThan(0.001f)); // 실제 발사 위치 보존
            SetKeys(); // 시험 입력 정리
        }

        [UnityTest] // 비행 연습에서 실제 렌더러와 재사용 적탄 확인
        public IEnumerator PilotBankAndThreatAppearancesFollowLiveFlight() // 자세와 피격 형상을 별도 검증
        {
            yield return LoadFlight(); // 입력과 적탄을 실제로 쓸 수 있는 비행 씬
            Transform pilot = FindPilot(); // 실제 비행 물리의 부모
            Transform aria = pilot.Find("PilotModels/Aria"); // 훈련의 초기 조종사 렌더 모형
            Vector3 center = Camera.main.WorldToViewportPoint(aria.Find("Face").position); // 화면 좌하단의 조종사 얼굴
            Assert.That(center.x, Is.LessThan(0.5f)); // 화면 중심 조준점을 가리지 않는 왼쪽 배치
            Assert.That(center.y, Is.LessThan(0.5f)); // 얼굴과 조준점 사이의 세로 분리
            var original = aria.localRotation; // 출발의 중립 비행 자세
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); // 오른쪽으로 실제 비행 시작
            yield return WaitFrames(34); // 가속으로 시각 기울기가 누적
            Assert.That(Quaternion.Angle(original, aria.localRotation), Is.GreaterThan(5f)); // 실제 이동으로 기울어진 메쉬
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이전 움직임 해제
            yield return WaitFrames(44); // 관성이 줄어든 중립 자세
            Assert.That(Quaternion.Angle(original, aria.localRotation), Is.LessThan(3f)); // 출발에서 같은 안정 자세
            Transform pool = GetThreatPool().transform; // 연습 모드와 보스가 공유하는 실제 적탄 풀
            Component practice = GameObject.Find("ThreatPractice").GetComponent("FlightThreatPractice"); // 시험 패턴 발사 진입점
            var emit = practice.GetType().GetMethod("EmitPattern"); // 실제 입력에서 사용하는 패턴 발사
            var returnAll = GetThreatPool().GetType().GetMethod("ReturnAll"); // 새 패턴 전에 회수 원본
            string[] patterns = { "Aimed", "Fan", "Ring", "Spiral", "Aimed" }; // 마지막 발로 회수 후 기본 외형 복원 확인
            string[] materials = { "ThreatEmber", "ThreatFan", "ThreatRing", "ThreatSpiral", "ThreatEmber" }; // 현재 패턴별 공유 파일
            var shapeMarker = new System.Collections.Generic.HashSet<(Mesh, Vector3)>(); // 형태와 크기가 다른 패턴 확인
            Transform reused = null; // 되돌려 쓴 탄을 직접 확인할 첫 표본
            for (int i = 0; i < patterns.Length; i++) // 정식 탄막 네 가지와 첫 탄 재사용
            {
                returnAll.Invoke(GetThreatPool(), null); // 이번 발사 전에 같은 풀에서 모두 회수
                object pattern = System.Enum.Parse(emit.GetParameters()[0].ParameterType, patterns[i]); // 실제 게임의 유형
                emit.Invoke(practice, new[] { pattern, (object)(pilot.position + pilot.forward * 10f), -pilot.forward }); // 실제 렌더·피격 적탄 생성
                foreach (Transform bolt in pool) // 활성 탄의 시각 상태 확인
                {
                    if (!bolt.gameObject.activeSelf) // 비활성 예비탄 무시
                    {
                        continue; // 표시하지 않는 재사용 탄
                    }
                    if (i == 0) // 첫 조준 단일발의 대상
                    {
                        reused = bolt; // 같은 조회에서 재사용되는지 확인
                    }
                    Transform visual = bolt.Find("PatternShape"); // 에디터에서 편집하는 탄막 외형
                    Assert.That(visual.GetComponent<MeshRenderer>().sharedMaterial.name, Is.EqualTo(materials[i])); // 재사용탄에 이전 색 잔류 금지
                    shapeMarker.Add((visual.GetComponent<MeshFilter>().sharedMesh, visual.localScale)); // 모양이 달라 색 이외에도 읽기 쉬운지 확인
                }
            }
            Assert.That(shapeMarker.Count, Is.EqualTo(4)); // 색과 함께 바뀌는 기하 네 가지
            Assert.That(reused, Is.Not.Null); // 실제 회수된 조준탄이 존재
            Assert.That(reused.Find("PatternShape").GetComponent<MeshRenderer>().sharedMaterial.name, Is.EqualTo("ThreatEmber")); // 같은 탄의 처음 외형으로 정상 복귀
        }

        [UnityTest] // 전투 게이지의 실제 상태와 생성 이미지
        public IEnumerator CombatHudShowsDamageAndKeepsAllGaugesLabeled() // HP와 자원별 표시 검증
        {
            yield return LoadFlight(); // 실제 비행 씬
            Transform hud = LocateCombatHud().transform; // 기존 안내와 별도 전투 HUD
            Transform hp = hud.Find("Health"); // 체력 게이지
            Transform mana = hud.Find("Mana"); // 마력 게이지
            Transform ultimate = hud.Find("Ultimate"); // 필살기 게이지
            Transform xp = hud.Find("Experience"); // 경험치 게이지
            Assert.That(HudText(hp.Find("Label")), Is.EqualTo("HP 100 / 100")); // 처음 체력 수치
            Assert.That(HudText(mana.Find("Label")), Is.EqualTo("MANA 100 / 100")); // 기본 비행 씬의 재화 장비 미적용 마력 표시
            Assert.That(HudText(ultimate.Find("Label")), Is.EqualTo("ULT 0 / 100")); // 초기 필살기 게이지
            Assert.That(HudText(xp.Find("Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 초기 레벨과 경험치
            foreach (Transform meter in new[] { hp, mana, ultimate, xp }) // 실제 채움 이미지 모두 확인
            {
                Component image = meter.Find("Fill").GetComponent("RawImage"); // 기본 색 사각형 대신 실제 텍스처
                Assert.That(image.GetType().GetProperty("texture").GetValue(image), Is.Not.Null); // 생성 파일이 실제 씬에 연결
            }
            float full = ((RectTransform)hp.Find("Fill")).rect.width; // 최대 HP 표시 폭
            Transform pilot = FindPilot(); // 현재 시험 기체
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 고속 적탄 피격
            yield return WaitFrames(4); // 체력과 HUD 업데이트 대기
            Assert.That(HudText(hp.Find("Label")), Is.EqualTo("HP 80 / 100")); // 피격 후 새 체력 수치
            Assert.That(((RectTransform)hp.Find("Fill")).rect.width / full, Is.EqualTo(0.8f).Within(0.02f)); // 채움 길이 80%
        }

        [UnityTest] // 스킬 슬롯의 부족 표시와 흰색 카운트다운
        public IEnumerator SkillHudDimsUnavailableArtAndDisplaysCooldown() // 세 슬롯과 시험 상태 반영
        {
            yield return LoadFlight(); // 실제 비행 씬
            Component hud = LocateCombatHud(); // 전투 HUD의 표시 상태
            Transform first = hud.transform.Find("SkillSlots/Q"); // Q 첫 스킬 슬롯
            Transform last = hud.transform.Find("SkillSlots/R"); // R 필살기 슬롯
            Assert.That(SkillArtColor(first).r, Is.GreaterThan(0.8f)); // 충분한 마력의 일반 아이콘
            Assert.That(SkillArtColor(last).r, Is.LessThan(0.55f)); // 에너지가 부족한 필살기 실루엣
            ((Behaviour)FindPilot().GetComponent("FlightSkillController")).enabled = false; // 표시값만 주입하는 독립 시험
            hud.GetType().GetMethod("SetResources").Invoke(hud, new object[] { 10, 100 }); // 마력 부족과 필살기 용량 상태
            hud.GetType().GetMethod("SetCooldown").Invoke(hud, new object[] { 0, 2.4f }); // 숫자 카운트다운 표시
            yield return WaitFrames(5); // 실제 HUD 표시 적용
            Assert.That(SkillArtColor(first).r, Is.LessThan(0.55f)); // 부족한 마력의 어두운 이미지
            Assert.That(SkillArtColor(last).r, Is.GreaterThan(0.8f)); // 충전된 필살기 아이콘
            Assert.That(HudText(first.Find("Cooldown")), Is.EqualTo("3")); // 흰색 정수 카운트다운 문구
            Component countdown = first.Find("Cooldown").GetComponent("Text"); // 제작 슬롯 안의 동적 숫자
            Color number = (Color)countdown.GetType().GetProperty("color").GetValue(countdown); // 실제 글자 색
            Assert.That(number, Is.EqualTo(Color.white)); // 카운트다운 흰색 표시
            hud.GetType().GetMethod("SetCooldown").Invoke(hud, new object[] { 0, 0.05f }); // 만료 직전 상태
            yield return WaitFrames(12); // 쿨타임 종료
            Assert.That(HudText(first.Find("Cooldown")), Is.Empty); // 카운트다운 종료 후 사라짐
        }

        private static Component LocateCombatHud() // 씬의 전투 UI 로직 탐색
        {
            GameObject root = GameObject.Find("CombatHUD"); // Hierarchy에 저장한 게이지 루트
            Assert.That(root, Is.Not.Null); // 실제 UI 배치 확인
            Component hud = root.GetComponent("FlightCombatHud"); // 게이지 표시 로직
            Assert.That(hud, Is.Not.Null); // 실제 게이지 연결 확인
            return hud; // 상태 검증에 사용하는 컴포넌트
        }

        private static string HudText(Transform child) // 현재 화면의 실제 글자
        {
            Component label = child.GetComponent("Text"); // 씬에 저장된 글자 노드
            return (string)label.GetType().GetProperty("text").GetValue(label); // 현재 상태에 표시된 값
        }

        private static Color SkillArtColor(Transform child) // 스킬 이미지의 실루엣 계산 확인
        {
            Component art = child.Find("Art").GetComponent("RawImage"); // 생성 스킬 이미지
            return (Color)art.GetType().GetProperty("color").GetValue(art); // 실제 이미지 표시 색
        }
        [UnityTest] // 좌클릭 없이 자동탄 명중과 독립 카드 순환 검증
        public IEnumerator AutomaticFireAndTwoCardLanesRotateIndependently() // 마법 카드와 다음 미리보기
        {
            yield return LoadFlight(); // 실제 게임 씬
            yield return WaitFrames(55); // 클릭 없이 기본탄 도달
            GameObject near = GameObject.Find("NearPracticeTarget"); // 화면 중앙 연습 표적
            Assert.That(ReadTargetMetric(near, "HitCount"), Is.GreaterThan(0)); // 기본탄 자동 명중
            GameObject cards = GameObject.Find("MagicCards"); // 오른쪽 아래 제작 카드 그룹
            Assert.That(cards, Is.Not.Null); // 씬의 실제 카드 배치
            Component deck = cards.GetComponent("FlightMagicDeck"); // 좌우 카드 순서 컴포넌트
            Assert.That(HudText(cards.transform.Find("LeftActive/Title")), Is.EqualTo("ARC")); // 왼쪽 시작 카드
            Assert.That(HudText(cards.transform.Find("LeftNext/Title")), Is.EqualTo("LANCE")); // 왼쪽 다음 카드
            Assert.That(HudText(cards.transform.Find("RightActive/Title")), Is.EqualTo("AEGIS")); // 오른쪽 시작 카드
            Assert.That(HudText(cards.transform.Find("RightNext/Title")), Is.EqualTo("NOVA")); // 오른쪽 다음 카드
            Assert.That(HudText(cards.transform.Find("LeftActive/ManaCost")), Is.EqualTo("12")); // 왼쪽 보석의 실제 마나
            Assert.That(HudText(cards.transform.Find("RightActive/ManaCost")), Is.EqualTo("14")); // 오른쪽 보석의 실제 마나
            yield return Click(1); // 실제 왼쪽 마우스 클릭
            Assert.That(Metric(deck, "LeftIndex"), Is.EqualTo(1)); // 왼쪽 카드만 한 칸 이동
            Assert.That(Metric(deck, "RightIndex"), Is.Zero); // 오른쪽 카드 보존
            Assert.That(HudText(cards.transform.Find("LeftActive/Title")), Is.EqualTo("LANCE")); // 다음 카드가 현재로 등장
            Assert.That(HudText(cards.transform.Find("LeftActive/ManaCost")), Is.EqualTo("18")); // 교체된 비용 보석 갱신
            yield return Click(2); // 실제 오른쪽 마우스 클릭
            Assert.That(Metric(deck, "LeftIndex"), Is.EqualTo(1)); // 왼쪽 순서 보존
            Assert.That(Metric(deck, "RightIndex"), Is.EqualTo(1)); // 오른쪽 순서만 이동
            Assert.That(HudText(cards.transform.Find("RightActive/Title")), Is.EqualTo("NOVA")); // 오른쪽 다음 카드가 현재로 등장
            Assert.That(HudText(cards.transform.Find("RightNext/Title")), Is.EqualTo("SHADE")); // 어둠 카드가 다음에 등장
            yield return Click(2); // 기본 오른쪽 덱의 두 번째 사용
            Assert.That(HudText(cards.transform.Find("RightActive/Title")), Is.EqualTo("SHADE")); // 어둠 카드가 실제로 올라옴
            Assert.That(HudText(cards.transform.Find("RightActive/ManaCost")), Is.EqualTo("16")); // 어둠 비용의 실제 보석 표시
            Component element = cards.transform.Find("RightActive/Element").GetComponent("RawImage"); // 어둠 속성 레이어
            Assert.That(((Texture)element.GetType().GetProperty("texture").GetValue(element)).name, Is.EqualTo("CardElementDark")); // 첨부 아이콘 실제 참조
            Component magic = FindPilot().GetComponent("FlightSkillController"); // 카드와 공유하는 실제 마력
            magic.GetType().GetField("mana", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(magic, 0f); // 마력이 부족한 시험 상태
            yield return Click(2); // 부족한 오른쪽 어둠 카드 시도
            Assert.That(Metric(deck, "RightIndex"), Is.EqualTo(2)); // 카드 사용 실패에서 순서 보존
        }

        [UnityTest] // 정사각형 스킬과 작고 바깥쪽에 놓인 카드
        public IEnumerator SkillKeysAndSharpHudStayAboveCardPreviews() // Q·E·R 실제 발동과 UI 레이아웃
        {
            yield return LoadFlight(); // 실제 UI 씬 로드
            Transform hud = LocateCombatHud().transform; // 제작 HUD
            Transform cards = hud.Find("MagicCards"); // 오른쪽 카드 영역
            Transform skills = hud.Find("SkillSlots"); // 세 스킬 위치
            RectTransform cardBounds = cards.GetComponent<RectTransform>(); // 카드 전체 크기
            Assert.That(((RectTransform)cards.Find("LeftActive")).rect.width, Is.EqualTo(128.8f).Within(0.01f)); // 현재 카드 너비 70%
            Assert.That(((RectTransform)cards.Find("LeftActive")).rect.height, Is.EqualTo(193.2f).Within(0.01f)); // 현재 카드 높이 70%
            Assert.That(((RectTransform)cards.Find("LeftNext")).rect.width, Is.EqualTo(100.8f).Within(0.01f)); // 미리보기 너비 70%
            Assert.That(((RectTransform)cards.Find("RightNext")).rect.height, Is.EqualTo(151.2f).Within(0.01f)); // 미리보기 높이 70%
            RectTransform skillBounds = skills.GetComponent<RectTransform>(); // 스킬 전체 크기
            Assert.That(skillBounds.position.y - skillBounds.rect.height * skillBounds.lossyScale.y * 0.02f, Is.GreaterThan(cardBounds.position.y + cardBounds.rect.height * cardBounds.lossyScale.y)); // 스킬로 카드를 가리지 않음
            Vector3[] corners = new Vector3[4]; // 화면의 실제 사각형 모서리
            ((RectTransform)skills.Find("R")).GetWorldCorners(corners); // 맨 오른쪽 스킬의 경계
            float skillRight = corners[3].x; // R 슬롯의 실제 오른쪽 끝
            ((RectTransform)cards.Find("RightNext")).GetWorldCorners(corners); // 오른쪽 다음 카드 바깥 경계
            Assert.That(skillRight, Is.EqualTo(corners[3].x).Within(0.5f)); // 서로 같은 오른쪽 끝
            Assert.That(cards.Find("LeftNext").position.x, Is.LessThan(cards.Find("LeftActive").position.x)); // 왼쪽 바깥 미리보기
            Assert.That(cards.Find("RightNext").position.x, Is.GreaterThan(cards.Find("RightActive").position.x)); // 오른쪽 바깥 미리보기
            Assert.That(((RectTransform)cards.Find("LeftNext")).rect.width, Is.LessThan(((RectTransform)cards.Find("LeftActive")).rect.width)); // 작은 다음 카드 너비
            foreach (string key in new[] { "Q", "E", "R" }) // 요청된 세 입력 슬롯
            {
                RectTransform rect = (RectTransform)skills.Find(key); // 씬의 정사각형 요소
                Assert.That(rect.rect.width, Is.EqualTo(rect.rect.height).Within(0.01f)); // 스킬은 정사각형 유지
                Assert.That(HudText(rect.Find("Key")), Is.EqualTo(key)); // 실제 키 문구 일치
            }
            Canvas canvas = hud.GetComponentInParent<Canvas>(); // 제작 텍스처와 글자를 그리는 캔버스
            Assert.That(canvas.pixelPerfect, Is.True); // 글자와 이미지의 픽셀 기준 정렬
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 실제 Q 스킬 입력
            yield return WaitFrames(6); // 쿨타임·자원 갱신
            Assert.That(HudText(skills.Find("Q/Cooldown")), Is.Empty); // 집중 포화 효과 중에는 쿨타임 이전
            Component weapon = FindPilot().GetComponent("FlightWeapon"); // 아리아의 실제 발사 간격
            Assert.That((float)weapon.GetType().GetProperty("FireIntervalScale").GetValue(weapon), Is.EqualTo(0.7f)); // 30% 짧아진 발사 간격
            Component magic = FindPilot().GetComponent("FlightSkillController"); // 실제 출격의 현재 마력과 장착 용량
            Assert.That(Metric(magic, "Mana"), Is.LessThan(Metric(magic, "MaxMana"))); // 장착 용량 대비 실제 마력 소모
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // Q 누름 해제
            yield return WaitFrames(181); // 집중 포화 종료 이후
            Assert.That(HudText(skills.Find("Q/Cooldown")), Is.Not.Empty); // 효과 뒤 시작되는 흰색 쿨타임
        }

        private IEnumerator Click(ushort button) // 실제 게임의 마우스 입력 프레임
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = button }); // 선택한 버튼 누름
            yield return WaitFrames(3); // 입력 업데이트 대기
            InputSystem.QueueStateEvent(mouse, new MouseState()); // 버튼 해제
            yield return WaitFrames(3); // 다음 입력 등록까지 대기
        }
        [UnityTest] // 아리아의 패시브 및 시험 출격 초기화
        public IEnumerator AriaRegularHitsStackAndDecayAfterMissing() // 기본·집중탄의 누적 화력
        {
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 속사형의 실제 비행체
            Component rapid = pilot.GetComponent("FlightRapidFire"); // 현재 누적 스크립트
            Assert.That(rapid, Is.Not.Null); // 씬에 편집 가능한 패시브 존재
            yield return WaitFrames(62); // 입력 없는 자동탄의 실제 명중
            Assert.That(Percent(rapid), Is.GreaterThan(0f)); // 자동탄의 실제 연속 적중
            var record = rapid.GetType().GetMethod("RecordHit"); // 실제 적중과 같은 갱신 경로
            for (int count = 0; count < 60; count++) // 한도 밖까지 누적
            {
                record.Invoke(rapid, null); // 연속 적중 재현
            }
            Assert.That(Percent(rapid), Is.EqualTo(50f)); // 최대 50% 상한
            var damage = (int)rapid.GetType().GetMethod("ScaleDamage").Invoke(rapid, new object[] { 8 }); // 최대 보너스의 실제 피해
            Assert.That(damage, Is.EqualTo(12)); // 기본 8에서 최대 +50%
            GameObject.Find("NearPracticeTarget").SetActive(false); // 명중을 중단할 실제 표적
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 발사 중단 상태 재현
            yield return WaitFrames(111); // 마지막 적중 이후 2초 이내
            Assert.That(Percent(rapid), Is.EqualTo(50f).Within(0.01f)); // 2초 유예
            yield return WaitFrames(80); // 2초를 넘겨 감소 시작
            Assert.That(Percent(rapid), Is.InRange(46.5f, 48.5f)); // 초당 2.5%p 감소
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 시험 출격 재시작
            yield return WaitFrames(4); // 초기화가 실제로 적용될 프레임
            Assert.That(Percent(rapid), Is.Zero); // 이전 출격의 피해 보너스 제거
        }

        private static float Percent(Component rapid) // 현재 화력 비율 확인
        {
            return (float)rapid.GetType().GetProperty("BonusPercent").GetValue(rapid); // 실제 패시브 표시값
        }
        [UnityTest] // 아리아 R 유성 탄막의 범위와 에너지
        public IEnumerator AriaMeteorClearsOnScreenThreatAndDamagesEnemy() // 실제 그레이즈 충전과 필살기
        {
            yield return LoadFlight(); // 실제 시작 씬
            Transform pilot = FindPilot(); // 기체 피격 중심
            for (int index = 0; index < 10; index++) // 필살기 에너지 100까지 충전
            {
                EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 실제 판정의 근접 회피 적탄
            }
            yield return WaitFrames(6); // 그레이즈의 에너지 수급
            Transform hud = LocateCombatHud().transform; // 전투 오버레이
            Assert.That(HudText(hud.Find("Ultimate/Label")), Is.EqualTo("ULT 100 / 100")); // 실제 에너지 충전 확인
            GameObject target = GameObject.Find("GlassfinEnemy"); // 시험 마물
            Camera camera = Camera.main; // 화면 안 탄막의 카메라
            EmitThreat(camera.ViewportPointToRay(new Vector3(0.64f, 0.63f)).GetPoint(20f), Vector3.forward, 1f); // 화면에 보이는 적탄
            EmitThreat(pilot.position + new Vector3(-40f, 10f, 3f), Vector3.forward, 1f); // 시야 바깥 적탄
            Component pool = GetThreatPool(); // 실제 남은 적탄
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(2)); // 두 위치의 적탄 준비
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 스킬 R 발동
            yield return WaitFrames(6); // 피해와 소거 대기
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(1)); // 보이는 적탄만 소거
            Assert.That(target.activeSelf, Is.False); // 시험 체력 80의 마물 피해
            Assert.That(HudText(hud.Find("Ultimate/Label")), Is.EqualTo("ULT 0 / 100")); // 에너지 100 소비
        }
        [UnityTest] // 시험 조작에서 두 캐릭터의 전환
        public IEnumerator RianSwitchesSkillArtAndWeakPointIndicator() // F2로 리안 선택 후 원복
        {
            yield return LoadFlight(); // 실제 씬과 저장된 아트 로드
            Transform hud = LocateCombatHud().transform; // 현재 HUD
            Transform slot = hud.Find("SkillSlots/Q"); // Q 전용 아이콘
            var art = slot.Find("Art").GetComponent<UnityEngine.UI.RawImage>(); // 현재 적용된 실제 그림
            var ariaArt = art.texture; // 원복할 아리아 원본 아이콘
            yield return SelectRian(); // 사용자가 누르는 실제 키
            Assert.That(art.texture, Is.Not.SameAs(ariaArt)); // 리안 Q 그림
            Assert.That(HudText(hud.Find("RapidBonus/Label")), Is.EqualTo("리안  약점 +50%")); // 리안 패시브 안내
            yield return SelectRian(); // 두 번째 입력으로 아리아 복구
            Assert.That(art.texture, Is.SameAs(ariaArt)); // 원래 그림과 속사 복귀
        }

        [UnityTest] // 집중 사격의 몸통과 약점 비교
        public IEnumerator RianFocusedBoltSeparatesExposedWeakPointFromBody() // 높이 차이가 20·30 피해로 반영
        {
            yield return LoadFlight(); // 실제 출격 경로
            yield return SelectRian(); // 저격 캐릭터 전환
            Transform pilot = FindPilot(); // 같은 비행체 기반
            Component weapon = pilot.GetComponent("FlightWeapon"); // 실제 자동·스킬 발사체
            ((Behaviour)weapon).enabled = false; // 측정 중 자동탄으로 인한 추가 피해 차단
            yield return WaitFrames(150); // 전환 이전에 발사된 탄의 최대 사거리 경과
            GameObject.Find("NearPracticeTarget").SetActive(false); // 앞의 기본 연습 표적 제거
            GameObject.Find("HighPracticeTarget")?.SetActive(false); // 고고도 표적 간섭 차단
            GameObject target = GameObject.Find("GlassfinEnemy"); // 프리팹이 저장된 시험 마물
            Transform marker = target.transform.Find("WeakPoint"); // 프리팹의 편집 가능한 약점
            Assert.That(marker, Is.Not.Null); // 별도 3D 약점
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 화면의 실제 중앙
            PlaceSniperTarget(target, ray.GetPoint(19f), 80); // 몸통만 중앙에 두고 체력 복원
            Physics.SyncTransforms(); // 이동한 콜라이더 등록
            InvokeFocusedCast(weapon); // 실제 집중 마법탄 생성
            yield return WaitFrames(48); // 월드 탄환이 표적에 도달
            Component life = target.GetComponent("FlightEnemy"); // 피해 적용
            Assert.That(Metric(life, "Health"), Is.EqualTo(60)); // 기본 집중 피해 20
            target.transform.position += ray.GetPoint(19f) - marker.position; // 같은 조준선에 노출된 약점 배치
            Physics.SyncTransforms(); // 약점 위치 즉시 반영
            InvokeFocusedCast(weapon); // 같은 집중탄으로 두 번째 측정
            yield return WaitFrames(48); // 새 명중 확인
            Assert.That(Metric(life, "Health"), Is.EqualTo(30)); // 약점 피해 30
        }

        [UnityTest] // 리안 전용 Q와 R의 실제 공간 범위
        public IEnumerator RianSkillsPierceTargetsButBeamStopsAtTerrain() // 관통 피해와 지형 뒤 탄막 보존
        {
            yield return LoadFlight(); // 실제 마물과 적탄 풀 로드
            yield return SelectRian(); // 리안 전투 설정
            Transform pilot = FindPilot(); // 기체의 실제 판정 중심
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 스킬 피해만 격리
            yield return WaitFrames(150); // 앞선 발사의 잔류 탄환 만료
            GameObject.Find("NearPracticeTarget").SetActive(false); // 기본 연습 표적 제거
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 실제 발사 방향 기준
            GameObject front = PlaceSniperTarget(GameObject.Find("GlassfinEnemy"), ray.GetPoint(16f), 400); // 첫 번째 중첩 관통 대상
            Transform muzzle = GameObject.Find("SpellMuzzle").transform; // 실제 발사 위치
            Physics.SyncTransforms(); // 선두 표적의 새 위치
            Assert.That(Physics.Raycast(ray, out RaycastHit impact, 85f, 1 << 9), Is.True); // 첫 표적 표면의 조준 접촉
            Vector3 direction = (impact.point - muzzle.position).normalized; // 총구에서 선두 표적 표면으로의 방향
            GameObject rear = PlaceSniperTarget(Object.Instantiate(front), impact.point + direction * 9f, 400); // 실제 직선 궤적 위의 두 번째 표적
            Physics.SyncTransforms(); // 관통 접촉 상태 준비

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 실제 관통 사격 키
            yield return WaitFrames(54); // 두 콜라이더를 통과할 시간
            Assert.That(Metric(front.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(300)); // 선두 적 한 번 피해
            Assert.That(Metric(rear.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(300)); // 뒤 적 한 번 피해
            Assert.That(HudText(LocateCombatHud().transform.Find("SkillSlots/Q/Cooldown")), Is.EqualTo("10")); // 시험 쿨타임

            for (int index = 0; index < 10; index++) // 실제 회피로 에너지 100 충전
            {
                EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 짧은 근접 회피 궤적
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // Q 해제
            yield return WaitFrames(6); // 그레이즈 자원 정산
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // 광선 중간의 실제 지형
            wall.layer = 8; // 지형 충돌 레이어
            wall.transform.position = ray.GetPoint(20f); // 두 표적 사이
            wall.transform.localScale = new Vector3(6f, 6f, 1f); // 광선과 벽의 접촉 면적
            Vector3 beamPoint = impact.point - direction; // 벽 앞쪽의 실제 광선 경로
            EmitThreat(beamPoint, direction, 0.001f); // 광선 안 적탄
            EmitThreat(beamPoint + Vector3.right * 4f, direction, 0.001f); // 광선 바깥 적탄
            EmitThreat(impact.point + direction * 10f, direction, 0.001f); // 벽 뒤 적탄
            Physics.SyncTransforms(); // 벽의 충돌 등록

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); // 실제 광선 필살기 키
            yield return WaitFrames(5); // 즉시 광선 피해와 적탄 회수
            Assert.That(Metric(front.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(80)); // 지형 앞 피해 220
            Assert.That(Metric(rear.GetComponent("FlightEnemy"), "Health"), Is.EqualTo(300)); // 지형 뒤 남은 체력
            Assert.That(Metric(GetThreatPool(), "ActiveCount"), Is.EqualTo(2)); // 광선 안의 앞쪽 탄만 소거
            Assert.That(HudText(LocateCombatHud().transform.Find("Ultimate/Label")), Is.EqualTo("ULT 0 / 100")); // 필살기 에너지 100 소비
        }

        [UnityTest] // 공용 보호막의 1회 피해 방어
        public IEnumerator RianGuardBlocksOnlyTheFirstThreat() // 같은 보호막에 연속 두 번 접촉
        {
            yield return LoadFlight(); // 보호막과 실제 피격 판정이 연결된 씬
            yield return SelectRian(); // 저격형 보호막 설정
            Transform pilot = FindPilot(); // 적탄 피격의 현재 중심
            Transform hud = LocateCombatHud().transform; // 남은 재사용 시간 표시
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 리안 E 발동
            yield return WaitFrames(4); // 보호막 자원과 효과 반영
            Assert.That(HudText(hud.Find("SkillSlots/E/Cooldown")), Is.EqualTo("12")); // 기획서의 12초 재사용 시간
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 방어할 첫 적탄
            yield return WaitFrames(4); // 첫 접촉 완료
            Assert.That(Metric(FlightVitals(pilot), "Health"), Is.EqualTo(100)); // 첫 피해 차단
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 계속 접근하는 두 번째 적탄
            yield return WaitFrames(4); // 두 번째 접촉 완료
            Assert.That(Metric(FlightVitals(pilot), "Health"), Is.EqualTo(80)); // 이미 사용한 보호막에는 추가 방어 없음
        }

        private IEnumerator SelectRian() // 프로필 전환 입력 한 번
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F2)); // 시험 캐릭터 전환 키
            yield return WaitFrames(3); // 프로필과 HUD 적용
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 누름 해제
            yield return WaitFrames(3); // 다음 입력 대기
        }

        private static void InvokeFocusedCast(Component weapon) // 집중 사격의 동일 충돌 경로
        {
            var cast = weapon.GetType().GetMethod("CastSpread"); // 실제 발사 API
            Assert.That(cast.Invoke(weapon, new object[] { true, 1, 0f, true }), Is.True); // 집중 자동탄 하나 발사
        }

        private static GameObject PlaceSniperTarget(GameObject target, Vector3 position, int health) // 체력과 조준 좌표 설정
        {
            target.SetActive(false); // 부활 시 체력 재설정
            target.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f)); // 노출 방향과 위치 적용
            Component life = target.GetComponent("FlightEnemy"); // 실제 마물 체력
            life.GetType().GetField("maxHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(life, health); // 피해 차이를 확인할 체력
            target.SetActive(true); // 시험 대상 활성화
            return target; // 실제 월드 오브젝트
        }


        [UnityTest] // 실제 처치로 생긴 3D 경험치 조각
        public IEnumerator ExperienceDropFallsCollectsAndCarriesAcrossLevels() // 세 배 크기와 연속 레벨업
        {
            yield return LoadFlight(); // 씬에 저장된 경험치 시스템
            Transform pilot = FindPilot(); // 조각을 수집할 실제 기체
            Transform hud = LocateCombatHud().transform; // 레벨과 경험치 게이지
            GameObject drops = GameObject.Find("ExperienceDrops"); // 씬의 편집 가능한 드롭 시스템
            Assert.That(drops, Is.Not.Null); // 실제 출격 씬에 경험치 연결
            Transform dropRoot = drops.transform; // 조각의 부모 하이러키
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 경험치를 주는 시험 마물
            Component life = enemy.GetComponent("FlightEnemy"); // 마물의 실제 처치 처리
            life.GetType().GetField("experienceReward", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(life, 35); // 한 번에 두 레벨을 넘길 시험값
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 실제 죽음에서 경험치 발생
            Assert.That(dropRoot.childCount, Is.EqualTo(1)); // 처치 한 번에 조각 하나
            Transform shard = dropRoot.GetChild(0); // 살아 있는 경험치 조각
            float height = shard.position.y; // 드롭 순간 고도
            Assert.That(shard.localScale.x, Is.EqualTo(0.48f).Within(0.01f)); // 기준 0.16의 세 배 표시
            yield return WaitFrames(62); // 공중에서 약 1초 낙하
            Assert.That(height - shard.position.y, Is.InRange(0.45f, 0.57f)); // 초당 0.5 월드 단위 낙하
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 획득 전에는 초기 수치
            pilot.position = shard.position; // 실제 기체와 조각의 접촉
            yield return WaitFrames(3); // 획득과 HUD 반영
            Assert.That(shard.gameObject.activeSelf, Is.False); // 획득 뒤 조각 풀 회수
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 3   XP 5 / 30")); // 연속 레벨업과 남은 경험치
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 현재 시험 출격 재시작
            yield return WaitFrames(4); // 출격 상태 초기화
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 새 출격 경험치 분리
        }

        [UnityTest] // 획득하지 않은 조각의 만료
        public IEnumerator ExperienceDropExpiresAndReusesItsView() // 만료 뒤 경험치 지급 방지와 풀 재사용
        {
            yield return LoadFlight(); // 실제 시작 씬
            GameObject drops = GameObject.Find("ExperienceDrops"); // 씬에서 편집할 처리 오브젝트
            Assert.That(drops, Is.Not.Null); // 실제 드롭 연결 확인
            Transform pilot = FindPilot(); // 획득 거리 밖 기체
            Transform hud = LocateCombatHud().transform; // 레벨업 여부 확인
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 경험치 드롭의 실제 공급원
            Component life = enemy.GetComponent("FlightEnemy"); // 반복 처치에 사용할 마물
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 첫 실제 처치
            Transform first = drops.transform.GetChild(0); // 고유 드롭 오브젝트
            yield return WaitFrames(488); // 8초의 공중 드롭 수명 경과
            Assert.That(first.gameObject.activeSelf, Is.False); // 채집되지 않은 조각의 회수
            pilot.position = first.position; // 만료된 조각과 겹치게 비행
            yield return WaitFrames(3); // 만료된 보상의 지급 여부 확인
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 만료 뒤 XP는 그대로
            enemy.SetActive(true); // 마물 시험 부활
            enemy.transform.position += Vector3.forward * 8f; // 두 번째 드롭만 기체와 떨어진 위치
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 같은 마물의 정상 재등장 후 처치
            Assert.That(drops.transform.childCount, Is.EqualTo(1)); // 추가 생성 없이 기존 조각 재사용
            Assert.That(first.gameObject.activeSelf, Is.True); // 반환된 조각이 다시 보임
        }

        [UnityTest] // 씬 상단 웨이브 표시와 실제 적 수
        public IEnumerator WaveHudCountsActiveEnemiesAndResetsElapsedTime() // 출격 시각과 웨이브 전환
        {
            yield return LoadFlight(); // 실제 시작 씬
            ((Behaviour)FindPilot().GetComponent("FlightWeapon")).enabled = false; // 표시 점검 중 임의 처치 방지
            Transform header = LocateCombatHud().transform.Find("WaveReadout"); // 편집 가능한 상단 중앙 HUD
            Assert.That(header, Is.Not.Null); // 씬에서 편집할 제목 그룹
            var layout = (RectTransform)header; // 그룹의 화면 기준 위치
            Assert.That(layout.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f))); // 화면 위쪽 가운데 고정
            Assert.That(layout.rect.width, Is.EqualTo(206.4f).Within(0.01f)); // 상단 전체의 가로 80%
            Assert.That(layout.rect.height, Is.EqualTo(76.8f).Within(0.01f)); // 상단 전체의 세로 80%
            Transform timer = header.Find("Timer"); // 큰 전투 시간
            Transform wave = header.Find("Wave"); // 시간 아래의 웨이브 번호
            Transform enemies = header.Find("Remaining"); // 아이콘 옆에 남은 적
            Assert.That(timer.GetComponent<UnityEngine.UI.Text>().fontSize, Is.GreaterThan(wave.GetComponent<UnityEngine.UI.Text>().fontSize)); // 타이머가 더 큰 글자
            Assert.That(timer.GetComponent<UnityEngine.UI.Text>().fontSize, Is.EqualTo(34)); // 43에서 80%로 바뀐 숫자
            Assert.That(wave.GetComponent<UnityEngine.UI.Text>().fontSize, Is.EqualTo(18)); // 22에서 80%로 바뀐 번호
            Assert.That(header.Find("MonsterIcon").GetComponent<UnityEngine.UI.RawImage>().texture, Is.Not.Null); // 원형 몬스터 그림
            Assert.That(HudText(wave), Is.EqualTo("Wave : 1")); // 첫 웨이브 번호
            Assert.That(HudText(enemies), Is.EqualTo(": 1")); // 첫 시험 적 한 마리
            yield return WaitFrames(64); // 1초 경과
            Assert.That(HudText(timer), Is.EqualTo("00:01")); // 정상적인 분·초 표기

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 실제 시험 웨이브 시작
            yield return WaitFrames(3); // 웨이브와 적 수 적용
            Assert.That(HudText(wave), Is.EqualTo("Wave : 2")); // 다음 시험 웨이브
            Assert.That(HudText(enemies), Is.EqualTo(": 3")); // 세 경로 마물
            Component life = GameObject.Find("AimedEnemy").GetComponent("FlightEnemy"); // 이번 웨이브의 실제 적
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 한 마리 처치
            yield return WaitFrames(3); // 처치 상태 반영
            Assert.That(HudText(enemies), Is.EqualTo(": 2")); // 실제로 남은 활성 마물
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 다시 출격
            yield return WaitFrames(4); // 초기 웨이브와 시간 적용
            Assert.That(HudText(timer), Is.EqualTo("00:00")); // 새 전투 시간
            Assert.That(HudText(wave), Is.EqualTo("Wave : 1")); // 첫 웨이브 복귀
            Assert.That(HudText(enemies), Is.EqualTo(": 1")); // 시작 표적 복귀
        }

        [UnityTest] // 연속 레벨업과 안전한 복귀
        public IEnumerator LevelUpChoicesPauseCombatAndPresentEveryEarnedLevel() // 선택 중 웨이브·조작 정지
        {
            yield return LoadFlight(); // XP와 카드 창이 부착된 실제 씬
            Transform pilot = FindPilot(); // 현재 비행체
            Transform canvas = LocateCombatHud().transform; // 전투와 보상 레이아웃
            Transform modal = canvas.Find("LevelUpChoices"); // 편집할 수 있는 실제 선택 창
            Assert.That(modal, Is.Not.Null); // 씬 편집된 카드 창
            Assert.That(modal.gameObject.activeSelf, Is.False); // 전투 시작 때 숨김
            GrantChoiceExperience(35); // 레벨 2와 3의 선택 둘 다 예약
            Assert.That(Time.timeScale, Is.Zero); // 실제 일시정지
            Assert.That(HudText(modal.Find("Heading")), Does.StartWith("레벨 2")); // 첫 번째 카드 차례
            string one = HudText(modal.Find("Choice1/Title")); // 세 선택지의 실제 이름
            string two = HudText(modal.Find("Choice2/Title")); // 중복 검증용 후보
            string three = HudText(modal.Find("Choice3/Title")); // 마지막 선택지
            Assert.That(new[] { one, two, three }, Is.Unique); // 동일 강화의 중복 등장 금지
            Vector3 start = pilot.position; // 선택 창을 띄울 때의 시작 위치
            string timer = HudText(canvas.Find("WaveReadout/Timer")); // 현재 출격 경과 시간
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Y, Key.F2)); // 모달 중 전투 조작 시도
            yield return WaitFrames(65); // 시간과 위치 유지 시험
            Assert.That(pilot.position, Is.EqualTo(start)); // 실제 비행 움직임 없음
            Assert.That(HudText(canvas.Find("WaveReadout/Wave")), Is.EqualTo("Wave : 1")); // Y가 새 웨이브를 만들지 않음
            Assert.That(HudText(canvas.Find("WaveReadout/Timer")), Is.EqualTo(timer)); // 일시정지 시간 제외
            modal.Find("Choice1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 마우스 버튼과 같은 선택 연결
            yield return WaitFrames(3); // 첫 번째 선택 적용
            Assert.That(HudText(modal.Find("Heading")), Does.StartWith("레벨 3")); // 남은 레벨업을 한 장씩 선택
            Assert.That(Time.timeScale, Is.Zero); // 두 번째 선택 전에는 아직 정지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit2)); // 실제 키보드 두 번째 카드
            yield return WaitFrames(4); // 게임 상태 복원
            Assert.That(modal.gameObject.activeSelf, Is.False); // 선택 창 종료
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 실제 전투 시간 복귀
        }

        [UnityTest] // 최대 단계와 후보 부족, E 교체 확인
        public IEnumerator LevelUpChoicesReplaceGuardAndFillMissingCandidates() // 대체 세 장과 원래 효과 해제
        {
            yield return LoadFlight(); // 실제 업그레이드 씬
            Transform canvas = LocateCombatHud().transform; // 보상 카드와 E 아이콘
            var upgrades = canvas.GetComponent("FlightUpgradeChoices"); // 출격 중 임시 성장
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; // 후보 시험용 필드
            Component crafted = canvas.GetComponent("FlightCraftedSpellBook"); // 제작 카드가 없는 기존 대체 후보 시험
            System.Array activeCrafted = (System.Array)crafted.GetType().GetField("runningLanes", flags).GetValue(crafted); // 실제 출격의 제작 카드
            activeCrafted.SetValue(null, 0); // 첫 제작 후보 제외
            activeCrafted.SetValue(null, 1); // 둘째 제작 후보 제외
            int[] ranks = (int[])upgrades.GetType().GetField("ranks", flags).GetValue(upgrades); // 포화에 도달한 시험 상태
            for (int index = 0; index < ranks.Length; index++) // 모든 강화 후보 한도 설정
            {
                ranks[index] = 3; // 일반·희귀·영웅의 상한 도달
            }
            ranks[5] = 0; // 교체할 E 산탄만 유효
            Transform pilot = FindPilot(); // 실제 보호막 적용 대상
            var vitals = FlightVitals(pilot); // 피격 후 남은 체력
            vitals.GetType().GetMethod("GrantSlotProtection").Invoke(vitals, new object[] { 1.4f }); // 활성화된 아리아 E 보호
            GrantChoiceExperience(10); // 첫 레벨업
            Transform modal = canvas.Find("LevelUpChoices"); // 실제 선택 UI
            Assert.That(HudText(modal.Find("Choice1/Title")), Is.EqualTo("산탄 스킬")); // 유효한 교체 카드
            Assert.That(HudText(modal.Find("Choice2/Title")), Is.EqualTo("필살기 충전")); // 대체 충전 후보
            Assert.That(HudText(modal.Find("Choice3/Title")), Is.EqualTo("체력 증가 1")); // 효과가 다른 최대 체력 후보
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1)); // E 교체 선택
            yield return WaitFrames(4); // 이전 E 해제와 아이콘 변경
            Assert.That((bool)upgrades.GetType().GetProperty("HasFan").GetValue(upgrades), Is.True); // 새 공용 스킬 적용
            Assert.That(canvas.Find("SkillSlots/E/Art").GetComponent<UnityEngine.UI.RawImage>().texture.name, Is.EqualTo("CardElementFire")); // 현재 슬롯에 맞는 그림
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 예전 E 보호가 남았는지 확인
            yield return WaitFrames(4); // 실제 피격 처리
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 이전 보호막이 새 슬롯에 남지 않음
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 새 출격
            yield return WaitFrames(4); // 성장 초기화
            Assert.That((bool)upgrades.GetType().GetProperty("HasFan").GetValue(upgrades), Is.False); // 이전 E 교체 제거
        }

        private static void GrantChoiceExperience(int amount) // 후보 상태만 검사하기 위한 경험치 지급
        {
            Component experience = GameObject.Find("ExperienceDrops").GetComponent("FlightExperience"); // 기존 출격 성장
            experience.GetType().GetMethod("Grant", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(experience, new object[] { amount }); // 레벨업 이벤트를 발생시키는 정상 처리
        }

        [UnityTest] // ESC 정지 UI와 출격 제어
        public IEnumerator EscapePauseMenuSwitchesPanelsAndRestartsAfterAbandon() // 정지·설정·재출격
        {
            yield return LoadFlight(); // 실제 시작 씬의 정지 화면
            Transform hud = LocateCombatHud().transform; // 전투 화면과 입력 메뉴
            Transform pause = hud.Find("PauseMenu"); // 편집 가능한 정지 메뉴
            Assert.That(pause, Is.Not.Null); // 실제 씬의 정지 메뉴
            Assert.That(pause.gameObject.activeSelf, Is.False); // 출격 중에 숨겨진 메뉴
            Transform pilot = FindPilot(); // 전투 멈춤을 비교할 비행체
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 ESC
            yield return WaitFrames(4); // 정지 프레임
            Assert.That(pause.gameObject.activeSelf, Is.True); // 메뉴 표시
            Assert.That(Time.timeScale, Is.Zero); // 전투 시간 정지
            Assert.That(AudioListener.pause, Is.True); // 실제 전투 음향 채널 정지
            Assert.That(HudText(pause.Find("MenuButtons/RunStatus/Timer")), Is.EqualTo(HudText(hud.Find("WaveReadout/Timer")))); // 정지 순간의 실제 시간
            Assert.That(HudText(pause.Find("MenuButtons/RunStatus/Wave")), Is.EqualTo("Wave : 1")); // 현재 웨이브 번호
            Assert.That(HudText(pause.Find("MenuButtons/RunStatus/Remaining")), Is.EqualTo(": 1")); // 첫 마물 수
            Assert.That(((RectTransform)pause.Find("Controls")).rect.width, Is.EqualTo(1124f).Within(0.01f)); // 안내선의 오른쪽 패널 너비
            Assert.That(((RectTransform)pause.Find("Controls")).rect.height, Is.EqualTo(820f).Within(0.01f)); // 안내선의 오른쪽 패널 높이
            Assert.That(((RectTransform)pause.Find("MenuButtons")).anchoredPosition.x, Is.EqualTo(-590f).Within(0.01f)); // 파란 왼쪽 버튼 열 위치
            Assert.That(((RectTransform)pause.Find("MenuButtons/Continue")).rect.width, Is.EqualTo(280f).Within(0.01f)); // 크게 이동한 첫 번째 버튼
            Assert.That(pause.Find("Controls/PanelArt").GetComponent<UnityEngine.UI.RawImage>().texture.name, Is.EqualTo("PausePanelArt")); // 새로 제작한 패널 배경
            var buttons = new[] { "Continue", "Restart", "Settings", "Abandon" }; // 화면에 배치한 순서
            for (int index = 1; index < buttons.Length; index++) // 버튼 사이 세 간격
            {
                var before = (RectTransform)pause.Find("MenuButtons/" + buttons[index - 1]); // 위쪽 버튼
                var after = (RectTransform)pause.Find("MenuButtons/" + buttons[index]); // 아래쪽 버튼
                Assert.That(before.anchoredPosition.y - after.anchoredPosition.y, Is.EqualTo(107f).Within(0.01f)); // 일정한 간격
                Assert.That(after.GetComponent<UnityEngine.UI.RawImage>().texture.name, Is.EqualTo("PauseButtonArt")); // 새로 제작한 버튼 배경
            }
            Assert.That(pause.Find("Controls/Keyboard").GetComponent<UnityEngine.UI.RawImage>().texture.name, Is.EqualTo("PauseKeyboard")); // 실제 벡터 키 배치
            Assert.That(pause.Find("Controls/Keyboard").GetComponent<UnityEngine.UI.RawImage>().texture.width, Is.EqualTo(1824)); // 원본 평면 키보드의 두 배 렌더 해상도
            Assert.That(pause.Find("Controls/Forward/Action").GetComponent<UnityEngine.UI.Text>().fontStyle, Is.EqualTo(FontStyle.Normal)); // 작은 글자의 실제 글꼴 획
            Vector3 position = pilot.position; // 멈춘 동안의 출발 좌표
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Y)); // 멈춘 전투에 비행과 웨이브 시도
            yield return WaitFrames(15); // 미입력 상태 확인
            Assert.That(pilot.position, Is.EqualTo(position)); // 비행 중지
            ClickPauseButton(pause, "MenuButtons/Settings"); // 왼쪽 설정 버튼의 실제 이벤트
            Assert.That(pause.Find("Settings").gameObject.activeSelf, Is.True); // 오른쪽 설정 열림
            Assert.That(pause.Find("Controls").gameObject.activeSelf, Is.False); // 오른쪽 조작 패널 교체
            ClickPauseButton(pause, "Settings/AudioTab"); // 사운드 및 마우스 감도 탭
            var volume = pause.Find("Settings/AudioSettings/Volume").GetComponent<UnityEngine.UI.Slider>(); // 전체 음량 제어
            volume.value = 0.65f; // 설정에서 조절한 값
            Assert.That(AudioListener.volume, Is.EqualTo(0.65f).Within(0.01f)); // 실제 전역 소리 변화
            ClickPauseButton(pause, "Settings/AudioSettings/Mute"); // 음소거 켜기
            Assert.That(AudioListener.volume, Is.Zero); // 실제 전체 소리 끄기
            ClickPauseButton(pause, "Settings/AudioSettings/Mute"); // 음소거 해제
            volume.value = 1f; // 다음 테스트에 영향 없는 음량 복원
            var sensitivity = pause.Find("Settings/AudioSettings/Sensitivity").GetComponent<UnityEngine.UI.Slider>(); // 시점 회전 감도
            sensitivity.value = 150f; // 변경한 설정값
            Component controller = pilot.GetComponent("FirstFlightController"); // 실제 비행 시점
            var scale = controller.GetType().GetField("sensitivityScale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 마우스 선회 배율
            Assert.That(scale.GetValue(controller), Is.EqualTo(1.5f)); // 실제 감도 값 적용
            sensitivity.value = 100f; // 다음 테스트에 영향 없는 감도 복원
            ClickPauseButton(pause, "MenuButtons/Continue"); // 계속하기 입력
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 누른 전투 키 해제
            yield return WaitFrames(4); // 재생 상태 복구
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 전투 다시 진행
            Assert.That(AudioListener.pause, Is.False); // 전투 음악과 효과 다시 진행
            Assert.That(pause.gameObject.activeSelf, Is.False); // 메뉴 닫힘
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 다시 정지
            yield return WaitFrames(4); // 새 메뉴 표시
            ClickPauseButton(pause, "MenuButtons/Abandon"); // 왼쪽 포기 선택
            ClickPauseButton(pause, "Confirm/Accept"); // 실제 포기 확정
            Assert.That(pause.Find("MenuButtons/Continue").GetComponent<UnityEngine.UI.Button>().interactable, Is.False); // 포기 뒤 계속하기 금지
            ClickPauseButton(pause, "MenuButtons/Restart"); // 새 시험 출격
            yield return WaitFrames(4); // 초기 화면 복귀
            Assert.That(pause.gameObject.activeSelf, Is.False); // 새 출격 화면
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 새 웨이브 실제 재생
            Assert.That(HudText(hud.Find("WaveReadout/Wave")), Is.EqualTo("Wave : 1")); // 첫 웨이브 초기화
            Assert.That(HudText(hud.Find("Experience/Label")), Is.EqualTo("LV. 1   XP 0 / 10")); // 경험치와 레벨 초기화
        }

        private static void ClickPauseButton(Transform panel, string path) // 실제 정지 메뉴의 버튼
        {
            panel.Find(path).GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 연결된 클릭 이벤트 실행
        }

        [UnityTest] // 설정의 키 변경이 각 화면과 실제 스킬에 반영
        public IEnumerator PauseKeyRebindUpdatesKeycapsAndActualSkillInput() // Q에서 G로 변경하고 원복
        {
            yield return LoadFlight(); // 실제 배포 시작 씬
            Transform pilot = FindPilot(); // 실제 자동 무기와 스킬 상태
            Transform hud = LocateCombatHud().transform; // 전투 스킬의 키 이미지
            Transform pause = hud.Find("PauseMenu"); // ESC로 여는 실제 설정 창
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 설정을 열기 전 정지
            yield return WaitFrames(3); // 메뉴 표시
            ClickPauseButton(pause, "MenuButtons/Settings"); // 현재 환경 설정
            ClickPauseButton(pause, "Settings/KeySettings/Special/Key"); // 고유기의 기존 키캡 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 이미 사용 중인 키
            yield return WaitFrames(3); // 중복 키 판정
            Assert.That(HudText(pause.Find("Settings/KeySettings/Special/Key/Label")), Is.EqualTo("Q")); // 이전 Q 유지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G)); // 비어 있는 새 키 선택
            yield return WaitFrames(3); // 키와 화면 다시 적용
            Assert.That(HudText(pause.Find("Settings/KeySettings/Special/Key/Label")), Is.EqualTo("G")); // 설정 키 이미지
            Assert.That(HudText(hud.Find("SkillSlots/Q/Key")), Is.EqualTo("G")); // 전투 HUD 이미지
            ClickPauseButton(pause, "Settings/Guide"); // 조작 안내로 이동
            Assert.That(HudText(pause.Find("Controls/Special/Keycap/Label")), Is.EqualTo("G")); // 설명 옆 키캡 갱신
            Component art = pause.Find("Controls/KeyboardHighlights").GetComponent("FlightKeyboardHighlights"); // 평면 그림의 실제 색 표시
            var marked = art.GetType().GetMethod("ContainsBinding"); // 설정 변화에 대응하는 강조 위치
            Assert.That(marked.Invoke(art, new object[] { Key.G }), Is.True); // 새 G 강조
            Assert.That(marked.Invoke(art, new object[] { Key.Q }), Is.False); // 기존 Q 강조 해제
            ClickPauseButton(pause, "MenuButtons/Continue"); // 이어서 전투
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 설정 입력 해제
            yield return WaitFrames(4); // 실제 전투 시간
            Component weapon = pilot.GetComponent("FlightWeapon"); // 실제 아리아 고유 효과
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 변경 전 키는 미발동
            yield return WaitFrames(3); // 실제 스킬 입력 경과
            Assert.That(weapon.GetType().GetProperty("FireIntervalScale").GetValue(weapon), Is.EqualTo(1f)); // 이전 키 동작 없음
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G)); // 설정한 키로 스킬 발동
            yield return WaitFrames(3); // 발사 속도 보너스 반영
            Assert.That(weapon.GetType().GetProperty("FireIntervalScale").GetValue(weapon), Is.EqualTo(0.7f)); // 새 키로 전투 기능 작동
        }

        [UnityTest] // 중간·최종 보스의 실제 처치 드롭 규칙
        public IEnumerator EnhancementDropFollowsBossRulesAndCollectsWithoutPause() // 확정 드롭과 비행 획득
        {
            yield return LoadFlight(); // 제작한 3D 조사 씬
            GameObject drops = GameObject.Find("EnhancementDrops"); // 강화 아이템 풀
            Assert.That(drops, Is.Not.Null); // 실제 출격에서 사용하는 드롭 오브젝트
            Component loot = drops.GetComponent("FlightEnhancements"); // 실제 드롭과 적용
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 중간·최종 보스로 설정할 시험 표적
            Component life = enemy.GetComponent("FlightEnemy"); // 처치와 보상 종류
            life.GetType().GetField("experienceReward", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(life, 0); // 강화 아이템만 독립 확인
            var tier = life.GetType().GetField("lootTier", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 씬에서 편집하는 보상 종류
            tier.SetValue(life, System.Enum.Parse(tier.FieldType, "FinalBoss")); // 최종 보스는 지급하지 않음
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 실제 처치
            Assert.That(Metric(loot, "ActiveCount"), Is.Zero); // 최종 보스에서 강화 아이템 없음
            enemy.SetActive(true); // 새 체력의 시험 표적
            enemy.transform.position += Vector3.up * 2f; // 경험치와 분리된 공중 위치
            tier.SetValue(life, System.Enum.Parse(tier.FieldType, "MidBoss")); // 중간 보스는 유효 아이템 확정
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 기존 실제 처리 경로
            Assert.That(Metric(loot, "ActiveCount"), Is.EqualTo(1)); // 처치 시 확정 한 개
            Transform item = drops.transform.GetChild(0); // 실제 드롭의 월드 위치
            float height = item.position.y; // 출현 고도
            yield return WaitFrames(62); // 다른 아이템과 같은 속도로 하강
            Assert.That(height - item.position.y, Is.InRange(0.45f, 0.56f)); // 낙하 정상
            Transform pilot = FindPilot(); // 마물 처치 전 위치
            Vector3 approach = (pilot.position - item.position).normalized; // 조각을 지나치지 않고 같은 쪽에서 접근
            pilot.position = item.position + approach * 1.3f; // 수집 반경보다 먼 위치
            yield return WaitFrames(3); // 실제 획득 사거리 판정
            Assert.That(item.gameObject.activeSelf, Is.True); // 반경 0.65 밖에 있는 드롭
            pilot.position = item.position; // 실제 획득 범위
            yield return WaitFrames(3); // 회수 상태 확인
            Assert.That(item.gameObject.activeSelf, Is.False); // 즉시 획득하고 풀로 복귀
            Assert.That((string)loot.GetType().GetProperty("LastPickup").GetValue(loot), Is.Not.Empty); // 실제로 획득한 효과와 등급
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 강화 아이템은 별도 선택 없이 획득
            enemy.SetActive(true); // 같은 보스의 다른 시험 처치
            enemy.transform.position = pilot.position + Vector3.up * 9f + Vector3.forward * 8f; // 수집하지 않을 두 번째 드롭
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 1000 }); // 강화 아이템을 한 번 더 드롭
            Assert.That(item.gameObject.activeSelf, Is.True); // 회수한 월드 오브젝트 재사용
            yield return WaitFrames(1140); // 드롭이 아직 만료되지 않는 19초
            Assert.That(item.gameObject.activeSelf, Is.True); // 유지 시간 20초 전
            yield return WaitFrames(68); // 20초를 지난 값
            Assert.That(item.gameObject.activeSelf, Is.False); // 만료되어 풀로 반환
        }

        [UnityTest] // 사격의 카드·아이템 공통 상한 및 보호막 순서
        public IEnumerator EnhancementRanksShareCardLimitsAndTimedShieldTakesPriority() // 화력·관통 상한과 보호막 갱신
        {
            yield return LoadFlight(); // 실제 강화 시스템 로드
            Component loot = GameObject.Find("EnhancementDrops")?.GetComponent("FlightEnhancements"); // 실제 아이템 효과
            Assert.That(loot, Is.Not.Null); // 씬에 연결한 강화
            Component upgrades = LocateCombatHud().GetComponent("FlightUpgradeChoices"); // 기존 카드의 공유 화력
            int[] cards = (int[])upgrades.GetType().GetField("ranks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(upgrades); // 시험 최대 카드 랭크
            cards[0] = 3; // 화력 카드 +30%
            Assert.That(ApplyEnhancement(loot, "Damage", 5), Is.True); // 화력 장비 +18%
            Assert.That(ApplyEnhancement(loot, "Damage", 5), Is.True); // 나머지 12%만 추가
            Assert.That((float)upgrades.GetType().GetMethod("DamageScale").Invoke(upgrades, new object[] { false }), Is.EqualTo(1.60f).Within(0.001f)); // 공통 피해 +60%
            Assert.That(ApplyEnhancement(loot, "Damage", 1), Is.False); // 상한 뒤 추가 아이템 제외
            cards[2] = 2; // 추가 관통 +2 기준
            Assert.That(ApplyEnhancement(loot, "Pierce", 5), Is.True); // 아이템은 남은 1만 추가
            Assert.That(Metric(upgrades, "AutoPierce"), Is.EqualTo(3)); // 공통 관통 상한 +3
            Assert.That(ApplyEnhancement(loot, "Shield", 3), Is.True); // 아이템 보호막 3회
            Assert.That(ApplyEnhancement(loot, "Shield", 2), Is.False); // 더 낮은 방어는 합산하지 않음
            Transform pilot = FindPilot(); // 실제 접촉 시험
            Component vitals = FlightVitals(pilot); // 현재 방어막과 체력
            vitals.GetType().GetMethod("GrantSlotProtection").Invoke(vitals, new object[] { 0.2f }); // 시간제 보호막 먼저 활성
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 보호막 동시 활성 시 접촉
            yield return WaitFrames(5); // 시간제 보호만 반영
            Assert.That(Metric(vitals, "ItemShield"), Is.EqualTo(3)); // 아이템 보호 횟수 유지
            yield return WaitFrames(15); // 시간제 보호 종료
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 아이템 보호만 남은 접촉
            yield return WaitFrames(4); // 방어막 자원 반영
            Assert.That(Metric(vitals, "ItemShield"), Is.EqualTo(2)); // 아이템 보호가 한 번 소모
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(100)); // 체력은 유지
        }

        private static bool ApplyEnhancement(Component loot, string kind, int grade) // 실제 획득 효과 경로를 직접 시험
        {
            var method = loot.GetType().GetMethod("TryApply", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 실제 효과 적용
            var type = method.GetParameters()[0].ParameterType; // 현재 아이템 종류
            return (bool)method.Invoke(loot, new[] { System.Enum.Parse(type, kind), (object)grade }); // 정해진 등급을 획득하는 검증
        }

        [UnityTest] // 두 보조 슬롯과 피격 보호 장치
        public IEnumerator EnhancementCardsEquipDroneBladesAndProtectionDevice() // 실제 추가 피해와 보호
        {
            yield return LoadFlight(); // 저장된 보조 장비와 각 공격
            Transform pilot = FindPilot(); // 실제 하이러키에 붙은 장비 대상
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 보조 공격만 독립 확인
            yield return WaitFrames(140); // 캐릭터 기본탄의 이전 발사 정리
            GameObject.Find("NearPracticeTarget").SetActive(false); // 드론에서 적까지 사선 확보
            GameObject enemy = GameObject.Find("GlassfinEnemy"); // 실제 공격 대상
            Component life = enemy.GetComponent("FlightEnemy"); // 이동한 마물의 실제 체력
            enemy.transform.position = pilot.position + Vector3.forward * 2.2f; // 칼날과 드론 사정거리
            Physics.SyncTransforms(); // 이동한 마물의 콜라이더 반영
            Component upgrades = LocateCombatHud().GetComponent("FlightUpgradeChoices"); // 보조 카드의 적용 경로
            ApplyGearCard(upgrades, "Drone"); // 첫 보조 슬롯
            ApplyGearCard(upgrades, "Blade"); // 두 번째 보조 슬롯
            ApplyGearCard(upgrades, "ProtectionDevice"); // 패시브 슬롯
            yield return WaitFrames(33); // 칼날 접촉과 보조 탄환 도달
            Assert.That(pilot.Find("AttackDrone").gameObject.activeSelf, Is.True); // 프로펠러 드론 활성화
            Assert.That(pilot.Find("OrbitBlades").gameObject.activeSelf, Is.True); // 실제 회전 칼날 표시
            Assert.That(Metric(life, "Health"), Is.EqualTo(66)); // 첫 칼날 8과 드론 탄 6
            Component vitals = FlightVitals(pilot); // 보조 보호 장치와 피해
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 장치가 충전될 첫 피해
            yield return WaitFrames(4); // 최초 피격 및 장치 충전
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 첫 피해 발생
            yield return WaitFrames(63); // 피격 후 공통 무적 시간이 지난 뒤
            EmitThreat(pilot.position + Vector3.forward * 12f, Vector3.back, 1500f); // 장치가 막을 새 탄
            yield return WaitFrames(4); // 실제 보호 장치 소모
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(80)); // 다음 피해 한 번 방어
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 새 출격에서 보조 슬롯 초기화
            yield return WaitFrames(4); // 이전 시제품 장비 제거
            Assert.That(pilot.Find("AttackDrone").gameObject.activeSelf, Is.False); // 드론 장착 초기화
            Assert.That(pilot.Find("OrbitBlades").gameObject.activeSelf, Is.False); // 칼날 장착 초기화
        }

        private static void ApplyGearCard(Component upgrades, string name) // 보조 카드의 실제 적용 경로
        {
            var method = upgrades.GetType().GetMethod("Apply", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 카드 결과 적용 메서드
            var reward = System.Enum.Parse(method.GetParameters()[0].ParameterType, name); // 지정한 장비 카드
            method.Invoke(upgrades, new[] { reward }); // 실제 선택과 같은 장착
        }

        [UnityTest] // 우상단 레이더와 전투 조작 설명 패널 제거
        public IEnumerator RadarTracksEnemyBearingWithoutGameplayGuide() // 실제 적 좌표의 방향·거리·제거
        {
            yield return LoadFlight(); // 저장된 새 레이더가 있는 시작 씬
            Assert.That(GameObject.Find("FlightControls"), Is.Null); // 전투 중 조작 설명 제거
            Assert.That(GameObject.Find("ControlBackplate"), Is.Null); // 전투용 옛 배경도 제거
            Transform hud = LocateCombatHud().transform; // 화면 위치 확인
            var radar = (RectTransform)hud.Find("EnemyRadar"); // 별도 우상단 레이더
            Assert.That(radar, Is.Not.Null); // 씬의 원형 레이더
            Assert.That(radar.anchorMin, Is.EqualTo(Vector2.one)); // 오른쪽 위 실제 고정
            Assert.That(hud.Find("WaveReadout"), Is.Not.Null); // 원래 상단 중앙 타이머 유지
            Transform pilot = FindPilot(); // 적 위치를 측정하는 기준
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 표적 이동의 중복 처치 방지
            Transform enemy = GameObject.Find("GlassfinEnemy").transform; // 첫 활성 표적
            Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized; // 실제 카메라의 수평 방향
            Vector3 right = Vector3.Cross(Vector3.up, forward); // 현재 미리보기 오른쪽
            enemy.position = pilot.position + right * 12f + forward * 5f; // 오른쪽 12·앞 5의 실제 거리 13
            yield return WaitFrames(2); // 이동한 적 위치 반영
            var contact = (RectTransform)radar.Find("Contact0"); // 해당 적의 실제 UI 위치
            Assert.That(contact.gameObject.activeSelf, Is.True); // 활성 표적 표시
            Assert.That(contact.anchoredPosition.x, Is.GreaterThan(0f)); // 오른쪽 점 위치
            Assert.That(contact.anchoredPosition.y, Is.GreaterThan(0f)); // 정면보다 살짝 오른쪽 앞
            Assert.That(HudText(contact.Find("Distance")), Is.EqualTo("13 m")); // 월드 위치를 이용한 직선 거리
            enemy.position = pilot.position - right * 12f - forward * 5f; // 반대편 왼쪽 뒤
            yield return WaitFrames(2); // 현재 위치 갱신
            Assert.That(contact.anchoredPosition.x, Is.LessThan(0f)); // 왼쪽으로 이동한 적
            Assert.That(contact.anchoredPosition.y, Is.LessThan(0f)); // 뒤쪽도 원에 표시
            enemy.gameObject.SetActive(false); // 이탈하거나 사라진 표적
            yield return WaitFrames(2); // 현재 표적 수 동기화
            Assert.That(contact.gameObject.activeSelf, Is.False); // 제거한 적의 레이더 점 삭제

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 시험 웨이브의 경로 마물 소환
            yield return WaitFrames(3); // 웨이브 진입
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 시험 키 해제
            Transform encounter = GameObject.Find("EnemyEncounter").transform; // 씬의 경로 마물 그룹
            Transform aimed = encounter.Find("AimedEnemy"); // 첫 거리 마커의 실제 마물
            Transform fan = encounter.Find("FanEnemy"); // 둘째 거리 마커의 실제 마물
            ((Behaviour)aimed.GetComponent("FlightEnemyRoute")).enabled = false; // 고정 위치를 이용한 겹침 확인
            ((Behaviour)fan.GetComponent("FlightEnemyRoute")).enabled = false; // 고정 위치를 이용한 거리 비교
            encounter.Find("DiverEnemy").gameObject.SetActive(false); // 겹침 검증에 필요한 적만 유지
            aimed.position = pilot.position + forward * 25f; // 표시상 가까운 위치의 먼 적
            fan.position = pilot.position + forward * 24f + right; // 표시와 글자가 겹치는 가까운 적
            yield return WaitFrames(2); // 글자 표시 우선순위 갱신
            Transform secondContact = radar.Find("Contact1"); // 두 번째 표시 슬롯
            Assert.That(contact.gameObject.activeSelf && secondContact.gameObject.activeSelf, Is.True); // 두 마커 모두 유지
            Assert.That(((Behaviour)contact.Find("Distance").GetComponent("Text")).enabled, Is.False); // 겹치는 먼 거리 숫자만 숨김
            Assert.That(((Behaviour)secondContact.Find("Distance").GetComponent("Text")).enabled, Is.True); // 가까운 거리 우선 표시
            aimed.position = pilot.position - right * 35f; // 글자가 겹치지 않는 위치
            yield return WaitFrames(2); // 거리 표시 복원
            Assert.That(((Behaviour)contact.Find("Distance").GetComponent("Text")).enabled, Is.True); // 적마다 읽을 수 있는 경우 함께 표시
        }

        [UnityTest] // 점수 지급과 재출격 회귀
        public IEnumerator ScoreCountsDefeatAndGrazeOnceAndClearsOnRestart() // 처치·스침·이탈과 출격 초기화
        {
            yield return LoadFlight(); // 저장된 전투 씬
            Transform score = LocateCombatHud().transform.Find("ScoreReadout"); // 연속 적중 아래의 점수 표시
            Assert.That(score, Is.Not.Null, "출격 점수 HUD"); // 전투 화면의 점수 노드
            Transform label = score.Find("Label"); // 점수 합계
            Assert.That(HudText(label), Is.EqualTo("점수 0")); // 새 출격의 점수 초기값
            Transform pilot = FindPilot(); // 탄막 스침의 기준
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 사격의 임의 처치 차단
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 첫 마물을 처치 없이 이탈시킴
            yield return WaitFrames(3); // 다음 시험 웨이브 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 시험 키 해제
            Assert.That(HudText(label), Is.EqualTo("점수 0")); // 씬 비활성화에는 보상 없음
            Component enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy").GetComponent("FlightEnemy"); // 실제 웨이브 적
            enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 1 }); // 적에게 피해만 준 상태
            Assert.That(HudText(label), Is.EqualTo("점수 0")); // 명중에는 처치 점수 없음
            enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 1000 }); // 실제 적 처치
            enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 1000 }); // 같은 적에게 들어온 중복 명중
            EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 그레이즈 한 건
            EmitThreat(pilot.position + Vector3.forward * 10f, Vector3.back, 1300f); // 점수를 주지 않는 직접 피격
            yield return WaitFrames(6); // 적탄 판정과 합계 갱신
            Assert.That(HudText(label), Is.EqualTo("점수 110")); // 처치 100과 유효 스침 10의 합
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 실제 재출격 키
            yield return WaitFrames(4); // 새 출격 초기화
            Assert.That(HudText(label), Is.EqualTo("점수 0")); // 이전 출격 점수 제거
            foreach (Transform popup in score.Find("Popups")) // 프레임에 남을 수 있는 이전 획득 숫자
            {
                Assert.That(popup.gameObject.activeSelf, Is.False); // 남은 획득 숫자도 삭제
            }
        }

        [UnityTest] // 획득 숫자 이동과 각 줄의 만료
        public IEnumerator ScorePopupsMoveUpAndExpireInOrderAfterPause() // 실제 점수 이벤트와 전투 시간 검증
        {
            yield return LoadFlight(); // 저장된 전투 씬
            Transform stack = LocateCombatHud().transform.Find("ScoreReadout/Popups"); // 씬에 배치한 획득 숫자 영역
            Assert.That(stack, Is.Not.Null, "획득 점수 숫자 영역"); // 전투 화면의 숫자 영역
            Transform pilot = FindPilot(); // 적탄 지나는 위치의 기준
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 표시 시간의 임의 처치 차단
            Component enemy = GameObject.Find("GlassfinEnemy").GetComponent("FlightEnemy"); // 첫 웨이브의 실제 적
            enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 1000 }); // 첫 획득 숫자 발생
            yield return WaitFrames(36); // 두 이벤트의 개별 만료 간격
            var older = (RectTransform)stack.Find("Popup0"); // 처음 등장한 숫자
            float firstY = older.anchoredPosition.y; // 새 숫자 전의 높이
            Assert.That(older.gameObject.activeSelf, Is.True); // 첫 점수 표시 상태
            Assert.That(HudText(older), Is.EqualTo("+100")); // 처치의 실제 획득 수치
            EmitThreat(pilot.position + Vector3.right * 0.65f + Vector3.forward * 12f, Vector3.back, 1500f); // 두 번째 획득 숫자
            yield return WaitFrames(9); // 새 줄과 기존 줄의 상승
            var newer = (RectTransform)stack.Find("Popup1"); // 아래에 생긴 새 획득 숫자
            Assert.That(HudText(newer), Is.EqualTo("+10")); // 그레이즈의 실제 획득 수치
            Assert.That(older.anchoredPosition.y, Is.GreaterThan(firstY)); // 이전 줄의 실제 위쪽 이동
            Assert.That(older.anchoredPosition.y, Is.GreaterThan(newer.anchoredPosition.y)); // 최신 숫자가 아래의 자리에 표시
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 전투 시간을 멈추는 실제 메뉴
            yield return WaitFrames(105); // 표시 시간보다 오래 유지
            Assert.That(Time.timeScale, Is.Zero); // 일시정지 입력 반영
            Assert.That(older.gameObject.activeSelf && newer.gameObject.activeSelf, Is.True); // 일시정지 중 숫자 수명 보존
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 키 해제
            yield return WaitFrames(2); // 복귀 입력 준비
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 전투 복귀
            yield return WaitFrames(60); // 첫 숫자만 수명을 넘긴 상태
            Assert.That(older.gameObject.activeSelf, Is.False); // 먼저 얻은 숫자가 먼저 사라짐
            Assert.That(newer.gameObject.activeSelf, Is.True); // 뒤에 얻은 숫자의 별도 수명
            yield return WaitFrames(41); // 새 숫자의 수명도 종료
            Assert.That(newer.gameObject.activeSelf, Is.False); // 남은 획득 숫자 종료
        }

        [UnityTest] // 배포 시작 씬의 자동 출현과 종료
        public IEnumerator OuterIslandsStartsScheduledWavesBeforeMidBoss() // 수동 키 없이 이어지는 첫 일반 전투
        {
            yield return LoadOuterIslands(); // 등록된 배포 시작 씬
            Transform pilot = FindPilot(); // 실제 비행체와 점수 지급 기준
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 출현 시간에 영향을 주는 자동 처치 제외
            Component vitals = pilot.GetComponent("FlightPilotVitals"); // 실제 피격 판정 대상
            vitals.GetType().GetMethod("GrantProtection").Invoke(vitals, new object[] { 100f }); // 웨이브 진행을 보기 위한 시험 보호 시간
            Component encounter = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 씬의 웨이브 실행기
            Assert.That(Metric(encounter, "WaveNumber"), Is.Zero); // 첫 출현 전 준비 구간
            Assert.That(Metric(encounter, "ActiveEnemyCount"), Is.Zero); // 안전한 준비 공간
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Y)); // 시험 씬 전용 웨이브 키 입력
            yield return WaitFrames(4); // 실전에서 일정이 바뀌는지 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 시험 키 해제
            Assert.That(Metric(encounter, "WaveNumber"), Is.Zero); // 시험 키 때문에 실전 일정이 바뀌지 않음
            Time.captureDeltaTime = 0.1f; // 80초짜리 일정의 재현 가능한 시험 속도
            yield return AdvanceStageTo(encounter, 3.2f); // 준비 구간의 첫 일반 적 시각
            Assert.That(Metric(encounter, "WaveNumber"), Is.EqualTo(1)); // 약한 적 한 마리의 자동 출현
            Assert.That(Metric(encounter, "ActiveEnemyCount"), Is.EqualTo(1)); // 첫 배치의 실제 적 수
            yield return AdvanceStageTo(encounter, 20.3f); // 첫 일반 전투 시작 시각
            Assert.That(Metric(encounter, "WaveNumber"), Is.EqualTo(2)); // 임의 입력 없이 다음 웨이브
            Assert.That(Metric(encounter, "ActiveEnemyCount"), Is.EqualTo(2)); // 조준형과 확산형 동시 출현
            Assert.That(HudText(LocateCombatHud().transform.Find("WaveReadout/Wave")), Is.EqualTo("Wave : 2")); // 기존 HUD와 실제 일정 연동
            yield return AdvanceStageTo(encounter, 79f); // 첫 전투와 회수 구간의 끝
            Assert.That(Metric(encounter, "WaveNumber"), Is.EqualTo(5)); // 보스 전에 진행하는 다섯 배치
            Assert.That(Metric(encounter, "ActiveEnemyCount"), Is.Zero); // 안전한 드롭 회수 구간
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace)); // 실제 새 출격 키
            yield return WaitFrames(4); // 준비 구간으로 복귀
            Assert.That(Metric(encounter, "WaveNumber"), Is.Zero); // 이전 웨이브와 종료 상태 초기화
            Assert.That(StageTime(encounter, "ElapsedSeconds"), Is.LessThan(1f)); // 새 전투 시계 시작
        }

        [UnityTest] // 중간 보스와 후속 일반 전투의 진행
        public IEnumerator OuterIslandsMidBossRewardsOnceAndStartsNextWaves() // 실제 등장·격파·보상·돌진형 전환
        {
            yield return LoadOuterIslands(); // 등록된 배포 경로
            Transform encounter = GameObject.Find("EnemyEncounter").transform; // 실전의 웨이브 부모
            Transform boss = encounter.Find("MidBoss"); // 씬에 배치한 실제 중간 보스
            Assert.That(boss, Is.Not.Null, "중간 보스 오브젝트"); // 보스 씬 연결
            Transform pilot = FindPilot(); // 보스의 공격 대상
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 임의 처치 차단
            Component vitals = pilot.GetComponent("FlightPilotVitals"); // 출격 체력과 적탄 판정
            vitals.GetType().GetMethod("GrantProtection").Invoke(vitals, new object[] { 210f }); // 항로 진행을 위한 시험 보호
            Component timeline = encounter.GetComponent("FlightEnemyEncounter"); // 실제 출격 시간표
            Time.captureDeltaTime = 0.1f; // 긴 항로를 재현하는 프레임 간격
            yield return AdvanceStageTo(timeline, 79f); // 회수 구간이 끝나기 전
            Assert.That(boss.gameObject.activeSelf, Is.False); // 보스 조기 출현 차단
            yield return AdvanceStageTo(timeline, 81.2f); // 예정된 중간 보스 등장
            Assert.That(boss.gameObject.activeSelf, Is.True); // 첫 전투 뒤 실제 보스 활성
            Component life = boss.GetComponent("FlightEnemy"); // 보스의 실제 체력과 보상
            Assert.That(Metric(life, "Health"), Is.EqualTo(600)); // 기획서의 초기 체력
            Assert.That(HudText(LocateCombatHud().transform.Find("WaveReadout/Wave")), Is.EqualTo("중간 보스")); // 기존 웨이브 HUD의 보스 상태
            yield return AdvanceStageTo(timeline, 119f); // 시험 목표 시각을 넘긴 전투
            Assert.That(boss.gameObject.activeSelf, Is.True); // 보스가 시간만으로 사라지지 않음
            Assert.That(Metric(timeline, "WaveNumber"), Is.EqualTo(6)); // 보스 생존 중 다음 웨이브 금지
            Component skills = pilot.GetComponent("FlightSkillController"); // 실제 필살기 에너지 보상
            Component loot = GameObject.Find("EnhancementDrops").GetComponent("FlightEnhancements"); // 보스의 강화 아이템
            Transform experience = GameObject.Find("ExperienceDrops").transform; // 경험치 드롭의 씬 계층
            int oldUltimate = Metric(skills, "Ultimate"); // 처치 직전 에너지
            Assert.That(Metric(life, "ExperienceReward"), Is.EqualTo(30)); // 보스의 시험 경험치
            float defeat = StageTime(timeline, "ElapsedSeconds"); // 실제로 격파한 시각
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 600 }); // 실제 보스 처치
            life.GetType().GetMethod("ReceiveHit").Invoke(life, new object[] { 600 }); // 중복된 처치 입력
            yield return WaitFrames(2); // 보스 탄막과 전환 상태 처리
            Assert.That(Metric(loot, "ActiveCount"), Is.EqualTo(1)); // 유효 후보가 있는 보스의 확정 드롭
            Assert.That(experience.childCount, Is.EqualTo(1)); // 보스 경험치 한 덩어리
            Assert.That(experience.GetChild(0).gameObject.activeSelf, Is.True); // 실제 획득할 수 있는 경험치 조각
            Assert.That(Metric(skills, "Ultimate"), Is.EqualTo(oldUltimate + 10)); // 일반 처치 에너지와 중복 없는 보상
            Assert.That(Metric(GetThreatPool(), "ActiveCount"), Is.Zero); // 보스 잔류 탄막 회수
            yield return AdvanceStageTo(timeline, defeat + 3.5f); // 보상을 회수할 여유
            Assert.That(Metric(timeline, "WaveNumber"), Is.EqualTo(6)); // 즉시 새 적 등장 차단
            yield return AdvanceStageTo(timeline, defeat + 4.5f); // 실제 격파 기준의 새 배치
            Assert.That(Metric(timeline, "WaveNumber"), Is.EqualTo(7)); // 중간 보스 뒤 일반 전투 시작
            Assert.That(encounter.Find("DiverEnemy").gameObject.activeSelf, Is.True); // 돌진형이 후속 전투에 합류
            yield return AdvanceStageTo(timeline, defeat + 65f); // 두 번째 일반 전투와 회수 구간 종료
            yield return WaitFrames(2); // 최종 보스 진입 전 상태 반영
            Assert.That((bool)timeline.GetType().GetProperty("IsStageComplete").GetValue(timeline), Is.False); // 최종 격파 전에 항로 완료 금지
            Assert.That(encounter.Find("FinalBoss").gameObject.activeSelf, Is.True); // 회수 구간 뒤 실제 최종 보스 진입
        }

        [UnityTest] // 최종 보스의 3페이즈와 출격 종료
        public IEnumerator OuterIslandsFinalBossClampsDamageAndClearsEveryPhase() // 실제 시간표·체력·탄막·완료를 연결
        {
            yield return LoadOuterIslands(); // 배포 항로 진입
            Transform encounter = GameObject.Find("EnemyEncounter").transform; // 실제 항로와 보스의 하이러키
            Transform boss = encounter.Find("FinalBoss"); // 최종 보스 씬 오브젝트
            Assert.That(boss, Is.Not.Null, "최종 보스 씬 오브젝트"); // 배치와 시작 경로 확인
            Transform hud = LocateCombatHud().transform.Find("BossReadout"); // 최종 보스의 전용 체력 표시
            Assert.That(hud, Is.Not.Null); // 편집 가능한 보스 HUD
            Assert.That(hud.gameObject.activeSelf, Is.False); // 보스가 없을 때 표시 숨김
            Transform pilot = FindPilot(); // 기존 전투 조작과 체력
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 무작위 자동 처치를 배제한 시간표 검증
            Component vitals = pilot.GetComponent("FlightPilotVitals"); // 보스 진행까지 생존할 시험 기체
            vitals.GetType().GetMethod("GrantProtection").Invoke(vitals, new object[] { 210f }); // 실제 시간표 검증용 보호
            Component timeline = encounter.GetComponent("FlightEnemyEncounter"); // 일차별 공통 전투 시간
            Time.captureDeltaTime = 0.1f; // 긴 일정의 재현 가능한 프레임 시간
            yield return AdvanceStageTo(timeline, 82f); // 첫 일반 전투 뒤 중간 보스
            Component mid = encounter.Find("MidBoss").GetComponent("FlightEnemy"); // 실제 와이드 웨이브의 관문
            mid.GetType().GetMethod("ReceiveHit").Invoke(mid, new object[] { 600 }); // 중간 보스 격파 경로
            float midDefeat = StageTime(timeline, "ElapsedSeconds"); // 후속 전투의 기준 시각
            yield return AdvanceStageTo(timeline, midDefeat + 64f); // 두 번째 회수 구간
            Assert.That(boss.gameObject.activeSelf, Is.False); // 회수 시간을 앞지르지 않는 최종 보스
            yield return AdvanceStageTo(timeline, midDefeat + 67.2f); // 최종 보스 진입과 첫 조준탄
            Component life = boss.GetComponent("FlightEnemy"); // 실제 적 체력과 격파 보상
            Component phases = boss.GetComponent("FlightFinalBoss"); // 체력 단계별 실제 공격
            Component pool = GetThreatPool(); // 공유하는 모든 실제 적탄
            var hit = life.GetType().GetMethod("ReceiveHit"); // 일반 사격과 관통탄의 공통 피해 경로
            Assert.That(GameObject.Find("FlightAudio/Music").GetComponent<AudioSource>().clip.name, Is.EqualTo("BossSquall")); // 실제 최종 보스의 전용곡
            Assert.That(Metric(life, "Health"), Is.EqualTo(1500)); // 최종 보스 시험 체력
            Assert.That(Metric(phases, "Phase"), Is.EqualTo(1)); // 첫 조준 페이즈
            Assert.That(Metric(pool, "ActiveCount"), Is.GreaterThan(0)); // 실제 1페이즈 적탄
            Assert.That(hud.gameObject.activeSelf, Is.True); // 활성 보스의 전용 체력바
            Assert.That(HudText(hud.Find("Health")), Is.EqualTo("1,500 / 1,500")); // 실제 전용 체력 수치
            Assert.That(HudText(hud.Find("PhaseDots/Dot1")), Is.EqualTo("●")); // 모두 남은 첫 점의 점등
            Assert.That(HudText(hud.Find("PhaseDots/Dot3")), Is.EqualTo("●")); // 마지막 점도 점등
            var fill = (RectTransform)hud.Find("Fill"); // 체력에 비례해 붉은 길이가 변하는 사각형
            float full = fill.rect.width; // 가득 찬 체력 길이

            hit.Invoke(life, new object[] { 3000 }); // 여러 경계를 넘는 큰 단일 피해
            hit.Invoke(life, new object[] { 3000 }); // 같은 프레임의 연속 피해
            float phaseTwo = StageTime(timeline, "ElapsedSeconds"); // 실제 첫 전환 시각
            Assert.That(Metric(life, "Health"), Is.EqualTo(1000)); // 초과 피해 폐기와 중복 피해 차단
            Assert.That(Metric(pool, "ActiveCount"), Is.Zero); // 첫 패턴의 모든 잔류탄 회수
            yield return WaitFrames(4); // 전환 중의 실제 프레임과 HUD 갱신
            hit.Invoke(life, new object[] { 100 }); // 전환 도중 별도의 탄환 피해
            Assert.That(Metric(life, "Health"), Is.EqualTo(1000)); // 전환 끝까지 체력 고정
            Assert.That(Metric(pool, "ActiveCount"), Is.Zero); // 이전 연사의 재발사 차단
            Assert.That(HudText(hud.Find("PhaseDots/Dot1")), Is.EqualTo("○")); // 끝난 페이즈 소등
            Assert.That(HudText(hud.Find("PhaseDots/Dot2")), Is.EqualTo("●")); // 남은 두 점 유지
            Assert.That(fill.rect.width / full, Is.EqualTo(2f / 3f).Within(0.015f)); // 경계에 맞춰 체력바 감소
            yield return AdvanceStageTo(timeline, phaseTwo + 1.6f); // 전환 뒤 원형 확산 발사
            Assert.That(Metric(phases, "Phase"), Is.EqualTo(2)); // 두 번째 페이즈 활성
            Assert.That(Metric(pool, "ActiveCount"), Is.GreaterThanOrEqualTo(12)); // 실제 원형 확산

            hit.Invoke(life, new object[] { 3000 }); // 두 번째 체력 경계 타격
            float phaseThree = StageTime(timeline, "ElapsedSeconds"); // 두 번째 전환 시각
            Assert.That(Metric(life, "Health"), Is.EqualTo(500)); // 마지막 페이즈까지 초과 피해 차단
            Assert.That(Metric(pool, "ActiveCount"), Is.Zero); // 두 번째 패턴 회수
            yield return AdvanceStageTo(timeline, phaseThree + 1.55f); // 좌우 교차 나선이 발사되는 때
            Assert.That(Metric(phases, "Phase"), Is.EqualTo(3)); // 마지막 페이즈
            Assert.That(Metric(pool, "ActiveCount"), Is.GreaterThanOrEqualTo(8)); // 두 발사점의 실제 교차탄
            Assert.That(HudText(hud.Find("PhaseDots/Dot2")), Is.EqualTo("○")); // 두 번째 점 소등
            Assert.That(HudText(hud.Find("PhaseDots/Dot3")), Is.EqualTo("●")); // 마지막 점 점등
            Component rewards = pilot.GetComponent("FlightSkillController"); // 기존 공통 처치 에너지
            Component drops = GameObject.Find("EnhancementDrops").GetComponent("FlightEnhancements"); // 강화 아이템의 실시간 기록
            int previousUltimate = Metric(rewards, "Ultimate"); // 최종 처치 직전 자원
            int previousDrops = Metric(drops, "ActiveCount"); // 최종 처치 직전 강화 수
            hit.Invoke(life, new object[] { 500 }); // 최종 격파
            hit.Invoke(life, new object[] { 500 }); // 중복 정산 방지 확인
            float finished = StageTime(timeline, "ElapsedSeconds"); // 결과에 보관할 마지막 전투 시간
            yield return WaitFrames(5); // 종료와 화면 정리 후 프레임
            Assert.That((bool)timeline.GetType().GetProperty("IsStageComplete").GetValue(timeline), Is.True); // 최종 격파가 출격 완료를 결정
            Assert.That(StageTime(timeline, "ElapsedSeconds"), Is.EqualTo(finished)); // 결과 이후 시계 정지
            Assert.That(StageTime(timeline, "TotalSeconds"), Is.GreaterThanOrEqualTo(finished)); // 전체 시계에 전투 시간 포함
            Assert.That(Metric(rewards, "Ultimate"), Is.EqualTo(previousUltimate + 20)); // 최종 보스 에너지 한 번 지급
            Assert.That(Metric(drops, "ActiveCount"), Is.EqualTo(previousDrops)); // 최종 강화 아이템 미지급
            Assert.That(Metric(pool, "ActiveCount"), Is.Zero); // 마지막 페이즈 적탄 회수
            Assert.That(hud.gameObject.activeSelf, Is.False); // 완료 뒤 HUD 숨김
            Transform completed = LocateCombatHud().transform.Find("SortieEnd"); // 최종 보스 격파에서 켜지는 결과 진입
            Assert.That(completed.gameObject.activeSelf, Is.True); // 결과 버튼의 실제 전투 조건
            ClickPauseButton(completed, "Open"); // 클리어한 출격 결과 이동
            yield return WaitForActiveScene("Assets/Scenes/FlightSortieResult.unity"); // 기존 전투와 분리한 결과
            Transform result = GameObject.Find("SortieResult").transform; // 실제 결과 하이러키
            Assert.That(GameObject.Find("FlightAudio/Music").GetComponent<AudioSource>().clip.name, Is.EqualTo("StageClear")); // 실제 완료 결과곡
            Assert.That(HudText(result.Find("Panel/Outcome")), Is.EqualTo("출격 완료")); // 실패와 다른 실제 성공 표시
            Assert.That(result.Find("PortraitArea/StandingIllustration").GetComponent<RawImage>().texture.name, Is.EqualTo("AriaStanding")); // 클리어에서도 실제 조종사 일러스트 적용
            Assert.That(long.Parse(HudText(result.Find("Panel/Score")), System.Globalization.NumberStyles.Number), Is.GreaterThan(0)); // 최종 격파의 실제 점수 전달
            ClickPauseButton(result, "Panel/Title"); // 결과에서 메인 화면 복귀
            yield return WaitForActiveScene("Assets/Scenes/FlightMainMenu.unity"); // 메인 씬의 정상 전환
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("FlightMainMenu")); // 기존 전투 대신 메인 화면 표시
        }

        [UnityTest] // 보스의 교대 패턴과 공격 후 약점
        public IEnumerator OuterIslandsMidBossAlternatesAndOpensARealWeakPoint() // 패턴 간 회피 공간과 약점 피해
        {
            yield return LoadOuterIslands(); // 배포 항로
            Transform encounter = GameObject.Find("EnemyEncounter").transform; // 보스와 웨이브 실행기
            Transform boss = encounter.Find("MidBoss"); // 실제 보스 참조
            Assert.That(boss, Is.Not.Null, "중간 보스 오브젝트"); // 씬 보스 연결
            Transform pilot = FindPilot(); // 실제 피격 대상
            Component weapon = pilot.GetComponent("FlightWeapon"); // 약점 시험에 사용하는 실제 마법탄
            ((Behaviour)weapon).enabled = false; // 임의 자동 사격 차단
            Component vitals = pilot.GetComponent("FlightPilotVitals"); // 초기 항로를 통과할 플레이어
            vitals.GetType().GetMethod("GrantProtection").Invoke(vitals, new object[] { 100f }); // 시험 항로 중의 생존 유지
            Component timeline = encounter.GetComponent("FlightEnemyEncounter"); // 실제 보스 출현 시각
            Time.captureDeltaTime = 0.1f; // 출현 직전까지 진행
            yield return AdvanceStageTo(timeline, 80.2f); // 보스가 진입하는 중
            Transform weak = boss.Find("WeakPoint"); // 표시와 콜라이더를 공유하는 약점
            Assert.That(weak, Is.Not.Null); // 편집 가능한 보스 약점
            Assert.That(weak.gameObject.activeSelf, Is.False); // 입장 및 공격 중 약점 닫힘
            vitals.GetType().GetMethod("ResetRun").Invoke(vitals, null); // 이전 웨이브의 시험 보호 제거
            pilot.position = new Vector3(0f, 9.07f, 20f); // 첫 직선 연사의 경로 위 기체 위치
            Component pattern = boss.GetComponent("FlightMidBoss"); // 보스의 실제 공격 실행
            yield return AdvanceStageTo(timeline, 82.45f); // 직선 연사 뒤 첫 약점 노출
            Assert.That(Metric(pattern, "AimedBursts"), Is.EqualTo(4)); // 한 방향 네 발 연사
            Assert.That(Metric(vitals, "Health"), Is.EqualTo(90)); // 실제 보스 탄막 한 발의 피해 10
            Assert.That(weak.gameObject.activeInHierarchy, Is.True); // 연사 종료 뒤 약점 표시 및 판정
            Component life = boss.GetComponent("FlightEnemy"); // 약점에 연결된 실제 보스 체력
            Component template = (Component)weapon.GetType().GetField("standardBolt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(weapon); // 전투에 쓰는 마법탄 프리팹
            Physics.SyncTransforms(); // 활성화된 약점 콜라이더 등록
            Vector3 heading = (weak.position - pilot.position).normalized; // 보이는 쪽에서 약점으로 조준
            Component shot = Object.Instantiate(template, weak.position - heading * 3f, Quaternion.LookRotation(heading)); // 실제 마법탄을 약점 방향으로 발사
            shot.GetType().GetMethod("Begin").Invoke(shot, new object[] { (LayerMask)(1 << 9), 12f, null, 0f, 0, 1f, 1f, 0, 20 }); // 기준 피해 20의 비관통 마법탄
            yield return WaitFrames(3); // 실제 약점 피격 처리
            Assert.That(Metric(life, "Health"), Is.EqualTo(570)); // 약점의 시험 보너스 +50%
            yield return AdvanceStageTo(timeline, 83.6f); // 노출 1초 뒤 다음 패턴 준비
            Assert.That(weak.gameObject.activeSelf, Is.False); // 공격 전 약점 판정 종료
            yield return AdvanceStageTo(timeline, 84.4f); // 부채꼴 연속 발사 확인
            Assert.That(Metric(pattern, "FanBursts"), Is.EqualTo(2)); // 실제 부채꼴 2회
            Transform pool = GetThreatPool().transform; // 실제 활성 적탄의 방향
            int active = 0; // 적탄 방향 표본 수
            foreach (Transform bolt in pool) // 현재 풀에 있는 항로 적탄
            {
                if (bolt.gameObject.activeSelf) // 발사 중인 탄만 사용
                {
                    Assert.That(Mathf.Abs(bolt.forward.y), Is.LessThan(0.12f)); // 상하 회피 공간을 남기는 고도 제한
                    active++; // 회피 각도에 사용한 실제 발사체 수
                }
            }
            Assert.That(active, Is.GreaterThanOrEqualTo(7)); // 넓게 펼쳐진 실제 부채꼴 탄환
        }

        [UnityTest] // 일시정지와 레벨업 선택에서 두 시간 기록 분리
        public IEnumerator OuterIslandsCountsPauseAndCardTimeOnlyInTotal() // 실전 시계 두 개의 실제 동작
        {
            yield return LoadOuterIslands(); // 저장된 새 항로
            Component encounter = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 실전의 출격 기록
            Transform hud = LocateCombatHud().transform; // 기존 HUD와 새 전체 시간 줄
            Assert.That(hud.Find("WaveReadout/Total"), Is.Not.Null); // 별도 전체 소요 시간 표시
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 일시정지 입력
            yield return WaitFrames(3); // 정지 상태 확정
            float active = StageTime(encounter, "ElapsedSeconds"); // 정지 시점의 전투 진행 시간
            float total = StageTime(encounter, "TotalSeconds"); // 정지 시점의 전체 소요 시간
            yield return new WaitForSecondsRealtime(0.12f); // 전투 시간을 쓰지 않는 대기
            Assert.That(StageTime(encounter, "ElapsedSeconds"), Is.EqualTo(active)); // 일시정지 중 전투 시계 보존
            Assert.That(StageTime(encounter, "TotalSeconds"), Is.GreaterThan(total + 0.06f)); // 전체 소요에 정지 포함
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // ESC 키 해제
            yield return WaitFrames(2); // 입력 정리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 계속하기 동작
            yield return WaitFrames(5); // 전투 시계 재개
            Assert.That(StageTime(encounter, "ElapsedSeconds"), Is.GreaterThan(active)); // 정지 해제 뒤 전투 시간 재개
            Component experience = GameObject.Find("ExperienceDrops").GetComponent("FlightExperience"); // 경험치 드롭의 실제 레벨업 경로
            experience.GetType().GetMethod("Grant", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(experience, new object[] { 10 }); // 선택 창이 열리는 경험치 지급
            yield return WaitFrames(4); // 실제 보상 선택 화면과 시간 동결
            Assert.That(hud.Find("LevelUpChoices").gameObject.activeInHierarchy, Is.True); // 레벨업 선택 화면 활성
            active = StageTime(encounter, "ElapsedSeconds"); // 카드 선택 시작 때의 전투 시간
            total = StageTime(encounter, "TotalSeconds"); // 카드 선택 시작 때의 전체 시간
            yield return new WaitForSecondsRealtime(0.12f); // 카드 고르는 동안의 실제 시간
            Assert.That(StageTime(encounter, "ElapsedSeconds"), Is.EqualTo(active)); // 카드 선택 중 전투 시계 보존
            Assert.That(StageTime(encounter, "TotalSeconds"), Is.GreaterThan(total + 0.06f)); // 전체 소요에 선택 시간 포함
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1)); // 실제 첫 번째 카드 선택
            yield return WaitFrames(5); // 카드 선택 뒤 시간 재개
            Assert.That(Time.timeScale, Is.GreaterThan(0f)); // 다음 시험과 실제 전투의 정지 해제
        }

        [UnityTest] // 편집 가능한 지형 구역과 깊이 연출
        public IEnumerator OuterIslandsUsesSceneVolumeAndMovesCloudLayers() // 실제 경계와 시차 움직임
        {
            yield return LoadOuterIslands(); // 씬에 저장된 배치
            GameObject zone = GameObject.Find("FlightVolume"); // 편집 가능한 전투 구역
            Assert.That(zone, Is.Not.Null); // 전투 구역 오브젝트 존재
            BoxCollider boundary = zone.GetComponent<BoxCollider>(); // 구역의 실제 위치와 크기
            Assert.That(boundary.isTrigger, Is.True); // 비행 경계는 충돌 표면과 분리
            Transform routes = GameObject.Find("SpawnPoints").transform; // 실전의 편집 지점 그룹
            Assert.That(routes.Find("AimedEnemy") && routes.Find("FanEnemy") && routes.Find("DiverEnemy"), Is.True); // 기존 세 적의 출현 경로
            Assert.That(routes.Find("MidBossRoute/Combat"), Is.Not.Null); // 보스 공격 위치의 편집점
            Assert.That(RenderSettings.skybox.HasProperty("_SurfaceMap"), Is.True, "낮은 고도의 바다와 해안 투영"); // 새 하늘과 지표 머티리얼
            Assert.That(RenderSettings.skybox.GetTexture("_SurfaceMap"), Is.Not.Null); // 생성한 지표 이미지 연결
            Transform pilot = FindPilot(); // 실제 비행 조작체
            pilot.position = new Vector3(boundary.bounds.max.x - 0.2f, 24f, -5f); // 충돌 지형 바깥의 우측 경계
            Transform near = GameObject.Find("CloudLayers").transform.Find("NearClouds"); // 가까운 구름층
            Transform far = GameObject.Find("CloudLayers").transform.Find("FarClouds"); // 먼 구름층
            Vector3 nearStart = near.position; // 가까운 층의 시작 위치
            Vector3 farStart = far.position; // 먼 층의 시작 위치
            SetKeys(Key.D); // 경계 바깥쪽으로 실제 비행
            yield return WaitFrames(45); // 비행 체적과 구름 이동 관찰
            SetKeys(); // 이동 해제
            Assert.That(pilot.position.x, Is.EqualTo(boundary.bounds.max.x).Within(0.01f)); // 씬 경계를 넘지 않는 이동
            Component nearDrift = near.GetComponent("FlightCloudDrift"); // 가까운 구름의 씬 이동 범위
            Component farDrift = far.GetComponent("FlightCloudDrift"); // 먼 구름의 씬 이동 범위
            var sweep = nearDrift.GetType().GetField("sweep", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 사인 곡선의 위상과 독립인 실제 편집값
            Vector3 nearSweep = (Vector3)sweep.GetValue(nearDrift); // 가까운 층의 최대 이동 폭
            Vector3 farSweep = (Vector3)sweep.GetValue(farDrift); // 먼 층의 최대 이동 폭
            Assert.That(nearSweep.magnitude, Is.GreaterThan(farSweep.magnitude)); // 가까운 구름층의 더 큰 반복 이동
            Assert.That(Vector3.Distance(near.position, nearStart), Is.GreaterThan(0.0001f)); // 가까운 층의 실제 이동
            Assert.That(Vector3.Distance(far.position, farStart), Is.GreaterThan(0.0001f)); // 먼 층의 실제 이동
        }

        [UnityTest] // 기본 부품과 시험 부품이 실제 비행에 미치는 변화
        public IEnumerator OuterIslandsBroomSwapUpdatesMountedArtAndFlightSpeed() // 정상 출격과 장착 API를 통한 교체
        {
            yield return LoadOuterIslands(); // 배포하는 실제 항로 로드
            Transform pilot = FindPilot(); // 입력과 부품을 함께 쓰는 출격 비행체
            Component equipment = pilot.GetComponent("FlightBroomEquipment"); // 게임 어셈블리에 배치한 실제 장착 상태
            Assert.That(equipment, Is.Not.Null, "빗자루 부품 컴포넌트"); // 정상 시작 경로의 장비 연결
            Transform mounts = pilot.Find("TrainingBroom/PartMounts"); // 에디터에서 편집하는 실제 장착 지점
            Assert.That(mounts.childCount, Is.EqualTo(8)); // 하이러키에 저장한 여덟 부품 슬롯
            var body = mounts.Find("Body/Visual").GetComponent<MeshRenderer>(); // 조립 부품의 실제 렌더러
            Material defaultArt = body.sharedMaterial; // 아리아의 시작 외형
            pilot.position = new Vector3(0f, 22f, -23f); // 장애물과 분리된 시험 비행 위치
            SetKeys(Key.W); // 실제 기본 빗자루 전진
            yield return WaitFrames(38); // 정격 속도에 도달할 때까지 가속
            float start = pilot.position.z; // 기본 장비의 이동 시작점
            yield return WaitFrames(15); // 0.25초의 실제 이동
            float baseline = (pilot.position.z - start) * 4f; // 실제 기본 비행 속도
            Assert.That(baseline, Is.EqualTo(12.3f).Within(0.08f)); // 기본 몸체의 +0.3 가산
            Assert.That(BroomAction(equipment, "TryEquip", "Aria", "Body", "trial.storm-body:1"), Is.True); // 별개 시험 몸체 장착
            Assert.That(body.sharedMaterial, Is.Not.SameAs(defaultArt)); // 같은 3D 지점에서 외형 변경
            yield return WaitFrames(8); // 변경된 장비의 가속 적용
            start = pilot.position.z; // 다른 몸체로 비행할 시작점
            yield return WaitFrames(15); // 동일한 0.25초의 실제 이동
            SetKeys(); // 실제 비행 입력 해제
            Assert.That((pilot.position.z - start) * 4f, Is.EqualTo(12.7f).Within(0.08f)); // 이전 +0.3 제거와 새 +0.7 적용
            Assert.That(BroomAction(equipment, "TryEquip", "Aria", "Body", "aria.body:1"), Is.True); // 시작 부품을 다시 장착
            Assert.That(body.sharedMaterial, Is.SameAs(defaultArt)); // 교체 후 기존 외형 복원
            Assert.That(BroomAction(equipment, "TryEquip", "Aria", "Propulsion", "trial.storm-drive:1"), Is.True); // 가속형 추진부로 교체
            var getBonus = equipment.GetType().GetMethod("GetBonus"); // 게임 어셈블리의 비행 계수 조회
            var stat = getBonus.GetParameters()[0].ParameterType; // 실제 부품 효과의 열거형
            Assert.That(getBonus.Invoke(equipment, new[] { System.Enum.Parse(stat, "Acceleration") }), Is.EqualTo(1.5f)); // 추진부의 가속 이득
            Assert.That(getBonus.Invoke(equipment, new[] { System.Enum.Parse(stat, "Braking") }), Is.EqualTo(-1f)); // 추진부의 제동 불이익
        }

        [UnityTest] // 공용 인벤토리의 개별 부품 소유와 캐릭터 전환
        public IEnumerator OuterIslandsBroomRejectsDuplicateItemsAndRestoresPilotLoadout() // 같은 종류와 개별 부품 구분
        {
            yield return LoadOuterIslands(); // 실제 항로의 기본 장착 상태
            Transform pilot = FindPilot(); // 두 캐릭터의 시험 조작체
            Component gear = pilot.GetComponent("FlightBroomEquipment"); // 게임 어셈블리의 공용 인벤토리
            Assert.That(gear, Is.Not.Null, "빗자루 부품 컴포넌트"); // 실제 시작 장비 연결
            Assert.That(Metric(gear, "OwnedCount"), Is.EqualTo(59)); // 시작 부품 56개와 시험용 3개
            Assert.That(BroomAction(gear, "GetEquippedId", "Aria", "Body"), Is.EqualTo("aria.body:1")); // 기본 몸체의 보유 ID
            Assert.That(BroomAction(gear, "TryEquip", "Aria", "Grip", "trial.storm-body:1"), Is.False); // 잘못된 슬롯에 교체 거부
            Assert.That(BroomAction(gear, "TryEquip", "Aria", "Body", "trial.storm-body:1"), Is.True); // 아리아에 첫 개별 부품 사용
            Assert.That(BroomAction(gear, "TryEquip", "Rian", "Body", "trial.storm-body:1"), Is.False); // 동일한 개별 부품 동시 장착 금지
            Assert.That(BroomAction(gear, "TryEquip", "Rian", "Body", "trial.storm-body:2"), Is.True); // 같은 종류의 다른 개별 부품 장착
            Assert.That(BroomAction(gear, "Unequip", "Aria", "Body"), Is.True); // 첫 부품을 공용 인벤토리로 반환
            Assert.That(BroomAction(gear, "TryEquip", "Rian", "Body", "trial.storm-body:1"), Is.True); // 해제한 개별 부품을 다른 캐릭터로 이동
            Assert.That(BroomAction(gear, "TryEquip", "Aria", "Body", "aria.body:1"), Is.True); // 아리아는 기본 조합 복원
            var art = pilot.Find("TrainingBroom/PartMounts/Body/Visual").GetComponent<MeshRenderer>(); // 실제 조작체의 몸체 표시
            Material ariaArt = art.sharedMaterial; // 아리아 시작 몸체의 재질 기준
            yield return SelectRian(); // 실제 F2 입력으로 캐릭터 전환
            Assert.That(art.sharedMaterial, Is.Not.SameAs(ariaArt)); // 리안 자신의 장착 조합 표시
            yield return SelectRian(); // 아리아로 돌아가는 실제 조작
            Assert.That(art.sharedMaterial, Is.SameAs(ariaArt)); // 아리아의 장착 상태 복원
        }

        [UnityTest] // 정상 정지 메뉴부터 빗자루 미리보기와 장착 저장
        public IEnumerator BroomWorkshopPreviewsThenRestoresConfirmedLoadout() // 미리보기와 저장 상태 분리
        {
            string key = "ProjectG.Broom.Test." + System.Guid.NewGuid().ToString("N"); // 기존 사용자 저장과 분리한 검증 키
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_BROOM_TEST_KEY"); // 이전 테스트 저장 설정
            System.Environment.SetEnvironmentVariable("PROJECTG_BROOM_TEST_KEY", key); // 저장 복원에 쓸 독립 키
            try // 검증 끝에 독립 저장 해제
            {
                yield return LoadOuterIslands(); // 실제 시작 씬
                var gear = FindPilot().GetComponent("FlightBroomEquipment"); // 실전 장착 상태
                Transform pause = GameObject.Find("CombatHUD").transform.Find("PauseMenu"); // 기존 ESC 메뉴
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 다음 실제 게임 프레임에 ESC 입력
                yield return WaitFrames(4); // 정지 화면 갱신
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이전 입력 해제
                Assert.That(pause.gameObject.activeSelf, Is.True); // 실제 정지 메뉴 진입 확인
                ClickPauseButton(pause, "Controls/BroomWorkshop"); // 조립 화면 진입
                Transform shop = pause.Find("BroomWorkshop"); // 편집 가능한 조립 화면
                Assert.That(shop.gameObject.activeInHierarchy, Is.True); // 정지 화면의 조립 패널 표시
                ClickPauseButton(shop, "Parts/Part1"); // 아리아 몸체 두 번째 후보
                Assert.That(BroomAction(gear, "GetEquippedId", "Aria", "Body"), Is.EqualTo("aria.body:1")); // 확인 전 실제 장비의 기본 몸체 유지
                var model = gear.transform.Find("TrainingBroom/PartMounts/Body/Visual").GetComponent<MeshRenderer>(); // 실제 장착 몸체
                var preview = shop.Find("PreviewStudio/Turntable/PartMounts/Body/Visual").GetComponent<MeshRenderer>(); // 미리보기 전용 몸체
                Assert.That(preview.sharedMaterial, Is.Not.SameAs(model.sharedMaterial)); // 확정 전 별도 미리보기 렌더러만 변경
                Assert.That(shop.Find("PartComparison").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("0.4")); // 새 후보의 속도 차이
                ClickPauseButton(shop, "Back"); // 바꾸지 않고 정지 화면 복귀
                Assert.That(BroomAction(gear, "GetEquippedId", "Aria", "Body"), Is.EqualTo("aria.body:1")); // 취소한 조합 제외
                ClickPauseButton(pause, "Controls/BroomWorkshop"); // 다시 열어 부품 장착
                ClickPauseButton(shop, "Parts/Part1"); // 질풍 몸체 후보 선택
                ClickPauseButton(shop, "Equip"); // 교체 저장 버튼의 실제 동작
                Assert.That(BroomAction(gear, "GetEquippedId", "Aria", "Body"), Is.EqualTo("trial.storm-body:1")); // 확인한 새 조합
                Assert.That(PlayerPrefs.HasKey(key), Is.True); // 독립 저장에 기록된 조합
                yield return LoadOuterIslands(); // 새 출격 오브젝트에 저장값 복원
                gear = FindPilot().GetComponent("FlightBroomEquipment"); // 새로 초기화한 실전 장비
                Assert.That(BroomAction(gear, "GetEquippedId", "Aria", "Body"), Is.EqualTo("trial.storm-body:1")); // 저장한 개별 ID 복원
                string invalid = PlayerPrefs.GetString(key).Replace("trial.storm-body:1", "rian.body:1"); // 다른 캐릭터에 이미 장착한 보유 ID
                PlayerPrefs.SetString(key, invalid); // 중복 장착된 시험 저장
                yield return LoadOuterIslands(); // 전체 장착 검증을 거치는 초기화
                gear = FindPilot().GetComponent("FlightBroomEquipment"); // 손상 저장을 읽은 실제 장착 상태
                Assert.That(BroomAction(gear, "GetEquippedId", "Aria", "Body"), Is.EqualTo("aria.body:1")); // 유효한 시작 장착 유지
                Assert.That(BroomAction(gear, "GetEquippedId", "Rian", "Body"), Is.EqualTo("rian.body:1")); // 중복 저장의 일부 적용 차단
            }
            finally // 고유 시험 키와 환경 설정 정리
            {
                PlayerPrefs.DeleteKey(key); // 자체 테스트 저장만 삭제
                PlayerPrefs.Save(); // 저장 검증 키의 제거 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_BROOM_TEST_KEY", prior); // 테스트 환경 복귀
            }
        }

        [UnityTest] // 수압 파동의 초기 지급 조합과 중복 피해 확인
        public IEnumerator WaterPressureHitsTheSameTargetOnlyOncePerCast() // 세 파동의 공유 적중 제한
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 전투 정지 메뉴
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 화면 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습 화면 입장
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            ClickPauseButton(editor, "Preset"); // 수압 파동 기본 구성 선택
            yield return WaitFrames(2); // 시험 발사 버튼 상태 갱신
            Assert.That(editor.Find("TestCast").GetComponent<Button>().interactable, Is.True); // 수압 파동 발사 가능
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 피해를 기록하는 연습 표적
            Component editorComponent = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 참조
            var rangeField = editorComponent.GetType().GetField("range", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 연습장 필드
            Component range = (Component)rangeField.GetValue(editorComponent); // 실제 연습 발사기
            var muzzleField = range.GetType().GetField("muzzle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 연습 발사 지점 필드
            Transform muzzle = (Transform)muzzleField.GetValue(range); // 실제 연습 발사 지점
            target.transform.position = muzzle.position + muzzle.forward * 3.5f; // 세 파동의 공통 명중 위치
            target.GetComponent<SphereCollider>().radius = 2f / target.transform.lossyScale.x; // 실제 2미터 반경의 넓은 표적
            Physics.SyncTransforms(); // 변경한 충돌체의 즉시 반영
            Assert.That(Physics.SphereCast(muzzle.position, 0.32f, muzzle.forward, out RaycastHit contact, 4f, 1 << 9, QueryTriggerInteraction.Ignore), Is.True, $"표적 {target.transform.position}, 발사점 {muzzle.position}, 축척 {target.transform.lossyScale}, 반경 {target.GetComponent<SphereCollider>().bounds.extents}, 레이어 {target.gameObject.layer}, 활성 {target.gameObject.activeInHierarchy}"); // 표적과 발사선의 물리 충돌
            Assert.That(contact.collider.gameObject, Is.EqualTo(target.gameObject)); // 첫 충돌체의 표적 확인
            ClickPauseButton(editor, "TestCast"); // 동일 시전의 세 파동 발사
            Assert.That(Metric(range, "CastCount"), Is.EqualTo(1)); // 수압 파동 시전 성공
            yield return WaitFrames(140); // 세 발의 명중과 소멸 대기
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(36)); // 동일 대상은 시전당 한 번만 피해
        }

        [UnityTest] // 실제 적에게 적용되는 수압 파동 넉백 확인
        public IEnumerator WaterPressurePushesAHitEnemyAlongTheCastDirection() // 실제 피해와 경로 이동 연결
        {
            yield return LoadOuterIslands(); // 실제 마물이 있는 출격 씬
            Transform enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy"); // 일반 마물의 실제 경로
            enemy.gameObject.SetActive(true); // 적의 체력과 충돌체 활성화
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 제작 메뉴를 여는 전투 정지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 준비
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 실제 제작 시험장 진입
            Transform editor = pause.Find("CraftedSpellEditor"); // 제작 시험 화면
            ClickPauseButton(editor, "Preset"); // 수압 파동 선택
            yield return WaitFrames(2); // 발사 가능 상태 갱신
            Component editorComponent = editor.GetComponent("FlightCraftedSpellEditor"); // 제작 화면 컴포넌트
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 직렬화된 연습 참조 조회
            Component range = (Component)editorComponent.GetType().GetField("range", hidden).GetValue(editorComponent); // 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 시험 발사 위치
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 기본 연습 표적
            target.gameObject.SetActive(false); // 일반 마물에만 충돌
            enemy.position = muzzle.position + muzzle.forward * 3.5f; // 발사선 위에 실제 마물 배치
            SphereCollider collider = enemy.gameObject.AddComponent<SphereCollider>(); // 시험 전용 넓은 마물 충돌체
            collider.radius = 2f / enemy.lossyScale.x; // 세 파동의 겹친 충돌 범위
            Physics.SyncTransforms(); // 표적 위치를 충돌 검사에 반영
            Vector3 before = enemy.position; // 넉백 전 위치
            Component life = enemy.GetComponent("FlightEnemy"); // 마물의 실제 체력
            ClickPauseButton(editor, "TestCast"); // 수압 파동 발사
            yield return WaitFrames(140); // 파동 명중과 넉백 대기
            Assert.That(Metric(life, "Health"), Is.EqualTo(44)); // 동일 시전 피해 36 한 번
            Assert.That(enemy.GetComponent("FlightCraftedStatus"), Is.Not.Null); // 실제 적에 연결된 공통 상태 처리
            Assert.That(Vector3.Dot(enemy.position - before, muzzle.forward), Is.InRange(1.9f, 2.1f)); // 발사 방향으로 기본 거리 이동
        }

        [UnityTest] // 정화 결계의 실제 적탄 상쇄와 쿨타임 확인
        public IEnumerator PurificationClearsAtMostSixteenAndStartsCooldownAfterExpiry() // 고정 결계의 수명과 상쇄 상한
        {
            yield return LoadOuterIslands(); // 실제 전투 발사기와 적탄 풀
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 외부 적탄 생성 차단
            Transform pilot = FindPilot(); // 실제 전투 조종체
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 공격에 의한 외부 변화 차단
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 제작 화면 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 정지 메뉴 열기
            yield return WaitFrames(4); // 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 화면 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 편집기의 실제 프리셋
            ClickPauseButton(editor, "Preset"); // 수압 파동
            ClickPauseButton(editor, "Preset"); // 가시 산탄
            ClickPauseButton(editor, "Preset"); // 정화 결계
            yield return WaitFrames(2); // 버튼 상태 갱신
            Assert.That(editor.Find("TestCast").GetComponent<Button>().interactable, Is.True); // 결계 구성이 발사 가능
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 실제 구성 조회
            Component editorComponent = editor.GetComponent("FlightCraftedSpellEditor"); // 편집 화면의 후보 구성
            object recipe = editorComponent.GetType().GetField("candidate", hidden).GetValue(editorComponent); // 정화 결계의 실제 구성
            ClickPauseButton(editor, "Back"); // 제작 화면에서 정지 메뉴 복귀
            ClickPauseButton(pause, "MenuButtons/Continue"); // 전투 시간 재개
            yield return WaitFrames(2); // 전투 상태 적용
            Component pool = GetThreatPool(); // 실제 적탄 풀
            var fire = pool.GetType().GetMethod("TryFire", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(int) }); // 일반 적탄 발사
            Vector3 center = pilot.position; // 결계가 고정될 시전 좌표
            for (int i = 0; i < 17; i++) // 상쇄 한도보다 한 발 많은 적탄
            {
                Assert.That(fire.Invoke(pool, new object[] { center + Vector3.right * 3f, Vector3.forward, 0.01f, 0 }), Is.True); // 결계 안에 활성 적탄 생성
            }
            System.Type patternType = pool.GetType().Assembly.GetType("ProjectG.FlightThreatPattern"); // 적탄의 실제 패턴 타입
            var restrictedFire = pool.GetType().GetMethod("TryFire", hidden, null, new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(int), patternType, typeof(bool) }, null); // 상쇄 허용 여부가 있는 발사
            object pattern = System.Enum.Parse(patternType, "Aimed"); // 기본 조준탄의 형태
            Assert.That(restrictedFire.Invoke(pool, new object[] { center + Vector3.right * 3f, Vector3.forward, 0.01f, 0, pattern, false }), Is.True); // 상쇄를 허용하지 않는 시험 적탄
            var active = (System.Collections.IList)pool.GetType().GetField("active", hidden).GetValue(pool); // 실제 풀의 활성 적탄
            Component restricted = (Component)active[active.Count - 1]; // 불허 적탄의 실제 객체
            Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실전 제작 마법 발사기
            var cast = caster.GetType().GetMethod("Cast", hidden); // 실제 전투 시전
            var cooldown = caster.GetType().GetMethod("CooldownRemaining", hidden); // 구성 ID의 남은 시간
            Assert.That(cast.Invoke(caster, new[] { recipe }), Is.True); // 정화 결계 시전 성공
            yield return WaitFrames(3); // 적탄 상쇄 처리
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(2)); // 불허 탄과 한도 초과 탄 보존
            Assert.That(restricted.gameObject.activeSelf, Is.True); // 상쇄 불허 탄은 유지
            Assert.That((float)cooldown.Invoke(caster, new[] { recipe }), Is.GreaterThan(15f)); // 유지 4초와 종료 후 12초
            yield return WaitFrames(245); // 결계의 4초 유지 종료
            Assert.That((float)cooldown.Invoke(caster, new[] { recipe }), Is.InRange(11f, 12.1f)); // 결계 종료 후 실제 쿨타임
            Assert.That(fire.Invoke(pool, new object[] { center + Vector3.right * 3f, Vector3.forward, 0.01f, 0 }), Is.True); // 종료된 결계에 새 적탄 진입
            yield return WaitFrames(3); // 추가 상쇄 여부 확인
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(3)); // 종료 후 적탄 유지
            pool.GetType().GetMethod("ReturnAll").Invoke(pool, null); // 불허 탄을 포함한 기존 적탄 회수
            Assert.That(fire.Invoke(pool, new object[] { center + Vector3.right * 3f, Vector3.forward, 0.01f, 0 }), Is.True); // 회수한 적탄의 일반 재사용
            Component reused = (Component)active[0]; // 재사용한 적탄 객체
            Assert.That(reused.GetType().GetProperty("CanCancel", hidden).GetValue(reused), Is.True); // 이전 불허 상태의 초기화
        }

        [UnityTest] // 시전 위치에 남는 정화 결계 확인
        public IEnumerator PurificationRemainsAtItsCastPositionWhenPilotMoves() // 결계와 이동하는 비행체의 위치 분리
        {
            yield return LoadOuterIslands(); // 실전 제작 마법과 적탄 풀
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 시험 외의 적탄 생성 차단
            Transform pilot = FindPilot(); // 실제 조종체
            Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실전 발사기
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 시전과 풀 조회
            System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 문서의 기본 마법 목록
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 읽기 전용 프리셋 목록
            object recipe = all[11].GetType().GetProperty("Recipe").GetValue(all[11]); // 정화 결계의 실제 구성
            Vector3 center = pilot.position; // 이동 전 결계 위치
            Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { recipe }), Is.True); // 고정 결계 시전
            pilot.position += Vector3.right * 12f; // 결계 밖으로 조종체 이동
            Physics.SyncTransforms(); // 이동 위치의 판정 반영
            Component pool = GetThreatPool(); // 같은 전투의 적탄 풀
            var fire = pool.GetType().GetMethod("TryFire", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(int) }); // 시험용 기본 적탄 발사
            Assert.That(fire.Invoke(pool, new object[] { center + Vector3.up * 3f, Vector3.forward, 0.01f, 0 }), Is.True); // 원래 결계 안의 적탄
            Assert.That(fire.Invoke(pool, new object[] { pilot.position + Vector3.up * 3f, Vector3.forward, 0.01f, 0 }), Is.True); // 이동한 조종체 부근의 적탄
            var active = (System.Collections.IList)pool.GetType().GetField("active", hidden).GetValue(pool); // 실제 활성 적탄 목록
            Component oldLocation = (Component)active[0]; // 원래 결계의 적탄
            Component newLocation = (Component)active[1]; // 이동한 조종체의 적탄
            yield return WaitFrames(3); // 결계 범위 상쇄
            Assert.That(oldLocation.gameObject.activeSelf, Is.False); // 원래 위치의 적탄 상쇄
            Assert.That(newLocation.gameObject.activeSelf, Is.True); // 조종체 이동 위치의 적탄 보존
        }

        [UnityTest] // 독립 연습 결계와 실전 적탄 풀 분리
        public IEnumerator PurificationPracticeDoesNotCancelLiveCombatProjectiles() // 연습 중 실전 적탄 보존
        {
            yield return LoadOuterIslands(); // 실제 전투와 독립 제작 연습장
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 외부 적탄 생성 차단
            Transform pilot = FindPilot(); // 현재 실전 조종체
            Component pool = GetThreatPool(); // 실제 전투 적탄 풀
            var fire = pool.GetType().GetMethod("TryFire", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(int) }); // 실제 일반 적탄 발사
            Assert.That(fire.Invoke(pool, new object[] { pilot.position + Vector3.right * 3f, Vector3.forward, 0.01f, 0 }), Is.True); // 실전에 남겨 둘 적탄
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 제작 연습 진입
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 전투 정지 입력
            yield return WaitFrames(4); // 정지 화면 표시
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 독립 연습 화면
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            ClickPauseButton(editor, "Preset"); // 수압 파동 선택
            ClickPauseButton(editor, "Preset"); // 가시 산탄 선택
            ClickPauseButton(editor, "Preset"); // 정화 결계 선택
            yield return WaitFrames(2); // 발사 가능 상태 갱신
            ClickPauseButton(editor, "TestCast"); // 연습 전용 결계 시전
            yield return WaitFrames(245); // 결계 수명이 끝날 때까지 대기
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(1)); // 실전 적탄의 상태 보존
        }

        [UnityTest] // 새 편성의 기본값과 빈 칸 및 중복 저장 확인
        public IEnumerator CraftedLoadoutPreservesEmptyAndDuplicateSlotsAcrossSorties() // 두 칸의 편성과 무작위 배정
        {
            string key = "ProjectG.Crafted.Test." + System.Guid.NewGuid().ToString("N"); // 다른 저장과 격리한 시험 키
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 원래 시험 환경 보관
            var randomState = Random.state; // 다른 시험의 난수 상태 보관
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 격리된 저장만 사용
            try // 저장을 검증한 뒤 반드시 시험 키 정리
            {
                yield return LoadOuterIslands(); // 실제 출격의 제작 마법 도감
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 실제 편성과 발사 도감
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 편성 API
                var slot = book.GetType().GetMethod("Slot", hidden); // 출격 전에 저장한 칸
                var active = book.GetType().GetMethod("ActiveSlot", hidden); // 실제 출격에서 고정된 칸
                var clear = book.GetType().GetMethod("ClearSlot", hidden); // 다음 출격의 빈 칸 설정
                var equip = book.GetType().GetMethod("Equip", hidden); // 다음 출격의 마법 편성
                var reset = book.GetType().GetMethod("ResetRun", hidden); // 새로운 출격의 무작위 배정
                object first = slot.Invoke(book, new object[] { 0 }); // 기본 파이어 볼트
                object second = slot.Invoke(book, new object[] { 1 }); // 기본 가시 산탄
                Assert.That(first, Is.Not.Null); // 첫 번째 초기 마법 존재
                Assert.That(second, Is.Not.Null); // 두 번째 초기 마법 존재
                string firstId = (string)first.GetType().GetProperty("Id").GetValue(first); // 첫 조합의 고유 ID
                string secondId = (string)second.GetType().GetProperty("Id").GetValue(second); // 둘째 조합의 고유 ID
                Assert.That(firstId, Is.Not.EqualTo(secondId)); // 서로 다른 초기 마법
                bool leftFirst = false; // 첫 마법이 왼쪽에 배치된 기록
                bool leftSecond = false; // 둘째 마법이 왼쪽에 배치된 기록
                Random.InitState(24680); // 반복 가능한 좌우 배정 시험
                for (int i = 0; i < 32; i++) // 여러 번 독립 출격 배정
                {
                    reset.Invoke(book, null); // 같은 두 장의 새 배정
                    object left = active.Invoke(book, new object[] { 0 }); // 배정된 왼쪽 마법
                    object right = active.Invoke(book, new object[] { 1 }); // 배정된 오른쪽 마법
                    string leftId = (string)left.GetType().GetProperty("Id").GetValue(left); // 실제 왼쪽 식별자
                    string rightId = (string)right.GetType().GetProperty("Id").GetValue(right); // 실제 오른쪽 식별자
                    Assert.That(leftId == firstId && rightId == secondId || leftId == secondId && rightId == firstId, Is.True); // 편성한 두 마법만 배치
                    leftFirst |= leftId == firstId; // 첫 마법이 왼쪽에 온 경우
                    leftSecond |= leftId == secondId; // 둘째 마법이 왼쪽에 온 경우
                }
                Assert.That(leftFirst && leftSecond, Is.True); // 좌우 두 배치가 모두 발생
                clear.Invoke(book, new object[] { 0 }); // 첫 준비 칸 비우기
                clear.Invoke(book, new object[] { 1 }); // 둘째 준비 칸 비우기
                Assert.That(active.Invoke(book, new object[] { 0 }), Is.Not.Null); // 진행 중 출격은 원래 편성 고정
                reset.Invoke(book, null); // 두 빈 칸으로 새 출격
                Assert.That(active.Invoke(book, new object[] { 0 }), Is.Null); // 첫 빈 칸 유지
                Assert.That(active.Invoke(book, new object[] { 1 }), Is.Null); // 둘째 빈 칸 유지
                yield return WaitFrames(2); // 빈 칸의 실제 HUD 갱신
                Transform cards = LocateCombatHud().transform.Find("MagicCards"); // 실제 전투 카드 영역
                Assert.That(cards.Find("LeftActive").gameObject.activeSelf, Is.False); // 빈 왼쪽 칸은 카드 숨김
                Assert.That(cards.Find("RightActive").gameObject.activeSelf, Is.False); // 빈 오른쪽 칸은 카드 숨김
                Assert.That(cards.Find("LeftNext").gameObject.activeSelf || cards.Find("RightNext").gameObject.activeSelf, Is.False); // 시험 덱 미리보기 숨김
                Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 실제 제작 마법 발사기
                yield return Click(1); // 빈 왼쪽 칸 클릭
                yield return Click(2); // 빈 오른쪽 칸 클릭
                Assert.That(Metric(caster, "CastCount"), Is.Zero); // 빈 칸은 마법이나 시험 카드 사용 없음
                Assert.That(equip.Invoke(book, new[] { (object)0, first }), Is.True); // 첫 칸에 파이어 볼트
                Assert.That(equip.Invoke(book, new[] { (object)1, first }), Is.True); // 같은 마법의 둘째 장착
                reset.Invoke(book, null); // 중복 편성으로 새 출격
                Assert.That(active.Invoke(book, new object[] { 0 }), Is.Not.Null); // 첫 중복 칸
                Assert.That(active.Invoke(book, new object[] { 1 }), Is.Not.Null); // 둘째 중복 칸
                Component magic = FindPilot().GetComponent("FlightSkillController"); // 제작 마법의 공용 마력
                var manaField = magic.GetType().GetField("mana", hidden); // 실제 마력 잔량
                var use = book.GetType().GetMethod("Use", hidden); // 전투 카드와 같은 실제 시전
                manaField.SetValue(magic, 0f); // 마력 부족 조건
                object beforeFailure = active.Invoke(book, new object[] { 0 }); // 부족 시도 전 현재 마법
                use.Invoke(book, new object[] { 0 }); // 부족한 마력의 실제 시전 시도
                Assert.That(Metric(caster, "CastCount"), Is.Zero); // 실패한 시전은 발사하지 않음
                Assert.That(Metric(magic, "Mana"), Is.Zero); // 실패한 시전은 마력을 소비하지 않음
                Assert.That(active.Invoke(book, new object[] { 0 }), Is.SameAs(beforeFailure)); // 실패해도 마법 순서 유지
                manaField.SetValue(magic, 100f); // 두 시전을 시도할 충분한 마력
                use.Invoke(book, new object[] { 0 }); // 첫 중복 마법 시전
                use.Invoke(book, new object[] { 1 }); // 같은 ID의 두 번째 시전 시도
                Assert.That(Metric(caster, "CastCount"), Is.EqualTo(1)); // 중복 마법은 대기시간 공유
                yield return LoadOuterIslands(); // 저장을 읽어 새 출격 재진입
                book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 다시 생성된 마법 도감
                object restoredLeft = slot.Invoke(book, new object[] { 0 }); // 첫 저장 칸 복원
                object restoredRight = slot.Invoke(book, new object[] { 1 }); // 둘째 저장 칸 복원
                Assert.That((string)restoredLeft.GetType().GetProperty("Id").GetValue(restoredLeft), Is.EqualTo(firstId)); // 첫 중복 마법 복원
                Assert.That((string)restoredRight.GetType().GetProperty("Id").GetValue(restoredRight), Is.EqualTo(firstId)); // 둘째 중복 마법 복원
            }
            finally // 시험 저장과 전역 난수 상태 정리
            {
                PlayerPrefs.DeleteKey(key); // 격리 시험 키만 삭제
                PlayerPrefs.Save(); // 시험 키 제거 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 원래 환경 복원
                Random.state = randomState; // 다른 시험의 난수 복원
            }
        }

        [UnityTest] // 이전 저장과 신규 빈 슬롯의 구분
        public IEnumerator CraftedLoadoutReadsLegacySavesWithoutOverwritingThem() // 기존 ID와 빈 슬롯 저장 호환
        {
            string key = "ProjectG.Crafted.Test." + System.Guid.NewGuid().ToString("N"); // 사용자 저장과 격리된 키
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 이전 환경 변수
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 이 시험의 저장만 사용
            try // 시험한 데이터만 정리
            {
                yield return LoadOuterIslands(); // 기본 마법 구성의 실제 ID 조회
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 실제 저장 도감
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 슬롯 확인
                var slot = book.GetType().GetMethod("Slot", hidden); // 준비한 마법 칸
                object fire = slot.Invoke(book, new object[] { 0 }); // 기본 파이어 볼트 구성
                string fireId = (string)fire.GetType().GetProperty("Id").GetValue(fire); // 이전 저장에 넣을 정확한 ID
                string legacy = "{\"recipe\":\"" + fireId + "\",\"left\":null,\"right\":\"" + fireId + "\"}"; // 이전 저장 형식과 한 칸의 마법
                PlayerPrefs.SetString(key, legacy); // 이전 버전의 저장 기록
                PlayerPrefs.Save(); // 실제 재진입에서 읽을 내용 확정
                yield return LoadOuterIslands(); // 이전 형식을 읽는 새 출격
                book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 복원한 도감
                Assert.That(slot.Invoke(book, new object[] { 0 }), Is.Null); // 이전 미장착 칸 보존
                object restored = slot.Invoke(book, new object[] { 1 }); // 이전 장착 마법
                Assert.That((string)restored.GetType().GetProperty("Id").GetValue(restored), Is.EqualTo(fireId)); // 유효한 이전 구성 ID 복원
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo(legacy)); // 읽기만 한 저장을 덮어쓰지 않음
                string trialDeck = "{\"recipe\":\"" + fireId + "\",\"left\":null,\"right\":null}"; // 이전 양쪽 시험 덱 저장
                PlayerPrefs.SetString(key, trialDeck); // 시험 덱을 쓰던 기존 기록
                PlayerPrefs.Save(); // 이전 형식 확정
                yield return LoadOuterIslands(); // 시험 덱 저장의 기본 편성 전환
                book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 새 출격 도감
                Assert.That(slot.Invoke(book, new object[] { 0 }), Is.Not.Null); // 기본 파이어 볼트 적용
                Assert.That(slot.Invoke(book, new object[] { 1 }), Is.Not.Null); // 기본 가시 산탄 적용
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo(trialDeck)); // 이전 원본 저장 보존
                string empty = "{\"version\":2,\"recipe\":\"" + fireId + "\",\"left\":null,\"right\":null}"; // 명시적으로 비운 두 칸
                PlayerPrefs.SetString(key, empty); // 최신 버전의 빈 편성
                PlayerPrefs.Save(); // 재시작 데이터 확정
                yield return LoadOuterIslands(); // 빈 편성 복원
                book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 다시 생성된 도감
                Assert.That(slot.Invoke(book, new object[] { 0 }), Is.Null); // 첫 빈 칸 복원
                Assert.That(slot.Invoke(book, new object[] { 1 }), Is.Null); // 둘째 빈 칸 복원
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo(empty)); // 명시적인 빈 저장 보존
            }
            finally // 다른 사용자 데이터에 영향 없는 종료
            {
                PlayerPrefs.DeleteKey(key); // 격리 키만 삭제
                PlayerPrefs.Save(); // 삭제 기록 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 원래 환경 복구
            }
        }

        [UnityTest] // 같은 구성의 강화 공유와 임시 성장 초기화
        public IEnumerator CraftedSpellUpgradesShareThreeRanksAndResetOnNewSortie() // 중복 슬롯의 강화 한도
        {
            string key = "ProjectG.Crafted.Growth." + System.Guid.NewGuid().ToString("N"); // 독립적인 제작 마법 저장
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 환경 보관
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 다른 저장과 격리
            try // 시험 저장만 정리
            {
                yield return LoadOuterIslands(); // 실제 출격과 마법 도감
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 두 전투 칸의 마법
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 성장 API 조회
                var slot = book.GetType().GetMethod("Slot", hidden); // 출격 준비 슬롯 조회
                object fire = slot.Invoke(book, new object[] { 0 }); // 기본 파이어 볼트
                var equip = book.GetType().GetMethod("Equip", hidden); // 다음 출격의 마법 편성
                Assert.That(equip.Invoke(book, new[] { (object)1, fire }), Is.True); // 두 칸에 같은 구성 편성
                var reset = book.GetType().GetMethod("ResetRun", hidden); // 실제 재출격 초기화
                reset.Invoke(book, null); // 중복 편성의 새로운 출격
                System.Type kind = book.GetType().GetNestedType("Upgrade", System.Reflection.BindingFlags.NonPublic); // 제작 마법 전용 강화 종류
                Assert.That(kind, Is.Not.Null); // 강화 종류가 실제 코드에 존재
                object damage = System.Enum.Parse(kind, "Damage"); // 마법 피해 강화
                object mana = System.Enum.Parse(kind, "Mana"); // 마력 소모 강화
                object cooldown = System.Enum.Parse(kind, "Cooldown"); // 재사용 대기 강화
                var upgrade = book.GetType().GetMethod("TryUpgrade", hidden); // 전투 중 실제 강화 적용
                var rank = book.GetType().GetMethod("UpgradeRank", hidden); // 구성 ID의 누적 단계
                Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 실제 전투 발사기
                var cast = caster.GetType().GetMethod("Cast", hidden); // 실제 마법 발사
                var cooldownRemaining = caster.GetType().GetMethod("CooldownRemaining", hidden); // 시전 후 남은 대기시간
                Assert.That(cast.Invoke(caster, new[] { fire }), Is.True); // 강화 전 파이어 볼트 발사
                var projectiles = (System.Collections.IList)caster.GetType().GetField("active", hidden).GetValue(caster); // 현재 비행 중인 제작 탄환
                Component projectile = (Component)projectiles[0]; // 강화 전에 발사한 실제 탄환
                Assert.That(upgrade.Invoke(book, new[] { (object)0, damage }), Is.True); // 첫 슬롯의 피해 강화
                Assert.That(rank.Invoke(book, new object[] { 1 }), Is.EqualTo(1)); // 같은 ID의 다른 슬롯에도 공유
                Assert.That(upgrade.Invoke(book, new[] { (object)1, mana }), Is.True); // 같은 구성의 마력 강화
                Assert.That(upgrade.Invoke(book, new[] { (object)0, cooldown }), Is.True); // 세 번째 강화 적용
                Assert.That(rank.Invoke(book, new object[] { 0 }), Is.EqualTo(3)); // 구성별 총 세 단계
                Assert.That(rank.Invoke(book, new object[] { 1 }), Is.EqualTo(3)); // 중복 슬롯의 동일 단계
                Assert.That(upgrade.Invoke(book, new[] { (object)1, damage }), Is.False); // 네 번째 강화 거절
                object originalProfile = projectile.GetType().GetField("profile", hidden).GetValue(projectile); // 발사 당시 고정한 탄환 수치
                Assert.That(originalProfile.GetType().GetField("Damage").GetValue(originalProfile), Is.EqualTo(35)); // 이미 발사한 탄환의 피해 유지
                Assert.That((float)cooldownRemaining.Invoke(caster, new[] { fire }), Is.GreaterThan(3.9f)); // 이미 시작한 대기시간은 소급하지 않음
                var stats = caster.GetType().GetMethod("EffectiveStats", hidden); // 강화가 반영된 시전 수치
                object profile = stats.Invoke(caster, new[] { fire }); // 동일 구성의 실제 수치
                Assert.That((float)profile.GetType().GetField("Mana").GetValue(profile), Is.EqualTo(14.25f).Within(0.001f)); // 시작 마력 오 퍼센트 감소
                Assert.That((float)profile.GetType().GetField("Cooldown").GetValue(profile), Is.EqualTo(3.6f).Within(0.001f)); // 재사용 시간 십 퍼센트 감소
                float upgradedDamage = (float)profile.GetType().GetField("Damage").GetValue(profile); // 반올림 전 강화 직격 피해
                Assert.That(upgradedDamage, Is.EqualTo(38.5f).Within(0.001f)); // 기준 피해에 십 퍼센트 적용
                Transform enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy"); // 실제 방어 속성이 있는 일반 마물
                Component life = enemy.GetComponent("FlightEnemy"); // 실제 마물의 체력 처리
                life.GetType().GetField("maxHealth", hidden).SetValue(life, 300); // 한 번의 유효 피해를 확인할 체력
                life.GetType().GetField("defenseElement", hidden).SetValue(life, "grass"); // 화염에 유리한 풀 속성
                enemy.gameObject.SetActive(true); // 지정한 체력으로 실제 마물 활성화
                projectile.GetType().GetMethod("Damage", hidden).Invoke(projectile, new object[] { life, upgradedDamage }); // 실제 속성 배율을 거치는 명중
                Assert.That(Metric(life, "Health"), Is.EqualTo(242)); // 강화 피해와 상성을 곱한 뒤 한 번 반올림
                caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 강화 기록을 유지한 채 발사와 대기 초기화
                Component skills = FindPilot().GetComponent("FlightSkillController"); // 실제 공용 마력
                var manaField = skills.GetType().GetField("mana", hidden); // 소수점 마력의 원본
                var use = book.GetType().GetMethod("Use", hidden); // 카드가 호출하는 실제 시전
                manaField.SetValue(skills, 14.2f); // 강화된 비용보다 적은 마력
                use.Invoke(book, new object[] { 0 }); // 마력이 부족한 시도
                Assert.That(Metric(caster, "CastCount"), Is.Zero); // 비용보다 적으면 시전 거절
                manaField.SetValue(skills, 14.25f); // 소수점까지 정확히 일치한 마력
                use.Invoke(book, new object[] { 0 }); // 비용 경계의 시전
                Assert.That(Metric(caster, "CastCount"), Is.EqualTo(1)); // 정확한 소수점 비용으로 시전 성공
                Assert.That((float)manaField.GetValue(skills), Is.EqualTo(0f).Within(0.001f)); // 실제 마력 소모량 일치
                reset.Invoke(book, null); // 일반 스테이지 재출격
                Assert.That(rank.Invoke(book, new object[] { 0 }), Is.Zero); // 임시 강화는 새 출격에서 초기화
                Assert.That(rank.Invoke(book, new object[] { 1 }), Is.Zero); // 중복 슬롯도 함께 초기화
            }
            finally // 격리한 저장만 삭제
            {
                PlayerPrefs.DeleteKey(key); // 시험 전용 저장 키 정리
                PlayerPrefs.Save(); // 시험 키 삭제 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 원래 환경 복원
            }
        }

        [UnityTest] // 실제 레벨업 선택에서 제작 마법 강화 반영
        public IEnumerator CraftedUpgradeAppearsInLevelUpAndUpdatesBothCards() // 중복 후보와 전투 HUD 확인
        {
            string key = "ProjectG.Crafted.Growth." + System.Guid.NewGuid().ToString("N"); // 독립적인 제작 마법 저장
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 이전 환경 값 보관
            var randomState = Random.state; // 이전 난수 상태 보관
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 시험 저장 격리
            try // 시험한 데이터만 정리
            {
                yield return LoadOuterIslands(); // 실제 레벨업과 두 마법 카드
                Transform hud = LocateCombatHud().transform; // 실제 전투 HUD
                Component book = hud.GetComponent("FlightCraftedSpellBook"); // 구성 ID별 강화 도감
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 레벨업 후보 관찰
                object fire = book.GetType().GetMethod("Slot", hidden).Invoke(book, new object[] { 0 }); // 기본 파이어 볼트
                Assert.That(book.GetType().GetMethod("Equip", hidden).Invoke(book, new[] { (object)1, fire }), Is.True); // 같은 마법을 두 칸에 편성
                book.GetType().GetMethod("ResetRun", hidden).Invoke(book, null); // 동일 편성으로 새 출격
                Component choices = Object.FindFirstObjectByType(System.Type.GetType("ProjectG.FlightUpgradeChoices, Assembly-CSharp")) as Component; // 실제 강화 선택기
                Assert.That(choices, Is.Not.Null); // 씬의 강화 선택기 연결
                var collect = choices.GetType().GetMethod("CollectCandidates", hidden); // 유효한 실제 선택지 수집
                collect.Invoke(choices, null); // 중복 장착 상태의 후보 생성
                var candidates = (System.Collections.IList)choices.GetType().GetField("candidates", hidden).GetValue(choices); // 생성한 후보 목록
                bool hasDamage = false; // 피해 강화 후보 확인
                foreach (object card in candidates) // 현재 실제 선택 가능 후보
                {
                    string reward = card.GetType().GetField("reward").GetValue(card).ToString(); // 후보의 내부 효과 이름
                    hasDamage |= reward == "CraftLeftDamage"; // 동일 마법의 화력 강화 존재
                    Assert.That(reward.StartsWith("CraftRight"), Is.False); // 같은 구성은 둘째 칸에서 중복 제시하지 않음
                }
                Assert.That(hasDamage, Is.True); // 피해가 있는 마법의 강화 후보
                choices.GetType().GetMethod("QueueChoices", hidden).Invoke(choices, new object[] { 1, 2 }); // 실제 레벨업 선택 모달 열기
                var offered = (System.Array)choices.GetType().GetField("offered", hidden).GetValue(choices); // 화면에 표시된 세 후보
                var present = choices.GetType().GetMethod("Present", hidden); // 실제 후보 재추첨
                int selected = -1; // 제작 마법 후보의 실제 화면 위치
                Random.InitState(8462); // 재현 가능한 후보 추첨
                for (int attempt = 0; attempt < 120 && selected < 0; attempt++) // 제작 강화가 나올 때까지 실제 추첨
                {
                    present.Invoke(choices, null); // 레벨업 화면의 후보 갱신
                    for (int i = 0; i < offered.Length; i++) // 화면의 각 선택지
                    {
                        string reward = offered.GetValue(i).GetType().GetField("reward").GetValue(offered.GetValue(i)).ToString(); // 실제 선택 효과
                        if (reward == "CraftLeftDamage") // 피해 강화 카드 발견
                        {
                            selected = i; // 실제 버튼 번호 기록
                            break; // 해당 카드 선택 준비
                        }
                    }
                }
                Assert.That(selected, Is.GreaterThanOrEqualTo(0)); // 실제 추첨에 제작 강화 등장
                choices.GetType().GetMethod("RequestChoice", hidden).Invoke(choices, new object[] { selected }); // 해당 레벨업 버튼의 동일 입력
                yield return WaitFrames(3); // 선택 적용과 전투 복귀
                var rank = book.GetType().GetMethod("UpgradeRank", hidden); // 구성별 공유 강화 단계
                Assert.That(rank.Invoke(book, new object[] { 0 }), Is.EqualTo(1)); // 선택한 마법의 실제 강화
                Assert.That(rank.Invoke(book, new object[] { 1 }), Is.EqualTo(1)); // 같은 구성의 반대쪽 슬롯 공유
                Transform cards = hud.Find("MagicCards"); // 실제 카드 이름과 상태 표시
                Assert.That(HudText(cards.Find("LeftActive/Title")), Does.Contain("+1")); // 왼쪽 카드의 강화 단계
                Assert.That(HudText(cards.Find("RightActive/Title")), Does.Contain("+1")); // 오른쪽 카드의 공유 단계
                Assert.That(HudText(cards.Find("LeftActive/Mouse")), Does.Contain("비용20")); // 현재 조합 비용 표시
                Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 전투 발사와 대기시간
                book.GetType().GetMethod("Use", hidden).Invoke(book, new object[] { 0 }); // 실제 첫 슬롯 시전
                yield return WaitFrames(2); // 두 카드의 대기 표기 갱신
                Assert.That(HudText(cards.Find("LeftActive/Mouse")), Does.Contain("초")); // 첫 카드의 남은 대기시간
                Assert.That(HudText(cards.Find("RightActive/Mouse")), Does.Contain("초")); // 같은 구성의 대기시간 공유
                caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 대기시간만 제거
                Component skills = FindPilot().GetComponent("FlightSkillController"); // 실제 공용 마력
                skills.GetType().GetField("mana", hidden).SetValue(skills, 0f); // 마력 부족 상태
                yield return WaitFrames(2); // 자원 경고 표기 갱신
                Assert.That(HudText(cards.Find("LeftActive/Mouse")), Does.Contain("마력 부족")); // 시전 실패 사유 표시
                System.Type kind = book.GetType().GetNestedType("Upgrade", System.Reflection.BindingFlags.NonPublic); // 제작 마법의 강화 종류
                object damage = System.Enum.Parse(kind, "Damage"); // 같은 구성의 피해 강화
                var upgrade = book.GetType().GetMethod("TryUpgrade", hidden); // 실제 수치 강화 적용
                Assert.That(upgrade.Invoke(book, new[] { (object)0, damage }), Is.True); // 두 번째 강화 단계
                Assert.That(upgrade.Invoke(book, new[] { (object)0, damage }), Is.True); // 세 번째 강화 단계
                collect.Invoke(choices, null); // 상한 적용 후 유효 후보 재생성
                foreach (object card in candidates) // 상한 적용 후 실제 후보
                {
                    string reward = card.GetType().GetField("reward").GetValue(card).ToString(); // 후보 효과 번호
                    Assert.That(reward.StartsWith("CraftLeft") || reward.StartsWith("CraftRight"), Is.False); // 최대 강화 마법의 후보 제외
                }
                Component equipment = FindPilot().GetComponent("FlightBroomEquipment"); // 마법 공격력의 실제 장비 보정
                float[] bonuses = (float[])equipment.GetType().GetField("bonuses", hidden).GetValue(equipment); // 현재 장비 효과 배열
                System.Type stat = equipment.GetType().Assembly.GetType("ProjectG.FlightBroomStat"); // 장비의 능력치 종류
                int magicPower = (int)System.Enum.Parse(stat, "MagicPower"); // 마법 편성 한도 보정 위치
                float originalBonus = bonuses[magicPower]; // 기존 장비 효과 보관
                bonuses[magicPower] = -99f; // 편성 비용보다 낮은 시험 한도
                yield return WaitFrames(2); // 실제 카드 제약 표시 갱신
                Assert.That(cards.Find("LeftActive").gameObject.activeSelf, Is.False); // 한도를 넘은 첫 전투 카드 자동 해제
                Assert.That(cards.Find("RightActive").gameObject.activeSelf, Is.False); // 한도를 넘은 둘째 전투 카드 자동 해제
                bonuses[magicPower] = originalBonus; // 시험한 장비 효과 복원
                yield return WaitFrames(2); // 원래 한도의 자동 재편성
                Assert.That(cards.Find("LeftActive").gameObject.activeSelf, Is.True); // 첫 카드의 원래 슬롯 복구
                Assert.That(cards.Find("RightActive").gameObject.activeSelf, Is.True); // 둘째 카드의 원래 슬롯 복구
            }
            finally // 시험 저장만 제거
            {
                PlayerPrefs.DeleteKey(key); // 격리된 시험 키 삭제
                PlayerPrefs.Save(); // 저장 삭제 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 이전 환경 복원
                Random.state = randomState; // 다른 시험의 난수 상태 복원
            }
        }

        [UnityTest] // 마법 종류에 따른 유효 강화와 시전별 수치 확인
        public IEnumerator CraftedUpgradeFiltersInvalidEffectsAndSnapshotsCountAndBarrier() // 발사체 수와 결계 범위·지속
        {
            string key = "ProjectG.Crafted.Growth." + System.Guid.NewGuid().ToString("N"); // 독립된 임시 저장
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 이전 환경 보관
            object researchState = null; // 시험에서 임시로 수정할 연구 상태
            string[] originalUnlocks = null; // 다른 시험의 연구 상태 보존
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 사용자 저장 격리
            try // 시험 키만 삭제
            {
                yield return LoadOuterIslands(); // 제작 마법과 실제 발사기
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 출격의 마법 강화
                Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 실제 탄환과 결계
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 시험 경로
                System.Type progressType = System.Type.GetType("ProjectG.FlightProgress, Assembly-CSharp", true); // 실제 공용 연구 프로필
                object progress = progressType.GetProperty("Current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null); // 격리된 메모리 프로필
                researchState = progress.GetType().GetField("state", hidden).GetValue(progress); // 임시 연구 해금 위치
                var unlocks = researchState.GetType().GetField("unlocks"); // 기존 연구 해금 배열
                originalUnlocks = (string[])unlocks.GetValue(researchState); // 기존 시험 상태 보존
                var testUnlocks = new System.Collections.Generic.List<string>(originalUnlocks); // 필요한 요소를 추가할 시험 목록
                testUnlocks.AddRange(new[] { "research.Shape.arrow", "research.Effect.pierce", "research.Shape.barrier", "research.Formation.sphere", "research.Trajectory.stationary", "research.FireMode.placed", "research.Origin.around", "research.Effect.cancel" }); // 실제 편성에 필요한 관통과 결계 연구
                unlocks.SetValue(researchState, testUnlocks.ToArray()); // 이 검증의 메모리 연구만 해금
                var slot = book.GetType().GetMethod("Slot", hidden); // 준비된 실제 마법
                var equip = book.GetType().GetMethod("Equip", hidden); // 다음 출격 편성
                var reset = book.GetType().GetMethod("ResetRun", hidden); // 새 출격
                var can = book.GetType().GetMethod("CanUpgrade", hidden); // 실제 강화 후보 검사
                var upgrade = book.GetType().GetMethod("TryUpgrade", hidden); // 실제 강화 선택
                System.Type kind = book.GetType().GetNestedType("Upgrade", System.Reflection.BindingFlags.NonPublic); // 기획의 강화 종류
                object count = System.Enum.Parse(kind, "Count"); // 다중 발사체 수 강화
                object pierce = System.Enum.Parse(kind, "Pierce"); // 기존 관통 대상 강화
                object damage = System.Enum.Parse(kind, "Damage"); // 피해 강화
                object width = System.Enum.Parse(kind, "Width"); // 범위 강화
                object duration = System.Enum.Parse(kind, "Duration"); // 유지 시간 강화
                object thorn = slot.Invoke(book, new object[] { 1 }); // 초기 가시 산탄
                Assert.That(equip.Invoke(book, new[] { (object)0, thorn }), Is.True); // 가시 산탄 중복 장착
                reset.Invoke(book, null); // 실제 전투 슬롯 배정
                Assert.That(can.Invoke(book, new[] { (object)0, count }), Is.True); // 여러 발인 마법의 탄 수 후보
                Assert.That(can.Invoke(book, new[] { (object)0, pierce }), Is.False); // 관통 효과가 없는 마법 제외
                Assert.That(upgrade.Invoke(book, new[] { (object)0, count }), Is.True); // 추가 발사체 한 단계
                Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { thorn }), Is.True); // 실제 강화 산탄 발사
                var shots = (System.Collections.IList)caster.GetType().GetField("active", hidden).GetValue(caster); // 생성한 제작 발사체
                Assert.That(shots.Count, Is.EqualTo(9)); // 기본 여덟 발에 한 발 추가
                book.GetType().GetMethod("ClearSlot", hidden).Invoke(book, new object[] { 1 }); // 둘째 준비 칸 비우기
                System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 문서의 기본 마법 목록
                var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 읽기 전용 프리셋
                object arrows = all[9].GetType().GetProperty("Recipe").GetValue(all[9]); // 기존 관통 효과가 있는 시험 마법
                Assert.That(equip.Invoke(book, new[] { (object)0, arrows }), Is.True); // 관통 마법 한 장 편성
                reset.Invoke(book, null); // 새 편성의 출격
                var active = book.GetType().GetMethod("ActiveSlot", hidden); // 무작위 배정된 실제 칸
                int arrowLane = active.Invoke(book, new object[] { 0 }) != null ? 0 : 1; // 관통 마법이 배정된 슬롯
                Assert.That(upgrade.Invoke(book, new[] { (object)arrowLane, pierce }), Is.True); // 관통 대상 한 명 추가
                Assert.That(upgrade.Invoke(book, new[] { (object)arrowLane, pierce }), Is.True); // 최대 다섯 명까지 추가
                Assert.That(can.Invoke(book, new[] { (object)arrowLane, pierce }), Is.False); // 다섯 명을 넘는 후보 제외
                Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { arrows }), Is.True); // 강화 관통 마법 시전
                Component arrow = (Component)shots[0]; // 실제 발사된 관통 화살
                Assert.That(arrow.GetType().GetField("pierceLimit", hidden).GetValue(arrow), Is.EqualTo(5)); // 탄환의 최대 관통 대상 고정
                object barrier = all[11].GetType().GetProperty("Recipe").GetValue(all[11]); // 정화 결계 구성
                Assert.That(equip.Invoke(book, new[] { (object)0, barrier }), Is.True); // 빈 칸과 정화 결계 편성
                reset.Invoke(book, null); // 결계의 새 출격
                int lane = active.Invoke(book, new object[] { 0 }) != null ? 0 : 1; // 결계가 배정된 슬롯
                Assert.That(can.Invoke(book, new[] { (object)lane, damage }), Is.False); // 피해 없는 결계의 화력 강화 제외
                Assert.That(can.Invoke(book, new[] { (object)lane, count }), Is.False); // 단일 결계의 추가 발사 제외
                Assert.That(can.Invoke(book, new[] { (object)lane, duration }), Is.True); // 지속형의 유지시간 강화 허용
                Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { barrier }), Is.True); // 강화 전 고정 결계 생성
                var barriers = (System.Collections.IList)caster.GetType().GetField("barriers", hidden).GetValue(caster); // 현재 결계 목록
                object existing = barriers[0]; // 강화 전에 만든 결계
                float oldExpiry = (float)existing.GetType().GetField("expires").GetValue(existing); // 이전 결계 종료 시각
                Assert.That(upgrade.Invoke(book, new[] { (object)lane, width }), Is.True); // 결계 범위 강화
                Assert.That(upgrade.Invoke(book, new[] { (object)lane, duration }), Is.True); // 결계 유지시간 강화
                Assert.That((float)existing.GetType().GetField("radius").GetValue(existing), Is.EqualTo(3.5f).Within(0.001f)); // 이미 설치한 반경 유지
                Assert.That((float)existing.GetType().GetField("expires").GetValue(existing), Is.EqualTo(oldExpiry).Within(0.001f)); // 이미 설치한 유지시간 유지
                caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 임시 강화는 유지하고 결계와 쿨타임만 제거
                float started = Time.time; // 새 결계의 시전 시각
                Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { barrier }), Is.True); // 강화된 다음 결계 시전
                object next = barriers[0]; // 새로 생성한 실제 결계
                Assert.That((float)next.GetType().GetField("radius").GetValue(next), Is.EqualTo(3.675f).Within(0.001f)); // 기준 반경의 오 퍼센트 증가
                Assert.That((float)next.GetType().GetField("expires").GetValue(next) - started, Is.EqualTo(4.5f).Within(0.001f)); // 기준 유지시간에 영 점 오 초 추가
            }
            finally // 시험 저장만 정리
            {
                if (researchState != null && originalUnlocks != null) // 시험에서 임시로 변경한 연구가 있는 경우
                {
                    researchState.GetType().GetField("unlocks").SetValue(researchState, originalUnlocks); // 다른 시험의 메모리 연구 복원
                }
                PlayerPrefs.DeleteKey(key); // 고유 시험 키 삭제
                PlayerPrefs.Save(); // 삭제 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 원래 환경 복원
            }
        }

        [UnityTest] // 실제 두 마법과 결계가 공유하는 생성 상한
        public IEnumerator CraftedEmissionLimitsRejectWholeCastsWithoutSpendingMana() // 초당 발사와 동시 활성 상한
        {
            string key = "ProjectG.Crafted.Emission." + System.Guid.NewGuid().ToString("N"); // 독립적인 마법 저장 키
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 환경 변수 보관
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 시험 외 저장 차단
            try // 시험 종료 후 저장 정리
            {
                yield return LoadOuterIslands(); // 실제 조종체와 제작 마법 발사기
                ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 시험 외 적탄과 전투 종료 차단
                Transform pilot = FindPilot(); // 실제 비행체
                ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 사격으로 인한 외부 변화 차단
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 두 칸의 제작 마법
                Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실제 제작 발사기
                Component skills = pilot.GetComponent("FlightSkillController"); // 실제 공용 마력
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 실제 발사 경로
                System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기본 마법 목록
                var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 기획서 프리셋
                object barrier = all[11].GetType().GetProperty("Recipe").GetValue(all[11]); // 정화 결계 구성
                book.GetType().GetMethod("ClearSlot", hidden).Invoke(book, new object[] { 1 }); // 둘째 준비 칸 비우기
                Assert.That(book.GetType().GetMethod("Equip", hidden).Invoke(book, new[] { (object)0, barrier }), Is.True); // 정화 결계 한 장 편성
                book.GetType().GetMethod("ResetRun", hidden).Invoke(book, null); // 실제 전투 편성 배정
                var activeSlot = book.GetType().GetMethod("ActiveSlot", hidden); // 무작위 배정 결과
                int lane = activeSlot.Invoke(book, new object[] { 0 }) != null ? 0 : 1; // 결계가 있는 칸
                object thorn = all[7].GetType().GetProperty("Recipe").GetValue(all[7]); // 확정 수치가 있는 가시 산탄
                var cast = caster.GetType().GetMethod("Cast", hidden); // 실제 발사체 생성
                var ready = (System.Collections.IDictionary)caster.GetType().GetField("readyAt", hidden).GetValue(caster); // 동일 마법의 대기 기록
                for (int i = 0; i < 5; i++) // 같은 프레임의 다섯 번 시험 시전
                {
                    ready.Clear(); // 생성 한도만 분리하여 시험하는 대기시간 초기화
                    Assert.That(cast.Invoke(caster, new[] { thorn }), Is.True); // 확정된 여덟 발의 실제 생성
                }
                var shots = (System.Collections.IList)caster.GetType().GetField("active", hidden).GetValue(caster); // 실제 활성 제작 발사체 목록
                var barriers = (System.Collections.IList)caster.GetType().GetField("barriers", hidden).GetValue(caster); // 실제 활성 결계 목록
                Assert.That(shots.Count, Is.EqualTo(40)); // 다섯 시전의 정확한 초당 한도
                var tokenField = shots[0].GetType().GetField("castToken", hidden); // 각 발사체의 원래 시전 기록
                Assert.That(tokenField, Is.Not.Null); // 후속 발사에 공유할 시전 정보 존재
                object firstToken = tokenField.GetValue(shots[0]); // 첫 시전의 생성 기록
                Assert.That(firstToken, Is.Not.Null); // 실제 초기 발사에 유효한 기록
                Assert.That(tokenField.GetValue(shots[7]), Is.SameAs(firstToken)); // 같은 시전의 여덟 발은 같은 기록
                Assert.That(tokenField.GetValue(shots[8]), Is.Not.SameAs(firstToken)); // 다른 시전은 별도의 누적 한도
                var continueCast = caster.GetType().GetMethod("TryContinueEmission", hidden); // 실제 발사기의 후속 생성 예약
                Assert.That(continueCast.Invoke(caster, new[] { firstToken, (object)1 }), Is.False); // 같은 플레이어의 초당 사십 발 제한
                var use = book.GetType().GetMethod("Use", hidden); // 실제 카드 사용과 마력 소비
                var mana = skills.GetType().GetField("mana", hidden); // 소수점 실제 마력
                mana.SetValue(skills, 100f); // 결계 비용보다 충분한 마력
                use.Invoke(book, new object[] { lane }); // 같은 초에 결계 시전 시도
                Assert.That(barriers.Count, Is.Zero); // 초당 한도에서 결계 생성 거절
                Assert.That((float)mana.GetValue(skills), Is.EqualTo(100f)); // 실패 시 마력 소비 없음
                yield return WaitFrames(2); // 생성 한도의 카드 상태 반영
                Transform cards = LocateCombatHud().transform.Find("MagicCards"); // 실제 전투 카드 영역
                string cardName = lane == 0 ? "LeftActive/Mouse" : "RightActive/Mouse"; // 결계가 있는 칸의 상태 글자
                Assert.That(HudText(cards.Find(cardName)), Does.Contain("생성 한도")); // 시전 실패의 실제 원인 표시
                yield return WaitFrames(60); // 초당 생성 시간 창 종료
                float beforeCast = (float)mana.GetValue(skills); // 장비의 회복을 포함한 시전 전 마력
                Assert.That(continueCast.Invoke(caster, new[] { firstToken, (object)1 }), Is.True); // 같은 시전의 후속 생성 허용
                use.Invoke(book, new object[] { lane }); // 한도 복구 뒤 같은 결계 시전
                Assert.That(barriers.Count, Is.EqualTo(1)); // 초당 제한 복구 후 결계 생성
                Assert.That((float)mana.GetValue(skills), Is.EqualTo(beforeCast - 30f).Within(0.01f)); // 성공한 결계만 마력 소비
                book.GetType().GetMethod("ResetRun", hidden).Invoke(book, null); // 실제 발사와 제한 초기화
                lane = activeSlot.Invoke(book, new object[] { 0 }) != null ? 0 : 1; // 재출격에서 다시 배정된 결계 칸
                Assert.That(continueCast.Invoke(caster, new[] { firstToken, (object)1 }), Is.False); // 이전 출격의 후속 생성 무효
                object prefab = caster.GetType().GetField("projectile", hidden).GetValue(caster); // 씬에 등록된 발사체 원본
                for (int i = 0; i < 128; i++) // 실제 활성 상한까지 채우는 시험 개체
                {
                    shots.Add(Object.Instantiate((Component)prefab)); // 발사기가 추적하는 별도 탄환
                }
                mana.SetValue(skills, 100f); // 활성 제한 시험의 마력 초기화
                use.Invoke(book, new object[] { lane }); // 이미 가득 찬 상태의 결계 시전
                Assert.That(barriers.Count, Is.Zero); // 활성 한도에서 결계 생성 거절
                Assert.That((float)mana.GetValue(skills), Is.EqualTo(100f)); // 실패한 활성 제한은 무소모
                Component released = (Component)shots[0]; // 하나를 회수할 실제 탄환
                shots.RemoveAt(0); // 활성 목록에서 한 발 회수
                Object.Destroy(released.gameObject); // 시험 탄환 제거 예약
                use.Invoke(book, new object[] { lane }); // 한 자리가 남은 상태에서 결계 시전
                Assert.That(barriers.Count, Is.EqualTo(1)); // 백스물여덟 개 경계에서 허용
                Assert.That((float)mana.GetValue(skills), Is.EqualTo(70f).Within(0.001f)); // 성공한 결계 비용만 차감
                caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 남은 시험 개체 정리
            }
            finally // 다른 사용자의 마법 저장에 영향 없는 정리
            {
                PlayerPrefs.DeleteKey(key); // 시험 전용 저장 제거
                PlayerPrefs.Save(); // 제거 내용 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 이전 환경 복구
            }
        }

        [UnityTest] // 기존 미지원 구성도 저장에서 잃지 않는지 확인
        public IEnumerator UnsupportedLegacyRecipeRemainsSavedButCannotBeCast() // 기존 구성 보존과 미지원 안내
        {
            string key = "ProjectG.Crafted.Unsupported." + System.Guid.NewGuid().ToString("N"); // 사용자 저장과 분리한 시험 키
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 원래 환경 변수 보관
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 독립 저장 사용
            try // 이 시험의 저장만 정리
            {
                System.Type selection = System.Type.GetType("ProjectG.CraftedSpellSelection, Assembly-CSharp"); // 기존 제작 입력 형식
                System.Type rules = System.Type.GetType("ProjectG.CraftedSpellRules, Assembly-CSharp"); // 동일 구성 ID 검증기
                object draft = System.Activator.CreateInstance(selection); // 기획 수치가 없는 기본 조합
                object[] compose = { draft, null }; // 유효 구성 결과
                Assert.That(rules.GetMethod("Compose").Invoke(null, compose).ToString(), Is.EqualTo("None")); // 저장 형식에 맞는 조합
                object recipe = compose[1]; // 저장할 기존 조합
                string id = (string)recipe.GetType().GetProperty("Id").GetValue(recipe); // 유실되면 안 되는 구성 ID
                string saved = "{\"version\":2,\"recipe\":\"" + id + "\",\"left\":\"" + id + "\",\"right\":null}"; // 기존 사용자의 편성 형식
                PlayerPrefs.SetString(key, saved); // 기존 마법의 시험 저장
                PlayerPrefs.Save(); // 재진입용 데이터 기록
                yield return LoadOuterIslands(); // 실제 저장을 읽는 출격
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 복원한 마법 도감
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 슬롯 경로
                var slot = book.GetType().GetMethod("Slot", hidden); // 준비 편성 조회
                object restored = slot.Invoke(book, new object[] { 0 }); // 이전에 편성한 미지원 마법
                Assert.That(restored, Is.Not.Null); // 구성 ID를 빈 칸으로 바꾸지 않음
                Assert.That((string)restored.GetType().GetProperty("Id").GetValue(restored), Is.EqualTo(id)); // 같은 조합의 식별자 보존
                var active = book.GetType().GetMethod("ActiveSlot", hidden); // 실제 출격의 무작위 배정
                Assert.That(active.Invoke(book, new object[] { 0 }), Is.Null); // 미지원 조합의 첫 전투 카드 제외
                Assert.That(active.Invoke(book, new object[] { 1 }), Is.Null); // 미지원 조합의 둘째 전투 카드 제외
                int lane = 0; // 비어 있는 전투 칸의 사용 시도
                Assert.That(book.GetType().GetMethod("CanCast", hidden).Invoke(book, new object[] { lane }), Is.False); // 미확정 수치의 실제 시전 차단
                Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 실제 전투 발사기
                book.GetType().GetMethod("Use", hidden).Invoke(book, new object[] { lane }); // 지원하지 않는 구성의 사용 시도
                Assert.That(Metric(caster, "CastCount"), Is.Zero); // 시전과 성공 효과 없음
                yield return WaitFrames(2); // 전투 카드의 상태 표시
                Transform cards = LocateCombatHud().transform.Find("MagicCards"); // 실제 마법 카드
                Assert.That(cards.Find("LeftActive").gameObject.activeSelf, Is.False); // 미지원 전투 카드를 별도로 표시하지 않음
                Assert.That(cards.Find("RightActive").gameObject.activeSelf, Is.False); // 무작위 배정에도 빈 카드 유지
                book.GetType().GetMethod("ClearSlot", hidden).Invoke(book, new object[] { 1 }); // 다른 편성 변경과 저장
                Assert.That(PlayerPrefs.GetString(key), Does.Contain(id)); // 다른 변경 뒤에도 기존 ID 유지
                yield return LoadOuterIslands(); // 변경한 저장으로 다시 진입
                book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 새로 생성된 도감
                restored = slot.Invoke(book, new object[] { 0 }); // 이전 마법 재복원
                Assert.That((string)restored.GetType().GetProperty("Id").GetValue(restored), Is.EqualTo(id)); // 다른 슬롯 저장 후에도 보존
            }
            finally // 시험 데이터만 삭제
            {
                PlayerPrefs.DeleteKey(key); // 독립 저장 키 제거
                PlayerPrefs.Save(); // 시험 키 삭제 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 원래 저장 환경 복구
            }
        }

        [UnityTest] // 실제 마물에 적용하는 지속 피해의 시간과 중첩 한도
        public IEnumerator CraftedStatusHonorsTickTimingStackCapAndNoRefresh() // 화상·비중첩 저주의 공통 상태 처리
        {
            yield return LoadOuterIslands(); // 실제 체력과 속성 판정이 있는 항로
            Transform pilot = FindPilot(); // 시험 중 자동 공격을 끌 조종체
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 기본탄의 외부 피해 차단
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 다른 마물과 전투 종료 차단
            Transform enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy"); // 실제 일반 마물
            Component life = enemy.GetComponent("FlightEnemy"); // 실제 피해와 체력
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실전 상태 API
            life.GetType().GetField("maxHealth", hidden).SetValue(life, 300); // 모든 틱을 확인할 체력
            enemy.gameObject.SetActive(true); // 새 체력의 마물 활성화
            System.Type statusType = System.Type.GetType("ProjectG.FlightCraftedStatus, Assembly-CSharp"); // 공통 지속 피해 상태
            Assert.That(statusType, Is.Not.Null); // 실제 상태 처리 구현 확인
            Component status = enemy.gameObject.AddComponent(statusType); // 이 적의 독립 상태 목록
            var apply = statusType.GetMethod("TryApply", hidden); // 시전 당시 피해와 중첩 적용
            object[] burn = { "fire-burn", "fire", 3f, 0.5f, 4, 3, false }; // 화상 한 중첩의 확정 시험값
            Assert.That(apply.Invoke(status, burn), Is.True); // 첫 화상 중첩
            Assert.That(apply.Invoke(status, burn), Is.True); // 둘째 화상 중첩
            Assert.That(apply.Invoke(status, burn), Is.True); // 셋째 화상 중첩
            Assert.That(apply.Invoke(status, burn), Is.False); // 최대 세 중첩에서 추가 거절
            yield return WaitFrames(25); // 첫 영 점 오 초 이전
            Assert.That(Metric(life, "Health"), Is.EqualTo(300)); // 즉시 지속 피해를 주지 않음
            yield return WaitFrames(102); // 각 중첩의 네 틱과 독립 만료
            Assert.That(Metric(life, "Health"), Is.EqualTo(264)); // 화상 세 중첩 총 열두 틱
            object[] mark = { "shadow-mark", "dark", 8f, 0.5f, 6, 1, false }; // 그림자 낙인의 확정 시험값
            Assert.That(apply.Invoke(status, mark), Is.True); // 첫 저주 적용
            yield return WaitFrames(35); // 첫 저주 틱 이후
            Assert.That(apply.Invoke(status, mark), Is.False); // 만료 전 재적용과 갱신 차단
            yield return WaitFrames(155); // 원래 시작 시각 기준 여섯 틱 종료
            Assert.That(Metric(life, "Health"), Is.EqualTo(216)); // 저주 한 번의 피해 사십팔
            object[] invalid = { "invalid", "fire", 3f, 0.1f, 4, 3, false }; // 최소 틱 간격보다 빠른 상태
            Assert.That(apply.Invoke(status, invalid), Is.False); // 공통 최소 영 점 오 초 제한
            object[] tooLong = { "invalid", "fire", 3f, 0.5f, 13, 3, false }; // 육 초를 넘는 지속 상태
            Assert.That(apply.Invoke(status, tooLong), Is.False); // 공통 지속 시간 상한
            Assert.That(apply.Invoke(status, burn), Is.True); // 첫 화상 중첩의 독립 시작
            yield return WaitFrames(30); // 첫 중첩 시작 후 영 점 오 초
            Assert.That(apply.Invoke(status, burn), Is.True); // 서로 다른 만료 시각의 둘째 중첩
            yield return WaitFrames(30); // 첫 중첩 시작 후 일 초
            Assert.That(apply.Invoke(status, burn), Is.True); // 다시 다른 만료 시각의 셋째 중첩
            Assert.That(apply.Invoke(status, burn), Is.False); // 세 중첩 동안 추가 차단
            yield return WaitFrames(62); // 첫 중첩만 만료되는 시간
            Assert.That(apply.Invoke(status, burn), Is.True); // 먼저 만료된 중첩의 자리만 재사용
            Assert.That(apply.Invoke(status, burn), Is.False); // 나머지 중첩은 독립적으로 유지
            enemy.gameObject.SetActive(false); // 적의 회수 또는 처치
            enemy.gameObject.SetActive(true); // 같은 마물의 새 출현
            yield return WaitFrames(40); // 이전 화상의 첫 틱 이후까지 대기
            Assert.That(Metric(life, "Health"), Is.EqualTo(300)); // 이전 출현의 상태가 남지 않음
        }

        [UnityTest] // 정지된 전투와 독립 연습의 지속 피해 시간
        public IEnumerator CraftedStatusKeepsPracticeAndPausedCombatSeparate() // 연습 표적과 실전 마물의 시간 격리
        {
            yield return LoadOuterIslands(); // 실제 전투와 제작 연습 화면
            Transform pilot = FindPilot(); // 자동 사격을 끌 조종체
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 실전 마물의 외부 피해 차단
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 외부 전투 진행 차단
            Transform enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy"); // 실제 전투 마물
            enemy.gameObject.SetActive(true); // 현재 체력 활성화
            Component life = enemy.GetComponent("FlightEnemy"); // 실전 체력
            int health = Metric(life, "Health"); // 정지 전 체력
            System.Type statusType = System.Type.GetType("ProjectG.FlightCraftedStatus, Assembly-CSharp"); // 공통 지속 피해 처리
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 상태 적용
            Component liveStatus = enemy.gameObject.AddComponent(statusType); // 실전 마물의 상태 목록
            var apply = statusType.GetMethod("TryApply", hidden); // 시전 시점의 상태 등록
            Assert.That(apply.Invoke(liveStatus, new object[] { "burn", "fire", 3f, 0.5f, 4, 3, false }), Is.True); // 실전 시간으로 흐르는 화상
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 정지 메뉴
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 전투 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 열기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 독립 제작 연습장
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 독립 연습 표적
            Component practiceStatus = target.gameObject.AddComponent(statusType); // 연습 표적의 상태 목록
            Assert.That(apply.Invoke(practiceStatus, new object[] { "burn", "fire", 3f, 0.5f, 4, 3, true }), Is.True); // 연습 전용 시간의 화상
            yield return WaitFrames(35); // 독립 연습에서 첫 틱이 지나는 시간
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(3)); // 정지 중에도 연습 피해 한 번 반영
            Assert.That(Metric(life, "Health"), Is.EqualTo(health)); // 정지된 실전 마물의 피해와 시간 보존
            ClickPauseButton(editor, "Back"); // 제작 연습을 닫고 원래 메뉴로 이동
            var effects = (System.Collections.IList)statusType.GetField("effects", hidden).GetValue(practiceStatus); // 연습 표적의 상태 목록
            Assert.That(effects.Count, Is.Zero); // 연습 종료 때 남은 중첩 제거
        }

        [UnityTest] // 마법 공격력의 피해 배율과 시전 시점 고정
        public IEnumerator CraftedMagicPowerScalesDamageAtCastTime() // 기준 공격력 백과 실제 장비 보정
        {
            string key = "ProjectG.Crafted.Power." + System.Guid.NewGuid().ToString("N"); // 다른 저장과 분리한 시험 키
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 환경 보관
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 임시 마법 저장 사용
            try // 시험이 끝나면 사용자 저장 환경 복구
            {
                yield return LoadOuterIslands(); // 실제 비행체와 제작 마법
                Transform pilot = FindPilot(); // 실제 전투 조종체
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 출격용 마법 구성
                Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 시전 수치를 고정하는 발사기
                Component equipment = pilot.GetComponent("FlightBroomEquipment"); // 실제 마법 공격력 보정
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 런타임 수치 확인
                object fire = book.GetType().GetMethod("Slot", hidden).Invoke(book, new object[] { 0 }); // 기준 파이어 볼트
                float[] bonuses = (float[])equipment.GetType().GetField("bonuses", hidden).GetValue(equipment); // 장착 수치 합계
                System.Type stat = equipment.GetType().Assembly.GetType("ProjectG.FlightBroomStat"); // 실제 장비 능력치
                int magicPower = (int)System.Enum.Parse(stat, "MagicPower"); // 마법 공격력 보정 위치
                float original = bonuses[magicPower]; // 이전 장비 수치
                bonuses[magicPower] = 20f; // 기준 백에 이십을 더한 공격력
                var stats = caster.GetType().GetMethod("EffectiveStats", hidden); // 현재 시전의 실제 피해
                object boosted = stats.Invoke(caster, new[] { fire }); // 공격력 백이십의 성능
                Assert.That((float)boosted.GetType().GetField("Damage").GetValue(boosted), Is.EqualTo(42f).Within(0.001f)); // 기준 삼십오에 일 점 이 배 적용
                Assert.That((float)boosted.GetType().GetField("Mana").GetValue(boosted), Is.EqualTo(15f)); // 공격력은 마력 비용을 변경하지 않음
                Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { fire }), Is.True); // 강화된 공격력으로 실제 발사
                var active = (System.Collections.IList)caster.GetType().GetField("active", hidden).GetValue(caster); // 생성된 발사체 목록
                Component shot = (Component)active[0]; // 시전 당시 수치를 가진 실제 탄환
                bonuses[magicPower] = -20f; // 발사 후 공격력을 팔십으로 변경
                object captured = shot.GetType().GetField("profile", hidden).GetValue(shot); // 이미 발사한 탄환의 기록
                Assert.That((float)captured.GetType().GetField("Damage").GetValue(captured), Is.EqualTo(42f).Within(0.001f)); // 이전 탄환의 피해 불변
                Transform enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy"); // 실제 속성 피해를 받을 마물
                Component life = enemy.GetComponent("FlightEnemy"); // 마물의 체력과 방어 속성
                life.GetType().GetField("maxHealth", hidden).SetValue(life, 300); // 폭발 피해를 측정할 체력
                life.GetType().GetField("defenseElement", hidden).SetValue(life, "grass"); // 화염에 유리한 방어 속성
                enemy.gameObject.SetActive(true); // 시험 마물 활성화
                enemy.position = pilot.position + Vector3.forward * 8f; // 충돌 가능한 실제 폭발 위치
                Physics.SyncTransforms(); // 폭발 검사의 위치 반영
                shot.GetType().GetMethod("Splash", hidden).Invoke(shot, new object[] { enemy.position }); // 같은 탄환의 실제 폭발 피해
                Assert.That(Metric(life, "Health"), Is.EqualTo(255)); // 기준 이십오에 공격력 일 점 이 배와 상성 일 점 오 배
                object next = stats.Invoke(caster, new[] { fire }); // 다음 시전의 낮아진 공격력
                Assert.That((float)next.GetType().GetField("Damage").GetValue(next), Is.EqualTo(28f).Within(0.001f)); // 기준 삼십오에 영 점 팔 배 적용
                bonuses[magicPower] = original; // 원래 장비 보정 복원
            }
            finally // 격리된 시험 저장 정리
            {
                PlayerPrefs.DeleteKey(key); // 시험 키만 삭제
                PlayerPrefs.Save(); // 제거 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 기존 환경 복구
            }
        }

        [UnityTest] // 기존 홀리 샤워의 확정 관통 조건 확인
        public IEnumerator HolyShowerArrowsPierceThreeDistinctTargetsOnlyOnce() // 한 발당 관통 한도와 네 번째 표적
        {
            yield return LoadOuterIslands(); // 실제 제작 연습장이 있는 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 제작 연습으로 가는 정지 메뉴
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 전투 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 독립 연습장 입장
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component editorComponent = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기의 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 연습 경로 조회
            Component range = (Component)editorComponent.GetType().GetField("range", hidden).GetValue(editorComponent); // 연습용 제작 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 카메라와 분리된 시험 발사점
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획 마법 목록
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 읽기 전용 프리셋
            object recipe = all[9].GetType().GetProperty("Recipe").GetValue(all[9]); // 홀리 샤워 구성
            System.Type vectors = range.GetType().Assembly.GetType("ProjectG.CraftedSpellVectors"); // 실제 발사 방향 계산기
            Vector3 direction = (Vector3)vectors.GetMethod("Direction").Invoke(null, new[] { recipe, (object)muzzle.rotation, 0, 36f }); // 첫 화살의 확정 원뿔 방향
            Component original = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 피해를 기록하는 원본 표적
            var targets = new System.Collections.Generic.List<Component>(); // 한 화살의 직선 경로에 놓을 표적
            for (int i = 0; i < 4; i++) // 한 발에 닿는 네 개의 서로 다른 대상
            {
                Component target = i == 0 ? original : Object.Instantiate(original, original.transform.parent); // 독립 피해 기록을 가진 표적 복제
                target.transform.position = muzzle.position + direction * (3f + i * 2f); // 첫 화살 경로의 연속 위치
                targets.Add(target); // 각 표적의 실제 피해 확인
            }
            Physics.SyncTransforms(); // 이동한 표적의 물리 위치 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 실제 홀리 샤워 시험 발사
            yield return WaitFrames(110); // 여섯 화살의 이동과 최대 사거리 대기
            for (int i = 0; i < 3; i++) // 첫 세 개의 서로 다른 표적
            {
                Assert.That(Metric(targets[i], "TotalDamage"), Is.EqualTo(12)); // 각 대상에 같은 화살의 단일 피해
            }
            Assert.That(Metric(targets[3], "TotalDamage"), Is.Zero); // 네 번째 표적은 관통 한도 밖
            range.GetType().GetMethod("ResetCasts", hidden).Invoke(range, null); // 발사체와 연습 대기시간 초기화
            original.GetType().GetMethod("ResetDamage", hidden).Invoke(original, null); // 첫 표적의 피해 기록 초기화
            for (int i = 1; i < targets.Count; i++) // 나머지 관통 표적을 충돌에서 제거
            {
                targets[i].gameObject.SetActive(false); // 넓은 표적 한 개만 유지
            }
            original.transform.position = muzzle.position + muzzle.forward * 3.5f; // 여섯 화살이 닿는 중앙 위치
            original.GetComponent<SphereCollider>().radius = 2f / original.transform.lossyScale.x; // 모든 화살이 닿는 실제 반경
            Physics.SyncTransforms(); // 표적 크기와 위치를 물리에 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 여섯 화살 재시전
            yield return WaitFrames(110); // 모든 화살의 단일 적중 대기
            Assert.That(Metric(original, "TotalDamage"), Is.EqualTo(72)); // 여섯 화살이 각각 한 번씩 주는 최대 피해
        }

        [UnityTest] // 급류 관통탄의 세 발과 개별 관통 검증
        public IEnumerator TorrentNeedlesConvergeAndEachPiercesThreeTargets() // 실제 발사체의 수치와 관통 경계
        {
            yield return LoadOuterIslands(); // 실제 제작 연습장이 있는 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 제작 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 독립 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component editorComponent = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실전과 같은 내부 발사 경로
            Component range = (Component)editorComponent.GetType().GetField("range", hidden).GetValue(editorComponent); // 독립 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 실제 연습 발사 위치
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 마법 목록
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 검증된 프리셋 목록
            object recipe = all[4].GetType().GetProperty("Recipe").GetValue(all[4]); // 급류 관통탄 구성
            var cast = range.GetType().GetMethod("Cast", hidden); // 실제 제작 마법 시전
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 연습 피해 표적
            target.transform.position = muzzle.position + muzzle.forward * 3.5f; // 세 발을 함께 맞는 표적 위치
            target.GetComponent<SphereCollider>().radius = 2f / target.transform.lossyScale.x; // 세 발이 닿는 표적 반경
            Physics.SyncTransforms(); // 변경한 실제 물리 위치
            Assert.That(cast.Invoke(range, new[] { recipe }), Is.True); // 확정된 급류 관통탄 시전
            var shots = (System.Collections.IList)range.GetType().GetField("active", hidden).GetValue(range); // 실제 생성된 세 발
            Assert.That(shots.Count, Is.EqualTo(3)); // 세 개의 독립 발사체
            Component first = (Component)shots[0]; // 첫 침탄
            Component second = (Component)shots[1]; // 가운데 침탄
            Assert.That(Vector3.Distance(first.transform.position, second.transform.position), Is.GreaterThan(0.1f)); // 서로 겹치지 않는 집중 발사 위치
            yield return WaitFrames(100); // 세 침탄이 넓은 표적에 닿을 시간
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(66)); // 세 발의 단일 표적 피해
            range.GetType().GetMethod("ResetCasts", hidden).Invoke(range, null); // 실제 탄과 연습 마력 초기화
            target.GetType().GetMethod("ResetDamage", hidden).Invoke(target, null); // 원본 표적의 피해 초기화
            target.GetComponent<SphereCollider>().radius = 0.5f; // 한 발만 맞는 원래 표적 크기
            Assert.That(cast.Invoke(range, new[] { recipe }), Is.True); // 관통 제한을 확인할 재시전
            first = (Component)shots[0]; // 첫 번째 실제 침탄
            Vector3 start = first.transform.position; // 첫 침탄의 개별 출발 위치
            Vector3 direction = first.transform.forward; // 수렴을 향하는 실제 발사 방향
            for (int i = 1; i < shots.Count; i++) // 다른 침탄의 시험 간섭 차단
            {
                Component other = (Component)shots[i]; // 나머지 실제 침탄
                other.gameObject.SetActive(false); // 이 시험에서는 한 발만 진행
            }
            var targets = new System.Collections.Generic.List<Component>(); // 한 침탄의 직선 경로에 놓을 대상
            for (int i = 0; i < 4; i++) // 관통 상한을 넘는 네 표적
            {
                Component entry = i == 0 ? target : Object.Instantiate(target, target.transform.parent); // 독립 피해 기록을 가진 표적
                entry.transform.position = start + direction * (3f + i * 2f); // 첫 침탄의 실제 진행 경로
                targets.Add(entry); // 각 표적의 피해 확인
            }
            Physics.SyncTransforms(); // 새 표적 위치를 실제 충돌에 반영
            yield return WaitFrames(110); // 한 침탄의 최대 거리까지 비행
            for (int i = 0; i < 3; i++) // 앞쪽의 세 표적
            {
                Assert.That(Metric(targets[i], "TotalDamage"), Is.EqualTo(22)); // 같은 침탄은 각 대상에 한 번 피해
            }
            Assert.That(Metric(targets[3], "TotalDamage"), Is.Zero); // 네 번째 대상은 관통 한도 밖
        }

        [UnityTest] // 플레임 스파이럴의 순차 발사와 화상 실제 판정
        public IEnumerator FlameSpiralFiresSequentiallyAndAppliesIndependentBurns() // 열두 발과 최대 세 화상 중첩
        {
            yield return LoadOuterIslands(); // 실제 제작 연습장이 있는 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 전투 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 열기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습 화면 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component editorComponent = editor.GetComponent("FlightCraftedSpellEditor"); // 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 발사와 통계 조회
            Component range = (Component)editorComponent.GetType().GetField("range", hidden).GetValue(editorComponent); // 격리된 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 실제 시험 발사점
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 검증된 프리셋 목록
            object recipe = all[1].GetType().GetProperty("Recipe").GetValue(all[1]); // 플레임 스파이럴 구성
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 실제 연습 피해 표적
            target.transform.position = muzzle.position + muzzle.forward * 3.5f; // 순차 발사 탄환의 실제 경로
            target.GetComponent<SphereCollider>().radius = 2f / target.transform.lossyScale.x; // 열두 발이 모두 닿는 반경
            Physics.SyncTransforms(); // 표적 충돌 위치 갱신
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 플레임 스파이럴 시전
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(1)); // 첫 탄환만 즉시 발사
            yield return WaitFrames(36); // 약 영 점 육 초 동안 순차 발사
            Assert.That(Metric(range, "EmissionCount"), Is.InRange(6, 7)); // 한꺼번에 열두 발을 만들지 않음
            yield return WaitFrames(24); // 첫 발부터 약 일 초가 지난 시점
            Assert.That(Metric(range, "EmissionCount"), Is.InRange(10, 11)); // 아직 마지막 영 점 일 초 간격 진행
            yield return WaitFrames(10); // 첫 발부터 약 일 점 일 초 경과
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(12)); // 지정한 열두 발의 순차 발사 완료
            yield return WaitFrames(186); // 세 화상의 마지막 틱까지 대기
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(96)); // 직격 육십과 세 중첩 화상 삼십육
            range.GetType().GetMethod("ResetCasts", hidden).Invoke(range, null); // 두 번째 시험 전 발사기 초기화
            target.GetType().GetMethod("ResetDamage", hidden).Invoke(target, null); // 이전 피해 기록 초기화
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 화상과 후속 발사의 재시작
            yield return WaitFrames(40); // 일부 화상과 순차 발사가 남은 상태
            ClickPauseButton(editor, "ResetTest"); // 실제 연습 초기화 버튼
            yield return WaitFrames(140); // 이전 후속 발사와 화상이 끝날 시간
            Assert.That(Metric(range, "EmissionCount"), Is.Zero); // 예약된 나머지 발사는 모두 취소
            Assert.That(Metric(target, "TotalDamage"), Is.Zero); // 초기화 전 화상도 더 이상 피해를 주지 않음
        }

        [UnityTest] // 회류 고리의 왕복 경로와 시전별 중복 피해 제한
        public IEnumerator ReturningRingHitsOnceOutwardAndOnceOnReturn() // 여섯 원반의 실제 왕복 피해
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 전투 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component editorComponent = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 발사 경로 조회
            Component range = (Component)editorComponent.GetType().GetField("range", hidden).GetValue(editorComponent); // 격리된 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 실제 시험 발사점
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 검증된 구성 목록
            object recipe = all[5].GetType().GetProperty("Recipe").GetValue(all[5]); // 회류 고리 구성
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 실제 피해를 기록하는 표적
            target.transform.position = muzzle.position + muzzle.forward * 6f; // 왕복 경로 중앙의 표적
            target.GetComponent<SphereCollider>().radius = 2f / target.transform.lossyScale.x; // 여섯 원반이 모두 통과하는 반경
            Physics.SyncTransforms(); // 변경된 실제 충돌 위치
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 회류 고리 실제 발사
            var shots = (System.Collections.IList)range.GetType().GetField("active", hidden).GetValue(range); // 현재 발사체 목록
            Assert.That(shots.Count, Is.EqualTo(6)); // 여섯 개의 독립 원반
            Component first = (Component)shots[0]; // 첫 번째 원반
            Component second = (Component)shots[1]; // 두 번째 원반
            Assert.That(Vector3.Distance(first.transform.position, second.transform.position), Is.GreaterThan(0.1f)); // 원형 배열의 서로 다른 발사 위치
            Assert.That(first.transform.localScale.z, Is.LessThan(first.transform.localScale.x * 0.5f)); // 구체와 구별되는 납작한 원반 외형
            Vector3 start = first.transform.position; // 첫 원반의 발사 시점 생성 위치
            yield return WaitFrames(33); // 전진 중 표적을 지나는 시간
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(28)); // 여섯 발이 같은 대상에 전진 피해 한 번
            Assert.That(Vector3.Distance(first.transform.position, start), Is.LessThanOrEqualTo(12.05f)); // 편도 거리 십이 미터 상한
            yield return WaitFrames(90); // 반환 경로를 지나 원래 발사점 복귀
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(56)); // 복귀 경로에서 한 번만 추가 피해
            Assert.That(first == null || !first.gameObject.activeInHierarchy, Is.True); // 발사 지점으로 돌아온 원반 종료
        }

        [UnityTest] // 씨앗 포대의 지연과 분열 후 피해 검증
        public IEnumerator SeedTurretSplitsOnlyAfterDelayAndDoesNotExplode() // 모탄 한 개와 자탄 세 발
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component owner = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 내부 상태 조회
            Component range = (Component)owner.GetType().GetField("range", hidden).GetValue(owner); // 격리된 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 실제 시험 발사점
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object recipe = all[8].GetType().GetProperty("Recipe").GetValue(all[8]); // 씨앗 포대 구성
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 실제 연습 표적
            target.transform.position = muzzle.position + muzzle.forward * 5f; // 첫 자탄 경로의 표적
            Physics.SyncTransforms(); // 변경된 충돌 위치 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 실제 포대 설치
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(1)); // 설치체 하나만 즉시 생성
            float initialWait = (float)range.GetType().GetMethod("CooldownRemaining", hidden).Invoke(range, new[] { recipe }); // 지연과 종료 후 재사용 시간
            Assert.That(initialWait, Is.InRange(8.9f, 9.1f)); // 일 초 설치와 팔 초 대기의 합
            yield return WaitFrames(45); // 일 초보다 짧은 설치 대기
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(1)); // 지연 중 자탄 없음
            Assert.That(Metric(target, "TotalDamage"), Is.Zero); // 모탄 자체의 폭발 피해 없음
            yield return WaitFrames(45); // 분열과 자탄의 표적 도달 대기
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(4)); // 원래 시전의 자탄 세 발
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(26)); // 자탄 한 발의 확정 피해
            yield return WaitFrames(80); // 자탄의 사거리 종료 후 대기
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(4)); // 자탄의 재분열 없음
            ClickPauseButton(editor, "ResetTest"); // 남아 있는 설치와 마력 초기화
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 새 포대 설치
            yield return WaitFrames(20); // 분열 이전의 설치 상태
            ClickPauseButton(editor, "ResetTest"); // 지연 중인 설치체 회수
            yield return WaitFrames(80); // 원래 분열 예정 시각 이후
            Assert.That(Metric(range, "EmissionCount"), Is.Zero); // 초기화 후 이전 자탄 생성 금지
        }

        [UnityTest] // 황혼 파열의 지정 좌표와 지연 광역 피해
        public IEnumerator TwilightBurstDamagesEachTargetOnceAfterDelay() // 직격 없이 한 번 폭발
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component owner = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 내부 상태 조회
            Component range = (Component)owner.GetType().GetField("range", hidden).GetValue(owner); // 격리된 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 실제 시험 발사점
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object recipe = all[14].GetType().GetProperty("Recipe").GetValue(all[14]); // 황혼 파열 구성
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 피해를 기록할 표적
            target.transform.position = muzzle.position + muzzle.forward * 20f; // 지정 거리의 폭발 중심
            Component outside = Object.Instantiate(target, target.transform.parent); // 범위 밖의 독립 표적
            outside.transform.position = target.transform.position + muzzle.right * 5f; // 폭발 반경보다 멀리 배치
            Physics.SyncTransforms(); // 변경된 충돌 위치 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 지정 좌표의 실제 설치
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(1)); // 폭발체 한 개만 생성
            yield return WaitFrames(45); // 일 초보다 짧은 지연
            Assert.That(Metric(target, "TotalDamage"), Is.Zero); // 설치체 직격 피해 없음
            yield return WaitFrames(40); // 지연 폭발 이후 대기
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(70)); // 중심 표적의 확정 폭발 피해
            Assert.That(Metric(outside, "TotalDamage"), Is.Zero); // 범위 밖 표적 피해 없음
            yield return WaitFrames(80); // 폭발 이후 추가 판정 검사
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(70)); // 한 시전의 폭발은 한 번
        }

        [UnityTest] // 덩굴 고리의 실제 적 이동 제어
        public IEnumerator VineRingRootsNormalEnemyOnceAndPreservesBossResistance() // 일반·정예 속박과 보스 면역
        {
            yield return LoadOuterIslands(); // 실제 적과 출격 마법이 있는 씬
            Transform pilot = FindPilot(); // 현재 조종사와 발사기
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 시험 중 웨이브 개입 차단
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 사격의 피해 제외
            Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실전 마법 발사기
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 이동과 조준 조회
            System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object recipe = all[6].GetType().GetProperty("Recipe").GetValue(all[6]); // 덩굴 고리 구성
            Vector3 point = (Vector3)caster.GetType().GetMethod("AimPoint", hidden).Invoke(caster, new object[] { 18f }); // 실전 조준으로 지정한 위치
            Transform enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy"); // 실제 일반 마물
            Component route = enemy.GetComponent("FlightEnemyRoute"); // 실제 출현 이동 경로
            route.GetType().GetMethod("Begin").Invoke(route, new object[] { GetThreatPool(), pilot }); // 정상적인 경로 시작
            route.GetType().GetField("runEntry", hidden).SetValue(route, point); // 영역 중앙에서 시작
            route.GetType().GetField("runAttack", hidden).SetValue(route, point + Vector3.right * 8f); // 속박 해제 후 이동할 위치
            route.GetType().GetField("entrySeconds", hidden).SetValue(route, 8f); // 초당 일 미터의 진입 경로
            enemy.position = point; // 실제 영역 중심에 배치
            Physics.SyncTransforms(); // 변경된 실제 충돌 위치
            Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { recipe }), Is.True); // 실제 덩굴 영역 설치
            yield return WaitFrames(35); // 일 점 이 초보다 짧은 시간
            Assert.That(Vector3.Distance(enemy.position, point), Is.LessThan(0.1f)); // 일반 적의 이동 속박
            yield return WaitFrames(70); // 속박 종료 이후 이동 대기
            Assert.That(Vector3.Distance(enemy.position, point), Is.GreaterThan(0.2f)); // 같은 시전의 재속박 없이 이동 재개
            Component status = enemy.GetComponent("FlightCraftedStatus"); // 실제 대상에 연결된 상태 관리자
            Component life = enemy.GetComponent("FlightEnemy"); // 일반·보스 구분을 가진 실제 체력
            object boss = System.Enum.Parse(life.GetType().Assembly.GetType("ProjectG.FlightLootTier"), "MidBoss"); // 중간 보스 등급
            life.GetType().GetField("lootTier", hidden).SetValue(life, boss); // 같은 적을 보스 조건으로 전환
            Assert.That(status.GetType().GetMethod("TryRoot", hidden).Invoke(status, new object[] { 1.2f }), Is.False); // 보스 강제 속박 차단
            object normal = System.Enum.Parse(life.GetType().Assembly.GetType("ProjectG.FlightLootTier"), "Normal"); // 일반 전투 등급
            life.GetType().GetField("lootTier", hidden).SetValue(life, normal); // 보스 면역 조건 해제
            life.GetType().GetField("elite", hidden).SetValue(life, true); // 동일한 일반 경로의 정예 마물
            Assert.That(status.GetType().GetMethod("TryRoot", hidden).Invoke(status, new object[] { 1.2f }), Is.True); // 정예에게 감소된 속박 적용
            float until = (float)route.GetType().GetField("rootedUntil", hidden).GetValue(route); // 실제 경로의 해제 시각
            Assert.That(until - Time.time, Is.EqualTo(0.6f).Within(0.02f)); // 정예의 속박 지속 시간 절반
            enemy.position = pilot.position + Vector3.up * 50f; // 지형이 없는 넉백 시험 위치
            Physics.SyncTransforms(); // 장애물 검사에 현재 위치 반영
            Vector3 before = enemy.position; // 실제 정예 이동 전 좌표
            Assert.That(status.GetType().GetMethod("TryKnockback", hidden).Invoke(status, new object[] { Vector3.right, 2f }), Is.True); // 정예의 실제 넉백
            Assert.That(Vector3.Distance(enemy.position, before), Is.EqualTo(1f).Within(0.02f)); // 기준 이 미터의 절반 이동
        }

        [UnityTest] // 잿불 항로의 지속 피해와 기록 거리
        public IEnumerator EmberTrailTicksOncePerIntervalAndClampsRecordedPath() // 마지막 틱과 경로 상한
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component owner = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 내부 상태 조회
            Component range = (Component)owner.GetType().GetField("range", hidden).GetValue(owner); // 격리된 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 실제 시험 발사점
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object recipe = all[2].GetType().GetProperty("Recipe").GetValue(all[2]); // 잿불 항로 구성
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 피해를 기록할 표적
            target.transform.position = muzzle.position; // 시작 지점의 실제 표적
            Physics.SyncTransforms(); // 변경된 충돌 위치 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 실제 경로 기록 시작
            yield return WaitFrames(20); // 첫 지속 피해 전
            Assert.That(Metric(target, "TotalDamage"), Is.Zero); // 즉시 피해 없음
            yield return WaitFrames(230); // 마지막 유효 틱까지의 시간
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(72)); // 영 점 오 초마다 구 피해와 여덟 번째 틱
            ClickPauseButton(editor, "ResetTest"); // 이전 영역과 피해 초기화
            Vector3 start = muzzle.position; // 새 시전의 실제 출발점
            target.transform.position = start + muzzle.right * 3f; // 왕복하여 겹치는 경로의 표적
            Physics.SyncTransforms(); // 새 표적 위치 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 두 번째 경로 기록
            muzzle.position = start + muzzle.right * 6f; // 첫 경로 선분
            yield return WaitFrames(2); // 전진 경로 기록
            muzzle.position = start; // 같은 경로를 역방향으로 이동
            yield return WaitFrames(2); // 겹치는 선분 기록
            muzzle.position = start + muzzle.right * 20f; // 제한을 넘는 추가 이동
            yield return WaitFrames(2); // 경로 상한 반영
            var trails = (System.Collections.IList)range.GetType().GetField("trails", hidden).GetValue(range); // 시전한 실제 잿불 영역
            object trail = trails[0]; // 하나의 고정 영역
            float distance = (float)trail.GetType().GetField("length").GetValue(trail); // 제한 후 기록한 전체 길이
            Assert.That(distance, Is.EqualTo(12f).Within(0.01f)); // 최대 십이 미터까지만 기록
            yield return WaitFrames(35); // 첫 경로 피해까지 대기
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(9)); // 겹친 선분에서도 한 틱 피해 한 번
        }

        [UnityTest] // 설치와 결계가 공유하는 동시 개수 상한
        public IEnumerator CraftedPlacementsShareThreeActiveLimit() // 세 개 허용과 네 번째 거절
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component owner = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 내부 상태 조회
            Component range = (Component)owner.GetType().GetField("range", hidden).GetValue(owner); // 격리된 연습 발사기
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            range.GetType().GetField("practiceMana", hidden).SetValue(range, 1000f); // 마력 부족과 동시 개수 한도 분리
            var cast = range.GetType().GetMethod("Cast", hidden); // 실제 생성과 마력 소비 경로
            foreach (int index in new[] { 2, 6, 11 }) // 잿불 경로와 덩굴 영역과 정화 결계
            {
                object recipe = all[index].GetType().GetProperty("Recipe").GetValue(all[index]); // 각각 다른 설치 구성
                Assert.That(cast.Invoke(range, new[] { recipe }), Is.True); // 세 번째 설치까지 허용
            }
            object seed = all[8].GetType().GetProperty("Recipe").GetValue(all[8]); // 네 번째로 시도할 씨앗 포대
            float mana = (float)range.GetType().GetField("practiceMana", hidden).GetValue(range); // 실패 직전 남은 마력
            Assert.That(cast.Invoke(range, new[] { seed }), Is.False); // 동시 세 개를 넘는 설치 거절
            Assert.That((float)range.GetType().GetField("practiceMana", hidden).GetValue(range), Is.EqualTo(mana)); // 실패한 설치의 마력 보존
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(3)); // 실패한 설치의 개체 미생성
            range.GetType().GetMethod("ResetCasts", hidden).Invoke(range, null); // 모든 설치체 회수
            Assert.That(cast.Invoke(range, new[] { seed }), Is.True); // 회수 이후 새로운 설치 허용
        }

        [UnityTest] // 지정 좌표가 지형 뒤에 놓이지 않는지 확인
        public IEnumerator CraftedPlacementAimStopsAtTerrain() // 실전 조준선과 장애물 접점
        {
            yield return LoadOuterIslands(); // 실제 카메라와 마법 발사기가 있는 씬
            Transform pilot = FindPilot(); // 실제 전투 비행체
            Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 지정 좌표를 산출하는 발사기
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 조준 참조 조회
            Component camera = (Component)caster.GetType().GetField("aimCamera", hidden).GetValue(caster); // 실전 공격 카메라
            Ray ray = (Ray)camera.GetType().GetMethod("GetAttackRay").Invoke(camera, null); // 화면 중심의 실제 공격선
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); // 지형 충돌을 나타내는 임시 장애물
            obstacle.layer = 8; // 실제 지형 충돌 레이어
            obstacle.transform.position = ray.GetPoint(4f); // 카메라 조준선 위의 가까운 장애물
            obstacle.transform.localScale = Vector3.one * 0.6f; // 조준선에서 확인할 크기
            Physics.SyncTransforms(); // 생성한 지형의 실제 충돌 반영
            Assert.That(Physics.Raycast(ray, out RaycastHit contact, 20f, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore), Is.True); // 실제 충돌 지점 확인
            Assert.That(contact.collider.gameObject, Is.SameAs(obstacle)); // 시험 장애물이 첫 충돌체
            Vector3 point = (Vector3)caster.GetType().GetMethod("AimPoint", hidden).Invoke(caster, new object[] { 20f }); // 실제 지정 좌표 계산
            Assert.That(Vector3.Distance(point, contact.point), Is.LessThan(0.01f)); // 지형 접점에 고정하고 벽 뒤에 생성하지 않음
            Object.Destroy(obstacle); // 시험에서 생성한 장애물만 회수
        }

        [UnityTest] // 그림자 낙인의 직격과 저주 중복 금지
        public IEnumerator ShadowMarkDealsDirectAndIndependentCurseDamage() // 직격 십사와 여섯 틱
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component owner = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 내부 상태 조회
            Component range = (Component)owner.GetType().GetField("range", hidden).GetValue(owner); // 격리된 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 실제 시험 발사점
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object recipe = all[13].GetType().GetProperty("Recipe").GetValue(all[13]); // 그림자 낙인 구성
            Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 피해를 기록할 표적
            target.transform.position = muzzle.position + muzzle.forward * 3f; // 탄환이 닿을 실제 위치
            Physics.SyncTransforms(); // 표적의 실제 충돌 위치 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 실제 그림자 낙인 발사
            yield return WaitFrames(18); // 직격 이후 첫 저주 피해 전
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(14)); // 기준 직격 피해
            Component status = target.GetComponent("FlightCraftedStatus"); // 직격한 표적의 실제 저주 상태
            var apply = status.GetType().GetMethod("TryApply", hidden); // 같은 상태의 재적용 경로
            Assert.That(apply.Invoke(status, new object[] { "curse", "dark", 8f, 0.5f, 6, 1, true }), Is.False); // 만료 전 중복과 지속 시간 갱신 차단
            yield return WaitFrames(205); // 여섯 번째 틱까지의 시간
            Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(62)); // 직격과 여섯 저주 피해의 합
        }

        [UnityTest] // 초당 상한에서 초과 자탄과 순차 발사의 즉시 차단
        public IEnumerator CraftedEmissionLimitSkipsOnlyOverflowWithoutDelaying() // 가능한 자탄만 생성하고 나머지 취소
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component owner = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 내부 상태 조회
            Component range = (Component)owner.GetType().GetField("range", hidden).GetValue(owner); // 격리된 연습 발사기
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object seed = all[8].GetType().GetProperty("Recipe").GetValue(all[8]); // 씨앗 포대 구성
            var cast = range.GetType().GetMethod("Cast", hidden); // 실제 시전 경로
            Assert.That(cast.Invoke(range, new[] { seed }), Is.True); // 모탄 한 개 생성
            yield return WaitFrames(45); // 분열 직전의 초당 한도 채우기
            object budget = range.GetType().GetField("emissions", hidden).GetValue(range); // 같은 발사기의 실제 생성 한도
            var begin = budget.GetType().GetMethod("TryBegin", hidden); // 최초 발사 예약 경로
            float now = (float)range.GetType().GetField("practiceTime", hidden).GetValue(range); // 연습의 실제 시각
            Assert.That(begin.Invoke(budget, new object[] { 24, 0, now, null }), Is.True); // 다른 발사 스물네 개 예약
            Assert.That(begin.Invoke(budget, new object[] { 14, 0, now, null }), Is.True); // 모탄을 합쳐 서른아홉 개
            var placements = (System.Collections.IList)range.GetType().GetField("placements", hidden).GetValue(range); // 실제 지연 설치체 목록
            object placement = placements[0]; // 아직 남은 모탄
            placement.GetType().GetField("splitAt").SetValue(placement, now + 0.001f); // 한도 유효 중에 분열 경계 배치
            yield return WaitFrames(4); // 여유 한 개만 있는 분열 시각
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(2)); // 여유 한 개에 자탄 하나만 생성
            yield return WaitFrames(100); // 다른 발사 기록이 만료된 이후
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(2)); // 초과 자탄은 지연 생성하지 않음
            ClickPauseButton(editor, "ResetTest"); // 이전 발사와 생성 한도 초기화
            object flame = all[1].GetType().GetProperty("Recipe").GetValue(all[1]); // 플레임 스파이럴 구성
            Assert.That(cast.Invoke(range, new[] { flame }), Is.True); // 첫 나선 탄환 발사
            now = (float)range.GetType().GetField("practiceTime", hidden).GetValue(range); // 새 시전의 실제 시각
            Assert.That(begin.Invoke(budget, new object[] { 24, 0, now, null }), Is.True); // 초당 한도의 다른 발사
            Assert.That(begin.Invoke(budget, new object[] { 15, 0, now, null }), Is.True); // 최초 탄환을 포함한 마흔 개
            yield return WaitFrames(110); // 마지막 예정 발사보다 충분히 긴 시간
            int emitted = Metric(range, "EmissionCount"); // 실제 생성한 나선 탄환 수
            Assert.That(emitted, Is.LessThan(12)); // 상한을 넘는 예정 탄환 차단
            yield return WaitFrames(100); // 생성 한도가 다시 비워진 이후
            Assert.That(Metric(range, "EmissionCount"), Is.EqualTo(emitted)); // 이미 차단한 탄환의 지연 생성 금지
        }

        [UnityTest] // 다크 오비트의 적탄 허용 여부와 시전당 환급 상한
        public IEnumerator DarkOrbitAbsorbsOnlyAllowedBulletsAndRestoresMana() // 위성 네 개의 실제 적탄 흡수
        {
            yield return LoadOuterIslands(); // 실제 적탄 풀과 출격 마법이 있는 씬
            Transform pilot = FindPilot(); // 현재 조종사와 발사기
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 시험 중 웨이브 개입 차단
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 사격의 간섭 차단
            Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실제 제작 마법 발사기
            Component skills = pilot.GetComponent("FlightSkillController"); // 실제 실전 마력
            Component pool = GetThreatPool(); // 실제 전투 적탄 풀
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 실제 상태 조회
            pool.GetType().GetMethod("ReturnAll").Invoke(pool, null); // 이전 적탄 제거
            skills.GetType().GetField("mana", hidden).SetValue(skills, 50f); // 최대치보다 낮은 실제 마력
            System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object recipe = all[12].GetType().GetProperty("Recipe").GetValue(all[12]); // 다크 오비트 구성
            System.Type patternType = pool.GetType().Assembly.GetType("ProjectG.FlightThreatPattern"); // 실제 적탄 패턴 종류
            object pattern = System.Enum.Parse(patternType, "Aimed"); // 일반 조준탄의 기본 형태
            var fire = pool.GetType().GetMethod("TryFire", hidden, null, new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(int), patternType, typeof(bool), typeof(bool) }, null); // 상쇄와 흡수의 독립 허용 발사
            Assert.That(fire, Is.Not.Null); // 서로 다른 상호작용을 표현할 발사 경로
            Vector3 contact = pilot.position + pilot.right * 2.2f; // 첫 위성의 초기 공전 위치
            for (int i = 0; i < 11; i++) // 시전당 흡수 한도보다 한 발 많은 허용탄
            {
                Assert.That(fire.Invoke(pool, new object[] { contact, Vector3.forward, 0.01f, 0, pattern, false, true }), Is.True); // 상쇄 불가지만 흡수 가능한 탄
            }
            Assert.That(fire.Invoke(pool, new object[] { contact, Vector3.forward, 0.01f, 0, pattern, true, false }), Is.True); // 상쇄는 가능하지만 흡수는 불가한 탄
            Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { recipe }), Is.True); // 실제 네 위성 소환
            Assert.That(Metric(caster, "EmissionCount"), Is.EqualTo(4)); // 시전 시 생성한 네 위성
            yield return WaitFrames(3); // 실제 공전과 적탄 접촉 처리
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(2)); // 허용 열 발만 흡수하고 나머지 보존
            float mana = (float)skills.GetType().GetField("mana", hidden).GetValue(skills); // 회복된 실제 소수 마력
            Assert.That(mana, Is.InRange(59.9f, 60.5f)); // 흡수 한 발당 일 마력과 기본 자연 회복
            yield return WaitFrames(30); // 같은 시전의 상한 유지 시간
            Assert.That(Metric(pool, "ActiveCount"), Is.EqualTo(2)); // 열한 번째와 흡수 불허탄 보존
            yield return WaitFrames(215); // 네 초 유지가 종료될 때까지 대기
            var orbits = (System.Collections.IList)caster.GetType().GetField("orbits", hidden).GetValue(caster); // 실제 공전 묶음
            Assert.That(orbits.Count, Is.Zero); // 네 위성이 함께 수명 종료
            float cooldown = (float)caster.GetType().GetMethod("CooldownRemaining", hidden).Invoke(caster, new[] { recipe }); // 유지 종료 후 재사용 시간
            Assert.That(cooldown, Is.InRange(13f, 14f)); // 십사 초 재사용 대기 시작
        }

        [UnityTest] // 루미너스 레이의 시간별 첫 대상 피해
        public IEnumerator LuminousRayHitsOnlyFirstTargetForSixTicks() // 삼 초 유지의 여섯 지속 피해
        {
            yield return LoadOuterIslands(); // 제작 연습장이 포함된 실제 출격 씬
            Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 독립 연습 진입 경로
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 정지 입력
            yield return WaitFrames(4); // 정지 메뉴 표시 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 정지 입력 해제
            ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 제작 연습장 열기
            Transform editor = pause.Find("CraftedSpellEditor"); // 실제 제작 화면
            Component owner = editor.GetComponent("FlightCraftedSpellEditor"); // 연습 발사기 소유자
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 내부 상태 조회
            Component range = (Component)owner.GetType().GetField("range", hidden).GetValue(owner); // 격리된 연습 발사기
            Transform muzzle = (Transform)range.GetType().GetField("muzzle", hidden).GetValue(range); // 실제 시험 발사점
            System.Type presets = range.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object recipe = all[10].GetType().GetProperty("Recipe").GetValue(all[10]); // 루미너스 레이 구성
            Component first = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 피해를 기록할 첫 표적
            first.transform.position = muzzle.position + muzzle.forward * 5f; // 광선 위의 가까운 표적
            Component behind = Object.Instantiate(first, first.transform.parent); // 다른 피해 기록을 가진 뒤쪽 표적
            behind.transform.position = muzzle.position + muzzle.forward * 9f; // 관통 금지를 확인할 뒤쪽 표적
            Physics.SyncTransforms(); // 두 표적의 실제 충돌 위치 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 실제 광선 유지 시작
            yield return WaitFrames(20); // 첫 지속 피해 이전
            Assert.That(Metric(first, "TotalDamage"), Is.Zero); // 시작 즉시 피해 없음
            yield return WaitFrames(180); // 최대 삼 초와 마지막 틱 이후
            Assert.That(Metric(first, "TotalDamage"), Is.EqualTo(132)); // 첫 표적에 스물둘씩 여섯 번
            Assert.That(Metric(behind, "TotalDamage"), Is.Zero); // 두 번째 표적은 관통하지 않음
            float cooldown = (float)range.GetType().GetMethod("CooldownRemaining", hidden).Invoke(range, new[] { recipe }); // 유지 종료 후 재사용 시간
            Assert.That(cooldown, Is.InRange(9f, 10f)); // 십 초 대기의 실제 시작
            ClickPauseButton(editor, "ResetTest"); // 광선과 첫 표적의 피해 초기화
            Quaternion original = muzzle.rotation; // 처음 조준 방향 보존
            behind.transform.position = muzzle.position + muzzle.right * 5f; // 조준을 바꿀 두 번째 표적
            Physics.SyncTransforms(); // 변경한 표적 위치 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 새 광선 유지
            yield return WaitFrames(35); // 첫 방향의 첫 피해
            Assert.That(Metric(first, "TotalDamage"), Is.EqualTo(22)); // 원래 조준의 첫 표적
            muzzle.rotation = Quaternion.LookRotation(muzzle.right, muzzle.up); // 실제 광선의 조준 방향 변경
            yield return WaitFrames(35); // 변경된 방향의 다음 피해
            Assert.That(Metric(first, "TotalDamage"), Is.EqualTo(22)); // 이전 방향에 추가 피해 없음
            Assert.That(Metric(behind, "TotalDamage"), Is.EqualTo(22)); // 바뀐 방향의 표적에 다음 피해
            ClickPauseButton(editor, "ResetTest"); // 유지 광선과 첫 표적 초기화
            muzzle.rotation = original; // 최초 조준 방향 복원
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); // 광선을 막을 임시 지형
            obstacle.layer = 8; // 실제 지형 충돌 레이어
            obstacle.transform.position = muzzle.position + muzzle.forward * 3f; // 첫 표적 앞의 지형
            Physics.SyncTransforms(); // 실제 장애물 충돌 반영
            Assert.That(range.GetType().GetMethod("Cast", hidden).Invoke(range, new[] { recipe }), Is.True); // 지형을 향한 광선
            yield return WaitFrames(35); // 지형 뒤 표적의 첫 틱 검사
            Assert.That(Metric(first, "TotalDamage"), Is.Zero); // 장애물 뒤로 피해가 통과하지 않음
            Object.Destroy(obstacle); // 시험에서 생성한 임시 지형 회수
        }

        [UnityTest] // 실제 마우스 유지와 루미너스 레이 자원 소비
        public IEnumerator LuminousRayUsesLiveManaAndEndsOnReleaseOrShortage() // 완전 유지와 입력 해제·마력 부족
        {
            yield return LoadOuterIslands(); // 실제 출격 조작과 전투 카드가 있는 씬
            Transform pilot = FindPilot(); // 현재 실전 조종체
            ((Behaviour)GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter")).enabled = false; // 시험 중 웨이브 개입 차단
            ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 사격의 간섭 차단
            Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실제 제작 마법 발사기
            Component skills = pilot.GetComponent("FlightSkillController"); // 실전 마력 관리
            Component equipment = pilot.GetComponent("FlightBroomEquipment"); // 마력 회복 수치
            Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 실제 좌우 마법 슬롯
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 시험 상태 조회
            System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획서의 기본 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 확정된 구성 목록
            object recipe = all[10].GetType().GetProperty("Recipe").GetValue(all[10]); // 루미너스 레이 구성
            System.Type progressType = caster.GetType().Assembly.GetType("ProjectG.FlightProgress"); // 실제 공용 연구 프로필
            object progress = progressType.GetProperty("Current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null); // 독립 메모리 프로필
            object research = progress.GetType().GetField("state", hidden).GetValue(progress); // 시험용 연구 상태
            var unlockField = research.GetType().GetField("unlocks"); // 기존 연구 목록
            string[] previousUnlocks = (string[])unlockField.GetValue(research); // 다른 시험의 해금 보존
            var unlocks = new System.Collections.Generic.List<string>(previousUnlocks); // 광선 시험에 필요한 연구
            unlocks.AddRange(new[] { "research.Shape.beam", "research.FireMode.held", "research.Effect.dot" }); // 실제 광선 구성의 요소 해금
            unlockField.SetValue(research, unlocks.ToArray()); // 이번 시험의 메모리 연구만 변경
            try // 실제 입력과 유지 자원 검증
            {
                var lanes = (System.Collections.IList)book.GetType().GetField("runningLanes", hidden).GetValue(book); // 이번 출격의 실제 좌우 편성
                lanes[0] = recipe; // 왼쪽 클릭에 유지 광선 배치
                var desired = (System.Collections.IList)book.GetType().GetField("desiredRun", hidden).GetValue(book); // 자동 복구가 참조하는 실제 편성
                desired[0] = recipe; // 동일한 슬롯의 출격 구성도 광선으로 설정
                float[] bonuses = (float[])equipment.GetType().GetField("bonuses", hidden).GetValue(equipment); // 현재 장비의 능력치 합계
                System.Type stat = equipment.GetType().Assembly.GetType("ProjectG.FlightBroomStat"); // 실제 빗자루 능력치 종류
                int recovery = (int)System.Enum.Parse(stat, "ManaRecovery"); // 초당 마력 회복 수치
                bonuses[recovery] = -6f; // 유지 비용만 측정하도록 기본 회복 제거
                skills.GetType().GetField("mana", hidden).SetValue(skills, 100f); // 완전 유지 전 실제 마력
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); // 실제 왼쪽 입력을 누르고 유지
                yield return WaitFrames(190); // 최대 삼 초의 유지와 종료
                Assert.That(Metric(caster, "CastCount"), Is.EqualTo(1)); // 실제 카드 경로의 성공 시전
                float mana = (float)skills.GetType().GetField("mana", hidden).GetValue(skills); // 유지가 끝난 실제 소수 마력
                Assert.That(mana, Is.EqualTo(66f).Within(0.15f)); // 시작 십과 초당 팔의 삼 초 소비
                var beams = (System.Collections.IList)caster.GetType().GetField("beams", hidden).GetValue(caster); // 현재 유지 중인 실제 광선
                Assert.That(beams.Count, Is.Zero); // 최대 유지 시간에 종료
                InputSystem.QueueStateEvent(mouse, new MouseState()); // 기존 클릭 해제
                yield return WaitFrames(2); // 새 클릭과 입력 분리
                caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 새 시전의 대기시간 초기화
                skills.GetType().GetField("mana", hidden).SetValue(skills, 100f); // 해제 시험의 시작 마력
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); // 유지 입력 시작
                yield return WaitFrames(30); // 영 점 오 초 유지
                InputSystem.QueueStateEvent(mouse, new MouseState()); // 실제 버튼 해제
                yield return WaitFrames(3); // 해제 처리 확인
                Assert.That(beams.Count, Is.Zero); // 입력 해제 시 광선 종료
                float cooldown = (float)caster.GetType().GetMethod("CooldownRemaining", hidden).Invoke(caster, new[] { recipe }); // 해제 이후의 실제 대기시간
                Assert.That(cooldown, Is.InRange(9f, 10f)); // 해제 시점부터 십 초 대기
                caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 고갈 시험의 대기시간 초기화
                skills.GetType().GetField("mana", hidden).SetValue(skills, 11f); // 시작 비용 이후 일 마력만 남는 상태
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); // 유지 입력 시작
                yield return WaitFrames(25); // 남은 마력 소진 이후
                Assert.That(beams.Count, Is.Zero); // 마력 부족 시 자동 종료
                mana = (float)skills.GetType().GetField("mana", hidden).GetValue(skills); // 종료 후 실제 마력
                Assert.That(mana, Is.InRange(0f, 0.01f)); // 음수 없는 실제 마력 고갈
                InputSystem.QueueStateEvent(mouse, new MouseState()); // 시험 입력 해제
                yield return WaitFrames(2); // 다음 클릭과 입력 분리
                caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 자연 회복 시험의 대기시간 초기화
                bonuses[recovery] = 0f; // 기본 초당 육 마력 회복 복원
                skills.GetType().GetField("mana", hidden).SetValue(skills, 100f); // 자연 회복 시험의 시작 마력
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); // 실제 유지 입력 시작
                yield return WaitFrames(190); // 삼 초 유지와 짧은 종료 후 회복
                mana = (float)skills.GetType().GetField("mana", hidden).GetValue(skills); // 자연 회복이 반영된 실제 마력
                Assert.That(mana, Is.InRange(84f, 85.5f)); // 유지 중에도 기본 회복이 계속 적용
                InputSystem.QueueStateEvent(mouse, new MouseState()); // 마지막 시험 입력 해제
            }
            finally // 이번 시험에 임시 적용한 연구만 복원
            {
                unlockField.SetValue(research, previousUnlocks); // 이전 메모리 연구 상태 복구
            }
        }

        [UnityTest] // 실제 웨이브 처치와 연구 과제의 정상 정산 연결
        public IEnumerator StoryWaveKillRecordsResearchOnlyAtSettlement() // 실제 일반 적의 열 번째 처치
        {
            System.Type progressType = System.Type.GetType("ProjectG.FlightProgress, Assembly-CSharp", true); // 씬이 공유하는 실제 프로필
            const System.Reflection.BindingFlags hiddenStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic; // 격리된 자동 시험의 프로필 초기화
            progressType.GetMethod("Reset", hiddenStatic).Invoke(null, null); // 다른 자동 시험의 메모리 프로필 분리
            try // 실제 웨이브와 정상 실패 정산
            {
                yield return LoadOuterIslands(); // 본편 웨이브의 실제 출격 씬
                Component encounter = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 지정 웨이브와 정산 경로
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 출격과 웨이브 조회
                encounter.GetType().GetProperty("ElapsedSeconds").GetSetMethod(true).Invoke(encounter, new object[] { 100f }); // 첫 웨이브 도달 시각
                yield return WaitFrames(3); // 실제 출격 ID와 첫 웨이브 시작
                Assert.That(encounter.GetType().GetField("sortieId", hidden).GetValue(encounter), Is.Not.Null); // 정상 본편 참여 기록
                var waves = (System.Collections.IList)encounter.GetType().GetField("stageWaves", hidden).GetValue(encounter); // 씬에 지정된 본편 웨이브
                object firstWave = waves[0]; // 첫 번째 실제 일반 적 웨이브
                var routes = (System.Collections.IList)firstWave.GetType().GetField("enemies").GetValue(firstWave); // 해당 웨이브의 지정 적 경로
                Component route = (Component)routes[0]; // 처치 이벤트가 연결된 첫 경로
                Component enemy = route.GetComponent("FlightEnemy"); // 실제 마물 체력과 처치 이벤트
                Assert.That(enemy.gameObject.activeInHierarchy, Is.True); // 실제로 출현한 마물
                encounter.GetType().GetField("researchNormalKills", hidden).SetValue(encounter, 9); // 같은 출격에서 이전에 처치한 아홉 마리
                enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 100000 }); // 열 번째 일반 적의 실제 처치
                Assert.That(encounter.GetType().GetField("researchNormalKills", hidden).GetValue(encounter), Is.EqualTo(10)); // 웨이브 이벤트에서 한 번 집계
                object store = progressType.GetProperty("Current", hiddenStatic).GetValue(null); // 현재 본편의 실제 영구 프로필
                Component weapon = FindPilot().GetComponent("FlightWeapon"); // 현재 출격의 실제 조종사
                object actor = weapon.GetType().GetProperty("ActivePilot").GetValue(weapon); // 정산할 조종사 ID
                var task = store.GetType().GetMethod("HasResearchTask", hidden); // 조종사별 연구 과제 확인
                Assert.That(task.Invoke(store, new[] { actor, (object)1 }), Is.False); // 전투 중에는 영구 과제 미확정
                Assert.That(encounter.GetType().GetMethod("Conclude", hidden).Invoke(encounter, new object[] { false }), Is.Not.Null); // 정상 실패 정산
                Assert.That(task.Invoke(store, new[] { actor, (object)1 }), Is.True); // 정산과 함께 해당 조종사의 과제 기록
            }
            finally // 이 자동 시험의 메모리 프로필만 정리
            {
                progressType.GetMethod("Reset", hiddenStatic).Invoke(null, null); // 다른 시험에 임시 연구 상태를 넘기지 않음
            }
        }

        [UnityTest] // 본편 지정 일반·정예 처치의 보상 정산
        public IEnumerator StoryKillsQueueResearchRewardsOnceAtSettlement() // 실제 처치 이벤트와 무료 선택권
        {
            System.Type progressType = System.Type.GetType("ProjectG.FlightProgress, Assembly-CSharp", true); // 씬이 공유하는 실제 프로필
            const System.Reflection.BindingFlags hiddenStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic; // 격리 자동 시험의 프로필 초기화
            progressType.GetMethod("Reset", hiddenStatic).Invoke(null, null); // 다른 자동 시험의 메모리 프로필 분리
            try // 실제 웨이브 처치와 정상 실패 정산
            {
                yield return LoadOuterIslands(); // 본편 웨이브의 실제 출격 씬
                Component encounter = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 지정 웨이브와 정산 경로
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 출격과 웨이브 조회
                encounter.GetType().GetProperty("ElapsedSeconds").GetSetMethod(true).Invoke(encounter, new object[] { 100f }); // 첫 웨이브 도달 시각
                yield return WaitFrames(3); // 실제 출격 ID와 첫 웨이브 시작
                var waves = (System.Collections.IList)encounter.GetType().GetField("stageWaves", hidden).GetValue(encounter); // 씬에 지정된 본편 웨이브
                var firstRoutes = (System.Collections.IList)waves[0].GetType().GetField("enemies").GetValue(waves[0]); // 첫 웨이브의 지정 일반 적
                Component first = ((Component)firstRoutes[0]).GetComponent("FlightEnemy"); // 실제 마물의 체력과 처치 이벤트
                encounter.GetType().GetField("researchNormalKills", hidden).SetValue(encounter, 29); // 같은 출격의 이전 일반 적 스물아홉 처치
                first.GetType().GetMethod("ReceiveHit").Invoke(first, new object[] { 100000 }); // 서른 번째 일반 적의 실제 처치
                Assert.That(encounter.GetType().GetField("researchNormalKills", hidden).GetValue(encounter), Is.EqualTo(30)); // 일반 보상 기준의 실제 집계
                yield return WaitFrames(4); // 다음 지정 웨이브의 실제 출현
                var nextRoutes = (System.Collections.IList)waves[1].GetType().GetField("enemies").GetValue(waves[1]); // 다음 웨이브의 지정 마물
                Component elite = ((Component)nextRoutes[0]).GetComponent("FlightEnemy"); // 정예로 지정할 실제 마물
                Assert.That(elite.gameObject.activeInHierarchy, Is.True); // 다음 웨이브의 실제 출현
                elite.GetType().GetField("elite", hidden).SetValue(elite, true); // 시험 대상의 정예 구분
                elite.GetType().GetMethod("ReceiveHit").Invoke(elite, new object[] { 100000 }); // 실제 정예 처치 이벤트
                Assert.That(encounter.GetType().GetField("researchEliteKills", hidden).GetValue(encounter), Is.EqualTo(1)); // 정예 보상 조건 한 번 집계
                Assert.That(encounter.GetType().GetField("researchNormalKills", hidden).GetValue(encounter), Is.EqualTo(30)); // 정예를 일반 적 수에 중복하지 않음
                object store = progressType.GetProperty("Current", hiddenStatic).GetValue(null); // 현재 본편의 실제 영구 프로필
                var pending = store.GetType().GetMethod("PendingResearchRewards", hidden); // 단계별 보류 선택권
                Assert.That(((string[])pending.Invoke(store, new object[] { 1 })).Length, Is.Zero); // 정상 정산 전 보상 미발급
                Assert.That(encounter.GetType().GetMethod("Conclude", hidden).Invoke(encounter, new object[] { false }), Is.Not.Null); // 실제 정상 실패 정산
                Assert.That(((string[])pending.Invoke(store, new object[] { 1 })).Length, Is.EqualTo(1)); // 일반 처치 보상 한 건
                Assert.That(((string[])pending.Invoke(store, new object[] { 2 })).Length, Is.EqualTo(1)); // 정예 처치 보상 한 건
                encounter.GetType().GetMethod("Conclude", hidden).Invoke(encounter, new object[] { false }); // 같은 출격의 정산 재요청
                Assert.That(((string[])pending.Invoke(store, new object[] { 1 })).Length, Is.EqualTo(1)); // 같은 출격의 중복 지급 방지
            }
            finally // 이 자동 시험의 메모리 프로필만 정리
            {
                progressType.GetMethod("Reset", hiddenStatic).Invoke(null, null); // 다른 시험에 임시 보상 상태를 넘기지 않음
            }
        }

        [UnityTest] // 잠긴 기존 저장과 실제 전투 편성의 분리
        public IEnumerator LockedSavedSpellIsPreservedAndRestoredAfterResearch() // 저장 ID 보존과 해금 전 전투 제외
        {
            string key = "ProjectG.Crafted.Locked." + System.Guid.NewGuid().ToString("N"); // 사용자 마법 저장과 분리한 시험 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 이전 격리 환경 보존
            System.Type progressType = System.Type.GetType("ProjectG.FlightProgress, Assembly-CSharp", true); // 씬이 공유하는 실제 프로필
            const System.Reflection.BindingFlags hiddenStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic; // 격리 자동 시험의 프로필 초기화
            progressType.GetMethod("Reset", hiddenStatic).Invoke(null, null); // 이전 시험의 메모리 연구 상태 분리
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 실제 사용자 편성 제외
            try // 기존 저장을 열고 연구 전후 편성 확인
            {
                System.Type presets = System.Type.GetType("ProjectG.CraftedSpellPresets, Assembly-CSharp", true); // 기획서의 고정 마법
                var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 같은 구성의 프리셋
                object holy = all[9].GetType().GetProperty("Recipe").GetValue(all[9]); // 연구가 필요한 홀리 샤워
                string id = (string)holy.GetType().GetProperty("Id").GetValue(holy); // 기존 저장의 실제 구성 ID
                PlayerPrefs.SetString(key, "{\"version\":2,\"recipe\":\"" + id + "\",\"left\":\"" + id + "\",\"right\":\"\"}"); // 연구 전 이미 저장한 이전 편성
                PlayerPrefs.Save(); // 실제 불러오기 경로에 시험 저장 반영
                yield return LoadOuterIslands(); // 실제 전투 카드와 출격 편성
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 사용자 조합을 복원한 마법 도감
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 편성 상태 조회
                var slot = book.GetType().GetMethod("Slot", hidden); // 보존된 이전 편성 확인
                var active = book.GetType().GetMethod("ActiveSlot", hidden); // 실제 출격에 배정한 마법
                Assert.That(slot.Invoke(book, new object[] { 0 }), Is.Not.Null); // 잠긴 이전 슬롯의 저장 ID 보존
                Assert.That(active.Invoke(book, new object[] { 0 }), Is.Null); // 미해금 마법의 첫 전투 칸 제외
                Assert.That(active.Invoke(book, new object[] { 1 }), Is.Null); // 무작위 배정에도 잠긴 카드 없음
                Assert.That(book.GetType().GetMethod("Equip", hidden).Invoke(book, new[] { (object)1, holy }), Is.False); // 연구 전 새 편성 거절
                object store = progressType.GetProperty("Current", hiddenStatic).GetValue(null); // 현재 계정의 실제 연구 상태
                object state = store.GetType().GetField("state", hidden).GetValue(store); // 시험에서만 변경할 공용 해금
                var unlockField = state.GetType().GetField("unlocks"); // 기존 공용 해금 ID 배열
                var unlocks = new System.Collections.Generic.List<string>((string[])unlockField.GetValue(state)); // 원래 기본 해금 보존
                unlocks.Add("research.Shape.arrow"); // 홀리 샤워의 화살탄 연구 완료
                unlocks.Add("research.Effect.pierce"); // 홀리 샤워의 관통 연구 완료
                unlockField.SetValue(state, unlocks.ToArray()); // 두 선행 요소의 공용 해금
                book.GetType().GetMethod("ResetRun", hidden).Invoke(book, null); // 새 출격의 저장 편성 재평가
                object left = active.Invoke(book, new object[] { 0 }); // 무작위 배정의 첫 칸
                object right = active.Invoke(book, new object[] { 1 }); // 무작위 배정의 둘째 칸
                Assert.That(left != null || right != null, Is.True); // 연구 후 기존 저장 마법 자동 복원
                Assert.That((string)(left ?? right).GetType().GetProperty("Id").GetValue(left ?? right), Is.EqualTo(id)); // 기존 구성 ID 그대로 사용
                Assert.That(PlayerPrefs.GetString(key), Does.Contain(id)); // 사용자 저장 자체는 해금 전후 보존
            }
            finally // 이 검증에서 만든 설정만 정리
            {
                PlayerPrefs.DeleteKey(key); // 시험용 마법 저장 제거
                PlayerPrefs.Save(); // 시험 키 정리 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 이전 격리 환경 복원
                progressType.GetMethod("Reset", hiddenStatic).Invoke(null, null); // 이 시험의 메모리 연구 상태 정리
            }
        }

        [UnityTest] // 실제 연구 화면의 선택·확인·취소와 공용 저장
        public IEnumerator ResearchScreenConfirmsPurchaseAndKeepsCancellationFree() // 제작 화면에서 연구까지의 실제 버튼
        {
            System.Type progressType = System.Type.GetType("ProjectG.FlightProgress, Assembly-CSharp", true); // 씬이 공유하는 실제 프로필
            const System.Reflection.BindingFlags hiddenStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic; // 격리 자동 시험의 프로필 초기화
            progressType.GetMethod("Reset", hiddenStatic).Invoke(null, null); // 다른 시험의 메모리 연구 상태 분리
            try // 실제 준비 화면의 연구와 취소
            {
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 출격 전 제작 화면이 있는 준비 씬
                yield return WaitFrames(4); // 실제 버튼과 공용 프로필 초기화
                object store = progressType.GetProperty("Current", hiddenStatic).GetValue(null); // 현재 계정의 실제 저장
                System.Type storeType = store.GetType(); // 실제 정산과 연구 타입
                System.Type pilotType = storeType.GetMethod("Begin").GetParameters()[0].ParameterType; // 실제 조종사 ID
                object aria = System.Enum.Parse(pilotType, "Aria"); // 연구 자격을 채울 조종사
                string run = (string)storeType.GetMethod("Begin").Invoke(store, new[] { aria }); // 정상 출격의 참여 기록
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 정상 정산과 해금 조회
                storeType.GetMethod("CompleteWithResearch", hidden).Invoke(store, new object[] { run, true, false, 10, false }); // 일반 자격과 실제 재화 확보
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 출격 준비 UI
                ClickPauseButton(briefing, "Ready/EditMagic"); // 제작과 편성 화면 열기
                Transform editor = briefing.Find("CraftedSpellEditor"); // 실제 마법 편집 화면
                ClickPauseButton(editor, "OpenResearch"); // 연구 화면 진입 버튼
                Transform panel = editor.Find("ResearchPanel"); // 같은 계정의 실제 연구 패널
                Assert.That(panel.gameObject.activeSelf, Is.True); // 연구 화면 표시
                ClickPauseButton(panel, "TabShape"); // 탄환 형태 분류 선택
                ClickPauseButton(panel, "Node1"); // 초기 구체탄 다음 소형탄 선택
                Assert.That(HudText(panel.Find("Detail")), Does.Contain("소형탄")); // 실제 선택한 연구 요소와 조건 표시
                int gold = (int)storeType.GetProperty("Gold").GetValue(store); // 구매 전 실제 공용 골드
                int crystals = (int)storeType.GetProperty("SharedCrystals").GetValue(store); // 구매 전 실제 공용 결정
                ClickPauseButton(panel, "Research"); // 연구 비용 확인 열기
                Transform confirm = panel.Find("Confirm"); // 최종 확인 패널
                Assert.That(confirm.gameObject.activeSelf, Is.True); // 즉시 차감하지 않고 확인 표시
                ClickPauseButton(confirm, "Cancel"); // 비용 확인 취소
                Assert.That(storeType.GetProperty("Gold").GetValue(store), Is.EqualTo(gold)); // 취소 시 골드 보존
                Assert.That(storeType.GetProperty("SharedCrystals").GetValue(store), Is.EqualTo(crystals)); // 취소 시 결정 보존
                ClickPauseButton(panel, "Research"); // 같은 연구의 확인 재진입
                ClickPauseButton(confirm, "Accept"); // 실제 확인 후 구매
                System.Type group = System.Type.GetType("ProjectG.CraftedSpellGroup, Assembly-CSharp", true); // 제작 분류 ID
                object shape = System.Enum.Parse(group, "Shape"); // 소형탄의 탄환 형태 분류
                Assert.That(storeType.GetMethod("IsResearchUnlocked", hidden).Invoke(store, new[] { shape, (object)"small" }), Is.True); // 실제 공용 연구 해금
                Assert.That(storeType.GetProperty("Gold").GetValue(store), Is.EqualTo(gold - 300)); // 확인 후에만 골드 차감
                Assert.That(storeType.GetProperty("SharedCrystals").GetValue(store), Is.EqualTo(crystals - 3)); // 확인 후에만 결정 차감
                for (int i = 0; i < 2; i++) // 상점 비용을 확보할 추가 정상 출격
                {
                    string extra = (string)storeType.GetMethod("Begin").Invoke(store, new[] { aria }); // 별도의 실제 출격 ID
                    storeType.GetMethod("CompleteWithResearch", hidden).Invoke(store, new object[] { extra, true, false, 0, false }); // 실제 정상 클리어 재화
                }
                ClickPauseButton(panel, "Node2"); // 소형탄 다음 대형탄 선택
                gold = (int)storeType.GetProperty("Gold").GetValue(store); // 상점 확인 전 실제 골드
                ClickPauseButton(panel, "Shop"); // 일반 요소의 고정 상점 구매
                ClickPauseButton(confirm, "Accept"); // 실제 상점 비용 확인
                Assert.That(storeType.GetMethod("IsResearchUnlocked", hidden).Invoke(store, new[] { shape, (object)"large" }), Is.True); // 같은 공용 ID의 상점 해금
                Assert.That(storeType.GetProperty("Gold").GetValue(store), Is.EqualTo(gold - 900)); // 일반 상점의 골드 구백 소비
                string rewardRun = (string)storeType.GetMethod("Begin").Invoke(store, new[] { aria }); // 무료 보상을 받을 실제 출격
                storeType.GetMethod("CompleteWithResearchRewards", hidden).Invoke(store, new object[] { rewardRun, false, false, 30, 0, false, "test-stage" }); // 일반 격파 조건의 보류 선택권
                ClickPauseButton(panel, "TabFormation"); // 발사 배열의 연구 분류
                ClickPauseButton(panel, "Node1"); // 단발 다음 병렬 배열 선택
                gold = (int)storeType.GetProperty("Gold").GetValue(store); // 무료 선택 전 실제 골드
                crystals = (int)storeType.GetProperty("SharedCrystals").GetValue(store); // 무료 선택 전 실제 결정
                ClickPauseButton(panel, "Reward"); // 보류 선택권으로 무료 선택
                ClickPauseButton(confirm, "Accept"); // 실제 무료 해금 확인
                object formation = System.Enum.Parse(group, "Formation"); // 병렬 배열의 제작 분류
                Assert.That(storeType.GetMethod("IsResearchUnlocked", hidden).Invoke(store, new[] { formation, (object)"parallel" }), Is.True); // 보상으로 같은 공용 ID 해금
                Assert.That(storeType.GetProperty("Gold").GetValue(store), Is.EqualTo(gold)); // 무료 선택의 골드 보존
                Assert.That(storeType.GetProperty("SharedCrystals").GetValue(store), Is.EqualTo(crystals)); // 무료 선택의 결정 보존
                ClickPauseButton(panel, "TabEffect"); // 특수 효과 분류
                ClickPauseButton(panel, "Node5"); // 침탄이 선행인 관통 효과
                ClickPauseButton(panel, "Parent"); // 다른 분류의 선행 노드로 이동
                Assert.That(HudText(panel.Find("Detail")), Does.Contain("침탄")); // 탄환 형태의 실제 선행 요소 표시
                ClickPauseButton(panel, "Close"); // 제작 화면으로 돌아가기
                Assert.That(panel.gameObject.activeSelf, Is.False); // 연구 화면 닫힘
            }
            finally // 이 자동 시험의 메모리 프로필만 정리
            {
                progressType.GetMethod("Reset", hiddenStatic).Invoke(null, null); // 다른 시험에 임시 연구 상태를 넘기지 않음
            }
        }

        [UnityTest] // 제작 마법 시험 발사부터 전투까지 같은 조합 확인
        public IEnumerator CraftedSpellEditorSavesAndCastsThroughTheCombatLane() // 편집기와 실전 카드의 공통 발사
        {
            string key = "ProjectG.Crafted.Test." + System.Guid.NewGuid().ToString("N"); // 독립적인 제작 마법 저장
            string prior = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 격리 환경 보관
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 값이 다른 사용자의 저장 제외
            try // 종료 때 시험 저장 반환
            {
                yield return LoadOuterIslands(); // 실전 항로의 실제 HUD와 마법 슬롯
                Transform pause = LocateCombatHud().transform.Find("PauseMenu"); // 실전 정지 메뉴
                Component initialBook = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 이번 출격의 고정 편성
                var initialActive = initialBook.GetType().GetMethod("ActiveSlot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 실제 사용 슬롯 조회
                object originalLeft = initialActive.Invoke(initialBook, new object[] { 0 }); // 출격 때 배정된 왼쪽
                object originalRight = initialActive.Invoke(initialBook, new object[] { 1 }); // 출격 때 배정된 오른쪽
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 ESC 입력
                yield return WaitFrames(4); // 정지 메뉴 전환
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 중복된 정지 입력 방지
                ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 마법 편집기 입장
                Transform editor = pause.Find("CraftedSpellEditor"); // 씬에 저장한 편집 화면
                Assert.That(editor.gameObject.activeInHierarchy, Is.True); // 실제 편집 패널
                ClickPauseButton(editor, "TestCast"); // 기본 파이어 볼트의 독립 연습 사격
                Component target = editor.GetComponentInChildren(System.Type.GetType("ProjectG.FlightCraftedTarget, Assembly-CSharp")); // 연습용 표적만 확인
                for (int frame = 0; frame < 180 && Metric(target, "TotalDamage") == 0; frame++) // 실제 비행과 충돌까지 대기
                {
                    yield return null; // 게임 정지시간에 영향 없는 연습 진행
                }
                Assert.That(Metric(target, "TotalDamage"), Is.EqualTo(60)); // 직격 35와 폭발 25를 한 번씩 적용
                ClickPauseButton(editor, "Save"); // 같은 제작 ID 보존
                ClickPauseButton(editor, "EquipLeft"); // 실제 좌클릭 슬롯에 편성
                Assert.That(PlayerPrefs.HasKey(key), Is.True); // 저장·편성 데이터 존재
                ClickPauseButton(editor, "Back"); // 기존 조작 안내로 복귀
                ClickPauseButton(pause, "MenuButtons/Continue"); // 실전 시간 재개
                yield return WaitFrames(4); // 조립 클릭과 실제 발사 입력 분리
                Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 실전과 연습의 같은 발사 컴포넌트
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); // 실전 좌클릭 사용
                yield return WaitFrames(4); // 실제 마법 카드 경로로 발사
                Assert.That(Metric(caster, "CastCount"), Is.EqualTo(1)); // 같은 조합의 첫 실전 시전
                var magic = FindPilot().GetComponent("FlightSkillController"); // 실제 장착 용량을 가진 마력
                Assert.That(Metric(magic, "Mana"), Is.InRange(Metric(magic, "MaxMana") - 15, Metric(magic, "MaxMana") - 14)); // 실제 보석 비용 15 차감
                InputSystem.QueueStateEvent(mouse, new MouseState()); // 마법 사용 클릭 해제
                Component encounter = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 실제 웨이브 시작 조건
                var preparing = encounter.GetType().GetProperty("IsPreparing"); // 배포 씬의 실제 출격 준비 판정
                for (int frame = 0; frame < 600 && (bool)preparing.GetValue(encounter); frame++) // 첫 웨이브 직전까지만 진행
                {
                    yield return null; // 전투 시간 진행
                }
                Assert.That(preparing.GetValue(encounter), Is.False); // 실제 전투가 시작된 상태
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 전투 중에 다시 정지
                yield return WaitFrames(4); // 메뉴의 실제 입력 확인
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 조합 편집 클릭으로 이동
                string stored = PlayerPrefs.GetString(key); // 전투 중 편집 시도 전의 저장 상태
                Assert.That(pause.Find("Controls/CraftedSpellEditor").GetComponent<Button>().interactable, Is.False); // 실제 전투 중 제작 버튼 비활성화
                ClickPauseButton(pause, "Controls/CraftedSpellEditor"); // 강제로 클릭 이벤트를 호출한 편집 시도
                Assert.That(editor.gameObject.activeInHierarchy, Is.False); // 전투 중 제작 화면 진입 차단
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo(stored)); // 전투 중 영구 저장 변경 없음
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 저장과 현재 편성을 분리하는 도감
                var activeLane = book.GetType().GetMethod("ActiveSlot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 현재 출격에서 쓰는 마법
                Assert.That(activeLane.Invoke(book, new object[] { 0 }), Is.SameAs(originalLeft)); // 출격 중 왼쪽 편성 유지
                Assert.That(activeLane.Invoke(book, new object[] { 1 }), Is.SameAs(originalRight)); // 출격 중 오른쪽 편성 유지
                ClickPauseButton(pause, "MenuButtons/Restart"); // 다음 출격 시작
                object saved = book.GetType().GetProperty("Saved").GetValue(book); // 편집기에서 저장한 파이어 볼트
                string savedId = (string)saved.GetType().GetProperty("Id").GetValue(saved); // 저장한 실제 구성 ID
                object newLeft = activeLane.Invoke(book, new object[] { 0 }); // 새 출격의 왼쪽 마법
                object newRight = activeLane.Invoke(book, new object[] { 1 }); // 새 출격의 오른쪽 마법
                string newLeftId = (string)newLeft.GetType().GetProperty("Id").GetValue(newLeft); // 새 출격의 첫 구성
                string newRightId = (string)newRight.GetType().GetProperty("Id").GetValue(newRight); // 새 출격의 둘째 구성
                Assert.That(new[] { newLeftId, newRightId }, Does.Contain(savedId)); // 전투 전에 저장한 마법의 실제 편성
            }
            finally // 격리된 시험 데이터만 정리
            {
                PlayerPrefs.DeleteKey(key); // 임시 마법 구성 삭제
                PlayerPrefs.Save(); // 시험 종료 기록
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", prior); // 다른 시험과 분리 복구
            }
        }

        [UnityTest] // 마을 진입·낭떠러지·출격소
        public IEnumerator VillageWalksFromLandingPadThroughSortieEntrance() // 실제 시작 버튼과 이동 및 출격 경로
        {
            Assert.That(SceneUtility.GetBuildIndexByScenePath(VillagePath), Is.GreaterThan(0)); // 배포에 편집된 마을 씬 포함
            yield return SceneManager.LoadSceneAsync(0); // Windows와 같은 첫 타이틀
            yield return WaitFrames(3); // 첫 UI의 실제 클릭 연결 대기
            ClickPauseButton(GameObject.Find("MainMenu").transform, "Home/NewGame"); // 실제 일반 시작 흐름
            yield return WaitForActiveScene(VillagePath); // 시작 버튼에서 착륙장 진입
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(VillagePath)); // 출격 준비 전에 실제 마을
            Transform walker = GameObject.Find("VillageWalker").transform; // 하이러키의 실제 도보 조종사
            Vector3 landing = walker.position; // 처음 실제 도착점
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 착륙장에서는 출격소 조작이 동작하지 않음
            yield return WaitFrames(8); // 전환이 실행되는지 확인
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(VillagePath)); // 출격소 밖의 입력 무시
            SetKeys(Key.A); // 마을 연결 다리 밖의 왼쪽 낭떠러지
            yield return WaitFrames(160); // 실제 섬 끝까지 이동
            Vector3 edge = walker.position; // 지면이 끝나는 위치
            yield return WaitFrames(60); // 가장자리를 계속 밀어도 이동 제한
            Assert.That(landing.x - edge.x, Is.GreaterThan(4f)); // 캐릭터가 실제로 걸어 이동
            Assert.That(Vector3.Distance(edge, walker.position), Is.LessThan(0.08f)); // 공중으로 걸어나가지 않음
            Assert.That(walker.position.y, Is.EqualTo(landing.y).Within(0.1f)); // 다리·섬의 높이 유지
            SetKeys(Key.D); // 중심 보행로로 돌아오기
            for (int frame = 0; frame < 200 && walker.position.x < -0.10f; frame++) // 다리의 중앙에 맞춤
            {
                yield return null; // 현재 마을 위치 확인
            }
            SetKeys(); // 옆 이동 해제
            yield return VillageBoard(); // 북쪽 다리와 출격소를 실제 걸어서 통과
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 마을에서 열린 실제 캐릭터 선택
            ClickPauseButton(briefing, "Ready/Back"); // 선택을 확정하지 않고 마을로 돌아오기
            yield return WaitForActiveScene(VillagePath); // 출격소 근처의 마을로 복귀
            walker = GameObject.Find("VillageWalker").transform; // 복귀한 새 마을 조종사
            Assert.That(walker.position.z, Is.GreaterThan(25f)); // 착륙장까지 되돌아갈 필요 없는 복귀
        }

        private IEnumerator VillageBoard() // 마을 북쪽 길에서 캐릭터 준비 진입
        {
            yield return WaitForActiveScene(VillagePath); // 시작 지점의 도보 조종사
            Transform walker = GameObject.Find("VillageWalker").transform; // 씬의 제어 대상
            Transform station = GameObject.Find("SortieEntrance").transform; // 월드 위치의 실제 출격소
            SetKeys(Key.W); // 광장을 지나 북쪽 실제 다리 건너기
            for (int frame = 0; frame < 430 && walker.position.z < station.position.z - 1.5f; frame++) // 이동을 제한하는 물리 경계의 실제 순회
            {
                yield return null; // 무한 대기 없이 북쪽 진행 확인
            }
            Assert.That(walker.position.z, Is.GreaterThan(station.position.z - 2.0f)); // 출격소 접근 및 다리 연결
            SetKeys(); // E 입력 전 이동 해제
            yield return null; // 접근 안내 UI의 활성화
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 출격소의 실제 상호작용을 다음 게임 프레임에 전달
            yield return WaitForActiveScene("Assets/Scenes/FlightSortieBriefing.unity"); // 다음 준비 씬의 실제 진입
            SetKeys(); // 다음 출격 버튼과 조작 분리
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("FlightSortieBriefing")); // 준비 씬을 실제로 여는 동선
        }

        [UnityTest] // 장소별 배경 곡과 실제 편집 가능 음향 소스
        public IEnumerator SceneAudioOwnsOneMusicSourceAndSeparatesPauseEffects() // 기존 마스터와 소리 재생 상태 확인
        {
            string[] scenes = { "FlightMainMenu", "FlightIslandVillage", "FlightSortieBriefing", "OuterIslandsStage", "FlightPrototype", "FlightSortieResult" }; // 실제 배포와 훈련의 오디오 씬
            string[] music = { "TitleAirship", "VillageWind", "VillageWind", "OuterIslands", "OuterIslands", null }; // 위치별 기본곡 또는 정산 대기
            for (int index = 0; index < scenes.Length; index++) // 각 화면을 실제 비동기로 열기
            {
                yield return SceneManager.LoadSceneAsync(scenes[index]); // Unity에서 실제 화면 전환
                yield return WaitFrames(4); // 오디오 참조와 시작 곡 설정
                Transform root = GameObject.Find("FlightAudio").transform; // 하이러키에서 편집 가능한 오디오 묶음
                var sources = root.GetComponentsInChildren<AudioSource>(); // 실제 세 가지 소리 재생 소스
                Assert.That(sources.Length, Is.EqualTo(3), scenes[index]); // 배경과 두 효과 소스
                Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length, Is.EqualTo(3), scenes[index]); // 이전 씬에서 중복으로 남는 오디오 없음
                Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1), scenes[index]); // 현재 카메라의 단일 청취자
                var theme = root.Find("Music").GetComponent<AudioSource>(); // 현재 씬의 실제 음악
                Assert.That(theme.clip != null ? theme.clip.name : null, Is.EqualTo(music[index]), scenes[index]); // 메뉴·마을·실전·결과 구분
                Assert.That(theme.loop, Is.EqualTo(music[index] != null), scenes[index]); // 결과를 기다리는 씬엔 반복 음악 없음
                Assert.That(root.Find("CombatSfx").GetComponent<AudioSource>().ignoreListenerPause, Is.False); // 전투 소리는 실제 Esc 정지에 연동
                Assert.That(root.Find("InterfaceSfx").GetComponent<AudioSource>().ignoreListenerPause, Is.True); // 메뉴 소리는 정지된 상태에서도 재생
            }
        }

        [UnityTest] // 앱 시작 메뉴·기존 출격 확인·설정 보존
        public IEnumerator MainMenuConfirmsNewGameAndSavesDisplayAndSound() // 기본 씬의 실제 저장과 출격 경로
        {
            string root = "ProjectG.Menu.Test." + System.Guid.NewGuid().ToString("N"); // 일반 설정과 분리한 검증 저장
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_MENU_TEST_KEY"); // 이전 실행 환경
            System.Environment.SetEnvironmentVariable("PROJECTG_MENU_TEST_KEY", root); // 메뉴와 화면의 독립 설정 키
            try // 확인 후 독립 저장만 정리
            {
                const string titlePath = "Assets/Scenes/FlightMainMenu.unity"; // 실제 배포 시작 씬
                Assert.That(SceneUtility.GetBuildIndexByScenePath(titlePath), Is.Zero); // 패키지 첫 화면 확인
                yield return SceneManager.LoadSceneAsync(0); // Windows 부팅과 같은 실제 흐름
                yield return WaitFrames(3); // 캔버스 버튼과 현재 저장 상태
                Transform menu = GameObject.Find("MainMenu").transform; // 메뉴의 편집 가능한 부모
                Button continueButton = menu.Find("Home/Continue").GetComponent<Button>(); // 실제 이어서하기 버튼
                Assert.That(continueButton.interactable, Is.False); // 저장 없는 최초 실행
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S)); // 지원하는 화살표·WASD 메뉴 이동
                yield return WaitFrames(3); // 비활성 이어서하기를 건너뜀
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 설정 선택 실행
                yield return WaitFrames(3); // 선택한 실제 설정 패널 표시
                Transform settings = menu.Find("Settings"); // 메인 메뉴 환경 설정
                Assert.That(settings.gameObject.activeSelf, Is.True); // 키보드 진입
                ClickPauseButton(settings, "DisplayMode"); // 창 모드 또는 전체 화면 변경
                var volume = settings.Find("Volume").GetComponent<Slider>(); // 실제 마스터 음량
                volume.value = 0.65f; // 사용자 음량 입력
                ClickPauseButton(settings, "Apply"); // 화면과 음량 저장
                Assert.That(PlayerPrefs.GetFloat(root + ".Volume"), Is.EqualTo(0.65f).Within(0.001f)); // 저장한 음량
                Assert.That(PlayerPrefs.HasKey(root + ".Screen"), Is.True); // 화면 설정 저장
                ClickPauseButton(settings, "Back"); // 실제 메인 화면 유지
                ClickPauseButton(menu, "Home/NewGame"); // 처음에는 바로 새 출격
                yield return VillageBoard(); // 마을에서 북쪽 출격소를 거쳐 준비 진입
                ClickPauseButton(GameObject.Find("SortieBriefing").transform, "Ready/Depart"); // 선택한 캐릭터로 출발 확정
                yield return WaitForActiveScene(OuterIslandsPath); // 비동기 실전 씬 전환 완료 대기
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(OuterIslandsPath)); // 실전으로 진입
                Assert.That(PlayerPrefs.HasKey(root + ".Resume"), Is.True); // 재실행에서 사용할 출격 기록
                Transform stagePause = LocateCombatHud().transform.Find("PauseMenu"); // 출격의 실제 일시정지
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 포기 확인 진입
                yield return WaitFrames(4); // 현재 출격을 멈추고 패널 표시
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 메뉴 복귀 전에 키 해제
                ClickPauseButton(stagePause, "MenuButtons/Abandon"); // 기존 전투를 포기
                ClickPauseButton(stagePause, "Confirm/Accept"); // 실제 사용자 승인
                ClickPauseButton(stagePause, "Confirm/Title"); // 정지에서 메인 화면으로 복귀
                yield return WaitForActiveScene(titlePath); // 배포 첫 씬의 실제 전환
                yield return WaitFrames(3); // 이전에 저장한 버튼 상태 연결
                menu = GameObject.Find("MainMenu").transform; // 새 타이틀 오브젝트
                ClickPauseButton(menu, "Home/OpenSettings"); // 저장했던 설정을 재진입 후 확인
                settings = menu.Find("Settings"); // 새 씬의 설정 패널
                Assert.That(settings.Find("Volume").GetComponent<Slider>().value, Is.EqualTo(0.65f).Within(0.001f)); // 이전에 저장한 실제 음량 복원
                ClickPauseButton(settings, "Back"); // 이어서하기 확인으로 이동
                Assert.That(menu.Find("Home/Continue").GetComponent<Button>().interactable, Is.True); // 저장된 출격 가능 상태
                ClickPauseButton(menu, "Home/NewGame"); // 저장 있는 상태의 새 게임
                Assert.That(menu.Find("Confirm").gameObject.activeSelf, Is.True); // 기존 출격 교체 확인
                ClickPauseButton(menu, "Confirm/Cancel"); // 기존 진행 보존
                Assert.That(menu.Find("Home").gameObject.activeSelf, Is.True); // 메인 화면 복귀
                ClickPauseButton(menu, "Home/Continue"); // 저장된 출격 불러오기
                yield return VillageBoard(); // 이어서하기도 같은 마을에서 저장한 조종사로 진입
                ClickPauseButton(GameObject.Find("SortieBriefing").transform, "Ready/Depart"); // 저장된 준비 상태로 실제 비행
                yield return WaitForActiveScene(OuterIslandsPath); // 실전 씬의 비동기 진입
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(OuterIslandsPath)); // 계속하기 실제 진입
            }
            finally // 다른 시험과 저장 키 분리
            {
                PlayerPrefs.DeleteKey(root + ".Volume"); // 시험 음량만 정리
                PlayerPrefs.DeleteKey(root + ".Sensitivity"); // 시험 감도만 정리
                PlayerPrefs.DeleteKey(root + ".Screen"); // 시험 화면 설정만 정리
                PlayerPrefs.DeleteKey(root + ".Resume"); // 시험 출격 표시만 정리
                PlayerPrefs.DeleteKey(root + ".Pilot"); // 시험 조종사 표시만 정리
                PlayerPrefs.Save(); // 남은 독립 시험 상태 확정
                System.Environment.SetEnvironmentVariable("PROJECTG_MENU_TEST_KEY", previous); // 실행 환경 복원
            }
        }

        [UnityTest] // 캐릭터·빗자루·마법을 실제 출격 씬에 연결
        public IEnumerator SortieBriefingCarriesRianGearAndMagicIntoStage() // 기획 일러스트와 세 가지 선택 보존
        {
            string root = "ProjectG.Briefing.Test." + System.Guid.NewGuid().ToString("N"); // 서로 다른 선택의 독립 저장
            string[] flags = { "PROJECTG_MENU_TEST_KEY", "PROJECTG_BROOM_TEST_KEY", "PROJECTG_CRAFTED_TEST_KEY" }; // 실제 제작 화면의 저장 키
            string[] original = new string[flags.Length]; // 시험 전의 환경
            for (int i = 0; i < flags.Length; i++) // 세 가지 프로필의 검증 분리
            {
                original[i] = System.Environment.GetEnvironmentVariable(flags[i]); // 이용자의 실제 환경값 보존
                System.Environment.SetEnvironmentVariable(flags[i], root + i); // 독립 출격에서 쓸 기록
            }
            try // 전용 준비·장비·마법 값을 시험한 뒤 정리
            {
                const string path = "Assets/Scenes/FlightSortieBriefing.unity"; // 출격 준비의 실제 씬
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(path); // 빌드 등록 여부
                Assert.That(buildIndex, Is.GreaterThan(0)); // 시작 다음에 열리는 씬
                yield return SceneManager.LoadSceneAsync(buildIndex); // 독립 준비 화면으로 진입
                yield return WaitFrames(3); // 실제 초기 화면 준비
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 편집 가능한 준비 화면
                ClickPauseButton(briefing, "Ready/SelectRian"); // 리안 조종사 선택
                Assert.That(briefing.Find("Ready/Portrait").GetComponent<RawImage>().texture.name, Is.EqualTo("RianStanding")); // 기획서 원본 스탠딩 적용
                ClickPauseButton(briefing, "Ready/EditGear"); // 기존 8슬롯 조립 화면 열기
                Transform gear = briefing.Find("BroomWorkshop"); // 출격 전에 편집하는 빗자루
                ClickPauseButton(gear, "Parts/Part1"); // 리안의 시험용 질풍 몸체 선택
                ClickPauseButton(gear, "Equip"); // 현재 장비의 부품 교체 확정
                ClickPauseButton(gear, "Back"); // 리안의 출격 준비로 복귀
                ClickPauseButton(briefing, "Ready/EditMagic"); // 기존 제작 마법 화면 열기
                Transform workshop = briefing.Find("CraftedSpellEditor"); // 같은 제작 조합과 저장 흐름
                ClickPauseButton(workshop, "Save"); // 현재 마법 조합 저장
                ClickPauseButton(workshop, "EquipLeft"); // 좌클릭 마법에 편성
                ClickPauseButton(workshop, "Back"); // 실제 출발 버튼으로 복귀
                ClickPauseButton(briefing, "Ready/Depart"); // 선택한 장비로 출격
                yield return WaitForActiveScene(OuterIslandsPath); // 준비 씬에서 실제 전투 진입
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(OuterIslandsPath)); // 정상 씬 전환
                Transform pilot = FindPilot(); // 현재 실제 비행체
                var weapon = pilot.GetComponent("FlightWeapon"); // 같은 캐릭터의 공격과 HUD
                Assert.That(weapon.GetType().GetProperty("IsRian").GetValue(weapon), Is.True); // 실제 리안의 전투 프로필
                Assert.That(BroomAction(pilot.GetComponent("FlightBroomEquipment"), "GetEquippedId", "Rian", "Body"), Is.EqualTo("trial.storm-body:1")); // 리안의 장착한 부품 복원
                var book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 전투 편성과 실제 시전 도감
                var slot = book.GetType().GetMethod("ActiveSlot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 전투에서 쓰는 좌우 슬롯
                Assert.That(slot.Invoke(book, new object[] { 0 }), Is.Not.Null); // 출격 전 저장한 좌측 마법 로드
            }
            finally // 시험에서만 사용한 프로필과 환경 정리
            {
                PlayerPrefs.DeleteKey(root + "0.Resume"); // 격리 출격 상태
                PlayerPrefs.DeleteKey(root + "0.Pilot"); // 격리 조종사 기록
                PlayerPrefs.DeleteKey(root + "1"); // 격리 장비
                PlayerPrefs.DeleteKey(root + "2"); // 격리 제작 마법
                PlayerPrefs.Save(); // 실제 배치의 설정값 저장소 정리
                for (int i = 0; i < flags.Length; i++) // 시험 전의 외부 설정 복원
                {
                    System.Environment.SetEnvironmentVariable(flags[i], original[i]); // 사용자 설정 분리 유지
                }
            }
        }

        [UnityTest] // 첫 사건 기록, 실제 보스 조우, 건너뛰기와 재출격
        public IEnumerator FirstIncidentArchiveAndBroadcastDoNotRepeatDuringRetry() // 기술과 장비 화면의 기존 입력 유지
        {
            var kind = System.Type.GetType("ProjectG.FlightFirstIncident, Assembly-CSharp"); // 실제 기록 패널 클래스
            var reset = kind.GetMethod("ResetOpening", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); // 평소 새 실행의 상태
            reset.Invoke(null, null); // 다른 출격을 먼저 실행한 테스트와 분리
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 현재 배포의 출격 준비
            yield return WaitFrames(4); // 열린 메뉴의 버튼 연결
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 준비 정보와 스탠딩
            Transform archive = briefing.Find("FirstIncident/Archive"); // 편집 가능한 기록 창
            ClickPauseButton(briefing, "Ready/FirstIncidentOpen"); // 화면 버튼으로 기록 열기
            Assert.That(archive.gameObject.activeSelf, Is.True); // 실제 팝업 표시
            Canvas.ForceUpdateCanvases(); // 글자 줄 나눔과 화면 크기 먼저 계산
            Text copy = archive.Find("Card/Body").GetComponent<Text>(); // 실제 본문이 들어가는 표시 영역
            Assert.That(copy.preferredHeight, Is.LessThanOrEqualTo(copy.rectTransform.rect.height + .1f)); // 마지막 문장까지 잘리지 않는 높이
            string content = copy.text; // 문서와 같은 첫 사건 사실
            Assert.That(content, Does.Contain("천공 수호조사단").And.Contain("아리아와 리안").And.Contain("마물 습격")); // 기획서에 확정된 조직·사건
            Assert.That(archive.Find("Card/AriaPortrait").GetComponent<RawImage>().texture.name, Is.EqualTo("AriaStanding")); // 실제 기획 이미지
            Assert.That(archive.Find("Card/RianPortrait").GetComponent<RawImage>().texture.name, Is.EqualTo("RianStanding")); // 두 번째 실제 조종사
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 열린 기록에서 기존 출발 키를 눌러 비교
            yield return WaitFrames(4); // 같은 화면 유지 확인
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("FlightSortieBriefing")); // 열람 중 출격 금지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 기록에서 되돌아가기
            yield return WaitFrames(3); // 현재 기록 창만 숨김
            Assert.That(archive.gameObject.activeSelf, Is.False); // 메뉴가 사라지지 않고 기록만 종료
            SetKeys(); // 다음 입력 준비
            ClickPauseButton(briefing, "Ready/SelectAria"); // 첫 사건의 실제 조종사 선택
            ClickPauseButton(briefing, "Ready/Depart"); // 정상적인 행동으로 실전 진입
            yield return WaitForActiveScene(OuterIslandsPath); // 배포 항로의 현재 HUD
            Transform hud = LocateCombatHud().transform; // 정식 스테이지의 전투 정보
            Transform transmission = hud.Find("FirstIncidentBroadcast/Transmission"); // 실제 화면의 통신
            Component timeline = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 보스 조우 확인
            Assert.That(transmission.gameObject.activeSelf, Is.True); // 최초 출격에서만 표시
            Assert.That(HudText(transmission.Find("Title")), Does.Contain("첫 사건")); // 첫 만남의 일반 서술
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F3)); // 전투 중 통신 건너뛰기
            yield return WaitFrames(4); // 선택은 시계에 영향을 주지 않음
            SetKeys(); // 중간 보스 안내를 다시 건너뛸 준비
            Assert.That(transmission.gameObject.activeSelf, Is.False); // 실제 통신이 닫힘
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 닫으면서 전투가 멈추지 않음
            Component vitals = FindPilot().GetComponent("FlightPilotVitals"); // 보스까지 생존하는 실제 비행체
            ((Behaviour)FindPilot().GetComponent("FlightWeapon")).enabled = false; // 이 시험의 보스 조우를 자동 사격과 분리
            vitals.GetType().GetMethod("GrantProtection").Invoke(vitals, new object[] { 130f }); // 이 테스트에서 조우까지 살아 있기
            Time.captureDeltaTime = .1f; // 80초의 실제 일정 진행
            yield return AdvanceStageTo(timeline, 80.35f); // 실제 중간 보스 등장
            Assert.That(transmission.gameObject.activeSelf, Is.True); // 조우 직후에만 나오는 안내
            Assert.That(HudText(transmission.Find("Title")), Does.Contain("중간 보스")); // 도입 문구가 남지 않는 상태
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F3)); // 진행 중 보스 안내 닫기
            yield return WaitFrames(4); // 같은 보스에 다시 켜지지 않는지 확인
            SetKeys(); // 이 다음 단계는 조작하지 않음
            Assert.That(transmission.gameObject.activeSelf, Is.False); // 같은 보스에 대면 기록 중복 없음
            vitals.GetType().GetMethod("ResetRun").Invoke(vitals, null); // 시험 보호 제거
            var hit = vitals.GetType().GetMethod("ResolveThreat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 실제 피격 처리
            hit.Invoke(vitals, new object[] { Vector3.zero, Vector3.forward, .1f, 125, false, false }); // 빠른 실패 정산에 필요한 통제 피격
            yield return WaitFrames(3); // 기존 결과 진입 버튼이 열리는 시점
            ClickPauseButton(hud.Find("SortieEnd"), "Open"); // 실제 결과 씬 이동
            yield return WaitForActiveScene("Assets/Scenes/FlightSortieResult.unity"); // 전투 끝난 조종사와 현재 정산
            Transform result = GameObject.Find("SortieResult").transform; // 기존 결과가 존재하는 오른쪽 패널
            ClickPauseButton(result, "FirstIncidentOpen"); // 정산에서도 같은 기록 열람
            Transform ending = result.Find("FirstIncident/Archive/Card/Result"); // 이번 출격에만 추가하는 한 줄
            Assert.That(HudText(ending), Does.Contain("임무를 완료하지 못했습니다")); // 실패와 세계관의 사실을 서로 분리
            ClickPauseButton(result, "FirstIncident/Archive/Card/Close"); // 원래 재출격 버튼으로 복귀
            ClickPauseButton(result, "Panel/Retry"); // 정상적인 실제 재시작
            yield return WaitForActiveScene(OuterIslandsPath); // 새 항로의 현재 안내
            Assert.That(LocateCombatHud().transform.Find("FirstIncidentBroadcast/Transmission").gameObject.activeSelf, Is.False); // 같은 실행에서 첫 사건을 다시 띄우지 않음
            reset.Invoke(null, null); // 다음 검증의 독립성 복구
        }

        [UnityTest] // 자유 조종 연습에서 실제 편성한 제작 마법 사용
        public IEnumerator FreeTrainingCastsEquippedMagicWithoutProgressRewards() // 실제 비행·마법·보상 격리
        {
            string key = "ProjectG.Crafted.Free." + System.Guid.NewGuid().ToString("N"); // 사용자 마법 저장과 분리한 시험 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 이전 격리 환경 보존
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 실제 사용자 편성 제외
            try // 출격 준비에서 자유 훈련으로 실제 이동
            {
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 선택과 편성 화면
                yield return WaitFrames(4); // 출격 준비 버튼 연결
                System.Type progressType = System.Type.GetType("ProjectG.FlightProgress, Assembly-CSharp", true); // 정식 보상과 같은 공용 프로필
                const System.Reflection.BindingFlags hiddenStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic; // 실제 프로필 조회
                object progress = progressType.GetProperty("Current", hiddenStatic).GetValue(null); // 연습 전 실제 공용 상태
                int crystals = (int)progress.GetType().GetProperty("SharedCrystals").GetValue(progress); // 연습 전 공용 결정
                int gold = (int)progress.GetType().GetProperty("Gold").GetValue(progress); // 연습 전 공용 골드
                Transform ready = GameObject.Find("SortieBriefing").transform; // 실제 출격 준비 하이러키
                ClickPauseButton(ready, "Ready/FreeTraining"); // 독립 자유 훈련 입장
                yield return WaitForActiveScene(ScenePath); // 실제 조종 가능한 비행 씬
                Transform hud = LocateCombatHud().transform; // 자유 훈련의 실제 HUD
                Transform card = hud.Find("FreeTraining/Options/Card"); // 연습할 표적과 패턴 선택
                ClickPauseButton(card, "Target"); // 실제 마물 판정을 가진 표적 시작
                yield return WaitFrames(4); // 조준·자원·편성 적용
                Transform pilot = FindPilot(); // 현재 자유 비행 조종체
                ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 사격의 표적 피해 제외
                Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실전과 같은 제작 발사기
                Assert.That(caster, Is.Not.Null); // 실제 자유 비행에서 제작 마법 지원
                Component book = hud.GetComponent("FlightCraftedSpellBook"); // 실제 저장 편성을 읽는 도감
                Assert.That(book, Is.Not.Null); // 자유 연습에 편성 연결
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 무작위 배정된 실제 칸 조회
                object fire = System.Type.GetType("ProjectG.CraftedSpellPresets, Assembly-CSharp", true).GetProperty("All").GetValue(null); // 문서의 실제 마법 목록
                object entry = ((System.Collections.IList)fire)[0]; // 기본 파이어 볼트
                object fireRecipe = entry.GetType().GetProperty("Recipe").GetValue(entry); // 실제 화염 구성
                string id = (string)fireRecipe.GetType().GetProperty("Id").GetValue(fireRecipe); // 편성한 실제 화염 구성 ID
                var active = book.GetType().GetMethod("ActiveSlot", hidden); // 자유 훈련의 실제 좌우 배정
                object left = active.Invoke(book, new object[] { 0 }); // 왼쪽에 배정된 구성
                int lane = left != null && (string)left.GetType().GetProperty("Id").GetValue(left) == id ? 0 : 1; // 파이어 볼트가 배정된 실제 입력
                Component training = hud.Find("FreeTraining").GetComponent("FlightTrainingMode"); // 독립 연습의 실제 상태
                Component target = (Component)training.GetType().GetField("target", hidden).GetValue(training); // 정산과 분리된 연습 표적
                int health = Metric(target, "Health"); // 발사 전 실제 표적 체력
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(lane == 0 ? MouseButton.Left : MouseButton.Right)); // 해당 편성의 실제 가상 클릭
                yield return WaitFrames(110); // 실제 발사체와 표적 충돌 대기
                Assert.That(Metric(caster, "CastCount"), Is.EqualTo(1)); // 실전과 같은 제작 마법 실제 시전
                Assert.That(Metric(target, "Health"), Is.LessThan(health)); // 자유 연습 표적에 실제 판정 반영
                Assert.That(progress.GetType().GetProperty("SharedCrystals").GetValue(progress), Is.EqualTo(crystals)); // 연습의 공용 결정 보상 없음
                Assert.That(progress.GetType().GetProperty("Gold").GetValue(progress), Is.EqualTo(gold)); // 연습의 공용 골드 보상 없음
                InputSystem.QueueStateEvent(mouse, new MouseState()); // 마지막 시험 입력 해제
            }
            finally // 이 검증에서 만든 설정만 정리
            {
                PlayerPrefs.DeleteKey(key); // 시험용 마법 저장 제거
                PlayerPrefs.Save(); // 시험 키 정리 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 이전 격리 환경 복원
            }
        }

        [UnityTest] // 저장 마법의 복제·이름·삭제와 재실행 복원
        public IEnumerator MagicGalleryKeepsCopiesAndProtectsEquippedSpells() // 같은 구성을 공유하는 독립 목록
        {
            string key = "ProjectG.Crafted.Gallery." + System.Guid.NewGuid().ToString("N"); // 사용자 저장과 분리한 검증 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 저장 환경
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 이번 시험의 독립 저장
            try // 목록과 실전 편성의 실제 저장 경로
            {
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 제작 도감이 있는 실제 씬
                yield return WaitFrames(4); // 저장과 버튼 초기화
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 출격 준비
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 도감 검사
                Component editor = briefing.Find("CraftedSpellEditor").GetComponent("FlightCraftedSpellEditor"); // 실제 제작 화면
                Component book = (Component)editor.GetType().GetField("book", hidden).GetValue(editor); // 화면이 사용하는 공용 제작 마법 저장
                object fire = book.GetType().GetMethod("Slot", hidden).Invoke(book, new object[] { 0 }); // 기본 편성 파이어 볼트
                book.GetType().GetMethod("Save", hidden).Invoke(book, new[] { fire }); // 기존 저장 경로로 도감 추가
                Assert.That(book.GetType().GetProperty("GalleryCount", hidden).GetValue(book), Is.EqualTo(1)); // 첫 저장 항목
                Assert.That(book.GetType().GetMethod("DuplicateGallery", hidden).Invoke(book, new object[] { 0 }), Is.True); // 같은 구성의 독립 복제본
                Assert.That(book.GetType().GetMethod("RenameGallery", hidden).Invoke(book, new object[] { 1, "연습 마법" }), Is.True); // 복제본만 이름 변경
                Assert.That(book.GetType().GetMethod("DeleteGallery", hidden).Invoke(book, new object[] { 1 }), Is.False); // 편성에서 참조 중인 구성 삭제 차단
                book.GetType().GetMethod("ClearSlot", hidden).Invoke(book, new object[] { 0 }); // 참조 슬롯 직접 해제
                Assert.That(book.GetType().GetMethod("DeleteGallery", hidden).Invoke(book, new object[] { 0 }), Is.True); // 참조가 없는 원본 삭제
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 저장 다시 읽기
                yield return WaitFrames(4); // 도감과 준비 화면 복원
                briefing = GameObject.Find("SortieBriefing").transform; // 재진입한 실제 준비
                editor = briefing.Find("CraftedSpellEditor").GetComponent("FlightCraftedSpellEditor"); // 다시 복원한 제작 화면
                book = (Component)editor.GetType().GetField("book", hidden).GetValue(editor); // 다시 복원한 저장
                Assert.That(book.GetType().GetProperty("GalleryCount", hidden).GetValue(book), Is.EqualTo(1)); // 복제본 한 장의 지속 저장
                object entry = book.GetType().GetMethod("GalleryAt", hidden).Invoke(book, new object[] { 0 }); // 남은 도감 항목
                Assert.That(entry.GetType().GetField("name").GetValue(entry), Is.EqualTo("연습 마법")); // 변경한 이름 보존
                Assert.That(entry.GetType().GetField("recipe").GetValue(entry), Is.EqualTo(fire.GetType().GetProperty("Id").GetValue(fire))); // 같은 구성의 공유 식별자 보존
            }
            finally // 이번 시험이 만든 키만 정리
            {
                PlayerPrefs.DeleteKey(key); // 검증 도감 삭제
                PlayerPrefs.Save(); // 키 삭제 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 이전 환경 복원
            }
        }

        [UnityTest] // 한도 변화에 따른 자동 해제와 복구
        public IEnumerator CraftedBudgetRestoresLowerPrioritySlotWithoutResettingItsState() // 우선순위와 임시 강화·쿨타임 보존
        {
            string key = "ProjectG.Crafted.AutoBudget." + System.Guid.NewGuid().ToString("N"); // 사용자 편성과 분리한 저장 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 저장 환경
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 이번 검증의 편성 저장
            try // 실제 전투 슬롯의 한도 감소와 복구
            {
                yield return LoadOuterIslands(); // 실제 장비와 제작 마법 출격
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 두 제작 마법의 실제 편성
                Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 쿨타임과 유지 효과
                Component equipment = FindPilot().GetComponent("FlightBroomEquipment"); // 마법 공격력의 실제 장비 보정
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실전 편성 조회
                var slot = book.GetType().GetMethod("Slot", hidden); // 저장된 논리 슬롯
                var active = book.GetType().GetMethod("ActiveSlot", hidden); // 무작위 배정된 실전 슬롯
                object fire = slot.Invoke(book, new object[] { 0 }); // 첫 번째 기본 마법
                object thorn = slot.Invoke(book, new object[] { 1 }); // 두 번째 기본 마법
                int fireCost = (int)fire.GetType().GetProperty("Cost").GetValue(fire); // 유지할 마법의 구성 비용
                int thornCost = (int)thorn.GetType().GetProperty("Cost").GetValue(thorn); // 자동 해제할 구성 비용
                Assert.That(thornCost, Is.GreaterThan(0)); // 한도 감소로 구분할 수 있는 실제 비용
                var priority = book.GetType().GetMethod("SetPriority", hidden); // 논리 슬롯별 유지 우선순위
                Assert.That(priority.Invoke(book, new object[] { 0, 2 }), Is.True); // 첫 마법을 높은 우선순위로 지정
                Assert.That(priority.Invoke(book, new object[] { 1, 0 }), Is.True); // 둘째 마법을 낮은 우선순위로 지정
                book.GetType().GetMethod("ResetRun", hidden).Invoke(book, null); // 우선순위를 포함한 실제 출격
                int thornLane = active.Invoke(book, new object[] { 0 }) == thorn ? 0 : 1; // 무작위 배정된 낮은 우선순위 슬롯
                System.Type kind = book.GetType().GetNestedType("Upgrade", System.Reflection.BindingFlags.NonPublic); // 제작 마법 임시 강화 종류
                object damage = System.Enum.Parse(kind, "Damage"); // 기존 피해 강화
                Assert.That(book.GetType().GetMethod("TryUpgrade", hidden).Invoke(book, new[] { (object)thornLane, damage }), Is.True); // 자동 해제 전 임시 강화
                Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { thorn }), Is.True); // 실제 쿨타임 시작
                float[] bonuses = (float[])equipment.GetType().GetField("bonuses", hidden).GetValue(equipment); // 현재 장비 합산 능력치
                System.Type stat = equipment.GetType().Assembly.GetType("ProjectG.FlightBroomStat"); // 빗자루 능력치 열거형
                int magicPower = (int)System.Enum.Parse(stat, "MagicPower"); // 마법 편성 한도 능력치
                float original = bonuses[magicPower]; // 이전 장비 보정
                bonuses[magicPower] = fireCost + thornCost - 1 - 100; // 두 마법의 합보다 한도 한 단계 감소
                yield return WaitFrames(3); // 자동 해제 적용
                Assert.That(active.Invoke(book, new object[] { thornLane }), Is.Null); // 낮은 우선순위의 실제 전투 슬롯 비움
                Assert.That(active.Invoke(book, new object[] { 1 - thornLane }), Is.SameAs(fire)); // 높은 우선순위 마법 보존
                Assert.That(slot.Invoke(book, new object[] { 1 }), Is.SameAs(thorn)); // 저장된 구성 자체 보존
                bonuses[magicPower] = original; // 원래 편성 한도 회복
                yield return WaitFrames(3); // 자동 재편성 적용
                Assert.That(active.Invoke(book, new object[] { thornLane }), Is.SameAs(thorn)); // 원래 무작위 슬롯에 복구
                Assert.That(book.GetType().GetMethod("UpgradeRank", hidden).Invoke(book, new object[] { thornLane }), Is.EqualTo(1)); // 출격 중 임시 강화 유지
                Assert.That((float)book.GetType().GetMethod("Cooldown", hidden).Invoke(book, new[] { thorn }), Is.GreaterThan(0f)); // 남은 쿨타임 유지
            }
            finally // 이번 검증의 저장만 정리
            {
                PlayerPrefs.DeleteKey(key); // 독립 편성 저장 삭제
                PlayerPrefs.Save(); // 검증 키 삭제 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 이전 환경 복원
            }
        }

        [UnityTest] // 사용자가 직접 해제한 슬롯의 자동 복구 제외
        public IEnumerator CraftedBudgetDoesNotRestoreManuallyClearedSlot() // 출격 준비에서 자동 해제와 직접 해제 구분
        {
            string key = "ProjectG.Crafted.AutoClear." + System.Guid.NewGuid().ToString("N"); // 사용자 편성과 분리한 시험 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 원래 저장 환경
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 독립 시험 편성 사용
            try // 실제 준비 화면의 슬롯과 장비 변경
            {
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 출격 준비의 실제 편성
                yield return WaitFrames(4); // 장비와 저장 초기화
                Transform editor = GameObject.Find("SortieBriefing").transform.Find("CraftedSpellEditor"); // 실제 제작 화면
                Component component = editor.GetComponent("FlightCraftedSpellEditor"); // 제작 화면의 저장 참조
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 내부 상태 조회
                Component book = (Component)component.GetType().GetField("book", hidden).GetValue(component); // 출격 준비 도감
                Component equipment = (Component)book.GetType().GetField("equipment", hidden).GetValue(book); // 선택 조종사의 실제 장비
                var slot = book.GetType().GetMethod("Slot", hidden); // 저장 편성의 논리 슬롯
                var active = book.GetType().GetMethod("ActiveSlot", hidden); // 한도를 적용한 준비 슬롯
                object fire = slot.Invoke(book, new object[] { 0 }); // 첫 기본 마법
                object thorn = slot.Invoke(book, new object[] { 1 }); // 둘째 기본 마법
                int fireCost = (int)fire.GetType().GetProperty("Cost").GetValue(fire); // 첫 편성 비용
                int thornCost = (int)thorn.GetType().GetProperty("Cost").GetValue(thorn); // 둘째 편성 비용
                var priority = book.GetType().GetMethod("SetPriority", hidden); // 저장하는 유지 우선순위
                priority.Invoke(book, new object[] { 0, 2 }); // 첫 슬롯을 우선 유지
                priority.Invoke(book, new object[] { 1, 0 }); // 둘째 슬롯을 먼저 자동 해제
                float[] bonuses = (float[])equipment.GetType().GetField("bonuses", hidden).GetValue(equipment); // 실제 장비의 능력치 배열
                System.Type stat = equipment.GetType().Assembly.GetType("ProjectG.FlightBroomStat"); // 실제 장비 능력치 종류
                int power = (int)System.Enum.Parse(stat, "MagicPower"); // 마법 공격력 위치
                float original = bonuses[power]; // 기존 장비 공격력
                bonuses[power] = fireCost + thornCost - 1 - 100; // 둘 다 유지할 수 없는 현재 한도
                Assert.That(active.Invoke(book, new object[] { 1 }), Is.Null); // 준비 요약을 읽을 때 즉시 자동 해제
                Assert.That(active.Invoke(book, new object[] { 0 }), Is.SameAs(fire)); // 높은 우선순위 편성 유지
                book.GetType().GetMethod("ClearSlot", hidden).Invoke(book, new object[] { 1 }); // 자동 해제된 슬롯을 사용자가 직접 비움
                bonuses[power] = original; // 기존 마법 공격력 복구
                yield return WaitFrames(3); // 자동 복구 검사
                Assert.That(active.Invoke(book, new object[] { 1 }), Is.Null); // 직접 해제한 마법은 복구하지 않음
                Assert.That(slot.Invoke(book, new object[] { 1 }), Is.Null); // 저장 편성에서도 명시적으로 비움
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 영구 편성 재진입
                yield return WaitFrames(4); // 저장 복원
                editor = GameObject.Find("SortieBriefing").transform.Find("CraftedSpellEditor"); // 새 준비 화면
                component = editor.GetComponent("FlightCraftedSpellEditor"); // 새 도감 참조
                book = (Component)component.GetType().GetField("book", hidden).GetValue(component); // 다시 읽은 편성
                Assert.That(book.GetType().GetMethod("Slot", hidden).Invoke(book, new object[] { 1 }), Is.Null); // 재실행 뒤에도 직접 빈 슬롯 보존
                Assert.That(book.GetType().GetMethod("Priority", hidden).Invoke(book, new object[] { 0 }), Is.EqualTo(2)); // 높은 우선순위 영구 복원
            }
            finally // 이번 검증의 저장만 정리
            {
                PlayerPrefs.DeleteKey(key); // 독립 편성 삭제
                PlayerPrefs.Save(); // 검증 키 삭제 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 이전 저장 환경 복원
            }
        }

        [UnityTest] // 자동 해제 시 유지 효과 종료와 독립 탄환 보존
        public IEnumerator CraftedBudgetStopsMaintainedEffectsButKeepsFiredProjectiles() // 정화 결계와 이미 발사한 탄환의 분리
        {
            string key = "ProjectG.Crafted.AutoEffects." + System.Guid.NewGuid().ToString("N"); // 사용자 편성과 분리한 저장 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 저장 환경
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 독립 편성 저장
            object researchState = null; // 시험에서만 수정할 공용 연구 상태
            string[] originalUnlocks = null; // 시험 이전의 연구 목록
            try // 실제 결계와 탄환을 동시에 유지한 상태
            {
                yield return LoadOuterIslands(); // 실제 출격과 제작 발사기
                Component book = LocateCombatHud().GetComponent("FlightCraftedSpellBook"); // 실제 두 슬롯
                Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 유지 효과와 탄환
                Component equipment = FindPilot().GetComponent("FlightBroomEquipment"); // 마법 공격력 보정
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 내부 실제 상태
                System.Type progressType = caster.GetType().Assembly.GetType("ProjectG.FlightProgress"); // 씬이 공유하는 연구 프로필
                object progress = progressType.GetProperty("Current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null); // 격리된 메모리 프로필
                researchState = progress.GetType().GetField("state", hidden).GetValue(progress); // 실제 연구 저장 스냅샷
                var unlockField = researchState.GetType().GetField("unlocks"); // 현재 해금 ID 배열
                originalUnlocks = (string[])unlockField.GetValue(researchState); // 다른 시험의 연구 상태 보존
                var unlocks = new System.Collections.Generic.List<string>(originalUnlocks); // 결계에 필요한 검증용 연구
                unlocks.AddRange(new[] { "research.Shape.barrier", "research.Formation.sphere", "research.Trajectory.stationary", "research.FireMode.placed", "research.Origin.around", "research.Effect.cancel" }); // 실제 결계 구성 요소 해금
                unlockField.SetValue(researchState, unlocks.ToArray()); // 이번 검증에만 적용
                System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 실제 확정 마법 목록
                var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 저장된 기획 마법
                object fire = all[0].GetType().GetProperty("Recipe").GetValue(all[0]); // 이미 발사할 독립 탄환
                object barrier = all[11].GetType().GetProperty("Recipe").GetValue(all[11]); // 유지 중인 정화 결계
                book.GetType().GetMethod("ClearSlot", hidden).Invoke(book, new object[] { 1 }); // 다른 슬롯을 명시적으로 비움
                Assert.That(book.GetType().GetMethod("Equip", hidden).Invoke(book, new[] { (object)0, barrier }), Is.True); // 정화 결계를 저장 편성
                book.GetType().GetMethod("ResetRun", hidden).Invoke(book, null); // 실제 출격에 결계 반영
                var active = book.GetType().GetMethod("ActiveSlot", hidden); // 실제 좌우 배정
                int lane = active.Invoke(book, new object[] { 0 }) != null ? 0 : 1; // 결계를 시전할 실제 슬롯
                book.GetType().GetMethod("Use", hidden).Invoke(book, new object[] { lane }); // 슬롯 입력과 같은 실제 결계 시전
                var barriers = (System.Collections.IList)caster.GetType().GetField("barriers", hidden).GetValue(caster); // 현재 유지 결계
                Assert.That(barriers.Count, Is.EqualTo(1)); // 실제 정화 결계 생성
                Assert.That(caster.GetType().GetMethod("Cast", hidden).Invoke(caster, new[] { fire }), Is.True); // 이미 발사된 독립 파이어 볼트
                var shots = (System.Collections.IList)caster.GetType().GetField("active", hidden).GetValue(caster); // 실제로 비행 중인 탄환
                Assert.That(shots.Count, Is.EqualTo(1)); // 결계와 별도 탄환 존재
                Component shot = (Component)shots[0]; // 자동 해제 후에도 살아 있어야 할 탄환
                float[] bonuses = (float[])equipment.GetType().GetField("bonuses", hidden).GetValue(equipment); // 현재 장비 능력치
                System.Type stat = equipment.GetType().Assembly.GetType("ProjectG.FlightBroomStat"); // 실제 능력치 종류
                int magicPower = (int)System.Enum.Parse(stat, "MagicPower"); // 마법 공격력 위치
                float original = bonuses[magicPower]; // 시험 이전 공격력 보정
                bonuses[magicPower] = -99f; // 결계의 편성 비용보다 작은 한도
                yield return WaitFrames(2); // 실제 자동 해제와 유지 종료
                Assert.That(active.Invoke(book, new object[] { lane }), Is.Null); // 정화 결계의 전투 슬롯 해제
                Assert.That(barriers.Count, Is.Zero); // 해당 슬롯의 유지 결계 종료
                Assert.That(shot != null && shot.gameObject.activeInHierarchy, Is.True); // 이미 발사된 독립 탄환 유지
                bonuses[magicPower] = original; // 이전 마법 공격력 복원
                yield return WaitFrames(2); // 같은 슬롯 자동 재편성
                Assert.That(active.Invoke(book, new object[] { lane }), Is.SameAs(barrier)); // 원래 결계 구성 복구
                Assert.That((float)book.GetType().GetMethod("Cooldown", hidden).Invoke(book, new[] { barrier }), Is.GreaterThan(0f)); // 조기 종료 뒤 남은 재사용 시간 보존
            }
            finally // 시험에서 바꾼 영구 설정과 연구 상태 정리
            {
                if (researchState != null && originalUnlocks != null) // 임시 해금을 적용한 경우
                {
                    researchState.GetType().GetField("unlocks").SetValue(researchState, originalUnlocks); // 이전 메모리 연구 상태 복원
                }
                PlayerPrefs.DeleteKey(key); // 독립 편성 저장 삭제
                PlayerPrefs.Save(); // 검증 키 삭제 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 기존 저장 환경 복원
            }
        }

        [UnityTest] // 제작 화면의 성능 선택과 저장 복원
        public IEnumerator MagicEditorSavesAndRestoresPerformancePreset() // 실제 고속형 선택 버튼과 저장 ID
        {
            string key = "ProjectG.Crafted.Performance." + System.Guid.NewGuid().ToString("N"); // 사용자 저장과 분리한 시험 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 저장 환경
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 독립 성능 저장 사용
            try // 제작 화면의 실제 성능 버튼
            {
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 출격 준비
                yield return WaitFrames(4); // 저장과 화면 연결
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 준비 화면
                ClickPauseButton(briefing, "Ready/EditMagic"); // 제작 화면 열기
                Transform editor = briefing.Find("CraftedSpellEditor"); // 실제 성능 선택 화면
                ClickPauseButton(editor, "Performance"); // 표준형에서 고속형 선택
                Assert.That(HudText(editor.Find("Performance/Label")), Does.Contain("고속형")); // 현재 성능 선택 표시
                ClickPauseButton(editor, "Save"); // 조합과 성능을 함께 저장
                Assert.That(PlayerPrefs.GetString(key), Does.Contain("/fast")); // 실제 저장 ID의 성능 구분
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 게임 재진입과 같은 저장 복원
                yield return WaitFrames(4); // 저장 복구와 버튼 연결
                briefing = GameObject.Find("SortieBriefing").transform; // 새 준비 화면
                ClickPauseButton(briefing, "Ready/EditMagic"); // 저장한 조합 다시 열기
                editor = briefing.Find("CraftedSpellEditor"); // 복원된 제작 화면
                Assert.That(HudText(editor.Find("Performance/Label")), Does.Contain("고속형")); // 저장한 성능 선택 복원
                Assert.That(HudText(editor.Find("SpellName")), Does.Contain("파이어 볼트")); // 같은 확정 마법 이름 유지
            }
            finally // 검증용 키만 정리
            {
                PlayerPrefs.DeleteKey(key); // 독립 성능 저장 제거
                PlayerPrefs.Save(); // 저장 삭제 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 원래 환경 복원
            }
        }

        [UnityTest] // 이동형 마법의 성능 프리셋과 공유 기준
        public IEnumerator CraftedPerformanceVariantsKeepDistinctCooldownsAndStats() // 표준·고속·장거리의 실제 수치와 시전
        {
            yield return LoadOuterIslands(); // 실제 제작 마법 전투 씬
            Component caster = FindPilot().GetComponent("FlightCraftedSpellCaster"); // 실전 발사기
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 시전과 수치 조회
            System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획의 확정 마법
            var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 실제 확정 마법 목록
            object standard = all[0].GetType().GetProperty("Recipe").GetValue(all[0]); // 표준 파이어 볼트
            string id = (string)standard.GetType().GetProperty("Id").GetValue(standard); // 기존 저장과 호환되는 기본 ID
            System.Type rules = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellRules"); // 구성 검증과 저장 복원 규칙
            var read = rules.GetMethod("TryRead"); // 실제 구성 ID 복원
            object[] fastInput = { id + "/fast", null }; // 같은 구성의 고속 프리셋 ID
            object[] longInput = { id + "/long", null }; // 같은 구성의 장거리 프리셋 ID
            Assert.That(read.Invoke(null, fastInput), Is.True); // 고속 프리셋의 저장 복원
            Assert.That(read.Invoke(null, longInput), Is.True); // 장거리 프리셋의 저장 복원
            object fast = fastInput[1]; // 검증된 고속 조합
            object distant = longInput[1]; // 검증된 장거리 조합
            var stats = caster.GetType().GetMethod("EffectiveStats", hidden); // 실제 장비를 반영한 시전 수치
            object basis = stats.Invoke(caster, new[] { standard }); // 같은 장비의 표준형 수치
            object fastStats = stats.Invoke(caster, new[] { fast }); // 같은 장비의 고속형 수치
            object longStats = stats.Invoke(caster, new[] { distant }); // 같은 장비의 장거리형 수치
            var speed = basis.GetType().GetField("Speed"); // 실제 투사체 속도
            var range = basis.GetType().GetField("Range"); // 실제 투사체 사거리
            Assert.That((float)speed.GetValue(fastStats), Is.EqualTo((float)speed.GetValue(basis) * 1.2f).Within(0.001f)); // 고속형 탄속 백이십 퍼센트
            Assert.That((float)range.GetValue(fastStats), Is.EqualTo((float)range.GetValue(basis) * 0.8f).Within(0.001f)); // 고속형 사거리 팔십 퍼센트
            Assert.That((float)speed.GetValue(longStats), Is.EqualTo((float)speed.GetValue(basis) * 0.8f).Within(0.001f)); // 장거리형 탄속 팔십 퍼센트
            Assert.That((float)range.GetValue(longStats), Is.EqualTo((float)range.GetValue(basis) * 1.2f).Within(0.001f)); // 장거리형 사거리 백이십 퍼센트
            var cast = caster.GetType().GetMethod("Cast", hidden); // 실제 발사와 쿨타임 적용
            Assert.That(cast.Invoke(caster, new[] { standard }), Is.True); // 표준형 실제 발사
            Assert.That(cast.Invoke(caster, new[] { fast }), Is.True); // 다른 프리셋의 독립 쿨타임
            Assert.That(cast.Invoke(caster, new[] { fast }), Is.False); // 같은 구성과 프리셋의 쿨타임 공유
            object barrier = all[11].GetType().GetProperty("Recipe").GetValue(all[11]); // 이동하지 않는 정화 결계
            string barrierId = (string)barrier.GetType().GetProperty("Id").GetValue(barrier); // 고정 결계의 기본 ID
            object[] invalid = { barrierId + "/fast", null }; // 비이동형에 적용할 수 없는 고속형
            Assert.That(read.Invoke(null, invalid), Is.False); // 결계 반경이나 지속시간의 임의 변경 방지
            caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 앞 시전의 탄환과 생성 한도 제거
            object ring = all[5].GetType().GetProperty("Recipe").GetValue(all[5]); // 왕복 이동하는 회류 고리
            string ringId = (string)ring.GetType().GetProperty("Id").GetValue(ring); // 기존 회류 구성 ID
            object[] ringInput = { ringId + "/long", null }; // 장거리 회류 프리셋
            Assert.That(read.Invoke(null, ringInput), Is.True); // 장거리 회류 구성 복원
            object longRing = ringInput[1]; // 실제 왕복 발사에 사용할 마법
            object ringStats = stats.Invoke(caster, new[] { longRing }); // 장거리 회류의 실제 이동 거리
            Assert.That(cast.Invoke(caster, new[] { longRing }), Is.True); // 여섯 발의 실제 장거리 회류 시전
            var shots = (System.Collections.IList)caster.GetType().GetField("active", hidden).GetValue(caster); // 실제 생성된 회류 원반
            Assert.That(shots.Count, Is.EqualTo(6)); // 기획대로 여섯 원반 생성
            object first = shots[0]; // 실제 첫 번째 원반
            Assert.That((bool)first.GetType().GetField("returningRing", hidden).GetValue(first), Is.True); // 성능 변경 후에도 왕복 동작 유지
            Assert.That((float)first.GetType().GetField("outboundRemaining", hidden).GetValue(first), Is.EqualTo((float)range.GetValue(ringStats) * 0.5f).Within(0.001f)); // 변경한 전체 왕복 거리의 절반을 편도로 적용
            caster.GetType().GetMethod("ResetCasts", hidden).Invoke(caster, null); // 회류 원반과 생성 한도 제거
            object seed = all[8].GetType().GetProperty("Recipe").GetValue(all[8]); // 고정 포대에서 나오는 이동 자탄
            string seedId = (string)seed.GetType().GetProperty("Id").GetValue(seed); // 기존 포대 구성 ID
            object[] seedInput = { seedId + "/fast", null }; // 고속 자탄 프리셋
            Assert.That(read.Invoke(null, seedInput), Is.True); // 이동 자탄의 성능 프리셋 복원
            Assert.That(cast.Invoke(caster, new[] { seedInput[1] }), Is.True); // 실제 고정 포대 시전
            yield return WaitFrames(70); // 실제 지연 후 분열 생성
            Assert.That(Metric(caster, "EmissionCount"), Is.EqualTo(4)); // 설치체 한 개와 이동 자탄 세 발
        }

        [UnityTest] // 이동형 마법에 장비 탄속과 사거리 보정
        public IEnumerator CraftedMovingProjectilesUseBroomSpeedAndRangeBonuses() // 이동형과 고정 결계의 적용 범위
        {
            yield return LoadOuterIslands(); // 실제 장비와 제작 마법 발사기
            Transform pilot = FindPilot(); // 실전 조종체
            Component equipment = pilot.GetComponent("FlightBroomEquipment"); // 장착한 빗자루의 실제 능력치
            Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 제작 마법의 실제 발사기
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 시험용 능력치 조회
            float[] bonuses = (float[])equipment.GetType().GetField("bonuses", hidden).GetValue(equipment); // 현재 장비의 합산 보정
            System.Type stat = equipment.GetType().Assembly.GetType("ProjectG.FlightBroomStat"); // 빗자루 능력치 열거형
            int speed = (int)System.Enum.Parse(stat, "BoltSpeed"); // 이동 탄속 보정 위치
            int range = (int)System.Enum.Parse(stat, "Range"); // 이동 사거리 보정 위치
            float oldSpeed = bonuses[speed]; // 원래 탄속 보정 보관
            float oldRange = bonuses[range]; // 원래 사거리 보정 보관
            try // 같은 프레임에서 실전 수치 비교
            {
                System.Type presets = caster.GetType().Assembly.GetType("ProjectG.CraftedSpellPresets"); // 기획의 확정 마법
                var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 실제 프리셋 목록
                object fire = all[0].GetType().GetProperty("Recipe").GetValue(all[0]); // 이동형 파이어 볼트
                object seed = all[8].GetType().GetProperty("Recipe").GetValue(all[8]); // 고정 설치체에서 나오는 이동 자탄
                object barrier = all[11].GetType().GetProperty("Recipe").GetValue(all[11]); // 이동하지 않는 정화 결계
                var stats = caster.GetType().GetMethod("EffectiveStats", hidden); // 실제 시전 수치 스냅샷
                bonuses[speed] = 0f; // 기준 탄속 보정
                bonuses[range] = 0f; // 기준 사거리 보정
                object baseFire = stats.Invoke(caster, new[] { fire }); // 장비 보정 전 이동형 수치
                object baseSeed = stats.Invoke(caster, new[] { seed }); // 장비 보정 전 씨앗 자탄 수치
                object baseBarrier = stats.Invoke(caster, new[] { barrier }); // 장비 보정 전 결계 수치
                bonuses[speed] = 20f; // 탄속 이십 퍼센트 보정
                bonuses[range] = 50f; // 사거리 오십 퍼센트 보정
                object boostedFire = stats.Invoke(caster, new[] { fire }); // 실제 장비 보정 후 이동형 수치
                object boostedSeed = stats.Invoke(caster, new[] { seed }); // 실제 장비 보정 후 씨앗 자탄 수치
                object boostedBarrier = stats.Invoke(caster, new[] { barrier }); // 실제 장비 보정 후 고정 결계
                var speedField = baseFire.GetType().GetField("Speed"); // 시전 수치의 실제 탄속
                var rangeField = baseFire.GetType().GetField("Range"); // 시전 수치의 실제 사거리
                Assert.That((float)speedField.GetValue(boostedFire), Is.EqualTo((float)speedField.GetValue(baseFire) * 1.2f).Within(0.001f)); // 이동 탄속의 장비 보정 한 번 적용
                Assert.That((float)rangeField.GetValue(boostedFire), Is.EqualTo((float)rangeField.GetValue(baseFire) * 1.5f).Within(0.001f)); // 이동 사거리의 장비 보정 한 번 적용
                Assert.That((float)speedField.GetValue(boostedSeed), Is.EqualTo((float)speedField.GetValue(baseSeed) * 1.2f).Within(0.001f)); // 고정 포대의 이동 자탄에도 탄속 보정
                Assert.That((float)rangeField.GetValue(boostedSeed), Is.EqualTo((float)rangeField.GetValue(baseSeed) * 1.5f).Within(0.001f)); // 고정 포대의 이동 자탄에도 사거리 보정
                Assert.That(speedField.GetValue(boostedBarrier), Is.EqualTo(speedField.GetValue(baseBarrier))); // 결계의 속도 불변
                Assert.That(rangeField.GetValue(boostedBarrier), Is.EqualTo(rangeField.GetValue(baseBarrier))); // 결계의 거리 불변
            }
            finally // 검증에 쓴 장비 보정 복원
            {
                bonuses[speed] = oldSpeed; // 이전 탄속 보정 복구
                bonuses[range] = oldRange; // 이전 사거리 보정 복구
            }
        }

        [UnityTest] // 이전 단일 저장을 새 도감에서 보존
        public IEnumerator MagicGalleryMigratesLegacySaveWithoutChangingItsSlots() // 기존 편성과 마법 구성 이관
        {
            string key = "ProjectG.Crafted.GalleryLegacy." + System.Guid.NewGuid().ToString("N"); // 독립된 구형 저장 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 저장 환경 보존
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 사용자 저장과 분리
            try // 기존 버전의 실제 불러오기와 다음 저장
            {
                System.Type presets = System.Type.GetType("ProjectG.CraftedSpellPresets, Assembly-CSharp", true); // 실제 확정 마법 목록
                var all = (System.Collections.IList)presets.GetProperty("All").GetValue(null); // 기획 마법의 실제 목록
                object fire = all[0].GetType().GetProperty("Recipe").GetValue(all[0]); // 이전에 저장한 기본 마법
                string id = (string)fire.GetType().GetProperty("Id").GetValue(fire); // 기존 저장의 구성 ID
                string legacy = "{\"version\":2,\"recipe\":\"" + id + "\",\"left\":\"" + id + "\",\"right\":\"\"}"; // 기존 단일 저장 형식
                PlayerPrefs.SetString(key, legacy); // 실제 이전 저장 입력
                PlayerPrefs.Save(); // 불러올 저장 확정
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 제작 화면 진입
                yield return WaitFrames(4); // 저장 복원 완료
                Transform editor = GameObject.Find("SortieBriefing").transform.Find("CraftedSpellEditor"); // 실제 제작 화면
                Component component = editor.GetComponent("FlightCraftedSpellEditor"); // 도감 소유자
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 저장 참조
                Component book = (Component)component.GetType().GetField("book", hidden).GetValue(component); // 기존 마법을 복원한 도감
                Assert.That(book.GetType().GetProperty("GalleryCount", hidden).GetValue(book), Is.EqualTo(1)); // 이전 단일 마법의 도감 이관
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo(legacy)); // 조회만으로 기존 저장을 덮어쓰지 않음
                Assert.That(book.GetType().GetMethod("DuplicateGallery", hidden).Invoke(book, new object[] { 0 }), Is.True); // 사용자의 명시적 복제 저장
                string updated = PlayerPrefs.GetString(key); // 이관 후 실제 저장
                Assert.That(updated, Does.Contain("\"left\":\"" + id + "\"")); // 이전 좌측 편성 보존
                Assert.That(updated, Does.Contain("\"right\":\"\"")); // 기존의 빈 우측 편성 보존
            }
            finally // 이번 검증의 키만 정리
            {
                PlayerPrefs.DeleteKey(key); // 독립 구형 저장 삭제
                PlayerPrefs.Save(); // 삭제 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 이전 환경 복원
            }
        }

        [UnityTest] // 실제 마법 도감의 저장·검색·복제·이름 변경
        public IEnumerator MagicGalleryScreenSearchesAndEditsSavedSpells() // 제작 화면의 도감 버튼 연결
        {
            string key = "ProjectG.Crafted.GalleryUI." + System.Guid.NewGuid().ToString("N"); // 독립된 실제 저장 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 환경 보존
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 사용자 마법 저장 격리
            try // 실제 제작과 도감 화면의 버튼 조작
            {
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 제작 화면이 있는 실제 씬
                yield return WaitFrames(4); // 제작 화면 연결
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 출격 준비
                ClickPauseButton(briefing, "Ready/EditMagic"); // 현재 조합 편집
                Transform editor = briefing.Find("CraftedSpellEditor"); // 실제 제작 화면
                string[] headings = { "Header", "OpenResearch", "OpenGallery", "SpellName", "Preset" }; // 상단에 나란히 보일 제목과 버튼
                for (int i = 1; i < headings.Length; i++) // 이웃한 실제 UI 영역 비교
                {
                    RectTransform left = editor.Find(headings[i - 1]).GetComponent<RectTransform>(); // 왼쪽 제목 또는 버튼
                    RectTransform right = editor.Find(headings[i]).GetComponent<RectTransform>(); // 오른쪽 제목 또는 버튼
                    Assert.That(right.anchoredPosition.x - left.anchoredPosition.x, Is.GreaterThanOrEqualTo((left.sizeDelta.x + right.sizeDelta.x) * 0.5f)); // 제목과 버튼의 가로 겹침 방지
                }
                string[] footer = { "Back", "FreeFlight", "Performance", "PreviewFlight", "Recall", "Save" }; // 제작 화면 하단의 실제 버튼 순서
                for (int i = 1; i < footer.Length; i++) // 이웃한 하단 버튼 영역 비교
                {
                    RectTransform left = editor.Find(footer[i - 1]).GetComponent<RectTransform>(); // 왼쪽 버튼
                    RectTransform right = editor.Find(footer[i]).GetComponent<RectTransform>(); // 오른쪽 버튼
                    Assert.That(right.anchoredPosition.x - left.anchoredPosition.x, Is.GreaterThanOrEqualTo((left.sizeDelta.x + right.sizeDelta.x) * 0.5f)); // 하단 버튼의 가로 겹침 방지
                }
                Assert.That(HudText(editor.Find("PriorityLeft/Label")), Does.Contain("보통")); // 기본 유지 우선순위 표시
                ClickPauseButton(editor, "PriorityLeft"); // 왼쪽 편성의 우선순위 변경
                Assert.That(HudText(editor.Find("PriorityLeft/Label")), Does.Contain("높음")); // 실제 저장된 높은 우선순위 표시
                ClickPauseButton(editor, "Save"); // 기본 조합을 도감에 저장
                ClickPauseButton(editor, "OpenGallery"); // 새 도감 화면 열기
                Transform gallery = editor.Find("GalleryPanel"); // 실제 목록과 필터
                Assert.That(HudText(gallery.Find("Entry0/Label")), Does.Contain("파이어 볼트")); // 저장한 첫 카드 표시
                ClickPauseButton(gallery, "Entry0"); // 현재 카드 선택
                ClickPauseButton(gallery, "Duplicate"); // 독립된 같은 구성 복제
                ClickPauseButton(gallery, "Entry1"); // 새 복제본 선택
                gallery.Find("Name").GetComponent<InputField>().text = "나의 연습 마법"; // 사용자 이름 입력
                ClickPauseButton(gallery, "Rename"); // 해당 카드의 이름 저장
                gallery.Find("Search").GetComponent<InputField>().text = "연습"; // 새 이름으로 실제 검색
                Assert.That(HudText(gallery.Find("Entry0/Label")), Does.Contain("나의 연습 마법")); // 검색 결과에 복제본 표시
                Assert.That(gallery.Find("Entry1").gameObject.activeSelf, Is.False); // 불일치한 원본은 결과에서 제외
                ClickPauseButton(gallery, "ElementFilter"); // 전체에서 화염 속성으로 전환
                Assert.That(gallery.Find("Entry0").gameObject.activeSelf, Is.True); // 실제 화염 속성과 필터 일치
                ClickPauseButton(gallery, "Entry0"); // 검색 결과 선택
                ClickPauseButton(gallery, "Load"); // 해당 구성을 제작 화면에 복원
                Assert.That(gallery.gameObject.activeSelf, Is.False); // 도감 닫기와 편집 복귀
                Assert.That(HudText(editor.Find("SpellName")), Does.Contain("파이어 볼트")); // 원래 구성으로 편집 복원
            }
            finally // 이번 UI 검증의 키만 정리
            {
                PlayerPrefs.DeleteKey(key); // 독립 도감 저장 삭제
                PlayerPrefs.Save(); // 검증 키 삭제 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 이전 환경 복원
            }
        }

        [UnityTest] // 제작 화면 안의 실제 비행과 같은 세션 전체 화면
        public IEnumerator MagicPreviewKeepsFlightSessionAcrossFullscreenAndInputFocus() // 편집 입력과 실제 조종 입력 분리
        {
            string key = "ProjectG.Crafted.Embedded." + System.Guid.NewGuid().ToString("N"); // 사용자 편성과 분리한 시험 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 기존 저장 환경 보존
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 실제 사용자 저장 격리
            try // 제작 화면과 독립 연습 씬의 실제 연결
            {
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 현재 캐릭터의 실제 제작 화면
                yield return WaitFrames(4); // 버튼과 캐릭터 초기화
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 출격 준비 화면
                ClickPauseButton(briefing, "Ready/EditMagic"); // 제작 화면 열기
                Transform editor = briefing.Find("CraftedSpellEditor"); // 실제 제작 UI
                ClickPauseButton(editor, "PreviewFlight"); // 편집 중인 조합의 내장 비행 세션 시작
                for (int frame = 0; frame < 600 && !SceneManager.GetSceneByName("FlightPrototype").isLoaded; frame++) // 비동기 연습 씬 준비 대기
                {
                    yield return null; // 비행 씬의 실제 로드 진행
                }
                Assert.That(SceneManager.GetSceneByName("FlightPrototype").isLoaded, Is.True); // 조종 가능한 실제 연습 씬 연결
                yield return WaitFrames(10); // 비행체와 렌더 대상 연결
                Transform pilot = FindPilot(); // 현재 캐릭터와 장비로 복제된 실제 비행체
                Component flight = pilot.GetComponent("FirstFlightController"); // 실제 이동 입력 제어
                ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동탄이 시험 표적에 주는 간섭 차단
                Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실전과 같은 제작 마법 발사기
                Component embedded = briefing.GetComponent("FlightEmbeddedMagicTraining"); // 제작 화면의 독립 연습 소유자
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 연습 표적 조회
                Component training = (Component)embedded.GetType().GetField("training", hidden).GetValue(embedded); // 추가된 실제 훈련 모드
                Component target = (Component)training.GetType().GetField("target", hidden).GetValue(training); // 실제 중립 피해 표적
                Transform muzzle = (Transform)caster.GetType().GetField("muzzle", hidden).GetValue(caster); // 실제 제작 마법 발사 위치
                Assert.That(Vector3.Distance(muzzle.position, target.transform.position), Is.EqualTo(10f).Within(0.05f)); // 표시된 십 미터의 총구 기준 실제 거리
                System.Type progressType = caster.GetType().Assembly.GetType("ProjectG.FlightProgress"); // 공용 재화 프로필
                object progress = progressType.GetProperty("Current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null); // 연습 전 실제 프로필
                int crystals = (int)progress.GetType().GetProperty("SharedCrystals").GetValue(progress); // 연습 전 공용 결정
                int gold = (int)progress.GetType().GetProperty("Gold").GetValue(progress); // 연습 전 공용 골드
                Vector3 initial = pilot.position; // 편집 중인 초기 비행 위치
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 편집 패널에서 비행 키 입력
                yield return WaitFrames(12); // 입력 분리 확인
                Assert.That(pilot.position, Is.EqualTo(initial)); // 미리보기 포커스 전 이동 차단
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 다음 비행 입력 분리
                ClickPauseButton(editor, "RangeView"); // 중앙 비행 화면의 입력 포커스
                Component previewDeck = (Component)training.GetType().GetField("deck", hidden).GetValue(training); // 실제 시험 마법 입력
                Assert.That(((Behaviour)previewDeck).enabled, Is.False); // 첫 포커스 클릭의 마법 입력 차단
                yield return WaitFrames(3); // 포커스 클릭의 발사 입력 소비
                Assert.That(Metric(caster, "CastCount"), Is.Zero); // 포커스 획득만으로 시험 발사 금지
                Assert.That(flight.GetType().GetProperty("CanFly").GetValue(flight), Is.True); // 비행 포커스에서 실제 조종 허용
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 실제 전진 비행 입력
                yield return WaitFrames(30); // 동일 세션의 실제 이동
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 입력 해제
                Assert.That(Vector3.Distance(pilot.position, initial), Is.GreaterThan(0.1f)); // 편집 화면 안의 실제 비행
                int health = Metric(target, "Health"); // 실제 시험 발사 전 표적 체력
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); // 편집 중인 기본 마법의 실제 발사
                yield return WaitFrames(110); // 발사체의 비행과 충돌 대기
                InputSystem.QueueStateEvent(mouse, new MouseState()); // 시험 클릭 해제
                Assert.That(Metric(caster, "CastCount"), Is.EqualTo(1)); // 실제 제작 발사기를 통한 한 번의 시전
                Assert.That(Metric(target, "Health"), Is.LessThan(health)); // 독립 연습 표적의 실제 피해
                yield return WaitFrames(2); // 실제 표적 상태의 화면 반영
                Assert.That(HudText(editor.Find("RangeResult")), Does.Contain($"표적 체력 {Metric(target, "Health")}")); // 보이는 표적의 실제 체력 표시
                Assert.That((int)progress.GetType().GetProperty("SharedCrystals").GetValue(progress), Is.EqualTo(crystals)); // 연습으로 공용 결정 지급 금지
                Assert.That((int)progress.GetType().GetProperty("Gold").GetValue(progress), Is.EqualTo(gold)); // 연습으로 공용 골드 지급 금지
                Vector3 moved = pilot.position; // 전체 화면 전환 전 현재 위치
                ClickPauseButton(editor, "PreviewFullscreen"); // 같은 비행 세션을 전체 화면으로 확대
                yield return WaitFrames(2); // UI 크기 변경 반영
                Assert.That(pilot, Is.Not.Null); // 전체 화면 전환에서 비행체 보존
                Assert.That(Vector3.Distance(pilot.position, moved), Is.LessThan(2f)); // 같은 세션의 이동 위치 보존
                RectTransform view = editor.Find("RangeView").GetComponent<RectTransform>(); // 실제 3D 비행 표시
                Assert.That(view.sizeDelta.x, Is.GreaterThan(1000f)); // 전체 화면으로 표시 영역 확대
                ClickPauseButton(editor, "PreviewFullscreen"); // 같은 세션의 원래 크기 복귀
                Assert.That(view.sizeDelta.x, Is.LessThan(600f)); // 중앙 미리보기 크기 복원
                Assert.That(PlayerPrefs.HasKey(key), Is.False); // 내장 연습과 화면 전환으로 영구 저장 금지
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 비행 입력에서 편집 포커스로 전환
                yield return WaitFrames(4); // 실제 Esc 입력 처리
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 복귀 키 해제
                Assert.That(flight.GetType().GetProperty("CanFly").GetValue(flight), Is.False); // 편집 포커스에서 비행 입력 차단
                Assert.That(editor.gameObject.activeSelf, Is.True); // 같은 Esc로 제작 화면을 닫지 않음
                Assert.That(Time.timeScale, Is.Zero); // 편집 중 실제 연습 시간 동결
                Vector3 beforeEdit = pilot.position; // 조합 변경 전의 같은 세션 위치
                ClickPauseButton(editor, "Performance"); // 현재 미저장 마법을 고속형으로 변경
                Component previewBook = (Component)training.GetType().GetField("crafted", hidden).GetValue(training); // 격리된 실제 시험 편성
                object changed = previewBook.GetType().GetMethod("ActiveSlot", hidden).Invoke(previewBook, new object[] { 0 }); // 현재 시험에 반영된 조합
                Assert.That((string)changed.GetType().GetProperty("Id").GetValue(changed), Does.EndWith("/fast")); // 편집한 성능이 같은 실제 연습에 반영
                Assert.That(pilot.position, Is.EqualTo(beforeEdit)); // 조합 변경 중 캐릭터 위치 유지
                ClickPauseButton(editor, "ResetTest"); // 실제 내장 연습의 표적과 비행 초기화
                Assert.That(Metric(target, "Health"), Is.EqualTo(Metric(target, "MaxHealth"))); // 실제 시험 표적 체력 초기화
                Assert.That(Metric(caster, "CastCount"), Is.Zero); // 실제 연습 발사 기록 초기화
                ClickPauseButton(editor, "Back"); // 제작 화면을 닫고 원래 출격 준비로 복귀
                yield return WaitFrames(15); // 독립 비행 씬과 임시 리소스 종료
                Assert.That(SceneManager.GetSceneByName("FlightPrototype").isLoaded, Is.False); // 연습 씬 완전 해제
                Assert.That(Time.timeScale, Is.EqualTo(1f)); // 메뉴의 게임 시간 복원
            }
            finally // 이번 검증의 저장만 정리
            {
                PlayerPrefs.DeleteKey(key); // 독립 편성 키 삭제
                PlayerPrefs.Save(); // 삭제 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 기존 저장 환경 복원
            }
        }

        [UnityTest] // 내장 비행을 여는 도중 제작 화면 종료
        public IEnumerator MagicPreviewCancelsPendingSceneLoadWithoutFreezingMenu() // 비동기 로드와 정식 화면의 분리
        {
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 출격 준비
            yield return WaitFrames(4); // 제작 버튼 연결
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 준비 화면
            ClickPauseButton(briefing, "Ready/EditMagic"); // 제작 화면 진입
            Transform editor = briefing.Find("CraftedSpellEditor"); // 실제 제작 화면
            ClickPauseButton(editor, "PreviewFlight"); // 비동기 내장 연습 로드 요청
            ClickPauseButton(editor, "Back"); // 같은 프레임에 제작 화면 닫기
            yield return WaitFrames(90); // 요청된 씬의 로드와 취소 정리
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("FlightSortieBriefing")); // 원래 출격 준비 씬 유지
            Assert.That(SceneManager.GetSceneByName("FlightPrototype").isLoaded, Is.False); // 취소한 연습 씬 해제
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 메뉴의 게임 시간이 멈추지 않음
            Assert.That(briefing.Find("Ready").gameObject.activeSelf, Is.True); // 출격 준비 화면 정상 복귀
        }

        [UnityTest] // 저장 전 편집 조합으로 자유 훈련 후 편집 복귀
        public IEnumerator MagicDraftSurvivesFreeFlightWithoutSaving() // 시험 시전과 편집 내용 분리
        {
            string key = "ProjectG.Crafted.Draft." + System.Guid.NewGuid().ToString("N"); // 사용자 마법 저장과 분리한 시험 키
            string previous = System.Environment.GetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY"); // 이전 격리 환경 보존
            System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", key); // 실제 사용자 편성 제외
            try // 저장하지 않은 기본 조합의 독립 자유 훈련
            {
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 제작 화면이 있는 출격 준비
                yield return WaitFrames(4); // 실제 제작 버튼 연결
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 출격 준비 하이러키
                ClickPauseButton(briefing, "Ready/EditMagic"); // 제작 화면 열기
                Transform editor = briefing.Find("CraftedSpellEditor"); // 저장 전 조합을 편집할 화면
                Assert.That(PlayerPrefs.HasKey(key), Is.False); // 연습 전 별도 저장이 없는 상태
                ClickPauseButton(editor, "FreeFlight"); // 현재 조합으로 자유 비행 시작
                yield return WaitForActiveScene(ScenePath); // 실제 자유 비행 씬
                Transform hud = LocateCombatHud().transform; // 자유 훈련의 실제 HUD
                Transform card = hud.Find("FreeTraining/Options/Card"); // 훈련 표적과 복귀 메뉴
                ClickPauseButton(card, "Target"); // 실제 피해 판정 표적 시작
                yield return WaitFrames(4); // 비행과 시험 조합 연결
                Transform pilot = FindPilot(); // 실제 자유 비행 조종체
                ((Behaviour)pilot.GetComponent("FlightWeapon")).enabled = false; // 자동 사격의 간섭 차단
                Component caster = pilot.GetComponent("FlightCraftedSpellCaster"); // 실전과 같은 제작 발사기
                Component training = hud.Find("FreeTraining").GetComponent("FlightTrainingMode"); // 독립 훈련의 실제 상태
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 표적의 실제 상태 조회
                Component target = (Component)training.GetType().GetField("target", hidden).GetValue(training); // 정산과 분리된 실제 표적
                int health = Metric(target, "Health"); // 시험 전 표적 체력
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); // 편집한 조합의 실제 시험 입력
                yield return WaitFrames(110); // 실제 발사와 표적 충돌 대기
                Assert.That(Metric(caster, "CastCount"), Is.EqualTo(1)); // 저장 전 조합의 실제 시험 시전
                Assert.That(Metric(target, "Health"), Is.LessThan(health)); // 자유 비행 표적의 실제 피해
                Assert.That(PlayerPrefs.HasKey(key), Is.False); // 시험 발사만으로 마법 영구 저장 금지
                InputSystem.QueueStateEvent(mouse, new MouseState()); // 시험 클릭 해제
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F4)); // 자유 훈련 설정과 복귀 열기
                yield return WaitFrames(4); // 실제 복귀 버튼 표시
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 연습 설정 입력 해제
                ClickPauseButton(card, "Return"); // 이전 제작 화면으로 복귀
                yield return WaitForActiveScene("Assets/Scenes/FlightSortieBriefing.unity"); // 실제 출격 준비 씬 복원
                yield return WaitFrames(3); // 저장 전 편집 조합 복원
                briefing = GameObject.Find("SortieBriefing").transform; // 다시 열린 출격 준비
                editor = briefing.Find("CraftedSpellEditor"); // 같은 조합으로 복귀한 제작 화면
                Assert.That(editor.gameObject.activeSelf, Is.True); // 별도 저장 없이 편집 화면 복귀
                Assert.That(HudText(editor.Find("SpellName")), Does.Contain("파이어 볼트")); // 시험했던 조합의 이름 유지
                Assert.That(PlayerPrefs.HasKey(key), Is.False); // 복귀만으로 영구 저장 금지
            }
            finally // 이 검증에서 만든 설정만 정리
            {
                PlayerPrefs.DeleteKey(key); // 시험용 마법 저장 제거
                PlayerPrefs.Save(); // 시험 키 정리 반영
                System.Environment.SetEnvironmentVariable("PROJECTG_CRAFTED_TEST_KEY", previous); // 이전 격리 환경 복원
            }
        }

        [UnityTest] // 정식 출격과 분리된 자유 훈련의 판정, 회수와 복귀
        public IEnumerator FreeTrainingSelectsPatternsAndTargetsWithoutProgressRewards() // 실제 준비의 해당 조종사와 반복 조건
        {
            yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 실제 선택 화면
            yield return WaitFrames(4); // 훈련 버튼의 연결
            Transform ready = GameObject.Find("SortieBriefing").transform; // 출격과 학습을 나란히 고르는 화면
            ClickPauseButton(ready, "Ready/SelectLumi"); // 정식 선택과 같은 루미 조종사
            ClickPauseButton(ready, "Ready/FreeTraining"); // 새 연습 모드의 실제 버튼
            yield return WaitForActiveScene(ScenePath); // 기존 조작 연습과 같은 물리 씬
            Transform hud = LocateCombatHud().transform; // 현재 실제 연습 화면
            Transform options = hud.Find("FreeTraining/Options"); // 하이러키에 배치된 자유 훈련
            Transform card = options.Find("Card"); // 현재 반복과 시작 옵션
            Transform pilot = FindPilot(); // 연습에서 쓰는 실제 조종사
            Component weapon = pilot.GetComponent("FlightWeapon"); // 실제 루미 기술
            Assert.That(weapon.GetType().GetProperty("ActivePilot").GetValue(weapon).ToString(), Is.EqualTo("Lumi")); // 준비에서 선택한 조종사 유지
            Assert.That(options.gameObject.activeSelf && Time.timeScale == 0f, Is.True); // 첫 화면에서 선택 시간을 제공
            ClickPauseButton(card, "Repeat"); // 한 번만 발사하도록 전환
            Assert.That(HudText(card.Find("Repeat/Label")), Does.Contain("꺼짐")); // 상태와 실제 단추 연결
            ClickPauseButton(card, "Ring"); // 실제 입체 고리 선택
            yield return WaitFrames(5); // 풀에서 한 번 발사
            Assert.That(options.gameObject.activeSelf, Is.False); // 전투 시작과 하이러키 표시
            Assert.That(Metric(GetThreatPool(), "ActiveCount"), Is.EqualTo(12)); // 고리 한 번에 실제 탄막 열두 발
            Component practice = GameObject.Find("ThreatPractice").GetComponent("FlightThreatPractice"); // 실제 연습의 일정
            var active = practice.GetType().GetField("active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 반복 발사를 결정하는 상태
            Assert.That(active.GetValue(practice), Is.False); // 꺼짐으로 시작한 고리에는 추가 발사 없음
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F4)); // 실제 조작으로 다시 설정
            yield return WaitFrames(4); // 잠겨 있던 커서와 시계가 설정에 전달
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키를 놓아 다음 입력과 분리
            Assert.That(options.gameObject.activeSelf && Time.timeScale == 0f, Is.True); // 사용 중인 훈련의 재설정
            ClickPauseButton(card, "Target"); // 조준점의 실제 표적 선택
            yield return WaitFrames(4); // 기존 탄막과 초기 시점 정리
            Assert.That(Metric(GetThreatPool(), "ActiveCount"), Is.Zero); // 이전 고리 탄막 제거
            var mode = options.parent.GetComponent("FlightTrainingMode"); // 실제 설정이 연결한 연습 목표
            const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 씬에서 연결한 실제 값
            var target = (Component)mode.GetType().GetField("target", fields).GetValue(mode); // 원래 조준 연습 표적
            Assert.That(target.gameObject.activeInHierarchy, Is.True); // 실제 판정과 체력을 가진 표적 등장
            target.GetType().GetMethod("ReceiveHit").Invoke(target, new object[] { 5000 }); // 실제 드롭 진입을 검사할 통제 적중
            yield return WaitFrames(4); // 보상 상태가 바뀌지 않는지 관찰
            var experience = (Behaviour)mode.GetType().GetField("experience", fields).GetValue(mode); // 씬의 경험치 공급 통제
            var equipment = (Behaviour)mode.GetType().GetField("enhancements", fields).GetValue(mode); // 씬의 강화 공급 통제
            Assert.That(experience.enabled || equipment.enabled, Is.False); // 모드에서 두 드롭과 보상 공급이 정지
            Assert.That(Metric(experience, "Level"), Is.EqualTo(1)); // 적 격파로 실전 카드 재화가 생기지 않음
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F4)); // 현재 훈련을 다시 시작할 화면
            yield return WaitFrames(4); // 다시 시작 단추로 이동
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 프레스와 UI 조작 분리
            ClickPauseButton(card, "Reset"); // 현재 연습에 같은 조준 표적 복원
            yield return WaitFrames(4); // 실제 풀과 표적 공개
            Assert.That(target.gameObject.activeInHierarchy, Is.True); // 현재 모드를 유지한 다시 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F4)); // 자유 훈련 메뉴 재진입
            yield return WaitFrames(4); // 다음 키의 대상
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 설정만 닫기
            yield return WaitFrames(4); // 같은 ESC가 기존 정지 메뉴에 전달되는지 검사
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 누름 이벤트 정리
            yield return WaitFrames(2); // 가상 키가 해제된 프레임
            Assert.That(options.gameObject.activeSelf || hud.Find("PauseMenu").gameObject.activeSelf, Is.False); // ESC를 중복 처리하지 않음
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F4)); // 출격 준비로 되돌아갈 선택
            yield return WaitFrames(4); // 실제 복귀 버튼 연결
            ClickPauseButton(card, "Return"); // 현재 캐릭터와 상태 끊기
            yield return WaitForActiveScene("Assets/Scenes/FlightSortieBriefing.unity"); // 같은 실제 준비 메뉴
            Transform restored = GameObject.Find("SortieBriefing").transform; // 새로 열린 조종사 정보
            Assert.That(restored.Find("Ready/Portrait").GetComponent<RawImage>().texture.name, Is.EqualTo("LumiStanding")); // 연습에서 선택한 캐릭터 복원
        }

        [UnityTest] // 마을에서 거리 입력과 선택형 대화, 실제 시설 이용
        public IEnumerator VillageNpcDialogueBranchesAndUsesExistingServices() // 기존 보행과 따로 세 NPC 확인
        {
            yield return SceneManager.LoadSceneAsync("FlightIslandVillage"); // 실제 배포 마을
            yield return WaitFrames(5); // 준비한 3D 오브젝트와 키 입력 연결
            Transform hud = GameObject.Find("VillageHUD").transform; // 마을의 기존 장비 시설과 대화
            Transform display = hud.Find("NPCDialogue/Display"); // 실제 화면 하단 대사
            Transform walker = GameObject.Find("VillageWalker").transform; // 사용자의 3D 보행 조종사
            Transform world = GameObject.Find("VillageWorld").transform; // 실제 편집 가능한 마을 계층
            var photo = world.Find("NPCDialogueCamera").GetComponent<Camera>(); // 대화에 쓰는 별도의 실제 화면
            var overhead = GameObject.Find("Village Camera").GetComponent<Camera>(); // 기존 보행 카메라
            yield return TapVillage(Key.F); // 멀리서 주민과 상호작용 시도
            Assert.That(display.gameObject.activeSelf, Is.False); // 가까이 가지 않으면 대화 불가

            string[] residents = { "MarketKeeper", "ForgeAttendant", "SortieGuide" }; // 서쪽·동쪽·북쪽 실제 주민
            string[] speakers = { "상점 주인", "공방 담당자", "출격 안내원" }; // 원래 설정을 유지한 역할 이름
            for (int i = 0; i < residents.Length; i++) // F로 실제 대화를 세 장소에서 검증
            {
                Transform npc = world.Find("VillageNPCs/" + residents[i]); // 각 NPC의 실제 월드 배치
                Assert.That(npc.Find("StandingModel/Head").GetComponent<MeshRenderer>(), Is.Not.Null); // 3D 캐릭터 렌더러
                var movement = walker.GetComponent<CharacterController>(); // 검증할 위치에서 기존 충돌 정리
                movement.enabled = false; // 검증 위치 지정 전 캐릭터 제어기 잠시 끄기
                walker.position = npc.position - Vector3.forward * 2.1f; // 입구 쪽 실제 대화 반경의 위치
                movement.enabled = true; // 정상 도보·지형 입력 복원
                yield return WaitFrames(3); // 거리와 실제 F 안내 갱신
                Assert.That(HudText(hud.Find("TalkPrompt/Label")), Does.Contain(speakers[i])); // 가장 가까운 F 상호작용만 표시
                yield return TapVillage(Key.F); // 실제 키로 대사 시작
                Assert.That(display.gameObject.activeSelf && photo.enabled && !overhead.enabled, Is.True); // 확대된 3D NPC 시점
                Assert.That(Physics.Linecast(photo.transform.position, npc.Find("StandingModel/Head").position, 1 << 8, QueryTriggerInteraction.Ignore), Is.False); // 건물이나 지면이 얼굴을 가리지 않는 시점
                Assert.That(hud.Find("Walking keys").gameObject.activeSelf, Is.False); // 대화창에 기본 조작 설명이 비치지 않음
                Assert.That(HudText(display.Find("Speech/SpeakerPlate/Speaker")), Is.EqualTo(speakers[i])); // 현재 정확한 화자 이름
                Vector3 still = walker.position; // 대화 동안 기존 걷기가 잠긴 위치
                yield return TapVillage(Key.W); // 대사를 보면서 도보 키를 누름
                Assert.That(walker.position, Is.EqualTo(still)); // 마을 이동과 대화 입력의 분리
                Transform choices = display.Find("Choices"); // 다음과 선택 방식이 바뀌는 창
                Assert.That(choices.gameObject.activeSelf, Is.False); // 첫 인사의 여러 문장 진행
                for (int j = 0; j < 4 && !choices.gameObject.activeSelf; j++) // 타자 공개와 다음 대사의 실제 F
                {
                    yield return TapVillage(Key.F); // 글자 펼치기 또는 다음
                }
                Assert.That(choices.gameObject.activeSelf, Is.True); // 선택지가 실제로 열리는 상태
                if (i == 0) // 상점 주인의 분기형 이용 설명을 먼저 확인
                {
                    yield return TapVillage(Key.Digit2); // 이용 방법 바로 선택
                    yield return TapVillage(Key.F); // 새 문장의 나머지 공개
                    Assert.That(HudText(display.Find("Speech/Body")), Does.Contain("공용 스킬")); // 첫 인사와 다른 안내 문장
                    yield return TapVillage(Key.Escape); // 대화를 종료하고 실제 도보로 돌아가기
                    Assert.That(!display.gameObject.activeSelf && overhead.enabled && !photo.enabled, Is.True); // 기존 시점과 키 복원
                    yield return TapVillage(Key.F); // 같은 주민과 새 인사로 다시 시작
                    for (int j = 0; j < 4 && !choices.gameObject.activeSelf; j++) // 선택지를 다시 실제 입력으로 열기
                    {
                        yield return TapVillage(Key.F); // 대사의 타자 효과 건너뛰기
                    }
                }
                yield return TapVillage(Key.Digit1); // 실제 시설을 열기
                if (i < 2) // 마을 안의 두 시설
                {
                    Assert.That(GameObject.Find(i == 0 ? "Market" : "Forge"), Is.Not.Null); // 기존 재고·강화 UI가 실제로 열림
                    Assert.That(display.gameObject.activeSelf, Is.False); // 시설을 열면 대화 닫기
                    if (i == 1) // 강화 공방의 제작 연구 연결
                    {
                        Transform forge = GameObject.Find("Forge").transform; // 실제로 열린 강화 공방
                        ClickPauseButton(forge, "OpenResearch"); // 공방에서 공용 마법 연구 열기
                        Transform research = GameObject.Find("ResearchPanel").transform; // 실제 연구 화면
                        Assert.That(research.gameObject.activeSelf, Is.True); // 공방에서 연구 화면 표시
                        ClickPauseButton(research, "Close"); // 공방으로 돌아가기
                        Assert.That(research.gameObject.activeSelf, Is.False); // 공방 복귀 시 연구 화면 종료
                    }
                    yield return TapVillage(Key.Escape); // 시설을 닫고 그대로 다음 마을 위치로
                }
            }
            yield return WaitForActiveScene("Assets/Scenes/FlightSortieBriefing.unity"); // 안내원의 실제 준비 화면 이동
        }

        private IEnumerator TapVillage(Key key) // 실제 Unity 프레임에 정상 전달하는 단축키
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 사용자 OS와 다른 가상 키 상태
            yield return WaitFrames(3); // 이번 화면의 Update
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            yield return WaitFrames(3); // 다음 대사로 들어갈 수 있는 별도 입력
        }

        [UnityTest] // 두 탐험 분기와 별도 저장·성장·실패 확인
        public IEnumerator ExpeditionRestoresItsCheckpointAndKeepsTemporaryGrowthSeparate() // 실제 F6와 전투 씬 왕복
        {
            const string flag = "PROJECTG_MENU_TEST_KEY"; // 일반 설정과 분리한 저장 경로
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; // 실제 피격과 자동탄 강화 읽기
            string prior = System.Environment.GetEnvironmentVariable(flag); // 기존 저장 환경 복원용
            string root = "ProjectG.Expedition.Test." + System.Guid.NewGuid().ToString("N"); // 이번 탐험만의 기록
            string record = root + ".Expedition.v1"; // 지도 체크포인트 한 개
            System.Environment.SetEnvironmentVariable(flag, root); // 실제 사용자 설정과 분리
            System.Type store = System.Type.GetType("ProjectG.FlightExpedition, Assembly-CSharp", true); // 실제 출격과 공유하는 저장 클래스
            var reset = store.GetMethod("ResetSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); // 실제 새 실행 초기화
            reset.Invoke(null, null); // 이전 지도 세션의 영향 제외
            try
            {
                PlayerPrefs.SetInt(root + ".Pilot", 1); // 다른 본편 조종사의 저장
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 사용자가 여는 편성 씬
                yield return WaitFrames(5); // 모드 버튼과 장비 준비
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 실제 탐험으로 연결하는 출격 준비
                ClickPauseButton(briefing, "Ready/SelectIris"); // 현재 탐험의 조종사
                yield return TapVillage(Key.F6); // 실제 탐험 지름길
                Transform map = briefing.Find("ExpeditionMap"); // 씬에 배치된 다섯 지점
                Assert.That(map.gameObject.activeSelf, Is.True); // F6에 현재 지도 표시
                ClickPauseButton(map, "Card/Primary"); // 이리스의 새 탐험
                ClickPauseButton(map, "Card/Primary"); // 항구를 떠나 경로 선택
                Assert.That(HudText(map.Find("Card/Detail")), Does.Contain("우회")); // 아직 선택 가능한 갈림길
                yield return TapVillage(Key.Digit2); // 우회 보급을 고르기
                Assert.That(HudText(map.Find("Card/Summary")), Does.Contain("보급표 1장")); // 우회 경로의 시험값
                ClickPauseButton(map, "Card/Secondary"); // 탐험 보급표로 자동탄 구매
                Assert.That(HudText(map.Find("Card/Summary")), Does.Contain("보급표 0장")); // 같은 재화 한 장만 소비
                Assert.That(HudText(map.Find("Card/Summary")), Does.Contain("+20%")); // 구매 및 실제 강화 안내
                Assert.That(map.Find("Card/Secondary").GetComponent<Button>().interactable, Is.False); // 같은 상품 중복 구매 차단
                ClickPauseButton(map, "Card/Primary"); // 최종 전투 입장 전 저장
                ClickPauseButton(map, "Card/Close"); // 체크포인트를 보존한 상태로 편성 복귀
                Assert.That(map.gameObject.activeSelf, Is.False); // 원래 출격 버튼으로 돌아옴
                string checkpoint = PlayerPrefs.GetString(record); // 게임 재실행에 쓰는 실제 직렬 데이터
                Assert.That(checkpoint, Does.Contain("\"power\":1")); // 첫 임시 강화를 저장
                reset.Invoke(null, null); // 임시 메모리가 없는 새 실행 재현
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 같은 실제 준비 씬 다시 열기
                yield return WaitFrames(5); // 초기 설정
                briefing = GameObject.Find("SortieBriefing").transform; // 새로 읽은 준비 화면
                ClickPauseButton(briefing, "Ready/SelectAria"); // 저장된 탐험 조종사를 바꾸지 않는 일반 선택
                ClickPauseButton(briefing, "Ready/Expedition"); // 마우스로도 같은 체크포인트
                map = briefing.Find("ExpeditionMap"); // 실제 복구한 지도
                Assert.That(HudText(map.Find("Card/Summary")), Does.Contain("이리스")); // 첫 탐험 조종사 유지
                Assert.That(HudText(map.Find("Card/Summary")), Does.Contain("+20%")); // 임시 성장 저장은 복원
                Assert.That(HudText(map.Find("Card/Primary/Label")), Does.Contain("최종 전투")); // 최종 전투 직전 지점
                ClickPauseButton(map, "Card/Primary"); // 실제 탐험의 최종 보스
                yield return WaitForActiveScene(OuterIslandsPath); // 같은 하늘섬 지형
                Transform pilot = FindPilot(); // 탐험의 이리스
                Component weapon = pilot.GetComponent("FlightWeapon"); // 실제 자동탄 배율 창구
                Assert.That(weapon.GetType().GetProperty("ExpeditionDamageScale", hidden).GetValue(weapon), Is.EqualTo(1.2f).Within(.001f)); // 구매한 값이 실제 피해로 전달
                Component timeline = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 최종 노드 시간표
                Time.captureDeltaTime = .1f; // 기존 전투 시간 진행
                yield return AdvanceStageTo(timeline, 4.5f); // 최종 보스만 나오는 시험 노드
                Assert.That((bool)timeline.GetType().GetProperty("IsFinalBossActive").GetValue(timeline), Is.True); // 본편의 일반 적 구간 건너뜀
                Component vitals = pilot.GetComponent("FlightPilotVitals"); // 실제 피격과 실패
                vitals.GetType().GetMethod("ResolveThreat", hidden).Invoke(vitals, new object[] { Vector3.zero, Vector3.forward, .1f, 200, false, false }); // 패배 후 임시 진행 초기화
                yield return WaitFrames(6); // 실제 결과 기록 전환
                Assert.That(PlayerPrefs.HasKey(record), Is.False); // 실패하면 임시 저장 제거
                ClickPauseButton(LocateCombatHud().transform, "SortieEnd/Open"); // 탐험 지도로 복귀
                yield return WaitForActiveScene("Assets/Scenes/FlightSortieBriefing.unity"); // 결과 대신 복구한 UI
                briefing = GameObject.Find("SortieBriefing").transform; // 탐험 지도와 편성의 실제 계층
                map = briefing.Find("ExpeditionMap"); // 전투 정산에서 자동으로 열린 지도
                Assert.That(map.gameObject.activeSelf, Is.True); // 일괄 정산 뒤 바로 탐험
                Assert.That(HudText(map.Find("Card/Notice")), Does.Contain("탐험 실패")); // 결과와 초기화 표시
                Assert.That(HudText(map.Find("Card/Summary")), Does.Contain("+0%")); // 새 출발에는 강화 없음

                ClickPauseButton(map, "Card/Primary"); // 처음부터 다시 탐험 시작
                ClickPauseButton(map, "Card/Primary"); // 이번에는 교전을 선택하는 갈림길
                ClickPauseButton(map, "Card/Primary"); // 실제 일반 마물 웨이브로 입장
                yield return WaitForActiveScene(OuterIslandsPath); // 모드의 다른 실제 전투
                Transform encounter = GameObject.Find("EnemyEncounter").transform; // 실제 장면의 시험 경로
                timeline = encounter.GetComponent("FlightEnemyEncounter"); // 보스 없는 두 마물 배치
                yield return AdvanceStageTo(timeline, 2.2f); // 첫 떼 등장
                Assert.That(Metric(timeline, "ActiveEnemyCount"), Is.EqualTo(2)); // 실제 첫 일반 마물 둘
                Assert.That(HudText(LocateCombatHud().transform.Find("WaveReadout/Wave")), Does.Contain("탐험")); // 기존 웨이브와 다른 탐험 상태를 표시
                foreach (Transform route in encounter) // 현재 실제 마물만 빠르게 종료
                {
                    if (route.gameObject.activeSelf && route.GetComponent("FlightEnemy") is Component enemy) // 장면의 적과 실제 피해
                    {
                        enemy.GetType().GetMethod("ReceiveHit").Invoke(enemy, new object[] { 2000 }); // 제어 피해로 교전 진행
                    }
                }
                yield return AdvanceStageTo(timeline, 10.3f); // 다음 웨이브 진입
                Assert.That(Metric(timeline, "WaveNumber"), Is.EqualTo(2)); // 두 번째 설정 배치 사용
                Component diver = encounter.Find("DiverEnemy").GetComponent("FlightEnemy"); // 둘째 배치의 통제할 마물
                diver.GetType().GetMethod("ReceiveHit").Invoke(diver, new object[] { 2000 }); // 교전 완료 조건의 실제 처치
                yield return AdvanceStageTo(timeline, 18.9f); // 구간 종료 직전의 실제 시간
                yield return WaitFrames(5); // 전투가 끝나며 고정되는 시계
                Assert.That((bool)timeline.GetType().GetProperty("IsStageComplete").GetValue(timeline), Is.True); // 결과 버튼으로 넘어갈 실제 완료
                ClickPauseButton(LocateCombatHud().transform, "SortieEnd/Open"); // 우회와 같은 임시 상점으로
                yield return WaitForActiveScene("Assets/Scenes/FlightSortieBriefing.unity"); // 지도에 교전 성과 복원
                map = GameObject.Find("SortieBriefing").transform.Find("ExpeditionMap"); // 다른 분기의 실제 결과
                Assert.That(HudText(map.Find("Card/Summary")), Does.Contain("보급표 2장")); // 교전에만 보급표 2장
                Assert.That(HudText(map.Find("Card/Summary")), Does.Contain("+20%")); // 같은 탐험 임시 성장
                Assert.That(PlayerPrefs.GetInt(root + ".Pilot"), Is.EqualTo(1)); // 본편 조종사 저장 보존
            }
            finally
            {
                reset.Invoke(null, null); // 다른 PlayMode 테스트에 임시 기록 영향 제거
                PlayerPrefs.DeleteKey(record); // 이번 독립 실험만 정리
                PlayerPrefs.DeleteKey(root + ".Pilot"); // 고유 본편 조종사 시험 값 삭제
                System.Environment.SetEnvironmentVariable(flag, prior); // 호출 전 저장 경로 복원
            }
        }

        [UnityTest] // 두 보스와 모드별 정산을 실제 배포 씬에서 확인
        public IEnumerator BossRushPreservesTheNormalSaveAndRecordsOnlyClears() // 시제품의 회복·페이즈·같은 모드 재도전
        {
            const string flag = "PROJECTG_MENU_TEST_KEY"; // 현재 설정과 분리한 테스트 경로
            string prior = System.Environment.GetEnvironmentVariable(flag); // 이전 테스트 환경의 저장 값
            string root = "ProjectG.BossRush.Test." + System.Guid.NewGuid().ToString("N"); // 이 조종사의 기록과 이어서하기
            string recordKey = root + ".BossRush.Best.2"; // 루미의 실제 최고 기록
            System.Environment.SetEnvironmentVariable(flag, root); // 실제 사용자 설정과 분리
            try
            {
                PlayerPrefs.SetString(root + ".Resume", "ExistingStory"); // 기존의 본편 계속하기 기록
                PlayerPrefs.SetInt(root + ".Pilot", 1); // 일반 출격을 마지막으로 한 리안
                yield return SceneManager.LoadSceneAsync("FlightSortieBriefing"); // 기존 출격 준비
                yield return WaitFrames(3); // 버튼과 조종사 연결 대기
                Transform briefing = GameObject.Find("SortieBriefing").transform; // 현재 장비와 마법의 준비 세트
                ClickPauseButton(briefing, "Ready/SelectLumi"); // 저장된 본편 조종사와 다른 루미
                ClickPauseButton(briefing, "Ready/BossRush"); // 실제 별도 모드 버튼
                yield return WaitForActiveScene(OuterIslandsPath); // 실제 보스가 있는 항로
                Transform pilot = FindPilot(); // 현재 조종사 기체
                Component weapon = pilot.GetComponent("FlightWeapon"); // 명중 제어와 선택한 조종사
                Assert.That(weapon.GetType().GetProperty("ActivePilot").GetValue(weapon).ToString(), Is.EqualTo("Lumi")); // 실제 조종사 전달
                Assert.That(BroomAction(pilot.GetComponent("FlightBroomEquipment"), "GetEquippedId", "Lumi", "Body"), Does.StartWith("lumi.")); // 현재 장비 편성 전달
                ((Behaviour)weapon).enabled = false; // 우연한 처치와 분리한 보스 타이밍 검증
                Component vitals = pilot.GetComponent("FlightPilotVitals"); // 단일 피해와 회복의 실제 대상
                Component skills = pilot.GetComponent("FlightSkillController"); // 마력과 필살기
                Transform encounter = GameObject.Find("EnemyEncounter").transform; // 본편 보스의 기존 배치
                Component timeline = encounter.GetComponent("FlightEnemyEncounter"); // 별도 시간표
                Assert.That(timeline.GetType().GetProperty("IsBossRush").GetValue(timeline), Is.EqualTo(true)); // 준비에서 선택한 모드
                Assert.That(PlayerPrefs.GetString(root + ".Resume"), Is.EqualTo("ExistingStory")); // 일반 이어서하기 보존
                Assert.That(PlayerPrefs.GetInt(root + ".Pilot"), Is.EqualTo(1)); // 기존 조종사 보존
                Transform combat = LocateCombatHud().transform; // 보스 러시에서도 쓰는 실제 전투 HUD
                Assert.That(combat.Find("FirstIncidentBroadcast/Transmission").gameObject.activeSelf, Is.False); // 본편 통신 분리
                Time.captureDeltaTime = 0.1f; // 보스 사이 시간을 합리적으로 단축한 테스트
                yield return AdvanceStageTo(timeline, 3.7f); // 첫 전투는 중간 보스
                Transform middle = encounter.Find("MidBoss"); // 본편과 동일한 실제 보스
                Assert.That(middle.gameObject.activeSelf, Is.True); // 일반 웨이브 없는 첫 전투
                Assert.That(Metric(timeline, "WaveNumber"), Is.EqualTo(1)); // 모드 전용 첫 출현
                Assert.That(Metric(timeline, "ActiveEnemyCount"), Is.EqualTo(1)); // 보스 한 개체
                const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 시험용 피격과 마나 상태
                var pilotHit = vitals.GetType().GetMethod("ResolveThreat", hidden); // 게임의 공통 피해 함수
                pilotHit.Invoke(vitals, new object[] { Vector3.zero, Vector3.forward, .1f, 35, false, false }); // 회복 전에 실제 피해 발생
                Assert.That(Metric(vitals, "Health"), Is.LessThan(Metric(vitals, "MaxHealth"))); // 실제 회복할 체력
                skills.GetType().GetField("mana", hidden).SetValue(skills, 10f); // 시험용 마나 소진
                skills.GetType().GetField("ultimate", hidden).SetValue(skills, 25f); // 회복 구간에서 잃지 않을 충전
                Component mid = middle.GetComponent("FlightEnemy"); // 실제 보스 피격과 정산
                mid.GetType().GetMethod("ReceiveHit").Invoke(mid, new object[] { 900 }); // 통제된 중간 보스 격파
                float defeated = StageTime(timeline, "ElapsedSeconds"); // 최종 보스 출현 기준
                Assert.That(Metric(vitals, "Health"), Is.EqualTo(Metric(vitals, "MaxHealth"))); // 중간 회복 조건
                Assert.That(Metric(skills, "Mana"), Is.EqualTo(Metric(skills, "MaxMana"))); // 최대 마력
                Assert.That(Metric(skills, "Ultimate"), Is.GreaterThanOrEqualTo(25)); // 앞에서 충전한 값 보존
                Assert.That(Metric(GetThreatPool(), "ActiveCount"), Is.Zero); // 앞 보스 적탄 정리
                Assert.That(((Behaviour)GameObject.Find("EnhancementDrops").GetComponent("FlightEnhancements")).enabled, Is.False); // 강화 드롭 제외
                Assert.That(PlayerPrefs.HasKey(recordKey), Is.False); // 중간 격파로 최고 기록을 만들지 않음
                vitals.GetType().GetMethod("GrantProtection").Invoke(vitals, new object[] { 40f }); // 최종 보스 단계 검증의 통제 보호
                yield return AdvanceStageTo(timeline, defeated + 2.3f); // 준비 여유 내 기존 탄막 없음
                Transform final = encounter.Find("FinalBoss"); // 현재 최종 보스 오브젝트
                Assert.That(final.gameObject.activeSelf, Is.False); // 2.6초 전에는 최종 보스 미출현
                yield return AdvanceStageTo(timeline, defeated + 4.75f); // 최종 보스가 실제로 입장
                Component finalLife = final.GetComponent("FlightEnemy"); // 체력 단계와 공통 충돌
                Component finalPhases = final.GetComponent("FlightFinalBoss"); // 실제 보스 패턴
                Assert.That(Metric(finalPhases, "Phase"), Is.EqualTo(1)); // 첫 페이즈에서 시작
                Assert.That(finalPhases.GetType().GetProperty("IsEntering").GetValue(finalPhases), Is.EqualTo(false)); // 진입 중에는 피해를 받지 않는 기존 규칙
                var finalHit = finalLife.GetType().GetMethod("ReceiveHit"); // 자동 사격과 동일한 피해 진입점
                for (int phase = 2; phase <= 3; phase++) // 두 체력 경계가 차례로 활성화
                {
                    finalHit.Invoke(finalLife, new object[] { 2000 }); // 단일 피해로도 단계 건너뛰기 방지
                    yield return AdvanceStageTo(timeline, StageTime(timeline, "ElapsedSeconds") + 1.65f); // 다음 패턴이 시작될 때까지 진행
                    Assert.That(Metric(finalPhases, "Phase"), Is.EqualTo(phase)); // 원래 보스의 실제 페이즈
                }

                finalHit.Invoke(finalLife, new object[] { 600 }); // 실제 최종 클리어
                yield return WaitFrames(4); // 정산과 기록 저장
                int best = PlayerPrefs.GetInt(recordKey); // 사용자 기록 저장 경로
                Assert.That(best, Is.GreaterThan(0)); // 실제로 완료한 기록만 저장
                ClickPauseButton(combat.Find("SortieEnd"), "Open"); // 기존 결과 씬 진입
                yield return WaitForActiveScene("Assets/Scenes/FlightSortieResult.unity"); // 좌우로 분리한 결과
                Transform result = GameObject.Find("SortieResult").transform; // 조종사와 기록의 실제 결과
                Assert.That(HudText(result.Find("Panel/Outcome")), Is.EqualTo("보스 러시 완료")); // 다른 모드의 제목
                Assert.That(HudText(result.Find("Panel/Rewards")), Does.Contain("신기록")); // 저장한 실제 백분의 일 초
                Assert.That(HudText(result.Find("Panel/Level")), Is.EqualTo("LV. 1")); // XP 없이 처음 조합의 레벨 보존
                Assert.That(result.Find("PortraitArea/StandingIllustration").GetComponent<RawImage>().texture.name, Is.EqualTo("LumiStanding")); // 실제 도전 조종사의 스탠딩
                ClickPauseButton(result, "Panel/Retry"); // 같은 모드로 다시 시작
                yield return WaitForActiveScene(OuterIslandsPath); // 다시 두 보스의 별도 항로
                Transform retryPilot = FindPilot(); // 같은 캐릭터로 새로 시작
                Assert.That(retryPilot.GetComponent("FlightWeapon").GetType().GetProperty("ActivePilot").GetValue(retryPilot.GetComponent("FlightWeapon")).ToString(), Is.EqualTo("Lumi")); // 다른 캐릭터 혼입 방지
                Component retry = GameObject.Find("EnemyEncounter").GetComponent("FlightEnemyEncounter"); // 신규 도전의 모드
                Assert.That(retry.GetType().GetProperty("IsBossRush").GetValue(retry), Is.EqualTo(true)); // 본편으로 돌아가지 않음
                pilotHit.Invoke(retryPilot.GetComponent("FlightPilotVitals"), new object[] { Vector3.zero, Vector3.forward, .1f, 120, false, false }); // 빠른 패배도 같은 판정
                yield return WaitFrames(4); // 실제 실패 기록 시점
                Assert.That(PlayerPrefs.GetInt(recordKey), Is.EqualTo(best)); // 실패가 성공 기록을 덮지 않음
                Assert.That(PlayerPrefs.GetString(root + ".Resume"), Is.EqualTo("ExistingStory")); // 다시하기와 실패도 본편을 보존
                Assert.That(PlayerPrefs.HasKey(root + ".BossRush.Best.1"), Is.False); // 다른 조종사의 기록과 분리
            }
            finally
            {
                PlayerPrefs.DeleteKey(recordKey); // 테스트 최고 기록 삭제
                PlayerPrefs.DeleteKey(root + ".Resume"); // 독립 시험 계속하기 삭제
                PlayerPrefs.DeleteKey(root + ".Pilot"); // 독립 시험 조종사 선택 삭제
                System.Environment.SetEnvironmentVariable(flag, prior); // 원래 저장 환경 복귀
            }
        }

        [UnityTest] // 실패 결과와 같은 캐릭터의 새 출격
        public IEnumerator SortieResultRetriesSamePilotAndClearsOldCombatState() // 전투 → 결과 → 재출격
        {
            yield return LoadOuterIslands(); // 정상 출격 항로
            yield return SelectRian(); // 실제 리안 전투 프로필
            Component vitals = FindPilot().GetComponent("FlightPilotVitals"); // 실제 체력과 피격
            var hit = vitals.GetType().GetMethod("ResolveThreat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // 탄환의 실제 피격 처리
            hit.Invoke(vitals, new object[] { Vector3.zero, Vector3.forward, 0.1f, 120, false, false }); // 초기 체력을 넘어서는 단일 시험 피격
            yield return WaitFrames(4); // 실패 상태와 HUD 갱신
            Assert.That(Metric(vitals, "Health"), Is.Zero); // 테스트가 실제 실패 상태에 도달
            Transform hud = LocateCombatHud().transform; // 실제 출격 HUD
            Transform finished = hud.Find("SortieEnd"); // 실패 후 결과 확인 화면
            Assert.That(finished, Is.Not.Null); // 실제 씬에 편집된 결과 열기
            Assert.That(finished.gameObject.activeSelf, Is.True); // 사망한 출격에 결과 화면 표시
            ClickPauseButton(finished, "Open"); // 화면에서 결과로 이동
            const string resultPath = "Assets/Scenes/FlightSortieResult.unity"; // 별도의 결과 씬
            yield return WaitForActiveScene(resultPath); // 실제 씬을 완전히 열기
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(resultPath)); // 다른 씬으로 이동 확인
            Transform result = GameObject.Find("SortieResult").transform; // 실제 편집한 결과 오브젝트
            Assert.That(GameObject.Find("FlightAudio/Music").GetComponent<AudioSource>().clip.name, Is.EqualTo("StageFailed")); // 실제 실패 결과곡
            Assert.That(HudText(result.Find("Panel/Outcome")), Is.EqualTo("출격 실패")); // 실패와 승리 구분
            var standing = result.Find("PortraitArea/StandingIllustration"); // 출격한 캐릭터의 실제 결과 그림
            Assert.That(standing, Is.Not.Null); // 결과 씬에서 편집할 왼쪽 일러스트
            Assert.That(standing.GetComponent<RawImage>().texture.name, Is.EqualTo("RianStanding")); // 기본 조종사와 다른 실제 출격 캐릭터
            var left = (RectTransform)standing.parent; // 인물 전신의 왼쪽 부모
            var right = (RectTransform)result.Find("Panel"); // 전투 기록의 오른쪽 프레임
            Assert.That(left.anchoredPosition.x + left.rect.width * 0.5f, Is.LessThan(right.anchoredPosition.x - right.rect.width * 0.5f)); // 인물과 수치가 겹치지 않는 좌우 구도
            Assert.That(HudText(result.Find("Panel/Pilot")), Does.Contain("리안")); // 끝난 같은 캐릭터
            ClickPauseButton(result, "Panel/Retry"); // 기존 조합으로 즉시 재출격
            yield return WaitForActiveScene(OuterIslandsPath); // 같은 항로를 새로운 런으로 시작
            Transform pilot = FindPilot(); // 재출격에서 새로 만들어진 캐릭터
            Assert.That(Metric(pilot.GetComponent("FlightPilotVitals"), "Health"), Is.EqualTo(100)); // 실패 전의 체력 상태 초기화
            Component weapon = pilot.GetComponent("FlightWeapon"); // 리안의 실제 슬롯과 조준
            Assert.That(weapon.GetType().GetProperty("IsRian").GetValue(weapon), Is.True); // 같은 조종사로 재출격
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 결과 전환에서 제거한 일시정지 상태
            Assert.That(LocateCombatHud().transform.Find("SortieEnd").gameObject.activeSelf, Is.False); // 이전 결과 화면 제거
        }

        [UnityTest] // 두 속사 후속탄과 리안의 약점 타격 후 재사용 감소
        public IEnumerator VerifiedGrowthEffectsFireAndRespectTheirLimits() // 선택한 실제 후속 카드의 효과 실행 검증
        {
            yield return LoadOuterIslands(); // 실제 3D 조종사와 장면의 마물
            Transform pilot = FindPilot(); // 이번 출격 조종사
            Component weapon = pilot.GetComponent("FlightWeapon"); // 실제 자동 발사 콜백
            Component upgrades = pilot.GetComponent("FlightUpgradeChoices") ?? LocateCombatHud().GetComponent("FlightUpgradeChoices"); // 씬의 실제 카드 적용
            if (upgrades == null) // 카드가 하이러키 다른 곳에 있는 경우
            {
                upgrades = Object.FindFirstObjectByType(System.Type.GetType("ProjectG.FlightUpgradeChoices, Assembly-CSharp")) as Component; // 배치한 카드 실제 컴포넌트
            }
            Assert.That(upgrades, Is.Not.Null); // 실제 실전 카드 데이터 확인
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; // 실제 선택 및 적중의 관찰
            var reward = upgrades.GetType().GetNestedType("Reward", System.Reflection.BindingFlags.NonPublic); // 선택한 효과의 실제 항목
            var apply = upgrades.GetType().GetMethod("Apply", hidden); // 선택되었을 때 같은 효과 적용
            var boltType = System.Type.GetType("ProjectG.FlightSpellBolt, Assembly-CSharp"); // 실제 플레이어 탄 개체
            ((Behaviour)weapon).enabled = false; // 기본 자동탄으로 인한 측정 오차 방지
            Component rapid = pilot.GetComponent("FlightRapidFire"); // 실제 연속 적중에 있는 보너스
            var hit = rapid.GetType().GetMethod("RecordHit"); // 이전 자동·집중 사격의 실제 적중 누적
            for (int i = 0; i < 30; i++) // 후속탄의 +30% 규칙을 시험
            {
                hit.Invoke(rapid, null); // 실제 패시브 누적 함수
            }
            Component enemy = GameObject.Find("EnemyEncounter").transform.Find("AimedEnemy").GetComponent("FlightEnemy"); // 현재 씬의 일반 마물
            enemy.gameObject.SetActive(true); // 시험 대상으로 활성화
            enemy.transform.position = pilot.position + Vector3.forward * 11f; // 추가탄 범위 안의 실제 위치
            apply.Invoke(upgrades, new[] { System.Enum.Parse(reward, "ChaseSecond") }); // 확인된 추격 두 번째 노드 효과
            var ariaHit = weapon.GetType().GetMethod("OnAriaHit", hidden); // 물리 적중이 전달하는 실제 처리
            int shots = Object.FindObjectsByType(boltType, FindObjectsSortMode.None).Length; // 기존 탄 수 기준
            for (int i = 0; i < 10; i++) // 한 대상 두 묶음의 같은 프레임 적중
            {
                ariaHit.Invoke(weapon, new object[] { enemy, false }); // 같은 실제 마물의 피해 관찰
            }
            Assert.That(Object.FindObjectsByType(boltType, FindObjectsSortMode.None).Length, Is.EqualTo(shots + 1)); // 다섯 회 충족과 초당 한 번 제한
            apply.Invoke(upgrades, new[] { System.Enum.Parse(reward, "SpreadSecond") }); // 선택한 확산 후속 카드
            weapon.GetType().GetMethod("BeginRapidFire").Invoke(weapon, null); // 실제 Q의 집중 포화 상태 시작
            var nextSpread = weapon.GetType().GetField("nextSpread", hidden); // 첫 0.5초 간격의 발사 시각
            float firstPulse = (float)nextSpread.GetValue(weapon); // 초기 후속탄의 예정 시각
            ((Behaviour)weapon).enabled = true; // 실제 LateUpdate에서 자동 발사
            yield return WaitFrames(35); // 첫 후속탄 시각을 넘겨 다음 0.5초 간격 확인
            Assert.That((float)nextSpread.GetValue(weapon), Is.EqualTo(firstPulse + 0.5f).Within(0.05f)); // 실제 집중 포화의 후속 발사 처리
            yield return SelectRian(); // 같은 준비 구간에서 실제 리안 프로필로 변경
            apply.Invoke(upgrades, new[] { System.Enum.Parse(reward, "WeakSecond") }); // 선택한 약점 사냥의 후속 효과
            Component skills = pilot.GetComponent("FlightSkillController"); // 실제 Q 쿨타임 저장
            var readyAt = (float[])skills.GetType().GetField("readyAt", hidden).GetValue(skills); // 현재 재사용 가능 시각
            readyAt[0] = Time.time + 6f; // 약점에 맞을 때 감소시킬 실제 Q 재사용
            float prior = readyAt[0]; // 초당 한 번 감소 전의 값
            var rianHit = weapon.GetType().GetMethod("OnRianHit", hidden); // 실제 약점 적중 콜백
            rianHit.Invoke(weapon, new object[] { enemy, false }); // 몸통 적중은 감소 대상 제외
            Assert.That(readyAt[0], Is.EqualTo(prior)); // 실제 몸통 피해는 Q 시각 유지
            rianHit.Invoke(weapon, new object[] { enemy, true }); // 첫 유효 약점 피해
            rianHit.Invoke(weapon, new object[] { enemy, true }); // 같은 순간의 중복 약점
            Assert.That(readyAt[0], Is.EqualTo(prior - 0.2f).Within(0.001f)); // 초당 한 번과 정확한 0.2초
        }

        [UnityTest] // 선택형 첫 출격 훈련과 실제 입력
        public IEnumerator FirstSortieGuideSupportsActualControlsAndSkip() // 실제 훈련에서 건너뛰고 준비로 복귀
        {
            const string briefingPath = "Assets/Scenes/FlightSortieBriefing.unity"; // 정식 출격 준비
            yield return SceneManager.LoadSceneAsync(SceneUtility.GetBuildIndexByScenePath(briefingPath)); // 실제 안내 진입의 기본 화면
            yield return null; // 장비와 조종사 버튼의 연결 완료
            Transform briefing = GameObject.Find("SortieBriefing").transform; // 준비 화면 하이러키
            Assert.That(briefing.Find("Ready/FirstSortieGuide"), Is.Not.Null); // 독립된 첫 비행 연습 메뉴
            ClickPauseButton(briefing, "Ready/SelectRian"); // 저격형도 같은 실전 사격 판정을 사용하는 훈련
            ClickPauseButton(briefing, "Ready/FirstSortieGuide"); // 실제 안내 선택
            yield return WaitForActiveScene(ScenePath); // 정식 출격 대신 연습 항로
            Transform guide = LocateCombatHud().transform.Find("FirstSortieGuide"); // 연습 안에서만 보이는 설명
            Assert.That(guide.gameObject.activeSelf, Is.True); // 선택한 경우에만 안내 표시
            Assert.That(HudText(guide.Find("Step")), Does.Contain("이동")); // 조작 전 첫 연습
            SetKeys(Key.W); // 실제 전진 조작
            yield return WaitFrames(32); // 움직인 거리에 따라 다음 연습 진행
            SetKeys(); // 이동 관성 정리
            Assert.That(HudText(guide.Find("Step")), Does.Contain("집중")); // 키 설명만 표시하는 흐름과 구분
            SetKeys(Key.LeftShift); // 기본 저장 키의 실제 집중 비행
            yield return WaitFrames(33); // 집중 유지 기준 통과
            SetKeys(); // 다음 사격 연습 진행
            Assert.That(HudText(guide.Find("Step")), Does.Match("자동 사격|그레이즈|스킬")); // 자동 사격 또는 이후 연습 진입
            yield return WaitFrames(270); // 자동 마법탄과 그레이즈의 물리적 판정 확인
            Assert.That(HudText(guide.Find("Step")), Does.Contain("스킬")); // 실제 적중과 그레이즈를 거쳐 마지막 안내
            ClickPauseButton(guide, "Skip"); // 안내를 건너뛰고 안전한 준비 화면으로
            yield return WaitForActiveScene(briefingPath); // 이전 선택 조종사의 준비 복원
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(briefingPath)); // 전투와 섞이지 않은 독립 훈련
            Assert.That(Time.timeScale, Is.EqualTo(1f)); // 이전 훈련의 시간과 UI 상태 제거
            yield return LoadOuterIslands(); // 실제 출격 경로도 다시 확인
            Assert.That(LocateCombatHud().transform.Find("FirstSortieGuide"), Is.Null); // 실전의 조작 설명 패널 삭제 요구 유지
        }

        private static IEnumerator WaitForActiveScene(string scenePath) // 프레임 수에 의존하지 않는 씬 전환 확인
        {
            float limit = Time.realtimeSinceStartup + 4f; // 허용되는 최대 로딩 시간
            while (SceneManager.GetActiveScene().path != scenePath && Time.realtimeSinceStartup < limit) // 현재 씬이 새로 바뀔 때까지 대기
            {
                yield return null; // 비동기 로드의 다음 프레임
            }
            yield return null; // 새 씬에서 메뉴와 버튼의 Start 실행
        }

        private static object BroomAction(Component equipment, string action, string pilot, string slot, string id = null) // 테스트 어셈블리에서 실제 장착 API 호출
        {
            var method = equipment.GetType().GetMethod(action); // 기존 게임 어셈블리 메서드
            var parameters = method.GetParameters(); // 메서드의 캐릭터와 슬롯 열거형
            object pilotValue = System.Enum.Parse(parameters[0].ParameterType, pilot); // 실제 캐릭터 인수
            object slotValue = System.Enum.Parse(parameters[1].ParameterType, slot); // 실제 슬롯 인수
            return method.Invoke(equipment, id == null ? new[] { pilotValue, slotValue } : new[] { pilotValue, slotValue, id }); // 장착 또는 조회 결과
        }

        private static IEnumerator LoadOuterIslands() // 정상 배포 순서로 새 항로 읽기
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(OuterIslandsPath); // 정식 항로의 빌드 등록
            Assert.That(buildIndex, Is.GreaterThan(0), "메인 화면에서 하늘섬 외곽 스테이지 진입"); // 메인 메뉴 뒤의 정식 출격 씬
            yield return SceneManager.LoadSceneAsync(buildIndex); // 기존 조작과 HUD 진입 경로
            yield return null; // 씬의 첫 프레임 초기화
        }

        private static float StageTime(Component encounter, string time) // 출격에서 기록한 시계 읽기
        {
            return (float)encounter.GetType().GetProperty(time).GetValue(encounter); // 현재 출격의 전투 또는 전체 시간
        }

        private static IEnumerator AdvanceStageTo(Component encounter, float seconds) // 특정 타임라인 시각까지 대기
        {
            for (int frame = 0; frame < 1200 && StageTime(encounter, "ElapsedSeconds") < seconds; frame++) // 정지 상태에서 무한 대기 방지
            {
                yield return null; // 실제 웨이브 갱신 프레임
            }
            Assert.That(StageTime(encounter, "ElapsedSeconds"), Is.GreaterThanOrEqualTo(seconds)); // 실제 타임라인 진행
        }

        private void SetKeys(params Key[] keys) // 가상 키보드 입력 반영
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 새로운 키 상태 적용
            InputSystem.Update(); // 검증 프레임에 즉시 전달
        }

        private static IEnumerator WaitFrames(int count) // 고정 시험 간격 대기
        {
            for (int frame = 0; frame < count; frame++) // 지정된 프레임 반복
            {
                yield return null; // 실제 게임 업데이트 대기
            }
        }
        private static IEnumerator LoadFlight() // 빌드에 등록된 실제 씬 확인
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(ScenePath); // 씬의 배포 순서
            Assert.That(buildIndex, Is.GreaterThanOrEqualTo(0), "비행 시험 씬 빌드 등록"); // 시험 씬의 유효한 진입점
            yield return SceneManager.LoadSceneAsync(buildIndex); // 등록된 씬을 정상 경로로 열기
            yield return null; // 카메라 초기화와 프레임 대기
        }

        private static Transform FindPilot() // 실제 하이러키 검증
        {
            GameObject pilot = GameObject.Find("AriaPilot"); // 저장된 아리아 오브젝트
            Assert.That(pilot, Is.Not.Null); // 씬 편집 가능 대상 확인
            Assert.That(pilot.transform.Find("TrainingBroom"), Is.Not.Null); // 빗자루 하이러키 확인
            return pilot.transform; // 비행 좌표 제공
        }

        private IEnumerator CheckMove(Transform pilot, Key key, Vector3 direction) // 입력 키별 이동 검증
        {
            Vector3 start = pilot.position; // 이동 전 초기 위치
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 시험 키 입력
            InputSystem.Update(); // 입력 이벤트 즉시 반영
            for (int frame = 0; frame < 6; frame++) // 입력 처리 프레임
            {
                yield return null; // 플레이 프레임 진행
            }
            Assert.That(keyboard[key].isPressed, Is.True, "가상 키 입력 수신: " + key); // 키 이벤트 처리 확인
            Vector3 movement = pilot.position - start; // 실제 이동 벡터
            Assert.That(Vector3.Dot(movement, direction), Is.GreaterThan(0f), key.ToString()); // 요청 축으로 이동 확인
            Assert.That(Vector3.Cross(movement, direction).sqrMagnitude, Is.LessThan(0.001f)); // 다른 축 흔들림 검증
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 현재 키 해제
            yield return WaitFrames(24); // 다음 방향 검증 전 관성 정지
        }
    }
}
