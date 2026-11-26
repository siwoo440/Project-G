# Story persistence contract tests

This console harness compiles the actual `Assets/ProjectG/Runtime/FlightStoryProgress.cs` source. It has no third-party test framework and does not use a replacement implementation. Run it with a .NET 10 SDK:

```bash
dotnet run --project tests/StoryPersistence.Tests/StoryPersistence.Tests.csproj
dotnet build tests/StoryPersistence.Tests/Api/StoryPersistence.Api.csproj
```

Both projects enforce C# 9.0. The separate API project compiles the same source against .NET Standard 2.1. The source-free NuGet configuration requires the SDK's .NET 10 references and the standard Microsoft `NETStandard.Library.Ref` 2.1.0 targeting pack to be installed or cached.

The executable cases cover defensive snapshots, safe precombat checkpoints, combat/mission boundaries, incremental mission and chapter completion, complete C8 transaction shape, failed/crashed attempts, recollection isolation, canonical E-S04 ending, and the FINAL-clear research predicate. Repeated mutations of returned arrays must not change prior state. A previous successful C8 attempt cannot supply a missing event in a later attempt's commit proposal.

The public merge rules validate the durable transaction's shape. They do not generate or authenticate physical rescue, arrival, stability, ACK, or encounter-resolution evidence. Those live requirements belong to the story director and mission rules.

## Verification recorded on 2026-11-26

- Pure production-source console cases: **31 passed, 0 failed** on .NET SDK 10.0.404 / runtime 10.0.15.
- The same source compiled against .NET Standard 2.1 with C# 9.0: **0 warnings, 0 errors**.
- Red phases observed before the implementation/fixes: missing production contract; three unsafe checkpoint/recollection cases; C01-S02 incorrectly treated as combat evidence. Each failing phase was followed by a green run.
- Actual store integration tests below: **14 authored, not executed**. Unity Editor and engine reference assemblies are unavailable in the current environment.
- Root checkout diff check for this slice completed without whitespace errors.

The local verification logs and source-hash manifest are recorded outside the checkout in `project_g_combat/test_environment`: `persistence-red.log`, `persistence-boundaries-red.log`, `persistence-prerequisite-red.log`, `persistence-green.log`, `persistence-api-build.log`, and `persistence-verification.json`.

## Actual store integration tests

`Assets/ProjectG/Tests/PlayMode/FlightStoryProgressTests.cs` contains NUnit reflection tests against the actual `FlightProgressStore` in Unity. These cover:

- Prepared run identity, matching pending retries, and no payment before actual participation.
- Real JsonUtility serialization and file reopen, old version-1 JSON, and legacy method behavior.
- Atomic file-write failure followed by retry, including inventory and C8 progress.
- Historical receipt replay during a newer pending run and defensive receipt copies.
- Normal failure retaining an earned FINAL part, crash recovery omitting new drops/research, and existing mastery-threshold tokens.
- Postcombat narrative completion without repeated combat rewards.
- Ordinary success versus FINAL success, and unseen recollection isolation.

Run these together with the existing Unity tests in **Unity 6000.3.21f1**. The console harness does not execute Unity serialization, engine APIs, File.Replace integration inside Unity, scenes, lifecycle callbacks, or PlayMode. Successful console/API checks must not be reported as a Unity build or PlayMode pass.
