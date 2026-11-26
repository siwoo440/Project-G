using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

internal static class Program
{
    private static Type rules;
    private static Type dataType;
    private static Type patchType;
    private static int passed;
    private static int failed;

    private static int Main()
    {
        Assembly source = typeof(Program).Assembly;
        rules = source.GetType("ProjectG.FlightStoryProgressRules");
        dataType = source.GetType("ProjectG.FlightStoryProgressData");
        patchType = source.GetType("ProjectG.FlightStoryCheckpoint");
        if (rules == null || dataType == null || patchType == null)
        {
            Console.Error.WriteLine("FAIL: production story persistence contract is missing.");
            return 1;
        }

        Run("old absent story state normalizes to empty independent arrays", () =>
        {
            object copy = Call("Copy", new object[] { null });
            foreach (string name in Arrays) Equal(0, Strings(copy, name).Length, name);
            Equal(0, ((Array)Get(copy, "Checkpoints")).Length, "checkpoints");
            Equal(false, Get(copy, "OperationClear"), "operation");
        });
        Run("copied arrays and checkpoint entries cannot change authority", () =>
        {
            object state = SafeStart();
            object checkpoint = Activator.CreateInstance(dataType.Assembly.GetType("ProjectG.FlightStoryCheckpointEntry", true));
            Set(checkpoint, "MissionId", "M-P-1"); Set(checkpoint, "NextSceneId", "P-S02");
            Array entries = Array.CreateInstance(checkpoint.GetType(), 1); entries.SetValue(checkpoint, 0); Set(state, "Checkpoints", entries);
            object copy = Call("Copy", state);
            Strings(copy, "SeenScenes")[0] = "modified";
            Set(((Array)Get(copy, "Checkpoints")).GetValue(0), "NextSceneId", "modified");
            Equal("P-S01", Strings(state, "SeenScenes")[0], "seen source");
            Equal("P-S02", Get(checkpoint, "NextSceneId"), "checkpoint source");
        });
        Run("noncombat mission saves without creating a combat segment", () =>
        {
            object patch = Patch(("MissionId", "M-2-1"), ("SeenScenes", S("C02-S01")), ("SceneEvents", S("EV_C02_S01_COMPLETE")), ("CompletedMissions", S("M-2-1")));
            Expect(Checkpoint(Data(), patch, false, out object next), "safe noncombat rejected");
            Sequence(S("M-2-1"), Strings(next, "CompletedMissions"));
            Equal(0, Strings(next, "CompletedSegments").Length, "no participation");
        });
        Run("dialogue seen cannot complete an unperformed mission", () =>
        {
            object patch = Patch(("MissionId", "M-1-1"), ("SeenScenes", S("C01-S01", "C01-S02", "C01-S03")), ("CompletedMissions", S("M-1-1")));
            Expect(!Checkpoint(Data(), patch, false, out _), "seen became evidence");
        });
        Run("active combat refuses a safe-checkpoint write", () =>
        {
            Expect(!Checkpoint(SafeStart(), Patch(("MissionId", "M-P-1"), ("NextSceneId", "P-S05")), true, out _), "active combat persisted");
        });
        Run("safe precombat resume cannot skip into a combat segment midpoint", () =>
        {
            Expect(Checkpoint(SafeStart(), Patch(("MissionId", "M-P-1"), ("NextSceneId", "P-S02")), false, out _), "combat entry boundary rejected");
            Expect(!Checkpoint(SafeStart(), Patch(("MissionId", "M-P-1"), ("NextSceneId", "P-S03")), false, out _), "midcombat resume accepted");
        });
        Run("C01 S02 is a safe prerequisite and S03 is the first combat entry", () =>
        {
            object patch = Patch(("MissionId", "M-1-1"), ("NextSceneId", "C01-S03"), ("SceneEvents", S("EV_C01_S01_COMPLETE", "EV_C01_S02_COMPLETE")));
            Expect(Checkpoint(Data(), patch, false, out object next), "noncombat prerequisite required a reward receipt");
            Equal(0, Strings(next, "CompletedSegments").Length, "precombat entry granted participation");
        });
        Run("canonical ending needs E S01 through S04 and does not require optional S05", () =>
        {
            object patch = Patch(("MissionId", "M-E-1"), ("SceneEvents", S("EV_E_S01_COMPLETE", "EV_E_S02_COMPLETE", "EV_E_S03_COMPLETE", "EV_E_S04_COMPLETE")), ("CompletedMissions", S("M-E-1")), ("CompletedChapters", S("E")));
            Expect(Checkpoint(Data(), patch, false, out object next), "optional postcredits scene blocked ending");
            Sequence(S("M-E-1"), Strings(next, "CompletedMissions"));
        });
        Run("safe checkpoint cannot forge a newly completed combat segment", () =>
        {
            Expect(!Checkpoint(Data(), PrologueSettlement(), false, out _), "forged segment accepted");
        });
        Run("combat event cannot be saved before its settlement", () =>
        {
            Expect(!Checkpoint(Data(), Patch(("SceneEvents", S("EV_C01_S03_COMPLETE"))), false, out _), "combat evidence accepted");
        });
        Run("combat settlement commits its boundary and leaves full mission pending", () =>
        {
            object before = SafeStart();
            object patch = PrologueSettlement();
            Expect(Settlement(before, patch, "ST-P-1", true, false, out object next), "settlement rejected");
            Sequence(S("ST-P-1"), Strings(next, "CompletedSegments"));
            Equal(0, Strings(next, "CompletedMissions").Length, "premature mission");
            Equal(0, Strings(before, "CompletedSegments").Length, "input was mutated");
            object entry = ((Array)Get(next, "Checkpoints")).GetValue(0);
            Equal("P-S05", Get(entry, "NextSceneId"), "safe postcombat resume");
            Strings(patch, "SceneEvents")[0] = "modified";
            Expect(Strings(next, "SceneEvents").Contains("EV_P_S02_COMPLETE"), "patch aliases committed data");
        });
        Run("postcombat scenes incrementally finish mission and chapter", () =>
        {
            Expect(Settlement(SafeStart(), PrologueSettlement(), "ST-P-1", true, false, out object state), "setup");
            object patch = Patch(("MissionId", "M-P-1"), ("SceneEvents", S("EV_P_S05_COMPLETE", "EV_P_S06_COMPLETE", "EV_P_S07_COMPLETE")), ("CompletedMissions", S("M-P-1")), ("CompletedChapters", S("P")));
            Expect(Checkpoint(state, patch, false, out object next), "followup rejected");
            Sequence(S("M-P-1"), Strings(next, "CompletedMissions"));
            Sequence(S("P"), Strings(next, "CompletedChapters"));
            Equal(0, ((Array)Get(next, "Checkpoints")).Length, "completed mission kept resume entry");
        });
        Run("chapter completion requires every main mission but no side mission", () =>
        {
            object state = Data(("CompletedSegments", S("ST-1-1", "ST-1-2")), ("CompletedMissions", S("M-1-1")), ("SceneEvents", S("EV_C01_S01_COMPLETE", "EV_C01_S02_COMPLETE", "EV_C01_S03_COMPLETE", "EV_C01_S04_COMPLETE")));
            object patch = Patch(("MissionId", "M-1-2"), ("SceneEvents", S("EV_C01_S05_COMPLETE", "EV_C01_S06_COMPLETE")), ("CompletedMissions", S("M-1-2")), ("CompletedChapters", S("C01")));
            Expect(Checkpoint(state, patch, false, out object next), "chapter refused");
            Sequence(S("C01"), Strings(next, "CompletedChapters"));
            Expect(!Checkpoint(Data(), Patch(("CompletedChapters", S("C01"))), false, out _), "empty chapter accepted");
        });
        Run("one settlement cannot complete a foreign segment", () =>
        {
            object patch = PrologueSettlement(); Set(patch, "CompletedSegments", S("ST-P-1", "ST-1-1"));
            Expect(!Settlement(SafeStart(), patch, "ST-P-1", true, false, out _), "foreign segment committed");
        });
        Run("successful settlement needs its boundary evidence", () =>
        {
            object patch = PrologueSettlement(); Set(patch, "SceneEvents", S("EV_P_S02_COMPLETE"));
            Expect(!Settlement(SafeStart(), patch, "ST-P-1", true, false, out _), "missing boundary accepted");
            Expect(!Settlement(Data(), Patch(), "ST-P-1", true, false, out _), "empty success accepted");
        });
        Run("route identifiers cannot create combat participation", () =>
        {
            foreach (string stage in S("ST-5-2", "ST-6-2", "ST-7-3", "unknown", ""))
                Equal(false, Call("IsCombatStage", stage), stage);
            Equal(true, Call("IsCombatStage", "ST-8-OP"), "operation combat stage");
        });
        Run("C8 seen history is distinct from provisional world events", () =>
        {
            Expect(Checkpoint(Data(), Patch(("SeenScenes", S("C08-S01"))), false, out object next), "seen history rejected");
            Equal(0, Strings(next, "SceneEvents").Length, "seen created world event");
            Expect(!Checkpoint(Data(), Patch(("SceneEvents", S("EV_C08_S01_COMPLETE"))), false, out _), "partial C8 event committed");
        });
        Run("C8 intermediate resume checkpoints are forbidden", () =>
        {
            Expect(!Checkpoint(Data(), Patch(("MissionId", "M-8-1"), ("NextSceneId", "C08-S03")), false, out _), "C8 middle resume saved");
            object patch = Operation(); Set(patch, "NextSceneId", "C08-S06");
            Expect(!Settlement(Data(), patch, "ST-8-OP", true, false, out _), "C8 final commit points backward");
        });
        Run("C8 provisional records cannot escape through a non-event field", () =>
        {
            Expect(!Checkpoint(Data(), Patch(("MissionId", "M-8-1"), ("Records", S("대피 도착 기록"))), false, out _), "C8 record escaped provisional state");
        });
        Run("C8 final proposal commits all seven events and three missions together", () =>
        {
            object before = SafeStart();
            Expect(Settlement(before, Operation(), "ST-8-OP", true, false, out object next), "complete operation rejected");
            Equal(true, Get(next, "OperationClear"), "operation clear");
            Equal(8, Strings(next, "SceneEvents").Length, "old event plus all seven");
            Sequence(S("M-8-1", "M-8-2", "M-8-3"), Strings(next, "CompletedMissions"));
            Sequence(S("C08"), Strings(next, "CompletedChapters"));
            Sequence(S("ST-8-OP"), Strings(next, "CompletedSegments"));
            Equal(false, Get(before, "OperationClear"), "atomic input preservation");
        });
        Run("C8 must supply every current event even after an earlier operation clear", () =>
        {
            Expect(Settlement(Data(), Operation(), "ST-8-OP", true, false, out object previous), "setup");
            foreach (string missing in OperationEvents())
            {
                object patch = Operation(); Set(patch, "SceneEvents", OperationEvents().Where(id => id != missing).ToArray());
                Expect(!Settlement(previous, patch, "ST-8-OP", true, false, out _), "old state replaced missing " + missing);
            }
        });
        Run("C8 missing mission chapter segment or owner cannot commit", () =>
        {
            foreach (string missing in S("M-8-1", "M-8-2", "M-8-3"))
            {
                object patch = Operation(); Set(patch, "CompletedMissions", S("M-8-1", "M-8-2", "M-8-3").Where(id => id != missing).ToArray());
                Expect(!Settlement(Data(), patch, "ST-8-OP", true, false, out _), "missing mission " + missing);
            }
            foreach ((string field, object value) in new[] { ("CompletedChapters", (object)S()), ("CompletedSegments", (object)S()), ("MissionId", (object)"M-8-2"), ("OperationClear", (object)false) })
            {
                object patch = Operation(); Set(patch, field, value);
                Expect(!Settlement(Data(), patch, "ST-8-OP", true, false, out _), "missing " + field);
            }
        });
        Run("C8 cannot be committed through a safe checkpoint or another stage", () =>
        {
            Expect(!Checkpoint(Data(), Operation(), false, out _), "operation bypassed settlement");
            Expect(!Settlement(Data(), Operation(), "ST-P-1", true, false, out _), "wrong stage granted operation");
        });
        Run("normal failure discards C8 proposal and preserves prior safe progress", () =>
        {
            object before = SafeStart();
            Expect(Settlement(before, Operation(), "ST-8-OP", false, false, out object next), "failure settlement rejected");
            Sequence(S("EV_P_S01_COMPLETE"), Strings(next, "SceneEvents"));
            Equal(false, Get(next, "OperationClear"), "failed operation committed");
            Equal(0, Strings(next, "CompletedMissions").Length, "failed missions committed");
        });
        Run("crash recovery never commits an uncommitted progress patch", () =>
        {
            Expect(Settlement(SafeStart(), Operation(), "ST-8-OP", false, true, out object next), "recovery refused");
            Sequence(S("P-S01"), Strings(next, "SeenScenes"));
            Sequence(S("EV_P_S01_COMPLETE"), Strings(next, "SceneEvents"));
            Equal(0, Strings(next, "CompletedSegments").Length, "recovery earned a segment");
        });
        Run("later failure never erases an already committed C8 success", () =>
        {
            Expect(Settlement(Data(), Operation(), "ST-8-OP", true, false, out object state), "setup");
            Expect(Settlement(state, null, "ST-8-OP", false, true, out object next), "recovery refused");
            Equal(true, Get(next, "OperationClear"), "old success was downgraded");
            Equal(3, Strings(next, "CompletedMissions").Length, "old missions erased");
        });
        Run("unseen recollection changes only seen scenes", () =>
        {
            object patch = Operation(); Set(patch, "RecollectionOnly", true); Set(patch, "SeenScenes", S("SC01-S01")); Set(patch, "Records", S("forged record")); Set(patch, "NextSceneId", "C08-S05");
            Expect(Checkpoint(SafeStart(), patch, false, out object next), "recollection rejected");
            Sequence(S("P-S01", "SC01-S01"), Strings(next, "SeenScenes"));
            Sequence(S("EV_P_S01_COMPLETE"), Strings(next, "SceneEvents"));
            Equal(0, Strings(next, "Records").Length, "recollection world record");
            Equal(0, Strings(next, "CompletedMissions").Length, "recollection mission");
            Equal(false, Get(next, "OperationClear"), "recollection ending");
        });
        Run("invalid recollection IDs cannot create an unreadable durable profile", () =>
        {
            Expect(!Checkpoint(Data(), Patch(("RecollectionOnly", true), ("SeenScenes", S(" "))), false, out _), "invalid scene persisted");
        });
        Run("repeated safe patches deduplicate without losing prior records", () =>
        {
            object patch = Patch(("MissionId", "M-P-1"), ("NextSceneId", "P-S02"), ("SceneEvents", S("EV_P_S01_COMPLETE", "EV_P_S01_COMPLETE")), ("Records", S("investigation")));
            Expect(Checkpoint(Data(), patch, false, out object one), "first");
            Expect(Checkpoint(one, patch, false, out object two), "repeat");
            Sequence(S("EV_P_S01_COMPLETE"), Strings(two, "SceneEvents"));
            Sequence(S("investigation"), Strings(two, "Records"));
            Equal(1, ((Array)Get(two, "Checkpoints")).Length, "duplicated checkpoint");
        });
        Run("malformed durable C8 progress is rejected on loading", () =>
        {
            Equal(false, Call("IsValid", Data(("CompletedMissions", S("M-8-1")))), "partial mission");
            Equal(false, Call("IsValid", Data(("OperationClear", true))), "empty operation");
            Expect(Settlement(Data(), Operation(), "ST-8-OP", true, false, out object complete), "setup");
            Equal(true, Call("IsValid", complete), "valid complete operation");
        });
        Run("FINAL research and special choice require both clear and FINAL evidence", () =>
        {
            Equal(false, Call("QualifiesFinalClear", false, false), "neither");
            Equal(false, Call("QualifiesFinalClear", true, false), "ordinary clear");
            Equal(false, Call("QualifiesFinalClear", false, true), "earned FINAL on failure");
            Equal(true, Call("QualifiesFinalClear", true, true), "FINAL clear");
        });

        Console.WriteLine("Story persistence: " + passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }

    private static readonly string[] Arrays = { "SeenScenes", "SceneEvents", "Records", "CompletedMissions", "CompletedChapters", "CompletedSegments" };
    private static string[] S(params string[] values) => values;
    private static object Data(params (string, object)[] fields) => Make(dataType, fields);
    private static object Patch(params (string, object)[] fields) => Make(patchType, fields);
    private static object Make(Type type, (string, object)[] fields)
    {
        object value = Activator.CreateInstance(type);
        foreach ((string name, object item) in fields) Set(value, name, item);
        return value;
    }
    private static object Get(object value, string name) => value.GetType().GetField(name).GetValue(value);
    private static void Set(object value, string name, object item) => value.GetType().GetField(name).SetValue(value, item);
    private static string[] Strings(object value, string name) => (string[])Get(value, name);
    private static object Call(string name, params object[] args) => rules.GetMethod(name, BindingFlags.Static | BindingFlags.Public).Invoke(null, args);
    private static bool Checkpoint(object state, object patch, bool pending, out object next)
    {
        object[] args = { state, patch, pending, null }; bool ok = (bool)Call("TryCheckpoint", args); next = args[3]; return ok;
    }
    private static bool Settlement(object state, object patch, string stage, bool cleared, bool recovered, out object next)
    {
        object[] args = { state, patch, stage, cleared, recovered, null }; bool ok = (bool)Call("TrySettlement", args); next = args[5]; return ok;
    }
    private static object SafeStart() => Data(("SeenScenes", S("P-S01")), ("SceneEvents", S("EV_P_S01_COMPLETE")));
    private static object PrologueSettlement() => Patch(("MissionId", "M-P-1"), ("NextSceneId", "P-S05"), ("SceneEvents", S("EV_P_S02_COMPLETE", "EV_P_S03_COMPLETE", "EV_P_S04_COMPLETE")), ("CompletedSegments", S("ST-P-1")));
    private static string[] OperationEvents() => S("EV_C08_S01_COMPLETE", "EV_C08_S02_COMPLETE", "EV_C08_S03_COMPLETE", "EV_C08_S04_COMPLETE", "EV_C08_S05_COMPLETE", "EV_C08_S06_COMPLETE", "EV_C08_S07_COMPLETE");
    private static object Operation() => Patch(("MissionId", "M-8-3"), ("SeenScenes", S("C08-S01", "C08-S02", "C08-S03", "C08-S04", "C08-S05", "C08-S06", "C08-S07")), ("SceneEvents", OperationEvents()), ("CompletedMissions", S("M-8-1", "M-8-2", "M-8-3")), ("CompletedChapters", S("C08")), ("CompletedSegments", S("ST-8-OP")), ("OperationClear", true));
    private static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + (error is TargetInvocationException && error.InnerException != null ? error.InnerException.Message : error.Message)); }
    }
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Equal(object expected, object actual, string message) { Expect(Equals(expected, actual), message + ": expected " + expected + ", got " + actual); }
    private static void Sequence(string[] expected, string[] actual) { Expect(expected.SequenceEqual(actual), "expected [" + string.Join(",", expected) + "], got [" + string.Join(",", actual) + "]"); }
}
