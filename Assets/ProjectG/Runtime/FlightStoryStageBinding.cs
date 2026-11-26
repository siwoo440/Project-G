using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectG
{
    [Serializable]
    public sealed class FlightStoryWavePlacement
    {
        public string waveId;
        public Transform[] spawnPoints = Array.Empty<Transform>();
        public Transform[] combatPoints = Array.Empty<Transform>();
        public float entrySeconds = 2f;
        public string activationFact;
        public string safetySceneId;
        public string[] safetyFacts = Array.Empty<string>();
        public string retreatPermissionFact;
        public Transform retreatTarget;
        public float retreatSeconds = 2f;
        public float automaticExitSeconds;
        public float outsideHoldSeconds;
        public Collider[] forbiddenExitVolumes = Array.Empty<Collider>();
        public Collider approvedExitAirspace;
    }

    [Serializable]
    public sealed class FlightStoryProtectionBinding
    {
        public string id;
        public double maximum;
        public string[] sceneIds = Array.Empty<string>();
        public FlightStoryProtectedBody body;
    }

    [Serializable]
    public sealed class FlightStorySceneBinding
    {
        public string sceneId;
        public FlightStoryEvidenceSignal[] evidence = Array.Empty<FlightStoryEvidenceSignal>();
        // Each source is the actual quest/cutscene/inspection producer for its corresponding signal.
        public MonoBehaviour[] evidenceProducers = Array.Empty<MonoBehaviour>();
        public string[] protectedTargetIds = Array.Empty<string>();
        public string[] readinessFacts = Array.Empty<string>();
        public bool allArrivals, sustainedStability, backupStability;
    }

    /// <summary>Scene-owned physical bindings. Empty population, sensors or geometry never imply success.</summary>
    [DisallowMultipleComponent]
    public sealed class FlightStoryStageBinding : MonoBehaviour
    {
        public string stageId;
        public Transform stageAnchor;
        public FlightEnemy enemyTemplate;
        public FlightStorySceneBinding[] scenes = Array.Empty<FlightStorySceneBinding>();
        public FlightStoryWavePlacement[] waves = Array.Empty<FlightStoryWavePlacement>();
        public FlightStoryProtectionBinding[] protections = Array.Empty<FlightStoryProtectionBinding>();
        public FlightStoryManifestEntry[] expectedRoster = Array.Empty<FlightStoryManifestEntry>();
        public FlightStoryArrivalGate[] arrivalGates = Array.Empty<FlightStoryArrivalGate>();
        public string[] requiredSites = Array.Empty<string>(), backupSites = Array.Empty<string>();
        public string ariaRosterId, rianRosterId;
        public bool rosterReviewed, spatialSafetyReviewed;

        public FlightStorySceneBinding Scene(string id) => Array.Find(scenes, x => x.sceneId == id);
        public FlightStoryWavePlacement Wave(string id) => Array.Find(waves, x => x.waveId == id);

        public string[] Validate(FlightStoryCatalog catalog)
        {
            var errors = new List<string>();
            FlightStoryStage stage = catalog?.Stage(stageId);
            if (stage == null) return new[] { "Unknown stage binding: " + stageId };
            errors.AddRange(FlightStorySignals.ValidateHierarchy(transform));
            if (stageAnchor == null) errors.Add("A scene anchor is required.");
            else if (Vector3.Distance(stageAnchor.lossyScale, Vector3.one) > 0.001f || Mathf.Abs(Mathf.DeltaAngle(stageAnchor.eulerAngles.x, 0)) > 0.01f || Mathf.Abs(Mathf.DeltaAngle(stageAnchor.eulerAngles.z, 0)) > 0.01f)
                errors.Add("The approved arena transform allows translation and yaw, with unit scale.");
            if (!spatialSafetyReviewed) errors.Add("Flight corridors, terrain clearance and retreat space await scene review.");
            if (stage.hasCombat && enemyTemplate == null) errors.Add("No enemy template is bound.");
            foreach (string sceneId in FlightStoryRunPlan.RequiredScenes(stage))
            {
                FlightStorySceneBinding item = Scene(sceneId);
                if (item == null || scenes.Count(x => x != null && x.sceneId == sceneId) != 1)
                { errors.Add("Missing or duplicate scene binding: " + sceneId); continue; }
                FlightStoryObjective objective = catalog.Objective(sceneId);
                foreach (string fact in objective.RequiredEvidence)
                {
                    int index = Array.FindIndex(item.evidence, x => x != null && x.SceneId == sceneId && x.FactId == fact);
                    if (index < 0 || index >= item.evidenceProducers.Length || item.evidenceProducers[index] == null)
                        errors.Add("Missing actual evidence producer: " + fact);
                }
                foreach (string fact in item.readinessFacts)
                {
                    int index = Array.FindIndex(item.evidence, x => x != null && x.SceneId == sceneId && x.FactId == fact);
                    if (string.IsNullOrWhiteSpace(fact) || index < 0 || index >= item.evidenceProducers.Length || item.evidenceProducers[index] == null)
                        errors.Add("Missing current-scene readiness producer: " + fact);
                }
                foreach (string id in item.protectedTargetIds)
                    if (!protections.Any(x => x != null && x.id == id && x.sceneIds.Contains(sceneId))) errors.Add("Unbound protection: " + id);
                FlightStoryNumericGoal numeric = catalog.NumericGoal(sceneId);
                if (numeric != null && numeric.hasDurability && !protections.Any(p => p != null && p.maximum == numeric.durability && p.sceneIds.Contains(sceneId) && item.protectedTargetIds.Contains(p.id)))
                    errors.Add("The scene needs its approved physical protection budget: " + sceneId);
            }
            foreach (FlightStoryWave wave in catalog.waves.Where(x => x.stageId == stageId))
            {
                FlightStoryWavePlacement item = Wave(wave.id);
                if (item == null || waves.Count(x => x != null && x.waveId == wave.id) != 1)
                { errors.Add("Missing finite wave placement: " + wave.id); continue; }
                if (item.spawnPoints.Length != wave.SlotCount || item.spawnPoints.Any(x => x == null)) errors.Add("Each wave slot needs one real spawn point: " + wave.id);
                if (string.IsNullOrWhiteSpace(item.activationFact) || !(Scene(wave.sceneId)?.readinessFacts.Contains(item.activationFact) ?? false))
                    errors.Add("Wave activation must use a current-scene evidence signal: " + wave.id);
                bool nonlethal = wave.composition.Any(x => FlightStoryCombatProfiles.FindEnemy(x.enemy)?.IsNonlethal == true && x.enemy != "H-ROCK");
                if (nonlethal && (item.safetyFacts.Length == 0 || string.IsNullOrWhiteSpace(item.safetySceneId))) errors.Add("Nonlethal encounter needs a separate safety confirmation: " + wave.id);
                if (nonlethal && wave.composition[0].enemy != "B-KARDEN" && (item.retreatTarget == null || item.approvedExitAirspace == null))
                    errors.Add("Nonlethal exit must have reviewed geometry: " + wave.id);
                if (item.automaticExitSeconds > 0 && (item.approvedExitAirspace == null || item.retreatTarget == null)) errors.Add("Timed survivors need reviewed safe exit geometry: " + wave.id);
                if (item.approvedExitAirspace != null && !(item.approvedExitAirspace is BoxCollider) && !(item.approvedExitAirspace is SphereCollider))
                    errors.Add("Safe airspace must use a supported box or sphere volume: " + wave.id);
                if (item.safetyFacts.Any(f => !(catalog.Objective(item.safetySceneId)?.RequiredEvidence.Contains(f) ?? false)))
                    errors.Add("Unknown safety evidence: " + wave.id);
                if (wave.composition.Any(x => x.enemy == "B-VARKAN" || x.enemy == "B-RESONANCE") && (item.outsideHoldSeconds < 2f || item.forbiddenExitVolumes.Length == 0))
                    errors.Add("Final retreat requires all evacuation volumes and a two-second outside hold: " + wave.id);
            }
            foreach (FlightStoryProtectionBinding p in protections)
                if (p == null || string.IsNullOrWhiteSpace(p.id) || p.maximum <= 0 || double.IsNaN(p.maximum) || double.IsInfinity(p.maximum) || p.body == null || p.body.TargetId != p.id || p.sceneIds.Length == 0 || p.body.ValidateConfiguration().Length != 0)
                    errors.Add("Invalid physical protected target.");
            if (protections.Where(x => x != null).GroupBy(x => x.id).Any(x => x.Count() != 1)) errors.Add("Protection IDs must be unique.");
            if (stageId == "ST-7-1" && (!rosterReviewed || expectedRoster.Length == 0)) errors.Add("The conditional boarding wave requires an approved manifest.");
            FlightStoryRosterMember[] members = GetComponentsInChildren<FlightStoryRosterMember>(true);
            foreach (FlightStoryManifestEntry expected in expectedRoster)
                if (members.Count(m => m.EntityId == expected.Id) != 1) errors.Add("Each approved identity needs one bound scene actor: " + expected.Id);
            foreach (FlightStoryArrivalGate gate in arrivalGates)
                if (gate == null || gate.GetComponent<Collider>() == null || !gate.GetComponent<Collider>().isTrigger) errors.Add("Arrival gates require real trigger geometry.");
            if (stageId == "ST-8-OP")
            {
                if (!rosterReviewed || expectedRoster.Length == 0 || expectedRoster.Any(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.GroupId) || string.IsNullOrWhiteSpace(x.DestinationId)))
                    errors.Add("C8 requires an approved, nonempty identity manifest.");
                if (expectedRoster.Select(x => x.Id).Distinct().Count() != expectedRoster.Length || string.IsNullOrWhiteSpace(ariaRosterId) || string.IsNullOrWhiteSpace(rianRosterId) || ariaRosterId == rianRosterId || !expectedRoster.Any(x => x.Id == ariaRosterId) || !expectedRoster.Any(x => x.Id == rianRosterId))
                    errors.Add("C8 must include distinct Aria and Rian identities in the complete manifest.");
                if (requiredSites.Length == 0 || backupSites.Length == 0 || requiredSites.Any(string.IsNullOrWhiteSpace) || backupSites.Any(string.IsNullOrWhiteSpace) || requiredSites.Distinct().Count() != requiredSites.Length || backupSites.Distinct().Count() != backupSites.Length)
                    errors.Add("Main and backup site sets must be explicitly authored and immutable during an attempt.");
                string[] c8Protection = { "evacuation_ship", "local_group", "work_platform" };
                foreach (string id in c8Protection)
                    if (!protections.Any(x => x.id == id && x.maximum == 240 && x.sceneIds.Contains("C08-S01") && x.sceneIds.Contains("C08-S02")))
                        errors.Add("C8 needs its own carried 240-point budget: " + id);
                foreach (string destination in expectedRoster.Select(x => x.DestinationId).Distinct())
                    if (!arrivalGates.Any(g => g != null && g.DestinationId == destination)) errors.Add("Missing actual arrival gate: " + destination);
                foreach (string fact in new[] { "management_route_open", "local_closure_verified", "crosscheck_complete", "commander_approved" })
                    if (!(Scene("C08-S07")?.readinessFacts.Contains(fact) ?? false)) errors.Add("Final operation evidence producer required: " + fact);
            }
            return errors.ToArray();
        }
    }
}
