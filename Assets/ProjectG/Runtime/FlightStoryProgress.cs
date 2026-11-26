using System;
using System.Collections.Generic;

namespace ProjectG
{
    [Serializable]
    public sealed class FlightStoryCheckpoint
    {
        public string MissionId;
        public string NextSceneId;
        public string[] SeenScenes = Array.Empty<string>();
        public string[] SceneEvents = Array.Empty<string>();
        public string[] Records = Array.Empty<string>();
        public string[] CompletedMissions = Array.Empty<string>();
        public string[] CompletedChapters = Array.Empty<string>();
        public string[] CompletedSegments = Array.Empty<string>();
        public bool OperationClear;
        public bool RecollectionOnly;
    }

    [Serializable]
    public sealed class FlightStoryCheckpointEntry
    {
        public string MissionId;
        public string NextSceneId;
    }

    [Serializable]
    public sealed class FlightStoryProgressData
    {
        public string[] SeenScenes = Array.Empty<string>();
        public string[] SceneEvents = Array.Empty<string>();
        public string[] Records = Array.Empty<string>();
        public string[] CompletedMissions = Array.Empty<string>();
        public string[] CompletedChapters = Array.Empty<string>();
        public string[] CompletedSegments = Array.Empty<string>();
        public FlightStoryCheckpointEntry[] Checkpoints = Array.Empty<FlightStoryCheckpointEntry>();
        public bool OperationClear;
    }

    // These rules validate the durable transaction shape. The mission director owns live safety proof.
    public static class FlightStoryProgressRules
    {
        private sealed class MissionRule
        {
            internal readonly string Id;
            internal readonly string Chapter;
            internal readonly string Stage;
            internal readonly string[] Events;

            internal MissionRule(string id, string chapter, string stage, params string[] scenes)
            {
                Id = id;
                Chapter = chapter;
                Stage = stage;
                Events = new string[scenes.Length];
                for (int index = 0; index < scenes.Length; index++) Events[index] = EventFor(scenes[index]);
            }
        }

        private sealed class StageRule
        {
            internal readonly string Id;
            internal readonly string Mission;
            internal readonly string BoundaryEvent;
            internal readonly string[] CombatEvents;

            internal StageRule(string id, string mission, string boundary, params string[] scenes)
            {
                Id = id;
                Mission = mission;
                BoundaryEvent = EventFor(boundary);
                CombatEvents = new string[scenes.Length];
                for (int index = 0; index < scenes.Length; index++) CombatEvents[index] = EventFor(scenes[index]);
            }
        }

        private const string OperationStage = "ST-8-OP";
        private static readonly string[] OperationMissions = { "M-8-1", "M-8-2", "M-8-3" };
        private static readonly string[] OperationEvents =
        {
            "EV_C08_S01_COMPLETE", "EV_C08_S02_COMPLETE", "EV_C08_S03_COMPLETE", "EV_C08_S04_COMPLETE",
            "EV_C08_S05_COMPLETE", "EV_C08_S06_COMPLETE", "EV_C08_S07_COMPLETE"
        };

        // The combat boundary is deliberately independent from the later full mission boundary.
        private static readonly StageRule[] Stages =
        {
            new StageRule("ST-P-1", "M-P-1", "P-S04", "P-S02", "P-S03", "P-S04"),
            new StageRule("ST-1-1", "M-1-1", "C01-S03", "C01-S03"),
            new StageRule("ST-1-2", "M-1-2", "C01-S04", "C01-S04"),
            new StageRule("ST-2-2", "M-2-2", "C02-S03", "C02-S02", "C02-S03"),
            new StageRule("ST-3-1", "M-3-1", "C03-S04", "C03-S04"),
            new StageRule("ST-3-2", "M-3-2", "C03-S06", "C03-S06"),
            new StageRule("ST-4-2", "M-4-2", "C04-S03", "C04-S03"),
            new StageRule("ST-5-1", "M-5-1", "C05-S02", "C05-S02"),
            new StageRule("ST-5-3", "M-5-3", "C05-S06", "C05-S06"),
            new StageRule("ST-6-1", "M-6-1", "C06-S02", "C06-S02"),
            new StageRule("ST-7-1", "M-7-1", "C07-S02", "C07-S02"),
            new StageRule("ST-7-2", "M-7-2", "C07-S06", "C07-S05", "C07-S06"),
            new StageRule(OperationStage, "M-8-3", "C08-S07", "C08-S01", "C08-S02", "C08-S03", "C08-S04", "C08-S05", "C08-S06", "C08-S07")
        };

        private static readonly MissionRule[] Missions = CreateMissions();

        public static bool IsCombatStage(string stageId) => FindStage(stageId) != null;

        public static bool QualifiesFinalClear(bool cleared, bool finalBoss) => cleared && finalBoss;

        public static FlightStoryProgressData Copy(FlightStoryProgressData source)
        {
            source ??= new FlightStoryProgressData();
            var checkpoints = new List<FlightStoryCheckpointEntry>();
            foreach (FlightStoryCheckpointEntry entry in source.Checkpoints ?? Array.Empty<FlightStoryCheckpointEntry>())
            {
                if (entry != null) checkpoints.Add(new FlightStoryCheckpointEntry { MissionId = entry.MissionId, NextSceneId = entry.NextSceneId });
            }
            return new FlightStoryProgressData
            {
                SeenScenes = Union(source.SeenScenes, null),
                SceneEvents = Union(source.SceneEvents, null),
                Records = Union(source.Records, null),
                CompletedMissions = Union(source.CompletedMissions, null),
                CompletedChapters = Union(source.CompletedChapters, null),
                CompletedSegments = Union(source.CompletedSegments, null),
                Checkpoints = checkpoints.ToArray(),
                OperationClear = source.OperationClear
            };
        }

        public static bool TryCheckpoint(FlightStoryProgressData current, FlightStoryCheckpoint patch, bool hasPendingCombat, out FlightStoryProgressData next)
        {
            next = null;
            if (patch == null || !IsValid(current)) return false;
            FlightStoryProgressData before = Copy(current);
            if (patch.RecollectionOnly)
            {
                if (!ValidStrings(patch.SeenScenes)) return false;
                before.SeenScenes = Union(before.SeenScenes, patch.SeenScenes);
                next = before;
                return true;
            }
            if (hasPendingCombat || HasOperationProposal(patch) || IsOperationCheckpoint(patch.MissionId, patch.NextSceneId)) return false;
            foreach (string segment in patch.CompletedSegments ?? Array.Empty<string>())
            {
                if (!Contains(before.CompletedSegments, segment)) return false;
            }
            return TryMerge(before, patch, out next);
        }

        public static bool TrySettlement(FlightStoryProgressData current, FlightStoryCheckpoint patch, string stageId, bool cleared, bool recovered, out FlightStoryProgressData next)
        {
            next = null;
            StageRule stage = FindStage(stageId);
            if (stage == null || !IsValid(current)) return false;
            FlightStoryProgressData before = Copy(current);
            if (recovered || !cleared)
            {
                next = before;
                return true;
            }
            if (patch == null || patch.RecollectionOnly || patch.MissionId != stage.Mission
                || !Contains(patch.CompletedSegments, stage.Id) || !Contains(patch.SceneEvents, stage.BoundaryEvent)) return false;
            foreach (string segment in patch.CompletedSegments ?? Array.Empty<string>())
            {
                if (segment != stage.Id && !Contains(before.CompletedSegments, segment)) return false;
            }
            if (stageId == OperationStage)
            {
                // Every item must belong to this proposal; old success cannot replace current-attempt proof.
                if (!patch.OperationClear || !All(patch.SceneEvents, OperationEvents) || !All(patch.CompletedMissions, OperationMissions)
                    || !Contains(patch.CompletedChapters, "C08") || IsC8Scene(patch.NextSceneId)) return false;
            }
            else if (HasOperationProposal(patch) || IsOperationCheckpoint(patch.MissionId, patch.NextSceneId)) return false;
            return TryMerge(before, patch, out next);
        }

        public static bool IsValid(FlightStoryProgressData state)
        {
            if (state == null) return true;
            if (!ValidStrings(state.SeenScenes) || !ValidStrings(state.SceneEvents) || !ValidStrings(state.Records)
                || !ValidStrings(state.CompletedMissions) || !ValidStrings(state.CompletedChapters) || !ValidStrings(state.CompletedSegments)) return false;
            var checkpointIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (FlightStoryCheckpointEntry entry in state.Checkpoints ?? Array.Empty<FlightStoryCheckpointEntry>())
            {
                if (entry == null || string.IsNullOrEmpty(entry.MissionId) || string.IsNullOrEmpty(entry.NextSceneId)
                    || !checkpointIds.Add(entry.MissionId) || IsOperationCheckpoint(entry.MissionId, entry.NextSceneId)
                    || IsUnsafeCombatCheckpoint(state, entry.NextSceneId)) return false;
            }
            if (!state.OperationClear && !HasC8(state.SceneEvents, "EV_C08_") && !HasC8(state.Records, "C08-")
                && !HasC8(state.CompletedMissions, "M-8-") && !Contains(state.CompletedChapters, "C08") && !Contains(state.CompletedSegments, OperationStage)) return true;
            return state.OperationClear && All(state.SceneEvents, OperationEvents) && All(state.CompletedMissions, OperationMissions)
                && Contains(state.CompletedChapters, "C08") && Contains(state.CompletedSegments, OperationStage);
        }

        private static bool TryMerge(FlightStoryProgressData before, FlightStoryCheckpoint patch, out FlightStoryProgressData next)
        {
            next = null;
            if (!ValidStrings(patch.SeenScenes) || !ValidStrings(patch.SceneEvents) || !ValidStrings(patch.Records)
                || !ValidStrings(patch.CompletedMissions) || !ValidStrings(patch.CompletedChapters) || !ValidStrings(patch.CompletedSegments)) return false;
            FlightStoryProgressData merged = Copy(before);
            merged.SeenScenes = Union(before.SeenScenes, patch.SeenScenes);
            merged.SceneEvents = Union(before.SceneEvents, patch.SceneEvents);
            merged.Records = Union(before.Records, patch.Records);
            merged.CompletedMissions = Union(before.CompletedMissions, patch.CompletedMissions);
            merged.CompletedChapters = Union(before.CompletedChapters, patch.CompletedChapters);
            merged.CompletedSegments = Union(before.CompletedSegments, patch.CompletedSegments);
            merged.OperationClear |= patch.OperationClear;
            foreach (StageRule stage in Stages)
            {
                foreach (string sceneEvent in stage.CombatEvents)
                {
                    if (Contains(patch.SceneEvents, sceneEvent) && !Contains(merged.CompletedSegments, stage.Id)) return false;
                }
            }
            foreach (string id in patch.CompletedMissions ?? Array.Empty<string>())
            {
                if (Contains(before.CompletedMissions, id)) continue;
                MissionRule mission = FindMission(id);
                if (mission == null || !All(merged.SceneEvents, mission.Events)
                    || (mission.Stage != null && !Contains(merged.CompletedSegments, mission.Stage))) return false;
            }
            foreach (string chapter in patch.CompletedChapters ?? Array.Empty<string>())
            {
                if (Contains(before.CompletedChapters, chapter)) continue;
                bool known = false;
                foreach (MissionRule mission in Missions)
                {
                    if (mission.Chapter != chapter) continue;
                    known = true;
                    if (!Contains(merged.CompletedMissions, mission.Id)) return false;
                }
                if (!known) return false;
            }
            var checkpoints = new List<FlightStoryCheckpointEntry>();
            foreach (FlightStoryCheckpointEntry entry in merged.Checkpoints)
            {
                if (Contains(merged.CompletedMissions, entry.MissionId)) continue;
                if (!string.IsNullOrEmpty(patch.NextSceneId) && entry.MissionId == patch.MissionId) continue;
                checkpoints.Add(entry);
            }
            if (!string.IsNullOrEmpty(patch.NextSceneId) && !Contains(merged.CompletedMissions, patch.MissionId))
            {
                if (FindMission(patch.MissionId) == null || IsUnsafeCombatCheckpoint(merged, patch.NextSceneId)) return false;
                checkpoints.Add(new FlightStoryCheckpointEntry { MissionId = patch.MissionId, NextSceneId = patch.NextSceneId });
            }
            merged.Checkpoints = checkpoints.ToArray();
            if (!IsValid(merged)) return false;
            next = merged;
            return true;
        }

        private static bool HasOperationProposal(FlightStoryCheckpoint patch)
        {
            bool operationRecord = patch.MissionId?.StartsWith("M-8-", StringComparison.Ordinal) == true
                && ((patch.SceneEvents?.Length ?? 0) != 0 || (patch.Records?.Length ?? 0) != 0);
            return operationRecord || patch.OperationClear || HasC8(patch.SceneEvents, "EV_C08_") || HasC8(patch.Records, "C08-")
                || HasC8(patch.CompletedMissions, "M-8-") || Contains(patch.CompletedChapters, "C08") || Contains(patch.CompletedSegments, OperationStage);
        }

        private static bool IsUnsafeCombatCheckpoint(FlightStoryProgressData state, string nextScene)
        {
            if (string.IsNullOrEmpty(nextScene)) return false;
            string nextEvent = EventFor(nextScene);
            foreach (StageRule stage in Stages)
            {
                if (Array.IndexOf(stage.CombatEvents, nextEvent) > 0 && !Contains(state.CompletedSegments, stage.Id)) return true;
            }
            return false;
        }

        private static bool IsOperationCheckpoint(string mission, string scene)
        {
            return !string.IsNullOrEmpty(scene) && ((mission?.StartsWith("M-8-", StringComparison.Ordinal) ?? false) || IsC8Scene(scene));
        }

        private static bool IsC8Scene(string scene) => scene?.StartsWith("C08-", StringComparison.Ordinal) == true;
        private static string EventFor(string scene) => "EV_" + scene.Replace('-', '_') + "_COMPLETE";
        private static StageRule FindStage(string id) => Array.Find(Stages, stage => stage.Id == id);
        private static MissionRule FindMission(string id) => Array.Find(Missions, mission => mission.Id == id);
        private static bool Contains(string[] values, string value) => values != null && Array.IndexOf(values, value) >= 0;
        private static bool HasC8(string[] values, string prefix) => values != null && Array.Exists(values, value => value?.StartsWith(prefix, StringComparison.Ordinal) == true);
        private static bool ValidStrings(string[] values) => values == null || !Array.Exists(values, string.IsNullOrWhiteSpace);

        private static bool All(string[] values, string[] required)
        {
            foreach (string value in required) if (!Contains(values, value)) return false;
            return true;
        }

        private static string[] Union(string[] existing, string[] added)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<string>();
            foreach (string[] values in new[] { existing, added })
            {
                foreach (string value in values ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrEmpty(value) && seen.Add(value)) result.Add(value);
                }
            }
            return result.ToArray();
        }

        private static MissionRule[] CreateMissions()
        {
            var result = new List<MissionRule>
            {
                new MissionRule("M-P-1", "P", "ST-P-1", "P-S01", "P-S02", "P-S03", "P-S04", "P-S05", "P-S06", "P-S07"),
                new MissionRule("M-1-1", "C01", "ST-1-1", "C01-S01", "C01-S02", "C01-S03"),
                new MissionRule("M-1-2", "C01", "ST-1-2", "C01-S04", "C01-S05", "C01-S06"),
                new MissionRule("M-2-1", "C02", null, "C02-S01"),
                new MissionRule("M-2-2", "C02", "ST-2-2", "C02-S02", "C02-S03", "C02-S04", "C02-S05", "C02-S06", "C02-S07"),
                new MissionRule("M-3-1", "C03", "ST-3-1", "C03-S01", "C03-S02", "C03-S03", "C03-S04"),
                new MissionRule("M-3-2", "C03", "ST-3-2", "C03-S05", "C03-S06", "C03-S07"),
                new MissionRule("M-4-1", "C04", null, "C04-S01", "C04-S02"),
                new MissionRule("M-4-2", "C04", "ST-4-2", "C04-S03", "C04-S04", "C04-S05", "C04-S06", "C04-S07"),
                new MissionRule("M-5-1", "C05", "ST-5-1", "C05-S01", "C05-S02"),
                new MissionRule("M-5-2", "C05", null, "C05-S03", "C05-S04"),
                new MissionRule("M-5-3", "C05", "ST-5-3", "C05-S05", "C05-S06", "C05-S07"),
                new MissionRule("M-6-1", "C06", "ST-6-1", "C06-S01", "C06-S02"),
                new MissionRule("M-6-2", "C06", null, "C06-S03", "C06-S04", "C06-S05", "C06-S06", "C06-S07"),
                new MissionRule("M-7-1", "C07", "ST-7-1", "C07-S01", "C07-S02", "C07-S03", "C07-S04"),
                new MissionRule("M-7-2", "C07", "ST-7-2", "C07-S05", "C07-S06"),
                new MissionRule("M-7-3", "C07", null, "C07-S07"),
                new MissionRule("M-8-1", "C08", OperationStage, "C08-S01", "C08-S03"),
                new MissionRule("M-8-2", "C08", OperationStage, "C08-S02"),
                new MissionRule("M-8-3", "C08", OperationStage, "C08-S04", "C08-S05", "C08-S06", "C08-S07"),
                new MissionRule("M-E-1", "E", null, "E-S01", "E-S02", "E-S03", "E-S04")
            };
            foreach (string prefix in new[] { "SC", "SR" })
            {
                int count = prefix == "SC" ? 7 : 4;
                for (int number = 1; number <= count; number++)
                {
                    string id = prefix + number.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                    result.Add(new MissionRule("M-" + id, id, null, id + "-S01", id + "-S02", id + "-S03"));
                }
            }
            return result.ToArray();
        }
    }
}
