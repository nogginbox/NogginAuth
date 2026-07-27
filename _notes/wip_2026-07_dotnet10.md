# WIP: Upgrade to .NET 10

Branch: `feature/dotnet10`

## Introduction

This branch upgrades the NogginAuth solution from .NET 8.0 to .NET 10. All three projects (Noggin.NetCoreAuth, Noggin.NetCoreAuth.Tests, Noggin.SampleSite) currently target `net8.0`.

## Context & Shared Knowledge

Decisions and findings that anyone picking this up needs to know:

- Branch was created from `origin/main` at commit `f0150e9` ("PR #34 - Option to configure callback URL host for a provider"); no commits have been made on this branch yet.

## Plan

1. Bump `TargetFramework` to `net10.0` in all 3 `.csproj` files.
2. Update stale `<PackageReleaseNotes>` in `Noggin.NetCoreAuth.csproj`.
3. Bump runtime-tied packages in `Directory.Packages.props` to `10.0.x` (EF Core Design/Sqlite/Tools, Extensions.Configuration.Binder, Extensions.Logging.Debug, Extensions.Options.ConfigurationExtensions, VS Web CodeGeneration.Design). Left `Flurl.Http`, `NSubstitute`, `xunit.v3`, `xunit.runner.visualstudio`, `BuildBundlerMinifier`, `BrowserLink`, `Azure.Containers.Tools.Targets`, `Microsoft.NET.Test.Sdk` untouched — framework-agnostic, restored fine as-is.
4. Update `Noggin.SampleSite/Dockerfile` base images to `10.0`.
5. Update both GitHub workflow files' `dotnet-version` to `10.0.x`. (`azure-pipelines.yml` has no explicit SDK pin — left alone.)
6. Build, test, smoke-test the sample site.

**DECIDED (2026-07-27):** Fixed a pre-existing Dockerfile bug found during verification — it never copied `Directory.Packages.props` before `dotnet restore`, so restore failed with `NU1015` (no versions found) under central package management. Added a `COPY ["Directory.Packages.props", "."]` line before the restore step. This bug existed since the .NET 8.0 upgrade (commit `456fe19`) and was unrelated to .NET 10, but blocked verifying the Docker build so it was fixed on this branch with the user's OK.

### Open questions

- [ ] `azure-pipelines.yml` has no explicit SDK version pin — confirm the `windows-latest` hosted agent image includes the .NET 10 SDK before merging (not fixed here, just flagged).

### Possible future work (out of scope for this branch)

- Modernizing `Startup.cs` to the minimal hosting model (still works fine as-is on .NET 10).
- Addressing pre-existing NuGet security advisories unrelated to the TFM bump: `Newtonsoft.Json` 12.0.2 (transitive, high severity), `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (transitive via EF Core Sqlite, high severity), `NuGet.Packaging`/`NuGet.Protocol` 6.12.1 (transitive via CodeGeneration.Design, low severity).
- Bumping the remaining non-runtime-tied packages to their latest majors (e.g. `Flurl.Http` 3.2.4 → 4.0.2, `NSubstitute` 5.3.0 → 6.0.0) — deferred since they build fine as-is and major bumps risk breaking changes outside this upgrade's scope.

---

## Work Log

*Add entries here as tasks are completed. Format: `[date] - What was done and why`*

- 2026-07-27 — Branch created, WIP file started.
- 2026-07-27 — Bumped `TargetFramework` to `net10.0` in all 3 csproj files; updated stale `PackageReleaseNotes`; bumped runtime-tied packages in `Directory.Packages.props` to `10.0.x`/`10.0.2`; updated Dockerfile base images and both GitHub workflow `dotnet-version` pins to `10.0`/`10.0.x`.
- 2026-07-27 — Verified: `dotnet build` (0 errors, only pre-existing nullable-annotation warnings), `dotnet test` (21/21 passing), `dotnet run` smoke test of `Noggin.SampleSite` (home page 200 OK, EF Core/SQLite loaded fine), `docker build` of the Dockerfile succeeds after fixing the `Directory.Packages.props` COPY-ordering bug (see Context section).

---

## TODO / Up Next

- [ ] Commit the changes (not yet committed — waiting for user request).
- [ ] Confirm `azure-pipelines.yml`'s hosted agent has .NET 10 SDK available (see Open questions).
