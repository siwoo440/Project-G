using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

internal static class Program
{
    private static int failures;
    private static int passed;

    private static int Main()
    {
        Run("Approved gauge boundaries preserve independent reward classes", GaugeProfiles);
        Run("Overkill is discarded and transition rejects repeated damage", Overkill);
        Run("Nonlethal and lethal endpoints remain distinct and idempotent", Endpoints);
        Run("Invalid or protected damage cannot mutate the gauge", InvalidDamage);
        Run("Whole burst refuses insufficient budget without partial allocation", AtomicBudget);
        Run("Future children retain capacity and clear invalidates old reservations", SplitBudget);
        Run("Two emitter attacks preserve geometry and explicit damage", PairedGeometry);
        Run("Split plans reserve parent and child counts before spawning", SplitGeometry);
        Run("Ordinary shots preserve explicit damage and authored special motion", OrdinaryGeometry);
        Run("Capacity waits repeat warning without consuming a burst or recovery", CapacityWait);
        Run("Rooted movement preserves remaining charge time", RootedChargeClock);
        Console.WriteLine($"RESULT {passed} passed; {failures} failed");
        return failures == 0 ? 0 : 1;
    }

    private static void Run(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception exception) { failures++; Console.WriteLine("FAIL " + name + ": " + exception.GetBaseException().Message); }
    }

    private static Type Type(string name)
    {
        var result = Assembly.GetExecutingAssembly().GetType("ProjectG." + name);
        Check(result != null, "Missing production feature " + name);
        return result;
    }

    private static dynamic New(string name, params object[] args) => Activator.CreateInstance(Type(name), args);
    private static dynamic Static(string name, string method, params object[] args) => Type(name).GetMethod(method).Invoke(null, args);
    private static dynamic Profile(string id) => Static("FlightStoryCombatProfiles", "FindEnemy", id);
    private static List<dynamic> Patterns(string id) => Items(Static("FlightStoryCombatProfiles", "PatternsFor", id));
    private static List<dynamic> Items(object collection)
    {
        var result = new List<dynamic>();
        foreach (object value in (IEnumerable)collection) result.Add(value);
        return result;
    }
    private static void Check(bool condition, string why) { if (!condition) throw new InvalidOperationException(why); }
    private static void Equal(object actual, object expected, string why) => Check(Equals(actual, expected), why + $"; expected {expected}, got {actual}");
    private static void Near(double actual, double expected, string why) => Check(Math.Abs(actual - expected) < 0.00001, why);

    private static void GaugeProfiles()
    {
        string[] ids = { "B-RAY", "B-PROLOGUE", "B-KARDEN", "B-VARKAN", "B-RESONANCE" };
        int[] maxima = { 600, 1500, 900, 900, 1800 };
        int[] firstFloors = { 300, 1000, 600, 600, 1200 };
        string[] rewards = { "ELITE", "FINAL", "MID", "FINAL", "FINAL" };
        int[] control = { 50, 0, 0, 0, 0 };
        for (int i = 0; i < ids.Length; i++)
        {
            dynamic profile = Profile(ids[i]);
            Equal((string)profile.RewardClass, rewards[i], "classification " + ids[i]);
            Equal((int)profile.ControlPercent, control[i], "control " + ids[i]);
            dynamic gauge = New("FlightStoryGauge", (int)profile.Gauge, Patterns(ids[i]).Count, (bool)profile.IsNonlethal);
            gauge.Apply(int.MaxValue, 1d, 1f);
            Equal((int)gauge.Health, firstFloors[i], "next approved threshold " + ids[i]);
            Equal((int)profile.Gauge, maxima[i], "starting gauge " + ids[i]);
        }
        Check(Profile("made-up-enemy") == null, "Unknown profiles must not silently manufacture a threat");
    }

    private static void Overkill()
    {
        dynamic gauge = New("FlightStoryGauge", 900, 3, true);
        gauge.Apply(4000, 1d, 1f);
        gauge.Apply(4000, 1d, 1f);
        gauge.Apply(4000, 1.899d, 1f);
        Equal((int)gauge.Health, 600, "same-frame and transition damage blocked");
        Equal((int)gauge.Phase, 2, "one boundary per accepted hit");
        gauge.Apply(20, 1.9d, 1f);
        Equal((int)gauge.Health, 580, "excess damage did not carry forward");
        gauge.Apply(10000, 2d, 1f);
        Equal((int)gauge.Health, 300, "next floor is not skipped");
        gauge.Apply(1, 2.9d, 1f);
        Equal((int)gauge.Health, 299, "exact transition endpoint resumes damage");
    }

    private static void Endpoints()
    {
        dynamic nonlethal = New("FlightStoryGauge", 80, 1, true);
        dynamic result = nonlethal.Apply(100, 1d, 1f);
        Check((bool)result.Ended && (bool)nonlethal.IsSuppressed, "nonlethal endpoint");
        result = nonlethal.Apply(100, 2d, 1f);
        Check(!(bool)result.Ended && !(bool)result.Changed, "endpoint emitted once");
        dynamic normal = New("FlightStoryGauge", 50, 1, false);
        result = normal.Apply(50, 1d, 1f);
        Check((bool)result.Ended && !(bool)normal.IsSuppressed, "ordinary death stays lethal");
    }

    private static void InvalidDamage()
    {
        dynamic gauge = New("FlightStoryGauge", 600, 2, true);
        gauge.ProtectUntil(2d);
        gauge.Apply(100, 1d, 1f);
        gauge.Apply(-1, 3d, 1f);
        gauge.Apply(100, double.NaN, 1f);
        gauge.Apply(100, 3d, float.NaN);
        Equal((int)gauge.Health, 600, "invalid/protected input");
        gauge.Apply(20, 3d, 0.5f);
        Equal((int)gauge.Health, 590, "Ray body guard still takes half damage");
    }

    private static void AtomicBudget()
    {
        dynamic budget = New("FlightStoryBurstBudget", 128, 96);
        Check(budget.TryReserve(7, 90) == null, "six free slots cannot make a partial fan");
        Equal((int)budget.Pending, 0, "failed reservation has no side effects");
        dynamic reservation = budget.TryReserve(7, 89);
        Check(reservation != null, "complete fan fits exactly");
        Equal((int)budget.Pending, 7, "all seven slots reserved");
        Check(budget.TryReserve(1, 89) == null, "another producer cannot steal slots");
        Check((bool)budget.Commit(reservation, 7), "spawn consumes reserved slots");
        Equal((int)budget.Pending, 0, "reservation consumed once");
        Check(!(bool)budget.Commit(reservation, 1), "double use rejected");
        Check(budget.TryReserve(1, 128) == null, "physical cap respected");
    }

    private static void SplitBudget()
    {
        dynamic budget = New("FlightStoryBurstBudget", 128, 96);
        dynamic reservation = budget.TryReserve(12, 84);
        Check((bool)budget.Commit(reservation, 3), "three parents consume three slots");
        Equal((int)budget.Pending, 9, "nine future children remain reserved");
        Check(budget.TryReserve(1, 87) == null, "waiting children cannot be stolen");
        Check((bool)budget.Release(reservation, 3), "one canceled parent releases its children");
        Equal((int)budget.Pending, 6, "other parents retain their entitlement");
        int epoch = budget.Epoch;
        budget.Clear();
        Check((int)budget.Epoch != epoch, "clear changes epoch");
        Equal((int)budget.Pending, 0, "clear releases all pending capacity");
        Check(!(bool)budget.Commit(reservation, 6), "late callback cannot resurrect children");
        Check(budget.TryReserve(12, 84) != null, "cleared reservations do not leak");
    }

    private static void PairedGeometry()
    {
        dynamic pattern = Patterns("B-PROLOGUE")[2];
        var shots = Items(Static("FlightStoryPatternRules", "Build", pattern, 2));
        Equal(shots.Count, 8, "both spiral emitters form one burst");
        for (int i = 0; i < 8; i++) { Equal((int)shots[i].Damage, 20, "explicit damage"); Near((float)shots[i].Cone, 8, "approved narrow cone"); }
        Near((float)shots[0].MuzzleX, -6, "left emitter"); Near((float)shots[4].MuzzleX, 6, "right emitter");
        Near((float)shots[0].Roll, 52, "left phase roll"); Near((float)shots[4].Roll, -7, "right phase roll");
        shots = Items(Static("FlightStoryPatternRules", "Build", Patterns("B-RESONANCE")[1], 0));
        Equal(shots.Count, 2, "crossed lines have only two bullets");
        Near((float)shots[0].Cone, 0, "crossed lines are not a spiral");
        shots = Items(Static("FlightStoryPatternRules", "Build", Patterns("B-RESONANCE")[2], 1));
        Equal(shots.Count, 12, "middle volley includes six from each emitter");
        Near((float)shots[0].Yaw, -10, "left fan starts at approved yaw"); Near((float)shots[5].Yaw, 10, "left fan ends at approved yaw");
    }

    private static void SplitGeometry()
    {
        var patterns = Patterns("B-VARKAN");
        int[] parentCounts = { 2, 3, 2 };
        int[] reserves = { 8, 12, 12 };
        for (int p = 0; p < 3; p++)
        {
            var shots = Items(Static("FlightStoryPatternRules", "Build", patterns[p], 0));
            Equal(shots.Count, parentCounts[p], "parent count");
            int reservation = 0;
            foreach (dynamic shot in shots)
            {
                reservation += 1 + ((float[])shot.ChildYaw).Length;
                Near((float)shot.Speed, 8, "parent speed"); Near((float)shot.SplitAfter, 0.8, "split time");
                Near((float)shot.ChildSpeed, 12, "child speed");
            }
            Equal(reservation, reserves[p], "parent+child total reserved together");
        }
    }

    private static void OrdinaryGeometry()
    {
        var shots = Items(Static("FlightStoryPatternRules", "BuildOrdinary", Profile("E-FAN")));
        Equal(shots.Count, 3, "story fan does not inherit legacy seven");
        Near((float)shots[0].Yaw, -20, "left fan yaw"); Near((float)shots[2].Yaw, 20, "right fan yaw");
        Equal((int)shots[0].Damage, 10, "no prefab20 fallback");
        shots = Items(Static("FlightStoryPatternRules", "BuildOrdinary", Profile("E-SWALLOW")));
        Near((float)shots[0].LaunchDelay, 1.2, "visible waiting thorn");
        shots = Items(Static("FlightStoryPatternRules", "BuildOrdinary", Profile("E-EEL")));
        Equal(shots.Count, 2, "two curve shots");
        Near((float)shots[0].TurnDegrees, -18, "left turn rate"); Near((float)shots[1].TurnDegrees, 18, "right turn rate");
        Near((float)shots[0].TurnSeconds, 1, "curve ends after one second");
        Check((bool)Profile("E-ARCHIVE").IsNonlethal && (bool)Profile("H-ROCK").IsNonlethal, "no reward-capable death for isolation/debris");
        shots = Items(Static("FlightStoryPatternRules", "BuildOrdinary", Profile("E-EAGLE")));
        Equal(shots.Count, 3, "diagonal prototype remains functional");
        Near((float)shots[0].Yaw, -15, "Step4 provisional diagonal yaw");
        Near((float)shots[2].Pitch, 6, "Step4 provisional diagonal pitch");
    }

    private static void CapacityWait()
    {
        dynamic clock = New("FlightStoryAttackClock");
        clock.Start(Patterns("B-PROLOGUE")[1], 0d);
        Check(!(bool)clock.IsReady(1.499d), "full warning required");
        Check((bool)clock.AcceptBurst(1.5d), "first ring");
        Equal((int)clock.Burst, 1, "one successful burst");
        clock.RefuseBurst(2.1d);
        Near((double)clock.AttackStarted, 3d, "a capacity warning must not fast-forward a later charge");
        Equal((int)clock.Burst, 1, "failed reservation did not consume second burst");
        Check(!(bool)clock.IsReady(3.599d), "fresh warning after refusal");
        Check(!(bool)clock.IsWeak(3.599d), "refusal cannot open weak point");
        Check((bool)clock.AcceptBurst(3.6d), "second ring after warning");
        Check((bool)clock.AcceptBurst(4.2d), "third ring");
        clock.Advance(4.2d);
        Check(!(bool)clock.IsWeak(4.2d), "approved active tail remains after last ring");
        clock.Advance(4.4d);
        Check((bool)clock.IsWeak(4.4d), "recovery opens weak point");
        clock.Advance(5.4d);
        Check(!(bool)clock.IsWeak(5.4d), "weak point lasts only first recovery second");
        Equal(clock.Stage.ToString(), "Recovery", "recovery tail remains");
        clock.Advance(6d);
        Equal(clock.Stage.ToString(), "Idle", "complete recovery before next cycle");
    }

    private static void RootedChargeClock()
    {
        dynamic clock = New("FlightStoryAttackClock");
        clock.Start(Patterns("B-KARDEN")[0], 0d);
        clock.AcceptBurst(1.5d);
        clock.Delay(1d);
        Near((double)clock.AttackStarted, 2.5d, "movement origin shifts with the held duration");
        clock.Advance(3.5d);
        Equal(clock.Stage.ToString(), "Active", "root must not consume the remaining charge");
        clock.Advance(4.5d);
        Equal(clock.Stage.ToString(), "Recovery", "recovery starts after complete active duration");
    }
}
