# CCleaner-Free-Open-Source
Open-source system cleaning and optimization tool inspired by CCleaner. Safely remove temporary files, caches, browser data, and other unnecessary files while giving users transparency, control, and a lightweight way to keep their systems clean and optimized.

Working product name: **Sweepbench**.

## Getting started

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows.

```
dotnet build Sweepbench.sln
dotnet run --project src/Sweepbench.App
dotnet test tests/Sweepbench.Core.Tests
```

## Layout

- `src/Sweepbench.Core` — UI-agnostic scan/clean engine. `ICleaner` implementations only
  read and measure; nothing is deleted until an explicit `CleanExecutor.ExecuteAsync` call.
  File/folder deletions go to the Recycle Bin; registry deletions are backed up to a JSON
  undo log first (`Registry/RegistryBackupWriter.cs`).
- `src/Sweepbench.App` — WPF (MVVM) shell with real navigation between four working
  screens (Health Check, Registry, Startup, Uninstall). Duplicates, Disk Map, and Erase
  are Phase 3 and appear in the sidebar as disabled placeholders.
- `tests/Sweepbench.Core.Tests` — xUnit tests for the engine (39 tests).

**Phase 1 — file cleanup:** temp files (user + Windows temp), browser cache (Chrome,
Edge, Firefox), Recycle Bin, Windows Update download cache.

**Phase 2 — registry, startup, uninstall:**
- *Registry*: MRU history (Run/TypedPaths/search — safe, auto-checked) plus orphaned
  App Paths and orphaned Add/Remove Programs entries (unchecked by default, deliberately
  conservative — see `RegistryPathHeuristics` for what counts as "verifiable"). MSI-managed
  entries are never touched; there's no file path to check against.
- *Startup*: reads the same four sources Task Manager's Startup tab does (HKCU/HKLM Run
  keys, user/common Startup folders) and toggles the same `StartupApproved` flag Windows
  itself uses — nothing is deleted or moved.
- *Uninstall*: lists installed apps from the registry (not `Win32_Product`/WMI, which
  triggers MSI reconfiguration as a side effect) and launches each app's own uninstaller.
  Deliberately doesn't attempt an automated "leftover file sweep" afterward — see
  `AppUninstaller` for why that's scope, not a gap.

**Known deviation from the original plan:** registry safety currently relies on the JSON
undo log rather than an automatic System Restore point (creating one reliably without
requiring elevation turned out to be its own project). Tracked as a follow-up.

## Design & roadmap

Full architecture diagram, UI mockups, live disk-usage tree, and the phased build plan:
https://claude.ai/code/artifact/a95df78c-64cd-4696-9819-16b055a03c96
