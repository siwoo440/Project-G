# Step 4: Unity story scene authoring handoff

## Approved source

Native Google Sheet: <https://docs.google.com/spreadsheets/d/1tdA5lcTt4TgFSrecJKRxw1d_3jGAWZlbM4YzFMDq-EM/edit>.

The relevant native tabs are [26. 웨이브](https://docs.google.com/spreadsheets/d/1tdA5lcTt4TgFSrecJKRxw1d_3jGAWZlbM4YzFMDq-EM/edit#gid=2082665740), [28. 목표 수치](https://docs.google.com/spreadsheets/d/1tdA5lcTt4TgFSrecJKRxw1d_3jGAWZlbM4YzFMDq-EM/edit#gid=1455912011), [29. 전장 좌표](https://docs.google.com/spreadsheets/d/1tdA5lcTt4TgFSrecJKRxw1d_3jGAWZlbM4YzFMDq-EM/edit#gid=381285521), and [31. 구현 검증](https://docs.google.com/spreadsheets/d/1tdA5lcTt4TgFSrecJKRxw1d_3jGAWZlbM4YzFMDq-EM/edit#gid=1953998935).

The tool reads the checked-in `Resources/Story/ProjectGStory` catalog. It does not rewrite the Sheet, change source content, or fetch a new revision.

## Generate an authoring draft

1. Open the project in its supported Unity version and allow the Editor and runtime assemblies to compile.
2. Open **Project G → Story → Authoring**. The catalog must validate before generation is available. The dropdown contains its 30 ordered flow groups.
3. Select a flow group and an existing `FlightEnemy` prefab asset in the template field. Combat groups require a real template; the tool does not synthesize an enemy or call internal encounter APIs. A persistent prefab asset is required because a prefab cannot preserve an external scene-object reference.
4. Choose **Create new authoring root and prefab**. The draft root is selected in the scene and the asset is saved at `Assets/ProjectG/Resources/StoryBindings/{stageId}.prefab`. Save/apply later native authoring changes explicitly. An existing asset disables creation; only **Overwrite existing prefab with a fresh authoring draft…**, followed by its confirmation, replaces it.
5. Use **Validate Selected** on the scene root, a descendant, or the prefab asset. The window reports missing producers, template, manifest, site sets, arrival gates, body geometry, entry endpoints and retreat volumes. The source panel retains the full arena, spawn, exit, trigger and numeric-goal text without adding a runtime notes component.

Generation is a concrete handoff for scene work, not a playable-stage approval. Both `spatialSafetyReviewed` and `rosterReviewed` are always false on a fresh draft. No director is created or started.

## Generated data and deliberate gaps

| Area | Generated | Native authoring still required |
| --- | --- | --- |
| Scenes | Every catalog scene ID, every objective `RequiredEvidence` signal, and current-scene `READY.{waveId}` signals | Every parallel `evidenceProducers` slot remains null. Bind real quest, cutscene, inspection or sensor producers. A signal is not its own producer. Enabling or viewing a draft publishes no success. |
| C8 final checks | The four required final readiness signals and scene flags for arrival, main stability and backup stability gates | Real management-route, local-closure, crosscheck and commander sources; actual current reports and measured geometry. |
| Finite waves | Exact composition slot counts, explicit unambiguous numeric spawn points and entry endpoints, readiness fact IDs | Review all entry routes, terrain clearance, forbidden corridors, automatic exit routing and conditional activation. Missing points remain null, never silently become `(0,0,0)` spawns. |
| Retreat | Exact numeric endpoints when unambiguous, explicit authored travel times where present, 30-second survivor exits where stated, Varkan's 24wu / 4wu/s travel and both final outside holds of 2 seconds | `±` alternatives, relative directions and unspecified empty airspace remain unbound. Review exit permission, separate safety evidence, empty-airspace volumes and all evacuation volumes. An isolation-door duration is never used as travel time. |
| Protection | Numeric maximum budgets and their scene scope; persistent IDs share a budget across repeated scene rows | Bind each real protected actor or area and its measured center/radius. Generated bodies have radius **0**, deliberately fail validation, and are not approved geometry or new civilian HP. |
| Manifest and sites | Empty arrays | Obtain the approved complete identity manifest, actor bindings, destinations, trigger gates, main-site and backup-site sets. Assign Aria/Rian from the real manifest. No invented population, destination or site count is supplied. |

The coordinate reader uses invariant numeric triples from the approved source text. It distinguishes the labelled prologue boss from the separately listed rescue ship, entry destinations from later exclusion points, and Varkan's labelled origin from its retreat endpoint. A two-second binding default remains visible as **review required** wherever no movement duration is authored. All transforms are local to `StageAnchor`; native scene placement may translate and yaw the anchor with unit scale.

Protection aliases are implementation identifiers, not new lore. C1 installation uses distinct `transport_ship` and `observation_base` budgets of 180; C3 transport uses distinct `supply_ship` and `rescue_ship` budgets of 240, reflecting the source's “each” wording. Other non-C8 budgets derive their ID from the first numeric-goal ID and carry across that stage's matching durability rows. C8 has exactly `evacuation_ship`, `local_group`, and `work_platform`, each **240**, shared between `C08-S01` and `C08-S02`. Later scenes create no replacement or refreshed budget.

Safety evidence and retreat permission are intentionally not guessed from narrative sentences. Select the relevant approved objective facts and bind the physical observations that establish them. A generated required fact, a timer, an empty list, or a missing report must never become automatic success.

## Required Unity gates

1. Compile runtime, Editor and test assemblies in the native Unity project. Open the menu and generate representative noncombat, normal-wave, Karden, Varkan and C8 drafts. Confirm all 30 dropdown entries and existing-prefab overwrite protection.
2. Inspect every generated coordinate against the source panel and native Sheet. Check source bodies, separate safe corridors, 20wu creation clearance, entry preview timing, swept contact paths, complete exit volumes, terrain and anchor rotation. Preserve fixed versus falling debris behavior.
3. Bind actual producers to every objective and readiness signal. Verify missing, stale, wrong-scene and wrong-run reports remain waiting. Skipping dialogue or reaching an expected travel time must not synthesize evidence.
4. Complete the manifest and immutable main/backup site sets from approved scene authoring; bind actual actor IDs, destinations, arrival trigger volumes, observations, reports, acknowledgements and stability measurements. Check distinct Aria/Rian identities and all support personnel without inventing a population count.
5. Measure each protected body's geometry, replace zero-radius placeholders, and verify separate damage budgets and the C8 S01→S02 carry. Review every generated and manually completed prefab with **Validate Selected**, then perform real native spatial and roster review before setting the corresponding review flags.
6. Run existing and added PlayMode regression tests, then exercise complete flow progression and failure/retry/pause behavior in actual gameplay. Confirm suppression is not death or settlement, finite waves and attack caps hold, safety exits precede encounter completion, and C8 settles once only at S07 after the complete current evidence and save transaction.

## Verification status

This environment has no Unity executable or engine/compiler references. The editor tool can receive a Roslyn C# 9 **syntax-only** check here. Native compilation, menu execution, prefab serialization, scene validation UI and all PlayMode/gameplay gates above remain **not run** until performed in Unity. No generated prefab is claimed playable, and this tool does not replace those gates.

The 2026-11-26 syntax check parsed the complete Editor source with Roslyn `LanguageVersion.CSharp9`: **1 file, 0 syntax errors**. This is not a semantic compilation against Unity assemblies. The command, from the worktree root, was:

```sh
/workspace/scratch/db40c359e931/project_g_combat/test_environment/dotnet/dotnet run --project /workspace/scratch/db40c359e931/project_g_combat/test_environment/story-parser/Parser.csproj --no-restore -- Assets/ProjectG/Editor/FlightStoryAuthoringTools.cs
```

`git diff --check` reported no whitespace errors. No Unity prefab was generated in this environment.
