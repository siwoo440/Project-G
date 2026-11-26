using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectG
{
    [Serializable]
    public sealed class FlightStoryCatalog
    {
        public int schemaVersion;
        public string sourceUrl, sourceCommit, sourceDate;
        public FlightStorySourceHash[] sourceHashes = Array.Empty<FlightStorySourceHash>();
        public FlightStoryMission[] missions = Array.Empty<FlightStoryMission>();
        public FlightStoryStage[] stages = Array.Empty<FlightStoryStage>();
        public FlightStoryScene[] scenes = Array.Empty<FlightStoryScene>();
        public FlightStoryObjective[] objectives = Array.Empty<FlightStoryObjective>();
        public FlightStoryLine[] lines = Array.Empty<FlightStoryLine>();
        public FlightStoryNumericGoal[] numericGoals = Array.Empty<FlightStoryNumericGoal>();
        public FlightStoryWave[] waves = Array.Empty<FlightStoryWave>();
        public FlightStoryArena[] arenas = Array.Empty<FlightStoryArena>();
        public FlightStorySourceRule[] rules = Array.Empty<FlightStorySourceRule>();

        public FlightStoryStage Stage(string id) => Array.Find(stages, x => x.id == id);
        public FlightStoryScene Scene(string id) => Array.Find(scenes, x => x.id == id);
        public FlightStoryMission Mission(string id) => Array.Find(missions, x => x.id == id);
        public FlightStoryObjective Objective(string sceneId) => Array.Find(objectives, x => x.sceneId == sceneId);
        public FlightStoryNumericGoal NumericGoal(string sceneId) => Array.Find(numericGoals, x => x.sceneId == sceneId);

        public string[] Validate()
        {
            var errors = new List<string>();
            if (schemaVersion != 1) errors.Add("Unsupported story catalog version.");
            CheckIds(missions?.Select(x => x?.id), 32, "missions", errors);
            CheckIds(scenes?.Select(x => x?.id), 100, "scenes", errors);
            CheckIds(objectives?.Select(x => x?.id), 100, "objectives", errors);
            CheckIds(lines?.Select(x => x?.id), 1034, "script rows", errors);
            CheckIds(numericGoals?.Select(x => x?.id), 22, "numeric goals", errors);
            CheckIds(waves?.Select(x => x?.id), 26, "waves", errors);
            CheckIds(arenas?.Select(x => x?.id), 13, "arenas", errors);
            CheckIds(stages?.Select(x => x?.id), 30, "flow groups", errors);
            if (errors.Count != 0) return errors.ToArray();
            var sceneIds = new HashSet<string>(scenes.Select(s => s.id), StringComparer.Ordinal);
            var ownership = missions.SelectMany(m => m.sceneIds ?? Array.Empty<string>()).ToArray();
            if (ownership.Length != 100 || ownership.Distinct().Count() != 100 || ownership.Any(x => !sceneIds.Contains(x)))
                errors.Add("Every scene must belong to exactly one mission.");
            foreach (FlightStoryObjective item in objectives)
            {
                if (!sceneIds.Contains(item.sceneId) || Mission(item.missionId) == null || string.IsNullOrWhiteSpace(item.completionEvent))
                    errors.Add("Invalid objective reference: " + item.id);
                if (item.clauses == null || item.clauses.Length == 0 || item.clauses.Any(x => x == null || string.IsNullOrWhiteSpace(x.id)))
                    errors.Add("Missing evidence requirements: " + item.id);
            }
            foreach (FlightStoryLine line in lines)
                if (!sceneIds.Contains(line.sceneId)) errors.Add("Orphan script row: " + line.id);
            foreach (FlightStoryStage stage in stages)
            {
                if (stage.sceneIds == null || stage.sceneIds.Length == 0 || stage.sceneIds.Any(x => !sceneIds.Contains(x)))
                    errors.Add("Invalid ordered flow: " + stage.id);
                if (stage.hasCombat && !stage.sceneIds.Contains(stage.settlementScene))
                    errors.Add("Missing settlement boundary: " + stage.id);
            }
            foreach (FlightStoryWave wave in waves)
            {
                FlightStoryStage stage = Stage(wave.stageId);
                if (stage == null || !stage.hasCombat || !stage.sceneIds.Contains(wave.sceneId) || wave.maxAlive < 1 || wave.offset < 0 || double.IsNaN(wave.offset))
                    errors.Add("Invalid finite wave: " + wave.id);
                if (wave.composition == null || wave.composition.Length == 0 || wave.composition.Any(x => x == null || x.count < 1 || string.IsNullOrWhiteSpace(x.enemy)))
                    errors.Add("Empty enemy slots: " + wave.id);
            }
            FlightStoryStage c8 = Stage("ST-8-OP");
            if (c8 == null || c8.settlementScene != "C08-S07" || !c8.sceneIds.SequenceEqual(Enumerable.Range(1, 7).Select(i => "C08-S" + i.ToString("00"))))
                errors.Add("C8 requires one ordered attempt and S07 settlement.");
            if (waves.Count(x => x.stageId == "ST-8-OP") != 1 || waves.Any(x => x.stageId == "ST-8-OP" && x.sceneId != "C08-S02"))
                errors.Add("C8 combat is restricted to S02.");
            return errors.ToArray();
        }

        private static void CheckIds(IEnumerable<string> ids, int expected, string label, List<string> errors)
        {
            string[] values = ids?.ToArray() ?? Array.Empty<string>();
            if (values.Length != expected || values.Any(string.IsNullOrWhiteSpace) || values.Distinct(StringComparer.Ordinal).Count() != expected)
                errors.Add("Invalid count or duplicate ID in " + label + ".");
        }
    }

    [Serializable] public sealed class FlightStorySourceHash { public string name, sha256; }
    [Serializable] public sealed class FlightStoryMission
    {
        public string id, chapter, title, kind, goal, cast, stageId, progressRules, rewardProfile, sourceStatus;
        public string[] sceneIds = Array.Empty<string>();
    }
    [Serializable] public sealed class FlightStoryStage
    {
        public string id, title, chapter, settlementScene;
        public string[] missionIds = Array.Empty<string>(), sceneIds = Array.Empty<string>();
        public bool hasCombat, optional;
    }
    [Serializable] public sealed class FlightStoryScene
    {
        public string id, chapter, title, location, cast, startConditions, purpose, exitConditions, missionId, objectiveId;
    }
    [Serializable] public sealed class FlightStoryObjective
    {
        public string id, missionId, sceneId, kind, goal, startConditions, successCriteria, failureCriteria, completionEvent, record, notes;
        public FlightStoryClause[] clauses = Array.Empty<FlightStoryClause>();
        public string[] RequiredEvidence => clauses.Where(x => x.kind != "Persistence").Select(x => x.id).ToArray();
    }
    [Serializable] public sealed class FlightStoryClause { public string id, text, kind; }
    [Serializable] public sealed class FlightStoryLine { public string id, sceneId, kind, speaker, text, action, camera; }
    [Serializable] public sealed class FlightStoryNumericGoal
    {
        public string id, sceneId, stageId, kind, protectedTarget, success, wait, recoverable, terminal, notes;
        public bool hasDurability, hasProgress;
        public double durability, progressSeconds, routeLength, speed;
    }
    [Serializable] public sealed class FlightStoryWave
    {
        public string id, sceneId, stageId, trigger, spawn, exit, goal, notes;
        public double offset;
        public int maxAlive;
        public FlightStoryEnemySlot[] composition = Array.Empty<FlightStoryEnemySlot>();
        public int SlotCount => composition.Sum(x => x.count);
    }
    [Serializable] public sealed class FlightStoryEnemySlot { public string enemy; public int count; }
    [Serializable] public sealed class FlightStoryArena
    {
        public string id, stageId, title, scenes, volume, entryPoints, protectedTargets, placement, transformRules, safety, completionNotes, sourceStatus;
    }
    [Serializable] public sealed class FlightStorySourceRule { public string table, id; public string[] values = Array.Empty<string>(); }
}
