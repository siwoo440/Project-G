using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProjectG;

internal static class Program
{
    private static int Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : "Assets/ProjectG/Resources/Story/ProjectGStory.json";
        string json = File.ReadAllText(path);
        Func<FlightStoryCatalog> fresh = () => JsonSerializer.Deserialize<FlightStoryCatalog>(json, new JsonSerializerOptions { IncludeFields = true });
        int count = 0;
        try
        {
            Check(fresh().Validate().Length == 0, "published catalog satisfies runtime references", ref count);
            FlightStoryCatalog bad = fresh();
            bad.scenes[1].id = bad.scenes[0].id;
            Check(bad.Validate().Length > 0, "duplicate IDs are rejected", ref count);
            bad = fresh(); bad.lines[100].sceneId = "deleted-scene";
            Check(bad.Validate().Length > 0, "orphan original dialogue is rejected", ref count);
            bad = fresh(); bad.Stage("ST-8-OP").settlementScene = "C08-S02";
            Check(bad.Validate().Length > 0, "boss suppression cannot become C8 settlement", ref count);
            bad = fresh(); Array.Reverse(bad.Stage("ST-8-OP").sceneIds);
            Check(bad.Validate().Length > 0, "C8 mission ownership cannot reorder S02/S03", ref count);
            bad = fresh(); bad.waves[0].stageId = "ST-5-2";
            Check(bad.Validate().Length > 0, "safe story segment rejects added fight", ref count);
            bad = fresh(); bad.objectives[0].clauses = Array.Empty<FlightStoryClause>();
            Check(bad.Validate().Length > 0, "empty evidence list cannot silently complete", ref count);
            bad = fresh();
            Check(bad.Stage("ST-P-1").settlementScene == "P-S04" && bad.Stage("ST-P-1").sceneIds.Last() == "P-S07", "combat and whole-mission boundaries remain distinct", ref count);
            Check(bad.Objective("P-S04").clauses.Any(x => x.kind == "Persistence") && bad.Objective("P-S04").RequiredEvidence.Length == 3, "atomic-write effect is not a circular prerequisite", ref count);
            Check(FlightStoryRunPlan.RequiredScenes(bad.Stage("M-E-1")).SequenceEqual(new[] { "E-S01", "E-S02", "E-S03", "E-S04" }) && bad.Scene("E-S05") != null, "canonical ending succeeds without opting into optional E-S05", ref count);
            Console.WriteLine("PASS " + count + " catalog runtime cases (production source; no Unity execution)");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void Check(bool ok, string name, ref int count)
    {
        if (!ok) throw new Exception("FAIL: " + name);
        count++;
        Console.WriteLine("PASS " + name);
    }
}
