using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

internal static class Program
{
    private sealed class Failed : Exception { internal Failed(string message) : base(message) { } }
    private static readonly string[] C8 = { "C08-S01", "C08-S02", "C08-S03", "C08-S04", "C08-S05", "C08-S06", "C08-S07" };
    private static readonly string[] FinalFacts = { "backup_verified", "management_route_open", "local_closure_verified", "maintenance_handoff", "crosscheck_complete", "commander_approved" };

    private static int Main()
    {
        var cases = new (string, Action)[]
        {
            ("scene facts require explicit current-sortie evidence", SceneFacts),
            ("chapter eight remains one provisional sortie", ChapterEight),
            ("chapter eight final cannot pass empty or stale evidence", ChapterEightFinal),
            ("encounter resolution is safe, idempotent and single-class", EncounterLedger),
            ("normal failure preserves earned ledger but crash excludes it", FailurePolicies),
            ("safe routes and practice cannot gain participation", NoCombatRewards),
            ("wave slot claims are finite and current-scene only", FiniteWaves),
            ("manifest rejects empty seal and wrong identity or destination", ManifestIdentity),
            ("discovery reopens manifest without erasing safe arrivals", ManifestDiscovery),
            ("individual durability is carried and hit events deduplicated", ProtectedTargets),
            ("cumulative work and continuous holds differ during interruption", WorkAndHold),
            ("active time pauses and rejects nonfinite values", ActiveClock),
            ("thirty seconds uses timestamp span not 120 sample count", TimestampWindow),
            ("local thirty-second windows must overlap", CommonWindow),
            ("missing sample starts a new continuous window", MissingSample),
            ("early and duplicate samples cannot manufacture time", SampleCadence),
            ("an early unsafe observation still revokes continuous safety", EarlyUnsafeObservation),
            ("reports expire after six inclusive active seconds", ReportFreshness),
            ("old phase route and sequence cannot acknowledge current work", ReportTags),
            ("out-of-order reports cannot refresh authorization", ReportOrder),
            ("rejected reports cannot erase the accepted sequence watermark", ReportWatermark),
            ("backup requires inclusive point-four for five seconds", BackupWindow),
            ("handoff cannot release before five continuous seconds", HandoffOverlap),
            ("final handoff needs another ten observed seconds", PostHandoff),
            ("every live operating condition can revoke qualification", LiveConditions),
            ("late stability loss preserves already safe people", RevocationKeepsArrivals),
            ("NaN infinity missing anchors and unknown sites fail closed", InvalidSamples),
            ("required sites and phase snapshots cannot be externally mutated", ImmutableScope),
            ("failed attempts cannot continue or commit mission completion", TerminalStates)
            ,("typed objective gates require every bound live target", TypedGates)
            ,("swept protection catches crossing tangent and inside hits", SweptProtection)
        };
        int failed = 0;
        foreach (var item in cases)
        {
            try { item.Item2(); Console.WriteLine("PASS " + item.Item1); }
            catch (Exception error)
            {
                failed++;
                Exception reason = error is TargetInvocationException wrapped && wrapped.InnerException != null ? wrapped.InnerException : error;
                Console.WriteLine("FAIL " + item.Item1 + ": " + reason.GetType().Name + ": " + reason.Message);
            }
        }
        Console.WriteLine("Story mission rules: " + (cases.Length - failed) + "/" + cases.Length + " passed.");
        return failed == 0 ? 0 : 1;
    }

    private static Type Runtime(string name)
    {
        Type type = Assembly.GetExecutingAssembly().GetType("ProjectG." + name);
        if (type == null) throw new Failed("Missing production runtime type ProjectG." + name);
        return type;
    }
    private static dynamic New(string name, params object[] args) => Activator.CreateInstance(Runtime(name), args);
    private static dynamic EnumValue(string name, string value) => Enum.Parse(Runtime(name), value);
    private static void True(bool value, string message = "expected true") { if (!value) throw new Failed(message); }
    private static void False(bool value, string message = "expected false") { if (value) throw new Failed(message); }
    private static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual)) throw new Failed("expected " + expected + ", got " + actual);
    }
    private static dynamic Attempt(string id = "run", string stage = "ST-1-2", string[] scenes = null, string mode = "Story")
        => New("FlightStoryAttempt", id, stage, scenes ?? new[] { "A", "B" }, EnumValue("FlightStoryPlayMode", mode));
    private static dynamic Requirement(params string[] facts)
    {
        dynamic result = New("FlightStorySceneRequirements"); result.Facts = facts; return result;
    }
    private static dynamic Phase(string run = "run", string phase = "maintenance", int sequence = 1, string route = "main")
        => New("FlightStoryPhase", run, phase, sequence, route);
    private static dynamic Manifest(string run = "run", bool two = false)
    {
        Array entries = Array.CreateInstance(Runtime("FlightStoryManifestEntry"), two ? 2 : 1);
        dynamic first = New("FlightStoryManifestEntry"); first.Id = "person-1"; first.GroupId = "human"; first.DestinationId = "safe-human"; entries.SetValue(first, 0);
        if (two)
        {
            dynamic second = New("FlightStoryManifestEntry"); second.Id = "person-2"; second.GroupId = "operations"; second.DestinationId = "safe-operations"; entries.SetValue(second, 1);
        }
        return New("FlightStoryManifest", run, entries);
    }
    private static dynamic ArrivedManifest(string run = "run")
    {
        dynamic manifest = Manifest(run); True(manifest.Seal()); True(manifest.ConfirmDeparture(run, "person-1")); True(manifest.ConfirmArrival(run, "person-1", "safe-human")); return manifest;
    }
    private static dynamic Stability(string run = "run", string[] sites = null)
    {
        dynamic result = New("FlightStoryStability", run, sites ?? new[] { "A" }); True(result.BeginPhase(Phase(run), 0d)); return result;
    }
    private static dynamic ReportObject(dynamic phase, string site, double at, long sequence)
    {
        dynamic report = New("FlightStorySiteReport"); report.Phase = phase; report.SiteId = site; report.ReportSequence = sequence; report.ObservedAt = at; report.HumanAcknowledged = true; report.LocalAcknowledged = true; return report;
    }
    private static dynamic SampleObject(dynamic phase, string site, double at, double deviation = 0.30d, bool handoff = true)
    {
        dynamic sample = New("FlightStorySiteSample"); sample.Phase = phase; sample.SiteId = site; sample.At = at;
        sample.Deviations = new[] { deviation, deviation, deviation }; sample.Equipment = true; sample.Power = true; sample.Personnel = true; sample.Chant = true; sample.Glyph = true;
        sample.OutgoingMaintainer = handoff; sample.IncomingMaintainer = handoff; return sample;
    }
    private static void Feed(dynamic stability, dynamic phase, double first, double last, double deviation = 0.30d, string[] sites = null)
    {
        foreach (string site in sites ?? new[] { "A" })
        {
            // Seed each site's authorized report; later updates remain tied to their observation time.
            True(stability.Report(ReportObject(phase, site, first, (long)(first * 4d) + 1L), first));
        }
        for (double at = first; at <= last; at += 0.25d)
        {
            foreach (string site in sites ?? new[] { "A" })
            {
                if (at > first && at % 2d == 0d) True(stability.Report(ReportObject(phase, site, at, (long)(at * 4d) + 1L), at));
                True(stability.Sample(SampleObject(phase, site, at, deviation)), "valid sample at " + at + " for " + site);
            }
        }
    }
    private static dynamic StableThrough(double end = 30d, string run = "run", string[] sites = null)
    {
        dynamic result = Stability(run, sites); dynamic phase = Phase(run);
        foreach (string site in sites ?? new[] { "A" }) True(result.BeginHandoff(site, 0d));
        Feed(result, phase, 0d, 5d, 0.30d, sites);
        foreach (string site in sites ?? new[] { "A" }) True(result.CompleteHandoff(site, 5d));
        if (end >= 5.25d) Feed(result, phase, 5.25d, end, 0.30d, sites);
        return result;
    }

    private static void SceneFacts()
    {
        dynamic attempt = Attempt(); True(attempt.DefineScene("A", Requirement("rescued"))); True(attempt.DefineScene("B", Requirement("handoff")));
        False(attempt.TryAdvance()); False(attempt.SetFact("old-run", "A", "rescued", true)); False(attempt.SetFact("run", "B", "handoff", true));
        False(attempt.SetFact("run", "A", "dialogue_skipped", true)); False(attempt.TryAdvance());
        True(attempt.SetFact("run", "A", "rescued", true)); True(attempt.TryAdvance()); Equal("B", attempt.CurrentSceneId);
        False(attempt.SetFact("run", "A", "rescued", false)); False(attempt.TryAdvance());
        True(attempt.SetFact("run", "B", "handoff", true)); True(attempt.TryAdvance()); Equal("Success", attempt.Status.ToString());
    }
    private static dynamic C8Final()
    {
        dynamic attempt = Attempt("run", "ST-8-OP", C8);
        foreach (string scene in C8) True(attempt.DefineScene(scene, Requirement("authored")));
        True(attempt.SetFact("run", "C08-S01", "authored", true)); True(attempt.TryAdvance()); True(attempt.BeginCombat("run"));
        True(attempt.RegisterEncounter("resonance", EnumValue("FlightStoryRewardClass", "Final"), true)); True(attempt.TryResolveEncounter("run", "resonance", true));
        for (int i = 1; i < 6; i++) { True(attempt.SetFact("run", C8[i], "authored", true)); True(attempt.TryAdvance()); }
        return attempt;
    }
    private static void ChapterEight()
    {
        dynamic attempt = C8Final(); Equal("run", attempt.RunId); Equal("C08-S07", attempt.CurrentSceneId); Equal("Active", attempt.Status.ToString());
        Equal(null, attempt.CreateSettlementProposal()); True(attempt.TryProposeMissionCompletion("M-8-1", new[] { "C08-S01", "C08-S03" }));
        False(attempt.CanCommitMissionCompletions); False(attempt.TryAdvance());
    }
    private static void ChapterEightFinal()
    {
        dynamic attempt = C8Final(); True(attempt.SetFact("run", "C08-S07", "authored", true));
        foreach (string fact in FinalFacts) True(attempt.SetFact("run", "C08-S07", fact, true));
        False(attempt.TryAdvance()); dynamic manifest = ArrivedManifest(); dynamic stable = StableThrough(); True(attempt.AdvanceTime(30d, false));
        True(attempt.TryAdvance(manifest, stable)); dynamic proposal = attempt.CreateSettlementProposal(); True(proposal.Success); True(proposal.FinalClear); Equal(1, proposal.FinalCount);
        Equal(7, proposal.CompletedSceneIds.Length); True(attempt.CanCommitMissionCompletions);
        True(attempt.MarkSettled()); False(attempt.Fail(EnumValue("FlightStoryFailureOrigin", "Crash"))); True(attempt.CreateSettlementProposal().Success);
    }
    private static void EncounterLedger()
    {
        dynamic attempt = Attempt(); True(attempt.RegisterEncounter("ray", EnumValue("FlightStoryRewardClass", "Elite"), true));
        False(attempt.RegisterEncounter("ray", EnumValue("FlightStoryRewardClass", "Mid"), true)); False(attempt.TryResolveEncounter("run", "ray", true));
        True(attempt.BeginCombat("run")); False(attempt.TryResolveEncounter("run", "ray", false)); False(attempt.TryResolveEncounter("old", "ray", true));
        True(attempt.TryResolveEncounter("run", "ray", true)); False(attempt.TryResolveEncounter("run", "ray", true));
        True(attempt.RegisterEncounter("rock", EnumValue("FlightStoryRewardClass", "None"), false)); True(attempt.TryResolveEncounter("run", "rock", true));
        Equal(0, attempt.NormalCount); Equal(1, attempt.EliteCount); Equal(0, attempt.MidCount); Equal(0, attempt.FinalCount);
    }
    private static void FailurePolicies()
    {
        foreach (string origin in new[] { "Normal", "Retry", "Abandon", "Crash" })
        {
            dynamic attempt = C8Final(); True(attempt.Fail(EnumValue("FlightStoryFailureOrigin", origin))); dynamic proposal = attempt.CreateSettlementProposal();
            False(proposal.Success); False(proposal.FinalClear); True(proposal.ParticipationEligible); Equal(origin != "Crash", proposal.AwardEarnedRewards);
            Equal(origin == "Crash" ? 0 : 1, proposal.FinalCount); Equal(0, proposal.CompletedSceneIds.Length); Equal(0, proposal.ProposedMissionIds.Length);
            True(attempt.MarkSettled()); True(attempt.MarkSettled()); Equal(proposal.FinalCount, attempt.CreateSettlementProposal().FinalCount);
        }
    }
    private static void NoCombatRewards()
    {
        dynamic route = Attempt("run", "ST-7-3"); True(route.Fail(EnumValue("FlightStoryFailureOrigin", "Abandon"))); False(route.CreateSettlementProposal().ParticipationEligible);
        foreach (string mode in new[] { "Practice", "Recollection", "Side" })
        {
            dynamic attempt = Attempt("run", "ST-P-1", null, mode); True(attempt.BeginCombat("run"));
            True(attempt.RegisterEncounter("enemy", EnumValue("FlightStoryRewardClass", "Normal"), true)); True(attempt.TryResolveEncounter("run", "enemy", true));
            True(attempt.Fail(EnumValue("FlightStoryFailureOrigin", "Normal"))); dynamic result = attempt.CreateSettlementProposal();
            False(result.ParticipationEligible); False(result.AwardEarnedRewards); Equal(0, result.NormalCount);
        }
    }
    private static void FiniteWaves()
    {
        dynamic attempt = Attempt(); True(attempt.RegisterWave("A", "wave", 2)); False(attempt.RegisterWave("A", "wave", 3));
        False(attempt.TryClaimWaveSlot("run", "A", "wave", 0)); True(attempt.BeginCombat("run"));
        True(attempt.TryClaimWaveSlot("run", "A", "wave", 0)); False(attempt.TryClaimWaveSlot("run", "A", "wave", 0));
        True(attempt.TryClaimWaveSlot("run", "A", "wave", 1)); False(attempt.TryClaimWaveSlot("run", "A", "wave", 2));
        False(attempt.TryClaimWaveSlot("old", "A", "wave", 1)); False(attempt.TryClaimWaveSlot("run", "B", "wave", 0));
    }
    private static void ManifestIdentity()
    {
        dynamic empty = New("FlightStoryManifest", "run", Array.CreateInstance(Runtime("FlightStoryManifestEntry"), 0)); False(empty.Seal()); False(empty.AllArrived);
        dynamic manifest = Manifest("run", true); True(manifest.Seal());
        False(manifest.ConfirmArrival("run", "person-1", "safe-human")); True(manifest.ConfirmDeparture("run", "person-1"));
        False(manifest.ConfirmArrival("run", "person-1", "wrong")); False(manifest.ConfirmArrival("old", "person-1", "safe-human"));
        True(manifest.ConfirmArrival("run", "person-1", "safe-human")); False(manifest.AllArrived);
        True(manifest.ConfirmDeparture("run", "person-2")); True(manifest.ConfirmArrival("run", "person-2", "safe-operations")); True(manifest.AllArrived);
    }
    private static void ManifestDiscovery()
    {
        dynamic manifest = ArrivedManifest(); True(manifest.AllArrived);
        dynamic newcomer = New("FlightStoryManifestEntry"); newcomer.Id = "person-2"; newcomer.GroupId = "human"; newcomer.DestinationId = "safe-human";
        True(manifest.AddDiscovered(newcomer)); False(manifest.IsSealed); False(manifest.AllArrived); True(manifest.HasArrived("person-1"));
        newcomer.Id = "person-1"; newcomer.GroupId = "monster"; False(manifest.AddDiscovered(newcomer));
        True(manifest.ConfirmDeparture("run", "person-2")); True(manifest.ConfirmArrival("run", "person-2", "safe-human")); False(manifest.AllArrived);
        True(manifest.Seal()); True(manifest.AllArrived);
    }
    private static void ProtectedTargets()
    {
        dynamic ship = New("FlightStoryProtectedTarget", "run", "ship", 240d); dynamic platform = New("FlightStoryProtectedTarget", "run", "platform", 240d);
        True(ship.TryHit("run", "hit-1", 20d, 0d)); Equal(220d, ship.Durability); Equal(240d, platform.Durability);
        False(ship.TryHit("run", "hit-2", 20d, 0.5d)); False(ship.TryHit("run", "hit-2", 20d, 2d));
        False(ship.TryHit("old", "stale", 20d, 2d)); False(ship.TryHit("run", "paused", 20d, 2d, true));
        True(ship.TryHit("run", "hit-3", 219d, 2d)); Equal(1d, ship.Durability); False(ship.IsFailed);
        True(ship.TryHit("run", "hit-4", 1d, 3d)); True(ship.IsFailed); False(platform.IsFailed);
        False(ship.TryHit("run", "hit-5", 1d, 4d)); Equal(0d, ship.Durability);
    }
    private static void WorkAndHold()
    {
        dynamic work = New("FlightStoryProgress", 60d, false); dynamic hold = New("FlightStoryProgress", 5d, true);
        True(work.Step(20d, true)); True(hold.Step(4d, true)); work.Step(2d, false); hold.Step(2d, false);
        Equal(20d, work.Seconds); Equal(0d, hold.Seconds); work.Step(100d, true, true); Equal(20d, work.Seconds);
        work.Step(40d, true); hold.Step(5d, true); True(work.Complete); True(hold.Complete);
        hold.Step(1d, false); False(hold.Complete); False(work.Step(double.NaN, true));
    }
    private static void ActiveClock()
    {
        dynamic attempt = Attempt(); True(attempt.AdvanceTime(1d, false)); False(attempt.AdvanceTime(600d, true)); Equal(1d, attempt.Now);
        False(attempt.AdvanceTime(double.PositiveInfinity, false)); False(attempt.AdvanceTime(-1d, false)); Equal(1d, attempt.Now);
    }
    private static void TimestampWindow()
    {
        dynamic stable = StableThrough(29.75d); False(stable.IsSustainedQualified(29.75d));
        True(stable.Report(ReportObject(Phase(), "A", 30d, 121L), 30d)); True(stable.Sample(SampleObject(Phase(), "A", 30d))); True(stable.IsSustainedQualified(30d));
    }
    private static void CommonWindow()
    {
        dynamic stable = Stability("run", new[] { "A", "B" }); dynamic phase = Phase(); True(stable.BeginHandoff("A", 0d)); True(stable.BeginHandoff("B", 0d));
        Feed(stable, phase, 0d, 5d, 0.30d, new[] { "A" }); True(stable.CompleteHandoff("A", 5d)); Feed(stable, phase, 5.25d, 30d, 0.30d, new[] { "A" });
        Feed(stable, phase, 31d, 36d, 0.30d, new[] { "B" }); True(stable.CompleteHandoff("B", 36d)); Feed(stable, phase, 36.25d, 61d, 0.30d, new[] { "B" });
        False(stable.IsSustainedQualified(61d));
    }
    private static void MissingSample()
    {
        dynamic stable = StableThrough(20d); True(stable.Sample(SampleObject(Phase(), "A", 20.5d))); False(stable.IsSustainedQualified(20.5d));
        Feed(stable, Phase(), 20.75d, 50.25d); False(stable.IsSustainedQualified(50.25d));
        True(stable.Sample(SampleObject(Phase(), "A", 50.5d))); True(stable.IsSustainedQualified(50.5d));
    }
    private static void SampleCadence()
    {
        dynamic stable = Stability(); dynamic phase = Phase(); True(stable.Report(ReportObject(phase, "A", 0d, 1L), 0d)); True(stable.Sample(SampleObject(phase, "A", 0d)));
        False(stable.Sample(SampleObject(phase, "A", 0d))); False(stable.Sample(SampleObject(phase, "A", 0.1d))); False(stable.IsBackupQualified(0.1d));
        True(stable.Sample(SampleObject(phase, "A", 0.25d))); False(stable.IsSustainedQualified(0.25d));
    }
    private static void ReportFreshness()
    {
        dynamic stable = Stability(); dynamic phase = Phase(); True(stable.Report(ReportObject(phase, "A", 0d, 1L), 0d));
        for (double at = 0d; at <= 6d; at += 0.25d) True(stable.Sample(SampleObject(phase, "A", at, 0.4d)));
        True(stable.IsBackupQualified(6d)); False(stable.Sample(SampleObject(phase, "A", 6.25d, 0.4d))); False(stable.IsBackupQualified(6.25d));
        True(stable.Report(ReportObject(phase, "A", 6.25d, 2L), 6.25d)); True(stable.Sample(SampleObject(phase, "A", 6.5d, 0.4d))); False(stable.IsBackupQualified(6.5d));
    }
    private static void EarlyUnsafeObservation()
    {
        dynamic stable = StableThrough();
        False(stable.Sample(SampleObject(Phase(), "A", 30.1d, 0.31d)));
        False(stable.IsSustainedQualified(30.1d));
        True(stable.IsBackupQualified(30.1d));
    }
    private static void ReportTags()
    {
        foreach (object wrong in new object[] { Phase("run", "old", 1), Phase("run", "maintenance", 2), Phase("run", "maintenance", 1, "wrong") })
        {
            dynamic stable = StableThrough(); False(stable.Report(ReportObject(wrong, "A", 30d, 999L), 30d)); False(stable.IsSustainedQualified(30d));
        }
    }
    private static void ReportOrder()
    {
        dynamic stable = StableThrough(); False(stable.Report(ReportObject(Phase(), "A", 28d, 113L), 30d)); False(stable.IsSustainedQualified(30d));
        dynamic fresh = StableThrough(); False(fresh.Report(ReportObject(Phase(), "A", 30d, 121L), 30d)); True(fresh.IsSustainedQualified(30d));
        False(fresh.Report(ReportObject(Phase(), "A", 31d, 121L), 31d)); False(fresh.IsSustainedQualified(31d));
    }
    private static void BackupWindow()
    {
        dynamic stable = Stability(); Feed(stable, Phase(), 0d, 4.75d, 0.4d); False(stable.IsBackupQualified(4.75d));
        True(stable.Sample(SampleObject(Phase(), "A", 5d, 0.4d))); True(stable.IsBackupQualified(5d)); False(stable.IsSustainedQualified(5d));
        True(stable.Sample(SampleObject(Phase(), "A", 5.25d, 0.4001d))); False(stable.IsBackupQualified(5.25d));
    }
    private static void ReportWatermark()
    {
        dynamic stable = StableThrough();
        False(stable.Report(ReportObject(Phase("run", "wrong", 1), "A", 30.25d, 122L), 30.25d));
        False(stable.Report(ReportObject(Phase(), "A", 30.25d, 10L), 30.25d), "old sequence must stay rejected after invalidation");
        True(stable.Report(ReportObject(Phase(), "A", 30.25d, 122L), 30.25d));
        False(stable.IsSustainedQualified(30.25d));
    }
    private static void HandoffOverlap()
    {
        dynamic stable = Stability(); True(stable.BeginHandoff("A", 0d)); Feed(stable, Phase(), 0d, 4.75d);
        False(stable.CompleteHandoff("A", 4.75d)); True(stable.Sample(SampleObject(Phase(), "A", 5d))); True(stable.CompleteHandoff("A", 5d));
        False(stable.CompleteHandoff("A", 5d));
    }
    private static void PostHandoff()
    {
        dynamic stable = Stability(); Feed(stable, Phase(), 0d, 20d); True(stable.BeginHandoff("A", 20d)); Feed(stable, Phase(), 20.25d, 25.25d);
        True(stable.CompleteHandoff("A", 25.25d)); Feed(stable, Phase(), 25.5d, 35d); False(stable.IsSustainedQualified(35d));
        True(stable.Sample(SampleObject(Phase(), "A", 35.25d))); True(stable.IsSustainedQualified(35.25d));
    }
    private static void LiveConditions()
    {
        foreach (string name in new[] { "Equipment", "Power", "Personnel", "Chant", "Glyph" })
        {
            dynamic stable = StableThrough(); object sample = SampleObject(Phase(), "A", 30.25d);
            Runtime("FlightStorySiteSample").GetField(name).SetValue(sample, false);
            False(stable.Sample((dynamic)sample)); False(stable.IsSustainedQualified(30.25d));
        }
    }
    private static void RevocationKeepsArrivals()
    {
        dynamic stable = StableThrough(); dynamic manifest = ArrivedManifest(); True(stable.IsSustainedQualified(30d));
        True(stable.Sample(SampleObject(Phase(), "A", 30.25d, 0.31d))); False(stable.IsSustainedQualified(30.25d)); True(manifest.AllArrived);
        Feed(stable, Phase(), 30.5d, 60.5d); True(stable.IsSustainedQualified(60.5d)); True(manifest.AllArrived);
    }
    private static void InvalidSamples()
    {
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, -0.1d })
        {
            dynamic stable = StableThrough(); False(stable.Sample(SampleObject(Phase(), "A", 30.25d, bad))); False(stable.IsSustainedQualified(30.25d));
        }
        dynamic other = StableThrough(); dynamic incomplete = SampleObject(Phase(), "A", 30.25d); incomplete.Deviations = new[] { 0.1d, 0.1d };
        False(other.Sample(incomplete)); False(other.IsSustainedQualified(30.25d));
        False(other.Sample(SampleObject(Phase(), "removed-site", 30.5d)));
    }
    private static void ImmutableScope()
    {
        string[] sites = { "A", "B" }; dynamic stable = Stability("run", sites); sites[1] = "A";
        Feed(stable, Phase(), 0d, 5d, 0.30d, new[] { "A" }); False(stable.IsBackupQualified(5d));
        dynamic phase = Phase(); True(stable.BeginPhase(Phase("run", "next", 2), 5d)); False(stable.Report(ReportObject(phase, "A", 5d, 99L), 5d));
        string[] scenes = { "A", "B" }; dynamic attempt = Attempt("run", "ST-X", scenes); scenes[0] = "B"; Equal("A", attempt.CurrentSceneId);
    }
    private static void TerminalStates()
    {
        dynamic attempt = Attempt(); True(attempt.DefineScene("A", Requirement("done"))); True(attempt.BeginCombat("run"));
        True(attempt.Fail(EnumValue("FlightStoryFailureOrigin", "Normal"))); False(attempt.SetFact("run", "A", "done", true)); False(attempt.TryAdvance());
        False(attempt.TryProposeMissionCompletion("mission", new[] { "A" })); True(attempt.MarkSettled()); False(attempt.AdvanceTime(1d, false));
        dynamic next = Attempt("next-run"); Equal(0d, next.Now); False(next.SetFact("run", "A", "done", true));
    }
    private static void TypedGates()
    {
        dynamic attempt = Attempt("run", "ST-X", new[] { "A" }); dynamic required = Requirement("ready");
        required.ProtectedTargets = new[] { "ship", "platform" }; required.AllArrivals = true; True(attempt.DefineScene("A", required));
        required.ProtectedTargets[1] = "ship"; True(attempt.SetFact("run", "A", "ready", true));
        dynamic ship = New("FlightStoryProtectedTarget", "run", "ship", 10d); dynamic platform = New("FlightStoryProtectedTarget", "run", "platform", 10d);
        Array targets = Array.CreateInstance(Runtime("FlightStoryProtectedTarget"), 2); targets.SetValue(ship, 0); targets.SetValue(platform, 1);
        False(attempt.TryAdvance(ArrivedManifest(), null)); False(attempt.TryAdvance(ArrivedManifest("old"), null, (dynamic)targets));
        True(ship.TryHit("run", "hit", 9d, 0d)); True(attempt.TryAdvance(ArrivedManifest(), null, (dynamic)targets));
    }
    private static dynamic Point(double x, double y, double z) => New("FlightStoryPoint", x, y, z);
    private static (bool, double) Sweep(dynamic start, dynamic end, dynamic center, double radius)
    {
        object[] args = { start, end, center, radius, 0d };
        bool hit = (bool)Runtime("FlightStorySweep").GetMethod("TrySphere").Invoke(null, args);
        return (hit, (double)args[4]);
    }
    private static void SweptProtection()
    {
        var crossing = Sweep(Point(-2d, 0d, 0d), Point(2d, 0d, 0d), Point(0d, 0d, 0d), 1d); True(crossing.Item1); Equal(0.25d, crossing.Item2);
        var tangent = Sweep(Point(-2d, 1d, 0d), Point(2d, 1d, 0d), Point(0d, 0d, 0d), 1d); True(tangent.Item1); Equal(0.5d, tangent.Item2);
        var inside = Sweep(Point(0d, 0d, 0d), Point(0d, 0d, 0d), Point(0d, 0d, 0d), 1d); True(inside.Item1); Equal(0d, inside.Item2);
        False(Sweep(Point(2d, 0d, 0d), Point(2d, 0d, 0d), Point(0d, 0d, 0d), 1d).Item1);
        False(Sweep(Point(0d, 0d, 0d), Point(2d, 0d, 0d), Point(-4d, 0d, 0d), 1d).Item1);
        False(Sweep(Point(double.NaN, 0d, 0d), Point(2d, 0d, 0d), Point(0d, 0d, 0d), 1d).Item1);
        False(Sweep(Point(-2d, 0d, 0d), Point(2d, 0d, 0d), Point(0d, 0d, 0d), -1d).Item1);
    }
}
