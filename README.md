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
  read and measure; nothing is deleted until an explicit `CleanExecutor.ExecuteAsync` call,
  and deletions go to the Recycle Bin rather than being removed outright.
- `src/Sweepbench.App` — WPF (MVVM) shell. Currently implements the Health Check screen;
  Registry, Startup, Uninstall, Duplicates, Disk Map, and Erase are Phase 2+ (see the
  roadmap below) and appear in the sidebar as placeholders.
- `tests/Sweepbench.Core.Tests` — xUnit tests for the engine.

**Phase 1 cleaners implemented today:** temp files (user + Windows temp), browser cache
(Chrome, Edge, Firefox), Recycle Bin, Windows Update download cache.

## Design & roadmap

Full architecture diagram, UI mockups, live disk-usage tree, and the phased build plan:
https://claude.ai/code/artifact/a95df78c-64cd-4696-9819-16b055a03c96
