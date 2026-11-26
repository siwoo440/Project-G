# Story combat regression checks

Run with a .NET 10 SDK:

```sh
dotnet run --project tests/StoryCombat.Tests/StoryCombat.Tests.csproj
```

No external test packages are required. The project compiles the **actual** `FlightStoryCombatProfiles.cs` and `FlightStoryCombatRules.cs` production files with C# 9 language rules. It does not reimplement their behavior in a test double.

The console checks cover independent reward/phase classification, one-boundary overkill, transition protection, repeated nonlethal endpoints, invalid damage, full-burst capacity refusal, future split reservations, epoch invalidation, paired-muzzle geometry, explicit damage, ordinary special motion, repeated warnings after capacity waits, full active/recovery timing, and charge timing held by control.

TDD evidence during implementation:

1. Initial run: **0 passed, 9 failed**. Each failed at the expected missing production feature; the harness itself compiled and ran.
2. Profile/gauge/budget/geometry implementation: **9 passed, 0 failed**.
3. New warning/recovery test: **9 passed, 1 failed** for the absent production attack clock; after implementation, **10 passed, 0 failed**.
4. New root/charge timing test: **10 passed, 1 failed** for absent `Delay`; after implementation, **11 passed, 0 failed**.
5. Capacity retry timing regression: **10 passed, 1 failed** when warning time did not shift the later charge; the production fix restored **11 passed, 0 failed**.

Unity reflection integration tests are in `Assets/ProjectG/Tests/PlayMode/FlightStoryCombatPlayTests.cs`. They exercise cloned real enemies, the actual damage entry point, the actual threat pool, complete-burst refusal, and delayed split cancellation. These PlayMode tests were **not run** in the implementation environment because Unity was unavailable. C# 9 parsing of the adapter files is a syntax check, not Unity compilation or physics/play evidence.

There are four Unity tests: nonlethal endpoint/re-activation, atomic pool/split clear, elite/boss control plus positive-gauge retreat targeting, and fixed debris versus explicitly configured falling rocks. They are authored gates, not reported passing tests.

## Recorded verification commands

The local SDK used was .NET10.0.404. From the repository root:

```sh
/workspace/scratch/db40c359e931/project_g_combat/test_environment/dotnet/dotnet run --project tests/StoryCombat.Tests/StoryCombat.Tests.csproj --no-restore
git diff --check
```

Result: **11 passed,0 failed**; whitespace/diff check returned0.

The two pure production files were also compiled by Roslyn with `-langversion:9 -nostdlib+ -warnaserror+`, using all121 actual .NET Standard2.1 reference assemblies from the local `netstandard.library.ref/2.1.0` package. The response file is a reproducible environment artifact listing those references and both production paths:

```sh
/workspace/scratch/db40c359e931/project_g_combat/test_environment/dotnet/dotnet exec /workspace/scratch/db40c359e931/project_g_combat/test_environment/dotnet/sdk/10.0.404/Roslyn/bincore/csc.dll @/workspace/scratch/db40c359e931/project_g_combat/test_environment/StoryCombat-netstandard21.rsp
```

Result: exit0, no warnings/errors. This verifies the pure implementation against Unity's requested framework API surface; it does not compile Unity adapters.

A temporary Roslyn parser at `project_g_combat/test_environment/story-parser/Parser.csproj` parses file arguments with `LanguageVersion.CSharp9` and reports syntax diagnostics. It was run on the three new combat files, Enemy/Pool/WeakPoint/CraftedStatus, the six changed targeter files, and the PlayMode test file: **14 files,0 syntax errors**. This tool intentionally makes no Unity semantic-compilation or gameplay claim.

## Runtime integration/readiness

`Create` returns null for missing/unknown dependencies, insufficient20wu player separation, or a terrain-overlapping spawn; the director must retain and retry its unclaimed finite slot. `SetEntryRoute` and `SetRetreatRoute` bind reviewed scene routes. Karden remains at the rescue site unless a route is explicitly supplied. `HasExited` means physical arrival; the director owns roster, protected-target, ceasefire, and outside-area hold confirmation. `ConfigureRock(false,landingY)` is the default fixed-debris behavior; only the later authored falling wave uses `true`.

Owner-specific cancellation and global epoch invalidation include delayed thorns and pending split children. Split/launch sub-frame sweeps use the pilot samples for their actual time intervals. Terrain and arena bounds are checked for charges/control; connected4wu escape corridors, visibility, protected-route separation, and visual warning clarity still need the authored scene review and Unity play validation. No gameplay, full-project Unity compilation, or final balance pass is claimed by the console checks.

`E-EAGLE` uses configurable initial diagonal directions `(-15,-6)`, `(0,0)`, `(15,6)` in degrees. These are explicitly authorized Step 4 prototype authoring values; the approved design table contained three diagonal shots and a 0.8-second warning but no angles. The original approved table values and notes remain in the profile catalog.
