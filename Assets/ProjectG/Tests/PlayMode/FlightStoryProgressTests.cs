using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectG.Tests
{
    // These tests exercise real JsonUtility, File.Replace and the production reward table in Unity.
    public sealed class FlightStoryProgressTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private Type storeType;
        private Type patchType;
        private object aria;
        private ScriptableObject catalog;
        private IDisposable store;
        private string directory;
        private string path;

        [SetUp]
        public void SetUp()
        {
            storeType = Type.GetType("ProjectG.FlightProgressStore, Assembly-CSharp", true);
            patchType = Type.GetType("ProjectG.FlightStoryCheckpoint, Assembly-CSharp", true);
            aria = Enum.Parse(storeType.GetMethod("Begin").GetParameters()[0].ParameterType, "Aria");
            directory = Path.Combine(Application.temporaryCachePath, "StoryProgress-" + Guid.NewGuid().ToString("N"));
            path = Path.Combine(directory, "progress.json");
            catalog = CreateRewardCatalog();
            store = Open();
        }

        [TearDown]
        public void TearDown()
        {
            store?.Dispose();
            if (catalog != null) UnityEngine.Object.DestroyImmediate(catalog);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void PreparedIdentityIsPersistedOnlyWhenActualCombatBeginsAndMustMatch()
        {
            Assert.That(Call("SaveStoryCheckpoint", SafeStart()), Is.True);
            Assert.That(Property("PendingId"), Is.Null.Or.Empty);
            Assert.That(Property("Gold"), Is.EqualTo(0));
            Assert.That(Call("BeginStory", "not-a-guid", aria, "ST-P-1"), Is.Null);
            Assert.That(Call("BeginStory", Guid.Empty.ToString("N"), aria, "ST-P-1"), Is.Null);
            Assert.That(Call("BeginStory", Guid.NewGuid().ToString("N"), aria, "ST-5-2"), Is.Null);
            string run = Guid.NewGuid().ToString("N");
            Assert.That(Call("BeginStory", run, aria, "ST-P-1"), Is.EqualTo(run));
            Assert.That(Call("BeginStory", run, aria, "ST-P-1"), Is.EqualTo(run));
            Assert.That(Call("BeginStory", run, aria, "ST-1-1"), Is.Null);
            object rian = Enum.Parse(aria.GetType(), "Rian");
            Assert.That(Call("BeginStory", run, rian, "ST-P-1"), Is.Null);
            Assert.That(Call("BeginStory", Guid.NewGuid().ToString("N"), aria, "ST-P-1"), Is.Null);
            Reopen();
            Assert.That(Property("PendingId"), Is.EqualTo(run));
            Assert.That(File.ReadAllText(path), Does.Contain("ST-P-1"));
        }

        [Test]
        public void SafeProgressPaysNothingAndExposesOnlyDefensiveSnapshots()
        {
            object patch = Patch(("MissionId", "M-2-1"), ("SeenScenes", S("C02-S01")), ("SceneEvents", S("EV_C02_S01_COMPLETE")), ("CompletedMissions", S("M-2-1")));
            Assert.That(Call("SaveStoryCheckpoint", patch), Is.True);
            object snapshot = Story();
            ((string[])Field(snapshot, "CompletedMissions"))[0] = "forged";
            Assert.That(Field(Story(), "CompletedMissions"), Is.EqualTo(S("M-2-1")));
            Assert.That(Property("Gold"), Is.EqualTo(0));
            Assert.That(Call("GetMastery", aria), Is.EqualTo(0));
            Assert.That(Call("Recover"), Is.Null);
            Reopen();
            Assert.That(Field(Story(), "CompletedMissions"), Is.EqualTo(S("M-2-1")));
        }

        [Test]
        public void OrdinaryCombatSuccessDoesNotGrantFinalResearchSpecialChoiceOrEnding()
        {
            string run = Begin("ST-1-2");
            object receipt = Complete(run, true, false, OrdinaryBoundary());
            Assert.That(receipt, Is.Not.Null);
            Assert.That(Field(receipt, "Mastery"), Is.EqualTo(20));
            Assert.That(Call("HasResearchTask", aria, 3), Is.False);
            Assert.That(Call("PendingResearchRewards", 3), Is.Empty);
            Assert.That(Property("HasClearedStory"), Is.False);
            Assert.That(Field(Story(), "CompletedSegments"), Is.EqualTo(S("ST-1-2")));
            Assert.That(Field(Story(), "CompletedMissions"), Is.Empty);
            Assert.That(Call("BeginStory", run, aria, "ST-1-2"), Is.Null);
            Reopen();
            Assert.That(Property("HasClearedStory"), Is.False);
        }

        [Test]
        public void PostCombatNarrationCompletesMissionWithoutPayingTheCombatAgain()
        {
            Assert.That(Call("SaveStoryCheckpoint", SafeStart()), Is.True);
            string run = Begin("ST-P-1");
            object combat = Patch(("MissionId", "M-P-1"), ("NextSceneId", "P-S05"), ("SceneEvents", S("EV_P_S02_COMPLETE", "EV_P_S03_COMPLETE", "EV_P_S04_COMPLETE")), ("CompletedSegments", S("ST-P-1")));
            Assert.That(Complete(run, true, true, combat), Is.Not.Null);
            Assert.That(Field(Story(), "CompletedMissions"), Is.Empty);
            Reopen();
            Assert.That(Call("Recover"), Is.Null);
            object followup = Patch(("MissionId", "M-P-1"), ("SceneEvents", S("EV_P_S05_COMPLETE", "EV_P_S06_COMPLETE", "EV_P_S07_COMPLETE")), ("CompletedMissions", S("M-P-1")), ("CompletedChapters", S("P")));
            Assert.That(Call("SaveStoryCheckpoint", followup), Is.True);
            Assert.That(Field(Story(), "CompletedMissions"), Is.EqualTo(S("M-P-1")));
            Assert.That(Property("Gold"), Is.EqualTo(600));
            Assert.That(((Array)Call("RewardBrooms")).Length, Is.EqualTo(1));
        }

        [Test]
        public void HistoricalReceiptReplaysAfterReopenWithoutDisturbingNewerPendingRun()
        {
            string first = Begin("ST-1-2");
            object original = Complete(first, true, false, OrdinaryBoundary(), 20);
            string rewardId = (string)Field(((Array)Field(original, "BroomRewards")).GetValue(0), "Id");
            string second = Begin("ST-1-2");
            Assert.That(Complete(second, true, false, OrdinaryBoundary()), Is.Not.Null);
            string pending = Begin("ST-8-OP");
            Reopen();
            object replay = Call("CompleteStory", first, false, true, 999, 99, true, true, null, null);
            Assert.That(Field(replay, "Cleared"), Is.True);
            Assert.That(Field(replay, "SourceStageId"), Is.EqualTo("ST-1-2"));
            Assert.That(Field(replay, "Termination"), Is.EqualTo("Succeeded"));
            Assert.That(Property("PendingId"), Is.EqualTo(pending));
            Assert.That(Property("Gold"), Is.EqualTo(1200));
            Assert.That(((Array)Call("RewardBrooms")).Length, Is.EqualTo(1));
            Set(replay, "Cleared", false);
            Set(((Array)Field(replay, "BroomRewards")).GetValue(0), "Id", "modified");
            object again = Call("CompleteStory", first, false, false, 0, 0, false, false, null, null);
            Assert.That(Field(again, "Cleared"), Is.True);
            Assert.That(Field(((Array)Field(again, "BroomRewards")).GetValue(0), "Id"), Is.EqualTo(rewardId));
        }

        [Test]
        public void C8FinalSuccessCommitsInventoryReceiptSevenEventsAndAllMissionsTogether()
        {
            string run = Begin("ST-8-OP");
            object partial = Operation(); Set(partial, "SceneEvents", S("EV_C08_S07_COMPLETE"));
            Assert.That(Complete(run, true, true, partial), Is.Null);
            Assert.That(Property("Gold"), Is.EqualTo(0));
            Assert.That(Property("PendingId"), Is.EqualTo(run));
            object receipt = Complete(run, true, true, Operation(), 40, 1);
            Assert.That(receipt, Is.Not.Null);
            Assert.That(((Array)Field(receipt, "BroomRewards")).Length, Is.EqualTo(4));
            Assert.That(Field(Story(), "OperationClear"), Is.True);
            Assert.That(((string[])Field(Story(), "SceneEvents")).Length, Is.EqualTo(7));
            Assert.That(Field(Story(), "CompletedMissions"), Is.EqualTo(S("M-8-1", "M-8-2", "M-8-3")));
            Assert.That(Property("HasClearedStory"), Is.True);
            Assert.That(((string[])Call("PendingResearchRewards", 3)).Length, Is.EqualTo(1));
            Reopen();
            Assert.That(Call("Recover"), Is.Null);
            Assert.That(Complete(run, false, false, null), Is.Not.Null);
            Assert.That(Property("Gold"), Is.EqualTo(600));
            Assert.That(((Array)Call("RewardBrooms")).Length, Is.EqualTo(4));
            Assert.That(Field(Story(), "OperationClear"), Is.True);
        }

        [Test]
        public void NormalC8FailureKeepsEarnedFinalPartWithoutFinalClearOrProvisionalProgress()
        {
            Assert.That(Call("SaveStoryCheckpoint", SafeStart()), Is.True);
            string run = Begin("ST-8-OP");
            object receipt = Complete(run, false, true, Operation());
            Assert.That(Field(receipt, "Mastery"), Is.EqualTo(10));
            Assert.That(Field(receipt, "Gold"), Is.EqualTo(150));
            Assert.That(Field(receipt, "Termination"), Is.EqualTo("Failed"));
            var awards = (Array)Field(receipt, "BroomRewards");
            Assert.That(awards.Length, Is.EqualTo(1));
            Assert.That(Field(awards.GetValue(0), "Source"), Is.EqualTo("FinalBoss"));
            Assert.That(Call("HasResearchTask", aria, 3), Is.False);
            Assert.That(Call("PendingResearchRewards", 3), Is.Empty);
            Assert.That(Field(Story(), "OperationClear"), Is.False);
            Assert.That(Field(Story(), "SceneEvents"), Is.EqualTo(S("EV_P_S01_COMPLETE")));
            Assert.That(Field(Story(), "CompletedMissions"), Is.Empty);
            Assert.That(Property("HasClearedStory"), Is.False);
        }

        [Test]
        public void CrashRecoveryPaysParticipationOnceAndKeepsOnlyPreviouslyCommittedProgress()
        {
            Assert.That(Call("SaveStoryCheckpoint", SafeStart()), Is.True);
            string earned = Begin("ST-8-OP");
            Assert.That(Complete(earned, false, true, null), Is.Not.Null);
            string interrupted = Begin("ST-8-OP");
            Reopen();
            object recovered = Call("Recover");
            Assert.That(Field(recovered, "RunId"), Is.EqualTo(interrupted));
            Assert.That(Field(recovered, "Recovered"), Is.True);
            Assert.That(Field(recovered, "Termination"), Is.EqualTo("Recovered"));
            Assert.That(Field(recovered, "SourceStageId"), Is.EqualTo("ST-8-OP"));
            Assert.That(Field(recovered, "BroomRewards"), Is.Empty);
            Assert.That(Field(recovered, "FirstTask"), Is.False);
            Assert.That(((Array)Call("RewardBrooms")).Length, Is.EqualTo(1));
            Assert.That(Property("Gold"), Is.EqualTo(300));
            Assert.That(Field(Story(), "SceneEvents"), Is.EqualTo(S("EV_P_S01_COMPLETE")));
            Assert.That(Call("Recover"), Is.Null);
            Assert.That(Field(Complete(interrupted, true, true, Operation()), "Recovered"), Is.True);
            Assert.That(Call("HasResearchTask", aria, 3), Is.False);
        }

        [Test]
        public void CrashParticipationStillPaysTheExistingMasteryThresholdToken()
        {
            for (int count = 0; count < 5; count++)
            {
                string old = (string)Call("Begin", aria);
                Assert.That(Call("Complete", old, false), Is.Not.Null);
            }
            Begin("ST-8-OP");
            object receipt = Call("Recover");
            Assert.That(Field(receipt, "Marks"), Is.EqualTo(1));
            Assert.That(Call("GetMastery", aria), Is.EqualTo(60));
            Assert.That(Call("GetMarks", aria), Is.EqualTo(1));
            Assert.That(Field(receipt, "BroomRewards"), Is.Empty);
        }

        [Test]
        public void WriteFailureLeavesAwardsProgressAndPendingIdentityUnchangedUntilRetry()
        {
            string run = Begin("ST-8-OP");
            string durableBefore = File.ReadAllText(path);
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(Complete(run, true, true, Operation()), Is.Null);
            Assert.That(File.ReadAllText(path), Is.EqualTo(durableBefore));
            Assert.That(Property("Gold"), Is.EqualTo(0));
            Assert.That(Property("PendingId"), Is.EqualTo(run));
            Assert.That(Field(Story(), "OperationClear"), Is.False);
            Assert.That(Call("RewardBrooms"), Is.Empty);
            Directory.Delete(path + ".tmp");
            Assert.That(Complete(run, true, true, Operation()), Is.Not.Null);
            Reopen();
            Assert.That(Property("Gold"), Is.EqualTo(600));
            Assert.That(Field(Story(), "OperationClear"), Is.True);
            Assert.That(((Array)Call("RewardBrooms")).Length, Is.EqualTo(1));
        }

        [Test]
        public void VersionOneSaveWithoutStoryFieldsPreservesLegacyBalancesAndMethods()
        {
            store.Dispose(); store = null;
            string[] pilots = new string[Enum.GetValues(aria.GetType()).Length];
            for (int index = 0; index < pilots.Length; index++) pilots[index] = "{\"mastery\":" + (index == Convert.ToInt32(aria) ? "40" : "0") + ",\"marks\":0}";
            File.WriteAllText(path, "{\"version\":1,\"crystals\":12,\"gold\":1200,\"pilots\":[" + string.Join(",", pilots) + "],\"clearedStory\":true,\"unlocks\":[\"pilot.aria\",\"pilot.rian\"]}");
            store = Open();
            Assert.That(Property("Error"), Is.Null);
            Assert.That(Property("Gold"), Is.EqualTo(1200));
            Assert.That(Call("GetMastery", aria), Is.EqualTo(40));
            Assert.That(Field(Story(), "CompletedMissions"), Is.Empty);
            string run = (string)Call("Begin", aria);
            Assert.That(Call("Complete", run, true), Is.Not.Null);
            Assert.That(Property("Gold"), Is.EqualTo(1800));
            Assert.That(Call("GetMastery", aria), Is.EqualTo(60));
            Assert.That(Call("GetMarks", aria), Is.EqualTo(1));
            Assert.That(File.ReadAllText(path), Does.Contain("\"version\": 1"));
        }

        [Test]
        public void LegacyResearchSemanticsStayUnchangedForLegacyRuns()
        {
            string run = (string)Call("Begin", aria);
            Assert.That(Call("CompleteWithResearch", run, true, false, 10, false), Is.Not.Null);
            Assert.That(Call("HasResearchTask", aria, 3), Is.True);
            Assert.That(Property("HasClearedStory"), Is.True);
        }

        [Test]
        public void LegacyCompletionCannotBypassAnActiveStoryTransaction()
        {
            string run = Begin("ST-8-OP");
            Assert.That(Call("Complete", run, true), Is.Null);
            Assert.That(Property("PendingId"), Is.EqualTo(run));
            Assert.That(Property("Gold"), Is.EqualTo(0));
        }

        [Test]
        public void UnseenRecollectionPersistsOnlySeenScenesAcrossReopen()
        {
            object patch = Operation(); Set(patch, "RecollectionOnly", true); Set(patch, "SeenScenes", S("SC01-S01")); Set(patch, "Records", S("fake world record"));
            Assert.That(Call("SaveStoryCheckpoint", patch), Is.True);
            Reopen();
            Assert.That(Field(Story(), "SeenScenes"), Is.EqualTo(S("SC01-S01")));
            Assert.That(Field(Story(), "SceneEvents"), Is.Empty);
            Assert.That(Field(Story(), "CompletedMissions"), Is.Empty);
            Assert.That(Field(Story(), "Records"), Is.Empty);
            Assert.That(Property("Gold"), Is.EqualTo(0));
        }

        private IDisposable Open() => (IDisposable)Activator.CreateInstance(storeType, path);
        private void Reopen() { store.Dispose(); store = Open(); }
        private object Call(string method, params object[] args) => storeType.GetMethod(method, Instance).Invoke(store, args);
        private object Property(string name) => storeType.GetProperty(name, Instance).GetValue(store);
        private object Story() => Property("StoryProgress");
        private string Begin(string stage)
        {
            string run = Guid.NewGuid().ToString("N");
            Assert.That(Call("BeginStory", run, aria, stage), Is.EqualTo(run));
            return run;
        }
        private object Complete(string run, bool cleared, bool finalBoss, object patch, int normal = 0, int elite = 0)
            => Call("CompleteStory", run, cleared, false, normal, elite, false, finalBoss, catalog, patch);
        private static string[] S(params string[] values) => values;
        private static object Field(object value, string field) => value.GetType().GetField(field).GetValue(value);
        private static void Set(object value, string field, object item) => value.GetType().GetField(field).SetValue(value, item);
        private object Patch(params (string, object)[] values)
        {
            object patch = Activator.CreateInstance(patchType);
            foreach ((string key, object value) in values) Set(patch, key, value);
            return patch;
        }
        private object SafeStart() => Patch(("MissionId", "M-P-1"), ("NextSceneId", "P-S02"), ("SeenScenes", S("P-S01")), ("SceneEvents", S("EV_P_S01_COMPLETE")));
        private object OrdinaryBoundary() => Patch(("MissionId", "M-1-2"), ("NextSceneId", "C01-S05"), ("SceneEvents", S("EV_C01_S04_COMPLETE")), ("CompletedSegments", S("ST-1-2")));
        private object Operation() => Patch(("MissionId", "M-8-3"), ("OperationClear", true), ("SceneEvents", S("EV_C08_S01_COMPLETE", "EV_C08_S02_COMPLETE", "EV_C08_S03_COMPLETE", "EV_C08_S04_COMPLETE", "EV_C08_S05_COMPLETE", "EV_C08_S06_COMPLETE", "EV_C08_S07_COMPLETE")), ("CompletedMissions", S("M-8-1", "M-8-2", "M-8-3")), ("CompletedChapters", S("C08")), ("CompletedSegments", S("ST-8-OP")));

        private ScriptableObject CreateRewardCatalog()
        {
            Type type = Type.GetType("ProjectG.FlightBroomCatalog, Assembly-CSharp", true);
            Type partType = Type.GetType("ProjectG.FlightBroomPart, Assembly-CSharp", true);
            var result = ScriptableObject.CreateInstance(type);
            Array parts = Array.CreateInstance(partType, 80);
            for (int index = 0; index < 80; index++)
            {
                object part = Activator.CreateInstance(partType, true);
                Set(part, "id", "story-test-part-" + index);
                Set(part, "displayName", "Story test part " + index);
                Set(part, "setId", "story-test-set-" + index / 8);
                parts.SetValue(part, index);
            }
            type.GetField("parts", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(result, parts);
            return result;
        }
    }
}
