# aweXpect.Migration agent guide

aweXpect.Migration ships Roslyn analyzers and code fixers that rewrite FluentAssertions and xUnit assertions to [aweXpect](https://github.com/aweXpect/aweXpect). The NuGet package `aweXpect.Migration` contains only the two analyzer assemblies, no runtime code.

## SDK and build settings

- `global.json` pins the .NET SDK `10.0.300` with `rollForward: latestPatch`. Tests target `net8.0` and `net10.0`, so both runtimes must be installed (CI installs `8.0.x` and `10.0.3xx`).
- Package versions are managed centrally in `Directory.Packages.props`; never put a `Version` on a `PackageReference`.
- `Source/Directory.Build.props` sets `netstandard2.0`, `LangVersion latest`, XML docs and `GeneratePackageOnBuild`. `Tests/Directory.Build.props` sets the test frameworks, TUnit and aweXpect. `global.json` runs `dotnet test` on the Microsoft.Testing.Platform.
- Build the whole solution with `dotnet build aweXpect.Migration.slnx`.

## Design goals

Correctness is a must. The other goals are strong defaults and win in this order when they conflict: stable and compatible, host compatible, a consistent and pleasant API, a fast happy path, extensible, errors at compile time, documented. [Docs/agents/design-goals.md](Docs/agents/design-goals.md) explains each goal, the code rules, the checks before a commit and the goals of this repository.

## Project constraints

- Analyzer and code fixer projects target `netstandard2.0` only; they run inside the consumer's compiler and IDE. Keep `EnforceExtendedAnalyzerRules` satisfied: no file system, network or other banned APIs in the analyzers.
- Roslyn floor: `MinimumRoslynVersion` in `Directory.Build.props` is the minimum compiler a consumer needs. The `Microsoft.CodeAnalysis.*` references in `Source/` use it as `VersionOverride`, so an API above the floor fails the build; the tests keep the newer versions from `Directory.Packages.props`. A compiler older than the referenced Roslyn skips the analyzer with `CS9057`. Raise the floor only when a needed API requires it, after checking which SDKs still have to be supported. The unit tests load Roslyn in-process and do not catch this; look for `CS9057` in the build output of `aweXpect.Migration` and `aweXpect.Migration.Example.Tests`.
- Diagnostic IDs use the `aweXpectM` prefix with three digits (`aweXpectM002` FluentAssertions, `aweXpectM003` xUnit), all in the `Usage` category, declared in `Source/aweXpect.Migration.Analyzers/Rules.cs`. Titles, messages and descriptions live in `Resources.resx` as `<id>Title`, `<id>MessageFormat` and `<id>Description`. Never reuse or renumber an ID; `aweXpect0001` belongs to the aweXpect package itself.
- Analyzer release tracking: `Microsoft.CodeAnalysis.Analyzers` picks up `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md`. Descriptors are created through a helper, so the build does not flag missing entries (RS2000); add new or changed rules to `AnalyzerReleases.Unshipped.md` by hand.
- A code fix must be faithful: when a rewrite would drop an argument or change semantics silently, return the unchanged document so no fix is offered. Documented behavioral differences are fine.
- Every supported rewrite has a row in `Docs/pages/01-fluentassertions.md` or `Docs/pages/02-xunit.md`, plus an entry under "Behavioral differences" when the result behaves differently. These pages are published to the docs site.

## Validation

- Run `dotnet test --project Tests/aweXpect.Migration.Tests` before pushing. For a quick loop, add `-f net10.0 --treenode-filter "/*/*/<Class>/*"`.
- Analyzer tests use `CSharpAnalyzerVerifier<TAnalyzer>` with `{|#0:...|}` markup and `Verifier.Diagnostic(Rules.X).WithLocation(0)`.
- Code fix rewrites are data-driven: add a case to `Tests/aweXpect.Migration.Tests/FluentAssertions/TestCases.cs` (`AddWithBecause`, a `{0}` placeholder generates the variant with a `because` argument) or `Tests/aweXpect.Migration.Tests/Xunit/TestCases.cs` (`AddTestCase`). Cases that must not get a fix go to `TestCases.NoCodeFix` or a dedicated `..._ShouldNotOfferCodeFix` test.
- `aweXpect.Migration.Example.Tests` is a manual playground, not part of the pipeline's unit tests.
- SonarCloud must report no issues and coverage must stay above 90% (see `CONTRIBUTING.md`). Ask before adding a `#pragma` or other suppression.

## Repository conventions

- Line endings: `.editorconfig` sets CRLF and tab indentation; the index stores LF (`* text=auto`). Raw string literals in tests take the checkout's line endings, which is why `FluentAssertionsCodeFixProviderTests` derives `SourceNewLine` instead of hard-coding `\r\n`. Edit such files with an editor tool, not `sed -i`.
- BOM: most C# files under `Source/` and `Tests/` start with a UTF-8 BOM, others do not. Keep each file's existing state. PowerShell rewrites (`Set-Content`, `Out-File`) add or strip BOMs; check `git diff` for a changed first line (U+FEFF) before committing.
- No em-dashes in docs, README or XML documentation comments.

## Documentation

Pages under `Docs/pages` are published on docs.testably.org and follow the [docs style guide](https://github.com/Testably/Testably.Site/blob/main/Docs/style-guide.md). The key rules:

- US English, sentence case for headings and labels, no em dashes, one line per paragraph.
- A `description` in the front matter of every page; the H1 is the title; order with unique `NN-` prefixes.
- Relative file links within the repository, site-absolute `/Library/...` links across libraries.
- Install instructions only through `<PackageInstall package="..." />`, never hand-written commands or NuGet badges.
- Examples check results with `await That(...)`; complete examples use TUnit `[Test]`.

The pages are published as the migration section of aweXpect (`/aweXpect/migration/`), next to the upgrade guides of the aweXpect repository, which `00-index.md` links with relative paths as they are on the site. Migration pages show the code of the source library before the rewrite. `README.md` is the GitHub and NuGet landing page and is maintained separately from `Docs/pages/00-index.md`; keep their content in sync by hand.

## Git workflow

- Work on a feature branch and open a pull request against `main`; PRs are squash-merged and the PR title becomes the commit subject.
- Use conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `ci:`, `chore:` (Dependabot uses `chore:`).
- The commit subject is the PR title; the body explains why, not what. Write each paragraph on a single line and add no co-author or attribution trailers.

## References

- [Docs/agents/design-goals.md](Docs/agents/design-goals.md): the goals and code rules for every change, the checks before a commit and the goals of this repository.
- [Docs/agents/workflows.md](Docs/agents/workflows.md): build, test and documentation commands, pipeline targets and CI workflows.
- [Docs/agents/project-structure.md](Docs/agents/project-structure.md): projects, source locations and how to add a migration rule end to end.
