using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ProjectG.EditorTools
{
    /// <summary>Creates an intentionally incomplete scene-authoring handoff from the approved catalog.</summary>
    public sealed class FlightStoryAuthoringTools : EditorWindow
    {
        private const string ResourcePath = "Story/ProjectGStory";
        private const string BindingFolder = "Assets/ProjectG/Resources/StoryBindings";
        private const string Number = @"[+-]?(?:\d+(?:\.\d+)?|\.\d+)";
        private static readonly Regex PointPattern = new Regex(@"\(\s*(?<x>" + Number + @")\s*,\s*(?<y>" + Number + @")\s*,\s*(?<z>" + Number + @")\s*\)", RegexOptions.CultureInvariant);
        private static readonly Regex EntryPattern = new Regex(@"(?<seconds>\d+(?:\.\d+)?)\s*초\s*(?:진입\s*후|동안)", RegexOptions.CultureInvariant);
        private static readonly string[] C8Protection = { "evacuation_ship", "local_group", "work_platform" };
        private static readonly string[] C8FinalFacts = { "management_route_open", "local_closure_verified", "crosscheck_complete", "commander_approved" };

        [SerializeField] private int selectedStage;
        [SerializeField] private FlightEnemy enemyTemplate;
        private FlightStoryCatalog catalog;
        private string[] catalogErrors = Array.Empty<string>();
        private string[] report = Array.Empty<string>();
        private Vector2 scroll;
        private bool showSource = true;

        [MenuItem("Project G/Story/Authoring")]
        public static void Open() => GetWindow<FlightStoryAuthoringTools>("Story Authoring");

        private void OnEnable() => LoadCatalog();

        private void LoadCatalog()
        {
            catalog = null;
            try
            {
                TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
                if (asset == null) { catalogErrors = new[] { "Missing Resources/Story/ProjectGStory.json." }; return; }
                catalog = JsonUtility.FromJson<FlightStoryCatalog>(asset.text);
                catalogErrors = catalog == null ? new[] { "The catalog could not be decoded." } : catalog.Validate();
                selectedStage = Mathf.Clamp(selectedStage, 0, Math.Max(0, (catalog?.stages?.Length ?? 0) - 1));
            }
            catch (Exception exception) { catalogErrors = new[] { "Catalog load failed: " + exception.Message }; }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Authoring draft only. Generated signals have no producers; geometry, manifests and site sets require real scene authoring. Generation never approves a playable stage.", MessageType.Info);
            if (GUILayout.Button("Reload and validate catalog")) LoadCatalog();
            foreach (string error in catalogErrors) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (catalog == null || catalogErrors.Length != 0) return;

            selectedStage = EditorGUILayout.Popup("Flow group (30)", selectedStage, catalog.stages.Select(x => x.id + " — " + x.title).ToArray());
            FlightStoryStage stage = catalog.stages[selectedStage];
            enemyTemplate = (FlightEnemy)EditorGUILayout.ObjectField("Enemy template (prefab)", enemyTemplate, typeof(FlightEnemy), false);
            if (stage.hasCombat && enemyTemplate == null) EditorGUILayout.HelpBox("Select an existing FlightEnemy prefab asset, or bind a real template later. No enemy template is synthesized.", MessageType.Warning);
            string path = BindingFolder + "/" + stage.id + ".prefab";
            EditorGUILayout.SelectableLabel(path, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null || System.IO.File.Exists(path);
            using (new EditorGUI.DisabledScope(exists || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Create new authoring root and prefab")) Create(stage, false);
            if (exists)
            {
                EditorGUILayout.HelpBox("This prefab already exists. Creation will not replace it. Select it for validation, or explicitly choose the overwrite action below.", MessageType.Warning);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("Overwrite existing prefab with a fresh authoring draft…") && EditorUtility.DisplayDialog("Overwrite story authoring prefab?", path + "\nExisting authored bindings will be replaced with an incomplete generated draft.", "Overwrite this prefab", "Cancel")) Create(stage, true);
            }
            if (GUILayout.Button("Validate Selected")) ValidateSelected();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (string item in report) EditorGUILayout.HelpBox(item, MessageType.Warning);
            showSource = EditorGUILayout.Foldout(showSource, "Approved source coordinates, exits and authoring requirements", true);
            if (showSource) DrawSource(stage);
            EditorGUILayout.EndScrollView();
        }

        private void DrawSource(FlightStoryStage stage)
        {
            EditorGUILayout.LabelField("Catalog source", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(catalog.sourceUrl ?? string.Empty, EditorStyles.wordWrappedLabel, GUILayout.Height(36));
            foreach (FlightStoryArena arena in catalog.arenas.Where(x => x.stageId == stage.id))
                SourceText(arena.id + " — " + arena.title, "Scenes: " + arena.scenes + "\nVolume: " + arena.volume + "\nEntry: " + arena.entryPoints + "\nProtected targets: " + arena.protectedTargets + "\nPlacement: " + arena.placement + "\nTransform: " + arena.transformRules + "\nSafety: " + arena.safety + "\nCompletion: " + arena.completionNotes);
            foreach (FlightStoryWave wave in catalog.waves.Where(x => x.stageId == stage.id))
            {
                var todos = new List<string>();
                ReadCoordinates(wave, todos);
                SourceText(wave.id + " — " + wave.sceneId, "Slots: " + string.Join(", ", wave.composition.Select(x => x.enemy + " × " + x.count)) + "\nTrigger: " + wave.trigger + "\nSpawn / approach:\n" + wave.spawn + "\nExit:\n" + wave.exit + "\nGoal:\n" + wave.goal + "\nNotes:\n" + wave.notes);
                foreach (string todo in todos) EditorGUILayout.HelpBox(todo, MessageType.Warning);
            }
            foreach (FlightStoryNumericGoal goal in catalog.numericGoals.Where(x => x.stageId == stage.id))
                SourceText(goal.id + " — " + goal.sceneId, "Protection: " + goal.protectedTarget + "\nBudget: " + (goal.hasDurability ? goal.durability.ToString(CultureInfo.InvariantCulture) : "No new budget") + "\nSuccess:\n" + goal.success + "\nWait:\n" + goal.wait + "\nNotes:\n" + goal.notes);
        }

        private static void SourceText(string title, string text)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(text), Math.Max(240, EditorGUIUtility.currentViewWidth - 50));
            EditorGUILayout.SelectableLabel(text, EditorStyles.wordWrappedLabel, GUILayout.Height(height + 8));
        }

        private void Create(FlightStoryStage stage, bool overwrite)
        {
            string path = BindingFolder + "/" + stage.id + ".prefab";
            if (!overwrite && (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null || System.IO.File.Exists(path)))
            { report = new[] { "Creation stopped: the prefab already exists. Use the explicit overwrite button." }; return; }
            if (catalog.Validate().Length != 0) { report = catalog.Validate(); return; }
            var notes = new List<string>();
            var root = new GameObject(stage.id + "_AUTHORING_REQUIRED");
            Undo.RegisterCreatedObjectUndo(root, "Create story authoring draft");
            try
            {
                FlightStoryStageBinding binding = root.AddComponent<FlightStoryStageBinding>();
                binding.stageId = stage.id;
                binding.stageAnchor = Child(root.transform, "StageAnchor", Vector3.zero);
                binding.enemyTemplate = enemyTemplate;
                binding.spatialSafetyReviewed = false;
                binding.rosterReviewed = false;
                binding.expectedRoster = Array.Empty<FlightStoryManifestEntry>();
                binding.arrivalGates = Array.Empty<FlightStoryArrivalGate>();
                binding.requiredSites = Array.Empty<string>();
                binding.backupSites = Array.Empty<string>();
                binding.protections = CreateProtections(binding, stage);
                binding.scenes = stage.sceneIds.Select(id => CreateScene(binding, id)).ToArray();
                binding.waves = catalog.waves.Where(x => x.stageId == stage.id).Select(wave => CreateWave(binding, wave, notes)).ToArray();
                EnsureFolder(BindingFolder);
                GameObject saved = PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);
                if (saved == null) throw new InvalidOperationException("Unity did not save the authoring prefab.");
                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(saved);
                EditorUtility.SetDirty(binding);
                report = new[] { "Saved authoring draft: " + path + ". The selected scene root is editable; apply later changes to the prefab explicitly." }
                    .Concat(notes).Concat(ValidationReport(binding)).Distinct().ToArray();
            }
            catch (Exception exception)
            {
                report = new[] { "Generation failed: " + exception.Message + ". The scene draft remains selected for inspection; no readiness was granted." };
                Selection.activeGameObject = root;
            }
        }

        private FlightStorySceneBinding CreateScene(FlightStoryStageBinding binding, string sceneId)
        {
            string[] ready = catalog.waves.Where(x => x.stageId == binding.stageId && x.sceneId == sceneId).Select(x => "READY." + x.id).ToArray();
            if (binding.stageId == "ST-8-OP" && sceneId == "C08-S07") ready = ready.Concat(C8FinalFacts).ToArray();
            string[] facts = catalog.Objective(sceneId).RequiredEvidence.Concat(ready).Distinct(StringComparer.Ordinal).ToArray();
            Transform group = Child(binding.transform, sceneId + "_Evidence", Vector3.zero);
            var signals = new List<FlightStoryEvidenceSignal>();
            foreach (string fact in facts)
            {
                FlightStoryEvidenceSignal signal = Child(group, fact, Vector3.zero).gameObject.AddComponent<FlightStoryEvidenceSignal>();
                signal.Configure(sceneId, fact);
                signals.Add(signal);
            }
            bool c8 = binding.stageId == "ST-8-OP";
            return new FlightStorySceneBinding
            {
                sceneId = sceneId,
                evidence = signals.ToArray(),
                evidenceProducers = new MonoBehaviour[facts.Length],
                readinessFacts = ready,
                protectedTargetIds = binding.protections.Where(x => x.sceneIds.Contains(sceneId)).Select(x => x.id).ToArray(),
                allArrivals = c8 && (sceneId == "C08-S06" || sceneId == "C08-S07"),
                sustainedStability = c8 && (sceneId == "C08-S05" || sceneId == "C08-S06" || sceneId == "C08-S07"),
                backupStability = c8 && (sceneId == "C08-S04" || sceneId == "C08-S05" || sceneId == "C08-S06" || sceneId == "C08-S07")
            };
        }

        private FlightStoryProtectionBinding[] CreateProtections(FlightStoryStageBinding binding, FlightStoryStage stage)
        {
            FlightStoryNumericGoal[] goals = catalog.numericGoals.Where(x => x.stageId == stage.id && x.hasDurability).ToArray();
            var result = new List<FlightStoryProtectionBinding>();
            foreach (IGrouping<double, FlightStoryNumericGoal> budget in goals.GroupBy(x => x.durability))
            {
                // These are implementation binding IDs, not new characters, populations or world locations.
                string[] ids = stage.id == "ST-8-OP" ? C8Protection
                    : stage.id == "ST-1-2" ? new[] { "transport_ship", "observation_base" }
                    : stage.id == "ST-3-1" ? new[] { "supply_ship", "rescue_ship" }
                    : new[] { "budget_" + budget.First().id };
                foreach (string id in ids)
                {
                    FlightStoryProtectedBody body = Child(binding.stageAnchor, id + "_GEOMETRY_REQUIRED", Vector3.zero).gameObject.AddComponent<FlightStoryProtectedBody>();
                    // Zero radius deliberately fails runtime validation: no approved shape or physical actor is invented.
                    body.Configure(id, 0f, Vector3.zero);
                    result.Add(new FlightStoryProtectionBinding { id = id, maximum = budget.Key, sceneIds = budget.Select(x => x.sceneId).ToArray(), body = body });
                }
            }
            return result.ToArray();
        }

        private FlightStoryWavePlacement CreateWave(FlightStoryStageBinding binding, FlightStoryWave wave, List<string> notes)
        {
            Coordinates coordinates = ReadCoordinates(wave, notes);
            Transform group = Child(binding.stageAnchor, wave.id, Vector3.zero);
            var placement = new FlightStoryWavePlacement
            {
                waveId = wave.id,
                spawnPoints = new Transform[wave.SlotCount],
                combatPoints = coordinates.Combat.Length == wave.SlotCount ? new Transform[wave.SlotCount] : Array.Empty<Transform>(),
                activationFact = "READY." + wave.id,
                entrySeconds = coordinates.EntrySeconds,
                retreatSeconds = coordinates.RetreatSeconds,
                automaticExitSeconds = coordinates.AutomaticExitSeconds,
                outsideHoldSeconds = wave.composition.Any(x => x.enemy == "B-VARKAN" || x.enemy == "B-RESONANCE") ? 2f : 0f
            };
            for (int slot = 0; slot < coordinates.Spawn.Length && slot < placement.spawnPoints.Length; slot++)
                placement.spawnPoints[slot] = Child(group, "Spawn_" + (slot + 1).ToString("00"), coordinates.Spawn[slot]);
            for (int slot = 0; slot < placement.combatPoints.Length; slot++)
                placement.combatPoints[slot] = Child(group, "Combat_" + (slot + 1).ToString("00"), coordinates.Combat[slot]);
            if (coordinates.Retreat.HasValue) placement.retreatTarget = Child(group, "Retreat_GEOMETRY_REVIEW", coordinates.Retreat.Value);
            // Safety facts, permission, volumes and airspace must be matched to real authored sensors.
            return placement;
        }

        private sealed class Coordinates
        {
            public Vector3[] Spawn = Array.Empty<Vector3>(), Combat = Array.Empty<Vector3>();
            public Vector3? Retreat;
            public float EntrySeconds = 2f, RetreatSeconds = 2f, AutomaticExitSeconds;
        }

        private static Coordinates ReadCoordinates(FlightStoryWave wave, List<string> notes)
        {
            var result = new Coordinates();
            string sourceCoordinates = (wave.spawn ?? string.Empty).Replace('\u2212', '-');
            string firstLine = sourceCoordinates.Split('\n')[0];
            Match entry = EntryPattern.Match(sourceCoordinates);
            string originText = entry.Success && entry.Index < firstLine.Length ? firstLine.Substring(0, entry.Index) : firstLine;
            Vector3[] origins = Points(originText);
            if (origins.Length == wave.SlotCount) result.Spawn = origins;
            else if (wave.SlotCount == 1 && origins.Length > 0 && originText.StartsWith("보스", StringComparison.Ordinal))
                result.Spawn = new[] { origins[0] }; // A separately labelled rescue ship is not a boss spawn slot.
            else notes.Add(wave.id + ": REVIEW spawn coordinates; the source does not identify exactly one unambiguous position per slot.");
            if (entry.Success)
            {
                result.EntrySeconds = Parse(entry.Groups["seconds"].Value);
                string destinationLine = sourceCoordinates.Substring(entry.Index + entry.Length).Split('\n')[0];
                Vector3[] destinations = Points(destinationLine);
                if (destinations.Length == wave.SlotCount) result.Combat = destinations;
                else notes.Add(wave.id + ": REVIEW combat/approach endpoints; the source describes a region without one numeric point per slot.");
            }

            string exit = (wave.exit ?? string.Empty).Replace('\u2212', '-');
            Vector3[] exits = Points(exit);
            bool alternatives = exit.Contains("±") || exit.Contains("또는");
            if (!alternatives && exits.Length == 1) result.Retreat = exits[0];
            else if (wave.id == "W_C07_03" && exits.Length == 2 && exit.Contains("뒤쪽"))
                result.Retreat = exits[1]; // The first triple is the explicitly labelled origin, not an alternate exit.
            else if (wave.composition.Any(x => x.enemy != "H-ROCK" && x.enemy != "B-KARDEN"))
                notes.Add(wave.id + ": REVIEW retreat endpoint; alternatives, relative directions or unspecified empty airspace remain unbound.");
            Match automatic = Regex.Match(exit, @"등장\s*(?<s>\d+(?:\.\d+)?)\s*초\s*후");
            if (automatic.Success) result.AutomaticExitSeconds = Parse(automatic.Groups["s"].Value);
            Match travel = Regex.Match(exit, @"(?<s>\d+(?:\.\d+)?)\s*초\s*(?:동안\s*외곽|간\s*이탈|이탈)");
            if (travel.Success) result.RetreatSeconds = Parse(travel.Groups["s"].Value);
            else if (wave.id == "W_C07_03" && exit.Contains("24wu") && exit.Contains("4wu/s")) result.RetreatSeconds = 24f / 4f;
            else if (result.Retreat.HasValue) notes.Add(wave.id + ": REVIEW retreat duration; the binding's two-second default is not an approved movement duration. Door-closing and outside-hold times are not travel times.");
            return result;
        }

        private static Vector3[] Points(string text)
        {
            var points = new List<Vector3>();
            foreach (Match match in PointPattern.Matches(text))
            {
                float x = Parse(match.Groups["x"].Value), y = Parse(match.Groups["y"].Value), z = Parse(match.Groups["z"].Value);
                if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y) || float.IsNaN(z) || float.IsInfinity(z)) continue;
                points.Add(new Vector3(x, y, z));
            }
            return points.ToArray();
        }

        private static float Parse(string value) => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child.transform;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
        }

        private void ValidateSelected()
        {
            GameObject selected = Selection.activeGameObject;
            FlightStoryStageBinding binding = selected == null ? null : selected.GetComponentInParent<FlightStoryStageBinding>();
            if (binding == null && selected != null) binding = selected.GetComponentInChildren<FlightStoryStageBinding>(true);
            if (binding == null) { report = new[] { "Select a story authoring root or prefab with FlightStoryStageBinding." }; return; }
            int index = Array.FindIndex(catalog.stages, x => x.id == binding.stageId);
            if (index >= 0) selectedStage = index;
            report = ValidationReport(binding).ToArray();
            if (report.Length == 0) report = new[] { "Binding checks passed. This is not Unity play verification; complete the documented native scene and gameplay gates before marking the stage ready." };
        }

        private IEnumerable<string> ValidationReport(FlightStoryStageBinding binding)
        {
            var errors = new List<string>(catalog.Validate());
            try { errors.AddRange(binding.Validate(catalog)); }
            catch (Exception exception) { errors.Add("Malformed binding requires Inspector repair: " + exception.Message); }
            foreach (FlightStorySceneBinding scene in binding.scenes ?? Array.Empty<FlightStorySceneBinding>())
            {
                if (scene == null) continue;
                string[] facts = (catalog.Objective(scene.sceneId)?.RequiredEvidence ?? Array.Empty<string>()).Concat(scene.readinessFacts ?? Array.Empty<string>()).Distinct().ToArray();
                foreach (string fact in facts)
                {
                    int index = Array.FindIndex(scene.evidence ?? Array.Empty<FlightStoryEvidenceSignal>(), x => x != null && x.SceneId == scene.sceneId && x.FactId == fact);
                    MonoBehaviour producer = index >= 0 && index < (scene.evidenceProducers?.Length ?? 0) ? scene.evidenceProducers[index] : null;
                    if (producer == null) errors.Add(scene.sceneId + " / " + fact + ": bind a real quest, cutscene or inspection producer; an empty signal cannot supply evidence.");
                    else if (producer is FlightStoryEvidenceSignal) errors.Add(scene.sceneId + " / " + fact + ": the signal itself is not an independent evidence producer.");
                }
            }
            foreach (FlightStoryProtectionBinding protection in binding.protections ?? Array.Empty<FlightStoryProtectionBinding>())
                if (protection != null && (protection.body == null || protection.body.ValidateConfiguration().Length != 0))
                    errors.Add(protection.id + ": bind the real protected actor/area, its measured center and positive body radius. Zero-radius generated placeholders are intentionally invalid.");
            foreach (FlightStoryWave wave in catalog.waves.Where(x => x.stageId == binding.stageId))
            {
                FlightStoryWavePlacement placement = binding.Wave(wave.id);
                var sourceTodos = new List<string>();
                Coordinates coordinates = ReadCoordinates(wave, sourceTodos);
                if (placement == null) continue;
                if (EntryPattern.IsMatch(wave.spawn ?? string.Empty) && (placement.combatPoints == null || placement.combatPoints.Length != wave.SlotCount || placement.combatPoints.Any(x => x == null)))
                    errors.Add(wave.id + ": author one approach endpoint per slot from the source region and review the complete entry path.");
                if (!binding.spatialSafetyReviewed && placement.retreatTarget != null && coordinates.Retreat.HasValue && sourceTodos.Any(x => x.Contains("retreat duration")))
                    errors.Add(wave.id + ": retreat travel time needs an explicit scene-authoring decision; the catalog has no exact travel duration.");
                bool needsExit = wave.composition.Any(x => x.enemy != "H-ROCK" && x.enemy != "B-KARDEN");
                if (needsExit && (placement.retreatTarget == null || placement.approvedExitAirspace == null))
                    errors.Add(wave.id + ": bind the retreat destination and reviewed empty-airspace volume; inspect the original exit text below.");
                if (wave.composition.Any(x => x.enemy == "B-VARKAN" || x.enemy == "B-RESONANCE") && (placement.forbiddenExitVolumes == null || placement.forbiddenExitVolumes.Length == 0 || placement.forbiddenExitVolumes.Any(x => x == null)))
                    errors.Add(wave.id + ": bind every actual evacuation volume for the two-second outside check.");
            }
            if (binding.stageId == "ST-7-1" || binding.stageId == "ST-8-OP")
                if (!binding.rosterReviewed || binding.expectedRoster == null || binding.expectedRoster.Length == 0)
                    errors.Add(binding.stageId + ": supply the approved identity manifest, matching scene actors and real arrival gates; no population is generated.");
            if (binding.stageId == "ST-8-OP")
            {
                if (binding.requiredSites == null || binding.requiredSites.Length == 0) errors.Add("C8: bind the complete approved main-site set and real measurement/report sources.");
                if (binding.backupSites == null || binding.backupSites.Length == 0) errors.Add("C8: bind the complete approved backup-site set and real measurement/report sources.");
                if (string.IsNullOrWhiteSpace(binding.ariaRosterId) || string.IsNullOrWhiteSpace(binding.rianRosterId)) errors.Add("C8: assign the actual Aria and Rian manifest identities; generated drafts invent neither identity nor destination.");
            }
            return errors.Distinct(StringComparer.Ordinal);
        }
    }
}
