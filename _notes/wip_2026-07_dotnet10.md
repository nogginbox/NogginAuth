# WIP: Upgrade to .NET 10

Branch: `feature/dotnet10`

## Introduction

This branch upgrades the NogginAuth solution from .NET 8.0 to .NET 10. All three projects (Noggin.NetCoreAuth, Noggin.NetCoreAuth.Tests, Noggin.SampleSite) currently target `net8.0`.

## Context & Shared Knowledge

- Branch was created from `origin/main` at commit `f0150e9` ("PR #34 - Option to configure callback URL host for a provider"); no commits had been made on this branch when it started.

## Plan

1. Bump `TargetFramework` to `net10.0` in all 3 `.csproj` files.
2. Update stale `<PackageReleaseNotes>` in `Noggin.NetCoreAuth.csproj`.
3. Bump runtime-tied packages in `Directory.Packages.props` to `10.0.x` (EF Core Design/Sqlite/Tools, Extensions.Configuration.Binder, Extensions.Logging.Debug, Extensions.Options.ConfigurationExtensions, VS Web CodeGeneration.Design). Left `Flurl.Http`, `NSubstitute`, `xunit.v3`, `xunit.runner.visualstudio`, `BuildBundlerMinifier`, `BrowserLink`, `Azure.Containers.Tools.Targets`, `Microsoft.NET.Test.Sdk` untouched initially — framework-agnostic, restored fine as-is (all later bumped to latest anyway, see Decisions).
4. Update `Noggin.SampleSite/Dockerfile` base images to `10.0`.
5. Update both GitHub workflow files' `dotnet-version` to `10.0.x`. (`azure-pipelines.yml` has no explicit SDK pin — left alone.)
6. Build, test, smoke-test the sample site.

## Decisions

- **Dockerfile COPY-order bug:** Fixed a pre-existing bug found during verification — the Dockerfile never copied `Directory.Packages.props` before `dotnet restore`, so restore failed with `NU1015` under central package management. Added a `COPY ["Directory.Packages.props", "."]` line before the restore step. Existed since the .NET 8.0 upgrade (commit `456fe19`), unrelated to .NET 10, but blocked verifying the Docker build so it was fixed here with the user's OK.

- **Flurl.Http 3.2.4 → 4.0.2:** User asked to update Flurl to latest. Real breaking change: Flurl 4.0 dropped Newtonsoft.Json as its default JSON serializer for System.Text.Json (STJ). All 10 provider model classes (GitHub/Google/Microsoft/Facebook `AccessTokenResult`, `UserResult`, `ErrorResult`, etc.) used Newtonsoft's `[JsonProperty(PropertyName = "...")]` to map snake_case/special field names (`access_token`, `avatar_url`, `@odata.context`, ...) — migrated to STJ's `[JsonPropertyName("...")]`, `using Newtonsoft.Json;` removed everywhere. Confirmed via Flurl's source (`DefaultJsonSerializer.cs`) that its STJ serializer defaults to `PropertyNameCaseInsensitive = true` when unconfigured, so properties differing only by case (`Scope`/`scope`, `Sub`/`sub`, `DisplayName`/`displayName`, etc.) needed no attribute changes — only underscore/special-character names did. Bonus: removed the transitive `Newtonsoft.Json` 12.0.2 high-severity vulnerability. Verified with 3 throwaway tests feeding real snake_case/camelCase JSON via `HttpTest.RespondWith(rawJsonString)` (not `RespondWithJson(object)`, which round-trips through our own serializer and wouldn't catch a casing regression) — all passed, then deleted.

- **Testing packages → latest:** Bumped `Microsoft.NET.Test.Sdk` 18.4.0 → 18.8.1 and `NSubstitute` 5.3.0 → 6.0.0. `xunit.v3`/`xunit.runner.visualstudio` were already latest. NSubstitute 6.0's breaking changes are legacy-API removal + its own TFM bump — codebase only uses `Substitute.For<T>()`/`.Returns(...)`, unaffected. No test code changes needed.

- **Full package review:** Confirmed all NuGet packages and the TFM are fully on .NET 10 / latest — nothing package-related outstanding. Remaining items are optional modernization, not upgrade work (see Possible future work).

- **Nullable enabled in Tests and SampleSite:** User asked to enable `<Nullable>enable</Nullable>` in `Noggin.NetCoreAuth.Tests` and `Noggin.SampleSite` (only `Noggin.NetCoreAuth` had it). Test project had zero nullable warnings. SampleSite surfaced 11: fixed properly rather than suppressed —
  - `User.Name`, `UserAuthAccount.Id`/`Provider`/`UserName`, and `IndexViewModel.User`/`AllUsers`/`UnlinkedProviders` marked `required` (all always set via object initializers at every construction site).
  - `IndexViewModel.UnlinkedProviders` setter changed from `internal set` to `set` — a `required` member's setter can't be less visible than its public containing type.
  - `UsefulLoginExtensions.FindUserWithProvider` return type changed `User` → `User?` (its `FirstOrDefault` can legitimately return null; the one caller already null-checks it).
  - `SampleLoginHandler.cs` and `_Layout.cshtml`: `User.Identity.IsAuthenticated` → `User.Identity?.IsAuthenticated == true` (or `!= true`) in both places, since `ClaimsPrincipal.Identity` is nullable.
  - `SampleLoginHandler.cs`: the `FirstOrDefault` fetching the already-authenticated user by claim now throws `AuthenticationException` if null (mirrors the same guard pattern already used in `UserAdminController.Index()`), instead of an unchecked dereference.
  - Verified: `dotnet build` 0 errors (only pre-existing unrelated `CS8981` migration-name warnings left), `dotnet test` 21/21 passing, sample site smoke-tested (home page 200, exercises the `_Layout.cshtml` fix on every request).

## Possible future work (out of scope for this branch)

- Confirm `azure-pipelines.yml`'s `windows-latest` hosted agent image includes the .NET 10 SDK before merging — it has no explicit SDK version pin.
- User needs to separately update "the instructions" (exact scope TBD — docs/CLAUDE.md/team instructions referencing the old setup?) to reflect this upgrade. Still deciding scope, not started.
- Modernizing `Startup.cs` to the minimal hosting model (still works fine as-is on .NET 10) — user said "might be for later", explicitly deferred.
- Remaining pre-existing NuGet security advisories unrelated to the TFM bump: `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (transitive via EF Core Sqlite, high severity), `NuGet.Packaging`/`NuGet.Protocol` 6.12.1 (transitive via CodeGeneration.Design, low severity). (`Newtonsoft.Json` advisory is resolved — see Flurl.Http decision above.)

---

## Work Log

*Add entries here as tasks are completed. Format: `[date] - What was done and why`*

- 2026-07-27 — Branch created, WIP file started.
- 2026-07-27 — TFM bumped to `net10.0` across all 3 projects, packages/Dockerfile/CI updated per Plan, verified (build/test/sample-site smoke test/docker build). Committed `1c1a95f`.
- 2026-07-27 — Flurl.Http bumped to 4.0.2, JSON attributes migrated (see Decisions). Committed `4226090`.
- 2026-07-27 — Testing packages bumped to latest (see Decisions). Committed `1b13529`.
- 2026-07-27 — Full package review done, no further packages outstanding; deferred items recorded. Committed `2e2d66d`.
- 2026-07-27 — Enabled `<Nullable>enable</Nullable>` in Tests and SampleSite; fixed the 11 warnings surfaced (see Decisions). Not yet committed.

---

## TODO / Up Next

- [ ] Commit the nullable-enable changes (Tests + SampleSite).
- [ ] Confirm `azure-pipelines.yml`'s hosted agent has .NET 10 SDK available (see Possible future work).
- [ ] User to decide scope of "instructions" update needed for this upgrade (see Possible future work).
