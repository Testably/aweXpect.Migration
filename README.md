# aweXpect.Migration

[![Nuget](https://img.shields.io/nuget/v/aweXpect.Migration)](https://www.nuget.org/packages/aweXpect.Migration)
[![Build](https://github.com/Testably/aweXpect.Migration/actions/workflows/build.yml/badge.svg)](https://github.com/Testably/aweXpect.Migration/actions/workflows/build.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Testably_aweXpect.Migration&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Testably_aweXpect.Migration)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Testably_aweXpect.Migration&metric=coverage)](https://sonarcloud.io/summary/overall?id=Testably_aweXpect.Migration)

A Roslyn analyzer and code-fix provider that migrates [FluentAssertions](https://fluentassertions.com/)
and [xUnit](https://xunit.net/) assertions to [aweXpect](https://github.com/aweXpect/aweXpect). Drop the
package into a project that uses one of those libraries and the analyzer flags each assertion; the
accompanying code fix rewrites the call site to its aweXpect equivalent.

## Installation

Install [aweXpect](https://www.nuget.org/packages/aweXpect) (the assertion library you are migrating
*to*) and `aweXpect.Migration` (the analyzers and code fixers) into the test project you want to
migrate:

```shell
dotnet add package aweXpect
dotnet add package aweXpect.Migration
```

`aweXpect.Migration` ships only the analyzer and code fixer, no runtime code.

It is usually convenient to add the following global usings to the project so the rewritten code
compiles without further edits:

```csharp
global using System.Threading.Tasks;
global using aweXpect;
```

## How it works

After installing the package, every assertion of the source library is reported as a warning:

| Diagnostic     | Source library   | Code fix title                         |
|----------------|------------------|----------------------------------------|
| `aweXpectM002` | FluentAssertions | *Migrate FluentAssertions to aweXpect* |
| `aweXpectM003` | xUnit            | *Migrate xUnit assertion to aweXpect*  |

`aweXpectM002` is raised on every `.Should()` invocation defined under the `FluentAssertions`
namespace (nested `.Should()` calls inside lambda arguments are intentionally skipped, so chains like
`.Should().AllSatisfy(x => x.Should().BeGreaterThan(0))` are migrated as a single unit).
`aweXpectM003` is raised on every method call on `Xunit.Assert`.

The code fix is offered only where the fixer knows a rewrite. An assertion without one, for example a
chained call it does not recognise, keeps its warning and has to be migrated by hand.

A typical migration looks like this:

```csharp
// Before
subject.Should().BeTrue();
Assert.Equal(expected, actual);

// After
await Expect.That(subject).IsTrue();
await Expect.That(actual).IsEqualTo(expected);
```

Expectations in aweXpect only run when they are awaited. The rewritten expressions are therefore
awaited in a second step, which the `aweXpect0001` analyzer that ships with `aweXpect` itself enforces
and fixes.

### In the IDE

Apply "Fix all in solution" for `aweXpectM002` and `aweXpectM003` from your IDE (Visual Studio, Rider,
VS Code with C# Dev Kit), then "Fix all" for `aweXpect0001` to add the missing `await` and mark the
surrounding methods `async`.

### From the command line

Migration is a two-pass operation: first apply the rewrites, then add the missing `await` and `async`
keywords:

```shell
# 1. Rewrite assertions
dotnet format analyzers --diagnostics aweXpectM002 aweXpectM003 --severity warn

# 2. Add the missing `await` (and mark surrounding methods `async`)
dotnet format analyzers --diagnostics aweXpect0001 --severity warn
```

### Supported assertions

The rewrites for each source library, and the cases in which the migrated expectation behaves
differently, are listed on separate pages:

- [Migrating from FluentAssertions](Docs/pages/01-fluentassertions.md)
- [Migrating from xUnit](Docs/pages/02-xunit.md)

## Removing `aweXpect.Migration` again

Once the warnings in a project have been resolved, drop the analyzer reference:

```shell
dotnet remove package aweXpect.Migration
```

`aweXpect` itself stays in the project; that is the assertion library the tests now depend on.
