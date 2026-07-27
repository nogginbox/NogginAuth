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

**DECIDED (2026-07-27):** Bumped `Flurl.Http` 3.2.4 → 4.0.2 (user requested "update to latest" after the initial upgrade landed). This is a major version with a real breaking change: Flurl 4.0 dropped Newtonsoft.Json as its default JSON serializer in favor of System.Text.Json (STJ). All 10 provider model classes (GitHub/Google/Microsoft/Facebook `AccessTokenResult`, `UserResult`, `ErrorResult`, etc.) used Newtonsoft's `[JsonProperty(PropertyName = "...")]` to map snake_case/special API field names (`access_token`, `avatar_url`, `@odata.context`, ...) — these were migrated to STJ's `[JsonPropertyName("...")]`, and all `using Newtonsoft.Json;` imports removed. Confirmed via Flurl's source (`DefaultJsonSerializer.cs`) that its STJ-backed serializer defaults to `PropertyNameCaseInsensitive = true` when unconfigured, so the many properties that only differed by case (`Scope`/`scope`, `Sub`/`sub`, `DisplayName`/`displayName`, etc., relying on Newtonsoft's case-insensitive default) needed no attribute changes — only the underscore/special-character names needed the attribute swap. As a bonus this also removed the transitive `Newtonsoft.Json` 12.0.2 high-severity vulnerability entirely.
  - Verified with 3 temporary throwaway tests (not committed) using real snake_case/camelCase JSON shapes matching actual GitHub/Google/Microsoft API responses fed via `HttpTest.RespondWith(rawJsonString)` — deliberately not `RespondWithJson(object)`, since that round-trips through our own serializer symmetrically and wouldn't catch a casing regression. All passed, then the temp file was deleted.
  - `NSubstitute` 5.3.0 → 6.0.0 was considered but not done at the time — not requested yet. (Since done — see below.)

**DECIDED (2026-07-27):** User asked to update the remaining testing-related NuGet packages to latest and fix anything that broke. Bumped `Microsoft.NET.Test.Sdk` 18.4.0 → 18.8.1 and `NSubstitute` 5.3.0 → 6.0.0. `xunit.v3` (3.2.2) and `xunit.runner.visualstudio` (3.1.5) were already at latest stable — left unchanged. NSubstitute 6.0's main breaking changes are around legacy/obsolete API removal and its own TFM bump to net8.0/netstandard2.0; this codebase only uses `Substitute.For<T>()` and `.Returns(...)`, none of the removed legacy API, so nothing needed changing in test code. `dotnet build` 0 errors, `dotnet test` 21/21 passing — no test fixes were needed.

### Possible future work (out of scope for this branch)

- Modernizing `Startup.cs` to the minimal hosting model (still works fine as-is on .NET 10).
- Addressing remaining pre-existing NuGet security advisories unrelated to the TFM bump: `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (transitive via EF Core Sqlite, high severity), `NuGet.Packaging`/`NuGet.Protocol` 6.12.1 (transitive via CodeGeneration.Design, low severity). (`Newtonsoft.Json` advisory is now resolved — see Flurl.Http 4.0.2 decision above.)

---

## Work Log

*Add entries here as tasks are completed. Format: `[date] - What was done and why`*

- 2026-07-27 — Branch created, WIP file started.
- 2026-07-27 — Bumped `TargetFramework` to `net10.0` in all 3 csproj files; updated stale `PackageReleaseNotes`; bumped runtime-tied packages in `Directory.Packages.props` to `10.0.x`/`10.0.2`; updated Dockerfile base images and both GitHub workflow `dotnet-version` pins to `10.0`/`10.0.x`.
- 2026-07-27 — Verified: `dotnet build` (0 errors, only pre-existing nullable-annotation warnings), `dotnet test` (21/21 passing), `dotnet run` smoke test of `Noggin.SampleSite` (home page 200 OK, EF Core/SQLite loaded fine), `docker build` of the Dockerfile succeeds after fixing the `Directory.Packages.props` COPY-ordering bug (see Context section).
- 2026-07-27 — Committed the .NET 10 upgrade (commit `1c1a95f`, "Upgrade to .NET 10.0").
- 2026-07-27 — Bumped `Flurl.Http` to 4.0.2 and migrated 10 provider model classes from Newtonsoft's `[JsonProperty]` to System.Text.Json's `[JsonPropertyName]` (see Context section for why this was needed). `dotnet build` 0 errors, `dotnet test` 21/21 passing, plus 3 throwaway real-JSON-shaped verification tests passed and were removed.
- 2026-07-27 — Committed the Flurl.Http 4.0.2 + JSON attribute migration (commit `4226090`).
- 2026-07-27 — Bumped `Microsoft.NET.Test.Sdk` to 18.8.1 and `NSubstitute` to 6.0.0. No test code changes needed — `dotnet build` 0 errors, `dotnet test` 21/21 passing. Not yet committed.

---

## TODO / Up Next

- [ ] Commit the testing package bumps (Microsoft.NET.Test.Sdk, NSubstitute).
- [ ] Confirm `azure-pipelines.yml`'s hosted agent has .NET 10 SDK available (see Open questions).
