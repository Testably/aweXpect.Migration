# Development workflows

## Build and test

```powershell
dotnet build aweXpect.Migration.slnx
dotnet test --project Tests/aweXpect.Migration.Tests
dotnet test --project Tests/aweXpect.Migration.Tests -f net10.0 --treenode-filter "/*/*/XunitAssertionCodeFixProviderTests/*"
```

- The test projects use TUnit and are executables of the Microsoft.Testing.Platform, which `global.json` selects for `dotnet test`. Filter with `--treenode-filter "/<assembly>/<namespace>/<class>/<method>"`; `dotnet run --project <project> -f net10.0 -- <options>` starts a single project directly.
- Tests target `net8.0` and `net10.0`. Pass `-f` when only one runtime is installed.
- The verifiers compile the test code against `ReferenceAssemblies.Net.Net100` plus the NuGet packages `xunit.v3` 1.1.0 and `FluentAssertions` 8.2.0 (7.2.0 for the `Legacy` cases). The first run downloads them into the NuGet cache, so it needs network access.
- `Tests/aweXpect.Migration.Example.Tests` references the analyzers as a consumer would. Build it to see the diagnostics in a real project and to spot `CS9057` (Roslyn floor too high for the compiler in use). It is not run by the pipeline. It runs on TUnit like the other tests and references only the assertion package `xunit.v3.assert`, so that `aweXpectM003` has xUnit assertions to report.

## Build pipeline

`build.ps1` (Windows), `build.sh` (Linux and macOS) and `build.cmd` build `Pipeline/Build.csproj`, a [Fallout](https://www.nuget.org/packages/Fallout.Common) build project, and run it with the given target. The SDK from `global.json` is downloaded into `.fallout/temp` if `dotnet` is not on the path.

```powershell
./build.ps1 --help        # list targets
./build.ps1 UnitTests
./build.ps1 Pack
```

Without a target the default `CodeAnalysis` runs, which starts a SonarCloud scan and needs `SonarToken`; always pass a target locally. Locally the configuration is `Debug`, on CI `Release`.

| Target         | Defined in              | What it does                                                                                                                                                                                                                  |
|----------------|-------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Compile`      | `Build.Compile.cs`      | Cleans `Artifacts/` and `TestResults/`, restores without cache, computes the version with GitVersion, builds the two analyzer projects and then the solution.                                                                 |
| `UnitTests`    | `Build.UnitTest.cs`     | Starts `aweXpect.Migration.Tests` with `dotnet run` once per target framework and writes `TestResults/<project>_<tfm>.trx` and `TestResults/<project>_<tfm>.cobertura.xml`.                                                   |
| `CodeCoverage` | `Build.CodeCoverage.cs` | Runs `UnitTests` and merges the coverage into `TestResults/reports/SonarQube.xml` (Sonar generic coverage format) with ReportGenerator.                                                                                      |
| `CodeAnalysis` | `Build.CodeAnalysis.cs` | Wraps `Compile` and `CodeCoverage` in a SonarCloud scan (project `Testably_aweXpect.Migration`). The scan is only completed on a server build.                                                                               |
| `UpdateReadme` | `Build.Pack.cs`         | Writes `Artifacts/README.md`, the package readme: adds a changelog badge, drops the build and quality gate badges and rewrites `Docs/pages/` links to absolute GitHub URLs.                                                     |
| `Pack`         | `Build.Pack.cs`         | Runs `UpdateReadme` and `Compile`, then moves the `.nupkg` and `.snupkg` of `aweXpect.Migration` to `Artifacts/Packages`.                                                                                                     |

GitVersion needs the full git history and tags, which is why CI checks out with `fetch-depth: 0`. Local builds get a `-DEV` pre-release suffix, CI builds `-CI`, and tag builds the suffix of the tag.

## CI workflows

| Workflow                                | Trigger                                  | Jobs                                                                                                                                                                                                                                             |
|-----------------------------------------|------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `.github/workflows/ci.yml`              | Pull requests to `main`, manual          | `Unit tests` on ubuntu, windows and macos (`CodeCoverage` target), `Static code analysis` (`CodeAnalysis`, skipped for Dependabot and forks), `Publish Tests Results`.                                                                           |
| `.github/workflows/build.yml`           | Push to `main`, tags `v*.*.*`, manual    | The same test and analysis jobs plus `Pack`. On a tag, `Push` publishes the packages to nuget.org with NuGet trusted publishing and creates a GitHub release for non-pre-release tags; `Finalize release` comments on the released issues and PRs. |
| `.github/workflows/notify-docs-site.yml`| Successful `Build` on `main`, manual     | Dispatches `extension-documentation-updated-event` to `Testably/Testably.Site`, which rebuilds the documentation.                                                                                                                                |

- Actions are pinned to full commit SHAs with the version in a trailing comment; keep that when adding or updating an action.
- Mergify (`.github/mergify.yml`) squash-merges Dependabot PRs once the three `Unit tests` checks pass. Dependabot (`.github/dependabot.yml`) runs weekly for GitHub Actions and NuGet, with groups for `Fallout*`, `tunit*`, `xunit*` and `aweXpect*`.

## Documentation

- `README.md` is the package readme (through `UpdateReadme`) and the GitHub landing page. `Docs/pages/00-index.md` is the landing page of the docs site and is maintained separately; keep both in sync by hand. Both follow the [docs style guide](https://github.com/Testably/Testably.Site/blob/main/Docs/style-guide.md); the README links to the docs with full `https://docs.testably.org/...` URLs.
- `Docs/pages/` is published to https://docs.testably.org/aweXpect/migration/ by `Testably/Testably.Site`; `_category_.json` sets the sidebar label and position. There is no local docs build and code fences are not compiled, so check snippets against the test cases by hand.
- `Docs/agents/` holds these guides for contributors and agents and is not published.
